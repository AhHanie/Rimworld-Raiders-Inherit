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
            record.finishDownedColonistsBeforeHandoff = ModSettings.finishDownedColonistsBeforeTakeover;
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
                record.finishDownedColonistsBeforeHandoff = false;
                return;
            }
            if (record.finishDownedColonistsBeforeHandoff)
            {
                List<Pawn> executionTargets = GetDownedColonistsToExecute();
                if (executionTargets.Count > 0)
                {
                    AdvanceExecutionPhase(record, executionTargets);
                    return;
                }
                record.finishDownedColonistsBeforeHandoff = false;
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

        private void AdvanceExecutionPhase(RaidRecord record, List<Pawn> targets)
        {
            if (GetLiveSpawnedPawns(record).Count == 0 && !AnyPawnStillArriving(record))
            {
                records.Remove(record);
                return;
            }
            AssignExecutionJobs(record, targets);
            bool anyExecuting = GetConsciousSpawnedRaiders(record).Any((Pawn r) => IsExecutingDownedColonist(r, targets));
            if (!anyExecuting)
            {
                WarnWaitingOnUnreachableTargets(record);
            }
            else
            {
                record.warnedUnreachableExecution = false;
            }
        }

        private static void WarnWaitingOnUnreachableTargets(RaidRecord record)
        {
            if (record.warnedUnreachableExecution)
            {
                return;
            }
            record.warnedUnreachableExecution = true;
            Logger.Warning("Raid from " + (record.faction?.Name ?? "an unknown faction") + " is waiting to hand off because no conscious raider can currently reach a downed colonist to execute.");
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
            List<Pawn> pawnsToEnslave = GetDownedColonistsToEnslave();
            int joinCount = ModSettings.inheritEntireRaid ? livePawns.Count : System.Math.Min(ModSettings.raidersToInherit, livePawns.Count);
            List<Pawn> shuffled = livePawns.InRandomOrder().ToList();
            List<Pawn> joiners = shuffled.Take(joinCount).ToList();
            List<Pawn> allies = shuffled.Skip(joinCount).ToList();
            foreach (Pawn pawn in joiners)
            {
                pawn.SetFaction(Faction.OfPlayer);
            }
            List<Pawn> enslaved = EnslaveDownedColonists(pawnsToEnslave);
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
            SendJoinedLetter(record, joiners, allies, enslaved);
            OpenColonyNamingDialog();
            records.Remove(record);
        }

        private List<Pawn> GetDownedColonistsToEnslave()
        {
            if (!ModSettings.enslaveDownedColonistsOnTakeover || !ModsConfig.IdeologyActive)
            {
                return new List<Pawn>();
            }
            return GetEligibleDownedFormerColonists();
        }

        private List<Pawn> GetDownedColonistsToExecute()
        {
            return GetEligibleDownedFormerColonists();
        }

        private List<Pawn> GetEligibleDownedFormerColonists()
        {
            List<Pawn> result = new List<Pawn>();
            foreach (Pawn pawn in map.mapPawns.AllPawns)
            {
                if (IsEligibleDownedFormerColonist(pawn))
                {
                    result.Add(pawn);
                }
            }
            return result;
        }

        private static bool IsEligibleDownedFormerColonist(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && !pawn.Dead && pawn.Downed && pawn.RaceProps.Humanlike && pawn.IsFreeNonSlaveColonist;
        }

        private static List<Pawn> GetConsciousSpawnedRaiders(RaidRecord record)
        {
            List<Pawn> result = new List<Pawn>();
            foreach (Pawn pawn in record.raidPawns)
            {
                if (pawn != null && pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.Faction == record.faction)
                {
                    result.Add(pawn);
                }
            }
            return result;
        }

        private static bool IsExecutingDownedColonist(Pawn raider, List<Pawn> targets)
        {
            Job job = raider.CurJob;
            if (job == null || job.def != RaidersInheritJobDefOf.RaidersInherit_ExecuteDowned || !job.killIncappedTarget)
            {
                return false;
            }
            Pawn target = job.GetTarget(TargetIndex.A).Thing as Pawn;
            return target != null && targets.Contains(target);
        }

        private void AssignExecutionJobs(RaidRecord record, List<Pawn> targets)
        {
            foreach (Pawn raider in GetConsciousSpawnedRaiders(record))
            {
                if (IsExecutingDownedColonist(raider, targets))
                {
                    continue;
                }
                Pawn target = FindExecutionTarget(raider, targets);
                if (target == null)
                {
                    continue;
                }
                Job job = JobMaker.MakeJob(RaidersInheritJobDefOf.RaidersInherit_ExecuteDowned, target);
                job.killIncappedTarget = true;
                job.attackDoorIfTargetLost = true;
                raider.jobs.StartJob(job, JobCondition.InterruptForced);
            }
        }

        private static Pawn FindExecutionTarget(Pawn raider, List<Pawn> targets)
        {
            Pawn best = null;
            int bestDistSq = int.MaxValue;
            foreach (Pawn target in targets)
            {
                if (raider.meleeVerbs?.TryGetMeleeVerb(target) == null)
                {
                    continue;
                }
                if (!raider.Map.attackTargetReservationManager.CanReserve(raider, target))
                {
                    continue;
                }
                if (!raider.CanReach(target, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }
                int distSq = (target.Position - raider.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = target;
                }
            }
            return best;
        }

        private static List<Pawn> EnslaveDownedColonists(List<Pawn> pawnsToEnslave)
        {
            List<Pawn> enslaved = new List<Pawn>();
            foreach (Pawn pawn in pawnsToEnslave)
            {
                if (pawn == null || pawn.Dead || !pawn.Downed || !pawn.IsFreeNonSlaveColonist)
                {
                    continue;
                }
                pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
                enslaved.Add(pawn);
            }
            return enslaved;
        }

        private void OpenColonyNamingDialog()
        {
            if (map.Parent is Settlement settlement && settlement.Faction == Faction.OfPlayer)
            {
                Find.WindowStack.Add(new Dialog_NamePlayerSettlement(settlement));
            }
        }

        private static void SendJoinedLetter(RaidRecord record, List<Pawn> joiners, List<Pawn> allies, List<Pawn> enslaved)
        {
            TaggedString label = "RaidersInherit.JoinedLetterLabel".Translate();
            TaggedString joinerNames = joiners.Select((Pawn p) => p.LabelShort).ToCommaList(useAnd: true);
            TaggedString factionName = record.faction?.Name ?? "RaidersInherit.UnknownFaction".Translate();
            string textKey = allies.Count == 0 ? "RaidersInherit.JoinedLetterTextAllJoined" : "RaidersInherit.JoinedLetterText";
            TaggedString text = textKey.Translate(joiners.Count, factionName, joinerNames, allies.Count);
            if (enslaved.Count > 0)
            {
                TaggedString enslavedNames = enslaved.Select((Pawn p) => p.LabelShort).ToCommaList(useAnd: true);
                text += "\n\n" + "RaidersInherit.EnslavedLetterAddendum".Translate(enslaved.Count, enslavedNames);
            }
            List<Pawn> lookTargets = joiners.Concat(enslaved).ToList();
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new LookTargets(lookTargets));
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
