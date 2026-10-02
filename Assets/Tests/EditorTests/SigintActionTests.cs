using System;
using System.Collections.Generic;
using System.Text.Json;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.AI;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Persistence;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    [TestFixture]
    public class SigintActionTests
    {
        private sealed class Rolls : ICombatRandom
        {
            private readonly Queue<int> _values;
            public int Count;
            public Rolls(params int[] values) { _values = new Queue<int>(values); }
            public int RollDie(int sides) { Assert.That(sides, Is.EqualTo(100)); Count++; return _values.Dequeue(); }
        }

        private static CombatUnit Hq(Side side = Side.Player) =>
            new CombatUnit("HQ", UnitClassification.HQ, UnitRole.GroundCombat, side, Nationality.USSR);

        private static CombatUnit Target(Side side = Side.AI, bool emitted = true)
        {
            var u = new CombatUnit("Infantry", UnitClassification.INF, UnitRole.GroundCombat, side, Nationality.USSR,
                deployedProfile: WeaponType.INF_REG_SV, mobileProfile: WeaponType.NONE, embarkedProfile: WeaponType.NONE);
            u.SetSpottedLevel(SpottedLevel.Level0);
            u.SetPosition(new Position2D(1000, 1000)); // Map-wide, no adjacency or spotting range.
            if (emitted) u.MarkMovedThisTurn();
            u.CompleteOwnTurnActivity();
            return u;
        }

        private static Leader IntelLeader(Side side)
        {
            var l = new Leader("Intel Leader", side, Nationality.USSR, CommandAbility.Average);
            l.AwardReputation(5000);
            Assert.That(l.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1), Is.True);
            Assert.That(l.UnlockSkill(IntelligenceDoctrine.EnhancedIntelligenceCollection_ImprovedGathering), Is.True);
            Assert.That(l.IntelActionBonus, Is.EqualTo(1f));
            return l;
        }

        [Test]
        public void Sweep_IndependentD100Rolls_15Succeeds16Fails_NoMpFee()
        {
            var hq = Hq(); hq.MovementPoints.SetCurrent(0);
            var a = Target(); var b = Target(); var rng = new Rolls(15, 16);
            float supply = hq.DaysSupply.Current;
            var result = SigintAction.Execute(hq, new[] { a, b }, rng);
            Assert.That(result.Executed, Is.True); Assert.That(rng.Count, Is.EqualTo(2));
            Assert.That(a.SpottedLevel, Is.EqualTo(SpottedLevel.Level1));
            Assert.That(b.SpottedLevel, Is.EqualTo(SpottedLevel.Level0));
            Assert.That(result.Changes.Count, Is.EqualTo(1)); Assert.That(hq.MovementPoints.Current, Is.Zero);
            Assert.That(hq.DaysSupply.Current, Is.EqualTo(supply - GameData.INTEL_ACTION_SUPPLY_COST));
            Assert.That(hq.IntelActions.Current, Is.EqualTo(1));
            Assert.That(hq.SigintSweepsThisTurn, Is.EqualTo(1));
        }

        [Test]
        public void RadioSilentFriendlyDestroyedTargets_AreNotRolled()
        {
            var silent = Target(emitted: false); var friendly = Target(Side.Player);
            var dead = Target(); dead.HitPoints.SetCurrent(0);
            var active = Target(); var rng = new Rolls(1);
            var result = SigintAction.Execute(Hq(), new[] { silent, friendly, dead, active, null }, rng);
            Assert.That(rng.Count, Is.EqualTo(1)); Assert.That(result.Changes.Count, Is.EqualTo(1));
            Assert.That(silent.SpottedLevel, Is.EqualTo(SpottedLevel.Level0));
        }

        [TestCase(SpottedLevel.Level0, SpottedLevel.Level1)]
        [TestCase(SpottedLevel.Level1, SpottedLevel.Level2)]
        [TestCase(SpottedLevel.Level2, SpottedLevel.Level3)]
        [TestCase(SpottedLevel.Level3, SpottedLevel.Level3)]
        [TestCase(SpottedLevel.Level4, SpottedLevel.Level4)]
        [TestCase(SpottedLevel.Level5, SpottedLevel.Level5)]
        public void Success_IncrementsOnlyToThree_AndNeverLowers(SpottedLevel before, SpottedLevel after)
        {
            var t = Target(); t.SetSpottedLevel(before);
            SigintAction.Execute(Hq(), new[] { t }, new Rolls(1));
            Assert.That(t.SpottedLevel, Is.EqualTo(after));
        }

        [TestCase(Side.Player)]
        [TestCase(Side.AI)]
        public void OrdinaryHq_OnlyOneSweepDespiteTwoActions_RefreshRestoresOnce(Side side)
        {
            var hq = Hq(side); var belief = new AIPerceptionState(); var targets = Array.Empty<CombatUnit>();
            Assert.That(hq.IntelActions.Current, Is.EqualTo(2));
            Assert.That(SigintAction.Execute(hq, targets, new Rolls(), belief).Executed, Is.True);
            float supply = hq.DaysSupply.Current;
            Assert.That(SigintAction.Execute(hq, targets, new Rolls(), belief).Executed, Is.False);
            Assert.That(hq.DaysSupply.Current, Is.EqualTo(supply));
            BattleManager.RefreshUnitForNewTurn(hq);
            Assert.That(hq.SigintSweepsThisTurn, Is.Zero); Assert.That(hq.GetIntelActions(), Is.EqualTo(1));
        }

        [TestCase(Side.Player)]
        [TestCase(Side.AI)]
        public void ExistingLeaderBonus_AddsOneActionAndOneSweep_NotCompoundedAtRefresh(Side side)
        {
            var hq = Hq(side); var leader = IntelLeader(side); var perception = new AIPerceptionState();
            hq.RefreshIntelActions(leader);
            Assert.That(hq.IntelActions.Max, Is.EqualTo(3)); Assert.That(hq.SigintSweepAllowance, Is.EqualTo(2));
            var target = Target(side == Side.Player ? Side.AI : Side.Player);
            for (int i = 0; i < 2; i++) Assert.That(SigintAction.Execute(hq, new[] { target }, new Rolls(1), perception, 7).Executed, Is.True);
            Assert.That(side == Side.Player ? target.SpottedLevel : perception.LevelOf(target.UnitID), Is.EqualTo(SpottedLevel.Level2));
            Assert.That(hq.IntelActions.Current, Is.EqualTo(1));
            Assert.That(SigintAction.Execute(hq, new[] { target }, new Rolls(1), perception).Executed, Is.False);
            hq.RefreshIntelActions(leader); hq.RefreshIntelActions(leader);
            Assert.That(hq.IntelActions.Max, Is.EqualTo(3)); Assert.That(hq.GetIntelActions(), Is.EqualTo(2));
            hq.RefreshIntelActions(null);
            Assert.That(hq.IntelActions.Max, Is.EqualTo(2)); Assert.That(hq.GetIntelActions(), Is.EqualTo(1));
        }

        [TestCase(0, 2)] [TestCase(21, 2)] [TestCase(41, 1)] [TestCase(61, 1)] [TestCase(81, 0)]
        public void OperationalCapacity_RestrictsRawActionsAndSweepEligibility(int damage, int capacity)
        {
            var hq = Hq(); hq.SetFacilityDamage(damage); hq.RefreshAllActions();
            Assert.That(hq.IntelActions.Current, Is.EqualTo(capacity));
            Assert.That(hq.GetIntelOperationalCapacity(), Is.EqualTo(capacity));
            Assert.That(hq.GetIntelActions(), Is.EqualTo(Math.Min(1, capacity)));
            Assert.That(SigintAction.Execute(hq, Array.Empty<CombatUnit>(), new Rolls()).Executed, Is.EqualTo(capacity > 0));
        }

        [TestCase(Side.Player)] [TestCase(Side.AI)]
        public void LeaderSweep_DoesNotBypassDamagedHqActionCapacity(Side side)
        {
            var hq = Hq(side); hq.SetFacilityDamage(41); hq.RefreshIntelActions(IntelLeader(side));
            Assert.That(hq.SigintSweepAllowance, Is.EqualTo(2)); Assert.That(hq.IntelActions.Current, Is.EqualTo(1));
            var belief = new AIPerceptionState();
            Assert.That(SigintAction.Execute(hq, Array.Empty<CombatUnit>(), new Rolls(), belief).Executed, Is.True);
            Assert.That(SigintAction.Execute(hq, Array.Empty<CombatUnit>(), new Rolls(), belief).Executed, Is.False);
        }

        [Test]
        public void DamageAfterFirstLeaderSweep_DoesNotGrantAnotherActionAtReducedCapacity()
        {
            var hq = Hq(); hq.RefreshIntelActions(IntelLeader(Side.Player));
            Assert.That(SigintAction.Execute(hq, Array.Empty<CombatUnit>(), new Rolls()).Executed, Is.True);
            hq.SetFacilityDamage(41);
            Assert.That(hq.GetIntelActions(), Is.Zero);
            Assert.That(SigintAction.Execute(hq, Array.Empty<CombatUnit>(), new Rolls()).Executed, Is.False);
        }

        [Test]
        public void InvalidContextSupplyDestroyedOrNonHq_NeverPayOrRoll()
        {
            var hq = Hq(); var t = Target(); var rng = new Rolls(); float supply = hq.DaysSupply.Current;
            Assert.That(SigintAction.Execute(hq, null, rng).Executed, Is.False);
            Assert.That(hq.DaysSupply.Current, Is.EqualTo(supply));
            hq.DaysSupply.SetCurrent(0); Assert.That(SigintAction.Execute(hq, new[] { t }, rng).Executed, Is.False);
            hq.DaysSupply.SetCurrent(supply); hq.HitPoints.SetCurrent(0);
            Assert.That(SigintAction.Execute(hq, new[] { t }, rng).Executed, Is.False);
            Assert.That(SigintAction.Execute(t, new[] { hq }, rng, new AIPerceptionState()).Executed, Is.False);
            var aiHq = Hq(Side.AI); Assert.That(SigintAction.Execute(aiHq, new[] { t }, rng).Executed, Is.False);
            Assert.That(rng.Count, Is.Zero); Assert.That(aiHq.SigintSweepsThisTurn, Is.Zero);
        }

        [Test]
        public void AiSuccess_UsesBeliefLedger_WithoutChangingPlayerSpottedLevel()
        {
            var p = Target(Side.Player); var belief = new AIPerceptionState();
            SigintAction.Execute(Hq(Side.AI), new[] { p }, new Rolls(15), belief, 7);
            Assert.That(p.SpottedLevel, Is.EqualTo(SpottedLevel.Level0));
            Assert.That(belief.LevelOf(p.UnitID), Is.EqualTo(SpottedLevel.Level1));
            Assert.That(belief.GetContact(p.UnitID).LastKnownPos, Is.EqualTo(p.MapPos));
            Assert.That(belief.GetContact(p.UnitID).LastSeenTurn, Is.EqualTo(7));
        }

        [Test]
        public void MultipleHqs_IndependentSweepsAccumulateOnlyToThree()
        {
            var t = Target();
            for (int i = 0; i < 5; i++) SigintAction.Execute(Hq(), new[] { t }, new Rolls(1));
            Assert.That(t.SpottedLevel, Is.EqualTo(SpottedLevel.Level3));
        }

        [Test]
        public void OwnTurnHistory_InitialSilence_MovementFireAndResupply_EnemyReactionDoesNotBackdate()
        {
            var u = Target(emitted: false); Assert.That(u.EmittedDuringLastOwnTurn, Is.False);
            u.MarkMovedThisTurn(); Assert.That(u.EmittedDuringLastOwnTurn, Is.False);
            u.CompleteOwnTurnActivity(); Assert.That(u.EmittedDuringLastOwnTurn, Is.True);
            BattleManager.RefreshUnitForNewTurn(u); Assert.That(u.HasEmittedThisTurn, Is.False);
            u.MarkFoughtThisTurn(); u.CompleteOwnTurnActivity(); // Being attacked alone is not firing.
            Assert.That(u.EmittedDuringLastOwnTurn, Is.False);
            u.MarkFiredThisTurn(); Assert.That(u.EmittedDuringLastOwnTurn, Is.False, "enemy-turn reaction cannot rewrite completed history");
            BattleManager.RefreshUnitForNewTurn(u); u.CompleteOwnTurnActivity();
            Assert.That(u.EmittedDuringLastOwnTurn, Is.False);
            u.MarkFiredThisTurn(); u.CompleteOwnTurnActivity(); Assert.That(u.EmittedDuringLastOwnTurn, Is.True);
            BattleManager.RefreshUnitForNewTurn(u); u.MarkResuppliedThisTurn(); u.CompleteOwnTurnActivity();
            Assert.That(u.EmittedDuringLastOwnTurn, Is.True);
        }

        [Test]
        public void Resupply_OnlyActualDeliveryEmits_ConsumptionDoesNot()
        {
            var u = Target(emitted: false); Assert.That(u.ReceiveSupplies(1), Is.Zero);
            Assert.That(u.HasEmittedThisTurn, Is.False);
            u.ConsumeSupplies(1); Assert.That(u.HasEmittedThisTurn, Is.False);
            Assert.That(u.ReceiveSupplies(0), Is.Zero); Assert.That(u.HasEmittedThisTurn, Is.False);
            Assert.That(u.ReceiveSupplies(0.5f), Is.EqualTo(0.5f)); Assert.That(u.HasEmittedThisTurn, Is.True);
        }

        [Test]
        public void SaveRoundTrip_PreservesHistoryAndConsumedLeaderAllowance()
        {
            var hq = Hq(); hq.RefreshIntelActions(IntelLeader(Side.Player));
            hq.MarkMovedThisTurn(); hq.CompleteOwnTurnActivity();
            SigintAction.Execute(hq, Array.Empty<CombatUnit>(), new Rolls());
            string json = JsonSerializer.Serialize(hq, JsonPolicy.Save);
            var copy = JsonSerializer.Deserialize<CombatUnit>(json, JsonPolicy.Save);
            Assert.That(copy.HasEmittedThisTurn, Is.True); Assert.That(copy.EmittedDuringLastOwnTurn, Is.True);
            Assert.That(copy.SigintSweepAllowance, Is.EqualTo(2)); Assert.That(copy.SigintSweepsThisTurn, Is.EqualTo(1));
            Assert.That(copy.GetIntelActions(), Is.EqualTo(1));
        }

        [Test]
        public void LowMpAirBoarding_IsAvailableUntilHalt_WithNoGroundTransitionFee()
        {
            var u = new CombatUnit("AM", UnitClassification.MAM, UnitRole.GroundCombat, Side.Player, Nationality.USSR,
                deployedProfile: WeaponType.INF_AM_SV, mobileProfile: WeaponType.APC_MTLB_SV, embarkedProfile: WeaponType.HEL_MI8T_SV);
            u.MovementPoints.SetCurrent(0.25f);
            Assert.That(u.GetDeployActions(), Is.EqualTo(1)); Assert.That(u.TryAirEmbark(out _), Is.True);
            u.SetDeploymentPosition(DeploymentPosition.Deployed); u.DeploymentActions.SetCurrent(1);
            u.MovementPoints.SetMax(3); u.MovementPoints.SetCurrent(0.25f); u.MarkMovementHalted();
            Assert.That(u.GetDeployActions(), Is.Zero); Assert.That(u.TryAirEmbark(out _), Is.False);
        }
    }
}
