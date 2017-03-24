using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Mictlanix.CodeGen.Entities;

namespace Mictlanix.CodeGen.My2Json {
	class Program {
		static void Main (string [] args)
		{
			var settings = new JsonSerializerSettings {
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
				ContractResolver = new WritablePropertiesOnlyResolver ()
			};

			try {
				var entities = GetEntities (args [0]);
				var json = JsonConvert.SerializeObject (entities, settings);

				File.WriteAllText (args [1], json);
			} catch (MySqlException ex) {
				Console.WriteLine ("Error: {0}", ex);

			}
		}

		static List<Entity> GetEntities (string connStr)
		{
			var entities = new List<Entity> ();

			using (var conn = new MySqlConnection (connStr + ";Pooling=False;")) {
				conn.Open ();

				using (var cmd = conn.CreateCommand ()) {
					cmd.CommandText = "select * from information_schema.columns " +
						"where table_schema = '" + conn.Database + "'" +
						"order by table_name, ordinal_position";

					using (var reader = cmd.ExecuteReader ()) {
						var entity = new Entity ();

						while (reader.Read ()) {
							var table_name = reader ["TABLE_NAME"].ToString ();
							var column_name = reader ["COLUMN_NAME"].ToString ();
							var length = reader.IsDBNull (8) ? 0 : (ulong) reader ["CHARACTER_MAXIMUM_LENGTH"];
							var precision = reader.IsDBNull (10) ? 0 : (ulong) reader ["NUMERIC_PRECISION"];
							var scale = reader.IsDBNull (11) ? 0 : (ulong) reader ["NUMERIC_SCALE"];
							var prop = new Property {
								Name = column_name.ToPascalCase (),
								Column = column_name,
								DbType = reader ["DATA_TYPE"].ToString (),
								IsPrimaryKey = "PRI".Equals (reader ["COLUMN_KEY"]),
								IsNullable = "YES".Equals (reader ["IS_NULLABLE"]),
								Lenght = length == 0 ? null : (int?) length,
								Precision = precision == 0 ? null : (int?) precision,
								Scale = scale == 0 ? null : (int?) scale
							};

							prop.Type = GetManagedType (prop.DbType, length, prop.IsNullable);

							if (entity.Table != table_name) {
								entity = new Entity ();

								entity.Name = table_name.ToPascalCase ();
								entity.Table = table_name;

								entities.Add (entity);
							}

							if (prop.Type == "bool") {
								prop.Name = "Is" + prop.Name;
							}

							entity.Properties.Add (prop);
						}
					}

					cmd.CommandText = "select count(*) from information_schema.key_column_usage " +
							"where table_schema = '" + conn.Database + "' and constraint_name <> 'primary' and " +
							"table_name = @table and column_name = @column";
					cmd.Parameters.Add ("table", MySqlDbType.VarString);
					cmd.Parameters.Add ("column", MySqlDbType.VarString);

					foreach (var entity in entities) {
						var pk = entity.Properties.Where (x => x.IsPrimaryKey).ToList ();

						foreach (var prop in entity.Properties) {
							cmd.Parameters ["table"].Value = entity.Table;
							cmd.Parameters ["column"].Value = prop.Column;

							if ((long) cmd.ExecuteScalar () > 0) {
								prop.Name += "Id";
								prop.IsFilter = true;
							}
						}

						if (pk.Count () == 1) {
							pk [0].Name = "Id";
							pk [0].IsFilter = false;
						}
					}

				}
			}

			return entities;
		}

		static string GetManagedType (string dbType, ulong length, bool isNull)
		{
			switch (dbType.ToLower ()) {
			case "binary":
				return length == 16 ? (isNull ? "Guid?" : "Guid") : "byte[]";
			case "char":
				return length == 36 ? (isNull ? "Guid?" : "Guid") : (length == 1 ? (isNull ? "char?" : "char") : "string");
			case "int":
				return isNull ? "int?" : "int";
			case "tinyint":
				return isNull ? "bool?" : "bool";
			case "decimal":
				return isNull ? "decimal?" : "decimal";
			case "varchar":
			case "text":
				return "string";
			case "date":
			case "datetime":
				return isNull ? "DateTime?" : "DateTime";
			default:
				return "object";
			}
		}
	}

	class WritablePropertiesOnlyResolver : DefaultContractResolver {
		protected override IList<JsonProperty> CreateProperties (Type type, MemberSerialization memberSerialization)
		{
			IList<JsonProperty> props = base.CreateProperties (type, memberSerialization);
			return props.Where (p => p.Writable).ToList ();
		}
	}

	public static class StringHelper {
		public static string ToPascalCase (this string str)
		{
			var tokens = str.ToLower ().Split (new [] { "_" }, StringSplitOptions.RemoveEmptyEntries);

			for (var i = 0; i < tokens.Length; i++) {
				var token = tokens [i];
				tokens [i] = token.Substring (0, 1).ToUpper () + token.Substring (1);
			}

			return string.Join ("", tokens);
		}

		public static string ToTitleCase (this string str)
		{
			var tokens = str.Split (new [] { " " }, StringSplitOptions.RemoveEmptyEntries);

			for (var i = 0; i < tokens.Length; i++) {
				var token = tokens [i];
				tokens [i] = token.Substring (0, 1).ToUpper () + token.Substring (1);
			}

			return string.Join (" ", tokens);
		}
	}
}