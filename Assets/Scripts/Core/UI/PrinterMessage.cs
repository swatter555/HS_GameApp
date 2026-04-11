using HammerAndSickle.Controllers;
using HammerAndSickle.Models;
using HammerAndSickle.Core.GameData;
using System;

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

            // Get intel report based on unit's side.
            IntelReport report;
            string reportType;
            if (unit.Side == Side.Player)
            {
                report = unit.GetIntelReport(SpottedLevel.Level4); // Full intel for player units
                reportType = "Message from:";
            }
            else
            {
                report = unit.GetIntelReport(unit.SpottedLevel);
                reportType = "Intel report on:";
            }

            // Construct the report lines based on the intel report and unit properties.
            string[] lines = new[]
            {
                $"{reportType} {unit.UnitName}",
                $"{report.Personnel} Men  {report.TANK} Tanks  {report.IFV + report.APC + report.RCN} IFVs/APCs",
                $"{report.ART + report.ROC} Guns/Rockets  {report.AAA + report.SAM} AAA/SAM  {report.AT} AT/ATGMs",
                $"{report.HEL} Helicopters",
                $"Experience: {unit.ExperienceLevel} | Effeciency: {FormatEfficiency(unit.EfficiencyLevel)}",
                $"Deployment: {unit.DeploymentPosition} | {unit.DaysSupply.Current:F1} days of supply",
            };

            return new PrinterMessage(lines, timestamp);
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
