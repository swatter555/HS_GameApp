using System.Collections.Generic;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using TMPro;
using UnityEngine;

namespace HammerAndSickle.Core
{
    /// <summary>
    /// Reactive unit panel: a single formatted text block for the selected unit, friendly OR enemy.
    ///
    /// Enemy units display here too as of 2026-07-25 (Bob's call — this REVERSES the 2026-07-24 §12.5.3
    /// ratification that made the panel friendly-only and routed enemy intel to the printer; the printer keeps
    /// every other dispatch class). Both sides use the SAME layout: the enemy view differs only by what the §12
    /// rung ladder has not revealed yet, so lines are omitted rather than filled with placeholders.
    ///
    /// Friendly = the Full ownership view (§12.2.7 — all seventeen buckets, no error). Enemy = the filtered
    /// report at the unit's current SpottedLevel. Detailed combat stats + the NATO symbol were parked
    /// 2026-07-23/24. Hit points deliberately appear NOWHERE here — §24.3.2.5 keeps the strength readout in the
    /// icon's hit-point box alone.
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
        /// Updates the unit panel from the selected unit, routing to the friendly or enemy view by side.
        /// </summary>
        public void UpdateUnitPanel()
        {
            var unit = GameDataManager.SelectedUnit;
            if (unit == null)
                return;

            var lines = unit.Side == Side.Player ? BuildFriendlyLines(unit) : BuildEnemyLines(unit);

            if (_infoText != null)
                _infoText.text = string.Join("\n", lines);
        }

        /// <summary>
        /// The owner's view of one of their own regiments — everything, unfiltered.
        /// Line 1: unit name.
        /// Line 2: commanding officer (omitted when no leader is assigned).
        /// Line 3: equipment flow — non-zero buckets, 2 spaces apart, wrapping at entry gaps.
        /// Line 4: "DEP: ...  EXP: ...  EFF: ...".
        /// Line 5: "Supply: N days".
        /// </summary>
        private static List<string> BuildFriendlyLines(CombatUnit unit)
        {
            var lines = new List<string> { unit.UnitName };

            // Line 2 only when a leader is assigned (self-contained resolve).
            if (unit.IsLeaderAssigned)
            {
                var leader = GameDataManager.Instance.GetLeader(unit.LeaderID);
                if (leader != null)
                    lines.Add($"{leader.FormattedRank} {leader.Name}");
            }

            // Equipment flow: the FULL friendly view (§12.2.7 — an ownership fact, not a rung, so it is not
            // reachable through GetIntelReport at any SpottedLevel), non-zero entries joined by 2 spaces. The
            // shared IntelReport formatter also drives the enemy view below, so the two can never drift —
            // the enemy side gets the coarse six-bucket tier from the same method.
            var entries = unit.GetFullIntelReport().GetEquipmentEntries();
            if (entries.Count > 0)
                lines.Add(string.Join("  ", entries));

            lines.Add($"DEP: {unit.DeploymentPosition}  EXP: {unit.ExperienceLevel}  EFF: {FormatEfficiency(unit.EfficiencyLevel)}");
            lines.Add($"Supply: {unit.DaysSupply.Current:F1} days");

            return lines;
        }

        /// <summary>
        /// The same layout as the friendly view, minus whatever the §12 ladder has not yet revealed. A line
        /// whose rung has not been earned is OMITTED rather than printed as a default or a row of question
        /// marks (§24.5a) — the panel shortening as intel degrades is itself the feedback.
        ///
        /// Per rung: L1 contact only · L2 + name · L3 + deployment · L4 + equipment in six coarse buckets at
        /// MAX_INTEL_ERROR · L5 + experience and efficiency at MODERATE_INTEL_ERROR.
        /// Supply and commanding officer are FRIENDLY-ONLY at every rung (§24.5a.5/.6) — they are not in the
        /// intel model at all, so they never appear here.
        /// </summary>
        private static List<string> BuildEnemyLines(CombatUnit unit)
        {
            SpottedLevel level = unit.SpottedLevel;
            IntelReport report = unit.GetIntelReport(level);

            // Line 1. Below Level2 the name has not been earned (§12.2.3), so the panel says only that
            // something is out there — which is itself information, and teaches the player the ladder exists.
            var lines = new List<string>
            {
                level >= SpottedLevel.Level2 ? report.UnitName : "UNIDENTIFIED CONTACT"
            };

            // Line 3 equivalent — equipment, Level4+. Same formatter as the friendly view; it yields the coarse
            // six-bucket tier for an enemy report and all seventeen for a friendly one.
            var entries = report.GetEquipmentEntries();
            if (entries.Count > 0)
                lines.Add(string.Join("  ", entries));

            // Line 4 equivalent — deployment at Level3+, experience and efficiency only at Level5. Built as one
            // line in the friendly order so the two panels read the same.
            if (level >= SpottedLevel.Level3)
            {
                string status = $"DEP: {report.DeploymentPosition}";

                if (level >= SpottedLevel.Level5)
                    status += $"  EXP: {report.UnitExperienceLevel}  EFF: {FormatEfficiency(report.UnitEfficiencyLevel)}";

                lines.Add(status);
            }

            // Every enemy figure carries deterministic error (§12.5.5), so the numbers above are estimates and
            // should not be read as a count. Only worth saying once there are numbers to qualify.
            if (entries.Count > 0)
                lines.Add("Figures are estimates.");

            return lines;
        }

        /// <summary>
        /// Blanks the panel's contents without closing it. The reactive panels open on the first hex selection
        /// of the battle and stay open (2026-07-25) — right-click deselect empties them instead of hiding them,
        /// so the HUD keeps a stable layout rather than popping panels in and out.
        ///
        /// Also the state for a hex holding no FRIENDLY unit: the panel is friendly-only (§12.5.3), so an empty
        /// hex and an enemy-occupied hex both leave it blank rather than closed.
        /// </summary>
        public void Clear()
        {
            if (_infoText != null)
                _infoText.text = string.Empty;
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
