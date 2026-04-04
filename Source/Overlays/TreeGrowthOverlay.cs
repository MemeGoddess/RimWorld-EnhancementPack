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
		private bool[] _shownCells;
		private bool[] _checkedCells;
		private Dictionary<float, Color> _lerpedColor = [];
		private Plant[] _plantCache;
		public TreeGrowthOverlay() : base() { }

		public override bool ShowCell(int index)
		{
			_checkedCells ??= new bool[Find.CurrentMap.cellIndices.NumGridCells];
			if (_checkedCells[index])
				return false;
			_shownCells ??= new bool[Find.CurrentMap.cellIndices.NumGridCells];
			if (_shownCells[index])
				return true;

			var valid = IsValidPlant(FindPlant(index));
			if (valid)
				_shownCells[index] = true;
			else
				_checkedCells[index] = true;
			return valid;
		}
		public override Color GetCellExtraColor(int index)
		{
			if (!_shownCells[index])
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

			_shownCells = null;
			_checkedCells = null;
			_plantCache = null;
		}

		public void Register(int index, Plant plant)
		{
			var numCells = Find.CurrentMap.cellIndices.NumGridCells;
			_shownCells ??= new bool[numCells];
			_checkedCells ??= new bool[numCells];
			_plantCache ??= new Plant[Find.CurrentMap.cellIndices.NumGridCells];
			_shownCells[index] = true;
			_checkedCells[index] = false;
			_plantCache[index] = plant;
		}

		public void Deregister(int index)
		{
			var numCells = Find.CurrentMap.cellIndices.NumGridCells;
			_shownCells ??= new bool[numCells];
			_checkedCells ??= new bool[numCells];
			_plantCache ??= new Plant[Find.CurrentMap.cellIndices.NumGridCells];
			_shownCells[index] = false;
			_checkedCells[index] = false;
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
				_shownCells ??= new bool[Find.CurrentMap.cellIndices.NumGridCells];
				_shownCells[index] = false;
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
