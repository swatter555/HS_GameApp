using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using System;
using System.Collections.Generic;

namespace HammerAndSickle.Services
{
    /// <summary>
    /// One objective hex captured by a move that ended on it (§17.5), carrying the data the caller
    /// needs to credit VictoryValue prestige (§18.2.1) and update objective accounting.
    /// </summary>
    public struct ObjectiveCapture
    {
        public Position2D Position;
        public float VictoryValue;
        public TileControl PreviousControl;
    }

    /// <summary>
    /// Outcome of applying movement-driven tile control for one move order.
    /// </summary>
    public struct TerritoryChangeResult
    {
        /// <summary>Non-objective hexes whose ownership changed (transit / occupation / ZoC sweep).</summary>
        public List<Position2D> FlippedHexes;

        /// <summary>Objective hexes captured by ending on them (§17.5) — prestige-bearing.</summary>
        public List<ObjectiveCapture> CapturedObjectives;

        public bool AnyChange =>
            (FlippedHexes != null && FlippedHexes.Count > 0) ||
            (CapturedObjectives != null && CapturedObjectives.Count > 0);
    }

    /// <summary>
    /// Movement-driven territorial control (§6.13 + §17.5). Pure map mutation + reporting: the caller
    /// (MovementController) applies prestige / objective accounting and triggers the redraw. Only the
    /// MOVEMENT-triggered flips live here — HCL decay/recovery (§6.13.5, the Upkeep half) lands with the
    /// supply pass. Fixed-wing transit does NOT flip ownership (§6.13.2) — the caller gates that out.
    /// </summary>
    public static class TerritoryService
    {
        private const string CLASS_NAME = nameof(TerritoryService);

        /// <summary>Maps a unit's Side to its tile-control faction (§4.7.1: Player→Red, AI→Blue).</summary>
        public static TileControl ControlForSide(Side side) =>
            side == Side.Player ? TileControl.Red : TileControl.Blue;

        private static TileControl EnemyControl(TileControl own) =>
            own == TileControl.Red ? TileControl.Blue : TileControl.Red;

        /// <summary>
        /// Applies §6.13 control for a completed GROUND/HELO move:
        ///   - Transit (§6.13.2): each NON-objective hex the mover passed THROUGH (path minus the final) flips.
        ///   - Occupation: the final NON-objective hex flips.
        ///   - End-of-move ZoC sweep (§6.13.3): each ENEMY-owned, non-objective neighbor of the final hex flips
        ///     (grey/none neighbors are NOT swept — the rule is "enemy-owned").
        ///   - Objective capture (§6.13.8 / §17.5): objectives are EXEMPT from transit & ZoC sweep; an objective
        ///     the mover ENDS on flips and is reported for prestige.
        ///   - HCL resets to 1.0 on every flip (§6.13.10).
        /// <paramref name="pathEntered"/> = the hexes actually entered this move, in order; the LAST element is
        /// the hex the unit ended on. Null/empty → no change.
        /// </summary>
        public static TerritoryChangeResult ApplyMoveControl(HexMap map, CombatUnit mover, IReadOnlyList<Position2D> pathEntered)
        {
            var result = new TerritoryChangeResult
            {
                FlippedHexes = new List<Position2D>(),
                CapturedObjectives = new List<ObjectiveCapture>()
            };

            try
            {
                if (map == null || mover == null || pathEntered == null || pathEntered.Count == 0)
                    return result;

                TileControl own = ControlForSide(mover.Side);
                int lastIndex = pathEntered.Count - 1;

                // Transit (§6.13.2): hexes passed through (everything but the final). Objectives exempt (§6.13.8).
                for (int i = 0; i < lastIndex; i++)
                {
                    var tile = map.GetHexAt(pathEntered[i]);
                    if (tile == null || tile.IsObjective) continue;
                    if (FlipTo(tile, own)) result.FlippedHexes.Add(pathEntered[i]);
                }

                // Final hex — occupation (non-objective) or objective capture (§17.5).
                var destPos = pathEntered[lastIndex];
                var destTile = map.GetHexAt(destPos);
                if (destTile != null)
                {
                    if (destTile.IsObjective)
                    {
                        // §17.5: an objective flips ONLY when a ground unit ends on it. Prestige-bearing.
                        if (destTile.TileControl != own)
                        {
                            var prev = destTile.TileControl;
                            FlipTo(destTile, own);
                            result.CapturedObjectives.Add(new ObjectiveCapture
                            {
                                Position = destPos,
                                VictoryValue = destTile.VictoryValue,
                                PreviousControl = prev
                            });
                        }
                    }
                    else if (FlipTo(destTile, own))
                    {
                        result.FlippedHexes.Add(destPos);
                    }

                    // End-of-move ZoC sweep (§6.13.3): enemy-owned, non-objective neighbors of the final hex.
                    TileControl enemy = EnemyControl(own);
                    foreach (var n in HexMapUtil.GetAllNeighborPositions(destPos))
                    {
                        var nt = map.GetHexAt(n);
                        if (nt == null || nt.IsObjective) continue;     // objectives exempt (§6.13.8)
                        if (nt.TileControl != enemy) continue;          // only enemy-owned swept (§6.13.3)
                        if (FlipTo(nt, own)) result.FlippedHexes.Add(n);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ApplyMoveControl), e);
                return result;
            }
        }

        /// <summary>
        /// Flips a hex to <paramref name="own"/> and resets HCL to 1.0 in the new owner's possession
        /// (§6.13.10). Returns true only if ownership actually changed.
        /// </summary>
        private static bool FlipTo(HexTile tile, TileControl own)
        {
            if (tile.TileControl == own) return false;
            tile.TileControl = own;
            tile.HexControlLevel = 1.0f;
            return true;
        }
    }
}
