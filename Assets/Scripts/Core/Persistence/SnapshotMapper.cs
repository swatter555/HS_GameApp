using HammerAndSickle.Controllers;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HammerAndSickle.Core.GameData;

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
        private const int CURRENT_SAVE_VERSION = GameData.SAVE_VERSION;

        /// <summary>
        /// Oldest save version this build can migrate FROM. Anything older is refused outright rather
        /// than guessed at. Set to the 1.0 shipping baseline per Bob's clean-break ruling (2026-07-27):
        /// pre-release dev saves are NOT supported, which is what keeps the string-enum change of that
        /// date free of a migration step.
        /// ⚠ Raise this ONLY when dropping support for a version that shipped to players.
        /// </summary>
        private const int MINIMUM_SUPPORTED_SAVE_VERSION = GameData.SAVE_VERSION;

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
                    Header = BuildHeader(mgr),
                    Campaign = mgr.CurrentCampaignData,
                    Scenario = mgr.CurrentScenarioData,
                    SaveVersion = CURRENT_SAVE_VERSION,
                    Units = new Dictionary<string, CombatUnit>(StringComparer.Ordinal),
                    Leaders = new Dictionary<string, Leader>(StringComparer.Ordinal),
                    MapData = null // Will be set below if map is loaded
                };

                // Capture map data if a map is currently loaded
                if (GameDataManager.CurrentHexMap != null)
                {
                    try
                    {
                        // Generate checksum for map integrity (simple hash based on map state)
                        var checksum = $"{GameDataManager.CurrentHexMap.MapName}_{GameDataManager.CurrentHexMap.Configuration}_{GameDataManager.CurrentHexMap.HexCount}_{DateTime.UtcNow:yyyyMMddHHmmss}";

                        /* ⚠ THE SAVE MUST CARRY THE MAP'S DIMENSIONS (G1, 2026-08-12 — the site the change
                         * request missed). This header is the ONLY geometry the restore path below has: it
                         * has no manifest to consult. Before dimensions were written here, saving a map
                         * loaded at explicit size produced a header with `Configuration = None` (which is
                         * what the explicit HexMap constructor sets) and no columns/rows — so reloading it
                         * resolved to nothing at all, resurrecting the exact silent-geometry failure G1
                         * exists to kill, one save/load cycle later. */
                        var mapSize = GameDataManager.CurrentHexMap.MapSize;
                        var mapHeader = new JsonMapHeader(
                            GameDataManager.CurrentHexMap.MapName,
                            GameDataManager.CurrentHexMap.Configuration,
                            checksum,
                            mapSize.x,
                            mapSize.y
                        );

                        // Extract all hexes from the map (HexMap implements IEnumerable<HexTile>)
                        var hexes = GameDataManager.CurrentHexMap.ToArray();

                        // Create JsonMapData with header and hexes
                        snapshot.MapData = new JsonMapData(mapHeader, hexes);

                        AppService.CaptureUiMessage($"Captured map data: {GameDataManager.CurrentHexMap.MapName} ({hexes.Length} hexes)");
                    }
                    catch (Exception ex)
                    {
                        AppService.HandleException(CLASS_NAME, METHOD_NAME,
                            new InvalidOperationException("Failed to capture map data in snapshot", ex));
                        // Continue with snapshot creation even if map capture fails
                        snapshot.MapData = null;
                    }
                }
                else
                {
                    AppService.CaptureUiMessage("No map loaded - creating between-battle save");
                }

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
                                deployedProfile: unit.EquipmentBays.Deployed,
                                mobileProfile: unit.EquipmentBays.Mobile,
                                embarkedProfile: unit.EquipmentBays.Embarked,
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

                            // Copy facing and the naval transient state
                            freshUnit.Facing = unit.Facing;
                            freshUnit.SetNavalEmbarked(unit.IsNavalEmbarked);

                            // Copy leader assignment (just the ID string, not the object)
                            freshUnit.LeaderID = unit.LeaderID;

                            // Copy facility state if applicable
                            if (unit.IsBase)
                            {
                                freshUnit.SetFacilityDamage(unit.BaseDamage);
                                if (unit.FacilityType == FacilityType.SupplyDepot)
                                {
                                    freshUnit.DaysSupply.SetCurrent(freshUnit.DaysSupply.Max);
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
                            leaderCopy.PrepareForSerialization();
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

                // Synchronize campaign core roster with active units/leaders
                // This ensures the campaign roster reflects current battle state (casualties, experience, new purchases)
                if (snapshot.Campaign != null)
                {
                    try
                    {
                        // Clear existing core roster
                        snapshot.Campaign.PlayerUnits.Clear();
                        snapshot.Campaign.PlayerLeaders.Clear();

                        // Rebuild core unit roster from active player units
                        int coreUnitCount = 0;
                        foreach (var unit in snapshot.Units.Values)
                        {
                            if (unit != null && unit.Side == Side.Player)
                            {
                                snapshot.Campaign.PlayerUnits[unit.UnitID] = unit;
                                coreUnitCount++;
                            }
                        }

                        // Rebuild core leader roster from player leaders
                        // Leaders assigned to player units OR unassigned leaders in the player pool
                        int coreLeaderCount = 0;
                        foreach (var leader in snapshot.Leaders.Values)
                        {
                            if (leader != null)
                            {
                                // Leader is core if: assigned to player unit OR unassigned but owned by player
                                bool isAssignedToPlayerUnit = false;
                                if (!string.IsNullOrEmpty(leader.UnitID) && snapshot.Campaign.PlayerUnits.ContainsKey(leader.UnitID))
                                {
                                    isAssignedToPlayerUnit = true;
                                }

                                // For unassigned leaders, check if they're in the original player roster
                                // (This handles leaders in reserve between assignments)
                                bool isInPlayerPool = !leader.IsAssigned;

                                if (isAssignedToPlayerUnit || isInPlayerPool)
                                {
                                    snapshot.Campaign.PlayerLeaders[leader.LeaderID] = leader;
                                    coreLeaderCount++;
                                }
                            }
                        }

                        AppService.CaptureUiMessage($"Synchronized core roster: {coreUnitCount} units, {coreLeaderCount} leaders");
                    }
                    catch (Exception ex)
                    {
                        AppService.HandleException(CLASS_NAME, METHOD_NAME,
                            new InvalidOperationException("Failed to synchronize campaign core roster", ex));
                        // Continue - non-fatal error
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
                // PrepareBattle inputs
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

                // Step 0.5: the save names the content it was made against — verify it is still installed
                // ⚠ BEFORE the wipe below. Throwing after ClearAll() would destroy the player's current
                // game to report that a DIFFERENT one could not be loaded.
                VerifyContentAvailable(snap);

                // Step 1: Complete wipe of current runtime state
                AppService.CaptureUiMessage("Clearing current game state...");
                mgr.ClearAll();

                // Step 2: Restore high-level objects
                AppService.CaptureUiMessage("Restoring campaign and scenario data...");
                mgr.CurrentCampaignData = snap.Campaign ?? new CampaignData();
                mgr.CurrentScenarioData = snap.Scenario; // May remain null for campaign-only saves

                // Step 2.5: Restore map data if present
                AppService.CaptureUiMessage("Restoring map data...");

                // The current hex map is statically referenced, so we set it to null first
                GameDataManager.CurrentHexMap = null;

                if (snap.MapData != null)
                {
                    try
                    {
                        // Validate map data before attempting to restore
                        if (!snap.MapData.IsValid())
                        {
                            throw new InvalidOperationException("Map data validation failed - corrupt or incomplete map data in save file");
                        }

                        AppService.CaptureUiMessage($"Restoring map: {snap.MapData.Header.MapName}");

                        // Create HexMap at the size the saved header declares (G1) — same single authority
                        // MapLoader uses, so the two paths cannot drift.
                        var restoredDims = snap.MapData.Header.ResolveMapDimensions();
                        var hexMap = new HexMap(snap.MapData.Header.MapName, restoredDims.x, restoredDims.y);

                        // Populate the map with hex tiles
                        int successCount = 0;
                        int failCount = 0;

                        foreach (HexTile hex in snap.MapData.Hexes)
                        {
                            if (hex == null)
                            {
                                AppService.HandleException(CLASS_NAME, METHOD_NAME,
                                    new InvalidOperationException("Null hex tile found in map data"));
                                continue;
                            }

                            if (!hexMap.SetHexAt(hex))
                            {
                                failCount++;
                                if (failCount <= 5) // Only log first 5 failures
                                {
                                    AppService.HandleException(CLASS_NAME, METHOD_NAME,
                                        new InvalidOperationException($"Failed to restore hex at position {hex.Position}"));
                                }
                            }
                            else
                            {
                                successCount++;
                            }
                        }

                        /* ⚠ Same hard failure as the load path (G6, 2026-08-12): a hex outside the saved
                         * header's own declared geometry means the save is corrupt, and restoring a
                         * silently truncated board is worse than refusing the file. This loop had the same
                         * count-and-discard shape MapLoader's did. */
                        if (failCount > 0)
                        {
                            throw new InvalidOperationException(
                                $"Save file map '{hexMap.MapName}': {failCount} of {snap.MapData.Hexes.Length} " +
                                $"hexes fell outside {hexMap.MapSize.x}x{hexMap.MapSize.y}. The save's map " +
                                "geometry is inconsistent and it cannot be restored.");
                        }

                        AppService.CaptureUiMessage($"Restored {successCount} hexes");

                        // Build neighbor relationships (critical for hex connectivity)
                        hexMap.BuildNeighborRelationships();

                        // Validate constructed map integrity
                        if (!hexMap.ValidateIntegrity())
                        {
                            throw new InvalidOperationException("Map integrity validation failed after restoration");
                        }

                        // Assign to GameDataManager
                        GameDataManager.CurrentHexMap = hexMap;
                        GameDataManager.CurrentMapSize = hexMap.MapSize;

                        AppService.CaptureUiMessage($"Map restored successfully: {hexMap.MapName} ({hexMap.HexCount} hexes)");
                    }
                    catch (Exception ex)
                    {
                        AppService.HandleException(CLASS_NAME, METHOD_NAME,
                            new InvalidOperationException("Failed to restore map data from snapshot", ex));
                        // Set to null on failure
                        GameDataManager.CurrentHexMap = null;
                        throw; // Re-throw since map restoration failure is critical for scenario saves
                    }
                }
                else
                {
                    AppService.CaptureUiMessage("No map data in save - between-battle save detected");
                }

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

                        /* D6 (P2 2026-08-08): deserialized units come through the [JsonConstructor],
                         * which never runs InitializeEquipmentBays — so TotalIntelStats ([JsonIgnore])
                         * arrives EMPTY, silently breaking intel reports and the loss-ledger multiplicand
                         * on every loaded save. Rebuild from the restored slots before registration. */
                        unit.EquipmentBays?.BuildIntelStats();

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

                    // Restore skill trees from deserialized SkillTreeData
                    foreach (var leader in snap.Leaders.Values)
                    {
                        leader?.RestoreFromDeserialization();
                    }
                }

                // Step 4: Rebuild all transient caches and cross-references
                // This handles Leader↔Unit bidirectional links, air unit attachments, etc.
                AppService.CaptureUiMessage("Rebuilding transient caches and cross-references...");
                mgr.RebuildTransientCaches();

                // Step 5: PrepareBattle the loaded state
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

                // Validate map data if present
                if (GameDataManager.CurrentHexMap != null)
                {
                    AppService.CaptureUiMessage("Validating map integrity...");

                    // Run comprehensive map validation
                    if (!GameDataManager.CurrentHexMap.ValidateIntegrity())
                    {
                        AppService.HandleException(CLASS_NAME, METHOD_NAME,
                            new InvalidOperationException("Map integrity validation failed after load"));
                    }

                    if (!GameDataManager.CurrentHexMap.ValidateDimensions())
                    {
                        AppService.HandleException(CLASS_NAME, METHOD_NAME,
                            new InvalidOperationException("Map dimensions validation failed after load"));
                    }

                    if (!GameDataManager.CurrentHexMap.ValidateConnectivity())
                    {
                        AppService.HandleException(CLASS_NAME, METHOD_NAME,
                            new InvalidOperationException("Map connectivity validation failed after load"));
                    }

                    // Validate that unit positions reference valid hexes on the map
                    foreach (var unit in allUnits.Where(u => u.MapPos != null && u.MapPos != Position2D.Zero))
                    {
                        var hex = GameDataManager.CurrentHexMap.GetHexAt(unit.MapPos);
                        if (hex == null)
                        {
                            AppService.HandleException(CLASS_NAME, METHOD_NAME,
                                new InvalidOperationException($"Unit {unit.UnitID} at position {unit.MapPos} references non-existent hex"));
                        }
                    }

                    AppService.CaptureUiMessage($"Map validation passed: {GameDataManager.CurrentHexMap.MapName} ({GameDataManager.CurrentHexMap.HexCount} hexes)");
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

        #region Private Content Resolution

        /// <summary>
        /// Verifies the scenario this save names is still installed (§3.3) — the one genuine hazard the
        /// content/save split leaves open: a patch that removes or renames a scenario strands every save
        /// that referenced it.
        ///
        /// ⚠ THE VERDICT DEPENDS ON THE SAVE'S KIND, which is principle P5 doing real work rather than
        /// being a description:
        ///   • IN-BATTLE (MapData != null) — SELF-CONTAINED. The map and units travel inside the save, so
        ///     it loads and plays perfectly well with its scenario uninstalled. Warn only; refusing here
        ///     would throw away a battle the file is fully capable of restoring.
        ///   • BETWEEN-BATTLE (MapData == null) — CONTENT-DEPENDENT. It holds a roster and a scenarioId
        ///     and nothing else, so a missing scenario means there is genuinely nothing to load. Refuse
        ///     with a message that NAMES the scenario, so the cause is "content X is missing" and not a
        ///     null-reference somewhere downstream.
        ///
        /// A save naming no scenario at all is legal (a bare roster) and is not checked.
        /// </summary>
        private static void VerifyContentAvailable(GameStateSnapshot snap)
        {
            string scenarioId = snap.Header?.ScenarioId;

            if (string.IsNullOrWhiteSpace(scenarioId))
                return;

            if (GameDataManager.FindManifestById(scenarioId) != null)
                return;

            if (snap.MapData != null)
            {
                AppService.CaptureUiMessage(
                    $"Scenario '{scenarioId}' is no longer installed, but this save carries its own map " +
                    "and will load normally.");
                return;
            }

            throw new InvalidOperationException(
                $"This save continues scenario '{scenarioId}', which is not installed. It may have been " +
                "removed or renamed by an update. Verify the game files, or start a new game — the save " +
                "itself is not damaged.");
        }

        #endregion // Private Content Resolution

        #region Private Provenance

        /// <summary>
        /// Builds the save's provenance header (§3.1): what wrote this file, and against what content.
        ///
        /// The scenario/campaign ids are what make a content patch diagnosable — without them, a save that
        /// no longer matches the installed content fails as a mystery instead of naming what it wanted.
        /// </summary>
        private static GameDataHeader BuildHeader(GameDataManager mgr)
        {
            var manifest = GameDataManager.CurrentManifest;

            return new GameDataHeader
            {
                SaveTime = DateTime.UtcNow,
                GameVersion = UnityEngine.Application.version ?? string.Empty,

                // ⚠ Prefer the live manifest, fall back to the restored ScenarioData: a between-battle save
                // has no manifest loaded, but still knows which scenario it just finished.
                ScenarioId = manifest?.ScenarioId ?? mgr.CurrentScenarioData?.ScenarioId ?? string.Empty,
                CampaignId = mgr.CurrentCampaignData?.CampaignId ?? string.Empty,

                CombatUnitCount = mgr.UnitCount,
                LeaderCount = mgr.LeaderCount
            };
        }

        #endregion // Private Provenance

        #region Private Upgrade Logic

        /// <summary>
        /// Migrates an older snapshot up to the current save format, one version at a time.
        /// Every <see cref="GameData.SAVE_VERSION"/> bump must add its step to the switch below.
        /// </summary>
        private static GameStateSnapshot UpgradeSnapshot(GameStateSnapshot snap) =>
            RunMigrationLadder(snap, MINIMUM_SUPPORTED_SAVE_VERSION, CURRENT_SAVE_VERSION, MigrateStep);

        /// <summary>
        /// Looks up the migration step that advances a snapshot from <paramref name="from"/> to
        /// <paramref name="from"/>+1. Returns null when no step is defined — the ladder turns that into
        /// the "a SAVE_VERSION bump must ship with its step" failure.
        ///
        /// ⚠ ONE ARM PER VERSION BUMP. Adding a member here is the whole of CLAUDE.md §2 item 12.
        /// </summary>
        private static Func<GameStateSnapshot, GameStateSnapshot> MigrateStep(int from) => from switch
        {
            // e.g.  3 => MigrateV3ToV4,   // AI2: AIPerceptionState enters the snapshot
            //       4 => MigrateV4ToV5,   // P5: loss ledger enters the snapshot
            //
            // NOTE there is deliberately NO 3 => arm for the 3→4 bump of 2026-07-28, NO 4 => arm for the
            // 4→5 profile-slot bump of 2026-08-08, and NO 5 => arm for the 5→6 over-water bump of
            // 2026-08-11. While MINIMUM_SUPPORTED_SAVE_VERSION tracks SAVE_VERSION (pre-1.0), any older
            // save is refused by the floor check before the ladder is ever entered, so a step would be
            // unreachable code pretending to be a migration. See the SAVE_VERSION comment in GameData.
            _ => null
        };

        /// <summary>
        /// The migration ladder itself, parameterised so it can be exercised.
        ///
        /// ⚠ WHY THE PARAMETERS EXIST: with the shipping constants, MINIMUM_SUPPORTED == CURRENT, so every
        /// older save is refused by the floor check and the loop below is UNREACHABLE. Its guards — the
        /// missing step and the step that fails to advance — could therefore never be tested through the
        /// production entry point, and would first be exercised by the real migration they exist to
        /// protect. Injecting the versions and the step lookup keeps ONE implementation while letting
        /// <c>SaveMigrationLadderTests</c> drive it. Production callers use
        /// <see cref="UpgradeSnapshot"/> and get the real constants.
        /// </summary>
        internal static GameStateSnapshot RunMigrationLadder(
            GameStateSnapshot snap,
            int minimumSupported,
            int currentVersion,
            Func<int, Func<GameStateSnapshot, GameStateSnapshot>> stepLookup)
        {
            // A save older than the supported floor is REFUSED, not guessed at.
            // ⚠ The previous implementation stamped `SaveVersion = CURRENT` and returned, which is worse
            // than doing nothing: it relabelled old data as current, so the save then claimed a shape it
            // did not have and no later migration could tell what it really was. Fail loudly instead.
            if (snap.SaveVersion < minimumSupported)
            {
                throw new InvalidOperationException(
                    $"Save file version {snap.SaveVersion} is older than the minimum supported version " +
                    $"{minimumSupported} and cannot be loaded. Saves written by pre-release " +
                    "builds are not supported.");
            }

            // Apply steps in order. Each step transforms the snapshot AND stamps its own new version;
            // the guard below catches a step that forgets to, which would otherwise spin here forever.
            while (snap.SaveVersion < currentVersion)
            {
                int from = snap.SaveVersion;

                var step = stepLookup(from)
                    ?? throw new InvalidOperationException(
                        $"No migration step is defined from save version {from} to {from + 1}. " +
                        $"A {nameof(GameData.SAVE_VERSION)} bump must ship with its migration step.");

                snap = step(snap);

                if (snap == null)
                {
                    throw new InvalidOperationException(
                        $"Migration step from version {from} returned null.");
                }

                if (snap.SaveVersion != from + 1)
                {
                    throw new InvalidOperationException(
                        $"Migration step from version {from} left the snapshot at version {snap.SaveVersion}; " +
                        "each step must advance exactly one version.");
                }

                AppService.CaptureUiMessage($"Migrated save from version {from} to {snap.SaveVersion}");
            }

            return snap;
        }

        #endregion
    }
}
