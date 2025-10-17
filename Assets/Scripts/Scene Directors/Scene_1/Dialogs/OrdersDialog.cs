using HammerAndSickle.Core.UI;
using HammerAndSickle.Services;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.SceneDirectors
{
    public class OrdersDialog : GenericDialog
    {
        private const string CLASS_NAME = nameof(OrdersDialog);

        #region Serialized Fields

        [SerializeField] private Button _buttonBegin;

        #endregion

        #region Unity Lifecycle

        protected void Start()
        {
            try
            {
                // Make sure there are controls here.
                if (_buttonBegin == null)
                    throw new System.Exception($"{CLASS_NAME} controls invalid");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Start), e);
                AppService.UnityQuit_DataUnsafe(); // Consider adding a Datasafe quit option
            }
        }

        #endregion
    }
}