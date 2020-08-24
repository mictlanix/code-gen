using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mictlanix.CodeGen.Entities;
using RazorLight;
using System.Threading.Tasks;
using System.Reflection;

namespace Mictlanix.CodeGen.Core {
	class Program {
		static void Main (string [] args)
		{
			new Program ().Run (args).Wait ();
		}

		public async Task Run (string [] args)
		{
			var ns = args [0];
			var json = GetJson (args [1]);
			var output_directory = args [2];
			var entities = JsonConvert.DeserializeObject<List<Entity>> (json);
			var tpl_path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Templates");
			var engine = new RazorLightEngineBuilder ()
					.UseFileSystemProject (tpl_path)
				    .UseMemoryCachingProvider ()
				    .Build ();
			//var engine = EngineFactory.CreatePhysical (tpl_path);

			if (Directory.Exists (output_directory)) {
				Directory.Delete (output_directory, true);
			}

			Directory.CreateDirectory (output_directory);
			Directory.CreateDirectory (Path.Combine (output_directory, "Entities"));
			Directory.CreateDirectory (Path.Combine (output_directory, "Repositories"));

			File.WriteAllText (Path.Combine (output_directory, "IUnitOfWork.cs"),
					   await engine.CompileRenderAsync ("IUnitOfWork.cshtml", new { Namespace = ns }));
			File.WriteAllText (Path.Combine (output_directory, "UnitOfWork.cs"),
					   await engine.CompileRenderAsync ("UnitOfWork.cshtml", new { Namespace = ns, Entities = entities }));
			File.WriteAllText (Path.Combine (output_directory, "Repositories", "RepositoryBase.cs"),
					   await engine.CompileRenderAsync ("RepositoryBase.cshtml", new { Namespace = ns }));

			foreach (var entity in entities) {
				var model = new {
					entity.Name,
					entity.Table,
					entity.Properties,
					entity.Methods,
					entity.PluralName,
					Namespace = ns
				};

				File.WriteAllText (Path.Combine (output_directory, "Entities", entity.Name + ".cs"), await engine.CompileRenderAsync ("Entity.cshtml", model));
				File.WriteAllText (Path.Combine (output_directory, "Repositories", "I" + entity.PluralName + "Repository.cs"), await engine.CompileRenderAsync ("IRepository.cshtml", model));
				File.WriteAllText (Path.Combine (output_directory, "Repositories", entity.PluralName + "Repository.cs"), await engine.CompileRenderAsync ("Repository.cshtml", model));
			}
		}



		static string GetJson (string filename)
		{
			var result = JArray.Parse (File.ReadAllText (filename));
			var files = Directory.GetFiles (Path.GetDirectoryName (filename), "*.Custom.json");

			foreach (var file in files) {
				var obj = JObject.Parse (File.ReadAllText (file));
				var entity = result.Children<JObject> ().FirstOrDefault (x => x ["Name"].ToString () == obj ["Name"].ToString ());

				if (entity == null)
					continue;

				var props = obj ["Properties"] as JArray;
				var properties = entity ["Properties"] as JArray;

				obj.Remove ("Name");

				if (properties == null) {
					properties = new JArray ();
					entity ["Properties"] = properties;
				}

				if (props != null) {
					obj.Remove ("Properties");

					foreach (var prop in props) {
						var property = properties.FirstOrDefault (x => x ["Name"].ToString () == prop ["Name"].ToString ()) as JObject;

						if (property == null) {
							properties.Add (prop);
						} else {
							property.Merge (prop, new JsonMergeSettings {
								MergeArrayHandling = MergeArrayHandling.Union
							});
						}
					}
				}

				entity.Merge (obj, new JsonMergeSettings {
					MergeArrayHandling = MergeArrayHandling.Union
				});
			}

 			return result.ToString ();
		}
	}
}
