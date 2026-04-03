using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.SceneManagement
{
    /// <summary>
    /// Attach to a list row template. Holds references to its own Image and Text
    /// so UIListBox can interact with the row without digging into children.
    /// </summary>
    public class UIListBoxItem : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _text;

        #endregion // Serialized Fields

        #region Fields

        private float _originalAlpha;

        #endregion // Fields

        #region Unity Lifecycle

        private void Awake()
        {
            if (_background != null)
                _originalAlpha = _background.color.a;
        }

        #endregion // Unity Lifecycle

        #region Public Methods

        /// <summary>
        /// Sets the display text for this row.
        /// </summary>
        public void SetText(string text)
        {
            _text.text = text;
        }

        /// <summary>
        /// Toggles the selection highlight on or off.
        /// Selected: restores the Image color as configured in the editor.
        /// Deselected: sets Image alpha to 0 (transparent).
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_background == null)
                return;

            Color color = _background.color;
            color.a = selected ? _originalAlpha : 0f;
            _background.color = color;
        }

        #endregion // Public Methods
    }
}
