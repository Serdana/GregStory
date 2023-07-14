using JetBrains.Annotations;
using Vintagestory.API.Common;

// Console.WriteLine is not being displayed?
// BUT
// Debug.WriteLine is working?

// Print any asset errors to a visible place

/*
remove combustiblePropsByType from everything except bits/nuggets/dusts?
otherwise i need to find a way to change the melting point in every single location
*/

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