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

            listing.CheckboxLabeled("RaidersInherit.InheritEntireRaid.Label".Translate(), ref ModSettings.inheritEntireRaid, "RaidersInherit.InheritEntireRaid.Tooltip".Translate());

            listing.Gap();

            if (raidersToInheritBuffer == null)
            {
                raidersToInheritBuffer = ModSettings.raidersToInherit.ToString();
            }

            GUI.enabled = !ModSettings.inheritEntireRaid;
            listing.TextFieldNumericLabeled("RaidersInherit.RaidersToInherit.Label".Translate(), ref ModSettings.raidersToInherit, ref raidersToInheritBuffer, 1f, 999f);
            GUI.enabled = true;

            listing.Label("RaidersInherit.RaidersToInherit.Tooltip".Translate());

            listing.End();
        }
    }
}
