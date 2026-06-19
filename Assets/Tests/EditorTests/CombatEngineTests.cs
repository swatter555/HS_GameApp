using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M1 validation: the §7.7.1 damage engine, one lane at a time. Dice are scripted (QueueRollRandom) in
    /// the engine's call order — band die(s) first, then terrain block, then contested-crossing — so each
    /// test asserts an exact HP result. Covers axis Δ→band→roll, the multiplier stack (quality / deployment
    /// only-if-defender / OL only-if-airstrike / post-stack scalar), terrain block + floor, BM/ambush bypass,
    /// embarkment band shift, and the contested-crossing block.
    /// </summary>
    [TestFixture]
    public class CombatEngineTests
    {
        // A baseline Δ used across tests: Atk 10 vs Def 8 → Δ +2 → Favorable (1d6+2). A band die of 4 → baseHP 6.
        private static LaneInput Favorable10v8() => new LaneInput
        {
            FirerAttack = 10,
            TargetDefense = 8,
            AttackType = AttackType.Direct,
            TargetTerrain = TerrainType.Clear,
        };

        #region Core pipeline

        [Test]
        public void ResolveLane_NoModifiers_ReturnsRolledHP()
        {
            var input = Favorable10v8();
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(4)); // 1d6=4 → 6
            Assert.AreEqual(6, dmg);
        }

        [Test]
        public void ResolveLane_QualityMultiplier_RoundsHalfUp()
        {
            var input = Favorable10v8();
            input.FirerQualityMult = 1.15f;                                    // 6 × 1.15 = 6.9 → 7
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(4));
            Assert.AreEqual(7, dmg);
        }

        [Test]
        public void ResolveLane_NaturalZero_StaysZero()
        {
            var input = new LaneInput
            {
                FirerAttack = 8, TargetDefense = 8,      // Δ 0 → Even (1d8−1)
                FirerQualityMult = 1.15f,
                AttackType = AttackType.Direct,
                TargetTerrain = TerrainType.Clear,
            };
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(1)); // 1d8=1 → baseHP 0
            Assert.AreEqual(0, dmg, "A natural-0 band roll deals 0 regardless of multipliers (§7.5.6.4)");
        }

        #endregion // Core pipeline

        #region Deployment — return lane only (§7.5.2)

        [Test]
        public void ResolveLane_Attacker_IgnoresDeploymentMod()
        {
            var input = Favorable10v8();
            input.FirerDeploymentMod = 1.3f;        // set, but firer is the attacker → must NOT apply
            input.FirerIsDefender = false;
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(4));
            Assert.AreEqual(6, dmg, "An attacker gets no deployment multiplier on outgoing damage (§7.5.2)");
        }

        [Test]
        public void ResolveLane_DefenderReturnFire_AppliesDeploymentMod()
        {
            var input = Favorable10v8();
            input.FirerDeploymentMod = 1.3f;        // Fortified
            input.FirerIsDefender = true;           // return lane → deployment applies
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(4)); // 6 × 1.3 = 7.8 → 8
            Assert.AreEqual(8, dmg);
        }

        #endregion // Deployment

        #region Airstrike OL (§11.6.1) and balance dial (§7.7.10)

        [Test]
        public void ResolveLane_Airstrike_AppliesOrdnanceLoadOverNine()
        {
            var input = new LaneInput
            {
                FirerAttack = 10, TargetDefense = 8,    // Δ +2 → Favorable
                AttackType = AttackType.Airstrike,
                OrdnanceLoad = 12,                      // OL/9 = 1.333…
                FirerIsAir = true,
                TargetTerrain = TerrainType.Clear,
            };
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(4)); // 6 × (12/9) = 8.0 → 8
            Assert.AreEqual(8, dmg);
        }

        #endregion // Airstrike OL

        #region Terrain block + floor (§7.5.6)

        [Test]
        public void ResolveLane_TerrainBlock_SubtractedAfterStack()
        {
            var input = Favorable10v8();
            input.TargetTerrain = TerrainType.Rough;                              // Medium 1d4
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(4, 3)); // band 4 → 6; block 3 → 6−3
            Assert.AreEqual(3, dmg);
        }

        [Test]
        public void ResolveLane_TerrainNeverReducesConnectingHitBelowOne()
        {
            var input = new LaneInput
            {
                FirerAttack = 8, TargetDefense = 8,      // Δ 0 → Even (1d8−1)
                AttackType = AttackType.Direct,
                TargetTerrain = TerrainType.MajorCity,   // Heavy 1d4+2
            };
            // band die 2 → baseHP 1; block die 4 → 6; 1 − 6 = −5 → floored to 1 (connecting hit).
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(2, 4));
            Assert.AreEqual(1, dmg);
        }

        [Test]
        public void ResolveLane_BallisticBypass_IgnoresTerrain()
        {
            var input = Favorable10v8();
            input.TargetTerrain = TerrainType.MajorCity; // Heavy — but bypassed
            input.BypassTerrainBlock = true;             // BM-class / ambushed victim (§7.5.6.6/.7)
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(4)); // no block die consumed → 6
            Assert.AreEqual(6, dmg);
        }

        #endregion // Terrain block + floor

        #region Band shift + post-stack scalar (§7.10.1 / §6.9.4)

        [Test]
        public void ResolveLane_EmbarkmentMalus_ShiftsBandUpAndDoublesDamage()
        {
            var input = Favorable10v8();
            input.BandShift = 1;            // Favorable → Advantaged (1d8+3)
            input.PostStackScalar = 2.0f;   // embarkment ×2 (§7.10.1.2)
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(5)); // 1d8=5 → 8; ×2 = 16
            Assert.AreEqual(16, dmg);
        }

        [Test]
        public void ResolveLane_Ambush_AppliesScalarOnClearTerrain()
        {
            var input = Favorable10v8();
            input.PostStackScalar = 1.5f;       // ambush ×1.5 (§6.9.4)
            input.BypassTerrainBlock = true;    // ambushed unit attacked as if on Clear (§7.5.6.7)
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(4)); // 6 × 1.5 = 9
            Assert.AreEqual(9, dmg);
        }

        #endregion // Band shift + post-stack scalar

        #region Contested crossing (§7.5.6.9)

        [Test]
        public void ResolveLane_ContestedCrossing_AddsBlockOnDirectFire()
        {
            var input = Favorable10v8();
            input.ContestedCrossing = true;                                       // direct fire across a river edge
            int dmg = CombatEngine.ResolveLane(input, new QueueRollRandom(4, 4)); // band 4 → 6; crossing 1d4=4 → 6−4
            Assert.AreEqual(2, dmg);
        }

        #endregion // Contested crossing
    }
}
