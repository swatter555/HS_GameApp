using System.Collections.Generic;
using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using UnityEngine;
using UnityEngine.Rendering;

namespace HammerAndSickle.Renderers.Chunked
{
    /// <summary>
    /// Per-build tuning bundle. The builder is pure — it holds no static configuration
    /// state. HexChunkRenderer constructs this from its inspector values once per build
    /// and threads it through the call chain.
    /// </summary>
    public readonly struct HexChunkBuildSettings
    {
        /// <summary>Exponent applied to corner blend weights. >1 peaks toward the dominant slot, &lt;1 flattens.</summary>
        public readonly float CornerWeightPower;
        /// <summary>Multiplier on the owning hex's slot weight at each corner before normalization. >1 makes hex interiors look more like their own texture.</summary>
        public readonly float CenterHexBias;
        /// <summary>Pulls corner UVs away from the [0,1] tile edges to avoid edge-pixel artifacts. Range ~[0, 0.15].</summary>
        public readonly float UVInset;
        /// <summary>Seed offset for the variant hash. Reshuffles which variant lands on which hex without rebuilding art.</summary>
        public readonly int VariantSeedOffset;

        public HexChunkBuildSettings(float cornerWeightPower, float centerHexBias, float uvInset, int variantSeedOffset)
        {
            CornerWeightPower = cornerWeightPower;
            CenterHexBias = centerHexBias;
            UVInset = uvInset;
            VariantSeedOffset = variantSeedOffset;
        }

        public static HexChunkBuildSettings Default => new(1f, 1f, 0f, 0);
    }

    /// <summary>
    /// Builds a Unity Mesh for one 16x16 hex chunk. Each hex becomes 6 fan triangles
    /// (center + an adjacent corner pair). Per-triangle vertex emission (no vertex
    /// dedup) keeps UVs relative to the owning hex; corner blend weights are cached
    /// per physical corner so adjacent hexes meeting at a corner agree on the blend.
    ///
    /// Vertex layout matches HexTerrainBlend.shader:
    ///   POSITION  (float3)  — world-space XY, Z=0
    ///   TEXCOORD0 (float2)  — UV in [0,1] relative to parent hex
    ///   TEXCOORD1 (float4)  — up to 4 Texture2DArray slice indices (nointerpolation)
    ///   TEXCOORD2 (float4)  — matching blend weights (sum ≈ 1 per vertex)
    /// </summary>
    public static class HexChunkMeshBuilder
    {
        public const int ChunkSize = 16;

        #region Vertex Layout

