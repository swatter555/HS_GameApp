using System;
using System.Collections.Generic;
using HammerAndSickle.Services;

/*───────────────────────────────────────────────────────────────────────────────
 IntelProfile  —  static organizational template system for unit intelligence
 ────────────────────────────────────────────────────────────────────────────────
 Overview
 ════════
 **IntelProfile** provides static organizational templates that define the
 maximum equipment composition for each unit type in Hammer & Sickle. Unlike
 per-unit data storage, this system uses shared static definitions to generate
 intelligence reports with fog-of-war distortion based on spotted levels.

 Major Responsibilities
 ══════════════════════
 • Static template storage for all unit organizational structures
     - Dictionary-based weapon system definitions per IntelProfileTypes
     - Maximum equipment counts for each WeaponSystems enum
     - Memory-efficient shared reference architecture
 • Intelligence report generation with fog-of-war effects
     - Level-based accuracy distortion (±30% to perfect intel)
     - Strength scaling based on current hit points
     - Bucket aggregation for GUI display categories
 • Startup initialization and data validation
     - Profile loading and consistency checking
     - Error handling through AppService integration
     - Thread-safe static access patterns

 Design Highlights
 ═════════════════
 • **Static Architecture**: Eliminates per-unit object storage overhead while
   maintaining organizational template integrity across all units of same type.
 • **Enum-Based Lookup**: CombatUnit stores only IntelProfileTypes enum value,
   enabling O(1) template access without reference resolution complexity.
 • **Fog-of-War System**: Sophisticated intelligence distortion with independent
   bucket-level error application for realistic reconnaissance simulation.
 • **Bucket Mapping**: Weapon system prefixes automatically categorized into
   GUI-friendly display buckets (Men, Tanks, IFVs, Artillery, etc.).
 • **Memory Optimization**: Single static definition per unit type serves
   thousands of individual units without duplication.

 Fog-of-War Intelligence Levels
 ══════════════════════════════
 • **Level 0 (Not Spotted)**: No intelligence available - method returns null
 • **Level 1 (Minimal Contact)**: Unit name and metadata only, zero equipment counts
 • **Level 2 (Poor Intel)**: Equipment buckets with ±30% random error per bucket
 • **Level 3 (Good Intel)**: Equipment buckets with ±10% random error per bucket  
 • **Level 4 (Perfect Intel)**: Exact equipment counts with no distortion

 Bucket Categories
 ═════════════════
 Equipment automatically categorized into display buckets:
 • **Personnel**: Men (infantry, crews, specialists)
 • **Vehicles**: Tanks, IFVs, APCs, Recon vehicles
 • **Artillery**: Artillery, Rocket Artillery, Surface-to-Surface Missiles
 • **Air Defense**: SAMs, Anti-Aircraft Artillery, MANPADs
 • **Support**: ATGMs, Engineering equipment
 • **Aircraft**: Attack Helicopters, Fighters, Multirole, Attack, Bombers, AWACS, Recon Aircraft

 Public Interface
 ════════════════
   ── Initialization ──────────────────────────────────────────────────────────
   InitializeProfiles()               Load all static profile definitions
   IsInitialized                      Check if profiles loaded successfully
   
   ── Intelligence Generation ─────────────────────────────────────────────────
   GenerateIntelReport(profileType,   Generate fog-of-war filtered intelligence
     unitName, currentHitPoints,      snapshot with specified accuracy level
     nationality, combatState, 
     xpLevel, effLevel, spottedLevel)
     
   ── Profile Management ──────────────────────────────────────────────────────
   HasProfile(profileType)            Check if profile type is defined
   GetWeaponSystemCount(profileType,  Get max count for specific weapon system
     weaponSystem)                    in profile type
   GetDefinedWeaponSystems(           Get all weapon systems in profile type
     profileType)

 Implementation Flow
 ═══════════════════
 1. **Startup Initialization**: LoadProfileDefinitions() populates static dictionary
    with organizational data for all IntelProfileTypes enum values
 2. **Strength Scaling**: Equipment counts multiplied by (currentHP / maxHP) to
    represent unit attrition and combat effectiveness
 3. **Bucket Aggregation**: Individual weapon systems mapped to GUI categories
    using prefix-based classification system
 4. **Fog-of-War Application**: Each bucket independently distorted based on
    spotted level with random directional error
 5. **Pruning**: Buckets with final values < 1 omitted from report to prevent
    "ghost" equipment display

 Thread Safety
 ═════════════
 Static initialization is thread-safe with double-checked locking. Profile
 access is read-only after initialization, enabling safe concurrent access
 from multiple game systems without synchronization overhead.

 ───────────────────────────────────────────────────────────────────────────────
 MAINTAIN CONSISTENCY WITH IntelReport BUCKET PROPERTIES!
 ─────────────────────────────────────────────────────────────────────────────── */
