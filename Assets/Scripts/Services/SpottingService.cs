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
        /// Full spotting pass: each player spotter checks all enemy units within range.
        /// Each spotter-target pair in range generates one spotting hit.
        /// Called at turn start.
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
                        if (enemy.SpottedLevel == SpottedLevel.Level4) continue;

                        int range = SpottingRangeAgainst(spotter, enemy);
                        int dist = HexMapUtil.GetHexDistance(spotter.MapPos, enemy.MapPos);
                        if (dist <= range)
                            IncrementSpottedLevel(enemy);
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
                    if (enemy.SpottedLevel == SpottedLevel.Level4) continue;

                    int range = SpottingRangeAgainst(mover, enemy);
                    int dist = HexMapUtil.GetHexDistance(newPos, enemy.MapPos);
                    if (dist <= range)
                    {
                        var oldLevel = enemy.SpottedLevel;
                        IncrementSpottedLevel(enemy);
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
                if (movedEnemy.SpottedLevel == SpottedLevel.Level4) return;

                var playerUnits = GameDataManager.Instance.GetPlayerUnits();
                foreach (var spotter in playerUnits)
                {
                    if (spotter.IsDestroyed()) continue;

                    int range = SpottingRangeAgainst(spotter, movedEnemy);
                    int dist = HexMapUtil.GetHexDistance(spotter.MapPos, movedEnemy.MapPos);
                    if (dist <= range)
                        IncrementSpottedLevel(movedEnemy);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CheckSpottingByStationary), e);
            }
        }

        /// <summary>
        /// Spotting decay (§3.3.4 / §12.6) — runs once per side at Refresh. Enemies not in range
        /// of any player spotter decay toward Level0: Level2+ drops to Level1, Level1 drops to
        /// Level0. One step per turn. (Renamed from ProcessAdminPhaseDecay: the AdminPhase is gone
        /// under the ratified §3.2 BattlePhase set; decay now lives in the Refresh phase.)
        /// </summary>
        public static void ProcessSpottingDecay()
        {
            try
            {
                var enemies = GameDataManager.Instance.GetAIUnits();
                foreach (var enemy in enemies)
                {
                    if (enemy.IsDestroyed()) continue;
                    if (enemy.SpottedLevel == SpottedLevel.Level0) continue;

                    if (IsCurrentlySpotted(enemy)) continue;

                    var oldLevel = enemy.SpottedLevel;
                    SpottedLevel newLevel;

                    if (oldLevel >= SpottedLevel.Level2)
                        newLevel = SpottedLevel.Level1;
                    else
                        newLevel = SpottedLevel.Level0;

                    enemy.SetSpottedLevel(newLevel);

                    if (EventManager.Instance != null)
                        EventManager.Instance.RaiseUnitSpottedLevelChanged(enemy, oldLevel, newLevel);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ProcessSpottingDecay), e);
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
                        // Detection success: enemy spotted at Level1
                        IncrementSpottedLevel(enemy);

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
        /// </summary>
        private static int SpottingRangeAgainst(CombatUnit spotter, CombatUnit target) =>
            target.IsAirborneSpottingTarget
                ? spotter.ActiveAirSpottingRange
                : spotter.ActiveGroundSpottingRange;

        private static void IncrementSpottedLevel(CombatUnit unit)
        {
            if (unit.SpottedLevel >= SpottedLevel.Level4) return;

            var oldLevel = unit.SpottedLevel;
            var newLevel = oldLevel + 1;
            unit.SetSpottedLevel(newLevel);

            if (EventManager.Instance != null)
                EventManager.Instance.RaiseUnitSpottedLevelChanged(unit, oldLevel, newLevel);
        }

        #endregion // Private Helpers
    }
}
