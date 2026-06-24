using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M9 validation (pure-resolver core): <see cref="CombatResolver.ResolveBaseAttack"/> runs a STANDOFF (air/indirect)
    /// strike on a base through the existing forward lane, applies HP to the soft 60-HP base, suppresses
    /// OperationalCapacity (proportional + STRATEGIC_OC_BONUS + the RUNWAY_CRATERING rider, §11.7.2.2/.2a), and rolls the
    /// parked-aircraft band hit (§11.7.2.3). Cross-checks rebuild the lane independently with <see cref="FixedRollRandom"/>
    /// (same value for every die, so the result is independent of dice count/order) — the asserted numbers track the engine,
    /// not magic constants.
    /// </summary>
    [TestFixture]
    public class BaseCombatResolverTests : BaseTestFixture
    {
        private const float TOL = 0.001f;

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();
        }

        private CombatUnit BuildStrike(UnitClassification cls, WeaponType jet)
        {
            var u = new CombatUnit("Strike", cls, UnitRole.AirGroundAttack, Side.Player, Nationality.USSR);
            u.RegimentProfile.InitializeRegimentProfile("Strike", RegimentProfileType.DEP,
                WeaponType.NONE, jet, WeaponType.NONE);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetExperienceLevel(ExperienceLevel.Trained);
            return u;
        }

        private CombatUnit BuildIndirect(UnitClassification cls, WeaponType deployed)
        {
            var u = new CombatUnit("Arty", cls, UnitRole.GroundCombatIndirect, Side.Player, Nationality.USSR);
            u.RegimentProfile.InitializeRegimentProfile("Arty", RegimentProfileType.DEP,
                WeaponType.NONE, deployed, WeaponType.NONE);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetExperienceLevel(ExperienceLevel.Trained);
            return u;
        }

        private CombatUnit BuildBase(UnitClassification cls, WeaponType baseProfile)
        {
            var b = new CombatUnit("Base", cls, UnitRole.GroundCombatStatic, Side.AI, Nationality.USSR);
            b.RegimentProfile.InitializeRegimentProfile("Base", RegimentProfileType.DEP,
                WeaponType.NONE, baseProfile, WeaponType.NONE);
            return b; // bases default to Deployed and cannot change posture (§9.3.4) — leave as-is
        }

        private CombatUnit BuildAttachedJet()
        {
            var j = new CombatUnit("Jet", UnitClassification.ATT, UnitRole.AirGroundAttack, Side.AI, Nationality.USSR);
            j.RegimentProfile.InitializeRegimentProfile("Jet", RegimentProfileType.DEP,
                WeaponType.NONE, WeaponType.ATT_SU25_SV, WeaponType.NONE);
            j.SetExperienceLevel(ExperienceLevel.Trained);
            return j;
        }

        #region HP / engine damage

        [Test]
        public void ResolveBaseAttack_Air_AppliesEngineDamageToBase()
        {
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.ATT_SU25_SV);
            var ab = BuildBase(UnitClassification.AIRB, WeaponType.BASE_AIRBASE);
            var ctx = new BaseAttackContext { BaseTerrain = TerrainType.Clear };
            const int v = 4;

            var expLane = CombatResolver.BuildBaseForwardLane(strike, ab, ctx);
            int exp = CombatEngine.ResolveLane(expLane, new FixedRollRandom(v));

            float hp0 = ab.HitPoints.Current;
            var r = CombatResolver.ResolveBaseAttack(strike, ab, ctx, new FixedRollRandom(v));

            Assert.AreEqual(60f, ab.HitPoints.Max, TOL, "a base is a 60-HP target (§7.7.9)");
            Assert.AreEqual(exp, r.DamageToBase, "base takes the standoff air lane's HP (effGA vs base GAD, OL, air balance)");
            Assert.AreEqual(hp0 - exp, ab.HitPoints.Current, TOL, "HP applied to the base");
        }

        [Test]
        public void ResolveBaseAttack_FlagsBaseDestroyed_AtZeroHp()
        {
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.ATT_SU25_SV);
            var ab = BuildBase(UnitClassification.AIRB, WeaponType.BASE_AIRBASE);
            ab.TakeDamage(ab.HitPoints.Max - 1f); // 1 HP left

            var r = CombatResolver.ResolveBaseAttack(strike, ab,
                new BaseAttackContext { BaseTerrain = TerrainType.Clear }, new FixedRollRandom(8));

            Assert.IsTrue(r.BaseDestroyed, "a base at 1 HP is destroyed by a connecting standoff strike (§11.7.2.1)");
            Assert.LessOrEqual(ab.HitPoints.Current, 0f);
        }

        #endregion // HP / engine damage

        #region OperationalCapacity suppression (§11.7.2.2/.2a)

        [Test]
        public void ResolveBaseAttack_Standoff_OcIsProportionalPlusStrategicPremium()
        {
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.ATT_SU25_SV); // no RUNWAY_CRATERING → rider 0
            var ab = BuildBase(UnitClassification.AIRB, WeaponType.BASE_AIRBASE);
            var ctx = new BaseAttackContext { BaseTerrain = TerrainType.Clear };
            const int v = 5;

            int dmg = CombatEngine.ResolveLane(CombatResolver.BuildBaseForwardLane(strike, ab, ctx), new FixedRollRandom(v));
            int expOc = CombatMath.RoundHalfUp((double)dmg / GameData.BASE_MAX_HP * 100.0)
                      + GameData.STRATEGIC_OC_BONUS + strike.ActiveOcSuppressionBonus;

            var r = CombatResolver.ResolveBaseAttack(strike, ab, ctx, new FixedRollRandom(v));

            Assert.AreEqual(0, strike.ActiveOcSuppressionBonus, "Su-25 carries no RUNWAY_CRATERING rider");
            Assert.AreEqual(expOc, r.OcDamageApplied, "OC = round(HP/60×100) + STRATEGIC_OC_BONUS (§11.7.2.2a)");
            Assert.AreEqual(Math.Min(GameData.MAX_DAMAGE, expOc), ab.BaseDamage, "AddFacilityDamage applies + clamps to 100");
            Assert.Greater(r.OcDamageApplied, 0, "a connecting standoff strike always suppresses some OC");
        }

        [Test]
        public void ResolveBaseAttack_RunwayCratering_AddsOcSuppressionRider()
        {
            // FGT_TORNADO_IDS_UK carries RUNWAY_CRATERING → OcSuppressionBonus 20 on top of the strategic premium.
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.FGT_TORNADO_IDS_UK);
            var ab = BuildBase(UnitClassification.AIRB, WeaponType.BASE_AIRBASE);
            var ctx = new BaseAttackContext { BaseTerrain = TerrainType.Clear };
            const int v = 5;

            Assert.AreEqual(20, strike.ActiveOcSuppressionBonus, "Tornado IDS RUNWAY_CRATERING rider is read by the accessor");

            int dmg = CombatEngine.ResolveLane(CombatResolver.BuildBaseForwardLane(strike, ab, ctx), new FixedRollRandom(v));
            int expOc = CombatMath.RoundHalfUp((double)dmg / GameData.BASE_MAX_HP * 100.0)
                      + GameData.STRATEGIC_OC_BONUS + 20;

            var r = CombatResolver.ResolveBaseAttack(strike, ab, ctx, new FixedRollRandom(v));
            Assert.AreEqual(expOc, r.OcDamageApplied, "RUNWAY_CRATERING OcSuppressionBonus adds on top of the strategic premium");
        }

        [Test]
        public void ResolveBaseAttack_OcSuppression_AppliesToAllBaseTypes()
        {
            // §11.7.2.10 — HP/OC apply to every base type; an HQ has no attached aircraft (no parked rolls).
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.ATT_SU25_SV);
            var hq = BuildBase(UnitClassification.HQ, WeaponType.BASE_HQ);

            var r = CombatResolver.ResolveBaseAttack(strike, hq,
                new BaseAttackContext { BaseTerrain = TerrainType.Clear }, new FixedRollRandom(4));

            Assert.Greater(r.OcDamageApplied, 0, "an HQ suffers OC suppression like any base (§11.7.2.10)");
            Assert.AreEqual(0, r.ParkedAircraft.Length, "an HQ has no attached aircraft → no parked rolls");
        }

        #endregion // OperationalCapacity suppression

        #region Standoff-cruise GAD ignore + indirect routing

        [Test]
        public void BuildBaseForwardLane_StandoffCruise_IgnoresBaseGad()
        {
            // Tu-22 STANDOFF_CRUISE_MISSILE zeroes the base GAD term (§11.6.1.1), same as vs a mobile target.
            var strike = BuildStrike(UnitClassification.BMB, WeaponType.BMB_TU22_SV);
            var ab = BuildBase(UnitClassification.AIRB, WeaponType.BASE_AIRBASE);

            var lane = CombatResolver.BuildBaseForwardLane(strike, ab,
                new BaseAttackContext { BaseTerrain = TerrainType.Clear });

            Assert.Greater(ab.ActiveGroundAirDefense, 0, "the base has a GAD that is being ignored");
            Assert.AreEqual(0, lane.TargetDefense, "cruise strike drops the base GAD term");
        }

        [Test]
        public void BuildBaseForwardLane_IndirectAttacker_UsesSoftAxis()
        {
            var spa = BuildIndirect(UnitClassification.SPA, WeaponType.SPA_2S1_SV);
            var ab = BuildBase(UnitClassification.AIRB, WeaponType.BASE_AIRBASE);

            var lane = CombatResolver.BuildBaseForwardLane(spa, ab,
                new BaseAttackContext { BaseTerrain = TerrainType.Clear });

            Assert.AreEqual(AttackType.Indirect, lane.AttackType, "a non-air attacker on a base uses the indirect forward lane");
            Assert.AreEqual(TargetClass.Soft, ab.ActiveTargetClass, "a base is a SOFT target (§7.4.1.2)");
            Assert.AreEqual(spa.GetAttackStatVsClass(TargetClass.Soft), lane.FirerAttack, "soft axis → firer SA");
            Assert.AreEqual(ab.GetDefenseStatVsClass(TargetClass.Soft), lane.TargetDefense, "soft axis → base SD");
            Assert.IsFalse(lane.FirerIsAir, "artillery is a ground firer → GroundBalanceMod");
        }

        #endregion // Standoff-cruise GAD ignore + indirect routing

        #region Parked aircraft band roll (§11.7.2.3)

        [Test]
        public void ResolveBaseAttack_RollsParkedAircraftBandHit()
        {
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.ATT_SU25_SV);
            var ab = BuildBase(UnitClassification.AIRB, WeaponType.BASE_AIRBASE);
            var jet1 = BuildAttachedJet();
            var jet2 = BuildAttachedJet();
            Assert.IsTrue(ab.AddAirUnit(jet1), "jet1 attaches to the airbase");
            Assert.IsTrue(ab.AddAirUnit(jet2), "jet2 attaches to the airbase");

            var ctx = new BaseAttackContext { BaseTerrain = TerrainType.Clear };
            const int v = 6;

            // Parked band = the strike's Δ-band (effGA − base GAD) shifted by ParkedHitBonus (0 — RAMP_STRIKE is
            // dormant, no live carrier). Flat roll: no multipliers, no terrain.
            var lane = CombatResolver.BuildBaseForwardLane(strike, ab, ctx);
            var band = CombatMath.ShiftBand(
                CombatMath.DeltaBand(lane.FirerAttack - lane.TargetDefense), strike.ActiveParkedHitBonus);
            int expParked = CombatMath.RollBandDamage(band, new FixedRollRandom(v));

            float j1hp0 = jet1.HitPoints.Current;
            var r = CombatResolver.ResolveBaseAttack(strike, ab, ctx, new FixedRollRandom(v));

            Assert.AreEqual(0, strike.ActiveParkedHitBonus, "Su-25 carries no RAMP_STRIKE rider (dormant — no live carrier)");
            Assert.AreEqual(2, r.ParkedAircraft.Length, "one parked roll per attached aircraft");
            Assert.AreEqual(expParked, r.ParkedAircraft[0].Damage, "parked hit = flat band roll of the strike Δ-band");
            Assert.AreEqual(j1hp0 - expParked, jet1.HitPoints.Current, TOL, "parked damage applied to the attached aircraft");
            Assert.AreEqual(jet1.UnitID, r.ParkedAircraft[0].AircraftId, "parked result keyed by aircraft id");
        }

        #endregion // Parked aircraft band roll
    }
}
