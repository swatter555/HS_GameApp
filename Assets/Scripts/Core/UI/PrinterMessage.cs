using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// One dispatch in the HQ message feed (§24.8). Every dispatch carries the ratified frame — a turn/date
    /// header, the source that filed it, and a short body — plus a category so the CRT's FILTER button can
    /// narrow the feed.
    ///
    /// The frame (§24.8.5a, REVISED 2026-07-26 — the turn/date line was folded into the source line to stop
    /// long unit names wrapping and burning a line of a fixed-height CRT):
    /// <code>
    /// 12: Message from 3rd Tank Rgt
    /// &lt;body, 2–4 lines&gt;
    /// </code>
    /// The campaign date is GONE from the frame, which also retires the §24.8.5a day-level-date problem — the
    /// calendar only ever resolved to a month, tracked the campaign turn rather than the battle turn, and no
    /// scenario start date existed to derive a day from.
    ///
    /// A destroyed unit cannot file its own report, so HQ files it — see the letterhead constants.
    /// </summary>
    public class PrinterMessage
    {
        private const string CLASS_NAME = nameof(PrinterMessage);

        #region Constants

        // Non-unit letterheads (§24.8.5a). Anything not filed by a named unit is filed under one of these.
        public const string SourceDivisionalHQ = "Divisional HQ";
        public const string SourceSupplySection = "Divisional Supply Section";
        public const string SourceWeatherSection = "Front Weather Section";

        #endregion // Constants

        #region Turn Provider

        // ----------------------------------------------------------------------------
        // The frame needs the live battle turn, but BattleManager.Instance LAZY-CREATES a GameObject when none
        // exists. Reading it directly from a plain data class would spawn a manager out of any headless
        // EditorTest that builds a message.
        //
        // So the live read is injected instead: PrinterControl installs a provider in Initialize() (battle
        // scene, where the manager genuinely exists) and clears it in OnDestroy. With no provider installed —
        // tests, main menu — messages carry turn 0, and tests that care pass a turn explicitly.
        // ----------------------------------------------------------------------------

        /// <summary>
        /// Supplies the battle turn for messages constructed without an explicit one.
        /// Installed by <see cref="PrinterControl"/> for the lifetime of the battle scene.
        /// </summary>
        public static Func<int> TurnProvider { get; set; }

        /// <summary>The current battle turn, or 0 when no provider is installed.</summary>
        public static int CurrentTurn()
        {
            try
            {
                return TurnProvider?.Invoke() ?? 0;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CurrentTurn), e);
                return 0;
            }
        }

        #endregion // Turn Provider

        #region Properties

        /// <summary>Battle turn the dispatch was filed on — the "12:" of the frame's first line.</summary>
        public int Turn { get; }

        /// <summary>Who filed the dispatch: a unit name, or one of the letterhead constants.</summary>
        public string Source { get; }

        /// <summary>The body, 2–4 lines by convention.</summary>
        public string[] Lines { get; }

        /// <summary>Filter tag (§24.8.4.1).</summary>
        public PrinterCategory Category { get; }

        /// <summary>The whole dispatch as the CRT renders it: header, source line, body. Built once.</summary>
        public string FullText => _fullText ??= BuildFullText();

        #endregion // Properties

        #region Fields

        private string _fullText;

        #endregion // Fields

        #region Constructors

        /// <param name="lines">Body lines.</param>
        /// <param name="source">Filing unit or letterhead. Defaults to Divisional HQ when blank.</param>
        /// <param name="category">Filter tag.</param>
        /// <param name="turn">Explicit battle turn; when negative the installed provider supplies one.</param>
        public PrinterMessage(string[] lines, string source, PrinterCategory category, int turn = -1)
        {
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
            Source = string.IsNullOrWhiteSpace(source) ? SourceDivisionalHQ : source;
            Category = category;
            Turn = turn >= 0 ? turn : CurrentTurn();
        }

        #endregion // Constructors

        #region Static Factory Methods

        // ----------------------------------------------------------------------------
        // NOTE: the ratified §24.8.6 message catalogue is built in the P7 emitter slice, against the doc.
        // The pre-rewrite ad-hoc factories (CreateBattleReport "ATK:/DEF:/RESULT:", CreateNewContactReport
        // "NEW CONTACT:", CreateAmbushReport "AMBUSH!", CreateSupplyReport / CreateMovementReport /
        // CreateAirThreatDetectedReport / CreateHQDispatch) were DELETED here: all had zero callers, and all
        // wrote in the terse-status-log register that §24.8 explicitly supersedes. Leaving them in place would
        // have invited P7 to keep text the doc no longer sanctions.
        // ----------------------------------------------------------------------------

        /// <summary>
        /// Creates a unit information report. Enemy content is gated by the §12 rung ladder; friendly units
        /// report in full (§24.5a).
        ///
        /// ⚠ SCOPE NARROWED 2026-07-25: this is NO LONGER the enemy selection readout — selecting a unit of
        /// either side now populates the Unit Panel (`Prefab_UnitPanel`). What survives here is the body of the
        /// §24.8.6 INTEL DISPATCH, which P7 raises on spotting events rather than on selection. Kept because
        /// that dispatch needs exactly this rung gating; do not re-point it at selection.
        /// </summary>
        public static PrinterMessage CreateUnitReport(CombatUnit unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));

            bool isFriendly = unit.Side == Side.Player;

            // §24.5a: the report is built from the FILTERED report, never from the live unit. Every line is
            // gated by the rung that reveals it, and a line whose rung has not been earned is OMITTED rather
            // than printed as a default. Supply and leader are FRIENDLY-ONLY — they are not in the intel model
            // at any enemy rung (§24.5a.5/.6).
            IntelReport report = isFriendly ? unit.GetFullIntelReport() : unit.GetIntelReport(unit.SpottedLevel);
            var level = unit.SpottedLevel;

            var lines = new List<string>();

            // Identity. A friendly unit files under its own name, so the frame's source line already carries
            // it and the body opens on content. An enemy report is filed by HQ, so identity goes in the body —
            // and below Level2 the name has not been earned (§12.2.3), which is itself information and is what
            // teaches the player that the ladder exists (§24.5a.7).
            if (!isFriendly)
            {
                lines.Add(level >= SpottedLevel.Level2
                    ? $"Enemy: {report.UnitName}"
                    : "Enemy: UNIDENTIFIED CONTACT");
            }

            // Equipment — Level4+ for an enemy (§24.5a.3). The shared formatter yields the coarse six-bucket
            // tier for enemies and all seventeen for friendlies, so the two views can never drift.
            var entries = report.GetEquipmentEntries();
            if (entries.Count > 0)
                lines.AddRange(FlowIntoColumns(entries, isFriendly ? 3 : 2));

            // Deployment — Level3+ (§24.5a.2).
            if (isFriendly || level >= SpottedLevel.Level3)
                lines.Add($"Deployment: {report.DeploymentPosition}");

            // Experience + efficiency — Level5 only (§24.5a.4).
            if (isFriendly || level >= SpottedLevel.Level5)
                lines.Add($"Experience: {report.UnitExperienceLevel} | Efficiency: {FormatEfficiency(report.UnitEfficiencyLevel)}");

            // Supply — friendly only (§24.5a.5).
            if (isFriendly)
                lines.Add($"{unit.DaysSupply.Current:F1} days of supply");

            // Estimates disclaimer — enemy figures are error-bearing at every rung (§12.5).
            if (!isFriendly && entries.Count > 0)
                lines.Add("Figures are estimates.");

            string source = isFriendly ? unit.UnitName : SourceDivisionalHQ;

            return new PrinterMessage(lines.ToArray(), source, PrinterCategory.Intel);
        }

        #endregion // Static Factory Methods

        #region Formatting Helpers

        /// <summary>
        /// Packs short entries into fixed-width columns so equipment lists fill the CRT instead of running off
        /// it. The old scrolling control set NoWrap + Ellipsis, which would have silently eaten the tail of any
        /// list — including the NBSP-joined entries the intel pass was careful to keep atomic.
        /// </summary>
        /// <param name="entries">Display entries, e.g. "120 tanks".</param>
        /// <param name="columns">Entries per row; values below 1 are treated as 1.</param>
        public static string[] FlowIntoColumns(IReadOnlyList<string> entries, int columns)
        {
            try
            {
                if (entries == null || entries.Count == 0) return Array.Empty<string>();

                int cols = Math.Max(1, columns);

                // Pad every entry to the widest so columns line up down the message.
                int width = 0;
                foreach (string entry in entries)
                {
                    if (entry != null && entry.Length > width) width = entry.Length;
                }

                var rows = new List<string>();
                for (int i = 0; i < entries.Count; i += cols)
                {
                    var sb = new StringBuilder();
                    for (int c = 0; c < cols && i + c < entries.Count; c++)
                    {
                        if (c > 0) sb.Append("  ");
                        sb.Append((entries[i + c] ?? string.Empty).PadRight(width));
                    }

                    rows.Add(sb.ToString().TrimEnd());
                }

                return rows.ToArray();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(FlowIntoColumns), e);
                return Array.Empty<string>();
            }
        }

        private string BuildFullText()
        {
            try
            {
                var sb = new StringBuilder();

                sb.AppendLine($"{Turn}: Message from {Abbreviate(Source)}");

                foreach (string line in Lines)
                    sb.AppendLine(line);

                return sb.ToString().TrimEnd();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(BuildFullText), e);
                return string.Empty;
            }
        }

        // Formation words shortened for the frame. "12: Message from 3rd Motor Rifle Regiment" is 41 characters
        // before the body starts and wraps on a fixed-width CRT, costing a line of a fixed-height message box.
        // Longest first so "Motor Rifle" is consumed before "Rifle" could be.
        private static readonly (string Long, string Short)[] SourceAbbreviations =
        {
            ("Reconnaissance", "Recon"),
            ("Motor Rifle",    "Mtr Rifle"),
            ("Mechanized",     "Mech"),
            ("Independent",    "Indep"),
            ("Artillery",      "Arty"),
            ("Battalion",      "Bn"),
            ("Regiment",       "Rgt"),
            ("Division",       "Div"),
            ("Brigade",        "Bde"),
            ("Engineer",       "Engr"),
            ("Airborne",       "AB"),
            ("Guards",         "Gds"),
        };

        /// <summary>
        /// Shortens formation words in a source name for display. The stored <see cref="Source"/> keeps the full
        /// name — only the rendered frame is abbreviated. Letterheads pass through untouched, since
        /// "Divisional HQ" and friends are already short and are proper nouns of a sort.
        /// </summary>
        public static string Abbreviate(string source)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;

            foreach (var (longForm, shortForm) in SourceAbbreviations)
                source = source.Replace(longForm, shortForm);

            return source;
        }

        private static string FormatEfficiency(EfficiencyLevel level) => level switch
        {
            EfficiencyLevel.FullOperations => "Full Ops",
            EfficiencyLevel.CombatOperations => "Combat Ops",
            EfficiencyLevel.NormalOperations => "Normal Ops",
            EfficiencyLevel.DegradedOperations => "Degraded Ops",
            EfficiencyLevel.StaticOperations => "Static Ops",
            _ => level.ToString()
        };

        #endregion // Formatting Helpers
    }
}
