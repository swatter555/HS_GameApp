using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;
using UnityEngine;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Manages reactive UI panels for terrain and units. (Leader info moved to a modal dialog
    /// 2026-07-23 — the reactive leader panel was removed.)
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
        [Tooltip("The printer/message panel root. INTERIM: shown with the other panels on selection, hidden on right-click deselect. The planned printer pass makes visibility message-driven — see Claude_TODO.")]
        private GameObject _messagePanelObject;

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

            // Terrain + message panels are shown whenever a hex is selected and hide together on
            // right-click deselect. (Message-panel visibility is INTERIM — the planned printer pass makes
            // it message-driven; see Claude_TODO.)
            UpdateTerrainPanel(true);
            UpdateMessagePanel(true);

            // Unit panel is FRIENDLY-ONLY (2026-07-24): enemy intel goes to the printer (wired in the
            // printer pass). Shown only when a player-side unit occupies the selected hex.
            bool hasFriendlyUnit = GameDataManager.SelectedUnit != null
                && GameDataManager.SelectedUnit.Side == Side.Player;
            UpdateUnitPanel(hasFriendlyUnit);
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

            // Message panel starts hidden; shown on selection, hidden on deselect (INTERIM — see Update).
            if (_messagePanelObject != null)
                _messagePanelObject.SetActive(false);
            else Debug.LogWarning("Message Panel Object is not assigned in the inspector.");
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

                // Resolve leader from the selected unit. Kept live (cheap) so the planned leader modal
                // and any future consumer can read GameDataManager.SelectedLeader off the current
                // selection; the reactive leader panel that used to display it was removed 2026-07-23.
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
        /// Updates the message (printer) panel visibility. INTERIM: tracks selection like the other panels
        /// so right-click deselect closes it. The planned printer pass replaces this with message-driven
        /// visibility (show on message received, hide on deselect) — see Claude_TODO.
        /// </summary>
        private void UpdateMessagePanel(bool show)
        {
            if (_messagePanelObject == null)
                return;

            _messagePanelObject.SetActive(show);
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

            if (_messagePanelObject != null)
                _messagePanelObject.SetActive(false);
        }

        #endregion // Panel Updates
    }
}
