using GregStory.Entities.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GregStory.Blocks {
	public class BlockSteamGenerator : Block, IIgnitable {
		private WorldInteraction[]? interactions;

		public override void OnLoaded(ICoreAPI api) {
			base.OnLoaded(api);

			if (api.Side != EnumAppSide.Client) { return; }

			interactions = ObjectCacheUtil.GetOrCreate(api, "steamgeneratorBlockInteractions", (CreateCachableObjectDelegate<WorldInteraction[]>)(() => {
				List<ItemStack> fuels = api.World.GetItem(new AssetLocation(GlobalConstants.DefaultDomain, "firewood")).GetHandBookStacks((ICoreClientAPI)api);

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
			}));
		}

		public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting) {
			if (byEntity.World.BlockAccessor.GetBlockEntity(pos) is not EntitySteamGenerator { CanIgnite: true, }) { return EnumIgniteState.NotIgnitablePreventDefault; }

			if (secondsIgniting > 0.25 && (int)(30.0 * secondsIgniting) % 9 == 1) {
				Random rand = byEntity.World.Rand;
				Vec3d vec3d = new(pos.X + 0.25 + 0.5 * rand.NextDouble(), pos.Y + 0.875, pos.Z + 0.25 + 0.5 * rand.NextDouble());
				Block block = byEntity.World.GetBlock(new AssetLocation(GlobalConstants.DefaultDomain, "fire"));
				AdvancedParticleProperties particleProperty = block.ParticleProperties[^1];
				particleProperty.basePos = vec3d;
				particleProperty.Quantity.avg = 1f;
				IPlayer? dualCallByPlayer = null;
				if (byEntity is EntityPlayer player) { dualCallByPlayer = player.World.PlayerByUid(player.PlayerUID); }

				byEntity.World.SpawnParticles(particleProperty, dualCallByPlayer);
				particleProperty.Quantity.avg = 0.0f;
			}

			return secondsIgniting >= 1.5 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
		}

		public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling) {
			if (secondsIgniting < 1.45) { return; }

			handling = EnumHandling.PreventDefault;
			if (byEntity is EntityPlayer entityPlayer && byEntity.World.PlayerByUid(entityPlayer.PlayerUID) != null && byEntity.World.BlockAccessor.GetBlockEntity(pos) is EntitySteamGenerator blockEntity) {
				blockEntity.TryIgnite();
			}
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) =>
				world.BlockAccessor.GetBlockEntity(blockSel.Position) is EntitySteamGenerator steamGenerator ? steamGenerator.OnRightClick(byPlayer) : base.OnBlockInteractStart(world, byPlayer, blockSel);

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) => interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}