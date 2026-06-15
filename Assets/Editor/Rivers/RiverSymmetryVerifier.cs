using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using UnityEditor;
using UnityEngine;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;

namespace HammerAndSickle.EditorTools.Rivers
{
    /// <summary>
    /// Verifies river border symmetry in a .map file. For every river edge on hex A,
    /// checks that the opposite edge on the corresponding neighbor hex B is also a river.
    /// Reports one-sided edges, out-of-bounds neighbors, and a total count.
    ///
    /// Uses pure geometric offsets for neighbor lookup (does not depend on HexGridSystem)
    /// so the verifier can run in edit mode without Unity runtime state and serves as an
    /// independent cross-check of the direction-table fix.
    /// </summary>
    public static class RiverSymmetryVerifier
    {
        [MenuItem("Tools/Rivers/Verify River Symmetry...")]
        public static void VerifyFromFile()
        {
            string startDir = Path.Combine(Application.dataPath, "Generated Data/map");
            if (!Directory.Exists(startDir)) startDir = Application.dataPath;

            string path = EditorUtility.OpenFilePanel("Select .map file", startDir, "map");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    IncludeFields = false,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                };
                var mapData = JsonSerializer.Deserialize<JsonMapData>(json, options);
                if (mapData == null || mapData.Hexes == null)
                {
                    Debug.LogError($"[RiverSymmetryVerifier] Failed to deserialize {path}");
                    return;
                }

