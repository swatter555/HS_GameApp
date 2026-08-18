using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using NUnit.Framework;
using System;
using System.IO;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// The 2026-08-12 map-standard pass: a map's geometry comes from its own `.map` header, never from a
    /// size enum. These pin the branches that DO NOT run in normal play and therefore cannot be caught by
    /// loading Khost — the legacy fallback, and every way a header can fail to describe a map.
    ///
    /// ⚠ WHY THIS FIXTURE EXISTS. The failure it guards was SILENT: a map that was not 32x21 built a 32x21
    /// grid, `SetHexAt` refused every hex outside it with a UI message and a `false` return, the loader
    /// counted those into a `failCount` it discarded, and the map loaded, played, and was wrong. Every
    /// replacement path either produces the right size or THROWS; "produces a plausible wrong size" is the
    /// outcome that must stay impossible.
    /// </summary>
    [TestFixture]
    public class MapStandardTests : BaseTestFixture
    {
        #region Helpers

        /// <summary>A header as the editor writes them today — explicit dimensions.</summary>
        private static JsonMapHeader Header(int cols, int rows, MapConfig config = MapConfig.None) =>
            new JsonMapHeader("TestMap", config, "checksum-not-validated", cols, rows);

        /// <summary>
        /// A manifest with everything valid except the dimensions under test. Object initializer, not a
        /// positional ctor — the 16-parameter [JsonConstructor] was deleted 2026-08-17 (V11.1), and an
        /// initializer never has to change when the manifest gains a field.
        /// </summary>
        private static ScenarioManifest Manifest(int w, int h) =>
            new ScenarioManifest
            {
                ScenarioId = "test_scenario",
                DisplayName = "Test Scenario",
                Description = "description",
                ThumbnailFilename = "thumb.png",
                MapFilename = "test.map",
                OobFilename = "test.oob",
                AiiFilename = "test.aii",
                BriefingFilename = "test.brf",
                PrestigePool = 100,
                IsCampaignScenario = false,
                MapTheme = MapTheme.MiddleEast,
                DifficultyLevel = DifficultyLevel.Colonel,
                MaxTurns = 20,
                DeploymentPointCap = 100,
                MapWidth = w,
                MapHeight = h
            };

        #endregion // Helpers

        #region ResolveMapDimensions — the normal path

        [Test]
        public void ResolveMapDimensions_UsesTheHeadersOwnNumbers()
        {
            /* The whole point of the pass: an arbitrary size, not one of two blessed ones. 44x21 is the
             * shape that used to load truncated to 32x21. */
            Assert.AreEqual(new Vector2Int(44, 21), Header(44, 21).ResolveMapDimensions());
            Assert.AreEqual(new Vector2Int(56, 31), Header(56, 31).ResolveMapDimensions());
            Assert.AreEqual(new Vector2Int(10, 10), Header(10, 10).ResolveMapDimensions(),
                "the 10x10 floor is itself legal");
        }

        [Test]
        public void ResolveMapDimensions_ExplicitDimensionsBeatTheLegacyTag()
        {
            /* A file may carry BOTH — the editor keeps writing `mapConfiguration` until G1 ships in a build
             * Bob runs, so `Small` alongside real dimensions is the normal transitional state. The numbers
             * win; if the tag were consulted first this would return 32x21 and the pass would be undone. */
            var header = Header(44, 28, MapConfig.Small);

            Assert.AreEqual(new Vector2Int(44, 28), header.ResolveMapDimensions());
        }

        #endregion // The normal path

        #region ResolveMapDimensions — the legacy fallback

        [Test]
        public void ResolveMapDimensions_FallsBackToTheLegacyTableForPre2026_08Files()
        {
            /* A `.map` written before mapColumns/mapRows existed. This branch has never run in play — every
             * shipped file already carries explicit dimensions — so it is exactly the code most likely to
             * be wrong and least likely to be noticed. */
            Assert.AreEqual(new Vector2Int(32, 21), Header(0, 0, MapConfig.Small).ResolveMapDimensions(),
                "legacy Small");
            Assert.AreEqual(new Vector2Int(32, 42), Header(0, 0, MapConfig.Large).ResolveMapDimensions(),
                "legacy Large");
        }

        #endregion // The legacy fallback

        #region ResolveMapDimensions — the refusals

        [Test]
        public void ResolveMapDimensions_ThrowsWhenNothingCanAnswer()
        {
            /* ⚠ THE CASE THAT NEARLY SHIPPED AS A SILENT FAILURE. A map loaded at explicit size gets
             * `Configuration = None` from HexMap's explicit constructor. If the SAVE path then wrote a
             * header without dimensions — which it did until the writer was fixed — the reload would land
             * exactly here with nothing to fall back on. It must throw, not guess. */
            Assert.Throws<InvalidDataException>(() => Header(0, 0, MapConfig.None).ResolveMapDimensions());
        }

        [Test]
        public void ResolveMapDimensions_ThrowsOnHalfStatedDimensions()
        {
            // Columns without rows (or vice versa) is a corrupt or hand-edited file, never a legacy one.
            Assert.Throws<InvalidDataException>(() => Header(44, 0).ResolveMapDimensions());
            Assert.Throws<InvalidDataException>(() => Header(0, 21).ResolveMapDimensions());
        }

        [Test]
        public void ResolveMapDimensions_ThrowsBelowTheTenByTenFloor()
        {
            /* ⚠ A positive-but-tiny value must NOT be treated as "absent" and silently upgraded to the
             * legacy 32x21 — that would be the old silent-wrong-size failure wearing a new hat. */
            Assert.Throws<InvalidDataException>(() => Header(9, 9, MapConfig.Small).ResolveMapDimensions());
            Assert.Throws<InvalidDataException>(() => Header(44, 4).ResolveMapDimensions());
        }

        #endregion // The refusals

        #region IsValid — the dimension gate

        [Test]
        public void IsValid_AcceptsBothStatedAndBothAbsent()
        {
            Assert.IsTrue(Header(44, 21).IsValid(), "explicit dimensions");
            Assert.IsTrue(Header(0, 0, MapConfig.Small).IsValid(), "legacy file, resolved later from the tag");
        }

        [Test]
        public void IsValid_RejectsMixedOrUndersizedDimensions()
        {
            /* Caught at validation so a corrupt header fails with a clear reason, rather than throwing
             * later from inside the load. */
            Assert.IsFalse(Header(44, 0).IsValid(), "columns without rows");
            Assert.IsFalse(Header(0, 21).IsValid(), "rows without columns");
            Assert.IsFalse(Header(9, 9).IsValid(), "below the 10x10 floor");
        }

        #endregion // IsValid

        #region HexMap — explicit sizing is the only sizing

        [Test]
        public void HexMap_TakesItsBoundsFromTheDimensionsItWasGiven()
        {
            var map = new HexMap("Wide", 44, 21);

            Assert.AreEqual(new Vector2Int(44, 21), map.MapSize);
            Assert.IsTrue(map.IsPositionInBounds(new Position2D(43, 20)), "the far corner is inside");
            Assert.IsFalse(map.IsPositionInBounds(new Position2D(44, 20)), "one past the last column is not");
        }

        [Test]
        public void HexMap_AcceptsHexesPastTheOldThirtyTwoColumnCeiling()
        {
            /* The regression in one assertion. Column 40 is where a 44-wide map used to lose its hexes:
             * `SetHexAt` returned false, the loader discarded the count, and the map played wrong. */
            var map = new HexMap("Wide", 44, 21);
            var hex = new HexTile(new Vector2Int(40, 10));
            hex.SetTerrain(TerrainType.Clear);

            Assert.IsTrue(map.SetHexAt(hex), "a hex at column 40 belongs to a 44-wide map");
            Assert.IsNotNull(map.GetHexAt(new Position2D(40, 10)));
        }

        [Test]
        public void HexMap_RefusesDimensionsBelowTheFloor()
        {
            Assert.Throws<ArgumentException>(() => new HexMap("Tiny", 9, 10));
            Assert.Throws<ArgumentException>(() => new HexMap("Tiny", 10, 9));
        }

        #endregion // HexMap

        #region ScenarioManifest — no more silent 32x21

        [Test]
        public void Manifest_ReportsItsOwnDimensions()
        {
            Assert.AreEqual(new Vector2Int(44, 21), Manifest(44, 21).GetMapDimensions());
            Assert.IsTrue(Manifest(44, 21).IsValid());
        }

        [Test]
        public void Manifest_WithoutDimensions_ReportsZeroAndFailsValidation()
        {
            /* ⚠ It used to assume 32x21 here, which is the same silent-wrong-answer pattern as the map
             * loader's — a manifest that does not state its size would confidently report someone else's.
             * Zero fails IsValid, so such a manifest is refused with a reason instead. This is a real
             * behaviour change to scenario discovery; both shipped manifests carry explicit dimensions. */
            var manifest = Manifest(0, 0);

            Assert.AreEqual(Vector2Int.zero, manifest.GetMapDimensions());
            Assert.IsFalse(manifest.IsValid(), "a manifest must state its map size");
        }

        #endregion // ScenarioManifest
    }
}
