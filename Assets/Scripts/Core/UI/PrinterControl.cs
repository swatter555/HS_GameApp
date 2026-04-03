using HammerAndSickle.Controllers;
using HammerAndSickle.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// Custom list control that simulates an HQ dot-matrix printer with alternating
    /// greenbar paper rows, top-down paper feed animation, and a fixed message buffer.
    /// </summary>
    public class PrinterControl : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(PrinterControl);

        #region Serialized Fields

        [Header("Scroll View")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _scrollContent;

        [Header("Row Appearance")]
        [SerializeField] private Sprite _evenRowSprite;
        [SerializeField] private Sprite _oddRowSprite;
        [SerializeField] private TMP_FontAsset _printerFont;
        [SerializeField] private float _fontSize = 14f;
        [SerializeField] private Color _textColor = new(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] private float _rowHeight = 20f;
        [SerializeField] private Vector4 _textPadding = new(6f, 0f, 4f, 0f);

        [Header("Audio")]
        [SerializeField] [Range(0f, 1f)] private float _printerVolume = 0.3f;

        [Header("Settings")]
        [SerializeField] private int _maxMessages = 25;
        [SerializeField] private float _printDelay = 0.04f;

        #endregion // Serialized Fields

        #region Fields

        private readonly Queue<PrinterMessage> _messageQueue = new();
        private readonly List<GameObject> _rowObjects = new();
        private int _totalRowCount;
        private bool _isPrinting;

        #endregion // Fields

        #region Unity Lifecycle

        private void OnEnable()
        {
            try
            {
                if (EventManager.Instance != null)
                    EventManager.Instance.OnPrinterMessage += EnqueueMessage;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnEnable), e);
            }
        }

        private void OnDisable()
        {
            try
            {
                if (EventManager.Instance != null)
                    EventManager.Instance.OnPrinterMessage -= EnqueueMessage;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnDisable), e);
            }
        }

        #endregion // Unity Lifecycle

        #region Public Methods

        /// <summary>
        /// Validates that all required components are assigned.
        /// </summary>
        public void Initialize()
        {
            try
            {
                if (_scrollRect == null)
                    throw new InvalidOperationException("ScrollRect is not assigned.");
                if (_scrollContent == null)
                    throw new InvalidOperationException("ScrollContent is not assigned.");
                if (_evenRowSprite == null || _oddRowSprite == null)
                    throw new InvalidOperationException("Row sprites are not assigned.");
                if (_printerFont == null)
                    throw new InvalidOperationException("Printer font is not assigned.");

                if (!_scrollContent.TryGetComponent<VerticalLayoutGroup>(out _))
                    AppService.CaptureUiMessage("Warning: PrinterControl scrollContent missing VerticalLayoutGroup.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Initialize), e);
            }
        }

        /// <summary>
        /// Adds a message to the print queue and starts processing if idle.
        /// </summary>
        public void EnqueueMessage(PrinterMessage message)
        {
            try
            {
                if (message == null) return;

                _messageQueue.Enqueue(message);

                if (!_isPrinting)
                    StartCoroutine(ProcessQueue());
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(EnqueueMessage), e);
            }
        }

        /// <summary>
        /// Destroys all rows and resets the printer.
        /// </summary>
        public void ClearAll()
        {
            try
            {
                foreach (GameObject row in _rowObjects)
                {
                    if (row != null)
                        Destroy(row);
                }

                _rowObjects.Clear();
                _totalRowCount = 0;
                _messageQueue.Clear();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ClearAll), e);
            }
        }

        #endregion // Public Methods

        #region Private Methods

        private IEnumerator ProcessQueue()
        {
            _isPrinting = true;

            while (_messageQueue.Count > 0)
            {
                PrinterMessage message = _messageQueue.Dequeue();
                yield return StartCoroutine(PrintMessage(message));
            }

            _isPrinting = false;
        }

        private IEnumerator PrintMessage(PrinterMessage message)
        {
            // Play printer sound
            try
            {
                GameAudioManager.EnsureExists();
                GameAudioManager.Instance.PlaySFXWithVariation(
                    GameAudioManager.SoundEffect.PrinterTick, _printerVolume, 0f);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(PrintMessage), e);
            }

            // Build all rows for this message (inserted in reverse order at index 0 so they read top-to-bottom)
            var rowsToInsert = new List<string>();

            // Content lines
            foreach (string line in message.Lines)
            {
                rowsToInsert.Add(line);
            }

            // Insert rows one at a time with a brief delay for the print effect
            // We insert at the top, but in order, so the first row of the message ends up on top
            int insertIndex = 0;
            foreach (string text in rowsToInsert)
            {
                bool isHeader = (insertIndex == 0);
                GameObject row = CreateRow(text, isHeader);
                row.transform.SetSiblingIndex(insertIndex);
                insertIndex++;

                yield return new WaitForSeconds(_printDelay);
            }

            // Add a blank separator row
            GameObject separator = CreateRow(string.Empty, false);
            separator.transform.SetSiblingIndex(insertIndex);

            // Force layout rebuild and scroll to top
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContent);
            yield return null;
            _scrollRect.verticalNormalizedPosition = 1f;

            // Trim buffer
            TrimBuffer();
        }

        private GameObject CreateRow(string text, bool isHeader)
        {
            GameObject rowObj = new($"PrintRow_{_totalRowCount}");
            rowObj.transform.SetParent(_scrollContent, false);

            // Background image with alternating colors
            Image bgImage = rowObj.AddComponent<Image>();
            bgImage.sprite = (_totalRowCount % 2 == 0) ? _evenRowSprite : _oddRowSprite;
            bgImage.type = Image.Type.Sliced;

            // Layout sizing
            LayoutElement layoutElement = rowObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = _rowHeight;
            layoutElement.preferredHeight = _rowHeight;
            layoutElement.flexibleWidth = 1;

            // Create text object
            GameObject textObj = new("Text");
            textObj.transform.SetParent(rowObj.transform, false);

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.font = _printerFont;
            textComponent.fontSize = _fontSize;
            textComponent.color = _textColor;
            textComponent.text = text;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            textComponent.overflowMode = TextOverflowModes.Ellipsis;
            textComponent.margin = _textPadding;

            if (isHeader)
            {
                textComponent.fontStyle = FontStyles.Bold;
            }

            // Stretch text to fill row
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _rowObjects.Add(rowObj);
            _totalRowCount++;

            return rowObj;
        }

        private void TrimBuffer()
        {
            try
            {
                // Estimate max rows: messages average ~5 rows each (header + lines + separator)
                int maxRows = _maxMessages * 6;

                while (_rowObjects.Count > maxRows)
                {
                    // Remove oldest rows from the end of the list (bottom of the printer)
                    GameObject oldest = _rowObjects[_rowObjects.Count - 1];
                    _rowObjects.RemoveAt(_rowObjects.Count - 1);

                    if (oldest != null)
                        Destroy(oldest);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(TrimBuffer), e);
            }
        }

        #endregion // Private Methods
    }
}
