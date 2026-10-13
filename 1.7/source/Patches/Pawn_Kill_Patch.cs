using HarmonyLib;
using Verse;

namespace Raiders_Inherit.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Pawn_Kill_Patch
    {
        public static void Prefix(Pawn __instance)
        {
            __instance.Map?.GetComponent<RaidInheritanceMapComponent>()?.Notify_PotentialColonyDefeat(__instance);
        }
    }
}
