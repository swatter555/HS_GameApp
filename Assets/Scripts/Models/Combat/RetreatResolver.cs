using System;
using System.Collections.Generic;
using System.Linq;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Outcome of the map-coupled displacement that follows a defender's stand check (HS_DesignDoc §6.8 / §7.9).
    /// Position/posture/HP changes are already applied to the unit; REMOVAL is only flagged — the caller
    /// (BattleManager) does the roster/prestige/Automatic-Advance follow-ups off these flags.
    /// </summary>
    public struct DisplacementResult
    {
        public StandOutcome Outcome;
        public bool Moved;                     // changed hex (retreat/rout)
        public Position2D FinalPosition;       // where the unit ended (unchanged if held/removed-in-place)
        public int HexesRetreated;             // 0 / 1 / 2
        public bool PostureDropped;            // rout dropped a dug-in tier (§7.9.5.2)
        public bool RemovedFromMap;            // left the board (shatter-quit OR a destruction)
        public bool Destroyed;                 // permanent loss (vs a shatter-quit survival)
        public bool Surrendered;               // failed Surrender Check (§7.9.6a) — permanent
        public bool SurrenderHeldInPlace;      // passed Surrender Check — forced Deployed, took the survival loss
        public bool StaticCollapsed;           // §7.9.7 catastrophic collapse destroyed it in place
        public bool AutomaticAdvanceAvailable; // attacker MAY advance into the vacated hex (§7.9.9)
        public Position2D VacatedHex;          // the hex freed for Automatic Advance
    }

    /// <summary>
    /// Applies the board consequences of a stand outcome (§7.9.5/.6/.7/.9): retreat/rout displacement into the
    /// rear arc (§6.8), posture drop, shatter withdrawal, the Surrender Check for "must retreat but cannot",
    /// and Static catastrophic collapse. Reuses HexMapUtil/GameDataManager for geometry, occupancy, and ZoC.
    /// Automatic Advance itself is the attacker's optional move and is left to the caller (this only reports it).
    /// </summary>
    public static class RetreatResolver
    {
        private const string CLASS_NAME = nameof(RetreatResolver);

        /// <summary>
        /// Resolves displacement for <paramref name="defender"/> after a stand check produced <paramref name="outcome"/>.
        /// Dice (Static collapse 1d100, Surrender 1d20) come from <paramref name="rng"/>; a Hold or a clean
        /// retreat/rout on an open map consumes none.
        /// </summary>
        public static DisplacementResult ResolveDisplacement(
            CombatUnit attacker, CombatUnit defender, StandOutcome outcome, HexMap map, ICombatRandom rng)
        {
            var result = new DisplacementResult
            {
                Outcome = outcome,
                FinalPosition = defender?.MapPos ?? Position2D.Zero,
                VacatedHex = defender?.MapPos ?? Position2D.Zero,
            };

            try
            {
                if (attacker == null) throw new ArgumentNullException(nameof(attacker));
                if (defender == null) throw new ArgumentNullException(nameof(defender));
                if (map == null) throw new ArgumentNullException(nameof(map));
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                if (outcome == StandOutcome.Hold) return result;

                // Rear arc is fixed by the original attack bearing (defender→attacker); each retreat step picks
                // among those directions from the unit's current hex (§6.8.1). Adjacent attacker → exact bearing;
                // a non-adjacent firer (indirect, §7.13) → the general direction toward the firer.
                HexDirection bearing = HexMapUtil.GetDirectionBetween(defender.MapPos, attacker.MapPos)
                                       ?? HexMapUtil.GetGeneralDirection(defender.MapPos, attacker.MapPos);
                HexDirection[] rearDirs = HexArc.RearArc(bearing);
                Side side = defender.Side;

                if (outcome == StandOutcome.Shatter)
                {
                    defender.TakeDamage(GameData.SHATTER_EXTRA_DAMAGE);          // §7.9.6.2
                    if (defender.HitPoints.Current <= 0f)
                        return MarkRemoved(ref result, destroyed: true, aa: true);

                    bool hasPath = BestCandidate(defender.MapPos, rearDirs, side, map).HasValue;
                    if (!hasPath)
                        return ResolveSurrender(defender, rng, ref result);      // §7.9.6.3 no-path → Surrender

                    if (StaticCollapses(defender, rng, ref result))              // §7.9.6.6 / §7.9.7
                        return result;

                    return MarkRemoved(ref result, destroyed: false, aa: true);  // breaks & quits the field, survives
                }

                // Retreat or Rout: find the first step.
                Position2D? first = BestCandidate(defender.MapPos, rearDirs, side, map);
                if (!first.HasValue)
                    return ResolveSurrender(defender, rng, ref result);          // §6.8.3 / §7.9.6a

                if (StaticCollapses(defender, rng, ref result))                  // §7.9.7 — fires only with a valid path
                    return result;

                HexMapUtil.MoveUnitTo(map, defender, first.Value);
                result.Moved = true;
                result.HexesRetreated = 1;
                result.FinalPosition = first.Value;
                result.AutomaticAdvanceAvailable = true;

                if (outcome == StandOutcome.Rout)
                {
                    DeploymentPosition dropped = DropDugInTier(defender.DeploymentPosition); // §7.9.5.2
                    if (dropped != defender.DeploymentPosition)
                    {
                        defender.SetDeploymentPosition(dropped);
                        result.PostureDropped = true;
                    }

                    Position2D? second = BestCandidate(defender.MapPos, rearDirs, side, map);
                    if (second.HasValue)
                    {
                        HexMapUtil.MoveUnitTo(map, defender, second.Value);
                        result.HexesRetreated = 2;
                        result.FinalPosition = second.Value;
                    }
                    // else: rout ends after one step (§6.8.4) — no extra penalty, the original hex is already vacated.
                }

                return result;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveDisplacement), e);
                return result;
            }
        }

        #region Sub-resolutions

        private static DisplacementResult MarkRemoved(ref DisplacementResult r, bool destroyed, bool aa)
        {
            r.RemovedFromMap = true;
            r.Destroyed = destroyed;
            r.AutomaticAdvanceAvailable = aa;
            return r;
        }

        /// <summary>§7.9.7: a Static unit forced to retreat WITH a valid path may collapse (1d100). Returns true if destroyed.</summary>
        private static bool StaticCollapses(CombatUnit defender, ICombatRandom rng, ref DisplacementResult r)
        {
            if (defender.EfficiencyLevel != EfficiencyLevel.StaticOperations) return false;
            if (!SurrenderCheck.ResolveStaticCollapse(defender.ExperienceLevel, rng)) return false;
            r.RemovedFromMap = true;
            r.Destroyed = true;
            r.StaticCollapsed = true;
            r.AutomaticAdvanceAvailable = true;
            return true;
        }

        /// <summary>§7.9.6a: must-retreat-but-cannot — hold in place at a cost, or be destroyed.</summary>
        private static DisplacementResult ResolveSurrender(CombatUnit defender, ICombatRandom rng, ref DisplacementResult r)
        {
            if (SurrenderCheck.ResolveSurrender(defender.ExperienceLevel, rng) == SurrenderOutcome.Destroyed)
            {
                r.RemovedFromMap = true;
                r.Destroyed = true;
                r.Surrendered = true;
                r.AutomaticAdvanceAvailable = true;          // a failed check empties the hex (§7.9.6a.3)
            }
            else
            {
                defender.SetDeploymentPosition(DeploymentPosition.Deployed); // forced to bare Deployed
                defender.TakeDamage(GameData.SURRENDER_SURVIVAL_LOSS);
                r.SurrenderHeldInPlace = true;
                if (defender.HitPoints.Current <= 0f) { r.RemovedFromMap = true; r.Destroyed = true; }
                // a passed check holds the hex — no Automatic Advance (§7.9.6a.3)
            }
            return r;
        }

        /// <summary>Drops one dug-in sublevel toward Deployed (§7.9.5.2); Deployed/Mobile/Embarked unchanged.</summary>
        private static DeploymentPosition DropDugInTier(DeploymentPosition p) => p switch
        {
            DeploymentPosition.Fortified    => DeploymentPosition.Entrenched,
            DeploymentPosition.Entrenched   => DeploymentPosition.HastyDefense,
            DeploymentPosition.HastyDefense => DeploymentPosition.Deployed,
            _                               => p,
        };

        #endregion // Sub-resolutions

        #region Candidate search (§6.8.2)

        /// <summary>
        /// Best valid retreat candidate among the rear-arc neighbours of <paramref name="from"/>, ranked
        /// (§6.8.2): friendly tile control, then higher terrain defense bonus, then lowest movement cost, then
        /// stable rear-arc order as the hex-index tiebreak. Null if none are valid.
        /// </summary>
        private static Position2D? BestCandidate(Position2D from, HexDirection[] rearDirs, Side side, HexMap map)
        {
            var valid = new List<Position2D>(3);
            foreach (HexDirection dir in rearDirs)
            {
                Position2D pos = HexMapUtil.GetNeighborPosition(from, dir);
                if (IsValidCandidate(pos, side, map)) valid.Add(pos);
            }
            if (valid.Count == 0) return null;

            return valid
                .OrderByDescending(p => IsFriendlyControl(map, p, side))
                .ThenByDescending(p => DefenseBonus(map, p))
                .ThenBy(p => MoveCost(map, p))
                .First();
        }

        /// <summary>§6.8.2a: a candidate is invalid if off-map, impassable/water, occupied, or in enemy ZoC.</summary>
        private static bool IsValidCandidate(Position2D pos, Side side, HexMap map)
        {
            HexTile tile = map.GetHexAt(pos);
            if (tile == null) return false;                                               // off-map (§6.8.6)
            if (tile.Terrain == TerrainType.Impassable || tile.Terrain == TerrainType.Water) return false; // §4.3.6
            if (GameDataManager.Instance.GetUnitAtPosition(pos) != null) return false;    // occupied by any unit
            if (InEnemyZoC(pos, side, map)) return false;                                 // ZoC blocks retreat
            return true;
        }

        /// <summary>True if any spotted, ZoC-projecting enemy of <paramref name="side"/> is adjacent to <paramref name="pos"/> (§6.5).</summary>
        private static bool InEnemyZoC(Position2D pos, Side side, HexMap map)
        {
            GameDataManager gdm = GameDataManager.Instance;
            IEnumerable<CombatUnit> enemies = side == Side.Player ? gdm.GetAIUnits() : gdm.GetPlayerUnits();
            foreach (CombatUnit e in enemies)
            {
                if (e.SpottedLevel == SpottedLevel.Level0) continue;
                if (!e.ProjectsZoC) continue;
                if (HexMapUtil.GetHexDistance(e.MapPos, pos) == GameData.ZOC_RANGE) return true;
            }
            return false;
        }

        private static bool IsFriendlyControl(HexMap map, Position2D pos, Side side)
        {
            TileControl own = side == Side.Player ? TileControl.Red : TileControl.Blue;
            return map.GetHexAt(pos)?.TileControl == own;
        }

        private static int MoveCost(HexMap map, Position2D pos) => map.GetHexAt(pos)?.MovementCost ?? int.MaxValue;

        /// <summary>Terrain defensive modifier (§4.3.2) used as a retreat-ranking preference.</summary>
        private static int DefenseBonus(HexMap map, Position2D pos) => (map.GetHexAt(pos)?.Terrain) switch
        {
            TerrainType.Forest    => 1,
            TerrainType.Rough     => 2,
            TerrainType.Marsh     => 3,
            TerrainType.Mountains => 4,
            TerrainType.MinorCity => 1,
            TerrainType.MajorCity => 3,
            _                     => 0,
        };

        #endregion // Candidate search
    }
}
