using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Editor tests for movement-driven tile control (§6.13) and objective capture (§17.5):
    /// transit flip, objective exemption, end-of-move ZoC sweep, capture reporting, HCL reset.
    /// </summary>
    [TestFixture]
    public class TerritoryServiceTests : BaseTestFixture
    {
        #region Helpers

        private HexMap CreateClearMap(int width = 12, int height = 12)
        {
            var map = new HexMap("TerritoryTestMap", MapConfig.Small);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var hex = new HexTile(new Vector2Int(x, y));
                    hex.SetTerrain(TerrainType.Clear);
                    hex.TileControl = TileControl.Grey;   // neutral baseline so flips are observable
                    map.SetHexAt(hex);
                }
            map.BuildNeighborRelationships();
            return map;
        }

        private static CombatUnit MakeUnit(Side side) =>
            new CombatUnit("TerrUnit", UnitClassification.INF, UnitRole.GroundCombat, side, Nationality.USSR);

        private static HexTile At(HexMap map, Position2D p) => map.GetHexAt(p);

        #endregion // Helpers

        #region Transit + occupation (§6.13.2)

        [Test]
        public void Transit_FlipsNonObjectivePathHexes_ObjectiveExempt_DestinationFlips()
        {
            var map = CreateClearMap();
            var player = MakeUnit(Side.Player);

            var a = new Position2D(1, 0);   // intermediate, non-objective
            var obj = new Position2D(2, 0); // intermediate, OBJECTIVE
            var b = new Position2D(3, 0);   // destination, non-objective

            At(map, a).TileControl = TileControl.Blue;
            At(map, obj).IsObjective = true;
            At(map, obj).TileControl = TileControl.Blue;
            At(map, obj).VictoryValue = 30;
            At(map, b).TileControl = TileControl.Blue;

            var result = TerritoryService.ApplyMoveControl(map, player, new List<Position2D> { a, obj, b });

            Assert.AreEqual(TileControl.Red, At(map, a).TileControl, "Intermediate non-objective hex should flip via transit.");
            Assert.AreEqual(1.0f, At(map, a).HexControlLevel, "Flipped hex HCL should reset to 1.0 (§6.13.10).");
            Assert.AreEqual(TileControl.Blue, At(map, obj).TileControl, "Objective passed through must be EXEMPT from transit (§6.13.8).");
            Assert.AreEqual(TileControl.Red, At(map, b).TileControl, "Destination hex should flip (occupation).");

            Assert.IsTrue(result.FlippedHexes.Contains(a), "Intermediate hex should be reported as flipped.");
            Assert.IsTrue(result.FlippedHexes.Contains(b), "Destination hex should be reported as flipped.");
            Assert.IsEmpty(result.CapturedObjectives, "Passing through an objective is not a capture.");
        }

        #endregion // Transit + occupation

        #region Objective capture (§17.5)

        [Test]
        public void Objective_EndedOn_IsCaptured_AndReported()
        {
            var map = CreateClearMap();
            var player = MakeUnit(Side.Player);

            var obj = new Position2D(4, 4);
            At(map, obj).IsObjective = true;
            At(map, obj).TileControl = TileControl.Blue;
            At(map, obj).VictoryValue = 50;

            var result = TerritoryService.ApplyMoveControl(map, player, new List<Position2D> { obj });

            Assert.AreEqual(TileControl.Red, At(map, obj).TileControl, "Objective ended on should flip to the mover (§17.5).");
            Assert.AreEqual(1, result.CapturedObjectives.Count, "The captured objective should be reported.");
            Assert.AreEqual(50f, result.CapturedObjectives[0].VictoryValue, "Reported VictoryValue drives prestige (§18.2.1).");
            Assert.AreEqual(TileControl.Blue, result.CapturedObjectives[0].PreviousControl, "Previous control should be reported for accounting.");
            Assert.IsEmpty(result.FlippedHexes, "Objective captures are reported separately from plain flips.");
        }

        #endregion // Objective capture

        #region End-of-move ZoC sweep (§6.13.3)

        [Test]
        public void ZocSweep_FlipsEnemyNeighbors_SkipsGreyAndObjectives()
        {
            var map = CreateClearMap();
            var player = MakeUnit(Side.Player);

            var dest = new Position2D(5, 4);
            var enemyN = HexMapUtil.GetNeighborPosition(dest, HexDirection.E);   // Blue → should flip
            var greyN  = HexMapUtil.GetNeighborPosition(dest, HexDirection.W);   // Grey → should NOT flip
            var objN   = HexMapUtil.GetNeighborPosition(dest, HexDirection.NE);  // Blue objective → exempt

            At(map, enemyN).TileControl = TileControl.Blue;
            At(map, greyN).TileControl = TileControl.Grey;
            At(map, objN).TileControl = TileControl.Blue;
            At(map, objN).IsObjective = true;

            var result = TerritoryService.ApplyMoveControl(map, player, new List<Position2D> { dest });

            Assert.AreEqual(TileControl.Red, At(map, enemyN).TileControl, "Enemy-owned neighbor should flip via the ZoC sweep.");
            Assert.AreEqual(TileControl.Grey, At(map, greyN).TileControl, "Grey neighbor must NOT be swept (rule is 'enemy-owned').");
            Assert.AreEqual(TileControl.Blue, At(map, objN).TileControl, "Objective neighbor must be EXEMPT from the sweep (§6.13.8).");
            Assert.IsTrue(result.FlippedHexes.Contains(enemyN), "Enemy neighbor should be reported as flipped.");
        }

        #endregion // End-of-move ZoC sweep

        #region No-op + side mapping

        [Test]
        public void AlreadyOwned_NoFlip_NoChange()
        {
            var map = CreateClearMap();
            var player = MakeUnit(Side.Player);

            var x = new Position2D(6, 6);
            At(map, x).TileControl = TileControl.Red;   // already player's

            var result = TerritoryService.ApplyMoveControl(map, player, new List<Position2D> { x });

            Assert.IsFalse(result.AnyChange, "Moving onto an already-owned hex should produce no territory change.");
        }

        [Test]
        public void AiMover_FlipsToBlue()
        {
            var map = CreateClearMap();
            var ai = MakeUnit(Side.AI);

            var y = new Position2D(7, 7);
            At(map, y).TileControl = TileControl.Red;

            TerritoryService.ApplyMoveControl(map, ai, new List<Position2D> { y });

            Assert.AreEqual(TileControl.Blue, At(map, y).TileControl, "An AI mover flips terrain to Blue (§4.7.1).");
        }

        #endregion // No-op + side mapping
    }
}
