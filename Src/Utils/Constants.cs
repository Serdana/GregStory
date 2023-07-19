namespace GregStory.Utils {
	public static class Constants {
		/// <summary> Seems impossible to load this from worldproperties? if it's possible, @ me how &lt;3 </summary>
		public static Dictionary<string, MetalInfo> AllMetalProperties { get; } = new() {
				{ "bismuth", new(217) },
				{ "bismuthbronze", new(850) },
				{ "blackbronze", new(1020) },
				{ "brass", new(920) },
				{ "chromium", new(1907) },
				{ "copper", new(1084) },
				{ "cupronickel", new(1171) },
				{ "electrum", new(1010) },
				{ "gold", new(1064) },
				{ "iron", new(1538) },
				{ "meteoriciron", new(1476) },
				{ "lead", new(327) },
				{ "molybdochalkos", new(902) },
				{ "platinum", new(1770) },
				{ "nickel", new(1325) },
				{ "silver", new(961) },
				{ "stainlesssteel", new(1530) },
				{ "steel", new(1502) },
				{ "tin", new(231) },
				{ "tinbronze", new(950) },
				{ "titanium", new(1668) },
				{ "uranium", new(1133) },
				{ "zinc", new(419) },
				{ "invar", new(1700) },
		};
	}
}