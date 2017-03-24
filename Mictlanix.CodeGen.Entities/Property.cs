using System;
namespace Mictlanix.CodeGen.Entities {
	public class Property {
		public string Name { get; set; }
		public string Type { get; set; }
		public string Column { get; set; }
		public string DbType { get; set; }
		public bool IsPrimaryKey { get; set; }
		public bool IsNullable { get; set; }
		public bool IsFilter { get; set; }
		public int? Lenght { get; set; }
		public int? Precision { get; set; }
		public int? Scale { get; set; }

		public string ArgumentName {
			get {
				var name = Name.Substring (0, 1).ToLower () + Name.Substring (1);
					
				if (name == "object") {
					name = "@object";
				}

				return name;
			}
		}
	}
}
