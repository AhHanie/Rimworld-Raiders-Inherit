using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Raiders_Inherit
{
    public class RaidInheritanceMapComponent : MapComponent
    {
        private List<RaidRecord> records = new List<RaidRecord>();

        public RaidInheritanceMapComponent(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref records, "records", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && records == null)
            {
                records = new List<RaidRecord>();
            }
        }

        public void Notify_RaidGenerated(Faction faction, List<Pawn> pawns)
        {
            if (faction == null || pawns == null || pawns.Count == 0)
            {
                return;
            }
            if (!Rand.Chance(Mathf.Clamp01(ModSettings.inheritanceChance)))
            {
                return;
            }
            RaidRecord record = new RaidRecord
            {
                faction = faction,
                raidPawns = new List<Pawn>(pawns)
            };
            records.Add(record);
        }

        public void Notify_PotentialColonyDefeat(Pawn victim)
        {
            if (victim == null || !victim.RaceProps.Humanlike || !victim.IsColonist)
            {
                return;
            }
            if (!NoViableColonistRemains(victim))
            {
                return;
            }
            RaidRecord record = FindMostRecentOngoingRaid();
            if (record == null)
            {
                return;
            }
            record.pending = true;
        }

        private RaidRecord FindMostRecentOngoingRaid()
        {
            for (int i = records.Count - 1; i >= 0; i--)
            {
                RaidRecord record = records[i];
                if (!record.pending && !IsRaidOver(record))
                {
                    return record;
                }
            }
            return null;
        }

        public bool HasPendingInheritance()
        {
            return records.Any((RaidRecord r) => r.pending);
        }

        public override void MapComponentTick()
        {
            if (records.Count == 0)
            {
                return;
            }
            for (int i = records.Count - 1; i >= 0; i--)
            {
                RaidRecord record = records[i];
                if (record.pending)
                {
                    ProcessPending(record);
                }
            }
            if (!map.IsHashIntervalTick(GenTicks.TickRareInterval))
            {
                return;
            }
            for (int i = records.Count - 1; i >= 0; i--)
            {
                RaidRecord record = records[i];
                if (!record.pending && IsRaidOver(record))
                {
                    records.RemoveAt(i);
                }
            }
        }

        private void ProcessPending(RaidRecord record)
        {
            if (!NoViableColonistRemains(null))
            {
                record.pending = false;
                return;
            }
            if (AnyPawnStillArriving(record))
            {
                return;
            }
            List<Pawn> livePawns = GetLiveSpawnedPawns(record);
            if (livePawns.Count == 0)
            {
                records.Remove(record);
                return;
            }
            PerformHandoff(record, livePawns);
        }

        private static bool IsRaidOver(RaidRecord record)
        {
            foreach (Pawn pawn in record.raidPawns)
            {
                if (pawn == null || pawn.Destroyed || pawn.Dead)
                {
                    continue;
                }
                if (!pawn.Spawned && pawn.IsWorldPawn())
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        private static bool AnyPawnStillArriving(RaidRecord record)
        {
            foreach (Pawn pawn in record.raidPawns)
            {
                if (pawn == null || pawn.Destroyed || pawn.Dead)
                {
                    continue;
                }
                if (!pawn.Spawned && !pawn.IsWorldPawn())
                {
                    return true;
                }
            }
            return false;
        }

        private void PerformHandoff(RaidRecord record, List<Pawn> livePawns)
        {
            int joinCount = ModSettings.inheritEntireRaid ? livePawns.Count : System.Math.Min(ModSettings.raidersToInherit, livePawns.Count);
            List<Pawn> shuffled = livePawns.InRandomOrder().ToList();
            List<Pawn> joiners = shuffled.Take(joinCount).ToList();
            List<Pawn> allies = shuffled.Skip(joinCount).ToList();
            foreach (Pawn pawn in joiners)
            {
                pawn.SetFaction(Faction.OfPlayer);
            }
            if (allies.Count > 0)
            {
                foreach (Pawn pawn in allies)
                {
                    pawn.SetFaction(null);
                }
                LordMaker.MakeNewLord(record.faction, new LordJob_ExitMapBest(LocomotionUrgency.Jog, canDig: false, canDefendSelf: true), map, allies);
            }
            Find.GameEnder.CheckOrUpdateGameOver();
            if (!Find.GameEnder.gameEnding)
            {
                Letter gameEndedLetter = Find.LetterStack.LettersListForReading.Find((Letter l) => l.def == LetterDefOf.GameEnded);
                if (gameEndedLetter != null)
                {
                    Find.LetterStack.RemoveLetter(gameEndedLetter);
                }
            }
            SendJoinedLetter(record, joiners, allies);
            OpenColonyNamingDialog();
            records.Remove(record);
        }

        private void OpenColonyNamingDialog()
        {
            if (map.Parent is Settlement settlement && settlement.Faction == Faction.OfPlayer)
            {
                Find.WindowStack.Add(new Dialog_NamePlayerSettlement(settlement));
            }
        }

        private static void SendJoinedLetter(RaidRecord record, List<Pawn> joiners, List<Pawn> allies)
        {
            TaggedString label = "RaidersInherit.JoinedLetterLabel".Translate();
            TaggedString joinerNames = joiners.Select((Pawn p) => p.LabelShort).ToCommaList(useAnd: true);
            TaggedString factionName = record.faction?.Name ?? "RaidersInherit.UnknownFaction".Translate();
            string textKey = allies.Count == 0 ? "RaidersInherit.JoinedLetterTextAllJoined" : "RaidersInherit.JoinedLetterText";
            TaggedString text = textKey.Translate(joiners.Count, factionName, joinerNames, allies.Count);
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new LookTargets(joiners));
        }

        private List<Pawn> GetLiveSpawnedPawns(RaidRecord record)
        {
            List<Pawn> result = new List<Pawn>();
            foreach (Pawn pawn in record.raidPawns)
            {
                if (pawn != null && pawn.Spawned && !pawn.Dead)
                {
                    result.Add(pawn);
                }
            }
            return result;
        }

        private static bool NoViableColonistRemains(Pawn p)
        {
            foreach (Pawn item in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
            {
                if (item.RaceProps.Humanlike && !item.IsPrisoner && ((item != p && !item.Downed) || (ModsConfig.BiotechActive && item.Deathresting && !SanguophageUtility.ShouldBeDeathrestingOrInComaInsteadOfDead(item))))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
