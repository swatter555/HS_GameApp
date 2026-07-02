using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M6 validation: the binary helicopter transit stand check (§11.8.9 — ground-AD-fire path). SV_helo =
    /// STAND_BASE + Experience_mod − Shock — the air stand check (§7.9.8) WITHOUT the (TS+MAN)/8 speed term
    /// (helo air stats are empty, §7A.14). Binary Hold/Abort on a 1d10; no rout/shatter. Shares STAND_BASE and
    /// the Shock clamp with the ground/air checks.
    /// </summary>
    [TestFixture]
    public class HeloTransitStandCheckTests
    {
        private static HeloTransitStandInput Helo(ExperienceLevel exp, int hpLost) => new HeloTransitStandInput
        {
            Experience = exp,
            HpLostThisMove = hpLost,
        };

        #region Stand value (§11.8.9)

        [Test]
        public void StandValue_TrainedUndamaged_IsBase()
        {
            // 6 + 0 − 0 = 6 — no speed term, unlike the air stand check
            Assert.AreEqual(6, HeloTransitStandCheck.ComputeStandValue(Helo(ExperienceLevel.Trained, 0)));
        }

        [Test]
        public void StandValue_AppliesShockFromHpLost()
        {
            // HpLost 8 → Shock ceil(8/4) = 2 → 6 + 0 − 2 = 4
            Assert.AreEqual(4, HeloTransitStandCheck.ComputeStandValue(Helo(ExperienceLevel.Trained, 8)));
        }

        [Test]
        public void StandValue_EliteAndRaw_ShiftByExperienceMod()
        {
            Assert.AreEqual(9, HeloTransitStandCheck.ComputeStandValue(Helo(ExperienceLevel.Elite, 0)), "Elite +3");
            Assert.AreEqual(4, HeloTransitStandCheck.ComputeStandValue(Helo(ExperienceLevel.Raw, 0)), "Raw −2");
        }

        [Test]
        public void StandValue_NoSpeedTerm_DiffersFromAirStandCheck()
        {
            // A fast helo's transit SV does NOT pick up the air check's floor((TS+MAN)/8) bonus.
            int helo = HeloTransitStandCheck.ComputeStandValue(Helo(ExperienceLevel.Trained, 0));
            int air = AirStandCheck.ComputeStandValue(new AirStandInput
            {
                TopSpeed = 10, Maneuverability = 12, Experience = ExperienceLevel.Trained, HpLostThisPass = 0
            });
            Assert.AreEqual(6, helo);
            Assert.AreEqual(8, air, "air check adds floor((10+12)/8) = 2");
            Assert.Less(helo, air);
        }

        [Test]
        public void StandValue_ShockClampsAtMax_CanGoNegative()
        {
            // HpLost 40 → ceil(40/4) = 10 → clamped to SHOCK_MAX (8) → 6 + 0 − 8 = −2 (always aborts)
            Assert.AreEqual(-2, HeloTransitStandCheck.ComputeStandValue(Helo(ExperienceLevel.Trained, 40)));
        }

        #endregion // Stand value

        #region Resolution (§11.8.9)

        [Test]
        public void ResolveStand_RollAtOrBelowSV_Holds()
        {
            Assert.AreEqual(HeloTransitOutcome.Hold, HeloTransitStandCheck.ResolveStand(6, new QueueRollRandom(6)));
        }

        [Test]
        public void ResolveStand_RollAboveSV_Aborts()
        {
            Assert.AreEqual(HeloTransitOutcome.Abort, HeloTransitStandCheck.ResolveStand(6, new QueueRollRandom(7)));
        }

        [Test]
        public void ResolveStand_NegativeSV_AlwaysAborts()
        {
            // SV −2: even a natural 1 exceeds it → Abort
            Assert.AreEqual(HeloTransitOutcome.Abort, HeloTransitStandCheck.ResolveStand(-2, new QueueRollRandom(1)));
        }

        #endregion // Resolution
    }
}
