using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
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
        [SerializeField] private SpriteRenderer outlineRenderer;

        [Header("Unit Outline Settings")]
        [SerializeField][Range(1f, 100f)] private float outlineThickness = 10f;

        #endregion // Inspector Fields

        #region Properties

        /// <summary>
        /// Gets the unit icon sprite renderer (the one that will receive outline treatment).
        /// </summary>
        public SpriteRenderer UnitIconRenderer => unitIconRenderer;

        #endregion // Properties

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
        }

        #endregion // Unity Lifecycle

        #region Public Methods

        /// <summary>
        /// Sets the unit icon sprite and updates the outline to match.
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

                // Set the main sprite
                unitIconRenderer.sprite = SpriteManager.GetSprite(spriteName);

                // Update outline sprite to match (if outline renderer exists)
                if (outlineRenderer != null)
                {
                    outlineRenderer.sprite = unitIconRenderer.sprite;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetUnitIcon), e);
            }
        }

        /// <summary>
        /// Applies outline rendering configuration based on unit nationality.
        /// Sets outline color, scale, and sorting order. The outline sprite is automatically
        /// matched to the unit sprite via SetUnitIcon().
        /// </summary>
        public void ApplyOutline(Nationality nationality)
        {
            try
            {
                if (outlineRenderer == null || unitIconRenderer == null)
                {
                    AppService.CaptureUiMessage($"{CLASS_NAME}.ApplyOutline: Renderer references are null.");
                    return;
                }

                // Get color based on nationality
                Color outlineColor = GetOutlineColorForNationality(nationality);
                outlineRenderer.color = outlineColor;

                // Scale the outline to be larger than the main sprite
                float scaleFactor = 1f + (outlineThickness / 100f);
                outlineRenderer.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

                // Ensure outline renders behind the main sprite
                outlineRenderer.sortingOrder = unitIconRenderer.sortingOrder - 1;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ApplyOutline), e);
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

            if (outlineRenderer == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateReferences: outlineRenderer is null");
        }

        /// <summary>
        /// Gets the outline color based on unit nationality.
        /// Soviet units = Red, Middle Eastern nations = Green, Others = Blue.
        /// </summary>
        private static Color GetOutlineColorForNationality(Nationality nationality)
        {
            return nationality switch
            {
                Nationality.USSR => Color.red,
                Nationality.MJ => Color.green,
                Nationality.IR => Color.green,
                Nationality.IQ => Color.green,
                Nationality.SAUD => Color.green,
                _ => Color.blue // USA, FRG, UK, FRA, GENERIC
            };
        }

        #endregion // Private Methods
    }
}
