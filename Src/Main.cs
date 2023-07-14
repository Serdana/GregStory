using JetBrains.Annotations;
using Vintagestory.API.Common;

namespace GregStory {
	[UsedImplicitly]
	public class Main : ModSystem {
		public static ILogger Logger { get; private set; } = default!;

		public override void StartPre(ICoreAPI api) => Logger = api.Logger;

		public override void Start(ICoreAPI api) {
			Logger.Event("#");
			Logger.Event("GregStory is Starting!");
			Logger.Event("#");
		}
	}
}