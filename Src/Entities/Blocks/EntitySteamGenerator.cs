using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GregStory.Entities.Blocks {
	public class EntitySteamGenerator : BlockEntity, IHeatSource {
		public const byte MaxFuel = 4;

		private ILoadedSound? ambientSound;
		private double lastTickTotalHours;
		private bool clientSidePrevBurning;

		public float FuelAmount { get; private set; }
		public float Temperature { get; private set; }
		public bool IsBurning { get; private set; }

		public bool CanIgnite => !IsBurning && FuelAmount > 0;

		public override void Initialize(ICoreAPI api) {
			base.Initialize(api);
			RegisterGameTickListener(BurnTick, 500);
			if (api.Side == EnumAppSide.Client) { RegisterGameTickListener(OnClientTick, 50); }
		}

		private void BurnTick(float dt) {
			if (IsBurning) {
				double num1 = Api.World.Calendar.TotalHours - lastTickTotalHours;
				if (FuelAmount > 0) { FuelAmount = MathF.Max(0f, FuelAmount - 5f / 16f * (float)num1); }
				if (FuelAmount <= 0) { IsBurning = false; }
				if (Temperature < 1100) { Temperature = Math.Min(1100f, Temperature + (float)num1 * 1500f); }
			}

			lastTickTotalHours = Api.World.Calendar.TotalHours;
		}

		private void OnClientTick(float dt) {
			if (clientSidePrevBurning != IsBurning) {
				ToggleAmbientSounds(IsBurning);
				clientSidePrevBurning = IsBurning;
			}

			if (IsBurning && Api.World.Rand.NextDouble() < 0.13) { BlockEntityCoalPile.SpawnBurningCoalParticles(Api, Pos.ToVec3d().Add(0.25, 0.875, 0.25), 0.5f, 0.5f); }
		}

		internal bool OnRightClick(IPlayer byPlayer) {
			if (FuelAmount + 1 <= MaxFuel && !byPlayer.Entity.Controls.ShiftKey) {
				ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
				if (activeHotbarSlot.Itemstack?.Item is not ItemFirewood) { return false; }

				Api.World.PlaySoundAt(new(GlobalConstants.DefaultDomain, "sounds/effects/woodswitch"), byPlayer, byPlayer, range: 16f);
				FuelAmount++;

				if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) {
					activeHotbarSlot.TakeOut(1);
					activeHotbarSlot.MarkDirty();
				}

				MarkDirty();
				return true;
			}

			return false;
		}

		public override void OnBlockRemoved() {
			base.OnBlockRemoved();
			ambientSound?.Dispose();
		}

		public override void OnBlockBroken(IPlayer? byPlayer = null) {
			base.OnBlockBroken(byPlayer);
			if (FuelAmount > 1 && !IsBurning) { Api.World.SpawnItemEntity(new(Api.World.GetItem(new AssetLocation(GlobalConstants.DefaultDomain, "firewood")), (int)MathF.Floor(FuelAmount)), Pos.ToVec3d().Add(0.5, 0.5, 0.5)); }
			ambientSound?.Dispose();
		}

		private void ToggleAmbientSounds(bool on) {
			if (Api.Side != EnumAppSide.Client) { return; }

			if (on) {
				if (ambientSound is { IsPlaying: true, }) { return; }

				ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new() {
						Location = new(GlobalConstants.DefaultDomain, "sounds/effect/embers.ogg"), ShouldLoop = true, Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f), DisposeOnFinish = false, Volume = 1f,
				});

				ambientSound.Start();
			} else {
				ambientSound?.Stop();
				ambientSound?.Dispose();
				ambientSound = null;
			}
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc) {
			if (FuelAmount == 0) { return; }
			dsc.AppendLine(Temperature <= 25 ? Lang.Get("Cold") : $"{Temperature:.0}\u00b0C");
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
			base.FromTreeAttributes(tree, worldAccessForResolve);
			FuelAmount = tree.GetFloat(nameof(FuelAmount));
			Temperature = tree.GetFloat(nameof(Temperature));
			IsBurning = tree.GetBool(nameof(IsBurning));
			lastTickTotalHours = tree.GetDouble(nameof(lastTickTotalHours));
		}

		public override void ToTreeAttributes(ITreeAttribute tree) {
			base.ToTreeAttributes(tree);
			tree.SetFloat(nameof(FuelAmount), FuelAmount);
			tree.SetFloat(nameof(Temperature), Temperature);
			tree.SetBool(nameof(IsBurning), IsBurning);
			tree.SetDouble(nameof(lastTickTotalHours), lastTickTotalHours);
		}

		internal void TryIgnite() {
			if (IsBurning) { return; }

			IsBurning = true;
			lastTickTotalHours = Api.World.Calendar.TotalHours;
			MarkDirty();
		}

		public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos) => IsBurning ? 7f : 0;
	}
}