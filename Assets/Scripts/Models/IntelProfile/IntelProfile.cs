using HammerAndSickle.Services;
using System;
using System.Collections.Generic;

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

        
        public static IntelReport GenerateIntelReport(
            IntelProfileTypes profileType,
            string unitName,
            int currentHitPoints,
            Nationality nationality,
            DeploymentPosition deploymentPosition,
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
                    DeploymentPosition = deploymentPosition
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


        #region Profile Database

        /// <summary>
        /// Loads all profile definitions from game data.
        /// This method should be expanded to load from configuration files or data sources.
        /// </summary>
        private static void LoadProfileDefinitions()
        {
            #region Soviet APCs

            // Motor Rifle Regiment- BTR70 profile
            var mrrBTR70 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 2523 },
                { WeaponSystems.TANK_T55A, 40 },
                { WeaponSystems.APC_BTR70, 129 },
                { WeaponSystems.IFV_BMP1, 26 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 16 },
                { WeaponSystems.MANPAD_GENERIC, 30 },
            };
            _profiles[IntelProfileTypes.SV_MRR_BTR70] = mrrBTR70;

            // Motor Rifle Regiment- BTR80 profile
            var mrrBTR80 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 2523 },
                { WeaponSystems.TANK_T72A, 40 },
                { WeaponSystems.APC_BTR80, 129 },
                { WeaponSystems.IFV_BMP2, 26 },
                { WeaponSystems.SPAAA_ZSU23, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 16 },
                { WeaponSystems.MANPAD_GENERIC, 30 },
            };
            _profiles[IntelProfileTypes.SV_MRR_BTR80] = mrrBTR80;

            #endregion // Soviet APCs

            #region Soviet IFVs

            // Motor Rifle Regiment- BMP1 profile
            var mrrBMP1 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 2424 },
                { WeaponSystems.TANK_T55A, 40 },
                { WeaponSystems.IFV_BMP1, 129 },
                { WeaponSystems.APC_BTR70, 26 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 16 },
                { WeaponSystems.MANPAD_GENERIC, 30 },
            };
            _profiles[IntelProfileTypes.SV_MRR_BMP1] = mrrBMP1;

            // Motor Rifle Regiment- BMP2 profile
            var mrrBMP2 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 2424 },
                { WeaponSystems.TANK_T72A, 40 },
                { WeaponSystems.IFV_BMP2, 129 },
                { WeaponSystems.APC_BTR70, 26 },
                { WeaponSystems.SPAAA_ZSU23, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 16 },
                { WeaponSystems.MANPAD_GENERIC, 30 },
            };
            _profiles[IntelProfileTypes.SV_MRR_BMP2] = mrrBMP2;

            // Motor Rifle Regiment- BMP3 profile
            var mrrBMP3 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 2424 },
                { WeaponSystems.TANK_T80B, 40 },
                { WeaponSystems.IFV_BMP3, 129 },
                { WeaponSystems.APC_BTR80, 26 },
                { WeaponSystems.SPAAA_ZSU23, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 16 },
                { WeaponSystems.MANPAD_GENERIC, 30 },
            };
            _profiles[IntelProfileTypes.SV_MRR_BMP3] = mrrBMP3;

            #endregion // Soviet IFVs

            #region Soviet Tank Units

            // Tank Regiment- T55 profile
            var tr_T55 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1143 },
                { WeaponSystems.TANK_T55A, 94 },
                { WeaponSystems.IFV_BMP1, 45 },
                { WeaponSystems.APC_BTR70, 21 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 12 },
                { WeaponSystems.MANPAD_GENERIC, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T55] = tr_T55;

            // Tank Regiment- T64A profile
            var tr_T64A = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1143 },
                { WeaponSystems.TANK_T64A, 94 },
                { WeaponSystems.IFV_BMP2, 45 },
                { WeaponSystems.APC_BTR70, 21 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 12 },
                { WeaponSystems.MANPAD_GENERIC, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T64A] = tr_T64A;

            // Tank Regiment- T64B profile
            var tr_T64B = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1143 },
                { WeaponSystems.TANK_T64B, 94 },
                { WeaponSystems.IFV_BMP2, 45 },
                { WeaponSystems.APC_BTR80, 21 },
                { WeaponSystems.SPAAA_ZSU23, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 12 },
                { WeaponSystems.MANPAD_GENERIC, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T64B] = tr_T64B;

            // Tank Regiment- T72A profile
            var tr_T72A = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1143 },
                { WeaponSystems.TANK_T72A, 94 },
                { WeaponSystems.IFV_BMP1, 45 },
                { WeaponSystems.APC_BTR70, 21 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 12 },
                { WeaponSystems.MANPAD_GENERIC, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T72A] = tr_T72A;

            // Tank Regiment- T72B profile
            var tr_T72B = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1143 },
                { WeaponSystems.TANK_T72B, 94 },
                { WeaponSystems.IFV_BMP2, 45 },
                { WeaponSystems.APC_BTR80, 21 },
                { WeaponSystems.SPAAA_2K22, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 12 },
                { WeaponSystems.MANPAD_GENERIC, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T72B] = tr_T72B;

            // Tank Regiment- T80B profile
            var tr_T80B = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1143 },
                { WeaponSystems.TANK_T80B, 94 },
                { WeaponSystems.IFV_BMP2, 45 },
                { WeaponSystems.APC_BTR80, 21 },
                { WeaponSystems.SPAAA_ZSU23, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 12 },
                { WeaponSystems.MANPAD_GENERIC, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T80B] = tr_T80B;

            // Tank Regiment- T80U profile
            var tr_T80U = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1143 },
                { WeaponSystems.TANK_T80U, 94 },
                { WeaponSystems.IFV_BMP2, 45 },
                { WeaponSystems.APC_BTR80, 21 },
                { WeaponSystems.SPAAA_2K22, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 12 },
                { WeaponSystems.MANPAD_GENERIC, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T80U] = tr_T80U;

            // Tank Regiment- T80BV profile
            var tr_T80BV = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1143 },
                { WeaponSystems.TANK_T80BV, 94 },
                { WeaponSystems.IFV_BMP3, 45 },
                { WeaponSystems.APC_BTR80, 21 },
                { WeaponSystems.SPAAA_2K22, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.ATGM_GENERIC, 12 },
                { WeaponSystems.MANPAD_GENERIC, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T80BV] = tr_T80BV;

            #endregion // Soviet Tank Units

            #region Soviet Artillery Units

            // Soviet heavy towed artillery
            var sv_heavyart = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1100 },
                { WeaponSystems.ART_HEAVY_GENERIC, 72 },
            };
            _profiles[IntelProfileTypes.SV_AR_HVY] = sv_heavyart;

            // Soviet light towed artillery
            var sv_lightart = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1100 },
                { WeaponSystems.ART_LIGHT_GENERIC, 72 },
            };
            _profiles[IntelProfileTypes.SV_AR_LGT] = sv_lightart;

            // Soviet artillery regiment 2S1
            var sv_2s1 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1062 },
                { WeaponSystems.SPA_2S1, 36 },
            };
            _profiles[IntelProfileTypes.SV_AR_2S1] = sv_2s1;

            // Soviet artillery regiment 2S3
            var sv_2s3 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1062 },
                { WeaponSystems.SPA_2S3, 36 },
            };
            _profiles[IntelProfileTypes.SV_AR_2S3] = sv_2s3;

            // Soviet artillery regiment 2S5
            var sv_2s5 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1062 },
                { WeaponSystems.SPA_2S5, 36 },
            };
            _profiles[IntelProfileTypes.SV_AR_2S5] = sv_2s5;

            // Soviet artillery regiment 2S19
            var sv_2s19 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1062 },
                { WeaponSystems.SPA_2S19, 36 },
            };
            _profiles[IntelProfileTypes.SV_AR_2S19] = sv_2s19;

            // Soviet rocket artillery regiment BM-21
            var sv_bm21 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1200 },
                { WeaponSystems.ROC_BM21, 54 },
            };
            _profiles[IntelProfileTypes.SV_ROC_BM21] = sv_bm21;

            // Soviet rocket artillery regiment BM-27
            var sv_bm27 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1200 },
                { WeaponSystems.ROC_BM27, 54 },
            };
            _profiles[IntelProfileTypes.SV_ROC_BM27] = sv_bm27;

            // Soviet rocket artillery regiment BM-30
            var sv_bm30 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 1200 },
                { WeaponSystems.ROC_BM30, 54 },
            };
            _profiles[IntelProfileTypes.SV_ROC_BM30] = sv_bm30;

            // Soviet ballistic missile regiment SCUD
            var sv_scud = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 800 },
                { WeaponSystems.SSM_SCUD, 18 },
            };
            _profiles[IntelProfileTypes.SV_BM_SCUDB] = sv_scud;

            #endregion // Soviet Artillery Units

            #region Soviet air mobile units

            // Soviet air mobile regiment MTLB
            var aar_MTLB = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV,       1800 },  // 3× air‑assault battalions + HQ & support
                { WeaponSystems.APC_MTLB,           93 },  // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,           6 },  // recon platoon
                { WeaponSystems.ART_LIGHT_GENERIC,  12 },  // 122 mm artillery battery
                { WeaponSystems.ATGM_GENERIC,       12 },  // mixed AT‑4/AT‑5 sections
                { WeaponSystems.MANPAD_GENERIC,     24 },  // SA‑14/16 squads
                { WeaponSystems.SPAAA_ZSU23,         2 },  // ZSU‑23‑4 Shilka (reduced strength)
                { WeaponSystems.TRANSHELO_MI8,       48 }, // 2× transport helicopter squadrons
            };
            _profiles[IntelProfileTypes.SV_AAR_MTLB] = aar_MTLB;

            // Soviet air mobile regiment BTR70
            var aar_BTR70 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV,       1800 },  // 3× air‑assault battalions + HQ & support
                { WeaponSystems.APC_BTR70,          93 },  // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,           6 },  // recon platoon
                { WeaponSystems.ART_LIGHT_GENERIC,  12 },  // 122 mm artillery battery
                { WeaponSystems.ATGM_GENERIC,       12 },  // mixed AT‑4/AT‑5 sections
                { WeaponSystems.MANPAD_GENERIC,     24 },  // SA‑14/16 squads
                { WeaponSystems.SPAAA_ZSU23,         2 },  // ZSU‑23‑4 Shilka (reduced strength)
                { WeaponSystems.TRANSHELO_MI8,      48 },  // 2× transport helicopter squadrons
            };
            _profiles[IntelProfileTypes.SV_AAR_BTR70] = aar_BTR70;

            // Soviet air mobile regiment BTR80
            var aar_BTR80 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV,       1800 },  // 3× air‑assault battalions + HQ & support
                { WeaponSystems.APC_BTR80,          93 },  // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,           6 },  // recon platoon
                { WeaponSystems.ART_LIGHT_GENERIC,  12 },  // 122 mm artillery battery
                { WeaponSystems.ATGM_GENERIC,       12 },  // mixed AT‑4/AT‑5 sections
                { WeaponSystems.MANPAD_GENERIC,     24 },  // SA‑14/16 squads
                { WeaponSystems.SPAAA_ZSU23,         2 },  // ZSU‑23‑4 Shilka (reduced strength)
                { WeaponSystems.TRANSHELO_MI8,      48 },  // 2× transport helicopter squadrons
            };
            _profiles[IntelProfileTypes.SV_AAR_BTR80] = aar_BTR80;

            #endregion // Soviet air mobile units

            #region Soviet VDV units

            // VDV airborne regiment – BMD‑1 (mid‑1980s baseline)
            // Sources: TO&E 38‑500 series; typical regiment strength ~1 800 men, three
            // BMD battalions (31 vehicles each) plus regimental assets.
            var vdv_BMD1 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.AB_INF_SV,          1800 }, // 3× airborne battalions + regt HQ/support
                { WeaponSystems.IFV_BMD1,             93 }, // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,             6 }, // recon platoon (BRDM‑2)
                { WeaponSystems.ART_LIGHT_GENERIC,    18 }, // 120 mm 2S9 Nona‑S battery
                { WeaponSystems.ATGM_GENERIC,         12 }, // AT‑4/AT‑5 sections
                { WeaponSystems.MANPAD_GENERIC,       24 }, // SA‑14/16 squads
                { WeaponSystems.SPAAA_ZSU23,           2 }, // ZSU‑23‑4 Shilka (regimental AD)
            };
            _profiles[IntelProfileTypes.SV_VDV_BMD1] = vdv_BMD1;

            // VDV airborne regiment – BMD‑2 (mid‑1980s baseline)
            var vdv_BMD2 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.AB_INF_SV,          1800 }, // 3× airborne battalions + regt HQ/support
                { WeaponSystems.IFV_BMD2,             93 }, // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,             6 }, // recon platoon (BRDM‑2)
                { WeaponSystems.ART_LIGHT_GENERIC,    18 }, // 120 mm 2S9 Nona‑S battery
                { WeaponSystems.ATGM_GENERIC,         12 }, // AT‑4/AT‑5 sections
                { WeaponSystems.MANPAD_GENERIC,       24 }, // SA‑14/16 squads
                { WeaponSystems.SPAAA_ZSU23,           2 }, // ZSU‑23‑4 Shilka (regimental AD)
            };
            _profiles[IntelProfileTypes.SV_VDV_BMD2] = vdv_BMD2;

            // VDV airborne regiment – BMD‑3 (mid‑1980s baseline)
            var vdv_BMD3 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.AB_INF_SV,          1800 }, // 3× airborne battalions + regt HQ/support
                { WeaponSystems.IFV_BMD3,             93 }, // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,             6 }, // recon platoon (BRDM‑2)
                { WeaponSystems.ART_LIGHT_GENERIC,    18 }, // 120 mm 2S9 Nona‑S battery
                { WeaponSystems.ATGM_GENERIC,         12 }, // AT‑4/AT‑5 sections
                { WeaponSystems.MANPAD_GENERIC,       24 }, // SA‑14/16 squads
                { WeaponSystems.SPAAA_ZSU23,           2 }, // ZSU‑23‑4 Shilka (regimental AD)
            };
            _profiles[IntelProfileTypes.SV_VDV_BMD3] = vdv_BMD3;

            #endregion // Soviet VDV units

            #region Soviet air units

            // Fighter Regiment- MiG-29 profile
            var fighterRegiment_Mig29 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ASF_MIG29, 36 }
            };
            _profiles[IntelProfileTypes.SV_FR_MIG29] = fighterRegiment_Mig29;

            #endregion // Soviet air units

            #region Soviet bases

            // Suppy Depot profile
            var sv_supplydepot = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 3000 }
            };
            _profiles[IntelProfileTypes.SV_DEPOT] = sv_supplydepot;

            // Airbase profile
            var sv_airbase = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.REG_INF_SV, 2500 }
            };
            _profiles[IntelProfileTypes.SV_AIRB] = sv_airbase;

            #endregion // Soviet bases
        }

        #endregion // Profile Database
    }
}