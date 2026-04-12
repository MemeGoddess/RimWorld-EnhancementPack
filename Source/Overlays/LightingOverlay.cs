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
	class LightingOverlay : BaseOverlay
	{
		public float skyGlow;

		private bool[] _glowerCells;
		private bool[] _checkedGlowerCells;

		public LightingOverlay() : base() { }

		public override bool ShowCell(int index)
		{
			Building edifice = Find.CurrentMap.edificeGrid[index];
			return edifice?.def.passability != Traversability.Impassable &&
				Find.CurrentMap.roofGrid.GetCellBool(index) || LightingAt(index) > skyGlow || GlowerAt(index);
		}

		public override Color GetCellExtraColor(int index)
		{
			return GlowerAt(index) 
				? Color.white 
				: Color.Lerp(Color.red, Color.green, LightingAt(index));
		}

		public float LightingAt(int index)
		{
			return Find.CurrentMap.glowGrid.GroundGlowAt(Find.CurrentMap.cellIndices.IndexToCell(index));
		}

		public bool GlowerAt(int index)
		{
			_checkedGlowerCells ??= new bool[Find.CurrentMap.cellIndices.NumGridCells];
			if (_checkedGlowerCells[index])
				return false;
			_glowerCells ??= new bool[Find.CurrentMap.cellIndices.NumGridCells];
			if (_glowerCells[index])
				return true;

			foreach(var thing in Find.CurrentMap.thingGrid.ThingsListAtFast(index))
				if(thing.TryGetComp<CompGlower>() is not null)
				{
					_glowerCells[index] = true;
					return true;
				}

			_checkedGlowerCells[index] = true;
			return false;
		}

		public void SetDirtySky(float newSky)
		{
			if (skyGlow == newSky) 
				return;

			skyGlow = newSky;
			SetDirty();
		}

		public override void Clear()
		{
			_checkedGlowerCells = null;
			_glowerCells = null;
		}

		private static Texture2D icon = ContentFinder<Texture2D>.Get("LampSun", true);
		public override Texture2D Icon() => icon;
		public override bool IconEnabled() => true;//from Settings
		public override string IconTip() => "TD.ToggleLighting".Translate();

		public override bool ShouldAutoDraw() => Mod.settings.autoOverlayLighting;
		public override IEnumerable<Type> AutoDesignator() => [ typeof(Designator_Build) ];
		public override bool DesignatorVerifier(Designator des)
		{
			return des is Designator_Build desBuild &&
				desBuild.PlacingDef is ThingDef def &&
				def.HasComp(typeof(CompGlower));
		}

		public void DirtyCell(int index)
		{
			_checkedGlowerCells ??= new bool[Find.CurrentMap.cellIndices.NumGridCells];
			_glowerCells ??= new bool[Find.CurrentMap.cellIndices.NumGridCells];
			_glowerCells[index] = false;
			_checkedGlowerCells[index] = false;
		}
	}

	[HarmonyPatch(typeof(GlowGrid), nameof(GlowGrid.DirtyCell))]
	static class GlowGridDirty_Patch
	{
		private static LightingOverlay overlay;
		public static void Postfix(IntVec3 cell, Map ___map)
		{
			if (___map != Find.CurrentMap) 
				return;

			overlay ??= BaseOverlay.GetOverlay<LightingOverlay>();
			overlay.DirtyCell(___map.cellIndices.CellToIndex(cell));
		}
	}

	[HarmonyPatch(typeof(SkyManager), "UpdateOverlays")]
	static class SkyManagerDirty_Patch
	{
		//private void UpdateOverlays(SkyTarget curSky)
		public static void Postfix(SkyManager __instance, Map ___map)
		{
			if (___map == Find.CurrentMap)
				(BaseOverlay.GetOverlay(typeof(LightingOverlay)) as LightingOverlay).SetDirtySky(__instance.CurSkyGlow);
		}
	}	
}
