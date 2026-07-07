using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M2 validation: the defender-only stand check (§7.9) — Shock (§7.9.1.1), the per-tier SV mods
    /// (§7.9.2–§7.9.4), Stand Value assembly (§7.9.1), and the 1d10 resolution gaps (§7.9.5). Pure math.
    /// </summary>
    [TestFixture]
    public class StandCheckTests
    {
        #region Shock (§7.9.1.1)

        [Test]
        public void Shock_CeilsHpOverFour_ClampedToEight()
        {
            Assert.AreEqual(0, StandCheck.Shock(0),  "0 HP → 0");
            Assert.AreEqual(1, StandCheck.Shock(1),  "1 HP → 1");
            Assert.AreEqual(1, StandCheck.Shock(4),  "4 HP → 1 (light hit)");
            Assert.AreEqual(2, StandCheck.Shock(5),  "5 HP → 2");
            Assert.AreEqual(3, StandCheck.Shock(10), "10 HP (≈25%) → 3");
            Assert.AreEqual(5, StandCheck.Shock(20), "20 HP (≈50%) → 5");
            Assert.AreEqual(8, StandCheck.Shock(32), "32 HP → 8 (cap)");
            Assert.AreEqual(8, StandCheck.Shock(40), "40 HP → 8 (clamped)");
        }

        #endregion // Shock

        #region Per-tier SV mods (§7.9.2–§7.9.4)

        [Test]
        public void DeploymentStandMod_MatchesTable()
        {
            Assert.AreEqual(-2, StandCheck.DeploymentStandMod(DeploymentPosition.Embarked),     "Embarked −2");
            Assert.AreEqual(0,  StandCheck.DeploymentStandMod(DeploymentPosition.Mobile),       "Mobile 0");
            Assert.AreEqual(0,  StandCheck.DeploymentStandMod(DeploymentPosition.Deployed),     "Deployed 0");
            Assert.AreEqual(1,  StandCheck.DeploymentStandMod(DeploymentPosition.HastyDefense), "HastyDefense +1");
            Assert.AreEqual(2,  StandCheck.DeploymentStandMod(DeploymentPosition.Entrenched),   "Entrenched +2");
            Assert.AreEqual(3,  StandCheck.DeploymentStandMod(DeploymentPosition.Fortified),    "Fortified +3");
        }

        [Test]
        public void TerrainStandMod_MatchesTable()
        {
            Assert.AreEqual(0, StandCheck.TerrainStandMod(TerrainType.Clear),     "Clear 0");
            Assert.AreEqual(0, StandCheck.TerrainStandMod(TerrainType.Water),     "Water 0");
            Assert.AreEqual(1, StandCheck.TerrainStandMod(TerrainType.Forest),    "Forest +1");
            Assert.AreEqual(1, StandCheck.TerrainStandMod(TerrainType.Rough),     "Rough +1");
            Assert.AreEqual(1, StandCheck.TerrainStandMod(TerrainType.Marsh),     "Marsh +1");
            Assert.AreEqual(1, StandCheck.TerrainStandMod(TerrainType.MinorCity), "MinorCity +1");
            Assert.AreEqual(2, StandCheck.TerrainStandMod(TerrainType.Mountains), "Mountains +2");
            Assert.AreEqual(2, StandCheck.TerrainStandMod(TerrainType.MajorCity), "MajorCity +2");
        }

        [Test]
        public void ExperienceStandMod_MatchesTable()
        {
            Assert.AreEqual(-2, StandCheck.ExperienceStandMod(ExperienceLevel.Raw),         "Raw −2");
            Assert.AreEqual(-1, StandCheck.ExperienceStandMod(ExperienceLevel.Green),       "Green −1");
            Assert.AreEqual(0,  StandCheck.ExperienceStandMod(ExperienceLevel.Trained),     "Trained 0");
            Assert.AreEqual(1,  StandCheck.ExperienceStandMod(ExperienceLevel.Experienced), "Experienced +1");
            Assert.AreEqual(2,  StandCheck.ExperienceStandMod(ExperienceLevel.Veteran),     "Veteran +2");
            Assert.AreEqual(3,  StandCheck.ExperienceStandMod(ExperienceLevel.Elite),       "Elite +3");
        }

        #endregion // Per-tier SV mods

        #region Stand Value assembly (§7.9.1)

        [Test]
        public void ComputeStandValue_OpenDeployedTrained_SolidHit()
        {
            var input = new StandValueInput
            {
                Deployment = DeploymentPosition.Deployed,
                Terrain = TerrainType.Clear,
                Experience = ExperienceLevel.Trained,
                HpDealtThisAttack = 10, // shock 3
            };
            // 6 + 0 + 0 + 0 − 3 = 3 (matches §7.9.5.1 illustrative SV 3)
            Assert.AreEqual(3, StandCheck.ComputeStandValue(input));
        }

        [Test]
        public void ComputeStandValue_FullStack_AllTermsContribute()
        {
            var input = new StandValueInput
            {
                Deployment = DeploymentPosition.Fortified,   // +3
                Terrain = TerrainType.Mountains,             // +2
                Experience = ExperienceLevel.Elite,          // +3
                LeaderMod = 3,                               // +3 (capped — the ONLY defensive leader SV term, §7.9.4b tombstone)
                AttackerCommand = 2,                         // −2 Superior
                FlankAttack = true,                          // −1
                HpDealtThisAttack = 8,                       // shock 2
            };
            // 6 +3 +2 +3 +3 −2 −1 −2 = 12 (max defensive stack is 16 pre-shock since the 7.9.4b retirement)
            Assert.AreEqual(12, StandCheck.ComputeStandValue(input));
        }

        [Test]
        public void ComputeStandValue_AttackerCommandAndFlank_LowerSV()
        {
            var input = new StandValueInput
            {
                Deployment = DeploymentPosition.Deployed,
                Terrain = TerrainType.Clear,
                Experience = ExperienceLevel.Trained,
                AttackerCommand = 3, // −3 Genius attacker (command shock)
                FlankAttack = true,  // −1
                HpDealtThisAttack = 0,
            };
            // 6 − 3 − 1 = 2
            Assert.AreEqual(2, StandCheck.ComputeStandValue(input));
        }

        #endregion // Stand Value assembly

        #region Stand resolution gaps (§7.9.5)

        [Test]
        public void ResolveStand_AtSV3_PartitionsBy1d10()
        {
            Assert.AreEqual(StandOutcome.Hold,    StandCheck.ResolveStand(3, new QueueRollRandom(3)),  "roll 3 ≤ SV → Hold");
            Assert.AreEqual(StandOutcome.Retreat, StandCheck.ResolveStand(3, new QueueRollRandom(4)),  "roll 4 (≤ SV+3) → Retreat");
            Assert.AreEqual(StandOutcome.Retreat, StandCheck.ResolveStand(3, new QueueRollRandom(6)),  "roll 6 (= SV+3) → Retreat");
            Assert.AreEqual(StandOutcome.Rout,    StandCheck.ResolveStand(3, new QueueRollRandom(7)),  "roll 7 (≤ SV+6) → Rout");
            Assert.AreEqual(StandOutcome.Rout,    StandCheck.ResolveStand(3, new QueueRollRandom(9)),  "roll 9 (= SV+6) → Rout");
            Assert.AreEqual(StandOutcome.Shatter, StandCheck.ResolveStand(3, new QueueRollRandom(10)), "roll 10 (> SV+6) → Shatter");
        }

        [Test]
        public void ResolveStand_HighSV_AlwaysHolds_LowSV_ShattersEasily()
        {
            Assert.AreEqual(StandOutcome.Hold,    StandCheck.ResolveStand(10, new QueueRollRandom(10)), "SV 10: max d10 still holds");
            Assert.AreEqual(StandOutcome.Shatter, StandCheck.ResolveStand(2,  new QueueRollRandom(9)),  "SV 2: roll 9 (> 8) → Shatter");
        }

        #endregion // Stand resolution gaps
    }
}
