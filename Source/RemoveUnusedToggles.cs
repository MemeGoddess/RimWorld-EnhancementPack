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
using System.Diagnostics;

namespace TD_Enhancement_Pack
{
	[HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]

	class RemoveUnusedToggles
	{
		private static bool Running = false;
		private static bool CapturingLabels = false;

		private static Dictionary<Texture2D, string> textureModIsolations = new Dictionary<Texture2D, string>();
		public static List<ToggleButton> labels;

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

			if (!textureModIsolations.TryGetValue(tex, out var texLabel))
			{
				var trace = new StackTrace();
				var frame = trace.GetFrame(2);
				var caller = frame?.GetMethod();
				var callerAsm = caller?.DeclaringType?.Assembly;

				texLabel = callerAsm.GetName().Name != "Assembly-CSharp"
					? callerAsm.GetName().Name + "." + tex.name
					: tex.name;

				textureModIsolations[tex] = texLabel;
			}

			return ShouldRender(tex, texLabel, tooltip);
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
			if (!textureModIsolations.TryGetValue(tex, out var texLabel))
			{
				var trace = new StackTrace();
				var frame = trace.GetFrame(2);
				var caller = frame?.GetMethod();
				var callerAsm = caller?.DeclaringType?.Assembly;

				texLabel = callerAsm.GetName().Name != "Assembly-CSharp"
					? callerAsm.GetName().Name + "." + tex.name
					: tex.name;

				textureModIsolations[tex] = texLabel;
			}

			return ShouldRender(tex, texLabel, tooltip);
		}

		public static bool ShouldRender(Texture2D tex, string texLabel, string tooltip)
		{
			if (!Running)
				return true;

			if (labels == null)
			{
				labels = new();
				CapturingLabels = true;
			}

			if (CapturingLabels)
				labels.Add(new ToggleButton(texLabel, tex, tooltip));

			if (!Mod.settings.toggleShowButtons.TryGetValue(texLabel, out var val))
			{
				Mod.settings.toggleShowButtons[texLabel] = true;
				return true;
			}

			return val;
		}
	}

	public record ToggleButton(string setting, Texture2D texture, string tooltip)
	{
		public string setting { get; } = setting;
		public Texture2D texture { get; } = texture;
		public string tooltip { get; } = tooltip;
	}
}