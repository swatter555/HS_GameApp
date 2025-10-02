using HammerAndSickle.Models;
using HammerAndSickle.Persistence;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// General constants used throughout the Hammer & Sickle application.
    /// </summary>
    public class GeneralConstants
    {
        #region Menu/Dialog IDs

        // Main startup scene menu/dialog IDs
        public const int DefaultID                   = 0;
        public const int MainScene_CoreInterface_ID  = 1;
        public const int MainScene_ContinueDialog_ID = 2;
        public const int MainScene_CampaignDialog_ID = 3;
        public const int MainScene_ScenarioDialog_ID = 4;
        public const int MainScene_OptionsDialog_ID  = 5;
        public const int MainScene_ExitDialog_ID     = 6;

        #endregion
    }

    /// <summary>
    /// Central data management system for Hammer & Sickle, managing combat units, 
    /// leaders, and game state with Unity-compliant singleton pattern.
    /// </summary>
    public class GameDataManager : MonoBehaviour
    {
        #region Constants

        private const string CLASS_NAME = nameof(GameDataManager);

        // File extensions for future use
        public const string MANIFEST_EXTENSION = ".manifest";
        public const string MAP_EXTENSION = ".map";
        public const string OOB_EXTENSION = ".oob";
        public const string AII_EXTENSION = ".aii";
        public const string BRF_EXTENSION = ".brf";
        public const string CMP_EXTENSION = ".cmp";

        #endregion // Constants

        #region Singleton

        private static GameDataManager _instance;

        /// <summary>
        /// Singleton instance with Unity-compliant lazy initialization.
        /// </summary>
        public static GameDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find existing instance in scene (using new Unity API)
                    _instance = FindAnyObjectByType<GameDataManager>();

                    // Create new instance if none exists
                    if (_instance == null)
                    {
                        GameObject go = new ("GameDataManager");
                        _instance = go.AddComponent<GameDataManager>();
                    }
                }
                return _instance;
            }
        }

        #endregion // Singleton

        #region Fields

        private readonly Dictionary<string, CombatUnit> _combatUnits = new();
        private readonly Dictionary<string, Leader> _leaders = new();
        private bool _isInitialized = false;

        #endregion // Fields

        #region Properties

        /// <summary>The player progression data that persists across scenarios.</summary>
        public CampaignData CurrentCampaignData { get; set; }

        /// <summary>The currently loaded scenario data (null outside of missions).</summary>
        public ScenarioData CurrentScenarioData { get; set; }

        /// <summary>Indicates whether the manager has been fully initialized.</summary>
        public bool IsReady => _isInitialized;

        /// <summary>Gets the count of registered combat units.</summary>
        public int UnitCount => _combatUnits.Count;

        /// <summary>Gets the count of registered leaders.</summary>
        public int LeaderCount => _leaders.Count;

        #endregion // Properties

        #region Unity Lifecycle

        private void Awake()
        {
            // Enforce singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize core systems early
            InitializeDatabases();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion // Unity Lifecycle

        #region Initialization

        /// <summary>
        /// Initializes all static databases required for the game.
        /// </summary>
        private void InitializeDatabases()
        {
            try
            {
                if (_isInitialized)
                    return;

                // Initialize static databases
                WeaponSystemsDatabase.Initialize();
                IntelProfileDatabase.Initialize();
                CombatUnitDatabase.Initialize();

                _isInitialized = true;
                AppService.CaptureUiMessage("Game databases initialized successfully");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(InitializeDatabases), ex);
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Ensures the GameDataManager singleton exists. Call this at game startup.
        /// </summary>
        public static void EnsureExists()
        {
            if (_instance == null)
            {
                _ = Instance; // Forces creation through the getter
            }
        }

        #endregion // Initialization

        #region Registration

        /// <summary>
        /// Registers a combat unit in the system.
        /// </summary>
        public bool RegisterCombatUnit(CombatUnit unit)
        {
            if (!ValidateEntity(unit, nameof(unit)))
                return false;

            try
            {
                if (_combatUnits.ContainsKey(unit.UnitID))
                {
                    AppService.HandleException(CLASS_NAME, nameof(RegisterCombatUnit),
                        new InvalidOperationException($"Combat unit with ID {unit.UnitID} already registered"));
                    return false;
                }

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
            if (!ValidateEntity(leader, nameof(leader)))
                return false;

            try
            {
                if (_leaders.ContainsKey(leader.LeaderID))
                {
                    AppService.HandleException(CLASS_NAME, nameof(RegisterLeader),
                        new InvalidOperationException($"Leader with ID {leader.LeaderID} already registered"));
                    return false;
                }

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
        /// Unregisters a combat unit from the system.
        /// </summary>
        public bool UnregisterCombatUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
                return false;

            return _combatUnits.Remove(unitId);
        }

        /// <summary>
        /// Unregisters a leader from the system.
        /// </summary>
        public bool UnregisterLeader(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId))
                return false;

            return _leaders.Remove(leaderId);
        }

        #endregion // Registration

        #region Retrieval Methods

        /// <summary>
        /// Retrieves a combat unit by its unique identifier.
        /// </summary>
        public CombatUnit GetCombatUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
                return null;

            return _combatUnits.TryGetValue(unitId, out CombatUnit unit) ? unit : null;
        }

        /// <summary>
        /// Get a leader by its unique identifier.
        /// </summary>
        public Leader GetLeader(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId))
                return null;

            return _leaders.TryGetValue(leaderId, out Leader leader) ? leader : null;
        }

        /// <summary>
        /// Retrieves all combat units currently managed by the system.
        /// </summary>
        public IReadOnlyCollection<CombatUnit> GetAllCombatUnits()
        {
            return _combatUnits.Values.ToList();
        }

        /// <summary>
        /// Retrieves all leaders currently stored in the system.
        /// </summary>
        public IReadOnlyCollection<Leader> GetAllLeaders()
        {
            return _leaders.Values.ToList();
        }

        #endregion // Retrieval Methods

        #region Query Methods

        /// <summary>
        /// Gets combat units that match the specified filter.
        /// </summary>
        public IReadOnlyCollection<CombatUnit> GetUnits(Predicate<CombatUnit> filter)
        {
            try
            {
                if (filter == null)
                    return GetAllCombatUnits();

                return _combatUnits.Values.Where(u => filter(u)).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetUnits), e);
                return new List<CombatUnit>();
            }
        }

        /// <summary>
        /// Gets leaders that match the specified filter.
        /// </summary>
        public IReadOnlyCollection<Leader> GetLeaders(Predicate<Leader> filter)
        {
            try
            {
                if (filter == null)
                    return GetAllLeaders();

                return _leaders.Values.Where(l => filter(l)).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetLeaders), e);
                return new List<Leader>();
            }
        }

        /// <summary>
        /// Retrieves all combat units that belong to the player.
        /// </summary>
        public IReadOnlyCollection<CombatUnit> GetPlayerUnits()
        {
            return GetUnits(unit => unit.Side == Side.Player);
        }

        /// <summary>
        /// Retrieves a collection of combat units controlled by the AI.
        /// </summary>
        public IReadOnlyCollection<CombatUnit> GetAIUnits()
        {
            return GetUnits(unit => unit.Side != Side.Player);
        }

        /// <summary>
        /// Retrieves units by classification.
        /// </summary>
        public IReadOnlyCollection<CombatUnit> GetCombatUnitsByClassification(UnitClassification classification)
        {
            return GetUnits(unit => unit.Classification == classification);
        }

        /// <summary>
        /// Retrieves unassigned leaders.
        /// </summary>
        public IReadOnlyCollection<Leader> GetUnassignedLeaders()
        {
            return GetLeaders(leader => !leader.IsAssigned);
        }

        /// <summary>
        /// Retrieves leaders by command grade.
        /// </summary>
        public IReadOnlyCollection<Leader> GetLeadersByGrade(CommandGrade grade)
        {
            return GetLeaders(leader => leader.CommandGrade == grade);
        }

        #endregion // Query Methods

        #region Leader-Unit Assignment

        /// <summary>
        /// Assign a leader to a combat unit by their unique identifiers.
        /// </summary>
        public bool AssignLeaderToUnit(string leaderID, string unitID)
        {
            try
            {
                var leader = GetLeader(leaderID);
                var unit = GetCombatUnit(unitID);

                if (leader == null || unit == null)
                    return false;

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
        /// Unassigns a leader from their current combat unit.
        /// </summary>
        public bool UnassignLeader(string leaderID)
        {
            try
            {
                var leader = GetLeader(leaderID);
                if (leader?.IsAssigned != true)
                    return false;

                var unit = GetCombatUnit(leader.UnitID);

                leader.UnassignFromUnit();
                if (unit != null)
                    unit.LeaderID = string.Empty;

                AppService.CaptureUiMessage($"Leader {leader.Name} unassigned");
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(UnassignLeader), e);
                return false;
            }
        }

        #endregion // Leader-Unit Assignment

        #region State Management

        /// <summary>
        /// Completely wipes all game state for loading new data.
        /// </summary>
        public void ClearAll()
        {
            try
            {
                AppService.CaptureUiMessage("Clearing game state...");

                int unitsCleared = _combatUnits.Count;
                int leadersCleared = _leaders.Count;

                _combatUnits.Clear();
                _leaders.Clear();

                CurrentCampaignData = null;
                CurrentScenarioData = null;

                AppService.CaptureUiMessage($"Cleared {unitsCleared} units and {leadersCleared} leaders");

                // Optional: Force garbage collection for large states
                if (unitsCleared + leadersCleared > 100)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ClearAll), ex);
                throw; // Re-throw as this is critical
            }
        }

        /// <summary>
        /// Rebuilds transient caches after loading from snapshot.
        /// </summary>
        public void RebuildTransientCaches()
        {
            try
            {
                // Rebuild Leader ↔ Unit linkage
                foreach (var leader in _leaders.Values)
                {
                    if (string.IsNullOrEmpty(leader.UnitID))
                        continue;

                    if (_combatUnits.TryGetValue(leader.UnitID, out var unit))
                    {
                        if (!leader.IsAssigned)
                        {
                            leader.AssignToUnit(leader.UnitID);
                        }
                        unit.LeaderID = leader.LeaderID;
                    }
                    else
                    {
                        leader.UnassignFromUnit();
                    }
                }

                // Rebuild air unit attachments for airbases
                foreach (var facility in _combatUnits.Values.Where(u => u.IsBase && u.FacilityType == FacilityType.Airbase))
                {
                    facility.ClearAllAirUnits();

                    var attachedIds = facility.AttachedUnitIDs;
                    if (attachedIds?.Count > 0)
                    {
                        int reattached = 0;
                        foreach (string unitId in attachedIds)
                        {
                            if (!string.IsNullOrEmpty(unitId) && _combatUnits.TryGetValue(unitId, out var airUnit))
                            {
                                if (facility.AddAirUnit(airUnit))
                                    reattached++;
                            }
                        }

                        if (reattached > 0)
                            AppService.CaptureUiMessage($"Reattached {reattached} air units to {facility.UnitName}");
                    }
                }

                AppService.CaptureUiMessage("Game caches rebuilt successfully");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RebuildTransientCaches), e);
                throw;
            }
        }

        #endregion // State Management

        #region Helper Methods

        /// <summary>
        /// Validates an entity is not null.
        /// </summary>
        private bool ValidateEntity<T>(T entity, string paramName) where T : class
        {
            if (entity == null)
            {
                AppService.HandleException(CLASS_NAME, "ValidateEntity",
                    new ArgumentNullException(paramName));
                return false;
            }
            return true;
        }

        #endregion // Helper Methods

        #region Static Database Helpers

        /// <summary>
        /// Checks if all required databases are initialized.
        /// </summary>
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

        /// <summary>
        /// Retrieves a combat unit template by its unique identifier.
        /// </summary>
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

        #endregion // Static Database Helpers
    }
}