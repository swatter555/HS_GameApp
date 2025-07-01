using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Interface for objects that need reference resolution after deserialization.
    /// Implements the two-phase loading pattern used throughout the Hammer and Sickle codebase.
    /// </summary>
    public interface IResolvableReferences
    {
        /// <summary>
        /// Gets the list of unresolved reference IDs that need to be resolved.
        /// </summary>
        /// <returns>Collection of object IDs that this object references</returns>
        IReadOnlyList<string> GetUnresolvedReferenceIDs();

        /// <summary>
        /// Resolves object references using the provided data manager.
        /// Called after all objects have been deserialized.
        /// </summary>
        /// <param name="manager">Game data manager containing all loaded objects</param>
        void ResolveReferences(GameDataManager manager);

        /// <summary>
        /// Checks if this object has unresolved references that need resolution.
        /// </summary>
        /// <returns>True if ResolveReferences() needs to be called</returns>
        bool HasUnresolvedReferences();
    }

    /// <summary>
    /// Metadata header for save files containing version and validation information.
    /// </summary>
    [Serializable]
    public class GameDataHeader : ISerializable
    {
        public int Version { get; set; }
        public DateTime SaveTime { get; set; }
        public string GameVersion { get; set; }
        public int CombatUnitCount { get; set; }
        public int LeaderCount { get; set; }
        public int WeaponProfileCount { get; set; }
        public int UnitProfileCount { get; set; }
        public int FacilityCount { get; set; }
        public string Checksum { get; set; }

        public GameDataHeader()
        {
            Version = 1;
            SaveTime = DateTime.UtcNow;
            GameVersion = UnityEngine.Application.version;
        }

        protected GameDataHeader(SerializationInfo info, StreamingContext context)
        {
            Version = info.GetInt32(nameof(Version));
            SaveTime = info.GetDateTime(nameof(SaveTime));
            GameVersion = info.GetString(nameof(GameVersion));
            CombatUnitCount = info.GetInt32(nameof(CombatUnitCount));
            LeaderCount = info.GetInt32(nameof(LeaderCount));
            WeaponProfileCount = info.GetInt32(nameof(WeaponProfileCount));
            UnitProfileCount = info.GetInt32(nameof(UnitProfileCount));

            // Handle backward compatibility
            try
            {
                FacilityCount = info.GetInt32(nameof(FacilityCount));
            }
            catch (SerializationException)
            {
                FacilityCount = 0; // Default for older save files
            }

            Checksum = info.GetString(nameof(Checksum));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Version), Version);
            info.AddValue(nameof(SaveTime), SaveTime);
            info.AddValue(nameof(GameVersion), GameVersion);
            info.AddValue(nameof(CombatUnitCount), CombatUnitCount);
            info.AddValue(nameof(LeaderCount), LeaderCount);
            info.AddValue(nameof(WeaponProfileCount), WeaponProfileCount);
            info.AddValue(nameof(UnitProfileCount), UnitProfileCount);
            info.AddValue(nameof(FacilityCount), FacilityCount);
            info.AddValue(nameof(Checksum), Checksum);
        }
    }

 /*───────────────────────────────────────────────────────────────────────────────
 GameDataManager  —  centralized repository for all game object lifecycle
 ────────────────────────────────────────────────────────────────────────────────
 Overview
 ════════
 The **GameDataManager** serves as the unified registry and persistence layer for
 all Hammer & Sickle game objects. This singleton manages the complete lifecycle
 of CombatUnits, Leaders, WeaponSystemProfiles, and UnitProfiles, implementing
 sophisticated two-phase loading with automatic reference resolution.

 Major Responsibilities
 ══════════════════════
 • Centralized object registry with efficient ID-based lookups
     - Thread-safe registration and retrieval for all game object types
     - Duplicate prevention and collision detection during registration
 • Two-phase serialization with automatic reference resolution
     - Phase 1: Deserialize all objects and store unresolved reference IDs
     - Phase 2: Reconnect object relationships using stored IDs and lookup tables
 • Comprehensive save/load operations with data validation
     - Binary serialization with versioned headers and metadata
     - Automatic backup creation and graceful recovery from load failures
 • Thread-safe operations with ReaderWriterLockSlim
     - Multiple concurrent readers, exclusive writers for data integrity
     - Recursive lock support for complex nested operations
 • Data integrity validation and error recovery
     - Bidirectional relationship verification (leader ↔ unit assignments)
     - Facility management consistency checks (airbase attachments, etc.)
     - Orphaned reference detection and reporting
 • Change tracking and dirty state management
     - Efficient save optimization by tracking modified objects only
     - Integration with file system paths via AppService

 Design Highlights
 ═════════════════
 • **Singleton Architecture**: Thread-safe lazy initialization with double-checked
   locking pattern ensures single instance across application lifecycle.
 • **IResolvableReferences Pattern**: Objects implementing this interface are
   automatically tracked during deserialization and resolved in Phase 2.
 • **Composite Key System**: WeaponSystemProfiles and UnitProfiles use compound
   keys (Type_Nationality) to support multiple variants per entity type.
 • **Facility Relationship Validation**: Enhanced integrity checking for complex
   base-to-unit relationships, especially airbase attachments and HQ assignments.
 • **Graceful Degradation**: Load operations attempt partial recovery when
   possible, with comprehensive error logging via AppService.HandleException.
 • **Versioned Persistence**: GameDataHeader enables backward compatibility
   checks and migration paths for future save format changes.

 Public-Method Reference
 ═══════════════════════
   ── Registration Interface ──────────────────────────────────────────────────
   RegisterCombatUnit(unit)           Registers unit with automatic facility tracking.
   RegisterLeader(leader)             Registers leader with reference resolution support.
   RegisterWeaponProfile(profile)     Registers weapon template (allows overwriting).
   RegisterUnitProfile(profile)       Registers unit template (allows overwriting).

   ── Retrieval Interface ─────────────────────────────────────────────────────
   GetCombatUnit(unitId)              Retrieves unit by ID or null if not found.
   GetLeader(leaderId)                Retrieves leader by ID or null if not found.
   GetWeaponProfile(system, nation)   Retrieves weapon profile by compound key.
   GetUnitProfile(name, nation)       Retrieves unit profile by compound key.
   GetAllCombatUnits()                Returns read-only collection of all units.
   GetAllLeaders()                    Returns read-only collection of all leaders.

   ── Reference Resolution ────────────────────────────────────────────────────
   ResolveAllReferences()             Resolves pending object references (Phase 2).
   ValidateDataIntegrity()            Comprehensive validation with error reporting.

   ── Serialization Operations ────────────────────────────────────────────────
   SaveGameState(filePath)            Saves complete state with backup creation.
   LoadGameState(filePath)            Loads complete state with reference resolution.
   SaveScenario(scenarioName)         Convenience save to scenario storage folder.
   LoadScenario(scenarioName)         Convenience load with auto-validation.

   ── Lifecycle Management ────────────────────────────────────────────────────
   MarkDirty(objectId)                Marks object as needing save (change tracking).
   ClearAll()                         Resets manager to empty state (thread-safe).
   Dispose()                          Releases resources and optionally auto-saves.

   ── Diagnostic Properties ───────────────────────────────────────────────────
   TotalObjectCount                   Total registered objects across all types.
   UnresolvedReferenceCount           Objects still awaiting reference resolution.
   HasUnsavedChanges                  Whether any objects are marked dirty.
   ObjectCounts                       Tuple with counts by type (includes facilities).

 Two-Phase Loading Architecture
 ══════════════════════════════
 The GameDataManager implements the sophisticated reference resolution pattern
 used throughout Hammer & Sickle for handling complex object relationships:

   **Phase 1 - Deserialization**: All objects are created from the save file.
   Objects with references store IDs in temporary collections rather than
   direct object references. Objects implementing IResolvableReferences are
   automatically tracked in _unresolvedObjects list.

   **Phase 2 - Reference Resolution**: ResolveAllReferences() iterates through
   tracked objects, calling ResolveReferences(this) to reconnect relationships
   using the manager's lookup methods. Objects are removed from the unresolved
   list only after successful resolution.

 Thread Safety Model
 ═══════════════════
 ReaderWriterLockSlim enables high-performance concurrent access:

   • **Read Operations**: Multiple threads can simultaneously query data
     (GetCombatUnit, GetLeader, property getters, validation checks)
   • **Write Operations**: Exclusive access for registration, loading, clearing
     (RegisterCombatUnit, LoadGameState, ClearAll, reference resolution)
   • **Recursive Support**: LockRecursionPolicy.SupportsRecursion handles
     nested operations during complex deserialization scenarios

 Compound Key Strategy
 ═════════════════════
 Profiles use composite identifiers to support nationality-specific variants:

   • **Weapon Profiles**: "{WeaponSystem}_{Nationality}" (e.g., "T80B_Soviet")
   • **Unit Profiles**: "{ProfileName}_{Nationality}" (e.g., "Guards_Soviet")

 This enables shared templates while maintaining regional differences in
 equipment specifications, organizational structures, and combat capabilities.

 Data Integrity Validation
 ═════════════════════════
 ValidateDataIntegrity() performs comprehensive consistency checks:

   • **Bidirectional Relationships**: Leader assignment cross-references
   • **Facility Consistency**: Base classification vs FacilityManager type
   • **Airbase Attachments**: Unit type validation for attached aircraft
   • **Reference Completeness**: Detection of orphaned or missing references
   • **Profile Validation**: Null checks for required template objects

 Save File Format
 ════════════════
 Binary serialization with structured layout:

   1. **GameDataHeader**: Metadata with version, timestamps, object counts, checksum
   2. **Combat Units Dictionary**: All CombatUnit objects with IDs as keys
   3. **Leaders Dictionary**: All Leader objects with IDs as keys  
   4. **Weapon Profiles Dictionary**: All WeaponSystemProfile templates
   5. **Unit Profiles Dictionary**: All IntelProfile templates

 Backup creation and validation ensure data safety during save operations.

 ───────────────────────────────────────────────────────────────────────────────
 KEEP THIS COMMENT BLOCK IN SYNC WITH PUBLIC API CHANGES!
 ───────────────────────────────────────────────────────────────────────────── */
    public class GameDataManager : IDisposable
    {
        #region Constants

        private const string CLASS_NAME = nameof(GameDataManager);
        private const int CURRENT_SAVE_VERSION = 1;
        private const string SAVE_FILE_EXTENSION = ".sce";
        private const string BACKUP_FILE_EXTENSION = ".bak";

        #endregion // Constants


        #region Singleton

        private static GameDataManager _instance;
        private static readonly object _instanceLock = new ();

        /// <summary>
        /// Gets the singleton instance of the GameDataManager.
        /// Thread-safe lazy initialization.
        /// </summary>
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

        // Thread synchronization
        private readonly ReaderWriterLockSlim _dataLock = new (LockRecursionPolicy.SupportsRecursion);

        // Object registries
        private readonly Dictionary<string, CombatUnit> _combatUnits = new();
        private readonly Dictionary<string, Leader> _leaders = new();
        private readonly Dictionary<string, WeaponSystemProfile> _weaponProfiles = new();
        private readonly Dictionary<string, IntelProfile> _unitProfiles = new();

        // State tracking
        private readonly HashSet<string> _dirtyObjects = new ();
        private readonly List<IResolvableReferences> _unresolvedObjects = new();

        // Lifecycle management
        private bool _isDisposed = false;

        #endregion // Fields


        #region Properties

        /// <summary>
        /// Gets the total number of registered objects across all types.
        /// </summary>
        public int TotalObjectCount
        {
            get
            {
                _dataLock.EnterReadLock();
                try
                {
                    return _combatUnits.Count + _leaders.Count + _weaponProfiles.Count +
                           _unitProfiles.Count;
                }
                finally
                {
                    _dataLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the number of objects with unresolved references.
        /// </summary>
        public int UnresolvedReferenceCount
        {
            get
            {
                _dataLock.EnterReadLock();
                try
                {
                    return _unresolvedObjects.Count;
                }
                finally
                {
                    _dataLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets whether there are unsaved changes to any registered objects.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get
            {
                _dataLock.EnterReadLock();
                try
                {
                    return _dirtyObjects.Count > 0;
                }
                finally
                {
                    _dataLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the count of each object type for diagnostic purposes.
        /// </summary>
        public (int CombatUnits, int Leaders, int WeaponProfiles, int UnitProfiles, int Facilities) ObjectCounts
        {
            get
            {
                _dataLock.EnterReadLock();
                try
                {
                    int facilityCount = _combatUnits.Values.Count(unit => unit.IsBase);
                    return (_combatUnits.Count, _leaders.Count, _weaponProfiles.Count,
                            _unitProfiles.Count, facilityCount);
                }
                finally
                {
                    _dataLock.ExitReadLock();
                }
            }
        }

        #endregion // Properties


        #region Constructor

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        public GameDataManager()
        {
            // Initialize empty state
        }

        #endregion // Constructor


        #region Registration Methods

        /// <summary>
        /// Registers a combat unit with the data manager.
        /// </summary>
        /// <param name="unit">The combat unit to register</param>
        /// <returns>True if registration was successful</returns>
        public bool RegisterCombatUnit(CombatUnit unit)
        {
            if (unit == null)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterCombatUnit),
                    new ArgumentNullException(nameof(unit)));
                return false;
            }

            try
            {
                _dataLock.EnterWriteLock();

                if (_combatUnits.ContainsKey(unit.UnitID))
                {
                    AppService.HandleException(CLASS_NAME, nameof(RegisterCombatUnit),
                        new InvalidOperationException($"Combat unit with ID {unit.UnitID} already registered"));
                    return false;
                }

                _combatUnits[unit.UnitID] = unit;
                MarkDirty(unit.UnitID);

                // Track for reference resolution if needed
                if (unit is IResolvableReferences resolvable && resolvable.HasUnresolvedReferences())
                {
                    _unresolvedObjects.Add(resolvable);
                }

                // Also track FacilityManager if it has unresolved references
                if (unit.FacilityManager != null &&
                    unit.FacilityManager is IResolvableReferences facilityResolvable &&
                    facilityResolvable.HasUnresolvedReferences())
                {
                    _unresolvedObjects.Add(facilityResolvable);
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterCombatUnit), e);
                return false;
            }
            finally
            {
                _dataLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers a leader with the data manager.
        /// </summary>
        /// <param name="leader">The leader to register</param>
        /// <returns>True if registration was successful</returns>
        public bool RegisterLeader(Leader leader)
        {
            if (leader == null)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterLeader),
                    new ArgumentNullException(nameof(leader)));
                return false;
            }

            try
            {
                _dataLock.EnterWriteLock();

                if (_leaders.ContainsKey(leader.LeaderID))
                {
                    AppService.HandleException(CLASS_NAME, nameof(RegisterLeader),
                        new InvalidOperationException($"Leader with ID {leader.LeaderID} already registered"));
                    return false;
                }

                _leaders[leader.LeaderID] = leader;
                MarkDirty(leader.LeaderID);

                // Track for reference resolution if needed
                if (leader is IResolvableReferences resolvable && resolvable.HasUnresolvedReferences())
                {
                    _unresolvedObjects.Add(resolvable);
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterLeader), e);
                return false;
            }
            finally
            {
                _dataLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers a weapon system profile with the data manager.
        /// </summary>
        /// <param name="profile">The weapon system profile to register</param>
        /// <returns>True if registration was successful</returns>
        public bool RegisterWeaponProfile(WeaponSystemProfile profile)
        {
            if (profile == null)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterWeaponProfile),
                    new ArgumentNullException(nameof(profile)));
                return false;
            }

            try
            {
                _dataLock.EnterWriteLock();

                string profileId = $"{profile.WeaponSystemID}_{profile.Nationality}";
                if (_weaponProfiles.ContainsKey(profileId))
                {
                    // Allow overwriting weapon profiles as they're shared templates
                    _weaponProfiles[profileId] = profile;
                }
                else
                {
                    _weaponProfiles[profileId] = profile;
                }

                MarkDirty(profileId);
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterWeaponProfile), e);
                return false;
            }
            finally
            {
                _dataLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers a unit profile with the data manager.
        /// </summary>
        /// <param name="profile">The unit profile to register</param>
        /// <returns>True if registration was successful</returns>
        public bool RegisterUnitProfile(IntelProfile profile)
        {
            if (profile == null)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterUnitProfile),
                    new ArgumentNullException(nameof(profile)));
                return false;
            }

            try
            {
                _dataLock.EnterWriteLock();

                string profileId = $"{profile.IntelProfileID}_{profile.Nationality}";
                if (_unitProfiles.ContainsKey(profileId))
                {
                    // Allow overwriting unit profiles as they're shared templates
                    _unitProfiles[profileId] = profile;
                }
                else
                {
                    _unitProfiles[profileId] = profile;
                }

                MarkDirty(profileId);
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RegisterUnitProfile), e);
                return false;
            }
            finally
            {
                _dataLock.ExitWriteLock();
            }
        }

        #endregion // Registration Methods


        #region Retrieval Methods

        /// <summary>
        /// Gets a combat unit by its unique identifier.
        /// </summary>
        /// <param name="unitId">The unit ID to lookup</param>
        /// <returns>The combat unit if found, null otherwise</returns>
        public CombatUnit GetCombatUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return null;

            try
            {
                _dataLock.EnterReadLock();
                return _combatUnits.TryGetValue(unitId, out CombatUnit unit) ? unit : null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetCombatUnit), e);
                return null;
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets a leader by their unique identifier.
        /// </summary>
        /// <param name="leaderId">The leader ID to lookup</param>
        /// <returns>The leader if found, null otherwise</returns>
        public Leader GetLeader(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId)) return null;

            try
            {
                _dataLock.EnterReadLock();
                return _leaders.TryGetValue(leaderId, out Leader leader) ? leader : null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetLeader), e);
                return null;
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets a weapon system profile by weapon system and nationality.
        /// </summary>
        /// <param name="weaponSystem">The weapon system type</param>
        /// <param name="nationality">The nationality</param>
        /// <returns>The weapon profile if found, null otherwise</returns>
        public WeaponSystemProfile GetWeaponProfile(WeaponSystems weaponSystem, Nationality nationality)
        {
            try
            {
                _dataLock.EnterReadLock();
                string profileId = $"{weaponSystem}_{nationality}";
                return _weaponProfiles.TryGetValue(profileId, out WeaponSystemProfile profile) ? profile : null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetWeaponProfile), e);
                return null;
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets a unit profile by name and nationality.
        /// </summary>
        /// <param name="profileName">The profile name</param>
        /// <param name="nationality">The nationality</param>
        /// <returns>The unit profile if found, null otherwise</returns>
        public IntelProfile GetUnitProfile(string profileName, Nationality nationality)
        {
            if (string.IsNullOrEmpty(profileName)) return null;

            try
            {
                _dataLock.EnterReadLock();
                string profileId = $"{profileName}_{nationality}";
                return _unitProfiles.TryGetValue(profileId, out IntelProfile profile) ? profile : null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetUnitProfile), e);
                return null;
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets all registered combat units.
        /// </summary>
        /// <returns>Read-only collection of all combat units</returns>
        public IReadOnlyCollection<CombatUnit> GetAllCombatUnits()
        {
            try
            {
                _dataLock.EnterReadLock();
                return _combatUnits.Values.ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAllCombatUnits), e);
                return new List<CombatUnit>();
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets all registered leaders.
        /// </summary>
        /// <returns>Read-only collection of all leaders</returns>
        public IReadOnlyCollection<Leader> GetAllLeaders()
        {
            try
            {
                _dataLock.EnterReadLock();
                return _leaders.Values.ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAllLeaders), e);
                return new List<Leader>();
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        #endregion // Retrieval Methods


        #region Reference Resolution

        /// <summary>
        /// Resolves all pending object references after deserialization.
        /// This method implements the second phase of the two-phase loading pattern.
        /// </summary>
        /// <returns>Number of objects that had references resolved</returns>
        public int ResolveAllReferences()
        {
            try
            {
                _dataLock.EnterWriteLock();

                int resolvedCount = 0;
                var remainingUnresolved = new List<IResolvableReferences>();

                foreach (var unresolvedObject in _unresolvedObjects)
                {
                    try
                    {
                        if (unresolvedObject.HasUnresolvedReferences())
                        {
                            unresolvedObject.ResolveReferences(this);

                            if (!unresolvedObject.HasUnresolvedReferences())
                            {
                                resolvedCount++;
                            }
                            else
                            {
                                remainingUnresolved.Add(unresolvedObject);
                            }
                        }
                        else
                        {
                            resolvedCount++; // Already resolved
                        }
                    }
                    catch (Exception e)
                    {
                        AppService.HandleException(CLASS_NAME, nameof(ResolveAllReferences), e);
                        remainingUnresolved.Add(unresolvedObject); // Keep for retry
                    }
                }

                _unresolvedObjects.Clear();
                _unresolvedObjects.AddRange(remainingUnresolved);

                return resolvedCount;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveAllReferences), e);
                return 0;
            }
            finally
            {
                _dataLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Validates data integrity by checking for missing references and other issues.
        /// Enhanced to validate facility relationships and nested references.
        /// </summary>
        /// <returns>List of validation errors found</returns>
        public List<string> ValidateDataIntegrity()
        {
            var errors = new List<string>();

            try
            {
                _dataLock.EnterReadLock();

                // Validate facility manager parent relationships
                foreach (var unit in _combatUnits.Values)
                {
                    if (unit.IsBase && unit.FacilityManager != null)
                    {
                        // Validate that base units have proper facility types
                        switch (unit.Classification)
                        {
                            case UnitClassification.HQ:
                                if (unit.FacilityManager.FacilityType != FacilityType.HQ)
                                    errors.Add($"HQ unit {unit.UnitName} has incorrect facility type: {unit.FacilityManager.FacilityType}");
                                break;
                            case UnitClassification.DEPOT:
                                if (unit.FacilityManager.FacilityType != FacilityType.SupplyDepot)
                                    errors.Add($"Depot unit {unit.UnitName} has incorrect facility type: {unit.FacilityManager.FacilityType}");
                                break;
                            case UnitClassification.AIRB:
                                if (unit.FacilityManager.FacilityType != FacilityType.Airbase)
                                    errors.Add($"Airbase unit {unit.UnitName} has incorrect facility type: {unit.FacilityManager.FacilityType}");
                                break;
                        }

                        // Validate airbase attachments more thoroughly
                        if (unit.FacilityManager.FacilityType == FacilityType.Airbase)
                        {
                            foreach (var attachedUnit in unit.FacilityManager.AirUnitsAttached)
                            {
                                if (!_combatUnits.ContainsKey(attachedUnit.UnitID))
                                {
                                    errors.Add($"Airbase {unit.UnitName} has attached unit {attachedUnit.UnitID} not in registry");
                                }
                                else
                                {
                                    var registeredUnit = _combatUnits[attachedUnit.UnitID];
                                    if (registeredUnit != attachedUnit)
                                    {
                                        errors.Add($"Airbase {unit.UnitName} attached unit reference mismatch for {attachedUnit.UnitID}");
                                    }
                                    if (attachedUnit.UnitType != UnitType.AirUnit)
                                    {
                                        errors.Add($"Airbase {unit.UnitName} has non-air unit {attachedUnit.UnitName} attached");
                                    }
                                }
                            }
                        }
                    }
                    else if (unit.IsBase && unit.FacilityManager == null)
                    {
                        errors.Add($"Base unit {unit.UnitName} is marked as base but has null FacilityManager");
                    }
                    else if (!unit.IsBase && unit.FacilityManager != null &&
                             unit.FacilityManager.FacilityType != FacilityType.HQ) // HQ can be on non-base units
                    {
                        errors.Add($"Non-base unit {unit.UnitName} has FacilityManager with type {unit.FacilityManager.FacilityType}");
                    }
                }

                // Check for unresolved references
                if (_unresolvedObjects.Count > 0)
                {
                    errors.Add($"{_unresolvedObjects.Count} objects have unresolved references");

                    // Log details about unresolved references
                    foreach (var obj in _unresolvedObjects)
                    {
                        var unresolvedIds = obj.GetUnresolvedReferenceIDs();
                        if (unresolvedIds.Count > 0)
                        {
                            errors.Add($"Object has unresolved references: {string.Join(", ", unresolvedIds)}");
                        }
                    }
                }

                // Check for orphaned leader assignments
                foreach (var leader in _leaders.Values)
                {
                    if (leader.IsAssigned && !string.IsNullOrEmpty(leader.UnitID))
                    {
                        if (!_combatUnits.ContainsKey(leader.UnitID))
                        {
                            errors.Add($"Leader {leader.Name} assigned to non-existent unit {leader.UnitID}");
                        }
                        else
                        {
                            // Verify bidirectional relationship
                            var unit = _combatUnits[leader.UnitID];
                            if (unit.CommandingOfficer == null || unit.CommandingOfficer.LeaderID != leader.LeaderID)
                            {
                                errors.Add($"Leader {leader.Name} thinks it's assigned to {leader.UnitID} but unit doesn't reference it");
                            }
                        }
                    }
                }

                // Check for missing commanding officers
                foreach (var unit in _combatUnits.Values)
                {
                    if (unit.CommandingOfficer != null)
                    {
                        bool leaderExists = _leaders.Values.Any(l => l.LeaderID == unit.CommandingOfficer.LeaderID);
                        if (!leaderExists)
                        {
                            errors.Add($"Unit {unit.UnitName} has commanding officer not in leader registry");
                        }
                        else
                        {
                            // Verify bidirectional relationship
                            var leader = unit.CommandingOfficer;
                            if (!leader.IsAssigned || leader.UnitID != unit.UnitID)
                            {
                                errors.Add($"Unit {unit.UnitName} references leader {leader.Name} but leader doesn't think it's assigned to this unit");
                            }
                        }
                    }

                    // Validate facility relationships for base units
                    if (unit.IsBase && unit.FacilityManager != null)
                    {
                        // Check facility manager consistency
                        if (unit.FacilityManager.FacilityType == FacilityType.Airbase)
                        {
                            foreach (var attachedUnit in unit.FacilityManager.AirUnitsAttached)
                            {
                                if (!_combatUnits.ContainsKey(attachedUnit.UnitID))
                                {
                                    errors.Add($"Airbase {unit.UnitName} has attached unit {attachedUnit.UnitID} not in registry");
                                }
                                else if (attachedUnit.UnitType != UnitType.AirUnit)
                                {
                                    errors.Add($"Airbase {unit.UnitName} has non-air unit {attachedUnit.UnitName} attached");
                                }
                            }
                        }
                    }

                    // Check for inconsistent leader assignment flags
                    if (unit.IsLeaderAssigned && unit.CommandingOfficer == null)
                    {
                        errors.Add($"Unit {unit.UnitName} has IsLeaderAssigned=true but no CommandingOfficer");
                    }
                    else if (!unit.IsLeaderAssigned && unit.CommandingOfficer != null)
                    {
                        errors.Add($"Unit {unit.UnitName} has CommandingOfficer but IsLeaderAssigned=false");
                    }
                }

                // Validate profile references
                foreach (var unit in _combatUnits.Values)
                {
                    if (unit.DeployedProfile == null)
                    {
                        errors.Add($"Unit {unit.UnitName} has null DeployedProfile");
                    }

                    if (unit.IntelProfile == null)
                    {
                        errors.Add($"Unit {unit.UnitName} has null UnitProfile");
                    }
                }

            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateDataIntegrity), e);
                errors.Add($"Validation failed with exception: {e.Message}");
            }
            finally
            {
                _dataLock.ExitReadLock();
            }

            return errors;
        }

        #endregion // Reference Resolution


        #region Serialization Operations

        /// <summary>
        /// Saves the complete game state to the specified file path.
        /// Creates a backup of any existing file before overwriting.
        /// </summary>
        /// <param name="filePath">Target file path for the save</param>
        /// <returns>True if save was successful</returns>
        public bool SaveGameState(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                AppService.HandleException(CLASS_NAME, nameof(SaveGameState),
                    new ArgumentException("File path cannot be null or empty"));
                return false;
            }

            try
            {
                _dataLock.EnterReadLock();

                // Ensure the file has the correct extension
                if (!filePath.EndsWith(SAVE_FILE_EXTENSION))
                {
                    filePath += SAVE_FILE_EXTENSION;
                }

                // Create backup if file exists
                if (File.Exists(filePath))
                {
                    string backupPath = Path.ChangeExtension(filePath, BACKUP_FILE_EXTENSION);
                    File.Copy(filePath, backupPath, true);
                }

                // Create header with metadata
                int facilityCount = _combatUnits.Values.Count(unit => unit.IsBase);
                var header = new GameDataHeader
                {
                    CombatUnitCount = _combatUnits.Count,
                    LeaderCount = _leaders.Count,
                    WeaponProfileCount = _weaponProfiles.Count,
                    UnitProfileCount = _unitProfiles.Count,
                    FacilityCount = facilityCount,
                    Checksum = CalculateChecksum()
                };

                // Serialize to file
                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    // Write header first
                    formatter.Serialize(stream, header);

                    // Write object collections
                    formatter.Serialize(stream, _combatUnits);
                    formatter.Serialize(stream, _leaders);
                    formatter.Serialize(stream, _weaponProfiles);
                    formatter.Serialize(stream, _unitProfiles);
                }

                // Clear dirty flags after successful save
                _dirtyObjects.Clear();

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SaveGameState), e);
                return false;
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Loads the complete game state from the specified file path.
        /// Automatically resolves object references after loading.
        /// </summary>
        /// <param name="filePath">Source file path for the load</param>
        /// <returns>True if load was successful</returns>
        public bool LoadGameState(string filePath)
        {
            // TODO: Consider implementing partial recovery when object counts don't match the header, rather than just logging the issue.

            if (string.IsNullOrEmpty(filePath))
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadGameState),
                    new ArgumentException("File path cannot be null or empty"));
                return false;
            }

            try
            {
                // Ensure the file has the correct extension
                if (!filePath.EndsWith(SAVE_FILE_EXTENSION))
                {
                    filePath += SAVE_FILE_EXTENSION;
                }

                if (!File.Exists(filePath))
                {
                    AppService.HandleException(CLASS_NAME, nameof(LoadGameState),
                        new FileNotFoundException($"Save file not found: {filePath}"));
                    return false;
                }

                _dataLock.EnterWriteLock();

                // Clear existing data
                ClearAllInternal();

                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // Read header first
                    var header = (GameDataHeader)formatter.Deserialize(stream);

                    // Validate version compatibility
                    if (header.Version > CURRENT_SAVE_VERSION)
                    {
                        AppService.HandleException(CLASS_NAME, nameof(LoadGameState),
                            new NotSupportedException($"Save file version {header.Version} is newer than supported version {CURRENT_SAVE_VERSION}"));
                        return false;
                    }

                    // Load object collections
                    var combatUnits = (Dictionary<string, CombatUnit>)formatter.Deserialize(stream);
                    var leaders = (Dictionary<string, Leader>)formatter.Deserialize(stream);
                    var weaponProfiles = (Dictionary<string, WeaponSystemProfile>)formatter.Deserialize(stream);
                    var unitProfiles = (Dictionary<string, IntelProfile>)formatter.Deserialize(stream);

                    // Transfer to internal collections and track resolvable objects
                    foreach (var kvp in combatUnits)
                    {
                        _combatUnits[kvp.Key] = kvp.Value;
                        if (kvp.Value is IResolvableReferences resolvable && resolvable.HasUnresolvedReferences())
                        {
                            _unresolvedObjects.Add(resolvable);
                        }

                        // Also track FacilityManager if it has unresolved references
                        if (kvp.Value.FacilityManager != null &&
                            kvp.Value.FacilityManager is IResolvableReferences facilityResolvable &&
                            facilityResolvable.HasUnresolvedReferences())
                        {
                            _unresolvedObjects.Add(facilityResolvable);
                        }
                    }

                    foreach (var kvp in leaders)
                    {
                        _leaders[kvp.Key] = kvp.Value;
                        if (kvp.Value is IResolvableReferences resolvable && resolvable.HasUnresolvedReferences())
                        {
                            _unresolvedObjects.Add(resolvable);
                        }
                    }

                    foreach (var kvp in weaponProfiles)
                        _weaponProfiles[kvp.Key] = kvp.Value;

                    foreach (var kvp in unitProfiles)
                        _unitProfiles[kvp.Key] = kvp.Value;

                    // Validate loaded data counts match header
                    int loadedFacilityCount = _combatUnits.Values.Count(unit => unit.IsBase);
                    if (_combatUnits.Count != header.CombatUnitCount ||
                        _leaders.Count != header.LeaderCount ||
                        _weaponProfiles.Count != header.WeaponProfileCount ||
                        _unitProfiles.Count != header.UnitProfileCount ||
                        (header.FacilityCount > 0 && loadedFacilityCount != header.FacilityCount))
                    {
                        AppService.HandleException(CLASS_NAME, nameof(LoadGameState),
                            new InvalidDataException("Loaded object counts do not match header metadata"));
                    }
                }

                // Clear dirty flags - loaded data is considered clean
                _dirtyObjects.Clear();

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadGameState), e);

                // Clear partially loaded data on failure
                _dataLock.EnterWriteLock();
                try
                {
                    ClearAllInternal();
                }
                finally
                {
                    _dataLock.ExitWriteLock();
                }

                return false;
            }
            finally
            {
                if (_dataLock.IsWriteLockHeld)
                {
                    _dataLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Saves a scenario with the given name to the scenario storage folder.
        /// </summary>
        /// <param name="scenarioName">Name of the scenario</param>
        /// <returns>True if save was successful</returns>
        public bool SaveScenario(string scenarioName)
        {
            if (string.IsNullOrEmpty(scenarioName))
            {
                AppService.HandleException(CLASS_NAME, nameof(SaveScenario),
                    new ArgumentException("Scenario name cannot be null or empty"));
                return false;
            }

            try
            {
                string scenarioPath = Path.Combine(AppService.ScenarioStorageFolderPath,
                    scenarioName + SAVE_FILE_EXTENSION);
                return SaveGameState(scenarioPath);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SaveScenario), e);
                return false;
            }
        }

        /// <summary>
        /// Loads a scenario with the given name from the scenario storage folder.
        /// </summary>
        /// <param name="scenarioName">Name of the scenario</param>
        /// <returns>True if load was successful</returns>
        public bool LoadScenario(string scenarioName)
        {
            if (string.IsNullOrEmpty(scenarioName))
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadScenario),
                    new ArgumentException("Scenario name cannot be null or empty"));
                return false;
            }

            try
            {
                string scenarioPath = Path.Combine(AppService.ScenarioStorageFolderPath,
                    scenarioName + SAVE_FILE_EXTENSION);

                bool loadResult = LoadGameState(scenarioPath);

                if (loadResult)
                {
                    // Resolve references after loading
                    int resolvedCount = ResolveAllReferences();

                    // Validate integrity
                    var validationErrors = ValidateDataIntegrity();
                    if (validationErrors.Count > 0)
                    {
                        foreach (var error in validationErrors)
                        {
                            AppService.HandleException(CLASS_NAME, nameof(LoadScenario),
                                new InvalidDataException($"Data integrity issue: {error}"), ExceptionSeverity.Minor);
                        }
                    }
                }

                return loadResult;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadScenario), e);
                return false;
            }
        }

        #endregion // Serialization Operations


        #region Lifecycle Management

        /// <summary>
        /// Marks an object as dirty (needing save).
        /// </summary>
        /// <param name="objectId">The ID of the object that changed</param>
        public void MarkDirty(string objectId)
        {
            if (string.IsNullOrEmpty(objectId)) return;

            try
            {
                _dataLock.EnterWriteLock();
                _dirtyObjects.Add(objectId);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(MarkDirty), e);
            }
            finally
            {
                // Ensure we always exit the lock, even if an exception occurs
                if (_dataLock.IsWriteLockHeld)
                {
                    _dataLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Clears all registered objects and resets the manager to empty state.
        /// </summary>
        public void ClearAll()
        {
            try
            {
                _dataLock.EnterWriteLock();
                ClearAllInternal();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ClearAll), e);
            }
            finally
            {
                _dataLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Internal method to clear all data. Assumes write lock is already held.
        /// </summary>
        private void ClearAllInternal()
        {
            _combatUnits.Clear();
            _leaders.Clear();
            _weaponProfiles.Clear();
            _unitProfiles.Clear();
            _dirtyObjects.Clear();
            _unresolvedObjects.Clear();
        }

        /// <summary>
        /// Calculates a simple checksum for data validation.
        /// Can be enhanced with more sophisticated algorithms as needed.
        /// </summary>
        /// <returns>Checksum string</returns>
        private string CalculateChecksum()
        {
            try
            {
                int checksum = _combatUnits.Count * 17 + _leaders.Count * 23 +
                              _weaponProfiles.Count * 31 + _unitProfiles.Count * 37;

                // Add facility count
                int facilityCount = _combatUnits.Values.Count(unit => unit.IsBase);
                checksum += facilityCount * 41;

                // Add some key data points for additional validation
                foreach (var unit in _combatUnits.Values)
                {
                    checksum += unit.UnitName.GetHashCode() / 1000; // Prevent overflow
                    if (unit.CommandingOfficer != null)
                    {
                        checksum += unit.CommandingOfficer.LeaderID.GetHashCode() / 1000;
                    }
                }

                foreach (var leader in _leaders.Values)
                {
                    checksum += leader.Name.GetHashCode() / 1000;
                }

                return Math.Abs(checksum).ToString("X8");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CalculateChecksum), e);
                return "ERROR";
            }
        }

        #endregion // Lifecycle Management


        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Save any unsaved changes before disposing
                        if (HasUnsavedChanges)
                        {
                            // Could implement auto-save here if desired
                        }

                        // Dispose the lock
                        _dataLock?.Dispose();

                        // Clear collections
                        ClearAllInternal();
                    }
                    catch (Exception e)
                    {
                        AppService.HandleException(CLASS_NAME, nameof(Dispose), e);
                    }
                }

                _isDisposed = true;
            }
        }

        #endregion // IDisposable Implementation
    }
}