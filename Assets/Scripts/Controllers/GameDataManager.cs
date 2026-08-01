using HammerAndSickle.Models;
using HammerAndSickle.Persistence;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Central data management system for Hammer & Sickle, managing combat units, 
    /// leaders, and game state with Unity-compliant singleton pattern.
    /// </summary>
    public class GameDataManager : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(GameDataManager);

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

        // Occupancy cache — rebuilt at turn start and after each completed move order
        private Dictionary<Position2D, List<CombatUnit>> _occupancyCache;

        #endregion // Fields

        #region Properties

        // Indicates whether the manager has been fully initialized.
        public bool IsReady => _isInitialized;

        /// ----------------------------------------------------
        /// Database of all registered combat units and leaders
        /// ----------------------------------------------------
        // The player progression data that persists across scenarios.
        public CampaignData CurrentCampaignData { get; set; }

        // The currently loaded scenario data (null outside of missions).
        public ScenarioData CurrentScenarioData { get; set; }

        // Gets the count of registered combat units.
        public int UnitCount => _combatUnits.Count;

        // Gets the count of registered leaders.
        public int LeaderCount => _leaders.Count;

        /// --------------------
        /// Map related data
        /// --------------------
        // Currently active hex map instance (null if none).
        public static HexMap CurrentHexMap { get; set; } = null;

        // Current map size in hex cells (width, height).
        public static Position2D CurrentMapSize { get; set; } = new(100, 100);

        // Current map theme for terrain and icons.
        public static MapTheme CurrentMapTheme { get; set; } = MapTheme.MiddleEast;

        // Hex outline color.
        public static HexOutlineColor CurrentHexOutlineColor { get; set; } = HexOutlineColor.Black;

        // Vector representing no hex is selected
        public static readonly Position2D NoHexSelected = new(-1, -1);

        // Currently selected hex coordinates (-1, -1) if none selected.
        public static Position2D SelectedHex { get; set; } = NoHexSelected;

        // Data of the currently selected hex (null if none).
        public static HexTile SelectedHexData { get; set; } = null;

        // Currently selected combat unit (null if no unit on selected hex).
        public static CombatUnit SelectedUnit { get; set; } = null;

        // Currently selected leader (null if selected unit has no leader).
        public static Leader SelectedLeader { get; set; } = null;

        /// ----------------------
        /// Scenario related data
        /// ----------------------
        public static ScenarioManifest CurrentManifest { get; set; } = null;

        // Cached scenario manifests loaded from disk. Persists across ClearAll() since
        // manifests are static metadata, not game state. Populated once by ScenarioDialog_Scene0.
        public static IReadOnlyList<ScenarioManifest> LoadedManifests => _loadedManifests;
        private static readonly List<ScenarioManifest> _loadedManifests = new();

        public static void SetLoadedManifests(List<ScenarioManifest> manifests)
        {
            _loadedManifests.Clear();
            if (manifests != null)
                _loadedManifests.AddRange(manifests);
        }

        /// <summary>
        /// Resolves a scenario id to its discovered manifest, or null if no installed scenario declares it.
        ///
        /// ⚠ A scenario id is PERMANENT ONCE SHIPPED (§7.1) precisely so this lookup keeps working across
        /// patches: saves reference scenarios by id, so renaming or removing one strands every save that
        /// names it. Callers must treat null as "content missing", never as "empty campaign".
        /// </summary>
        public static ScenarioManifest FindManifestById(string scenarioId)
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
                return null;

            foreach (ScenarioManifest manifest in _loadedManifests)
            {
                if (string.Equals(manifest.ScenarioId, scenarioId, StringComparison.OrdinalIgnoreCase))
                    return manifest;
            }

            return null;
        }

        #endregion // Properties

        #region Loss Ledger (printer P5)

        // ────────────────────────────────────────────────────────────────────────────────────────────
        // EQUIPMENT LOSS LEDGER — the accounting behind the §24.8 loss report (printer P6).
        //
        // THE MODEL (Bob's, ratified 2026-07-25): HIT POINTS ALREADY ARE EQUIPMENT. A unit's
        // RegimentProfile.TotalIntelStats is its FULL-STRENGTH roster of weapon systems — the intel stats
        // of its deployed/mobile/embarked WeaponProfiles, summed — and §12.2.6 scales those linearly by
        // currentHP/maxHP for display. So HP lost converts directly into weapon systems lost. There is no
        // second model to keep in sync; this is a reading of the one that already exists.
        //
        // ⚠ KEYED BY WeaponType, NOT by display bucket. The report's rows (Men/Tanks/AFVs/Guns/Aircraft/
        // Helicopters) are a rollup performed at RENDER time through the same name-prefix logic
        // RegimentProfile.GetIntelReport() uses, so the loss report and the intel report cannot drift
        // apart. Per-type granularity ("18 T-72A lost") then comes free later at no extra cost.
        //
        // ⚠ THE VALUES ARE float AND THAT IS LOAD-BEARING — do not "tidy" this to int. Rounding per damage
        // event silently destroys everything small: a unit holding 3 tanks that takes 1 HP of 40
        // contributes 3 × 0.025 = 0.075, which rounds to ZERO, and the unit can be ground to death having
        // reported no tank losses at all. Accumulate in float; round ONCE when building the report.
        //
        // ⚠ STATIC, NOT INSTANCE, AND DELIBERATELY SO. CombatUnit.TakeDamage books into this, and
        // GameDataManager.Instance LAZY-CREATES a GameObject — so an instance call from a plain model class
        // would spawn a manager out of every headless EditorTest that damages a unit. This is the same trap
        // PrinterMessage hit in the printer pass (solved there with an injected HeaderProvider); statics
        // sidestep it entirely, and GDM already keeps its global state static.
        // ────────────────────────────────────────────────────────────────────────────────────────────

        private static readonly Dictionary<Side, Dictionary<WeaponType, float>> _lossLedger = new()
        {
            { Side.Player, new Dictionary<WeaponType, float>() },
            { Side.AI,     new Dictionary<WeaponType, float>() }
        };

        // DAILY ledger — the same booking, reset at each turn boundary, for the per-turn loss report.
        //
        // ⚠ A SECOND ACCUMULATOR, not a diff against a snapshot of the cumulative one. A snapshot-and-
        // subtract would look cheaper but goes wrong the moment the cumulative ledger is itself cleared or
        // restored (new battle, save load), because the baseline and the total then disagree with no way to
        // tell. Two independent accumulators fed from the same booking cannot drift apart.
        private static readonly Dictionary<Side, Dictionary<WeaponType, float>> _dailyLossLedger = new()
        {
            { Side.Player, new Dictionary<WeaponType, float>() },
            { Side.AI,     new Dictionary<WeaponType, float>() }
        };

        /// <summary>
        /// Books the weapon systems represented by <paramref name="hitPointsLost"/> against the unit's own
        /// side. Called from <see cref="CombatUnit.TakeDamage"/>, which is the single funnel every damage
        /// source in the game already passes through.
        ///
        /// ⚠ FED FROM DAMAGE, NEVER FROM UNIT REMOVAL. Destruction then needs no special case — a unit
        /// driven 40 → 0 books 100% of its equipment across however many events got it there. Conversely a
        /// unit REMOVED without being destroyed is not a loss and must not be booked: shatter withdrawal
        /// (§7.9.6.4), air units returning to base, and §11.7.2 aircraft evacuation are all removals.
        /// Hooking damage rather than removal gets that distinction for free.
        /// ⚠ SURRENDER IS THE ONE EXCEPTION (§7.9.6a) — a surrendering unit is lost without necessarily
        /// being damaged to zero, so its REMAINING equipment must be booked explicitly at the surrender
        /// site via <see cref="RecordRemainingEquipmentAsLost"/>.
        /// </summary>
        public static void RecordEquipmentLosses(CombatUnit unit, float hitPointsLost)
        {
            try
            {
                if (unit == null || hitPointsLost <= 0f)
                    return;

                float maxHitPoints = unit.HitPoints.Max;
                if (maxHitPoints <= 0f)
                    return;

                // Clamped because a single blow can exceed the unit's remaining HP; a unit cannot lose
                // more than all of its equipment.
                float lostFraction = Mathf.Clamp01(hitPointsLost / maxHitPoints);

                BookLosses(unit, lostFraction);
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(GameDataManager), nameof(RecordEquipmentLosses), e);
            }
        }

        /// <summary>
        /// Books ALL of a unit's currently-surviving equipment as lost, for the case where a unit is lost
        /// without being damaged to zero — surrender (§7.9.6a).
        /// ⚠ Call this INSTEAD of, not in addition to, letting the unit take lethal damage: the fraction
        /// booked here is what the unit still has, so calling both would double-count.
        /// </summary>
        public static void RecordRemainingEquipmentAsLost(CombatUnit unit)
        {
            try
            {
                if (unit == null)
                    return;

                float maxHitPoints = unit.HitPoints.Max;
                if (maxHitPoints <= 0f)
                    return;

                BookLosses(unit, Mathf.Clamp01(unit.HitPoints.Current / maxHitPoints));
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(GameDataManager), nameof(RecordRemainingEquipmentAsLost), e);
            }
        }

        /// <summary>
        /// Reads one side's running losses. Values are FRACTIONAL and deliberately un-rounded — the caller
        /// rounds once, at render time (see the float warning above).
        /// </summary>
        public static IReadOnlyDictionary<WeaponType, float> GetLossLedger(Side side) => _lossLedger[side];

        /// <summary>
        /// Reads one side's losses SINCE THE CURRENT TURN BEGAN. Fractional and un-rounded, like the
        /// cumulative ledger.
        /// </summary>
        public static IReadOnlyDictionary<WeaponType, float> GetDailyLossLedger(Side side) => _dailyLossLedger[side];

        /// <summary>
        /// Resets the DAILY ledger, beginning a new reporting day. Called from
        /// <c>BattleManager.SetTurn</c> — the single place the turn number changes.
        /// ⚠ Leaves the cumulative ledger untouched; that is the entire distinction between the two.
        /// </summary>
        public static void StartNewDailyLossPeriod()
        {
            foreach (var ledger in _dailyLossLedger.Values)
                ledger.Clear();
        }

        /// <summary>Empties BOTH ledgers, both sides. Called on <see cref="ClearAll"/> — losses are per-battle.</summary>
        public static void ClearLossLedger()
        {
            foreach (var ledger in _lossLedger.Values)
                ledger.Clear();

            StartNewDailyLossPeriod();
        }

        /// <summary>
        /// Adds <paramref name="lostFraction"/> of the unit's full-strength roster to its side's ledger.
        /// </summary>
        private static void BookLosses(CombatUnit unit, float lostFraction)
        {
            if (lostFraction <= 0f)
                return;

            // The unit's FULL-STRENGTH weapon systems. TotalIntelStats is never HP-scaled at rest — the
            // scaling happens in CombatUnit.ApplyEquipmentBuckets at display time — which is exactly what
            // makes it the correct multiplicand here.
            Dictionary<WeaponType, int> fullStrengthStats = unit.RegimentProfile?.TotalIntelStats;
            if (fullStrengthStats == null || fullStrengthStats.Count == 0)
                return;

            // ⚠ Booked into BOTH accumulators from the one place, which is what keeps the daily figures a
            // true subset of the cumulative ones. A second booking call site would eventually feed one and
            // not the other.
            Dictionary<WeaponType, float> cumulative = _lossLedger[unit.Side];
            Dictionary<WeaponType, float> daily = _dailyLossLedger[unit.Side];

            foreach (KeyValuePair<WeaponType, int> entry in fullStrengthStats)
            {
                float lost = entry.Value * lostFraction;
                if (lost <= 0f)
                    continue;

                cumulative[entry.Key] = cumulative.TryGetValue(entry.Key, out float running)
                    ? running + lost
                    : lost;

                daily[entry.Key] = daily.TryGetValue(entry.Key, out float runningDaily)
                    ? runningDaily + lost
                    : lost;
            }
        }

        #endregion // Loss Ledger (printer P5)

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

            // PrepareBattle core systems early
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

                // PrepareBattle static databases
                WeaponProfileDB.Initialize();
                CombatUnitDB.Initialize();

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

        /// <summary>
        /// Gets the first combat unit located at the specified map position, or null if none.
        /// </summary>
        public CombatUnit GetUnitAtPosition(Position2D position)
        {
            try
            {
                return _combatUnits.Values.FirstOrDefault(u => u.MapPos == position);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetUnitAtPosition), e);
                return null;
            }
        }

        /// <summary>
        /// Clears all selection state (hex, unit, leader).
        /// </summary>
        public static void ClearSelection()
        {
            SelectedHex = NoHexSelected;
            SelectedHexData = null;
            SelectedUnit = null;
            SelectedLeader = null;
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

        #region Occupancy Cache

        /// <summary>
        /// Builds the occupancy cache by scanning all registered units grouped by MapPos.
        /// Call at turn start and after every completed move order.
        /// </summary>
        public void BuildOccupancyCache()
        {
            try
            {
                _occupancyCache = new Dictionary<Position2D, List<CombatUnit>>();

                foreach (var unit in _combatUnits.Values)
                {
                    if (unit.IsDestroyed()) continue;

                    if (!_occupancyCache.TryGetValue(unit.MapPos, out var list))
                    {
                        list = new List<CombatUnit>();
                        _occupancyCache[unit.MapPos] = list;
                    }
                    list.Add(unit);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(BuildOccupancyCache), e);
            }
        }

        /// <summary>
        /// Clears the occupancy cache, forcing a rebuild on next query.
        /// </summary>
        public void InvalidateOccupancy()
        {
            _occupancyCache = null;
        }

        private void EnsureOccupancyCache()
        {
            if (_occupancyCache == null)
                BuildOccupancyCache();
        }

        /// <summary>
        /// Returns all units at the given hex position.
        /// </summary>
        public List<CombatUnit> GetUnitsAtHex(Position2D pos)
        {
            try
            {
                EnsureOccupancyCache();
                return _occupancyCache.TryGetValue(pos, out var list) ? list : new List<CombatUnit>();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetUnitsAtHex), e);
                return new List<CombatUnit>();
            }
        }

        /// <summary>
        /// Returns the ground unit at the given position, or null if none.
        /// </summary>
        public CombatUnit GetGroundUnitAtHex(Position2D pos)
        {
            try
            {
                var units = GetUnitsAtHex(pos);
                foreach (var u in units)
                {
                    if (!u.IsAirUnit) return u;
                }
                return null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetGroundUnitAtHex), e);
                return null;
            }
        }

        /// <summary>
        /// Returns the air unit at the given position, or null if none.
        /// </summary>
        public CombatUnit GetAirUnitAtHex(Position2D pos)
        {
            try
            {
                var units = GetUnitsAtHex(pos);
                foreach (var u in units)
                {
                    if (u.IsAirUnit) return u;
                }
                return null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAirUnitAtHex), e);
                return null;
            }
        }

        /// <summary>
        /// Returns true if a friendly ground unit occupies the hex.
        /// </summary>
        public bool IsHexOccupiedByFriendlyGround(Position2D pos, Side side)
        {
            var ground = GetGroundUnitAtHex(pos);
            return ground != null && ground.Side == side;
        }

        /// <summary>
        /// Returns true if an enemy ground unit occupies the hex.
        /// </summary>
        public bool IsHexOccupiedByEnemyGround(Position2D pos, Side side)
        {
            var ground = GetGroundUnitAtHex(pos);
            return ground != null && ground.Side != side;
        }

        #endregion // Occupancy Cache

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

                // Losses are per-battle: a new scenario starts from zero, or the previous battle's
                // casualties bleed into the next one's report.
                ClearLossLedger();

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
                return CombatUnitDB.IsInitialized &&
                       WeaponProfileDB.IsInitialized;
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
                return CombatUnitDB.GetUnitTemplate(templateId);
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
                return CombatUnitDB.CreateUnitFromTemplate(templateId, unitName);
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
                return CombatUnitDB.GetTemplatesByNationality(nationality);
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
                return CombatUnitDB.GetTemplatesByClassification(classification);
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
                return CombatUnitDB.HasUnitTemplate(templateId);
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
                return CombatUnitDB.TemplateCount;
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
                return CombatUnitDB.GetAllTemplateIds();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAllTemplateIds), e);
                return new List<string>();
            }
        }

        /// <summary>
        /// Retrieves a weapon profile by its enum identifier.
        /// </summary>
        public static WeaponProfile GetWeaponProfile(WeaponType weaponProfileID)
        {
            try
            {
                return WeaponProfileDB.GetWeaponProfile(weaponProfileID);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetWeaponProfile), e);
                return null;
            }
        }

        /// <summary>
        /// Gets the total number of weapon profiles in the database.
        /// </summary>
        public static int GetWeaponProfileCount()
        {
            try
            {
                return WeaponProfileDB.ProfileCount;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetWeaponProfileCount), e);
                return 0;
            }
        }

        #endregion // Static Database Helpers
    }
}
