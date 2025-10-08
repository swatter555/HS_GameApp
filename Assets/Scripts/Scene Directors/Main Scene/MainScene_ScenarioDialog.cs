using HammerAndSickle.Services;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.SceneDirectors
{
    /// <summary>
    /// Main scene core interface menu handler.
    /// </summary>
    public class MainScene_ScenarioDialog : MenuHandler
    {
        #region Singleton Instance

        public static MainScene_ScenarioDialog Instance { get; private set; }

        #endregion // Singleton Instance

        #region Control Fields

        // UI Controls
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _randomButton;
        [SerializeField] private Button _exitButton;
        

        #endregion // Control Fields

        #region Overrides

        public override void ToggleMenu()
        {
            // Set active state and interactivity based on visibility and input focus.

            // Continue button
            if (_loadButton != null)
            {
                _loadButton.gameObject.SetActive(IsVisible);
                _loadButton.interactable = IsInputFocus;
            }

            // Campaign button
            if (_randomButton != null)
            {
                _randomButton.gameObject.SetActive(IsVisible);
                _randomButton.interactable = IsInputFocus;
            }

            // Scenario button
            if (_exitButton != null)
            {
                _exitButton.gameObject.SetActive(IsVisible);
                _exitButton.interactable = IsInputFocus;
            }
        }

        public override void Start()
        {
            if (_loadButton == null ||
                _randomButton == null ||
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

        public static void OnLoadButton()
        {
            // Continue game logic
            Debug.Log("Load button pressed.");
        }

        public void OnRandomButton()
        {
            // Start new campaign logic
            Debug.Log("random button pressed.");
        }

        public void OnExitButton()
        {
            // Start new scenario logic
            Debug.Log("exit button pressed.");
        }

        #endregion // Callbacks
    }
}