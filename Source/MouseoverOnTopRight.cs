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
using System.Drawing;
using Color = UnityEngine.Color;

namespace TD_Enhancement_Pack
{

	[HarmonyPatch]
	public static class MouseoverOnTopRight
	{
		static Vector2 lastMousePos = Vector2.zero;
		private static double timeLastMoved;
		private const double secondsWaitUnfade = 0.25;
		private const double secondsDurationUnfade = 0.25;
		private static int labelsDrawn = 0;
		private static bool shouldDoBuffer = false;

		[HarmonyTargetMethods]
		public static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(MouseoverReadout), "MouseoverReadoutOnGUI");
			MethodInfo vee = AccessTools.Method("VEE.HarmonyInit:AddDroughtLine"); // VE Events
			if( vee != null )
				yield return vee;
			MethodInfo dbh = AccessTools.Method("DubsBadHygiene.Patches.HarmonyPatches_Fertilizer/H_MouseoverReadoutOnGUI:AddSewageLine");
			if( dbh != null ) // Dubs Bad Hygiene
				yield return dbh;
		}

		//public void MouseoverReadoutOnGUI()
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo DrawTextWinterShadowInfo = AccessTools.Method(typeof(GenUI), "DrawTextWinterShadow");
			MethodInfo LabelInfo = AccessTools.Method(typeof(Widgets), "Label", new Type[] { typeof(Rect), typeof(string) });
			MethodInfo LabelTaggedInfo = AccessTools.Method(typeof(Widgets), "Label", new Type[] { typeof(Rect), typeof(TaggedString) });
			MethodInfo OpenTabInfo = AccessTools.Property(typeof(MainTabsRoot), "OpenTab").GetGetMethod();

			List<CodeInstruction> instList = instructions.ToList();
			for (int i = 0; i < instList.Count; i++)
			{
				CodeInstruction inst = instList[i];

				//Topright winter shadow
				if (inst.Calls(DrawTextWinterShadowInfo))
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MouseoverOnTopRight), nameof(DrawTextWinterShadowTR)));

				//Transform Widgets.Label rect
				else if (inst.Calls(LabelInfo))
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MouseoverOnTopRight), nameof(LabelTransform)));
				else if (inst.Calls(LabelTaggedInfo))
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MouseoverOnTopRight), nameof(LabelTaggedTransform)));
				else
					yield return inst;

				if (inst.Calls(OpenTabInfo))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MouseoverOnTopRight), nameof(FilterForOpenTab)));// 0 != null is false
				}
			}
		}

		public static void DrawTextWinterShadowTR(Rect badRect)
		{
			switch (Mod.settings.mouseoverInfoLocation)
			{
				case MouseoverInfoLocation.BottomLeft:
					GenUI.DrawTextWinterShadow(badRect);
					break;
				case MouseoverInfoLocation.TopRight:
					GenUI.DrawTextWinterShadow(new Rect(UI.screenWidth - 256f, 256f, 256f, -256f));
					break;
				case MouseoverInfoLocation.Mouse:
					var mouse = Event.current.mousePosition;
					var height = Math.Clamp(labelsDrawn * Text.LineHeight + 30f, 0f, 256f);
					labelsDrawn = 0;
					var rect = new Rect(mouse + new Vector2(256f + 15f, height + 15f), new Vector2(-256f, -height));

					var color = GUI.color;
					var didColourChange = GetColorForFade(out var fade, true);
					GUI.color = fade;
					DrawTextWinterShadow(rect);
					if (didColourChange)
						GUI.color = color;
					break;
				case null:
					GenUI.DrawTextWinterShadow(badRect);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static void LabelTransform(Rect rect, string label)
		{
			var didColourChange = false;
			var colour = GUI.color;
			if (Mod.settings.mouseoverInfoLocation == MouseoverInfoLocation.Mouse)
			{
				labelsDrawn++;
				didColourChange = GetColorForFade(out var fade);
				GUI.color = fade;
			}
			Widgets.Label(Transform(rect, label), label);

			if (didColourChange)
				GUI.color = colour;
		}
		public static void LabelTaggedTransform(Rect rect, TaggedString label)
		{
			var didColourChange = false;
			var colour = GUI.color;
			if (Mod.settings.mouseoverInfoLocation == MouseoverInfoLocation.Mouse)
			{
				labelsDrawn++;
				didColourChange = GetColorForFade(out var fade);
				GUI.color = fade;
			}

			Widgets.Label(Transform(rect, label), label);

			if (didColourChange)
				GUI.color = colour;
		}
		public static Rect Transform(Rect rect, string label)
		{
			switch (Mod.settings.mouseoverInfoLocation)
			{
				case MouseoverInfoLocation.TopRight:
					rect.x = UI.screenWidth - rect.x; //flip x
					rect.y = UI.screenHeight - rect.y - 50f; //flip y, adjust for maintabs margin: BotLeft.y = 65f, BotLeft.x = 15f
					rect.x -= Text.CalcSize(label).x;//adjust for text width
					break;
				case MouseoverInfoLocation.Mouse:
					var mouse = Event.current.mousePosition;
					if (Mod.settings.mouseLocationDoFade && (!shouldDoBuffer && mouse != lastMousePos) || Vector2.Distance(mouse, lastMousePos) > 25f)
					{
						timeLastMoved = Time.realtimeSinceStartupAsDouble;
						lastMousePos = mouse;
						shouldDoBuffer = false;
					}
					rect.y = UI.screenHeight - rect.y - 50f; //flip y, adjust for maintabs margin: BotLeft.y = 65f, BotLeft.x = 15f
					rect.position += mouse;
					rect.position += new Vector2(15f, 15f);
					break;
			}

			return rect;
		}

		public static MainButtonDef FilterForOpenTab(MainButtonDef def)
		{
			return Mod.settings.mouseoverInfoLocation != MouseoverInfoLocation.BottomLeft ? null : def;
		}

		private static bool GetColorForFade(out Color selectedColor, bool forShadow = false)
		{
			if (Mod.settings.mouseLocationDoFade)
			{
				var min = forShadow ? 0f : 0.2f;
				var max = forShadow ? 0.8f : 1f;
				var color = GUI.color;
				var timeSinceLastMoved = Time.realtimeSinceStartupAsDouble - timeLastMoved;
				float alpha;
				switch (timeSinceLastMoved)
				{
					case <= secondsWaitUnfade:
						alpha = min;
						break;
					case > secondsWaitUnfade + secondsDurationUnfade:
						alpha = max;
						shouldDoBuffer = true;
						break;
					default:
						alpha = Mathf.Clamp((float)((timeSinceLastMoved - secondsWaitUnfade) / secondsDurationUnfade), min, max);
						break;
				}

				selectedColor = new Color(color.r, color.g, color.b, color.a * alpha);
				return true;
			}

			selectedColor = GUI.color;
			return false;
		}

		private static void DrawTextWinterShadow(Rect rect)
		{
			float a = GenUI.BackgroundDarkAlphaForText();
			if ((double)a <= 1.0 / 1000.0)
				return;
			GUI.color = new Color(1f, 1f, 1f, a);
			GUI.DrawTexture(rect, TexButton.UnderShadowTex, ScaleMode.ScaleAndCrop);
			GUI.color = Color.white;
		}
	}


  //And the method that MouseoverReadoutOnGUI calls which calls Widgets.Label:
  //private void DrawGas(GasType gasType, byte density, ref float curYOffset)
	[HarmonyPatch(typeof(MouseoverReadout), "DrawGas")]
	public static class MouseoverOnTopRightDrawGas
	{
		//public void MouseoverReadoutOnGUI()
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			 => MouseoverOnTopRight.Transpiler(instructions);
	}

	[HarmonyPatch]
	public static class Patch_MouseoverReadout_ShouldShow
	{
		static MethodBase TargetMethod() =>
			AccessTools.PropertyGetter(typeof(MouseoverReadout), "ShouldShow");

		static bool Prefix(ref bool __result)
		{
			if (!Mod.settings.mouseoverInfoTopRight) return true;

			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(GenUI), nameof(GenUI.BackgroundDarkAlphaForText))]
	public static class Patch_BackgroundDarkAlphaForText_UseGUIAlpha
	{
		public static void Postfix(ref float __result)
		{
			if(Mod.settings.mouseLocationDoFade)
				__result *= GUI.color.a;
		}
	}
}