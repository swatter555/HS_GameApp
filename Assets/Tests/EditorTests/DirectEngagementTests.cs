using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M2 validation: <see cref="CombatEngine.ResolveDirectEngagement"/> — universal return fire (§6.12 / §7.7.3)
    /// wiring two damage lanes plus the defender-only stand check end to end. Dice are scripted in the engine's
    /// call order: forward lane, return lane, then the defender's 1d10 stand roll.
    /// </summary>
    [TestFixture]
    public class DirectEngagementTests
    {
        // Both combatants: Atk 10 vs Def 8 → Δ +2 → Favorable (1d6+2); a band die of 4 → 6 HP.
        private static LaneInput Forward() => new LaneInput
        {
            FirerAttack = 10, TargetDefense = 8,
            AttackType = AttackType.Direct,
            TargetTerrain = TerrainType.Clear,
        };

        private static LaneInput Return() => new LaneInput
        {
            FirerAttack = 10, TargetDefense = 8,
            AttackType = AttackType.Direct,
            FirerIsDefender = true,           // return lane: deployment would apply (here Deployed → neutral)
            TargetTerrain = TerrainType.Clear,
        };

        [Test]
        public void ResolveDirectEngagement_BothDealDamage_DefenderHolds()
        {
            var defenderStand = new StandValueInput
            {
                Deployment = DeploymentPosition.Deployed,
                Terrain = TerrainType.Clear,
                Experience = ExperienceLevel.Trained,
                // HpDealtThisAttack is filled by the engine from the forward-lane result.
            };

            // forward band 4 → 6; return band 4 → 6; shock(6)=2 → SV = 6 − 2 = 4; stand roll 4 (≤4) → Hold.
            var result = CombatEngine.ResolveDirectEngagement(
                Forward(), Return(), defenderStand, new QueueRollRandom(4, 4, 4));

            Assert.AreEqual(6, result.DamageToDefender, "forward lane HP");
            Assert.AreEqual(6, result.DamageToAttacker, "return lane HP (universal return fire)");
            Assert.AreEqual(2, result.DefenderShock, "Shock from 6 HP");
            Assert.AreEqual(4, result.DefenderStandValue, "SV = 6 − Shock 2");
            Assert.AreEqual(StandOutcome.Hold, result.DefenderOutcome);
        }

        [Test]
        public void ResolveDirectEngagement_CommandAndFlankDriveShatter()
        {
            var defenderStand = new StandValueInput
            {
                Deployment = DeploymentPosition.Deployed, // 0
                Terrain = TerrainType.Clear,              // 0
                Experience = ExperienceLevel.Raw,         // −2
                AttackerCommand = 3,                      // −3 Genius attacker
                FlankAttack = true,                       // −1
            };

            // forward 4 → 6; return 4 → 6; shock(6)=2; SV = 6 −2 −3 −1 −2 = −2; SV+6 = 4; stand roll 5 (>4) → Shatter.
            var result = CombatEngine.ResolveDirectEngagement(
                Forward(), Return(), defenderStand, new QueueRollRandom(4, 4, 5));

            Assert.AreEqual(6, result.DamageToDefender);
            Assert.AreEqual(6, result.DamageToAttacker);
            Assert.AreEqual(-2, result.DefenderStandValue, "SV driven negative by exp/command/flank/shock");
            Assert.AreEqual(StandOutcome.Shatter, result.DefenderOutcome);
        }
    }
}
