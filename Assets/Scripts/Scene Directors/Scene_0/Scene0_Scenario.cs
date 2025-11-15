using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;

namespace HammerAndSickle.SceneDirectors
{
    /// <summary>
    /// Main scene core interface menu handler.
    /// </summary>
    public class Scene0_ScenarioDialog : MenuHandler
    {
        private const string CLASS_NAME = nameof(Scene0_ScenarioDialog);

        #region Singleton Instance

        public static Scene0_ScenarioDialog Instance { get; private set; }

        #endregion // Singleton Instance

        #region Fields

        // Dialog GameObject Root
        [SerializeField] private GameObject dialogRoot;

        // Contains dialog controls.
        [SerializeField] private ScenarioDialog _scenarioDialog;

        // Manifest storage
        private List<ScenarioManifest> _loadedManifests = new List<ScenarioManifest>();

        #endregion // Fields

        #region Events

        /// <summary>
        /// Fired when the difficulty button is pressed.
        /// </summary>
        public event Action DifficultyButtonPressed;

        /// <summary>
        /// Fired when a new scenario is selected.
        /// </summary>
        public event Action ScenarioSelectionChanged;

        #endregion // Events

        #region Unity Lifecycle

        public override void Awake()
        {
            base.Awake();

            // Set the dialog ID.
            Initialize(GameData.MainScene_ScenarioDialog_ID, false);
        }

        private void OnEnable()
        {
            try
            {
                // Subscribe to selection change event
                if (_scenarioDialog != null)
                {
                    _scenarioDialog.SelectionChanged += ReactSelectionChange;
                }

                LoadScenarioManifests();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnEnable), e);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks
            if (_scenarioDialog != null)
            {
                _scenarioDialog.SelectionChanged -= ReactSelectionChange;
            }
        }

        #endregion // Unity Lifecycle

        #region Overrides

        public override void ToggleMenu()
        {
            // Set active state and interactivity based on visibility and input focus.
            if (IsVisible && IsInputFocus) dialogRoot.SetActive(true);
            else dialogRoot.SetActive(false);
        }

        public override void SetupSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion // Overrides

        #region Callbacks

        public void OnLoadButton()
        {
            // PrepareBattle a scenario is selected and get the manifest.
            ScenarioManifest manifest = GetSelectedManifest();
            if (manifest == null)
            {
                AppService.CaptureUiMessage("No scenario selected to load.");
                return;
            }

            // Get the scene ID based on the selected scenario ID.
            int? sceneId = manifest.ScenarioId switch
            {
                GameData.SCENARIO_ID_MISSION_KHOST => (int)SceneID.Scenario_Khost,
                GameData.SCENARIO_ID_CAMPAIGN_KHOST => (int)SceneID.Campaign_Khost,
                _ => null
            };

            // Load the scene if a valid scene ID was found.
            if (sceneId.HasValue)
            {
                SceneManager.Instance.LoadScene(sceneId.Value);
            }
            else
            {
                AppService.CaptureUiMessage($"Unknown scenario ID: {manifest.ScenarioId}");
            }
        }

        public void OnDifficultyButton()
        {
            // Check if a manifest is selected before firing the event
            int selectedIndex = _scenarioDialog.GetSelectedIndex();

            if (selectedIndex < 0 || selectedIndex >= _loadedManifests.Count)
            {
                AppService.CaptureUiMessage("No scenario selected. Please select a scenario first.");
                return;
            }

            // Fire the event for difficulty selector to handle
            DifficultyButtonPressed?.Invoke();
        }

        public void OnExitButton()
        {
            // Return to the main interface.
            Scene0_Director.Instance.SetActiveMenuByID(GameData.MainScene_CoreInterface_ID);
        }

        #endregion // Callbacks

        #region Private Methods

