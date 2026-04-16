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

namespace TD_Enhancement_Pack
{
	[StaticConstructorOnStartup]
	class WalkSpeedOverlay : CachedOverlay<TerrainDef>
	{
		private static Color?[] colorCache;

		public WalkSpeedOverlay() : base()
		{
			var terrains = DefDatabase<TerrainDef>.AllDefsListForReading;

			var max = terrains.Max(x => x.pathCost);
			colorCache = new Color?[max];
		}

		protected override TerrainDef GetValue(int index)
		{
			return Find.CurrentMap.terrainGrid.TerrainAt(index);
		}

		protected override Color GetColor(TerrainDef item, int index)
		{
			var pathCost = item.pathCost;
			var color = colorCache[pathCost];
			if(color != null)
				return color.Value;

			var f = 13f / (pathCost + 13f);
			color = f < 1 ? Color.Lerp(Color.red, Color.green, f * 0.75f)
				: Color.Lerp(Color.green, Color.white, f - 1);
			colorCache[pathCost] = color;
			return color.Value;
		}
		
		public override bool IsValid(TerrainDef item, int index) =>
			Find.CurrentMap.edificeGrid[index]?.def.passability != Traversability.Impassable;

		private static Texture2D icon = ContentFinder<Texture2D>.Get("Footprint", true);
		public override Texture2D Icon() => icon;
		public override bool IconEnabled() => true;//from Settings
		public override string IconTip() => "TD.ToggleWalkSpeed".Translate();

		public override void Deregister(int index)
		{
			base.Deregister(index);

			if (Find.CurrentMap?.edificeGrid[index]?.def?.passability == Traversability.Impassable)
				_cellCache[index] = CacheStatus.False;
			else
				_cellCache[index] = CacheStatus.True;
		}
	}

	[HarmonyPatch(typeof(TerrainGrid), "DoTerrainChangedEffects")]
	static class DoTerrainChangedEffects_Patch_WalkSpeed
	{
		private static WalkSpeedOverlay overlay;
		public static void Postfix(IntVec3 c, TerrainDef oldTerr, TerrainDef newTerr, Map ___map)
		{
			if (___map != Find.CurrentMap)
				return;

			overlay ??= BaseOverlay.GetOverlay<WalkSpeedOverlay>();

			var index = ___map.cellIndices.CellToIndex(c);
			if (!overlay.IsValid(newTerr, index))
				return;

			overlay.Register(index, newTerr);
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(EdificeGrid), "Register")]
	static class EdificeGrid_Register_SetDirty
	{
		private static WalkSpeedOverlay overlay;
		public static void Postfix(Building ed, Map ___map)
		{
			if (___map != Find.CurrentMap)
				return;
			overlay ??= BaseOverlay.GetOverlay<WalkSpeedOverlay>();
      var cellIndices = ___map.cellIndices;
      var cellRect = ed.OccupiedRect();
			for (var minZ = cellRect.minZ; minZ <= cellRect.maxZ; ++minZ)
			{
				for (var minX = cellRect.minX; minX <= cellRect.maxX; ++minX)
				{
					var c = new IntVec3(minX, 0, minZ);
					var index = cellIndices.CellToIndex(c);
					overlay.Deregister(index);
				}
			}
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(EdificeGrid), "DeRegister")]
	static class EdificeGrid_DeRegister_SetDirty
	{
		private static WalkSpeedOverlay overlay;
		public static void Postfix(Building ed, Map ___map)
		{
			if (___map != Find.CurrentMap)
				return;
			overlay ??= BaseOverlay.GetOverlay<WalkSpeedOverlay>();
			var cellIndices = ___map.cellIndices;
			var cellRect = ed.OccupiedRect();
			for (var minZ = cellRect.minZ; minZ <= cellRect.maxZ; ++minZ)
			{
				for (var minX = cellRect.minX; minX <= cellRect.maxX; ++minX)
				{
					var c = new IntVec3(minX, 0, minZ);
					var index = cellIndices.CellToIndex(c);
					var terrain = ___map.terrainGrid.TerrainAt(index);
					overlay.Register(index, terrain);
				}
			}
			overlay.SetDirty();
		}
	}
}
