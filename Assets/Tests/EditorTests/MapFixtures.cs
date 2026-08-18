using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Shared map-building helpers for EditorTests (V16.2, 2026-08-17). Fourteen fixtures each grew a
    /// private CreateClearMap/Strip copy before this existed — and the G3 constructor change had to touch
    /// eleven of them. New tests build maps HERE; do not mint a fifteenth private copy.
    /// ⚠ `HexMap(name, w, h)` throws below 10x10 and does NOT prefill tiles — every helper fills the
    /// full rectangle explicitly.
    /// </summary>
    public static class MapFixtures
    {
        /// <summary>
        /// A fully-populated map of one terrain under one controller, neighbors built. The default —
        /// Clear under Grey — is the neutral baseline on which flips and scoring are observable.
        /// </summary>
        public static HexMap UniformMap(int width = 12, int height = 12,
            TerrainType terrain = TerrainType.Clear, TileControl control = TileControl.Grey,
            string name = "TestMap")
        {
            var map = new HexMap(name, width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var hex = new HexTile(new Vector2Int(x, y));
                    hex.SetTerrain(terrain);
                    hex.TileControl = control;
                    map.SetHexAt(hex);
                }
            }

            map.BuildNeighborRelationships();
            return map;
        }

        /// <summary>Tile lookup shorthand — the same At() every private fixture re-declared.</summary>
        public static HexTile At(HexMap map, int x, int y) => map.GetHexAt(new Position2D(x, y));

        /// <summary>Stamps victory value + control on one hex and returns it, for ledger arithmetic setups.</summary>
        public static HexTile SetVictory(HexMap map, int x, int y, float value, TileControl control)
        {
            var hex = At(map, x, y);
            hex.VictoryValue = value;
            hex.TileControl = control;
            return hex;
        }
    }
}
