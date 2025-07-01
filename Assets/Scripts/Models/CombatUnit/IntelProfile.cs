using System;
using System.Collections.Generic;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
  /*
  * IntelProfile — Hammer & Sickle data‑model
  * -----------------------------------------------------------
  *  Purpose
  *  -------
  *  Acts as an **organizational template** for a combat unit.  It stores the
  *  maximum count of every WeaponSystems enum that can appear in that unit and
  *  can generate an `IntelReport` reflecting the unit’s *current* equipment
  *  (scaled by hit‑points) with an optional Fog‑of‑War distortion.
  *
  *  Fog‑of‑War / SpottedLevel semantics
  *  -----------------------------------
  *      Level0 • Not spotted — nothing about composition is known; callers
  *                should treat the unit as invisible.  This method therefore
  *                returns **null** or an empty report (implementation choice).
  *
  *      Level1 • Minimal contact — *Unit name only* is populated in the returned
  *                IntelReport, all other fields remain zero / default.
  *
  *      Level2 • Poor intel — Equipment buckets are included but each bucket is
  *                independently modified by ±30 % random error.  CombatState is
  *                exposed; Experience/Efficiency are **not**.
  *
  *      Level3 • Good intel — Same as Level2 but the random error band is
  *                ±10 %.  Experience & Efficiency levels are now included.
  *
  *      Level4 • Perfect intel — Full, error‑free information.  The report
  *                mirrors the unit’s true internal state exactly.
  *
  *  Public API (signatures only)
  *  ----------------------------
  *      // Constructor
  *      IntelProfile(IntelProfileTypes profileID);
  *
  *      // Add a new weapon‑system definition (throws on duplicates / bad data)
  *      bool AddWeaponSystem(WeaponSystems weaponSystem, int maxQuantity);
  *
  *      // Generate a fog‑of‑war filtered intelligence snapshot
  *      IntelReport GenerateIntelReport(
  *          string        unitName,
  *          int           currentHitPoints,
  *          Nationality   nationality,
  *          CombatState   combatState,
  *          ExperienceLevel xpLevel,
  *          EfficiencyLevel effLevel,
  *          SpottedLevel  spottedLevel = SpottedLevel.Level1);
  *
  *  How the class works (high‑level flow)
  *  -------------------------------------
  *  1. **Strength scaling** — All maximum equipment counts are multiplied by
  *     `currentHitPoints / CUConstants.MAX_HP` (clamped to 0) to represent
  *     attrition.
  *  2. **Bucket aggregation** — Individual weapon systems are mapped to GUI
  *     buckets (Men, Tanks, IFVs, …) via `MapPrefixToBucket`.
  *  3. **Fog‑of‑War** — Each bucket is optionally blurred according to the
  *     spotted level (see above).
  *  4. **Pruning** — Any bucket whose final tally is < 1 is omitted so no
  *     fractional or “ghost” equipment appears.
  *
  *  Exception & Logging Policy
  *  --------------------------
  *  All public methods wrap their bodies in try/catch and delegate to
  *  `AppService.HandleException(CLASS_NAME, methodName, e)` for centralised
  *  logging; the exception is re‑thrown where failure cannot safely be ignored.
  *
  *  Thread‑Safety Note
  *  ------------------
  *  The class is *not* thread‑safe; callers must synchronise access if a single
  *  instance is shared across threads.
  *
  * -----------------------------------------------------------
  */
    public class IntelProfile
    {
        #region Constants

        private const string CLASS_NAME = nameof(IntelProfile);

        #endregion // Constants


        #region Fields

        private readonly Dictionary<WeaponSystems, int> weaponSystems;    // Tracks weapon systems with max quantities.

        #endregion // Fields


        #region Properties

        public IntelProfileTypes IntelProfileID { get; private set; }       // Unique identifier for this profile.

        #endregion // Properties


        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IntelProfile"/> class with the specified profile ID and
        /// nationality.
        /// </summary>
        /// <param name="profileID">The unique identifier for the unit profile, representing its type.</param>
        /// <param name="nationality">The nationality associated with the unit profile.</param>
        public IntelProfile(IntelProfileTypes profileID)
        {
            try
            {
                IntelProfileID = profileID;
                weaponSystems = new Dictionary<WeaponSystems, int>();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        #endregion // Constructors


        #region Public Methods

        /// <summary>
        /// Adds a new weapon system entry with specified max quantity.
        /// </summary>
        /// <param name="weaponSystem">WeaponSystems enum</param>
        /// <param name="maxQuantity">Max num possible</param>
        /// <returns></returns>
        public bool AddWeaponSystem(WeaponSystems weaponSystem, int maxQuantity)
        {
            try
            {
                // Check for valid WeaponSystems value
                if (weaponSystem == WeaponSystems.DEFAULT)
                    throw new ArgumentException("Cannot add DEFAULT weapon system");

                // Check for valid maxQuantity
                if (maxQuantity < 0)
                    throw new ArgumentOutOfRangeException(nameof(maxQuantity), "Max quantity cannot be negative");

                // Check if this exact weaponSystem already exists
                if (weaponSystems.ContainsKey(weaponSystem))
                    throw new InvalidOperationException($"WeaponSystemEntry for {weaponSystem} already exists");

                // Add new entry
                weaponSystems[weaponSystem] = maxQuantity;

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AddWeaponSystem), e);
                return false;
            }
        }

        /// <summary>
        /// Generates an IntelReport object containing bucketed weapon system data and unit metadata.
        /// This provides structured data for the GUI to display unit intelligence information.
        /// Applies fog of war effects to final buckets only based on spotted level for AI units.
        /// Reports all weapon systems in the profile regardless of their ProfileItem designation.
        /// Buckets with values less than 1 are omitted from the final report.
        /// </summary>
        /// <param name="unitName">Display name of the unit</param>
        /// <param name="combatState">Current combat state of the unit</param>
        /// <param name="xpLevel">Experience level of the unit</param>
        /// <param name="effLevel">Efficiency level of the unit</param>
        /// <param name="spottedLevel">Intelligence level for AI units (default Level0 for player units)</param>
        /// <returns>IntelReport object with categorized weapon data and unit information</returns>
        public IntelReport GenerateIntelReport(string unitName,
            int currentHitPoints,
            Nationality nationality,
            CombatState combatState, 
            ExperienceLevel xpLevel, 
            EfficiencyLevel effLevel, 
            SpottedLevel spottedLevel = SpottedLevel.Level1)
        {
            try
            {
                // Create the intel report object
                var intelReport = new IntelReport();

                // Set unit metadata
                intelReport.UnitNationality = nationality;
                intelReport.UnitName = unitName;
                intelReport.UnitState = combatState;
                intelReport.UnitExperienceLevel = xpLevel;
                intelReport.UnitEfficiencyLevel = effLevel;

                // Handle special spotted levels
                if (spottedLevel == SpottedLevel.Level1)
                {
                    // Level1: Only unit name is visible, skip all calculations
                    return intelReport;
                }

                // Calculate multiplier for current strength, guard against divide by zero.
                float safeHitPoints = currentHitPoints;
                if (safeHitPoints <= 0) safeHitPoints = 1;
                float currentMultiplier = safeHitPoints / CUConstants.MAX_HP;

                // Step 1: Accumulate weapon systems by type with float precision (NO fog-of-war here)
                var weaponSystemAccumulators = new Dictionary<WeaponSystems, float>();

                foreach (var kvp in weaponSystems)
                {
                    WeaponSystems weaponSystem = kvp.Key;
                    int maxQuantity = kvp.Value; // Safe - captured from iteration
                    float scaledValue = maxQuantity * currentMultiplier;

                    // Accumulate multiple entries for same weapon system (float precision)
                    if (weaponSystemAccumulators.ContainsKey(weaponSystem))
                    {
                        weaponSystemAccumulators[weaponSystem] += scaledValue;
                    }
                    else if (scaledValue > 0f)
                    {
                        weaponSystemAccumulators[weaponSystem] = scaledValue;
                    }
                }

                // Step 2: Categorize weapons into buckets with float precision (NO fog-of-war here)
                var bucketAccumulators = new Dictionary<string, float>();

                foreach (var kvp in weaponSystemAccumulators)
                {
                    WeaponSystems weaponSystem = kvp.Key;
                    float weaponCount = kvp.Value;

                    string prefix = GetWeaponSystemPrefix(weaponSystem);
                    string bucketName = MapPrefixToBucket(prefix);

                    if (bucketName != null && weaponCount > 0f)
                    {
                        // Accumulate in bucket with float precision
                        if (bucketAccumulators.ContainsKey(bucketName))
                        {
                            bucketAccumulators[bucketName] += weaponCount;
                        }
                        else
                        {
                            bucketAccumulators[bucketName] = weaponCount;
                        }
                    }
                }

                // Step 3: Apply fog of war to buckets ONLY, round to final integer values, and omit buckets < 1
                foreach (var bucketKvp in bucketAccumulators)
                {
                    string bucketName = bucketKvp.Key;
                    float accumulatedValue = bucketKvp.Value;

                    // Calculate fog of war multiplier for this bucket (ONLY fog-of-war application)
                    float bucketMultiplier = 1f;
                    if (spottedLevel == SpottedLevel.Level2 || spottedLevel == SpottedLevel.Level3)
                    {
                        // Each bucket gets its own independent fog-of-war direction and magnitude
                        bool isPositiveDirection = UnityEngine.Random.Range(0f, 1f) >= 0.5f;
                        float errorRangeMin = 1f;
                        float errorRangeMax = spottedLevel == SpottedLevel.Level2 ? 30f : 10f;
                        float errorPercent = UnityEngine.Random.Range(errorRangeMin, errorRangeMax);
                        bucketMultiplier = isPositiveDirection ? (1f + errorPercent / 100f) : (1f - errorPercent / 100f);
                    }

                    // Apply fog of war and round ONLY at the final step
                    int finalValue = (int)Math.Round(accumulatedValue * bucketMultiplier);

                    // Only assign non-zero values - omit buckets with values < 1
                    if (finalValue > 0)
                    {
                        // Assign to appropriate IntelReport property
                        switch (bucketName)
                        {
                            case "Men":
                                intelReport.Men = finalValue;
                                break;
                            case "Tanks":
                                intelReport.Tanks = finalValue;
                                break;
                            case "IFVs":
                                intelReport.IFVs = finalValue;
                                break;
                            case "APCs":
                                intelReport.APCs = finalValue;
                                break;
                            case "Recon":
                                intelReport.RCNs = finalValue;
                                break;
                            case "Artillery":
                                intelReport.ARTs = finalValue;
                                break;
                            case "Rocket Artillery":
                                intelReport.ROCs = finalValue;
                                break;
                            case "Surface To Surface Missiles":
                                intelReport.SSMs = finalValue;
                                break;
                            case "SAMs":
                                intelReport.SAMs = finalValue;
                                break;
                            case "Anti-aircraft Artillery":
                                intelReport.AAAs = finalValue;
                                break;
                            case "MANPADs":
                                intelReport.MANPADs = finalValue;
                                break;
                            case "ATGMs":
                                intelReport.ATGMs = finalValue;
                                break;
                            case "Attack Helicopters":
                                intelReport.HEL = finalValue;
                                break;
                            case "Fighters":
                                intelReport.ASFs = finalValue;
                                break;
                            case "Multirole":
                                intelReport.MRFs = finalValue;
                                break;
                            case "Attack":
                                intelReport.ATTs = finalValue;
                                break;
                            case "Bombers":
                                intelReport.BMBs = finalValue;
                                break;
                            case "AWACS":
                                intelReport.AWACS = finalValue;
                                break;
                            case "Recon Aircraft":
                                intelReport.RCNAs = finalValue;
                                break;
                        }
                    }
                    // If finalValue <= 0, the bucket is omitted from the report
                }

                return intelReport;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GenerateIntelReport), e);
                throw;
            }
        }

        #endregion // Public Methods


        #region Helper Methods

        /// <summary>
        /// Extracts the prefix from the name of a weapon system.
        /// </summary>
        /// <param name="weaponSystem">The weapon system whose name prefix is to be extracted.</param>
        /// <returns>A string containing the prefix of the weapon system's name. If the name does not contain an underscore, the
        /// entire name of the weapon system is returned.</returns>
        private string GetWeaponSystemPrefix(WeaponSystems weaponSystem)
        {
            string weaponName = weaponSystem.ToString();
            int underscoreIndex = weaponName.IndexOf('_');
            return underscoreIndex >= 0 ? weaponName.Substring(0, underscoreIndex) : weaponName;
        }

        /// <summary>
        /// Maps a given prefix to its corresponding bucket category.
        /// </summary>
        /// <remarks>The method uses a predefined mapping of prefixes to bucket categories. For example:
        /// <list type="bullet"> <item><description>Prefixes such as "REG", "AB", and "AM" map to the "Men"
        /// category.</description></item> <item><description>"TANK" maps to "Tanks".</description></item>
        /// <item><description>Prefixes like "SAM" and "SPSAM" map to "SAMs".</description></item> </list> If the prefix
        /// does not match any of the predefined mappings, the method returns <see langword="null"/>.</remarks>
        /// <param name="prefix">The prefix representing a specific category. This value determines the bucket to which it is mapped.</param>
        /// <returns>A string representing the bucket category corresponding to the provided prefix.  Returns <see
        /// langword="null"/> if the prefix does not match any known category.</returns>
        private string MapPrefixToBucket(string prefix)
        {
            return prefix switch
            {
                "REG" or "AB" or "AM" or "MAR" or "SPEC" or "ENG" => "Men",
                "TANK" => "Tanks",
                "IFV" => "IFVs",
                "APC" => "APCs",
                "RCN" => "Recon",
                "ART" or "SPA" => "Artillery",
                "ROC" => "Rocket Artillery",
                "SSM" => "Surface To Surface Missiles",
                "SAM" or "SPSAM" => "SAMs",
                "AAA" or "SPAAA" => "Anti-aircraft Artillery",
                "MANPAD" => "MANPADs",
                "ATGM" => "ATGMs",
                "HEL" => "Attack Helicopters",
                "ASF" => "Fighters",
                "MRF" => "Multirole",
                "ATT" => "Attack",
                "BMB" => "Bombers",
                "AWACS" => "AWACS",
                "RCNA" => "Recon Aircraft",
                _ => null
            };
        }

        #endregion // Helper Methods
    }
}