using System.Collections.Generic;
using System.Globalization;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using TMPro;
using UnityEngine;

namespace HammerAndSickle.Core
{
    /// <summary>
    /// Reactive unit panel: a formatted monitor and action bar for the selected unit, friendly OR enemy.
    ///
    /// Enemy units display here too as of 2026-07-25 (Bob's call — this REVERSES the 2026-07-24 §12.5.3
    /// ratification that made the panel friendly-only and routed enemy intel to the printer; the printer keeps
    /// every other dispatch class). Both sides use the SAME layout: the enemy view differs only by what the §12
    /// rung ladder has not revealed yet, so lines are omitted rather than filled with placeholders.
    ///
    /// Friendly = the Full ownership view (§12.2.7 — all seventeen buckets, no error). Enemy = the filtered
    /// report at the unit's current SpottedLevel. Flag and classification are visible from Level1; action
    /// counts are available actions for friendly units and zero placeholders for enemies. Hit points deliberately appear NOWHERE here — §24.3.2.5 keeps the strength readout in the
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

        [Header("Action Panel Fields")]
        [SerializeField] private UnityEngine.UI.Image _flagImage;
        [SerializeField, InspectorName("NATO Image")] private UnityEngine.UI.Image _natoImage;
        [SerializeField] private TextMeshProUGUI _moveActionsText;
        [SerializeField] private TextMeshProUGUI _combatActionsText;
        [SerializeField] private TextMeshProUGUI _deployActionsText;
        [SerializeField] private TextMeshProUGUI _intelActionsText;
        [SerializeField] private TextMeshProUGUI _oppActionsText;

        private Nationality? _displayedNationality;
        private UnitClassification? _displayedClassification;

        #endregion // Inspector Fields

        #region Initialization

