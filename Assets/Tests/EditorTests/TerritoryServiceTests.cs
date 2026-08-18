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
    /// Editor tests for movement-driven tile control (§6.13) and stronghold capture (§17.5):
    /// transit flip, stronghold exemption, end-of-move ZoC sweep, capture reporting, HCL reset,
    /// and the IsStronghold derivation itself (prestige pass V1/V3, 2026-08-17 — stickiness derives
    /// from terrain/infrastructure; the authored isObjective flag is gameplay-dead).
    /// </summary>
    [TestFixture]
    public class TerritoryServiceTests : BaseTestFixture
    {
        #region Helpers

        // Clear-under-Grey via the shared fixture (V16.2) — the neutral baseline so flips are observable.
        private static HexMap CreateClearMap() => MapFixtures.UniformMap(12, 12, name: "TerritoryTestMap");

        private static CombatUnit MakeUnit(Side side) =>
            new CombatUnit("TerrUnit", UnitClassification.INF, UnitRole.GroundCombat, side, Nationality.USSR);

        private static HexTile At(HexMap map, Position2D p) => map.GetHexAt(p);

        #endregion // Helpers

        #region IsStronghold derivation (V1)

        [Test]
        public void IsStronghold_TruthTable()
        {
            var map = CreateClearMap();

            // Cities by terrain.
            At(map, new Position2D(1, 1)).SetTerrain(TerrainType.MajorCity);
            At(map, new Position2D(2, 1)).SetTerrain(TerrainType.MinorCity);
            Assert.IsTrue(At(map, new Position2D(1, 1)).IsStronghold, "MajorCity is a stronghold.");
            Assert.IsTrue(At(map, new Position2D(2, 1)).IsStronghold, "MinorCity is a stronghold.");

            // Installations by infrastructure, on open ground — the Hamburg case (airbases on Clear).
            At(map, new Position2D(3, 1)).IsFort = true;
            At(map, new Position2D(4, 1)).IsAirbase = true;
            At(map, new Position2D(5, 1)).IsPort = true;
            Assert.IsTrue(At(map, new Position2D(3, 1)).IsStronghold, "A fort on open ground is a stronghold.");
            Assert.IsTrue(At(map, new Position2D(4, 1)).IsStronghold, "An airbase on open ground is a stronghold.");
            Assert.IsTrue(At(map, new Position2D(5, 1)).IsStronghold, "A port on open ground is a stronghold.");

            // Plain terrain is not.
            Assert.IsFalse(At(map, new Position2D(6, 1)).IsStronghold, "Plain Clear is not a stronghold.");
            At(map, new Position2D(7, 1)).SetTerrain(TerrainType.Mountains);
            Assert.IsFalse(At(map, new Position2D(7, 1)).IsStronghold, "Terrain that is merely hard is not sticky.");
        }

        [Test]
        public void IsStronghold_IgnoresTheDeadFlagAndValue()
        {
            // The decoupling that IS the V1/V2 change: neither the authored isObjective flag nor a
            // victory value makes a hex sticky — only terrain/infrastructure does.
            var map = CreateClearMap();
            var hex = At(map, new Position2D(4, 4));
            hex.IsObjective = true;         // gameplay-dead UI marker
            hex.VictoryValue = 100f;        // economic weight, not a badge

            Assert.IsFalse(hex.IsStronghold,
                "An authored objective flag on plain terrain must NOT create a stronghold — the flag is gameplay-dead (V2).");
        }

        #endregion // IsStronghold derivation

        #region Transit + occupation (§6.13.2)

        [Test]
        public void Transit_FlipsNonStrongholdPathHexes_StrongholdExempt_DestinationFlips()
        {
            var map = CreateClearMap();
            var player = MakeUnit(Side.Player);

            var a = new Position2D(1, 0);   // intermediate, non-stronghold
            var sh = new Position2D(2, 0);  // intermediate, STRONGHOLD (minor city)
            var b = new Position2D(3, 0);   // destination, non-stronghold

            At(map, a).TileControl = TileControl.Blue;
            At(map, sh).SetTerrain(TerrainType.MinorCity);   // same movement cost as Clear — fixture stays behaviourally clean
            At(map, sh).TileControl = TileControl.Blue;
            At(map, sh).VictoryValue = 30;
            At(map, b).TileControl = TileControl.Blue;

            var result = TerritoryService.ApplyMoveControl(map, player, new List<Position2D> { a, sh, b });

            Assert.AreEqual(TileControl.Red, At(map, a).TileControl, "Intermediate non-stronghold hex should flip via transit.");
            Assert.AreEqual(1.0f, At(map, a).HexControlLevel, "Flipped hex HCL should reset to 1.0 (§6.13.10).");
            Assert.AreEqual(TileControl.Blue, At(map, sh).TileControl, "Stronghold passed through must be EXEMPT from transit (§6.13.8).");
            Assert.AreEqual(TileControl.Red, At(map, b).TileControl, "Destination hex should flip (occupation).");

            Assert.IsTrue(result.FlippedHexes.Contains(a), "Intermediate hex should be reported as flipped.");
            Assert.IsTrue(result.FlippedHexes.Contains(b), "Destination hex should be reported as flipped.");
            Assert.IsEmpty(result.CapturedStrongholds, "Passing through a stronghold is not a capture.");
        }

        [Test]
        public void Transit_ValuedNonStronghold_StillFlips()
        {
            // Value is an economic weight, not a protection — only the stronghold derivation exempts.
            var map = CreateClearMap();
            var player = MakeUnit(Side.Player);

            var valued = new Position2D(1, 0);
            At(map, valued).TileControl = TileControl.Blue;
            At(map, valued).VictoryValue = 200f;
            var dest = new Position2D(2, 0);
            At(map, dest).TileControl = TileControl.Blue;

            TerritoryService.ApplyMoveControl(map, player, new List<Position2D> { valued, dest });

            Assert.AreEqual(TileControl.Red, At(map, valued).TileControl,
                "A valued Clear hex flips by transit like any other — value must not confer stickiness.");
        }

        #endregion // Transit + occupation

        #region Stronghold capture (§17.5)

        [Test]
        public void Stronghold_EndedOn_IsCaptured_AndReported()
        {
            var map = CreateClearMap();
            var player = MakeUnit(Side.Player);

            var sh = new Position2D(4, 4);
            At(map, sh).SetTerrain(TerrainType.MinorCity);
            At(map, sh).TileControl = TileControl.Blue;
            At(map, sh).VictoryValue = 50;

            var result = TerritoryService.ApplyMoveControl(map, player, new List<Position2D> { sh });

            Assert.AreEqual(TileControl.Red, At(map, sh).TileControl, "Stronghold ended on should flip to the mover (§17.5).");
            Assert.AreEqual(1, result.CapturedStrongholds.Count, "The captured stronghold should be reported.");
            Assert.AreEqual(50f, result.CapturedStrongholds[0].VictoryValue, "Reported VictoryValue feeds capture accounting (§17.5.3).");
            Assert.AreEqual(TileControl.Blue, result.CapturedStrongholds[0].PreviousControl, "Previous control should be reported for accounting.");
            Assert.IsEmpty(result.FlippedHexes, "Stronghold captures are reported separately from plain flips.");
        }

        #endregion // Stronghold capture

        #region End-of-move ZoC sweep (§6.13.3)

        [Test]
        public void ZocSweep_FlipsEnemyNeighbors_SkipsGreyAndStrongholds()
        {
            var map = CreateClearMap();
            var player = MakeUnit(Side.Player);

            var dest = new Position2D(5, 4);
            var enemyN = HexMapUtil.GetNeighborPosition(dest, HexDirection.E);   // Blue → should flip
            var greyN  = HexMapUtil.GetNeighborPosition(dest, HexDirection.W);   // Grey → should NOT flip
            var shN    = HexMapUtil.GetNeighborPosition(dest, HexDirection.NE);  // Blue stronghold → exempt

            At(map, enemyN).TileControl = TileControl.Blue;
            At(map, greyN).TileControl = TileControl.Grey;
            At(map, shN).TileControl = TileControl.Blue;
            At(map, shN).SetTerrain(TerrainType.MinorCity);

            var result = TerritoryService.ApplyMoveControl(map, player, new List<Position2D> { dest });

            Assert.AreEqual(TileControl.Red, At(map, enemyN).TileControl, "Enemy-owned neighbor should flip via the ZoC sweep.");
            Assert.AreEqual(TileControl.Grey, At(map, greyN).TileControl, "Grey neighbor must NOT be swept (rule is 'enemy-owned').");
            Assert.AreEqual(TileControl.Blue, At(map, shN).TileControl, "Stronghold neighbor must be EXEMPT from the sweep (§6.13.8).");
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
