using HammerAndSickle.Core.GameData;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Validation of the dual-domain spotting ranges (§12.3): a spotter uses its GROUND range against ground
    /// targets and its (often longer) AIR range against airborne targets. The crux: air-defence platforms have
    /// long AIR-search ranges but only the basic ground reach. Pure static rule tables — no unit construction.
    /// </summary>
    [TestFixture]
    public class SpottingRangeTests
    {
        #region Ground-domain ranges

        [Test]
        public void GroundSpottingRange_PerClassification()
        {
            Assert.AreEqual(2, GameData.GroundSpottingRange(UnitClassification.TANK), "ground combat = 2");
            Assert.AreEqual(3, GameData.GroundSpottingRange(UnitClassification.RECON), "recon ground = 3");
            Assert.AreEqual(2, GameData.GroundSpottingRange(UnitClassification.AAA), "AD ground reach is basic 2");
            Assert.AreEqual(2, GameData.GroundSpottingRange(UnitClassification.SAM), "AD ground reach is basic 2");
            Assert.AreEqual(4, GameData.GroundSpottingRange(UnitClassification.HQ), "facilities are long-range ground spotters");
            Assert.AreEqual(4, GameData.GroundSpottingRange(UnitClassification.AIRB));
            Assert.AreEqual(2, GameData.GroundSpottingRange(UnitClassification.FGT), "fixed-wing ground look = 2");
            Assert.AreEqual(8, GameData.GroundSpottingRange(UnitClassification.RECONA), "recon aircraft = long ground reach");
            Assert.AreEqual(8, GameData.GroundSpottingRange(UnitClassification.AWACS));
        }

        #endregion // Ground-domain ranges

        #region Air-domain ranges

        [Test]
        public void AirSpottingRange_PerClassification()
        {
            Assert.AreEqual(2, GameData.AirSpottingRange(UnitClassification.TANK));
            Assert.AreEqual(3, GameData.AirSpottingRange(UnitClassification.RECON));
            Assert.AreEqual(3, GameData.AirSpottingRange(UnitClassification.AAA), "AAA air-search = 3");
            Assert.AreEqual(6, GameData.AirSpottingRange(UnitClassification.SAM), "SAM air-search = 6");
            Assert.AreEqual(4, GameData.AirSpottingRange(UnitClassification.HQ));
            Assert.AreEqual(4, GameData.AirSpottingRange(UnitClassification.FGT));
            Assert.AreEqual(4, GameData.AirSpottingRange(UnitClassification.RECONA), "recon aircraft air-search = 4");
            Assert.AreEqual(12, GameData.AirSpottingRange(UnitClassification.AWACS), "AWACS = the air picture, 12");
        }

        [Test]
        public void AdPlatform_LongAir_ShortGround()
        {
            // The crux of the rule: a SAM detects aircraft far but reveals ground units only at the basic 2.
            Assert.AreEqual(2, GameData.GroundSpottingRange(UnitClassification.SAM));
            Assert.AreEqual(6, GameData.AirSpottingRange(UnitClassification.SAM));
            Assert.Greater(GameData.AirSpottingRange(UnitClassification.SAM), GameData.GroundSpottingRange(UnitClassification.SAM));
        }

        #endregion // Air-domain ranges

        #region Airborne-target classification

        [Test]
        public void IsAirborneClassification_FixedWing_True()
        {
            Assert.IsTrue(GameData.IsAirborneClassification(UnitClassification.FGT));
            Assert.IsTrue(GameData.IsAirborneClassification(UnitClassification.ATT));
            Assert.IsTrue(GameData.IsAirborneClassification(UnitClassification.BMB));
            Assert.IsTrue(GameData.IsAirborneClassification(UnitClassification.RECONA));
            Assert.IsTrue(GameData.IsAirborneClassification(UnitClassification.AWACS));
            Assert.IsTrue(GameData.IsAirborneClassification(UnitClassification.WW));
            Assert.IsTrue(GameData.IsAirborneClassification(UnitClassification.TRN));
        }

        [Test]
        public void IsAirborneClassification_GroundClassesAndNoeHelo_False()
        {
            Assert.IsFalse(GameData.IsAirborneClassification(UnitClassification.TANK));
            Assert.IsFalse(GameData.IsAirborneClassification(UnitClassification.SAM));
            Assert.IsFalse(GameData.IsAirborneClassification(UnitClassification.RECON));
            Assert.IsFalse(GameData.IsAirborneClassification(UnitClassification.HELO), "attack helo flies NOE → ground-spotted");
            // A dismounted AM/MAM is ground by class; the EmbarkedHelo air-assault case is instance-level (CombatUnit).
            Assert.IsFalse(GameData.IsAirborneClassification(UnitClassification.AM));
            Assert.IsFalse(GameData.IsAirborneClassification(UnitClassification.MAM));
        }

        #endregion // Airborne-target classification
    }
}
