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

        // The printer/message panel is no longer managed here. Its visibility became message-driven in the
        // printer pass (2026-07-25): PrinterControl owns its own _panelRoot, shows it when a dispatch arrives,
        // and hides it on right-click deselect. See PrinterControl and HS_DesignDoc §24.8.

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
        /// Update is called once per frame to manage panel content.
        ///
        /// PANEL MODEL (revised 2026-07-27). The three information panels — terrain, unit, and the printer's
        /// dispatch CRT — are OPEN FROM SCENE START and never close. Visibility is not a thing that happens
        /// any more: right-click deselect CLEARS contents, it does not hide, so the HUD holds one stable
        /// layout for the whole battle. The printer is the exception to the clearing half — a dispatch log is
        /// not about the selected hex, so it keeps its history (§24.8). The printer opens itself; this manager
        /// owns only the terrain and unit panels.
        /// </summary>
        private void Update()
        {
            if (GameDataManager.SelectedHex == GameDataManager.NoHexSelected)
            {
                ClearSelectionPanels();
                return;
            }

            // Resolve unit and leader at the selected hex
            ResolveSelection();

            Prefab_TerrainPanel.Instance.UpdateTerrainPanel();

            // Unit panel shows BOTH sides as of 2026-07-25 (reverses the 2026-07-24 friendly-only rule; the
            // printer keeps every other dispatch class). An empty hex blanks the panel.
            //
            // ⚠ The Level0 gate is a fog-of-war boundary, not a formatting choice: GetUnitAtPosition answers
            // from the map regardless of spotting, so without it clicking an empty-looking hex would report an
            // unspotted enemy sitting on it and hand the player free intel the ladder never granted.
            var selected = GameDataManager.SelectedUnit;
            bool displayable = selected != null
                && (selected.Side == Side.Player || selected.SpottedLevel >= SpottedLevel.Level1);

            if (displayable) Prefab_UnitPanel.Instance.UpdateUnitPanel();
            else Prefab_UnitPanel.Instance.Clear();
        }

        #endregion // Unity Lifecycle

        #region Initialization

        /// <summary>
        /// Brings both panels up EMPTY at scene start (2026-07-27). They stay up for the rest of the battle —
        /// only their contents change. Clearing here matters: without it the panels would display whatever
        /// placeholder text the prefab was authored with until the player's first hex click.
        /// </summary>
        private void Initialize()
        {
            if (_terrainPanelObject != null)
            {
                _terrainPanelObject.SetActive(true);

                if (!Prefab_TerrainPanel.Instance.Initialize())
                {
                    Debug.LogError("Failed to initialize Terrain Panel.");
                }

                Prefab_TerrainPanel.Instance.Clear();
            }
            else Debug.LogWarning("Terrain Panel Object is not assigned in the inspector.");

            if (_unitPanelObject != null)
            {
                _unitPanelObject.SetActive(true);

                if (!Prefab_UnitPanel.Instance.Initialize())
                {
                    Debug.LogError("Failed to initialize Unit Panel.");
                }

                Prefab_UnitPanel.Instance.Clear();
            }
            else Debug.LogWarning("Unit Panel Object is not assigned in the inspector.");
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
                //
                // No side gate is needed here: there are NO enemy leaders in the game (§14.2.3, permanent —
                // every Leader construction path passes Side.Player). An enemy unit therefore never reports
                // IsLeaderAssigned, so this can only ever resolve a friendly commander.
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
        /// Empties the terrain and unit panels on deselect and drops the resolved unit/leader. The panels stay
        /// OPEN — only their contents go. The printer is deliberately untouched: its history is not about the
        /// selected hex, so a right-click does not wipe the dispatch log.
        /// </summary>
        private void ClearSelectionPanels()
        {
            GameDataManager.SelectedUnit = null;
            GameDataManager.SelectedLeader = null;

            Prefab_TerrainPanel.Instance.Clear();
            Prefab_UnitPanel.Instance.Clear();
        }

        #endregion // Panel Updates
    }
}
