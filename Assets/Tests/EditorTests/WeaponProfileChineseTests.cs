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
    ///
    /// ⚠ SINCE RE-4 (2026-09-02) EVERY ICM PIN HERE CARRIES THE ×0.9 SECOND_LINE_FORMATION FACTOR.
    /// A Chinese profile's ICM is therefore 0.90 unless it also has a fire-control trait (only the Type 80's
    /// laser rangefinder does: 1.05 × 0.9 = 0.945). The lone exception is the Type 86 IFV at 1.00 — it is a
    /// Mobile-bay ride, and closed-bay doctrine prices the formation on the deployed profile only.
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
                AssertGround(WeaponType.TANK_TYPE59_CH, 7, 6, 5, 7, 7, 0);
                Assert.AreEqual(0.90f, P(WeaponType.TANK_TYPE59_CH).ICM, 0.01f, "Type 59 ICM 0.90 (second line)");

                // Type 80 (105mm): Gen2 + LASER_RANGEFINDER (basic FCS, no thermal).
                AssertGround(WeaponType.TANK_TYPE80_CH, 10, 8, 7, 6, 7, 0);
                Assert.AreEqual(0.945f, P(WeaponType.TANK_TYPE80_CH).ICM, 0.01f,
                    "Type 80 LRF 1.05 × second-line 0.9");
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
                AssertGround(WeaponType.IFV_TYPE86_CH, 8, 4, 8, 7, 7, 0);
                Assert.IsTrue(P(WeaponType.IFV_TYPE86_CH).HasCapability(WeaponCapability.Amphibious), "Type 86 amphibious");
                // ⚠ THE ONE CHINESE PROFILE WITHOUT SECOND_LINE_FORMATION. The Type 86 is the Mobile-bay
                // ride of the mechanised regiment, never a unit's sole profile, so the formation ICM would
                // be charged twice if it were here. This pin is the guard against that regression.
                Assert.AreEqual(1.00f, P(WeaponType.IFV_TYPE86_CH).ICM, 0.01f,
                    "Type 86 ICM 1.00 — Mobile-bay ride carries NO formation trait");
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
                AssertGround(WeaponType.SPA_TYPE83_CH, 5, 7, 9, 7, 7, 0);
                Assert.AreEqual(10, (int)P(WeaponType.SPA_TYPE83_CH).MaxMovementPoints, "Type 82 MMP (SELF_PROPELLED)");

                // PHZ-89 tracked MRL: SELF_PROPELLED + ROCKET_ARTILLERY → double-fire.
                AssertGround(WeaponType.ROC_PHZ89_CH, 5, 7, 9, 7, 7, 0);
                Assert.IsTrue(P(WeaponType.ROC_PHZ89_CH).HasCapability(WeaponCapability.RocketArtillery), "PHZ-89 rocket-artillery double-fire");

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
                AssertGround(WeaponType.SPAAA_TYPE53_CH, 4, 6, 9, 8, 11, 11);
                Assert.AreEqual(10, (int)P(WeaponType.SPAAA_TYPE53_CH).MaxMovementPoints, "Type 53 MMP (SELF_PROPELLED)");

                // HQ-7 mobile point SAM: Sam + SELF_PROPELLED + COMMAND_GUIDANCE (GAT+2) → GAT 14 (post-rebalance), SR 6.
                AssertGround(WeaponType.SPSAM_HQ7_CH, 1, 5, 1, 5, 7, 14);
                Assert.AreEqual(6, (int)P(WeaponType.SPSAM_HQ7_CH).SpottingRange, "HQ-7 SAM SR 6");
                // §11.8.2d envelope — Crotale clone, rides the point-defense band (Bob, 2026-08-22).
                Assert.AreEqual(4, (int)P(WeaponType.SPSAM_HQ7_CH).IndirectRange, "HQ-7 IR 4 (point defense)");
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
                AssertGround(WeaponType.HEL_Z9_CH, 11, 6, 10, 7, 10, 0);
                Assert.AreEqual(0.90f, P(WeaponType.HEL_Z9_CH).ICM, 0.01f, "Z-9 ICM 0.90 (second line)");
                Assert.AreEqual(3, (int)P(WeaponType.HEL_Z9_CH).SpottingRange, "H-9 SR 3");
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
                WeaponProfile j7 = P(WeaponType.FGT_J7_CH);
                Assert.AreEqual(8, (int)j7.Dogfighting,   "J-7 DF");
                Assert.AreEqual(9, (int)j7.Maneuverability,"J-7 MAN");
                Assert.AreEqual(10,(int)j7.TopSpeed,      "J-7 TS");
                Assert.AreEqual(2, (int)j7.GroundAttack,  "J-7 GA floor 2");
                Assert.AreEqual(4, (int)j7.SpottingRange, "J-7 air SR 4 (W8)");

                // J-8 fast radar interceptor: FighterEarly + BVR (DF+2) + RWR (SUR+1) + TS+3 + HIGH_MACH_DASH;
                // agility stays early-gen, no look-down ICM (Chinese radar lag).
                WeaponProfile j8 = P(WeaponType.FGT_J8_CH);
                Assert.AreEqual(10, (int)j8.Dogfighting, "J-8 DF (BVR radar interceptor)");
                Assert.AreEqual(13, (int)j8.TopSpeed,    "J-8 TS 13 (high-mach)");
                Assert.AreEqual(7,  (int)j8.Survivability,"J-8 SUR (RWR)");
                Assert.AreEqual(2,  (int)j8.GroundAttack,"J-8 GA floor 2");
                Assert.AreEqual(0.90f, j8.ICM, 0.01f,    "J-8 no radar-suite ICM, second line 0.90");

                // Q-5 Fantan: Attack + DF-2 + TS+2; crude attacker, GA at archetype floor 10.
                WeaponProfile q5 = P(WeaponType.ATT_Q5_CH);
                Assert.AreEqual(2,  (int)q5.Dogfighting, "Q-5 DF 2 (no air-to-air)");
                Assert.AreEqual(9,  (int)q5.TopSpeed,    "Q-5 TS 9 (supersonic)");
                Assert.AreEqual(10, (int)q5.GroundAttack,"Q-5 GA 10 (archetype floor, no AG traits)");

                // H-6 (Tu-16 copy): Bomber + CARPET_BOMBING + STRATEGIC_PAYLOAD → GA 9, OL 16, GaVsSoft 3.
                WeaponProfile h6 = P(WeaponType.BMB_H6_CH);
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

        #region Formation quality (RE-4)

        /// <summary>
        /// The SECOND_LINE_FORMATION sweep: every Chinese profile that a template names in its DEPLOYED
        /// bay must carry the x0.9 formation factor, and the one Mobile-bay ride must not. Enumerated
        /// rather than looped (house rule: no loops in Editor tests) so a failure names the offender.
        /// </summary>
        [Test]
        public void FormationQuality_SecondLineAppliesToDeployedProfilesOnly()
        {
            try
            {
                // Baseline 0.90 = 1.00 x 0.9. Thirteen of the fourteen deployed profiles have no
                // fire-control trait, so they land exactly on the factor.
                Assert.AreEqual(0.90f, P(WeaponType.TANK_TYPE59_CH).ICM,   0.01f, "Type 59");
                Assert.AreEqual(0.90f, P(WeaponType.SPA_TYPE83_CH).ICM,    0.01f, "Type 83 SPH");
                Assert.AreEqual(0.90f, P(WeaponType.ROC_PHZ89_CH).ICM,     0.01f, "PHZ-89");
                Assert.AreEqual(0.90f, P(WeaponType.ART_LIGHT_CH).ICM,     0.01f, "Light towed");
                Assert.AreEqual(0.90f, P(WeaponType.ART_HEAVY_CH).ICM,     0.01f, "Heavy towed");
                Assert.AreEqual(0.90f, P(WeaponType.SPAAA_TYPE53_CH).ICM,  0.01f, "Type 53 SPAAA");
                Assert.AreEqual(0.90f, P(WeaponType.SPSAM_HQ7_CH).ICM,     0.01f, "HQ-7");
                Assert.AreEqual(0.90f, P(WeaponType.HEL_Z9_CH).ICM,        0.01f, "Z-9");
                Assert.AreEqual(0.90f, P(WeaponType.FGT_J7_CH).ICM,        0.01f, "J-7");
                Assert.AreEqual(0.90f, P(WeaponType.FGT_J8_CH).ICM,        0.01f, "J-8");
                Assert.AreEqual(0.90f, P(WeaponType.ATT_Q5_CH).ICM,        0.01f, "Q-5");
                Assert.AreEqual(0.90f, P(WeaponType.BMB_H6_CH).ICM,        0.01f, "H-6");
                Assert.AreEqual(0.90f, P(WeaponType.INF_REG_CH).ICM,       0.01f, "PLA Regulars");
                Assert.AreEqual(0.90f, P(WeaponType.INF_AB_CH).ICM,        0.01f, "PLA Airborne");

                // The fourteenth stacks its rangefinder on top: 1.05 x 0.9. Pinned separately because it
                // proves the factor MULTIPLIES the fire-control stack rather than replacing it.
                Assert.AreEqual(0.945f, P(WeaponType.TANK_TYPE80_CH).ICM, 0.01f, "Type 80 = 1.05 x 0.9");

                // And the exception, restated here so this test alone documents the doctrine.
                Assert.AreEqual(1.00f, P(WeaponType.IFV_TYPE86_CH).ICM, 0.01f,
                    "Type 86 is a Mobile-bay ride - no formation trait");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(FormationQuality_SecondLineAppliesToDeployedProfilesOnly), ex); throw; }
        }

        /// <summary>
        /// The trait is CHINA-ONLY as of RE-4. Spot-checks one profile from each other faction that would
        /// be the obvious next candidate (or an accidental casualty of a blanket apply), because the shared
        /// commodity blueprints now take national traits and a stray argument would leak the factor.
        /// </summary>
        [Test]
        public void FormationQuality_SecondLineIsChinaOnly()
        {
            try
            {
                // The commodity blueprints are shared. If SECOND_LINE_FORMATION leaked into one of them,
                // every nation's towed gun would drop to 0.90 - these three are the tripwire.
                Assert.AreEqual(1.00f, P(WeaponType.ART_LIGHT_SV).ICM,   0.01f, "Soviet light towed unaffected");
                Assert.AreEqual(1.00f, P(WeaponType.ART_LIGHT_NATO).ICM, 0.01f, "NATO light towed unaffected");
                Assert.AreEqual(1.00f, P(WeaponType.ART_HEAVY_SV).ICM,   0.01f, "Soviet heavy towed unaffected");

                // Iraq and Iran are the named future candidates - they must NOT have it yet.
                Assert.AreEqual(1.00f, P(WeaponType.INF_REG_IQ).ICM, 0.01f, "Iraqi infantry not yet second line");
                Assert.AreEqual(1.00f, P(WeaponType.INF_REG_IR).ICM, 0.01f, "Iranian infantry not yet second line");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(FormationQuality_SecondLineIsChinaOnly), ex); throw; }
        }

        #endregion // Formation quality
    }
}
