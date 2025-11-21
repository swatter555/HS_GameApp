using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;
using HammerAndSickle.Models;
using HammerAndSickle.Controllers;
using HammerAndSickle.Services;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Helpers
{
    /// <summary>
    /// A flat data representation of a combat unit for serialization to/from OOB files.
    /// </summary>
    [Serializable]
    public class OobUnitData
    {
        public string UnitID { get; set; }
        public string UnitName { get; set; }
        public float MapPosX { get; set; }
        public float MapPosY { get; set; }
        public Side Side { get; set; }
        public Nationality Nationality { get; set; }
        public UnitClassification Classification { get; set; }
        public UnitRole Role { get; set; }
        public IntelProfileTypes IntelProfileType { get; set; }
        public WeaponSystems DeployedProfileID { get; set; }
        public WeaponSystems MobileProfileID { get; set; }
        public WeaponSystems EmbarkedProfileID { get; set; }
        public bool IsMountable { get; set; }
        public bool IsEmbarkable { get; set; }
        public ExperienceLevel Experience { get; set; }
        public EfficiencyLevel Efficiency { get; set; }
        public DeploymentPosition Deployment { get; set; }
        public SpottedLevel Spotted { get; set; }
        public float ICM { get; set; }
        public float HitPoints { get; set; }
        public float DaysSupply { get; set; }
        public DepotCategory DepotCategory { get; set; }
        public DepotSize DepotSize { get; set; }
        public List<string> AttachedAirUnitIDs { get; set; } = new List<string>();
    }

    /// <summary>
    /// Static helper for loading OOB (Order of Battle) files from standalone or campaign scenarios.
    /// </summary>
    public static class OOBFileLoader
    {
        #region Private Fields

        private const string CLASS_NAME = nameof(OOBFileLoader);
        private readonly static bool _debug = false;

        #endregion // Private Fields

        #region Public Methods

        /// <summary>
        /// Loads a standalone OOB file from the user's Documents folder.
        /// Clears existing units and registers loaded units with GameDataManager.
        /// </summary>
        /// <param name="filename">Filename with or without .oob extension</param>
        /// <returns>True if load succeeded, false otherwise</returns>
        public static bool LoadStandaloneOob(string filename)
        {
            if (_debug)
                Debug.Log($"{CLASS_NAME}.{nameof(LoadStandaloneOob)}: Loading standalone OOB file: {filename}");

            string filePath = BuildFilePath(filename, AppService.OobPath);
            return LoadOobFile(filePath, "standalone");
        }

        /// <summary>
        /// Loads a campaign OOB file from the generated data folder.
        /// Clears existing units and registers loaded units with GameDataManager.
        /// </summary>
        /// <param name="filename">Filename with or without .oob extension</param>
        /// <returns>True if load succeeded, false otherwise</returns>
        public static bool LoadCampaignOob(string filename)
        {
            if (_debug)
                Debug.Log($"{CLASS_NAME}.{nameof(LoadCampaignOob)}: Loading campaign OOB file: {filename}");

            string filePath = BuildFilePath(filename, AppService.GDP_OobPath);
            return LoadOobFile(filePath, "campaign");
        }

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Builds the full file path from filename and base directory.
        /// Handles both "xxx.oob" and "xxx" formats.
        /// </summary>
        private static string BuildFilePath(string filename, string basePath)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentException("Filename cannot be null or empty", nameof(filename));
            }

            // Handle both "xxx.oob" and "xxx" formats
            string cleanFilename = filename.EndsWith(".oob", StringComparison.OrdinalIgnoreCase)
                ? filename
                : $"{filename}.oob";

            string fullPath = Path.Combine(basePath, cleanFilename);

            if (_debug)
                Debug.Log($"{CLASS_NAME}.{nameof(BuildFilePath)}: Built path: {fullPath}");

            return fullPath;
        }

        /// <summary>
        /// Core loading logic for OOB files.
        /// </summary>
        /// <returns>True if load succeeded, false otherwise</returns>
        private static bool LoadOobFile(string filePath, string sourceType)
        {
            try
            {
                if (_debug)
                    Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Starting load from {sourceType} path: {filePath}");

                // Validate file exists
                if (!File.Exists(filePath))
                {
                    string errorMsg = $"OOB file not found: {filePath}";
                    AppService.CaptureUiMessage(errorMsg);
                    throw new FileNotFoundException(errorMsg, filePath);
                }

                // Read JSON content
                string jsonContent = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    string errorMsg = $"OOB file is empty: {filePath}";
                    AppService.CaptureUiMessage(errorMsg);
                    throw new InvalidDataException(errorMsg);
                }

                if (_debug)
                    Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Read {jsonContent.Length} characters from file");

                // Deserialize OOB data
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var oobDataList = JsonSerializer.Deserialize<List<OobUnitData>>(jsonContent, options);

                if (oobDataList == null || oobDataList.Count == 0)
                {
                    string errorMsg = $"No units found in OOB file: {filePath}";
                    AppService.CaptureUiMessage(errorMsg);
                    throw new InvalidDataException(errorMsg);
                }

                if (_debug)
                    Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Deserialized {oobDataList.Count} unit entries");

                // Clear existing game state
                if (_debug)
                    Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Clearing existing game state");

                GameDataManager.Instance.ClearAll();

                // First pass: Create all units from OOB data
                var unitMap = new Dictionary<string, CombatUnit>();
                int playerCount = 0;
                int aiCount = 0;

                foreach (var data in oobDataList)
                {
                    if (_debug)
                        Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Creating unit: {data.UnitName} (ID: {data.UnitID})");

                    // Create unit directly with full constructor
                    var unit = new CombatUnit(
                        data.UnitName,
                        data.Classification,
                        data.Role,
                        data.Side,
                        data.Nationality,
                        data.IntelProfileType,
                        data.DeployedProfileID,
                        data.IsMountable,
                        data.MobileProfileID,
                        data.IsEmbarkable,
                        data.EmbarkedProfileID,
                        data.DepotCategory,
                        data.DepotSize
                    );

                    // Apply saved state
                    unit.SetUnitID(data.UnitID);
                    unit.SetPosition(new Position2D(data.MapPosX, data.MapPosY));
                    unit.SetExperienceLevel(data.Experience);
                    unit.SetEfficiencyLevel(data.Efficiency);
                    unit.SetDeploymentPosition(data.Deployment);
                    unit.SetSpottedLevel(data.Spotted);
                    unit.SetICM(data.ICM);
                    unit.HitPoints.SetCurrent(unit.HitPoints.Max * data.HitPoints);
                    unit.DaysSupply.SetCurrent(unit.DaysSupply.Max * data.DaysSupply);

                    // Track for second pass
                    unitMap[unit.UnitID] = unit;

                    // Register with GameDataManager
                    if (GameDataManager.Instance.RegisterCombatUnit(unit))
                    {
                        if (unit.Side == Side.Player)
                            playerCount++;
                        else
                            aiCount++;

                        if (_debug)
                            Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Registered unit: {unit.UnitName} ({unit.Side})");
                    }
                    else
                    {
                        Debug.LogWarning($"{CLASS_NAME}.{nameof(LoadOobFile)}: Failed to register unit: {unit.UnitName}");
                    }
                }

                if (_debug)
                    Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: First pass complete. Created {unitMap.Count} units");

                // Second pass: Restore air unit attachments
                int attachmentCount = 0;
                foreach (var data in oobDataList.Where(d => d.AttachedAirUnitIDs != null && d.AttachedAirUnitIDs.Count > 0))
                {
                    if (unitMap.TryGetValue(data.UnitID, out var airbase))
                    {
                        if (_debug)
                            Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Restoring air attachments for: {airbase.UnitName}");

                        foreach (var airUnitId in data.AttachedAirUnitIDs)
                        {
                            if (unitMap.TryGetValue(airUnitId, out var airUnit))
                            {
                                if (airbase.AddAirUnit(airUnit))
                                {
                                    attachmentCount++;
                                    if (_debug)
                                        Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Attached {airUnit.UnitName} to {airbase.UnitName}");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"{CLASS_NAME}.{nameof(LoadOobFile)}: Air unit ID not found: {airUnitId}");
                            }
                        }
                    }
                }

                if (_debug && attachmentCount > 0)
                    Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Second pass complete. Restored {attachmentCount} air unit attachments");

                // Success message
                int totalUnits = playerCount + aiCount;
                string successMsg = $"Loaded {totalUnits} units (Player: {playerCount}, AI: {aiCount}) from {sourceType} OOB: {Path.GetFileName(filePath)}";
                AppService.CaptureUiMessage(successMsg);

                if (_debug)
                    Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: {successMsg}");

                return true;
            }
            catch (Exception e)
            {
                string errorMsg = $"Failed to load OOB file: {Path.GetFileName(filePath)}";
                AppService.HandleException(CLASS_NAME, nameof(LoadOobFile), e);
                AppService.CaptureUiMessage(errorMsg);
                return false;
            }
        }

        #endregion // Private Methods
    }
}
