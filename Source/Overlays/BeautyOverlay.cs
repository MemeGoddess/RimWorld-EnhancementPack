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
	class BeautyOverlay : BaseOverlay
	{
		public static bool firstRender = false;
		private HashSet<int> _shownCells = [];
		private HashSet<int> _checkedCells = [];
		private Dictionary<float, Color> _lerpedColorGood = [];
		private Dictionary<float, Color> _lerpedColorBad = [];
		private float[] _beautyCache;
		public BeautyOverlay() : base() { }

		public override bool ShowCell(int index)
		{
			if (_checkedCells.Contains(index))
				return false;
			if (_shownCells.Contains(index))
				return true;

			var valid = BeautyAt(index) != 0 && _shownCells.Add(index);
			if(!valid)
				_checkedCells.Add(index);

			return valid;
		}

		public override Color GetCellExtraColor(int index)
		{
			firstRender = true;
			if (!_shownCells.Contains(index))
				return Color.white.ToTransparent(0);

			var amount = BeautyAt(index);

			var good = amount > 0;
			amount = amount > 0 ? amount/50 : -amount/10;

			if ((good ? _lerpedColorGood : _lerpedColorBad).TryGetValue(amount, out var color)) 
				return color;

			var baseColor = good ? Color.green : Color.red;
			baseColor.a = 0;

			color = good && amount > 1
				? Color.Lerp(Color.green, Color.white, amount - 1)
				: Color.Lerp(baseColor, good ? Color.green : Color.red, amount);
			(good ? _lerpedColorGood : _lerpedColorBad)[amount] = color;

			return color;
		}

		public override void Clear()
		{
			_shownCells.Clear();
			_checkedCells.Clear();
			_beautyCache = null;
		}

		public void Register(int index, float beauty)
		{
			_beautyCache ??= new float[Find.CurrentMap.cellIndices.NumGridCells];
			_shownCells.Add(index);
			_checkedCells.Remove(index);
			_beautyCache[index] = beauty;
		}

		public void Deregister(int index)
		{
			_beautyCache ??= new float[Find.CurrentMap.cellIndices.NumGridCells];
			_shownCells.Remove(index);
			_checkedCells.Remove(index);
			_beautyCache[index] = 0;
		}

		public float BeautyAt(int index)
		{
			_beautyCache ??= new float[Find.CurrentMap.cellIndices.NumGridCells];
			var beauty = _beautyCache[index];
			if (beauty != 0)
				return beauty;

			beauty = BeautyUtility.CellBeauty(Find.CurrentMap.cellIndices.IndexToCell(index), Find.CurrentMap);

			if (beauty == 0)
			{
				_shownCells.Remove(index);
				return 0;
			}
			_beautyCache[index] = beauty;
			return beauty;
		}

		private static Texture2D icon = ContentFinder<Texture2D>.Get("Heart", true);
		public override Texture2D Icon() => icon;
		public override bool IconEnabled() => true;//from Settings
		public override string IconTip() => "TD.ToggleBeauty".Translate();

	}

	[HarmonyPatch(typeof(TerrainGrid), "DoTerrainChangedEffects")]
	static class TerrainChangedSetDirty
	{
		public static void Postfix(Map ___map)
		{
			if(___map == Find.CurrentMap)
				BaseOverlay.SetDirty(typeof(BeautyOverlay));
		}
	}

	[HarmonyPatch(typeof(ThingGrid), "Register")]
	public static class BeautyDirtierRegister
	{
		private static BeautyOverlay overlay;
		public static void Postfix(Thing t, Map ___map)
		{
			if (!BeautyOverlay.firstRender)
				return;
			if (___map != Find.CurrentMap) 
				return;
			if (!BeautyUtility.BeautyRelevant(t.def.category)) 
				return;

			overlay ??= BaseOverlay.GetOverlay<BeautyOverlay>();
			var index = ___map.cellIndices.CellToIndex(t.Position);
			overlay.Register(index, BeautyUtility.CellBeauty(t.Position, Find.CurrentMap));
			overlay.SetDirty();
		}
	}

	[HarmonyPatch(typeof(ThingGrid), "Deregister")]
	public static class BeautyDirtierDeregister
	{
		private static BeautyOverlay overlay;
		public static void Postfix(Thing t, Map ___map)
		{
			if (___map != Find.CurrentMap)
				return;
			if (!BeautyUtility.BeautyRelevant(t.def.category))
				return;

			overlay ??= BaseOverlay.GetOverlay<BeautyOverlay>();
			var index = ___map.cellIndices.CellToIndex(t.Position);
			overlay.Deregister(index);
			overlay.SetDirty();
		}
	}
}
