using System;
namespace Mictlanix.CodeGen.Entities {
	public class Method {
		public string Name { get; set; }
		public string Type { get; set; }
		public string Arguments { get; set; }
		public string Query { get; set; }
		public bool IsSingleResult { get; set; }

		public string Initializers {
			get {
				var initializers = string.Empty;

				foreach (var pair in Arguments.Split (',')) {
					var argument = pair.Split (' ') [1];

					if (initializers.Length > 0) {
						initializers += ", ";
					}

					initializers += argument.Substring (0, 1).ToUpper () + argument.Substring (1) + " = " + argument;
				}

				return initializers;
			}
		}
	}
}
