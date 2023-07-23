using System.Text;
using GregStory.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GregStory.Entities.Blocks {
	public class EntitySteamGenerator : BlockEntity, IHeatSource {
		public const byte MaxFuel = 11;
		private static SimpleParticleProperties SmokeParticles { get; } =
			new(1f, 1f, ColorUtil.ToRgba(128, 110, 110, 110), new(), new(), new(-0.2f, 0.3f, -0.2f), new(0.2f, 0.3f, 0.2f), 1.5f, 0f, 0.5f, model: EnumParticleModel.Quad) {
					WindAffected = false, AddPos = new(0.75f, 0, 0.75f), AddQuantity = 0.2f, SelfPropelled = true, OpacityEvolve = new(EnumTransformFunction.LINEAR, -255f), SizeEvolve = new(EnumTransformFunction.LINEAR, 2f),
			};

		private static SimpleParticleProperties FireParticles { get; } = new(1f, 1f, 0, new(), new(), new(-0.2f, 0.3f, -0.2f), new(0.2f, 0.3f, 0.2f), 0.5f, 0f, 0.5f, model: EnumParticleModel.Quad) {
				WindAffected = false, AddPos = new(0.75f, 0, 0.75f), AddQuantity = 0.2f, SelfPropelled = true, SizeEvolve = new(EnumTransformFunction.LINEAR, -0.7f), VertexFlags = 128,
		};

		private Random ParticleRandom { get; } = new();
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
			double num1 = Api.World.Calendar.TotalHours - lastTickTotalHours;
			if (IsBurning) {
				if (FuelAmount > 0) {
					int oldAmount = (int)MathF.Ceiling(FuelAmount);
					FuelAmount = MathF.Max(0f, FuelAmount - 5f * (float)num1);
					if (oldAmount != (int)MathF.Ceiling(FuelAmount)) { MarkDirty(true); }
				}

				if (FuelAmount <= 0) {
					IsBurning = false;
					MarkDirty(true);
				} else if (Temperature < 1100) { Temperature = Math.Min(1100f, Temperature + (float)num1 * 1500f); }
			} else if (Temperature > 0) { Temperature = Math.Max(0, Temperature - (float)num1 * 1500f); }

			lastTickTotalHours = Api.World.Calendar.TotalHours;
		}

		private void OnClientTick(float dt) {
			if (clientSidePrevBurning != IsBurning) {
				ToggleAmbientSounds(IsBurning);
				clientSidePrevBurning = IsBurning;
			}

			if (IsBurning) {
				SmokeParticles.MinPos.Set(Pos.X + 0.25 - 0.125, Pos.Y + 0.1, Pos.Z + 0.25 - 0.125);
				FireParticles.MinPos.Set(Pos.X + 0.25 - 0.125, Pos.Y + 0.1, Pos.Z + 0.25 - 0.125);
				FireParticles.Color = ColorUtil.ToRgba(255, 255, ParticleRandom.Next(0, 214), 0);

				switch (Api.World.Rand.Next(7)) {
					case < 2:
						Api.World.SpawnParticles(SmokeParticles);
						break;
					case < 4:
						Api.World.SpawnParticles(SmokeParticles);
						Api.World.SpawnParticles(FireParticles);
						break;
					default: break;
				}
			}
		}

		internal bool OnRightClick(IPlayer byPlayer) {
			if (FuelAmount + 1 <= MaxFuel && !byPlayer.Entity.Controls.ShiftKey) {
				ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
				if (activeHotbarSlot.Itemstack?.Item is not ItemFirewood) { return false; }

				Api.World.PlaySoundAt(AssetH.FromDefault("sounds/effect/woodswitch"), byPlayer, byPlayer, range: 16f);
				FuelAmount++;

				if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) {
					activeHotbarSlot.TakeOut(1);
					activeHotbarSlot.MarkDirty();
				}

				MarkDirty(true);
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
			if (FuelAmount > 1 && !IsBurning) { Api.World.SpawnItemEntity(new(Api.World.GetItem(AssetH.FromDefault("firewood")), (int)MathF.Floor(FuelAmount)), Pos.ToVec3d().Add(0.5, 0.5, 0.5)); }
			ambientSound?.Dispose();
		}

		public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) {
			if (Block == null || FuelAmount <= 0) { return false; }
			mesher.AddMeshData(GetOrCreateMesh());
			return base.OnTesselation(mesher, tessThreadTesselator);
		}

		private MeshData? GetOrCreateMesh() {
			Dictionary<string, MeshData> dictionary = ObjectCacheUtil.GetOrCreate<Dictionary<string, MeshData>>(Api, Main.ModId + "-firewood-meshes", () => new());

			if (!dictionary.TryGetValue($"steam_generator_firewood_{(int)MathF.Ceiling(FuelAmount)}", out MeshData? modeldata) && Api.World.BlockAccessor.GetBlock(Pos) is { BlockId: not 0, } block) {
				ShapeElement[] elements = new ShapeElement[(int)Math.Ceiling(FuelAmount)];
				for (int i = 0; i < elements.Length; i++) { elements[i] = Shape.TryGet(Api, AssetH.FromGreg("shapes/block/machines/steam_generator_firewood.json")).GetElementByName($"Log{i + 1}"); }

				((ICoreClientAPI)Api).Tesselator.TesselateShape(block, new() { Elements = elements, }, out modeldata, block.Code.ToString() switch {
						$"{Main.ModId}:steamgenerator-north" => new() { Y = 90, },
						$"{Main.ModId}:steamgenerator-east" => new(),
						$"{Main.ModId}:steamgenerator-south" => new() { Y = 270, },
						$"{Main.ModId}:steamgenerator-west" => new() { Y = 180, },
						_ => throw new ArgumentOutOfRangeException(),
				});
			}

			return modeldata;
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc) {
			if (FuelAmount <= 0) { return; }
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
			FuelAmount -= 0.1f; // Floating point bugfix. now it's a "feature"! fuel is used starting the fire!
			MarkDirty();
		}

		private void ToggleAmbientSounds(bool on) {
			if (Api.Side != EnumAppSide.Client) { return; }

			if (on) {
				if (ambientSound is { IsPlaying: true, }) { return; }

				ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new() {
						Location = AssetH.FromDefault("sounds/environment/fireplace.ogg"), ShouldLoop = true, Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f), DisposeOnFinish = false, Volume = 1f,
				});

				ambientSound.Start();
			} else {
				ambientSound?.Stop();
				ambientSound?.Dispose();
				ambientSound = null;
			}
		}

		public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos) => IsBurning ? 7f : 0;
	}
}