using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M4 validation: indirect fire + counter-battery (§7.13). Firer is a 2S1 SPA (IndirectRange SHORT = 4)
    /// at (3,6); targets sit due east so hex distance is the column gap. Counter-battery fires only when the
    /// target is an artillery class whose IR reaches the firer. DB-backed (real artillery profiles); no map
    /// needed — the resolver reads unit positions directly.
    /// </summary>
    [TestFixture]
    public class IndirectResolverTests : BaseTestFixture
    {
        private const float TOL = 0.001f;
        private static readonly Position2D FirerHex = new Position2D(3, 6);

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();
        }

        private CombatUnit BuildUnitAt(UnitClassification cls, WeaponType deployed, Position2D pos)
        {
            var u = new CombatUnit("U", cls, UnitRole.GroundCombat, Side.Player, Nationality.USSR);
            u.RegimentProfile.InitializeRegimentProfile("U", RegimentProfileType.DEP,
                WeaponType.NONE, deployed, WeaponType.NONE);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetExperienceLevel(ExperienceLevel.Trained);
            u.SetPosition(pos);
            return u;
        }

        [Test]
        public void IndirectAttack_NonArtilleryTarget_DealsForwardDamage_NoCounterBattery()
        {
            var firer = BuildUnitAt(UnitClassification.SPA, WeaponType.SPA_2S1_SV, FirerHex);
            var target = BuildUnitAt(UnitClassification.INF, WeaponType.INF_REG_SV, new Position2D(6, 6));
            var ctx = new IndirectAttackContext { TargetTerrain = TerrainType.Clear, FirerTerrain = TerrainType.Clear };
            const int v = 8;

            // Expected forward lane: axis by target (Soft) → firer SA vs infantry SD; indirect, no deployment mult.
            TargetClass axis = target.ActiveTargetClass;
            var expFwd = new LaneInput
            {
                FirerAttack = firer.GetAttackStatVsClass(axis),
                TargetDefense = target.GetDefenseStatVsClass(axis),
                FirerQualityMult = firer.GetCombatQualityMultiplier(),
                AttackType = AttackType.Indirect,
                TargetTerrain = TerrainType.Clear,
            };
            int exp = CombatEngine.ResolveLane(expFwd, new FixedRollRandom(v));

            float firerHp0 = firer.HitPoints.Current;
            float targetHp0 = target.HitPoints.Current;
            var res = CombatResolver.ResolveIndirectAttack(firer, target, ctx, new FixedRollRandom(v));

            Assert.AreEqual(exp, res.DamageToTarget, "forward indirect HP");
            Assert.IsFalse(res.CounterBatteryFired, "an infantry target cannot counter-battery");
            Assert.AreEqual(0, res.DamageToFirer);
            Assert.AreEqual(firerHp0, firer.HitPoints.Current, TOL, "no counter-battery → firer unharmed");
            Assert.AreEqual(targetHp0 - exp, target.HitPoints.Current, TOL);
            Assert.IsTrue(res.FirerRevealed, "firing exposes the battery (§7.13.5.4)");
        }

        [Test]
        public void IndirectAttack_ArtilleryTargetInRange_FiresCounterBattery()
        {
            var firer = BuildUnitAt(UnitClassification.SPA, WeaponType.SPA_2S1_SV, FirerHex);
            var target = BuildUnitAt(UnitClassification.SPA, WeaponType.SPA_2S1_SV, new Position2D(6, 6)); // dist 3 ≤ IR 4
            var ctx = new IndirectAttackContext { TargetTerrain = TerrainType.Clear, FirerTerrain = TerrainType.Clear };
            float firerHp0 = firer.HitPoints.Current;

            var res = CombatResolver.ResolveIndirectAttack(firer, target, ctx, new FixedRollRandom(8));

            Assert.IsTrue(res.CounterBatteryFired, "artillery in range returns fire");
            Assert.Greater(res.DamageToFirer, 0, "counter-battery deals damage");
            Assert.AreEqual(firerHp0 - res.DamageToFirer, firer.HitPoints.Current, TOL, "firer takes counter-battery HP");
        }

        [Test]
        public void IsCounterBatteryEligible_RequiresArtilleryWithinItsIndirectRange()
        {
            var firer = BuildUnitAt(UnitClassification.SPA, WeaponType.SPA_2S1_SV, FirerHex);
            var artInRange = BuildUnitAt(UnitClassification.SPA, WeaponType.SPA_2S1_SV, new Position2D(6, 6));   // dist 3
            var artOutOfRange = BuildUnitAt(UnitClassification.SPA, WeaponType.SPA_2S1_SV, new Position2D(10, 6)); // dist 7
            var infantry = BuildUnitAt(UnitClassification.INF, WeaponType.INF_REG_SV, new Position2D(6, 6));

            Assert.IsTrue(CombatResolver.IsCounterBatteryEligible(firer, artInRange), "SPA at 3 ≤ IR 4");
            Assert.IsFalse(CombatResolver.IsCounterBatteryEligible(firer, artOutOfRange), "SPA at 7 > IR 4");
            Assert.IsFalse(CombatResolver.IsCounterBatteryEligible(firer, infantry), "infantry is not artillery");
        }

        [Test]
        public void IsInIndirectRange_IsOneToIR()
        {
            var firer = BuildUnitAt(UnitClassification.SPA, WeaponType.SPA_2S1_SV, FirerHex); // IR 4
            Assert.IsTrue(CombatResolver.IsInIndirectRange(firer, new Position2D(6, 6)),  "dist 3 ∈ [1,4]");
            Assert.IsFalse(CombatResolver.IsInIndirectRange(firer, new Position2D(10, 6)), "dist 7 > 4");
            Assert.IsFalse(CombatResolver.IsInIndirectRange(firer, FirerHex),              "dist 0 < 1 (self)");
        }
    }
}