        /// <summary>
        /// Loads scenario manifests from the designated directory and populates the scenario dialog with their display
        /// names.
        /// </summary>
        /// <remarks>This method clears any previously loaded manifests and attempts to load all manifest
        /// files from the directory specified  by <see cref="AppService.ManifestsPath"/>. If the directory does not
        /// exist, contains no manifest files, or if all manifest  files are invalid, appropriate error messages are
        /// displayed, and the operation is aborted.  Valid manifests are added to the internal collection and their
        /// display names are shown in the scenario dialog.</remarks>
        private void LoadScenarioManifests()
        {
            _loadedManifests.Clear();
            _scenarioDialog.ClearList();

            try
            {
                string manifestPath = AppService.ManifestsPath;

                if (!Directory.Exists(manifestPath))
                {
                    ShowErrorState("Scenario manifests directory not found. Please consult settings to rebuild scenario data and manifests.");
                    AppService.CaptureUiMessage("Manifest directory missing - rebuild required");
                    return;
                }

                string[] manifestFiles = Directory.GetFiles(manifestPath, "*" + GameData.MANIFEST_EXTENSION);

                if (manifestFiles.Length == 0)
                {
                    ShowErrorState("No scenario manifests found. Please consult settings to rebuild scenario data and manifests.");
                    AppService.CaptureUiMessage("No manifest files found - rebuild required");
                    return;
                }

                List<string> displayNames = new List<string>();

                foreach (string manifestFile in manifestFiles)
                {
                    try
                    {
                        string json = File.ReadAllText(manifestFile);

                        ScenarioManifest manifest = JsonSerializer.Deserialize<ScenarioManifest>(json);

                        if (manifest == null || !manifest.IsValid())
                        {
                            AppService.CaptureUiMessage($"Invalid manifest: {Path.GetFileName(manifestFile)}");
                            continue;
                        }

                        _loadedManifests.Add(manifest);
                        displayNames.Add(manifest.DisplayName);
                    }
                    catch (Exception e)
                    {
                        AppService.HandleException(CLASS_NAME, nameof(LoadScenarioManifests), e);
                    }
                }

                if (_loadedManifests.Count == 0)
                {
                    ShowErrorState("All scenario manifests are corrupted. Please consult settings to rebuild scenario data and manifests.");
                    AppService.CaptureUiMessage("All manifests corrupted - rebuild required");
                    return;
                }

                _scenarioDialog.PopulateList(displayNames);

                // Auto-load first scenario content (visual selection requires user click)
                if (_loadedManifests.Count > 0)
                {
                    ReactSelectionChange(0);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadScenarioManifests), e);
                ShowErrorState("Error loading scenario manifests. Please consult settings to rebuild scenario data and manifests.");
            }
        }

        /// <summary>
        /// Reacts to a change in the selected scenario index by loading the corresponding thumbnail and briefing text.
        /// </summary>
        /// <param name="newIndex"></param>
        private void ReactSelectionChange(int newIndex)
        {
            try
            {
                if (newIndex < 0 || newIndex >= _loadedManifests.Count)
                    return;

                ScenarioManifest selectedManifest = _loadedManifests[newIndex];

                LoadThumbnail(selectedManifest);
                LoadBriefing(selectedManifest);

                // Fire event to notify difficulty selector
                ScenarioSelectionChanged?.Invoke();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ReactSelectionChange), e);
            }
        }

        #endregion

        #region Resource Loading

        /// <summary>
        /// Loads the thumbnail image for the given scenario manifest.
        /// </summary>
        private void LoadThumbnail(ScenarioManifest manifest)
        {
            try
            {
                string thumbnailPath = manifest.GetThumbnailPath();

                if (string.IsNullOrEmpty(thumbnailPath))
                {
                    SetPlaceholderThumbnail();
                    return;
                }

                // Remove .png extension for Resources.Load
                if (thumbnailPath.EndsWith(".png"))
                {
                    thumbnailPath = thumbnailPath.Substring(0, thumbnailPath.Length - 4);
                }

                Sprite thumbnail = Resources.Load<Sprite>(thumbnailPath);

                if (thumbnail == null)
                {
                    AppService.CaptureUiMessage($"Thumbnail not found: {manifest.ThumbnailFilename}");
                    SetPlaceholderThumbnail();
                    return;
                }

                if (_scenarioDialog.ThumbnailImage != null)
                {
                    _scenarioDialog.ThumbnailImage.sprite = thumbnail;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadThumbnail), e);
                SetPlaceholderThumbnail();
            }
        }

        /// <summary>
        /// Loads the briefing text for the given scenario manifest.
        /// </summary>
        private void LoadBriefing(ScenarioManifest manifest)
        {
            try
            {
                string briefingPath = manifest.GetBriefingFilePath();

                if (string.IsNullOrEmpty(briefingPath))
                {
                    ShowBriefingError("Briefing file path not specified in manifest. Please consult settings to rebuild scenario data and manifests.");
                    AppService.CaptureUiMessage($"Briefing path missing for scenario: {manifest.DisplayName}");
                    return;
                }

                if (!File.Exists(briefingPath))
                {
                    ShowBriefingError("Briefing file not found. Please consult settings to rebuild scenario data and manifests.");
                    AppService.CaptureUiMessage($"Briefing file missing: {manifest.BriefingFilename}");
                    return;
                }

                string briefingContent = File.ReadAllText(briefingPath);

                if (_scenarioDialog.BriefingText != null)
                {
                    _scenarioDialog.BriefingText.text = briefingContent;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadBriefing), e);
                ShowBriefingError("Error loading briefing file. Please consult settings to rebuild scenario data and manifests.");
            }
        }

        #endregion // Resource Loading

        #region Helper Methods

        /// <summary>
        /// Sets the scenario dialog content based on the selected index.
        /// </summary>
        private void SetPlaceholderThumbnail()
        {
            if (_scenarioDialog.ThumbnailImage != null && _scenarioDialog.PlaceholderThumbnail != null)
            {
                _scenarioDialog.ThumbnailImage.sprite = _scenarioDialog.PlaceholderThumbnail;
            }
        }

        /// <summary>
        /// Sets the scenario dialog content based on the selected index.
        /// </summary>
        /// <param name="errorMessage"></param>
        private void ShowBriefingError(string errorMessage)
        {
            if (_scenarioDialog.BriefingText != null)
            {
                _scenarioDialog.BriefingText.text = errorMessage;
            }
        }

        /// <summary>
        /// Shows an error state in the scenario dialog with the provided error message and a placeholder thumbnail.
        /// </summary>
        /// <param name="errorMessage"></param>
        private void ShowErrorState(string errorMessage)
        {
            if (_scenarioDialog.BriefingText != null)
            {
                _scenarioDialog.BriefingText.text = errorMessage;
            }

            SetPlaceholderThumbnail();
        }

        #endregion // Helper Methods

        #region Public API

        /// <summary>
        /// Gets the currently selected scenario manifest, or null if none selected.
        /// </summary>
        public ScenarioManifest GetSelectedManifest()
        {
            int selectedIndex = _scenarioDialog.GetSelectedIndex();

            if (selectedIndex >= 0 && selectedIndex < _loadedManifests.Count)
            {
                ScenarioManifest selected = _loadedManifests[selectedIndex];

                // Create a new instance to persist across scenes.
                ScenarioManifest manifestCopy = new ScenarioManifest(
                    selected.ScenarioId,
                    selected.DisplayName,
                    selected.Description,
                    selected.ThumbnailFilename,
                    selected.MapFilename,
                    selected.OobFilename,
                    selected.AiiFilename,
                    selected.BriefingFilename,
                    selected.PrestigePool,
                    selected.IsCampaignScenario,
                    selected.MapTheme,
                    selected.DifficultyLevel,
                    selected.MaxTurns,
                    selected.MaxCoreUnits
                );

                // Store the new instance in GDM to persist across scenes.
                GameDataManager.CurrentManifest = manifestCopy;

                // Return the new instance.
                return manifestCopy;
            }

            return null;
        }

        /// <summary>
        /// Gets the currently selected scenario manifest without creating a copy (read-only access).
        /// </summary>
        public ScenarioManifest GetCurrentlySelectedManifest()
        {
            int selectedIndex = _scenarioDialog.GetSelectedIndex();
            if (selectedIndex >= 0 && selectedIndex < _loadedManifests.Count)
            {
                return _loadedManifests[selectedIndex];
            }
            return null;
        }

        /// <summary>
        /// Updates the difficulty level of the currently selected manifest.
        /// </summary>
        public void UpdateSelectedManifestDifficulty(DifficultyLevel newDifficulty)
        {
            int selectedIndex = _scenarioDialog.GetSelectedIndex();
            if (selectedIndex >= 0 && selectedIndex < _loadedManifests.Count)
            {
                _loadedManifests[selectedIndex].DifficultyLevel = newDifficulty;
            }
        }

        #endregion // Public API
    }
}
