using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GregStory.Utils {
	public static class AssetH {
		public static AssetLocation FromDefault(string path) => new(GlobalConstants.DefaultDomain, path);
		public static AssetLocation FromGreg(string path) => new(Main.ModId, path);
	}
}