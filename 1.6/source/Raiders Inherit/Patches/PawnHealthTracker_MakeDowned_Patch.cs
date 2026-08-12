using System.Reflection;
using HarmonyLib;
using Verse;

namespace Raiders_Inherit.Patches
{
    [HarmonyPatch]
    public static class PawnHealthTracker_MakeDowned_Patch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn_HealthTracker), "MakeDowned", new[] { typeof(DamageInfo?), typeof(Hediff) });
        }

        public static void Prefix(DamageInfo? dinfo, Pawn ___pawn)
        {
            ___pawn.Map?.GetComponent<RaidInheritanceMapComponent>()?.Notify_PotentialDecisiveDefeat(___pawn, dinfo);
        }
    }
}
