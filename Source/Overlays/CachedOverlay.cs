using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace TD_Enhancement_Pack.Overlays
{
	abstract class CachedOverlay<T> : BaseOverlay
  {
	  private bool[] _shownCells;
	  private bool[] _checkedCells;
	  private T[] _cache;

	  protected static Color transparent = Color.white.ToTransparent(0);

		public override bool ShowCell(int index)
	  {
		  _checkedCells ??= new bool[Find.CurrentMap.cellIndices.NumGridCells];
		  if (_checkedCells[index])
			  return false;
		  _shownCells ??= new bool[Find.CurrentMap.cellIndices.NumGridCells];
		  if (_shownCells[index])
			  return true;

		  var valid = IsValid(GetAndCache(index));
			if(valid)
				_shownCells[index] = true;
			else
				_checkedCells[index] = true;

			return valid;
	  }

	  public override Color GetCellExtraColor(int index)
	  {
		  if (!_shownCells[index])
			  return transparent;

		  var item = GetAndCache(index);
		  if (item == null)
		  {
			  _shownCells[index] = false;
			  return transparent;
		  }

		  return GetColor(item);
	  }

	  public override void Clear()
	  {
		  _shownCells = null;
		  _checkedCells = null;
		  _cache = null;
	  }

	  public void Register(int index, T item)
	  {
		  var numCells = Find.CurrentMap.cellIndices.NumGridCells;
		  _shownCells ??= new bool[numCells];
		  _checkedCells ??= new bool[numCells];
		  _cache ??= new T[Find.CurrentMap.cellIndices.NumGridCells];
		  _shownCells[index] = true;
		  _checkedCells[index] = false;
		  _cache[index] = item;
	  }

	  public void Deregister(int index)
	  {
		  var numCells = Find.CurrentMap.cellIndices.NumGridCells;
		  _shownCells ??= new bool[numCells];
		  _checkedCells ??= new bool[numCells];
		  _cache ??= new T[Find.CurrentMap.cellIndices.NumGridCells];
		  _shownCells[index] = false;
		  _checkedCells[index] = false;
		  _cache[index] = default;
	  }

	  private T GetAndCache(int index)
	  {
		  _cache ??= new T[Find.CurrentMap.cellIndices.NumGridCells];
		  var item = _cache[index];
		  if (item != null)
			  return item;
		  
		  item = GetValue(index);
		  if (item == null)
		  {
			  _shownCells[index] = false;
			  return default;
		  }

			_cache[index] = item;
			return item;
	  }

		[CanBeNull]
		protected abstract T GetValue(int index);
	  protected abstract Color GetColor(T item);

		public abstract bool IsValid(T item);
  }
}
