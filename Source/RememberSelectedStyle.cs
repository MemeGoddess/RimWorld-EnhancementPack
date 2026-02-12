using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TD_Enhancement_Pack
{
	[HarmonyPatch]
	static class RememberSelectedStyle
	{
		public static Dictionary<DrawStyleCategoryDef, DrawStyleDef> styles = new();
		static MethodBase TargetMethod()
		{
			return AccessTools.PropertySetter(typeof(DesignatorManager), "SelectedStyle");
		}

		static void Prefix(DesignatorManager __instance, DrawStyleDef value)
		{
			if (!Mod.settings.rememberDesignatorShape)
				return;

			if (__instance.SelectedDesignator.DrawStyleCategory == null)
				return;

			styles[__instance.SelectedDesignator.DrawStyleCategory] = value;
		}
	}

	[HarmonyPatch]
	static class SetSelectedStyle
	{
		static MethodBase[] TargetMethods()
		{
			var baseMethod = AccessTools.Method(typeof(Designator), nameof(Designator.Selected));
			return typeof(Designator).AllSubclassesNonAbstract().Select(x => AccessTools.Method(x, nameof(Designator.Selected)))
				.Where(x => x != baseMethod).ToArray() as MethodBase[];
		}
		[HarmonyPostfix]
		public static void Set(Designator __instance)
		{
			if (!Mod.settings.rememberDesignatorShape)
				return;

			if (__instance.DrawStyleCategory == null)
				return;

			if (!RememberSelectedStyle.styles.TryGetValue(__instance.DrawStyleCategory, out var style))
				return;

			Find.DesignatorManager.SelectedStyle = style;
		}
	}

	[HarmonyPatch]
	static class ClearSelectedStyle
	{
		[HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.PreOpen))]
		[HarmonyPrefix]
		public static void Clear()
		{
			RememberSelectedStyle.styles.Clear();
		}
	}
}
