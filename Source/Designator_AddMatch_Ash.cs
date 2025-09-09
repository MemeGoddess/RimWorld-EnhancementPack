using HarmonyLib;
using Mashed_Ashlands;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TD_Enhancement_Pack
{
  internal class Designator_AddMatch_Ash : Designator_ZoneAdd_GrowingAsh
	{
		public Designator_AddMatch_Ash() : base()
		{
			this.defaultLabel = "TD.Match".Translate();
			this.defaultDesc = "TD.MatchDesc".Translate();
		}
		public static AccessTools.FieldRef<DesignationDragger, IntVec3> GetStartDragCell =
			AccessTools.FieldRefAccess<DesignationDragger, IntVec3>("startDragCell");
		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
			var original = base.CanDesignateCell(c);
			if(!original.Accepted) return original;

			FertilityGrid grid = Map.fertilityGrid;
			IntVec3 startDragCell = GetStartDragCell(Find.DesignatorManager.Dragger);
			return grid.FertilityAt(c) == grid.FertilityAt(startDragCell);
		}

		public override bool Visible => Mod.settings.matchGrowButton && base.Visible;
	}

	[HarmonyPatch(typeof(Zone_GrowingAsh), nameof(Zone_GrowingAsh.GetZoneAddGizmos))]
	public static class SelectedZoneMatchGizmoAsh
	{
		//public override IEnumerable<Gizmo> GetZoneAddGizmos()
		public static void Postfix(ref IEnumerable<Gizmo> __result)
		{
			Log.Message("Injecting Ash");
			List<Gizmo> result = new List<Gizmo>();
			result.Add(DesignatorUtility.FindAllowedDesignator<Designator_AddMatch_Ash>());
			result.AddRange(__result);
			__result = result;
		}
	}
}
