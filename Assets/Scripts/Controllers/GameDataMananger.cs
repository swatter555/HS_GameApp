using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using HammerAndSickle.Core;
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
        public int LandBaseCount { get; set; }
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
            LandBaseCount = info.GetInt32(nameof(LandBaseCount));
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
            info.AddValue(nameof(LandBaseCount), LandBaseCount);
            info.AddValue(nameof(Checksum), Checksum);
        }
    }

    /// <summary>
    /// Comprehensive game data manager that handles all object lifecycle, serialization,
    /// and reference resolution for the Hammer and Sickle game state.
    /// 
    /// This manager serves as the central repository for all game objects and implements
    /// the established two-phase loading pattern for complex object relationships.
    /// 
    /// Key Features:
    /// - Centralized object registry with efficient ID-based lookups
    /// - Two-phase serialization with automatic reference resolution
    /// - Thread-safe operations with ReaderWriterLockSlim
    /// - Comprehensive error handling and data validation
    /// - Integration with AppService file management
    /// - Support for partial loading and graceful degradation
    /// 
    /// Usage Example:
    /// ```csharp
    /// var manager = GameDataManager.Instance;
    /// manager.RegisterCombatUnit(unit);
    /// manager.RegisterLeader(leader);
    /// unit.CommandingOfficer = leader; // Creates reference
    /// 
    /// manager.SaveGameState("scenario1.dat");
    /// manager.ClearAll();
    /// manager.LoadGameState("scenario1.dat"); // References automatically resolved
    /// ```
    /// </summary>
    public class GameDataManager : IDisposable
    {
        #region Constants

        private const string CLASS_NAME = nameof(GameDataManager);
        private const int CURRENT_SAVE_VERSION = 1;
        private const string SAVE_FILE_EXTENSION = ".dat";
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
        private readonly Dictionary<string, UnitProfile> _unitProfiles = new();
        private readonly Dictionary<string, LandBaseFacility> _landBases = new();

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
                           _unitProfiles.Count + _landBases.Count;
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
        public (int CombatUnits, int Leaders, int WeaponProfiles, int UnitProfiles, int LandBases) ObjectCounts
        {
            get
            {
                _dataLock.EnterReadLock();
                try
                {
                    return (_combatUnits.Count, _leaders.Count, _weaponProfiles.Count,
                            _unitProfiles.Count, _landBases.Count);
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
        private GameDataManager()
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterCombatUnit),
                    new ArgumentNullException(nameof(unit)));
                return false;
            }

            try
            {
                _dataLock.EnterWriteLock();

                if (_combatUnits.ContainsKey(unit.UnitID))
                {
                    ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterCombatUnit),
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

                return true;
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterCombatUnit), e);
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterLeader),
                    new ArgumentNullException(nameof(leader)));
                return false;
            }

            try
            {
                _dataLock.EnterWriteLock();

                if (_leaders.ContainsKey(leader.LeaderID))
                {
                    ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterLeader),
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterLeader), e);
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterWeaponProfile),
                    new ArgumentNullException(nameof(profile)));
                return false;
            }

            try
            {
                _dataLock.EnterWriteLock();

                string profileId = $"{profile.WeaponSystem}_{profile.Nationality}";
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterWeaponProfile), e);
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
        public bool RegisterUnitProfile(UnitProfile profile)
        {
            if (profile == null)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterUnitProfile),
                    new ArgumentNullException(nameof(profile)));
                return false;
            }

            try
            {
                _dataLock.EnterWriteLock();

                string profileId = $"{profile.UnitProfileID}_{profile.Nationality}";
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterUnitProfile), e);
                return false;
            }
            finally
            {
                _dataLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers a land base profile with the data manager.
        /// </summary>
        /// <param name="profile">The land base profile to register</param>
        /// <param name="baseId">Unique identifier for this base instance</param>
        /// <returns>True if registration was successful</returns>
        public bool RegisterLandBase(LandBaseFacility profile, string baseId)
        {
            if (profile == null)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterLandBase),
                    new ArgumentNullException(nameof(profile)));
                return false;
            }

            if (string.IsNullOrEmpty(baseId))
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterLandBase),
                    new ArgumentException("Base ID cannot be null or empty", nameof(baseId)));
                return false;
            }

            try
            {
                _dataLock.EnterWriteLock();

                if (_landBases.ContainsKey(baseId))
                {
                    ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterLandBase),
                        new InvalidOperationException($"Land base with ID {baseId} already registered"));
                    return false;
                }

                _landBases[baseId] = profile;
                MarkDirty(baseId);

                // Track for reference resolution if needed
                if (profile is IResolvableReferences resolvable && resolvable.HasUnresolvedReferences())
                {
                    _unresolvedObjects.Add(resolvable);
                }

                return true;
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(RegisterLandBase), e);
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(GetCombatUnit), e);
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(GetLeader), e);
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(GetWeaponProfile), e);
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
        public UnitProfile GetUnitProfile(string profileName, Nationality nationality)
        {
            if (string.IsNullOrEmpty(profileName)) return null;

            try
            {
                _dataLock.EnterReadLock();
                string profileId = $"{profileName}_{nationality}";
                return _unitProfiles.TryGetValue(profileId, out UnitProfile profile) ? profile : null;
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(GetUnitProfile), e);
                return null;
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets a land base profile by its unique identifier.
        /// </summary>
        /// <param name="baseId">The base ID to lookup</param>
        /// <returns>The land base profile if found, null otherwise</returns>
        public LandBaseFacility GetLandBase(string baseId)
        {
            if (string.IsNullOrEmpty(baseId)) return null;

            try
            {
                _dataLock.EnterReadLock();
                return _landBases.TryGetValue(baseId, out LandBaseFacility profile) ? profile : null;
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(GetLandBase), e);
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(GetAllCombatUnits), e);
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(GetAllLeaders), e);
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
                        ErrorHandler.HandleException(CLASS_NAME, nameof(ResolveAllReferences), e);
                        remainingUnresolved.Add(unresolvedObject); // Keep for retry
                    }
                }

                _unresolvedObjects.Clear();
                _unresolvedObjects.AddRange(remainingUnresolved);

                return resolvedCount;
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(ResolveAllReferences), e);
                return 0;
            }
            finally
            {
                _dataLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Validates data integrity by checking for missing references and other issues.
        /// </summary>
        /// <returns>List of validation errors found</returns>
        public List<string> ValidateDataIntegrity()
        {
            var errors = new List<string>();

            try
            {
                _dataLock.EnterReadLock();

                // Check for unresolved references
                if (_unresolvedObjects.Count > 0)
                {
                    errors.Add($"{_unresolvedObjects.Count} objects have unresolved references");
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
                    }
                }

                // Add more validation checks as needed...

            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(ValidateDataIntegrity), e);
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(SaveGameState),
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
                var header = new GameDataHeader
                {
                    CombatUnitCount = _combatUnits.Count,
                    LeaderCount = _leaders.Count,
                    WeaponProfileCount = _weaponProfiles.Count,
                    UnitProfileCount = _unitProfiles.Count,
                    LandBaseCount = _landBases.Count,
                    // Calculate simple checksum (can be enhanced)
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
                    formatter.Serialize(stream, _landBases);
                }

                // Clear dirty flags after successful save
                _dirtyObjects.Clear();

                return true;
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(SaveGameState), e);
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
            if (string.IsNullOrEmpty(filePath))
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(LoadGameState),
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
                    ErrorHandler.HandleException(CLASS_NAME, nameof(LoadGameState),
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
                        ErrorHandler.HandleException(CLASS_NAME, nameof(LoadGameState),
                            new NotSupportedException($"Save file version {header.Version} is newer than supported version {CURRENT_SAVE_VERSION}"));
                        return false;
                    }

                    // Load object collections
                    var combatUnits = (Dictionary<string, CombatUnit>)formatter.Deserialize(stream);
                    var leaders = (Dictionary<string, Leader>)formatter.Deserialize(stream);
                    var weaponProfiles = (Dictionary<string, WeaponSystemProfile>)formatter.Deserialize(stream);
                    var unitProfiles = (Dictionary<string, UnitProfile>)formatter.Deserialize(stream);
                    var landBases = (Dictionary<string, LandBaseFacility>)formatter.Deserialize(stream);

                    // Transfer to internal collections and track resolvable objects
                    foreach (var kvp in combatUnits)
                    {
                        _combatUnits[kvp.Key] = kvp.Value;
                        if (kvp.Value is IResolvableReferences resolvable && resolvable.HasUnresolvedReferences())
                        {
                            _unresolvedObjects.Add(resolvable);
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

                    foreach (var kvp in landBases)
                    {
                        _landBases[kvp.Key] = kvp.Value;
                        if (kvp.Value is IResolvableReferences resolvable && resolvable.HasUnresolvedReferences())
                        {
                            _unresolvedObjects.Add(resolvable);
                        }
                    }

                    // Validate loaded data counts match header
                    if (_combatUnits.Count != header.CombatUnitCount ||
                        _leaders.Count != header.LeaderCount ||
                        _weaponProfiles.Count != header.WeaponProfileCount ||
                        _unitProfiles.Count != header.UnitProfileCount ||
                        _landBases.Count != header.LandBaseCount)
                    {
                        ErrorHandler.HandleException(CLASS_NAME, nameof(LoadGameState),
                            new InvalidDataException("Loaded object counts do not match header metadata"));
                    }
                }

                // Clear dirty flags - loaded data is considered clean
                _dirtyObjects.Clear();

                return true;
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(LoadGameState), e);

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
                ErrorHandler.HandleException(CLASS_NAME, nameof(SaveScenario),
                    new ArgumentException("Scenario name cannot be null or empty"));
                return false;
            }

            try
            {
                string scenarioPath = Path.Combine(AppService.Instance.ScenarioStorageFolderPath,
                    scenarioName + SAVE_FILE_EXTENSION);
                return SaveGameState(scenarioPath);
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(SaveScenario), e);
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(LoadScenario),
                    new ArgumentException("Scenario name cannot be null or empty"));
                return false;
            }

            try
            {
                string scenarioPath = Path.Combine(AppService.Instance.ScenarioStorageFolderPath,
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
                            ErrorHandler.HandleException(CLASS_NAME, nameof(LoadScenario),
                                new InvalidDataException($"Data integrity issue: {error}"), ExceptionSeverity.Minor);
                        }
                    }
                }

                return loadResult;
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(LoadScenario), e);
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(MarkDirty), e);
            }
            finally
            {
                _dataLock.ExitWriteLock();
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
                ErrorHandler.HandleException(CLASS_NAME, nameof(ClearAll), e);
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
            _landBases.Clear();
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
                // Simple checksum based on object counts and a few key values
                int checksum = _combatUnits.Count * 17 + _leaders.Count * 23 +
                              _weaponProfiles.Count * 31 + _unitProfiles.Count * 37 +
                              _landBases.Count * 41;

                return checksum.ToString("X8");
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(CLASS_NAME, nameof(CalculateChecksum), e);
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
                        ErrorHandler.HandleException(CLASS_NAME, nameof(Dispose), e);
                    }
                }

                _isDisposed = true;
            }
        }

        #endregion // IDisposable Implementation
    }
}