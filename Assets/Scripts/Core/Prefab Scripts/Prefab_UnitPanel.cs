using System.Collections.Generic;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using TMPro;
using UnityEngine;

namespace HammerAndSickle.Core
{
    /// <summary>
    /// Reactive unit panel: a single formatted text block for the selected FRIENDLY unit (ReactivePanelManager
    /// only shows it for player-side units; enemy intel goes to the printer). Full readout — friendly units
    /// always display Level-4 intel. Detailed combat stats + the NATO symbol were parked 2026-07-23/24.
    /// </summary>
    public class Prefab_UnitPanel : MonoBehaviour
    {
        #region Singleton

        private static Prefab_UnitPanel _instance;

        /// <summary>
        /// Singleton instance with Unity-compliant lazy initialization.
        /// </summary>
        public static Prefab_UnitPanel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<Prefab_UnitPanel>();

                    if (_instance == null)
                    {
                        GameObject go = new("Prefab_UnitPanel");
                        _instance = go.AddComponent<Prefab_UnitPanel>();
                    }
                }
                return _instance;
            }
        }

        #endregion // Singleton

        #region Inspector Fields

        [Header("Unit Panel Fields")]
        [SerializeField] private TextMeshProUGUI _infoText;

        #endregion // Inspector Fields

        #region Initialization

        /// <summary>
        /// Initializes the unit panel by verifying that all required UI components are assigned.
        /// </summary>
        public bool Initialize()
        {
            string errorString = "";

            if (_infoText == null) errorString += "Info Text is not assigned. ";

            if (!string.IsNullOrEmpty(errorString))
            {
                Debug.LogError($"[Prefab_UnitPanel] Initialization errors found: {errorString}");
                return false;
            }

            // Word-wrap must be ON so the flowed equipment line wraps at its entry gaps.
            _infoText.textWrappingMode = TextWrappingModes.Normal;
            return true;
        }

        #endregion // Initialization

        #region Unit Panel Update

        /// <summary>
        /// Updates the unit panel from the selected unit (friendly — RPM gates on side; full Level-4 intel).
        /// Line 1: unit name.
        /// Line 2: commanding officer (omitted when no leader is assigned).
        /// Line 3: equipment flow — non-zero buckets, 2 spaces apart, wrapping at entry gaps.
        /// Line 4: "DEP: ...  EXP: ...  EFF: ...".
        /// Line 5: "Supply: N days".
        /// </summary>
        public void UpdateUnitPanel()
        {
            var unit = GameDataManager.SelectedUnit;
            if (unit == null)
                return;

            var lines = new List<string> { unit.UnitName };

            // Line 2 only when a leader is assigned (self-contained resolve).
            if (unit.IsLeaderAssigned)
            {
                var leader = GameDataManager.Instance.GetLeader(unit.LeaderID);
                if (leader != null)
                    lines.Add($"{leader.FormattedRank} {leader.Name}");
            }

            // Equipment flow: full Level-4 report (friendly), non-zero entries joined by 2 spaces. The
            // shared IntelReport formatter is reused by the printer's enemy report so the two never drift.
            var entries = unit.GetIntelReport(SpottedLevel.Level4).GetEquipmentEntries();
            if (entries.Count > 0)
                lines.Add(string.Join("  ", entries));

            lines.Add($"DEP: {unit.DeploymentPosition}  EXP: {unit.ExperienceLevel}  EFF: {FormatEfficiency(unit.EfficiencyLevel)}");
            lines.Add($"Supply: {unit.DaysSupply.Current:F1} days");

            if (_infoText != null)
                _infoText.text = string.Join("\n", lines);
        }

        #endregion // Unit Panel Update

        #region Helper Methods

        /// <summary>Efficiency level as a short display string (matches the printer's wording).</summary>
        private static string FormatEfficiency(EfficiencyLevel level) => level switch
        {
            EfficiencyLevel.FullOperations => "Full Ops",
            EfficiencyLevel.CombatOperations => "Combat Ops",
            EfficiencyLevel.NormalOperations => "Normal Ops",
            EfficiencyLevel.DegradedOperations => "Degraded Ops",
            EfficiencyLevel.StaticOperations => "Static Ops",
            _ => level.ToString()
        };

        #endregion // Helper Methods
    }
}