        private static readonly VertexAttributeDescriptor[] VertexLayout =
        {
            new(VertexAttribute.Position,  VertexAttributeFormat.Float32, 3),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            new(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
            new(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4),
        };

        private struct Vertex
        {
            public Vector3 pos;
            public Vector2 uv;
            public Vector4 terrainIdx;
            public Vector4 weights;
        }

        #endregion // Vertex Layout

        #region Hex Geometry

        // Voronoi-cell corners for a regular pointy-top hex, in HexGridSystem world units.
        // Derivation (Voronoi vertices = circumcenters of the three hex centers meeting at
        // each corner), with HW = HALF_HEX_WIDTH (1.28) and VS = VERTICAL_SPACING (sqrt(3)*HW):
        //   dy_near = (HW² - VS²) / (2·VS)  ≈ -0.7391
        //   dy_far  = -(VS² + HW²) / (2·VS) ≈ -1.4781
        //
        // Index convention (matches HexDirection NE=0, E=1, SE=2, SW=3, W=4, NW=5):
        //   corner C is the vertex shared with the neighbor in direction C and the
        //   neighbor in direction (C+5)%6.
        private static readonly Vector2[] CornerOffsets = new Vector2[6];

        // Half-extents of the hex's UV bounding box, used by CornerUV.
        private static float _hexHalfWidth;
        private static float _hexHalfHeight;

        static HexChunkMeshBuilder()
        {
            // Pull from HexGridSystem so geometry stays consistent with HexToWorld.
            const float HW = HexGridSystem.HALF_HEX_WIDTH;
            const float VS = HexGridSystem.VERTICAL_SPACING;

            float dyNear = (HW * HW - VS * VS) / (2f * VS);
            float dyFar  = -(VS * VS + HW * HW) / (2f * VS);

            CornerOffsets[0] = new Vector2(0f,  -dyFar);   // top         (between NW, NE)
            CornerOffsets[1] = new Vector2(+HW, -dyNear);  // upper-right (between NE, E)
            CornerOffsets[2] = new Vector2(+HW,  dyNear);  // lower-right (between E, SE)
            CornerOffsets[3] = new Vector2(0f,   dyFar);   // bottom      (between SE, SW)
            CornerOffsets[4] = new Vector2(-HW,  dyNear);  // lower-left  (between SW, W)
            CornerOffsets[5] = new Vector2(-HW, -dyNear);  // upper-left  (between W, NW)

            _hexHalfWidth = HW;
            _hexHalfHeight = -dyFar;
        }

        #endregion // Hex Geometry

        #region Public API

        /// <summary>
        /// Builds a mesh for the chunk at grid position (chunkX, chunkY). Reads terrain
        /// data from the HexMap and delegates coordinate math to HexGridSystem. All
        /// build-time tuning passes through <paramref name="settings"/>.
        /// Returns null when the chunk region contains no in-bounds hexes.
        /// </summary>
        public static Mesh Build(int chunkX, int chunkY, HexMap map, HexGridSystem grid, HexChunkBuildSettings settings)
        {
            int startCol = chunkX * ChunkSize;
            int startRow = chunkY * ChunkSize;

            var chunkHexes = new List<HexTile>();
            for (int row = startRow; row < startRow + ChunkSize; row++)
            {
                for (int col = startCol; col < startCol + ChunkSize; col++)
                {
                    var pos = new Position2D(col, row);
                    var tile = map.GetHexAt(pos);
                    if (tile != null) chunkHexes.Add(tile);
                }
            }

            if (chunkHexes.Count == 0) return null;

            // Cache (slot indices, weights) per physical corner so the two hexes (or three
            // at 3-way corners) meeting at a corner produce identical blend data, avoiding
            // mismatched seams.
            var cornerBlendCache = new Dictionary<long, (Vector4 idx, Vector4 w)>();

            // Per-triangle vertex emission, no vertex dedup. UVs always relative to the
            // owning hex's center — sharing corner vertices across hexes makes UVs come
            // from whichever hex emitted the vertex first, producing kaleidoscopic patterns.
            var vertices = new List<Vertex>();
            var indices = new List<int>();

            foreach (var tile in chunkHexes)
            {
                Vector3 center = grid.HexToWorld(tile.Position);
                int centerSlot = HexChunkVariantSelector.GetSlot(tile.Position, tile.Terrain, settings.VariantSeedOffset);

                var cornerPositions = new Vector3[6];
                var cornerBlends = new (Vector4 idx, Vector4 w)[6];

                for (int c = 0; c < 6; c++)
                {
                    cornerPositions[c] = center + (Vector3)CornerOffsets[c];

                    long key = CanonicalCornerKey(tile.Position, c, grid);
                    if (!cornerBlendCache.TryGetValue(key, out var blend))
                    {
                        blend = ComputeCornerBlend(tile, c, map, grid, settings);
                        cornerBlendCache[key] = blend;
                    }
                    cornerBlends[c] = blend;
                }

                for (int c = 0; c < 6; c++)
                {
                    int next = (c + 1) % 6;

                    // Slot union across center + both corners. Shader's nointerpolation
                    // qualifier on terrainIndices requires all three vertices of a triangle
                    // to carry the same slot list in the same order.
                    var slotSet = new List<int>(4);
                    AddSlotFromIdx(slotSet, centerSlot);
                    CollectSlotsFromBlend(slotSet, cornerBlends[c].idx, cornerBlends[c].w);
                    CollectSlotsFromBlend(slotSet, cornerBlends[next].idx, cornerBlends[next].w);

                    if (slotSet.Count > 4)
                    {
                        // Hard 4-slot ceiling. Rank by effective per-vertex weight summed
                        // across the 3 triangle vertices; apply CornerWeightPower so the
                        // ranking matches the shader's rendered output. Center vertex weight
                        // on centerSlot is 1 (1^p == 1) — keep the binary form for it.
                        float power = settings.CornerWeightPower;
                        float Eff(float w)
                        {
                            if (Mathf.Approximately(power, 1f)) return w;
                            return Mathf.Pow(Mathf.Max(0f, w), Mathf.Max(0.01f, power));
                        }

                        slotSet.Sort((a, b) =>
                        {
                            float wa = (a == centerSlot ? 1f : 0f)
                                       + Eff(BlendWeightForSlot(cornerBlends[c], a))
                                       + Eff(BlendWeightForSlot(cornerBlends[next], a));
                            float wb = (b == centerSlot ? 1f : 0f)
                                       + Eff(BlendWeightForSlot(cornerBlends[c], b))
                                       + Eff(BlendWeightForSlot(cornerBlends[next], b));
                            return wb.CompareTo(wa);
                        });
                        slotSet.RemoveRange(4, slotSet.Count - 4);
                    }

                    var sharedIdx = new Vector4(
                        slotSet.Count > 0 ? slotSet[0] : 0,
                        slotSet.Count > 1 ? slotSet[1] : 0,
                        slotSet.Count > 2 ? slotSet[2] : 0,
                        slotSet.Count > 3 ? slotSet[3] : 0);

                    var centerWeights = RemapWeightsToShared(
                        new Vector4(centerSlot, 0, 0, 0),
                        new Vector4(1, 0, 0, 0),
                        slotSet, settings.CornerWeightPower);

                    var wC = RemapWeightsToShared(cornerBlends[c].idx, cornerBlends[c].w, slotSet, settings.CornerWeightPower);
                    var wN = RemapWeightsToShared(cornerBlends[next].idx, cornerBlends[next].w, slotSet, settings.CornerWeightPower);

                    int baseIdx = vertices.Count;

                    vertices.Add(new Vertex
                    {
                        pos = center,
                        uv = new Vector2(0.5f, 0.5f),
                        terrainIdx = sharedIdx,
                        weights = centerWeights,
                    });
                    vertices.Add(new Vertex
                    {
                        pos = cornerPositions[c],
                        uv = CornerUV(CornerOffsets[c], settings.UVInset),
                        terrainIdx = sharedIdx,
                        weights = wC,
                    });
                    vertices.Add(new Vertex
                    {
                        pos = cornerPositions[next],
                        uv = CornerUV(CornerOffsets[next], settings.UVInset),
                        terrainIdx = sharedIdx,
                        weights = wN,
                    });

                    indices.Add(baseIdx);
                    indices.Add(baseIdx + 1);
                    indices.Add(baseIdx + 2);
                }
            }

            return CreateMesh(vertices, indices);
        }

        #endregion // Public API

        #region Corner Blend Helpers

        /// <summary>
        /// Computes (slot indices, weights) for a corner shared by 1-3 hexes.
        /// Hexes meeting at the corner that share the same TerrainType collapse into a
        /// single slot (deterministic representative variant) so a 3-way corner contributes
        /// at most one slot per distinct terrain. This keeps per-triangle slot unions ≤ 4
        /// in practice and avoids asymmetric truncation that would produce one-sided seams.
        /// </summary>
        private static (Vector4 idx, Vector4 w) ComputeCornerBlend(
            HexTile tile, int cornerIndex, HexMap map, HexGridSystem grid, HexChunkBuildSettings settings)
        {
            var posA = tile.Position;
            var posB = grid.GetNeighborPosition(posA, (HexDirection)(cornerIndex % 6));
            var posC = grid.GetNeighborPosition(posA, (HexDirection)((cornerIndex + 5) % 6));

            var tileB = map.GetHexAt(posB);
            var tileC = map.GetHexAt(posC);

            int count = 1 + (tileB != null ? 1 : 0) + (tileC != null ? 1 : 0);
            float baseW = 1f / count;

            // Group 0 = owning hex A. CenterHexBias scales its weight; the totals are
            // renormalized at the end so the sum is still 1.
            TerrainType t0 = tile.Terrain;
            int s0 = HexChunkVariantSelector.GetSlot(posA, t0, settings.VariantSeedOffset);
            Position2D p0 = posA;
            float w0 = baseW * Mathf.Max(0.01f, settings.CenterHexBias);

            TerrainType t1 = default; int s1 = 0; Position2D p1 = default; float w1 = 0f; bool has1 = false;
            TerrainType t2 = default; int s2 = 0; Position2D p2 = default; float w2 = 0f; bool has2 = false;

            void AddHex(TerrainType t, int slot, Position2D pos)
            {
                if (t == t0)
                {
                    w0 += baseW;
                    if (ComparePos(pos, p0) < 0) { s0 = slot; p0 = pos; }
                    return;
                }
                if (has1 && t == t1)
                {
                    w1 += baseW;
                    if (ComparePos(pos, p1) < 0) { s1 = slot; p1 = pos; }
                    return;
                }
                if (!has1)
                {
                    t1 = t; s1 = slot; p1 = pos; w1 = baseW; has1 = true;
                    return;
                }
                if (has2 && t == t2)
                {
                    w2 += baseW;
                    if (ComparePos(pos, p2) < 0) { s2 = slot; p2 = pos; }
                    return;
                }
                t2 = t; s2 = slot; p2 = pos; w2 = baseW; has2 = true;
            }

            if (tileB != null) AddHex(tileB.Terrain, HexChunkVariantSelector.GetSlot(posB, tileB.Terrain, settings.VariantSeedOffset), posB);
            if (tileC != null) AddHex(tileC.Terrain, HexChunkVariantSelector.GetSlot(posC, tileC.Terrain, settings.VariantSeedOffset), posC);

            float wSum = w0 + w1 + w2;
            if (wSum > 1e-5f) { w0 /= wSum; w1 /= wSum; w2 /= wSum; }

            // Unused slots default to s0 (the owning hex), never 0 — slot 0 is Water
            // variant 0, which is magenta in POC arrays. Defaulting unused channels to
            // s0 ensures any accidental sampling produces the owning hex's terrain.
            //
            // The 4th channel is intentionally a duplicate of s0 with zero weight — a
            // safe placeholder for the shader's nointerpolation slot list. CollectSlotsFromBlend
            // gates on weight > 0, so this duplicate doesn't inflate the slot-union count.
            return (
                new Vector4(s0, has1 ? s1 : s0, has2 ? s2 : s0, s0),
                new Vector4(w0, w1, w2, 0f)
            );
        }

        /// <summary>
        /// UV from a corner offset, mapped to [0,1] relative to hex center.
        /// uvInset compresses UVs toward 0.5 to avoid sampling tile-edge pixels.
        /// </summary>
        private static Vector2 CornerUV(Vector2 offset, float uvInset)
        {
            float u = offset.x / (2f * _hexHalfWidth) + 0.5f;
            float v = offset.y / (2f * _hexHalfHeight) + 0.5f;
            float scale = 1f - 2f * Mathf.Clamp(uvInset, 0f, 0.49f);
            u = 0.5f + (u - 0.5f) * scale;
            v = 0.5f + (v - 0.5f) * scale;
            return new Vector2(u, v);
        }

        private static void AddSlotFromIdx(List<int> slotSet, int slot)
        {
            if (!slotSet.Contains(slot)) slotSet.Add(slot);
        }

        private static void CollectSlotsFromBlend(List<int> slotSet, Vector4 idx, Vector4 w)
        {
            if (w.x > 0f) AddSlotFromIdx(slotSet, Mathf.RoundToInt(idx.x));
            if (w.y > 0f) AddSlotFromIdx(slotSet, Mathf.RoundToInt(idx.y));
            if (w.z > 0f) AddSlotFromIdx(slotSet, Mathf.RoundToInt(idx.z));
            if (w.w > 0f) AddSlotFromIdx(slotSet, Mathf.RoundToInt(idx.w));
        }

        private static float BlendWeightForSlot((Vector4 idx, Vector4 w) blend, int slot)
        {
            if (Mathf.RoundToInt(blend.idx.x) == slot) return blend.w.x;
            if (Mathf.RoundToInt(blend.idx.y) == slot) return blend.w.y;
            if (Mathf.RoundToInt(blend.idx.z) == slot) return blend.w.z;
            if (Mathf.RoundToInt(blend.idx.w) == slot) return blend.w.w;
            return 0f;
        }

        /// <summary>
        /// Remaps a vertex's (slot, weight) pairs into the triangle's shared slot order,
        /// applies CornerWeightPower, and renormalizes so the result sums to 1.
        /// </summary>
        private static Vector4 RemapWeightsToShared(Vector4 origIdx, Vector4 origW, List<int> slotSet, float cornerWeightPower)
        {
            var result = Vector4.zero;
            for (int i = 0; i < slotSet.Count && i < 4; i++)
            {
                int slot = slotSet[i];
                if (Mathf.RoundToInt(origIdx.x) == slot) result[i] += origW.x;
                if (Mathf.RoundToInt(origIdx.y) == slot) result[i] += origW.y;
                if (Mathf.RoundToInt(origIdx.z) == slot) result[i] += origW.z;
                if (Mathf.RoundToInt(origIdx.w) == slot) result[i] += origW.w;
            }

            if (!Mathf.Approximately(cornerWeightPower, 1f))
            {
                float p = Mathf.Max(0.01f, cornerWeightPower);
                result.x = Mathf.Pow(result.x, p);
                result.y = Mathf.Pow(result.y, p);
                result.z = Mathf.Pow(result.z, p);
                result.w = Mathf.Pow(result.w, p);
            }

            float sum = result.x + result.y + result.z + result.w;
            if (sum > 1e-5f) result /= sum;
            else result.x = 1f;
            return result;
        }

        #endregion // Corner Blend Helpers

        #region Corner Key

        /// <summary>
        /// Canonical 64-bit key for a hex corner so the same physical corner produces
        /// the same key regardless of which adjacent hex computes it. Encodes the
        /// minimum and second-minimum of the three hex positions meeting at the corner;
        /// any approach to the corner sees the same min+mid pair.
        /// </summary>
        private static long CanonicalCornerKey(Position2D hexPos, int cornerIndex, HexGridSystem grid)
        {
            var posA = hexPos;
            var posB = grid.GetNeighborPosition(hexPos, (HexDirection)(cornerIndex % 6));
            var posC = grid.GetNeighborPosition(hexPos, (HexDirection)((cornerIndex + 5) % 6));

            var min = MinPos(MinPos(posA, posB), posC);
            var mid = MidPos(posA, posB, posC, min);
            return ((long)min.IntX << 40) | ((long)(min.IntY & 0xFFFFF) << 20) |
                   ((long)(mid.IntX & 0x3FF) << 10) | (long)(mid.IntY & 0x3FF);
        }

        private static Position2D MinPos(Position2D a, Position2D b)
        {
            if (a.IntY < b.IntY) return a;
            if (a.IntY > b.IntY) return b;
            return a.IntX <= b.IntX ? a : b;
        }

        private static Position2D MidPos(Position2D a, Position2D b, Position2D c, Position2D min)
        {
            var sorted = new Position2D[3] { a, b, c };
            if (ComparePos(sorted[0], sorted[1]) > 0) (sorted[0], sorted[1]) = (sorted[1], sorted[0]);
            if (ComparePos(sorted[1], sorted[2]) > 0) (sorted[1], sorted[2]) = (sorted[2], sorted[1]);
            if (ComparePos(sorted[0], sorted[1]) > 0) (sorted[0], sorted[1]) = (sorted[1], sorted[0]);
            return sorted[1];
        }

        private static int ComparePos(Position2D a, Position2D b)
        {
            int cmp = a.IntY.CompareTo(b.IntY);
            return cmp != 0 ? cmp : a.IntX.CompareTo(b.IntX);
        }

        #endregion // Corner Key

        #region Mesh Creation

        private static Mesh CreateMesh(List<Vertex> verts, List<int> tris)
        {
            var mesh = new Mesh
            {
                name = "HexChunkMesh",
                indexFormat = verts.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };

            mesh.SetVertexBufferParams(verts.Count, VertexLayout);
            mesh.SetVertexBufferData(verts.ToArray(), 0, 0, verts.Count);
            mesh.SetIndices(tris.ToArray(), MeshTopology.Triangles, 0, calculateBounds: true);
            // Required: SetIndices(calculateBounds:true) does NOT correctly compute bounds
            // when vertex positions came from SetVertexBufferData with a custom
            // VertexAttributeDescriptor layout. Removing this call leaves bounds at
            // default/zero, frustum culling drops the chunks, and the terrain disappears.
            mesh.RecalculateBounds();

            return mesh;
        }

        #endregion // Mesh Creation
    }
}
