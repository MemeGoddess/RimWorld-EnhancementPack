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

		public override bool IsValid(Plant plant) =>
			plant?.def?.plant is { harvestTag: "Standard" };

		protected override Plant GetValue(int index) =>
			Find.CurrentMap.thingGrid.ThingsListAtFast(index)
				.FirstOrDefault(t => t is Plant plant && IsValid(plant)) as Plant;

		protected override Color GetColor(Plant plant)
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
			if (t is not Plant plant || !overlay.IsValid(plant)) 
				return;

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
			overlay ??= BaseOverlay.GetOverlay<PlantHarvestOverlay>();
			if (___map != Find.CurrentMap)
				return;
			if (t is not Plant plant || overlay.IsValid(plant))
				return;

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
			overlay ??= BaseOverlay.GetOverlay<PlantHarvestOverlay>();
			if (__instance.Map != Find.CurrentMap)
				return;

			if (!overlay.IsValid(__instance))
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
