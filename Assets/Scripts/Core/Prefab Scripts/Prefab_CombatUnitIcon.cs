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

            if (hitPointsText == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: hitPointsText is not assigned");

            if (fontAsset == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: fontAsset is not assigned");
        }

        #endregion // Private Methods
    }
}
