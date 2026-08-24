using RimWorld;
using Verse;

namespace Raiders_Inherit
{
    [DefOf]
    public static class RaidersInheritJobDefOf
    {
        public static JobDef RaidersInherit_ExecuteDowned;

        static RaidersInheritJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RaidersInheritJobDefOf));
        }
    }
}
