using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Integration validation: the lane-aware accessors `CombatUnit` exposes to the damage engine — target
    /// class, raw axis stats, the quality multiplier (no deployment), and the defender-only deployment mod.
    /// Uses real WeaponProfileDB profiles so the values are the live catalog ones.
    /// </summary>
    [TestFixture]
    public class CombatUnitIntegrationTests : BaseTestFixture
    {
        private const float TOL = 0.001f;

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize(); // unit accessors read real profiles via the DB
        }

        /// <summary>Builds a Deployed, Trained, full-strength unit around a known WeaponType (Deployed slot only).</summary>
        private CombatUnit BuildUnit(UnitClassification cls, WeaponType deployed)
        {
            var u = new CombatUnit("U", cls, UnitRole.GroundCombat, Side.Player, Nationality.USSR);
            u.RegimentProfile.InitializeRegimentProfile("U", RegimentProfileType.DEP,
                WeaponType.NONE, deployed, WeaponType.NONE);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetExperienceLevel(ExperienceLevel.Trained);
            return u;
        }

        [Test]
        public void ActiveTargetClass_DefaultsFromProfilePrefix()
        {
            Assert.AreEqual(TargetClass.Hard, BuildUnit(UnitClassification.TANK, WeaponType.TANK_T55A_SV).ActiveTargetClass, "TANK = Hard");
            Assert.AreEqual(TargetClass.Soft, BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV).ActiveTargetClass, "INF = Soft");
        }

        [Test]
        public void AxisStats_ReadProfileStatsBySelectedClass()
        {
            var tank = BuildUnit(UnitClassification.TANK, WeaponType.TANK_T55A_SV);
            var p = tank.GetActiveWeaponProfile();
            Assert.IsNotNull(p, "profile resolved from DB");

            Assert.AreEqual(p.HardAttack,  tank.GetAttackStatVsClass(TargetClass.Hard),  "Hard axis → HardAttack");
            Assert.AreEqual(p.SoftAttack,  tank.GetAttackStatVsClass(TargetClass.Soft),  "Soft axis → SoftAttack");
            Assert.AreEqual(p.HardDefense, tank.GetDefenseStatVsClass(TargetClass.Hard), "Hard axis → HardDefense");
            Assert.AreEqual(p.SoftDefense, tank.GetDefenseStatVsClass(TargetClass.Soft), "Soft axis → SoftDefense");
        }

        [Test]
        public void CombatQualityMultiplier_IsStrengthExpEffIcm_WithoutDeployment()
        {
            var tank = BuildUnit(UnitClassification.TANK, WeaponType.TANK_T55A_SV);
            float icm = tank.GetActiveWeaponProfile().ICM;

            // Full HP (≥80% → 1.15) × Full efficiency (1.0) × Trained (1.0) × ICM.
            float expected = GameData.STRENGTH_MOD_FULL * 1.0f * 1.0f * icm;
            Assert.AreEqual(expected, tank.GetCombatQualityMultiplier(), TOL, "quality = Str×Eff×Exp×ICM");

            // Deployment must NOT leak into the quality multiplier — Fortified leaves it unchanged.
            tank.SetDeploymentPosition(DeploymentPosition.Fortified);
            Assert.AreEqual(expected, tank.GetCombatQualityMultiplier(), TOL, "quality excludes deployment (§7.5.2)");
        }

        [Test]
        public void DeploymentCombatMod_MatchesPosture_FixedWingSkips()
        {
            var tank = BuildUnit(UnitClassification.TANK, WeaponType.TANK_T55A_SV);

            tank.SetDeploymentPosition(DeploymentPosition.Deployed);
            Assert.AreEqual(1.0f, tank.GetDeploymentCombatMod(), TOL, "Deployed → 1.0");
            tank.SetDeploymentPosition(DeploymentPosition.HastyDefense);
            Assert.AreEqual(GameData.COMBAT_MOD_HASTY_DEFENSE, tank.GetDeploymentCombatMod(), TOL, "HastyDefense");
            tank.SetDeploymentPosition(DeploymentPosition.Entrenched);
            Assert.AreEqual(GameData.COMBAT_MOD_ENTRENCHED, tank.GetDeploymentCombatMod(), TOL, "Entrenched");
            tank.SetDeploymentPosition(DeploymentPosition.Fortified);
            Assert.AreEqual(GameData.COMBAT_MOD_FORTIFIED, tank.GetDeploymentCombatMod(), TOL, "Fortified");

            // Fixed-wing skip deployment entirely (§10.3c.1).
            var fighter = new CombatUnit("F", UnitClassification.FGT, UnitRole.AirSuperiority, Side.Player, Nationality.USSR);
            Assert.AreEqual(1.0f, fighter.GetDeploymentCombatMod(), TOL, "fixed-wing → 1.0");
        }
    }
}
