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

namespace TD_Enhancement_Pack
{
	[StaticConstructorOnStartup]
	class TreeGrowthOverlay : BaseOverlay
	{
		private HashSet<int> _shownCells = [];
		private HashSet<int> _checkedCells = [];
		private HashSet<int> _fullyGrown = [];
		private Dictionary<float, Color> _lerpedColor = [];
		private Plant[] _plantCache;
		public TreeGrowthOverlay() : base() { }

		public override bool ShowCell(int index)
		{
			if (_checkedCells.Contains(index))
				return false;
			if (_shownCells.Contains(index))
				return true;

			var valid = IsValidPlant(FindPlant(index)) && _shownCells.Add(index);
			if (!valid)
				_checkedCells.Add(index);
			return valid;
		}
		public override Color GetCellExtraColor(int index)
		{
			if (!_shownCells.Contains(index))
				return Color.white.ToTransparent(0);

			var tree = FindPlant(index);
			if (tree == null) return Color.magenta;//shouldn't happen

			return tree.Growth > 0.99900001287460327 ? Color.white :
				tree.HarvestableNow ? Color.green :
				tree.HarvestableSoon ? Color.yellow :
				Color.red;
		}

		public static bool IsValidPlant(Plant plant) =>
			plant?.def.plant is { harvestTag: "Wood", Harvestable: true };

		public override bool ShouldAutoDraw() => Mod.settings.autoOverlayTreeGrowth;
		public override IEnumerable<Type> AutoDesignator() => [ typeof(Designator_PlantsHarvestWood), typeof(Designator_PlantsCut) ];

		public override void Clear()
		{
			_shownCells.Clear();
			_checkedCells.Clear();
			_fullyGrown.Clear();
			_plantCache = null;
		}

		public void Register(int index, Plant plant)
		{
			_plantCache ??= new Plant[Find.CurrentMap.cellIndices.NumGridCells];
			_shownCells.Add(index);
			_checkedCells.Remove(index);
			_plantCache[index] = plant;
		}

		public void Deregister(int index)
		{
			_plantCache ??= new Plant[Find.CurrentMap.cellIndices.NumGridCells];
			_shownCells.Remove(index);
			_checkedCells.Remove(index);
			_fullyGrown.Remove(index);
			_plantCache[index] = null;
		}

		private Plant FindPlant(int index)
		{
			_plantCache ??= new Plant[Find.CurrentMap.cellIndices.NumGridCells];
			var plant = _plantCache[index];
			if (plant != null)
				return plant;

			plant = Find.CurrentMap.thingGrid.ThingsListAtFast(index)
				.FirstOrDefault(t => t is Plant plant &&  IsValidPlant(plant)) as Plant;


			if (plant == null)
			{
				_shownCells.Remove(index);
				return null;
			}

			_plantCache[index] = plant;

			return plant;
		}
	}

	[HarmonyPatch(typeof(ThingGrid), "Deregister")]
	public static class ThingDirtierDeregister_TreeGrowth
	{
		private static TreeGrowthOverlay overlay;
		public static void Postfix(Thing t, Map ___map)
		{
			if (___map != Find.CurrentMap) 
				return;
			if (t is not Plant plant || !TreeGrowthOverlay.IsValidPlant(plant))
				return;

			overlay ??= BaseOverlay.GetOverlay<TreeGrowthOverlay>();
			overlay.Deregister(___map.cellIndices.CellToIndex(t.Position));
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(ThingGrid), "Register")]
	public static class ThingDirtierRegister_TreeGrowth
	{
		private static TreeGrowthOverlay overlay;
		public static void Postfix(Thing t, Map ___map)
		{

			if (___map != Find.CurrentMap)
				return;
			if (t is not Plant plant || !TreeGrowthOverlay.IsValidPlant(plant))
				return;

			overlay ??= BaseOverlay.GetOverlay<TreeGrowthOverlay>();
			overlay.Register(___map.cellIndices.CellToIndex(t.Position), plant);
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(Plant), "PlantCollected")]
	public static class PlantCollected
	{
		private static TreeGrowthOverlay overlay;
		public static void Postfix(Plant __instance)
		{
			if (__instance.Map != Find.CurrentMap)
				return;

			if (!PlantHarvestOverlay.IsValidPlant(__instance))
				return;

			overlay ??= BaseOverlay.GetOverlay<TreeGrowthOverlay>();
			overlay.Deregister(__instance.Map.cellIndices.CellToIndex(__instance.Position));
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
