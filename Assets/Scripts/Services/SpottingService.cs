using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Services
{
    /// <summary>
    /// Result of an air ambush detection check.
    /// </summary>
    public enum AirAmbushResult
    {
        NoThreat,
        Detected,
        Ambushed
    }

    /// <summary>
    /// Handles all spotting, fog-of-war, and ambush detection logic for the battle scene.
    /// Called by MovementController per hex step and by BattleManager at turn start/admin phase.
    /// </summary>
    public static class SpottingService
    {
        private const string CLASS_NAME = nameof(SpottingService);

        #region Core Spotting

        /// <summary>
        /// Full spotting pass: each player spotter checks all enemy units within range and applies its
        /// PASSIVE CONTACT CEILING (§12.4.2 — Level 1 at range, Level 2 adjacent, Level 3 for an adjacent
        /// RECON unit). Called at turn start.
        ///
        /// Passive spotting can no longer walk a unit up the whole ladder: repeated looks do not accumulate
        /// (§12.4.1). Equipment counts and morale come from combat and IntelActions, never from staring.
        /// </summary>
        public static void RecomputeAllSpotting()
        {
            try
            {
                var gdm = GameDataManager.Instance;
                var playerUnits = gdm.GetPlayerUnits();
                var enemyUnits = gdm.GetAIUnits();

                foreach (var spotter in playerUnits)
                {
                    if (spotter.IsDestroyed()) continue;

                    foreach (var enemy in enemyUnits)
                    {
                        if (enemy.IsDestroyed()) continue;

                        int range = SpottingRangeAgainst(spotter, enemy);
                        int dist = HexMapUtil.GetHexDistance(spotter.MapPos, enemy.MapPos);
                        if (dist <= range)
                            RaiseToCeiling(enemy, PassiveContactCeiling(spotter, dist));
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RecomputeAllSpotting), e);
            }
        }

        /// <summary>
        /// Incremental spotting: checks enemies within the mover's spotting range at newPos.
        /// Returns list of enemies that transitioned from Level0 (newly visible).
        /// Called per hex step during movement.
        /// </summary>
        public static List<CombatUnit> CheckSpottingForMover(CombatUnit mover, Position2D newPos)
        {
            var newlySpotted = new List<CombatUnit>();
            try
            {
                var enemies = GameDataManager.Instance.GetAIUnits();
                foreach (var enemy in enemies)
                {
                    if (enemy.IsDestroyed()) continue;

                    int range = SpottingRangeAgainst(mover, enemy);
                    int dist = HexMapUtil.GetHexDistance(newPos, enemy.MapPos);
                    if (dist <= range)
                    {
                        var oldLevel = enemy.SpottedLevel;
                        RaiseToCeiling(enemy, PassiveContactCeiling(mover, dist));
                        if (oldLevel == SpottedLevel.Level0)
                            newlySpotted.Add(enemy);
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CheckSpottingForMover), e);
            }
            return newlySpotted;
        }

        /// <summary>
        /// Reverse spotting: checks all player spotters against a moved enemy unit.
        /// Used for AI-turn spotting.
        /// </summary>
        public static void CheckSpottingByStationary(CombatUnit movedEnemy)
        {
            try
            {
                if (movedEnemy == null || movedEnemy.IsDestroyed()) return;

                var playerUnits = GameDataManager.Instance.GetPlayerUnits();
                foreach (var spotter in playerUnits)
                {
                    if (spotter.IsDestroyed()) continue;

                    int range = SpottingRangeAgainst(spotter, movedEnemy);
                    int dist = HexMapUtil.GetHexDistance(spotter.MapPos, movedEnemy.MapPos);
                    if (dist <= range)
                        RaiseToCeiling(movedEnemy, PassiveContactCeiling(spotter, dist));
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CheckSpottingByStationary), e);
            }
        }

        /// <summary>
        /// Spotting decay (§3.3.4 / §12.6, REWRITTEN 2026-07-24) — runs once per side at Refresh, one step
        /// per turn. GRADUATED, not a collapse:
        ///
        ///  1. Re-derive the enemy's SUSTAINED FLOOR from the board (§12.6.2). Passive contact never decays.
        ///  2. At or below the floor: hold (and top up to the floor if something has pushed it lower).
        ///  3. Above the floor but ADJACENT to a friendly unit: hold (§12.6.3 — contact preserves intel).
        ///  4. Otherwise: drop exactly one rung, never below the floor, all the way down to Level0.
        ///
        /// The old model dropped anything at Level2+ straight to Level1, which on a six-rung ladder would
        /// erase three IntelActions of investment in a single Refresh. Decay must still be able to reach
        /// Level0 or units would stop disappearing and fog of war (§12.8) would break.
        /// </summary>
        public static void ProcessSpottingDecay()
        {
            try
            {
                var enemies = GameDataManager.Instance.GetAIUnits();
                foreach (var enemy in enemies)
                {
                    if (enemy.IsDestroyed()) continue;

                    var floor = SustainedFloor(enemy);
                    var current = enemy.SpottedLevel;

                    if (current <= floor)
                    {
                        RaiseToCeiling(enemy, floor);   // no-op unless passive contact outranks the current level
                        continue;
                    }

                    if (IsAdjacentToPlayerUnit(enemy)) continue;

                    var newLevel = current - 1;
                    if (newLevel < floor) newLevel = floor;

                    ApplyLevel(enemy, newLevel);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ProcessSpottingDecay), e);
            }
        }

        /// <summary>
        /// Ground IntelAction application (§12.4.5): every ENEMY unit ADJACENT to <paramref name="actor"/>
        /// gains one rung, ceiling Level 5. Adjacency is required — there is no intel range — and all adjacent
        /// enemies are affected rather than a picked target.
        ///
        /// This is the ONLY route to Level 5, and the deliberate alternative to attacking: three IntelActions
        /// walk a unit from contact to a full picture without firing a shot, where combat buys Level 4 in one
        /// action but starts a fight. The caller is responsible for spending the action itself
        /// (CombatUnit.PerformIntelAction) — this method only applies the intel result.
        /// </summary>
        public static void ApplyGroundIntelAction(CombatUnit actor)
        {
            try
            {
                if (actor == null || actor.IsDestroyed()) return;

                var gdm = GameDataManager.Instance;
                foreach (var neighborPos in HexMapUtil.GetAllNeighborPositions(actor.MapPos))
                {
                    var target = gdm.GetGroundUnitAtHex(neighborPos);
                    if (target == null || target.IsDestroyed()) continue;
                    if (target.Side == actor.Side) continue;

                    RaiseByOneRung(target, SpottedLevel.Level5);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ApplyGroundIntelAction), e);
            }
        }

        /// <summary>
        /// Direct-combat intel (§12.4.6): both participants are set to Level 4 against each other — you have
        /// been close enough to count what the other side is fighting with. Applied to the ENEMY side only,
        /// since CombatUnit.SpottedLevel is the player's view of AI units; the AI's reciprocal knowledge lives
        /// in its belief store.
        ///
        /// Scope note: this is DIRECT combat only. Indirect fire has its own explicit reveal rule for the
        /// FIRER (§12.4.9.2, +1 rung minimum Level 1); the doc says nothing about what an indirect firer
        /// learns about its target, so nothing is applied there rather than inventing a rule.
        /// </summary>
        public static void ApplyDirectCombatContact(CombatUnit attacker, CombatUnit defender)
        {
            try
            {
                if (attacker == null || defender == null) return;

                if (attacker.Side != Side.Player) RaiseToCeiling(attacker, SpottedLevel.Level4);
                if (defender.Side != Side.Player) RaiseToCeiling(defender, SpottedLevel.Level4);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ApplyDirectCombatContact), e);
            }
        }

        /// <summary>
        /// Checks if an enemy unit is currently within spotting range of any player unit.
        /// </summary>
        public static bool IsCurrentlySpotted(CombatUnit enemy)
        {
            try
            {
                var playerUnits = GameDataManager.Instance.GetPlayerUnits();
                foreach (var spotter in playerUnits)
                {
                    if (spotter.IsDestroyed()) continue;

                    int range = SpottingRangeAgainst(spotter, enemy);
                    int dist = HexMapUtil.GetHexDistance(spotter.MapPos, enemy.MapPos);
                    if (dist <= range) return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsCurrentlySpotted), e);
                return false;
            }
        }

        #endregion // Core Spotting

        #region AI-Side Perception (AI2b — symmetric sweep, AI-Design-Supplement Part 3)

        /// <summary>
        /// The AI-side mirror of <see cref="RecomputeAllSpotting"/>: every AI spotter checks every player
        /// unit under the same dual-domain ranges (§12.3), but hits feed the AI's BELIEF STORE — never
        /// CombatUnit.SpottedLevel, which remains the player's view of AI units. Run at AI_Refresh
        /// (§3.3.4 "per side"). Camouflage (§14.9.4) applies symmetrically via SpottingRangeAgainst.
        /// </summary>
        public static void RecomputeAIPerception(Models.AI.AIPerceptionState perception, int currentTurn)
        {
            try
            {
                if (perception == null) return;

                var gdm = GameDataManager.Instance;
                foreach (var spotter in gdm.GetAIUnits())
                {
                    if (spotter.IsDestroyed()) continue;

                    foreach (var target in gdm.GetPlayerUnits())
                    {
                        if (target.IsDestroyed()) continue;

                        int range = SpottingRangeAgainst(spotter, target);
                        int dist = HexMapUtil.GetHexDistance(spotter.MapPos, target.MapPos);
                        if (dist <= range)
                        {
                            // Same source-ceiling rule the player side uses (§12.4.2) — the AI does not get
                            // a different progression model, only a different store.
                            perception.RecordSpot(
                                target.UnitID, target.MapPos, currentTurn,
                                target.Classification,
                                ObservedHpPercent(target),
                                Mathf.Max(1, Mathf.RoundToInt(target.MovementPoints.Max)),
                                PassiveContactCeiling(spotter, dist));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RecomputeAIPerception), e);
            }
        }

        /// <summary>
        /// The AI-side mirror of <see cref="ProcessSpottingDecay"/> (§12.6): player units currently inside
        /// some AI spotter's range hold their contact level; the rest decay one step in the belief store
        /// (ghosting at Level0). Run at AI_Refresh, after <see cref="RecomputeAIPerception"/>.
        /// </summary>
        public static void StepAIPerceptionDecay(Models.AI.AIPerceptionState perception, int currentTurn)
        {
            try
            {
                if (perception == null) return;

                var gdm = GameDataManager.Instance;
                var floors = new Dictionary<string, SpottedLevel>();
                var adjacent = new HashSet<string>();

                foreach (var target in gdm.GetPlayerUnits())
                {
                    if (target.IsDestroyed()) continue;

                    foreach (var spotter in gdm.GetAIUnits())
                    {
                        if (spotter.IsDestroyed()) continue;

                        int dist = HexMapUtil.GetHexDistance(spotter.MapPos, target.MapPos);
                        if (dist <= 1) adjacent.Add(target.UnitID);

                        int range = SpottingRangeAgainst(spotter, target);
                        if (dist > range) continue;

                        // The AI's SUSTAINED FLOOR, built from the same PassiveContactCeiling the player side
                        // uses (§12.6.2). It must be the best ceiling across ALL spotters, so this cannot
                        // break early on the first in-range hit the way a boolean in-range test could.
                        var ceiling = PassiveContactCeiling(spotter, dist);
                        if (!floors.TryGetValue(target.UnitID, out var best) || ceiling > best)
                            floors[target.UnitID] = ceiling;
                    }
                }

                perception.StepDecay(currentTurn, floors, adjacent);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(StepAIPerceptionDecay), e);
            }
        }

        private static int ObservedHpPercent(CombatUnit unit)
        {
            float max = unit.HitPoints.Max;
            return max <= 0f ? 0 : Mathf.RoundToInt(unit.HitPoints.Current / max * 100f);
        }

        #endregion // AI-Side Perception

        #region Ambush Detection

        /// <summary>
        /// Checks for ground ambush: returns the unspotted enemy whose ZoC the mover entered, or null.
        /// </summary>
        public static CombatUnit CheckGroundAmbush(CombatUnit mover, Position2D newPos)
        {
            try
            {
                var gdm = GameDataManager.Instance;
                var neighbors = HexMapUtil.GetAllNeighborPositions(newPos);

                foreach (var neighborPos in neighbors)
                {
                    var ground = gdm.GetGroundUnitAtHex(neighborPos);
                    if (ground == null) continue;
                    if (ground.Side == mover.Side) continue;
                    if (ground.SpottedLevel != SpottedLevel.Level0) continue;
                    if (!ground.ProjectsZoC) continue;

                    return ground; // ambusher found
                }

                return null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CheckGroundAmbush), e);
                return null;
            }
        }

        /// <summary>
        /// Checks for air ambush: unspotted SAM/SPSAM/AAA/SPAAA within engagement range.
        /// Rolls 1d6 vs detection table keyed by air unit's ExperienceLevel.
        /// </summary>
        public static AirAmbushResult CheckAirAmbush(CombatUnit mover, Position2D newPos)
        {
            try
            {
                var gdm = GameDataManager.Instance;
                var enemies = gdm.GetAIUnits();

                foreach (var enemy in enemies)
                {
                    if (enemy.IsDestroyed()) continue;
                    if (enemy.SpottedLevel != SpottedLevel.Level0) continue;

                    bool isAA = enemy.Classification == UnitClassification.SAM
                             || enemy.Classification == UnitClassification.SPSAM
                             || enemy.Classification == UnitClassification.AAA
                             || enemy.Classification == UnitClassification.SPAAA;
                    if (!isAA) continue;

                    int engagementRange = Mathf.FloorToInt(enemy.ActivePrimaryRange);
                    if (engagementRange <= 0) engagementRange = 2; // fallback

                    int dist = HexMapUtil.GetHexDistance(newPos, enemy.MapPos);
                    if (dist > engagementRange) continue;

                    // Detection roll (§6.10.3/.4) — delegated to the pure, seedable AirAmbushCheck.
                    if (AirAmbushCheck.RollDetection(mover.ExperienceLevel, new CombatRandom()))
                    {
                        // Detection success, no shot fired: the AA unit is revealed at Level1 (§12.4.8).
                        // (The FAILED branch — ambusher fires — reveals at Level4 per §12.4.9.1; that
                        // belongs to the AD-fire wiring, which is M13 caller work.)
                        RaiseToCeiling(enemy, SpottedLevel.Level1);

                        if (EventManager.Instance != null)
                            EventManager.Instance.RaiseAirAmbushDetected(enemy, mover);

                        return AirAmbushResult.Detected;
                    }

                    // Ambush: detection failed
                    return AirAmbushResult.Ambushed;
                }

                return AirAmbushResult.NoThreat;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CheckAirAmbush), e);
                return AirAmbushResult.NoThreat;
            }
        }

        #endregion // Ambush Detection

        #region Private Helpers

        /// <summary>
        /// The spotting range <paramref name="spotter"/> uses against <paramref name="target"/> under the
        /// dual-domain rule (§12.3): the spotter's AIR range vs an airborne target (any fixed-wing, or an
        /// AM/MAM air-assault lift in EmbarkedHelo state), its GROUND range otherwise. Classification-driven
        /// and decoupled from the profile SpottingRange (which is UI-only now). Attack helos fly NOE and are
        /// ground targets; air-defence platforms' long ranges are air-search only (a SAM reveals ground at 2).
        /// Superior Camouflage (§14.9.4) shortens the range against a led target — applied here, at the
        /// §12.3.10 comparison, so it affects the sweep, per-hex checks, and decay uniformly.
        /// </summary>
        private static int SpottingRangeAgainst(CombatUnit spotter, CombatUnit target)
        {
            int range = target.IsAirborneSpottingTarget
                ? spotter.ActiveAirSpottingRange
                : spotter.ActiveGroundSpottingRange;

            return Math.Max(0, range - target.EnemySpottingRangeReduction);
        }

        /// <summary>
        /// The PASSIVE-CONTACT ceiling a spotter earns against a target (§12.4.2): Level 3 if the spotter is a
        /// RECON-class unit standing adjacent, Level 2 if any spotter is adjacent, Level 1 at range. Range only
        /// establishes CONTACT — how much that contact is worth is decided here, which is what stops a unit
        /// from learning everything by standing still and staring (§12.4.4).
        /// </summary>
        private static SpottedLevel PassiveContactCeiling(CombatUnit spotter, int distance)
        {
            if (distance > 1) return SpottedLevel.Level1;

            return spotter.Classification == UnitClassification.RECON
                ? SpottedLevel.Level3
                : SpottedLevel.Level2;
        }

        /// <summary>
        /// "Set to" source semantics (§12.4.3): raises the target UP TO <paramref name="ceiling"/> and never
        /// lowers an already-higher level. Used by passive spotting, combat, and the ambush reveals.
        /// </summary>
        private static void RaiseToCeiling(CombatUnit unit, SpottedLevel ceiling)
        {
            if (unit.SpottedLevel >= ceiling) return;
            ApplyLevel(unit, ceiling);
        }

        /// <summary>
        /// "+1 rung" source semantics (§12.4.3): adds exactly one level and stops at <paramref name="ceiling"/>.
        /// Used by IntelActions, the SIGINT sweep, and air recon.
        /// </summary>
        private static void RaiseByOneRung(CombatUnit unit, SpottedLevel ceiling)
        {
            if (unit.SpottedLevel >= ceiling) return;
            ApplyLevel(unit, unit.SpottedLevel + 1);
        }

        /// <summary>
        /// Commits a level change and raises the change event. SetSpottedLevel may clamp below the requested
        /// value (Concealed Operations Base, §14.8.7), so the event always reports the level that actually
        /// landed rather than the one asked for.
        /// </summary>
        private static void ApplyLevel(CombatUnit unit, SpottedLevel level)
        {
            var oldLevel = unit.SpottedLevel;
            unit.SetSpottedLevel(level);

            if (unit.SpottedLevel == oldLevel) return;

            if (EventManager.Instance != null)
                EventManager.Instance.RaiseUnitSpottedLevelChanged(unit, oldLevel, unit.SpottedLevel);
        }

        /// <summary>True when any live player unit stands adjacent to <paramref name="enemy"/> (§12.6.3 hold).</summary>
        private static bool IsAdjacentToPlayerUnit(CombatUnit enemy)
        {
            foreach (var spotter in GameDataManager.Instance.GetPlayerUnits())
            {
                if (spotter.IsDestroyed()) continue;
                if (HexMapUtil.GetHexDistance(spotter.MapPos, enemy.MapPos) <= 1) return true;
            }
            return false;
        }

        /// <summary>
        /// The SUSTAINED FLOOR (§12.6.2): the best passive ceiling any live player spotter currently earns
        /// against this enemy. Re-derived from the board every Refresh, and never decays — decay only eats the
        /// rungs that were bought with combat, IntelActions, or a sweep.
        /// </summary>
        private static SpottedLevel SustainedFloor(CombatUnit enemy)
        {
            var floor = SpottedLevel.Level0;

            foreach (var spotter in GameDataManager.Instance.GetPlayerUnits())
            {
                if (spotter.IsDestroyed()) continue;

                int range = SpottingRangeAgainst(spotter, enemy);
                int dist = HexMapUtil.GetHexDistance(spotter.MapPos, enemy.MapPos);
                if (dist > range) continue;

                var ceiling = PassiveContactCeiling(spotter, dist);
                if (ceiling > floor) floor = ceiling;
            }

            return floor;
        }

        #endregion // Private Helpers
    }
}
