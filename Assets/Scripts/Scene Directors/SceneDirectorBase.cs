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

        // Scene core interface menu ID
        private int _coreInterfaceMenuID = GeneralConstants.DefaultID;

        // Currently active menu ID
        private int _activeMenuID = GeneralConstants.DefaultID;

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

            if (_coreInterfaceMenuID == GeneralConstants.DefaultID)
            {
                AppService.HandleException(GetClassName(), nameof(Start),
                    new Exception("Core interface not set after initialization"));
            }
        }

        protected virtual void OnDestroy()
        {
            OnSceneCleanup();
        }

        #endregion // Unity Lifecycle

        #region Public Methods

        /// <summary>
        /// Sets the core interface by assigning a menu ID and registering the associated menu handler.
        /// </summary>
        /// <param name="menuID">The unique identifier for the core interface menu.</param>
        /// <param name="menuHandler">The <see cref="MenuHandler"/> instance responsible for handling the core interface menu.</param>
        public void SetCoreInterface(int menuID, MenuHandler menuHandler)
        {
            // Set the core interface ID.
            _coreInterfaceMenuID = menuID;

            // Initialize the interface.
            menuHandler.Initialize(menuID, true);

            // Register the menu.
            RegisterMenu(menuHandler);

            // Set as the active menu.
            _activeMenuID = menuID;
        }

        /// <summary>
        /// Activates the menu corresponding to the specified menu ID and manages focus/visibility states.
        /// Core interface remains visible but loses focus when other menus activate.
        /// </summary>
        /// <param name="menuID">The unique identifier of the menu to activate.</param>
        public void SetActiveMenuByID(int menuID)
        {
            try
            {
                // Check if the requested menu is already active
                if (_activeMenuID == menuID) return;

                // Validate the new menu exists
                if (!menuDictionary.TryGetValue(menuID, out MenuHandler newMenu))
                {
                    throw new Exception($"Menu with ID {menuID} not found in the menu dictionary.");
                }

                // Handle the currently active menu
                if (_activeMenuID != GeneralConstants.DefaultID)
                {
                    if (menuDictionary.TryGetValue(_activeMenuID, out MenuHandler currentMenu))
                    {
                        currentMenu.Hide();
                    }
                    // If current menu not found, log but continue (non-fatal)
                    else
                    {
                        AppService.HandleException(GetClassName(), nameof(SetActiveMenuByID),
                            new Exception($"Current active menu with ID {_activeMenuID} not found. Continuing with activation."));
                    }
                }

                // Remove focus from all menus first (ensures single focus)
                foreach (var menu in menuDictionary.Values)
                {
                    menu.IsInputFocus = false;
                }

                // Activate the new menu
                newMenu.Show();
                _activeMenuID = menuID;

                // If activating core interface, ensure no other menus are visible
                if (menuID == _coreInterfaceMenuID)
                {
                    foreach (var kvp in menuDictionary)
                    {
                        if (kvp.Key != _coreInterfaceMenuID && kvp.Value.IsVisible)
                        {
                            kvp.Value.Hide();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(GetClassName(), nameof(SetActiveMenuByID), e);

                // Fallback: try to activate core interface if available
                if (_coreInterfaceMenuID != GeneralConstants.DefaultID &&
                    _coreInterfaceMenuID != menuID &&
                    menuDictionary.ContainsKey(_coreInterfaceMenuID))
                {
                    _activeMenuID = _coreInterfaceMenuID;
                    menuDictionary[_coreInterfaceMenuID].IsInputFocus = true;
                }
            }
        }

        #endregion // Public Methods

        #region Protected Methods

        protected abstract void SetupSingleton();

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

        private int _menuID           = GeneralConstants.DefaultID;
        private bool _isCoreInterface = false;
        private bool _isVisible       = false;
        private bool _isInputFocus    = false;
        private bool _isInitialized   = false;

        #endregion // Fields

        #region Properties

        public int MenuID { get => _menuID; private set => _menuID = value; }

        public bool IsCoreInterface { get => _isCoreInterface; private set => _isCoreInterface = value; }

        public bool IsVisible { get => _isVisible; protected set => _isVisible = value; }

        public bool IsInputFocus { get => _isInputFocus; set => _isInputFocus = value; }

        public bool IsInitialized { get => _isInitialized; private set => _isInitialized = value; }

        #endregion // Properties

        #region Public Methods

        public void Initialize(int menuID, bool isCore)
        {
            MenuID = menuID;
            IsCoreInterface = isCore;

            // Set the core interface to be always visible and current input focus.
            if (IsCoreInterface)
            {
                IsVisible = true;
                IsInputFocus = true;
            }

            IsInitialized = true;
        }

        public void Show()
        {
            // Show the menu.
            ShowMenu();

            // Set visibility and input focus.
            IsVisible = true;
            IsInputFocus = true;
        }

        public void Hide()
        {
            // Check if this is the core interface first.
            if (IsCoreInterface)
            {
                // Core interface menus cannot be hidden, only lose input focus.
                IsInputFocus = false;
                IsVisible = true;
            }
            else
            {
                // Hide the menu.
                HideMenu();

                // Clear visibility and input focus.
                IsVisible = false;
                IsInputFocus = false;
            }
        }

        #endregion // Public Methods

        #region Abstract Methods

        public abstract void ShowMenu();
        public abstract void HideMenu();

        #endregion // Abstract Methods
    }
}