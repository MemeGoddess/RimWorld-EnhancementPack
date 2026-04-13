using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
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

		private bool[] _shownCells;
		private bool[] _checkedCells;

		private bool[] _glowerCells;
		private bool[] _checkedGlowerCells;

		private bool[] _roofCells;
		private bool[] _checkedRoofCells;

		private float?[] _lightingAt;

		private bool[] _dirtyCells;

		private Dictionary<float, Color> _lerpedColor = new();
		

		private Map _map;

		public LightingOverlay() : base() { }

		public override bool ShowCell(int index)
		{
			_map ??= Find.CurrentMap;
			_checkedCells ??= new bool[_map.cellIndices.NumGridCells];
			if (_checkedCells[index])
				return false;

			_shownCells ??= new bool[_map.cellIndices.NumGridCells];
			if (_shownCells[index])
				return true;

 			var edifice = _map.edificeGrid[index];
			if (edifice?.def.passability != Traversability.Impassable &&
			    Roofed(index) || LightingAt(index) > skyGlow || GlowerAt(index))
			{
				_shownCells[index] = true;
				return true;
			}

			_checkedCells[index] = true;
			return false;
		}

		public override Color GetCellExtraColor(int index)
		{
			if (GlowerAt(index))
				return Color.white;

			var lighting = LightingAt(index);
			if (_lerpedColor.TryGetValue(lighting, out var color))
				return color;

			color = Color.Lerp(Color.red, Color.green, LightingAt(index));
			_lerpedColor[lighting] = color;
			return color;
		}

		public float LightingAt(int index)
		{
			_lightingAt ??= new float?[_map.cellIndices.NumGridCells];

			var lighting = _lightingAt[index];
			if (lighting != null)
				return lighting.Value;

			lighting = _map.glowGrid.GroundGlowAt(_map.cellIndices.IndexToCell(index));
			_lightingAt[index] = lighting;
			return lighting.Value;

		}

		public bool GlowerAt(int index)
		{
			_checkedGlowerCells ??= new bool[_map.cellIndices.NumGridCells];
			if (_checkedGlowerCells[index])
				return false;
			_glowerCells ??= new bool[_map.cellIndices.NumGridCells];
			if (_glowerCells[index])
				return true;

			foreach(var thing in _map.thingGrid.ThingsListAtFast(index))
				if(thing.TryGetComp<CompGlower>() is not null)
				{
					_glowerCells[index] = true;
					return true;
				}

			_checkedGlowerCells[index] = true;
			return false;
		}

		public bool Roofed(int index)
		{
			_checkedRoofCells ??= new bool[_map.cellIndices.NumGridCells];
			if (_checkedRoofCells[index])
				return false;

			_roofCells ??= new bool[_map.cellIndices.NumGridCells];
			if (_roofCells[index])
				return true;

			if (_map.roofGrid.GetCellBool(index))
			{
				_roofCells[index] = true;
				return true;
			}

			_checkedRoofCells[index] = true;
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
			_lightingAt = null;
			_roofCells = null;
			_checkedRoofCells = null;
			_shownCells = null;
			_checkedCells = null;
			_dirtyCells = null;
			_map = null;
		}

		public void DirtyCell(int index)
		{
			_map ??= Find.CurrentMap;
			_dirtyCells ??= new bool[_map.cellIndices.NumGridCells];

			_dirtyCells[index] = true;
		}

		public void DoClean()
		{
			if (_dirtyCells == null)
				return;

			_map ??= Find.CurrentMap;
			_checkedGlowerCells ??= new bool[_map.cellIndices.NumGridCells];
			_glowerCells ??= new bool[_map.cellIndices.NumGridCells];
			_lightingAt ??= new float?[_map.cellIndices.NumGridCells];
			_roofCells ??= new bool[_map.cellIndices.NumGridCells];
			_checkedRoofCells ??= new bool[_map.cellIndices.NumGridCells];
			_shownCells ??= new bool[_map.cellIndices.NumGridCells];
			_checkedCells ??= new bool[_map.cellIndices.NumGridCells];

			for (int i = 0; i < _dirtyCells.Length; i++)
			{
				if (!_dirtyCells[i])
					continue;

				CleanCell(i);
			}
			SetDirty();
		}

		private void CleanCell(int index)
		{
			_glowerCells[index] = false;
			_checkedGlowerCells[index] = false;

			_lightingAt[index] = null;

			_roofCells[index] = false;
			_checkedRoofCells[index] = false;

			_shownCells[index] = false;
			_checkedCells[index] = false;

			_dirtyCells[index] = false;
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

	[HarmonyPatch(typeof(GlowGrid), nameof(GlowGrid.GlowGridUpdate_First))]
	static class GlowGridClean_Patch
	{
		private static LightingOverlay overlay;
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;

				if(instruction.opcode == OpCodes.Stfld && 
				   instruction.operand is FieldInfo { Name: "anyDirtyCell" })
				{
					yield return new CodeInstruction(OpCodes.Call,
						AccessTools.Method(typeof(GlowGridClean_Patch), nameof(DoClean)));
				}
			}
		}

		public static void DoClean()
		{
			overlay ??= BaseOverlay.GetOverlay<LightingOverlay>();
			overlay.DoClean();
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
