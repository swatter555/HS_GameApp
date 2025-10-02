using HammerAndSickle.Services;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// Base class for dialog windows with title, scroll view list, and input field.
    /// Provides common functionality for populating scrollable lists with alternating
    /// background images and handling selection.
    /// </summary>
    public class GenericDialog : MonoBehaviour
    {
        private const string CLASS_NAME = "GenericDialog";

        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TMP_Text dialogTitle;
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private RectTransform scrollViewContent;
        [SerializeField] private TMP_InputField inputField;

        [Header("List Appearance")]
        [SerializeField] private Sprite evenLineImage;
        [SerializeField] private Sprite oddLineImage;
        [SerializeField] private Sprite selectedLineImage;

        [Header("Input Behavior")]
        [SerializeField] private bool allowInputEditing = false;

        #endregion // Serialized Fields

        #region Private Fields

        private readonly List<GameObject> _listItems = new();
        private int _selectedIndex = -1;
        private GameObject _currentSelectedRow = null;

        #endregion // Private Fields

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            try
            {
                ValidateComponents();
                ConfigureInputField();
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Awake", e);
            }
        }

        // TODO: Add dialog animation - start tiny at screen center, expand to full size in ~200ms

        #endregion // Unity Lifecycle

        #region Protected Methods

        /// <summary>
        /// Populates the scroll view with a list of string entries.
        /// Creates alternating background images with text overlays.
        /// </summary>
        protected void PopulateList(List<string> entries)
        {
            try
            {
                ClearList();

                if (entries == null || entries.Count == 0)
                {
                    AppService.CaptureUiMessage("No entries to display.");
                    return;
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    CreateListRow(entries[i], i);
                }
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, "PopulateList", e);
            }
        }

        /// <summary>
        /// Clears all items from the scroll view list.
        /// </summary>
        protected void ClearList()
        {
            try
            {
                foreach (GameObject item in _listItems)
                {
                    if (item != null)
                    {
                        Destroy(item);
                    }
                }

                _listItems.Clear();
                _selectedIndex = -1;
                _currentSelectedRow = null;

                if (inputField != null)
                {
                    inputField.text = string.Empty;
                }
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ClearList", e);
            }
        }

        /// <summary>
        /// Gets the currently selected entry text, or empty string if none selected.
        /// </summary>
        protected string GetSelectedEntry()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _listItems.Count)
            {
                TMP_Text textComponent = _listItems[_selectedIndex].GetComponentInChildren<TMP_Text>();
                return textComponent != null ? textComponent.text : string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the currently selected index, or -1 if none selected.
        /// </summary>
        protected int GetSelectedIndex()
        {
            return _selectedIndex;
        }

        #endregion // Protected Methods

        #region Private Methods

        private void ValidateComponents()
        {
            if (scrollViewContent == null)
            {
                throw new System.InvalidOperationException("scrollViewContent is not assigned.");
            }

            // Check for VerticalLayoutGroup on content
            if (!scrollViewContent.TryGetComponent<VerticalLayoutGroup>(out _))
            {
                AppService.CaptureUiMessage("Warning: scrollViewContent missing VerticalLayoutGroup component.");
            }

            if (evenLineImage == null || oddLineImage == null || selectedLineImage == null)
            {
                throw new System.InvalidOperationException("Line image sprites are not assigned.");
            }
        }

        private void ConfigureInputField()
        {
            if (inputField != null)
            {
                inputField.readOnly = !allowInputEditing;
                inputField.interactable = allowInputEditing;
            }
        }

        private void CreateListRow(string text, int index)
        {
            // Create row GameObject
            GameObject rowObj = new($"Row_{index}");
            rowObj.transform.SetParent(scrollViewContent, false);

            // Add and configure Image component
            Image bgImage = rowObj.AddComponent<Image>();
            bgImage.sprite = (index % 2 == 0) ? evenLineImage : oddLineImage;
            bgImage.type = Image.Type.Sliced;

            // Configure RectTransform for proper sizing
            RectTransform rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0.5f, 1);
            rowRect.sizeDelta = new Vector2(0, 30); // Height of 30, width matches parent

            // Create text child
            GameObject textObj = new ("Text");
            textObj.transform.SetParent(rowObj.transform, false);

            TMP_Text textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.margin = new Vector4(10, 0, 10, 0);

            // Configure text RectTransform to fill parent
            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            // Add button component for click handling
            Button button = rowObj.AddComponent<Button>();
            button.targetGraphic = bgImage;
            button.transition = Selectable.Transition.None; // We handle visuals manually

            int capturedIndex = index;
            button.onClick.AddListener(() => OnRowClicked(capturedIndex));

            _listItems.Add(rowObj);
        }

        private void OnRowClicked(int index)
        {
            try
            {
                if (index < 0 || index >= _listItems.Count)
                {
                    return;
                }

                // Restore previous selection to even/odd background
                if (_currentSelectedRow != null && _selectedIndex >= 0)
                {
                    if (_currentSelectedRow.TryGetComponent<Image>(out Image previousImage))
                    {
                        previousImage.sprite = (_selectedIndex % 2 == 0) ? evenLineImage : oddLineImage;
                    }
                }

                // Update new selection
                _selectedIndex = index;
                _currentSelectedRow = _listItems[index];

                if (_currentSelectedRow.TryGetComponent<Image>(out Image selectedImage))
                {
                    selectedImage.sprite = selectedLineImage;
                }

                // Update input field
                if (inputField != null)
                {
                    TMP_Text textComponent = _currentSelectedRow.GetComponentInChildren<TMP_Text>();
                    if (textComponent != null)
                    {
                        inputField.text = textComponent.text;
                    }
                }

                // Notify derived classes
                OnSelectionChanged(index);
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, "OnRowClicked", e);
            }
        }

        /// <summary>
        /// Virtual method called when selection changes. Override in derived classes.
        /// </summary>
        protected virtual void OnSelectionChanged(int newIndex)
        {
            // Override in derived classes to handle selection changes
        }

        #endregion // Private Methods
    }
}