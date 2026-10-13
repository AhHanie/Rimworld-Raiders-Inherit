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

        // Infix on the single IncidentWorker.CanFireNow call inside Notify_PawnEvent: the vanilla colonist-viability
        // checks still run, and only the final "queue the Man in Black" step is vetoed while a takeover is pending.
        [HarmonyInfix(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow), typeof(IncidentParms))]
        public static bool Postfix(bool __result, [HarmonyOuter] StorytellerComp __instance)
        {
            if (!__result)
            {
                return false;
            }
            if (!(__instance.props is StorytellerCompProperties_Triggered props) || props.incident != StrangerInBlackJoin)
            {
                return true;
            }
            return !AnyMapHasPendingInheritance();
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
