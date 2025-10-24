using System;
using HammerAndSickle.Services;
using UnityEngine;

namespace HammerAndSickle.SceneDirectors
{
    public class Scene1_Core : MenuHandler
    {
        #region Singleton Instance

        public static Scene1_Core Instance { get; private set; }

        #endregion // Singleton Instance

        #region Control Fields

        #endregion // Control Fields

        #region Overrides

        public override void ToggleMenu()
        {
            try
            {
                Debug.Log($"{GetType().Name}: Toggling menu. IsVisible={IsVisible}, IsInputFocus={IsInputFocus}");

                // Map scroll, hotkeys, and other input enabled only if visible and has input focus.
                InputService_BattleMap.Instance.SetInputEnabled(IsInputFocus);
            }
            catch (Exception e)
            {
                AppService.HandleException(GetType().Name, nameof(ToggleMenu), e);
            }
        }

        public override void Start()
        {

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


        #endregion // Callbacks
    }
}
