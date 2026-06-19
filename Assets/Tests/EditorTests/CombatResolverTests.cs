using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Integration validation: `CombatResolver.ResolveDirectAttack` drives the engine on two real units and
    /// applies HP. The cross-check builds the two lanes independently from the spec (axis by target class,
    /// attacker no-deployment, defender return fire with deployment, attacker-hex inert) and confirms the
    /// resolver produces the same per-lane HP — pinning the adapter's axis selection and multiplier assembly.
    ///
    /// Dice use FixedRollRandom (same value for every die), so the cross-check is independent of how many dice
    /// each band/terrain expression happens to consume.
    /// </summary>
    [TestFixture]
    public class CombatResolverTests : BaseTestFixture
    {
        private const float TOL = 0.001f;

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize(); // resolver reads real profiles via the DB
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

        [Test]
        public void ResolveDirectAttack_AppliesPerLaneEngineDamage_AxisFromTargetClass()
        {
            var attacker = BuildUnit(UnitClassification.TANK, WeaponType.TANK_T55A_SV); // Hard
            var defender = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);    // Soft
            var ctx = new DirectAttackContext { DefenderTerrain = TerrainType.Clear };

            const int v = 4;

            // Expected forward lane (attacker → defender): axis = defender's class (Soft) → attacker SA vs infantry SD.
            TargetClass fwdAxis = defender.ActiveTargetClass;
            var expForward = new LaneInput
            {
                FirerAttack = attacker.GetAttackStatVsClass(fwdAxis),
                TargetDefense = defender.GetDefenseStatVsClass(fwdAxis),
                FirerQualityMult = attacker.GetCombatQualityMultiplier(),
                AttackType = AttackType.Direct,
                TargetTerrain = TerrainType.Clear,
            };

            // Expected return lane (defender → attacker): axis = attacker's class (Hard) → infantry HA vs tank HD;
            // deployment applies; attacker's hex is inert (terrain bypassed).
            TargetClass retAxis = attacker.ActiveTargetClass;
            var expReturn = new LaneInput
            {
                FirerAttack = defender.GetAttackStatVsClass(retAxis),
                TargetDefense = attacker.GetDefenseStatVsClass(retAxis),
                FirerQualityMult = defender.GetCombatQualityMultiplier(),
                FirerDeploymentMod = defender.GetDeploymentCombatMod(),
                FirerIsDefender = true,
                AttackType = AttackType.Direct,
                BypassTerrainBlock = true,
            };

            int expToDefender = CombatEngine.ResolveLane(expForward, new FixedRollRandom(v));
            int expToAttacker = CombatEngine.ResolveLane(expReturn, new FixedRollRandom(v));

            float defHp0 = defender.HitPoints.Current;
            float atkHp0 = attacker.HitPoints.Current;

            var result = CombatResolver.ResolveDirectAttack(attacker, defender, ctx, new FixedRollRandom(v));

            Assert.AreEqual(expToDefender, result.DamageToDefender, "forward lane HP (attacker SA vs Soft defender)");
            Assert.AreEqual(expToAttacker, result.DamageToAttacker, "return lane HP (infantry HA vs Hard attacker)");
            Assert.AreEqual(defHp0 - expToDefender, defender.HitPoints.Current, TOL, "defender HP applied");
            Assert.AreEqual(atkHp0 - expToAttacker, attacker.HitPoints.Current, TOL, "attacker HP applied (return fire §6.12)");
        }

        [Test]
        public void ResolveDirectAttack_FlagsDefenderDestroyed_AtZeroHP()
        {
            var attacker = BuildUnit(UnitClassification.TANK, WeaponType.TANK_T55A_SV);
            var defender = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);

            // Bring the defender to 1 HP, then deliver a guaranteed connecting hit (max die on a connecting band).
            defender.TakeDamage(defender.HitPoints.Max - 1f);
            var ctx = new DirectAttackContext { DefenderTerrain = TerrainType.Clear };

            var result = CombatResolver.ResolveDirectAttack(attacker, defender, ctx, new FixedRollRandom(8));

            Assert.GreaterOrEqual(result.DamageToDefender, 1, "a connecting hit lands ≥ 1");
            Assert.IsTrue(result.DefenderDestroyed, "1 HP − connecting hit → destroyed");
            Assert.AreEqual(0f, defender.HitPoints.Current, TOL, "HP floored at 0");
        }

        /// <summary>Same unit at a known position + side, so flank geometry is deterministic.</summary>
        private CombatUnit BuildUnitAt(UnitClassification cls, WeaponType deployed, Position2D pos, Side side)
        {
            var nat = side == Side.Player ? Nationality.USSR : Nationality.MJ;
            var u = new CombatUnit("U", cls, UnitRole.GroundCombat, side, nat);
            u.RegimentProfile.InitializeRegimentProfile("U", RegimentProfileType.DEP,
                WeaponType.NONE, deployed, WeaponType.NONE);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetExperienceLevel(ExperienceLevel.Trained);
            u.SetPosition(pos);
            return u;
        }

        [Test]
        public void AutoFlank_FromFlankArc_RaisesDamageAndLowersStandValue()
        {
            var defPos = new Position2D(6, 6);
            var ctx = new DirectAttackContext { DefenderTerrain = TerrainType.Clear };

            // Player defender faces W by default → front arc {SW, W, NW}. Frontal attacker sits on the W edge.
            var defFront = BuildUnitAt(UnitClassification.INF, WeaponType.INF_REG_SV, defPos, Side.Player);
            var atkFront = BuildUnitAt(UnitClassification.TANK, WeaponType.TANK_T55A_SV,
                HexMapUtil.GetNeighborPosition(defPos, HexDirection.W), Side.AI);
            var front = CombatResolver.ResolveDirectAttack(atkFront, defFront, ctx, new FixedRollRandom(8));

            // Flank attacker on the E edge (rear arc for a W-facing unit).
            var defFlank = BuildUnitAt(UnitClassification.INF, WeaponType.INF_REG_SV, defPos, Side.Player);
            var atkFlank = BuildUnitAt(UnitClassification.TANK, WeaponType.TANK_T55A_SV,
                HexMapUtil.GetNeighborPosition(defPos, HexDirection.E), Side.AI);
            var flank = CombatResolver.ResolveDirectAttack(atkFlank, defFlank, ctx, new FixedRollRandom(8));

            Assert.GreaterOrEqual(flank.DamageToDefender, front.DamageToDefender, "flank ×1.15 ≥ frontal damage");
            Assert.Less(flank.DefenderStandValue, front.DefenderStandValue, "flank lowers defender Stand Value");
        }

        #region Ambush (§6.9)

        [Test]
        public void ResolveAmbush_OneWayScaledDamage_NoReturnFire()
        {
            var ambusher = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);
            var mover = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);
            var ctx = new DirectAttackContext { DefenderTerrain = TerrainType.Clear };
            const int v = 8;

            // Expected ambush lane: axis by the mover's class, ambusher quality only, ×1.5, terrain bypassed.
            TargetClass axis = mover.ActiveTargetClass;
            var expLane = new LaneInput
            {
                FirerAttack = ambusher.GetAttackStatVsClass(axis),
                TargetDefense = mover.GetDefenseStatVsClass(axis),
                FirerQualityMult = ambusher.GetCombatQualityMultiplier(),
                FirerDeploymentMod = ambusher.GetDeploymentCombatMod(),
                FirerIsDefender = true,
                AttackType = AttackType.Direct,
                BypassTerrainBlock = true,
                PostStackScalar = GameData.AMBUSH_BONUS_MULT,
            };
            int exp = CombatEngine.ResolveLane(expLane, new FixedRollRandom(v));

            float ambusherHp0 = ambusher.HitPoints.Current;
            float moverHp0 = mover.HitPoints.Current;
            var res = CombatResolver.ResolveAmbush(ambusher, mover, ctx, new FixedRollRandom(v));

            Assert.AreEqual(exp, res.DamageToMover, "×1.5 one-way ambush damage");
            Assert.AreEqual(ambusherHp0, ambusher.HitPoints.Current, TOL, "ambusher takes no return fire (§6.9.5)");
            Assert.AreEqual(moverHp0 - exp, mover.HitPoints.Current, TOL, "mover HP applied");
        }

        [Test]
        public void ResolveAmbush_EmbarkedMover_AddsEmbarkmentMalus()
        {
            var ambusher = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);
            var mover = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);
            mover.SetDeploymentPosition(DeploymentPosition.Embarked); // no embarked profile → deployed stats stand in
            var ctx = new DirectAttackContext { DefenderTerrain = TerrainType.Clear };
            const int v = 8;

            TargetClass axis = mover.ActiveTargetClass;
            var expLane = new LaneInput
            {
                FirerAttack = ambusher.GetAttackStatVsClass(axis),
                TargetDefense = mover.GetDefenseStatVsClass(axis),
                FirerQualityMult = ambusher.GetCombatQualityMultiplier(),
                FirerDeploymentMod = ambusher.GetDeploymentCombatMod(),
                FirerIsDefender = true,
                AttackType = AttackType.Direct,
                BypassTerrainBlock = true,
                BandShift = 1,                                       // embarkment +1 band (§7.10.1.1)
                PostStackScalar = GameData.AMBUSH_BONUS_MULT * 2.0f, // ambush ×1.5 × embarkment ×2 (§9.10.7.2)
            };
            int exp = CombatEngine.ResolveLane(expLane, new FixedRollRandom(v));

            var res = CombatResolver.ResolveAmbush(ambusher, mover, ctx, new FixedRollRandom(v));
            Assert.AreEqual(exp, res.DamageToMover, "embarked ambush = +1 band and ×1.5×2");
        }

        [Test]
        public void ResolveAmbush_DugInAmbusher_HitsHarder()
        {
            var ctx = new DirectAttackContext { DefenderTerrain = TerrainType.Clear };

            var deployedAmbusher = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV); // Deployed → ×1.0
            var fortifiedAmbusher = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);
            fortifiedAmbusher.SetDeploymentPosition(DeploymentPosition.Fortified);           // → ×1.3 COMBAT_MOD
            var mover1 = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);
            var mover2 = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV);

            var rDeployed = CombatResolver.ResolveAmbush(deployedAmbusher, mover1, ctx, new FixedRollRandom(8));
            var rFortified = CombatResolver.ResolveAmbush(fortifiedAmbusher, mover2, ctx, new FixedRollRandom(8));

            Assert.Greater(rFortified.DamageToMover, rDeployed.DamageToMover,
                "a dug-in ambusher hits harder via its deployment COMBAT_MOD");
        }

        #endregion // Ambush
    }
}
