using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Phase 3 pilot: the 9 Appendix W §16 worked tank derivations driven end-to-end through
    /// <see cref="WeaponProfile.FromProfileDef"/> (archetype + delta + trait → published statline + ICM).
    /// This recreates the deleted §16 resolver test, now validating the full ProfileDef→WeaponProfile path
    /// — the factory wiring, the published-stat mapping, and the legacy capability-bool bridge. DB-free
    /// (factory = resolver + constructor; no WeaponProfileDB / SpriteManager).
    ///
    /// Per design §2 = Option A, SR and PR also flow through the resolver, so the asserted SR/PR show the
    /// trait deltas live — OPTICS/THERMAL push SR above the base 2, and GUN_LAUNCHED_ATGM gives PR 2 (standoff).
    /// </summary>
    [TestFixture]
    public class WeaponProfileFactoryTests : BaseTestFixture
    {
        private const string CLASS_NAME = nameof(WeaponProfileFactoryTests);

        #region Helpers

        // The WeaponType is a stand-in (it only sets the auto-default TargetClass, not asserted here).
        private static void AssertTank(string name, Archetype arch, Dictionary<ProfileStat, int> deltas,
            WeaponTrait[] traits, int ha, int hd, int sa, int sd, int gad, float icm, int mmp, int sr, int pr)
        {
            WeaponProfile p = WeaponProfile.FromProfileDef(name, name, WeaponType.TANK_T55A_SV,
                new ProfileDef(arch, deltas, traits));
            Assert.AreEqual(ha,  (int)p.HardAttack,    $"{name} HA");
            Assert.AreEqual(hd,  p.HardDefense,        $"{name} HD");
            Assert.AreEqual(sa,  p.SoftAttack,         $"{name} SA");
            Assert.AreEqual(sd,  p.SoftDefense,        $"{name} SD");
            Assert.AreEqual(gad, p.GroundAirDefense,   $"{name} GAD");
            Assert.AreEqual(icm, p.ICM, 0.01f,         $"{name} ICM");
            Assert.AreEqual(mmp, p.MaxMovementPoints,  $"{name} MMP");
            Assert.AreEqual(sr,  (int)p.SpottingRange, $"{name} SR");
            Assert.AreEqual(pr,  (int)p.PrimaryRange,  $"{name} PR");
        }

        private static WeaponProfile WithTraits(params WeaponTrait[] traits)
            => WeaponProfile.FromProfileDef("x", "x", WeaponType.TANK_T55A_SV,
                new ProfileDef(TankArchetypes.Gen1, new Dictionary<ProfileStat, int>(), traits));

        #endregion // Helpers

        #region §16 worked tank statlines

        [Test]
        public void SovietTanks_ResolveSixteenLines()
        {
            try
            {
                Archetype g1 = TankArchetypes.Gen1, g2 = TankArchetypes.Gen2, g3 = TankArchetypes.Gen3;

                AssertTank("T-55A", g1, new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.LOW_PROFILE, WeaponTrait.NBC_PROTECTED },
                    7, 6, 5, 7, 7, 1.00f, 10, 2, 1);   // SR 2 (no optics), PR 1 (no GLATGM)

                AssertTank("T-64BV", g3, new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ERA_LIGHT, WeaponTrait.GUN_LAUNCHED_ATGM, WeaponTrait.LOW_PROFILE,
                            WeaponTrait.LASER_RANGEFINDER, WeaponTrait.BALLISTIC_COMPUTER },
                    15, 14, 9, 7, 7, 1.10f, 10, 2, 2);   // PR 2 — GUN_LAUNCHED_ATGM standoff

                AssertTank("T-72A", g2, new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.GUN_125_SMOOTH, WeaponTrait.SPACED_ARMOR, WeaponTrait.LASER_RANGEFINDER },
                    12, 9, 7, 6, 7, 1.05f, 10, 2, 1);

                AssertTank("T-80B", g2, new Dictionary<ProfileStat, int> { { ProfileStat.HA, 1 } },
                    new[] { WeaponTrait.GUN_LAUNCHED_ATGM, WeaponTrait.COMPOSITE_CERAMIC, WeaponTrait.LASER_RANGEFINDER,
                            WeaponTrait.BALLISTIC_COMPUTER, WeaponTrait.GAS_TURBINE },
                    13, 10, 7, 6, 7, 1.10f, 12, 2, 2);   // PR 2 — GUN_LAUNCHED_ATGM standoff
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(SovietTanks_ResolveSixteenLines), ex); throw; }
        }

        [Test]
        public void NatoTanks_ResolveSixteenLines()
        {
            try
            {
                Archetype g2 = TankArchetypes.Gen2, g3 = TankArchetypes.Gen3;

                AssertTank("Leopard 1", g2,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, -1 }, { ProfileStat.HD, -1 }, { ProfileStat.SA, 1 }, { ProfileStat.MMP, 2 } },
                    new[] { WeaponTrait.OPTICS_GEN2, WeaponTrait.LASER_RANGEFINDER },
                    9, 7, 8, 6, 7, 1.10f, 12, 3, 1);   // SR 3 — OPTICS_GEN2 +1

                AssertTank("Leopard 2", g3,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, 1 }, { ProfileStat.SA, -1 }, { ProfileStat.MMP, 2 } },
                    new[] { WeaponTrait.SPACED_ARMOR, WeaponTrait.OPTICS_GEN3, WeaponTrait.LASER_RANGEFINDER,
                            WeaponTrait.BALLISTIC_COMPUTER, WeaponTrait.THERMAL_IMAGER },
                    14, 12, 8, 6, 7, 1.33f, 12, 4, 1);   // SR 4 — OPTICS_GEN3 +1, THERMAL +1

                AssertTank("M1 (105)", g3,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, -3 }, { ProfileStat.SA, -1 } },
                    new[] { WeaponTrait.COMPOSITE_CERAMIC, WeaponTrait.LASER_RANGEFINDER, WeaponTrait.BALLISTIC_COMPUTER,
                            WeaponTrait.OPTICS_GEN3, WeaponTrait.THERMAL_IMAGER, WeaponTrait.GAS_TURBINE },
                    10, 13, 8, 6, 7, 1.33f, 12, 4, 1);   // SR 4 — OPTICS_GEN3 +1, THERMAL +1

                AssertTank("M1A1 (120)", g3,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, 1 }, { ProfileStat.SA, -1 } },
                    new[] { WeaponTrait.COMPOSITE_DU, WeaponTrait.LASER_RANGEFINDER, WeaponTrait.BALLISTIC_COMPUTER,
                            WeaponTrait.OPTICS_GEN3, WeaponTrait.THERMAL_IMAGER, WeaponTrait.GAS_TURBINE },
                    14, 14, 8, 6, 7, 1.33f, 12, 4, 1);   // SR 4 — OPTICS_GEN3 +1, THERMAL +1

                AssertTank("Challenger 1", g3,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HD, 1 }, { ProfileStat.SA, -1 } },
                    new[] { WeaponTrait.COMPOSITE_CERAMIC, WeaponTrait.LASER_RANGEFINDER, WeaponTrait.BALLISTIC_COMPUTER,
                            WeaponTrait.THERMAL_IMAGER },
                    13, 14, 8, 6, 7, 1.21f, 10, 3, 1);   // SR 3 — THERMAL +1
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(NatoTanks_ResolveSixteenLines), ex); throw; }
        }

        #endregion // §16 worked tank statlines

        #region Capability bridge (legacy bools derived from resolved caps)

        [Test]
        public void Bridge_Amphibious_SetsCapAndBool()
        {
            try
            {
                WeaponProfile p = WithTraits(WeaponTrait.AMPHIBIOUS);
                Assert.IsTrue(p.HasCapability(WeaponCapability.Amphibious), "HasCapability(Amphibious)");
                Assert.IsTrue(p.IsAmphibious, "IsAmphibious bridge");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Bridge_Amphibious_SetsCapAndBool), ex); throw; }
        }

        [Test]
        public void Bridge_NonCombatant_ClearsIsAttackCapable()
        {
            try
            {
                WeaponProfile p = WithTraits(WeaponTrait.NON_COMBATANT);
                Assert.IsTrue(p.HasCapability(WeaponCapability.NonCombatant), "HasCapability(NonCombatant)");
                Assert.IsFalse(p.IsAttackCapable, "IsAttackCapable bridge (NON_COMBATANT)");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Bridge_NonCombatant_ClearsIsAttackCapable), ex); throw; }
        }

        [Test]
        public void Bridge_RocketArtillery_SetsIsDoubleFire()
        {
            try
            {
                WeaponProfile p = WithTraits(WeaponTrait.ROCKET_ARTILLERY);
                Assert.IsTrue(p.HasCapability(WeaponCapability.RocketArtillery), "HasCapability(RocketArtillery)");
                Assert.IsTrue(p.IsDoubleFire, "IsDoubleFire bridge (ROCKET_ARTILLERY)");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Bridge_RocketArtillery_SetsIsDoubleFire), ex); throw; }
        }

        [Test]
        public void Bridge_NoCapabilityTraits_DefaultBools()
        {
            try
            {
                WeaponProfile p = WithTraits(WeaponTrait.LOW_PROFILE); // a stat-only trait, no capabilities
                Assert.IsFalse(p.IsAmphibious, "IsAmphibious default false");
                Assert.IsFalse(p.IsDoubleFire, "IsDoubleFire default false");
                Assert.IsTrue(p.IsAttackCapable, "IsAttackCapable default true");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Bridge_NoCapabilityTraits_DefaultBools), ex); throw; }
        }

        #endregion // Capability bridge

        #region Rule B air-to-ground strike riders (plumbing — stored, unconsumed)

        // Builds on the Attack air archetype (GA 10 / OL 9) so the flat GA deltas land alongside the riders.
        private static WeaponProfile Strike(params WeaponTrait[] traits)
            => WeaponProfile.FromProfileDef("x", "x", WeaponType.ATT_SU25_SV,
                new ProfileDef(FamilyArchetypes.Attack, new Dictionary<ProfileStat, int>(), traits));

        [Test]
        public void RuleB_StrikeRiders_AccumulateAndStore()
        {
            try
            {
                // HEAVY_AG_CANNON (GA+2, GaVsHard+2) + AT_GUIDED_AIR (GA+3, GaVsHard+1) + CARPET_BOMBING (GA+1, GaVsSoft+3)
                // + BUNKER_PENETRATOR (GaVsBase+4) + RUNWAY_CRATERING (OcSuppression+20) + RAMP_STRIKE (ParkedHit+1).
                WeaponProfile p = Strike(WeaponTrait.HEAVY_AG_CANNON, WeaponTrait.AT_GUIDED_AIR,
                    WeaponTrait.CARPET_BOMBING, WeaponTrait.BUNKER_PENETRATOR,
                    WeaponTrait.RUNWAY_CRATERING, WeaponTrait.RAMP_STRIKE);

                Assert.AreEqual(3,  p.GaBonusVsHard,      "GaVsHard 2+1");
                Assert.AreEqual(3,  p.GaBonusVsSoft,      "GaVsSoft 3");
                Assert.AreEqual(4,  p.GaBonusVsBase,      "GaVsBase 4");
                Assert.AreEqual(20, p.OcSuppressionBonus, "OcSuppression 20");
                Assert.AreEqual(1,  p.ParkedHitBonus,     "ParkedHit 1");
                Assert.AreEqual(16, p.GroundAttack,       "flat GA 10+2+3+1 lands alongside riders");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(RuleB_StrikeRiders_AccumulateAndStore), ex); throw; }
        }

        [Test]
        public void RuleB_NoStrikeTraits_RidersZero()
        {
            try
            {
                WeaponProfile p = Strike(WeaponTrait.CAS_ARMORED); // SUR+2 only, no riders
                Assert.AreEqual(0, p.GaBonusVsHard,      "no GaVsHard");
                Assert.AreEqual(0, p.GaBonusVsSoft,      "no GaVsSoft");
                Assert.AreEqual(0, p.GaBonusVsBase,      "no GaVsBase");
                Assert.AreEqual(0, p.OcSuppressionBonus, "no OcSuppression");
                Assert.AreEqual(0, p.ParkedHitBonus,     "no ParkedHit");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(RuleB_NoStrikeTraits_RidersZero), ex); throw; }
        }

        [Test]
        public void RuleB_CapabilityHooks_AreDormant()
        {
            try
            {
                // avoid-GAD and loiter re-attack are Dormant (no consumer yet) — the resolver must skip them.
                WeaponProfile p = Strike(WeaponTrait.STANDOFF_CRUISE_MISSILE, WeaponTrait.LOITER_PERSISTENCE);
                Assert.IsFalse(p.HasCapability(WeaponCapability.IgnoreAirDefense), "avoid-GAD dormant (skipped)");
                Assert.IsFalse(p.HasCapability(WeaponCapability.LoiterReattack),  "loiter dormant (skipped)");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(RuleB_CapabilityHooks_AreDormant), ex); throw; }
        }

        #endregion // Rule B air-to-ground strike riders
    }
}
