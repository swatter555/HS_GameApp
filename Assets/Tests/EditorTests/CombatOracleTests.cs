using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.AI;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// CombatOracle (AI0, AI-Design-Supplement Part 5) — exact-forecast tests. Two families:
    /// (1) hand-computed goldens (assume GROUND/AIR_BALANCE_MOD at their default 1.0);
    /// (2) DRIFT GUARDS that exhaustively enumerate every dice combination through the REAL
    ///     CombatEngine.ResolveLane and require the oracle's distribution to match EXACTLY.
    ///     If the §7.7.1 pipeline ever changes, these fail and the oracle mirror must be
    ///     updated in the same pass. (Bounded loops, ≤ 576 engine calls per test — the one
    ///     place enumeration is load-bearing; flagged per the no-loops guideline.)
    /// </summary>
    [TestFixture]
    public class CombatOracleTests
    {
        private const double EPS = 1e-9;

        #region Pmf primitives

        [Test]
        public void Pmf_TwoD6Plus5_MatchesCommandingExpression()
        {
            Pmf pmf = Pmf.DicePlus(2, 6, 5);

            Assert.AreEqual(1.0, pmf.TotalMass, EPS);
            Assert.AreEqual(7, pmf.MinValue);
            Assert.AreEqual(17, pmf.MaxValue);
            Assert.AreEqual(12.0, pmf.ExpectedValue, EPS);
            Assert.AreEqual(1.0 / 36.0, pmf.Prob(7), EPS);   // double ones
            Assert.AreEqual(6.0 / 36.0, pmf.Prob(12), EPS);  // most likely sum
        }

        [Test]
        public void BandPmf_Even_IsUniformZeroToSeven()
        {
            Pmf pmf = CombatOracle.BandDamagePmf(DamageBand.Even); // 1d8−1

            Assert.AreEqual(1.0, pmf.TotalMass, EPS);
            for (int v = 0; v <= 7; v++)
                Assert.AreEqual(0.125, pmf.Prob(v), EPS, $"P({v})");
        }

        #endregion // Pmf primitives

        #region Lane forecast — hand goldens (balance mods at default 1.0)

        [Test]
        public void ForecastLane_EvenBandClearTerrain_IsUniform()
        {
            var lane = new LaneInput
            {
                FirerAttack = 10,
                TargetDefense = 10,               // Δ 0 → Even, 1d8−1
                AttackType = AttackType.Direct,
                TargetTerrain = TerrainType.Clear, // block 0
            };

            LaneForecast f = CombatOracle.ForecastLane(lane);

            Assert.AreEqual(1.0, f.Damage.TotalMass, EPS);
            Assert.AreEqual(3.5, f.ExpectedDamage, EPS);
            Assert.AreEqual(0.125, f.MissChance, EPS);        // the natural 0
            for (int v = 0; v <= 7; v++)
                Assert.AreEqual(0.125, f.Damage.Prob(v), EPS, $"P({v})");
        }

        [Test]
        public void ForecastLane_GrimVsMountains_FloorsEveryHitToOne()
        {
            // Grim (1d3, no natural 0) into a Heavy block (1d4+2, 3..6): every connecting hit is
            // blocked below 1 and floors at exactly 1 (§7.5.6.4).
            var lane = new LaneInput
            {
                FirerAttack = 4,
                TargetDefense = 10,                    // Δ −6 → Grim
                AttackType = AttackType.Direct,
                TargetTerrain = TerrainType.Mountains,
            };

            LaneForecast f = CombatOracle.ForecastLane(lane);

            Assert.AreEqual(1.0, f.Damage.Prob(1), EPS);
            Assert.AreEqual(0.0, f.MissChance, EPS);
        }

        [Test]
        public void ForecastLane_QualityMultiplier_RoundsHalfUpWithGaps()
        {
            // Favorable (1d6+2 → 3..8) × quality 1.25, no terrain: 3.75→4, 5, 6.25→6, 7.5→8, 8.75→9, 10.
            // Note the gap at 7 — pointwise rounding produces holes a naive scaler would miss.
            var lane = new LaneInput
            {
                FirerAttack = 13,
                TargetDefense = 10,                // Δ +3 → Favorable
                FirerQualityMult = 1.25f,
                AttackType = AttackType.Direct,
                BypassTerrainBlock = true,
            };

            LaneForecast f = CombatOracle.ForecastLane(lane);

            double sixth = 1.0 / 6.0;
            Assert.AreEqual(sixth, f.Damage.Prob(4), EPS);
            Assert.AreEqual(sixth, f.Damage.Prob(5), EPS);
            Assert.AreEqual(sixth, f.Damage.Prob(6), EPS);
            Assert.AreEqual(0.0,   f.Damage.Prob(7), EPS);
            Assert.AreEqual(sixth, f.Damage.Prob(8), EPS);
            Assert.AreEqual(sixth, f.Damage.Prob(9), EPS);
            Assert.AreEqual(sixth, f.Damage.Prob(10), EPS);
        }

        [Test]
        public void ForecastLane_CommandMitigation_LiftsDeltaTowardEvenFloor()
        {
            // Δ −5 (Grim) lifted by FirerCommand 3 → clamped at −2 → Disadvantaged (1d4), §7.7.12.
            var lane = new LaneInput
            {
                FirerAttack = 5,
                TargetDefense = 10,
                FirerCommand = 3,
                AttackType = AttackType.Direct,
                BypassTerrainBlock = true,
            };

            LaneForecast f = CombatOracle.ForecastLane(lane);

            Assert.AreEqual(1, f.Damage.MinValue);
            Assert.AreEqual(4, f.Damage.MaxValue);
            Assert.AreEqual(0.25, f.Damage.Prob(3), EPS);
        }

        #endregion // Lane forecast — hand goldens

        #region Lane forecast — drift guards vs the real engine (exhaustive dice enumeration)

        [Test]
        public void ForecastLane_MatchesEngine_EvenBandForest()
        {
            var lane = new LaneInput
            {
                FirerAttack = 10,
                TargetDefense = 10,                 // Even → d8
                AttackType = AttackType.Direct,
                TargetTerrain = TerrainType.Forest, // Light → d2
            };

            AssertOracleMatchesEngine(lane, 8, 2);
        }

        [Test]
        public void ForecastLane_MatchesEngine_ReturnLaneCommandingCityCrossing()
        {
            // Return lane with the full stack: Commanding (2d6+5), deployment 1.2, quality 1.1,
            // MajorCity Heavy block (d4+2), contested crossing (+d4). Dice order: d6, d6, block d4, crossing d4.
            var lane = new LaneInput
            {
                FirerAttack = 18,
                TargetDefense = 6,                  // Δ +12 → Commanding
                FirerQualityMult = 1.1f,
                FirerDeploymentMod = 1.2f,
                FirerIsDefender = true,
                AttackType = AttackType.Direct,
                TargetTerrain = TerrainType.MajorCity,
                ContestedCrossing = true,
            };

            AssertOracleMatchesEngine(lane, 6, 6, 4, 4);
        }

        [Test]
        public void ForecastLane_MatchesEngine_AirstrikeShiftedWithScalar()
        {
            // Airstrike: Δ +2 Favorable shifted +1 → Advantaged (1d8+3); OL 12 → ×12/9; post-stack ×2;
            // AirBalanceMod path; Marsh Medium block (d4). Dice order: band d8, block d4.
            var lane = new LaneInput
            {
                FirerAttack = 10,
                TargetDefense = 8,
                AttackType = AttackType.Airstrike,
                OrdnanceLoad = 12,
                FirerIsAir = true,
                BandShift = 1,
                PostStackScalar = 2.0f,
                TargetTerrain = TerrainType.Marsh,
            };

            AssertOracleMatchesEngine(lane, 8, 4);
        }

        /// <summary>
        /// Enumerates every combination of the lane's dice through the REAL CombatEngine.ResolveLane
        /// (uniform weight per combination) and asserts the oracle's Pmf matches exactly.
        /// <paramref name="diceSides"/> lists each die's side count in engine consumption order:
        /// band die(s), then terrain block die (rolled even on a natural-0 band roll), then crossing die.
        /// </summary>
        private static void AssertOracleMatchesEngine(LaneInput lane, params int[] diceSides)
        {
            int combos = 1;
            foreach (int sides in diceSides) combos *= sides;

            var expected = new Dictionary<int, double>();
            double weight = 1.0 / combos;
            var faces = new int[diceSides.Length];

            for (int i = 0; i < combos; i++)
            {
                int rem = i;
                for (int k = 0; k < diceSides.Length; k++)
                {
                    faces[k] = (rem % diceSides[k]) + 1;
                    rem /= diceSides[k];
                }

                int dmg = CombatEngine.ResolveLane(lane, new QueueRollRandom(faces));
                Accumulate(expected, dmg, weight);
            }

            Pmf actual = CombatOracle.ForecastLane(lane).Damage;

            Assert.AreEqual(1.0, actual.TotalMass, EPS, "oracle mass");
            foreach (KeyValuePair<int, double> kv in expected)
                Assert.AreEqual(kv.Value, actual.Prob(kv.Key), EPS, $"P(damage = {kv.Key})");
            Assert.AreEqual(expected.Count, System.Linq.Enumerable.Count(actual.Outcomes), "support size");
        }

        private static void Accumulate(Dictionary<int, double> target, int value, double mass)
        {
            target.TryGetValue(value, out double existing);
            target[value] = existing + mass;
        }

        #endregion // Lane forecast — drift guards

        #region Stand & fate forecasts

        [Test]
        public void ForecastStand_KnownShock_SplitsTheD10Exactly()
        {
            // Fixed 8 damage → Shock 2 → SV = 6 + 0 + 0 + 0 − 2 = 4 → hold .4 / retreat .3 / rout .3 / shatter 0.
            var stand = new StandValueInput
            {
                Deployment = DeploymentPosition.Deployed,
                Terrain = TerrainType.Clear,
                Experience = ExperienceLevel.Trained,
            };

            StandForecast f = CombatOracle.ForecastStand(stand, Pmf.Constant(8));

            Assert.AreEqual(0.4, f.PHold, EPS);
            Assert.AreEqual(0.3, f.PRetreat, EPS);
            Assert.AreEqual(0.3, f.PRout, EPS);
            Assert.AreEqual(0.0, f.PShatter, EPS);
        }

        [Test]
        public void ForecastDefenderFate_LethalBand_AllDamageKillsOutright()
        {
            // Strong band (1d10+4, min 5) against a 5-HP defender: every outcome destroys — no stand check.
            var lane = new LaneInput
            {
                FirerAttack = 18,
                TargetDefense = 10,                // Δ +8 → Strong
                AttackType = AttackType.Direct,
                BypassTerrainBlock = true,
            };
            var stand = new StandValueInput
            {
                Deployment = DeploymentPosition.Deployed,
                Terrain = TerrainType.Clear,
                Experience = ExperienceLevel.Trained,
            };
            var ctx = new DefenderFateContext { DefenderCurrentHP = 5f, RetreatPathExists = true };

            DefenderFateForecast f = CombatOracle.ForecastDefenderFate(lane, stand, ctx);

            Assert.AreEqual(1.0, f.PDestroyed, EPS);
            Assert.AreEqual(1.0, f.PVacatesHex, EPS);
            Assert.AreEqual(0.0, f.PQuitsField, EPS);
            Assert.AreEqual(0.0, f.PStaysInHex, EPS);
        }

        [Test]
        public void ForecastDefenderFate_NoRetreatPath_SurrenderOdds()
        {
            // Fixed 4 damage on a 20-HP Trained defender in the open, POCKETED (no valid retreat hex):
            // Shock 1 → SV 5 → hold .5, displaced .5 (retreat .3 + rout .2), shatter 0.
            // Displaced → Surrender Check (Trained check number 10 → P(destroyed) = .5):
            //   destroyed .25 (vacates); passed .25 → −10 HP survival loss on 16 HP remaining → survives, stays.
            var stand = new StandValueInput
            {
                Deployment = DeploymentPosition.Deployed,
                Terrain = TerrainType.Clear,
                Experience = ExperienceLevel.Trained,
            };
            var ctx = new DefenderFateContext { DefenderCurrentHP = 20f, RetreatPathExists = false };

            DefenderFateForecast f = CombatOracle.ForecastDefenderFate(Pmf.Constant(4), stand, ctx);

            Assert.AreEqual(0.25, f.PDestroyed, EPS);
            Assert.AreEqual(0.25, f.PVacatesHex, EPS);
            Assert.AreEqual(0.75, f.PStaysInHex, EPS);
            Assert.AreEqual(0.0, f.PQuitsField, EPS);
        }

        [Test]
        public void ForecastDefenderFate_ShatterWithPath_QuitsFieldWithoutDying()
        {
            // Fixed 19 damage on a 30-HP RAW defender in the open, path available:
            // Shock 5 → SV = 6 − 2 − 5 = −1 → hold 0, retreat .2, rout .3, shatter .5.
            // Shatter survives the +4 extra (30−19−4 = 7 HP) → quits the field (vacated, NOT destroyed).
            var stand = new StandValueInput
            {
                Deployment = DeploymentPosition.Deployed,
                Terrain = TerrainType.Clear,
                Experience = ExperienceLevel.Raw,
            };
            var ctx = new DefenderFateContext { DefenderCurrentHP = 30f, RetreatPathExists = true };

            DefenderFateForecast f = CombatOracle.ForecastDefenderFate(Pmf.Constant(19), stand, ctx);

            Assert.AreEqual(0.0, f.PDestroyed, EPS);
            Assert.AreEqual(1.0, f.PVacatesHex, EPS);   // .5 displaced + .5 shatter-quit
            Assert.AreEqual(0.5, f.PQuitsField, EPS);
            Assert.AreEqual(0.0, f.PStaysInHex, EPS);
        }

        [Test]
        public void ForecastDirectEngagement_ReturnFireKillOdds()
        {
            // Return lane Strong (1d10+4 → 5..14) against a 10-HP attacker: P(d ≥ 10) = P(die ≥ 6) = 0.5.
            var forward = new LaneInput
            {
                FirerAttack = 10,
                TargetDefense = 10,
                AttackType = AttackType.Direct,
                BypassTerrainBlock = true,
            };
            var ret = new LaneInput
            {
                FirerAttack = 18,
                TargetDefense = 10,
                FirerIsDefender = true,
                AttackType = AttackType.Direct,
                BypassTerrainBlock = true,
            };
            var stand = new StandValueInput
            {
                Deployment = DeploymentPosition.Deployed,
                Terrain = TerrainType.Clear,
                Experience = ExperienceLevel.Trained,
            };
            var ctx = new DefenderFateContext { DefenderCurrentHP = 40f, RetreatPathExists = true };

            EngagementForecast f = CombatOracle.ForecastDirectEngagement(forward, ret, stand, ctx, attackerCurrentHP: 10f);

            Assert.AreEqual(0.5, f.PAttackerDestroyed, EPS);
            Assert.AreEqual(1.0, f.DamageToAttacker.TotalMass, EPS);
        }

        #endregion // Stand & fate forecasts

        #region Degradation odds passthroughs

        [Test]
        public void DegradationOdds_MirrorTheTables()
        {
            Assert.AreEqual(0.35, CombatOracle.CombatEfficiencyLossChance(ExperienceLevel.Elite), EPS);
            Assert.AreEqual(0.50, CombatOracle.CombatEfficiencyLossChance(ExperienceLevel.Raw), EPS);
            Assert.AreEqual(0.18, CombatOracle.MoveEfficiencyLossChance(ExperienceLevel.Trained), EPS);
            Assert.AreEqual(0.58, CombatOracle.CombatSupplyLossChance(ExperienceLevel.Trained), EPS);
            Assert.AreEqual(0.50, CombatOracle.CounterBatterySupplyLossChance(), EPS);
        }

        #endregion // Degradation odds passthroughs
    }
}
