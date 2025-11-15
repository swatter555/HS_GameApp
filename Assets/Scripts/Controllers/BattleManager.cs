using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Helpers;
using HammerAndSickle.Core.Map;
using HammerAndSickle.Helpers;
using HammerAndSickle.Services;
using System;
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

        #endregion // Fields

        #region Properties

        // Indicates whether the battle manager has been fully initialized.
        public bool IsReady => _isInitialized;

        /// --------------------
        /// Turn Management
        /// --------------------

        public int CurrentTurnNumber { get; private set; } = 1;
        public int MaxTurnNumber { get; private set; } = 20;
        public BattlePhase CurrentPhase { get; private set; } = BattlePhase.NotStarted;

        /// --------------------
        /// Conditions
        /// --------------------

        public WeatherCondition CurrentWeather { get; private set; } = WeatherCondition.Clear;
        public int MaxNumberCoreUnitAllowed { get; private set; } = 0;

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
        /// Initializes the battle manager with default settings.
        /// </summary>
        private void InitializeBattleManager()
        {
            try
            {
                if (_isInitialized)
                    return;

                // Set default values
                CurrentTurnNumber = 1;
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

            // Refresh the hex map renderer to display the newly loaded map
            HexMapRenderer.Instance.RefreshMap();

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

            return true;
        }

        /// <summary>
        /// Retrieves and assigns data from the current game manifest to the corresponding properties.
        /// </summary>
        private void GrabManifestData()
        {
            ScenarioID = GameDataManager.CurrentManifest.ScenarioId;
            IsCampaignBattle = GameDataManager.CurrentManifest.IsCampaignScenario;
            CurrentPrestige = GameDataManager.CurrentManifest.PrestigePool;
            MaxNumberCoreUnitAllowed = GameDataManager.CurrentManifest.MaxCoreUnits;
            MaxTurnNumber = GameDataManager.CurrentManifest.MaxTurns;

        }

        /// <summary>
        /// Initializes a new battle with specified parameters.
        /// </summary>
        public void StartBattle(string scenarioId, int maxTurns, int prestige, int maxCore, bool isCampaignBattle = false)
        {
            try
            {
                ScenarioID = scenarioId;
                MaxTurnNumber = maxTurns;
                IsCampaignBattle = isCampaignBattle;
                CurrentTurnNumber = 1;
                CurrentPhase = BattlePhase.PlayerTurn;
                CurrentResult = BattleResult.Ongoing;
                CurrentPrestige = prestige;
                MaxNumberCoreUnitAllowed = maxCore;

                // Reset statistics
                PrestigeEarned = 0;
                PrestigeSpent = 0;

                AppService.CaptureUiMessage($"Battle started: {scenarioId}, Max turns: {maxTurns}");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(StartBattle), ex);
            }
        }

        #endregion // Initialization

        #region Turn Management

        /// <summary>
        /// Advances to the next turn.
        /// </summary>
        public bool AdvanceTurn()
        {
            try
            {
                if (CurrentResult != BattleResult.Ongoing)
                {
                    AppService.CaptureUiMessage("Cannot advance turn - battle is complete");
                    return false;
                }

                CurrentTurnNumber++;

                if (CurrentTurnNumber > MaxTurnNumber)
                {
                    CompleteBattle();
                    return false;
                }

                CurrentPhase = BattlePhase.PlayerTurn;
                AppService.CaptureUiMessage($"Turn {CurrentTurnNumber} of {MaxTurnNumber}");
                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(AdvanceTurn), ex);
                return false;
            }
        }

        /// <summary>
        /// Ends the current player turn and advances to AI turn.
        /// </summary>
        public void EndPlayerTurn()
        {
            try
            {
                if (CurrentPhase != BattlePhase.PlayerTurn)
                {
                    AppService.CaptureUiMessage("Cannot end player turn - not in player phase");
                    return;
                }

                CurrentPhase = BattlePhase.AITurn;
                AppService.CaptureUiMessage("Player turn ended - AI turn starting");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(EndPlayerTurn), ex);
            }
        }

        /// <summary>
        /// Ends the current AI turn and processes end-of-turn events.
        /// </summary>
        public void EndAITurn()
        {
            try
            {
                if (CurrentPhase != BattlePhase.AITurn)
                {
                    AppService.CaptureUiMessage("Cannot end AI turn - not in AI phase");
                    return;
                }

                CurrentPhase = BattlePhase.EndTurnProcessing;
                ProcessEndOfTurn();
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(EndAITurn), ex);
            }
        }

        /// <summary>
        /// Processes end-of-turn events and checks victory conditions.
        /// </summary>
        private void ProcessEndOfTurn()
        {
            try
            {
                // TODO: Process end-of-turn events
                // - Refresh unit actions and movement
                // - Update supply status
                // - Check victory conditions
                // - Update weather/time of day
                // - Process facility effects

                AppService.CaptureUiMessage("Processing end of turn events");

                // Check if battle should continue
                if (!CheckVictoryConditions())
                {
                    AdvanceTurn();
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ProcessEndOfTurn), ex);
            }
        }

        #endregion // Turn Management

        #region Battle Status

        /// <summary>
        /// Checks victory conditions and updates battle result.
        /// </summary>
        /// <returns>True if battle is complete, false otherwise</returns>
        private bool CheckVictoryConditions()
        {
            try
            {
                // TODO: Implement victory condition checking
                // - Check objective control
                // - Check unit losses
                // - Check turn limit
                // - Calculate victory level

                return false;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CheckVictoryConditions), ex);
                return false;
            }
        }

        /// <summary>
        /// Completes the battle and calculates final result.
        /// </summary>
        private void CompleteBattle()
        {
            try
            {
                CurrentPhase = BattlePhase.BattleComplete;

                // TODO: Calculate final battle result based on objectives and losses
                // For now, default to draw
                CurrentResult = BattleResult.Draw;

                AppService.CaptureUiMessage($"Battle complete: {CurrentResult}");
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
                CurrentTurnNumber = 1;
                CurrentPhase = BattlePhase.NotStarted;
                CurrentResult = BattleResult.Ongoing;
                CurrentWeather = WeatherCondition.Clear;

                ObjectiveHexesOccupied = 0;
                ObjectiveHexesUnoccupied = 0;
                TotalObjectiveHexes = 0;

                PrestigeEarned = 0;
                PrestigeSpent = 0;

                ScenarioID = string.Empty;
                IsCampaignBattle = false;

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
