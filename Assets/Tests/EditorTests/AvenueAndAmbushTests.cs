using System.Linq;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.AI;
using HammerAndSickle.Models.Map;
using NUnit.Framework;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// AI1b — avenues of approach + ambush-site catalog (AI-Design-Supplement Parts 4.4/4.6) on
    /// synthetic maps (12×12 canvas per the HexMap 10×10 minimum; only tiles under test populated).
    /// </summary>
    [TestFixture]
    public class AvenueAndAmbushTests
    {
        #region Fixtures

        private const int CANVAS = 12;

        private static Position2D P(int x, int y) => new Position2D(x, y);

        private static HexTile Tile(int x, int y, TerrainType terrain)
        {
            var tile = new HexTile(new Vector2Int(x, y));
            tile.SetTerrain(terrain);
            tile.MovementCost = terrain switch
            {
                TerrainType.Clear => 1,
                TerrainType.Forest => 2,
                TerrainType.Rough => 3,
                TerrainType.Marsh => 4,
                TerrainType.Mountains => 5,
                TerrainType.MinorCity => 1,
                TerrainType.MajorCity => 1,
                _ => 0,
            };
            return tile;
        }

        /// <summary>Clear strip x = 0..length−1 at y = 0 on an otherwise empty canvas.</summary>
        private static HexMap ClearStrip(int length)
        {
            var map = new HexMap("test-avenue", CANVAS, CANVAS);
            for (int x = 0; x < length; x++)
                map.SetHexAt(Tile(x, 0, TerrainType.Clear));
            return map;
        }

        /// <summary>
        /// Two parallel Clear corridors (y = 0 and y = 2, x = 0..9) joined only at the ends by
        /// (0,1) and (9,1) — a classic fork for diversity testing (rows 0 and 2 are never adjacent).
        /// </summary>
        private static HexMap Fork()
        {
            var map = new HexMap("test-fork", CANVAS, CANVAS);
            for (int x = 0; x <= 9; x++)
            {
                map.SetHexAt(Tile(x, 0, TerrainType.Clear));
                map.SetHexAt(Tile(x, 2, TerrainType.Clear));
            }
            map.SetHexAt(Tile(0, 1, TerrainType.Clear));
            map.SetHexAt(Tile(9, 1, TerrainType.Clear));
            return map;
        }

        #endregion // Fixtures

        #region Avenues

        [Test]
        public void FindAvenues_SingleCorridor_OnePathDespiteHigherK()
        {
            HexMap map = ClearStrip(8);

            var avenues = AvenueAnalysis.FindAvenues(map, new[] { P(0, 0) }, new[] { P(7, 0) }, k: 2);

            Assert.AreEqual(1, avenues.Count, "the only corridor dedupes under penalties");
            Assert.AreEqual(8, avenues[0].Path.Count);
            Assert.AreEqual(7, avenues[0].PathCost);
            Assert.AreEqual(P(0, 0), avenues[0].Source);
            Assert.AreEqual(P(7, 0), avenues[0].Target);
            Assert.AreEqual(0.0, avenues[0].CoverFraction, 1e-9);
        }

        [Test]
        public void FindAvenues_Fork_SecondAvenueTakesTheOtherCorridor()
        {
            HexMap map = Fork();

            var avenues = AvenueAnalysis.FindAvenues(map, new[] { P(0, 1) }, new[] { P(9, 1) }, k: 2);

            Assert.AreEqual(2, avenues.Count);
            bool firstUsesTop = avenues[0].Path.Contains(P(5, 0));
            bool secondUsesTop = avenues[1].Path.Contains(P(5, 0));
            Assert.AreNotEqual(firstUsesTop, secondUsesTop, "avenues diverge onto different corridors");
            Assert.IsTrue(avenues[0].Path.Contains(P(5, 0)) || avenues[0].Path.Contains(P(5, 2)));
            Assert.IsTrue(avenues[1].Path.Contains(P(5, 0)) || avenues[1].Path.Contains(P(5, 2)));
        }

        [Test]
        public void FindAvenues_CoverFraction_CountsBlockingTerrain()
        {
            HexMap map = ClearStrip(8);
            map.GetHexAt(P(2, 0)).SetTerrain(TerrainType.Forest);
            map.GetHexAt(P(2, 0)).MovementCost = 2;
            map.GetHexAt(P(5, 0)).SetTerrain(TerrainType.Forest);
            map.GetHexAt(P(5, 0)).MovementCost = 2;

            var avenues = AvenueAnalysis.FindAvenues(map, new[] { P(0, 0) }, new[] { P(7, 0) }, k: 1);

            Assert.AreEqual(1, avenues.Count);
            Assert.AreEqual(0.25, avenues[0].CoverFraction, 1e-9, "2 covered hexes of 8");
        }

        #endregion // Avenues

        #region Ambush catalog

        [Test]
        public void AmbushCatalog_CoveredFlankHex_BecomesSiteWithTriggerAndDisplace()
        {
            HexMap map = ClearStrip(8);
            map.SetHexAt(Tile(3, 1, TerrainType.Forest)); // covered flank: adjacent to path hexes (3,0) & (4,0)
            map.SetHexAt(Tile(3, 2, TerrainType.Clear));  // displace exit, clear of the avenue's adjacency halo

            var avenues = AvenueAnalysis.FindAvenues(map, new[] { P(0, 0) }, new[] { P(7, 0) }, k: 1);
            var sites = AmbushSiteCatalog.Build(map, avenues);

            Assert.AreEqual(1, sites.Count);
            AmbushSite site = sites[0];
            Assert.AreEqual(P(3, 1), site.Hex);
            Assert.AreEqual(P(3, 0), site.TriggerHex, "earliest path hex the mover reaches");
            Assert.AreEqual(2, site.PathAdjacency, "flanks two path hexes");
            Assert.AreEqual(1, site.CoverTier, "Forest = Light");
            Assert.IsTrue(site.HasDisplaceRoute, "(3,2) is a valid shoot-and-scoot exit");
        }

        [Test]
        public void AmbushCatalog_NoCoverAnywhere_YieldsNoSites()
        {
            HexMap map = ClearStrip(8);
            map.SetHexAt(Tile(3, 1, TerrainType.Clear)); // flanking hex exists but offers no cover

            var avenues = AvenueAnalysis.FindAvenues(map, new[] { P(0, 0) }, new[] { P(7, 0) }, k: 1);
            var sites = AmbushSiteCatalog.Build(map, avenues);

            Assert.AreEqual(0, sites.Count);
        }

        [Test]
        public void AmbushCatalog_NoDisplaceExit_FlagsFalse()
        {
            HexMap map = ClearStrip(8);
            map.SetHexAt(Tile(3, 1, TerrainType.Forest)); // covered, but boxed in — no exit off the halo

            var avenues = AvenueAnalysis.FindAvenues(map, new[] { P(0, 0) }, new[] { P(7, 0) }, k: 1);
            var sites = AmbushSiteCatalog.Build(map, avenues);

            Assert.AreEqual(1, sites.Count);
            Assert.IsFalse(sites[0].HasDisplaceRoute);
        }

        #endregion // Ambush catalog
    }
}
