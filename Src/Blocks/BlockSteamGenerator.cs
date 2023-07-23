using GregStory.Entities.Blocks;
using GregStory.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GregStory.Blocks {
	public class BlockSteamGenerator : Block, IIgnitable {
		private WorldInteraction[]? interactions;

		public override void OnLoaded(ICoreAPI api) {
			base.OnLoaded(api);

			if (api.Side != EnumAppSide.Client) { return; }

			interactions = ObjectCacheUtil.GetOrCreate(api, "steamgeneratorBlockInteractions", () => {
				List<ItemStack> fuels = api.World.GetItem(AssetH.FromDefault("firewood")).GetHandBookStacks((ICoreClientAPI)api);

				return new WorldInteraction[] {
						new() {
								ActionLangCode = "blockhelp-steamgenerator-fuel",
								MouseButton = EnumMouseButton.Right,
								Itemstacks = fuels.ToArray(),
								GetMatchingStacks = (wi, bs, _) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is EntitySteamGenerator { FuelAmount: < EntitySteamGenerator.MaxFuel, } ? wi.Itemstacks : null,
						},
						new() {
								ActionLangCode = "blockhelp-steamgenerator-ignite",
								HotKeyCode = "shift",
								MouseButton = EnumMouseButton.Right,
								Itemstacks = BlockBehaviorCanIgnite.CanIgniteStacks(api, true).ToArray(),
								GetMatchingStacks = (wi, bs, _) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is EntitySteamGenerator { CanIgnite: true, } ? wi.Itemstacks : null,
						},
				};
			});
		}

		public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting) {
			if (byEntity.World.BlockAccessor.GetBlockEntity(pos) is not EntitySteamGenerator { CanIgnite: true, }) { return EnumIgniteState.NotIgnitablePreventDefault; }

			if (secondsIgniting > 0.25f && (int)(secondsIgniting * 30f) % 9 == 1) {
				Random rand = byEntity.World.Rand;
				AdvancedParticleProperties particleProperty = byEntity.World.GetBlock(AssetH.FromDefault("fire")).ParticleProperties[^1];
				particleProperty.basePos = new(pos.X + 0.25f + 0.5f * rand.NextDouble(), pos.Y + 0.875f, pos.Z + 0.25f + 0.5f * rand.NextDouble());
				particleProperty.Quantity.avg = 1f;
				byEntity.World.SpawnParticles(particleProperty, byEntity is EntityPlayer player ? player.World.PlayerByUid(player.PlayerUID) : null);
				particleProperty.Quantity.avg = 0f;
			}

			return secondsIgniting >= 1.5f ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
		}

		public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling) {
			if (secondsIgniting < 1.45f) { return; }

			handling = EnumHandling.PreventDefault;
			if (byEntity is EntityPlayer player && byEntity.World.PlayerByUid(player.PlayerUID) != null && byEntity.World.BlockAccessor.GetBlockEntity(pos) is EntitySteamGenerator steamGenerator) { steamGenerator.TryIgnite(); }
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) =>
				world.BlockAccessor.GetBlockEntity(blockSel.Position) is EntitySteamGenerator steamGenerator ? steamGenerator.OnRightClick(byPlayer) : base.OnBlockInteractStart(world, byPlayer, blockSel);

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) => interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}