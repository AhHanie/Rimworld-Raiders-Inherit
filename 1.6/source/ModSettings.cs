using UnityEngine;
using Verse;

namespace Raiders_Inherit
{
    public class ModSettings : Verse.ModSettings
    {
        public static int raidersToInherit = 5;
        public static bool inheritEntireRaid = false;
        public static float inheritanceChance = 1f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref raidersToInherit, "raidersToInherit", 5);
            Scribe_Values.Look(ref inheritEntireRaid, "inheritEntireRaid", defaultValue: false);
            Scribe_Values.Look(ref inheritanceChance, "inheritanceChance", 1f);
            inheritanceChance = Mathf.Clamp01(inheritanceChance);
        }
    }
}
