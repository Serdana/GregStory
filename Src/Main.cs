using JetBrains.Annotations;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

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

#if DEBUG
		public override void StartClientSide(ICoreClientAPI api) {
			api.World.Logger.EntryAdded += (logType, message, args) => {
				if (!api.PlayerReadyFired) { return; }

				switch (logType) {
					case EnumLogType.VerboseDebug or EnumLogType.Chat or EnumLogType.Audit:
					case EnumLogType.Notification when message.StartsWith("Message to all in group") || message.StartsWith("Track music") || message.StartsWith("Client pause"): break;
					default:
						api.SendChatMessage($"[Client {logType}] {string.Format(message, args)}");
						break;
				}
			};
		}

		public override void StartServerSide(ICoreServerAPI api) {
			api.Server.Logger.EntryAdded += (logType, message, args) => {
				if (!api.World.AllOnlinePlayers.Any(player => player != null && ((ServerPlayer)player).ConnectionState == EnumClientState.Playing)) { return; }

				switch (logType) {
					case EnumLogType.VerboseDebug or EnumLogType.Chat or EnumLogType.Audit:
					case EnumLogType.Notification when message.StartsWith("Message to all in group"): break;
					default:
						api.BroadcastMessageToAllGroups($"[Server {logType}] {string.Format(message, args)}", EnumChatType.OwnMessage);
						break;
				}
			};
		}
#endif
	}
}