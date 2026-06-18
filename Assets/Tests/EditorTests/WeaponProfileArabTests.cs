using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;
using System;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Phase 3 regression guard for the ARAB faction rebuild (Archetype + Delta + Trait via
    /// <see cref="WeaponProfile.FromProfileDef"/>). Loads the live <see cref="WeaponProfileDB"/> and asserts the
    /// resolved statline of a representative profile per family as each Arab batch lands. Mirrors
    /// <c>WeaponProfileSovietTests</c> / <c>WeaponProfileNatoTests</c>. The Iraqi T-55A/T-62A are export
    /// "monkey-models" (Soviet line + EXPORT_DOWNGRADE); the Iranian M60A3 is a real US tank (no downgrade).
    /// </summary>
    [TestFixture]
    public class WeaponProfileArabTests : BaseTestFixture
    {
        private const string CLASS_NAME = nameof(WeaponProfileArabTests);

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

        /// <summary>Asserts the air-combat block (DF/MAN/TS/SUR) + GA for an aircraft profile.</summary>
        private static void AssertAir(WeaponType wt, int df, int man, int ts, int sur, int ga)
        {
            WeaponProfile p = P(wt);
            Assert.AreEqual(df,  (int)p.Dogfighting,     $"{wt} DF");
            Assert.AreEqual(man, (int)p.Maneuverability, $"{wt} MAN");
            Assert.AreEqual(ts,  (int)p.TopSpeed,        $"{wt} TS");
            Assert.AreEqual(sur, (int)p.Survivability,   $"{wt} SUR");
            Assert.AreEqual(ga,  (int)p.GroundAttack,    $"{wt} GA");
        }

        #endregion // Helpers

        #region MBTs (export monkey-models + Iranian M60)

        [Test]
        public void Mbts_ResolveConvertedLines()
        {
            try
            {
                // Iraqi T-55A: Soviet Gen1 line (LOW_PROFILE + NBC_PROTECTED) + EXPORT_DOWNGRADE (HD-2/SD-1/ICM×0.9).
                AssertGround(WeaponType.TANK_T55A_IQ, 7, 4, 5, 6, 7, 0);
                Assert.AreEqual(0.90f, P(WeaponType.TANK_T55A_IQ).ICM, 0.01f, "T-55A_IQ export ICM 0.90");

                // Iraqi T-62A: same + HA+1 (115mm up-gun).
                AssertGround(WeaponType.TANK_T62A_IQ, 8, 4, 5, 6, 7, 0);
                Assert.AreEqual(0.90f, P(WeaponType.TANK_T62A_IQ).ICM, 0.01f, "T-62A_IQ export ICM 0.90");

                // Iranian M60A3: NATO M60A3 line minus THERMAL_IMAGER (no TTS post-1979) — LRF only, ICM 1.05, SR 2.
                AssertGround(WeaponType.TANK_M60A3_IR, 10, 8, 8, 6, 7, 0);
                Assert.AreEqual(1.05f, P(WeaponType.TANK_M60A3_IR).ICM, 0.01f, "M60A3_IR LRF-only ICM 1.05");
                Assert.AreEqual(2, (int)P(WeaponType.TANK_M60A3_IR).SpottingRange, "M60A3_IR SR 2 (no thermal)");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Mbts_ResolveConvertedLines), ex); throw; }
        }

        #endregion // MBTs

        #region IFV / APC

        [Test]
        public void IfvApc_ResolveConvertedLines()
        {
            try
            {
                // Iraqi BMP-1: Soviet BMP-1P line (Ifv + ATGM_RAIL + AMPHIBIOUS), no export downgrade.
                AssertGround(WeaponType.IFV_BMP1_IQ, 8, 4, 8, 7, 7, 0);
                Assert.IsTrue(P(WeaponType.IFV_BMP1_IQ).IsAmphibious, "BMP-1_IQ amphibious");
                Assert.AreEqual(10, (int)P(WeaponType.IFV_BMP1_IQ).MaxMovementPoints, "BMP-1_IQ MMP 10");

                // Iraqi MT-LB: Soviet MT-LB line (Apc + AMPHIBIOUS).
                AssertGround(WeaponType.APC_MTLB_IQ, 3, 4, 6, 7, 7, 0);
                Assert.IsTrue(P(WeaponType.APC_MTLB_IQ).IsAmphibious, "MT-LB_IQ amphibious");
                Assert.AreEqual(8, (int)P(WeaponType.APC_MTLB_IQ).MaxMovementPoints, "MT-LB_IQ MMP 8");

                // Iranian M113: bare NATO M113 line (real US APC, amphibious dropped to match NATO M113).
                AssertGround(WeaponType.APC_M113_IR, 3, 4, 6, 7, 7, 0);
                Assert.IsFalse(P(WeaponType.APC_M113_IR).IsAmphibious, "M113_IR non-amphibious (matches NATO M113)");
                Assert.AreEqual(8, (int)P(WeaponType.APC_M113_IR).MaxMovementPoints, "M113_IR MMP 8");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(IfvApc_ResolveConvertedLines), ex); throw; }
        }

        #endregion // IFV / APC

        #region Artillery

        [Test]
        public void Artillery_ResolveConvertedLines()
        {
            try
            {
                // Iraqi 2S1 (122mm SP): Artillery + IR SHORT + SELF_PROPELLED (= Soviet 2S1 line).
                AssertGround(WeaponType.SPA_2S1_IQ, 5, 7, 9, 7, 7, 0);
                Assert.AreEqual(10, (int)P(WeaponType.SPA_2S1_IQ).MaxMovementPoints, "2S1_IQ MMP 10 (SELF_PROPELLED)");
                Assert.AreEqual(4,  (int)P(WeaponType.SPA_2S1_IQ).IndirectRange, "2S1_IQ IR 4");

                // Light towed: bare Artillery, foot MMP 4, GAD 8 (= standard light towed).
                AssertGround(WeaponType.ART_LIGHT_ARAB, 5, 5, 9, 5, 8, 0);
                Assert.AreEqual(4, (int)P(WeaponType.ART_LIGHT_ARAB).MaxMovementPoints, "Arab Lt towed MMP 4");
                Assert.AreEqual(4, (int)P(WeaponType.ART_LIGHT_ARAB).IndirectRange, "Arab Lt towed IR 4");

                // Heavy towed: Artillery + SA+1, IR MEDIUM (= standard heavy towed).
                AssertGround(WeaponType.ART_HEAVY_ARAB, 5, 5, 10, 5, 8, 0);
                Assert.AreEqual(5, (int)P(WeaponType.ART_HEAVY_ARAB).IndirectRange, "Arab Hvy towed IR 5");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Artillery_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Artillery

        #region Air Defense

        [Test]
        public void AirDefense_ResolveConvertedLines()
        {
            try
            {
                // Iraqi ZSU-57-2: Soviet line (Aaa + SELF_PROPELLED), optical gun. GAT provisional (rebalance pass).
                AssertGround(WeaponType.SPAAA_ZSU57_IQ, 4, 6, 9, 8, 11, 9);
                Assert.AreEqual(10, (int)P(WeaponType.SPAAA_ZSU57_IQ).MaxMovementPoints, "ZSU-57_IQ MMP 10");

                // Iraqi 2K12 Kub: Soviet line (Sam + SELF_PROPELLED + SARH_LONG_RANGE + MOBILE_SHOOT_SCOOT).
                AssertGround(WeaponType.SPSAM_2K12_IQ, 1, 5, 1, 5, 7, 13);
                Assert.AreEqual(6, (int)P(WeaponType.SPSAM_2K12_IQ).IndirectRange, "2K12_IQ IR 6");

                // Mujahideen AAA: improvised, weaker GAT + non-dug-in GAD (invented MJ line).
                AssertGround(WeaponType.AAA_GEN_MJ, 3, 4, 8, 6, 10, 7);
                Assert.AreEqual(4, (int)P(WeaponType.AAA_GEN_MJ).MaxMovementPoints, "MJ AAA foot MMP 4");

                // Mujahideen Stinger team: air-only MANPADS, GAT 8, no radar (SR 2).
                AssertGround(WeaponType.SAM_GEN_MJ, 1, 3, 1, 3, 10, 8);
                Assert.AreEqual(2, (int)P(WeaponType.SAM_GEN_MJ).SpottingRange, "MJ Stinger SR 2 (no radar)");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(AirDefense_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Air Defense

        #region Jets

        [Test]
        public void Jets_ResolveConvertedLines()
        {
            try
            {
                // Iraqi MiG-21: export interceptor, a notch below the Soviet MiG-21 (DF-1/MAN-1). Pure → GA 2.
                AssertAir(WeaponType.FGT_MIG21_IQ, 7, 8, 10, 6, 2);

                // Iraqi MiG-23: export fighter, below the Soviet MiG-23. Pure → GA 2.
                AssertAir(WeaponType.FGT_MIG23_IQ, 10, 9, 11, 7, 2);

                // Iraqi Su-17: = Soviet Su-17 line (FighterEarly + MULTIROLE_STRIKE) → GA 6.
                AssertAir(WeaponType.ATT_SU17_IQ, 8, 9, 10, 7, 6);

                // Iranian F-4: real US Phantom, pure fighter → GA 2 (= US/FRG F-4).
                AssertAir(WeaponType.FGT_F4_IR, 9, 8, 11, 7, 2);

                // Iranian F-14: prized Tomcats, a notch below the US F-14 (DF12 vs 14). Pure → GA 2.
                AssertAir(WeaponType.FGT_F14_IR, 12, 11, 11, 8, 2);
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Jets_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Jets

        #region Trucks + Infantry (regular + Mujahideen)

        [Test]
        public void TrucksAndInfantry_ResolveConvertedLines()
        {
            try
            {
                // Arab truck: Truck + NON_COMBATANT (= Soviet/NATO generic truck).
                Assert.IsFalse(P(WeaponType.TRK_GEN_ARAB).IsAttackCapable, "Arab truck non-combatant");

                // Iraqi/Iranian regulars: Soviet regular line (RPG_LAW + MANPADS_BASIC Strela).
                AssertGround(WeaponType.INF_REG_IQ, 6, 7, 7, 8, 10, 6);
                AssertGround(WeaponType.INF_REG_IR, 6, 7, 7, 8, 10, 6);

                // Mujahideen regulars: RPG + MOUNTAIN_TRAINED, no MANPADS (GAT 0).
                AssertGround(WeaponType.INF_REG_MJ, 6, 7, 7, 8, 10, 0);
                Assert.IsTrue(P(WeaponType.INF_REG_MJ).HasCapability(WeaponCapability.MountainMovement), "MJ regulars mountain");

                // MJ elite: SPECIAL_FORCES (HA8/SA9/SD9/ICM1.10) + recon SR 3 + mountain.
                AssertGround(WeaponType.INF_SPEC_MJ, 8, 7, 9, 9, 10, 0);
                Assert.AreEqual(1.10f, P(WeaponType.INF_SPEC_MJ).ICM, 0.01f, "MJ elite SF ICM");
                Assert.AreEqual(3, (int)P(WeaponType.INF_SPEC_MJ).SpottingRange, "MJ elite SR 3");
                Assert.IsTrue(P(WeaponType.INF_SPEC_MJ).HasCapability(WeaponCapability.MountainMovement), "MJ elite mountain");

                // MJ cavalry: mounted MMP 10 + mountain.
                AssertGround(WeaponType.INF_CAV_MJ, 6, 7, 7, 8, 10, 0);
                Assert.AreEqual(10, (int)P(WeaponType.INF_CAV_MJ).MaxMovementPoints, "MJ cavalry MMP 10");
                Assert.IsTrue(P(WeaponType.INF_CAV_MJ).HasCapability(WeaponCapability.MountainMovement), "MJ cavalry mountain");

                // MJ RPG teams: AT specialists (HA8/SA6) + mountain.
                AssertGround(WeaponType.INF_RPG_MJ, 8, 7, 6, 8, 10, 0);

                // MJ artillery (invented improvised lines): short-range, dispersed (GAD 10).
                AssertGround(WeaponType.ART_MORTAR_MJ, 5, 5, 7, 5, 10, 0);
                Assert.AreEqual(3, (int)P(WeaponType.ART_MORTAR_MJ).IndirectRange, "MJ mortar IR 3");
                AssertGround(WeaponType.ART_LIGHT_MJ, 5, 5, 6, 5, 10, 0);
                Assert.AreEqual(3, (int)P(WeaponType.ART_LIGHT_MJ).IndirectRange, "MJ light arty IR 3");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(TrucksAndInfantry_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Trucks + Infantry
    }
}
