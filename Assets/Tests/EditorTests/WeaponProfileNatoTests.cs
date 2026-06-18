using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;
using System;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Phase 3 regression guard for the NATO faction rebuild (Archetype + Delta + Trait via
    /// <see cref="WeaponProfile.FromProfileDef"/>). Loads the live <see cref="WeaponProfileDB"/> and asserts the
    /// resolved statline of a representative profile per family as each NATO batch lands. Mirrors
    /// <c>WeaponProfileSovietTests</c>. NATO tanks (Batch 2) are already guarded by WeaponProfileFactoryTests.
    /// </summary>
    [TestFixture]
    public class WeaponProfileNatoTests : BaseTestFixture
    {
        private const string CLASS_NAME = nameof(WeaponProfileNatoTests);

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
            Assert.AreEqual(df,  (int)p.Dogfighting,    $"{wt} DF");
            Assert.AreEqual(man, (int)p.Maneuverability, $"{wt} MAN");
            Assert.AreEqual(ts,  (int)p.TopSpeed,       $"{wt} TS");
            Assert.AreEqual(sur, (int)p.Survivability,  $"{wt} SUR");
            Assert.AreEqual(ga,  (int)p.GroundAttack,   $"{wt} GA");
        }

        #endregion // Helpers

        #region Batch A — IFV / APC

        [Test]
        public void Ifv_ResolveConvertedLines()
        {
            try
            {
                // M2 Bradley: Ifv + ATGM_RAIL (TOW) + AUTOCANNON_LIGHT (25mm).
                AssertGround(WeaponType.IFV_M2_US, 8, 4, 9, 7, 7, 0);
                Assert.AreEqual(10, (int)P(WeaponType.IFV_M2_US).MaxMovementPoints, "M2 MMP 10");

                // Warrior: Ifv + AUTOCANNON_HEAVY (30mm RARDEN); no vehicle ATGM.
                AssertGround(WeaponType.IFV_WARRIOR_UK, 5, 4, 9, 7, 7, 0);

                // Marder: Ifv + ATGM_RAIL (MILAN) + AUTOCANNON_LIGHT (20mm).
                AssertGround(WeaponType.IFV_MARDER_GE, 8, 4, 9, 7, 7, 0);
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Ifv_ResolveConvertedLines), ex); throw; }
        }

        [Test]
        public void Apc_ResolveConvertedLines()
        {
            try
            {
                // M113: bare Apc archetype (tracked), MMP 8.
                AssertGround(WeaponType.APC_M113_US, 3, 4, 6, 7, 7, 0);
                Assert.AreEqual(8, (int)P(WeaponType.APC_M113_US).MaxMovementPoints, "M113 MMP 8 (APC baseline)");

                // Humvee: Apc + THIN_TOP (soft-skin GAD 6).
                AssertGround(WeaponType.APC_HUMVEE_US, 3, 4, 6, 7, 6, 0);

                // LVTP-7: Apc + AMPHIBIOUS.
                AssertGround(WeaponType.APC_LVTP7_US, 3, 4, 6, 7, 7, 0);
                Assert.IsTrue(P(WeaponType.APC_LVTP7_US).IsAmphibious, "LVTP-7 amphibious");

                // VAB: bare Apc (wheeled).
                AssertGround(WeaponType.APC_VAB_FR, 3, 4, 6, 7, 7, 0);
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Apc_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Batch A — IFV / APC

        #region Batch B — Recon

        [Test]
        public void Recon_ResolveConvertedLines()
        {
            try
            {
                // All four are HARD armored-car/cavalry scouts (§7A.5 override), NOT RECON_FRAGILE.
                // M3 Bradley CFV: Recon + ATGM_RAIL (TOW) + AUTOCANNON_LIGHT (25mm).
                AssertGround(WeaponType.RCN_M3_US, 6, 5, 6, 9, 7, 0);
                Assert.AreEqual(TargetClass.Hard, P(WeaponType.RCN_M3_US).TargetClass, "M3 Hard");
                Assert.AreEqual(3, (int)P(WeaponType.RCN_M3_US).SpottingRange, "M3 SR 3");
                Assert.AreEqual(1.00f, P(WeaponType.RCN_M3_US).ICM, 0.01f, "M3 not fragile");

                // Luchs: Recon + AUTOCANNON_LIGHT (20mm) + AMPHIBIOUS.
                AssertGround(WeaponType.RCN_LUCHS_GE, 2, 5, 6, 9, 7, 0);
                Assert.AreEqual(TargetClass.Hard, P(WeaponType.RCN_LUCHS_GE).TargetClass, "Luchs Hard");
                Assert.IsTrue(P(WeaponType.RCN_LUCHS_GE).IsAmphibious, "Luchs amphibious");

                // FV105: Recon + AUTOCANNON_HEAVY (30mm RARDEN).
                AssertGround(WeaponType.RCN_FV105_UK, 3, 5, 6, 9, 7, 0);
                Assert.AreEqual(TargetClass.Hard, P(WeaponType.RCN_FV105_UK).TargetClass, "FV105 Hard");

                // ERC-90: Recon + residual HA+4 (90mm gun).
                AssertGround(WeaponType.RCN_ERC90_FR, 6, 5, 5, 9, 7, 0);
                Assert.AreEqual(TargetClass.Hard, P(WeaponType.RCN_ERC90_FR).TargetClass, "ERC-90 Hard");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Recon_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Batch B — Recon

        #region Batch C — SP-arty / towed / rocket

        [Test]
        public void Artillery_ResolveConvertedLines()
        {
            try
            {
                // M109 (155mm SP): Artillery + SELF_PROPELLED + SA+1 (= the 2S3 line). All 4 nations identical.
                AssertGround(WeaponType.SPA_M109_US, 5, 7, 10, 7, 7, 0);
                Assert.AreEqual(10, (int)P(WeaponType.SPA_M109_US).MaxMovementPoints, "M109 MMP 10 (SELF_PROPELLED)");
                Assert.AreEqual(5,  (int)P(WeaponType.SPA_M109_US).IndirectRange, "M109 IR 5");
                AssertGround(WeaponType.SPA_M109_UK, 5, 7, 10, 7, 7, 0); // guard the export variants resolve the same

                // Light towed (105mm): bare Artillery, foot MMP 4, GAD 8.
                AssertGround(WeaponType.ART_LIGHT_WEST, 5, 5, 9, 5, 8, 0);
                Assert.AreEqual(4, (int)P(WeaponType.ART_LIGHT_WEST).MaxMovementPoints, "Lt towed MMP 4");

                // Heavy towed (155mm): Artillery + SA+1.
                AssertGround(WeaponType.ART_HEAVY_WEST, 5, 5, 10, 5, 8, 0);

                // MLRS: tracked rocket artillery — SELF_PROPELLED + ROCKET_ARTILLERY + SMART_MUNITION (analog of BM-27).
                AssertGround(WeaponType.ROC_MLRS_US, 8, 7, 11, 7, 7, 0);
                Assert.AreEqual(6, (int)P(WeaponType.ROC_MLRS_US).IndirectRange, "MLRS IR 6");
                Assert.IsTrue(P(WeaponType.ROC_MLRS_US).IsDoubleFire, "MLRS rocket-artillery double-fire");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Artillery_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Batch C — SP-arty / towed / rocket

        #region Batch D — Air Defense

        [Test]
        public void AirDefense_ResolveConvertedLines()
        {
            try
            {
                // Guns (Aaa archetype + SELF_PROPELLED). GAT provisional (pending GAT rebalance).
                AssertGround(WeaponType.SPAAA_M163_US, 4, 6, 9, 8, 11, 9);   // optical Vulcan (= ZSU-57-2)
                AssertGround(WeaponType.SPSAM_GEPARD_GE, 4, 6, 9, 8, 11, 11); // radar 35mm (= ZSU-23-4); SPSAM-classified gun
                AssertGround(WeaponType.SPAAA_ROLAND_FR, 4, 6, 9, 8, 11, 11); // SPAAA-classified missile (AAA stat line)

                // SP SAMs (Sam archetype + SELF_PROPELLED), air-only HA/SA 1.
                AssertGround(WeaponType.SPSAM_CHAP_US, 1, 5, 1, 5, 7, 11);    // IR fire-and-forget
                AssertGround(WeaponType.SPSAM_CROTALE_FR, 1, 5, 1, 5, 7, 12); // command
                AssertGround(WeaponType.SPSAM_RAPIER_UK, 1, 5, 1, 5, 7, 12);  // SACLOS

                // Hawk: static medium SARH SAM (= NATO S-75), MMP 0.
                AssertGround(WeaponType.SAM_HAWK_US, 1, 3, 1, 3, 8, 13);
                Assert.AreEqual(0, (int)P(WeaponType.SAM_HAWK_US).MaxMovementPoints, "Hawk static MMP 0");
                Assert.AreEqual(10, (int)P(WeaponType.SPSAM_CHAP_US).MaxMovementPoints, "Chaparral SP MMP 10");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(AirDefense_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Batch D — Air Defense

        #region Batch E — Helicopters

        [Test]
        public void Helicopters_ResolveConvertedLines()
        {
            try
            {
                // AH-64 Apache: apex gunship (FNF Hellfire + cannon + rockets + armor + countermeasures), = Mi-28 line.
                AssertGround(WeaponType.HEL_AH64_US, 12, 7, 13, 8, 12, 0);
                Assert.AreEqual(1.05f, P(WeaponType.HEL_AH64_US).ICM, 0.01f, "AH-64 FNF ICM");
                Assert.AreEqual(3, (int)P(WeaponType.HEL_AH64_US).SpottingRange, "AH-64 SR 3");

                // AH-1 Cobra: SACLOS TOW + cannon + rockets, = Mi-24D minus armor.
                AssertGround(WeaponType.HEL_AH1, 11, 6, 13, 7, 10, 0);

                // Bo-105: light AT helo (HOT only).
                AssertGround(WeaponType.HEL_BO105_GE, 11, 6, 10, 7, 10, 0);

                // UH-60 Black Hawk: non-combatant lift + helo-transport category.
                Assert.IsFalse(P(WeaponType.HEL_UH60_US).IsAttackCapable, "UH-60 non-combatant");
                Assert.AreEqual(TransportCategory.HeloTransport, P(WeaponType.HEL_UH60_US).TransportCategory, "UH-60 helo transport");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Helicopters_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Batch E — Helicopters

        #region Batch F — Infantry

        [Test]
        public void Infantry_ResolveConvertedLines()
        {
            try
            {
                // All NATO infantry = Infantry archetype (5/7/7/8 GAD10 MMP4) + RPG_LAW (HA+1) + an ATGM trait.
                // Regular/Marine carry ATGM_LIGHT (HA8); Airborne/Air-Mobile carry ATGM_MEDIUM (HA9, light forces lean on ATGM).
                // MANPADS: US/FRG/FR = Stinger-class (GAT 8 + ICM 1.05 + FNF); UK = MANPADS_BASIC (Blowpipe, GAT 6, no ICM).

                // --- US (Stinger) ---
                AssertGround(WeaponType.INF_REG_US, 8, 7, 7, 8, 10, 8);
                Assert.AreEqual(1.05f, P(WeaponType.INF_REG_US).ICM, 0.01f, "US Regulars Stinger ICM");
                Assert.IsTrue(P(WeaponType.INF_REG_US).HasCapability(WeaponCapability.EngageAir), "US Regulars engage-air");
                Assert.IsTrue(P(WeaponType.INF_REG_US).HasCapability(WeaponCapability.FireAndForget), "US Regulars Stinger FNF");

                AssertGround(WeaponType.INF_MAR_US, 8, 7, 7, 8, 10, 8);
                Assert.IsTrue(P(WeaponType.INF_MAR_US).IsAmphibious, "US Marines amphibious");

                AssertGround(WeaponType.INF_AB_US, 9, 7, 7, 8, 10, 8);
                Assert.IsTrue(P(WeaponType.INF_AB_US).HasCapability(WeaponCapability.AirDroppable), "US Airborne air-droppable");

                AssertGround(WeaponType.INF_AM_US, 9, 7, 7, 8, 10, 8);
                Assert.IsTrue(P(WeaponType.INF_AM_US).HasCapability(WeaponCapability.MountainMovement), "US Air-Mobile mountain movement");

                // --- UK (MANPADS_BASIC — deliberately weaker AD: GAT 6, ICM 1.00) ---
                AssertGround(WeaponType.INF_REG_UK, 8, 7, 7, 8, 10, 6);
                Assert.AreEqual(1.00f, P(WeaponType.INF_REG_UK).ICM, 0.01f, "UK Regulars no Stinger ICM");
                AssertGround(WeaponType.INF_AB_UK, 9, 7, 7, 8, 10, 6);
                Assert.IsTrue(P(WeaponType.INF_AB_UK).HasCapability(WeaponCapability.AirDroppable), "UK Airborne air-droppable");

                // --- FRG (Stinger) ---
                AssertGround(WeaponType.INF_REG_GE, 8, 7, 7, 8, 10, 8);
                AssertGround(WeaponType.INF_AB_GE, 9, 7, 7, 8, 10, 8);
                Assert.IsTrue(P(WeaponType.INF_AB_GE).HasCapability(WeaponCapability.AirDroppable), "FRG Airborne air-droppable");

                // --- FR (Mistral = Stinger-class) ---
                AssertGround(WeaponType.INF_REG_FR, 8, 7, 7, 8, 10, 8);
                Assert.AreEqual(1.05f, P(WeaponType.INF_REG_FR).ICM, 0.01f, "FR Regulars Mistral ICM");
                AssertGround(WeaponType.INF_AB_FR, 9, 7, 7, 8, 10, 8);
                Assert.IsTrue(P(WeaponType.INF_AB_FR).HasCapability(WeaponCapability.AirDroppable), "FR Airborne air-droppable");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Infantry_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Batch F — Infantry

        #region Batch G — Jets (US sub-batch)

        [Test]
        public void Jets_US_ResolveConvertedLines()
        {
            try
            {
                // Pure air-superiority fighters → Rule-A GA floor 2 (DF/MAN/TS/SUR preserved via residuals).
                AssertAir(WeaponType.FGT_F15_US, 16, 14, 12, 11, 2);
                Assert.AreEqual(6, (int)P(WeaponType.FGT_F15_US).OrdinanceLoad, "F-15 OL 6");
                Assert.AreEqual(4, (int)P(WeaponType.FGT_F15_US).SpottingRange, "F-15 SR 4");
                AssertAir(WeaponType.FGT_F4_US, 10, 9, 12, 8, 2);
                AssertAir(WeaponType.FGT_F14_US, 14, 13, 12, 9, 2);

                // F-16 multirole: MULTIROLE_STRIKE (+4) + AT_GUIDED_AIR (+3) → GA9; GaVsHard 1 stored.
                AssertAir(WeaponType.FGT_F16_US, 14, 15, 11, 8, 9);
                Assert.AreEqual(1, P(WeaponType.FGT_F16_US).GaBonusVsHard, "F-16 GaVsHard 1 (Maverick)");

                // A-10: apex CAS — HEAVY_AG_CANNON + AT_GUIDED_AIR → GA15, GaVsHard 3; SUR15/OL11 preserved.
                AssertAir(WeaponType.ATT_A10_US, 4, 4, 7, 15, 15);
                Assert.AreEqual(11, (int)P(WeaponType.ATT_A10_US).OrdinanceLoad, "A-10 OL 11");
                Assert.AreEqual(3, P(WeaponType.ATT_A10_US).GaBonusVsHard, "A-10 GaVsHard 3");

                // F-117 stealth strike: GA13, tiny bay OL6, STL15, GaVsBase 4, Stealth cap.
                AssertAir(WeaponType.ATT_F117_US, 1, 3, 10, 8, 13);
                Assert.AreEqual(6, (int)P(WeaponType.ATT_F117_US).OrdinanceLoad, "F-117 OL 6 (internal bay)");
                Assert.AreEqual(15, (int)P(WeaponType.ATT_F117_US).Stealth, "F-117 STL 15");
                Assert.AreEqual(4, P(WeaponType.ATT_F117_US).GaBonusVsBase, "F-117 GaVsBase 4");
                Assert.IsTrue(P(WeaponType.ATT_F117_US).HasCapability(WeaponCapability.Stealth), "F-117 stealth");

                // F-111 interdictor: GA13, OL14, GaVsBase 4.
                AssertAir(WeaponType.BMB_F111_US, 6, 6, 14, 8, 13);
                Assert.AreEqual(14, (int)P(WeaponType.BMB_F111_US).OrdinanceLoad, "F-111 OL 14");
                Assert.AreEqual(4, P(WeaponType.BMB_F111_US).GaBonusVsBase, "F-111 GaVsBase 4");

                // E-3 AWACS: non-combatant, SR 12.
                Assert.IsFalse(P(WeaponType.AWACS_E3_US).IsAttackCapable, "E-3 non-combatant");
                Assert.AreEqual(12, (int)P(WeaponType.AWACS_E3_US).SpottingRange, "E-3 SR 12");

                // SR-71 recon: non-combatant, Mach-3 TS 21, SR 8.
                Assert.IsFalse(P(WeaponType.RCNA_SR71_US).IsAttackCapable, "SR-71 non-combatant");
                Assert.AreEqual(21, (int)P(WeaponType.RCNA_SR71_US).TopSpeed, "SR-71 TS 21");
                Assert.AreEqual(8, (int)P(WeaponType.RCNA_SR71_US).SpottingRange, "SR-71 SR 8");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Jets_US_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Batch G — Jets (US sub-batch)

        #region Batch G — Jets (Euro-NATO sub-batch)

        [Test]
        public void Jets_Euro_ResolveConvertedLines()
        {
            try
            {
                // Tornado IDS: heavy interdictor — MULTIROLE_STRIKE + LASER_GUIDED + HEAVY_PAYLOAD + RUNWAY_CRATERING.
                AssertAir(WeaponType.FGT_TORNADO_IDS_UK, 12, 11, 10, 7, 8);
                Assert.AreEqual(9, (int)P(WeaponType.FGT_TORNADO_IDS_UK).OrdinanceLoad, "Tornado IDS OL 9");
                Assert.AreEqual(20, P(WeaponType.FGT_TORNADO_IDS_UK).OcSuppressionBonus, "Tornado IDS runway cratering");

                // Tornado GR.1: lighter UK strike variant (no HEAVY_PAYLOAD → OL6).
                AssertAir(WeaponType.FGT_TORNADO_GR1_US, 13, 11, 10, 7, 8);
                Assert.AreEqual(6, (int)P(WeaponType.FGT_TORNADO_GR1_US).OrdinanceLoad, "Tornado GR.1 OL 6");
                Assert.AreEqual(20, P(WeaponType.FGT_TORNADO_GR1_US).OcSuppressionBonus, "Tornado GR.1 runway cratering");

                // F-4F: pure interceptor, GA floor 2 (= US F-4).
                AssertAir(WeaponType.FGT_F4_GE, 10, 9, 12, 8, 2);

                // Mirage 2000: most agile NATO fighter here; multirole strike GA8.
                AssertAir(WeaponType.FGT_MIRAGE2000_FR, 14, 15, 12, 7, 8);

                // Mirage F1: older, lighter striker GA6.
                AssertAir(WeaponType.FGT_MIRAGEF1_FR, 11, 10, 11, 8, 6);

                // Jaguar: low-level attack — GA8, OL9, runway cratering.
                AssertAir(WeaponType.ATT_JAGUAR_FR, 8, 9, 9, 9, 8);
                Assert.AreEqual(9, (int)P(WeaponType.ATT_JAGUAR_FR).OrdinanceLoad, "Jaguar OL 9");
                Assert.AreEqual(20, P(WeaponType.ATT_JAGUAR_FR).OcSuppressionBonus, "Jaguar runway cratering");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Jets_Euro_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Batch G — Jets (Euro-NATO sub-batch)
    }
}
