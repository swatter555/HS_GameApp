using System;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Persistence;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    // Requires the native fixture lifecycle/manager; compile-only when no Unity runner is permitted.
    [TestFixture]
    public class SigintIntegrationTests : BaseTestFixture
    {
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
        }

        public override void SetUp() { base.SetUp(); GameManager.ClearAll(); }

        private CombatUnit Unit(UnitClassification cls, WeaponType profile, Side side = Side.Player) =>
            new CombatUnit("Unit", cls, UnitRole.GroundCombat, side, Nationality.USSR,
                deployedProfile: profile, mobileProfile: WeaponType.NONE, embarkedProfile: WeaponType.NONE);

        [TestCase(Side.Player)] [TestCase(Side.AI)]
        public void AssignedLeader_PublicRefreshAddsTwoSweepAllowance_AndSnapshotPreservesSpend(Side side)
        {
            var hq = Unit(UnitClassification.HQ, WeaponType.NONE, side);
            var leader = new Leader("Intel Leader", side, Nationality.USSR, CommandAbility.Average);
            leader.AwardReputation(5000);
            Assert.That(leader.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1), Is.True);
            Assert.That(leader.UnlockSkill(IntelligenceDoctrine.EnhancedIntelligenceCollection_ImprovedGathering), Is.True);
            Assert.That(GameManager.RegisterCombatUnit(hq), Is.True);
            Assert.That(GameManager.RegisterLeader(leader), Is.True);
            Assert.That(GameManager.AssignLeaderToUnit(leader.LeaderID, hq.UnitID), Is.True);
            BattleManager.RefreshUnitForNewTurn(hq);
            Assert.That(hq.IntelActions.Max, Is.EqualTo(3)); Assert.That(hq.GetIntelActions(), Is.EqualTo(2));
            hq.MarkFiredThisTurn(); hq.CompleteOwnTurnActivity();
            Assert.That(hq.PerformIntelAction(), Is.True);
            var copy = SnapshotMapper.ToSnapshot(GameManager).Units[hq.UnitID];
            Assert.That(copy.HasEmittedThisTurn, Is.True); Assert.That(copy.EmittedDuringLastOwnTurn, Is.True);
            Assert.That(copy.SigintSweepsThisTurn, Is.EqualTo(1)); Assert.That(copy.SigintSweepAllowance, Is.EqualTo(2));
            Assert.That(copy.GetIntelActions(), Is.EqualTo(1));
            Assert.That(hq.PerformIntelAction(), Is.True); Assert.That(hq.PerformIntelAction(), Is.False);
            BattleManager.RefreshUnitForNewTurn(hq);
            Assert.That(hq.GetIntelActions(), Is.EqualTo(2)); Assert.That(hq.HasEmittedThisTurn, Is.False);
        }

        [Test]
        public void DirectExchange_BothActualFirersEmit()
        {
            var a = Unit(UnitClassification.INF, WeaponType.INF_REG_SV);
            var b = Unit(UnitClassification.INF, WeaponType.INF_REG_SV, Side.AI);
            CombatResolver.ResolveDirectAttack(a, b, new DirectAttackContext { DefenderTerrain = TerrainType.Clear }, new FixedRollRandom(1));
            Assert.That(a.HasEmittedThisTurn, Is.True); Assert.That(b.HasEmittedThisTurn, Is.True);
        }

        [Test]
        public void Ambush_OnlyShooterEmits()
        {
            var a = Unit(UnitClassification.INF, WeaponType.INF_REG_SV);
            var b = Unit(UnitClassification.INF, WeaponType.INF_REG_SV, Side.AI);
            CombatResolver.ResolveAmbush(a, b, new DirectAttackContext { DefenderTerrain = TerrainType.Clear }, new FixedRollRandom(1));
            Assert.That(a.HasEmittedThisTurn, Is.True); Assert.That(b.HasEmittedThisTurn, Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void IndirectExchange_OnlyAnActualCounterBatteryTargetEmits(bool artilleryTarget)
        {
            var a = Unit(UnitClassification.SPA, WeaponType.SPA_2S1_SV);
            var b = Unit(artilleryTarget ? UnitClassification.SPA : UnitClassification.INF,
                artilleryTarget ? WeaponType.SPA_2S1_SV : WeaponType.INF_REG_SV, Side.AI);
            a.SetPosition(new Position2D(3, 6)); b.SetPosition(new Position2D(5, 6));
            var result = CombatResolver.ResolveIndirectAttack(a, b,
                new IndirectAttackContext { TargetTerrain = TerrainType.Clear, FirerTerrain = TerrainType.Clear }, new FixedRollRandom(1));
            Assert.That(result.CounterBatteryFired, Is.EqualTo(artilleryTarget));
            Assert.That(a.HasEmittedThisTurn, Is.True); Assert.That(b.HasEmittedThisTurn, Is.EqualTo(artilleryTarget));
        }

        [Test]
        public void RefusedResolverInput_DoesNotMarkEmissions()
        {
            var a = Unit(UnitClassification.INF, WeaponType.INF_REG_SV);
            CombatResolver.ResolveDirectAttack(a, null, new DirectAttackContext(), new FixedRollRandom(1));
            Assert.That(a.HasEmittedThisTurn, Is.False);
        }
    }
}
