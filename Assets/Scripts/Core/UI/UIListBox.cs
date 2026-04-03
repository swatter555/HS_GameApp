using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.SceneManagement
{
    /// <summary>
    /// Simple text list box built on a ScrollView.
    /// Populates by cloning a UIListBoxItem template, tracks selection via per-row highlight.
    /// </summary>
    public class UIListBox : MonoBehaviour
    {
        #region Serialized Fields

        [Header("ScrollView")]
        [SerializeField] private RectTransform _content;

        [Header("Row Template")]
        [SerializeField] private UIListBoxItem _itemTemplate;

        #endregion // Serialized Fields

        #region Fields

        private readonly List<UIListBoxItem> _items = new();
        private int _selectedIndex = -1;

        #endregion // Fields

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

            // Hide the template so it doesn't render in the list
            _itemTemplate.gameObject.SetActive(false);

            for (int i = 0; i < entries.Count; i++)
            {
                // Clone the template row into the scroll content
                UIListBoxItem item = Instantiate(_itemTemplate, _content);
                item.gameObject.SetActive(true);
                item.gameObject.name = $"Row_{i}";

                item.SetText(entries[i]);
                item.SetSelected(false);

                // Add click handling
                Button button = item.GetComponent<Button>();
                if (button == null)
                    button = item.gameObject.AddComponent<Button>();

                button.transition = Selectable.Transition.None;

                int capturedIndex = i;
                button.onClick.AddListener(() => SelectIndex(capturedIndex));

                _items.Add(item);
            }
        }

        /// <summary>
        /// Clears all items from the list and resets selection.
        /// </summary>
        public void Clear()
        {
            foreach (UIListBoxItem item in _items)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }

            _items.Clear();
            _selectedIndex = -1;
        }

        /// <summary>
        /// Selects an item by index. Updates the highlight and fires SelectionChanged.
        /// </summary>
        public void SelectIndex(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            if (index == _selectedIndex)
                return;

            // Clear the previous selection
            if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
                _items[_selectedIndex].SetSelected(false);

            // Highlight the new selection
            _selectedIndex = index;
            _items[_selectedIndex].SetSelected(true);

            SelectionChanged?.Invoke(_selectedIndex);
        }

        #endregion // Public Methods
    }
}
