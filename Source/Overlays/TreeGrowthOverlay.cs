using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using TD_Enhancement_Pack.Overlays;
using KTrie;

namespace TD_Enhancement_Pack
{
	[StaticConstructorOnStartup]
	class TreeGrowthOverlay : CachedOverlay<Plant>
	{
		private byte[] _fullyGrownCache;
		public TreeGrowthOverlay() : base() { }
		public override bool ShouldAutoDraw() => Mod.settings.autoOverlayTreeGrowth;
		public override IEnumerable<Type> AutoDesignator() => [ typeof(Designator_PlantsHarvestWood), typeof(Designator_PlantsCut) ];
		
		protected override Plant GetValue(int index) =>
			Find.CurrentMap.thingGrid.ThingsListAtFast(index)
				.FirstOrDefault(t => t is Plant plant && IsValid(plant, index)) as Plant;
		
		protected override Color GetColor(Plant tree, int index)
		{
			if (tree == null) return Color.magenta; //shouldn't happen

			_fullyGrownCache ??= new byte[Find.CurrentMap.cellIndices.NumGridCells];

			switch (_fullyGrownCache[index])
			{
				case 0:
					_fullyGrownCache[index] = (byte)(tree.Growth > 0.99900001287460327 ? 2 : 1);
					return _fullyGrownCache[index] == 2 ? Color.white :
						tree.HarvestableNow ? Color.green :
						tree.HarvestableSoon ? Color.yellow :
						Color.red;
				case 1:
					return tree.HarvestableNow ? Color.green :
						tree.HarvestableSoon ? Color.yellow :
						Color.red;
				case 2:
					return Color.white;
				default:
					throw new ArgumentOutOfRangeException("_fullyGrownCache[index]");
			}
		}
		
		public override bool IsValid(Plant plant, int index) =>
			plant?.def.plant is { harvestTag: "Wood", Harvestable: true };

		public override void Deregister(int index)
		{
			base.Deregister(index);
			if(_fullyGrownCache != null)
				_fullyGrownCache[index] = 0;
		}
	}

	[HarmonyPatch(typeof(ThingGrid), "Deregister")]
	public static class ThingDirtierDeregister_TreeGrowth
	{
		private static TreeGrowthOverlay overlay;
		public static void Postfix(Thing t, Map ___map)
		{
			overlay ??= BaseOverlay.GetOverlay<TreeGrowthOverlay>();
			if (___map != Find.CurrentMap)
				return;

			var index = ___map.cellIndices.CellToIndex(t.Position);
			if (t is not Plant plant || !overlay.IsValid(plant, index))
				return;

			overlay.Deregister(index);
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(ThingGrid), "Register")]
	public static class ThingDirtierRegister_TreeGrowth
	{
		private static TreeGrowthOverlay overlay;
		public static void Postfix(Thing t, Map ___map)
		{
			overlay ??= BaseOverlay.GetOverlay<TreeGrowthOverlay>();
			if (___map != Find.CurrentMap)
				return;

			var index = ___map.cellIndices.CellToIndex(t.Position);
			if (t is not Plant plant || !overlay.IsValid(plant, index))
				return;

			overlay.Register(index, plant);
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(Plant), "PlantCollected")]
	public static class PlantCollected
	{
		private static TreeGrowthOverlay overlay;
		public static void Postfix(Plant __instance)
		{
			overlay ??= BaseOverlay.GetOverlay<TreeGrowthOverlay>();
			if (__instance.Map != Find.CurrentMap)
				return;

			var index = __instance.Map.cellIndices.CellToIndex(__instance.Position);
			if (!overlay.IsValid(__instance, index))
				return;

			overlay.Deregister(index);
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(Plant), "TickLong")]
	public static class TickGrow
	{
		private static TreeGrowthOverlay overlay;
		public static void Postfix()
		{
			overlay ??= BaseOverlay.GetOverlay<TreeGrowthOverlay>();
			overlay.SetDirty();
		}
	}
}
