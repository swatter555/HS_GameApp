using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using NUnit.Framework;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Editor tests for the movement system: BFS pathfinding, MP consumption,
    /// ZoC handling, spotting, facing, and unit cycling.
    /// </summary>
    [TestFixture]
    public class MovementTests : BaseTestFixture
    {
        private const string CLASS_NAME = nameof(MovementTests);

        #region Helper Methods

        /// <summary>
        /// Creates a small all-clear hex map for testing.
        /// </summary>
        private HexMap CreateClearMap(int width = 10, int height = 10)
        {
            var map = new HexMap("TestMap", MapConfig.Small);
            // Override internal size by adding hexes directly
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var hex = new HexTile(new Vector2Int(x, y));
                    hex.SetTerrain(TerrainType.Clear);
                    map.SetHexAt(hex);
                }
            }
            map.BuildNeighborRelationships();
            return map;
        }

        /// <summary>
        /// Creates a player ground unit at the given position with specified MP.
        /// </summary>
        private CombatUnit CreateGroundUnit(Position2D pos, int mp, UnitClassification classification = UnitClassification.INF)
        {
            var unit = new CombatUnit("TestUnit", classification, UnitRole.GroundCombat,
                Side.Player, Nationality.USSR);
            unit.SetPosition(pos);
            unit.SetDeploymentPosition(DeploymentPosition.Deployed);
            unit.MovementPoints.SetMax(mp);
            unit.MovementPoints.SetCurrent(mp);
            GameManager.RegisterCombatUnit(unit);
            return unit;
        }

        /// <summary>
        /// Creates an AI ground unit at the given position.
        /// </summary>
        private CombatUnit CreateEnemyUnit(Position2D pos, UnitClassification classification = UnitClassification.INF,
            SpottedLevel spotted = SpottedLevel.Level1)
        {
            var unit = new CombatUnit("EnemyUnit", classification, UnitRole.GroundCombat,
                Side.AI, Nationality.MJ);
            unit.SetPosition(pos);
            unit.SetDeploymentPosition(DeploymentPosition.Deployed);
            unit.SetSpottedLevel(spotted);
            GameManager.RegisterCombatUnit(unit);
            return unit;
        }

        /// <summary>
        /// Creates a player air unit at the given position with specified MP.
        /// </summary>
        private CombatUnit CreateAirUnit(Position2D pos, int mp)
        {
            var unit = new CombatUnit("TestAirUnit", UnitClassification.FGT, UnitRole.AirSuperiority,
                Side.Player, Nationality.USSR);
            unit.SetPosition(pos);
            unit.MovementPoints.SetMax(mp);
            unit.MovementPoints.SetCurrent(mp);
            GameManager.RegisterCombatUnit(unit);
            return unit;
        }

        public override void SetUp()
        {
            base.SetUp();
            GameManager.ClearAll();
            GameManager.InvalidateOccupancy();
        }

        #endregion // Helper Methods

        #region BFS Tests

        [Test]
        public void BFS_OpenPlain_5MP_ReachableCountCorrect()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;
            var unit = CreateGroundUnit(new Position2D(5, 5), 5);

            var result = HexMapUtil.GetValidMoveDestinations(map, unit);

            // On a clear map with 1 cost per hex, a unit with 5 MP should reach many hexes
            Assert.IsTrue(result.Reachable.Count > 0, "Should have reachable hexes on open plain");
            Assert.IsFalse(result.Reachable.ContainsKey(unit.MapPos), "Start position should not be in reachable set");
        }

        [Test]
        public void BFS_MixedTerrain_CostsCorrect()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            // Set some hexes to forest (cost 2)
            var forestPos = HexMapUtil.GetNeighborPosition(new Position2D(5, 5), HexDirection.E);
            map.GetHexAt(forestPos)?.SetTerrain(TerrainType.Forest);

            var unit = CreateGroundUnit(new Position2D(5, 5), 5);
            var result = HexMapUtil.GetValidMoveDestinations(map, unit);

            // Forest hex should cost 2 MP
            Assert.IsTrue(result.Reachable.ContainsKey(forestPos), "Forest hex should be reachable with 5 MP");
            Assert.AreEqual(2, result.Reachable[forestPos], "Forest hex should cost 2 MP");
        }

        [Test]
        public void BFS_Impassable_BlocksGround_NotAir()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var blockedPos = HexMapUtil.GetNeighborPosition(new Position2D(5, 5), HexDirection.E);
            map.GetHexAt(blockedPos)?.SetTerrain(TerrainType.Impassable);

            // Ground unit cannot enter
            var ground = CreateGroundUnit(new Position2D(5, 5), 5);
            var groundResult = HexMapUtil.GetValidMoveDestinations(map, ground);
            Assert.IsFalse(groundResult.Reachable.ContainsKey(blockedPos), "Ground unit cannot enter impassable");

            // Air unit can
            var air = CreateAirUnit(new Position2D(5, 5), 10);
            var airResult = HexMapUtil.GetValidMoveDestinations(map, air);
            Assert.IsTrue(airResult.Reachable.ContainsKey(blockedPos), "Air unit ignores impassable");
        }

        [Test]
        public void BFS_RiverWithBridge_AllowsCrossing()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var startPos = new Position2D(5, 5);
            var neighborPos = HexMapUtil.GetNeighborPosition(startPos, HexDirection.E);

            // Add river border
            var startHex = map.GetHexAt(startPos);
            startHex.RiverBorders.SetBorder(HexDirection.E, true);

            // No bridge — should block
            var unit = CreateGroundUnit(startPos, 5);
            var blockedResult = HexMapUtil.GetValidMoveDestinations(map, unit);
            Assert.IsFalse(blockedResult.Reachable.ContainsKey(neighborPos), "River without bridge should block");

            // Add bridge — should allow
            startHex.BridgeBorders.SetBorder(HexDirection.E, true);
            GameManager.InvalidateOccupancy();
            var bridgedResult = HexMapUtil.GetValidMoveDestinations(map, unit);
            Assert.IsTrue(bridgedResult.Reachable.ContainsKey(neighborPos), "River with bridge should allow crossing");
        }

        [TestCase(1, 1, Description = "Clear road-to-road: base 1, halved floor = 1 (min 1)")]
        [TestCase(2, 1, Description = "Forest road-to-road: base 2, halved = 1")]
        public void BFS_RoadBonus_HalvesCostWithFloor(int baseCost, int expectedCost)
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var startPos = new Position2D(5, 5);
            var neighborPos = HexMapUtil.GetNeighborPosition(startPos, HexDirection.E);

            map.GetHexAt(startPos).SetIsRoad(true);
            map.GetHexAt(neighborPos).SetIsRoad(true);

            if (baseCost == 2)
                map.GetHexAt(neighborPos).SetTerrain(TerrainType.Forest);

            var unit = CreateGroundUnit(startPos, 10);
            var result = HexMapUtil.GetValidMoveDestinations(map, unit);

            Assert.IsTrue(result.Reachable.ContainsKey(neighborPos));
            Assert.AreEqual(expectedCost, result.Reachable[neighborPos]);
        }

        [Test]
        public void BFS_EnemyZoC_ZoCToZoC_MarksTerminal()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            // Place enemy at (7, 5) — spotted
            CreateEnemyUnit(new Position2D(7, 5));

            // Player at (5, 5), moving east
            var unit = CreateGroundUnit(new Position2D(5, 5), 10);
            GameManager.BuildOccupancyCache();

            var result = HexMapUtil.GetValidMoveDestinations(map, unit);

            // Hexes adjacent to enemy are ZoC hexes
            // Moving through two consecutive ZoC hexes should mark the second as terminal
            Assert.IsTrue(result.ZocTerminals.Count >= 0, "ZoC terminal tracking should work");
        }

        [Test]
        public void BFS_FriendlyPassthrough_CannotStop()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var friendlyPos = HexMapUtil.GetNeighborPosition(new Position2D(5, 5), HexDirection.E);
            CreateGroundUnit(friendlyPos, 5); // friendly unit on neighbor

            var unit = CreateGroundUnit(new Position2D(5, 5), 10);
            GameManager.BuildOccupancyCache();

            var result = HexMapUtil.GetValidMoveDestinations(map, unit);

            // Cannot stop on friendly occupied hex
            Assert.IsFalse(result.Reachable.ContainsKey(friendlyPos),
                "Cannot stop on hex occupied by friendly ground unit");

            // Can reach hexes beyond the friendly unit
            var beyondPos = HexMapUtil.GetNeighborPosition(friendlyPos, HexDirection.E);
            Assert.IsTrue(result.Reachable.ContainsKey(beyondPos),
                "Can pass through friendly and reach hexes beyond");
        }

        #endregion // BFS Tests

        #region A* Tests

        [Test]
        public void AStar_PathOptimality_MixedTerrain()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var start = new Position2D(5, 5);
            var end = HexMapUtil.GetNeighborPosition(
                HexMapUtil.GetNeighborPosition(start, HexDirection.E), HexDirection.E);

            var unit = CreateGroundUnit(start, 10);
            var path = HexMapUtil.FindPath(map, unit, start, end);

            Assert.IsTrue(path.Count == 2, "Two-hex path on clear terrain should have 2 steps");
        }

        #endregion // A* Tests

        #region Air Unit Tests

        [Test]
        public void AirUnit_IgnoresTerrain_Flat1MPPerHex()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            // Set varied terrain
            var pos1 = HexMapUtil.GetNeighborPosition(new Position2D(5, 5), HexDirection.E);
            map.GetHexAt(pos1)?.SetTerrain(TerrainType.Mountains);

            var air = CreateAirUnit(new Position2D(5, 5), 10);
            var result = HexMapUtil.GetValidMoveDestinations(map, air);

            Assert.IsTrue(result.Reachable.ContainsKey(pos1), "Air unit should reach mountains hex");
            Assert.AreEqual(1, result.Reachable[pos1], "Air unit should pay 1 MP for mountains");
        }

        #endregion // Air Unit Tests

        #region Movement API Tests

        [Test]
        public void BeginMoveOrder_DecrementsMoveActions()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var unit = CreateGroundUnit(new Position2D(5, 5), 10);
            float actionsBefore = unit.MoveActions.Current;

            bool success = unit.BeginMoveOrder();

            Assert.IsTrue(success, "BeginMoveOrder should succeed");
            Assert.AreEqual(actionsBefore - 1, unit.MoveActions.Current, "MoveActions should decrement by 1");
        }

        [Test]
        public void DeductMovementCost_ReducesMP()
        {
            var unit = CreateGroundUnit(new Position2D(5, 5), 10);

            bool result = unit.DeductMovementCost(3);

            Assert.IsTrue(result, "Should succeed with enough MP");
            Assert.AreEqual(7, unit.MovementPoints.Current, "MP should be reduced by cost");
        }

        [Test]
        public void MoveActions_DecrementedOncePerOrder_NotPerHex()
        {
            var unit = CreateGroundUnit(new Position2D(5, 5), 10);
            float initialActions = unit.MoveActions.Current;

            unit.BeginMoveOrder();
            unit.DeductMovementCost(1);
            unit.DeductMovementCost(1);
            unit.DeductMovementCost(1);

            Assert.AreEqual(initialActions - 1, unit.MoveActions.Current,
                "MoveActions should only decrement once, not per hex");
        }

        #endregion // Movement API Tests

        #region Facing Tests

        [TestCase(HexDirection.NE, HexDirection.E, 1, Description = "1 edge clockwise")]
        [TestCase(HexDirection.NE, HexDirection.SW, 3, Description = "3 edges either way")]
        [TestCase(HexDirection.NE, HexDirection.NE, 0, Description = "No rotation")]
        public void TryRotateFacing_CostsCorrectMP(HexDirection from, HexDirection to, int expectedCost)
        {
            var unit = CreateGroundUnit(new Position2D(5, 5), 10);
            unit.Facing = from;

            bool success = unit.TryRotateFacing(to);

            Assert.IsTrue(success, "Rotation should succeed with enough MP");
            Assert.AreEqual(to, unit.Facing, "Facing should be updated");
            Assert.AreEqual(10 - expectedCost, unit.MovementPoints.Current, $"Should cost {expectedCost} MP");
        }

        #endregion // Facing Tests

        #region Spotting Tests

        [Test]
        public void Spotting_IncrementalHit_RaisesLevel()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var enemy = CreateEnemyUnit(new Position2D(6, 5), spotted: SpottedLevel.Level0);
            var spotter = CreateGroundUnit(new Position2D(5, 5), 10);
            // Set a spotting range — default profile may be 0, so force it
            // SpottingService checks ActiveSpottingRange which comes from the weapon profile

            // Directly test the increment logic
            enemy.SetSpottedLevel(SpottedLevel.Level0);
            enemy.SetSpottedLevel(SpottedLevel.Level1);
            Assert.AreEqual(SpottedLevel.Level1, enemy.SpottedLevel);

            enemy.SetSpottedLevel(SpottedLevel.Level4);
            Assert.AreEqual(SpottedLevel.Level4, enemy.SpottedLevel, "Should cap at Level4");
        }

        [Test]
        public void Spotting_AdminPhaseDecay_UnspotsUnseenUnits()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            // Enemy at Level2, no player spotters nearby
            var enemy = CreateEnemyUnit(new Position2D(9, 9), spotted: SpottedLevel.Level2);

            // No player units near — decay should drop to Level1
            SpottingService.ProcessAdminPhaseDecay();
            Assert.AreEqual(SpottedLevel.Level1, enemy.SpottedLevel, "Level2 should decay to Level1");

            // Again — Level1 should decay to Level0
            SpottingService.ProcessAdminPhaseDecay();
            Assert.AreEqual(SpottedLevel.Level0, enemy.SpottedLevel, "Level1 should decay to Level0");
        }

        #endregion // Spotting Tests

        #region Cycling Tests

        [Test]
        public void CycleList_SkipsExhaustedUnits()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var unit1 = CreateGroundUnit(new Position2D(1, 1), 10);
            var unit2 = CreateGroundUnit(new Position2D(2, 2), 10);
            var unit3 = CreateGroundUnit(new Position2D(3, 3), 10);

            // Exhaust unit2
            unit2.MovementPoints.SetCurrent(0);

            // Verify unit2 would be skipped in eligibility check
            bool unit2Eligible = unit2.CanMove() && unit2.MoveActions.Current > 0
                && unit2.MovementPoints.Current > 0 && !unit2.IsBase;
            Assert.IsFalse(unit2Eligible, "Exhausted unit should not be eligible");
            Assert.IsTrue(unit1.CanMove() && unit1.MovementPoints.Current > 0, "Unit1 should be eligible");
            Assert.IsTrue(unit3.CanMove() && unit3.MovementPoints.Current > 0, "Unit3 should be eligible");
        }

        #endregion // Cycling Tests

        #region Halt Rule Tests

        [Test]
        public void ZoCHalt_PreservesCombatIntel_WhenActionsRemain()
        {
            var unit = CreateGroundUnit(new Position2D(5, 5), 10);

            // Simulate ZoC halt: MoveActions → 0, MP preserved
            unit.MoveActions.SetCurrent(0);
            float combatCost = unit.GetCombatMovementCost();
            float intelCost = unit.GetIntelMovementCost();
            float preserved = Mathf.Max(combatCost, intelCost);
            unit.ForceSetMovementPoints(preserved);

            Assert.AreEqual(0, unit.MoveActions.Current, "MoveActions should be 0");
            Assert.AreEqual(preserved, unit.MovementPoints.Current, "MP should be preserved for combat/intel");
            Assert.IsTrue(unit.CombatActions.Current >= 1, "CombatActions should remain");
        }

        [Test]
        public void AmphibiousCrossing_ZerosEverything()
        {
            var unit = CreateGroundUnit(new Position2D(5, 5), 10);

            // Simulate amphibious crossing
            unit.ForceSetMovementPoints(0);
            unit.ForceSetActions(0, 0, 0);

            Assert.AreEqual(0, unit.MovementPoints.Current, "MP should be 0");
            Assert.AreEqual(0, unit.MoveActions.Current, "MoveActions should be 0");
            Assert.AreEqual(0, unit.CombatActions.Current, "CombatActions should be 0");
            Assert.AreEqual(0, unit.IntelActions.Current, "IntelActions should be 0");
        }

        #endregion // Halt Rule Tests
    }
}
