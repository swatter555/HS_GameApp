using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M3 validation: efficiency & supply degradation (§7.15) — the per-tier 1d100 threshold tables, the
    /// roll resolvers, the floored tier drops / capped recovery, and the Upkeep recovery rule. Pure math.
    /// </summary>
    [TestFixture]
    public class DegradationTests
    {
        #region Threshold tables (§7.15.2–§7.15.5)

        [Test]
        public void MoveEfficiencyThreshold_MatchesTable()
        {
            Assert.AreEqual(20, DegradationCheck.MoveEfficiencyThreshold(ExperienceLevel.Raw),         "Raw 20");
            Assert.AreEqual(20, DegradationCheck.MoveEfficiencyThreshold(ExperienceLevel.Green),       "Green 20");
            Assert.AreEqual(18, DegradationCheck.MoveEfficiencyThreshold(ExperienceLevel.Trained),     "Trained 18");
            Assert.AreEqual(15, DegradationCheck.MoveEfficiencyThreshold(ExperienceLevel.Experienced), "Experienced 15");
            Assert.AreEqual(12, DegradationCheck.MoveEfficiencyThreshold(ExperienceLevel.Veteran),     "Veteran 12");
            Assert.AreEqual(10, DegradationCheck.MoveEfficiencyThreshold(ExperienceLevel.Elite),       "Elite 10");
        }

        [Test]
        public void CombatEfficiencyThreshold_MatchesTable()
        {
            Assert.AreEqual(50, DegradationCheck.CombatEfficiencyThreshold(ExperienceLevel.Raw),         "Raw 50");
            Assert.AreEqual(50, DegradationCheck.CombatEfficiencyThreshold(ExperienceLevel.Green),       "Green 50");
            Assert.AreEqual(48, DegradationCheck.CombatEfficiencyThreshold(ExperienceLevel.Trained),     "Trained 48");
            Assert.AreEqual(45, DegradationCheck.CombatEfficiencyThreshold(ExperienceLevel.Experienced), "Experienced 45");
            Assert.AreEqual(40, DegradationCheck.CombatEfficiencyThreshold(ExperienceLevel.Veteran),     "Veteran 40");
            Assert.AreEqual(35, DegradationCheck.CombatEfficiencyThreshold(ExperienceLevel.Elite),       "Elite 35");
        }

        [Test]
        public void MoveSupplyThreshold_MirrorsMoveEfficiencyTable()
        {
            Assert.AreEqual(20, DegradationCheck.MoveSupplyThreshold(ExperienceLevel.Raw),     "Raw 20");
            Assert.AreEqual(18, DegradationCheck.MoveSupplyThreshold(ExperienceLevel.Trained), "Trained 18");
            Assert.AreEqual(10, DegradationCheck.MoveSupplyThreshold(ExperienceLevel.Elite),   "Elite 10");
        }

        [Test]
        public void CombatSupplyThreshold_MatchesTable()
        {
            Assert.AreEqual(60, DegradationCheck.CombatSupplyThreshold(ExperienceLevel.Raw),         "Raw 60");
            Assert.AreEqual(60, DegradationCheck.CombatSupplyThreshold(ExperienceLevel.Green),       "Green 60");
            Assert.AreEqual(58, DegradationCheck.CombatSupplyThreshold(ExperienceLevel.Trained),     "Trained 58");
            Assert.AreEqual(55, DegradationCheck.CombatSupplyThreshold(ExperienceLevel.Experienced), "Experienced 55");
            Assert.AreEqual(50, DegradationCheck.CombatSupplyThreshold(ExperienceLevel.Veteran),     "Veteran 50");
            Assert.AreEqual(45, DegradationCheck.CombatSupplyThreshold(ExperienceLevel.Elite),       "Elite 45");
        }

        #endregion // Threshold tables

        #region Roll resolvers (1d100 ≤ threshold)

        [Test]
        public void RollResolvers_TriggerAtOrBelowThreshold()
        {
            // Move EL, Trained = 18.
            Assert.IsTrue(DegradationCheck.RollMoveEfficiencyLoss(ExperienceLevel.Trained, new QueueRollRandom(18)),  "18 ≤ 18 → loss");
            Assert.IsFalse(DegradationCheck.RollMoveEfficiencyLoss(ExperienceLevel.Trained, new QueueRollRandom(19)), "19 > 18 → no loss");
            // Combat EL, Elite = 35.
            Assert.IsTrue(DegradationCheck.RollCombatEfficiencyLoss(ExperienceLevel.Elite, new QueueRollRandom(35)),  "35 ≤ 35 → loss");
            Assert.IsFalse(DegradationCheck.RollCombatEfficiencyLoss(ExperienceLevel.Elite, new QueueRollRandom(36)), "36 > 35 → no loss");
            // Move supply, Veteran = 12.
            Assert.IsTrue(DegradationCheck.RollMoveSupplyLoss(ExperienceLevel.Veteran, new QueueRollRandom(12)),  "12 ≤ 12 → loss");
            Assert.IsFalse(DegradationCheck.RollMoveSupplyLoss(ExperienceLevel.Veteran, new QueueRollRandom(13)), "13 > 12 → no loss");
            // Combat supply, Raw = 60.
            Assert.IsTrue(DegradationCheck.RollCombatSupplyLoss(ExperienceLevel.Raw, new QueueRollRandom(60)),  "60 ≤ 60 → loss");
            Assert.IsFalse(DegradationCheck.RollCombatSupplyLoss(ExperienceLevel.Raw, new QueueRollRandom(61)), "61 > 60 → no loss");
        }

        [Test]
        public void RollCounterBatterySupplyLoss_FlatFiftyPercent()
        {
            Assert.IsTrue(DegradationCheck.RollCounterBatterySupplyLoss(new QueueRollRandom(50)),  "50 ≤ 50 → loss");
            Assert.IsFalse(DegradationCheck.RollCounterBatterySupplyLoss(new QueueRollRandom(51)), "51 > 50 → no loss");
        }

        #endregion // Roll resolvers

        #region Tier transitions (§7.15.2.3 / §7.15.3.3 / §7.15.8.4)

        [Test]
        public void DropOneTier_StepsDownButHonorsFloor()
        {
            Assert.AreEqual(EfficiencyLevel.CombatOperations,   DegradationCheck.DropOneTier(EfficiencyLevel.FullOperations, DegradationCheck.COMBAT_EFFICIENCY_FLOOR),     "Full → Combat");
            Assert.AreEqual(EfficiencyLevel.DegradedOperations, DegradationCheck.DropOneTier(EfficiencyLevel.NormalOperations, DegradationCheck.MOVE_EFFICIENCY_FLOOR),     "Normal → Degraded");
            Assert.AreEqual(EfficiencyLevel.DegradedOperations, DegradationCheck.DropOneTier(EfficiencyLevel.DegradedOperations, DegradationCheck.MOVE_EFFICIENCY_FLOOR),   "Degraded holds at move floor");
            Assert.AreEqual(EfficiencyLevel.StaticOperations,   DegradationCheck.DropOneTier(EfficiencyLevel.DegradedOperations, DegradationCheck.COMBAT_EFFICIENCY_FLOOR), "Degraded → Static (combat floor)");
            Assert.AreEqual(EfficiencyLevel.StaticOperations,   DegradationCheck.DropOneTier(EfficiencyLevel.StaticOperations, DegradationCheck.COMBAT_EFFICIENCY_FLOOR),   "Static holds at Static");
            Assert.AreEqual(EfficiencyLevel.StaticOperations,   DegradationCheck.DropOneTier(EfficiencyLevel.StaticOperations, DegradationCheck.MOVE_EFFICIENCY_FLOOR),     "Already-Static unit unchanged by move floor");
        }

        [Test]
        public void Recover_RaisesTiersCappedAtFull()
        {
            Assert.AreEqual(EfficiencyLevel.NormalOperations, DegradationCheck.Recover(EfficiencyLevel.StaticOperations, 2),   "Static +2 → Normal");
            Assert.AreEqual(EfficiencyLevel.NormalOperations, DegradationCheck.Recover(EfficiencyLevel.DegradedOperations, 1), "Degraded +1 → Normal");
            Assert.AreEqual(EfficiencyLevel.FullOperations,   DegradationCheck.Recover(EfficiencyLevel.NormalOperations, 2),   "Normal +2 → Full");
            Assert.AreEqual(EfficiencyLevel.FullOperations,   DegradationCheck.Recover(EfficiencyLevel.CombatOperations, 2),   "Combat +2 → Full (cap)");
            Assert.AreEqual(EfficiencyLevel.FullOperations,   DegradationCheck.Recover(EfficiencyLevel.FullOperations, 2),     "Full +2 → Full");
            Assert.AreEqual(EfficiencyLevel.NormalOperations, DegradationCheck.Recover(EfficiencyLevel.NormalOperations, 0),   "+0 → unchanged");
        }

        #endregion // Tier transitions

        #region Combined appliers

        [Test]
        public void ApplyMoveEfficiencyLoss_DropsOnHit_HonorsDegradedFloor()
        {
            Assert.AreEqual(EfficiencyLevel.CombatOperations,
                DegradationCheck.ApplyMoveEfficiencyLoss(EfficiencyLevel.FullOperations, ExperienceLevel.Trained, new QueueRollRandom(18)),
                "Full, roll 18 ≤ 18 → Combat");
            Assert.AreEqual(EfficiencyLevel.FullOperations,
                DegradationCheck.ApplyMoveEfficiencyLoss(EfficiencyLevel.FullOperations, ExperienceLevel.Trained, new QueueRollRandom(19)),
                "Full, roll 19 > 18 → unchanged");
            Assert.AreEqual(EfficiencyLevel.DegradedOperations,
                DegradationCheck.ApplyMoveEfficiencyLoss(EfficiencyLevel.DegradedOperations, ExperienceLevel.Trained, new QueueRollRandom(1)),
                "Degraded, hit → stays Degraded (movement floor)");
        }

        [Test]
        public void ApplyCombatEfficiencyLoss_CanReachStatic()
        {
            Assert.AreEqual(EfficiencyLevel.StaticOperations,
                DegradationCheck.ApplyCombatEfficiencyLoss(EfficiencyLevel.DegradedOperations, ExperienceLevel.Trained, new QueueRollRandom(1)),
                "Degraded, combat hit → Static");
        }

        #endregion // Combined appliers

        #region Upkeep recovery (§7.15.8)

        [Test]
        public void RecoveryTiers_RestTwoMoveOneFoughtZero()
        {
            Assert.AreEqual(2, DegradationCheck.RecoveryTiers(moved: false, fought: false), "rested → +2");
            Assert.AreEqual(1, DegradationCheck.RecoveryTiers(moved: true,  fought: false), "moved only → +1");
            Assert.AreEqual(0, DegradationCheck.RecoveryTiers(moved: false, fought: true),  "fought → 0");
            Assert.AreEqual(0, DegradationCheck.RecoveryTiers(moved: true,  fought: true),  "moved and fought → 0");
        }

        [Test]
        public void ApplyUpkeepRecovery_AppliesTiersWithCap()
        {
            Assert.AreEqual(EfficiencyLevel.NormalOperations,
                DegradationCheck.ApplyUpkeepRecovery(EfficiencyLevel.StaticOperations, moved: false, fought: false),
                "Static rested → +2 → Normal");
            Assert.AreEqual(EfficiencyLevel.CombatOperations,
                DegradationCheck.ApplyUpkeepRecovery(EfficiencyLevel.NormalOperations, moved: true, fought: false),
                "Normal moved → +1 → Combat");
            Assert.AreEqual(EfficiencyLevel.CombatOperations,
                DegradationCheck.ApplyUpkeepRecovery(EfficiencyLevel.CombatOperations, moved: false, fought: true),
                "Combat fought → +0 → unchanged");
        }

        #endregion // Upkeep recovery
    }
}
