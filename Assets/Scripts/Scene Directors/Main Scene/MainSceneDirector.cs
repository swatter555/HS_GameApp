using HammerAndSickle.Controllers;
using HammerAndSickle.Services;

namespace HammerAndSickle.SceneDirectors
{
    public class MainSceneDirector : SceneDirectorBase
    {
        #region Singleton Instance

        public static MainSceneDirector Instance { get; private set; }

        #endregion // Singleton Instance

        #region Protected Methods
        /// <summary>
        /// Retrieves the name of the class associated with this instance.
        /// </summary>
        protected override string GetClassName() => nameof(MainSceneDirector);

        /// <summary>
        /// Sets up the singleton instance for the MainSceneDirector.
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
            MainSceneCoreInterface.Instance.Initialize(GeneralConstants.MainScene_CoreInterface_ID, true);
            RegisterCoreInterface(GeneralConstants.MainScene_CoreInterface_ID, MainSceneCoreInterface.Instance);

            // Initialize audio manager and start main menu music
            InitializeAudio();
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

                // Preload common UI sound effects for immediate playback
                GameAudioManager.Instance.PreloadSFX(
                    GameAudioManager.SoundEffect.ButtonClick,
                    GameAudioManager.SoundEffect.ButtonHover,
                    GameAudioManager.SoundEffect.MenuOpen,
                    GameAudioManager.SoundEffect.MenuClose,
                    GameAudioManager.SoundEffect.RadioButtonClick
                );

                // Start playing main menu music on loop with 1 second fade-in
                GameAudioManager.Instance.PlayMusic(
                    GameAudioManager.MusicTrack.MainMenu,
                    loop: true,
                    fadeInTime: 1.0f
                );
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
    }
}