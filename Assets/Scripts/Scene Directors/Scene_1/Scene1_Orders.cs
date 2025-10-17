using HammerAndSickle.Controllers;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Services;
using System;
using UnityEngine;

namespace HammerAndSickle.SceneDirectors
{
    public class Scene1_Orders : MenuHandler
    {
        private const string CLASS_NAME = nameof(Scene1_Orders);

        #region Singleton

        public static Scene1_Orders Instance { get; private set; }

        #endregion // Singleton

        #region Fields

        // Dialog GameObject root
        [SerializeField] private GameObject _dialogRoot;

        // Dialog controls
        [SerializeField] private OrdersDialog _ordersDialog;

        #endregion // Fields

        #region Unity Lifecycle

        public override void Awake()
        {
            base.Awake();

            // Set the dialog ID.
            Initialize(GeneralConstants.KhostScene_OrdersDialog_ID, false);
        }

        #endregion // Unity Lifecycle

        #region Overrides

        public override void ToggleMenu()
        {
            // Set active state and interactivity based on visibility and input focus.
            if (IsVisible && IsInputFocus) _dialogRoot.SetActive(true);
            else _dialogRoot.SetActive(false);
        }

        public override void SetupSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion // Overrides

        #region Callbacks

        public void OnBeginButton()
        {
            // Return to main interface
            Scene1_Director.Instance.SetActiveMenuByID(GeneralConstants.KhostScene_CoreInterface_ID);
        }

        #endregion // Callbacks


    }
}