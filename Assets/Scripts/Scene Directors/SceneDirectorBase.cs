using HammerAndSickle.Controllers;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.SceneDirectors
{
    /// <summary>
    /// Base class for scene directors to manage scene-specific logic and state.
    /// </summary>
    public abstract class SceneDirectorBase : MonoBehaviour
    {
        #region Input State Management

        // Stores registered menus with their IDs
        private Dictionary<int, MenuHandler> menuDictionary = new Dictionary<int, MenuHandler>();

        // Currently active menu ID
        private int _activeMenuID = 0; // Zero is always the default menu.

        #endregion //Input State Management

        #region Abstract Methods

        protected abstract string GetClassName();
        protected abstract void OnSceneInitialize();
        protected abstract void OnSceneCleanup();

        #endregion // Abstract Methods

        #region Input Control Methods

        /// <summary>
        /// Registers a menu with the scene director.
        /// </summary>
        public void RegisterMenu(MenuHandler menu)
        {
            if (!menuDictionary.ContainsKey(menu.MenuID))
            {
                menuDictionary.Add(menu.MenuID, menu);
            }
        }

        #endregion // Input Control Methods

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            SetupSingleton();
        }

        protected virtual void Start()
        {
            ValidateGameSystems();
            OnSceneInitialize();
        }

        protected virtual void Update()
        {
            InputListener();
        }

        protected virtual void OnDestroy()
        {
            OnSceneCleanup();
        }

        #endregion // Unity Lifecycle

        #region Public Methods

        /// <summary>
        /// Activates the menu corresponding to the specified menu ID and deactivates the currently active menu.
        /// </summary>
        /// <remarks>If the specified menu is already active, the method returns immediately without
        /// making any changes. If the currently active menu or the specified menu cannot be found in the menu
        /// dictionary, an exception is logged.</remarks>
        /// <param name="menuID">The unique identifier of the menu to activate. Must correspond to a valid menu in the menu dictionary.</param>
        public void SetActiveMenuByID(int menuID)
        {
            try
            {
                // Check if the requested menu is already active
                if (_activeMenuID == menuID) return; // Already active

                // Get the active menu if it exists
                bool result = menuDictionary.TryGetValue(_activeMenuID, out MenuHandler menu);
                if (result)
                {
                    // Deactivate the currently active menu
                    menu.SetInactive();
                }
                else throw new Exception($"Active menu with ID {_activeMenuID} not found in the menu dictionary.");

                // Activate the new menu if it exists
                result = menuDictionary.TryGetValue(menuID, out menu);
                if (result)
                {
                    menu.SetActive();
                    _activeMenuID = menuID;
                }
                else throw new Exception($"Menu with ID {menuID} not found in the menu dictionary.");
            }
            catch (Exception e)
            {
                AppService.HandleException(GetClassName(), nameof(SetActiveMenuByID), e);
            }
        }

        #endregion // Public Methods

        #region Protected Methods

        protected abstract void SetupSingleton();
        protected abstract void InputListener();

        protected virtual void ValidateGameSystems()
        {
            try
            {
                if (GameDataManager.Instance == null)
                    throw new Exception("GameDataManager instance is null.");

                if (!GameDataManager.Instance.IsReady)
                    throw new Exception("GameDataManager instance is not ready.");
            }
            catch (Exception e)
            {
                AppService.HandleException(GetClassName(), nameof(ValidateGameSystems), e);
                AppService.UnityQuit_DataUnsafe();
            }
        }

        #endregion // Protected Methods
    }

    /// <summary>
    /// This class serves as a base for handling menu activation and deactivation.
    /// </summary>
    public abstract class MenuHandler
    {
        #region Fields

        private int _menuID;

        #endregion // Fields

        #region Properties

        public bool IsActive { get; private set; } = false;
        public int MenuID => _menuID;

        #endregion // Properties

        #region Abstract Methods

        public abstract void ActivateMenu();
        public abstract void DeactivateMenu();

        #endregion // Abstract Methods

        #region Public Methods

        /// <summary>
        /// Sets the menu ID. Must be called before registering with SceneDirector.
        /// </summary>
        public void SetMenuID(int menuID)
        {
            _menuID = menuID;
        }

        /// <summary>
        /// Set the menu state to active and calls the ActivateMenu method.
        /// </summary>
        public void SetActive()
        {
            IsActive = true;
            ActivateMenu();
        }

        /// <summary>
        /// Marks the object as inactive and performs any necessary deactivation actions.
        /// </summary>
        public void SetInactive()
        {
            IsActive = false;
            DeactivateMenu();
        }

        #endregion
    }
}