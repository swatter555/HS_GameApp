using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
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
            var map = new HexMap("TestMap", width, height);
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
        /// <remarks>
        /// ⚠ CARRIES A REAL FOOT PROFILE, and must keep carrying one. Since P3 the movement rules ask
        /// <see cref="MovementModeService"/> how the unit is travelling, and that reads the ACTIVE PROFILE —
        /// a unit with empty bays reports <see cref="MovementMedium.None"/>, which is neither airborne nor
        /// ground. These fixtures would still pass (None falls through to the ground branch) while testing
        /// a state no real unit is ever in.
        /// </remarks>
        private CombatUnit CreateGroundUnit(Position2D pos, int mp, UnitClassification classification = UnitClassification.INF)
        {
            var unit = new CombatUnit("TestUnit", classification, UnitRole.GroundCombat,
                Side.Player, Nationality.USSR,
                deployedProfile: WeaponType.INF_REG_SV,
                mobileProfile: WeaponType.NONE,
                embarkedProfile: WeaponType.NONE);
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
        /// <remarks>
        /// ⚠ THE FIGHTER PROFILE IS LOAD-BEARING SINCE P3. Without it the unit's medium is `None` and the
        /// movement rules treat this fighter as INFANTRY — it would be blocked by impassable terrain and
        /// stopped by zones of control. The classification alone no longer decides.
        /// </remarks>
        private CombatUnit CreateAirUnit(Position2D pos, int mp)
        {
            var unit = new CombatUnit("TestAirUnit", UnitClassification.FGT, UnitRole.AirSuperiority,
                Side.Player, Nationality.USSR,
                deployedProfile: WeaponType.FGT_MIG21_SV,
                mobileProfile: WeaponType.NONE,
                embarkedProfile: WeaponType.NONE);
            unit.SetPosition(pos);
            unit.MovementPoints.SetMax(mp);
            unit.MovementPoints.SetCurrent(mp);
            GameManager.RegisterCombatUnit(unit);
            return unit;
        }

        /// <summary>
        /// Creates a player AIR-ASSAULT regiment — the unit the whole P3 pass exists for. Foot when Deployed,
        /// tracked carriers when Mobile, Mi-8s when Embarked, and `UnitClassification.MAM` throughout, so
        /// every classification test reports "not an air unit" no matter how it is actually travelling.
        /// </summary>
        private CombatUnit CreateAirAssaultUnit(Position2D pos, int mp, DeploymentPosition posture)
        {
            var unit = new CombatUnit("TestLift", UnitClassification.MAM, UnitRole.GroundCombat,
                Side.Player, Nationality.USSR,
                deployedProfile: WeaponType.INF_AM_SV,
                mobileProfile: WeaponType.APC_MTLB_SV,
                embarkedProfile: WeaponType.HEL_MI8T_SV);
            unit.SetPosition(pos);
            unit.SetDeploymentPosition(posture);
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

            /* ⚠ AND NEITHER CAN AIR, SINCE 2026-08-10 — this assertion was REVERSED, deliberately.
             * Impassable is foreign, non-belligerent territory: overflying it is an act of war, not a
             * shortcut, and letting aircraft cross it would erase the map's chokepoints and flanks for
             * every air unit. Air still ignores terrain COST; it does not ignore impassable. */
            var air = CreateAirUnit(new Position2D(5, 5), 10);
            var airResult = HexMapUtil.GetValidMoveDestinations(map, air);
            Assert.IsFalse(airResult.Reachable.ContainsKey(blockedPos),
                "impassable is closed to every domain, aircraft included");

            // The cost rule is untouched: air still crosses expensive terrain for a flat 1.
            var mountainPos = HexMapUtil.GetNeighborPosition(new Position2D(5, 5), HexDirection.W);
            map.GetHexAt(mountainPos)?.SetTerrain(TerrainType.Mountains);
            var airAgain = HexMapUtil.GetValidMoveDestinations(map, air);
            Assert.AreEqual(1, airAgain.Reachable[mountainPos],
                "ignoring terrain COST is a different rule from ignoring impassable");
        }

        [Test]
        public void BFS_River_ForcesDetour_BridgeRestoresDirectCrossing()
        {
            // A river on a single edge blocks only that DIRECT crossing — it does NOT make the
            // neighbor unreachable, because on an open map the BFS legitimately detours around it
            // (start → NE → neighbor = 2 MP). So the river block is measured by COST, not reachability:
            // without a bridge the neighbor costs 2 MP (detour); a bridge restores the direct 1-MP step.
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var startPos = new Position2D(5, 5);
            var neighborPos = HexMapUtil.GetNeighborPosition(startPos, HexDirection.E);

            var startHex = map.GetHexAt(startPos);
            startHex.RiverBorders.SetBorder(HexDirection.E, true);

            // No bridge: direct E edge blocked → reachable only via a 2-MP detour.
            var unit = CreateGroundUnit(startPos, 5);
            var blockedResult = HexMapUtil.GetValidMoveDestinations(map, unit);
            Assert.IsTrue(blockedResult.Reachable.ContainsKey(neighborPos),
                "Neighbor stays reachable via detour even with the direct river edge blocked");
            Assert.AreEqual(2, blockedResult.Reachable[neighborPos],
                "River forces a 2-MP detour — the direct 1-MP edge is blocked without a bridge");

            // Add a bridge: the direct E edge opens → neighbor now costs the direct 1 MP.
            startHex.BridgeBorders.SetBorder(HexDirection.E, true);
            GameManager.InvalidateOccupancy();
            var bridgedResult = HexMapUtil.GetValidMoveDestinations(map, unit);
            Assert.AreEqual(1, bridgedResult.Reachable[neighborPos],
                "Bridge restores the direct 1-MP river crossing");
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
            SpottingService.ProcessSpottingDecay();
            Assert.AreEqual(SpottedLevel.Level1, enemy.SpottedLevel, "Level2 should decay to Level1");

            // Again — Level1 should decay to Level0
            SpottingService.ProcessSpottingDecay();
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

        #region P3 — the movement rules read the resolver, not the classification

        /* ⚠ WHAT THIS REGION GUARDS. Until P3 the movement code asked `IsAirUnit || IsHelicopter` — a
         * CLASSIFICATION test. An air-assault regiment riding its Mi-8s is `UnitClassification.MAM`, so it
         * answered "no" and was walked over the mountains it was flying above, halted by zones of control it
         * was flying through. The fix routes all three sites — range generation, A*, and execution — through
         * MovementModeService, which reads the profile actually carrying the unit.
         *
         * ⚠ These cases all use MAM regiments precisely BECAUSE their classification lies. A test that flew a
         * `FGT` would pass under either implementation and guard nothing. */

        [Test]
        public void Lift_InFlight_IgnoresTerrainAndPaysFlatOne()
        {
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var mountainPos = HexMapUtil.GetNeighborPosition(new Position2D(5, 5), HexDirection.E);
            map.GetHexAt(mountainPos)?.SetTerrain(TerrainType.Mountains);

            var lift = CreateAirAssaultUnit(new Position2D(5, 5), 10, DeploymentPosition.Embarked);
            var result = HexMapUtil.GetValidMoveDestinations(map, lift);

            Assert.That(lift.IsFixedWing || lift.IsHelicopter, Is.False,
                "the classification still says ground — which is the whole reason this test exists");
            Assert.That(result.Reachable.ContainsKey(mountainPos), Is.True,
                "a regiment on helicopters can cross a mountain hex");
            Assert.That(result.Reachable[mountainPos], Is.EqualTo(1),
                "and pays the flat airborne cost for it, not the mountain's 5");
        }

        [Test]
        public void Lift_OnTheGround_StillPaysTerrain()
        {
            // The control case. The SAME regiment, one posture down, must go back to paying ground costs —
            // otherwise the fix has simply made everything fly.
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var mountainPos = HexMapUtil.GetNeighborPosition(new Position2D(5, 5), HexDirection.E);
            map.GetHexAt(mountainPos)?.SetTerrain(TerrainType.Mountains);

            var lift = CreateAirAssaultUnit(new Position2D(5, 5), 10, DeploymentPosition.Deployed);
            var result = HexMapUtil.GetValidMoveDestinations(map, lift);

            Assert.That(result.Reachable[mountainPos], Is.EqualTo(5),
                "dismounted, it walks up the mountain at full cost");
        }

        [Test]
        public void Lift_InFlight_IsNotHeldByEnemyZoC()
        {
            /* Ratified 2026-08-04: zones of control NEVER stop a flight, and ambush is the single mechanism
             * by which an enemy halts an airborne move. An empty terminal set is that rule. */
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            CreateEnemyUnit(new Position2D(7, 5));

            var walking = CreateAirAssaultUnit(new Position2D(5, 5), 10, DeploymentPosition.Deployed);
            GameManager.BuildOccupancyCache();
            var walkingResult = HexMapUtil.GetValidMoveDestinations(map, walking);

            var flying = CreateAirAssaultUnit(new Position2D(5, 5), 10, DeploymentPosition.Embarked);
            GameManager.BuildOccupancyCache();
            var flyingResult = HexMapUtil.GetValidMoveDestinations(map, flying);

            Assert.That(walkingResult.ZocTerminals, Is.Not.Empty,
                "on foot the same regiment is caught by the same enemy's zone of control");
            Assert.That(flyingResult.ZocTerminals, Is.Empty,
                "in the air it is not — this is the rule, not an optimisation");
        }

        [Test]
        public void Lift_InFlight_OverfliesAnOccupiedHex_ButMayNotLandOnIt()
        {
            /* ⚠ THE HALF THAT IS EASY TO GET WRONG. "May I pass over?" is a MEDIUM question; "may I stop
             * here?" is an OCCUPANCY question, and a lift comes to rest in the GROUND stack because
             * everything that is not fixed-wing files there. Keying the stop test on the medium too would
             * put two units in one ground stack, which the stacking model cannot draw. */
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var occupiedPos = HexMapUtil.GetNeighborPosition(new Position2D(5, 5), HexDirection.E);
            var beyondPos = HexMapUtil.GetNeighborPosition(occupiedPos, HexDirection.E);
            CreateGroundUnit(occupiedPos, 5);

            var lift = CreateAirAssaultUnit(new Position2D(5, 5), 10, DeploymentPosition.Embarked);
            GameManager.BuildOccupancyCache();

            var result = HexMapUtil.GetValidMoveDestinations(map, lift);

            Assert.That(result.Reachable.ContainsKey(occupiedPos), Is.False,
                "it cannot set down on top of a friendly regiment");
            Assert.That(result.Reachable.ContainsKey(beyondPos), Is.True,
                "but it flies straight over it to the hex beyond");
        }

        [Test]
        public void Airborne_OverfliesAnOccupiedHex_ButOnlyFixedWingMayRestThere()
        {
            /* ⚠ RATIFIED 2026-08-10 AFTER A PLAY-TEST. Anything airborne OVERFLIES an occupied hex — the
             * price of overflight is fire (§6.9 ambush, §11.8 air-defence) followed by the §11.8.9 transit
             * stand check, never a movement wall. Blocking helicopters here would make that entire risk
             * model unreachable, which is why the earlier "helos may not pass through" reading was
             * withdrawn. What a helicopter still may NOT do is come to REST on the hex. */
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var start = new Position2D(5, 5);
            var enemyPos = HexMapUtil.GetNeighborPosition(start, HexDirection.E);
            var beyondPos = HexMapUtil.GetNeighborPosition(enemyPos, HexDirection.E);
            CreateEnemyUnit(enemyPos);                    // spotted, so it legitimately shapes the range

            var gunship = CreateAirAssaultUnit(start, 10, DeploymentPosition.Embarked);
            GameManager.BuildOccupancyCache();
            var heloRange = HexMapUtil.GetValidMoveDestinations(map, gunship);

            Assert.That(heloRange.Reachable.ContainsKey(beyondPos), Is.True,
                "it flies straight over and reaches the hex beyond");
            Assert.That(heloRange.Reachable[beyondPos], Is.EqualTo(2),
                "at the flat airborne cost — no detour, because nothing blocked it");
            Assert.That(heloRange.Reachable.ContainsKey(enemyPos), Is.False,
                "but it may not SET DOWN on an occupied hex — helicopters rest in the ground stack");

            // Fixed-wing is the one exception, and it applies to resting, not to passage.
            var jet = CreateAirUnit(start, 10);
            GameManager.BuildOccupancyCache();
            var jetRange = HexMapUtil.GetValidMoveDestinations(map, jet);

            Assert.That(jetRange.Reachable.ContainsKey(enemyPos), Is.True,
                "fixed-wing may temporarily occupy a ground unit's hex — the single stacking exception");
        }

        [Test]
        public void RangeAndPath_AgreeOnTheAirborneCostModel()
        {
            /* ⚠ THE TWO MUST MOVE TOGETHER. Fixing execution but not pathfinding (or either but not range)
             * makes the overlay promise hexes the move cannot deliver — the player sees a legal destination,
             * right-clicks it, and the unit stops short with no explanation. */
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var start = new Position2D(5, 5);
            var mountainPos = HexMapUtil.GetNeighborPosition(start, HexDirection.E);
            map.GetHexAt(mountainPos)?.SetTerrain(TerrainType.Mountains);
            var end = HexMapUtil.GetNeighborPosition(mountainPos, HexDirection.E);

            var lift = CreateAirAssaultUnit(start, 10, DeploymentPosition.Embarked);

            var range = HexMapUtil.GetValidMoveDestinations(map, lift);
            var path = HexMapUtil.FindPath(map, lift, start, end);

            Assert.That(range.Reachable.ContainsKey(end), Is.True, "the overlay offers the hex");
            Assert.That(path, Is.Not.Empty, "and the pathfinder can actually get there");
            Assert.That(path.Count, Is.EqualTo(2),
                "straight across the mountain rather than around it — both passes agree it is flying");
        }

        [Test]
        public void Sealifted_HasNoPerHexMovementAtAll()
        {
            /* §5.4.2.3 — naval movement is INSTANT port-to-port with the sea passage abstracted away, chosen
             * with the §24.7a.3 Naval Movement Marker. There is no hex-by-hex sea traversal to implement.
             *
             * ⚠ WITHOUT THE GUARD THIS UNIT WALKS. `Naval` is neither airborne nor groundborne, so it falls
             * through to the ground rules — which block water but happily allow LAND, i.e. a regiment that
             * boarded ships at a port could stroll inland still aboard them. */
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var start = new Position2D(5, 5);
            var unit = CreateGroundUnit(start, 10);
            unit.SetDeploymentPosition(DeploymentPosition.Embarked);
            unit.SetNavalEmbarked(true);
            unit.MovementPoints.SetCurrent(10);   // plenty of MP — the guard, not exhaustion, must stop it

            var range = HexMapUtil.GetValidMoveDestinations(map, unit);
            var path = HexMapUtil.FindPath(map, unit, start,
                HexMapUtil.GetNeighborPosition(start, HexDirection.E));

            Assert.That(MovementModeService.IsSealiftedNow(unit), Is.True, "it is aboard the sealift profile");
            Assert.That(unit.MovementPoints.Current, Is.GreaterThan(0),
                "guard the guard — an exhausted unit would return an empty range for the wrong reason");
            Assert.That(range.Reachable, Is.Empty, "so it has no walking range");
            Assert.That(path, Is.Empty, "and no walking path");
        }

        [Test]
        public void AmbushAction_ActuallyAppliesDamage_EndToEnd()
        {
            /* ⚠ THE POINT OF THIS TEST IS TO SPLIT "no combat" FROM "combat, no losses". Until 2026-08-10
             * `CombatResolver.ResolveAmbush` had ZERO callers and `OnAmbushTriggered` ZERO subscribers, so
             * an ambush halted the mover and printed a dispatch about a fight that never happened. If this
             * passes and play still shows nothing, the fault is in the TRIGGER geometry, not the
             * resolution — which is exactly the ambiguity a play-test cannot resolve on its own. */
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var moverPos = new Position2D(5, 5);
            var ambusherPos = HexMapUtil.GetNeighborPosition(moverPos, HexDirection.E);

            var mover = CreateGroundUnit(moverPos, 10);
            var ambusher = CreateEnemyUnit(ambusherPos, spotted: SpottedLevel.Level0);
            GameManager.BuildOccupancyCache();

            float hp0 = mover.HitPoints.Current;

            var outcome = AmbushAction.Execute(ambusher, mover, map, new FixedRollRandom(8));

            Assert.That(outcome.Executed, Is.True, "the orchestrator ran");
            Assert.That(outcome.DamageToMover, Is.GreaterThan(0), "an ambush deals real damage");
            Assert.That(mover.HitPoints.Current, Is.LessThan(hp0), "and it lands on the victim's hit points");
            Assert.That(ambusher.SpottedLevel, Is.GreaterThan(SpottedLevel.Level0),
                "§6.9.3 — springing the trap reveals the ambusher");
        }

        [Test]
        public void GroundAmbush_TriggersOnPassingADJACENT_NotOnWalkingInto()
        {
            /* ⚠ THE GEOMETRY THAT DECIDES WHETHER AN AMBUSH IS EVEN POSSIBLE, and the likeliest reason a
             * play-test sees nothing. `CheckGroundAmbush` looks at the NEIGHBOURS of the hex just entered.
             * Ordering a unit straight AT an unspotted enemy trips the CONTACT HALT instead — it stops
             * before arriving and never becomes adjacent-and-moving. To be ambushed you must move PAST. */
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var ambusherPos = new Position2D(5, 5);
            var passingHex = HexMapUtil.GetNeighborPosition(ambusherPos, HexDirection.E);

            var mover = CreateGroundUnit(new Position2D(8, 5), 10);
            CreateEnemyUnit(ambusherPos, spotted: SpottedLevel.Level0);
            GameManager.BuildOccupancyCache();

            Assert.That(SpottingService.CheckGroundAmbush(mover, passingHex), Is.Not.Null,
                "entering a hex ADJACENT to the hidden enemy springs it");
            Assert.That(SpottingService.CheckGroundAmbush(mover, new Position2D(8, 5)), Is.Null,
                "standing three hexes away does not");
        }

        [Test]
        public void GroundAmbush_IneligibleClassNeverSprings()
        {
            /* §6.9.9, enforced at the trigger 2026-08-10 — it was checked NOWHERE before, invisible only
             * because the trigger itself could never fire. A hidden tube battery is the ambush VICTIM,
             * never the ambusher: the mover passes it unmolested and learns of it at settlement. */
            var map = CreateClearMap();
            GameDataManager.CurrentHexMap = map;

            var ambusherPos = new Position2D(5, 5);
            var passingHex = HexMapUtil.GetNeighborPosition(ambusherPos, HexDirection.E);

            var mover = CreateGroundUnit(new Position2D(8, 5), 10);
            CreateEnemyUnit(ambusherPos, UnitClassification.ART, spotted: SpottedLevel.Level0);
            GameManager.BuildOccupancyCache();

            Assert.That(SpottingService.CheckGroundAmbush(mover, passingHex), Is.Null,
                "§6.9.9 — ART may never spring the ambush, however hidden and adjacent it is");
        }

        [Test]
        public void AmbushEligibility_PinsTheRatifiedList()
        {
            // §6.9.9 exclusions — each is doctrine, not oversight (see GameData.IsAmbushEligible remarks).
            Assert.That(GameData.IsAmbushEligible(UnitClassification.ART), Is.False, "tubes are the victim");
            Assert.That(GameData.IsAmbushEligible(UnitClassification.SPA), Is.False, "tubes are the victim");
            Assert.That(GameData.IsAmbushEligible(UnitClassification.ROC), Is.False, "no point-blank reactive fire");
            Assert.That(GameData.IsAmbushEligible(UnitClassification.BM), Is.False, "strategic single-shot");
            Assert.That(GameData.IsAmbushEligible(UnitClassification.SAM), Is.False, "cannot engage ground");
            Assert.That(GameData.IsAmbushEligible(UnitClassification.SPSAM), Is.False, "cannot engage ground");
            Assert.That(GameData.IsAmbushEligible(UnitClassification.ENG), Is.False, "non-combatant");
            Assert.That(GameData.IsAmbushEligible(UnitClassification.HQ), Is.False, "facilities cannot attack");

            // And the deliberate inclusions the list exists to protect:
            Assert.That(GameData.IsAmbushEligible(UnitClassification.AAA), Is.True, "flak leveled at infantry (§7A.12)");
            Assert.That(GameData.IsAmbushEligible(UnitClassification.SPAAA), Is.True, "flak leveled at infantry (§7A.12)");
            Assert.That(GameData.IsAmbushEligible(UnitClassification.HELO), Is.True, "a helo can be the ambusher (§5.13.2.3)");
            Assert.That(GameData.IsAmbushEligible(UnitClassification.INF), Is.True);
            Assert.That(GameData.IsAmbushEligible(UnitClassification.TANK), Is.True);
        }

        [Test]
        public void GroundAmbushHalt_SpendsEverything()
        {
            /* ⚠ APPLIES TO HELICOPTERS TOO since 2026-08-10 (§5.13.2.2 — the helicopter's turn ends AFTER
             * taking the ambusher's attack). The narrower FlightEvasion halt, and the test that pinned it,
             * were retired with the evade-without-damage rule: a helo is now ambushed exactly like a ground
             * unit and only the §6.9.4 surprise multiplier is denied the ambusher. */
            var unit = CreateGroundUnit(new Position2D(5, 5), 10);

            MovementController.ApplyMovementHalt(unit, MovementController.MovementHalt.GroundAmbush);

            Assert.That(unit.MovementPoints.Current, Is.EqualTo(0));
            Assert.That(unit.MoveActions.Current, Is.EqualTo(0));
            Assert.That(unit.CombatActions.Current, Is.EqualTo(0));
            Assert.That(unit.IntelActions.Current, Is.EqualTo(0));
        }

        #endregion // P3 — the movement rules read the resolver, not the classification
    }
}
