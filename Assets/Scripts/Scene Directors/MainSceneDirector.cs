using HammerAndSickle.Controllers;
using HammerAndSickle.Services;
using System;
using UnityEngine;

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
            // Main scene specific initialization
        }

        /// <summary>
        /// Performs cleanup operations after a scene has been processed.
        /// </summary>
        /// <remarks>This method is called to release resources or reset state related to the scene. 
        /// Override this method to implement custom cleanup logic specific to your application.</remarks>
        protected override void OnSceneCleanup()
        {
            // Cleanup code
        }

        #endregion // Protected Methods
    }
}