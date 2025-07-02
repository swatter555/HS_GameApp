using System;
using System.Collections.Generic;
using HammerAndSickle.Services;

/*───────────────────────────────────────────────────────────────────────────────
  IntelProfile ─ static template repository for unit organizational intelligence
────────────────────────────────────────────────────────────────────────────────
 Summary
 ═══════
 • Holds an immutable lookup table that defines the *maximum* equipment
   composition for every unit type (one entry per **IntelProfileTypes** value).  
 • Generates fog-of-war filtered **IntelReport** snapshots, scaling counts by
   current strength and applying ±error based on *SpottedLevel*.  
 • Eliminates per-unit memory overhead—each **CombatUnit** stores only an enum
   reference to its template. :contentReference[oaicite:0]{index=0}

 Public properties
 ═════════════════
   bool IsInitialized { get; }                       // true after successful static init

 Constructors
 ═════════════
   // none – static class

 Public API (method signatures ⇢ purpose)
 ═══════════════════════════════════════
   public static void  InitializeProfiles()                                 // one-time load of all templates
   public static bool  HasProfile(IntelProfileTypes profileType)            // quick existence check
   public static int   GetWeaponSystemCount(IntelProfileTypes type,
                                            WeaponSystems ws)               // max count for a given WS
   public static IReadOnlyDictionary<WeaponSystems,int>
                       GetDefinedWeaponSystems(IntelProfileTypes type)      // full WS→count map
   public static IntelReport GenerateIntelReport(IntelProfileTypes type,
                                                 string unitName,
                                                 int currentHP,
                                                 Nationality nat,
                                                 CombatState state,
                                                 ExperienceLevel xp,
                                                 EfficiencyLevel eff,
                                                 SpottedLevel spot = SpottedLevel.Level1)
                                                                            // fog-of-war report builder

 Private helpers
 ═══════════════
   static void   EnsureInitialized()                         // guard against premature use
   static void   LoadProfileDefinitions()                    // populate _profiles (TODO: data-file driven)
   static string GetWeaponSystemPrefix(WeaponSystems ws)     // extract “TANK”, “IFV”, etc.
   static string MapPrefixToBucket(string prefix)            // prefix→GUI bucket
   static float  CalculateFogOfWarMultiplier(SpottedLevel s) // ±error based on intel level
   static float  GetRandomMultiplier(float min, float max)   // randomised ±% helper
   static void   AssignBucketToReport(IntelReport rpt,
                                      string bucket,
                                      int value)             // write into report fields

 Developer notes
 ═══════════════
 • **Thread-Safety** – Double-checked locking around *InitializeProfiles()* ensures
   safe concurrent startup. Once initialised, data are read-only and lock-free.  
 • **Fog-of-War Maths** – Error percentages come from *CUConstants*; tweak those
   constants to rebalance intel accuracy without touching this class.  
 • **Bucket Consistency** – *MapPrefixToBucket()* **must** remain in sync with the
   bucket properties in **IntelReport**. Add a new case whenever you introduce a
   new weapon-system prefix or display bucket.  
───────────────────────────────────────────────────────────────────────────────*/
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
                SpottedLevel.Level3 => GetRandomMultiplier(CUConstants.MIN_INTEL_ERROR, CUConstants.MODERATE_INTEL_ERROR),
                SpottedLevel.Level2 => GetRandomMultiplier(CUConstants.MIN_INTEL_ERROR, CUConstants.MAX_INTEL_ERROR), 
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