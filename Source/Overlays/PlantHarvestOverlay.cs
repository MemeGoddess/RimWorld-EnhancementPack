using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using TD_Enhancement_Pack.Overlays;

namespace TD_Enhancement_Pack
{
	[StaticConstructorOnStartup]
	class PlantHarvestOverlay : CachedOverlay<Plant>
	{
		private Dictionary<float, Color> _lerpedColor = [];
		public PlantHarvestOverlay() : base() { }
		
		public override bool ShouldAutoDraw() => Mod.settings.autoOverlayPlantHarvest;
		public override IEnumerable<Type> AutoDesignator() => [ typeof(Designator_PlantsHarvest), typeof(Designator_PlantsCut) ];

		public static Texture2D icon = ContentFinder<Texture2D>.Get("UI/Designators/Harvest", true);
		public override Texture2D Icon() => icon;
		public override bool IconEnabled() => true;
		public override string IconTip() => "TD.TogglePlantHarveset".Translate();

		public override bool IsValid(Plant plant, int index) =>
			plant?.def?.plant is { harvestTag: "Standard" };

		protected override Plant GetValue(int index) =>
			Find.CurrentMap.thingGrid.ThingsListAtFast(index)
				.FirstOrDefault(t => t is Plant plant && IsValid(plant, index)) as Plant;

		protected override Color GetColor(Plant plant, int index)
		{
			if (plant == null) 
				return Color.magenta; //shouldn't happen

			if (plant.Growth > 0.99900001287460327)
				return Color.white;

			if (_lerpedColor.TryGetValue(plant.Growth, out var color))
				return color;

			color = Color.Lerp(Color.red, Color.green, plant.Growth);
			_lerpedColor[plant.Growth] = color;
			return color;
		}
	}

	[HarmonyPatch(typeof(ThingGrid), "Deregister")]
	public static class ThingDirtierDeregister_PlantHarvest
	{
		private static PlantHarvestOverlay overlay;
		public static void Postfix(Thing t, Map ___map)
		{
			overlay ??= BaseOverlay.GetOverlay<PlantHarvestOverlay>();
			if (___map != Find.CurrentMap)
				return;

			var index = ___map.cellIndices.CellToIndex(t.Position);
			if (t is not Plant plant || !overlay.IsValid(plant, index)) 
				return;

			overlay.Deregister(index);
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(ThingGrid), nameof(ThingGrid.Register))]
	public static class ThingDirtierRegister_PlantHarvest
	{
		private static PlantHarvestOverlay overlay;
		public static void Postfix(Thing t, Map ___map)
		{
			overlay ??= BaseOverlay.GetOverlay<PlantHarvestOverlay>();
			if (___map != Find.CurrentMap)
				return;

			var index = ___map.cellIndices.CellToIndex(t.Position);
			if (t is not Plant plant || overlay.IsValid(plant, index))
				return;

			overlay.Register(index, plant);
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(Plant), "PlantCollected")]
	public static class PlantCollected_PlantHarvest
	{
		private static PlantHarvestOverlay overlay;
		public static void Postfix(Plant __instance)
		{
			overlay ??= BaseOverlay.GetOverlay<PlantHarvestOverlay>();
			if (__instance.Map != Find.CurrentMap)
				return;

			var index = __instance.Map.cellIndices.CellToIndex(__instance.Position);
			if (!overlay.IsValid(__instance, index))
				return;

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
