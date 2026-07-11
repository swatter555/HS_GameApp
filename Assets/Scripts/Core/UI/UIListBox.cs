using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.SceneManagement
{
    /// <summary>
    /// Simple text list box built on a ScrollView. Rows are plain TMP_Text objects
    /// created at runtime (no prefab) and styled from the serialized font/color/size
    /// fields. Selection is shown by a shared highlight Image — built at runtime from
    /// the serialized selection sprite — repositioned and resized over the selected row.
    /// </summary>
    public class UIListBox : MonoBehaviour
    {
        #region Serialized Fields

        [Header("ScrollView")]
        [SerializeField] private RectTransform _content;

        [Header("Row Style")]
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private float _fontSize = 24f;
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _selectedTextColor = Color.white;

        [Header("Selection")]
        [SerializeField] private Sprite _selectionHighlight;
        [Tooltip("Blink rate of the selection highlight, in on/off cycles per second. 0 = no blink (solid).")]
        [SerializeField] private float _highlightCyclesPerSecond = 3f;

        #endregion // Serialized Fields

        #region Fields

        private readonly List<Row> _rows = new();
        private int _selectedIndex = -1;

        // Runtime highlight Image, built from the _selectionHighlight sprite.
        private Image _highlightImage;
        private Coroutine _blinkRoutine;

        #endregion // Fields

        #region Nested Types

        // One runtime row: its transform (for highlight placement) and text (for styling).
        private sealed class Row
        {
            public RectTransform Rect;
            public TMP_Text Text;
        }

        #endregion // Nested Types

        #region Events

        /// <summary>
        /// Fired when the selected item changes. Carries the new index.
        /// </summary>
        public event Action<int> SelectionChanged;

        #endregion // Events

        #region Properties

        public int SelectedIndex => _selectedIndex;

        #endregion // Properties

        #region Public Methods

        /// <summary>
        /// Populates the list with the given entries. Clears any existing items.
        /// </summary>
        public void Populate(List<string> entries)
        {
            Clear();

            if (entries == null || entries.Count == 0)
                return;

            PrepareHighlight();

            for (int i = 0; i < entries.Count; i++)
                CreateRow(i, entries[i]);
        }

        /// <summary>
        /// Clears all rows from the list and resets selection.
        /// </summary>
        public void Clear()
        {
            foreach (Row row in _rows)
            {
                if (row?.Rect != null)
                {
                    // Deactivate before Destroy: Destroy is deferred to end of frame, but a
                    // LayoutGroup ignores inactive children, so a rebuild triggered during the
                    // following Populate won't lay out these doomed rows (which would otherwise
                    // shift the new rows — and the selection highlight — down a line).
                    row.Rect.gameObject.SetActive(false);
                    Destroy(row.Rect.gameObject);
                }
            }

            _rows.Clear();
            _selectedIndex = -1;

            StopBlink();

            if (_highlightImage != null)
            {
                _highlightImage.gameObject.SetActive(false);
                Destroy(_highlightImage.gameObject);
                _highlightImage = null;
            }
        }

        /// <summary>
        /// Selects a row by index. Updates the highlight and fires SelectionChanged.
        /// </summary>
        public void SelectIndex(int index)
        {
            if (index < 0 || index >= _rows.Count)
                return;

            if (index == _selectedIndex)
                return;

            // Restore the previously selected row's text color
            if (_selectedIndex >= 0 && _selectedIndex < _rows.Count)
                _rows[_selectedIndex].Text.color = _textColor;

            // Recolor and highlight the new selection
            _selectedIndex = index;
            _rows[_selectedIndex].Text.color = _selectedTextColor;

            MoveHighlightTo(_selectedIndex);

            SelectionChanged?.Invoke(_selectedIndex);
        }

        #endregion // Public Methods

        #region Private Methods

        // Builds one runtime row: styled TMP text + a click Button under the content transform.
        private void CreateRow(int index, string entry)
        {
            var go = new GameObject($"Row_{index}", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_content, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            if (_font != null)
                text.font = _font;
            text.fontSize = _fontSize;
            text.color = _textColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.text = entry;

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = text; // TMP text is a raycast target, so clicks register

            int capturedIndex = index;
            button.onClick.AddListener(() => SelectIndex(capturedIndex));

            _rows.Add(new Row { Rect = rect, Text = text });
        }

        // Builds the shared highlight Image from the sprite: a layout-ignoring child of
        // content, rendered behind the rows.
        private void PrepareHighlight()
        {
            if (_selectionHighlight == null)
                return;

            var go = new GameObject("SelectionHighlight", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_content, false);

            _highlightImage = go.AddComponent<Image>();
            _highlightImage.sprite = _selectionHighlight;
            _highlightImage.raycastTarget = false; // never intercept row clicks

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            rect.SetAsFirstSibling(); // render behind the row text
            go.SetActive(false);
        }

        // Repositions and resizes the highlight to sit behind the selected row.
        private void MoveHighlightTo(int index)
        {
            if (_highlightImage == null)
                return;

            // Rows are placed by the VerticalLayoutGroup; force the pass so rects are valid.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);

            RectTransform row = _rows[index].Rect;
            RectTransform highlight = _highlightImage.rectTransform;

            highlight.anchorMin = row.anchorMin;
            highlight.anchorMax = row.anchorMax;
            highlight.pivot = row.pivot;
            highlight.anchoredPosition = row.anchoredPosition;
            highlight.sizeDelta = row.sizeDelta;

            highlight.SetAsFirstSibling(); // stay behind the row text
            _highlightImage.gameObject.SetActive(true);
            _highlightImage.enabled = true;

            RestartBlink();
        }

        // (Re)starts the highlight blink. A cycle is one on/off pair, so the image toggles
        // twice per cycle. A rate of 0 (or less) leaves the highlight solid.
        private void RestartBlink()
        {
            StopBlink();

            if (_highlightImage != null && _highlightCyclesPerSecond > 0f)
                _blinkRoutine = StartCoroutine(BlinkHighlight());
        }

        private void StopBlink()
        {
            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }
        }

        private IEnumerator BlinkHighlight()
        {
            float halfPeriod = 0.5f / _highlightCyclesPerSecond;
            var wait = new WaitForSeconds(halfPeriod);

            while (_highlightImage != null)
            {
                _highlightImage.enabled = !_highlightImage.enabled;
                yield return wait;
            }
        }

        #endregion // Private Methods
    }
}
