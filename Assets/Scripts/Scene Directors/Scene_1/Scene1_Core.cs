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