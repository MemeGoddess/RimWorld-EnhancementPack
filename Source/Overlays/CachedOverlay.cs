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
	  protected CacheStatus[] _cellCache;
	  protected T[] _cache;

	  protected static Color transparent = Color.white.ToTransparent(0);

		public override bool ShowCell(int index)
	  {
			var cell = _cellCache[index];
			switch (cell)
			{
				case CacheStatus.Uncached:
					var item = GetAndCache(index);
					var valid = IsValid(item, index);
					if (valid)
					{
						_cellCache[index] = CacheStatus.True;
					}
					else
					{
						_cellCache[index] = CacheStatus.False;
						if (item != null)
							_cache[index] = default;
					}
					return true;
				case CacheStatus.True:
					return true;
				case CacheStatus.False: 
					return false;
				default:
					throw new NotImplementedException();
			}
	  }

	  public override Color GetCellExtraColor(int index)
	  {
		  if (_cellCache[index] == CacheStatus.False)
			  return transparent;

		  var item = GetAndCache(index);
		  if (item != null) 
				return GetColor(item, index);

		  _cellCache[index] = CacheStatus.False;
		  return transparent;
	  }

	  //public override void Clear()
	  //{
		 // _map = null;
		 // var cells = _map.cellIndices.NumGridCells;
			//_cellCache = null;
		 // _cache = null;
	  //}

	  public virtual void Register(int index, T item)
	  {
		  _cellCache ??= new CacheStatus[Find.CurrentMap.cellIndices.NumGridCells];
		  _cache ??= new T[Find.CurrentMap.cellIndices.NumGridCells];
			_cellCache[index] = CacheStatus.True;
		  _cache[index] = item;
	  }

	  public virtual void Deregister(int index)
	  {
		  _cellCache ??= new CacheStatus[Find.CurrentMap.cellIndices.NumGridCells];
		  _cache ??= new T[Find.CurrentMap.cellIndices.NumGridCells];

			_cellCache[index] = CacheStatus.Uncached;
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
			  _cellCache[index] = CacheStatus.False;
			  return default;
		  }

			_cache[index] = item;
			return item;
	  }

		[CanBeNull]
		protected abstract T GetValue(int index);
	  protected abstract Color GetColor(T item, int index);

		public abstract bool IsValid(T item, int index);
  }

	public enum CacheStatus : byte
	{
		Uncached,
		False,
		True
	}
}
