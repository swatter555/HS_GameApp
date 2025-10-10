using HammerAndSickle.Services;
using UnityEngine;
using UnityEngine.UI;
using HammerAndSickle.Controllers;

namespace HammerAndSickle.SceneDirectors
{
    /// <summary>
    /// Main scene core interface menu handler.
    /// </summary>
    public class Scene0_Core : MenuHandler
    {
        #region Singleton Instance

        public static Scene0_Core Instance { get; private set; }

        #endregion // Singleton Instance

        #region Control Fields

        // UI Controls
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _campaignButton;
        [SerializeField] private Button _scenarioButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;

        #endregion // Control Fields

        #region Overrides

        public override void ToggleMenu()
        {
            // Set active state and interactivity based on visibility and input focus.

            // Continue button
            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(IsVisible);
                _continueButton.interactable = IsInputFocus;
            }

            // Campaign button
            if (_campaignButton != null)
            {
                _campaignButton.gameObject.SetActive(IsVisible);
                _campaignButton.interactable = IsInputFocus;
            }

            // Scenario button
            if (_scenarioButton != null)
            {
                _scenarioButton.gameObject.SetActive(IsVisible);
                _scenarioButton.interactable = IsInputFocus;
            }

            // Settings button
            if (_settingsButton != null)
            {
                _settingsButton.gameObject.SetActive(IsVisible);
                _settingsButton.interactable = IsInputFocus;
            }

            // Exit button
            if (_exitButton != null)
            {
                _exitButton.gameObject.SetActive(IsVisible);
                _exitButton.interactable = IsInputFocus;
            }
        }

        public override void Start()
        {
            if (_continueButton == null ||
                _campaignButton == null ||
                _scenarioButton == null ||
                _settingsButton == null ||
                _exitButton == null)
            {
                AppService.HandleException(GetType().Name, nameof(Start), new System.Exception("MainSceneCoreInterface control invalid"));
                AppService.UnityQuit_DataUnsafe(); // Consider adding a Datasafe quit option
            }
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

        public static void OnContinueButton()
        {
            // Continue game logic
            Debug.Log("Continue button pressed.");
        }

        public void OnCampaignButton()
        {
            // Start new campaign logic
            Debug.Log("Campaign button pressed.");
        }

        public void OnScenarioButton()
        {
            // Switch to scenario dialog.
            Scene0_Director.Instance.SetActiveMenuByID(GeneralConstants.MainScene_ScenarioDialog_ID);
        }

        public void OnSettingsButton()
        {
            // Open settings menu logic
            Debug.Log("Settings button pressed.");
        }

        public void OnExitButton()
        {
            // Exit game logic
            Debug.Log("Exit button pressed.");
        }

        #endregion // Callbacks
    }
}