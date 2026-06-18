using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;
using System;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Phase 3 regression guard for the CHINESE faction rebuild (Archetype + Delta + Trait via
    /// <see cref="WeaponProfile.FromProfileDef"/>). Loads the live <see cref="WeaponProfileDB"/> and asserts the
    /// resolved statline of a representative profile per family as each Chinese batch lands. Mirrors the Soviet /
    /// NATO / Arab guards. Chinese kit is domestic (no EXPORT_DOWNGRADE) but its fire-control lags the West.
    /// </summary>
    [TestFixture]
    public class WeaponProfileChineseTests : BaseTestFixture
    {
        private const string CLASS_NAME = nameof(WeaponProfileChineseTests);

        #region Setup

        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
        }

        #endregion // Setup

        #region Helpers

        private static WeaponProfile P(WeaponType wt) => WeaponProfileDB.GetWeaponProfile(wt);

        /// <summary>Asserts the five ground combat stats + GAD + GAT for a profile.</summary>
        private static void AssertGround(WeaponType wt, int ha, int hd, int sa, int sd, int gad, int gat)
        {
            WeaponProfile p = P(wt);
            Assert.AreEqual(ha,  (int)p.HardAttack,       $"{wt} HA");
            Assert.AreEqual(hd,  (int)p.HardDefense,      $"{wt} HD");
            Assert.AreEqual(sa,  (int)p.SoftAttack,       $"{wt} SA");
            Assert.AreEqual(sd,  (int)p.SoftDefense,      $"{wt} SD");
            Assert.AreEqual(gad, (int)p.GroundAirDefense, $"{wt} GAD");
            Assert.AreEqual(gat, (int)p.GroundAirAttack,  $"{wt} GAT");
        }

        #endregion // Helpers

        #region MBTs

        [Test]
        public void Mbts_ResolveConvertedLines()
        {
            try
            {
                // Type 59 (T-54 copy): Gen1 + LOW_PROFILE (= T-55A line minus dormant NBC).
                AssertGround(WeaponType.TANK_TYPE59, 7, 6, 5, 7, 7, 0);
                Assert.AreEqual(1.00f, P(WeaponType.TANK_TYPE59).ICM, 0.01f, "Type 59 ICM 1.00");

                // Type 80 (105mm): Gen2 + LASER_RANGEFINDER (basic FCS, no thermal).
                AssertGround(WeaponType.TANK_TYPE80, 10, 8, 7, 6, 7, 0);
                Assert.AreEqual(1.05f, P(WeaponType.TANK_TYPE80).ICM, 0.01f, "Type 80 LRF ICM 1.05");

                // Type 95 (125mm): Gen3 + LRF + BC, no thermal → ICM 1.10 / SR 2 (below Western Gen3).
                AssertGround(WeaponType.TANK_TYPE95, 13, 11, 9, 6, 7, 0);
                Assert.AreEqual(1.10f, P(WeaponType.TANK_TYPE95).ICM, 0.01f, "Type 95 LRF+BC ICM 1.10");
                Assert.AreEqual(2, (int)P(WeaponType.TANK_TYPE95).SpottingRange, "Type 95 SR 2 (no thermal)");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Mbts_ResolveConvertedLines), ex); throw; }
        }

        #endregion // MBTs
    }
}
