using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// The HQ dispatch feed (§24.8) — the centre CRT of the battle HUD.
    ///
    /// ONE MESSAGE IS DISPLAYED AT A TIME, revealed with a typewriter effect and a blinking cursor. The player
    /// walks the history with six on-CRT buttons (oldest / previous / next / latest / filter / clear), which are
    /// Inspector-wired to DefaultDialog_Scene1's callbacks and reach this control through EventManager.
    ///
    /// Replaces the pre-2026-07-25 scrolling greenbar list: the row pool, the alternating row sprites, the
    /// ScrollRect and the per-row print delay are all gone — the CRT graphic carries the look now.
    /// </summary>
    public class PrinterControl : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(PrinterControl);

        #region Constants

        // Shown when the feed is empty — after CLEAR, or before the first dispatch arrives.
        private const string EmptyPlaceholder = "— NO MESSAGES —";

        // Cached so cycling the filter does not allocate an array on every button press.
        private static readonly PrinterFilter[] FilterCycle =
            (PrinterFilter[])Enum.GetValues(typeof(PrinterFilter));

        #endregion // Constants

        #region Serialized Fields

        [Header("CRT")]
        [Tooltip("The toggled visual root. PrinterControl itself MUST live on an always-active object — see " +
                 "the note in Initialize().")]
        [SerializeField] private GameObject _panelRoot;

        [Tooltip("The single message text. Fixed font size, auto-sizing OFF, anchored top-left in a fixed rect.")]
        [SerializeField] private TextMeshProUGUI _messageText;

        [Tooltip("Font size for the message text. 0 = leave whatever is set on the TMP component alone. " +
                 "Size against the TALLEST message (the 9-line loss report) — turn on the debug seeds and use " +
                 "the CALIBRATION dispatch, which is deliberately 9 lines.")]
        [SerializeField] private float _fontSize;

        [Tooltip("Position readout — 'MSG 7 / 24'.")]
        [SerializeField] private TextMeshProUGUI _positionText;

        [Tooltip("Optional. Displays the active filter (ALL / COMBAT / INTEL / SUPPLY / PERSONNEL).")]
        [SerializeField] private TextMeshProUGUI _filterLabel;

        [Tooltip("Optional. Lit while the cursor is off the newest message — doubles as the unread indicator.")]
        [SerializeField] private GameObject _latestIndicator;

        [Header("Typewriter")]
        [Tooltip("Reveal speed in characters per second. Framerate-independent.")]
        [SerializeField] private float _charsPerSecond = 120f;

        [Tooltip("Cursor glyph. '_' is safe in any font; '█' looks better but only if it is in the TMP atlas.")]
        [SerializeField] private string _cursorGlyph = "_";

        [Tooltip("Seconds between cursor blinks once the message is fully revealed.")]
        [SerializeField] private float _cursorBlinkInterval = 0.5f;

        [Header("Audio")]
        [SerializeField] [Range(0f, 1f)] private float _printerVolume = 0.3f;

        [Header("Settings")]
        [Tooltip("History cap. Oldest dispatches are discarded past this.")]
        [SerializeField] private int _maxMessages = 100;

        [Header("Dispatch Volume")]
        [Tooltip("ON = file a dispatch for every event, including routine attacks the player ordered and " +
                 "watched. OFF = report by exception: defensive reports always file, but the player's own " +
                 "attacks file only when losses are Moderate or worse, the enemy's state changed, or the " +
                 "attack cannot continue. OFF is the design intent (§24.8.2); ON is here to compare in play.")]
        [SerializeField] private bool _verbose;

        [Header("Debug")]
        [Tooltip("Seeds representative dispatches at startup so the CRT can be exercised before the remaining " +
                 "emitters exist. Includes a 9-line CALIBRATION message for sizing the font. OFF for real play.")]
        [SerializeField] private bool _debugSeedMessages;

        #endregion // Serialized Fields

        #region Fields

        private readonly List<PrinterMessage> _history = new();   // everything received, oldest first
        private readonly List<PrinterMessage> _view = new();      // _history narrowed by _filter
        private int _cursor;                                      // index into _view
        private PrinterFilter _filter = PrinterFilter.All;

        // Typewriter state
        private float _revealed;          // characters revealed so far, fractional
        private bool _isRevealing;
        private float _blinkTimer;
        private bool _cursorVisible = true;

        // Render caches — avoid reassigning TMP text (and reallocating strings) every frame.
        private string _lastMessageRender;
        private string _lastPositionRender;
        private string _lastFilterRender;

        private Func<int> _turnProvider;
        private bool _initialized;

        #endregion // Fields

        #region Unity Lifecycle

        private void Start()
        {
            // Scene1_Controller also calls Initialize(); whichever runs first wins. Ordering between the two
            // Start() methods is not guaranteed, and a dispatch raised during BattleManager setup must not be
            // dropped because this control had not subscribed yet.
            Initialize();
        }

        private void OnDestroy()
        {
            try
            {
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.OnPrinterMessage -= EnqueueMessage;
                    EventManager.Instance.OnPrinterOldestRequested -= GoOldest;
                    EventManager.Instance.OnPrinterPreviousRequested -= GoPrevious;
                    EventManager.Instance.OnPrinterNextRequested -= GoNext;
                    EventManager.Instance.OnPrinterLatestRequested -= GoLatest;
                    EventManager.Instance.OnPrinterFilterCycleRequested -= CycleFilter;
                    EventManager.Instance.OnPrinterClearRequested -= ClearAll;
                }

                PrinterDispatch.Detach();

                if (_turnProvider != null && PrinterMessage.TurnProvider == _turnProvider)
                    PrinterMessage.TurnProvider = null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnDestroy), e);
            }
        }

        private void Update()
        {
            try
            {
                // The CRT is open from scene start and never closes; this only guards against the root being
                // switched off by hand in the Inspector.
                if (_panelRoot == null || !_panelRoot.activeSelf) return;

                if (_isRevealing)
                {
                    _revealed += Time.deltaTime * _charsPerSecond;

                    if (_revealed >= CurrentFullText.Length)
                        CompleteReveal();

                    RenderMessageText();
                }
                else
                {
                    // Blink the cursor at rest.
                    _blinkTimer += Time.deltaTime;
                    if (_blinkTimer >= _cursorBlinkInterval)
                    {
                        _blinkTimer = 0f;
                        _cursorVisible = !_cursorVisible;
                        RenderMessageText();
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Update), e);
            }
        }

        #endregion // Unity Lifecycle

        #region Public Methods

        /// <summary>
        /// Validates references, subscribes to the feed and navigation events, and installs the message header
        /// provider. Idempotent — safe to call from both Scene1_Controller and Start().
        /// </summary>
        public void Initialize()
        {
            try
            {
                if (_initialized) return;
                _initialized = true;

                if (_messageText == null)
                    throw new InvalidOperationException("Message text is not assigned.");
                if (_panelRoot == null)
                    throw new InvalidOperationException("Panel root is not assigned.");

                // ⚠ THE P3 DEADLOCK GUARD. This component must sit on an ALWAYS-ACTIVE object with _panelRoot
                // as the toggled child. If PrinterControl lived on the object being hidden, SetActive(false)
                // would silence the component that receives the message that would show it again.
                if (_panelRoot == gameObject)
                {
                    AppService.CaptureUiMessage(
                        "Warning: PrinterControl's panel root is its own GameObject — hiding it would stop the " +
                        "printer receiving messages. Move PrinterControl to an always-active parent.");
                }

                ConfigureTextComponent();

                // Dispatch volume — see PrinterDispatch for what the two settings actually gate.
                PrinterDispatch.Verbose = _verbose;

                // PrinterDispatch subscribes to the broadcast triggers (weather, spotting); this control owns
                // its lifetime, since it is the always-active battle-scene component of the printer domain.
                PrinterDispatch.Attach();

                // The turn needs a live singleton; supplying it from here keeps PrinterMessage free of them.
                _turnProvider = CurrentBattleTurn;
                PrinterMessage.TurnProvider = _turnProvider;

                // See EventManager — subscribe in Start(), never OnEnable (§3.6b cross-singleton rule).
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.OnPrinterMessage += EnqueueMessage;
                    EventManager.Instance.OnPrinterOldestRequested += GoOldest;
                    EventManager.Instance.OnPrinterPreviousRequested += GoPrevious;
                    EventManager.Instance.OnPrinterNextRequested += GoNext;
                    EventManager.Instance.OnPrinterLatestRequested += GoLatest;
                    EventManager.Instance.OnPrinterFilterCycleRequested += CycleFilter;
                    EventManager.Instance.OnPrinterClearRequested += ClearAll;
                }

                // Open from scene start (2026-07-27) showing the empty placeholder, in step with the terrain
                // and unit panels. Complete rather than reveal — the placeholder is a resting state, not a
                // dispatch, so there is nothing to type out.
                _panelRoot.SetActive(true);
                CompleteReveal();
                RenderChrome();
                RenderMessageText();

                if (_debugSeedMessages)
                    SeedDebugMessages();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Initialize), e);
            }
        }

        /// <summary>
        /// Receives a dispatch, shows the panel, and begins the typewriter reveal.
        /// </summary>
        public void EnqueueMessage(PrinterMessage message)
        {
            try
            {
                if (message == null) return;

                // Auto-follow only when the player is already reading the newest dispatch. A player walking the
                // history keeps their place, and the latest indicator lights — which is exactly what makes that
                // button double as the unread flag (§24.8.4.1).
                bool wasAtNewest = _view.Count == 0 || _cursor >= _view.Count - 1;
                PrinterMessage displayed = CurrentMessage;

                _history.Add(message);
                TrimHistory();
                RebuildView();

                if (wasAtNewest && _view.Count > 0)
                    _cursor = _view.Count - 1;

                ClampCursor();
                ShowPanel();
                PlayPrinterSound();

                if (!ReferenceEquals(displayed, CurrentMessage))
                    BeginReveal();

                RenderChrome();
                RenderMessageText();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(EnqueueMessage), e);
            }
        }

        /// <summary>Jumps to the oldest dispatch in the current filter.</summary>
        public void GoOldest() => Navigate(0);

        /// <summary>Steps back one dispatch.</summary>
        public void GoPrevious() => Navigate(_cursor - 1);

        /// <summary>Steps forward one dispatch.</summary>
        public void GoNext() => Navigate(_cursor + 1);

        /// <summary>Jumps to the newest dispatch in the current filter.</summary>
        public void GoLatest() => Navigate(_view.Count - 1);

        /// <summary>
        /// Advances the filter (All → Combat → Intel → Supply → Personnel → All), keeping the displayed
        /// dispatch if it survives the new filter.
        /// </summary>
        public void CycleFilter()
        {
            try
            {
                _filter = FilterCycle[((int)_filter + 1) % FilterCycle.Length];

                PrinterMessage displayed = CurrentMessage;
                RebuildView();

                int index = displayed != null ? _view.IndexOf(displayed) : -1;
                _cursor = index >= 0 ? index : _view.Count - 1;
                ClampCursor();

                if (!ReferenceEquals(displayed, CurrentMessage))
                    BeginReveal();

                RenderChrome();
                RenderMessageText();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CycleFilter), e);
            }
        }

        /// <summary>
        /// Purges the dispatch history. The panel stays up showing the empty placeholder — the player pressed a
        /// button and should see its result rather than have the CRT vanish.
        /// </summary>
        public void ClearAll()
        {
            try
            {
                _history.Clear();
                RebuildView();
                _cursor = 0;

                BeginReveal();
                RenderChrome();
                RenderMessageText();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ClearAll), e);
            }
        }

        #endregion // Public Methods

        #region Navigation Internals

        /// <summary>
        /// Moves the cursor, or — if the reveal is still running — completes it instead (§24.8.4.2).
        /// </summary>
        private void Navigate(int targetIndex)
        {
            try
            {
                if (_isRevealing)
                {
                    CompleteReveal();
                    RenderMessageText();
                    return;
                }

                if (_view.Count == 0) return;

                int clamped = Mathf.Clamp(targetIndex, 0, _view.Count - 1);
                if (clamped == _cursor) return;

                _cursor = clamped;
                BeginReveal();
                RenderChrome();
                RenderMessageText();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Navigate), e);
            }
        }

        private PrinterMessage CurrentMessage =>
            _view.Count > 0 && _cursor >= 0 && _cursor < _view.Count ? _view[_cursor] : null;

        private string CurrentFullText => CurrentMessage?.FullText ?? EmptyPlaceholder;

        private void RebuildView()
        {
            _view.Clear();

            foreach (PrinterMessage message in _history)
            {
                if (PassesFilter(message))
                    _view.Add(message);
            }
        }

        private bool PassesFilter(PrinterMessage message) => _filter switch
        {
            PrinterFilter.All => true,
            PrinterFilter.Combat => message.Category == PrinterCategory.Combat,
            PrinterFilter.Intel => message.Category == PrinterCategory.Intel,
            PrinterFilter.Supply => message.Category == PrinterCategory.Supply,
            PrinterFilter.Personnel => message.Category == PrinterCategory.Personnel,
            _ => true
        };

        private void ClampCursor() => _cursor = _view.Count == 0 ? 0 : Mathf.Clamp(_cursor, 0, _view.Count - 1);

        private void TrimHistory()
        {
            int excess = _history.Count - Mathf.Max(1, _maxMessages);
            if (excess > 0)
                _history.RemoveRange(0, excess);
        }

        #endregion // Navigation Internals

        #region Reveal

        private void BeginReveal()
        {
            _revealed = 0f;
            _isRevealing = true;
            _cursorVisible = true;
            _blinkTimer = 0f;
        }

        private void CompleteReveal()
        {
            _revealed = CurrentFullText.Length;
            _isRevealing = false;
            _cursorVisible = true;
            _blinkTimer = 0f;
        }

        #endregion // Reveal

        #region Rendering

        /// <summary>
        /// Renders the revealed substring plus the cursor. Called every frame while revealing or blinking, so it
        /// short-circuits when the composed string has not actually changed.
        /// </summary>
        private void RenderMessageText()
        {
            if (_messageText == null) return;

            string full = CurrentFullText;
            int shown = _isRevealing ? Mathf.Clamp((int)_revealed, 0, full.Length) : full.Length;

            // The cursor is solid while typing and blinks once the message is complete.
            string glyph = (_isRevealing || _cursorVisible) ? _cursorGlyph : " ";
            string composed = full.Substring(0, shown) + glyph;

            if (composed == _lastMessageRender) return;

            _lastMessageRender = composed;
            _messageText.text = composed;
        }

        /// <summary>
        /// Renders the position readout, filter label, and latest indicator. Only called on state changes.
        /// </summary>
        private void RenderChrome()
        {
            if (_positionText != null)
            {
                string position = _view.Count == 0 ? "MSG 0 / 0" : $"MSG {_cursor + 1} / {_view.Count}";
                if (position != _lastPositionRender)
                {
                    _lastPositionRender = position;
                    _positionText.text = position;
                }
            }

            if (_filterLabel != null)
            {
                string filter = _filter.ToString().ToUpperInvariant();
                if (filter != _lastFilterRender)
                {
                    _lastFilterRender = filter;
                    _filterLabel.text = filter;
                }
            }

            if (_latestIndicator != null)
            {
                bool unread = _view.Count > 0 && _cursor < _view.Count - 1;
                if (_latestIndicator.activeSelf != unread)
                    _latestIndicator.SetActive(unread);
            }
        }

        /// <summary>
        /// Enforces the two text settings the dispatch frame depends on. Auto-sizing is the important one: TMP
        /// fits per text object, so a 2-line dispatch would render huge next to the 9-line loss report, and the
        /// size would visibly change MID-TYPE as the revealed substring grows (§24.8.4).
        /// </summary>
        private void ConfigureTextComponent()
        {
            if (_messageText == null) return;

            if (_messageText.enableAutoSizing)
            {
                _messageText.enableAutoSizing = false;
                AppService.CaptureUiMessage(
                    "Warning: PrinterControl message text had auto-sizing enabled; forced off (§24.8.4). " +
                    "Set PrinterControl's Font Size instead — auto-sizing would resize the text mid-type.");
            }

            // 0 = leave the TMP component's own size alone, so this never silently fights an Inspector value
            // that was set deliberately.
            if (_fontSize > 0f)
                _messageText.fontSize = _fontSize;

            _messageText.alignment = TextAlignmentOptions.TopLeft;
            _messageText.textWrappingMode = TextWrappingModes.Normal;
            _messageText.overflowMode = TextOverflowModes.Overflow;
        }

        #endregion // Rendering

        #region Visibility

        /// <summary>
        /// Guarantees the CRT is up. It is opened once at scene start (2026-07-27) and never closed, so in
        /// practice this is a no-op — kept so a dispatch still surfaces if the root was switched off by hand.
        ///
        /// There is deliberately no counterpart that closes it. An earlier draft hid the panel on right-click
        /// deselect, inherited from the interim ReactivePanelManager behaviour where the message panel was one
        /// of several floating panels tracking the selected hex. The dispatch feed is not contextual — it is a
        /// running log unrelated to the selection — and dismissing it stranded the whole history behind nav
        /// buttons that were themselves inside the hidden root.
        ///
        /// It is also the one panel deselect does not CLEAR: the terrain and unit panels empty on right-click,
        /// but a dispatch log is not about the selected hex. CLEAR empties it, and does not close it.
        /// </summary>
        private void ShowPanel()
        {
            if (_panelRoot != null && !_panelRoot.activeSelf)
                _panelRoot.SetActive(true);
        }

        #endregion // Visibility

        #region Helpers

        /// <summary>
        /// The live battle turn for the dispatch frame (§24.8.5a). The campaign date left the frame on
        /// 2026-07-26 — long unit names were wrapping the header and costing a line — which also retired the
        /// day-level-date problem, since no date is rendered at all now.
        /// </summary>
        private int CurrentBattleTurn()
        {
            try
            {
                return BattleManager.Instance != null ? BattleManager.Instance.CurrentTurnNumber : 0;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CurrentBattleTurn), e);
                return 0;
            }
        }

        private void PlayPrinterSound()
        {
            try
            {
                GameAudioManager.EnsureExists();
                GameAudioManager.Instance.PlaySFXWithVariation(
                    GameAudioManager.SoundEffect.PrinterTick, _printerVolume, 0f);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(PlayPrinterSound), e);
            }
        }

        /// <summary>
        /// Representative dispatches for exercising the CRT before the P7 emitters exist: every category, and a
        /// length spread from one line to the loss-report height budget.
        /// </summary>
        private void SeedDebugMessages()
        {
            // The three combat shapes, in the real §24.8.6 wording PrinterDispatch produces.
            EnqueueMessage(new PrinterMessage(
                new[]
                {
                    "We are attacking enemy forces at 15,10.",
                    "Losses are moderate.",
                    "Enemy is holding."
                },
                "3rd Tank Regiment", PrinterCategory.Combat, 12));

            EnqueueMessage(new PrinterMessage(
                new[]
                {
                    "We are attacking strong enemy forces at 15,10.",
                    "Losses are very heavy.",
                    "Enemy is holding. We are halting the attack."
                },
                "3rd Tank Regiment", PrinterCategory.Combat, 12));

            EnqueueMessage(new PrinterMessage(
                new[]
                {
                    "We are under attack at 8,14.",
                    "Losses are heavy.",
                    "Position untenable. Withdrawing to the secondary line."
                },
                "3rd Motor Rifle Regiment", PrinterCategory.Combat, 13));

            EnqueueMessage(new PrinterMessage(
                new[]
                {
                    "We are bombarding enemy forces at 9,3.",
                    "We have taken no losses.",
                    "Enemy is retreating."
                },
                "1st Artillery Regiment", PrinterCategory.Combat, 13));

            EnqueueMessage(new PrinterMessage(
                new[] { "Overcast moving into the Khost valley. Air operations suspended. Visibility poor." },
                PrinterMessage.SourceWeatherSection, PrinterCategory.General, 14));

            // ⚠ FONT CALIBRATION — deliberately 9 lines, the height budget set by the P6 loss report (§24.8.4).
            // Size the CRT text so THIS message fits without clipping; everything shorter then leaves space
            // below rather than jumping in size.
            EnqueueMessage(new PrinterMessage(
                new[]
                {
                    "CUMULATIVE LOSSES — Khost",
                    "                OURS     ENEMY",
                    "Men            2,340     4,110",
                    "Tanks             18        31",
                    "AFVs              44        62",
                    "Guns               6        24",
                    "Aircraft           3         7",
                    "Helicopters        2         0"
                },
                PrinterMessage.SourceDivisionalHQ, PrinterCategory.General, 14));
        }

        #endregion // Helpers
    }
}
