using HammerAndSickle.Controllers;
using HammerAndSickle.Services;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Core.Patterns;
using System;
using UnityEngine;

namespace HammerAndSickle.SceneManagement.Controllers
{
    /// <summary>
    /// Scene controller for Scene 0 (Main Menu).
    /// Owns dialog flow: subscribes to EventManager dialog events and manages
    /// which overlay is visible on top of the always-visible default dialog.
    /// </summary>
    public class Scene0_Controller : Singleton<Scene0_Controller>
    {
        private const string CLASS_NAME = nameof(Scene0_Controller);

        #region Serialized Fields

        [SerializeField] private UIPanel _defaultDialog;

        #endregion // Serialized Fields

        #region Fields

        private UIPanel _activeOverlay;

        #endregion // Fields

        #region Unity Lifecycle

        private void Start()
        {
            ValidateGameSystems();

            // See EventManager: subscribe to Scene 0 dialog switch requests
            EventManager.Instance.OnScene0DialogRequested += OnDialogRequested;

            // Show the default (home) dialog with focus
            _defaultDialog.Show();
            _defaultDialog.SetFocus(true);

            InitializeAudio();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            try
            {
                // See EventManager: unsubscribe from Scene 0 dialog switch requests
                if (EventManager.Instance != null)
                    EventManager.Instance.OnScene0DialogRequested -= OnDialogRequested;

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
        /// the default dialog or opens the requested overlay on top of it.
        /// </summary>
        /// <param name="target">The dialog panel to switch to</param>
        private void OnDialogRequested(UIPanel target)
        {
            // Hide the current overlay if one is open
            if (_activeOverlay != null)
                _activeOverlay.Hide();

            if (target == _defaultDialog)
            {
                // Returning to home: clear overlay reference, restore default focus
                _activeOverlay = null;
                _defaultDialog.SetFocus(true);
            }
            else
            {
                // Opening an overlay: show the target, defocus default so its buttons
                // become non-interactable while the overlay is on screen
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

                GameAudioManager.Instance.PreloadSFX(
                    GameAudioManager.SoundEffect.ButtonClick,
                    GameAudioManager.SoundEffect.ButtonHover,
                    GameAudioManager.SoundEffect.MenuOpen,
                    GameAudioManager.SoundEffect.MenuClose,
                    GameAudioManager.SoundEffect.RadioButtonClick
                );

                GameAudioManager.Instance.PlayMusic(
                    GameAudioManager.MusicTrack.MainMenu,
                    loop: true,
                    fadeInTime: 1.0f
                );
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
