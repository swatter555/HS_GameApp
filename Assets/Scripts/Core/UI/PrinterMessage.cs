using HammerAndSickle.Controllers;
using HammerAndSickle.Models;
using HammerAndSickle.Core.GameData;
using System;
using System.Collections.Generic;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// Represents a structured message to be displayed on the HQ printer control.
    /// </summary>
    public class PrinterMessage
    {
        #region Properties

        public string[] Lines { get; }
        public string Timestamp { get; }

        #endregion // Properties

        #region Constructors

        public PrinterMessage(string[] lines, string timestamp)
        {
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
            Timestamp = timestamp ?? string.Empty;
        }

        #endregion // Constructors

        #region Static Factory Methods

        /// <summary>
        /// Creates a unit information report for the HQ printer.
        /// </summary>
        public static PrinterMessage CreateUnitReport(CombatUnit unit, string timestamp)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));

            bool isFriendly = unit.Side == Side.Player;

            // §24.5a: the report is built from the FILTERED report, never from the live unit. Every line is
            // gated by the rung that reveals it, and a line whose rung has not been earned is OMITTED rather
            // than printed as a default. Supply and leader are FRIENDLY-ONLY — they are not in the intel model
            // at any enemy rung (§24.5a.5/.6).
            IntelReport report = isFriendly ? unit.GetFullIntelReport() : unit.GetIntelReport(unit.SpottedLevel);
            var level = unit.SpottedLevel;

            var lines = new List<string>
            {
                isFriendly ? $"Message from: {unit.UnitName}" : IntelHeader(report, level)
            };

            // Equipment — Level4+ for an enemy (§24.5a.3). The shared formatter yields the coarse six-bucket
            // tier for enemies and all seventeen for friendlies, so the two views can never drift.
            var entries = report.GetEquipmentEntries();
            if (entries.Count > 0)
                lines.Add(string.Join("  ", entries));

            // Deployment — Level3+ (§24.5a.2).
            if (isFriendly || level >= SpottedLevel.Level3)
                lines.Add($"Deployment: {report.DeploymentPosition}");

            // Experience + efficiency — Level5 only (§24.5a.4).
            if (isFriendly || level >= SpottedLevel.Level5)
                lines.Add($"Experience: {report.UnitExperienceLevel} | Efficiency: {FormatEfficiency(report.UnitEfficiencyLevel)}");

            // Supply — friendly only (§24.5a.5).
            if (isFriendly)
                lines.Add($"{unit.DaysSupply.Current:F1} days of supply");

            return new PrinterMessage(lines.ToArray(), timestamp);
        }

        /// <summary>
        /// Creates a battle report for the HQ printer.
        /// </summary>
        public static PrinterMessage CreateBattleReport(
            string attackerName, float attackerHpBefore, float attackerHpAfter,
            string defenderName, float defenderHpBefore, float defenderHpAfter,
            string result, string timestamp)
        {
            string[] lines = new[]
            {
                $"ATK: {attackerName} {attackerHpBefore:0}% -> {attackerHpAfter:0}%",
                $"DEF: {defenderName} {defenderHpBefore:0}% -> {defenderHpAfter:0}%",
                $"RESULT: {result}"
            };

            return new PrinterMessage(lines, timestamp);
        }

        /// <summary>
        /// Creates a supply report for the HQ printer. Stub for future implementation.
        /// </summary>
        public static PrinterMessage CreateSupplyReport(string[] details, string timestamp)
        {
            return new PrinterMessage(details, timestamp);
        }

        /// <summary>
        /// Creates a movement report for the HQ printer.
        /// </summary>
        public static PrinterMessage CreateMovementReport(string[] details, string timestamp)
        {
            return new PrinterMessage(details, timestamp);
        }

        /// <summary>
        /// Creates a "new contact" printer message when an enemy is first spotted.
        /// </summary>
        public static PrinterMessage CreateNewContactReport(string unitName, string timestamp)
        {
            return new PrinterMessage(new[] { $"NEW CONTACT: {unitName}" }, timestamp);
        }

        /// <summary>
        /// Creates an ambush notification for the printer.
        /// </summary>
        public static PrinterMessage CreateAmbushReport(string ambusherName, string victimName, string timestamp)
        {
            return new PrinterMessage(new[] { $"AMBUSH! {ambusherName} attacks {victimName}" }, timestamp);
        }

        /// <summary>
        /// Creates an air ambush detection notification for the printer.
        /// </summary>
        public static PrinterMessage CreateAirThreatDetectedReport(string timestamp)
        {
            return new PrinterMessage(new[] { "Threat detected — mission continues" }, timestamp);
        }

        /// <summary>
        /// Creates an HQ dispatch for the HQ printer. Stub for future implementation.
        /// </summary>
        public static PrinterMessage CreateHQDispatch(string[] details, string timestamp)
        {
            return new PrinterMessage(details, timestamp);
        }

        #endregion // Static Factory Methods

        #region Private Helpers

        /// <summary>
        /// The enemy report header. Below Level2 the unit's NAME has not been earned (§12.2.3), so the report
        /// says only that something is there — which is itself information, and is what teaches the player
        /// that the ladder exists (§24.5a.7).
        /// </summary>
        private static string IntelHeader(IntelReport report, SpottedLevel level) =>
            level >= SpottedLevel.Level2
                ? $"Intel report on: {report.UnitName}"
                : "Intel report: UNIDENTIFIED CONTACT";

        private static string FormatEfficiency(EfficiencyLevel level) => level switch
        {
            EfficiencyLevel.FullOperations => "Full Ops",
            EfficiencyLevel.CombatOperations => "Combat Ops",
            EfficiencyLevel.NormalOperations => "Normal Ops",
            EfficiencyLevel.DegradedOperations => "Degraded Ops",
            EfficiencyLevel.StaticOperations => "Static Ops",
            _ => level.ToString()
        };

        #endregion // Private Helpers
    }
}
