using HammerAndSickle.Controllers;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Services;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.SceneManagement
{
    /// <summary>
    /// Orders dialog for Scene 1 (Battle).
    /// Displays scenario briefing at battle start. The Begin button dismisses
    /// the overlay and returns focus to the HUD, enabling map input.
    /// </summary>
    public class OrdersDialog_Scene1 : UIPanel
    {
        private const string CLASS_NAME = nameof(OrdersDialog_Scene1);

        #region Serialized Fields

        [Header("Navigation")]
        [SerializeField] private UIPanel _defaultDialog;

        [Header("Controls")]
        [SerializeField] private Button _buttonBegin;
        [SerializeField] private TMP_Text _briefingText;

        #endregion // Serialized Fields

        #region Unity Lifecycle

        private void Start()
        {
            try
            {
                if (_defaultDialog == null || _buttonBegin == null || _briefingText == null)
                    throw new Exception($"{CLASS_NAME} controls invalid");

                LoadBriefing();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Start), e);
                AppService.UnityQuit_DataUnsafe();
            }
        }

        #endregion // Unity Lifecycle

        #region Button Callbacks

        /// <summary>
        /// Called by the Begin button. Dismisses the orders overlay and
        /// returns focus to the HUD, which enables map input.
        /// </summary>
        public void OnBeginButton()
        {
            // See EventManager: request dialog switch back to HUD (closes orders overlay, enables map input)
            EventManager.Instance.RaiseScene1DialogRequested(_defaultDialog);
        }

        #endregion // Button Callbacks

        #region Private Methods

        private void LoadBriefing()
        {
            try
            {
                var manifest = GameDataManager.CurrentManifest;
                if (manifest == null)
                {
                    _briefingText.text = "No scenario manifest loaded.";
                    return;
                }

                string briefingPath = manifest.GetBriefingFilePath();

                if (string.IsNullOrEmpty(briefingPath) || !File.Exists(briefingPath))
                {
                    _briefingText.text = "Briefing file not found.";
                    AppService.CaptureUiMessage($"Briefing file missing for scenario: {manifest.DisplayName}");
                    return;
                }

                _briefingText.text = File.ReadAllText(briefingPath);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadBriefing), e);
                _briefingText.text = "Error loading briefing.";
            }
        }

        #endregion // Private Methods
    }
}
