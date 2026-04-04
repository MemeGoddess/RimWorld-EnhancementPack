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
		private HashSet<int> _shownCells = [];
		private HashSet<int> _checkedCells = [];
		private HashSet<int> _fullyGrown = [];
		private Dictionary<float, Color> _lerpedColor = [];
		private Plant[] _plantCache;
		public PlantHarvestOverlay() : base() { }

		public override bool ShowCell(int index)
		{
			if (_shownCells.Contains(index))
				return true;

			if (_checkedCells.Contains(index))
				return false;

			return _checkedCells.Add(index) && IsValidPlant(FindPlant(index)) && _shownCells.Add(index);
		}

		public override Color GetCellExtraColor(int index)
		{
			if (!_shownCells.Contains(index))
				return Color.white.ToTransparent(0);

			var plant = FindPlant(index);
			if (plant == null) return Color.magenta;//shouldn't happen

			if(_fullyGrown.Contains(index) || (plant.LifeStage == PlantLifeStage.Mature && _fullyGrown.Add(index)))
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
			_shownCells.Clear();
			_checkedCells.Clear();
			_fullyGrown.Clear();
			_plantCache = null;
		}

		public void Register(int index, Plant plant)
		{
			_plantCache ??= new Plant[Find.CurrentMap.cellIndices.NumGridCells];
			_shownCells.Add(index);
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
				.FirstOrDefault(t => t is Plant plant && IsValidPlant(plant)) as Plant;

			
			if(plant == null)
			{
				_shownCells.Remove(index);
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
