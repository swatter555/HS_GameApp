using HammerAndSickle.Controllers;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Services;
using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HammerAndSickle.SceneManagement
{
    /// <summary>
    /// Default (home) dialog for Scene 1 (Battle).
    /// Always visible — represents the battle HUD. When focused, map input is enabled.
    /// When an overlay opens, this dialog loses focus and map input is disabled.
    ///
    /// Also acts as the click-through controller: holds references to HUD panel
    /// RectTransforms and provides hit-testing so InputService_BattleMap can determine
    /// whether a click lands on a UI panel (and should be blocked from hex selection).
    ///
    /// Implements its own singleton pattern (rather than extending Singleton&lt;T&gt;)
    /// because it must extend UIPanel for the dialog flow system's Show/Hide/SetFocus.
    /// </summary>
    public class DefaultDialog_Scene1 : UIPanel
    {
        private const string CLASS_NAME = nameof(DefaultDialog_Scene1);

        #region Singleton

        // Manual singleton — can't use Singleton<T> base class because we need UIPanel
        // as the base for Show/Hide/SetFocus in the dialog flow system.
        public static DefaultDialog_Scene1 Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion // Singleton

        #region Serialized Fields

        [Header("Canvas Camera (required for Screen Space - Camera)")]
        [SerializeField] private Camera _uiCamera;

        [Header("Click-Through Panels")]
        [SerializeField] private RectTransform _topMenuBar;
        [SerializeField] private RectTransform _terrainPanel;

        // ONE unit panel since the consolidation (§4.3) — the separate ground/air panels and the leader
        // panel are gone. FormerlySerializedAs migrates a live _unitGroundPanel binding rather than
        // silently dropping it. Slots left null fail OPEN (see WarnOnUnassignedPanels).
        [FormerlySerializedAs("_unitGroundPanel")]
        [SerializeField] private RectTransform _unitPanel;
        [SerializeField] private RectTransform _printerPanel;

        [Header("Unit Cycling")]
        [SerializeField] private Button _nextUnitButton;
        [SerializeField] private Button _prevUnitButton;

        [Header("Debug")]
        [SerializeField] private bool _debug;

        #endregion // Serialized Fields

        #region Fields

        // Cached array of all HUD panels that should block map clicks.
        // Built once in Start() from the serialized references.
        private RectTransform[] _panels;

        #endregion // Fields

        #region Unity Lifecycle

        private void Start()
        {
            // Build the panel array once for efficient iteration during hit-testing.
            _panels = new[]
            {
                _topMenuBar,
                _terrainPanel,
                _unitPanel,
                _printerPanel
            };

            WarnOnUnassignedPanels();

            // Wire unit cycling buttons
            _nextUnitButton?.onClick.AddListener(() => EventManager.Instance?.RaiseNextUnitRequested());
            _prevUnitButton?.onClick.AddListener(() => EventManager.Instance?.RaisePreviousUnitRequested());
        }

        /// <summary>
        /// Warns for every unassigned click-through slot. <see cref="IsScreenPointOverUI"/> skips nulls, so a
        /// missing reference FAILS OPEN — the click reaches the map, and for right-click that is a move order
        /// issued under the panel the player thought they were clicking. Silent for four days once already.
        /// </summary>
        private void WarnOnUnassignedPanels()
        {
            (string name, RectTransform rect)[] slots =
            {
                (nameof(_topMenuBar), _topMenuBar),
                (nameof(_terrainPanel), _terrainPanel),
                (nameof(_unitPanel), _unitPanel),
                (nameof(_printerPanel), _printerPanel)
            };

            foreach ((string name, RectTransform rect) in slots)
            {
                if (rect == null)
                {
                    Debug.LogWarning($"{CLASS_NAME}: click-through slot '{name}' is UNASSIGNED — clicks over that " +
                        "panel will reach the map (right-click there = move order).");
                }
            }

            // Screen Space - Camera canvases need the camera for the rect test; without it every panel
            // mis-tests and the whole HUD leaks clicks, not just one slot.
            if (_uiCamera == null)
            {
                Debug.LogWarning($"{CLASS_NAME}: _uiCamera is UNASSIGNED — click-through hit-testing will be " +
                    "wrong for any Screen Space - Camera canvas.");
            }
        }

        #endregion // Unity Lifecycle

        #region UIPanel Overrides

        /// <summary>
        /// Enables or disables map input based on whether the HUD has focus.
        /// When an overlay dialog is open, focus is removed and all map
        /// scrolling, clicking, and zooming ceases.
        /// </summary>
        protected override void OnFocusChanged(bool hasFocus)
        {
            // Gate all map input through the InputService.
            // hasFocus == true:  overlay closed, map becomes interactive
            // hasFocus == false: overlay open, map input disabled and state reset
            InputService_BattleMap.Instance.SetInputEnabled(hasFocus);
        }

        #endregion // UIPanel Overrides

        #region Click-Through Detection

        /// <summary>
        /// Returns true if the screen position is inside any registered HUD panel.
        /// Called by InputService_BattleMap during mouse click processing to decide
        /// whether the click should be consumed by the UI or passed through to hex selection.
        /// </summary>
        public bool IsScreenPointOverUI(Vector2 screenPoint)
        {
            try
            {
                foreach (RectTransform panel in _panels)
                {
                    if (panel == null) continue;

                    bool isActive = panel.gameObject.activeInHierarchy;
                    bool containsPoint = isActive
                        && RectTransformUtility.RectangleContainsScreenPoint(panel, screenPoint, _uiCamera);

                    if (_debug && isActive)
                    {
                        Debug.Log($"{CLASS_NAME}: Panel={panel.name}, Active={isActive}, " +
                            $"Contains={containsPoint}, ScreenPoint={screenPoint}, " +
                            $"PanelRect={panel.rect}, PanelPos={panel.position}");
                    }

                    if (containsPoint)
                        return true;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsScreenPointOverUI), e);
            }

            return false;
        }

        #endregion // Click-Through Detection

        #region Printer Button Callbacks

        // ----------------------------------------------------------------------------
        // The six on-CRT navigation buttons of the HQ dispatch feed (§24.8.4.1).
        //
        // These are Inspector-wired: Bob assigns each Button's onClick to the matching
        // method here (the §3.6b nav-button half of the division of labour). They must
        // therefore NOT also be hooked with AddListener in Start() — a code listener on
        // top of the Inspector wiring would fire every press twice.
        //
        // Each callback does exactly one thing: raise its event. PrinterControl owns all
        // history/cursor/filter state and subscribes to these. See EventManager.
        // ----------------------------------------------------------------------------

        /// <summary>Jumps the dispatch feed to the oldest message in the current filter.</summary>
        public void OnPrinterOldestButton() => EventManager.Instance?.RaisePrinterOldestRequested();

        /// <summary>Steps the dispatch feed back one message.</summary>
        public void OnPrinterPreviousButton() => EventManager.Instance?.RaisePrinterPreviousRequested();

        /// <summary>Steps the dispatch feed forward one message.</summary>
        public void OnPrinterNextButton() => EventManager.Instance?.RaisePrinterNextRequested();

        /// <summary>Jumps the dispatch feed to the newest message.</summary>
        public void OnPrinterLatestButton() => EventManager.Instance?.RaisePrinterLatestRequested();

        /// <summary>Advances the feed filter: All → Combat → Intel → Supply → Personnel → All.</summary>
        public void OnPrinterFilterButton() => EventManager.Instance?.RaisePrinterFilterCycleRequested();

        /// <summary>Purges the dispatch history.</summary>
        public void OnPrinterClearButton() => EventManager.Instance?.RaisePrinterClearRequested();

        #endregion // Printer Button Callbacks
    }
}
