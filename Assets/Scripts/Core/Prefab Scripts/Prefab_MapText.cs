using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;
using TMPro;
using UnityEngine;


namespace HammerAndSickle.Core
{
    /// <summary>
    /// Available text sizes for map labels.
    /// </summary>
    public enum TextSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// Available font weights for map labels.
    /// </summary>
    public enum FontWeight
    {
        Light,
        Medium,
        Bold
    }

    public class Prefab_MapText : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(Prefab_MapText);

        #region Constants

        // Default text sizes in pixels
        [Header("Text Sizes")]
        [SerializeField] private float smallTextSize = 12f;
        [SerializeField] private float mediumTextSize = 16f;
        [SerializeField] private float largeTextSize = 24f;

        #endregion // Constants

        #region Inspector Fields

        [Header("Required Components")]
        [SerializeField] private TextMeshPro textComponent;

        [Header("Font Assets")]
        [SerializeField] private TMP_FontAsset lightFont;
        [SerializeField] private TMP_FontAsset mediumFont;
        [SerializeField] private TMP_FontAsset boldFont;

        #endregion // Inspector Fields

        #region Unity Lifecycle

        /// <summary>
        /// Initializes the text label component.
        /// </summary>
        private void Awake()
        {
            ValidateReferences();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates that all required components are assigned in the inspector.
        /// </summary>
        private void ValidateReferences()
        {
            if (textComponent == null)
            {
                AppService.HandleException(CLASS_NAME, "ValidateReferences", new System.Exception("Text component is not assigned."));
            }
        }

        #endregion // Private Methods

        #region Public Methods

        /// <summary>
        /// Sets the text content of the label.
        /// </summary>
        /// <param name="text">Text to display. Must not be null.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when text is null.</exception>
        public void SetText(string text)
        {
            if (text == null)
            {
                throw new System.ArgumentNullException(nameof(text));
            }

            try
            {
                textComponent.text = text;
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetText", e);
            }
        }

        /// <summary>
        /// Sets the font size of the label.
        /// </summary>
        /// <param name="size">Size enum value to apply.</param>
        public void SetSize(TextSize size)
        {
            try
            {
                textComponent.fontSize = size switch
                {
                    TextSize.Small => smallTextSize,
                    TextSize.Medium => mediumTextSize,
                    TextSize.Large => largeTextSize,
                    _ => mediumTextSize
                };
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetSize", e);
            }
        }

        /// <summary>
        /// Sets the font weight of the label.
        /// </summary>
        /// <param name="weight">Font weight to apply.</param>
        public void SetFont(FontWeight weight)
        {
            try
            {
                textComponent.font = weight switch
                {
                    FontWeight.Light => lightFont,
                    FontWeight.Medium => mediumFont,
                    FontWeight.Bold => boldFont,
                    _ => mediumFont
                };
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetFont", e);
            }
        }

        /// <summary>
        /// Sets the text color using the standard color enum.
        /// </summary>
        /// <param name="color">Color enum value to apply</param>
        public void SetColor(TextColor color)
        {
            try
            {
                Color newColor = color switch
                {
                    TextColor.Black => Color.black,
                    TextColor.White => Color.white,
                    TextColor.Gold => new Color(1f, 0.84f, 0f, 1f),
                    TextColor.Red => Color.red,
                    TextColor.Blue => Color.blue,
                    TextColor.Grey => Color.gray,
                    TextColor.Yellow => Color.yellow,
                    TextColor.Green => Color.green,
                    TextColor.Teal => new Color(0f, 1f, 1f, 1f),
                    _ => Color.white
                };

                textComponent.color = newColor;
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetColor", e);
            }
        }

        /// <summary>
        /// Sets the text outline thickness.
        /// </summary>
        /// <param name="thickness"></param>
        public void SetOutlineThickness(float thickness)
        {
            try
            {
                textComponent.outlineWidth = thickness;
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetOutlineThickness", e);
            }
        }

        #endregion // Public Methods
    }
}