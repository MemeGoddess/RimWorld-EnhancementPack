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
	[HarmonyPatch]
	public static class MouseoverOnTopRight
	{
		static Vector2 lastMousePos = Vector2.zero;
		private static double timeLastMoved;
		private const double secondsWaitUnfade = 0.25;
		private const double secondsDurationUnfade = 0.25;

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
					var rect = new Rect(mouse, new Vector2(256f, 256f));
					GenUI.DrawTextWinterShadow(rect);
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
				var timeSinceLastMoved = Time.realtimeSinceStartupAsDouble - timeLastMoved;
				float alpha;
				if (timeSinceLastMoved <= secondsWaitUnfade)
				{
					alpha = 0.4f;
				}
				else
				{
					alpha = Mathf.Clamp((float)((timeSinceLastMoved - secondsWaitUnfade) / secondsDurationUnfade), 0.4f, 1);
				}

				didColourChange = true;
				GUI.color = new Color(colour.r, colour.g, colour.b, colour.a * alpha);
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
				var timeSinceLastMoved = Time.realtimeSinceStartupAsDouble - timeLastMoved;
				float alpha;
				if (timeSinceLastMoved <= secondsWaitUnfade)
				{
					alpha = 0.4f;
				}
				else
				{
					alpha = Mathf.Clamp((float)((timeSinceLastMoved - secondsWaitUnfade) / secondsDurationUnfade), 0.4f, 1);
				}

				didColourChange = true;
				GUI.color = new Color(colour.r, colour.g, colour.b, colour.a * alpha);
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
					if (mouse != lastMousePos)
					{
						timeLastMoved = Time.realtimeSinceStartupAsDouble;
						lastMousePos = mouse;
					}
					rect.y = UI.screenHeight - rect.y - 50f; //flip y, adjust for maintabs margin: BotLeft.y = 65f, BotLeft.x = 15f
					rect.position += mouse;
					break;
			}

			return rect;
		}

		public static MainButtonDef FilterForOpenTab(MainButtonDef def)
		{
			return Mod.settings.mouseoverInfoLocation != MouseoverInfoLocation.BottomLeft ? null : def;
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
}