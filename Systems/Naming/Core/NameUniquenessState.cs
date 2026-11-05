using System.Collections.Generic;

namespace LabelMan.Naming {
	public sealed class NameUniquenessState {
		public Dictionary<string, List<string>> Exact { get; set; }
		public Dictionary<string, List<string>> Fuzzy { get; set; }
	}
}
