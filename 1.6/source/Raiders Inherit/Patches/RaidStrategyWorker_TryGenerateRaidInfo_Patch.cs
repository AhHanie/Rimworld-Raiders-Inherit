using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Raiders_Inherit.Patches
{
    [HarmonyPatch(typeof(IncidentWorker_Raid), nameof(IncidentWorker_Raid.TryGenerateRaidInfo))]
    public static class RaidStrategyWorker_TryGenerateRaidInfo_Patch
    {
        public static void Postfix(IncidentWorker_Raid __instance, IncidentParms parms, ref List<Pawn> pawns, bool debugTest, bool __result)
        {
            if (!__result || debugTest || !(__instance is IncidentWorker_RaidEnemy))
            {
                return;
            }
            if (pawns == null || pawns.Count == 0)
            {
                return;
            }
            if (!(parms.target is Map map))
            {
                return;
            }
            map.GetComponent<RaidInheritanceMapComponent>()?.Notify_RaidGenerated(parms.faction, pawns);
        }
    }
}
