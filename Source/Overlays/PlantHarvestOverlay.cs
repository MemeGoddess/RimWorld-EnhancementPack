using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace TD_Enhancement_Pack
{
	[StaticConstructorOnStartup]
	class PlantHarvestOverlay : BaseOverlay
	{
		private bool[] _shownCells;
		private bool[] _checkedCells;
		private Dictionary<float, Color> _lerpedColor = [];
		private Plant[] _plantCache;
		public PlantHarvestOverlay() : base() { }

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

			var plant = FindPlant(index);
			if (plant == null) return Color.magenta;//shouldn't happen

			if(plant.Growth > 0.99900001287460327)
				return Color.white;

			if (_lerpedColor.TryGetValue(plant.Growth, out var color))
				return color;

			color = Color.Lerp(Color.red, Color.green, plant.Growth);
			_lerpedColor[plant.Growth] = color;
			return color;
		}

		public static bool IsValidPlant(Plant plant) =>
			plant?.def?.plant is { harvestTag: "Standard" } && plant.sown;

		public override bool ShouldAutoDraw() => Mod.settings.autoOverlayPlantHarvest;
		public override IEnumerable<Type> AutoDesignator() => [ typeof(Designator_PlantsHarvest), typeof(Designator_PlantsCut) ];

		public static Texture2D icon = ContentFinder<Texture2D>.Get("UI/Designators/Harvest", true);
		public override Texture2D Icon() => icon;
		public override bool IconEnabled() => true;
		public override string IconTip() => "TD.TogglePlantHarveset".Translate();
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
			_plantCache ??= new Plant[numCells];
			_shownCells[index] = true;
			_checkedCells[index] = false;
			_plantCache[index] = plant;
		}

		public void Deregister(int index)
		{
			var numCells = Find.CurrentMap.cellIndices.NumGridCells;
			_shownCells ??= new bool[numCells];
			_checkedCells ??= new bool[numCells];
			_plantCache ??= new Plant[numCells];
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
				.FirstOrDefault(t => t is Plant plant && IsValidPlant(plant)) as Plant;

			
			if(plant == null)
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
	public static class ThingDirtierDeregister_PlantHarvest
	{
		private static PlantHarvestOverlay overlay;
		public static void Postfix(Thing t, Map ___map)
		{
			if (___map != Find.CurrentMap) 
				return;
			if (t is not Plant plant || !PlantHarvestOverlay.IsValidPlant(plant)) 
				return;

			overlay ??= BaseOverlay.GetOverlay<PlantHarvestOverlay>();
			overlay.Deregister(___map.cellIndices.CellToIndex(t.Position));
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(ThingGrid), nameof(ThingGrid.Register))]
	public static class ThingDirtierRegister_PlantHarvest
	{
		private static PlantHarvestOverlay overlay;
		public static void Postfix(Thing t, Map ___map)
		{
			if (___map != Find.CurrentMap)
				return;
			if (t is not Plant plant || !PlantHarvestOverlay.IsValidPlant(plant))
				return;

			overlay ??= BaseOverlay.GetOverlay<PlantHarvestOverlay>();
			overlay.Register(___map.cellIndices.CellToIndex(t.Position), plant);
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(Plant), "PlantCollected")]
	public static class PlantCollected_PlantHarvest
	{
		private static PlantHarvestOverlay overlay;
		public static void Postfix(Plant __instance)
		{
			if (__instance.Map != Find.CurrentMap) 
				return;

			if (!PlantHarvestOverlay.IsValidPlant(__instance))
				return;

			overlay ??= BaseOverlay.GetOverlay<PlantHarvestOverlay>();
			overlay.Deregister(__instance.Map.cellIndices.CellToIndex(__instance.Position));
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(Plant), "TickLong")]
	public static class TickGrow_PlantHarvest
	{
		private static PlantHarvestOverlay overlay;
		public static void Postfix()
		{
			overlay ??= BaseOverlay.GetOverlay<PlantHarvestOverlay>();
			overlay.SetDirty();
		}
	}
}
