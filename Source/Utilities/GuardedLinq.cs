using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TD_Enhancement_Pack.Utilities
{
  internal static class GuardedLinq
  {
	  public static IEnumerable<T> WhereDefGuarded<T>(this IEnumerable<T> source,
		  Func<T, bool> predicate)
		  where T : Def, new()
	  {
		  if (predicate == null) throw new ArgumentNullException(nameof(predicate));

		  return source.Where(x =>
		  {
			  try
			  {
				  return predicate(x);
			  }
			  catch (ArgumentNullException)
			  {
				  if (!x.defName.StartsWith("blueprint_", StringComparison.CurrentCultureIgnoreCase) &&
				      !x.defName.StartsWith("frame_", StringComparison.CurrentCultureIgnoreCase))
					  Verse.Log.Warning(
						  $"[TD Enhancement Pack] The def {x.defName} ('{x.label}') from '{x.modContentPack.Name}' threw an exception when fetching PlaceWorkers.");

				  return false;
			  }
			  catch (Exception ex)
			  {
				  if (!x.defName.StartsWith("blueprint_", StringComparison.CurrentCultureIgnoreCase) &&
				      !x.defName.StartsWith("frame_", StringComparison.CurrentCultureIgnoreCase))
					  Verse.Log.Warning(
						  $"[TD Enhancement Pack] The def {x.defName} ('{x.label}') from '{x.modContentPack.Name}' threw an unknown exception, possibly to do with PlaceWorkers.");

					Log.Message(ex.Message);

				  return false;
			  }
		  });
	  }

	  public static IEnumerable<T> RemoveBlueprints<T>(this IEnumerable<T> source) where T : Def, new()
	  {
		  return source.Where(x =>
			  !x.defName.StartsWith("blueprint_", StringComparison.CurrentCultureIgnoreCase) &&
			  !x.defName.StartsWith("frame_", StringComparison.CurrentCultureIgnoreCase));
	  }
	}
}
