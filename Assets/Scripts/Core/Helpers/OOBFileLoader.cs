using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;
using HammerAndSickle.Models;
using HammerAndSickle.Controllers;
using HammerAndSickle.Persistence;
using HammerAndSickle.Services;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Helpers
{
    /// <summary>
    /// Wrapper for the Scenario Editor's new JSON format: { units: [...], leaders: [...] }
    /// Falls back to treating the entire file as a unit array for legacy compatibility.
    /// </summary>
    [Serializable]
    public class OobFileData
    {
        [JsonPropertyName("units")]
        public List<OobUnitData> Units { get; set; }

        [JsonPropertyName("leaders")]
        public List<OobLeaderData> Leaders { get; set; }
    }

    /// <summary>
    /// A flat data representation of a combat unit for serialization to/from OOB files.
    /// Compatible with the Scenario Editor's JSON output format, where weapon profile IDs
    /// are serialized as strings (e.g., "INF_REG_SV") rather than integer enum values.
    /// </summary>
    [Serializable]
    public class OobUnitData
    {
        public string UnitID { get; set; }
        public string UnitName { get; set; }
        public float MapPosX { get; set; }
        public float MapPosY { get; set; }
        // ⚠ These are ENUM-TYPED, not int, since 2026-07-27 — and that is load-bearing.
        // JsonStringEnumConverter (JsonPolicy.Content) only applies to enum-TYPED properties. While these
        // were declared `int`, a name-form .oob ("Side": "AI") could not be read at ALL: System.Text.Json
        // would throw converting a string to Int32 and the whole file would fail. Declared as enums, the
        // converter accepts BOTH the old integers and the new names, so old and regenerated .oob files
        // both load, and an unknown NAME throws loudly instead of silently landing on member 0.
        public Side Side { get; set; }
        public Nationality Nationality { get; set; }
        public UnitClassification Classification { get; set; }
        // Preferred classification field: the UnitClassification enum NAME (e.g. "BMB").
        // Resolved ahead of Classification, which is fragile in files written BEFORE name-form existed —
        // M0 inserted WW/TRN mid-enum, shifting every value after ATT. Keep emitting this until name-form
        // Classification is confirmed in play; it is the only thing that would mask a failure.
        public string ClassificationName { get; set; }
        public UnitRole Role { get; set; }
        /* `IntelProfileType`/`IsMountable`/`IsEmbarkable` DELETED from the contract 2026-08-08 (P1) —
         * capacity is derived from the slots below. Older `.oob` files still carrying the keys load
         * fine: System.Text.Json skips unmapped members. The Scenario Editor already stopped emitting
         * the first two (their Reply 4); the third goes on our P1-done signal. */

        // String-based weapon profile IDs from the Scenario Editor
        public string DeployedProfileID { get; set; }
        public string MobileProfileID { get; set; }
        public string EmbarkedProfileID { get; set; }

        public ExperienceLevel Experience { get; set; }
        public EfficiencyLevel Efficiency { get; set; }
        public DeploymentPosition Deployment { get; set; }
        public SpottedLevel Spotted { get; set; }
        public float HitPoints { get; set; }
        public float DaysSupply { get; set; }
        public DepotCategory DepotCategory { get; set; }
        public DepotSize DepotSize { get; set; }
        public List<string> AttachedAirUnitIDs { get; set; } = new List<string>();
    }

    /// <summary>
    /// A flat data representation of a leader for serialization to/from OOB files.
    /// The Scenario Editor generates leaders with:
    ///   - CommandGrade/CommandAbility as enum NAMES or integers (both accepted since 2026-07-28)
    ///   - UnitID linking the leader to its assigned combat unit
    ///   - UnlockedSkills as an empty array (skills are unlocked in-game)
    /// </summary>
    [Serializable]
    public class OobLeaderData
    {
        public string LeaderName { get; set; }
        public string UnitID { get; set; }

        // ⚠ ENUM-TYPED since 2026-07-28, for the same reason as OobUnitData — and this class was MISSED in
        // that sweep, which the scenario-editor agent caught by reading the file. While these were `int`,
        // a name-form leader entry would have thrown on deserialize and taken the WHOLE .oob down with it,
        // units and all. Enum-typed, the converter accepts either form.
        public Side Side { get; set; }
        public Nationality Nationality { get; set; }

        // ⚠ CommandGrade is NOT zero-indexed — JuniorGrade = 1 (GameData.cs:241). An absent or 0-valued
        // field would otherwise deserialize to an UNDEFINED enum value that passes every cast silently, so
        // the default is set explicitly here rather than left to default(CommandGrade).
        public CommandGrade CommandGrade { get; set; } = CommandGrade.JuniorGrade;
        public CommandAbility CommandAbility { get; set; } = CommandAbility.Average;

        public int ReputationPoints { get; set; }
        public string PortraitId { get; set; }

        // Skill IDs as strings; the editor always writes []. Skills are unlocked in-game, and saves persist
        // them through SkillReference (name-first), so no enum ordinal is involved on either side.
        public List<string> UnlockedSkills { get; set; } = new List<string>();
    }

    /// <summary>
    /// Static helper for loading OOB (Order of Battle) files generated by the Scenario Editor.
    ///
    /// The Scenario Editor outputs JSON in two possible formats:
    ///   1. Legacy: A flat JSON array of unit objects
    ///   2. Current: An object with "units" and "leaders" arrays
    ///
    /// Unit data:
    ///   - Enum fields (Side, Nationality, Classification, etc.) are serialized as integers
    ///   - WeaponType profile fields are serialized as strings matching the WeaponType enum name
    ///   - AttachedAirUnitIDs contains string unit IDs for fixed-wing aircraft assigned to airbases
    ///   - HitPoints and DaysSupply are stored as 0.0-1.0 ratios
    ///
    /// Leader data:
    ///   - Only present for Player (Soviet) units
    ///   - CommandGrade and CommandAbility as integer enum values
    ///   - ReputationPoints pre-set to TIER1_REP_COST (60)
    ///   - UnlockedSkills starts empty; skills are unlocked during gameplay
    /// </summary>
    public static class OOBFileLoader
    {
        #region Private Fields

        private const string CLASS_NAME = nameof(OOBFileLoader);
        private readonly static bool _debug = false;

        // Cache for WeaponType string-to-enum lookup
        private static Dictionary<string, WeaponType> _weaponTypeLookup;

        #endregion // Private Fields

        #region Public Methods

        /// <summary>
        /// Loads the OOB named by a scenario's manifest, from that scenario's own content folder.
        /// Clears existing units and registers loaded units with GameDataManager.
        ///
        /// ⚠ REPLACED LoadStandaloneOob + LoadCampaignOob (2026-07-27). Those differed ONLY in which root
        /// they read from — Documents/My Games vs Assets/Generated Data — a storage distinction the caller
        /// had to make by inspecting IsCampaignScenario. Scenarios are now self-contained folders, so
        /// there is nothing left to choose between.
        /// </summary>
        /// <param name="manifest">The scenario whose OOB should be loaded.</param>
        /// <returns>True if load succeeded, false otherwise</returns>
        public static bool LoadOob(ScenarioManifest manifest)
        {
            if (manifest == null)
            {
                Debug.LogError($"{CLASS_NAME}.{nameof(LoadOob)}: manifest is null");
                return false;
            }

            string filePath = manifest.GetOobFilePath();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.LogError($"{CLASS_NAME}.{nameof(LoadOob)}: manifest '{manifest.ScenarioId}' names no OOB " +
                               $"file (OobFilename='{manifest.OobFilename}', ContentRoot='{manifest.ContentRoot}')");
                return false;
            }

            if (_debug)
                Debug.Log($"{CLASS_NAME}.{nameof(LoadOob)}: Loading OOB for '{manifest.ScenarioId}': {filePath}");

            return LoadOobFile(filePath, manifest.ScenarioId);
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

            string cleanFilename = filename.EndsWith(".oob", StringComparison.OrdinalIgnoreCase)
                ? filename
                : $"{filename}.oob";

            string fullPath = Path.Combine(basePath, cleanFilename);

            if (_debug)
                Debug.Log($"{CLASS_NAME}.{nameof(BuildFilePath)}: Built path: {fullPath}");

            return fullPath;
        }

        /// <summary>
        /// Initializes the WeaponType string-to-enum lookup dictionary on first use.
        /// Maps enum member names (e.g., "INF_REG_SV") to their WeaponType enum values.
        /// </summary>
        private static void EnsureWeaponTypeLookup()
        {
            if (_weaponTypeLookup != null)
                return;

            _weaponTypeLookup = new Dictionary<string, WeaponType>(StringComparer.OrdinalIgnoreCase);
            foreach (WeaponType wt in Enum.GetValues(typeof(WeaponType)))
            {
                _weaponTypeLookup[wt.ToString()] = wt;
            }

            if (_debug)
                Debug.Log($"{CLASS_NAME}.{nameof(EnsureWeaponTypeLookup)}: Built lookup with {_weaponTypeLookup.Count} entries");
        }

        /// <summary>
        /// Resolves a string-based weapon profile ID to its WeaponType enum value.
        /// Returns WeaponType.NONE for null, empty, or "NONE" inputs.
        /// </summary>
        private static WeaponType ResolveWeaponType(string profileId)
        {
            if (string.IsNullOrEmpty(profileId) || profileId == "NONE")
                return WeaponType.NONE;

            EnsureWeaponTypeLookup();

            if (_weaponTypeLookup.TryGetValue(profileId, out WeaponType result))
                return result;

            Debug.LogWarning($"{CLASS_NAME}.{nameof(ResolveWeaponType)}: Unknown weapon type string: '{profileId}', defaulting to NONE");
            return WeaponType.NONE;
        }

        /// <summary>
        /// Resolves a unit's classification, preferring the string enum NAME (reorder-safe)
        /// over the legacy integer. The int path is fragile: M0 inserted WW/TRN mid-enum, so
        /// any .oob authored before that has its post-ATT classifications shifted by +1. New
        /// .oob files should carry ClassificationName; regenerate older ones to be safe.
        /// </summary>
        private static UnitClassification ResolveClassification(string name, UnitClassification fromField)
        {
            if (!string.IsNullOrEmpty(name)
                && Enum.TryParse(name, ignoreCase: true, out UnitClassification parsed))
                return parsed;

            if (!string.IsNullOrEmpty(name))
                Debug.LogWarning($"{CLASS_NAME}.{nameof(ResolveClassification)}: Unknown classification name '{name}', falling back to the Classification field ({fromField})");

            return fromField;
        }

        /// <summary>
        /// Detects whether the JSON content is the new wrapper format or legacy flat array.
        /// Parses accordingly and returns the unit list and optional leader list.
        /// </summary>
        private static (List<OobUnitData> units, List<OobLeaderData> leaders) DeserializeOobContent(
            string jsonContent, JsonSerializerOptions options)
        {
            // Trim to detect format: '[' = legacy array, '{' = new wrapper object
            string trimmed = jsonContent.TrimStart();

            if (trimmed.StartsWith("{"))
            {
                // New format: { "units": [...], "leaders": [...] }
                var wrapper = JsonSerializer.Deserialize<OobFileData>(jsonContent, options);
                var units = wrapper?.Units ?? new List<OobUnitData>();
                var leaders = wrapper?.Leaders ?? new List<OobLeaderData>();

                if (_debug)
                    Debug.Log($"{CLASS_NAME}.{nameof(DeserializeOobContent)}: Detected wrapper format — {units.Count} units, {leaders.Count} leaders");

                return (units, leaders);
            }
            else
            {
                // Legacy format: flat JSON array of units
                var units = JsonSerializer.Deserialize<List<OobUnitData>>(jsonContent, options);

                if (_debug)
                    Debug.Log($"{CLASS_NAME}.{nameof(DeserializeOobContent)}: Detected legacy array format — {units?.Count ?? 0} units, 0 leaders");

                return (units ?? new List<OobUnitData>(), new List<OobLeaderData>());
            }
        }

        /// <summary>
        /// Core loading logic for OOB files produced by the Scenario Editor.
        ///
        /// Processing order:
        ///   1. Read and validate JSON file
        ///   2. Detect format (wrapper vs legacy) and deserialize
        ///   3. Clear existing game state
        ///   4. First pass: Create all CombatUnit instances with resolved WeaponType enums
        ///   5. Second pass: Restore air unit attachments between airbases and fixed-wing units
        ///   6. Third pass: Create Leader objects and assign to their units
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

                // Deserialize — auto-detects wrapper vs legacy format
                // Serialization policy is centralized — see JsonPolicy for why enums must persist by NAME.
                // Do not reintroduce a local options object here.
                var options = JsonPolicy.Content;

                var (oobDataList, leaderDataList) = DeserializeOobContent(jsonContent, options);

                if (oobDataList.Count == 0)
                {
                    string errorMsg = $"No units found in OOB file: {filePath}";
                    AppService.CaptureUiMessage(errorMsg);
                    throw new InvalidDataException(errorMsg);
                }

                if (_debug)
                    Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Deserialized {oobDataList.Count} unit entries, {leaderDataList.Count} leader entries");

                // Clear existing game state
                if (_debug)
                    Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Clearing existing game state");

                GameDataManager.Instance.ClearAll();

                // ── First pass: Create all units from OOB data ──
                var unitMap = new Dictionary<string, CombatUnit>();
                int playerCount = 0;
                int aiCount = 0;
                int resolveWarnings = 0;

                foreach (var data in oobDataList)
                {
                    if (_debug)
                        Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Creating unit: {data.UnitName} (ID: {data.UnitID})");

                    // Resolve string-based weapon profile IDs to WeaponType enum values
                    WeaponType deployedProfile = ResolveWeaponType(data.DeployedProfileID);
                    WeaponType mobileProfile = ResolveWeaponType(data.MobileProfileID);
                    WeaponType embarkedProfile = ResolveWeaponType(data.EmbarkedProfileID);

                    if (deployedProfile == WeaponType.NONE && !string.IsNullOrEmpty(data.DeployedProfileID) && data.DeployedProfileID != "NONE")
                    {
                        Debug.LogWarning($"{CLASS_NAME}.{nameof(LoadOobFile)}: Could not resolve deployed profile '{data.DeployedProfileID}' for unit {data.UnitName}");
                        resolveWarnings++;
                    }

                    // No casts needed — the DTO is enum-typed and the converter has already parsed either
                    // form. ClassificationName still wins where present (see the field comment).
                    var side = data.Side;
                    var nationality = data.Nationality;
                    var classification = ResolveClassification(data.ClassificationName, data.Classification);
                    var role = data.Role;
                    var depotCategory = data.DepotCategory;
                    var depotSize = data.DepotSize;

                    // Create unit with the bay constructor. ⚠ NAMED arguments on the slot trio, on
                    // purpose: all three are WeaponTypes, and the pre-P1 positional call was one
                    // signature edit away from silently re-binding them (todo_profiles §3).
                    var unit = new CombatUnit(
                        data.UnitName,
                        classification,
                        role,
                        side,
                        nationality,
                        deployedProfile: deployedProfile,
                        mobileProfile: mobileProfile,
                        embarkedProfile: embarkedProfile,
                        category: depotCategory,
                        size: depotSize
                    );

                    // Apply saved state
                    unit.SetUnitID(data.UnitID);
                    unit.SetPosition(new Position2D(data.MapPosX, data.MapPosY));
                    unit.SetExperienceLevel(data.Experience);
                    unit.SetEfficiencyLevel(data.Efficiency);
                    unit.SetDeploymentPosition(data.Deployment);
                    // D5 (P2 2026-08-08): the ctor sized MP from the DEPLOYED profile; a unit authored
                    // at Mobile/Embarked starts on its posture's real ceiling, not the foot one.
                    unit.RefreshMovementPointsForPosture();
                    unit.SetSpottedLevel(data.Spotted);
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
                    Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: First pass complete. Created {unitMap.Count} units ({resolveWarnings} profile resolve warnings)");

                // ── Second pass: Restore air unit attachments ──
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

                // ── Third pass: Create and assign leaders ──
                int leaderCount = 0;
                int leaderWarnings = 0;

                foreach (var ld in leaderDataList)
                {
                    if (string.IsNullOrEmpty(ld.UnitID))
                    {
                        Debug.LogWarning($"{CLASS_NAME}.{nameof(LoadOobFile)}: Leader '{ld.LeaderName}' has no UnitID, skipping");
                        leaderWarnings++;
                        continue;
                    }

                    if (!unitMap.TryGetValue(ld.UnitID, out var targetUnit))
                    {
                        Debug.LogWarning($"{CLASS_NAME}.{nameof(LoadOobFile)}: Leader '{ld.LeaderName}' references unknown unit '{ld.UnitID}', skipping");
                        leaderWarnings++;
                        continue;
                    }

                    // No casts — the DTO is enum-typed and the converter has already parsed either form.
                    var commandAbility = ld.CommandAbility;
                    var side = ld.Side;
                    var nationality = ld.Nationality;
                    var commandGrade = ld.CommandGrade;

                    // Create leader using the standard constructor: (name, side, nationality, commandAbility)
                    var leader = new Leader(ld.LeaderName, side, nationality, commandAbility);

                    // Restore grade and reputation
                    leader.SetCommandGrade(commandGrade);
                    leader.SetReputationPoints(ld.ReputationPoints);

                    // Restore portrait from OOB data
                    if (!string.IsNullOrEmpty(ld.PortraitId))
                    {
                        leader.SetPortraitId(ld.PortraitId);
                    }

                    // Register with GameDataManager first, then assign to unit via
                    // GameDataManager.AssignLeaderToUnit to maintain bidirectional links
                    if (GameDataManager.Instance.RegisterLeader(leader))
                    {
                        if (GameDataManager.Instance.AssignLeaderToUnit(leader.LeaderID, ld.UnitID))
                        {
                            leaderCount++;
                            if (_debug)
                                Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Registered leader: {leader.Name} -> {targetUnit.UnitName}");
                        }
                        else
                        {
                            Debug.LogWarning($"{CLASS_NAME}.{nameof(LoadOobFile)}: Failed to assign leader '{ld.LeaderName}' to unit '{ld.UnitID}'");
                            leaderWarnings++;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"{CLASS_NAME}.{nameof(LoadOobFile)}: Failed to register leader: {ld.LeaderName}");
                        leaderWarnings++;
                    }
                }

                if (_debug && leaderCount > 0)
                    Debug.Log($"{CLASS_NAME}.{nameof(LoadOobFile)}: Third pass complete. Created {leaderCount} leaders ({leaderWarnings} warnings)");

                // Success message
                int totalUnits = playerCount + aiCount;
                string successMsg = $"Loaded {totalUnits} units (Player: {playerCount}, AI: {aiCount})";

                if (leaderCount > 0)
                    successMsg += $", {leaderCount} leaders";

                successMsg += $" from {sourceType} OOB: {Path.GetFileName(filePath)}";

                if (resolveWarnings > 0 || leaderWarnings > 0)
                    successMsg += $" ({resolveWarnings + leaderWarnings} warnings)";

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
