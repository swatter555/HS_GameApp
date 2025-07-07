using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Defines methods for managing and resolving references within a data structure.
    /// </summary>
    /// <remarks>This interface is designed to handle scenarios where certain references may initially be
    /// unresolved and require resolution using external data or context. Implementations of this interface should
    /// provide mechanisms to identify unresolved references, resolve them, and check their resolution status.</remarks>
    public interface IResolvableReferences
    {
        IReadOnlyList<string> GetUnresolvedReferenceIDs();
        void ResolveReferences(GameDataManager manager);
        bool HasUnresolvedReferences();
    }

    /// <summary>
    /// Represents metadata for saved game data, including versioning, timestamps, and integrity checks.
    /// </summary>
    /// <remarks>This class provides essential information about a saved game, such as the version of the
    /// game,  the time the save was created, and a checksum for data integrity verification. It supports  serialization
    /// for persistence and deserialization for loading saved game data.</remarks>
    [Serializable]
    public class GameDataHeader : ISerializable
    {
        public int Version { get; set; }
        public DateTime SaveTime { get; set; }
        public string GameVersion { get; set; }
        public int CombatUnitCount { get; set; }
        public int LeaderCount { get; set; }
        public string Checksum { get; set; }

        #region Constructors

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

        #endregion // Constructors
    }


/*────────────────────────────────────────────────────────────────────────────

CampaignData ─ serializable container for player campaign progression

──────────────────────────────────────────────────────────────────────────────

Summary

═══════

CampaignData encapsulates all persistent player progression information across a campaign in the Panzer General tradition. It tracks core force development, scenario completion, victory points, prestige economy, and campaign branching state. This class serves as the complete save state for campaign files (.cmp) and contains only player-owned assets and progression metrics.

The class implements the classic "beer and pretzels" wargaming progression where a core force of units gains experience and veterancy across multiple scenarios, with prestige serving as the currency for unit purchases and upgrades between missions.

Public properties

═════════════════

string CampaignName { get; set; }

int CurrentScenarioIndex { get; set; }

List<string> CompletedScenarios { get; set; }

int TotalVictoryPoints { get; set; }

int CampaignTurnsElapsed { get; set; }

int CoreForcePrestige { get; set; }

Dictionary<string, int> UnitKillCounts { get; set; }

Dictionary<string, int> UnitBattleCount { get; set; }

Dictionary<string, CombatUnit> PlayerUnits { get; set; }

Dictionary<string, Leader> PlayerLeaders { get; set; }

Dictionary<string, bool> CampaignFlags { get; set; }

List<string> UnlockedScenarios { get; set; }

Constructors

═════════════

public CampaignData()

protected CampaignData(SerializationInfo info, StreamingContext context)

Public method signatures

════════════════════════

void GetObjectData(SerializationInfo info, StreamingContext context) - Serializes campaign data for binary persistence.

Important aspects

═════════════════

• **Panzer General Heritage**: Follows the established progression model where core units persist across scenarios, gaining experience and battle honors while consuming prestige for reinforcements and upgrades.

• **Victory and Progression Tracking**: Records scenario completion, victory points earned, and total campaign time elapsed to determine campaign success and unlock alternate mission paths.

• **Prestige Economy**: Maintains the prestige point balance used for purchasing new units, equipment upgrades, and replacements between scenarios.

• **Unit Performance Metrics**: Tracks individual unit kill counts and battle participation to determine veterancy progression and unit replacement costs.

• **Campaign Branching**: Uses boolean flags and unlocked scenario lists to support multiple campaign paths based on player performance and choices.

• **Player-Only Data**: Contains exclusively player-controlled assets and progression state. AI forces and scenario-specific data are handled separately by the scenario system.

• **Binary Serialization**: Implements ISerializable for robust save game compatibility and version management across game updates.

────────────────────────────────────────────────────────────────────────────*/
    [Serializable]
    public class CampaignData : ISerializable
    {
        public string CampaignName { get; set; }
        public int CurrentScenarioIndex { get; set; }
        public List<string> CompletedScenarios { get; set; }
        public int TotalVictoryPoints { get; set; }
        public int CampaignTurnsElapsed { get; set; }

        // Core force tracking (Panzer General style)
        public int CoreForcePrestige { get; set; }
        public Dictionary<string, int> UnitKillCounts { get; set; }
        public Dictionary<string, int> UnitBattleCount { get; set; }

        // Player progression
        public Dictionary<string, CombatUnit> PlayerUnits { get; set; }
        public Dictionary<string, Leader> PlayerLeaders { get; set; }

        // Campaign branching
        public Dictionary<string, bool> CampaignFlags { get; set; }
        public List<string> UnlockedScenarios { get; set; }

        public CampaignData()
        {
            CompletedScenarios = new List<string>();
            UnitKillCounts = new Dictionary<string, int>();
            UnitBattleCount = new Dictionary<string, int>();
            PlayerUnits = new Dictionary<string, CombatUnit>();
            PlayerLeaders = new Dictionary<string, Leader>();
            CampaignFlags = new Dictionary<string, bool>();
            UnlockedScenarios = new List<string>();
        }

        protected CampaignData(SerializationInfo info, StreamingContext context)
        {
            CampaignName = info.GetString(nameof(CampaignName));
            CurrentScenarioIndex = info.GetInt32(nameof(CurrentScenarioIndex));
            CompletedScenarios = (List<string>)info.GetValue(nameof(CompletedScenarios), typeof(List<string>));
            TotalVictoryPoints = info.GetInt32(nameof(TotalVictoryPoints));
            CampaignTurnsElapsed = info.GetInt32(nameof(CampaignTurnsElapsed));
            CoreForcePrestige = info.GetInt32(nameof(CoreForcePrestige));
            UnitKillCounts = (Dictionary<string, int>)info.GetValue(nameof(UnitKillCounts), typeof(Dictionary<string, int>));
            UnitBattleCount = (Dictionary<string, int>)info.GetValue(nameof(UnitBattleCount), typeof(Dictionary<string, int>));
            PlayerUnits = (Dictionary<string, CombatUnit>)info.GetValue(nameof(PlayerUnits), typeof(Dictionary<string, CombatUnit>));
            PlayerLeaders = (Dictionary<string, Leader>)info.GetValue(nameof(PlayerLeaders), typeof(Dictionary<string, Leader>));
            CampaignFlags = (Dictionary<string, bool>)info.GetValue(nameof(CampaignFlags), typeof(Dictionary<string, bool>));
            UnlockedScenarios = (List<string>)info.GetValue(nameof(UnlockedScenarios), typeof(List<string>));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(CampaignName), CampaignName);
            info.AddValue(nameof(CurrentScenarioIndex), CurrentScenarioIndex);
            info.AddValue(nameof(CompletedScenarios), CompletedScenarios);
            info.AddValue(nameof(TotalVictoryPoints), TotalVictoryPoints);
            info.AddValue(nameof(CampaignTurnsElapsed), CampaignTurnsElapsed);
            info.AddValue(nameof(CoreForcePrestige), CoreForcePrestige);
            info.AddValue(nameof(UnitKillCounts), UnitKillCounts);
            info.AddValue(nameof(UnitBattleCount), UnitBattleCount);
            info.AddValue(nameof(PlayerUnits), PlayerUnits);
            info.AddValue(nameof(PlayerLeaders), PlayerLeaders);
            info.AddValue(nameof(CampaignFlags), CampaignFlags);
            info.AddValue(nameof(UnlockedScenarios), UnlockedScenarios);
        }
    }


