using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;

namespace HammerAndSickle.Renderers.Chunked
{
    /// <summary>
    /// Deterministic per-hex variant selection. Pure function of (Position2D, seed)
    /// — produces a stable variant index in [0, VariantsPerTerrain) and the matching
    /// Texture2DArray slice index for a given terrain.
    /// </summary>
    public static class HexChunkVariantSelector
    {
        public const int VariantsPerTerrain = 12;

        /// <summary>
        /// Default seed used by the parameter-less overloads. HexChunkRenderer mirrors
        /// its inspector value here so editor tools and gizmos see the same shuffle as
        /// the build pipeline. The build pipeline itself passes the seed explicitly via
        /// HexChunkBuildSettings — prefer the seeded overloads in new code.
        /// </summary>
        public static int SeedOffset = 0;

        /// <summary>Variant index in [0, VariantsPerTerrain) for the given hex, using SeedOffset.</summary>
        public static int GetVariant(Position2D pos) => GetVariant(pos, SeedOffset);

        /// <summary>
        /// Variant index with an explicit seed. Wymix-style integer mixer — gives
        /// clean 2D distribution without the diagonal correlations of (x*P1)^(y*P2)
        /// hashes, which surface once adjacent-variant deduplication is added.
        /// </summary>
        public static int GetVariant(Position2D pos, int seedOffset)
        {
            unchecked
            {
                uint x = (uint)(pos.IntX + seedOffset);
                uint y = (uint)(pos.IntY - seedOffset);
                uint h = x * 0x9E3779B1u;
                h ^= y * 0x85EBCA77u;
                h ^= h >> 13;
                h *= 0xC2B2AE3Du;
                h ^= h >> 16;
                return (int)(h % VariantsPerTerrain);
            }
        }

        /// <summary>
        /// Texture2DArray slice index for a hex's terrain and position:
        /// slot = (int)baseTerrain * VariantsPerTerrain + variant.
        /// MinorCity, MajorCity, and Impassable have no chunk art — they fall back to
        /// Clear so the underlying terrain blends correctly. Overlay layers draw their
        /// city/icon visuals on top.
        /// </summary>
        public static int GetSlot(Position2D pos, TerrainType terrain) => GetSlot(pos, terrain, SeedOffset);

        public static int GetSlot(Position2D pos, TerrainType terrain, int seedOffset)
        {
            TerrainType baseTerrain = terrain switch
            {
                TerrainType.MinorCity => TerrainType.Clear,
                TerrainType.MajorCity => TerrainType.Clear,
                TerrainType.Impassable => TerrainType.Clear,
                _ => terrain,
            };
            return (int)baseTerrain * VariantsPerTerrain + GetVariant(pos, seedOffset);
        }
    }
}
