using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TD_Enhancement_Pack.Utilities
{
  internal static class DefExtensions
  {
	  internal static bool IsOrBuildsToTable(this ThingDef def) =>
		  def.IsTable || ((def.entityDefToBuild as ThingDef)?.IsTable ?? false);
  }
}
