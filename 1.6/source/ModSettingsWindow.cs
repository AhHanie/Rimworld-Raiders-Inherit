using UnityEngine;
using Verse;

namespace Raiders_Inherit
{
    public static class ModSettingsWindow
    {
        private static string raidersToInheritBuffer;

        public static void Draw(Rect parent)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(parent);

            string inheritanceChanceLabel = "RaidersInherit.InheritanceChance.Label".Translate(ModSettings.inheritanceChance.ToStringPercent("0"));
            TaggedString inheritanceChanceTooltip = "RaidersInherit.InheritanceChance.Tooltip".Translate();
            Rect sliderRowRect = listing.GetRect(Text.LineHeight * 2f);
            ModSettings.inheritanceChance = Widgets.HorizontalSlider(sliderRowRect, ModSettings.inheritanceChance, 0f, 1f, middleAlignment: false, inheritanceChanceLabel, "0%", "100%", 0.01f);
            TooltipHandler.TipRegion(sliderRowRect, inheritanceChanceTooltip);

            listing.Gap();

            listing.CheckboxLabeled("RaidersInherit.InheritEntireRaid.Label".Translate(), ref ModSettings.inheritEntireRaid, "RaidersInherit.InheritEntireRaid.Tooltip".Translate());

            listing.Gap();

            if (raidersToInheritBuffer == null)
            {
                raidersToInheritBuffer = ModSettings.raidersToInherit.ToString();
            }

            GUI.enabled = !ModSettings.inheritEntireRaid;
            string raidersToInheritLabel = "RaidersInherit.RaidersToInherit.Label".Translate();
            Rect rowRect = listing.GetRect(Text.LineHeight);
            Rect labelRect = new Rect(rowRect.x, rowRect.y, Text.CalcSize(raidersToInheritLabel).x, rowRect.height);
            Rect fieldRect = new Rect(labelRect.xMax + 4f, rowRect.y, 100f, rowRect.height);
            TextAnchor oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, raidersToInheritLabel);
            Text.Anchor = oldAnchor;
            Widgets.TextFieldNumeric(fieldRect, ref ModSettings.raidersToInherit, ref raidersToInheritBuffer, 1f, 999f);
            GUI.enabled = true;

            listing.Label("RaidersInherit.RaidersToInherit.Tooltip".Translate());

            listing.Gap();

            GUI.enabled = ModsConfig.IdeologyActive;
            string enslaveLabel = "RaidersInherit.EnslaveDownedColonists.Label".Translate();
            TaggedString enslaveTooltip = "RaidersInherit.EnslaveDownedColonists.Tooltip".Translate();
            listing.CheckboxLabeled(enslaveLabel, ref ModSettings.enslaveDownedColonistsOnTakeover, enslaveTooltip);
            GUI.enabled = true;

            listing.End();
        }
    }
}
