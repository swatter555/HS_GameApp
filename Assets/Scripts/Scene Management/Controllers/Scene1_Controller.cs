using HammerAndSickle.Controllers;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Core.Patterns;
using HammerAndSickle.Services;
using System;
using UnityEngine;

namespace HammerAndSickle.SceneManagement.Controllers
{
    /// <summary>
    /// Scene controller for Scene 1 (Battle).
    /// Owns dialog flow: subscribes to EventManager dialog events and manages
    /// which overlay is visible on top of the always-visible HUD.
    /// When the HUD loses focus, map input is disabled via BattleHudPanel.OnFocusChanged.
    /// </summary>
    public class Scene1_Controller : Singleton<Scene1_Controller>
    {
        private const string CLASS_NAME = nameof(Scene1_Controller);

        #region Serialized Fields

        [SerializeField] private UIPanel _defaultDialog;
        [SerializeField] private UIPanel _ordersDialog;
        [SerializeField] private PrinterControl _printerControl;

        #endregion // Serialized Fields

        #region Fields

        private UIPanel _activeOverlay;

        #endregion // Fields

        #region Unity Lifecycle

        private void Start()
        {
            ValidateGameSystems();

            // See EventManager: subscribe to Scene 1 dialog switch requests
            EventManager.Instance.OnScene1DialogRequested += OnDialogRequested;

            // Initialize the HQ printer (must be called before battle data loads)
            _printerControl.Initialize();

            // Load map, OOB, and refresh the renderer
            if (!BattleManager.Instance.SetupBattleManagerData())
                AppService.HandleException(CLASS_NAME, nameof(Start),
                    new Exception("Failed to setup BattleManager data."));

            // Show the HUD but defocused — input stays disabled until orders overlay closes
            _defaultDialog.Show();
            _defaultDialog.SetFocus(false);

            // Open the orders overlay through the same event path all dialog switches use
            // See EventManager: open orders overlay at scene start
            EventManager.Instance.RaiseScene1DialogRequested(_ordersDialog);

            InitializeAudio();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            try
            {
                // See EventManager: unsubscribe from Scene 1 dialog switch requests
                if (EventManager.Instance != null)
                    EventManager.Instance.OnScene1DialogRequested -= OnDialogRequested;

                if (GameAudioManager.Instance != null)
                    GameAudioManager.Instance.StopMusic(fadeOutTime: 0.5f);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnDestroy), e);
            }
        }

        #endregion // Unity Lifecycle

        #region Dialog Flow

        /// <summary>
        /// Handles dialog switch requests from EventManager.
        /// Hides the current overlay (if any), then either returns focus to
        /// the HUD (enabling map input) or opens the requested overlay
        /// (disabling map input via HUD losing focus).
        /// </summary>
        /// <param name="target">The dialog panel to switch to</param>
        private void OnDialogRequested(UIPanel target)
        {
            // Hide the current overlay if one is open
            if (_activeOverlay != null)
                _activeOverlay.Hide();

            if (target == _defaultDialog)
            {
                // Returning to HUD: clear overlay reference, restore focus.
                // BattleHudPanel.OnFocusChanged(true) will enable map input.
                _activeOverlay = null;
                _defaultDialog.SetFocus(true);
            }
            else
            {
                // Opening an overlay: show the target, defocus HUD.
                // BattleHudPanel.OnFocusChanged(false) will disable map input.
                _activeOverlay = target;
                _activeOverlay.Show();
                _defaultDialog.SetFocus(false);
            }
        }

        #endregion // Dialog Flow

        #region Private Methods

        private void InitializeAudio()
        {
            try
            {
                GameAudioManager.EnsureExists();
                GameAudioManager.Instance.StopMusic();
                GameAudioManager.Instance.PlaySFX(GameAudioManager.SoundEffect.MeduimSnareDrum);
                GameAudioManager.Instance.PlayAmbient(GameAudioManager.AmbientSound.AmbientCombat, fadeInTime: 1.0f);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(InitializeAudio), e);
            }
        }

        private void ValidateGameSystems()
        {
            try
            {
                if (GameDataManager.Instance == null)
                    throw new Exception("GameDataManager instance is null.");

                if (!GameDataManager.Instance.IsReady)
                    throw new Exception("GameDataManager instance is not ready.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateGameSystems), e);
                AppService.UnityQuit_DataUnsafe();
            }
        }

        #endregion // Private Methods
    }
}
