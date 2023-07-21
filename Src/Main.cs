using System.Text;
using GregStory.Blocks;
using GregStory.Entities.Blocks;
using GregStory.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace GregStory {
	[UsedImplicitly]
	public class Main : ModSystem {
		public const string ModId = "gregstory";

		internal static ILogger Logger { get; private set; } = default!; // TODO remove this

		public override void StartPre(ICoreAPI api) => Logger = api.Logger;

		public override void Start(ICoreAPI api) {
			Logger.Event("#");
			Logger.Event("GregStory is Starting!");
			Logger.Event("#");

			api.RegisterBlockClass("BlockSteamGenerator", typeof(BlockSteamGenerator));
			api.RegisterBlockEntityClass("EntitySteamGenerator", typeof(EntitySteamGenerator));
		}

		public override void AssetsLoaded(ICoreAPI api) {
			if (api.Side == EnumAppSide.Client) { return; }

			Logger.Event("Injecting new properties into Items");

			Dictionary<AssetLocation, JObject> injectAssets = new();
			foreach (KeyValuePair<AssetLocation, JObject> pair in api.Assets.GetMany<JObject>(Logger, nameof(AssetCategory.itemtypes))) {
				JObject? property = (JObject?)pair.Value.SelectToken("combustiblePropsByType");
				if (property == null) { continue; }

				bool didInject = false;
				foreach (JProperty innerProp in property.Children<JProperty>()) {
					MetalInfo? metalInfo = Constants.AllMetalProperties.Where(pair => innerProp.Name.EndsWith(pair.Key)).Select(pair => pair.Value).FirstOrDefault(); // haha. ew
					if (metalInfo == null) { continue; }

					((JValue?)innerProp.Value.SelectToken("meltingPoint") ?? throw new NullReferenceException($"{pair.Key} - \"{innerProp.Name}\" does not contain a melting point!")).Replace(new JValue(metalInfo.MeltingPoint));
					didInject = true;
				}

				if (didInject) {
					injectAssets.Add(pair.Key, pair.Value);
					Logger.Debug($"Injected new properties into Item: {pair.Key}");
				}
			}

			foreach (KeyValuePair<AssetLocation, JObject> pair in injectAssets) { api.Assets.AllAssets[pair.Key].Data = Encoding.UTF8.GetBytes(injectAssets[pair.Key].ToString(Formatting.None)); }
		}
	}
}