using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M11 validation (pure layer): the RECONA Recon Box chain (§11.11) — stealth bypass (§11.11.7), Roll 1
    /// mission-success contest (§11.11.8, tie→recon), Roll 2 1d30 escape (§11.11.9), Roll 3 combat round whose Δ
    /// surrogate feeds the §7.6 band with the winner damaging the loser (§11.11.10, tie→recon). Pure: dice scripted
    /// via <see cref="QueueRollRandom"/>; the engine returns the tier + HP numbers, the caller applies them.
    /// Dice order: [stealth 1d100 if STL&gt;0] · Roll1 recon 1d6 · Roll1 interceptor 1d6 · Roll2 1d30 ·
    /// Roll3 recon 1d6 · Roll3 interceptor 1d6 · band die.
    /// </summary>
    [TestFixture]
    public class ReconMissionEngineTests
    {
        // Recon: TS 12 / MAN 8 / SUR 10, no stealth, Trained (mod 0), neutral quality.
        private static ReconInput Recon(int stl = 0) => new ReconInput
        {
            TopSpeed = 12, Maneuverability = 8, Survivability = 10, Stealth = stl,
            Experience = ExperienceLevel.Trained, QualityMult = 1.0f,
        };

        // Interceptor: TS 14 / DF 12, Trained, neutral quality.
        private static InterceptorInput Interceptor() => new InterceptorInput
        {
            TopSpeed = 14, Dogfighting = 12, Experience = ExperienceLevel.Trained, QualityMult = 1.0f,
        };

        #region Ratings (§11.11.8 / §11.11.10)

        [Test]
        public void Ratings_MatchDesignDocFormulas()
        {
            Assert.AreEqual(10, ReconMissionEngine.ReconMissionRating(Recon()), "floor((12×2+8)/3) = 10");
            Assert.AreEqual(13, ReconMissionEngine.InterceptorMissionRating(Interceptor()), "floor((14+12)/2) = 13");
            Assert.AreEqual(11, ReconMissionEngine.ReconCombatRating(Recon()), "floor((12×3+10)/4) = 11");
            Assert.AreEqual(13, ReconMissionEngine.InterceptorCombatRating(Interceptor()), "floor((12+14×2)/3) = 13");
        }

        #endregion // Ratings

        #region Stealth bypass (§11.11.7)

        [Test]
        public void Resolve_StealthBypass_FullMissionNoEngagement()
        {
            // STL 10 → 85% chance; a 1d100 of 1 clears it. Interceptor never engages.
            var r = ReconMissionEngine.Resolve(Recon(stl: 10), Interceptor(), new QueueRollRandom(1));
            Assert.AreEqual(ReconMissionTier.Full, r.Tier);
            Assert.IsTrue(r.StealthBypassed);
            Assert.IsFalse(r.ReachedCombat);
            Assert.AreEqual(0, r.DamageToRecon);
            Assert.AreEqual(0, r.DamageToInterceptor);
        }

        [Test]
        public void Resolve_NonStealthRecon_ConsumesNoStealthDie()
        {
            // STL 0 short-circuits without a roll, so the first die is Roll-1 recon. recon 6 / int 3 → 16 vs 16,
            // tie → recon wins Roll 1 → Full. (If a stealth die were consumed this queue would misalign.)
            var r = ReconMissionEngine.Resolve(Recon(), Interceptor(), new QueueRollRandom(6, 3));
            Assert.AreEqual(ReconMissionTier.Full, r.Tier);
            Assert.IsFalse(r.StealthBypassed);
        }

        #endregion // Stealth bypass

        #region Roll 1 — mission success (§11.11.8)

        [Test]
        public void Resolve_Roll1ReconWinsOnTie_FullMission()
        {
            // recon 10+6 = 16, interceptor 13+3 = 16 → tie favours the recon (§11.11.8.4).
            var r = ReconMissionEngine.Resolve(Recon(), Interceptor(), new QueueRollRandom(6, 3));
            Assert.AreEqual(ReconMissionTier.Full, r.Tier);
            Assert.IsFalse(r.ReachedCombat, "a Roll-1 win skips Rolls 2 and 3");
        }

        #endregion // Roll 1

        #region Roll 2 — escape (§11.11.9)

        [Test]
        public void Resolve_Roll1LossThenEscape_PartialMissionNoDamage()
        {
            // Roll 1: recon 10+1 = 11 < interceptor 13+1 = 14 (loss). Roll 2: 1d30 = 5 ≤ TS 12 → escape.
            var r = ReconMissionEngine.Resolve(Recon(), Interceptor(), new QueueRollRandom(1, 1, 5));
            Assert.AreEqual(ReconMissionTier.Partial, r.Tier);
            Assert.IsFalse(r.ReachedCombat, "a clean escape skips the combat round");
            Assert.AreEqual(0, r.DamageToRecon);
            Assert.AreEqual(0, r.DamageToInterceptor);
        }

        #endregion // Roll 2

        #region Roll 3 — combat round (§11.11.10)

        [Test]
        public void Resolve_Roll3ReconWinsOnTie_PartialMission_InterceptorTakesEvenBand()
        {
            // Roll1 loss [1,1] → Roll2 caught [20] → Roll3 recon 11+6 = 17, interceptor 13+4 = 17 (tie → recon wins).
            // Δ surrogate = 0 → Even band 1d8−1; band die 5 → 4 HP to the interceptor, recon untouched.
            var r = ReconMissionEngine.Resolve(Recon(), Interceptor(), new QueueRollRandom(1, 1, 20, 6, 4, 5));
            Assert.AreEqual(ReconMissionTier.Partial, r.Tier);
            Assert.IsTrue(r.ReachedCombat);
            Assert.IsTrue(r.ReconWonCombat);
            Assert.AreEqual(4, r.DamageToInterceptor, "Even band 1d8−1 with a 5");
            Assert.AreEqual(0, r.DamageToRecon);
        }

        [Test]
        public void Resolve_Roll3ReconLoses_MarginalMission_ReconTakesBandByDelta()
        {
            // Roll1 loss [1,1] → Roll2 caught [20] → Roll3 recon 11+1 = 12 < interceptor 13+6 = 19 (loss).
            // Δ surrogate = 7 → Advantaged band 1d8+3; band die 5 → 8 HP to the recon, interceptor untouched.
            var r = ReconMissionEngine.Resolve(Recon(), Interceptor(), new QueueRollRandom(1, 1, 20, 1, 6, 5));
            Assert.AreEqual(ReconMissionTier.Marginal, r.Tier);
            Assert.IsTrue(r.ReachedCombat);
            Assert.IsFalse(r.ReconWonCombat);
            Assert.AreEqual(8, r.DamageToRecon, "Advantaged band 1d8+3 with a 5");
            Assert.AreEqual(0, r.DamageToInterceptor);
        }

        #endregion // Roll 3
    }
}
