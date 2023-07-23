#if DEBUG

using JetBrains.Annotations;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace GregStory {
	[UsedImplicitly]
	public class Debug : ModSystem {
		public override void StartClientSide(ICoreClientAPI api) {
			api.World.Logger.EntryAdded += (logType, message, args) => {
				if (!api.PlayerReadyFired) { return; }

				switch (logType) {
					case EnumLogType.VerboseDebug or EnumLogType.Chat or EnumLogType.Audit:
					case EnumLogType.Notification when message.StartsWith("Message to all in group") || message.StartsWith("Track music") || message.StartsWith("Client pause"): break;
					default:
						// Crashing???? figure this out
						//api.ShowChatMessage($"[Client {logType}] {string.Format(message, args)}");
						break;
				}
			};
		}

		public override void StartServerSide(ICoreServerAPI api) {
			api.Server.Logger.EntryAdded += (logType, message, args) => {
				if (!api.World.AllOnlinePlayers.Any(player => player is ServerPlayer { ConnectionState: EnumClientState.Playing, })) { return; }

				switch (logType) {
					case EnumLogType.VerboseDebug or EnumLogType.Chat or EnumLogType.Audit:
					case EnumLogType.Notification when message.StartsWith("Message to all in group"): break;
					default:
						api.BroadcastMessageToAllGroups($"[Server {logType}] {string.Format(message, args)}", EnumChatType.OwnMessage);
						break;
				}
			};
		}
	}
}

#endif