                Verify(mapData, path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RiverSymmetryVerifier] Exception: {ex}");
            }
        }

        private static void Verify(JsonMapData mapData, string sourcePath)
        {
            // Index hexes by (col, row) for O(1) neighbor lookup.
            var byPos = new Dictionary<(int, int), HexTile>(mapData.Hexes.Length);
            foreach (var h in mapData.Hexes)
            {
                if (h == null) continue;
                byPos[(h.Position.IntX, h.Position.IntY)] = h;
            }

            int riverEdgeCount = 0;   // total unique river edges
            int symmetric = 0;        // both sides agree
            int oneSided = 0;         // only one side has the flag
            int outOfBounds = 0;      // neighbor hex missing from map
            var mismatches = new List<string>(64);

            // Canonical right-half iteration avoids double-counting symmetric edges.
            // Each hex owns responsibility for checking its NE, E, SE edges.
            HexDirection[] canonical = { HexDirection.NE, HexDirection.E, HexDirection.SE };

            foreach (var hex in mapData.Hexes)
            {
                if (hex == null || hex.RiverBorders == null) continue;

                foreach (var dir in canonical)
                {
                    bool hexSide = hex.RiverBorders.GetBorder(dir);

                    // Check neighbor regardless — we also want to catch cases where the
                    // neighbor has the opposite flag set but this hex does not.
                    var neighborPos = GeometricNeighbor(hex.Position.IntX, hex.Position.IntY, dir);
                    if (!byPos.TryGetValue(neighborPos, out var neighbor))
                    {
                        if (hexSide)
                        {
                            riverEdgeCount++;
                            outOfBounds++;
                            mismatches.Add($"OOB: hex ({hex.Position.IntX},{hex.Position.IntY}).{dir}=true but neighbor ({neighborPos.Item1},{neighborPos.Item2}) is outside map");
                        }
                        continue;
                    }

                    HexDirection opposite = Opposite(dir);
                    bool neighborSide = neighbor.RiverBorders != null && neighbor.RiverBorders.GetBorder(opposite);

                    if (!hexSide && !neighborSide) continue;

                    riverEdgeCount++;
                    if (hexSide && neighborSide)
                    {
                        symmetric++;
                    }
                    else
                    {
                        oneSided++;
                        if (mismatches.Count < 200)
                        {
                            mismatches.Add(
                                $"One-sided: ({hex.Position.IntX},{hex.Position.IntY}).{dir}={hexSide} vs " +
                                $"({neighbor.Position.IntX},{neighbor.Position.IntY}).{opposite}={neighborSide}");
                        }
                    }
                }
            }

            // Also scan the left-half (W, NW, SW) to catch edges the canonical pass would miss:
            // a river declared only on the left side of an edge, with no right-owner sibling.
            // These are reported separately so they are not double-counted with the right-half scan.
            int orphanLeftHalf = 0;
            HexDirection[] leftHalf = { HexDirection.SW, HexDirection.W, HexDirection.NW };
            foreach (var hex in mapData.Hexes)
            {
                if (hex == null || hex.RiverBorders == null) continue;
                foreach (var dir in leftHalf)
                {
                    if (!hex.RiverBorders.GetBorder(dir)) continue;

                    var neighborPos = GeometricNeighbor(hex.Position.IntX, hex.Position.IntY, dir);
                    if (!byPos.TryGetValue(neighborPos, out var neighbor)) continue;
                    if (neighbor.RiverBorders == null || !neighbor.RiverBorders.GetBorder(Opposite(dir)))
                    {
                        orphanLeftHalf++;
                        if (mismatches.Count < 200)
                        {
                            mismatches.Add(
                                $"Left-half orphan: ({hex.Position.IntX},{hex.Position.IntY}).{dir}=true but " +
                                $"({neighbor.Position.IntX},{neighbor.Position.IntY}).{Opposite(dir)}=false");
                        }
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[RiverSymmetryVerifier] {Path.GetFileName(sourcePath)}");
            sb.AppendLine($"  Hexes scanned:          {mapData.Hexes.Length}");
            sb.AppendLine($"  River edges (canonical): {riverEdgeCount}");
            sb.AppendLine($"  Symmetric:              {symmetric}");
            sb.AppendLine($"  One-sided (canonical):  {oneSided}");
            sb.AppendLine($"  Out-of-bounds edges:    {outOfBounds}");
            sb.AppendLine($"  Left-half orphans:      {orphanLeftHalf}");

            if (mismatches.Count == 0)
            {
                sb.AppendLine("  RESULT: all river edges are symmetric.");
                Debug.Log(sb.ToString());
            }
            else
            {
                int shown = Math.Min(mismatches.Count, 200);
                sb.AppendLine($"  RESULT: {oneSided + orphanLeftHalf + outOfBounds} asymmetric/orphaned edges (showing up to {shown}):");
                for (int i = 0; i < shown; i++) sb.AppendLine("    " + mismatches[i]);
                if (mismatches.Count > 200) sb.AppendLine($"    ... ({mismatches.Count - 200} more)");
                Debug.LogWarning(sb.ToString());
            }
        }

        // Pure geometric neighbor lookup — matches the corrected HexGridSystem tables.
        // Independent implementation so the verifier cross-checks the fix.
        private static (int, int) GeometricNeighbor(int col, int row, HexDirection dir)
        {
            bool oddRow = (row & 1) == 1;
            int dx = 0, dy = 0;
            if (oddRow)
            {
                switch (dir)
                {
                    case HexDirection.NE: dx = 1; dy = 1; break;
                    case HexDirection.E: dx = 1; dy = 0; break;
                    case HexDirection.SE: dx = 1; dy = -1; break;
                    case HexDirection.SW: dx = 0; dy = -1; break;
                    case HexDirection.W: dx = -1; dy = 0; break;
                    case HexDirection.NW: dx = 0; dy = 1; break;
                }
            }
            else
            {
                switch (dir)
                {
                    case HexDirection.NE: dx = 0; dy = 1; break;
                    case HexDirection.E: dx = 1; dy = 0; break;
                    case HexDirection.SE: dx = 0; dy = -1; break;
                    case HexDirection.SW: dx = -1; dy = -1; break;
                    case HexDirection.W: dx = -1; dy = 0; break;
                    case HexDirection.NW: dx = -1; dy = 1; break;
                }
            }
            return (col + dx, row + dy);
        }

        private static HexDirection Opposite(HexDirection d) => (HexDirection)(((int)d + 3) % 6);
    }
}
