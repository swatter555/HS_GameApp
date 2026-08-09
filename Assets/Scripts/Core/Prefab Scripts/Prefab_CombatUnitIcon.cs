using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Renderers;
using HammerAndSickle.Services;
using System;
using TMPro;
using UnityEngine;

// This file sits in namespace HammerAndSickle.Core, where the bare name "GameData" binds to the CHILD
// NAMESPACE HammerAndSickle.Core.GameData rather than to the constants class of the same name inside it.
// The alias disambiguates. (Files in HammerAndSickle.Models etc. can write GameData.FOO directly, which is
// why this bites only here.)
using GameDataConst = HammerAndSickle.Core.GameData.GameData;

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

        [Header("Motion Animation")]
        [Tooltip("Frame rate for the 6-frame motion flipbook (helo rotors) while the icon is moving. 40 play-tested best (Bob 2026-07-22).")]
        [SerializeField] private float animationFps = 40f;

        #endregion // Inspector Fields

        #region Fields

        /// <summary>
        /// The unit ID this prefab instance represents. Set during initialization.
        /// </summary>
        private string _unitId;

        /// <summary>
        /// Callback provided by the renderer to resolve a deployment position (and the naval transient
        /// state, todo_profiles §4.5) into a sprite name.
        /// </summary>
        private Func<DeploymentPosition, bool, string> _resolveDeploySprite;

        // Motion flipbook (RegimentIconType.Helo_Animation): atlas ships 6 frames per helo named
        // "<unit>_Frame0".."_Frame5". While the icon is tween-moving the frames loop at animationFps;
        // at rest the icon sits on Frame0 (motion-only by design, Bob 2026-07-22).
        private const int MOTION_FRAME_COUNT = 6;
        private const string MOTION_FRAME0_SUFFIX = "_Frame0";

        private string _unitSpriteName;    // last name passed to SetUnitIcon (atlas clones mangle sprite.name)
        private Sprite[] _motionFrames;    // resolved frames while the loop runs; null otherwise
        private int _motionIndex;
        private float _motionTimer;
        private bool _motionActive;

        // Intel gating (§12.2 / §24.3.2). The icon is an intel channel, so the hit-point box and the
        // deployment icon are gated by the viewer's rung on this unit. Friendly units are always Exact +
        // known-posture. The last true HP percent is cached so a mode change re-renders without needing a
        // fresh event, and so an HP change mid-fog does not leak through the band.
        private HitPointDisplayMode _hpMode = HitPointDisplayMode.Exact;
        private bool _deploymentKnown = true;
        private int _hpPercent;
        private DeploymentPosition _deploymentPosition;
        private bool _isNavalEmbarked;

        private const string HP_HIDDEN_TEXT = "-";
        private const string HP_BAND_FULL = "FUL";
        private const string HP_BAND_DEPLETED = "DEP";
        private const string HP_BAND_LOW = "LOW";

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

        private void Update()
        {
            // Motion flipbook: advance frames while active. Multi-step advance keeps the cycle
            // smooth through frame hitches.
            if (!_motionActive || _motionFrames == null || unitIcon == null) return;

            float frameTime = 1f / Mathf.Max(1f, animationFps);
            _motionTimer += Time.deltaTime;
            if (_motionTimer < frameTime) return;

            int steps = (int)(_motionTimer / frameTime);
            _motionTimer -= steps * frameTime;
            _motionIndex = (_motionIndex + steps) % _motionFrames.Length;
            unitIcon.sprite = _motionFrames[_motionIndex];
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
        /// <param name="isNavalEmbarked">The naval transient state (relevant when position is Embarked)</param>
        /// <param name="resolveDeploySprite">Callback to resolve a DeploymentPosition + naval flag to a sprite name</param>
        public void Initialize(string unitId, int hitPointPercent, DeploymentPosition deploymentPosition,
            bool isNavalEmbarked, Func<DeploymentPosition, bool, string> resolveDeploySprite = null)
        {
            _unitId = unitId;
            _resolveDeploySprite = resolveDeploySprite;
            _hpPercent = hitPointPercent;
            _deploymentPosition = deploymentPosition;
            _isNavalEmbarked = isNavalEmbarked;

            InitializeHitPointsText();
            RefreshHitPointText();
            RefreshDeployIcon();

            SubscribeToEvents();
        }

        /// <summary>
        /// Applies the viewer's intel gating to this icon (§12.2 / §24.3.2): how the hit-point box reads, and
        /// whether the real deployment posture is shown or replaced by the unknown marker. Called by
        /// GameIconRenderer at spawn and again whenever the unit's SpottedLevel changes. Friendly units are
        /// always (Exact, known).
        /// </summary>
        public void SetIntelDisplay(HitPointDisplayMode hpMode, bool deploymentKnown)
        {
            try
            {
                if (_hpMode == hpMode && _deploymentKnown == deploymentKnown) return;

                _hpMode = hpMode;
                _deploymentKnown = deploymentKnown;

                RefreshHitPointText();
                RefreshDeployIcon();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetIntelDisplay), e);
            }
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

                // A DIFFERENT sprite invalidates a running motion loop's cached frames (the next
                // AnimateIconStep restarts it from the new name). Re-setting the SAME name mid-loop
                // (facing refresh during a move) leaves the loop running — its next tick overrides
                // the Frame0 assigned below.
                if (_motionActive && spriteName != _unitSpriteName)
                    StopMotionAnimation();

                _unitSpriteName = spriteName;
                unitIcon.sprite = SpriteManager.GetSprite(spriteName);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetUnitIcon), e);
            }
        }

        /// <summary>
        /// Starts the motion flipbook (helo rotor loop). No-op unless the current unit sprite is a
        /// "&lt;name&gt;_Frame0" flipbook set — callers never need to know the unit type. Idempotent while
        /// running. GameIconRenderer.AnimateIconStep starts it; SnapIcon stops it (motion-only).
        /// </summary>
        public void StartMotionAnimation()
        {
            try
            {
                if (_motionActive) return;
                if (unitIcon == null || string.IsNullOrEmpty(_unitSpriteName)) return;
                if (!_unitSpriteName.EndsWith(MOTION_FRAME0_SUFFIX, StringComparison.Ordinal)) return;

                // "<unit>_Frame0" -> "<unit>_Frame" + i. Resolve all frames once; tolerate atlas gaps
                // by looping only what resolves.
                string baseName = _unitSpriteName[..^1];
                var resolved = new Sprite[MOTION_FRAME_COUNT];
                int count = 0;
                for (int i = 0; i < MOTION_FRAME_COUNT; i++)
                {
                    var frame = SpriteManager.GetSprite(baseName + i);
                    if (frame != null) resolved[count++] = frame;
                }
                if (count < 2) return;   // nothing to loop

                _motionFrames = new Sprite[count];
                Array.Copy(resolved, _motionFrames, count);
                _motionIndex = 0;
                _motionTimer = 0f;
                _motionActive = true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(StartMotionAnimation), e);
            }
        }

        /// <summary>Stops the motion flipbook and rests the icon on Frame0. Safe to call when not running.</summary>
        public void StopMotionAnimation()
        {
            try
            {
                if (!_motionActive) return;
                _motionActive = false;
                if (unitIcon != null && _motionFrames != null && _motionFrames.Length > 0)
                    unitIcon.sprite = _motionFrames[0];
                _motionFrames = null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(StopMotionAnimation), e);
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

        // Render order of the elements WITHIN this prefab (offset added to the slot's base order by
        // SortingConfig). Lives here in the script — the single place the internal stack is defined.
        private const int UnitIconSubOrder = 0;
        private const int NationIconSubOrder = 1;
        private const int DeployIconSubOrder = 2;
        private const int BoxIconSubOrder = 3;
        private const int BoxTextSubOrder = 4;
        private const int StackingIconSubOrder = 5;

        /// <summary>
        /// Stamps every child renderer onto <paramref name="slot"/> (GroundUnit or AirUnit) via
        /// <see cref="SortingConfig"/>, overriding baked prefab sorting. Sub-orders above define the internal
        /// unit-icon stack. Called from GameIconRenderer.CreateUnitIcon after instantiation.
        /// </summary>
        public void ApplySorting(SortSlot slot)
        {
            try
            {
                SortingConfig.Apply(unitIcon, slot, UnitIconSubOrder);
                SortingConfig.Apply(nationIcon, slot, NationIconSubOrder);
                SortingConfig.Apply(deployIcon, slot, DeployIconSubOrder);
                SortingConfig.Apply(boxIcon, slot, BoxIconSubOrder);
                SortingConfig.Apply(boxText != null ? boxText.GetComponent<Renderer>() : null, slot, BoxTextSubOrder);
                SortingConfig.Apply(stackingIcon, slot, StackingIconSubOrder);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ApplySorting), e);
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
        /// Subscribes to EventManager events for hit-point changes.
        /// (The OnUnitDeploymentChanged subscription was DELETED 2026-08-08 with the event itself,
        /// D10 ratified — the event was never raised; posture changes refresh via the coarse
        /// RaiseRedrawMapIcons redraw, which rebuilds every icon from live unit state.)
        /// </summary>
        private void SubscribeToEvents()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnUnitHitPointsChanged += OnUnitHitPointsChanged;
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

        /// <summary>
        /// Renders the hit-point box from the cached true percentage through the current gate (§12.2.5/.6):
        /// a dash below Level4, a strength band at Level4, the exact percentage at Level5 and for friendlies.
        /// </summary>
        private void RefreshHitPointText()
        {
            HitPointsRatio = _hpMode switch
            {
                HitPointDisplayMode.Hidden => HP_HIDDEN_TEXT,
                HitPointDisplayMode.Band => StrengthBandText(_hpPercent),
                _ => _hpPercent.ToString(),
            };
        }

        /// <summary>
        /// The Section 8 strength tiers as a short box label — Full (>=80%), Depleted (>=50%), Low below that.
        /// Reads the SAME floors the combat engine uses, so the band the player sees is the band that is
        /// actually being applied to the unit's damage output.
        /// </summary>
        private static string StrengthBandText(int hpPercent)
        {
            float ratio = hpPercent / 100f;

            if (ratio >= GameDataConst.FULL_STRENGTH_FLOOR) return HP_BAND_FULL;
            if (ratio >= GameDataConst.DEPLETED_STRENGTH_FLOOR) return HP_BAND_DEPLETED;
            return HP_BAND_LOW;
        }

        /// <summary>
        /// Renders the deployment icon from the cached true posture through the current gate: the real posture
        /// sprite once Level3 is reached, the unknown marker below it (§12.2.2 / §24.3.2.2).
        /// </summary>
        private void RefreshDeployIcon()
        {
            if (_resolveDeploySprite == null) return;

            string spriteName = _deploymentKnown
                ? _resolveDeploySprite(_deploymentPosition, _isNavalEmbarked)
                : SpriteManager.UnknownDeploymentIcon;

            SetDeployIcon(spriteName);
        }

        #endregion // Private Methods

        #region Event Handlers

        /// <summary>
        /// Handles hit point changes for this unit. Caches the true value and re-renders through the
        /// current intel gate — a fogged enemy's box must not start showing numbers just because it took damage.
        /// </summary>
        private void OnUnitHitPointsChanged(string unitId, int currentPercent)
        {
            if (unitId != _unitId) return;

            _hpPercent = currentPercent;
            RefreshHitPointText();
        }

        // (OnUnitDeploymentChanged handler DELETED 2026-08-08 — D10. The event had no raiser; the
        // coarse RaiseRedrawMapIcons redraw is the ratified refresh mechanism for posture changes.)

        #endregion // Event Handlers
    }
}
