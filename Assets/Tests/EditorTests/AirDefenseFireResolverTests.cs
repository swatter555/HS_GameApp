using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M6 validation (pure-resolver core): <see cref="CombatResolver.ResolveAirDefenseFire"/> is the ground-to-air
    /// opportunity-fire DAMAGE primitive (§11.8.1) — one-way (§7.12.3), attacker pipeline (no deployment mult, §7.12.2),
    /// GroundBalanceMod (the firer is a ground unit, §7.7.10), no terrain/OL aloft. The Δ axis is target-driven: a
    /// helicopter (HELO or an air-mobile unit in EmbarkedHelo transit) is engaged GAT−GAD (§7A.14); a fixed-wing
    /// aircraft GAT−(MAN+SUR)/2. The GAT&lt;6 interdiction gate (§11.8.2) suppresses the shot. Cross-checks rebuild the
    /// lane independently with <see cref="FixedRollRandom"/> so the asserted numbers track the engine.
    /// </summary>
    [TestFixture]
    public class AirDefenseFireResolverTests : BaseTestFixture
    {
        private const float TOL = 0.001f;

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();
        }

        private CombatUnit Build(string name, UnitClassification cls, UnitRole role, WeaponType deployed)
        {
            var u = new CombatUnit(name, cls, role, Side.Player, Nationality.USSR);
            u.RegimentProfile.InitializeRegimentProfile(name, RegimentProfileType.DEP,
                WeaponType.NONE, deployed, WeaponType.NONE);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetExperienceLevel(ExperienceLevel.Trained);
            return u;
        }

        #region Fixed-wing axis (GAT − (MAN+SUR)/2)

        [Test]
        public void ResolveAirDefenseFire_FixedWing_UsesManSurAxis()
        {
            var sam = Build("SAM", UnitClassification.SAM, UnitRole.AirDefenseArea, WeaponType.SAM_S75_SV);
            var jet = Build("Jet", UnitClassification.ATT, UnitRole.AirGroundAttack, WeaponType.ATT_SU25_SV);
            const int v = 4;

            var expLane = CombatResolver.BuildAirDefenseFireLane(sam, jet);
            Assert.AreEqual(sam.ActiveGroundAirAttack, expLane.FirerAttack, "firer attack = GAT");
            Assert.AreEqual((jet.ActiveManeuverability + jet.ActiveSurvivability) / 2, expLane.TargetDefense,
                "a fixed-wing target is engaged GAT − (MAN+SUR)/2");
            Assert.IsFalse(expLane.FirerIsAir, "an air-defense unit is a GROUND firer → GroundBalanceMod (§7.7.10)");
            int exp = CombatEngine.ResolveLane(expLane, new FixedRollRandom(v));

            float hp0 = jet.HitPoints.Current;
            var r = CombatResolver.ResolveAirDefenseFire(sam, jet, new FixedRollRandom(v));

            Assert.IsTrue(r.Engaged, "SAM GAT clears the interdiction gate (§11.8.2)");
            Assert.IsFalse(r.HeloAxis, "fixed-wing target → (MAN+SUR)/2 axis");
            Assert.AreEqual(exp, r.DamageToAircraft, "ground-to-air lane damage");
            Assert.AreEqual(hp0 - exp, jet.HitPoints.Current, TOL, "HP applied to the aircraft");
        }

        #endregion // Fixed-wing axis

        #region Helo axis (GAT − GAD, §7A.14)

        [Test]
        public void ResolveAirDefenseFire_Helo_UsesGadAxis()
        {
            var sam = Build("SAM", UnitClassification.SAM, UnitRole.AirDefenseArea, WeaponType.SAM_S75_SV);
            var helo = Build("Helo", UnitClassification.HELO, UnitRole.GroundCombat, WeaponType.HEL_MI24V_SV);

            var lane = CombatResolver.BuildAirDefenseFireLane(sam, helo);
            Assert.Greater(helo.ActiveGroundAirDefense, 0, "a helo carries the GAD evasion proxy (§7B.2)");
            Assert.AreEqual(helo.ActiveGroundAirDefense, lane.TargetDefense, "a helo is engaged GAT − GAD (§7A.14)");

            var r = CombatResolver.ResolveAirDefenseFire(sam, helo, new FixedRollRandom(4));
            Assert.IsTrue(r.HeloAxis, "HELO classification resolves on the GAD axis");
            Assert.IsTrue(r.Engaged);
        }

        [Test]
        public void ResolveAirDefenseFire_EmbarkedHelo_UsesGadAxis()
        {
            // An air-mobile unit caught mid-transit in EmbarkedHelo is engaged on the helo (GAD) axis (§11.8.9 / §5.13.2.4).
            var sam = Build("SAM", UnitClassification.SAM, UnitRole.AirDefenseArea, WeaponType.SAM_S75_SV);
            var am = Build("AM", UnitClassification.AM, UnitRole.GroundCombat, WeaponType.INF_AM_SV);
            am.SetCurrentEmbarkmentState(EmbarkmentState.EmbarkedHelo);

            Assert.IsTrue(CombatResolver.IsHeloAirDefenseTarget(am), "EmbarkedHelo transit → helo axis");
            var lane = CombatResolver.BuildAirDefenseFireLane(sam, am);
            Assert.AreEqual(am.ActiveGroundAirDefense, lane.TargetDefense, "embarked-helo target engaged GAT − GAD");
            Assert.AreNotEqual((am.ActiveManeuverability + am.ActiveSurvivability) / 2, lane.TargetDefense,
                "the GAD axis is taken, NOT the fixed-wing (MAN+SUR)/2 axis");
        }

        #endregion // Helo axis

        #region GAT interdiction gate (§11.8.2)

        [Test]
        public void ResolveAirDefenseFire_BelowGatGate_DoesNotEngage()
        {
            var tank = Build("Tank", UnitClassification.TANK, UnitRole.GroundCombat, WeaponType.TANK_T55A_SV); // GAT 0
            var jet = Build("Jet", UnitClassification.ATT, UnitRole.AirGroundAttack, WeaponType.ATT_SU25_SV);

            Assert.Less(tank.ActiveGroundAirAttack, GameData.GAT_INTERDICT_THRESHOLD, "a tank's GAT is below the gate");
            float hp0 = jet.HitPoints.Current;
            var r = CombatResolver.ResolveAirDefenseFire(tank, jet, new FixedRollRandom(8));

            Assert.IsFalse(r.Engaged, "GAT < 6 → no engagement (§11.8.2)");
            Assert.AreEqual(0, r.DamageToAircraft, "no shot fired");
            Assert.AreEqual(hp0, jet.HitPoints.Current, TOL, "no HP applied below the gate");
        }

        #endregion // GAT interdiction gate

        #region Outcome reporting

        [Test]
        public void ResolveAirDefenseFire_FlagsAircraftDestroyed_AtZeroHp()
        {
            var sam = Build("SAM", UnitClassification.SAM, UnitRole.AirDefenseArea, WeaponType.SAM_S75_SV);
            var jet = Build("Jet", UnitClassification.ATT, UnitRole.AirGroundAttack, WeaponType.ATT_SU25_SV);
            jet.TakeDamage(jet.HitPoints.Max - 1f); // 1 HP left

            var r = CombatResolver.ResolveAirDefenseFire(sam, jet, new FixedRollRandom(8));

            Assert.IsTrue(r.AircraftDestroyed, "a 1-HP aircraft is downed by a connecting AD shot");
            Assert.LessOrEqual(jet.HitPoints.Current, 0f);
        }

        #endregion // Outcome reporting
    }
}
