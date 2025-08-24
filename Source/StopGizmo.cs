using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using HarmonyLib;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;

namespace TD_Enhancement_Pack
{
	[StaticConstructorOnStartup]
	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	public static class StopGizmo
	{
		private static Texture2D StopIcon = ContentFinder<Texture2D>.Get("Stop", true);

		//public override IEnumerable<Gizmo> GetGizmos()
		public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> result, Pawn __instance)
		{
			foreach( Gizmo gizmo in result )
				yield return gizmo;

			if (__instance.Drafted ? !Mod.settings.showStopGizmoDrafted : !Mod.settings.showStopGizmo) yield break;

			if (Find.World.renderer.wantedMode != WorldRenderMode.None) yield break;


			if (!DebugSettings.godMode)
			{
				if (!(__instance.drafter?.ShowDraftGizmo ?? false))
					yield break;

				if (__instance.jobs.curJob != null && !__instance.jobs.IsCurrentJobPlayerInterruptible())
					yield break;

				if (__instance.Downed || __instance.Deathresting)
					yield break;

				if (ModsConfig.BiotechActive && __instance.IsColonyMech && !MechanitorUtility.CanDraftMech(__instance))
					yield break;
			}

			yield return new Command_Action()
			{
				defaultLabel = "TD.StopGizmo".Translate(),
				icon = StopIcon,
				defaultDesc = (__instance.Drafted ? "TD.StopDescDrafted".Translate() : "TD.StopDescUndrafted".Translate()) + "\n\n" + "TD.AddedByTD".Translate(),
				action = delegate
				{
					foreach (Pawn pawn in Find.Selector.SelectedObjects.Where(o => o is Pawn).Cast<Pawn>())
					{
						pawn.jobs.StopAll(false);
					}
				},
				hotKey = KeyBindingDefOf.Designator_Deconstruct,
				Order = -30f
			};
		}
	}
}
