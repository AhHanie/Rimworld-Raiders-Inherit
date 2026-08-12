using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Raiders_Inherit
{
    public class RaidRecord : IExposable
    {
        public Faction faction;
        public List<Pawn> raidPawns = new List<Pawn>();
        public bool pending;

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Collections.Look(ref raidPawns, "raidPawns", LookMode.Reference);
            Scribe_Values.Look(ref pending, "pending", defaultValue: false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && raidPawns == null)
            {
                raidPawns = new List<Pawn>();
            }
        }
    }
}
