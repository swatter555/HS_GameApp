using HammerAndSickle.Models;
using HammerAndSickle.Persistence;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HammerAndSickle.Controllers
{ 
    public class GameDataManager : MonoBehaviour
    {
        #region Constants

        private const string CLASS_NAME = nameof(GameDataManager);

        // File extensions
        public const string MANIFEST_EXTENSION = ".manifest";
        public const string MAP_EXTENSION = ".map";
        public const string OOB_EXTENSION = ".oob";
        public const string AII_EXTENSION = ".aii";
        public const string BRF_EXTENSION = ".brf";
        public const string CMP_EXTENSION = ".cmp";

        #endregion // Constants

        #region Singleton

        private static GameDataManager _instance;
        private static readonly object _instanceLock = new();
        public static GameDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        _instance ??= new GameDataManager();
                    }
                }
                return _instance;
            }
        }

        #endregion // Singleton

        #region Fields

        private readonly Dictionary<string, CombatUnit> _combatUnits = new();
        private readonly Dictionary<string, Leader> _leaders = new();

        #endregion // Fields

        #region Properties

        /// <summary>The player‑progression data that persists across scenarios.</summary>
        public CampaignData CurrentCampaignData { get; set; }

        /// <summary>The currently loaded scenario data (null outside of missions).</summary>
        public ScenarioData CurrentScenarioData { get; set; }

        #endregion // Properties

        #region Unity Lifecycle

        /// <summary>
        /// Initialize the loading service on start.
        /// </summary>
        private void Start()
        {
            try
            {
                // Initialize static databases
                WeaponSystemsDatabase.Initialize();
                IntelProfileDatabase.Initialize();
                CombatUnitDatabase.Initialize();
                
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(Start), ex);
            }
        }

        /// <summary>
        /// Perform the map loading test on the first update call.
        /// </summary>
        private void Update()
        {
            
        }

        #endregion // Unity Lifecycle

        #region Registration

        /// <summary>
        /// Registers a combat unit in the system.
        /// </summary>
        public bool RegisterCombatUnit(CombatUnit unit)
        {
            // Validate input
            if (unit == null)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterCombatUnit),
                    new ArgumentNullException(nameof(unit)));
                return false;
            }

            try
            {
                // Check if the unit ID is valid
                if (_combatUnits.ContainsKey(unit.UnitID))
                {
                    AppService.HandleException(CLASS_NAME, nameof(RegisterCombatUnit),
                        new InvalidOperationException($"Combat unit with ID {unit.UnitID} already registered"));
                    return false;
                }

                // Register the combat unit
                _combatUnits[unit.UnitID] = unit;

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterCombatUnit), e);
                return false;
            }
        }

        /// <summary>
        /// Registers a leader in the system.
        /// </summary>
        public bool RegisterLeader(Leader leader)
        {
            // Validate input
            if (leader == null)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterLeader),
                    new ArgumentNullException(nameof(leader)));
                return false;
            }

            try
            {
                // Check if the leader ID is valid
                if (_leaders.ContainsKey(leader.LeaderID))
                {
                    AppService.HandleException(CLASS_NAME, nameof(RegisterLeader),
                        new InvalidOperationException($"Leader with ID {leader.LeaderID} already registered"));
                    return false;
                }

                // Register the leader
                _leaders[leader.LeaderID] = leader;

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterLeader), e);
                return false;
            }
        }

        /// <summary>
        /// Unregisters a combat unit from the system using its unique identifier.
        /// </summary>
        public bool UnregisterCombatUnit(string unitId)
        {
            // Validate input
            if (string.IsNullOrEmpty(unitId))
                return false;

            // Check if the unit exists
            if (_combatUnits.Remove(unitId)) return true;
            else return false;
        }

        /// <summary>
        /// Unregisters a leader from the system using the specified leader ID.
        /// </summary>
        public bool UnregisterLeader(string leaderId)
        {
            // Validate input
            if (string.IsNullOrEmpty(leaderId))
                return false;

            // Check if the leader exists
            if (_leaders.Remove(leaderId)) return true;
            else return false;
        }

        #endregion // Registration

        #region Retrieval Methods

        /// <summary>
        /// Retrieves a combat unit by its unique identifier.
        /// </summary>
        public CombatUnit GetCombatUnit(string unitId)
        {
            // Validate input
            if (string.IsNullOrEmpty(unitId)) return null;

            try
            {
                // Attempt to retrieve the combat unit from the dictionary
                return _combatUnits.TryGetValue(unitId, out CombatUnit unit) ? unit : null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetCombatUnit), e);
                return null;
            }
        }

        /// <summary>
        /// Get a leader by its unique identifier.
        /// </summary>
        public Leader GetLeader(string leaderId)
        {
            // 
            if (string.IsNullOrEmpty(leaderId)) return null;

            try
            {
                // Attempt to retrieve the leader from the dictionary
                return _leaders.TryGetValue(leaderId, out Leader leader) ? leader : null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetLeader), e);
                return null;
            }
        }

        /// <summary>
        /// Retrieves all combat units currently managed by the system.
        /// </summary>
        public IReadOnlyCollection<CombatUnit> GetAllCombatUnits()
        {
            try
            {
                return _combatUnits.Values.ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAllCombatUnits), e);
                return new List<CombatUnit>();
            }
        }

        /// <summary>
        /// Retrieves all leaders currently stored in the system.
        /// </summary>
        public IReadOnlyCollection<Leader> GetAllLeaders()
        {
            try
            {
                return _leaders.Values.ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAllLeaders), e);
                return new List<Leader>();
            }
        }

        /// <summary>
        /// Retrieves all combat units that belong to the player.
        /// </summary>
        public IReadOnlyCollection<CombatUnit> GetPlayerUnits()
        {
            try
            {
                return _combatUnits.Values.Where(unit => unit.Side == Side.Player).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetPlayerUnits), e);
                return new List<CombatUnit>();
            }
        }

        /// <summary>
        /// Retrieves a collection of combat units controlled by the AI.
        /// </summary>
        public IReadOnlyCollection<CombatUnit> GetAIUnits()
        {
            try
            {
                return _combatUnits.Values.Where(unit => unit.Side != Side.Player).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAIUnits), e);
                return new List<CombatUnit>();
            }
        }

        #endregion // Retrieval Methods

        #region Query Methods

        /// <summary>
        /// Retrieves a collection of combat units that match the specified classification.
        /// </summary>
        public IReadOnlyCollection<CombatUnit> GetCombatUnitsByClassification(UnitClassification classification)
        {
            try
            {
                return _combatUnits.Values.Where(unit => unit.Classification == classification).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetCombatUnitsByClassification), e);
                return new List<CombatUnit>();
            }
        }

        /// <summary>
        /// Get all CombatUnits that are bases.
        /// </summary>
        public IReadOnlyCollection<CombatUnit> GetAllFacilities()
        {
            try
            {
                return _combatUnits.Values.Where(unit => unit.IsBase).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAllFacilities), e);
                return new List<CombatUnit>();
            }
        }

        /// <summary>
        /// Retrieves a collection of leaders who are not currently assigned.
        /// </summary>
        public IReadOnlyCollection<Leader> GetUnassignedLeaders()
        {
            try
            {
                return _leaders.Values.Where(leader => !leader.IsAssigned).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetUnassignedLeaders), e);
                return new List<Leader>();
            }
        }

        /// <summary>
        /// Retrieves a collection of leaders filtered by the specified command grade.
        /// </summary>
        public IReadOnlyCollection<Leader> GetLeadersByGrade(CommandGrade grade)
        {
            try
            {
                return _leaders.Values.Where(leader => leader.CommandGrade == grade).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetLeadersByGrade), e);
                return new List<Leader>();
            }
        }

        #endregion // Query Methods

        #region Public Methods

        /// <summary>
        /// Assign a leader to a combat unit by their unique identifiers.
        /// </summary>
        public bool AssignLeaderToUnit(string leaderID, string unitID)
        {
            try
            {
                var leader = GetLeader(leaderID);
                var unit = GetCombatUnit(unitID);

                if (leader == null || unit == null) return false;

                // Check if leader is already assigned elsewhere
                if (leader.IsAssigned)
                {
                    AppService.CaptureUiMessage($"Leader {leader.Name} is already assigned to another unit");
                    return false;
                }

                // Handle bidirectional assignment
                leader.AssignToUnit(unitID);
                unit.LeaderID = leaderID;

                AppService.CaptureUiMessage($"Leader {leader.Name} assigned to {unit.UnitName}");
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AssignLeaderToUnit), e);
                return false;
            }
        }

        /// <summary>
        /// Unassigns a leader from their current combat unit by their unique identifier.
        /// </summary>
        public bool UnassignLeader(string leaderID)
        {
            try
            {
                var leader = GetLeader(leaderID);
                if (leader?.IsAssigned != true) return false;

                var unit = GetCombatUnit(leader.UnitID);

                leader.UnassignFromUnit();
                if (unit != null) unit.LeaderID = string.Empty;

                AppService.CaptureUiMessage($"Leader {leader.Name} unassigned");
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(UnassignLeader), e);
                return false;
            }
        }

        /// <summary>
        /// Completely wipes every mutable runtime collection so a brand‑new snapshot can be applied
        /// without leaving behind zombie references.  This must be called <b>before</b> any new state
        /// (campaign, scenario, units, leaders, caches) is loaded.
        /// </summary>
        public void ClearAll()
        {
            const string METHOD_NAME = nameof(ClearAll);

            try
            {
                AppService.CaptureUiMessage("Beginning complete game state wipe...");

                // ──────────────────────────────────────────────────────────────────────────────
                // Clear all entity dictionaries
                // ──────────────────────────────────────────────────────────────────────────────

                int unitsCleared = _combatUnits.Count;
                int leadersCleared = _leaders.Count;

                _combatUnits.Clear();
                _leaders.Clear();

                AppService.CaptureUiMessage($"Cleared {unitsCleared} units and {leadersCleared} leaders from registries");

                // ──────────────────────────────────────────────────────────────────────────────
                // Null out high‑level game state objects
                // ──────────────────────────────────────────────────────────────────────────────

                CurrentCampaignData = null;
                CurrentScenarioData = null;

                AppService.CaptureUiMessage("Cleared campaign and scenario data");

                // ──────────────────────────────────────────────────────────────────────────────
                // Clear transient caches and computed data
                // ──────────────────────────────────────────────────────────────────────────────

                ClearTransientCaches();

                // ──────────────────────────────────────────────────────────────────────────────
                // Force garbage collection of cleared objects (optional but helpful)
                // ──────────────────────────────────────────────────────────────────────────────

                // Suggest garbage collection to clean up the large number of objects we just released
                // This is optional but can help with memory pressure after clearing large game states
                GC.Collect();
                GC.WaitForPendingFinalizers();

                AppService.CaptureUiMessage("Game state completely cleared and ready for new data");
            }
            catch (Exception ex)
            {
                // This is a critical operation - if ClearAll fails, the game state could be corrupted
                AppService.HandleException(CLASS_NAME, METHOD_NAME, ex);
                throw; // Re‑throw so calling code knows the clear failed
            }
        }

        /// <summary>
        /// Clears all transient (non-persistent) caches and computed data.
        /// This is called by ClearAll() and can also be called independently to refresh caches.
        /// </summary>
        private void ClearTransientCaches()
        {
            const string METHOD_NAME = nameof(ClearTransientCaches);

            try
            {
                // ──────────────────────────────────────────────────────────────────────────────
                // Clear any cached lookups, pathfinding data, supply networks, etc.
                // ──────────────────────────────────────────────────────────────────────────────

                // Note: Add cache clearing logic here as new caches are implemented
                // Examples of what might go here in the future:
                // - Pathfinding graphs
                // - Supply network calculations  
                // - Unit visibility caches
                // - Threat assessment matrices
                // - AI decision trees

                // For now, this is mostly a placeholder but provides the structure
                // for future cache management

                AppService.CaptureUiMessage("Transient caches cleared");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, METHOD_NAME, ex);
                // Don't re-throw cache clearing errors - they're not critical to core functionality
            }
        }

        /// <summary>
        /// Re‑hydrate all transient (non‑persistent) data after loading a <see cref="GameDataSnapshot"/>.
        /// </summary>
        /// <remarks>
        ///   • Re‑establishes the <strong>Leader ↔ Unit</strong> two‑way link so that skills, morale buffs,
        ///     etc. can resolve their owning commander at runtime.  <br/>
        ///   • Restores <strong>air‑unit attachments</strong> for airbases – they are stored by <em>UnitID</em>
        ///     inside <c>_attachedUnitIDs</c> during serialization, but the actual <c>CombatUnit</c>
        ///     references live in <c>_airUnitsAttached</c>.  <br/>
        ///   • Serves as a single choke‑point if we add more caches in the future (supply projection,
        ///     path‑finding, quick‑look lists, etc.).
        /// </remarks>
        public void RebuildTransientCaches()
        {
            try
            {
                // -------------------------------------------------
                // 1  Leader ↔ Unit linkage                    
                // -------------------------------------------------
                foreach (var leader in _leaders.Values)
                {
                    if (string.IsNullOrEmpty(leader.UnitID))
                        continue; // Leader currently un‑assigned

                    if (_combatUnits.TryGetValue(leader.UnitID, out var unit))
                    {
                        // If the snapshot didn’t preserve the runtime assignment flag, redo it so any
                        // listeners (UI, combat calcs) receive their event callbacks.
                        if (!leader.IsAssigned)
                        {
                            leader.AssignToUnit(leader.UnitID); // Fires LeaderAssigned event internally
                        }

                        // Some versions expose a writable LeaderID on CombatUnit; others keep the link
                        // private.  Reflection keeps us build‑agnostic while remaining type‑safe for
                        // release builds where the property exists.
                        var leaderIdProp = unit.GetType().GetProperty("LeaderID", BindingFlags.Public | BindingFlags.Instance);
                        if (leaderIdProp != null && leaderIdProp.CanWrite)
                        {
                            leaderIdProp.SetValue(unit, leader.LeaderID);
                        }
                    }
                    else
                    {
                        // Dangling reference – clear it to avoid null checks elsewhere.
                        leader.UnassignFromUnit();
                    }
                }

                // -------------------------------------------------
                // 2️  Air‑unit attachments for facilities       
                // -------------------------------------------------
                foreach (var facility in _combatUnits.Values.Where(cu => cu.IsBase))
                {
                    if (facility.FacilityType == FacilityType.Airbase)
                    {
                        // Clear existing attachments
                        facility.ClearAllAirUnits();

                        // Get the attached unit IDs (this will now be populated from JSON)
                        var attachedIds = facility.AttachedUnitIDs;

                        if (attachedIds?.Count > 0)
                        {
                            int reattachedCount = 0;
                            foreach (string unitId in attachedIds)
                            {
                                if (!string.IsNullOrEmpty(unitId) && _combatUnits.TryGetValue(unitId, out var airUnit))
                                {
                                    if (facility.AddAirUnit(airUnit))
                                    {
                                        reattachedCount++;
                                    }
                                }
                                else
                                {
                                    AppService.CaptureUiMessage($"Warning: Could not find air unit {unitId} for reattachment to {facility.UnitName}");
                                }
                            }

                            AppService.CaptureUiMessage($"Reattached {reattachedCount} air units to {facility.UnitName}");
                        }
                    }
                }

                // -------------------------------------------------
                // 3️  Future caches can be rebuilt here         
                // -------------------------------------------------
                // e.g. supply graph, quick lookup dictionaries, etc.

                AppService.CaptureUiMessage("Game data caches rebuilt successfully.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RebuildTransientCaches), e);
                throw; // Re‑throw so calling code can decide how to proceed
            }
        }

        #endregion // Public Methods

        #region Private Helper Methods

        /// <summary>
        /// Clears all player-related data, including units, leaders, and other associated objects.
        /// </summary>
        private void ClearPlayerData()
        {
            var playerUnits = GetPlayerUnits().Select(unit => unit.UnitID).ToList();
            foreach (var unitId in playerUnits)
            {
                UnregisterCombatUnit(unitId);
            }

            var allLeaders = GetAllLeaders().Select(leader => leader.LeaderID).ToList();
            foreach (var leaderId in allLeaders)
            {
                UnregisterLeader(leaderId);
            }
        }

        /// <summary>
        /// Calculates a checksum value based on the current state of combat units and leaders.
        /// </summary>
        private string CalculateChecksum()
        {
            try
            {
                int checksum = _combatUnits.Count * 17 + _leaders.Count * 23;
                return Math.Abs(checksum).ToString("X8");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CalculateChecksum), e);
                return "ERROR";
            }
        }

        #endregion // Private Helper Methods

        #region Template and Database Access

        /// <summary>
        /// Retrieves a combat unit template by its unique identifier.
        /// </summary>
        /// <param name="templateId">Template identifier string</param>
        /// <returns>Unit template or null if not found</returns>
        public static CombatUnit GetUnitTemplate(string templateId)
        {
            try
            {
                return CombatUnitDatabase.GetUnitTemplate(templateId);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetUnitTemplate), e);
                return null;
            }
        }

        /// <summary>
        /// Creates a new combat unit instance from template.
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <param name="unitName">Name for the new unit instance</param>
        /// <returns>New CombatUnit instance or null if template not found</returns>
        public static CombatUnit CreateUnitFromTemplate(string templateId, string unitName)
        {
            try
            {
                return CombatUnitDatabase.CreateUnitFromTemplate(templateId, unitName);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateUnitFromTemplate), e);
                return null;
            }
        }

        /// <summary>
        /// Gets all template identifiers for a specific nationality.
        /// </summary>
        /// <param name="nationality">Nationality to filter by</param>
        /// <returns>List of template IDs matching nationality</returns>
        public static List<string> GetTemplatesByNationality(Nationality nationality)
        {
            try
            {
                return CombatUnitDatabase.GetTemplatesByNationality(nationality);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetTemplatesByNationality), e);
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets all template identifiers for a specific unit classification.
        /// </summary>
        /// <param name="classification">Unit classification to filter by</param>
        /// <returns>List of template IDs matching classification</returns>
        public static List<string> GetTemplatesByClassification(UnitClassification classification)
        {
            try
            {
                return CombatUnitDatabase.GetTemplatesByClassification(classification);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetTemplatesByClassification), e);
                return new List<string>();
            }
        }

        /// <summary>
        /// Checks if a combat unit template exists.
        /// </summary>
        /// <param name="templateId">Template identifier to check</param>
        /// <returns>True if template exists</returns>
        public static bool HasUnitTemplate(string templateId)
        {
            try
            {
                return CombatUnitDatabase.HasUnitTemplate(templateId);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HasUnitTemplate), e);
                return false;
            }
        }

        /// <summary>
        /// Gets the total number of unit templates currently stored.
        /// </summary>
        /// <returns>Template count</returns>
        public static int GetTemplateCount()
        {
            try
            {
                return CombatUnitDatabase.TemplateCount;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetTemplateCount), e);
                return 0;
            }
        }

        /// <summary>
        /// Gets all template identifiers currently stored in the database.
        /// </summary>
        /// <returns>List of all template IDs</returns>
        public static List<string> GetAllTemplateIds()
        {
            try
            {
                return CombatUnitDatabase.GetAllTemplateIds();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAllTemplateIds), e);
                return new List<string>();
            }
        }

        /// <summary>
        /// Retrieves a weapon system profile by its enum identifier.
        /// </summary>
        /// <param name="weaponSystemID">The weapon system identifier to look up</param>
        /// <returns>The corresponding WeaponSystemProfile, or null if not found</returns>
        public static WeaponSystemProfile GetWeaponSystemProfile(WeaponSystems weaponSystemID)
        {
            try
            {
                return WeaponSystemsDatabase.GetWeaponSystemProfile(weaponSystemID);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetWeaponSystemProfile), e);
                return null;
            }
        }

        /// <summary>
        /// Gets the total number of weapon system profiles in the database.
        /// </summary>
        /// <returns>Profile count</returns>
        public static int GetWeaponSystemProfileCount()
        {
            try
            {
                return WeaponSystemsDatabase.ProfileCount;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetWeaponSystemProfileCount), e);
                return 0;
            }
        }

        /// <summary>
        /// Checks if a specific intel profile type has been defined in the system.
        /// </summary>
        /// <param name="profileType">The profile type to check</param>
        /// <returns>True if the profile type is defined</returns>
        public static bool HasIntelProfile(IntelProfileTypes profileType)
        {
            try
            {
                return IntelProfileDatabase.HasProfile(profileType);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HasIntelProfile), e);
                return false;
            }
        }

        /// <summary>
        /// Gets the maximum count for a specific weapon system in an intel profile type.
        /// </summary>
        /// <param name="profileType">The profile type to query</param>
        /// <param name="weaponSystem">The weapon system to look up</param>
        /// <returns>Maximum count, or 0 if not found</returns>
        public static int GetIntelWeaponSystemCount(IntelProfileTypes profileType, WeaponSystems weaponSystem)
        {
            try
            {
                return IntelProfileDatabase.GetWeaponSystemCount(profileType, weaponSystem);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetIntelWeaponSystemCount), e);
                return 0;
            }
        }

        /// <summary>
        /// Generates an intelligence report for a unit based on the specified spotted level.
        /// </summary>
        /// <param name="profileType">Intel profile type</param>
        /// <param name="unitName">Unit name for the report</param>
        /// <param name="hitPoints">Current hit points</param>
        /// <param name="nationality">Unit nationality</param>
        /// <param name="deploymentPosition">Current deployment state</param>
        /// <param name="experienceLevel">Unit experience level</param>
        /// <param name="efficiencyLevel">Unit efficiency level</param>
        /// <param name="spottedLevel">Intelligence accuracy level</param>
        /// <returns>IntelReport with unit intelligence data, or null if generation fails</returns>
        public static IntelReport GenerateIntelReport(IntelProfileTypes profileType, string unitName, int hitPoints,
            Nationality nationality, DeploymentPosition deploymentPosition, ExperienceLevel experienceLevel,
            EfficiencyLevel efficiencyLevel, SpottedLevel spottedLevel = SpottedLevel.Level1)
        {
            try
            {
                return IntelProfileDatabase.GenerateIntelReport(profileType, unitName, hitPoints, nationality,
                    deploymentPosition, experienceLevel, efficiencyLevel, spottedLevel);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GenerateIntelReport), e);
                return null;
            }
        }

        /// <summary>
        /// Gets whether all static databases have been successfully initialized.
        /// </summary>
        /// <returns>True if all databases are initialized</returns>
        public static bool AreAllDatabasesInitialized()
        {
            try
            {
                return CombatUnitDatabase.IsInitialized &&
                       WeaponSystemsDatabase.IsInitialized &&
                       IntelProfileDatabase.IsInitialized;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AreAllDatabasesInitialized), e);
                return false;
            }
        }

        #endregion // Template and Database Access
    }
}