using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
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
                    int range = Mathf.FloorToInt(spotter.ActiveSpottingRange);
                    if (range <= 0) continue;

                    foreach (var enemy in enemyUnits)
                    {
                        if (enemy.IsDestroyed()) continue;
                        if (enemy.SpottedLevel == SpottedLevel.Level4) continue;

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
                int range = Mathf.FloorToInt(mover.ActiveSpottingRange);
                if (range <= 0) return newlySpotted;

                var enemies = GameDataManager.Instance.GetAIUnits();
                foreach (var enemy in enemies)
                {
                    if (enemy.IsDestroyed()) continue;
                    if (enemy.SpottedLevel == SpottedLevel.Level4) continue;

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
                    int range = Mathf.FloorToInt(spotter.ActiveSpottingRange);
                    if (range <= 0) continue;

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
        /// Admin phase decay: enemies not in range of any player spotter decay toward Level0.
        /// Level2+ drops to Level1, Level1 drops to Level0. One step per admin phase.
        /// </summary>
        public static void ProcessAdminPhaseDecay()
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
                AppService.HandleException(CLASS_NAME, nameof(ProcessAdminPhaseDecay), e);
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
                    int range = Mathf.FloorToInt(spotter.ActiveSpottingRange);
                    if (range <= 0) continue;

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

                    // Roll detection check
                    int roll = UnityEngine.Random.Range(1, 7); // 1d6
                    int detectThreshold = GetAirDetectionThreshold(mover.ExperienceLevel);

                    if (roll >= detectThreshold)
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

        private static void IncrementSpottedLevel(CombatUnit unit)
        {
            if (unit.SpottedLevel >= SpottedLevel.Level4) return;

            var oldLevel = unit.SpottedLevel;
            var newLevel = oldLevel + 1;
            unit.SetSpottedLevel(newLevel);

            if (EventManager.Instance != null)
                EventManager.Instance.RaiseUnitSpottedLevelChanged(unit, oldLevel, newLevel);
        }

        /// <summary>
        /// Returns the minimum d6 roll needed to detect an air ambush, keyed by experience.
        /// </summary>
        private static int GetAirDetectionThreshold(ExperienceLevel level) => level switch
        {
            ExperienceLevel.Raw => 6,
            ExperienceLevel.Green => 5,
            ExperienceLevel.Trained => 4,
            ExperienceLevel.Experienced => 3,
            ExperienceLevel.Veteran => 2,
            ExperienceLevel.Elite => 1,
            _ => 6
        };

        #endregion // Private Helpers
    }
}
