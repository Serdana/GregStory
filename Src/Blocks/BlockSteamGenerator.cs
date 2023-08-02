using System.Text;
using GregStory.Entities.Blocks;
using GregStory.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GregStory.Blocks {
	public class BlockSteamGenerator : BlockLiquidContainerBase, IIgnitable {
		public override bool AllowHeldLiquidTransfer => false;

		public override void OnLoaded(ICoreAPI api) {
			base.OnLoaded(api);

			if (api.Side != EnumAppSide.Client) { return; }

			interactions = ObjectCacheUtil.GetOrCreate(api, "steamgeneratorBlockInteractions", () => {
				ItemStack[] fuels = api.World.GetItem(AssetH.FromDefault("firewood")).GetHandBookStacks((ICoreClientAPI)api).ToArray();
				ItemStack[] liquidContainers = api.World.Collectibles.Where(c => c is BlockLiquidContainerBase { IsTopOpened: true, AllowHeldLiquidTransfer: true, }).Select(c => new ItemStack(c)).ToArray();

				return new WorldInteraction[] {
						new() {
								ActionLangCode = "blockhelp-steamgenerator-addwater",
								MouseButton = EnumMouseButton.Right,
								Itemstacks = liquidContainers,
								GetMatchingStacks = (wi, bs, _) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is EntitySteamGenerator { Block: BlockLiquidContainerBase block, } e &&
																   (e.GetContent()?.StackSize / block.GetContentProps(bs.Position)?.ItemsPerLitre ?? 0) < EntitySteamGenerator.MaxWaterLiters
										? wi.Itemstacks
										: null,
						},
						new() {
								ActionLangCode = "blockhelp-steamgenerator-removewater",
								MouseButton = EnumMouseButton.Right,
								Itemstacks = liquidContainers,
								GetMatchingStacks = (wi, bs, _) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is EntitySteamGenerator e && (e.GetContent()?.StackSize ?? 0) > 0 ? wi.Itemstacks : null,
						},
						new() {
								ActionLangCode = "blockhelp-steamgenerator-fuel",
								MouseButton = EnumMouseButton.Right,
								Itemstacks = fuels,
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

		public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot) =>
				new WorldInteraction[] { new() { ActionLangCode = "heldhelp-place", HotKeyCode = "shift", MouseButton = EnumMouseButton.Right, ShouldApply = (_, _, _) => true, }, };

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
				world.BlockAccessor.GetBlockEntity(blockSel.Position) is EntitySteamGenerator steamGenerator
						? steamGenerator.OnRightClick(byPlayer) switch {
								EntitySteamGenerator.ClickResult.Failed => false,
								EntitySteamGenerator.ClickResult.WasContainer => base.OnBlockInteractStart(world, byPlayer, blockSel),
								EntitySteamGenerator.ClickResult.WasWater => base.OnBlockInteractStart(world, byPlayer, blockSel),
								EntitySteamGenerator.ClickResult.WasFuel => true,
								_ => throw new ArgumentException("Unknown result"),
						}
						: base.OnBlockInteractStart(world, byPlayer, blockSel);

		public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer) {
			if (world.BlockAccessor.GetBlockEntity(pos) is EntitySteamGenerator blockEntity) {
				float steamAmount = GetContainableProps(blockEntity.Inventory[1].Itemstack) is { } contentProps ? blockEntity.Inventory[1].StackSize / contentProps.ItemsPerLitre : 0;
				float waterAmount = GetCurrentLitres(pos);

				StringBuilder sb = new();
				sb.AppendLine(waterAmount > 0 ? Lang.Get("{0} litres of {1}", waterAmount, Lang.Get($"{GlobalConstants.DefaultDomain}:incontainer-item-waterportion")) : Lang.Get($"{GlobalConstants.DefaultDomain}:Empty"));
				sb.AppendLine(steamAmount > 0 ? Lang.Get("{0} litres of {1}", steamAmount, Lang.Get($"{Main.ModId}:incontainer-item-steamportion")) : Lang.Get($"{GlobalConstants.DefaultDomain}:Empty"));
				sb.AppendLine(blockEntity.Temperature <= 25 ? Lang.Get($"{GlobalConstants.DefaultDomain}:Cold") : $"{Lang.Get("Burn temperature: {0}\u00b0C", blockEntity.Temperature.ToString(".0"))}");

				return sb.ToString();
			}

			return "Broken! Send help!";
		}
	}
}