using HammerAndSickle.Controllers;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HammerAndSickle.Persistence
{
    /// <summary>
    /// Maps the live <see cref="GameDataManager"/> runtime state to a flat <see cref="GameStateSnapshot"/>
    /// object (and back again).  This is the *only* place that translation logic should live so that
    /// the snapshot format remains stable and version‑controlled.
    /// </summary>
    public static class SnapshotMapper
    {
        #region Constants

        private const string CLASS_NAME = nameof(SnapshotMapper);
        private const int CURRENT_SAVE_VERSION = CUConstants.SAVE_VERSION;

        #endregion // Constants

        #region Public API

        /// <summary>
        /// Creates a complete snapshot of the current game state for persistence by generating
        /// independent copies of all game entities and their current state.
        /// </summary>
        public static GameStateSnapshot ToSnapshot(GameDataManager mgr)
        {
            const string METHOD_NAME = nameof(ToSnapshot);

            try
            {
                if (mgr == null)
                    throw new ArgumentNullException(nameof(mgr), "GameDataManager cannot be null");

                // Create the snapshot with current game state
                var snapshot = new GameStateSnapshot
                {
                    Campaign = mgr.CurrentCampaignData,
                    Scenario = mgr.CurrentScenarioData,
                    SaveVersion = CURRENT_SAVE_VERSION,
                    Units = new Dictionary<string, CombatUnit>(StringComparer.Ordinal),
                    Leaders = new Dictionary<string, Leader>(StringComparer.Ordinal)
                };

                // Create fresh units with same parameters but independent state
                foreach (var unit in mgr.GetAllCombatUnits())
                {
                    if (unit != null)
                    {
                        try
                        {
                            // Create fresh unit with same template parameters
                            var freshUnit = new CombatUnit(
                                unitName: unit.UnitName,
                                classification: unit.Classification,
                                role: unit.Role,
                                side: unit.Side,
                                nationality: unit.Nationality,
                                intelProfileType: unit.IntelProfileType,
                                deployedProfileID: unit.DeployedProfileID,
                                isMountable: unit.IsMountable,
                                mobileProfileID: unit.MobileProfileID,
                                isEmbarkable: unit.IsEmbarkable,
                                embarkProfileID: unit.EmbarkedProfileID,
                                category: unit.IsBase ? unit.DepotCategory : DepotCategory.Secondary,
                                size: unit.IsBase ? unit.DepotSize : DepotSize.Small
                            );

                            // CRITICAL FIX: Preserve the original unit ID to maintain leader references
                            freshUnit.SetUnitID(unit.UnitID);

                            // Copy essential current state
                            freshUnit.HitPoints.SetCurrent(unit.HitPoints.Current);
                            freshUnit.DaysSupply.SetCurrent(unit.DaysSupply.Current);
                            freshUnit.MovementPoints.SetCurrent(unit.MovementPoints.Current);

                            // Copy action states
                            freshUnit.MoveActions.SetCurrent(unit.MoveActions.Current);
                            freshUnit.CombatActions.SetCurrent(unit.CombatActions.Current);
                            freshUnit.DeploymentActions.SetCurrent(unit.DeploymentActions.Current);
                            freshUnit.OpportunityActions.SetCurrent(unit.OpportunityActions.Current);
                            freshUnit.IntelActions.SetCurrent(unit.IntelActions.Current);

                            // Copy deployment and experience state
                            freshUnit.SetDeploymentPosition(unit.DeploymentPosition);
                            freshUnit.ExperienceLevel = unit.ExperienceLevel;
                            freshUnit.ExperiencePoints = unit.ExperiencePoints;
                            freshUnit.SetEfficiencyLevel(unit.EfficiencyLevel);

                            // Copy position and spotted state
                            freshUnit.SetPosition(unit.MapPos);
                            freshUnit.SetSpottedLevel(unit.SpottedLevel);
                            freshUnit.SetICM(unit.IndividualCombatModifier);

                            // Copy leader assignment (just the ID string, not the object)
                            freshUnit.LeaderID = unit.LeaderID;

                            // Copy facility state if applicable
                            if (unit.IsBase)
                            {
                                freshUnit.SetFacilityDamage(unit.BaseDamage);
                                if (unit.FacilityType == FacilityType.SupplyDepot)
                                {
                                    freshUnit.DaysSupply.SetCurrent(CUConstants.MaxDaysSupplyDepot);
                                }

                                // Copy attached air unit IDs for airbases
                                if (unit.FacilityType == FacilityType.Airbase)
                                {
                                    var attachedUnits = unit.AirUnitsAttached;
                                    if (attachedUnits?.Count > 0)
                                    {
                                        // Use reflection to populate the _attachedUnitIDs field
                                        var attachedIdsField = typeof(CombatUnit).GetField("_attachedUnitIDs",
                                            BindingFlags.NonPublic | BindingFlags.Instance);

                                        if (attachedIdsField != null)
                                        {
                                            var freshAttachedIds = (List<string>)attachedIdsField.GetValue(freshUnit);
                                            freshAttachedIds.Clear();

                                            foreach (var attachedUnit in attachedUnits)
                                            {
                                                if (attachedUnit != null && !string.IsNullOrEmpty(attachedUnit.UnitID))
                                                {
                                                    freshAttachedIds.Add(attachedUnit.UnitID);
                                                }
                                            }

                                            AppService.CaptureUiMessage($"Preserved {freshAttachedIds.Count} air unit attachments for {freshUnit.UnitName}");
                                        }
                                    }
                                }
                            }

                            // Finally, add to snapshot dictionary
                            snapshot.Units[freshUnit.UnitID] = freshUnit;
                        }
                        catch (Exception ex)
                        {
                            AppService.HandleException(CLASS_NAME, METHOD_NAME,
                                new InvalidOperationException($"Failed to create snapshot copy of unit {unit.UnitName}", ex));
                            // Continue with other units rather than failing completely
                        }
                    }
                }

                // Use existing leader snapshot conversion (this part already works)
                foreach (var leader in mgr.GetAllLeaders())
                {
                    if (leader != null)
                    {
                        try
                        {
                            var leaderData = leader.ToSnapshot();
                            var leaderCopy = LeaderSnapshotExtensions.FromSnapshot(leaderData);
                            snapshot.Leaders[leaderCopy.LeaderID] = leaderCopy;
                        }
                        catch (Exception ex)
                        {
                            AppService.HandleException(CLASS_NAME, METHOD_NAME,
                                new InvalidOperationException($"Failed to create snapshot copy of leader {leader.Name}", ex));
                            // Continue with other leaders rather than failing completely
                        }
                    }
                }

                AppService.CaptureUiMessage($"Snapshot created with {snapshot.Units.Count} units and {snapshot.Leaders.Count} leaders");
                return snapshot;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, METHOD_NAME, ex);
                throw;
            }
        }

        /// <summary>
        /// Wipes <paramref name="mgr"/> and re‑hydrates it from <paramref name="snap"/>.
        /// A second pass restores object links that cannot be represented by IDs alone.
        /// </summary>
        /// <param name="snap">The snapshot to restore from</param>
        /// <param name="mgr">The GameDataManager to populate</param>
        /// <exception cref="ArgumentNullException">Thrown when snap or mgr is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when save version is incompatible</exception>
        public static void ApplySnapshot(GameStateSnapshot snap, GameDataManager mgr)
        {
            const string METHOD_NAME = nameof(ApplySnapshot);

            try
            {
                // Validate inputs
                if (snap == null)
                    throw new ArgumentNullException(nameof(snap), "GameStateSnapshot cannot be null");
                if (mgr == null)
                    throw new ArgumentNullException(nameof(mgr), "GameDataManager cannot be null");

                // Version compatibility check
                if (snap.SaveVersion > CURRENT_SAVE_VERSION)
                {
                    throw new InvalidOperationException(
                        $"Save file version {snap.SaveVersion} is newer than supported version {CURRENT_SAVE_VERSION}. " +
                        "Please update the application to load this save file.");
                }

                // If the save version is lower than the current version, we need to upgrade it
                if (snap.SaveVersion < CURRENT_SAVE_VERSION)
                {
                    AppService.CaptureUiMessage($"Upgrading save file from version {snap.SaveVersion} to {CURRENT_SAVE_VERSION}");
                    snap = UpgradeSnapshot(snap);
                }

                // Step 1: Complete wipe of current runtime state
                AppService.CaptureUiMessage("Clearing current game state...");
                mgr.ClearAll();

                // Step 2: Restore high-level objects
                AppService.CaptureUiMessage("Restoring campaign and scenario data...");
                mgr.CurrentCampaignData = snap.Campaign ?? new CampaignData();
                mgr.CurrentScenarioData = snap.Scenario; // May remain null for campaign-only saves

                // Step 3: Re-populate entity dictionaries using manager's registration methods
                // This ensures all internal invariants and validation logic is preserved
                AppService.CaptureUiMessage($"Registering {snap.Units?.Count ?? 0} combat units...");
                if (snap.Units != null)
                {
                    foreach (var unit in snap.Units.Values)
                    {
                        if (unit == null)
                        {
                            AppService.HandleException(CLASS_NAME, METHOD_NAME,
                                new InvalidOperationException("Null unit found in snapshot"));
                            continue; // Skip null units but continue processing
                        }

                        if (!mgr.RegisterCombatUnit(unit))
                        {
                            AppService.HandleException(CLASS_NAME, METHOD_NAME,
                                new InvalidOperationException($"Failed to register unit {unit.UnitID}"));
                        }
                    }
                }

                AppService.CaptureUiMessage($"Registering {snap.Leaders?.Count ?? 0} leaders...");
                if (snap.Leaders != null)
                {
                    foreach (var leader in snap.Leaders.Values)
                    {
                        if (leader == null)
                        {
                            AppService.HandleException(CLASS_NAME, METHOD_NAME,
                                new InvalidOperationException("Null leader found in snapshot"));
                            continue; // Skip null leaders but continue processing
                        }

                        if (!mgr.RegisterLeader(leader))
                        {
                            AppService.HandleException(CLASS_NAME, METHOD_NAME,
                                new InvalidOperationException($"Failed to register leader {leader.LeaderID}"));
                        }
                    }
                }

                // Step 4: Rebuild all transient caches and cross-references
                // This handles Leader↔Unit bidirectional links, air unit attachments, etc.
                AppService.CaptureUiMessage("Rebuilding transient caches and cross-references...");
                mgr.RebuildTransientCaches();

                // Step 5: Validate the loaded state
                ValidateLoadedState(mgr);

                AppService.CaptureUiMessage("Game state successfully restored from snapshot");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, METHOD_NAME, ex);
                throw; // Re-throw since load failures are critical
            }
        }

        #endregion // Public API

        #region Private Validation

        /// <summary>
        /// Performs basic validation on the loaded game state to catch obvious corruption.
        /// </summary>
        /// <param name="mgr">The GameDataManager to validate</param>
        private static void ValidateLoadedState(GameDataManager mgr)
        {
            const string METHOD_NAME = nameof(ValidateLoadedState);

            try
            {
                // Basic sanity checks
                var allUnits = mgr.GetAllCombatUnits();
                var allLeaders = mgr.GetAllLeaders();

                // Check for duplicate IDs (shouldn't happen with proper registration, but worth checking)
                var unitIds = allUnits.Select(u => u.UnitID).ToList();
                var leaderIds = allLeaders.Select(l => l.LeaderID).ToList();

                if (unitIds.Count != unitIds.Distinct().Count())
                {
                    throw new InvalidOperationException("Duplicate unit IDs detected after loading");
                }

                if (leaderIds.Count != leaderIds.Distinct().Count())
                {
                    throw new InvalidOperationException("Duplicate leader IDs detected after loading");
                }

                // Check Leader↔Unit assignment consistency
                foreach (var leader in allLeaders.Where(l => l.IsAssigned))
                {
                    if (string.IsNullOrEmpty(leader.UnitID))
                    {
                        AppService.HandleException(CLASS_NAME, METHOD_NAME,
                            new InvalidOperationException($"Leader {leader.LeaderID} marked as assigned but has no UnitID"));
                        continue;
                    }

                    var assignedUnit = mgr.GetCombatUnit(leader.UnitID);
                    if (assignedUnit == null)
                    {
                        AppService.HandleException(CLASS_NAME, METHOD_NAME,
                            new InvalidOperationException($"Leader {leader.LeaderID} assigned to non-existent unit {leader.UnitID}"));
                    }
                    else if (assignedUnit.LeaderID != leader.LeaderID)
                    {
                        AppService.HandleException(CLASS_NAME, METHOD_NAME,
                            new InvalidOperationException($"Leader-Unit assignment mismatch: Leader {leader.LeaderID} thinks it's assigned to {assignedUnit.UnitName}, but unit thinks its leader is {assignedUnit.LeaderID}"));
                    }
                }

                // Check for units with invalid leader references
                foreach (var unit in allUnits.Where(u => !string.IsNullOrEmpty(u.LeaderID)))
                {
                    var assignedLeader = mgr.GetLeader(unit.LeaderID);
                    if (assignedLeader == null)
                    {
                        AppService.HandleException(CLASS_NAME, METHOD_NAME,
                            new InvalidOperationException($"Unit {unit.UnitID} references non-existent leader {unit.LeaderID}"));
                    }
                    else if (!assignedLeader.IsAssigned || assignedLeader.UnitID != unit.UnitID)
                    {
                        AppService.HandleException(CLASS_NAME, METHOD_NAME,
                            new InvalidOperationException($"Unit-Leader assignment mismatch: Unit {unit.UnitID} thinks its leader is {unit.LeaderID}, but leader is not properly assigned back"));
                    }
                }

                AppService.CaptureUiMessage($"State validation completed: {allUnits.Count} units, {allLeaders.Count} leaders");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, METHOD_NAME, ex);
                // Don't re-throw validation errors - log them but allow the load to complete
            }
        }

        #endregion // Private Validation

        #region Private Upgrade Logic

        /// <summary>
        /// Upgrades an old snapshot to the current save format.
        /// </summary>
        /// <param name="oldSnap"></param>
        /// <returns></returns>
        private static GameStateSnapshot UpgradeSnapshot(GameStateSnapshot oldSnap)
        {
            // TODO: Implement migration logic when save format changes
            // For now, just update the version number
            oldSnap.SaveVersion = CURRENT_SAVE_VERSION;
            return oldSnap;
        }

        #endregion
    }
}