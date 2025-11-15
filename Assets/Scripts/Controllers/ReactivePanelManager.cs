using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Manages reactive UI panels for terrain, units, and leaders.
    /// </summary>
    public class ReactivePanelManager : MonoBehaviour
    {
        #region Inspector Fields

        [SerializeField]
        private bool _debug;

        [SerializeField]
        private GameObject _terrainPanelObject;

        [SerializeField]
        private GameObject _unitPanelObject;

        [SerializeField]
        private GameObject _leaderPanelObject;

        #endregion // Inspector Fields

        #region Unity Lifecycle

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Update is called once per frame to manage panel visibility and updates.
        /// </summary>
        private void Update()
        {
            if (GameDataManager.SelectedHex != GameDataManager.NoHexSelected)
            {
                // Update Terrain Panel
                if (Prefab_TerrainPanel.Instance != null)
                    Prefab_TerrainPanel.Instance.UpdateTerrainPanel();

                // Set Terrain Panel Active
                if (_terrainPanelObject != null)
                    _terrainPanelObject.SetActive(true);
            }
            else
            {
                // Hide Terrain Panel if no hex is selected.
                if (_terrainPanelObject != null)
                    _terrainPanelObject.SetActive(false);
            }
        }

        #endregion // Unity Lifecycle

        #region Initialization

        private void Initialize()
        {
            // Initialize Panels to be inactive at start
            if (_terrainPanelObject != null)
            {
                // Initialize the panel
                if(!Prefab_TerrainPanel.Instance.Initialize())
                {
                    Debug.LogError("Failed to initialize Terrain Panel.");
                }

                // Set it to inactive
                _terrainPanelObject.SetActive(false);
            }
            else Debug.LogWarning("Terrain Panel Object is not assigned in the inspector.");

            if (_unitPanelObject != null)
            {
                _unitPanelObject.SetActive(false);
            }
            else Debug.LogWarning("Unit Panel Object is not assigned in the inspector.");

            if (_leaderPanelObject != null)
            {
                _leaderPanelObject.SetActive(false);
            }
            else Debug.LogWarning("Leader Panel Object is not assigned in the inspector.");
        }

        #endregion // Initialization
    }
}
