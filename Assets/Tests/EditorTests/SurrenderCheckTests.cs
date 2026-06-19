using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M2 validation: the Surrender Check (§7.9.6a) and Static catastrophic collapse (§7.9.7) — both pure
    /// experience rolls (no leader/command term, §7.9.6a.2.2).
    /// </summary>
    [TestFixture]
    public class SurrenderCheckTests
    {
        #region Surrender Check (§7.9.6a)

        [Test]
        public void SurrenderCheckNumber_MatchesTable()
        {
            Assert.AreEqual(14, SurrenderCheck.SurrenderCheckNumber(ExperienceLevel.Raw),         "Raw 14");
            Assert.AreEqual(12, SurrenderCheck.SurrenderCheckNumber(ExperienceLevel.Green),       "Green 12");
            Assert.AreEqual(10, SurrenderCheck.SurrenderCheckNumber(ExperienceLevel.Trained),     "Trained 10");
            Assert.AreEqual(8,  SurrenderCheck.SurrenderCheckNumber(ExperienceLevel.Experienced), "Experienced 8");
            Assert.AreEqual(6,  SurrenderCheck.SurrenderCheckNumber(ExperienceLevel.Veteran),     "Veteran 6");
            Assert.AreEqual(4,  SurrenderCheck.SurrenderCheckNumber(ExperienceLevel.Elite),       "Elite 4");
        }

        [Test]
        public void ResolveSurrender_AboveCheckHolds_AtOrBelowDestroys()
        {
            // Trained check = 10.
            Assert.AreEqual(SurrenderOutcome.HoldInPlace, SurrenderCheck.ResolveSurrender(ExperienceLevel.Trained, new QueueRollRandom(11)), "11 > 10 → HoldInPlace");
            Assert.AreEqual(SurrenderOutcome.Destroyed,   SurrenderCheck.ResolveSurrender(ExperienceLevel.Trained, new QueueRollRandom(10)), "10 = check → Destroyed");
            Assert.AreEqual(SurrenderOutcome.Destroyed,   SurrenderCheck.ResolveSurrender(ExperienceLevel.Trained, new QueueRollRandom(1)),  "1 → Destroyed");
            // Elite check = 4.
            Assert.AreEqual(SurrenderOutcome.HoldInPlace, SurrenderCheck.ResolveSurrender(ExperienceLevel.Elite, new QueueRollRandom(5)), "5 > 4 → HoldInPlace");
            Assert.AreEqual(SurrenderOutcome.Destroyed,   SurrenderCheck.ResolveSurrender(ExperienceLevel.Elite, new QueueRollRandom(4)), "4 = check → Destroyed");
        }

        #endregion // Surrender Check

        #region Static Catastrophic Collapse (§7.9.7)

        [Test]
        public void StaticCollapseThreshold_MatchesTable()
        {
            Assert.AreEqual(40, SurrenderCheck.StaticCollapseThreshold(ExperienceLevel.Raw),         "Raw 40");
            Assert.AreEqual(35, SurrenderCheck.StaticCollapseThreshold(ExperienceLevel.Green),       "Green 35");
            Assert.AreEqual(30, SurrenderCheck.StaticCollapseThreshold(ExperienceLevel.Trained),     "Trained 30");
            Assert.AreEqual(25, SurrenderCheck.StaticCollapseThreshold(ExperienceLevel.Experienced), "Experienced 25");
            Assert.AreEqual(20, SurrenderCheck.StaticCollapseThreshold(ExperienceLevel.Veteran),     "Veteran 20");
            Assert.AreEqual(15, SurrenderCheck.StaticCollapseThreshold(ExperienceLevel.Elite),       "Elite 15");
        }

        [Test]
        public void ResolveStaticCollapse_AtOrBelowThresholdDestroys()
        {
            // Trained threshold = 30.
            Assert.IsTrue(SurrenderCheck.ResolveStaticCollapse(ExperienceLevel.Trained, new QueueRollRandom(30)),  "30 ≤ 30 → destroyed");
            Assert.IsFalse(SurrenderCheck.ResolveStaticCollapse(ExperienceLevel.Trained, new QueueRollRandom(31)), "31 > 30 → survives");
            // Raw threshold = 40.
            Assert.IsTrue(SurrenderCheck.ResolveStaticCollapse(ExperienceLevel.Raw, new QueueRollRandom(40)),  "40 ≤ 40 → destroyed");
            Assert.IsFalse(SurrenderCheck.ResolveStaticCollapse(ExperienceLevel.Raw, new QueueRollRandom(41)), "41 > 40 → survives");
        }

        #endregion // Static Catastrophic Collapse
    }
}
