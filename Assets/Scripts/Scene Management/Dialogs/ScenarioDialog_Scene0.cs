using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.SceneManagement
{
    /// <summary>
    /// Scenario selection dialog for Scene 0 (Main Menu).
    /// Owns all UI controls directly: scenario list, briefing text, thumbnail, difficulty.
    /// Loads manifest files and lets the player pick a scenario to play.
    /// </summary>
    public class ScenarioDialog_Scene0 : UIPanel
    {
        private const string CLASS_NAME = nameof(ScenarioDialog_Scene0);

        #region Serialized Fields

        [Header("Navigation")]
        [SerializeField] private UIPanel _defaultDialog;

        [Header("Scenario List")]
        [SerializeField] private UIListBox _scenarioList;
        [SerializeField] private TMP_InputField _selectionField;

        [Header("Scenario Display")]
        [SerializeField] private TMP_Text _briefingText;
        [SerializeField] private Image _thumbnailImage;
        [SerializeField] private Sprite _placeholderThumbnail;
        [SerializeField] private TMP_Text _difficultyText;

        [Header("Difficulty Buttons")]
        [SerializeField] private Button _colonelButton;
        [SerializeField] private Button _mjGenButton;
        [SerializeField] private Button _ltGenButton;

        [Header("Action Buttons")]
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _backButton;

        #endregion // Serialized Fields

        #region Fields

        private readonly List<ScenarioManifest> _loadedManifests = new();

        #endregion // Fields

        #region Unity Lifecycle

        private void Start()
        {
            try
            {
                if (_scenarioList == null || _selectionField == null ||
                    _briefingText == null || _thumbnailImage == null ||
                    _difficultyText == null || _loadButton == null ||
                    _backButton == null)
                {
                    throw new Exception($"{CLASS_NAME} controls invalid");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Start), e);
                AppService.UnityQuit_DataUnsafe();
            }
        }

        #endregion // Unity Lifecycle

        #region UIPanel Overrides

        protected override void OnShow()
        {
            // Subscribe to list selection changes when the dialog opens
            _scenarioList.SelectionChanged += OnSelectionChanged;

            // Load manifests from disk once, then use the GDM cache on subsequent opens
            if (GameDataManager.LoadedManifests.Count == 0)
                LoadScenarioManifests();
            else
                PopulateFromCache();
        }

        protected override void OnHide()
        {
            // Unsubscribe from list selection changes when the dialog closes
            _scenarioList.SelectionChanged -= OnSelectionChanged;
        }

        #endregion // UIPanel Overrides

        #region Button Callbacks

        /// <summary>
        /// Called by the Back button. Returns to the default (home) dialog.
        /// </summary>
        public void OnBackButton()
        {
            // See EventManager: request dialog switch back to home
            EventManager.Instance.RaiseScene0DialogRequested(_defaultDialog);
        }

        /// <summary>
        /// Called by the Load button. Loads the selected scenario.
        /// </summary>
        public void OnLoadButton()
        {
            // CurrentManifest is already set by OnSelectionChanged
            ScenarioManifest manifest = GetSelectedManifest();
            if (manifest == null)
            {
                AppService.CaptureUiMessage("No scenario selected to load.");
                return;
            }

            // Determine which scene to load based on the scenario ID
            int? sceneId = manifest.ScenarioId switch
            {
                GameData.SCENARIO_ID_MISSION_KHOST => (int)SceneID.Scenario_Khost,
                GameData.SCENARIO_ID_CAMPAIGN_KHOST => (int)SceneID.Campaign_Khost,
                _ => null
            };

            if (sceneId.HasValue)
            {
                // Scene transition — direct call to SceneManager, not a dialog event
                SceneManager.Instance.LoadScene(sceneId.Value);
            }
            else
            {
                AppService.CaptureUiMessage($"Unknown scenario ID: {manifest.ScenarioId}");
            }
        }

        /// <summary>
        /// Called by the Colonel difficulty button.
        /// </summary>
        public void OnColonelButton()
        {
            UpdateSelectedManifestDifficulty(DifficultyLevel.Colonel);
        }

        /// <summary>
        /// Called by the Major General difficulty button.
        /// </summary>
        public void OnMjGenButton()
        {
            UpdateSelectedManifestDifficulty(DifficultyLevel.MjGeneral);
        }

        /// <summary>
        /// Called by the Lieutenant General difficulty button.
        /// </summary>
        public void OnLtGenButton()
        {
            UpdateSelectedManifestDifficulty(DifficultyLevel.LtGeneral);
        }

        #endregion // Button Callbacks

        #region Private Methods

        private void LoadScenarioManifests()
        {
            _loadedManifests.Clear();
            _scenarioList.Clear();

            try
            {
                string manifestPath = AppService.ManifestsPath;

                if (!Directory.Exists(manifestPath))
                {
                    ShowErrorState("Scenario manifests directory not found. Please consult settings to rebuild scenario data and manifests.");
                    return;
                }

                string[] manifestFiles = Directory.GetFiles(manifestPath, "*" + GameData.MANIFEST_EXTENSION);

                if (manifestFiles.Length == 0)
                {
                    ShowErrorState("No scenario manifests found. Please consult settings to rebuild scenario data and manifests.");
                    return;
                }

                List<string> displayNames = new();

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
                    ShowErrorState("No valid scenario manifests available. Please consult settings to rebuild scenario data and manifests.");
                    return;
                }

                // Cache in GameDataManager so subsequent opens skip the filesystem
                GameDataManager.SetLoadedManifests(_loadedManifests);

                _scenarioList.Populate(displayNames);
                _scenarioList.SelectIndex(0);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadScenarioManifests), e);
                ShowErrorState("Error loading scenario manifests. Please consult settings to rebuild scenario data and manifests.");
            }
        }

        private void PopulateFromCache()
        {
            _loadedManifests.Clear();
            _loadedManifests.AddRange(GameDataManager.LoadedManifests);

            List<string> displayNames = new();
            foreach (ScenarioManifest manifest in _loadedManifests)
                displayNames.Add(manifest.DisplayName);

            _scenarioList.Populate(displayNames);
            _scenarioList.SelectIndex(0);
        }

        private void OnSelectionChanged(int newIndex)
        {
            try
            {
                if (newIndex < 0 || newIndex >= _loadedManifests.Count)
                    return;

                ScenarioManifest selected = _loadedManifests[newIndex];

                // Update the selection field to show the chosen scenario name
                if (_selectionField != null)
                    _selectionField.text = selected.DisplayName;

                // Store the selected manifest so other systems can access it
                GameDataManager.CurrentManifest = selected;

                LoadThumbnail(selected);
                LoadBriefing(selected);
                UpdateDifficultyText(selected.DifficultyLevel);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnSelectionChanged), e);
            }
        }

        #endregion // Private Methods

        #region Resource Loading

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

                // Resources.Load requires the path without the file extension
                if (thumbnailPath.EndsWith(".png"))
                    thumbnailPath = thumbnailPath[..^4];

                Sprite thumbnail = Resources.Load<Sprite>(thumbnailPath);

                if (thumbnail == null)
                {
                    AppService.CaptureUiMessage($"Thumbnail not found: {manifest.ThumbnailFilename}");
                    SetPlaceholderThumbnail();
                    return;
                }

                _thumbnailImage.sprite = thumbnail;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadThumbnail), e);
                SetPlaceholderThumbnail();
            }
        }

        private void LoadBriefing(ScenarioManifest manifest)
        {
            try
            {
                string briefingPath = manifest.GetBriefingFilePath();

                if (string.IsNullOrEmpty(briefingPath))
                {
                    _briefingText.text = "Briefing file path not specified in manifest.";
                    AppService.CaptureUiMessage($"Briefing path missing for scenario: {manifest.DisplayName}");
                    return;
                }

                if (!File.Exists(briefingPath))
                {
                    _briefingText.text = "Briefing file not found.";
                    AppService.CaptureUiMessage($"Briefing file missing: {manifest.BriefingFilename}");
                    return;
                }

                _briefingText.text = File.ReadAllText(briefingPath);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadBriefing), e);
                _briefingText.text = "Error loading briefing file.";
            }
        }

        #endregion // Resource Loading

        #region Helper Methods

        private ScenarioManifest GetSelectedManifest()
        {
            int index = _scenarioList.SelectedIndex;

            if (index < 0 || index >= _loadedManifests.Count)
                return null;

            return _loadedManifests[index];
        }

        private void UpdateSelectedManifestDifficulty(DifficultyLevel newDifficulty)
        {
            int index = _scenarioList.SelectedIndex;
            if (index >= 0 && index < _loadedManifests.Count)
            {
                _loadedManifests[index].DifficultyLevel = newDifficulty;
                UpdateDifficultyText(newDifficulty);
            }
        }

        private void UpdateDifficultyText(DifficultyLevel difficulty)
        {
            _difficultyText.text = difficulty switch
            {
                DifficultyLevel.Colonel => "Difficulty: Colonel",
                DifficultyLevel.MjGeneral => "Difficulty: Mj. General",
                DifficultyLevel.LtGeneral => "Difficulty: Lt. General",
                _ => "Difficulty: Unknown"
            };
        }

        private void SetPlaceholderThumbnail()
        {
            if (_thumbnailImage != null && _placeholderThumbnail != null)
                _thumbnailImage.sprite = _placeholderThumbnail;
        }

        private void ShowErrorState(string errorMessage)
        {
            _briefingText.text = errorMessage;
            SetPlaceholderThumbnail();
        }

        #endregion // Helper Methods
    }
}
