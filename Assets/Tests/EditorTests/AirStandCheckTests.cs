using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M7 validation: the binary air-unit stand check (§7.9.8). Covers the SV_air assembly
    /// (STAND_BASE + Exp_mod + floor((TS+MAN)/8) − Shock), the shared Shock clamp, and the 1d10
    /// hold/disengage boundary. STAND_BASE is 6 per the design doc air formula.
    /// </summary>
    [TestFixture]
    public class AirStandCheckTests
    {
        private static AirStandInput Mig(ExperienceLevel exp, int hpLost) => new AirStandInput
        {
            TopSpeed = 10,
            Maneuverability = 12,
            Experience = exp,
            HpLostThisPass = hpLost,
        };

        #region Stand value (§7.9.8.2)

        [Test]
        public void StandValue_TrainedUndamaged_IsBasePlusSpeedTerm()
        {
            // 6 + 0 + floor((10+12)/8 = 2) − 0 = 8
            Assert.AreEqual(8, AirStandCheck.ComputeStandValue(Mig(ExperienceLevel.Trained, 0)));
        }

        [Test]
        public void StandValue_AppliesShockFromHpLost()
        {
            // HpLost 8 → Shock ceil(8/4) = 2 → 6 + 0 + 2 − 2 = 6
            Assert.AreEqual(6, AirStandCheck.ComputeStandValue(Mig(ExperienceLevel.Trained, 8)));
        }

        [Test]
        public void StandValue_EliteAndRaw_ShiftByExperienceMod()
        {
            Assert.AreEqual(11, AirStandCheck.ComputeStandValue(Mig(ExperienceLevel.Elite, 0)), "Elite +3");
            Assert.AreEqual(6, AirStandCheck.ComputeStandValue(Mig(ExperienceLevel.Raw, 0)), "Raw −2");
        }

        [Test]
        public void StandValue_HighMachSpeedTerm()
        {
            // TS 21 (high-mach), MAN 12 → floor(33/8) = 4 → 6 + 0 + 4 = 10
            var input = new AirStandInput { TopSpeed = 21, Maneuverability = 12, Experience = ExperienceLevel.Trained, HpLostThisPass = 0 };
            Assert.AreEqual(10, AirStandCheck.ComputeStandValue(input));
        }

        [Test]
        public void StandValue_ShockClampsAtMax()
        {
            // HpLost 40 → ceil(40/4) = 10 → clamped to SHOCK_MAX (8) → 6 + 0 + 2 − 8 = 0
            Assert.AreEqual(0, AirStandCheck.ComputeStandValue(Mig(ExperienceLevel.Trained, 40)));
        }

        #endregion // Stand value

        #region Resolution (§7.9.8.3)

        [Test]
        public void ResolveStand_RollAtOrBelowSV_Holds()
        {
            Assert.AreEqual(AirStandOutcome.Hold, AirStandCheck.ResolveStand(8, new QueueRollRandom(8)));
        }

        [Test]
        public void ResolveStand_RollAboveSV_Disengages()
        {
            Assert.AreEqual(AirStandOutcome.Disengage, AirStandCheck.ResolveStand(8, new QueueRollRandom(9)));
        }

        #endregion // Resolution
    }
}
