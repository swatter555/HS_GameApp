using HammerAndSickle.Core.GameData;
using HammerAndSickle.SceneDirectors;
using HammerAndSickle.Services;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Core
{
    /// <summary>
    /// Manages difficulty selection UI by resizing difficulty level images
    /// and updating the selected scenario manifest's difficulty level.
    /// </summary>
    public class Scene0_DifficultySelector : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(Scene0_DifficultySelector);

        #region Enums

        /// <summary>
        /// Percentage by which to enlarge the selected difficulty image.
        /// </summary>
        public enum EnlargementSize
        {
            FivePercent = 5,
            TenPercent = 10,
            FifteenPercent = 15,
            TwentyPercent = 20
        }

        #endregion // Enums

        #region Serialized Fields

        [Header("Difficulty Images")]
        [SerializeField] private Image majorImage;
        [SerializeField] private Image colonelImage;
        [SerializeField] private Image generalImage;

        [Header("References")]
        [SerializeField] private ScenarioPanel _scenarioPanel;

        [Header("Settings")]
        [SerializeField] private EnlargementSize enlargementSize = EnlargementSize.TenPercent;
        [SerializeField] private bool _debug = false;

        #endregion // Serialized Fields

        #region Private Fields

        private Vector3 _majorOriginalScale;
        private Vector3 _colonelOriginalScale;
        private Vector3 _generalOriginalScale;

        private DifficultyLevel _currentDifficulty = DifficultyLevel.Colonel;

        #endregion // Private Fields

        #region Unity Lifecycle

        private void Awake()
        {
            try
            {
                // Validate that all images are assigned
                if (majorImage == null)
                    Debug.LogError("Major Image is not assigned in Scene0_DifficultySelector!");
                if (colonelImage == null)
                    Debug.LogError("Colonel Image is not assigned in Scene0_DifficultySelector!");
                if (generalImage == null)
                    Debug.LogError("General Image is not assigned in Scene0_DifficultySelector!");

                // Cache original scales in Awake (BEFORE OnEnable runs!)
                if (majorImage != null)
                {
                    _majorOriginalScale = majorImage.transform.localScale;
                    if (_debug) Debug.Log($"Cached Major original scale: {_majorOriginalScale}");
                }

                if (colonelImage != null)
                {
                    _colonelOriginalScale = colonelImage.transform.localScale;
                    if (_debug) Debug.Log($"Cached Colonel original scale: {_colonelOriginalScale}");
                }

                if (generalImage != null)
                {
                    _generalOriginalScale = generalImage.transform.localScale;
                    if (_debug) Debug.Log($"Cached General original scale: {_generalOriginalScale}");
                }
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Awake), e);
            }
        }

        private void OnEnable()
        {
            try
            {
                if (_debug) Debug.Log("Scene0_DifficultySelector.OnEnable called");

                // Subscribe to events when dialog becomes active
                if (_scenarioPanel != null)
                {
                    if (_debug) Debug.Log("Subscribing to DifficultyButtonPressed and ScenarioSelectionChanged events");
                    _scenarioPanel.DifficultyButtonPressed += OnDifficultyButtonPressed;
                    _scenarioPanel.ScenarioSelectionChanged += OnScenarioSelectionChanged;
                    if (_debug) Debug.Log("Successfully subscribed to events");
                }
                else
                {
                    Debug.LogWarning("_scenarioPanel is null in OnEnable");
                }

                // PrepareBattle difficulty display when dialog becomes active
                InitializeDifficultyDisplay();
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnEnable), e);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (_scenarioPanel != null)
            {
                _scenarioPanel.DifficultyButtonPressed -= OnDifficultyButtonPressed;
                _scenarioPanel.ScenarioSelectionChanged -= OnScenarioSelectionChanged;
            }
        }

        #endregion // Unity Lifecycle

        #region Event Handlers

        /// <summary>
        /// Handles the difficulty button press by cycling to the next difficulty level.
        /// </summary>
        private void OnDifficultyButtonPressed()
        {
            try
            {
                if (_debug) Debug.Log($"OnDifficultyButtonPressed called! Current difficulty: {_currentDifficulty}");

                // Cycle to the next difficulty
                DifficultyLevel newDifficulty = _currentDifficulty switch
                {
                    DifficultyLevel.Major => DifficultyLevel.Colonel,
                    DifficultyLevel.Colonel => DifficultyLevel.General,
                    DifficultyLevel.General => DifficultyLevel.Major,
                    _ => DifficultyLevel.Colonel
                };

                if (_debug) Debug.Log($"New difficulty will be: {newDifficulty}");

                // Update the difficulty
                SetDifficulty(newDifficulty);

                // Update the manifest
                if (_scenarioPanel != null)
                {
                    _scenarioPanel.UpdateSelectedManifestDifficulty(newDifficulty);
                }
                else
                {
                    Debug.LogWarning("_scenarioPanel is null - cannot update manifest");
                }
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnDifficultyButtonPressed), e);
            }
        }

        /// <summary>
        /// Handles scenario selection change by resetting the difficulty display.
        /// </summary>
        private void OnScenarioSelectionChanged()
        {
            try
            {
                InitializeDifficultyDisplay();
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnScenarioSelectionChanged), e);
            }
        }

        #endregion // Event Handlers

        #region Private Methods

        /// <summary>
        /// Initializes the difficulty display based on the currently selected manifest.
        /// </summary>
        private void InitializeDifficultyDisplay()
        {
            if (_scenarioPanel == null)
            {
                Debug.LogWarning("Cannot initialize difficulty display - _scenarioPanel is null");
                return;
            }

            ScenarioManifest currentManifest = _scenarioPanel.GetCurrentlySelectedManifest();
            if (currentManifest != null)
            {
                SetDifficulty(currentManifest.DifficultyLevel);
            }
            else
            {
                // No manifest selected yet, use default (Colonel)
                if (_debug) Debug.Log("No manifest selected yet, using default difficulty: Colonel");
                SetDifficulty(DifficultyLevel.Colonel);
            }
        }

        /// <summary>
        /// Sets the difficulty level and updates the image scaling accordingly.
        /// </summary>
        private void SetDifficulty(DifficultyLevel difficulty)
        {
            if (_debug) Debug.Log($"SetDifficulty called with: {difficulty}");

            // Reset all images to original scale
            ResetAllImageScales();

            // Store the current difficulty
            _currentDifficulty = difficulty;

            // Enlarge the selected difficulty image
            float scaleFactor = 1.0f + ((int)enlargementSize / 100.0f);
            if (_debug) Debug.Log($"Scale factor: {scaleFactor} (enlargement: {enlargementSize})");

            switch (difficulty)
            {
                case DifficultyLevel.Major:
                    if (majorImage != null)
                    {
                        Vector3 newScale = _majorOriginalScale * scaleFactor;
                        if (_debug) Debug.Log($"Setting Major scale from {majorImage.transform.localScale} to {newScale}");
                        majorImage.transform.localScale = newScale;
                    }
                    else
                    {
                        Debug.LogWarning("majorImage is null!");
                    }
                    break;

                case DifficultyLevel.Colonel:
                    if (colonelImage != null)
                    {
                        Vector3 newScale = _colonelOriginalScale * scaleFactor;
                        if (_debug) Debug.Log($"Setting Colonel scale from {colonelImage.transform.localScale} to {newScale}");
                        colonelImage.transform.localScale = newScale;
                    }
                    else
                    {
                        Debug.LogWarning("colonelImage is null!");
                    }
                    break;

                case DifficultyLevel.General:
                    if (generalImage != null)
                    {
                        Vector3 newScale = _generalOriginalScale * scaleFactor;
                        if (_debug) Debug.Log($"Setting General scale from {generalImage.transform.localScale} to {newScale}");
                        generalImage.transform.localScale = newScale;
                    }
                    else
                    {
                        Debug.LogWarning("generalImage is null!");
                    }
                    break;
            }

            if (_debug) Debug.Log($"Difficulty set complete: {difficulty}");
        }

        /// <summary>
        /// Resets all difficulty images to their original scale.
        /// </summary>
        private void ResetAllImageScales()
        {
            if (_debug) Debug.Log("Resetting all image scales to original");

            if (majorImage != null)
            {
                if (_debug) Debug.Log($"Resetting Major from {majorImage.transform.localScale} to {_majorOriginalScale}");
                majorImage.transform.localScale = _majorOriginalScale;
            }

            if (colonelImage != null)
            {
                if (_debug) Debug.Log($"Resetting Colonel from {colonelImage.transform.localScale} to {_colonelOriginalScale}");
                colonelImage.transform.localScale = _colonelOriginalScale;
            }

            if (generalImage != null)
            {
                if (_debug) Debug.Log($"Resetting General from {generalImage.transform.localScale} to {_generalOriginalScale}");
                generalImage.transform.localScale = _generalOriginalScale;
            }
        }

        #endregion // Private Methods
    }
}
