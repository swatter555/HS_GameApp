using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using NUnit.Framework;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Integration tests for the ground direct-attack orchestrator (GroundCombatAction.Execute) — the model-layer
    /// caller that wires the green resolvers into one player action: validate → spend the action economy (§8.2.1)
    /// → engagement (§7.7.3) → probabilistic degradation (§7.15.3/.5) → displacement (§7.9) → removal/reporting.
    /// Real weapon profiles (so the engine reads real stats); seeded dice (FixedRollRandom) for stable outcomes.
    /// </summary>
    [TestFixture]
    public class GroundCombatActionTests : BaseTestFixture
    {
        private const float TOL = 0.001f;

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();
        }

        public override void SetUp()
        {
            base.SetUp();
            GameManager.ClearAll();
            GameManager.InvalidateOccupancy();
            GameDataManager.CurrentHexMap = CreateClearMap();
        }

        #region Helpers

        private HexMap CreateClearMap(int width = 16, int height = 16)
        {
            var map = new HexMap("TestMap", MapConfig.Small);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var hex = new HexTile(new Vector2Int(x, y));
                    hex.SetTerrain(TerrainType.Clear);
                    map.SetHexAt(hex);
                }
            map.BuildNeighborRelationships();
            return map;
        }

        private CombatUnit Build(UnitClassification cls, WeaponType deployed, Side side, Position2D pos,
            SpottedLevel spotted = SpottedLevel.Level0, int mp = 12, float supply = 5f)
        {
            var nat = side == Side.Player ? Nationality.USSR : Nationality.MJ;
            var u = new CombatUnit("U", cls, UnitRole.GroundCombat, side, nat);
            u.RegimentProfile.InitializeRegimentProfile("U", RegimentProfileType.DEP,
                WeaponType.NONE, deployed, WeaponType.NONE);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetExperienceLevel(ExperienceLevel.Trained);
            u.SetEfficiencyLevel(EfficiencyLevel.FullOperations);
            u.SetPosition(pos);
            u.MovementPoints.SetMax(mp);
            u.MovementPoints.SetCurrent(mp);
            u.DaysSupply.SetMax(5f);
            u.DaysSupply.SetCurrent(supply);
            u.SetSpottedLevel(spotted);
            GameManager.RegisterCombatUnit(u);
            return u;
        }

        private CombatUnit Tank(Side side, Position2D pos, SpottedLevel spotted = SpottedLevel.Level0) =>
            Build(UnitClassification.TANK, WeaponType.TANK_T55A_SV, side, pos, spotted);

        private CombatUnit Infantry(Side side, Position2D pos, SpottedLevel spotted = SpottedLevel.Level0) =>
            Build(UnitClassification.INF, WeaponType.INF_REG_SV, side, pos, spotted);

        #endregion // Helpers

        #region Validation (rejected before any cost)

        [Test]
        public void Reject_NotAdjacent_NoCostPaid()
        {
            var attacker = Tank(Side.Player, new Position2D(5, 5));
            var defender = Infantry(Side.AI, new Position2D(5, 9), SpottedLevel.Level2); // distance 4

            var r = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsFalse(r.Executed, "Non-adjacent target is rejected");
            StringAssert.Contains("adjacent", r.Reason);
            Assert.AreEqual(1f, attacker.CombatActions.Current, "No CombatAction spent on a rejected attack");
            Assert.AreEqual(12f, attacker.MovementPoints.Current, "No MP spent on a rejected attack");
        }

        [Test]
        public void Reject_FriendlyTarget()
        {
            var attacker = Tank(Side.Player, new Position2D(5, 5));
            var friend = Infantry(Side.Player, new Position2D(6, 5));

            var r = GroundCombatAction.Execute(attacker, friend, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsFalse(r.Executed);
            StringAssert.Contains("friendly", r.Reason);
        }

        [Test]
        public void Reject_UnspottedEnemy()
        {
            var attacker = Tank(Side.Player, new Position2D(5, 5));
            var defender = Infantry(Side.AI, new Position2D(6, 5)); // SpottedLevel0 by default

            var r = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsFalse(r.Executed, "Cannot strike what you cannot see");
            StringAssert.Contains("spotted", r.Reason);
        }

        #endregion // Validation

        #region Action economy + engagement

        [Test]
        public void Success_SpendsActionEconomy_AndMarksFought()
        {
            var attacker = Tank(Side.Player, new Position2D(5, 5));
            var defender = Infantry(Side.AI, new Position2D(6, 5), SpottedLevel.Level2);
            float combatCost = attacker.GetCombatMovementCost();

            var r = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsTrue(r.Executed, "Adjacent spotted enemy → attack executes");
            Assert.AreEqual(0f, attacker.CombatActions.Current, "1 CombatAction spent (§8.2.1)");
            Assert.AreEqual(12f - combatCost, attacker.MovementPoints.Current, TOL, "25% max MP spent (§8.2.1)");
            Assert.IsTrue(attacker.HasFoughtThisTurn, "attacker flagged fought (§7.15.8.3)");
            Assert.IsTrue(defender.HasFoughtThisTurn, "defender flagged fought");
        }

        [Test]
        public void Success_AppliesDamage_AndProbabilisticDegradation()
        {
            // FixedRollRandom(1): a minimal connecting hit (defender holds, survives), and every 1d100 degradation
            // roll = 1 ≤ threshold → both sides drop one Efficiency tier (§7.15.3) and lose 1 supply (§7.15.5).
            var attacker = Tank(Side.Player, new Position2D(5, 5));
            var defender = Infantry(Side.AI, new Position2D(6, 5), SpottedLevel.Level2);
            float defHp0 = defender.HitPoints.Current;

            var r = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsTrue(r.Executed);
            Assert.AreEqual(StandOutcome.Hold, r.DefenderOutcome, "small hit + low stand roll → holds");
            Assert.GreaterOrEqual(r.DamageToDefender, 1, "a connecting hit lands ≥ 1");
            Assert.AreEqual(defHp0 - r.DamageToDefender, defender.HitPoints.Current, TOL, "defender HP applied");

            Assert.AreEqual(EfficiencyLevel.CombatOperations, attacker.EfficiencyLevel, "attacker Full → Combat (§7.15.3)");
            Assert.AreEqual(EfficiencyLevel.CombatOperations, defender.EfficiencyLevel, "defender Full → Combat (§7.15.3)");
            Assert.AreEqual(4f, attacker.DaysSupply.Current, TOL, "attacker lost 1 supply (§7.15.5)");
            Assert.AreEqual(4f, defender.DaysSupply.Current, TOL, "defender lost 1 supply (§7.15.5)");
        }

        #endregion // Action economy + engagement

        #region Displacement + destruction

        [Test]
        public void Success_HardHit_DislodgesDefender_AndOpensAutomaticAdvance()
        {
            var defPos = new Position2D(8, 8);
            var defender = Infantry(Side.AI, defPos, SpottedLevel.Level2);
            var atkPos = HexMapUtil.GetNeighborPosition(defPos, HexDirection.W);
            var attacker = Tank(Side.Player, atkPos);

            // FixedRollRandom(8): a heavy hit → high Shock → low SV; stand roll 8 vacates the hex (retreat/rout, or a
            // shatter-quit). Not lethal to a full-HP infantry, so this exercises the displacement + AA wiring, not a kill.
            var r = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, new FixedRollRandom(8));

            Assert.IsTrue(r.Executed);
            Assert.AreNotEqual(StandOutcome.Hold, r.DefenderOutcome, "heavy hit dislodges the defender");
            Assert.IsTrue(r.DefenderMoved || r.DefenderRemovedFromMap, "defender vacates the hex (retreat/rout or shatter-quit)");
            Assert.IsFalse(r.DefenderDestroyed, "a full-HP infantry is not destroyed by the hit");
            Assert.IsTrue(r.AutomaticAdvanceAvailable, "a vacated hex opens Automatic Advance (§7.9.9)");
            Assert.AreEqual(defPos.X, r.VacatedHex.X);
            Assert.AreEqual(defPos.Y, r.VacatedHex.Y);
        }

        [Test]
        public void Success_LethalHit_Destroys_ReportsPrestige_AndUnregisters()
        {
            var defPos = new Position2D(6, 5);
            var defender = Infantry(Side.AI, defPos, SpottedLevel.Level2);
            defender.TakeDamage(defender.HitPoints.Max - 1f); // 1 HP
            var attacker = Tank(Side.Player, new Position2D(5, 5));
            string defId = defender.UnitID;

            var r = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, new FixedRollRandom(8));

            Assert.IsTrue(r.Executed);
            Assert.IsTrue(r.DefenderDestroyed, "1 HP − connecting hit → destroyed");
            Assert.IsTrue(r.DefenderRemovedFromMap);
            Assert.IsTrue(r.AutomaticAdvanceAvailable, "destruction vacates the hex (§7.9.9.2)");
            Assert.AreEqual(defPos.X, r.VacatedHex.X);
            Assert.AreEqual(defPos.Y, r.VacatedHex.Y);
            Assert.Greater(r.PrestigeOwedToAttacker, 0, "half purchase cost owed on a kill (§18.2.3)");
            Assert.IsNull(GameManager.GetCombatUnit(defId), "a destroyed unit is unregistered");
        }

        #endregion // Displacement + destruction
    }
}
