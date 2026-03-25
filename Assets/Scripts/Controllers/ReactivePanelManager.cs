using HammerAndSickle.Core;
using HammerAndSickle.Services;
using UnityEngine;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Manages reactive UI panels for terrain, units, and leaders.
    /// Panels stack vertically: unit (top), leader (middle), terrain (bottom).
    /// </summary>
    public class ReactivePanelManager : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(ReactivePanelManager);

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
            if (GameDataManager.SelectedHex == GameDataManager.NoHexSelected)
            {
                HideAllPanels();
                return;
            }

            // Resolve unit and leader at the selected hex
            ResolveSelection();

            // Terrain panel is always shown when a hex is selected
            UpdateTerrainPanel(true);

            // Unit panel shown only when a unit occupies the selected hex
            bool hasUnit = GameDataManager.SelectedUnit != null;
            UpdateUnitPanel(hasUnit);

            // Leader panel shown only when the selected unit has an assigned leader
            bool hasLeader = GameDataManager.SelectedLeader != null;
            UpdateLeaderPanel(hasLeader);
        }

        #endregion // Unity Lifecycle

        #region Initialization

        private void Initialize()
        {
            // Initialize panels to be inactive at start
            if (_terrainPanelObject != null)
            {
                if (!Prefab_TerrainPanel.Instance.Initialize())
                {
                    Debug.LogError("Failed to initialize Terrain Panel.");
                }

                _terrainPanelObject.SetActive(false);
            }
            else Debug.LogWarning("Terrain Panel Object is not assigned in the inspector.");

            if (_unitPanelObject != null)
            {
                if (!Prefab_UnitPanel.Instance.Initialize())
                {
                    Debug.LogError("Failed to initialize Unit Panel.");
                }

                _unitPanelObject.SetActive(false);
            }
            else Debug.LogWarning("Unit Panel Object is not assigned in the inspector.");

            if (_leaderPanelObject != null)
            {
                if (!Prefab_LeaderPanel.Instance.Initialize())
                {
                    Debug.LogError("Failed to initialize Leader Panel.");
                }

                _leaderPanelObject.SetActive(false);
            }
            else Debug.LogWarning("Leader Panel Object is not assigned in the inspector.");
        }

        #endregion // Initialization

        #region Panel Updates

        /// <summary>
        /// Resolves the selected unit and leader based on the currently selected hex.
        /// </summary>
        private void ResolveSelection()
        {
            try
            {
                // Find unit at the selected hex position
                GameDataManager.SelectedUnit = GameDataManager.Instance.GetUnitAtPosition(GameDataManager.SelectedHex);

                // Resolve leader from the selected unit
                if (GameDataManager.SelectedUnit != null && GameDataManager.SelectedUnit.IsLeaderAssigned)
                {
                    GameDataManager.SelectedLeader = GameDataManager.Instance.GetLeader(GameDataManager.SelectedUnit.LeaderID);
                }
                else
                {
                    GameDataManager.SelectedLeader = null;
                }
            }
            catch (System.Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveSelection), e);
                GameDataManager.SelectedUnit = null;
                GameDataManager.SelectedLeader = null;
            }
        }

        /// <summary>
        /// Updates the terrain panel visibility and content.
        /// </summary>
        private void UpdateTerrainPanel(bool show)
        {
            if (_terrainPanelObject == null)
                return;

            _terrainPanelObject.SetActive(show);

            if (show && Prefab_TerrainPanel.Instance != null)
                Prefab_TerrainPanel.Instance.UpdateTerrainPanel();
        }

        /// <summary>
        /// Updates the unit panel visibility and content.
        /// </summary>
        private void UpdateUnitPanel(bool show)
        {
            if (_unitPanelObject == null)
                return;

            _unitPanelObject.SetActive(show);

            if (show && Prefab_UnitPanel.Instance != null)
                Prefab_UnitPanel.Instance.UpdateUnitPanel();
        }

        /// <summary>
        /// Updates the leader panel visibility and content.
        /// </summary>
        private void UpdateLeaderPanel(bool show)
        {
            if (_leaderPanelObject == null)
                return;

            _leaderPanelObject.SetActive(show);

            if (show && Prefab_LeaderPanel.Instance != null)
                Prefab_LeaderPanel.Instance.UpdateLeaderPanel();
        }

        /// <summary>
        /// Hides all panels and clears unit/leader selection state.
        /// </summary>
        private void HideAllPanels()
        {
            GameDataManager.SelectedUnit = null;
            GameDataManager.SelectedLeader = null;

            if (_terrainPanelObject != null)
                _terrainPanelObject.SetActive(false);

            if (_unitPanelObject != null)
                _unitPanelObject.SetActive(false);

            if (_leaderPanelObject != null)
                _leaderPanelObject.SetActive(false);
        }

        #endregion // Panel Updates
    }
}
