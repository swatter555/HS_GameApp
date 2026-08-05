using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HammerAndSickle.Audio;
using HammerAndSickle.Controllers;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// Click audio for a UI button, configured per-button in the Inspector.
    /// </summary>
    /// <remarks>
    /// ⚠ HOVER AUDIO WAS DELETED 2026-08-03 (Bob's call, ratified HS_DesignDoc §27.7 / todo_audio D6) and
    /// should not come back. It fires on pointer MOTION rather than on intent, so it machine-guns on any
    /// sweep across a button row, and it says nothing the hover VISUAL (UIButtonHoverScale) has not already
    /// said. It was also the project's clearest example of the "looks wired, does nothing" trap: the
    /// handlers had been commented out at some point, leaving a fully populated "Hover Sound Settings"
    /// block in the Inspector, an assigned clip on all seven menu buttons, and a shipped
    /// SFX_ButtonHover.wav that never once played. Removed with the clip; deleting beat disabling, because
    /// a switched-off option still reads as available.
    /// </remarks>
    [RequireComponent(typeof(Button))]
    public class UIButtonAudio : MonoBehaviour, IPointerDownHandler
    {
        #region Inspector Fields

        [Header("Click Sound Settings")]
        [SerializeField] private bool enableClickSound = true;
        [SerializeField] private GameAudioManager.SoundEffect clickSound = GameAudioManager.SoundEffect.ButtonClick;

        [Header("Optional Settings")]
        [SerializeField] private bool playOnlyIfInteractable = true;
        [SerializeField] private float clickVolumeScale = 1.0f;

        #endregion // Inspector Fields

        #region Private Fields

        private Button _button;

        #endregion // Private Fields

        #region Unity Lifecycle

        /// <summary>
        /// Cache button reference on awake for performance.
        /// </summary>
        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        #endregion // Unity Lifecycle

        #region Event Handlers

        /// <summary>
        /// Handles pointer down for immediate audio feedback.
        /// </summary>
        /// <remarks>
        /// ⚠ PointerDown, NOT onClick, and NOT PointerUp. Sound fires on PRESS because that is when the
        /// player commits, and any later is perceptibly late. It also means button audio is completely
        /// independent of the Inspector-owns-onClick contract (Claude_Project §3.6b) — adding audio to a
        /// button can never double-fire its callback.
        /// </remarks>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (ShouldPlayClickSound())
            {
                PlayClickSound();
            }
        }

        #endregion // Event Handlers

        #region Private Methods

        /// <summary>
        /// Determines if click sound should play based on settings and button state.
        /// </summary>
        private bool ShouldPlayClickSound()
        {
            if (!enableClickSound) return false;
            if (playOnlyIfInteractable && _button != null && !_button.interactable) return false;
            return true;
        }

        /// <summary>
        /// Plays the configured click sound effect.
        /// </summary>
        private void PlayClickSound()
        {
            try
            {
                if (clickSound != GameAudioManager.SoundEffect.None)
                {
                    GameAudio.Play(clickSound, clickVolumeScale);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UIButtonAudio.PlayClickSound failed: {e.Message}");
            }
        }

        #endregion // Private Methods

        #region Public Methods

        /// <summary>
        /// Manually trigger click sound (useful for keyboard navigation).
        /// </summary>
        public void TriggerClickSound()
        {
            if (ShouldPlayClickSound())
            {
                PlayClickSound();
            }
        }

        /// <summary>
        /// Updates the click sound at runtime.
        /// </summary>
        public void SetClickSound(GameAudioManager.SoundEffect sound, bool enable = true)
        {
            clickSound = sound;
            enableClickSound = enable;
        }

        #endregion // Public Methods
    }
}
