using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// Provides smooth scale animation for UI buttons on hover.
    /// Scales the button up when hovered and back to original size when pointer exits.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Enums

        /// <summary>
        /// Predefined scale increase percentages for hover effect.
        /// </summary>
        public enum ScaleAmount
        {
            Five_Percent = 5,
            Ten_Percent = 10,
            Fifteen_Percent = 15
        }

        #endregion // Enums

        #region Inspector Fields

        [Header("Hover Scale Settings")]
        [SerializeField] private ScaleAmount scaleIncrease = ScaleAmount.Ten_Percent;
        [SerializeField] private float animationDuration = 0.15f;
        [SerializeField] private bool onlyIfInteractable = true;

        [Header("Advanced Settings")]
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        #endregion // Inspector Fields

        #region Private Fields

        private Button _button;
        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private Coroutine _scaleCoroutine;
        private bool _isHovering = false;

        #endregion // Private Fields

        #region Unity Lifecycle

        /// <summary>
        /// Initialize references and cache original scale.
        /// </summary>
        private void Awake()
        {
            _button = GetComponent<Button>();
            _originalScale = transform.localScale;
            CalculateTargetScale();
        }

        /// <summary>
        /// Reset scale when component is disabled.
        /// </summary>
        private void OnDisable()
        {
            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
                _scaleCoroutine = null;
            }
            transform.localScale = _originalScale;
            _isHovering = false;
        }

        /// <summary>
        /// Recalculate target scale if changed in Inspector at runtime.
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying && _originalScale != Vector3.zero)
            {
                CalculateTargetScale();
            }
        }

        #endregion // Unity Lifecycle

        #region Event Handlers

        /// <summary>
        /// Handles pointer entering the button area - scales up.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ShouldAnimate())
            {
                _isHovering = true;
                AnimateScale(_targetScale);
            }
        }

        /// <summary>
        /// Handles pointer exiting the button area - scales back to normal.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            AnimateScale(_originalScale);
        }

        #endregion // Event Handlers

        #region Private Methods

        /// <summary>
        /// Calculates the target scale based on the selected percentage increase.
        /// </summary>
        private void CalculateTargetScale()
        {
            float scaleMultiplier = 1f + ((float)scaleIncrease / 100f);
            _targetScale = _originalScale * scaleMultiplier;
        }

        /// <summary>
        /// Determines if the scale animation should play.
        /// </summary>
        private bool ShouldAnimate()
        {
            if (onlyIfInteractable && _button != null && !_button.interactable)
                return false;
            return true;
        }

        /// <summary>
        /// Starts the scale animation to the target scale.
        /// </summary>
        private void AnimateScale(Vector3 targetScale)
        {
            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
            }
            _scaleCoroutine = StartCoroutine(ScaleCoroutine(targetScale));
        }

        /// <summary>
        /// Coroutine that smoothly animates the scale change.
        /// </summary>
        private IEnumerator ScaleCoroutine(Vector3 targetScale)
        {
            Vector3 startScale = transform.localScale;
            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / animationDuration;

                // Apply animation curve for smooth easing
                float curveValue = scaleCurve.Evaluate(t);
                transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);

                yield return null;
            }

            // Ensure we end exactly at target scale
            transform.localScale = targetScale;
            _scaleCoroutine = null;
        }

        #endregion // Private Methods

        #region Public Methods

        /// <summary>
        /// Manually trigger the hover scale effect.
        /// Useful for keyboard or controller navigation.
        /// </summary>
        public void TriggerHoverEffect()
        {
            if (ShouldAnimate() && !_isHovering)
            {
                _isHovering = true;
                AnimateScale(_targetScale);
            }
        }

        /// <summary>
        /// Manually reset to original scale.
        /// </summary>
        public void ResetScale()
        {
            _isHovering = false;
            AnimateScale(_originalScale);
        }

        /// <summary>
        /// Updates the scale percentage at runtime.
        /// </summary>
        public void SetScaleAmount(ScaleAmount amount)
        {
            scaleIncrease = amount;
            CalculateTargetScale();

            // Update current scale if hovering
            if (_isHovering)
            {
                AnimateScale(_targetScale);
            }
        }

        #endregion // Public Methods
    }
}