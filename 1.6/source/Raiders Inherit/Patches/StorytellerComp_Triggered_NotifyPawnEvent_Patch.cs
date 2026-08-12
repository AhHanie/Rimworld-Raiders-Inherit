using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Raiders_Inherit.Patches
{
    [HarmonyPatch(typeof(StorytellerComp_Triggered), nameof(StorytellerComp_Triggered.Notify_PawnEvent))]
    public static class StorytellerComp_Triggered_NotifyPawnEvent_Patch
    {
        private static IncidentDef strangerInBlackJoin;

        private static IncidentDef StrangerInBlackJoin => strangerInBlackJoin ?? (strangerInBlackJoin = DefDatabase<IncidentDef>.GetNamedSilentFail("StrangerInBlackJoin"));

        public static bool Prefix(StorytellerComp __instance, Pawn p)
        {
            if (!(__instance.props is StorytellerCompProperties_Triggered props) || props.incident != StrangerInBlackJoin)
            {
                return true;
            }
            if (AnyMapHasPendingInheritance())
            {
                return false;
            }
            return true;
        }

        private static bool AnyMapHasPendingInheritance()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                RaidInheritanceMapComponent component = maps[i].GetComponent<RaidInheritanceMapComponent>();
                if (component != null && component.HasPendingInheritance())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
