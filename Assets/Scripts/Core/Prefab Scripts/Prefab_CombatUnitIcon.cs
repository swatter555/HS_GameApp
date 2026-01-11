using HammerAndSickle.Controllers;
using HammerAndSickle.Services;
using System;
using TMPro;
using UnityEngine;

namespace HammerAndSickle.Core
{
    /// <summary>
    /// Manages a combat unit icon prefab instance including unit sprite, flag, and NATO icon.
    /// </summary>
    public class Prefab_CombatUnitIcon : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(Prefab_CombatUnitIcon);

        #region Inspector Fields

        [Header("Component References")]
        [SerializeField] private SpriteRenderer unitIconRenderer;
        [SerializeField] private SpriteRenderer flagRenderer;
        [SerializeField] private SpriteRenderer stackingIconRenderer;
        [SerializeField] private TextMeshPro hitPointsText;

        [Header("Font Settings")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Color ratioTextColor = Color.black;

        #endregion // Inspector Fields

        #region Properties

        /// <summary>
        /// Gets the unit icon sprite renderer.
        /// </summary>
        public SpriteRenderer UnitIconRenderer => unitIconRenderer;

        /// <summary>
        /// Gets the flag sprite renderer.
        /// </summary>
        public SpriteRenderer FlagRenderer => flagRenderer;

        /// <summary>
        /// Gets the stacking icon sprite renderer.
        /// </summary>
        public SpriteRenderer StackingIconRenderer => stackingIconRenderer;

        /// <summary>
        /// Gets or sets the hit points display text.
        /// </summary>
        public string HitPointsRatio
        {
            get => hitPointsText != null ? hitPointsText.text : string.Empty;
            set
            {
                if (hitPointsText != null)
                {
                    hitPointsText.text = value;
                }
            }
        }

        #endregion // Properties

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
        }

        #endregion // Unity Lifecycle

        #region Public Methods

        /// <summary>
        /// Sets the unit icon sprite.
        /// </summary>
        public void SetUnitIcon(string spriteName)
        {
            try
            {
                if (string.IsNullOrEmpty(spriteName))
                {
                    AppService.CaptureUiMessage($"{CLASS_NAME}.SetUnitIcon: Sprite name is null or empty.");
                    return;
                }

                unitIconRenderer.sprite = SpriteManager.GetSprite(spriteName);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetUnitIcon), e);
            }
        }

        /// <summary>
        /// Sets the unit flag sprite.
        /// </summary>
        public void SetFlag(string spriteName)
        {
            try
            {
                if (flagRenderer == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(spriteName))
                {
                    AppService.CaptureUiMessage($"{CLASS_NAME}.SetFlag: Sprite name is null or empty.");
                    return;
                }

                flagRenderer.sprite = SpriteManager.GetSprite(spriteName);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetFlag), e);
            }
        }

        /// <summary>
        /// Initializes the TextMeshPro component with the assigned font and color.
        /// Call this after instantiation if settings need to be applied dynamically.
        /// </summary>
        public void InitializeHitPointsText()
        {
            if (hitPointsText == null) return;

            if (fontAsset != null)
            {
                hitPointsText.font = fontAsset;
            }

            hitPointsText.color = ratioTextColor;
        }

        /// <summary>
        /// Sets the stacking icon sprite for air/land stacking toggle.
        /// </summary>
        /// <param name="spriteName">Name of the stacking icon sprite</param>
        public void SetStackingIcon(string spriteName)
        {
            try
            {
                if (stackingIconRenderer == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(spriteName))
                {
                    AppService.CaptureUiMessage($"{CLASS_NAME}.SetStackingIcon: Sprite name is null or empty.");
                    return;
                }

                stackingIconRenderer.sprite = SpriteManager.GetSprite(spriteName);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetStackingIcon), e);
            }
        }

        /// <summary>
        /// Shows or hides the stacking icon.
        /// </summary>
        /// <param name="show">True to show, false to hide</param>
        public void ShowStackingIcon(bool show)
        {
            if (stackingIconRenderer != null)
            {
                stackingIconRenderer.enabled = show;
            }
        }

        /// <summary>
        /// Sets the opacity of the unit icon and flag renderers.
        /// Used for stacking to show non-dominant units at reduced opacity.
        /// </summary>
        /// <param name="opacity">Opacity value from 0 (transparent) to 1 (opaque)</param>
        public void SetOpacity(float opacity)
        {
            opacity = Mathf.Clamp01(opacity);

            if (unitIconRenderer != null)
            {
                Color color = unitIconRenderer.color;
                color.a = opacity;
                unitIconRenderer.color = color;
            }

            if (flagRenderer != null)
            {
                Color color = flagRenderer.color;
                color.a = opacity;
                flagRenderer.color = color;
            }

            if (hitPointsText != null)
            {
                Color color = hitPointsText.color;
                color.a = opacity;
                hitPointsText.color = color;
            }
        }

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Validates that all required references are set.
        /// </summary>
        private void ValidateReferences()
        {
            if (unitIconRenderer == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateReferences: unitIconRenderer is null");

            // Optional components - log warnings but don't throw
            if (flagRenderer == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: flagRenderer is not assigned");

            if (stackingIconRenderer == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: stackingIconRenderer is not assigned");

            if (hitPointsText == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: hitPointsText is not assigned");

            if (fontAsset == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: fontAsset is not assigned");
        }

        #endregion // Private Methods
    }
}
