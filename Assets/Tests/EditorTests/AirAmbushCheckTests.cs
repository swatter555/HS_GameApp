using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M6 validation: the fixed-wing air-ambush detection roll (§6.10.3/.4), extracted from SpottingService into a
    /// pure, seedable helper. 1d6 ≥ the experience-keyed threshold → detected (ambush averted). Covers the full
    /// per-tier table (Raw 16.7% [6] → Elite 100% [1–6]) and the boundary in both directions.
    /// </summary>
    [TestFixture]
    public class AirAmbushCheckTests
    {
        #region Detection threshold table (§6.10.4)

        [Test]
        public void DetectionThreshold_MatchesPerTierTable()
        {
            Assert.AreEqual(6, AirAmbushCheck.DetectionThreshold(ExperienceLevel.Raw), "Raw 16.7% — needs a 6");
            Assert.AreEqual(5, AirAmbushCheck.DetectionThreshold(ExperienceLevel.Green), "Green 33.3% — 5–6");
            Assert.AreEqual(4, AirAmbushCheck.DetectionThreshold(ExperienceLevel.Trained), "Trained 50% — 4–6");
            Assert.AreEqual(3, AirAmbushCheck.DetectionThreshold(ExperienceLevel.Experienced), "Experienced 66.7% — 3–6");
            Assert.AreEqual(2, AirAmbushCheck.DetectionThreshold(ExperienceLevel.Veteran), "Veteran 83.3% — 2–6");
            Assert.AreEqual(1, AirAmbushCheck.DetectionThreshold(ExperienceLevel.Elite), "Elite 100% — 1–6");
        }

        #endregion // Detection threshold table

        #region Roll resolution (§6.10.3)

        [Test]
        public void RollDetection_AtThreshold_Detects()
        {
            // Trained threshold 4: a 4 detects (ambush averted)
            Assert.IsTrue(AirAmbushCheck.RollDetection(ExperienceLevel.Trained, new QueueRollRandom(4)));
        }

        [Test]
        public void RollDetection_BelowThreshold_Misses()
        {
            // Trained threshold 4: a 3 fails to detect (the ambusher resolves its attack)
            Assert.IsFalse(AirAmbushCheck.RollDetection(ExperienceLevel.Trained, new QueueRollRandom(3)));
        }

        [Test]
        public void RollDetection_Raw_OnlyDetectsOnSix()
        {
            Assert.IsFalse(AirAmbushCheck.RollDetection(ExperienceLevel.Raw, new QueueRollRandom(5)), "5 misses");
            Assert.IsTrue(AirAmbushCheck.RollDetection(ExperienceLevel.Raw, new QueueRollRandom(6)), "6 detects");
        }

        [Test]
        public void RollDetection_Elite_AlwaysDetects()
        {
            // Threshold 1 — even a natural 1 clears it
            Assert.IsTrue(AirAmbushCheck.RollDetection(ExperienceLevel.Elite, new QueueRollRandom(1)));
        }

        #endregion // Roll resolution
    }
}
