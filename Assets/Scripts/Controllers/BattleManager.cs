using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Helpers;
using HammerAndSickle.Core.Map;
using HammerAndSickle.Helpers;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Renderers;
using HammerAndSickle.Renderers.Chunked;
using HammerAndSickle.Services;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Manages battle flow, turn progression, and scenario victory conditions.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(BattleManager);

        #region Singleton

        private static BattleManager _instance;

        /// <summary>
        /// Singleton instance with Unity-compliant lazy initialization.
        /// </summary>
        public static BattleManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find existing instance in scene
                    _instance = FindAnyObjectByType<BattleManager>();

                    // Create new instance if none exists
                    if (_instance == null)
                    {
                        GameObject go = new("BattleManager");
                        _instance = go.AddComponent<BattleManager>();
                    }
                }
                return _instance;
            }
        }

        #endregion // Singleton

        #region Fields

        private bool _isInitialized = false;

        // ----------------------------------------------------------------------------
        // Inspector-assigned UI references.
        // Wire these up in the Unity Inspector on the BattleManager GameObject in the
        // battle scene. They drive the player-facing turn/phase HUD and the end-turn
        // button. All three are required for the turn flow to be visible and operable.
        // ----------------------------------------------------------------------------

        [Header("Turn HUD References")]
        // TMP_Text that displays the current turn in the format "Turn X of Y".
        // Turn 0 represents the deployment phase before the first played turn.
        [SerializeField] private TMP_Text _turnText;

        // Parent panel that hosts the phase text. Acts as a "turn processing"
        // indicator — shown during Deployment, Upkeep, AI_Turn, and TurnBoundary to tell
        // the player something other than their own turn is happening, and hidden
        // during PlayerTurn so it does not clutter the HUD while they're acting.
        [SerializeField] private GameObject _turnProcessingPanel;

        // TMP_Text that displays the current battle phase (e.g. "Deployment",
        // "Enemy Turn", "Processing..."). Lives as a child of _turnProcessingPanel,
        // so toggling the panel automatically hides/shows this text as well.
        [SerializeField] private TMP_Text _phaseText;

        // Button the player clicks to end their turn (or to leave deployment).
        // Disabled automatically while non-player phases are running.
        [SerializeField] private Button _endTurnButton;

        // ----------------------------------------------------------------------------
        // Coroutine pacing knobs. The turn sequence pauses briefly between phases so
        // the player can read the phase HUD and any printer messages we emit. Tunable
        // from the Inspector without recompiling.
        // ----------------------------------------------------------------------------

        [Header("Turn Pacing (seconds)")]
        // Pause held on each phase transition during the turn sequence so the player
        // can register what is happening before the next phase begins.
        [SerializeField] private float _phaseTransitionDelay = 0.6f;

        // Placeholder dwell time for the AI turn until real AI logic exists.
        [SerializeField] private float _aiTurnPlaceholderDelay = 1.0f;

        // ----------------------------------------------------------------------------
        // Coroutine bookkeeping. Only one turn sequence may be in flight at a time;
        // the field exists so we can stop it deterministically when the battle ends.
        // ----------------------------------------------------------------------------
        private Coroutine _turnSequenceCoroutine;

        // True once a terminal condition has been reached. The turn sequence coroutine
        // checks this between phases and bails out if it flips mid-sequence (e.g. the
        // player captures the final objective during their turn).
        private bool _battleEnded = false;

        #endregion // Fields

        #region Properties

        // Indicates whether the battle manager has been fully initialized.
        public bool IsReady => _isInitialized;

        /// --------------------
        /// Turn Management
        /// --------------------

        // Turn 0 == deployment phase. Turns 1..MaxTurnNumber are the played turns.
        public int CurrentTurnNumber { get; private set; } = 0;
        public int MaxTurnNumber { get; private set; } = 20;
        public BattlePhase CurrentPhase { get; private set; } = BattlePhase.NotStarted;

        /// --------------------
        /// Conditions
        /// --------------------

        public WeatherCondition CurrentWeather { get; private set; } = WeatherCondition.Clear;

        // Per-scenario Deployment fielding budget (§20.1 / §35.4), from the manifest.
        // (Replaces the retired MaxNumberCoreUnitAllowed / manifest.maxCoreUnits.)
        public int DeploymentPointCap { get; private set; } = 0;

        /// --------------------
        /// Objective Tracking
        /// --------------------

        public int ObjectiveHexesOccupied { get; private set; } = 0;
        public int ObjectiveHexesUnoccupied { get; private set; } = 0;
        public int TotalObjectiveHexes { get; private set; } = 0;

        /// --------------------
        /// Battle Statistics
        /// --------------------

        public BattleResult CurrentResult { get; private set; } = BattleResult.Ongoing;
        public int PrestigeEarned { get; private set; } = 0;
        public int PrestigeSpent { get; private set; } = 0;
        public int CurrentPrestige { get; private set; } = 0;

        // TODO: Loss tracking system
        // Track player unit losses (destroyed units)
        // Track AI unit losses (destroyed units)
        // Track unit damage statistics
        // Track kill/loss ratios by unit type
        // Track experience gained by surviving units

        /// --------------------
        /// Battle Configuration
        /// --------------------

        public bool IsCampaignBattle { get; private set; } = false;
        public string ScenarioID { get; private set; } = string.Empty;

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

            InitializeBattleManager();

            // Hook the inspector-assigned end-turn button. Done here (rather than
            // Start) so the binding is in place before any other system can poke us.
            // Null-check is defensive: if a developer hasn't wired the button yet, we
            // log and continue rather than crashing the scene load.
            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
            }
            else
            {
                Debug.LogWarning($"{CLASS_NAME}.Awake: _endTurnButton not assigned in Inspector — turn flow will be inert.");
            }
        }

        private void OnDestroy()
        {
            // Unhook the button so we don't leak the listener through scene reloads.
            // Always paired with the AddListener above to keep subscription hygiene clean.
            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.RemoveListener(OnEndTurnButtonClicked);
            }

            // Stop any in-flight turn sequence so the coroutine doesn't keep running
            // after this MonoBehaviour is destroyed.
            if (_turnSequenceCoroutine != null)
            {
                StopCoroutine(_turnSequenceCoroutine);
                _turnSequenceCoroutine = null;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion // Unity Lifecycle

        #region Initialization

        /// <summary>
        /// Initializes the battle manager with default settings.
        /// </summary>
        private void InitializeBattleManager()
        {
            try
            {
                if (_isInitialized)
                    return;

                // Set default values. Turn 0 is the pre-battle deployment slot; the
                // first played turn is Turn 1. CurrentPhase stays NotStarted until
                // SetupBattleManagerData() finishes loading the scenario, at which
                // point we transition into Deployment.
                CurrentTurnNumber = 0;
                CurrentPhase = BattlePhase.NotStarted;
                CurrentWeather = WeatherCondition.Clear;
                CurrentResult = BattleResult.Ongoing;

                _isInitialized = true;
                AppService.CaptureUiMessage("BattleManager initialized successfully");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(InitializeBattleManager), ex);
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Ensures the BattleManager singleton exists. Call this at battle startup.
        /// </summary>
        public static void EnsureExists()
        {
            if (_instance == null)
            {
                _ = Instance; // Forces creation through the getter
            }
        }

        /// <summary>
        /// Sets up the battle manager data by loading the hex map and order of battle (OOB) files  based on the current
        /// scenario manifest.
        /// </summary>
        /// an error occurs during the setup.</returns>
        public bool SetupBattleManagerData()
        {
            // Destroy existing hex map if any
            if (GameDataManager.CurrentHexMap != null)
            {
                GameDataManager.CurrentHexMap.Dispose();
                GameDataManager.CurrentHexMap = null;
            }

            // Check for a valid ScenarioManifest
            if (GameDataManager.CurrentManifest == null)
            {
                Debug.LogError("BattleManager.SetupBattleManagerData: No valid ScenarioManifest found.");
                return false;
            }
                

            // Load the hex map from the specified scenario manifest
            if (!MapLoader.LoadMapFile(GameDataManager.CurrentManifest))
            {
                Debug.LogError($"BattleManager.SetupBattleManagerData: Failed to load map file: {GameDataManager.CurrentManifest.MapFilename}");
                return false;
            }

            // Initialize the coordinate system with the loaded map's dimensions.
            // HexGridSystem is required by the chunk renderer and any consumer that
            // does hex↔world math; MapLoader does not call this itself.
            var mapSize = GameDataManager.CurrentMapSize;
            HexGridSystem.Instance.Initialize(mapSize.IntX, mapSize.IntY);

            // Build the chunk-based terrain. Null-check so the scene still runs if the
            // HexChunkRenderer GameObject is not yet present in the scene hierarchy.
            if (HexChunkRenderer.Instance != null)
            {
                HexChunkRenderer.Instance.SetActiveTerrainSet(GameDataManager.CurrentMapTheme);
                HexChunkRenderer.Instance.BuildAllChunks(GameDataManager.CurrentHexMap, HexGridSystem.Instance);
            }
            else
            {
                Debug.LogWarning("BattleManager.SetupBattleManagerData: HexChunkRenderer not found in scene — terrain will not render.");
            }

            // Refresh the hex map renderer (draws outlines, icons, labels on top of the chunked terrain).
            HexGridRenderer.Instance.RefreshMap();

            // Load the order of battle (OOB) file based on whether it's a campaign or stand-alone scenario
            if (GameDataManager.CurrentManifest.IsCampaignScenario)
            {
                if(!OOBFileLoader.LoadCampaignOob(GameDataManager.CurrentManifest.OobFilename))
                {
                    Debug.LogError($"BattleManager.SetupBattleManagerData: Failed to load campaign OOB file: {GameDataManager.CurrentManifest.OobFilename}");
                    return false;
                }
            }
            else
            {
                // Load the stand-alone oob file.
                if (!OOBFileLoader.LoadStandaloneOob(GameDataManager.CurrentManifest.OobFilename))
                {
                    Debug.LogError($"BattleManager.SetupBattleManagerData: Failed to load OOB file: {GameDataManager.CurrentManifest.OobFilename}");
                    return false;
                }
            }

            // Grab and store other data from the scenario manifest
            GrabManifestData();

            // Fog-of-war reset (fix 2026-07-06): OOB files can carry stale/spurious Spotted values, and
            // RecomputeAllSpotting only ever INCREMENTS — without this, "spotted" enemies from the data file
            // render from Deployment onward. Zero every AI unit, then run the initial sweep so only enemies
            // genuinely inside player spotting range start the battle visible.
            foreach (var aiUnit in GameDataManager.Instance.GetAIUnits())
                aiUnit.SetSpottedLevel(SpottedLevel.Level0);
            SpottingService.RecomputeAllSpotting();

            // Redraw all map icons now that units are loaded
            if (GameIconRenderer.Instance == null || !GameIconRenderer.Instance.IsInitialized)
            {
                Debug.LogWarning($"{CLASS_NAME}.SetupBattleManagerData: GameIconRenderer not ready — skipping map icon redraw.");
            }
            else
            {
                EventManager.Instance.RaiseRedrawMapIcons();
            }

            // Battle data is now loaded. Reset transient battle state and enter the
            // Deployment phase. The HUD will read "Turn 0 of {MaxTurnNumber}" and the
            // end-turn button is the player's signal that deployment is finished.
            _battleEnded = false;
            CurrentResult = BattleResult.Ongoing;
            PrestigeEarned = 0;
            PrestigeSpent = 0;

            SetTurn(0);
            SetPhase(BattlePhase.Deployment);

            // Seed objective tracking from the loaded map so the §17 victory check has real
            // totals to compare against as hexes flip during play.
            InitializeObjectivesFromMap();

            // Open the battle framed on the player's main supply depot (view only — no
            // selection). Done after units load so the depot exists to center on.
            CenterCameraOnStart();

            return true;
        }

        /// <summary>
        /// Counts objective hexes on the loaded map and seeds the held/total counters (player = Red,
        /// §4.7.1). Recomputed from the map rather than tracked incrementally at load so the totals are
        /// always truthful; movement-driven captures then adjust the counters via CaptureObjective /
        /// LoseObjective (§17.5). Safe no-op if no map is loaded.
        /// </summary>
        private void InitializeObjectivesFromMap()
        {
            try
            {
                var map = GameDataManager.CurrentHexMap;
                int total = 0, occupied = 0;

                if (map != null)
                {
                    foreach (var hex in map)
                    {
                        if (hex == null || !hex.IsObjective) continue;
                        total++;
                        if (hex.TileControl == TileControl.Red) occupied++;
                    }
                }

                TotalObjectiveHexes = total;
                ObjectiveHexesOccupied = occupied;
                ObjectiveHexesUnoccupied = total - occupied;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(InitializeObjectivesFromMap), ex);
            }
        }

        /// <summary>
        /// Centers the map on the player's main supply depot at battle start (A7). View only —
        /// no unit is selected. If the player fields more than one main depot, one is chosen at
        /// random (UnityEngine.Random — presentation only, NOT the seeded combat RNG). Falls back
        /// to any living player unit, then the map center, if no main depot is present.
        /// </summary>
        private void CenterCameraOnStart()
        {
            try
            {
                if (CameraService.Instance == null) return;

                var players = GameDataManager.Instance.GetPlayerUnits();

                // Primary: a player main supply depot (IsMainDepot = IsBase && DepotCategory.Main).
                var mainDepots = players
                    .Where(u => u != null && !u.IsDestroyed() && u.IsMainDepot)
                    .ToList();

                Position2D target;
                if (mainDepots.Count > 0)
                {
                    target = mainDepots[UnityEngine.Random.Range(0, mainDepots.Count)].MapPos;
                }
                else
                {
                    // Fallback: any living player unit, else the map center.
                    var anyPlayer = players.FirstOrDefault(u => u != null && !u.IsDestroyed());
                    if (anyPlayer != null)
                    {
                        target = anyPlayer.MapPos;
                    }
                    else
                    {
                        var size = GameDataManager.CurrentMapSize;
                        target = new Position2D(size.IntX / 2, size.IntY / 2);
                    }
                }

                CameraService.Instance.CenterOnPosition(target);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CenterCameraOnStart), ex);
            }
        }

        /// <summary>
        /// Retrieves and assigns data from the current game manifest to the corresponding properties.
        /// </summary>
        private void GrabManifestData()
        {
            ScenarioID = GameDataManager.CurrentManifest.ScenarioId;
            IsCampaignBattle = GameDataManager.CurrentManifest.IsCampaignScenario;
            CurrentPrestige = GameDataManager.CurrentManifest.PrestigePool;
            DeploymentPointCap = GameDataManager.CurrentManifest.DeploymentPointCap;
            MaxTurnNumber = GameDataManager.CurrentManifest.MaxTurns;

        }

        #endregion // Initialization

        #region Turn HUD and Phase Transitions

        // ----------------------------------------------------------------------------
        // SetTurn / SetPhase are the *only* places that mutate CurrentTurnNumber and
        // CurrentPhase. Centralizing the writes guarantees the HUD, button gating, and
        // EventManager broadcasts stay in sync with the underlying state. Any code
        // that needs to change the turn or phase MUST go through these methods.
        // ----------------------------------------------------------------------------

        /// <summary>
        /// Updates the current turn number, refreshes the turn TMP_Text, and broadcasts
        /// the change via EventManager.OnBattleTurnAdvanced. Turn 0 represents the
        /// deployment phase; turns 1..MaxTurnNumber are the played turns.
        /// </summary>
        private void SetTurn(int newTurn)
        {
            CurrentTurnNumber = newTurn;
            RefreshTurnText();

            // See EventManager — broadcast so any subscriber (UI, audio, AI) can react.
            if (EventManager.Instance != null)
            {
                EventManager.Instance.RaiseBattleTurnAdvanced(newTurn);
            }
        }

        /// <summary>
        /// Updates the current battle phase, refreshes the phase TMP_Text, gates the
        /// end-turn button (only PlayerTurn and Deployment leave it interactable), and
        /// broadcasts the transition via EventManager.OnBattlePhaseChanged.
        /// </summary>
        private void SetPhase(BattlePhase newPhase)
        {
            CurrentPhase = newPhase;
            RefreshPhaseText();
            RefreshTurnProcessingPanel();
            RefreshEndTurnButtonInteractable();

            // See EventManager — broadcast so any subscriber (UI, audio, AI) can react.
            if (EventManager.Instance != null)
            {
                EventManager.Instance.RaiseBattlePhaseChanged(newPhase);
            }
        }

        /// <summary>
        /// Pushes the current turn counters into the inspector-assigned TMP_Text in
        /// the format "Turn X of Y". Safe no-op if the field is unwired.
        /// </summary>
        private void RefreshTurnText()
        {
            if (_turnText == null) return;
            _turnText.text = $"Turn {CurrentTurnNumber} of {MaxTurnNumber}";
        }

        /// <summary>
        /// Pushes a player-friendly name for the current phase into the inspector-assigned
        /// TMP_Text. Uses GetPhaseDisplayName to translate raw enum values into UI strings.
        /// Safe no-op if the field is unwired.
        /// </summary>
        private void RefreshPhaseText()
        {
            if (_phaseText == null) return;
            _phaseText.text = GetPhaseDisplayName(CurrentPhase);
        }

        /// <summary>
        /// Shows the turn-processing panel during any phase that is not the player's
        /// own turn (Deployment, Upkeep, AI_Turn, TurnBoundary, BattleComplete) and hides
        /// it during PlayerTurn so the HUD stays clean while the player is acting.
        /// Hiding the panel also hides the phase text since the text is parented to it.
        /// Safe no-op if the panel reference is unwired.
        /// </summary>
        private void RefreshTurnProcessingPanel()
        {
            if (_turnProcessingPanel == null) return;

            bool showPanel = CurrentPhase != BattlePhase.PlayerTurn;
            _turnProcessingPanel.SetActive(showPanel);
        }

        /// <summary>
        /// Enables the end-turn button only when the player can legitimately act —
        /// during Deployment (to leave deployment) and during PlayerTurn (to end the
        /// turn). All other phases hard-disable it to prevent double-fires from frame-
        /// perfect clicks. Safe no-op if the button is unwired.
        /// </summary>
        private void RefreshEndTurnButtonInteractable()
        {
            if (_endTurnButton == null) return;

            bool canPlayerAct =
                CurrentPhase == BattlePhase.PlayerTurn ||
                CurrentPhase == BattlePhase.Deployment;

            _endTurnButton.interactable = canPlayerAct && !_battleEnded;
        }

        /// <summary>
        /// Translates a BattlePhase enum value into a player-facing string for the
        /// phase HUD. Keep these short — the HUD field is a single line.
        /// </summary>
        private static string GetPhaseDisplayName(BattlePhase phase) => phase switch
        {
            BattlePhase.NotStarted     => "Not Started",
            BattlePhase.Deployment     => "Deployment",
            BattlePhase.PlayerRefresh  => "Refreshing...",
            BattlePhase.PlayerTurn     => "Your Turn",
            BattlePhase.PlayerUpkeep   => "Processing...",
            BattlePhase.AI_Refresh     => "Refreshing...",
            BattlePhase.AI_Turn        => "Enemy Turn",
            BattlePhase.AI_Upkeep      => "Processing...",
            BattlePhase.TurnBoundary   => "Processing...",
            BattlePhase.BattleComplete => "Battle Over",
            _                          => phase.ToString()
        };

        #endregion // Turn HUD and Phase Transitions

        #region Turn Management

        /// <summary>
        /// Click handler for the inspector-assigned end-turn button. The button is the
        /// player's single point of interaction with the turn flow:
        ///   - During Deployment, clicking it leaves deployment and starts Turn 1.
        ///   - During PlayerTurn, clicking it kicks off the full turn sequence
        ///     (PlayerUpkeep → AI_Turn → AI_Upkeep → TurnBoundary → next PlayerTurn).
        /// The button is hard-disabled the instant the click is processed and only
        /// re-enabled when control returns to the player. This prevents double-fires.
        /// </summary>
        private void OnEndTurnButtonClicked()
        {
            try
            {
                // Hard-disable immediately to block frame-perfect re-clicks before any
                // logic runs. Phase changes will manage the interactable flag from here.
                if (_endTurnButton != null) _endTurnButton.interactable = false;

                // Refuse to do anything if a turn sequence is already in flight.
                if (_turnSequenceCoroutine != null)
                {
                    return;
                }

                if (_battleEnded || CurrentResult != BattleResult.Ongoing)
                {
                    return;
                }

                // Deployment exit: same button advances Turn 0 → Turn 1. No EOT/AI runs
                // on the deployment-exit click — the player simply begins their first
                // played turn, which still runs through PlayerRefresh (§3.3) first so
                // units start the turn with full actions/MP and a fresh spotting sweep.
                if (CurrentPhase == BattlePhase.Deployment)
                {
                    AppService.CaptureUiMessage("Deployment complete — Turn 1 begins.");
                    _turnSequenceCoroutine = StartCoroutine(RunPlayerTurnStart(1));
                    return;
                }

                // Normal mid-battle case: end the player turn and run the rest of the
                // turn sequence as a coroutine so phases can pace themselves and emit
                // visible feedback between transitions.
                if (CurrentPhase == BattlePhase.PlayerTurn)
                {
                    _turnSequenceCoroutine = StartCoroutine(RunTurnSequence());
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnEndTurnButtonClicked), ex);
            }
        }

        /// <summary>
        /// Drives the back half of a turn as a coroutine, then chains into the next turn's
        /// start (§3.1 cycle):
        ///
        ///     PlayerTurn (just ended)
        ///         → PlayerUpkeep   (§3.5 — efficiency recovery; supply chain stubbed)
        ///         → AI_Refresh     (§3.3 — refresh AI side)
        ///         → AI_Turn        (AI moves and fights — placeholder for now)
        ///         → AI_Upkeep      (§3.5 — efficiency recovery for AI)
        ///         → TurnBoundary   (turn counter increment, victory checks)
        ///         → PlayerRefresh  (§3.3 — refresh player side, spotting sweep)
        ///         → PlayerTurn     (new turn) -OR- BattleComplete
        ///
        /// Between every phase transition we yield for _phaseTransitionDelay so the
        /// player can register what is happening on the HUD. The _battleEnded flag
        /// is checked at every yield point so an immediate-win condition (e.g. final
        /// objective captured) can short-circuit the rest of the sequence.
        /// </summary>
        private IEnumerator RunTurnSequence()
        {
            // -------- Post-player Upkeep (§3.5) --------
            SetPhase(BattlePhase.PlayerUpkeep);
            AppService.CaptureUiMessage("Processing end of player turn...");
            ProcessUpkeep(isPlayerSide: true);
            yield return new WaitForSeconds(_phaseTransitionDelay);
            if (_battleEnded) { _turnSequenceCoroutine = null; yield break; }

            // -------- AI Refresh (§3.3) --------
            SetPhase(BattlePhase.AI_Refresh);
            ProcessRefresh(isPlayerSide: false);
            yield return new WaitForSeconds(_phaseTransitionDelay);
            if (_battleEnded) { _turnSequenceCoroutine = null; yield break; }

            // -------- AI Turn --------
            // Placeholder until real AI exists. The phase enters, sits for the
            // configured dwell, and exits. When AI logic lands it slots in here.
            SetPhase(BattlePhase.AI_Turn);
            AppService.CaptureUiMessage("Enemy turn underway...");
            yield return new WaitForSeconds(_aiTurnPlaceholderDelay);
            if (_battleEnded) { _turnSequenceCoroutine = null; yield break; }

            // -------- Post-AI Upkeep (§3.5) --------
            SetPhase(BattlePhase.AI_Upkeep);
            AppService.CaptureUiMessage("Processing end of enemy turn...");
            ProcessUpkeep(isPlayerSide: false);
            yield return new WaitForSeconds(_phaseTransitionDelay);
            if (_battleEnded) { _turnSequenceCoroutine = null; yield break; }

            // -------- Turn Boundary --------
            // Turn counter is incremented at PlayerRefresh (below), *after* both Upkeeps
            // have run. Victory checks (turn-limit and objective) live here so the very
            // last turn can end cleanly without rolling into a phantom Turn (Max+1).
            SetPhase(BattlePhase.TurnBoundary);
            yield return new WaitForSeconds(_phaseTransitionDelay);

            // Turn-limit check: if we just finished the final scheduled turn, the
            // battle ends here instead of advancing into a new player turn.
            if (CurrentTurnNumber >= MaxTurnNumber)
            {
                CompleteBattle();
                _turnSequenceCoroutine = null;
                yield break;
            }

            // Objective check: if anything that ran during the EOTs flipped victory
            // conditions in the player's favor, end the battle now.
            if (CheckVictoryConditions())
            {
                CompleteBattle();
                _turnSequenceCoroutine = null;
                yield break;
            }

            // -------- Next Player Turn (PlayerRefresh → PlayerTurn) --------
            // Hand off to the shared turn-start coroutine. It owns clearing
            // _turnSequenceCoroutine, so this method must not touch it afterward.
            yield return RunPlayerTurnStart(CurrentTurnNumber + 1);
        }

        /// <summary>
        /// Opens a player turn: advances the turn counter, runs PlayerRefresh (§3.3), then
        /// hands control to the player at PlayerTurn. Shared by the deployment-exit click and
        /// the end-of-turn sequence so the §3.3 refresh always precedes a player turn. Clears
        /// _turnSequenceCoroutine on exit (it is the turn flow's final step).
        /// </summary>
        private IEnumerator RunPlayerTurnStart(int turnNumber)
        {
            SetTurn(turnNumber);

            // -------- Player Refresh (§3.3) --------
            SetPhase(BattlePhase.PlayerRefresh);
            ProcessRefresh(isPlayerSide: true);
            yield return new WaitForSeconds(_phaseTransitionDelay);
            if (_battleEnded) { _turnSequenceCoroutine = null; yield break; }

            // -------- Player Turn --------
            SetPhase(BattlePhase.PlayerTurn);
            AppService.CaptureUiMessage($"Turn {turnNumber} of {MaxTurnNumber} — your move.");

            _turnSequenceCoroutine = null;
        }

        /// <summary>
        /// The AI side's belief store (AI-Design-Supplement Part 3 — honest-spotting Option B).
        /// Owned here until the dedicated AI turn driver exists (AI3); fed by the SpottingService
        /// symmetric sweep at AI_Refresh. Scene-scoped like the manager itself; snapshot
        /// serialization is the AI2b-3 work item.
        /// </summary>
        public Models.AI.AIPerceptionState AIPerception { get; private set; } = new Models.AI.AIPerceptionState();

        /// <summary>
        /// Refresh phase (§3.3) for one side. Order per §3.3: action/MP refresh and per-turn
        /// flag reset for every living unit on that side; (out-of-supply consequences §3.3.3 —
        /// HOOK reserved, inert until the supply system lands); spotting decay + recompute
        /// (§3.3.4); weather check (§3.3.6).
        ///
        /// Spotting (§3.3.4) runs only for the player side: SpottedLevel lives on AI units and
        /// is set by player spotters; AI-side fog of war is unmodelled in v1.
        /// </summary>
        private void ProcessRefresh(bool isPlayerSide)
        {
            try
            {
                var units = isPlayerSide
                    ? GameDataManager.Instance.GetPlayerUnits()
                    : GameDataManager.Instance.GetAIUnits();

                // §3.3.1 / §3.3.2 — counters + MP to max; §7.15.8 recovery flags cleared.
                foreach (var u in units)
                {
                    if (u == null || u.IsDestroyed()) continue;
                    RefreshUnitForNewTurn(u);
                }

                // §3.3.3 out-of-supply consequences — HOOK reserved, INERT this pass. No depot
                // distribution exists yet, so applying the 2-tier Efficiency drop + 10% MAX_HP
                // loss now would punish everything immediately. Activated with the supply pass.
                // ApplyOutOfSupplyConsequences(units);

                // §3.3.4 spotting decay + sweep, per side: the player perspective mutates
                // CombatUnit.SpottedLevel; the AI perspective feeds its belief store (Part 3.2)
                // through the symmetric SpottingService sweep — same rules, separate ledger.
                if (isPlayerSide)
                {
                    SpottingService.ProcessSpottingDecay();
                    SpottingService.RecomputeAllSpotting();
                }
                else
                {
                    SpottingService.StepAIPerceptionDecay(AIPerception, CurrentTurnNumber);
                    SpottingService.RecomputeAIPerception(AIPerception, CurrentTurnNumber);
                }

                // §3.3.6 weather check — single-state (Clear) in v1; per-turn weather variance
                // is a future pass (§4.5.5). No-op placeholder.
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ProcessRefresh), ex);
            }
        }

        /// <summary>
        /// Upkeep phase (§3.5) for the side that just finished its turn. Implemented now:
        /// efficiency recovery (§3.5.8) per living unit, driven by whether it moved/fought.
        /// STUBBED pending the supply system: loss tracking (§3.5.1), depot generation /
        /// minor-depot / airbase replenishment (§3.5.4–.6), HCL decay (§3.5.9).
        /// </summary>
        private void ProcessUpkeep(bool isPlayerSide)
        {
            try
            {
                var units = isPlayerSide
                    ? GameDataManager.Instance.GetPlayerUnits()
                    : GameDataManager.Instance.GetAIUnits();

                // §3.5.1 loss tracking — STUB (stats system not built; see RecordPlayerUnitLoss).
                // §3.5.4–.6 depot generation / minor-depot / airbase replenishment — STUB (supply pass).

                // §3.5.8 efficiency recovery: +2 idle / +1 moved / 0 fought, cap Full.
                foreach (var u in units)
                {
                    if (u == null || u.IsDestroyed()) continue;
                    ApplyUpkeepRecovery(u);
                }

                // §3.5.9 HCL decay/recovery — STUB (needs depot supply tracing; supply pass).
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ProcessUpkeep), ex);
            }
        }

        // ----------------------------------------------------------------------------
        // Per-unit turn-boundary helpers. Static + side-effect-only on the passed unit so
        // they are unit-test-friendly (no BattleManager/Unity singleton coupling).
        // ----------------------------------------------------------------------------

        /// <summary>§3.3.1/.2 — reset a single unit's action counters, MP, and per-turn flags.</summary>
        public static void RefreshUnitForNewTurn(CombatUnit unit)
        {
            unit.RefreshAllActions();
            unit.RefreshMovementPoints();
            unit.ResetTurnFlags();
        }

        /// <summary>§3.5.8 — apply Efficiency recovery to a single unit from its moved/fought flags.
        /// (Recovery PAUSE for out-of-supply units, §15.5.3.5, is deferred with the supply pass.)</summary>
        public static void ApplyUpkeepRecovery(CombatUnit unit)
        {
            var recovered = DegradationCheck.ApplyUpkeepRecovery(
                unit.EfficiencyLevel, unit.HasMovedThisTurn, unit.HasFoughtThisTurn);
            unit.SetEfficiencyLevel(recovered);
        }

        /// <summary>
        /// Public entry point for "the player just captured the final objective hex,
        /// end the battle right now". Called by whatever code processes hex capture.
        /// Sets the battle-ended flag so the in-flight turn coroutine (if any) bails
        /// at its next yield point, and triggers CompleteBattle synchronously so the
        /// HUD updates immediately even outside the turn sequence.
        /// </summary>
        public void TriggerImmediateVictory()
        {
            try
            {
                if (_battleEnded) return;
                CompleteBattle();
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(TriggerImmediateVictory), ex);
            }
        }

        #endregion // Turn Management

        #region Battle Status

        /// <summary>
        /// Checks scenario victory conditions. Currently only the "all objectives held"
        /// condition is implemented, since the objective tracking fields already exist.
        /// Turn-limit termination is handled directly by the turn coroutine.
        /// </summary>
        /// <returns>True if a victory condition is met and the battle should end.</returns>
        private bool CheckVictoryConditions()
        {
            try
            {
                // Immediate victory: every objective hex on the map is held by the
                // player. Mirrors Panzer Corps — capture the last objective and the
                // scenario ends right away rather than waiting for the turn limit.
                if (TotalObjectiveHexes > 0 && ObjectiveHexesOccupied >= TotalObjectiveHexes)
                {
                    return true;
                }

                // TODO: additional conditions (unit-loss thresholds, special events).
                return false;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CheckVictoryConditions), ex);
                return false;
            }
        }

        /// <summary>
        /// Terminal state transition. Marks the battle as ended, stops any in-flight
        /// turn coroutine, hard-disables the end-turn button, computes a placeholder
        /// final result, and broadcasts OnBattleEnded so any listening UI/audio system
        /// can react. Idempotent: calling it twice is a no-op.
        /// </summary>
        private void CompleteBattle()
        {
            try
            {
                if (_battleEnded) return;
                _battleEnded = true;

                // Stop the turn coroutine if it's still running. The coroutine also
                // checks _battleEnded at every yield, but stopping it explicitly is
                // cleaner when the trigger came from outside the sequence (e.g.
                // TriggerImmediateVictory called by hex-capture code).
                if (_turnSequenceCoroutine != null)
                {
                    StopCoroutine(_turnSequenceCoroutine);
                    _turnSequenceCoroutine = null;
                }

                // TODO: Calculate final BattleResult based on objectives held, unit
                // losses, and turn used. For now, draw is the placeholder.
                CurrentResult = BattleResult.Draw;

                SetPhase(BattlePhase.BattleComplete);
                AppService.CaptureUiMessage($"Battle complete: {CurrentResult}");

                // See EventManager — broadcast terminal event so listeners can respond.
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.RaiseBattleEnded(CurrentResult);
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CompleteBattle), ex);
            }
        }

        #endregion // Battle Status

        #region Environmental Management

        /// <summary>
        /// Sets the current weather condition.
        /// </summary>
        public void SetWeather(WeatherCondition weather)
        {
            try
            {
                CurrentWeather = weather;
                EventManager.Instance?.RaiseWeatherChanged(weather);
                AppService.CaptureUiMessage($"Weather changed to: {weather}");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetWeather), ex);
            }
        }

        #endregion // Environmental Management

        #region Objective Management

        /// <summary>
        /// Sets the total number of objective hexes for the scenario.
        /// </summary>
        public void SetTotalObjectiveHexes(int total)
        {
            try
            {
                TotalObjectiveHexes = total;
                ObjectiveHexesUnoccupied = total;
                ObjectiveHexesOccupied = 0;
                AppService.CaptureUiMessage($"Total objective hexes set to: {total}");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetTotalObjectiveHexes), ex);
            }
        }

        /// <summary>
        /// Updates objective hex occupation status.
        /// </summary>
        public void UpdateObjectiveStatus(int occupied, int unoccupied)
        {
            try
            {
                ObjectiveHexesOccupied = occupied;
                ObjectiveHexesUnoccupied = unoccupied;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(UpdateObjectiveStatus), ex);
            }
        }

        /// <summary>
        /// Increments occupied objective count (when player captures).
        /// </summary>
        public void CaptureObjective()
        {
            try
            {
                ObjectiveHexesOccupied++;
                ObjectiveHexesUnoccupied--;
                AppService.CaptureUiMessage($"Objective captured! ({ObjectiveHexesOccupied}/{TotalObjectiveHexes})");

                // Panzer-Corps-style immediate end: if that capture took the player
                // to full objective control, end the battle now without waiting for
                // the turn to complete. The turn coroutine (if running) will see
                // _battleEnded at its next yield and bail.
                if (CheckVictoryConditions())
                {
                    TriggerImmediateVictory();
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CaptureObjective), ex);
            }
        }

        /// <summary>
        /// Decrements occupied objective count (when player loses control).
        /// </summary>
        public void LoseObjective()
        {
            try
            {
                ObjectiveHexesOccupied--;
                ObjectiveHexesUnoccupied++;
                AppService.CaptureUiMessage($"Objective lost! ({ObjectiveHexesOccupied}/{TotalObjectiveHexes})");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoseObjective), ex);
            }
        }

        #endregion // Objective Management

        #region Statistics Management

        /// <summary>
        /// Records a player unit loss.
        /// </summary>
        public void RecordPlayerUnitLoss()
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(RecordPlayerUnitLoss), ex);
            }
        }

        /// <summary>
        /// Records an AI unit destroyed.
        /// </summary>
        public void RecordAIUnitDestroyed()
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(RecordAIUnitDestroyed), ex);
            }
        }

        /// <summary>
        /// Adds prestige earned during battle.
        /// </summary>
        public void AddPrestige(int amount)
        {
            try
            {
                PrestigeEarned += amount;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(AddPrestige), ex);
            }
        }

        /// <summary>
        /// Subtracts prestige spent during battle.
        /// </summary>
        public void SpendPrestige(int amount)
        {
            try
            {
                PrestigeSpent += amount;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SpendPrestige), ex);
            }
        }

        #endregion // Statistics Management

        #region State Management

        /// <summary>
        /// Resets the battle manager to initial state.
        /// </summary>
        public void ResetBattle()
        {
            try
            {
                // Stop any in-flight turn sequence so the reset doesn't race a coroutine.
                if (_turnSequenceCoroutine != null)
                {
                    StopCoroutine(_turnSequenceCoroutine);
                    _turnSequenceCoroutine = null;
                }

                _battleEnded = false;
                CurrentResult = BattleResult.Ongoing;
                CurrentWeather = WeatherCondition.Clear;

                ObjectiveHexesOccupied = 0;
                ObjectiveHexesUnoccupied = 0;
                TotalObjectiveHexes = 0;

                PrestigeEarned = 0;
                PrestigeSpent = 0;

                ScenarioID = string.Empty;
                IsCampaignBattle = false;

                // Use the chokepoints so HUD + button + events all stay in sync.
                SetTurn(0);
                SetPhase(BattlePhase.NotStarted);

                AppService.CaptureUiMessage("Battle manager reset");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResetBattle), ex);
            }
        }

        #endregion // State Management
    }
}
