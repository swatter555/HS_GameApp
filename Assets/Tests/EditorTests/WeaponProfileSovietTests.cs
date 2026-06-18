using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;
using System;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Phase 3 regression guard for the SOVIET faction rebuild: every Soviet weapon family was converted from
    /// the old additive constructor to <see cref="WeaponProfile.FromProfileDef"/> (archetype + delta + trait).
    /// This fixture loads the live <see cref="WeaponProfileDB"/> and asserts the resolved statline of one (or a
    /// few) representative profile per family, so a future tweak to an archetype, trait, or per-profile delta that
    /// shifts a published stat trips a test. Stat reads are cast to int; ICM is float (±0.01).
    /// </summary>
    [TestFixture]
    public class WeaponProfileSovietTests : BaseTestFixture
    {
        private const string CLASS_NAME = nameof(WeaponProfileSovietTests);
        private const float ICM_TOL = 0.01f;

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

        #region Tanks

        [Test]
        public void Tanks_ResolveConvertedLines()
        {
            try
            {
                // Gen1 baseline + apex super-tank (the two ends of the §16 ladder).
                AssertGround(WeaponType.TANK_T55A_SV, 7, 6, 5, 7, 7, 0);
                Assert.AreEqual(10, (int)P(WeaponType.TANK_T55A_SV).MaxMovementPoints, "T-55A MMP");
                Assert.AreEqual(1.00f, P(WeaponType.TANK_T55A_SV).ICM, ICM_TOL, "T-55A ICM");

                // T-80BVM: Gen4 + ERA_RELIKT + APS + GLATGM + APFSDS + turbine + LRF/BC + thermal.
                AssertGround(WeaponType.TANK_T80BV_SV, 20, 20, 10, 7, 7, 0);
                Assert.AreEqual(1.21f, P(WeaponType.TANK_T80BV_SV).ICM, ICM_TOL, "T-80BVM ICM");
                Assert.AreEqual(12, (int)P(WeaponType.TANK_T80BV_SV).MaxMovementPoints, "T-80BVM MMP");
                Assert.AreEqual(3,  (int)P(WeaponType.TANK_T80BV_SV).SpottingRange, "T-80BVM SR");

                // Validated §16 T-72A.
                AssertGround(WeaponType.TANK_T72A_SV, 12, 9, 7, 6, 7, 0);
                Assert.AreEqual(1.05f, P(WeaponType.TANK_T72A_SV).ICM, ICM_TOL, "T-72A ICM");
                Assert.IsTrue(P(WeaponType.TANK_T72A_SV).IsAmphibious, "T-72A amphibious (trait-restored)");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Tanks_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Tanks

        #region Mech (IFV / APC / Recon)

        [Test]
        public void Mech_ResolveConvertedLines()
        {
            try
            {
                AssertGround(WeaponType.IFV_BMP2_SV, 9, 4, 9, 7, 7, 0);   // 30mm + Konkurs
                Assert.IsTrue(P(WeaponType.IFV_BMP2_SV).IsAmphibious, "BMP-2 amphibious");

                AssertGround(WeaponType.APC_BTR70_SV, 3, 4, 6, 8, 7, 0);  // APC archetype + SD
                Assert.AreEqual(8, (int)P(WeaponType.APC_BTR70_SV).MaxMovementPoints, "BTR-70 MMP (APC baseline 8)");

                // BRDM-2 scout: hardened hull (HD5/SD9) + RECON_FRAGILE ICM 0.60, SR 3.
                AssertGround(WeaponType.RCN_BRDM2_SV, 2, 5, 5, 9, 7, 0);
                Assert.AreEqual(0.60f, P(WeaponType.RCN_BRDM2_SV).ICM, ICM_TOL, "BRDM-2 RECON_FRAGILE ICM");
                Assert.AreEqual(3, (int)P(WeaponType.RCN_BRDM2_SV).SpottingRange, "BRDM-2 SR");

                // BRDM-2 AT: ATGM (HA 6), Hard target, NOT fragile.
                AssertGround(WeaponType.RCN_BRDM2AT_SV, 6, 5, 5, 9, 7, 0);
                Assert.AreEqual(TargetClass.Hard, P(WeaponType.RCN_BRDM2AT_SV).TargetClass, "BRDM-2 AT Hard");
                Assert.AreEqual(1.00f, P(WeaponType.RCN_BRDM2AT_SV).ICM, ICM_TOL, "BRDM-2 AT no fragile penalty");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Mech_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Mech

        #region Artillery (SP / towed / rocket / ballistic)

        [Test]
        public void Artillery_ResolveConvertedLines()
        {
            try
            {
                // 2S19 SP howitzer: Krasnopol SMART_MUNITION (HA 8) on the tracked chassis.
                AssertGround(WeaponType.SPA_2S19_SV, 8, 7, 10, 7, 7, 0);
                Assert.AreEqual(10, (int)P(WeaponType.SPA_2S19_SV).MaxMovementPoints, "2S19 MMP (SELF_PROPELLED)");
                Assert.AreEqual(5,  (int)P(WeaponType.SPA_2S19_SV).IndirectRange, "2S19 IR");

                // Towed light: bare Artillery archetype (foot, MMP 4), GAD 8.
                AssertGround(WeaponType.ART_LIGHT_SV, 5, 5, 9, 5, 8, 0);
                Assert.AreEqual(4, (int)P(WeaponType.ART_LIGHT_SV).MaxMovementPoints, "Lt towed MMP 4");

                // BM-21: rocket artillery → IsDoubleFire; truck chassis GAD 6, MMP 8.
                AssertGround(WeaponType.ROC_BM21_SV, 5, 5, 9, 5, 6, 0);
                Assert.IsTrue(P(WeaponType.ROC_BM21_SV).IsDoubleFire, "BM-21 rocket-artillery double-fire");
                Assert.AreEqual(8, (int)P(WeaponType.ROC_BM21_SV).MaxMovementPoints, "BM-21 MMP (TRUCK_MOUNTED)");

                // Scud (R3): HA 11 / SA 15, single-fire (W5 excludes it from ROCKET_ARTILLERY).
                AssertGround(WeaponType.ROC_SCUD_SV, 11, 5, 15, 5, 6, 0);
                Assert.IsFalse(P(WeaponType.ROC_SCUD_SV).IsDoubleFire, "Scud is single-fire (W5)");
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
                // GAT rebalance (2026-06-18, +2 base): ZSU-23-4 SPAAA: Aaa + radar gun → GAT 13, GAD 11, MMP 10.
                AssertGround(WeaponType.SPAAA_ZSU23_SV, 4, 6, 9, 8, 11, 13);

                // S-75 site SAM: air-only, GAT 15, static (MMP 0).
                AssertGround(WeaponType.SAM_S75_SV, 1, 3, 1, 3, 8, 15);
                Assert.AreEqual(0, (int)P(WeaponType.SAM_S75_SV).MaxMovementPoints, "S-75 static MMP 0");

                // S-300: apex GAT 16, truck-mobile (MMP 8 per Bob), SR 10.
                AssertGround(WeaponType.SAM_S300_SV, 1, 3, 1, 3, 8, 16);
                Assert.AreEqual(8,  (int)P(WeaponType.SAM_S300_SV).MaxMovementPoints, "S-300 truck MMP 8");
                Assert.AreEqual(10, (int)P(WeaponType.SAM_S300_SV).SpottingRange, "S-300 SR 10");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(AirDefense_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Air defense

        #region Helicopters

        [Test]
        public void Helicopters_ResolveConvertedLines()
        {
            try
            {
                // Mi-28 apex gunship: FNF ATGM (ICM 1.05) + cannon/rockets/armor/countermeasures.
                AssertGround(WeaponType.HEL_MI28_SV, 12, 7, 13, 8, 12, 0);
                Assert.AreEqual(1.05f, P(WeaponType.HEL_MI28_SV).ICM, ICM_TOL, "Mi-28 FNF ICM");
                Assert.AreEqual(3, (int)P(WeaponType.HEL_MI28_SV).SpottingRange, "Mi-28 SR 3");

                // Mi-8T transport: non-combatant + helo-transport category.
                Assert.IsFalse(P(WeaponType.HEL_MI8T_SV).IsAttackCapable, "Mi-8T non-combatant");
                Assert.AreEqual(TransportCategory.HeloTransport, P(WeaponType.HEL_MI8T_SV).TransportCategory, "Mi-8T helo transport");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Helicopters_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Helicopters

        #region Infantry

        [Test]
        public void Infantry_ResolveConvertedLines()
        {
            try
            {
                // Regulars: RPG (HA 6) + Strela MANPADS (GAT floor 6), infantry GAD 10 (R1).
                AssertGround(WeaponType.INF_REG_SV, 6, 7, 7, 8, 10, 6);

                // Spetsnaz: SPECIAL_FORCES (R5 SD 9) + ICM 1.10 + recon SR 3.
                AssertGround(WeaponType.INF_SPEC_SV, 8, 7, 9, 9, 10, 6);
                Assert.AreEqual(1.10f, P(WeaponType.INF_SPEC_SV).ICM, ICM_TOL, "Spetsnaz SF ICM");
                Assert.AreEqual(3, (int)P(WeaponType.INF_SPEC_SV).SpottingRange, "Spetsnaz SR 3");

                Assert.IsTrue(P(WeaponType.INF_MAR_SV).IsAmphibious, "Marines amphibious");
                Assert.IsTrue(P(WeaponType.INF_ENG_SV).HasCapability(WeaponCapability.FieldFortification), "Engineers field-fortification");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Infantry_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Infantry

        #region Jets (air block + W8 spotting + non-combatants)

        [Test]
        public void Jets_ResolveConvertedLines()
        {
            try
            {
                // Air-stat enrichment: fighter DF/MAN/TS/SUR now built from traits off the generation archetype
                // (FighterEarly/Mid/Late), not preservation residuals. Air-to-air missile tiers: BVR_RADAR_MISSILE
                // (semi-active, DF+2) < ACTIVE_RADAR_AAM (AA-12/AMRAAM, DF+3); HIGH_OFF_BORESIGHT_IR (Archer, DF+1);
                // LOOKDOWN_SHOOTDOWN = radar-suite ICM ×1.10.

                // MiG-31 Foxhound: FighterLate + ACTIVE_RADAR_AAM (DF+3) + LOOKDOWN_SHOOTDOWN (ICM 1.10) + RWR; pure
                // interceptor → GA floor 2.
                WeaponProfile mig31 = P(WeaponType.FGT_MIG31_SV);
                Assert.AreEqual(2,  (int)mig31.GroundAttack, "MiG-31 pure interceptor GA 2");
                Assert.AreEqual(15, (int)mig31.Dogfighting,  "MiG-31 DF (active-radar AAM)");
                Assert.AreEqual(1.10f, mig31.ICM, ICM_TOL,   "MiG-31 look-down/shoot-down ICM");

                // MiG-29 Fulcrum: FighterLate + AGILE + Archer (DF+1) + R-27 BVR (DF+2) + RWR/chaff + MULTIROLE; the
                // N019 radar lagged the West → NO radar-suite ICM. W8 base air SR 4.
                WeaponProfile mig29 = P(WeaponType.FGT_MIG29_SV);
                Assert.AreEqual(15, (int)mig29.Dogfighting,    "MiG-29 DF");
                Assert.AreEqual(14, (int)mig29.Maneuverability,"MiG-29 MAN (AGILE)");
                Assert.AreEqual(10, (int)mig29.TopSpeed,       "MiG-29 TS (archetype)");
                Assert.AreEqual(11, (int)mig29.Survivability,  "MiG-29 SUR (RWR+chaff)");
                Assert.AreEqual(6,  (int)mig29.GroundAttack,   "MiG-29 multirole GA 6");
                Assert.AreEqual(1.00f, mig29.ICM, ICM_TOL,     "MiG-29 no radar-suite ICM");
                Assert.AreEqual(4,  (int)mig29.SpottingRange,  "MiG-29 air SR 4 (W8)");

                // Su-27 Flanker: apex air-superiority — FighterLate + AGILE + AA-12 (DF+3) + Archer (DF+1) +
                // look-down ICM + RWR/chaff + MULTIROLE.
                WeaponProfile su27 = P(WeaponType.FGT_SU27_SV);
                Assert.AreEqual(16, (int)su27.Dogfighting,     "Su-27 DF (apex A2A)");
                Assert.AreEqual(14, (int)su27.Maneuverability, "Su-27 MAN");
                Assert.AreEqual(11, (int)su27.Survivability,   "Su-27 SUR");
                Assert.AreEqual(1.10f, su27.ICM, ICM_TOL,      "Su-27 radar-suite ICM");
                Assert.AreEqual(6,  (int)su27.GroundAttack,    "Su-27 multirole GA 6");

                // Su-47 Berkut: experimental super-maneuver singleton — tops the roster on MAN/SUR.
                WeaponProfile su47 = P(WeaponType.FGT_SU47_SV);
                Assert.AreEqual(17, (int)su47.Maneuverability, "Su-47 MAN (forward-swept TVC)");
                Assert.AreEqual(12, (int)su47.Survivability,   "Su-47 SUR (full suite)");

                // MiG-23 radar fighter: semi-active BVR (DF+2) + RWR/chaff (SUR+2).
                WeaponProfile mig23 = P(WeaponType.FGT_MIG23_SV);
                Assert.AreEqual(10, (int)mig23.Dogfighting,   "MiG-23 DF (BVR)");
                Assert.AreEqual(8,  (int)mig23.Survivability, "MiG-23 SUR (RWR+chaff)");

                // MiG-27 Flogger-D: re-homed Attack→FighterEarly + MULTIROLE_STRIKE → GA 6.
                WeaponProfile mig27 = P(WeaponType.FGT_MIG27_SV);
                Assert.AreEqual(8, (int)mig27.Dogfighting,  "MiG-27 fighter-base DF 8");
                Assert.AreEqual(6, (int)mig27.GroundAttack, "MiG-27 multirole GA 6");

                // Su-25 Frogfoot: Soviet A-10 — HEAVY_AG_CANNON + AT_GUIDED_AIR → GA 15, GaVsHard 3 stored.
                WeaponProfile su25 = P(WeaponType.ATT_SU25_SV);
                Assert.AreEqual(15, (int)su25.GroundAttack, "Su-25 GA 15");
                Assert.AreEqual(3,  su25.GaBonusVsHard,     "Su-25 GaVsHard 3");

                // Su-25B: apex CAS — heavy survivability, GA 15, GaVsHard 3.
                WeaponProfile su25b = P(WeaponType.ATT_SU25B_SV);
                Assert.AreEqual(15, (int)su25b.Survivability, "Su-25B SUR");
                Assert.AreEqual(15, (int)su25b.GroundAttack,  "Su-25B GA 15");
                Assert.AreEqual(3,  su25b.GaBonusVsHard,      "Su-25B GaVsHard 3");

                // Su-24 Fencer: Soviet F-111 interdictor — GA 13, GaVsBase 4.
                WeaponProfile su24 = P(WeaponType.BMB_SU24_SV);
                Assert.AreEqual(13, (int)su24.GroundAttack, "Su-24 GA 13");
                Assert.AreEqual(4,  su24.GaBonusVsBase,     "Su-24 GaVsBase 4");

                // Tu-16 Badger: area level bomber — CARPET_BOMBING (GaVsSoft 3) + STRATEGIC_PAYLOAD (OL 16).
                WeaponProfile tu16 = P(WeaponType.BMB_TU16_SV);
                Assert.AreEqual(9,  (int)tu16.GroundAttack,  "Tu-16 GA 9");
                Assert.AreEqual(16, (int)tu16.OrdinanceLoad, "Tu-16 OL 16");
                Assert.AreEqual(3,  tu16.GaBonusVsSoft,      "Tu-16 GaVsSoft 3");

                // Tu-22 Blinder: standoff/area — Kh-22 heavy warhead → GA 12; GaVsSoft 3; avoid-GAD cap DORMANT.
                WeaponProfile tu22 = P(WeaponType.BMB_TU22_SV);
                Assert.AreEqual(12, (int)tu22.GroundAttack, "Tu-22 GA 12 (Kh-22)");
                Assert.AreEqual(3,  tu22.GaBonusVsSoft,     "Tu-22 GaVsSoft 3");
                Assert.IsFalse(tu22.HasCapability(WeaponCapability.IgnoreAirDefense), "Tu-22 avoid-GAD dormant");

                // Tu-22M3: apex strategic — Kh-22 heavy warhead GA 12, riders + payload on top.
                WeaponProfile tu = P(WeaponType.BMB_TU22M3_SV);
                Assert.AreEqual(16, (int)tu.TopSpeed,     "Tu-22M3 TS");
                Assert.AreEqual(12, (int)tu.GroundAttack, "Tu-22M3 GA 12 (Kh-22)");
                Assert.AreEqual(16, (int)tu.OrdinanceLoad,"Tu-22M3 OL 16");
                Assert.AreEqual(3,  tu.GaBonusVsSoft,     "Tu-22M3 GaVsSoft 3");
                Assert.AreEqual(4,  tu.GaBonusVsBase,     "Tu-22M3 GaVsBase 4");

                // A-50 AWACS: W8 SR 12, non-combatant.
                Assert.AreEqual(12, (int)P(WeaponType.AWACS_A50_SV).SpottingRange, "A-50 AWACS SR 12 (W8)");
                Assert.IsFalse(P(WeaponType.AWACS_A50_SV).IsAttackCapable, "A-50 non-combatant");

                // MiG-25R recon: W8 SR 8, non-combatant.
                Assert.AreEqual(8, (int)P(WeaponType.RCNA_MIG25R_SV).SpottingRange, "MiG-25R recon SR 8 (W8)");
                Assert.IsFalse(P(WeaponType.RCNA_MIG25R_SV).IsAttackCapable, "MiG-25R non-combatant");

                // An-12 fixed-wing transport: non-combatant + transport category.
                Assert.IsFalse(P(WeaponType.TRN_AN8_SV).IsAttackCapable, "An-12 non-combatant");
                Assert.AreEqual(TransportCategory.FixedWingTransport, P(WeaponType.TRN_AN8_SV).TransportCategory, "An-12 fixed-wing transport");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Jets_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Jets

        #region Trucks / naval

        [Test]
        public void Transports_ResolveConvertedLines()
        {
            try
            {
                Assert.IsFalse(P(WeaponType.TRK_GEN_SV).IsAttackCapable, "Truck non-combatant");
                Assert.AreEqual(6, (int)P(WeaponType.TRK_GEN_SV).GroundAirDefense, "Truck GAD 6 (R1)");

                Assert.IsFalse(P(WeaponType.TRN_NAVAL).IsAttackCapable, "Naval non-combatant");
                Assert.IsTrue(P(WeaponType.TRN_NAVAL).IsAmphibious, "Naval amphibious");
                Assert.AreEqual(10, (int)P(WeaponType.TRN_NAVAL).MaxMovementPoints, "Naval MMP 10");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Transports_ResolveConvertedLines), ex); throw; }
        }

        #endregion // Trucks / naval
    }
}
