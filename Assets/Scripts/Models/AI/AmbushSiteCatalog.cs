using System;
using System.Collections.Generic;
using System.Linq;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using HammerAndSickle.Utils;

namespace HammerAndSickle.Models.AI
{
    /// <summary>
    /// One catalogued ambush position: a covered hex flanking an avenue, from which an unspotted unit
    /// springs the §6.9 ambush when a mover enters an adjacent path hex.
    /// </summary>
    public sealed class AmbushSite
    {
        public Position2D Hex;

        /// <summary>Avenue this site covers.</summary>
        public int AvenueId;

        /// <summary>The earliest avenue hex whose entry triggers the ambush (§6.9.1 — mover adjacent to the unspotted ambusher).</summary>
        public Position2D TriggerHex;

        /// <summary>Terrain block tier of the site hex (None 0 … Heavy 3) — cover for the ambusher.</summary>
        public int CoverTier;

        /// <summary>How many avenue path hexes this site flanks (multi-hex kill zone).</summary>
        public int PathAdjacency;

        /// <summary>True if a shoot-and-scoot exit exists — a traversable neighbor clear of the avenue's adjacency.</summary>
        public bool HasDisplaceRoute;

        /// <summary>Ranking heuristic: cover ×2 + kill-zone breadth + displace bonus (tunable, Part 4.6).</summary>
        public double Score;
    }

    /// <summary>
    /// Ambush-site catalog (AI-Design-Supplement Part 4.6): for each avenue, every covered, occupiable
    /// hex flanking the path, annotated with trigger geometry, kill-zone breadth, and displace-route
    /// availability. TERRAIN ONLY — ambusher class eligibility (§6.9.9) and spotting exposure are the
    /// web manager's (AI4) and belief layer's (AI2) concerns. Primary consumer: the irregular doctrine.
    /// </summary>
    public static class AmbushSiteCatalog
    {
        private const string CLASS_NAME = nameof(AmbushSiteCatalog);

        public static List<AmbushSite> Build(HexMap map, IReadOnlyList<Avenue> avenues)
        {
            var sites = new List<AmbushSite>();
            try
            {
                if (map == null) throw new ArgumentNullException(nameof(map));
                if (avenues == null) throw new ArgumentNullException(nameof(avenues));

                foreach (Avenue avenue in avenues)
                {
                    var pathSet = new HashSet<Position2D>(avenue.Path);
                    var pathIndex = new Dictionary<Position2D, int>();
                    for (int i = 0; i < avenue.Path.Count; i++) pathIndex[avenue.Path[i]] = i;

                    // Hexes adjacent to any path hex — a displace exit must clear this halo.
                    var adjacentToPath = new HashSet<Position2D>();
                    foreach (Position2D p in avenue.Path)
                        foreach (Position2D n in HexMapUtil.GetAllNeighborPositions(p))
                            adjacentToPath.Add(n);

                    // Collect candidate sites: covered, occupiable hexes flanking the path.
                    var byHex = new Dictionary<Position2D, AmbushSite>();
                    foreach (Position2D p in avenue.Path)
                    {
                        foreach (Position2D n in HexMapUtil.GetAllNeighborPositions(p))
                        {
                            if (pathSet.Contains(n)) continue;
                            HexTile tile = map.GetHexAt(n);
                            if (tile == null) continue;
                            if (tile.Terrain == TerrainType.Water || tile.Terrain == TerrainType.Impassable) continue;
                            if (tile.MovementCost <= 0) continue;

                            var tier = CombatMath.BlockTier(tile.Terrain);
                            if (tier == TerrainBlockTier.None) continue; // no cover — not an ambush site

                            if (!byHex.TryGetValue(n, out AmbushSite site))
                            {
                                site = new AmbushSite
                                {
                                    Hex = n,
                                    AvenueId = avenue.Id,
                                    TriggerHex = p,
                                    CoverTier = (int)tier,
                                };
                                byHex[n] = site;
                            }
                            site.PathAdjacency++;
                            // Earliest trigger along the avenue = where the mover meets the ambush first.
                            if (pathIndex[p] < pathIndex[site.TriggerHex]) site.TriggerHex = p;
                        }
                    }

                    // Displace routes + scoring.
                    foreach (AmbushSite site in byHex.Values)
                    {
                        HexTile siteTile = map.GetHexAt(site.Hex);
                        for (int d = 0; d < 6; d++)
                        {
                            var dir = (HexDirection)d;
                            Position2D exit = HexMapUtil.GetNeighborPosition(site.Hex, dir);
                            if (pathSet.Contains(exit) || adjacentToPath.Contains(exit)) continue;
                            HexTile exitTile = map.GetHexAt(exit);
                            if (exitTile == null) continue;
                            if (MobilityMap.GroundStepCost(siteTile, exitTile, dir) < 0) continue;
                            site.HasDisplaceRoute = true;
                            break;
                        }

                        site.Score = site.CoverTier * 2.0 + site.PathAdjacency + (site.HasDisplaceRoute ? 2.0 : 0.0);
                        sites.Add(site);
                    }
                }

                return sites.OrderByDescending(s => s.Score)
                            .ThenBy(s => (int)s.Hex.Y).ThenBy(s => (int)s.Hex.X)
                            .ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Build), e);
                return sites;
            }
        }
    }
}