        /// <summary>
        /// Initializes the unit panel by verifying that all required UI components are assigned.
        /// </summary>
        public bool Initialize()
        {
            string errorString = "";

            if (_infoText == null) errorString += "Info Text is not assigned. ";
            if (_flagImage == null) errorString += "Flag Image is not assigned. ";
            if (_natoImage == null) errorString += "NATO Image is not assigned. ";
            if (_moveActionsText == null) errorString += "Move Actions Text is not assigned. ";
            if (_combatActionsText == null) errorString += "Combat Actions Text is not assigned. ";
            if (_deployActionsText == null) errorString += "Deploy Actions Text is not assigned. ";
            if (_intelActionsText == null) errorString += "Intel Actions Text is not assigned. ";
            if (_oppActionsText == null) errorString += "Opp Actions Text is not assigned. ";

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
            if (unit == null || (unit.Side != Side.Player && unit.SpottedLevel < SpottedLevel.Level1))
            {
                Clear();
                return;
            }

            UpdateIdentityImages(unit);
            UpdateActionCounts(unit);

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
        /// Line 5: "Supply: N/Max days" — ONE line for every unit (SUP-1: one supply number per unit;
        ///         a fixed-wing reads 0.0/0, its supply lives on the airbase).
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

            /* ONE SUPPLY NUMBER PER UNIT (SUP-1, Bob's ruling 2026-08-24) — so ONE supply line, no
             * facility branch: DaysSupply IS the pool for everyone, only the caps differ (regiment 5 ·
             * airbase 30 · depot 30/50/80/110 by size · fixed-wing 0, whose supply lives on its airbase).
             * Current/max because the cap ladder is the rule the player reads off the panel. The short-lived
             * depot "Stockpile:" branch that stood here for a few hours was the band-aid over the old
             * dual-number model — do not reintroduce a per-facility supply readout. */
            lines.Add($"Supply: {unit.DaysSupply.Current:F1}/{unit.DaysSupply.Max:F0} days");

            /* §5.13.2 — the PERSISTENT half of the over-water warning (ratified: "an info box when the unit
             * is selected"). The printer dispatch that fires when the clock starts scrolls away with the
             * next few messages; this line is here every time the player looks at the unit, right up until
             * it makes land or is lost. ⚠ Last so it reads as an alert rather than a stat. */
            if (unit.EndedTurnOverWater)
                lines.Add("OVER WATER — REACH LAND THIS TURN OR BE LOST");

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
        /// Clears contents; The Battle HUD hides this root until a visible unit is selected.
        ///
        /// Also used for an empty hex or an enemy contact below Level1; no previous identity or actions remain.
        /// </summary>
        public void Clear()
        {
            if (_infoText != null)
                _infoText.text = string.Empty;

            SetIdentityImage(_flagImage, null);
            SetIdentityImage(_natoImage, null);
            _displayedNationality = null;
            _displayedClassification = null;
            ClearActionCounts();
        }

        #endregion // Unit Panel Update

        #region Helper Methods

        private void UpdateIdentityImages(CombatUnit unit)
        {
            // The panel refreshes each frame; resolve atlas sprites only when the identity changes.
            if (_flagImage != null && _displayedNationality != unit.Nationality)
            {
                string flagName = FlagSpriteName(unit.Nationality);
                SetIdentityImage(_flagImage, flagName == null ? null : SpriteManager.GetSprite(flagName));
                _displayedNationality = unit.Nationality;
            }

            if (_natoImage != null && _displayedClassification != unit.Classification)
            {
                string spriteName = Prefab_CombatUnitIcon.NatoSpriteName(unit.Classification);
                Sprite sprite = SpriteManager.GetUnitPrefabSprite(spriteName);
                if (sprite == null && spriteName != SpriteManager.UnknownDeploymentIcon)
                    sprite = SpriteManager.GetUnitPrefabSprite(SpriteManager.UnknownDeploymentIcon);

                SetIdentityImage(_natoImage, sprite);
                _displayedClassification = unit.Classification;
            }
        }

        private static void SetIdentityImage(UnityEngine.UI.Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private void UpdateActionCounts(CombatUnit unit)
        {
            // Remaining enemy actions are not part of the intel report at any spotting level.
            if (unit.Side != Side.Player)
            {
                SetActionCount(_moveActionsText, 0);
                SetActionCount(_combatActionsText, 0);
                SetActionCount(_deployActionsText, 0);
                SetActionCount(_intelActionsText, 0);
                SetActionCount(_oppActionsText, 0);
                return;
            }

            SetActionCount(_moveActionsText, unit.GetMoveActions());
            SetActionCount(_combatActionsText, unit.GetCombatActions());
            SetActionCount(_deployActionsText, unit.GetDeployActions());
            SetActionCount(_intelActionsText, unit.GetIntelActions());
            SetActionCount(_oppActionsText, unit.GetOpportunityActions());
        }

        private static void SetActionCount(TextMeshProUGUI text, float count)
        {
            if (text != null)
                text.text = Mathf.Max(0, Mathf.FloorToInt(count)).ToString(CultureInfo.InvariantCulture);
        }

        private void ClearActionCounts()
        {
            if (_moveActionsText != null) _moveActionsText.text = string.Empty;
            if (_combatActionsText != null) _combatActionsText.text = string.Empty;
            if (_deployActionsText != null) _deployActionsText.text = string.Empty;
            if (_intelActionsText != null) _intelActionsText.text = string.Empty;
            if (_oppActionsText != null) _oppActionsText.text = string.Empty;
        }

        /// <summary>Generic or unrecognized nationalities have no authored national flag.</summary>
        internal static string FlagSpriteName(Nationality nationality) => nationality switch
        {
            Nationality.USSR => SpriteManager.Flag_SV,
            Nationality.USA => SpriteManager.Flag_US,
            Nationality.FRG => SpriteManager.Flag_GE,
            Nationality.UK => SpriteManager.Flag_UK,
            Nationality.FRA => SpriteManager.Flag_FR,
            Nationality.BE => SpriteManager.Flag_BE,
            Nationality.DE => SpriteManager.Flag_DE,
            Nationality.NE => SpriteManager.Flag_NE,
            Nationality.MJ => SpriteManager.Flag_MJ,
            Nationality.IR => SpriteManager.Flag_Iran,
            Nationality.IQ => SpriteManager.Flag_Iraq,
            Nationality.SAUD => SpriteManager.Flag_Saudi,
            Nationality.KW => SpriteManager.Flag_Kuwait,
            Nationality.China => SpriteManager.Flag_China,
            _ => null
        };

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