/*────────────────────────────────────────────────────────────────────────────

GameDataManager ─ central registry for all game objects and campaign persistence

──────────────────────────────────────────────────────────────────────────────

Summary

═══════

GameDataManager provides a singleton service that manages all CombatUnit and Leader objects during gameplay. It handles campaign progression persistence (.cmp files) and temporary scenario data loading (.sce files). The manager distinguishes between player forces that persist across missions and AI forces that are loaded temporarily for individual scenarios.

The class implements a simple collection-based approach using Side enumeration to filter player versus AI units. Only player data affects the dirty state for campaign saves, while AI units are considered temporary and cleared after scenario completion.

Public properties

═════════════════

int TotalObjectCount { get; }

int UnresolvedReferenceCount { get; }

bool HasUnsavedChanges { get; }

(int CombatUnits, int Leaders, int Facilities) ObjectCounts { get; }

static GameDataManager Instance { get; }

Constructors

═════════════

private GameDataManager()

Public method signatures

════════════════════════

bool RegisterCombatUnit(CombatUnit unit) - Adds a combat unit to the registry. Player units are marked for campaign persistence.

bool RegisterLeader(Leader leader) - Adds a leader to the registry. Only player leaders exist in the game.

bool UnregisterCombatUnit(string unitId) - Removes a combat unit from the registry.

bool UnregisterLeader(string leaderId) - Removes a leader from the registry.

CombatUnit GetCombatUnit(string unitId) - Retrieves a combat unit by ID. Used by CombatUnit for leader lookups.

Leader GetLeader(string leaderId) - Retrieves a leader by ID. Primary method for CombatUnit leader references.

IReadOnlyCollection<CombatUnit> GetAllCombatUnits() - Returns all registered combat units (player and AI).

IReadOnlyCollection<Leader> GetAllLeaders() - Returns all registered leaders (player only).

IReadOnlyCollection<CombatUnit> GetPlayerUnits() - Returns only player-controlled combat units.

IReadOnlyCollection<CombatUnit> GetAIUnits() - Returns only AI-controlled combat units.

IReadOnlyCollection<CombatUnit> GetCombatUnitsByClassification(UnitClassification classification) - Filters units by type.

IReadOnlyCollection<CombatUnit> GetCombatUnitsBySide(Side side) - Filters units by controlling side.

IReadOnlyCollection<CombatUnit> GetAllFacilities() - Returns all base units regardless of side.

IReadOnlyCollection<Leader> GetUnassignedLeaders() - Returns leaders not currently assigned to units.

IReadOnlyCollection<Leader> GetLeadersByGrade(CommandGrade grade) - Filters leaders by command grade.

bool HasCombatUnit(string unitId) - Checks if a unit ID exists in the registry.

bool HasLeader(string leaderId) - Checks if a leader ID exists in the registry.

int ResolveAllReferences() - Performs second-phase loading to resolve object cross-references.

List<string> ValidateDataIntegrity() - Validates object relationships and internal consistency.

bool LoadCampaign(string campaignPath) - Loads player progression data from a campaign file.

bool SaveCampaign(string campaignPath) - Saves player progression data to a campaign file.

bool LoadScenario(string scenarioName) - Loads AI forces and scenario data from the scenario folder.

void ClearScenarioData() - Removes all AI units after scenario completion.

bool TransferUnitToCampaign(string unitId) - Converts a captured AI unit to player control.

void MarkDirty(string objectId) - Flags a player object as needing save (AI objects ignored).

void ClearAll() - Removes all objects and resets to empty state.

void Dispose() - Cleans up resources and warns about unsaved changes.

Private method signatures

════════════════════════

void ClearPlayerData() - Removes all player units and leaders from collections.

void MarkDirtyPlayerObject(string objectId) - Internal method to flag player objects for campaign save.

string CalculateChecksum() - Generates simple checksum for save file validation.

Important aspects

═════════════════

• **Campaign vs Scenario Data**: The manager treats player forces as persistent campaign data and AI forces as temporary scenario data. Only player changes trigger dirty flags for campaign saves.

• **Leader References**: CombatUnit objects store leader IDs as strings and use GetLeader() for lookups rather than storing direct Leader references.

• **File Format Strategy**: Campaign files (.cmp) contain only player progression data while scenario files (.sce) are read-only and contain AI forces.

• **Single Collections**: Uses unified collections with Side-based filtering rather than separate player/AI dictionaries for simplicity.

• **Reference Resolution**: Implements two-phase loading where objects are deserialized first, then cross-references are resolved using the registry.

────────────────────────────────────────────────────────────────────────────*/
    public class GameDataManager : IDisposable
    {
        #region Constants

        private const string CLASS_NAME = nameof(GameDataManager);
        private const int CURRENT_SAVE_VERSION = 1;
        private const string SCENARIO_FILE_EXTENSION = ".sce";
        private const string CAMPAIGN_FILE_EXTENSION = ".cmp";
        private const string BACKUP_FILE_EXTENSION = ".bak";

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
        private readonly List<IResolvableReferences> _unresolvedObjects = new();
        private readonly HashSet<string> _dirtyPlayerObjects = new();
        private bool _isDisposed = false;

        #endregion // Fields


        #region Properties

        public int TotalObjectCount => _combatUnits.Count + _leaders.Count;

        public int UnresolvedReferenceCount => _unresolvedObjects.Count;

        public bool HasUnsavedChanges => _dirtyPlayerObjects.Count > 0;

        public (int CombatUnits, int Leaders, int Facilities) ObjectCounts
        {
            get
            {
                int facilityCount = _combatUnits.Values.Count(unit => unit.IsBase);
                return (CombatUnits: _combatUnits.Count, Leaders: _leaders.Count, Facilities: facilityCount);
            }
        }

        #endregion // Properties


        #region Constructor

        private GameDataManager()
        {
            // Initialize empty state
        }

        #endregion // Constructor


        #region Registration

        /// <summary>
        /// Registers a combat unit in the system.
        /// </summary>
        /// <remarks>If the combat unit is already registered, the method returns <see langword="false"/>
        /// and logs an exception. Player units are marked as dirty for campaign saving upon registration. If the combat
        /// unit implements <see cref="IResolvableReferences"/> and has unresolved references, it is added to the list
        /// of unresolved objects.</remarks>
        /// <param name="unit">The combat unit to register. Cannot be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the combat unit was successfully registered; otherwise, <see langword="false"/>.</returns>
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
                if (_combatUnits.ContainsKey(unit.UnitID))
                {
                    AppService.HandleException(CLASS_NAME, nameof(RegisterCombatUnit),
                        new InvalidOperationException($"Combat unit with ID {unit.UnitID} already registered"));
                    return false;
                }

                _combatUnits[unit.UnitID] = unit;

                // Only mark player units as dirty for campaign saving
                if (unit.Side == Side.Player)
                {
                    MarkDirtyPlayerObject(unit.UnitID);
                }

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
        }

        /// <summary>
        /// Registers a leader in the system.
        /// </summary>
        /// <remarks>A leader is identified by its unique <c>LeaderID</c>. If a leader with the same
        /// <c>LeaderID</c> is already registered,  the method will fail and return <see langword="false"/>.
        /// Additionally, if the leader implements <c>IResolvableReferences</c>  and has unresolved references, it will
        /// be added to the unresolved objects collection.</remarks>
        /// <param name="leader">The leader to register. Cannot be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the leader was successfully registered; otherwise, <see langword="false"/>.</returns>
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
                if (_leaders.ContainsKey(leader.LeaderID))
                {
                    AppService.HandleException(CLASS_NAME, nameof(RegisterLeader),
                        new InvalidOperationException($"Leader with ID {leader.LeaderID} already registered"));
                    return false;
                }

                _leaders[leader.LeaderID] = leader;
                MarkDirtyPlayerObject(leader.LeaderID); // Only player leaders exist

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
        }

        /// <summary>
        /// Unregisters a combat unit from the system using its unique identifier.
        /// </summary>
        /// <remarks>This method removes the specified combat unit from the internal collection and
        /// performs cleanup on related objects. If the unit does not exist or an error occurs during the operation, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="unitId">The unique identifier of the combat unit to unregister. Cannot be <see langword="null"/> or empty.</param>
        /// <returns><see langword="true"/> if the combat unit was successfully unregistered; otherwise, <see langword="false"/>.</returns>
        public bool UnregisterCombatUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
                return false;

            try
            {
                if (_combatUnits.Remove(unitId))
                {
                    _dirtyPlayerObjects.Remove(unitId);
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
        }

        /// <summary>
        /// Unregisters a leader from the system using the specified leader ID.
        /// </summary>
        /// <remarks>This method removes the leader from the internal collection and cleans up associated
        /// objects. If the specified <paramref name="leaderId"/> does not exist, the method returns <see
        /// langword="false"/>.</remarks>
        /// <param name="leaderId">The unique identifier of the leader to be unregistered. Cannot be null or empty.</param>
        /// <returns><see langword="true"/> if the leader was successfully unregistered; otherwise, <see langword="false"/>.</returns>
        public bool UnregisterLeader(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId))
                return false;

            try
            {
                if (_leaders.Remove(leaderId))
                {
                    _dirtyPlayerObjects.Remove(leaderId);
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
        }

        #endregion // Registration


        #region Retrieval Methods

        /// <summary>
        /// Retrieves a combat unit by its unique identifier.
        /// </summary>
        /// <remarks>This method attempts to retrieve a combat unit from an internal collection using the
        /// provided identifier. If the identifier is null or empty, the method returns <see
        /// langword="null"/>.</remarks>
        /// <param name="unitId">The unique identifier of the combat unit to retrieve. Cannot be null or empty.</param>
        /// <returns>The <see cref="CombatUnit"/> associated with the specified <paramref name="unitId"/>,  or <see
        /// langword="null"/> if no matching unit is found or if <paramref name="unitId"/> is invalid.</returns>
        public CombatUnit GetCombatUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return null;

            try
            {
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
        /// <param name="leaderId">Unique LeaderID</param>
        /// <returns>Leader object</returns>
        public Leader GetLeader(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId)) return null;

            try
            {
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
        /// <remarks>This method provides a snapshot of the current combat units. The returned collection
        /// is read-only and cannot be modified directly. If an exception occurs during retrieval, the method handles
        /// the error internally and returns an empty collection.</remarks>
        /// <returns>A read-only collection of <see cref="CombatUnit"/> objects representing the combat units. If an error
        /// occurs, an empty collection is returned.</returns>
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
        /// <remarks>This method returns a read-only collection of leaders. If an error occurs during
        /// retrieval,  an empty collection is returned instead. The method is designed to handle exceptions internally 
        /// and will not propagate them to the caller.</remarks>
        /// <returns>A read-only collection of <see cref="Leader"/> objects representing all leaders.  If no leaders are
        /// available or an error occurs, the collection will be empty.</returns>
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
        /// <remarks>This method filters combat units based on their <see cref="Side"/> property,
        /// returning only those associated with the player.</remarks>
        /// <returns>A read-only collection of <see cref="CombatUnit"/> objects representing the player's units. If no player
        /// units are found, an empty collection is returned.</returns>
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
        /// <remarks>This method returns all combat units that are not associated with the player. The
        /// returned collection is read-only and will be empty if no AI-controlled units are available.</remarks>
        /// <returns>A read-only collection of <see cref="CombatUnit"/> objects representing the AI-controlled units. If no
        /// AI-controlled units exist, an empty collection is returned.</returns>
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
        /// <remarks>This method filters the available combat units based on their classification. The
        /// returned collection is immutable and can be safely enumerated without modifying the underlying
        /// data.</remarks>
        /// <param name="classification">The classification of combat units to retrieve. Must be a valid <see cref="UnitClassification"/> value.</param>
        /// <returns>A read-only collection of <see cref="CombatUnit"/> objects that match the specified classification. If no
        /// units match the classification, an empty collection is returned.</returns>
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
        /// Retrieves all combat units associated with the specified side.
        /// </summary>
        /// <remarks>This method filters combat units based on their <see cref="CombatUnit.Side"/>
        /// property. It guarantees that the returned collection is never null.</remarks>
        /// <param name="side">The side for which to retrieve combat units. Must be a valid <see cref="Side"/> enumeration value.</param>
        /// <returns>A read-only collection of <see cref="CombatUnit"/> objects that belong to the specified side. If no units
        /// are found, an empty collection is returned.</returns>
        public IReadOnlyCollection<CombatUnit> GetCombatUnitsBySide(Side side)
        {
            try
            {
                return _combatUnits.Values.Where(unit => unit.Side == side).ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetCombatUnitsBySide), e);
                return new List<CombatUnit>();
            }
        }

        /// <summary>
        /// Get all CombatUnits that are bases.
        /// </summary>
        /// <returns>Returns a collection of bases</returns>
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
        /// <remarks>This method returns all leaders from the internal collection whose <see
        /// cref="Leader.IsAssigned"/> property is <see langword="false"/>. If an error occurs during execution, an
        /// empty collection is returned.</remarks>
        /// <returns>A read-only collection of <see cref="Leader"/> objects representing unassigned leaders. If no unassigned
        /// leaders exist, the collection will be empty.</returns>
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
        /// <remarks>This method filters leaders based on their <see cref="Leader.CommandGrade"/>
        /// property. If an exception occurs during execution, the method handles the exception internally and returns
        /// an empty collection.</remarks>
        /// <param name="grade">The command grade used to filter the leaders. Must be a valid <see cref="CommandGrade"/> value.</param>
        /// <returns>A read-only collection of <see cref="Leader"/> objects whose <see cref="Leader.CommandGrade"/> matches the
        /// specified grade. Returns an empty collection if no leaders match the grade or if an error occurs.</returns>
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

        /// <summary>
        /// Determines whether a combat unit with the specified identifier exists.
        /// </summary>
        /// <param name="unitId">The unique identifier of the combat unit to check. Cannot be null or empty.</param>
        /// <returns><see langword="true"/> if a combat unit with the specified identifier exists; otherwise, <see
        /// langword="false"/>. </returns>
        public bool HasCombatUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return false;
            return _combatUnits.ContainsKey(unitId);
        }

        /// <summary>
        /// Determines whether a leader with the specified identifier exists.
        /// </summary>
        /// <param name="leaderId">The unique identifier of the leader to check. Cannot be null or empty.</param>
        /// <returns><see langword="true"/> if a leader with the specified identifier exists; otherwise, <see langword="false"/>.</returns>
        public bool HasLeader(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId)) return false;
            return _leaders.ContainsKey(leaderId);
        }

        #endregion // Query Methods


        #region Reference Resolution

        /// <summary>
        /// Resolves all unresolved references in the current context.
        /// </summary>
        /// <remarks>This method iterates through all objects with unresolved references and attempts to
        /// resolve them. If an object successfully resolves its references, it is removed from the unresolved list. Any
        /// objects that fail to resolve their references remain in the unresolved list.</remarks>
        /// <returns>The number of objects whose references were successfully resolved. Returns 0 if an exception occurs during
        /// the resolution process.</returns>
        public int ResolveAllReferences()
        {
            try
            {
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
                            resolvedCount++;
                        }
                    }
                    catch (Exception e)
                    {
                        AppService.HandleException(CLASS_NAME, nameof(ResolveAllReferences), e);
                        remainingUnresolved.Add(unresolvedObject);
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
        }

        /// <summary>
        /// Validates the integrity of the data within the current context and identifies any inconsistencies.
        /// </summary>
        /// <remarks>This method performs a series of checks to ensure the internal consistency of combat
        /// units, leader assignments,  and unresolved object references. If any issues are detected, they are returned
        /// as a list of error messages.</remarks>
        /// <returns>A list of strings containing error messages that describe any detected inconsistencies.  The list will be
        /// empty if no issues are found.</returns>
        public List<string> ValidateDataIntegrity()
        {
            var errors = new List<string>();

            try
            {
                if (_unresolvedObjects.Count > 0)
                {
                    errors.Add($"{_unresolvedObjects.Count} objects have unresolved references");
                }

                foreach (var unit in _combatUnits.Values)
                {
                    var unitErrors = unit.ValidateInternalConsistency();
                    errors.AddRange(unitErrors);
                }

                // Validate leader assignments
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
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateDataIntegrity), e);
                errors.Add($"Validation failed with exception: {e.Message}");
            }

            return errors;
        }

        #endregion // Reference Resolution


        #region Campaign Operations

        /// <summary>
        /// Loads a campaign from the specified file path.
        /// </summary>
        /// <remarks> The method validates the provided file path and ensures it ends with the expected
        /// campaign file extension. If the file does not exist or the path is invalid, the method logs the error and
        /// returns <see langword="false"/>. Upon successful loading, the campaign data is deserialized, and player
        /// units and leaders are initialized. Any unresolved references in the loaded data are tracked for further
        /// resolution. </remarks>
        /// <param name="campaignPath">The path to the campaign file. The file must exist and have the correct extension.</param>
        /// <returns><see langword="true"/> if the campaign is successfully loaded; otherwise, <see langword="false"/>. </returns>
        public bool LoadCampaign(string campaignPath)
        {
            if (string.IsNullOrEmpty(campaignPath))
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadCampaign),
                    new ArgumentException("Campaign path cannot be null or empty"));
                return false;
            }

            try
            {
                if (!campaignPath.EndsWith(CAMPAIGN_FILE_EXTENSION))
                {
                    campaignPath += CAMPAIGN_FILE_EXTENSION;
                }

                if (!File.Exists(campaignPath))
                {
                    AppService.HandleException(CLASS_NAME, nameof(LoadCampaign),
                        new FileNotFoundException($"Campaign file not found: {campaignPath}"));
                    return false;
                }

                // Clear existing player data
                ClearPlayerData();

                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(campaignPath, FileMode.Open, FileAccess.Read))
                {
                    var header = (GameDataHeader)formatter.Deserialize(stream);
                    var campaignData = (CampaignData)formatter.Deserialize(stream);

                    // Load player units and leaders
                    foreach (var kvp in campaignData.PlayerUnits)
                    {
                        _combatUnits[kvp.Key] = kvp.Value;
                        if (kvp.Value is IResolvableReferences resolvable && resolvable.HasUnresolvedReferences())
                        {
                            _unresolvedObjects.Add(resolvable);
                        }
                    }

                    foreach (var kvp in campaignData.PlayerLeaders)
                    {
                        _leaders[kvp.Key] = kvp.Value;
                        if (kvp.Value is IResolvableReferences resolvable && resolvable.HasUnresolvedReferences())
                        {
                            _unresolvedObjects.Add(resolvable);
                        }
                    }
                }

                _dirtyPlayerObjects.Clear();
                AppService.CaptureUiMessage($"Campaign loaded successfully. {_combatUnits.Count} units, {_leaders.Count} leaders.");

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadCampaign), e);
                ClearPlayerData();
                return false;
            }
        }

        /// <summary>
        /// Saves the current campaign data to the specified file path.
        /// </summary>
        /// <remarks>If a file already exists at the specified path, a backup of the existing file will be
        /// created  with a different extension before overwriting it. The method captures a UI message upon successful 
        /// save and clears the dirty player object state.</remarks>
        /// <param name="campaignPath">The file path where the campaign data should be saved. If the path does not include the expected  campaign
        /// file extension, it will be appended automatically. Cannot be null or empty.</param>
        /// <returns><see langword="true"/> if the campaign data was successfully saved; otherwise, <see langword="false"/>.</returns>
        public bool SaveCampaign(string campaignPath)
        {
            if (string.IsNullOrEmpty(campaignPath))
            {
                AppService.HandleException(CLASS_NAME, nameof(SaveCampaign),
                    new ArgumentException("Campaign path cannot be null or empty"));
                return false;
            }

            try
            {
                if (!campaignPath.EndsWith(CAMPAIGN_FILE_EXTENSION))
                {
                    campaignPath += CAMPAIGN_FILE_EXTENSION;
                }

                if (File.Exists(campaignPath))
                {
                    string backupPath = Path.ChangeExtension(campaignPath, BACKUP_FILE_EXTENSION);
                    File.Copy(campaignPath, backupPath, true);
                }

                var playerUnits = GetPlayerUnits().ToDictionary(unit => unit.UnitID, unit => unit);
                var playerLeaders = _leaders.ToDictionary(leader => leader.Key, leader => leader.Value);

                var campaignData = new CampaignData
                {
                    PlayerUnits = playerUnits,
                    PlayerLeaders = playerLeaders
                    // TODO: Add actual campaign progression data
                };

                var header = new GameDataHeader
                {
                    CombatUnitCount = playerUnits.Count,
                    LeaderCount = playerLeaders.Count,
                    Checksum = CalculateChecksum()
                };

                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(campaignPath, FileMode.Create, FileAccess.Write))
                {
                    formatter.Serialize(stream, header);
                    formatter.Serialize(stream, campaignData);
                }

                _dirtyPlayerObjects.Clear();
                AppService.CaptureUiMessage($"Campaign saved successfully. {playerUnits.Count} units, {playerLeaders.Count} leaders.");

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SaveCampaign), e);
                return false;
            }
        }

        #endregion // Campaign Operations


        #region Scenario Operations

        /// <summary>
        /// Loads a scenario from the specified file name and initializes AI data.
        /// </summary>
        /// <remarks>This method attempts to load a scenario file from the application's scenario storage
        /// folder.  If the file does not exist or an error occurs during loading, the method returns <see
        /// langword="false"/>  and logs the error. The method clears existing AI data before loading the new scenario
        /// and resolves  references for AI units. Any data integrity issues found during the load process are
        /// logged.</remarks>
        /// <param name="scenarioName">The name of the scenario file to load, without the file extension.  Must not be null or empty.</param>
        /// <returns><see langword="true"/> if the scenario was successfully loaded; otherwise, <see langword="false"/>. </returns>
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
                    scenarioName + SCENARIO_FILE_EXTENSION);

                if (!File.Exists(scenarioPath))
                {
                    AppService.HandleException(CLASS_NAME, nameof(LoadScenario),
                        new FileNotFoundException($"Scenario file not found: {scenarioPath}"));
                    return false;
                }

                // Clear existing AI data
                ClearScenarioData();

                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(scenarioPath, FileMode.Open, FileAccess.Read))
                {
                    var header = (GameDataHeader)formatter.Deserialize(stream);
                    var combatUnits = (Dictionary<string, CombatUnit>)formatter.Deserialize(stream);
                    var leaders = (Dictionary<string, Leader>)formatter.Deserialize(stream);

                    // Load AI units (non-player)
                    foreach (var kvp in combatUnits)
                    {
                        if (kvp.Value.Side != Side.Player)
                        {
                            _combatUnits[kvp.Key] = kvp.Value;
                            if (kvp.Value is IResolvableReferences resolvable && resolvable.HasUnresolvedReferences())
                            {
                                _unresolvedObjects.Add(resolvable);
                            }
                        }
                    }

                    // Note: No scenario leaders since only player forces have leaders
                }

                int resolvedCount = ResolveAllReferences();
                AppService.CaptureUiMessage($"Scenario loaded successfully. {GetAIUnits().Count} AI units. Resolved {resolvedCount} references.");

                var validationErrors = ValidateDataIntegrity();
                if (validationErrors.Count > 0)
                {
                    AppService.CaptureUiMessage($"Found {validationErrors.Count} data integrity issues during scenario load.");
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadScenario), e);
                return false;
            }
        }

        /// <summary>
        /// Clears out all scenario data.
        /// </summary>
        public void ClearScenarioData()
        {
            try
            {
                var aiUnits = GetAIUnits().Select(unit => unit.UnitID).ToList();
                foreach (var unitId in aiUnits)
                {
                    UnregisterCombatUnit(unitId);
                }

                AppService.CaptureUiMessage($"Cleared {aiUnits.Count} AI units from scenario data.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ClearScenarioData), e);
            }
        }

        #endregion // Scenario Operations


        #region Unit Transfer

        /// <summary>
        /// Add ComtUnit to player forces.
        /// </summary>
        /// <param name="unitId">The UnitID</param>
        /// <returns>Success/Fail</returns>
        public bool TransferUnitToCampaign(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
                return false;

            try
            {
                var unit = GetCombatUnit(unitId);
                if (unit == null || unit.Side == Side.Player)
                    return false;

                // Change unit to player side
                // Note: This assumes CombatUnit has a way to change sides
                // unit.Side = Side.Player; // TODO: Implement this in CombatUnit

                MarkDirtyPlayerObject(unitId);
                AppService.CaptureUiMessage($"Unit {unit.UnitName} transferred to player forces.");

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(TransferUnitToCampaign), e);
                return false;
            }
        }

        #endregion // Unit Transfer


        #region State Management

        /// <summary>
        /// Marks a player-owned unit or leader as "dirty" (modified) so that it will be included in the next campaign save.
        /// Only player units and leaders are tracked for campaign persistence. If the object ID does not correspond to a player unit or leader, no action is taken.
        /// <summary>
        /// <param name="objectId">ObjectID</param>
        public void MarkDirty(string objectId)
        {
            if (string.IsNullOrEmpty(objectId)) return;

            try
            {
                // Only mark player objects as dirty for campaign saves
                var unit = GetCombatUnit(objectId);
                if (unit != null && unit.Side == Side.Player)
                {
                    MarkDirtyPlayerObject(objectId);
                    return;
                }

                var leader = GetLeader(objectId);
                if (leader != null)
                {
                    MarkDirtyPlayerObject(objectId);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(MarkDirty), e);
            }
        }

        /// <summary>
        /// Clears all game-related data, including combat units, leaders, player objects, and unresolved objects.
        /// </summary>
        /// <remarks>This method removes all entries from the internal collections and sends a UI message
        /// indicating that the game data has been cleared. If an exception occurs during the operation, it is handled
        /// and logged appropriately.</remarks>
        public void ClearAll()
        {
            try
            {
                _combatUnits.Clear();
                _leaders.Clear();
                _dirtyPlayerObjects.Clear();
                _unresolvedObjects.Clear();
                AppService.CaptureUiMessage("All game data cleared.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ClearAll), e);
            }
        }

        #endregion // State Management


        #region Private Helper Methods

        /// <summary>
        /// Clears all player-related data, including units, leaders, and other associated objects.
        /// </summary>
        /// <remarks>This method removes all player units and leaders from their respective registries and
        /// clears any cached or dirty player objects. It is typically used to reset player state during game
        /// transitions or cleanup operations.</remarks>
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

            _dirtyPlayerObjects.Clear();
        }

        /// <summary>
        /// Marks the specified player object as dirty, indicating that it requires further processing or updates.
        /// </summary>
        /// <remarks>This method adds the specified object ID to an internal collection of dirty player
        /// objects. Ensure that <paramref name="objectId"/> is valid and unique within the context of the
        /// application.</remarks>
        /// <param name="objectId">The unique identifier of the player object to mark as dirty. Cannot be null or empty.</param>
        private void MarkDirtyPlayerObject(string objectId)
        {
            _dirtyPlayerObjects.Add(objectId);
        }

        /// <summary>
        /// Calculates a checksum value based on the current state of combat units and leaders.
        /// </summary>
        /// <remarks>The checksum is derived from the counts of combat units and leaders, using a
        /// predefined formula. This method is intended for internal use and may return "DEFAULT" if an exception is
        /// encountered.</remarks>
        /// <returns>A hexadecimal string representing the calculated checksum. Returns "DEFAULT" if an exception occurs during
        /// calculation.</returns>
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


        #region IDisposable Implementation

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (HasUnsavedChanges)
                {
                    AppService.CaptureUiMessage("GameDataManager disposing with unsaved changes.");
                }

                _combatUnits.Clear();
                _leaders.Clear();
                _dirtyPlayerObjects.Clear();
                _unresolvedObjects.Clear();

                _isDisposed = true;
            }
        }

        #endregion // IDisposable Implementation
    }
}