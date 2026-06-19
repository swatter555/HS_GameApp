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

        #region IFV / APC

        [Test]
        public void Ifv_ResolvesConvertedLines()
        {
            try
            {
                // Type 86 (BMP-1 copy + HJ-73 rail): Ifv + ATGM_RAIL (HA+4) + AMPHIBIOUS. Mirrors BMP-1P.
                AssertGround(WeaponType.IFV_TYPE86, 8, 4, 8, 7, 7, 0);
                Assert.IsTrue(P(WeaponType.IFV_TYPE86).HasCapability(WeaponCapability.Amphibious), "Type 86 amphibious");
                Assert.AreEqual(1.00f, P(WeaponType.IFV_TYPE86).ICM, 0.01f, "Type 86 ICM 1.00");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Ifv_ResolvesConvertedLines), ex); throw; }
        }

        #endregion // IFV / APC

        #region Artillery (SP / towed / rocket)

        [Test]
        public void Artillery_ResolveConvertedLines()
        {
            try
            {
                // Type 82 122mm SP howitzer: Artillery + SELF_PROPELLED (tracked) → MMP 10.
                AssertGround(WeaponType.SPA_TYPE82, 5, 7, 9, 7, 7, 0);
                Assert.AreEqual(10, (int)P(WeaponType.SPA_TYPE82).MaxMovementPoints, "Type 82 MMP (SELF_PROPELLED)");

                // PHZ-89 tracked MRL: SELF_PROPELLED + ROCKET_ARTILLERY → double-fire.
                AssertGround(WeaponType.ROC_PHZ89, 5, 7, 9, 7, 7, 0);
                Assert.IsTrue(P(WeaponType.ROC_PHZ89).HasCapability(WeaponCapability.RocketArtillery), "PHZ-89 rocket-artillery double-fire");

                // Light towed: bare Artillery archetype (foot, MMP 4), GAD 8.
                AssertGround(WeaponType.ART_LIGHT_CH, 5, 5, 9, 5, 8, 0);
                Assert.AreEqual(4, (int)P(WeaponType.ART_LIGHT_CH).MaxMovementPoints, "Lt towed MMP 4");

                // Heavy towed: Artillery + SA+1 (heavier tube), foot MMP 4.
                AssertGround(WeaponType.ART_HEAVY_CH, 5, 5, 10, 5, 8, 0);
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Artillery_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Artillery

        #region Air defense (AAA / SAM)

        [Test]
        public void AirDefense_ResolveConvertedLines()
        {
            try
            {
                // Type 53 SPAAA (twin 57mm, optical — no radar): Aaa + SELF_PROPELLED → GAT 11 (post-rebalance), GAD 11, MMP 10.
                AssertGround(WeaponType.SPAAA_TYPE53, 4, 6, 9, 8, 11, 11);
                Assert.AreEqual(10, (int)P(WeaponType.SPAAA_TYPE53).MaxMovementPoints, "Type 53 MMP (SELF_PROPELLED)");

                // HQ-7 mobile point SAM: Sam + SELF_PROPELLED + COMMAND_GUIDANCE (GAT+2) → GAT 14 (post-rebalance), SR 6.
                AssertGround(WeaponType.SPSAM_HQ7, 1, 5, 1, 5, 7, 14);
                Assert.AreEqual(6, (int)P(WeaponType.SPSAM_HQ7).SpottingRange, "HQ-7 SAM SR 6");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(AirDefense_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Air defense

        #region Helicopters

        [Test]
        public void Helicopter_ResolvesH9()
        {
            try
            {
                // H-9 light AT helo: Helicopter + ATGM_HELO_SACLOS (HA+4); unarmoured, no cannon.
                AssertGround(WeaponType.HEL_H9, 11, 6, 10, 7, 10, 0);
                Assert.AreEqual(1.00f, P(WeaponType.HEL_H9).ICM, 0.01f, "H-9 ICM 1.00");
                Assert.AreEqual(3, (int)P(WeaponType.HEL_H9).SpottingRange, "H-9 SR 3");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Helicopter_ResolvesH9), ex); throw; }
        }

        #endregion // Helicopters

        #region Jets

        [Test]
        public void Jets_ResolveConvertedLines()
        {
            try
            {
                // J-7 (MiG-21 copy): FighterEarly bare, GA Rule-A floor 2, W8 air SR 4.
                WeaponProfile j7 = P(WeaponType.FGT_J7);
                Assert.AreEqual(8, (int)j7.Dogfighting,   "J-7 DF");
                Assert.AreEqual(9, (int)j7.Maneuverability,"J-7 MAN");
                Assert.AreEqual(10,(int)j7.TopSpeed,      "J-7 TS");
                Assert.AreEqual(2, (int)j7.GroundAttack,  "J-7 GA floor 2");
                Assert.AreEqual(4, (int)j7.SpottingRange, "J-7 air SR 4 (W8)");

                // J-8 fast radar interceptor: FighterEarly + BVR (DF+2) + RWR (SUR+1) + TS+3 + HIGH_MACH_DASH;
                // agility stays early-gen, no look-down ICM (Chinese radar lag).
                WeaponProfile j8 = P(WeaponType.FGT_J8);
                Assert.AreEqual(10, (int)j8.Dogfighting, "J-8 DF (BVR radar interceptor)");
                Assert.AreEqual(13, (int)j8.TopSpeed,    "J-8 TS 13 (high-mach)");
                Assert.AreEqual(7,  (int)j8.Survivability,"J-8 SUR (RWR)");
                Assert.AreEqual(2,  (int)j8.GroundAttack,"J-8 GA floor 2");
                Assert.AreEqual(1.00f, j8.ICM, 0.01f,    "J-8 no radar-suite ICM");

                // Q-5 Fantan: Attack + DF-2 + TS+2; crude attacker, GA at archetype floor 10.
                WeaponProfile q5 = P(WeaponType.ATT_Q5);
                Assert.AreEqual(2,  (int)q5.Dogfighting, "Q-5 DF 2 (no air-to-air)");
                Assert.AreEqual(9,  (int)q5.TopSpeed,    "Q-5 TS 9 (supersonic)");
                Assert.AreEqual(10, (int)q5.GroundAttack,"Q-5 GA 10 (archetype floor, no AG traits)");

                // H-6 (Tu-16 copy): Bomber + CARPET_BOMBING + STRATEGIC_PAYLOAD → GA 9, OL 16, GaVsSoft 3.
                WeaponProfile h6 = P(WeaponType.BMB_H6);
                Assert.AreEqual(9,  (int)h6.GroundAttack,  "H-6 GA 9");
                Assert.AreEqual(16, (int)h6.OrdinanceLoad, "H-6 OL 16");
                Assert.AreEqual(3,  h6.GaBonusVsSoft,      "H-6 GaVsSoft 3");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Jets_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Jets

        #region Infantry

        [Test]
        public void Infantry_ResolveConvertedLines()
        {
            try
            {
                // PLA Regulars: RPG (HA 6) + Strela MANPADS (GAT floor 6), infantry GAD 10 (R1).
                AssertGround(WeaponType.INF_REG_CH, 6, 7, 7, 8, 10, 6);

                // PLA Airborne: same statline + air-droppable capability.
                AssertGround(WeaponType.INF_AB_CH, 6, 7, 7, 8, 10, 6);
                Assert.IsTrue(P(WeaponType.INF_AB_CH).HasCapability(WeaponCapability.AirDroppable), "PLA Airborne air-droppable");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Infantry_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Infantry
    }
}
