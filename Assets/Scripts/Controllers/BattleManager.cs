using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Helpers;
using HammerAndSickle.Core.Map;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Helpers;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Persistence;
using HammerAndSickle.Renderers;
using HammerAndSickle.Renderers.Chunked;
using HammerAndSickle.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

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
        // Inspector-assigned UI references — the player-facing turn/phase HUD.
        // Wire these up on the BattleManager GameObject in the battle scene.
        // ⚠ There is deliberately NO Button reference here. Since 2026-07-27 (§3.6b) every button is
        // Inspector-wired to a public callback, and a script holds a Button ONLY if it must drive that
        // button's state. BattleManager does not: the end-turn gate lives in `CanEndTurn`, checked inside
        // `OnEndTurnButton` itself, so it holds however the button is wired — or if it is not wired at all.
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
        /// Objective Tracking — RETIRED (prestige pass Stage 3, 2026-08-17)
        /// --------------------
        // The three incremental counters (Occupied/Unoccupied/Total) are DELETED: the recomputed
        // VictoryLedger is the territorial score, and the C6 mission-objective gate reads the stamped
        // hex flags fresh. An incremental counter desyncs the moment a control change takes a path
        // that forgot to update it — which is exactly what happened when flips widened to strongholds
        // while the total still counted authored objectives. Their save-DTO fields drop with the
        // Stage 5 SAVE_VERSION bump.

        // §18.2 income + §17.3 scoring knobs (V7/V9/V10), cached from the manifest like
        // DeploymentPointCap. ⚠ Stage 5 must rule on their save mirror (V11.6) — an in-battle save
        // restores WITHOUT a manifest (§7.3).
        public int PrestigeStipend { get; private set; } = 0;
        public float PrestigeIncomeRate { get; private set; } = 0f;
        public float PrestigeProgressBonusRate { get; private set; } = 0f;
        public float EarlyFinishMultiplier { get; private set; } = 1.25f;
        public float VictoryThresholdMinor { get; private set; } = 0f;
        public float VictoryThresholdMajor { get; private set; } = 0f;
        public float VictoryThresholdDecisive { get; private set; } = 0f;
        public BattleResult RequiredResult { get; private set; } = BattleResult.MinorVictory;

        /// --------------------
        /// Battle Statistics
        /// --------------------

        public BattleResult CurrentResult { get; private set; } = BattleResult.Ongoing;

        // §18 wallet (V8, 2026-08-17). One spendable balance + two tallies, arithmetic in the
        // headless-testable PrestigeWallet — before this, AddPrestige credited PrestigeEarned while
        // purchases were meant to draw on CurrentPrestige, and the two fields never met.
        private readonly PrestigeWallet _prestige = new PrestigeWallet();
        public int PrestigeEarned => _prestige.Earned;
        public int PrestigeSpent => _prestige.Spent;
        public int CurrentPrestige => _prestige.Current;

        /// --------------------
        /// Victory ledger (V5 — derived, recomputed once per turn + once at battle start, NEVER accumulated)
        /// --------------------

        /// <summary>Last computed victory-value distribution. Refreshed in ProcessUpkeep; UI/debug read it.</summary>
        public VictoryLedger CurrentLedger { get; private set; }

        /// <summary>
        /// The player's share of map victory value at battle start — the anchor the §17.3 scoring ladder
        /// mirrors around (V9.1). Captured ONCE after map+OOB load; cannot be recomputed after the first
        /// flip, so it must persist (V12, Stage 5).
        /// </summary>
        public float StartingPlayerShare { get; private set; }

        /// <summary>
        /// Highest PlayerValue ever held this battle — the V7.2 anti-farm mark: the progress bonus pays
        /// only on value above this, so losing ground and retaking it pays nothing twice. Persists (V12).
        /// </summary>
        public float HighWaterVictoryValue { get; private set; }

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
        }

        private void OnDestroy()
        {

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

            // Fit the background room art to the loaded map's footprint (any map size). The
            // glowing table window is baked into the image; the fitter moves/scales the
            // background only — the map never moves. Null-tolerant like the chunk renderer.
            var backgroundFitter = FindAnyObjectByType<BattleBackgroundFitter>();
            if (backgroundFitter != null)
            {
                backgroundFitter.FitToMap(mapSize.IntX, mapSize.IntY);
            }
            else
            {
                Debug.LogWarning("BattleManager.SetupBattleManagerData: BattleBackgroundFitter not found in scene — background not fitted to map size.");
            }

            // Camera scroll bounds, derived from the map that actually loaded (G5).
            ApplyDerivedScrollBounds(mapSize);

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

            // Load the order of battle from the scenario's own content folder. Campaign and standalone
            // scenarios load identically since 2026-07-27 — the manifest knows where it came from.
            if (!OOBFileLoader.LoadOob(GameDataManager.CurrentManifest))
            {
                Debug.LogError($"{CLASS_NAME}.SetupBattleManagerData: Failed to load OOB file: " +
                               $"{GameDataManager.CurrentManifest.GetOobFilePath()}");
                return false;
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
            // (Prestige needs no reset here — GrabManifestData seeded the wallet, which zeroes both tallies.)

            SetTurn(0);
            SetPhase(BattlePhase.Deployment);

            // V5.3: capture the victory-value baseline the §17.3 scoring ladder mirrors around.
            // Once-only — the starting share CANNOT be recomputed after the first flip.
            // (Replaced InitializeObjectivesFromMap + the counter trio, retired Stage 3.)
            CaptureStartingLedger();

            // Open the battle framed on the player's main supply depot (view only — no
            // selection). Done after units load so the depot exists to center on.
            CenterCameraOnStart();

            return true;
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
            _prestige.Seed(GameDataManager.CurrentManifest.PrestigePool);
            DeploymentPointCap = GameDataManager.CurrentManifest.DeploymentPointCap;
            MaxTurnNumber = GameDataManager.CurrentManifest.MaxTurns;
            PrestigeStipend = GameDataManager.CurrentManifest.PrestigeStipend;
            PrestigeIncomeRate = GameDataManager.CurrentManifest.PrestigeIncomeRate;
            PrestigeProgressBonusRate = GameDataManager.CurrentManifest.PrestigeProgressBonusRate;
            EarlyFinishMultiplier = GameDataManager.CurrentManifest.EarlyFinishMultiplier;
            VictoryThresholdMinor = GameDataManager.CurrentManifest.VictoryThresholdMinor;
            VictoryThresholdMajor = GameDataManager.CurrentManifest.VictoryThresholdMajor;
            VictoryThresholdDecisive = GameDataManager.CurrentManifest.VictoryThresholdDecisive;
            RequiredResult = GameDataManager.CurrentManifest.RequiredResult;
        }

        /// <summary>
        /// Captures the battle-start victory ledger and the two persistent anchors derived from it:
        /// <see cref="StartingPlayerShare"/> (the V9.1 mirror anchor) and the initial
        /// <see cref="HighWaterVictoryValue"/> (the V7.2 anti-farm mark). Called exactly once per battle,
        /// from SetupBattleManagerData after map + OOB load — everything else recomputes.
        /// </summary>
        private void CaptureStartingLedger()
        {
            try
            {
                var ledger = VictoryLedger.Compute(GameDataManager.CurrentHexMap);
                CurrentLedger = ledger;
                StartingPlayerShare = ledger.PlayerShare;
                HighWaterVictoryValue = ledger.PlayerValue;

                // V5.4: a zero-value map is legitimate (every currently shipped map) — the scenario
                // simply declares no scoring, and V9 grades it Draw. Loud, not fatal.
                if (ledger.TotalValue <= 0f)
                    AppService.CaptureUiMessage("Map carries no victory value — scenario will not be scored.");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CaptureStartingLedger), ex);
            }
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

            // A new turn is a new reporting day for the loss report (printer P6). This sits here precisely
            // because SetTurn is the ONLY place the turn number changes — anywhere else and the daily
            // figures would drift out of step with the turn they claim to cover.
            GameDataManager.StartNewDailyLossPeriod();

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
        /// True while the player may legitimately end the turn — during Deployment (to leave it) and
        /// during PlayerTurn (to end it), and never once the battle is over.
        /// This used to drive a serialized Button's `interactable` flag. BattleManager no longer holds a
        /// Button (§3.6b, 2026-07-27), so the check moved INTO <see cref="OnEndTurnButton"/> where it
        /// gates the action itself — a guard on the logic holds however the button is wired, or if it is
        /// not wired at all.
        /// </summary>
        private bool CanEndTurn =>
            (CurrentPhase == BattlePhase.PlayerTurn || CurrentPhase == BattlePhase.Deployment)
            && !_battleEnded;

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
        /// END TURN button callback — Bob wires this to the Button's onClick in the Inspector (§3.6b).
        /// The button is the player's single point of interaction with the turn flow:
        ///   - During Deployment, clicking it leaves deployment and starts Turn 1.
        ///   - During PlayerTurn, clicking it kicks off the full turn sequence
        ///     (PlayerUpkeep → AI_Turn → AI_Upkeep → TurnBoundary → next PlayerTurn).
        /// ⚠ PUBLIC NAME IS A CONTRACT — a UnityEvent binds by method-name STRING, so renaming this
        /// silently breaks the Inspector wiring with no compile error. See CLAUDE.md §2.13.
        /// ⚠ Every re-entry guard here is a LOGIC guard, deliberately: BattleManager no longer holds the
        /// Button, so it cannot disable it, and a click arriving at the wrong moment must be refused on
        /// its own merits rather than prevented by the UI.
        /// </summary>
        public void OnEndTurnButton()
        {
            try
            {
                // Wrong phase, or the battle is over. Replaces the old hard-disable of the button's
                // interactable flag, which could not survive the button reference being removed.
                if (!CanEndTurn)
                {
                    return;
                }

                // Refuse to do anything if a turn sequence is already in flight. This is what actually
                // stops a frame-perfect double-click, and it did even while the button was being disabled.
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
                AppService.HandleException(CLASS_NAME, nameof(OnEndTurnButton), ex);
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
                // §5.13.2 over-water grace runs in the same pass — a helicopter still at sea on its second
                // Upkeep is lost, so it must not then be given recovery it will not live to use.
                // ⚠ Iterating a COPY: ApplyOverWaterGrace unregisters the unit it loses, which mutates the
                // manager's collection underneath us.
                var map = GameDataManager.CurrentHexMap;
                bool anyLostAtSea = false;

                foreach (var u in new List<CombatUnit>(units))
                {
                    if (u == null || u.IsDestroyed()) continue;

                    if (ApplyOverWaterGrace(u, map))
                    {
                        anyLostAtSea = true;
                        continue;
                    }

                    ApplyUpkeepRecovery(u);
                }

                // Only when something actually went into the sea (§3.6e — the coarse redraw is the sole
                // icon refresh). Raising it every Upkeep would be a full repaint per turn for nothing.
                if (anyLostAtSea)
                    EventManager.Instance?.RaiseRedrawMapIcons();

                // V5.3 + V7 (§18.2): the once-per-turn ledger recompute AND the prestige income it
                // pays. Player side only — the AI has no economy (scripted-only ruling, AI design
                // pass 1); the symmetric branch is ABSENT, not forgotten. The high-water mark is what
                // defeats lose-and-retake farming (V7.2): the progress bonus pays only on value above
                // the highest ever held.
                if (isPlayerSide)
                {
                    VictoryLedger ledger = VictoryLedger.Compute(GameDataManager.CurrentHexMap);
                    CurrentLedger = ledger;

                    int paid = ComputeIncome(ledger, PrestigeStipend, PrestigeIncomeRate,
                        PrestigeProgressBonusRate, HighWaterVictoryValue, out float newHighWater);
                    HighWaterVictoryValue = newHighWater;
                    if (paid != 0) AddPrestige(paid);
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

        /// <summary>
        /// How far outside the map's own footprint the camera may scroll, in world units (~4 hexes).
        /// Enough to bring an edge hex comfortably in from the screen border without letting the view
        /// wander off the board.
        /// </summary>
        private const float SCROLL_BOUNDS_MARGIN = 10f;

        /// <summary>
        /// Derives the camera's scroll limits from the loaded map's world footprint (G5, 2026-08-12).
        /// </summary>
        /// <remarks>
        /// ⚠ WHY THIS EXISTS: `SetScrollBounds` had ZERO CALLERS. Scroll limits were whatever was serialized
        /// on the Inspector (±100), hand-calibrated against 32x21 Khost — so they were wrong for every other
        /// map size, in both directions: too tight on a larger map (edges unreachable) and far too loose on a
        /// smaller one (the camera sails off the board). Deferred by Bob until the first non-32x21 map
        /// existed; that map is being authored now.
        ///
        /// ⚠ THE EXTENT IS ASKED OF `HexGridSystem`, NEVER RE-DERIVED. §3.5 makes those four constants the
        /// single geometry authority precisely because a second spelling of a hex dimension drifts — so this
        /// converts corner HEXES to world space rather than multiplying a width by `HEX_WIDTH` itself. Both
        /// row parities are sampled for the right edge because odd rows are staggered half a hex right, so
        /// which row is widest depends on the map's height.
        /// </remarks>
        private static void ApplyDerivedScrollBounds(Position2D mapSize)
        {
            try
            {
                var input = InputService_BattleMap.Instance;
                var grid = HexGridSystem.Instance;
                if (input == null || grid == null || mapSize.IntX <= 0 || mapSize.IntY <= 0) return;

                int lastCol = mapSize.IntX - 1;
                int lastRow = mapSize.IntY - 1;

                Vector3 origin = grid.HexToWorld(new Position2D(0, 0));

                // Widest column centre: sample an even row and (if one exists) an odd row.
                float maxX = grid.HexToWorld(new Position2D(lastCol, 0)).x;
                if (mapSize.IntY > 1)
                    maxX = Mathf.Max(maxX, grid.HexToWorld(new Position2D(lastCol, 1)).x);

                float maxY = grid.HexToWorld(new Position2D(0, lastRow)).y;

                input.SetScrollBounds(
                    new Vector2(origin.x - SCROLL_BOUNDS_MARGIN, origin.y - SCROLL_BOUNDS_MARGIN),
                    new Vector2(maxX + SCROLL_BOUNDS_MARGIN, maxY + SCROLL_BOUNDS_MARGIN));
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ApplyDerivedScrollBounds), ex);
            }
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
        /// §5.13.2 over-water grace — a helicopter may END a turn over Water, but must reach land by the end
        /// of its NEXT turn or it is lost at sea. Returns true if <paramref name="unit"/> was lost.
        /// </summary>
        /// <remarks>
        /// ⚠ RUNS AT UPKEEP, NOT REFRESH, and the difference is the whole grace period. The punch list said
        /// "checked at Refresh", but Refresh fires at the START of a turn — before the unit has had the very
        /// move the rule gives it to escape — so a literal Refresh check kills a helicopter that has not yet
        /// been given its chance, i.e. zero turns of grace instead of one. Upkeep is "end of your turn",
        /// which is exactly what "by the end of its next move" means.
        ///
        /// ⚠ ONE BOOL GIVES EXACTLY ONE TURN because the flag is read BEFORE it is written. End turn N over
        /// water: flag was false → set it, survive. End turn N+1 still over water: flag was true → lost.
        /// Reach land at any point: cleared, and the clock is fully reset for next time.
        ///
        /// ⚠ HELICOPTERS ONLY (`MovementMedium.Helo`), per §5.13.2. A fixed-wing aircraft parked over water
        /// is not this rule's business — it is the §5.13.5 auto-return gap, which is unbuilt, and quietly
        /// drowning jets here would disguise that as a working feature.
        ///
        /// ⚠ A LOADED LIFT TAKES ITS REGIMENT WITH IT, and that needs no special handling: the lift IS the
        /// regiment (one UnitID, one HP pool, riding its Embarked profile), so removing the unit removes
        /// both. The equipment is booked explicitly because NO DAMAGE EVENT FIRES — same reason
        /// RetreatResolver books a surrender (§3.6d): `TakeDamage` is the only automatic booking hook, and a
        /// unit lost at sea never passes through it.
        /// </remarks>
        public static bool ApplyOverWaterGrace(CombatUnit unit, HexMap map)
        {
            if (unit == null) return false;

            bool overWater = MovementModeService.CurrentMedium(unit) == MovementMedium.Helo
                          && map?.GetHexAt(unit.MapPos)?.Terrain == TerrainType.Water;

            if (!overWater)
            {
                unit.SetEndedTurnOverWater(false);
                return false;
            }

            if (!unit.EndedTurnOverWater)
            {
                // First turn out over the water: the clock starts, and the player is told.
                unit.SetEndedTurnOverWater(true);
                PrinterDispatch.ReportStrandedOverWater(unit);
                return false;
            }

            GameDataManager.RecordRemainingEquipmentAsLost(unit);
            PrinterDispatch.ReportLostAtSea(unit);
            GameDataManager.Instance?.UnregisterCombatUnit(unit.UnitID);
            return true;
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

        /// <summary>
        /// End Scenario button (V10.2 — §17.x early finish). Inspector-wired by Bob, like
        /// OnEndTurnButton — do NOT add a HUD copy, and ⚠ the name is a contract (§3.6b).
        /// The availability gate lives HERE, not in button state (the CanEndTurn precedent):
        /// player turn · scoring declared · an actual VICTORY achieved (share ≥ minor — never
        /// requiredResult, the C5 turn-1 defensive cash-out exploit) · every mission objective held
        /// (C6). Cashing out pays unusedTurns × steady income × earlyFinishMultiplier (C3: computed
        /// LIVE — no stored field to go stale across a save), then ends the battle through the
        /// normal grading path, which the gate terms guarantee will grade ≥ MinorVictory.
        /// </summary>
        public void OnEndScenarioButton()
        {
            try
            {
                if (_battleEnded || CurrentPhase != BattlePhase.PlayerTurn)
                {
                    AppService.CaptureUiMessage("The scenario can only be ended during your turn.");
                    return;
                }

                var map = GameDataManager.CurrentHexMap;
                VictoryLedger ledger = VictoryLedger.Compute(map);

                if (VictoryThresholdMinor <= 0f || ledger.TotalValue <= 0f)
                {
                    AppService.CaptureUiMessage("This scenario is not scored — it runs to the turn limit.");
                    return;
                }

                if (ledger.PlayerShare < VictoryThresholdMinor || !HexMapUtil.AllMissionObjectivesHeld(map))
                {
                    AppService.CaptureUiMessage("Cannot end the scenario — victory has not been achieved yet.");
                    return;
                }

                int unusedTurns = Math.Max(0, MaxTurnNumber - CurrentTurnNumber);
                int bonus = ComputeEarlyFinishBonus(ledger, PrestigeStipend, PrestigeIncomeRate,
                    unusedTurns, EarlyFinishMultiplier);
                if (bonus > 0)
                {
                    AddPrestige(bonus);
                    AppService.CaptureUiMessage(
                        $"Scenario ended early — {unusedTurns} unused day(s) pay a {bonus} prestige bonus.");
                }

                TriggerImmediateVictory();
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnEndScenarioButton), ex);
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
                /* V10.1 (Stage 4) — the nothing-further-to-gain end: the top rung is reached and the
                 * C6 gate holds, so no better result exists and the battle ends without waiting for
                 * the turn limit. (REPLACED the all-objectives instant win, retired Stage 2 — that
                 * rule made defensive scenarios unwinnable and, under derived strongholds, would have
                 * auto-won on any 12 stronghold captures.)
                 * ⚠ C1 guard FIRST: with no scoring declared, decisiveCut is 0 and PlayerShare >= 0
                 * would end every battle at its first turn boundary.
                 * ⚠ Fresh compute (C2), and it WRITES CurrentLedger (editor addendum): this runs at
                 * TurnBoundary, AFTER both Upkeeps, so the upkeep-cached copy is legitimately staler
                 * than the map — and the moment an AI turn exists it would be a full enemy turn stale.
                 * ⚠ The C6 gate term: the battle must never auto-end at a rung the gate would then
                 * deny — a decisive SHARE with a lost objective is not a decisive victory. */
                if (VictoryThresholdMinor <= 0f) return false;

                var map = GameDataManager.CurrentHexMap;
                VictoryLedger ledger = VictoryLedger.Compute(map);
                CurrentLedger = ledger;

                if (ledger.TotalValue <= 0f) return false;
                if (!HexMapUtil.AllMissionObjectivesHeld(map)) return false;

                return ledger.PlayerShare >= VictoryThresholdDecisive;
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

                // V9 (Stage 4): grade the battle — fresh ledger (C2), written to CurrentLedger so the
                // HUD and this verdict describe the same instant (editor addendum), C6 gate applied
                // inside GradeBattleResult. The full arithmetic is logged (V9.4) because Bob hand-tunes
                // these numbers across maps and an opaque verdict is unusable to him.
                var finalMap = GameDataManager.CurrentHexMap;
                VictoryLedger finalLedger = VictoryLedger.Compute(finalMap);
                CurrentLedger = finalLedger;
                bool objectivesHeld = HexMapUtil.AllMissionObjectivesHeld(finalMap);

                CurrentResult = GradeBattleResult(finalLedger, StartingPlayerShare,
                    VictoryThresholdMinor, VictoryThresholdMajor, VictoryThresholdDecisive,
                    objectivesHeld, RequiredResult);

                Debug.Log($"[{CLASS_NAME}] Battle graded: share {finalLedger.PlayerShare:0.###} " +
                          $"({finalLedger.PlayerValue:0.#}/{finalLedger.TotalValue:0.#}, start {StartingPlayerShare:0.###}) | " +
                          $"cuts {VictoryThresholdMinor}/{VictoryThresholdMajor}/{VictoryThresholdDecisive} " +
                          $"(mirrored defeat cuts {2f * StartingPlayerShare - VictoryThresholdMinor:0.###}/" +
                          $"{2f * StartingPlayerShare - VictoryThresholdMajor:0.###}/" +
                          $"{2f * StartingPlayerShare - VictoryThresholdDecisive:0.###}) | " +
                          $"objectives held: {objectivesHeld} | required: {RequiredResult} | result: {CurrentResult}");

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

        #region Victory Economy

        /// <summary>
        /// §18.2 per-turn income (V7): stipend floor + rate × held value + progress bonus on value
        /// above the high-water mark. Static and pure so EditorTests can pin the arithmetic (the
        /// TurnStructureTests precedent — instance methods on a MonoBehaviour are not headless-
        /// testable). Accumulates in double and rounds ONCE — rounding the components separately
        /// loses a point per turn to truncation, a real number over 21 turns (V7.3). The high-water
        /// mark only ever ratchets UP (V7.2): losing 100 and retaking it pays nothing, which is the
        /// entire anti-farm mechanism — no per-hex state, no capture events.
        /// </summary>
        internal static int ComputeIncome(VictoryLedger ledger, int stipend, float incomeRate,
            float progressBonusRate, float highWater, out float newHighWater)
        {
            double income = stipend + (double)ledger.PlayerValue * incomeRate;

            if (progressBonusRate > 0f && ledger.PlayerValue > highWater)
                income += ((double)ledger.PlayerValue - highWater) * progressBonusRate;

            newHighWater = ledger.PlayerValue > highWater ? ledger.PlayerValue : highWater;
            return (int)Math.Round(income);
        }

        /// <summary>
        /// §17.3 grading (V9) + the §17.x mission-objective gate cap (C6). Static and pure for
        /// EditorTests. Order matters: the C1 no-scoring guard FIRST (all-zero thresholds would
        /// otherwise grade every share DecisiveVictory), the zero-value map second (V5.4 — every
        /// pre-rebalance shipped map), the mirrored ladder third, the gate cap LAST.
        /// ⚠ The defeat cuts MIRROR AROUND THE ACTUAL STARTING SHARE (V9.1), not 0.5 — the stalemate
        /// premise stays a design convention Bob can deliberately break, never an assumption welded
        /// into the scoring. Since decisive &gt; major &gt; minor &gt; s0, the mirrored cuts descend
        /// correctly by construction.
        /// ⚠ The C6 cap is ONE RUNG BELOW requiredResult, not a flat Draw: a flat Draw cap would let
        /// a defensive scenario (required = Draw) LOSE its objectives and still pass. One rung below
        /// fails the mission in every scenario shape while the share still grades how badly.
        /// </summary>
        internal static BattleResult GradeBattleResult(VictoryLedger ledger, float startingShare,
            float minorCut, float majorCut, float decisiveCut, bool objectivesHeld, BattleResult requiredResult)
        {
            if (minorCut <= 0f) return BattleResult.Draw;          // C1: no scoring declared
            if (ledger.TotalValue <= 0f) return BattleResult.Draw; // V5.4: unscored map

            float share = ledger.PlayerShare;
            float s0 = startingShare;

            BattleResult byShare = share switch
            {
                _ when share >= decisiveCut => BattleResult.DecisiveVictory,
                _ when share >= majorCut => BattleResult.MajorVictory,
                _ when share >= minorCut => BattleResult.MinorVictory,
                _ when share > 2f * s0 - minorCut => BattleResult.Draw,
                _ when share > 2f * s0 - majorCut => BattleResult.MinorDefeat,
                _ when share > 2f * s0 - decisiveCut => BattleResult.MajorDefeat,
                _ => BattleResult.DecisiveDefeat
            };

            if (objectivesHeld) return byShare;

            // Gate unmet: the worse of the share grade and the cap. BattleResult ordinals ascend
            // toward defeat (Ongoing = 0 is the sentinel and can reach here from neither input).
            BattleResult cap = OneRungBelow(requiredResult);
            return (BattleResult)Math.Max((int)byShare, (int)cap);
        }

        /// <summary>
        /// One rung toward defeat from <paramref name="result"/>. DecisiveDefeat has no rung below and
        /// caps to itself (the degenerate authoring case noted in todo_prestige C6 — a scenario
        /// requiring DecisiveDefeat cannot be failed further by its gate).
        /// </summary>
        internal static BattleResult OneRungBelow(BattleResult result)
        {
            return result >= BattleResult.DecisiveDefeat ? BattleResult.DecisiveDefeat : result + 1;
        }

        /// <summary>
        /// §17.x early-finish bonus (V10.2/C3): unusedTurns × the STEADY income the player would earn
        /// by sitting still (stipend + rate × held value — deliberately NOT the progress bonus, which
        /// sitting still never pays) × the manifest multiplier. Computed LIVE from the ledger at
        /// cash-out — no stored lastTurnIncome, so it cannot go stale across a save. One rounding.
        /// Any multiplier above 1 makes cashing out strictly dominate sitting; the only way to raise
        /// the bonus is to hold MORE value first, which is the behaviour the design wants.
        /// </summary>
        internal static int ComputeEarlyFinishBonus(VictoryLedger ledger, int stipend, float incomeRate,
            int unusedTurns, float multiplier)
        {
            if (unusedTurns <= 0) return 0;

            double steadyIncome = stipend + (double)ledger.PlayerValue * incomeRate;
            return (int)Math.Round(unusedTurns * steadyIncome * multiplier);
        }

        /// <summary>
        /// HUD roll-up for a player stronghold capture (V6.3) — replaces the retired counter pair
        /// (CaptureObjective/LoseObjective), whose "(3/12)" arithmetic desynced by construction once
        /// flips widened to strongholds. Computes a throwaway ledger for the message and deliberately
        /// does NOT write CurrentLedger (Upkeep owns that — V5.3) and runs NO victory check (the
        /// instant win retired in Stage 2; Stage 4's share-based rule runs at TurnBoundary).
        /// </summary>
        public void ReportStrongholdTaken()
        {
            try
            {
                var ledger = VictoryLedger.Compute(GameDataManager.CurrentHexMap);
                AppService.CaptureUiMessage(
                    $"Stronghold taken — victory value {ledger.PlayerValue:0.#}/{ledger.TotalValue:0.#} ({ledger.PlayerShare * 100f:0}%).");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ReportStrongholdTaken), ex);
            }
        }

        /// <summary>HUD roll-up for a stronghold lost to the AI (V6.3).</summary>
        public void ReportStrongholdLost()
        {
            try
            {
                var ledger = VictoryLedger.Compute(GameDataManager.CurrentHexMap);
                AppService.CaptureUiMessage(
                    $"Stronghold lost — victory value {ledger.PlayerValue:0.#}/{ledger.TotalValue:0.#} ({ledger.PlayerShare * 100f:0}%).");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ReportStrongholdLost), ex);
            }
        }

        /// <summary>
        /// Copies the battle's prestige + scoring state INTO the save DTO — SnapshotMapper.ToSnapshot
        /// calls this when a battle is live (Stage 5, SAVE_VERSION 7). ⚠ Deliberately ONLY the
        /// prestige-pass slice: the wallet, the two scoring anchors, and the manifest-knob mirror
        /// (V11.6 — an in-battle save restores without its manifest, §7.3). The REST of battle state
        /// (turn, phase, weather…) gets its sync when saving is wired to UI — a separate unbuilt
        /// feature; do not read this pair as it.
        /// </summary>
        public void CaptureScenarioState(ScenarioData data)
        {
            try
            {
                if (data == null) return;

                data.CurrentPrestige = _prestige.Current;
                data.PrestigeEarned = _prestige.Earned;
                data.PrestigeSpent = _prestige.Spent;
                data.StartingPlayerShare = StartingPlayerShare;
                data.HighWaterVictoryValue = HighWaterVictoryValue;
                data.PrestigeStipend = PrestigeStipend;
                data.PrestigeIncomeRate = PrestigeIncomeRate;
                data.PrestigeProgressBonusRate = PrestigeProgressBonusRate;
                data.EarlyFinishMultiplier = EarlyFinishMultiplier;
                data.VictoryThresholdMinor = VictoryThresholdMinor;
                data.VictoryThresholdMajor = VictoryThresholdMajor;
                data.VictoryThresholdDecisive = VictoryThresholdDecisive;
                data.RequiredResult = RequiredResult;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CaptureScenarioState), ex);
            }
        }

        /// <summary>
        /// Restores the prestige/scoring slice FROM a loaded save DTO — SnapshotMapper.ApplySnapshot
        /// calls this AFTER the map is restored, so the ledger recompute below sees restored control.
        /// The ledger itself is never persisted (V12.2) — recomputed here, the derived-state rule.
        /// </summary>
        public void RestoreScenarioState(ScenarioData data)
        {
            try
            {
                if (data == null) return;

                _prestige.Restore(data.CurrentPrestige, data.PrestigeEarned, data.PrestigeSpent);
                StartingPlayerShare = data.StartingPlayerShare;
                HighWaterVictoryValue = data.HighWaterVictoryValue;
                PrestigeStipend = data.PrestigeStipend;
                PrestigeIncomeRate = data.PrestigeIncomeRate;
                PrestigeProgressBonusRate = data.PrestigeProgressBonusRate;
                EarlyFinishMultiplier = data.EarlyFinishMultiplier;
                VictoryThresholdMinor = data.VictoryThresholdMinor;
                VictoryThresholdMajor = data.VictoryThresholdMajor;
                VictoryThresholdDecisive = data.VictoryThresholdDecisive;
                RequiredResult = data.RequiredResult;

                CurrentLedger = VictoryLedger.Compute(GameDataManager.CurrentHexMap);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(RestoreScenarioState), ex);
            }
        }

        #endregion // Victory Economy

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
        /// Credits prestige to the wallet — spendable balance AND earned tally together (V8.1).
        /// No-op on non-positive amounts. See EventManager (OnPrestigeChanged).
        /// </summary>
        public void AddPrestige(int amount)
        {
            try
            {
                int credited = _prestige.Add(amount);
                if (credited > 0 && EventManager.Instance != null)
                    EventManager.Instance.RaisePrestigeChanged(_prestige.Current, credited);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(AddPrestige), ex);
            }
        }

        /// <summary>
        /// Atomic check-and-debit (V8.2): false — and NOTHING mutates — when the balance cannot cover the
        /// amount. The P4 purchase flow relies on this being one step; do not pre-check then spend.
        /// See EventManager (OnPrestigeChanged).
        /// </summary>
        public bool SpendPrestige(int amount)
        {
            try
            {
                if (!_prestige.TrySpend(amount)) return false;

                if (EventManager.Instance != null)
                    EventManager.Instance.RaisePrestigeChanged(_prestige.Current, -amount);
                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SpendPrestige), ex);
                return false;
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

                // V8.3: the wallet resets WITH the battle — before this, CurrentPrestige survived a
                // reset and a replayed scenario would inherit the previous run's pool.
                _prestige.Seed(0);
                CurrentLedger = default;
                StartingPlayerShare = 0f;
                HighWaterVictoryValue = 0f;

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
