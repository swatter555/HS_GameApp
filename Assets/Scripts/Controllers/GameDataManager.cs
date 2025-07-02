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
    /*────────────────────────────────────────────────────────────────────────────
     IResolvableReferences ─ two‑phase loading contract 
    ──────────────────────────────────────────────────────────────────────────────

    Summary
    ═══════
    • Defines a lightweight contract for objects that cannot fully materialise on
      their initial deserialisation pass and must resolve cross‑object links in a
      second phase.
    • Enables GameDataManager.ResolveAllReferences() to iterate generically over
      heterogeneous objects while maintaining strict type safety.

    Required members
    ════════════════
      IReadOnlyList<string> GetUnresolvedReferenceIDs()  ─ IDs still pending
      void                 ResolveReferences(GameDataManager mgr)
      bool                 HasUnresolvedReferences()

    Developer notes
    ═══════════════
    • Implementations should populate unresolved IDs during ISerializable ctor and
      clear them once resolution succeeds to minimise GC pressure.
    • Always return an empty list instead of null to avoid allocations in hot
      loops.
   ────────────────────────────────────────────────────────────────────────────*/
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


    /*────────────────────────────────────────────────────────────────────────────
     GameDataHeader ─ save file metadata shim 
    ─────────────────────────────────── ──────────────────────────────────────────

    Summary
    ═══════
    • Lightweight, serialisable header placed at the start of every .sce save file
      to enable version negotiation and quick integrity checks without loading the
      full object graph.

    Data fields
    ═══════════
      int        Version             ─ incremental format revision
      DateTime   SaveTime            ─ UTC timestamp of save creation
      string     GameVersion         ─ client build string (Unity Application.version)
      int        CombatUnitCount     ─ unit objects in payload
      int        LeaderCount         ─ leader objects in payload
      string     Checksum            ─ SHA‑256 over compressed payload bytes

    Constructors
    ════════════
      GameDataHeader()                               // default → now, version = 1
      private GameDataHeader(SerializationInfo, StreamingContext)

    Interface implementation
    ════════════════════════
      void GetObjectData(SerializationInfo, StreamingContext)

    Developer notes
    ═══════════════
    • Bump Version when header fields change; maintain backwards readers in
      GameDataManager.
    • Checksum is calculated by GameDataManager.SaveGameState; setter only used
      during deserialisation.
   ────────────────────────────────────────────────────────────────────────────*/
    [Serializable]
    public class GameDataHeader : ISerializable
    {
        public int Version { get; set; }
        public DateTime SaveTime { get; set; }
        public string GameVersion { get; set; }
        public int CombatUnitCount { get; set; }
        public int LeaderCount { get; set; }
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
            Checksum = info.GetString(nameof(Checksum));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Version), Version);
            info.AddValue(nameof(SaveTime), SaveTime);
            info.AddValue(nameof(GameVersion), GameVersion);
            info.AddValue(nameof(CombatUnitCount), CombatUnitCount);
            info.AddValue(nameof(LeaderCount), LeaderCount);
            info.AddValue(nameof(Checksum), Checksum);
        }
    }


    /*────────────────────────────────────────────────────────────────────────────
     GameDataManager ─ global registry & persistence manager 
    ──────────────────────────────────────────────────────────────────────────────

    Summary
    ═══════
    • Provides a thread‑safe, singleton service that registers, tracks, serialises,
      and validates all persistent game objects (CombatUnit, Leader, etc.) across
      Hammer & Sickle.
    • Implements the two‑phase loading pattern (deserialize → ResolveAllReferences)
      to support shared template catalogs and cross‑object links.
    • Exposes diagnostics (dirty/unsaved state, unresolved reference count) for UI
      and automated tests.

    Public properties
    ═════════════════
      int       TotalObjectCount         { get; }
      int       UnresolvedReferenceCount { get; }
      bool      HasUnsavedChanges        { get; }
      (int CombatUnits, int Leaders, int Facilities)
                ObjectCounts             { get; }
      static    GameDataManager Instance { get; }

    Constructors
    ════════════
      private GameDataManager()                               // hidden ctor (singleton)

    Public methods
    ══════════════
      bool   RegisterCombatUnit(CombatUnit unit)              // add & track combat unit
      bool   RegisterLeader(Leader leader)                    // add & track leader
      bool   UnregisterCombatUnit(string unitId)              // remove combat unit
      bool   UnregisterLeader(string leaderId)                // remove leader
      CombatUnit GetCombatUnit(string unitId)                 // fetch combat unit or null
      Leader GetLeader(string leaderId)                       // fetch leader or null
      IReadOnlyCollection<CombatUnit> GetAllCombatUnits()     // enumerate combat units
      IReadOnlyCollection<Leader> GetAllLeaders()             // enumerate leaders
      IReadOnlyCollection<CombatUnit> GetCombatUnitsByClassification(UnitClassification c)
      IReadOnlyCollection<CombatUnit> GetCombatUnitsBySide(Side side)
      IReadOnlyCollection<CombatUnit> GetAllFacilities()
      IReadOnlyCollection<Leader> GetUnassignedLeaders()
      IReadOnlyCollection<Leader> GetLeadersByGrade(CommandGrade grade)
      bool   HasCombatUnit(string unitId)
      bool   HasLeader(string leaderId)
      int    ResolveAllReferences()                           // second‑phase linking
      List<string> ValidateDataIntegrity()                    // cross‑object checks
      bool   SaveGameState(string filePath)                   // serialize to .sce (+.bak)
      bool   LoadGameState(string filePath)                   // load & re‑hydrate
      bool   SaveScenario(string scenarioName)                // save to scenario folder
      bool   LoadScenario(string scenarioName)                // load scenario & validate
      void   MarkDirty(string objectId)                       // flag object as changed
      void   ClearAll()                                       // wipe all collections
      void   Dispose()                                        // dispose resources

    Private helpers
    ═══════════════
      void   ClearAllInternal()
      void   MarkDirtyInternal(string objectId)
      string CalculateChecksum()
      void   Dispose(bool disposing)

    Developer notes
    ═══════════════
    • Any change to the persisted object graph must update CalculateChecksum() and
      CURRENT_SAVE_VERSION.
    • All public methods wrap try/catch and forward to AppService.HandleException
      for uniform error handling as mandated by coding guidelines.
    • Immutable template objects (WeaponSystemProfile, UnitProfile, etc.) are
      intentionally excluded; they are resolved during startup via master catalogs.
    • Keep an extra blank line between #region blocks and append region names after
      each #endregion as per project style preferences.
   ────────────────────────────────────────────────────────────────────────────*/
    public class GameDataManager : IDisposable
    {
        #region Constants

        private const string CLASS_NAME = nameof(GameDataManager);
        private const int CURRENT_SAVE_VERSION = 1;
        private const string SAVE_FILE_EXTENSION = ".sce";
        private const string BACKUP_FILE_EXTENSION = ".bak";

        #endregion


        #region Singleton

        private static GameDataManager _instance;
        private static readonly object _instanceLock = new();

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

        #endregion


        #region Fields

        // Thread synchronization
        private readonly ReaderWriterLockSlim _dataLock = new(LockRecursionPolicy.SupportsRecursion);

        // Object registries - only persistent objects that need cross-object reference resolution
        private readonly Dictionary<string, CombatUnit> _combatUnits = new();
        private readonly Dictionary<string, Leader> _leaders = new();

        // Reference resolution tracking
        private readonly List<IResolvableReferences> _unresolvedObjects = new();

        // State tracking
        private readonly HashSet<string> _dirtyObjects = new();
        private bool _isDisposed = false;

        #endregion


        #region Properties

        /// <summary>
        /// Gets the total number of registered objects.
        /// </summary>
        public int TotalObjectCount
        {
            get
            {
                _dataLock.EnterReadLock();
                try
                {
                    return _combatUnits.Count + _leaders.Count;
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
        /// Gets whether there are unsaved changes.
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
        /// Gets detailed object counts for diagnostics.
        /// </summary>
        public (int CombatUnits, int Leaders, int Facilities) ObjectCounts
        {
            get
            {
                _dataLock.EnterReadLock();
                try
                {
                    int facilityCount = _combatUnits.Values.Count(unit => unit.IsBase);
                    return (CombatUnits: _combatUnits.Count, Leaders: _leaders.Count, Facilities: facilityCount);
                }
                finally
                {
                    _dataLock.ExitReadLock();
                }
            }
        }

        #endregion


        #region Constructor

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private GameDataManager()
        {
            // Initialize empty state
        }

        #endregion


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
                MarkDirtyInternal(unit.UnitID);

                // Track for reference resolution if needed
                if (unit is IResolvableReferences resolvable && resolvable.HasUnresolvedReferences())
                {
                    _unresolvedObjects.Add(resolvable);
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
                MarkDirtyInternal(leader.LeaderID);

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

        #endregion


        #region Unregistration Methods

        /// <summary>
        /// Unregisters a combat unit from the data manager.
        /// </summary>
        /// <param name="unitId">The unit ID to unregister</param>
        /// <returns>True if unregistration was successful</returns>
        public bool UnregisterCombatUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
            {
                return false;
            }

            try
            {
                _dataLock.EnterWriteLock();

                if (_combatUnits.Remove(unitId))
                {
                    _dirtyObjects.Remove(unitId);

                    // Remove from unresolved objects list if present
                    _unresolvedObjects.RemoveAll(obj => obj is CombatUnit unit && unit.UnitID == unitId);

                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(UnregisterCombatUnit), e);
                return false;
            }
            finally
            {
                _dataLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Unregisters a leader from the data manager.
        /// </summary>
        /// <param name="leaderId">The leader ID to unregister</param>
        /// <returns>True if unregistration was successful</returns>
        public bool UnregisterLeader(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId))
            {
                return false;
            }

            try
            {
                _dataLock.EnterWriteLock();

                if (_leaders.Remove(leaderId))
                {
                    _dirtyObjects.Remove(leaderId);

                    // Remove from unresolved objects list if present
                    _unresolvedObjects.RemoveAll(obj => obj is Leader leader && leader.LeaderID == leaderId);

                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(UnregisterLeader), e);
                return false;
            }
            finally
            {
                _dataLock.ExitWriteLock();
            }
        }

        #endregion


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

        #endregion


        #region Query Methods

        /// <summary>
        /// Gets all combat units of a specific classification.
        /// </summary>
        /// <param name="classification">The unit classification to filter by</param>
        /// <returns>Read-only collection of matching units</returns>
        public IReadOnlyCollection<CombatUnit> GetCombatUnitsByClassification(UnitClassification classification)
        {
            try
            {
                _dataLock.EnterReadLock();
                return _combatUnits.Values.Where(unit => unit.Classification == classification).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetCombatUnitsByClassification), e);
                return new List<CombatUnit>();
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets all combat units controlled by a specific side.
        /// </summary>
        /// <param name="side">The side to filter by</param>
        /// <returns>Read-only collection of matching units</returns>
        public IReadOnlyCollection<CombatUnit> GetCombatUnitsBySide(Side side)
        {
            try
            {
                _dataLock.EnterReadLock();
                return _combatUnits.Values.Where(unit => unit.Side == side).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetCombatUnitsBySide), e);
                return new List<CombatUnit>();
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets all base units (facilities).
        /// </summary>
        /// <returns>Read-only collection of base units</returns>
        public IReadOnlyCollection<CombatUnit> GetAllFacilities()
        {
            try
            {
                _dataLock.EnterReadLock();
                return _combatUnits.Values.Where(unit => unit.IsBase).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAllFacilities), e);
                return new List<CombatUnit>();
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets all leaders that are currently unassigned.
        /// </summary>
        /// <returns>Read-only collection of unassigned leaders</returns>
        public IReadOnlyCollection<Leader> GetUnassignedLeaders()
        {
            try
            {
                _dataLock.EnterReadLock();
                return _leaders.Values.Where(leader => !leader.IsAssigned).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetUnassignedLeaders), e);
                return new List<Leader>();
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets all leaders of a specific command grade.
        /// </summary>
        /// <param name="grade">The command grade to filter by</param>
        /// <returns>Read-only collection of matching leaders</returns>
        public IReadOnlyCollection<Leader> GetLeadersByGrade(CommandGrade grade)
        {
            try
            {
                _dataLock.EnterReadLock();
                return _leaders.Values.Where(leader => leader.CommandGrade == grade).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetLeadersByGrade), e);
                return new List<Leader>();
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Checks if a combat unit with the specified ID exists.
        /// </summary>
        /// <param name="unitId">The unit ID to check</param>
        /// <returns>True if the unit exists</returns>
        public bool HasCombatUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return false;

            try
            {
                _dataLock.EnterReadLock();
                return _combatUnits.ContainsKey(unitId);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HasCombatUnit), e);
                return false;
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Checks if a leader with the specified ID exists.
        /// </summary>
        /// <param name="leaderId">The leader ID to check</param>
        /// <returns>True if the leader exists</returns>
        public bool HasLeader(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId)) return false;

            try
            {
                _dataLock.EnterReadLock();
                return _leaders.ContainsKey(leaderId);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HasLeader), e);
                return false;
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }

        #endregion


        #region Reference Resolution

        /// <summary>
        /// Resolves all pending object references after deserialization.
        /// Implements the second phase of the two-phase loading pattern.
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
        /// Validates data integrity by checking cross-object relationships and calling each object's internal validation.
        /// Each object validates its own internal consistency via ValidateInternalConsistency().
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

                    foreach (var obj in _unresolvedObjects)
                    {
                        var unresolvedIds = obj.GetUnresolvedReferenceIDs();
                        if (unresolvedIds.Count > 0)
                        {
                            errors.Add($"Object has unresolved references: {string.Join(", ", unresolvedIds)}");
                        }
                    }
                }

                // Validate each CombatUnit's internal consistency
                foreach (var unit in _combatUnits.Values)
                {
                    var unitErrors = unit.ValidateInternalConsistency();
                    errors.AddRange(unitErrors);
                }

                // Validate cross-object leader assignment relationships
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

                // Validate unit leader references point to actual leaders
                foreach (var unit in _combatUnits.Values)
                {
                    if (unit.CommandingOfficer != null)
                    {
                        bool leaderExists = _leaders.ContainsKey(unit.CommandingOfficer.LeaderID);
                        if (!leaderExists)
                        {
                            errors.Add($"Unit {unit.UnitName} has commanding officer {unit.CommandingOfficer.LeaderID} not in leader registry");
                        }
                        else
                        {
                            // Verify bidirectional relationship
                            var leader = _leaders[unit.CommandingOfficer.LeaderID];
                            if (!leader.IsAssigned || leader.UnitID != unit.UnitID)
                            {
                                errors.Add($"Unit {unit.UnitName} references leader {leader.Name} but leader doesn't think it's assigned to this unit");
                            }
                        }
                    }
                }

                // Validate cross-object airbase attachment relationships
                foreach (var unit in _combatUnits.Values)
                {
                    if (unit.IsBase && unit.FacilityType == FacilityType.Airbase)
                    {
                        foreach (var attachedUnit in unit.AirUnitsAttached)
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
                            }
                        }
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

        #endregion


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

                // Ensure correct extension
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
                    Checksum = CalculateChecksum()
                };

                // Serialize to file
                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    formatter.Serialize(stream, header);
                    formatter.Serialize(stream, _combatUnits);
                    formatter.Serialize(stream, _leaders);
                }

                // Clear dirty flags after successful save
                _dirtyObjects.Clear();

                AppService.CaptureUiMessage($"Game state saved successfully. {_combatUnits.Count} units, {_leaders.Count} leaders.");

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
            if (string.IsNullOrEmpty(filePath))
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadGameState),
                    new ArgumentException("File path cannot be null or empty"));
                return false;
            }

            try
            {
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

                    // Validate loaded data counts match header
                    if (_combatUnits.Count != header.CombatUnitCount || _leaders.Count != header.LeaderCount)
                    {
                        AppService.HandleException(CLASS_NAME, nameof(LoadGameState),
                            new InvalidDataException("Loaded object counts do not match header metadata"));
                    }
                }

                // Clear dirty flags - loaded data is clean
                _dirtyObjects.Clear();

                AppService.CaptureUiMessage($"Game state loaded successfully. {_combatUnits.Count} units, {_leaders.Count} leaders.");

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadGameState), e);

                // Clear partially loaded data on failure
                try
                {
                    ClearAllInternal();
                }
                catch (Exception clearEx)
                {
                    AppService.HandleException(CLASS_NAME, nameof(LoadGameState), clearEx, ExceptionSeverity.Minor);
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
        /// Automatically resolves references and validates data integrity after loading.
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
                    AppService.CaptureUiMessage($"Resolved {resolvedCount} object references.");

                    // Validate integrity
                    var validationErrors = ValidateDataIntegrity();
                    if (validationErrors.Count > 0)
                    {
                        AppService.CaptureUiMessage($"Found {validationErrors.Count} data integrity issues during scenario load.");

                        foreach (var error in validationErrors.Take(5)) // Log first 5 errors
                        {
                            AppService.HandleException(CLASS_NAME, nameof(LoadScenario),
                                new InvalidDataException($"Data integrity issue: {error}"), ExceptionSeverity.Minor);
                        }

                        if (validationErrors.Count > 5)
                        {
                            AppService.CaptureUiMessage($"... and {validationErrors.Count - 5} additional integrity issues (see logs).");
                        }
                    }
                    else
                    {
                        AppService.CaptureUiMessage("Scenario loaded with no integrity issues.");
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

        #endregion


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
                MarkDirtyInternal(objectId);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(MarkDirty), e);
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
        /// Clears all registered objects and resets the manager to empty state.
        /// </summary>
        public void ClearAll()
        {
            try
            {
                _dataLock.EnterWriteLock();
                ClearAllInternal();
                AppService.CaptureUiMessage("All game data cleared.");
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

        #endregion


        #region Private Helper Methods

        /// <summary>
        /// Internal method to clear all data. Assumes write lock is already held.
        /// </summary>
        private void ClearAllInternal()
        {
            _combatUnits.Clear();
            _leaders.Clear();
            _dirtyObjects.Clear();
            _unresolvedObjects.Clear();
        }

        /// <summary>
        /// Internal method to mark object dirty. Assumes write lock is already held.
        /// </summary>
        /// <param name="objectId">The ID of the object that changed</param>
        private void MarkDirtyInternal(string objectId)
        {
            _dirtyObjects.Add(objectId);
        }

        /// <summary>
        /// Calculates a simple checksum for data validation.
        /// </summary>
        /// <returns>Checksum string</returns>
        private string CalculateChecksum()
        {
            try
            {
                int checksum = _combatUnits.Count * 17 + _leaders.Count * 23;

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

        #endregion


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
                            AppService.CaptureUiMessage("GameDataManager disposing with unsaved changes - consider implementing auto-save.");
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

        #endregion
    }
}