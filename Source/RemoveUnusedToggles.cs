using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace TD_Enhancement_Pack
{
	[HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
	
	class RemoveToggles
	{
		private static bool Running = false;
		private static bool CapturingLabels = false;
		public static List<(string, Texture2D)> labels;
		[HarmonyPriority(Priority.First)]
		public static bool Prefix()
		{
 			Running = true;
			return true;
		}

		[HarmonyPriority(Priority.Last)]
		public static void Postfix()
		{
			CapturingLabels = false;
			Running = false;
		}

		[HarmonyPatch(typeof(WidgetRow), nameof(WidgetRow.ToggleableIcon))]
		[HarmonyPrefix]
		public static bool ToggleableIconRendering(ref bool toggleable,
			Texture2D tex,
			string tooltip,
			SoundDef mouseoverSound = null,
			string tutorTag = null)
		{
			return ShouldRender(tex);
		}

		[HarmonyPatch(typeof(WidgetRow), nameof(WidgetRow.ButtonIcon))]
		[HarmonyPrefix]
		public static bool ButtonIconRendering(
			Texture2D tex,
			string tooltip = null,
			Color? mouseoverColor = null,
			Color? backgroundColor = null,
			Color? mouseoverBackgroundColor = null,
			bool doMouseoverSound = true,
			float overrideSize = -1f)
		{

			return ShouldRender(tex);
		}

		public static bool ShouldRender(Texture2D tex)
		{
			if (!Running)
				return true;

			if (labels == null)
			{
				labels = new List<(string, Texture2D)>();
				CapturingLabels = true;
			}

			if (CapturingLabels)
				labels.Add((tex.name, tex));

			if (!Mod.settings.toggleShowButtons.TryGetValue(tex.name, out var val))
			{
				Mod.settings.toggleShowButtons[tex.name] = true;
				return true;
			}

			return val;
		}
	}

  //[HarmonyPatch(typeof(PlaySettings), "DoMapControls")]
	class RemoveUnusedToggles
	{
		//public void DoPlaySettingsGlobalControls(WidgetRow row, bool worldView)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo ToggleableIconInfo = AccessTools.Method(typeof(WidgetRow), nameof(WidgetRow.ToggleableIcon));
			MethodInfo ToggleableIconReplacement = AccessTools.Method(typeof(RemoveUnusedToggles), nameof(ToggleableIconFiltered));

			FieldInfo settingsInfo = AccessTools.Field(typeof(Mod), nameof(Mod.settings));

			// PlaySettings draws icons via WidgetRow.ToggleableIcon(ref bool toggleable, Texture2D tex ...)
			// Each bool has its own icon, so detect PlaySettings by their icon:
			string[] fieldNames = [
				nameof(Verse.TexButton.ShowLearningHelper),
				nameof(Verse.TexButton.ShowZones),
				nameof(Verse.TexButton.ShowBeauty),
				nameof(Verse.TexButton.ShowRoomStats),
				nameof(Verse.TexButton.ShowColonistBar),
				nameof(Verse.TexButton.ShowRoofOverlay),
				nameof(Verse.TexButton.ShowFertilityOverlay),
				nameof(Verse.TexButton.ShowTerrainAffordanceOverlay),
				nameof(Verse.TexButton.AutoHomeArea),
				nameof(Verse.TexButton.AutoRebuild),
				nameof(Verse.TexButton.ShowTemperatureOverlay),
				nameof(Verse.TexButton.CategorizedResourceReadout),
				nameof(Verse.TexButton.ShowPollutionOverlay)
				];
			int fieldIndex = 0;

			// Find the ILCode that loads this texture:
			FieldInfo showToggleButtonTexInfo = AccessTools.Field(typeof(Verse.TexButton), fieldNames[fieldIndex]);
			// (These are ordered by when them method uses them, so only need to check one at a time)

			bool modifyThisCall = false;

			foreach (CodeInstruction inst in instructions)
			{
				// When we load the next TexButton...
				if (showToggleButtonTexInfo != null && inst.LoadsField(showToggleButtonTexInfo))
				{
					modifyThisCall = true;


					// Get the TD Toggle Setting bool for this PlaySetting: named toggle<TexName>
					FieldInfo settingToToggleThat = AccessTools.Field(typeof(Settings), "toggle" + fieldNames[fieldIndex]);

					// (And prep for next field:)
					fieldIndex++;
					showToggleButtonTexInfo = fieldIndex < fieldNames.Length ? AccessTools.Field(typeof(Verse.TexButton), fieldNames[fieldIndex]) : null;

					// Simply load the TD toggle setting bool before the texture
					yield return new CodeInstruction(OpCodes.Ldsfld, settingsInfo); //Mod.settings
					yield return new CodeInstruction(OpCodes.Ldfld, settingToToggleThat);//Mod.settings.toggleShow~Whatever~

					yield return inst;  //TexButton.Show~Whatever~

					// And then...
				}
				else if (inst.Calls(ToggleableIconInfo) && modifyThisCall)
				{
					// Modify the WidgetRow.ToggleableIcon to our override with inserted bool settingToToggleThat
					yield return new CodeInstruction(OpCodes.Call, ToggleableIconReplacement);


					// and make sure we don't modify ToggleableIcons calls that didn't just get this treatment
					modifyThisCall = false;
				}
				else
					yield return inst;
			}
		}

		//public void ToggleableIcon(ref bool toggleable, Texture2D tex, string tooltip, SoundDef mouseoverSound = null, string tutorTag = null)
		public static void ToggleableIconFiltered(WidgetRow row, ref bool toggleable, bool settingToToggleThat, Texture2D tex, string tooltip, SoundDef mouseoverSound = null, string tutorTag = null)
		{
			if(settingToToggleThat)
				row.ToggleableIcon(ref toggleable, tex, tooltip, mouseoverSound, tutorTag);
		}
	}
}