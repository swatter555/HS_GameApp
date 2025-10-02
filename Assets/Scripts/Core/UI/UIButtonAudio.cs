using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HammerAndSickle.Controllers;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// Provides audio feedback for UI button interactions with customizable sounds
    /// for hover and click events. Configurable per-button through Inspector.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler
    {
        #region Inspector Fields

        [Header("Click Sound Settings")]
        [SerializeField] private bool enableClickSound = true;
        [SerializeField] private GameAudioManager.SoundEffect clickSound = GameAudioManager.SoundEffect.ButtonClick;

        [Header("Hover Sound Settings")]
        [SerializeField] private bool enableHoverSound = true;
        [SerializeField] private GameAudioManager.SoundEffect hoverSound = GameAudioManager.SoundEffect.ButtonHover;

        [Header("Optional Settings")]
        [SerializeField] private bool playOnlyIfInteractable = true;
        [SerializeField] private float clickVolumeScale = 1.0f;
        [SerializeField] private float hoverVolumeScale = 0.8f;

        #endregion // Inspector Fields

        #region Private Fields

        private Button _button;
        private bool _isHovering = false;

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
        /// Handles pointer entering the button area.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isHovering && ShouldPlayHoverSound())
            {
                _isHovering = true;
                PlayHoverSound();
            }
        }

        /// <summary>
        /// Handles pointer exiting the button area.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
        }

        /// <summary>
        /// Handles pointer click completion (mouse up over button).
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // Click event for consistency, but sound plays on PointerDown for responsiveness
        }

        /// <summary>
        /// Handles pointer down for immediate audio feedback.
        /// </summary>
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
        /// Determines if hover sound should play based on settings and button state.
        /// </summary>
        private bool ShouldPlayHoverSound()
        {
            if (!enableHoverSound) return false;
            if (playOnlyIfInteractable && _button != null && !_button.interactable) return false;
            if (GameAudioManager.Instance == null) return false;
            return true;
        }

        /// <summary>
        /// Determines if click sound should play based on settings and button state.
        /// </summary>
        private bool ShouldPlayClickSound()
        {
            if (!enableClickSound) return false;
            if (playOnlyIfInteractable && _button != null && !_button.interactable) return false;
            if (GameAudioManager.Instance == null) return false;
            return true;
        }

        /// <summary>
        /// Plays the configured hover sound effect.
        /// </summary>
        private void PlayHoverSound()
        {
            try
            {
                if (hoverSound != GameAudioManager.SoundEffect.None)
                {
                    GameAudioManager.Instance.PlaySFXWithVariation(
                        hoverSound,
                        hoverVolumeScale,
                        0f // No pitch variation for UI sounds
                    );
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UIButtonAudio.PlayHoverSound failed: {e.Message}");
            }
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
                    GameAudioManager.Instance.PlaySFXWithVariation(
                        clickSound,
                        clickVolumeScale,
                        0f // No pitch variation for UI sounds
                    );
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
        /// Manually trigger hover sound (useful for controller navigation).
        /// </summary>
        public void TriggerHoverSound()
        {
            if (ShouldPlayHoverSound())
            {
                PlayHoverSound();
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

        /// <summary>
        /// Updates the hover sound at runtime.
        /// </summary>
        public void SetHoverSound(GameAudioManager.SoundEffect sound, bool enable = true)
        {
            hoverSound = sound;
            enableHoverSound = enable;
        }

        #endregion // Public Methods
    }
}