using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// Dialog controller for loading standalone scenario files from the user's Documents folder.
    /// Displays scenario list, briefing text, and thumbnail preview.
    /// </summary>
    public class MainScene_ScenarioDialog : GenericDialog
    {
        private const string CLASS_NAME = "MainScene_ScenarioDialog";

        #region Serialized Fields

        [Header("Scenario Display")]
        [SerializeField] private TMP_Text briefingText;
        [SerializeField] private Sprite placeholderThumbnail;
        [SerializeField] private Image thumbnailImage;

        #endregion // Serialized Fields

        #region Private Fields

        private List<ScenarioManifest> _loadedManifests = new List<ScenarioManifest>();

        #endregion // Private Fields

        #region Unity Lifecycle

        private void OnEnable()
        {
            try
            {
                LoadScenarioManifests();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnEnable), e);
            }
        }

        #endregion // Unity Lifecycle

        #region Manifest Loading

        private void LoadScenarioManifests()
        {
            _loadedManifests.Clear();
            ClearList();

            try
            {
                string manifestPath = AppService.ManifestsPath;

                if (!Directory.Exists(manifestPath))
                {
                    ShowErrorState("Scenario manifests directory not found. Please consult settings to rebuild scenario data and manifests.");
                    AppService.CaptureUiMessage("Manifest directory missing - rebuild required");
                    return;
                }

                string[] manifestFiles = Directory.GetFiles(manifestPath, "*.manifest");

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

                PopulateList(displayNames);

                // Auto-load first scenario content (visual selection requires user click)
                if (_loadedManifests.Count > 0)
                {
                    OnSelectionChanged(0);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadScenarioManifests), e);
                ShowErrorState("Error loading scenario manifests. Please consult settings to rebuild scenario data and manifests.");
            }
        }

        #endregion // Manifest Loading

        #region Selection Handling

        protected override void OnSelectionChanged(int newIndex)
        {
            try
            {
                if (newIndex < 0 || newIndex >= _loadedManifests.Count)
                    return;

                ScenarioManifest selectedManifest = _loadedManifests[newIndex];

                LoadThumbnail(selectedManifest);
                LoadBriefing(selectedManifest);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnSelectionChanged), e);
            }
        }

        #endregion // Selection Handling

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

                if (thumbnailImage != null)
                {
                    thumbnailImage.sprite = thumbnail;
                }
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

                if (briefingText != null)
                {
                    briefingText.text = briefingContent;
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

        private void SetPlaceholderThumbnail()
        {
            if (thumbnailImage != null && placeholderThumbnail != null)
            {
                thumbnailImage.sprite = placeholderThumbnail;
            }
        }

        private void ShowBriefingError(string errorMessage)
        {
            if (briefingText != null)
            {
                briefingText.text = errorMessage;
            }
        }

        private void ShowErrorState(string errorMessage)
        {
            if (briefingText != null)
            {
                briefingText.text = errorMessage;
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
            int selectedIndex = GetSelectedIndex();
            if (selectedIndex >= 0 && selectedIndex < _loadedManifests.Count)
            {
                return _loadedManifests[selectedIndex];
            }
            return null;
        }

        #endregion // Public API
    }
}