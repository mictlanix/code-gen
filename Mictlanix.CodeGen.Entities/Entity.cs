using System.Collections.Generic;

namespace Mictlanix.CodeGen.Entities {
	public class Entity {
		public string Name { get; set; }
		public string Table { get; set; }
		public List<Property> Properties { get; set; } = new List<Property> ();
		public List<Method> Methods { get; set; } = new List<Method> ();

		public override string ToString ()
		{
			return string.Format ("{0} [Table={1}, Properties={2}, Methods={3}]", Name, Table, Properties.Count, Methods.Count);
		}

		public string PluralName {
			get {
				if (Name.EndsWith ("y", System.StringComparison.Ordinal) && !"aeiou".Contains (Name[Name.Length - 2])) {
					return Name.Substring (0, Name.Length - 1) + "ies";
				}

				return Name + "s";
			}
		}

		public string PluralTable {
			get {
				if (Table.EndsWith ("y", System.StringComparison.Ordinal)) {
					return Table.Substring (0, Table.Length - 1) + "ies";
				}

				return Table + "s";
			}
		}
	}
}