namespace HammerAndSickle.Models
{
    public static class IntelProfile
    {
        #region Constants

        private const string CLASS_NAME = nameof(IntelProfile);

        #endregion


        #region Fields

        private static readonly Dictionary<IntelProfileTypes, Dictionary<WeaponSystems, int>> _profiles
            = new Dictionary<IntelProfileTypes, Dictionary<WeaponSystems, int>>();

        private static readonly object _initializationLock = new object();
        private static bool _isInitialized = false;

        #endregion


        #region Properties

        /// <summary>
        /// Gets whether the profile system has been successfully initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        #endregion


        #region Public Methods

        /// <summary>
        /// Initializes all static profile definitions. Must be called during application startup
        /// before any intelligence reports can be generated.
        /// </summary>
        public static void InitializeProfiles()
        {
            if (_isInitialized) return;

            lock (_initializationLock)
            {
                if (_isInitialized) return; // Double-check after acquiring lock

                try
                {
                    LoadProfileDefinitions();
                    _isInitialized = true;
                }
                catch (Exception e)
                {
                    AppService.HandleException(CLASS_NAME, nameof(InitializeProfiles), e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Checks if a specific profile type has been defined in the system.
        /// </summary>
        /// <param name="profileType">The profile type to check</param>
        /// <returns>True if the profile type is defined</returns>
        public static bool HasProfile(IntelProfileTypes profileType)
        {
            try
            {
                EnsureInitialized();
                return _profiles.ContainsKey(profileType);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HasProfile), e);
                return false;
            }
        }

        /// <summary>
        /// Gets the maximum count for a specific weapon system in a profile type.
        /// </summary>
        /// <param name="profileType">The profile type to query</param>
        /// <param name="weaponSystem">The weapon system to look up</param>
        /// <returns>Maximum count, or 0 if not found</returns>
        public static int GetWeaponSystemCount(IntelProfileTypes profileType, WeaponSystems weaponSystem)
        {
            try
            {
                EnsureInitialized();

                if (_profiles.TryGetValue(profileType, out var weaponSystems))
                {
                    return weaponSystems.TryGetValue(weaponSystem, out int count) ? count : 0;
                }

                return 0;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetWeaponSystemCount), e);
                return 0;
            }
        }

