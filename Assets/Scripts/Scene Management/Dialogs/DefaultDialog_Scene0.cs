using HammerAndSickle.Controllers;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Services;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.SceneManagement
{
    /// <summary>
    /// Default (home) dialog for Scene 0 (Main Menu).
    /// Displays navigation buttons and raises dialog switch events through EventManager.
    /// </summary>
    public class DefaultDialog_Scene0 : UIPanel
    {
        private const string CLASS_NAME = nameof(DefaultDialog_Scene0);

        #region Serialized Fields

        [Header("Navigation Buttons")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _campaignButton;
        [SerializeField] private Button _scenarioButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;

        [Header("Dialog Targets")]
        [SerializeField] private UIPanel _continueDialog;
        [SerializeField] private UIPanel _campaignDialog;
        [SerializeField] private UIPanel _scenarioDialog;
        [SerializeField] private UIPanel _settingsDialog;
        [SerializeField] private UIPanel _exitDialog;

        #endregion // Serialized Fields

        #region Unity Lifecycle

        private void Start()
        {
            try
            {
                if (_continueButton == null ||
                    _campaignButton == null ||
                    _scenarioButton == null ||
                    _settingsButton == null ||
                    _exitButton == null)
                {
                    throw new Exception($"{CLASS_NAME} button controls invalid");
                }

                if (_continueDialog == null ||
                    _campaignDialog == null ||
                    _scenarioDialog == null ||
                    _settingsDialog == null ||
                    _exitDialog == null)
                {
                    // TODO: When all dialogs are implemeneted, uncommment the exception.
                    //throw new Exception($"{CLASS_NAME} dialog targets invalid");
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

        protected override void OnFocusChanged(bool hasFocus)
        {
            // Enable or disable menu buttons based on whether this dialog has focus.
            // Buttons are disabled when an overlay dialog is open on top of us.
            if (_continueButton != null) _continueButton.interactable = hasFocus;
            if (_campaignButton != null) _campaignButton.interactable = hasFocus;
            if (_scenarioButton != null) _scenarioButton.interactable = hasFocus;
            if (_settingsButton != null) _settingsButton.interactable = hasFocus;
            if (_exitButton != null)     _exitButton.interactable = hasFocus;
        }

        #endregion // UIPanel Overrides

        #region Button Callbacks

        /// <summary>
        /// Called by the Continue button. Requests switch to the continue dialog.
        /// </summary>
        public void OnContinueButton()
        {
            // See EventManager: request dialog switch to continue
            EventManager.Instance.RaiseScene0DialogRequested(_continueDialog);
        }

        /// <summary>
        /// Called by the Campaign button. Requests switch to the new campaign dialog.
        /// </summary>
        public void OnCampaignButton()
        {
            // See EventManager: request dialog switch to new campaign
            EventManager.Instance.RaiseScene0DialogRequested(_campaignDialog);
        }

        /// <summary>
        /// Called by the Scenario button. Requests switch to the scenario selection dialog.
        /// </summary>
        public void OnScenarioButton()
        {
            // See EventManager: request dialog switch to scenario selection
            EventManager.Instance.RaiseScene0DialogRequested(_scenarioDialog);
        }

        /// <summary>
        /// Called by the Settings button. Requests switch to the settings dialog.
        /// </summary>
        public void OnSettingsButton()
        {
            // See EventManager: request dialog switch to settings
            EventManager.Instance.RaiseScene0DialogRequested(_settingsDialog);
        }

        /// <summary>
        /// Called by the Exit button. Requests switch to the exit confirmation dialog.
        /// </summary>
        public void OnExitButton()
        {
            // See EventManager: request dialog switch to exit confirmation
            EventManager.Instance.RaiseScene0DialogRequested(_exitDialog);
        }

        #endregion // Button Callbacks
    }
}
