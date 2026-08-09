using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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

        /// <summary>
        /// Builds the cumulative loss report (§24.8 / printer P6): two columns, OURS and ENEMY, over the
        /// six ratified rows — Men · Tanks · AFVs · Guns · Aircraft · Helicopters.
        ///
        /// ⚠ ROUNDING HAPPENS HERE AND ONLY HERE. The ledgers accumulate FRACTIONAL equipment on purpose
        /// (a unit holding 3 tanks that takes 1 HP of 40 contributes 0.075 of a tank); rounding earlier
        /// destroys every small contribution and lets a regiment be ground to death reporting no losses.
        /// This is the render step the ledger's float values were protecting.
        ///
        /// ⚠ THE ROLLUP GOES THROUGH <see cref="EquipmentBays.ClassifyWeaponType"/> — the same classifier
        /// the intel report uses — so the loss report and the intel report cannot disagree about what
        /// counts as a tank.
        ///
        /// ⚠ ENEMY FIGURES ARE EXACT, ratified and deliberate: this is a post-hoc HQ tally, not live intel,
        /// so it carries none of the §12.5 estimate error and no "figures are estimates" disclaimer.
        /// </summary>
        /// <param name="ourLosses">Player-side ledger, fractional and un-rounded.</param>
        /// <param name="enemyLosses">AI-side ledger, fractional and un-rounded.</param>
        public static PrinterMessage CreateLossReport(
            IReadOnlyDictionary<WeaponType, float> ourLosses,
            IReadOnlyDictionary<WeaponType, float> enemyLosses,
            bool dailyOnly = false)
        {
            try
            {
                string heading = dailyOnly ? "TURN LOSSES" : "ALL LOSSES";

                // Build the rows first so the empty case can REPLACE them rather than trail below them.
                var rows = new List<string>();
                bool anyLosses = false;

                foreach ((string label, EquipmentBucket[] buckets) in LossReportRows)
                {
                    int ours = RollUp(ourLosses, buckets);
                    int theirs = RollUp(enemyLosses, buckets);

                    if (ours > 0 || theirs > 0)
                        anyLosses = true;

                    rows.Add($"{label,-LOSS_ROW_LABEL_WIDTH}{ours,LOSS_ROW_VALUE_WIDTH}{theirs,LOSS_ROW_VALUE_WIDTH}");
                }

                var lines = new List<string>();

                if (anyLosses)
                {
                    // ⚠ HEADING AND COLUMN HEADER SHARE ONE LINE — the heading sits in the row-label column
                    // ("ALL LOSSES      OURS   ENEMY"). Two separate lines plus a blank spacer made the
                    // report 10 lines tall against a panel that shows about that many, so it overran
                    // vertically (Bob, in play 2026-07-28). This is the cheapest line to remove because the
                    // label column is empty on that row anyway — pure dead space carrying no information.
                    lines.Add($"{heading,-LOSS_ROW_LABEL_WIDTH}{"OURS",LOSS_ROW_VALUE_WIDTH}{"ENEMY",LOSS_ROW_VALUE_WIDTH}");
                    lines.AddRange(rows);
                }
                else
                {
                    // ⚠ THE EMPTY REPORT REPLACES THE TABLE, it does not follow it. Appended below six zero
                    // rows the notice landed past the bottom of the panel — so the one line that mattered
                    // was the one clipped, and the message said nothing at all in the exact case it existed
                    // to explain. Six zeroes also read as a bug on their own.
                    lines.Add(heading);
                    lines.Add(dailyOnly ? "No losses this turn." : "No losses reported.");
                }

                return new PrinterMessage(lines.ToArray(), SourceDivisionalHQ, PrinterCategory.Combat);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateLossReport), e);
                return new PrinterMessage(new[] { "Loss report unavailable." }, SourceDivisionalHQ, PrinterCategory.Combat);
            }
        }

        /// <summary>
        /// Sums a ledger's fractional entries across the buckets making up one display row, rounding ONCE
        /// at the end.
        /// ⚠ Sum first, THEN round — rounding each bucket before adding re-introduces exactly the loss of
        /// small values the fractional ledger exists to prevent.
        /// </summary>
        private static int RollUp(IReadOnlyDictionary<WeaponType, float> ledger, EquipmentBucket[] buckets)
        {
            if (ledger == null || ledger.Count == 0) return 0;

            float total = 0f;

            foreach (KeyValuePair<WeaponType, float> entry in ledger)
            {
                EquipmentBucket bucket = EquipmentBays.ClassifyWeaponType(entry.Key);

                foreach (EquipmentBucket wanted in buckets)
                {
                    if (bucket == wanted)
                    {
                        total += entry.Value;
                        break;
                    }
                }
            }

            return Mathf.RoundToInt(total);
        }

        #endregion // Static Factory Methods

        #region Loss Report Layout

        private const int LOSS_ROW_LABEL_WIDTH = 14;
        private const int LOSS_ROW_VALUE_WIDTH = 8;

        // The six RATIFIED rows, and which equipment buckets roll into each.
        //
        // ⚠ AFVs EXISTS BECAUSE A SOVIET MECH FORCE LOSES MOSTLY AFVs and a report without the row would
        // look wrong (Bob's call). Recon vehicles file here rather than as tanks.
        //
        // ⚠ TRUCKS ARE NOT REPORTED, AND CORRECTLY SO (Bob, 2026-07-28): no truck profile declares any
        // intel stats at all — TRK_GEN_SV and TRK_WEST add none — so trucks are absent from the intel model
        // itself and there is nothing to report. Not a gap; there is no data.
        //
        // ⚠ BUT `EquipmentBucket.TRN` IS IN THE AIRCRAFT ROW, and that is not a typo. The TRN bucket
        // catches both the TRN_ and TRK_ prefixes, and the ONLY profile in it that declares intel stats is
        // the An-12 — a fixed-wing TRANSPORT PLANE carrying 48. Left out of every row, a destroyed An-12
        // regiment would have printed "Aircraft 0" while 48 aircraft quietly vanished from the tally.
        // Since trucks contribute nothing and TRN_NAVAL declares nothing either, folding TRN into Aircraft
        // is exact today rather than approximate. ⚠ Revisit if a truck or naval transport ever GAINS intel
        // stats — they would then wrongly land under Aircraft, and the bucket needs splitting.
        private static readonly (string Label, EquipmentBucket[] Buckets)[] LossReportRows =
        {
            ("Men",         new[] { EquipmentBucket.Personnel }),
            ("Tanks",       new[] { EquipmentBucket.TANK }),
            ("AFVs",        new[] { EquipmentBucket.IFV, EquipmentBucket.APC, EquipmentBucket.RCN }),
            ("Guns",        new[] { EquipmentBucket.ART, EquipmentBucket.ROC, EquipmentBucket.SAM,
                                    EquipmentBucket.AAA, EquipmentBucket.AT }),
            ("Aircraft",    new[] { EquipmentBucket.FGT, EquipmentBucket.ATT, EquipmentBucket.BMB,
                                    EquipmentBucket.AWACS, EquipmentBucket.RCNA, EquipmentBucket.TRN }),
            ("Helicopters", new[] { EquipmentBucket.HEL })
        };

        #endregion // Loss Report Layout

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
