using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace TD_Enhancement_Pack;

[HarmonyPatch(typeof(PlaySettings), "DoMapControls")]
class ShowVanillaMapControlToggles
{
    static bool Prefix(WidgetRow row, PlaySettings __instance)
    {
        // row.ToggleableIcon(ref __instance.showLearningHelper, Verse.TexButton.ShowLearningHelper, "ShowLearningHelperWhenEmptyToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        if (Mod.settings.toggleShowLearningHelper)
        {
            row.ToggleableIcon(ref __instance.showLearningHelper, Verse.TexButton.ShowLearningHelper, "ShowLearningHelperWhenEmptyToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        }

        // row.ToggleableIcon(ref __instance.showZones, Verse.TexButton.ShowZones, "ZoneVisibilityToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        if (Mod.settings.toggleShowZones)
        {
            row.ToggleableIcon(ref __instance.showZones, Verse.TexButton.ShowZones, "ZoneVisibilityToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        }
        // string arg = KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.ToggleBeautyDisplay, KeyPrefs.BindingSlot.A).ToStringReadable();
        // string tooltip = string.Format("{0}: {1}\n\n{2}", "HotKeyTip".Translate(), arg, "ShowBeautyToggleButton".Translate());
        // row.ToggleableIcon(ref __instance.showBeauty, Verse.TexButton.ShowBeauty, tooltip, SoundDefOf.Mouseover_ButtonToggle);
        // PlaySettings.CheckKeyBindingToggle(KeyBindingDefOf.ToggleBeautyDisplay, ref __instance.showBeauty);
        if (Mod.settings.toggleShowBeauty)
        {
            string arg = KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.ToggleBeautyDisplay, KeyPrefs.BindingSlot.A).ToStringReadable();
            string tooltip = string.Format("{0}: {1}\n\n{2}", "HotKeyTip".Translate(), arg, "ShowBeautyToggleButton".Translate());
            row.ToggleableIcon(ref __instance.showBeauty, Verse.TexButton.ShowBeauty, tooltip, SoundDefOf.Mouseover_ButtonToggle);
            PlaySettings.CheckKeyBindingToggle(KeyBindingDefOf.ToggleBeautyDisplay, ref __instance.showBeauty);
        }

        // string arg2 = KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.ToggleRoomStatsDisplay, KeyPrefs.BindingSlot.A).ToStringReadable();
        // string tooltip2 = string.Format("{0}: {1}\n\n{2}", "HotKeyTip".Translate(), arg2, "ShowRoomStatsToggleButton".Translate());
        // row.ToggleableIcon(ref __instance.showRoomStats, Verse.TexButton.ShowRoomStats, tooltip2, SoundDefOf.Mouseover_ButtonToggle);
        // PlaySettings.CheckKeyBindingToggle(KeyBindingDefOf.ToggleRoomStatsDisplay, ref __instance.showRoomStats);
        if (Mod.settings.toggleShowRoomStats)
        {
            string arg2 = KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.ToggleRoomStatsDisplay, KeyPrefs.BindingSlot.A).ToStringReadable();
            string tooltip2 = string.Format("{0}: {1}\n\n{2}", "HotKeyTip".Translate(), arg2, "ShowRoomStatsToggleButton".Translate());
            row.ToggleableIcon(ref __instance.showRoomStats, Verse.TexButton.ShowRoomStats, tooltip2, SoundDefOf.Mouseover_ButtonToggle);
            PlaySettings.CheckKeyBindingToggle(KeyBindingDefOf.ToggleRoomStatsDisplay, ref __instance.showRoomStats);
        }

        // row.ToggleableIcon(ref __instance.showColonistBar, Verse.TexButton.ShowColonistBar, SteamDeck.IsSteamDeckInNonKeyboardMode ? "ShowColonistBarToggleButtonController".Translate() : "ShowColonistBarToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        if (Mod.settings.toggleShowColonistBar)
        {
            row.ToggleableIcon(ref __instance.showColonistBar, Verse.TexButton.ShowColonistBar, SteamDeck.IsSteamDeckInNonKeyboardMode ? "ShowColonistBarToggleButtonController".Translate() : "ShowColonistBarToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        }

        // row.ToggleableIcon(ref __instance.showRoofOverlay, Verse.TexButton.ShowRoofOverlay, "ShowRoofOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        if (Mod.settings.toggleShowRoofOverlay)
        {
            row.ToggleableIcon(ref __instance.showRoofOverlay, Verse.TexButton.ShowRoofOverlay, "ShowRoofOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        }

		if (Mod.settings.toggleShowFertilityOverlay)
		{
			row.ToggleableIcon(ref __instance.showFertilityOverlay, Verse.TexButton.ShowFertilityOverlay, "ShowFertilityOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
		}
		if (Mod.settings.toggleShowTerrainAffordanceOverlay)
		{
			row.ToggleableIcon(ref __instance.showTerrainAffordanceOverlay, Verse.TexButton.ShowTerrainAffordanceOverlay, "ShowTerrainAffordanceOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
		}
		if (Mod.settings.toggleAutoHomeArea)
		{
			row.ToggleableIcon(ref __instance.autoHomeArea, Verse.TexButton.AutoHomeArea, "AutoHomeAreaToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
		}
		if (Mod.settings.toggleAutoRebuild)
		{
			row.ToggleableIcon(ref __instance.autoRebuild, Verse.TexButton.AutoRebuild, "AutoRebuildButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
		}
		if (Mod.settings.toggleShowTemperatureOverlay)
		{
			row.ToggleableIcon(ref __instance.showTemperatureOverlay, Verse.TexButton.ShowTemperatureOverlay, "ShowTemperatureOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
		}

        // bool toggleable = Prefs.ResourceReadoutCategorized;
        // bool flag = toggleable;
        // row.ToggleableIcon(ref toggleable, Verse.TexButton.CategorizedResourceReadout, "CategorizedResourceReadoutToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        // if (toggleable != flag)
        // {
        // 	Prefs.ResourceReadoutCategorized = toggleable;
        // }
        if (Mod.settings.toggleCategorizedResourceReadout)
        {
            bool toggleable = Prefs.ResourceReadoutCategorized;
            bool flag = toggleable;
            row.ToggleableIcon(ref toggleable, Verse.TexButton.CategorizedResourceReadout, "CategorizedResourceReadoutToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
            if (toggleable != flag)
            {
                Prefs.ResourceReadoutCategorized = toggleable;
            }
        }

		if (ModsConfig.BiotechActive && Mod.settings.toggleShowPollutionOverlay)
        {
            row.ToggleableIcon(ref __instance.showPollutionOverlay, Verse.TexButton.ShowPollutionOverlay, "ShowPollutionOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        }
		if (ModsConfig.OdysseyActive && Find.CurrentMap != null && Find.CurrentMap.Biome.inVacuum)
		{
			row.ToggleableIcon(ref __instance.showVacuumOverlay, Verse.TexButton.ShowVacuumOverlay, "ShowVacuumOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
		}
		else
		{
			__instance.showVacuumOverlay = false;
		}
		string arg3 = KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.OpenMapSearch, KeyPrefs.BindingSlot.A).ToStringReadable();
		string tooltip3 = string.Format("{0}: {1}\n\n{2}", "HotKeyTip".Translate(), arg3, "SearchTheMapDesc".Translate());
		if (row.ButtonIcon(Verse.TexButton.SearchButton, tooltip3) || (KeyBindingDefOf.OpenMapSearch.JustPressed && Event.current.type == EventType.KeyDown))
		{
			Event.current.Use();
			Find.WindowStack.Add(new Dialog_MapSearch(Find.CurrentMap));
		}
		if (ModsConfig.AnomalyActive && Find.Anomaly.AnomalyStudyEnabled)
		{
			UIHighlighter.HighlightOpportunity(row.ButtonIconRect(), "EntityCodex");
			if (row.ButtonIcon(Verse.TexButton.CodexButton, "EntityCodexGizmoTip".Translate()))
			{
				Find.WindowStack.Add(new Dialog_EntityCodex());
			}
		}
        return false; // Return false to skip the original method
    }
}
