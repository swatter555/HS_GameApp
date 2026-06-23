using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M8 validation: `CombatResolver.ResolveAirStrike` drives the §7.7.1 engine in its Airstrike configuration on
    /// real units (effGA vs GAD, OL/9, terrain, AirBalanceMod, fixed-wing deployment-skip) and applies HP. The
    /// cross-check builds the airstrike lane independently and confirms the resolver produces the same HP — pinning
    /// the effGA/GAD axis, the STANDOFF_CRUISE_MISSILE GAD-ignore branch (§11.6.1.1), and the WW band shift
    /// (§11.4.8.6). FixedRollRandom (same value for every die) keeps the cross-check independent of dice count.
    /// </summary>
    [TestFixture]
    public class AirStrikeResolverTests : BaseTestFixture
    {
        private const float TOL = 0.001f;

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();
        }

        private CombatUnit BuildUnit(UnitClassification cls, WeaponType deployed)
        {
            var u = new CombatUnit("U", cls, UnitRole.GroundCombat, Side.Player, Nationality.USSR);
            u.RegimentProfile.InitializeRegimentProfile("U", RegimentProfileType.DEP,
                WeaponType.NONE, deployed, WeaponType.NONE);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetExperienceLevel(ExperienceLevel.Trained);
            return u;
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

        #region Core air-strike lane (§11.6.1)

        [Test]
        public void ResolveAirStrike_AppliesEffGaVsGadEngineDamage()
        {
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.ATT_SU25_SV);
            var target = BuildUnit(UnitClassification.TANK, WeaponType.TANK_T55A_SV); // Hard, GAD 7
            var ctx = new AirStrikeContext { TargetTerrain = TerrainType.Clear };

            const int v = 4;

            // Expected airstrike lane: effGA (with Rule-B riders) vs the target's single GAD, OL/9, air balance.
            var expLane = new LaneInput
            {
                FirerAttack = strike.GetEffectiveGroundAttack(target.ActiveTargetClass, target.IsBase),
                TargetDefense = target.ActiveGroundAirDefense,
                FirerQualityMult = strike.GetCombatQualityMultiplier(),
                OrdnanceLoad = strike.ActiveOrdnanceLoad,
                AttackType = AttackType.Airstrike,
                FirerIsAir = true,
                TargetTerrain = TerrainType.Clear,
            };
            int exp = CombatEngine.ResolveLane(expLane, new FixedRollRandom(v));

            float hp0 = target.HitPoints.Current;
            var r = CombatResolver.ResolveAirStrike(strike, target, ctx, new FixedRollRandom(v));

            Assert.AreEqual(exp, r.DamageToTarget, "effGA vs GAD airstrike lane (OL, terrain, air balance)");
            Assert.AreEqual(hp0 - exp, target.HitPoints.Current, TOL, "HP applied to target");
            Assert.IsFalse(r.GadIgnored, "Su-25 is not a standoff-cruise carrier");
        }

        [Test]
        public void ResolveAirStrike_StandoffCruise_IgnoresGad()
        {
            // Tu-22 carries STANDOFF_CRUISE_MISSILE → IgnoreAirDefense (now Live): the GAD term is zeroed (§11.6.1.1).
            var strike = BuildStrike(UnitClassification.BMB, WeaponType.BMB_TU22_SV);
            var target = BuildUnit(UnitClassification.TANK, WeaponType.TANK_T55A_SV);
            var ctx = new AirStrikeContext { TargetTerrain = TerrainType.Clear };

            const int v = 4;

            var expLane = new LaneInput
            {
                FirerAttack = strike.GetEffectiveGroundAttack(target.ActiveTargetClass, target.IsBase),
                TargetDefense = 0, // GAD ignored
                FirerQualityMult = strike.GetCombatQualityMultiplier(),
                OrdnanceLoad = strike.ActiveOrdnanceLoad,
                AttackType = AttackType.Airstrike,
                FirerIsAir = true,
                TargetTerrain = TerrainType.Clear,
            };
            int exp = CombatEngine.ResolveLane(expLane, new FixedRollRandom(v));

            var r = CombatResolver.ResolveAirStrike(strike, target, ctx, new FixedRollRandom(v));

            Assert.IsTrue(r.GadIgnored, "Tu-22 STANDOFF_CRUISE_MISSILE ignores target GAD");
            Assert.AreEqual(exp, r.DamageToTarget, "cruise lane drops the GAD term (Δ = effGA)");
        }

        #endregion // Core air-strike lane

        #region Wild Weasel band shift (§11.4.8.6)

        [Test]
        public void BuildAirStrikeLane_WildWeaselAlive_SetsBandShift()
        {
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.ATT_SU25_SV);
            var target = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);

            var noWw = CombatResolver.BuildAirStrikeLane(strike, target,
                new AirStrikeContext { TargetTerrain = TerrainType.Clear, WildWeaselAlive = false });
            var ww = CombatResolver.BuildAirStrikeLane(strike, target,
                new AirStrikeContext { TargetTerrain = TerrainType.Clear, WildWeaselAlive = true });

            Assert.AreEqual(0, noWw.BandShift, "no WW → no band shift");
            Assert.AreEqual(1, ww.BandShift, "surviving WW shifts the strike band up one (§11.4.8.6)");
        }

        #endregion // Wild Weasel band shift

        #region Embarkment malus (§7.10.1)

        [Test]
        public void ResolveAirStrike_EmbarkedTarget_AddsEmbarkmentMalus()
        {
            // An embarked air-mobile unit caught in transit is a ground target a fixed-wing strike can catch:
            // +1 band and ×2 damage (§7.10.1), exactly as the direct/ambush lanes apply it.
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.ATT_SU25_SV);
            var mover = BuildUnit(UnitClassification.AM, WeaponType.INF_AM_SV);
            mover.SetDeploymentPosition(DeploymentPosition.Embarked); // no embarked profile → deployed stats stand in
            var ctx = new AirStrikeContext { TargetTerrain = TerrainType.Clear };
            const int v = 6;

            var expLane = new LaneInput
            {
                FirerAttack = strike.GetEffectiveGroundAttack(mover.ActiveTargetClass, mover.IsBase),
                TargetDefense = mover.ActiveGroundAirDefense,
                FirerQualityMult = strike.GetCombatQualityMultiplier(),
                OrdnanceLoad = strike.ActiveOrdnanceLoad,
                AttackType = AttackType.Airstrike,
                FirerIsAir = true,
                TargetTerrain = TerrainType.Clear,
                BandShift = 1,           // embarkment +1 band (§7.10.1.1)
                PostStackScalar = 2.0f,  // embarkment ×2 (§7.10.1.2)
            };
            int exp = CombatEngine.ResolveLane(expLane, new FixedRollRandom(v));

            var r = CombatResolver.ResolveAirStrike(strike, mover, ctx, new FixedRollRandom(v));
            Assert.AreEqual(exp, r.DamageToTarget, "embarked airstrike target takes +1 band and ×2");
        }

        [Test]
        public void BuildAirStrikeLane_EmbarkedAndWildWeasel_BandShiftsStack()
        {
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.ATT_SU25_SV);
            var mover = BuildUnit(UnitClassification.AM, WeaponType.INF_AM_SV);
            mover.SetDeploymentPosition(DeploymentPosition.Embarked);

            var lane = CombatResolver.BuildAirStrikeLane(strike, mover,
                new AirStrikeContext { TargetTerrain = TerrainType.Clear, WildWeaselAlive = true });

            Assert.AreEqual(2, lane.BandShift, "WW (+1) and embarkment (+1) band shifts stack");
            Assert.AreEqual(2.0f, lane.PostStackScalar, "embarkment ×2 still applies under WW");
        }

        #endregion // Embarkment malus

        #region Outcome reporting

        [Test]
        public void ResolveAirStrike_FlagsTargetDestroyed_AtZeroHP()
        {
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.ATT_SU25_SV);
            var target = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);
            target.TakeDamage(target.HitPoints.Max - 1f); // 1 HP left

            // Max die → a guaranteed connecting hit ≥ 1 finishes it.
            var r = CombatResolver.ResolveAirStrike(strike, target,
                new AirStrikeContext { TargetTerrain = TerrainType.Clear }, new FixedRollRandom(8));

            Assert.IsTrue(r.TargetDestroyed, "target at 1 HP is destroyed by a connecting strike");
            Assert.LessOrEqual(target.HitPoints.Current, 0f);
        }

        [Test]
        public void ResolveAirStrike_IsDamageOnly_NoForcedMovement()
        {
            // Airstrikes are damage-only: the target keeps its position and posture (no stand check / displacement).
            var strike = BuildStrike(UnitClassification.ATT, WeaponType.ATT_SU25_SV);
            var target = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);
            var pos0 = target.MapPos;
            var posture0 = target.DeploymentPosition;

            CombatResolver.ResolveAirStrike(strike, target,
                new AirStrikeContext { TargetTerrain = TerrainType.Clear }, new FixedRollRandom(4));

            Assert.AreEqual(pos0, target.MapPos, "airstrike does not move the target");
            Assert.AreEqual(posture0, target.DeploymentPosition, "airstrike does not change the target's posture");
        }

        #endregion // Outcome reporting
    }
}
