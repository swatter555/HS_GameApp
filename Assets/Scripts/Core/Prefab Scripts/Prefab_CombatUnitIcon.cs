using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;
using System;
using TMPro;
using UnityEngine;

namespace HammerAndSickle.Core
{
    /// <summary>
    /// Manages a combat unit icon prefab instance including unit sprite, nationality symbol,
    /// hit points display, deployment state icon, and stacking icon.
    /// Subscribes to EventManager for hit point and deployment state changes.
    /// </summary>
    public class Prefab_CombatUnitIcon : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(Prefab_CombatUnitIcon);

        #region Inspector Fields

        [Header("Component References")]
        [SerializeField] private SpriteRenderer unitIcon;
        [SerializeField] private SpriteRenderer nationIcon;
        [SerializeField] private SpriteRenderer boxIcon;
        [SerializeField] private TextMeshPro boxText;
        [SerializeField] private SpriteRenderer deployIcon;
        [SerializeField] private SpriteRenderer stackingIcon;

        [Header("Font Settings")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Color ratioTextColor = Color.black;

        #endregion // Inspector Fields

        #region Fields

        /// <summary>
        /// The unit ID this prefab instance represents. Set during initialization.
        /// </summary>
        private string _unitId;

        /// <summary>
        /// Callback provided by the renderer to resolve a deployment position and embarkment state into a sprite name.
        /// </summary>
        private Func<DeploymentPosition, EmbarkmentState, string> _resolveDeploySprite;

        #endregion // Fields

        #region Properties

        /// <summary>
        /// Gets the unit ID this icon represents.
        /// </summary>
        public string UnitId => _unitId;

        /// <summary>
        /// Gets the unit icon sprite renderer.
        /// </summary>
        public SpriteRenderer UnitIconRenderer => unitIcon;

        /// <summary>
        /// Gets the nationality icon sprite renderer.
        /// </summary>
        public SpriteRenderer NationIconRenderer => nationIcon;

        /// <summary>
        /// Gets the stacking icon sprite renderer.
        /// </summary>
        public SpriteRenderer StackingIconRenderer => stackingIcon;

        /// <summary>
        /// Gets or sets the hit points display text as a percentage (1-100).
        /// </summary>
        public string HitPointsRatio
        {
            get => boxText != null ? boxText.text : string.Empty;
            set
            {
                if (boxText != null)
                {
                    boxText.text = value;
                }
            }
        }

        #endregion // Properties

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion // Unity Lifecycle

        #region Public Methods

        /// <summary>
        /// Initializes the prefab with its owning unit ID, sets initial visual state, and subscribes to events.
        /// Must be called after instantiation before the prefab will respond to events.
        /// </summary>
        /// <param name="unitId">The unit ID this icon represents</param>
        /// <param name="hitPointPercent">Current hit points as a percentage (1-100)</param>
        /// <param name="deploymentPosition">Current deployment position for the deploy icon</param>
        /// <param name="embarkmentState">Current embarkment state (relevant when position is Embarked)</param>
        /// <param name="resolveDeploySprite">Callback to resolve a DeploymentPosition and EmbarkmentState to a sprite name</param>
        public void Initialize(string unitId, int hitPointPercent, DeploymentPosition deploymentPosition,
            EmbarkmentState embarkmentState, Func<DeploymentPosition, EmbarkmentState, string> resolveDeploySprite = null)
        {
            _unitId = unitId;
            _resolveDeploySprite = resolveDeploySprite;

            InitializeHitPointsText();
            HitPointsRatio = hitPointPercent.ToString();

            if (_resolveDeploySprite != null)
            {
                string spriteName = _resolveDeploySprite(deploymentPosition, embarkmentState);
                SetDeployIcon(spriteName);
            }

            SubscribeToEvents();
        }

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

                unitIcon.sprite = SpriteManager.GetSprite(spriteName);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetUnitIcon), e);
            }
        }

        /// <summary>
        /// Sets the nationality symbol sprite.
        /// </summary>
        public void SetNationIcon(string spriteName)
        {
            try
            {
                if (nationIcon == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(spriteName))
                {
                    AppService.CaptureUiMessage($"{CLASS_NAME}.SetNationIcon: Sprite name is null or empty.");
                    return;
                }

                nationIcon.sprite = SpriteManager.GetSprite(spriteName);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetNationIcon), e);
            }
        }

        /// <summary>
        /// Sets the box icon sprite (background behind hit points text).
        /// </summary>
        public void SetBoxIcon(string spriteName)
        {
            try
            {
                if (boxIcon == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(spriteName))
                {
                    AppService.CaptureUiMessage($"{CLASS_NAME}.SetBoxIcon: Sprite name is null or empty.");
                    return;
                }

                boxIcon.sprite = SpriteManager.GetSprite(spriteName);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetBoxIcon), e);
            }
        }

        /// <summary>
        /// Shows or hides the box icon.
        /// </summary>
        public void ShowBoxIcon(bool show)
        {
            if (boxIcon != null)
            {
                boxIcon.enabled = show;
            }
        }

        /// <summary>
        /// Sets the deployment state icon sprite.
        /// </summary>
        public void SetDeployIcon(string spriteName)
        {
            try
            {
                if (deployIcon == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(spriteName))
                {
                    AppService.CaptureUiMessage($"{CLASS_NAME}.SetDeployIcon: Sprite name is null or empty.");
                    return;
                }

                deployIcon.sprite = SpriteManager.GetSprite(spriteName);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetDeployIcon), e);
            }
        }

        /// <summary>
        /// Shows or hides the deployment state icon.
        /// </summary>
        public void ShowDeployIcon(bool show)
        {
            if (deployIcon != null)
            {
                deployIcon.enabled = show;
            }
        }

        /// <summary>
        /// Sets the stacking icon sprite for air/land stacking toggle.
        /// </summary>
        public void SetStackingIcon(string spriteName)
        {
            try
            {
                if (stackingIcon == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(spriteName))
                {
                    AppService.CaptureUiMessage($"{CLASS_NAME}.SetStackingIcon: Sprite name is null or empty.");
                    return;
                }

                stackingIcon.sprite = SpriteManager.GetSprite(spriteName);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetStackingIcon), e);
            }
        }

        /// <summary>
        /// Shows or hides the stacking icon.
        /// </summary>
        public void ShowStackingIcon(bool show)
        {
            if (stackingIcon != null)
            {
                stackingIcon.enabled = show;
            }
        }

        /// <summary>
        /// Sets the opacity of the unit icon, nationality icon, and hit points text.
        /// Used for stacking to show non-dominant units at reduced opacity.
        /// </summary>
        public void SetOpacity(float opacity)
        {
            opacity = Mathf.Clamp01(opacity);

            if (unitIcon != null)
            {
                Color color = unitIcon.color;
                color.a = opacity;
                unitIcon.color = color;
            }

            if (nationIcon != null)
            {
                Color color = nationIcon.color;
                color.a = opacity;
                nationIcon.color = color;
            }

            if (boxIcon != null)
            {
                Color color = boxIcon.color;
                color.a = opacity;
                boxIcon.color = color;
            }

            if (boxText != null)
            {
                Color color = boxText.color;
                color.a = opacity;
                boxText.color = color;
            }

            if (deployIcon != null)
            {
                Color color = deployIcon.color;
                color.a = opacity;
                deployIcon.color = color;
            }
        }

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Initializes the TextMeshPro component with the assigned font and color.
        /// </summary>
        private void InitializeHitPointsText()
        {
            if (boxText == null) return;

            if (fontAsset != null)
            {
                boxText.font = fontAsset;
            }

            boxText.color = ratioTextColor;
        }

        /// <summary>
        /// Subscribes to EventManager events for hit points and deployment changes.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnUnitHitPointsChanged += OnUnitHitPointsChanged;
                EventManager.Instance.OnUnitDeploymentChanged += OnUnitDeploymentChanged;
            }
        }

        /// <summary>
        /// Unsubscribes from EventManager events.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnUnitHitPointsChanged -= OnUnitHitPointsChanged;
                EventManager.Instance.OnUnitDeploymentChanged -= OnUnitDeploymentChanged;
            }
        }

        /// <summary>
        /// Validates that all required references are set.
        /// </summary>
        private void ValidateReferences()
        {
            if (unitIcon == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateReferences: unitIcon is null");

            if (nationIcon == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: nationIcon is not assigned");

            if (boxIcon == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: boxIcon is not assigned");

            if (boxText == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: boxText is not assigned");

            if (deployIcon == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: deployIcon is not assigned");

            if (stackingIcon == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: stackingIcon is not assigned");

            if (fontAsset == null)
                Debug.LogWarning($"{CLASS_NAME}.ValidateReferences: fontAsset is not assigned");
        }

        #endregion // Private Methods

        #region Event Handlers

        /// <summary>
        /// Handles hit point changes for this unit. Updates the hit points text display.
        /// </summary>
        private void OnUnitHitPointsChanged(string unitId, int currentPercent)
        {
            if (unitId != _unitId) return;

            HitPointsRatio = currentPercent.ToString();
        }

        /// <summary>
        /// Handles deployment state changes for this unit. Updates the deploy icon sprite.
        /// </summary>
        private void OnUnitDeploymentChanged(string unitId, DeploymentPosition newPosition, EmbarkmentState embarkmentState)
        {
            if (unitId != _unitId) return;

            if (_resolveDeploySprite == null)
            {
                return;
            }

            string spriteName = _resolveDeploySprite(newPosition, embarkmentState);
            SetDeployIcon(spriteName);
        }

        #endregion // Event Handlers
    }
}
