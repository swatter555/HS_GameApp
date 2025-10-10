using HammerAndSickle.Services;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// Dialog controller for loading standalone scenario files from the user's Documents folder.
    /// Displays scenario list, briefing text, and thumbnail preview.
    /// </summary>
    public class ScenarioDialog : GenericDialog
    {
        private const string CLASS_NAME = nameof(ScenarioDialog);

        #region Serialized Fields

        [Header("Scenario Display")]
        [SerializeField] private TMP_Text _briefingText;
        [SerializeField] private Sprite   _placeholderThumbnail;
        [SerializeField] private Image    _thumbnailImage;
        [SerializeField] private Button   _loadButton;
        [SerializeField] private Button   _randomButton;
        [SerializeField] private Button   _exitButton;

        #endregion // Serialized Fields

        #region Events

        /// <summary>
        /// Fired when the selected scenario changes.
        /// </summary>
        public event Action<int> SelectionChanged;

        #endregion // Events

        #region Properties

        public TMP_Text BriefingText
        {
            get => _briefingText;
            set => _briefingText = value;
        }

        public Sprite PlaceholderThumbnail
        {
            get => _placeholderThumbnail;
            set => _placeholderThumbnail = value;
        }

        public Image ThumbnailImage
        {
            get => _thumbnailImage;
            set => _thumbnailImage = value;
        }

        public Button LoadButton
        {
            get => _loadButton;
            set => _loadButton = value;
        }

        public Button RandomButton
        {
            get => _randomButton;
            set => _randomButton = value;
        }

        public Button ExitButton
        {
            get => _exitButton;
            set => _exitButton = value;
        }

        #endregion // Properties

        #region Unity Lifecycle

        protected void Start()
        {
            try
            {
                // Make sure there are controls here.
                if (_briefingText == null ||
                    _placeholderThumbnail == null ||
                    _thumbnailImage == null ||
                    _loadButton == null ||
                    _randomButton == null ||
                    _exitButton == null)
                {
                    throw new System.Exception($"{CLASS_NAME} controls invalid");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Start), e);
                AppService.UnityQuit_DataUnsafe(); // Consider adding a Datasafe quit option
            }
        }

        #endregion

        #region Selection Handling

        protected override void OnSelectionChanged(int newIndex)
        {
            // Invoke event if any subscribers exist
            SelectionChanged?.Invoke(newIndex);
        }

        #endregion // Selection Handling
    }
}