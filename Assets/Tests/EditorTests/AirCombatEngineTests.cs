using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M7 validation: the pure air-to-air engine (§11.4.8). Covers the dogfight offense/defense ratings, the
    /// mutual dogfight pass (reusing the §7.7.1 damage engine through the air pipeline — AirBalanceMod 1.0, no
    /// terrain, no deployment, no OL), the post-pass air stand checks, the breakthrough opposed roll (tie →
    /// interceptor, damage penalty), and the stealth avoidance table. Dice are scripted in call order.
    /// </summary>
    [TestFixture]
    public class AirCombatEngineTests
    {
        // A late-fighter-ish dogfighter: DF 12 / MAN 12 / TS 10 / SUR 9, Trained, neutral quality.
        private static DogfighterInput Fighter(ExperienceLevel exp = ExperienceLevel.Trained, float quality = 1f) =>
            new DogfighterInput
            {
                Dogfighting = 12,
                Maneuverability = 12,
                TopSpeed = 10,
                Survivability = 9,
                Experience = exp,
                QualityMult = quality,
            };

        #region Ratings (§11.4.8.2)

        [Test]
        public void DogfightRatings_MatchFormula()
        {
            Assert.AreEqual(12, AirCombatEngine.DogfightOffense(12, 12), "(DF+MAN)/2");
            Assert.AreEqual(11, AirCombatEngine.DogfightDefense(12, 9), "(MAN*2+SUR)/3 = 33/3");
            Assert.AreEqual(8, AirCombatEngine.DogfightDefense(9, 6), "early fighter (18+6)/3");
        }

        [Test]
        public void PairingMetric_AddsExperienceMod()
        {
            Assert.AreEqual(12, AirCombatEngine.PairingMetric(Fighter(ExperienceLevel.Trained)));
            Assert.AreEqual(15, AirCombatEngine.PairingMetric(Fighter(ExperienceLevel.Elite)), "+3 elite");
        }

        #endregion // Ratings

        #region Dogfight pass (§11.4.8.2 + §11.4.8.2a)

        [Test]
        public void DogfightPass_MutualDamageAndStands()
        {
            // Both Δ = 12 − 11 = +1 → Even (1d8−1).
            // Dice: A→B 1d8=5 → baseHP 4; B→A 1d8=3 → baseHP 2; standA 1d10=7; standB 1d10=8.
            // standA: HpLost 2 → Shock 1 → SV 6+0+2−1 = 7; roll 7 → Hold.
            // standB: HpLost 4 → Shock 1 → SV 7; roll 8 → Disengage.
            var rng = new QueueRollRandom(5, 3, 7, 8);
            var r = AirCombatEngine.ResolveDogfightPass(Fighter(), Fighter(), rng);

            Assert.AreEqual(4, r.DamageToB, "A dealt 4");
            Assert.AreEqual(2, r.DamageToA, "B dealt 2");
            Assert.AreEqual(AirStandOutcome.Hold, r.StandA);
            Assert.AreEqual(AirStandOutcome.Disengage, r.StandB);
        }

        [Test]
        public void DogfightPass_QualityMultiplierAppliesToDamage()
        {
            // A quality 1.15: A→B baseHP 4 × 1.15 = 4.6 → 5. B→A baseHP 2 × 1.0 = 2.
            // standA HpLost 2 → SV 7, roll 1 → Hold. standB HpLost 5 → Shock ceil(5/4)=2 → SV 6, roll 1 → Hold.
            var rng = new QueueRollRandom(5, 3, 1, 1);
            var r = AirCombatEngine.ResolveDogfightPass(Fighter(quality: 1.15f), Fighter(), rng);

            Assert.AreEqual(5, r.DamageToB, "quality 1.15 rounds 4.6 → 5");
            Assert.AreEqual(2, r.DamageToA);
            Assert.AreEqual(AirStandOutcome.Hold, r.StandA);
            Assert.AreEqual(AirStandOutcome.Hold, r.StandB);
        }

        [Test]
        public void DogfightPass_NaturalZeroDealsZero()
        {
            // A→B 1d8=1 → baseHP 0 → 0 dmg (floor: natural 0 stays 0). B→A 1d8=5 → 4.
            var rng = new QueueRollRandom(1, 5, 1, 1);
            var r = AirCombatEngine.ResolveDogfightPass(Fighter(), Fighter(), rng);

            Assert.AreEqual(0, r.DamageToB, "natural-0 band roll deals 0");
            Assert.AreEqual(4, r.DamageToA);
        }

        #endregion // Dogfight pass

        #region Breakthrough (§11.4.8.2.1)

        [Test]
        public void Breakthrough_InterceptorWinsHigherTotal()
        {
            // interceptor rating (10+12)/2 + 0 − 0 = 11; escort (12+12)/2 + 0 = 12.
            // Dice: interceptor 1d6=6 → 17; escort 1d6=1 → 13. 17 ≥ 13 → breaks through.
            Assert.IsTrue(AirCombatEngine.ResolveBreakthrough(Fighter(), 0, Fighter(), new QueueRollRandom(6, 1)));
        }

        [Test]
        public void Breakthrough_EscortWinsHigherTotal()
        {
            // interceptor 11+1 = 12; escort 12+1 = 13. 12 ≥ 13 false → held in screen.
            Assert.IsFalse(AirCombatEngine.ResolveBreakthrough(Fighter(), 0, Fighter(), new QueueRollRandom(1, 1)));
        }

        [Test]
        public void Breakthrough_TieFavorsInterceptor()
        {
            // interceptor 11+2 = 13; escort 12+1 = 13. tie → interceptor breaks through.
            Assert.IsTrue(AirCombatEngine.ResolveBreakthrough(Fighter(), 0, Fighter(), new QueueRollRandom(2, 1)));
        }

        [Test]
        public void Breakthrough_DamagePenaltyCanFlipResult()
        {
            // Same dice (interceptor 3, escort 1). pct 0: 11+3=14 vs 13 → true.
            Assert.IsTrue(AirCombatEngine.ResolveBreakthrough(Fighter(), 0, Fighter(), new QueueRollRandom(3, 1)));
            // pct 50 → −floor(50/25)=−2 → rating 9 → 9+3=12 vs 13 → false.
            Assert.IsFalse(AirCombatEngine.ResolveBreakthrough(Fighter(), 50, Fighter(), new QueueRollRandom(3, 1)));
        }

        #endregion // Breakthrough

        #region Stealth avoidance (§11.5)

        [Test]
        public void StealthChance_MatchesTable()
        {
            Assert.AreEqual(0, AirCombatEngine.StealthAvoidanceChance(0));
            Assert.AreEqual(15, AirCombatEngine.StealthAvoidanceChance(2));
            Assert.AreEqual(30, AirCombatEngine.StealthAvoidanceChance(4));
            Assert.AreEqual(45, AirCombatEngine.StealthAvoidanceChance(6));
            Assert.AreEqual(60, AirCombatEngine.StealthAvoidanceChance(8));
            Assert.AreEqual(75, AirCombatEngine.StealthAvoidanceChance(9));
            Assert.AreEqual(85, AirCombatEngine.StealthAvoidanceChance(10));
        }

        [Test]
        public void StealthRoll_BoundaryAndZero()
        {
            Assert.IsFalse(AirCombatEngine.RollStealthAvoidance(0, new FixedRollRandom(1)), "STL 0 never avoids (no dice)");
            Assert.IsTrue(AirCombatEngine.RollStealthAvoidance(4, new QueueRollRandom(30)), "30 ≤ 30%");
            Assert.IsFalse(AirCombatEngine.RollStealthAvoidance(4, new QueueRollRandom(31)), "31 > 30%");
            Assert.IsTrue(AirCombatEngine.RollStealthAvoidance(10, new QueueRollRandom(85)), "85 ≤ 85%");
        }

        #endregion // Stealth avoidance
    }
}