        /// <summary>
        /// Gets all weapon systems defined for a specific profile type.
        /// </summary>
        /// <param name="profileType">The profile type to query</param>
        /// <returns>Dictionary of weapon systems and their maximum counts</returns>
        public static IReadOnlyDictionary<WeaponSystems, int> GetDefinedWeaponSystems(IntelProfileTypes profileType)
        {
            try
            {
                EnsureInitialized();

                if (_profiles.TryGetValue(profileType, out var weaponSystems))
                {
                    return weaponSystems;
                }

                return new Dictionary<WeaponSystems, int>();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetDefinedWeaponSystems), e);
                return new Dictionary<WeaponSystems, int>();
            }
        }

        /// <summary>
        /// Generates an IntelReport object containing bucketed weapon system data and unit metadata.
        /// Applies fog-of-war effects based on spotted level to simulate realistic intelligence gathering.
        /// </summary>
        /// <param name="profileType">The organizational profile type for this unit</param>
        /// <param name="unitName">Display name of the unit</param>
        /// <param name="currentHitPoints">Current hit points representing unit strength</param>
        /// <param name="nationality">National affiliation of the unit</param>
        /// <param name="combatState">Current tactical posture of the unit</param>
        /// <param name="xpLevel">Experience level of the unit</param>
        /// <param name="effLevel">Operational efficiency level of the unit</param>
        /// <param name="spottedLevel">Intelligence accuracy level (default Level1)</param>
        /// <returns>IntelReport with categorized equipment data and unit metadata, or null if not spotted</returns>
        public static IntelReport GenerateIntelReport(
            IntelProfileTypes profileType,
            string unitName,
            int currentHitPoints,
            Nationality nationality,
            CombatState combatState,
            ExperienceLevel xpLevel,
            EfficiencyLevel effLevel,
            SpottedLevel spottedLevel = SpottedLevel.Level1)
        {
            try
            {
                EnsureInitialized();

                // Level 0: Not spotted - return null
                if (spottedLevel == SpottedLevel.Level0)
                {
                    return null;
                }

                // Create the intel report object
                var intelReport = new IntelReport
                {
                    UnitNationality = nationality,
                    UnitName = unitName,
                    UnitState = combatState
                };

                // Level 1: Only unit name and basic metadata visible
                if (spottedLevel == SpottedLevel.Level1)
                {
                    return intelReport;
                }

                // For levels 2-4, include experience and efficiency based on spotted level
                if (spottedLevel == SpottedLevel.Level3 || spottedLevel == SpottedLevel.Level4)
                {
                    intelReport.UnitExperienceLevel = xpLevel;
                    intelReport.UnitEfficiencyLevel = effLevel;
                }

                // Get the profile definition
                if (!_profiles.TryGetValue(profileType, out var weaponSystems))
                {
                    AppService.HandleException(CLASS_NAME, nameof(GenerateIntelReport),
                        new KeyNotFoundException($"Profile type {profileType} not found"));
                    return intelReport; // Return basic report with no equipment
                }

                // Calculate strength multiplier, guard against divide by zero
                float safeHitPoints = Math.Max(currentHitPoints, 1);
                float currentMultiplier = safeHitPoints / CUConstants.MAX_HP;

                // Step 1: Scale weapon systems by current strength
                var scaledWeaponSystems = new Dictionary<WeaponSystems, float>();
                foreach (var kvp in weaponSystems)
                {
                    float scaledValue = kvp.Value * currentMultiplier;
                    if (scaledValue > 0f)
                    {
                        scaledWeaponSystems[kvp.Key] = scaledValue;
                    }
                }

                // Step 2: Aggregate into display buckets
                var bucketAccumulators = new Dictionary<string, float>();
                foreach (var kvp in scaledWeaponSystems)
                {
                    string prefix = GetWeaponSystemPrefix(kvp.Key);
                    string bucketName = MapPrefixToBucket(prefix);

                    if (bucketName != null && kvp.Value > 0f)
                    {
                        if (bucketAccumulators.ContainsKey(bucketName))
                        {
                            bucketAccumulators[bucketName] += kvp.Value;
                        }
                        else
                        {
                            bucketAccumulators[bucketName] = kvp.Value;
                        }
                    }
                }

                // Step 3: Apply fog-of-war and assign to report
                foreach (var bucketKvp in bucketAccumulators)
                {
                    string bucketName = bucketKvp.Key;
                    float accumulatedValue = bucketKvp.Value;

                    // Calculate fog-of-war multiplier
                    float bucketMultiplier = CalculateFogOfWarMultiplier(spottedLevel);

                    // Apply fog-of-war and round to final integer value
                    int finalValue = (int)Math.Round(accumulatedValue * bucketMultiplier);

                    // Only assign non-zero values - omit buckets with values < 1
                    if (finalValue > 0)
                    {
                        AssignBucketToReport(intelReport, bucketName, finalValue);
                    }
                }

                return intelReport;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GenerateIntelReport), e);
                throw;
            }
        }

        #endregion


        #region Private Helper Methods

        /// <summary>
        /// Ensures the profile system has been initialized before use.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("IntelProfile system not initialized. Call InitializeProfiles() first.");
            }
        }

        /// <summary>
        /// Loads all profile definitions from game data.
        /// This method should be expanded to load from configuration files or data sources.
        /// </summary>
        private static void LoadProfileDefinitions()
        {
            // TODO: Load profile definitions from data files or configuration
            // For now, create basic example profiles for testing

            // Example: Tank Regiment profile
            var tankRegiment = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 400 },
                { WeaponSystems.TANK_T80B, 42 },
                { WeaponSystems.IFV_BMP2, 12 },
                { WeaponSystems.APC_BTR80, 8 },
                { WeaponSystems.SPA_2S1, 6 }
            };
            _profiles[IntelProfileTypes.SV_TR] = tankRegiment;

            // Example: Infantry Regiment profile  
            var motorRifleRegiment = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 2000 },
                { WeaponSystems.IFV_BMP2, 30 },
                { WeaponSystems.APC_BTR80, 20 },
                { WeaponSystems.SPA_2S1, 12 },
                { WeaponSystems.ATGM_GENERIC, 8 }
            };
            _profiles[IntelProfileTypes.SV_MRR] = motorRifleRegiment;

            // Example: Artillery Battalion profile
            var artilleryBattalion = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 200 },
                { WeaponSystems.SPA_2S3, 18 },
                { WeaponSystems.ROC_BM21, 6 },
                { WeaponSystems.APC_BTR80, 4 }
            };
            _profiles[IntelProfileTypes.SV_ART] = artilleryBattalion;

            // Add more profiles as needed...
        }

        /// <summary>
        /// Extracts the prefix from a weapon system name for bucket categorization.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to extract prefix from</param>
        /// <returns>The prefix portion of the weapon system name</returns>
        private static string GetWeaponSystemPrefix(WeaponSystems weaponSystem)
        {
            string weaponName = weaponSystem.ToString();
            int underscoreIndex = weaponName.IndexOf('_');
            return underscoreIndex >= 0 ? weaponName.Substring(0, underscoreIndex) : weaponName;
        }

        /// <summary>
        /// Maps weapon system prefixes to display bucket categories.
        /// </summary>
        /// <param name="prefix">The weapon system prefix</param>
        /// <returns>The display bucket name, or null if not mapped</returns>
        private static string MapPrefixToBucket(string prefix)
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

        /// <summary>
        /// Calculates the fog-of-war multiplier for a bucket based on spotted level.
        /// </summary>
        /// <param name="spottedLevel">The intelligence accuracy level</param>
        /// <returns>Multiplier to apply to bucket values</returns>
        private static float CalculateFogOfWarMultiplier(SpottedLevel spottedLevel)
        {
            return spottedLevel switch
            {
                SpottedLevel.Level4 => 1f, // Perfect intel - no distortion
                SpottedLevel.Level3 => GetRandomMultiplier(1f, 10f), // ±10% error
                SpottedLevel.Level2 => GetRandomMultiplier(1f, 30f), // ±30% error
                _ => 1f // Level1 and Level0 handled elsewhere
            };
        }

        /// <summary>
        /// Generates a random multiplier within the specified error range.
        /// </summary>
        /// <param name="errorRangeMin">Minimum error percentage</param>
        /// <param name="errorRangeMax">Maximum error percentage</param>
        /// <returns>Random multiplier for fog-of-war distortion</returns>
        private static float GetRandomMultiplier(float errorRangeMin, float errorRangeMax)
        {
            bool isPositiveDirection = UnityEngine.Random.Range(0f, 1f) >= 0.5f;
            float errorPercent = UnityEngine.Random.Range(errorRangeMin, errorRangeMax);
            return isPositiveDirection ? (1f + errorPercent / 100f) : (1f - errorPercent / 100f);
        }

        /// <summary>
        /// Assigns a bucket value to the appropriate property in the IntelReport.
        /// </summary>
        /// <param name="report">The IntelReport to update</param>
        /// <param name="bucketName">The bucket category name</param>
        /// <param name="value">The value to assign</param>
        private static void AssignBucketToReport(IntelReport report, string bucketName, int value)
        {
            switch (bucketName)
            {
                case "Men":
                    report.Men = value;
                    break;
                case "Tanks":
                    report.Tanks = value;
                    break;
                case "IFVs":
                    report.IFVs = value;
                    break;
                case "APCs":
                    report.APCs = value;
                    break;
                case "Recon":
                    report.RCNs = value;
                    break;
                case "Artillery":
                    report.ARTs = value;
                    break;
                case "Rocket Artillery":
                    report.ROCs = value;
                    break;
                case "Surface To Surface Missiles":
                    report.SSMs = value;
                    break;
                case "SAMs":
                    report.SAMs = value;
                    break;
                case "Anti-aircraft Artillery":
                    report.AAAs = value;
                    break;
                case "MANPADs":
                    report.MANPADs = value;
                    break;
                case "ATGMs":
                    report.ATGMs = value;
                    break;
                case "Attack Helicopters":
                    report.HEL = value;
                    break;
                case "Fighters":
                    report.ASFs = value;
                    break;
                case "Multirole":
                    report.MRFs = value;
                    break;
                case "Attack":
                    report.ATTs = value;
                    break;
                case "Bombers":
                    report.BMBs = value;
                    break;
                case "AWACS":
                    report.AWACS = value;
                    break;
                case "Recon Aircraft":
                    report.RCNAs = value;
                    break;
            }
        }

        #endregion
    }
}