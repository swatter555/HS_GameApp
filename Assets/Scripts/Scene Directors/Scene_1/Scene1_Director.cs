using System;
using HammerAndSickle.Controllers;
using HammerAndSickle.Services;
using HammerAndSickle.Core.Helpers;
using HammerAndSickle.Core.Map;

namespace HammerAndSickle.SceneDirectors
{
    public class Scene1_Director : SceneDirectorBase
    {
        #region Singleton Instance

        public static Scene1_Director Instance { get; private set; }

        #endregion // Singleton Instance

        #region Protected Methods

        /// <summary>
        /// Retrieves the name of the class associated with this instance.
        /// </summary>
        protected override string GetClassName() => nameof(Scene1_Director);

        /// <summary>
        /// Sets up the singleton instance for the Scene0_Director.
        /// </summary>
        protected override void SetupSingleton()
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

        /// <summary>
        /// Main scene specific initialization logic.
        /// </summary>
        protected override void OnSceneInitialize()
        {
            // Provide ID to core interface and register.
            Scene1_Core.Instance.Initialize(GeneralConstants.KhostScene_CoreInterface_ID, true);
            RegisterCoreInterface(GeneralConstants.KhostScene_CoreInterface_ID, Scene1_Core.Instance);

            // Register Orders dialog.
            RegisterMenu(Scene1_Orders.Instance);

            // Start the scene with the OrdersDialog open.
            SetActiveMenuByID(GeneralConstants.KhostScene_OrdersDialog_ID);

            // Initialize audio manager and start main menu music
            InitializeAudio();

            // Initialize game data and scene specific elements
            Initialize();
        }

        /// <summary>
        /// Initializes the GameAudioManager and starts playing main menu music.
        /// </summary>
        private void InitializeAudio()
        {
            try
            {
                // Ensure GameAudioManager exists
                GameAudioManager.EnsureExists();

                // Cancel any existing sounds immediately.
                GameAudioManager.Instance.StopMusic();

                // Play the snare drum sound at the start of the scenario.
                GameAudioManager.Instance.PlaySFX(GameAudioManager.SoundEffect.MeduimSnareDrum);

                // Scenario scenes just need ambient music, no music.
                GameAudioManager.Instance.PlayAmbient(GameAudioManager.AmbientSound.AmbientCombat, fadeInTime: 1.0f);
            }
            catch (System.Exception e)
            {
                AppService.HandleException(GetClassName(), "InitializeAudio", e);
            }
        }

        /// <summary>
        /// Performs cleanup operations after a scene has been processed.
        /// </summary>
        /// <remarks>This method is called to release resources or reset state related to the scene. 
        /// Override this method to implement custom cleanup logic specific to your application.</remarks>
        protected override void OnSceneCleanup()
        {
            // Optional: Fade out music when leaving main menu
            try
            {
                if (GameAudioManager.Instance != null)
                {
                    GameAudioManager.Instance.StopMusic(fadeOutTime: 0.5f);
                }
            }
            catch (System.Exception e)
            {
                AppService.HandleException(GetClassName(), "OnSceneCleanup", e);
            }
        }

        #endregion // Protected Methods

        #region Private Methods

        /// <summary>
        /// Initialize game data and scene specific elements.
        /// </summary>
        private void Initialize()
        {
            try
            {
                // Destroy existing hex map if any
                if (GameDataManager.CurrentHexMap != null)
                {
                    GameDataManager.CurrentHexMap.Dispose();
                    GameDataManager.CurrentHexMap = null;
                }

                // Check for a valid ScenarioManifest
                if (GameDataManager.CurrentManifest == null)
                    throw new InvalidOperationException("CurrentManifest is null during scene initialization.");

                // Load the hex map from the specified scenario manifest
                if (!MapLoader.LoadMapFile(GameDataManager.CurrentManifest))
                {
                    throw new InvalidOperationException("Failed to load hex map from scenario manifest");
                }

                HexMapRenderer.Instance.RefreshMap();
            }
            catch (Exception e)
            {
                // Any errors here are fatal.
                AppService.HandleException(GetClassName(), "Initialize", e);
                AppService.UnityQuit_DataUnsafe();
            }
        }
        
        #endregion // Private Methods
    }
}