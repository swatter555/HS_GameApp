using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using static Codice.CM.Common.Purge.PurgeReport;

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
            //-------------------------------------------------------------//
            #region Soviet APCs

            // Motor Rifle Regiment- BTR70 profile
            var mrrBTR70 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2523 },
                { WeaponSystems.TANK_T55A, 40 },
                { WeaponSystems.APC_BTR70, 129 },
                { WeaponSystems.IFV_BMP1, 26 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 16 },
                { WeaponSystems.GENERIC_MANPAD, 30 },
            };
            _profiles[IntelProfileTypes.SV_MRR_BTR70] = mrrBTR70;

            // Motor Rifle Regiment- BTR80 profile
            var mrrBTR80 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2523 },
                { WeaponSystems.TANK_T72A, 40 },
                { WeaponSystems.APC_BTR80, 129 },
                { WeaponSystems.IFV_BMP2, 26 },
                { WeaponSystems.SPAAA_ZSU23, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 16 },
                { WeaponSystems.GENERIC_MANPAD, 30 },
            };
            _profiles[IntelProfileTypes.SV_MRR_BTR80] = mrrBTR80;

            #endregion // Soviet APCs

            #region Soviet IFVs

            // Motor Rifle Regiment- BMP1 profile
            var mrrBMP1 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2424 },
                { WeaponSystems.TANK_T55A, 40 },
                { WeaponSystems.IFV_BMP1, 129 },
                { WeaponSystems.APC_BTR70, 26 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 16 },
                { WeaponSystems.GENERIC_MANPAD, 30 },
            };
            _profiles[IntelProfileTypes.SV_MRR_BMP1] = mrrBMP1;

            // Motor Rifle Regiment- BMP2 profile
            var mrrBMP2 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2424 },
                { WeaponSystems.TANK_T72A, 40 },
                { WeaponSystems.IFV_BMP2, 129 },
                { WeaponSystems.APC_BTR70, 26 },
                { WeaponSystems.SPAAA_ZSU23, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 16 },
                { WeaponSystems.GENERIC_MANPAD, 30 },
            };
            _profiles[IntelProfileTypes.SV_MRR_BMP2] = mrrBMP2;

            // Motor Rifle Regiment- BMP3 profile
            var mrrBMP3 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2424 },
                { WeaponSystems.TANK_T80B, 40 },
                { WeaponSystems.IFV_BMP3, 129 },
                { WeaponSystems.APC_BTR80, 26 },
                { WeaponSystems.SPAAA_ZSU23, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 16 },
                { WeaponSystems.GENERIC_MANPAD, 30 },
            };
            _profiles[IntelProfileTypes.SV_MRR_BMP3] = mrrBMP3;

            #endregion // Soviet IFVs

            #region Soviet Tank Units

            // Tank Regiment- T55 profile
            var tr_T55 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1143 },
                { WeaponSystems.TANK_T55A, 94 },
                { WeaponSystems.IFV_BMP1, 45 },
                { WeaponSystems.APC_BTR70, 21 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T55] = tr_T55;

            // Tank Regiment- T64A profile
            var tr_T64A = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1143 },
                { WeaponSystems.TANK_T64A, 94 },
                { WeaponSystems.IFV_BMP2, 45 },
                { WeaponSystems.APC_BTR70, 21 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T64A] = tr_T64A;

            // Tank Regiment- T64B profile
            var tr_T64B = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1143 },
                { WeaponSystems.TANK_T64B, 94 },
                { WeaponSystems.IFV_BMP2, 45 },
                { WeaponSystems.APC_BTR80, 21 },
                { WeaponSystems.SPAAA_ZSU23, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T64B] = tr_T64B;

            // Tank Regiment- T72A profile
            var tr_T72A = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1143 },
                { WeaponSystems.TANK_T72A, 94 },
                { WeaponSystems.IFV_BMP1, 45 },
                { WeaponSystems.APC_BTR70, 21 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T72A] = tr_T72A;

            // Tank Regiment- T72B profile
            var tr_T72B = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1143 },
                { WeaponSystems.TANK_T72B, 94 },
                { WeaponSystems.IFV_BMP2, 45 },
                { WeaponSystems.APC_BTR80, 21 },
                { WeaponSystems.SPAAA_2K22, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T72B] = tr_T72B;

            // Tank Regiment- T80B profile
            var tr_T80B = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1143 },
                { WeaponSystems.TANK_T80B, 94 },
                { WeaponSystems.IFV_BMP2, 45 },
                { WeaponSystems.APC_BTR80, 21 },
                { WeaponSystems.SPAAA_ZSU23, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T80B] = tr_T80B;

            // Tank Regiment- T80U profile
            var tr_T80U = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1143 },
                { WeaponSystems.TANK_T80U, 94 },
                { WeaponSystems.IFV_BMP2, 45 },
                { WeaponSystems.APC_BTR80, 21 },
                { WeaponSystems.SPAAA_2K22, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T80U] = tr_T80U;

            // Tank Regiment- T80BV profile
            var tr_T80BV = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1143 },
                { WeaponSystems.TANK_T80BV, 94 },
                { WeaponSystems.IFV_BMP3, 45 },
                { WeaponSystems.APC_BTR80, 21 },
                { WeaponSystems.SPAAA_2K22, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 12 },
            };
            _profiles[IntelProfileTypes.SV_TR_T80BV] = tr_T80BV;

            #endregion // Soviet Tank Units

            #region Soviet Artillery Units

            // Soviet heavy towed artillery
            var sv_heavyart = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1100 },
                { WeaponSystems.GENERIC_ART_HEAVY, 72 },
            };
            _profiles[IntelProfileTypes.SV_AR_HVY] = sv_heavyart;

            // Soviet light towed artillery
            var sv_lightart = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1100 },
                { WeaponSystems.GENERIC_ART_LIGHT, 72 },
            };
            _profiles[IntelProfileTypes.SV_AR_LGT] = sv_lightart;

            // Soviet artillery regiment 2S1
            var sv_2s1 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1062 },
                { WeaponSystems.SPA_2S1, 36 },
            };
            _profiles[IntelProfileTypes.SV_AR_2S1] = sv_2s1;

            // Soviet artillery regiment 2S3
            var sv_2s3 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1062 },
                { WeaponSystems.SPA_2S3, 36 },
            };
            _profiles[IntelProfileTypes.SV_AR_2S3] = sv_2s3;

            // Soviet artillery regiment 2S5
            var sv_2s5 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1062 },
                { WeaponSystems.SPA_2S5, 36 },
            };
            _profiles[IntelProfileTypes.SV_AR_2S5] = sv_2s5;

            // Soviet artillery regiment 2S19
            var sv_2s19 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1062 },
                { WeaponSystems.SPA_2S19, 36 },
            };
            _profiles[IntelProfileTypes.SV_AR_2S19] = sv_2s19;

            // Soviet rocket artillery regiment BM-21
            var sv_bm21 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1200 },
                { WeaponSystems.ROC_BM21, 54 },
            };
            _profiles[IntelProfileTypes.SV_ROC_BM21] = sv_bm21;

            // Soviet rocket artillery regiment BM-27
            var sv_bm27 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1200 },
                { WeaponSystems.ROC_BM27, 54 },
            };
            _profiles[IntelProfileTypes.SV_ROC_BM27] = sv_bm27;

            // Soviet rocket artillery regiment BM-30
            var sv_bm30 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1200 },
                { WeaponSystems.ROC_BM30, 54 },
            };
            _profiles[IntelProfileTypes.SV_ROC_BM30] = sv_bm30;

            // Soviet ballistic missile regiment SCUD
            var sv_scud = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 800 },
                { WeaponSystems.SSM_SCUD, 18 },
            };
            _profiles[IntelProfileTypes.SV_BM_SCUDB] = sv_scud;

            #endregion // Soviet Artillery Units

            #region Soviet air mobile units

            // Soviet air mobile regiment MTLB
            var aar_MTLB = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_AM,           2300 },   // 3× air‑assault battalions + HQ & support
                { WeaponSystems.APC_MTLB,           68 },  // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,          13 },  // recon platoon
                { WeaponSystems.GENERIC_ART_LIGHT,  18 },  // 122 mm artillery battery
                { WeaponSystems.GENERIC_ATGM,       14 },  // mixed AT‑4/AT‑5 sections
                { WeaponSystems.GENERIC_MANPAD,     45 },  // SA‑14/16 squads
                { WeaponSystems.GENERIC_AAA,         2 },  // ZSU‑23‑4 Shilka (reduced strength)
                { WeaponSystems.HEL_MI8T,     166 },  // 2× transport helicopter squadrons
            };
            _profiles[IntelProfileTypes.SV_AAR_MTLB] = aar_MTLB;

            // Soviet air mobile regiment BMD1
            var aar_BMD1 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_AM,           2300 },   // 3× air‑assault battalions + HQ & support
                { WeaponSystems.IFV_BMD1,           68 },  // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,          13 },  // recon platoon
                { WeaponSystems.GENERIC_ART_LIGHT,  18 },  // 122 mm artillery battery
                { WeaponSystems.GENERIC_ATGM,       14 },  // mixed AT‑4/AT‑5 sections
                { WeaponSystems.GENERIC_MANPAD,     45 },  // SA‑14/16 squads
                { WeaponSystems.GENERIC_AAA,         2 },  // ZSU‑23‑4 Shilka (reduced strength)
                { WeaponSystems.HEL_MI8T,     166 },  // 2× transport helicopter squadrons
            };
            _profiles[IntelProfileTypes.SV_AAR_BMD1] = aar_BMD1;

            // Soviet air mobile regiment BTR80
            var aar_BMD2 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_AM,           2300 },   // 3× air‑assault battalions + HQ & support
                { WeaponSystems.IFV_BMD2,           68 },  // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,          13 },  // recon platoon
                { WeaponSystems.GENERIC_ART_LIGHT,  18 },  // 122 mm artillery battery
                { WeaponSystems.GENERIC_ATGM,       14 },  // mixed AT‑4/AT‑5 sections
                { WeaponSystems.GENERIC_MANPAD,     45 },  // SA‑14/16 squads
                { WeaponSystems.GENERIC_AAA,         2 },  // ZSU‑23‑4 Shilka (reduced strength)
                { WeaponSystems.HEL_MI8T,     166 },  // 2× transport helicopter squadrons
            };
            _profiles[IntelProfileTypes.SV_AAR_BMD2] = aar_BMD2;

            // Soviet air mobile regiment BMD3
            var aar_BMD3 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_AM,           2300 },   // 3× air‑assault battalions + HQ & support
                { WeaponSystems.IFV_BMD3,           68 },  // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,          13 },  // recon platoon
                { WeaponSystems.GENERIC_ART_LIGHT,  18 },  // 122 mm artillery battery
                { WeaponSystems.GENERIC_ATGM,       14 },  // mixed AT‑4/AT‑5 sections
                { WeaponSystems.GENERIC_MANPAD,     45 },  // SA‑14/16 squads
                { WeaponSystems.GENERIC_AAA,         2 },  // ZSU‑23‑4 Shilka (reduced strength)
                { WeaponSystems.HEL_MI8T,     166 },  // 2× transport helicopter squadrons
            };
            _profiles[IntelProfileTypes.SV_AAR_BMD3] = aar_BMD3;

            #endregion // Soviet air mobile units

            #region Soviet VDV units

            // VDV airborne regiment – BMD‑1 (mid‑1980s baseline)
            // Sources: TO&E 38‑500 series; typical regiment strength ~1 800 men, three
            // BMD battalions (31 vehicles each) plus regimental assets.
            var vdv_BMD1 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_AB,             2250 }, // 3× airborne battalions + regt HQ/support
                { WeaponSystems.IFV_BMD1,             93 }, // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,             6 }, // recon platoon (BRDM‑2)
                { WeaponSystems.GENERIC_ART_LIGHT,    18 }, // 120 mm 2S9 Nona‑S battery
                { WeaponSystems.GENERIC_ATGM,         12 }, // AT‑4/AT‑5 sections
                { WeaponSystems.GENERIC_MANPAD,       45 }, // SA‑14/16 squads
                { WeaponSystems.GENERIC_AAA,           6 },
            };
            _profiles[IntelProfileTypes.SV_VDV_BMD1] = vdv_BMD1;

            // VDV airborne regiment – BMD‑2 (mid‑1980s baseline)
            var vdv_BMD2 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_AB,             2250 }, // 3× airborne battalions + regt HQ/support
                { WeaponSystems.IFV_BMD2,             93 }, // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,             6 }, // recon platoon (BRDM‑2)
                { WeaponSystems.GENERIC_ART_LIGHT,    18 }, // 120 mm 2S9 Nona‑S battery
                { WeaponSystems.GENERIC_ATGM,         12 }, // AT‑4/AT‑5 sections
                { WeaponSystems.GENERIC_MANPAD,       45 }, // SA‑14/16 squads
                { WeaponSystems.GENERIC_AAA,           6 },
            };
            _profiles[IntelProfileTypes.SV_VDV_BMD2] = vdv_BMD2;

            // VDV airborne regiment – BMD‑3 (mid‑1980s baseline)
            var vdv_BMD3 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_AB,             2250 }, // 3× airborne battalions + regt HQ/support
                { WeaponSystems.IFV_BMD3,             93 }, // 31 per battalion (3 rifle coys + HQ)
                { WeaponSystems.RCN_BRDM2,             6 }, // recon platoon (BRDM‑2)
                { WeaponSystems.GENERIC_ART_LIGHT,    18 }, // 120 mm 2S9 Nona‑S battery
                { WeaponSystems.GENERIC_ATGM,         12 }, // AT‑4/AT‑5 sections
                { WeaponSystems.GENERIC_MANPAD,       45 }, // SA‑14/16 squads
                { WeaponSystems.GENERIC_AAA,           6 },
            };
            _profiles[IntelProfileTypes.SV_VDV_BMD3] = vdv_BMD3;

            // VDV artillery regiment
            var vdv_artreg = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_AB,             1200 }, // regt HQ/support
                { WeaponSystems.GENERIC_ART_LIGHT,    36 }, // 120 mm 2S9 Nona‑S battery
            };
            _profiles[IntelProfileTypes.SV_VDV_ART] = vdv_artreg;

            // VDV airborne regiment – support (mid‑1980s baseline)
            var vdv_sup = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_AB,             1150 },
                { WeaponSystems.TANK_T55A,            31 },
                { WeaponSystems.RCN_BRDM2AT,          18 }, 
                { WeaponSystems.GENERIC_ART_LIGHT,     6 }, 
                { WeaponSystems.GENERIC_ATGM,         12 }, 
                { WeaponSystems.GENERIC_MANPAD,       12 }, 
                { WeaponSystems.GENERIC_AAA,           2 },
            };
            _profiles[IntelProfileTypes.SV_VDV_SUP] = vdv_sup;

            #endregion // Soviet VDV units

            #region Soviet naval infantry units

            // Naval Assault Brigade- T55 profile
            var navT55 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2750 },
                { WeaponSystems.TANK_T55A, 44 },
                { WeaponSystems.IFV_BMP1, 44 },
                { WeaponSystems.APC_BTR70, 145 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 36 },
            };
            _profiles[IntelProfileTypes.SV_NAV_T55] = navT55;

            // Naval Assault Brigade- T72 profile
            var navT72 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2750 },
                { WeaponSystems.TANK_T72A, 44 },
                { WeaponSystems.IFV_BMP2, 44 },
                { WeaponSystems.APC_BTR70, 145 },
                { WeaponSystems.SPAAA_ZSU23, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 36 },
            };
            _profiles[IntelProfileTypes.SV_NAV_T72] = navT72;

            // Naval Assault Brigade- T80 profile
            var navT80 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2750 },
                { WeaponSystems.TANK_T80U, 44 },
                { WeaponSystems.IFV_BMP3, 44 },
                { WeaponSystems.APC_BTR80, 145 },
                { WeaponSystems.SPAAA_2K22, 4},
                { WeaponSystems.SPSAM_9K31, 4 },
                { WeaponSystems.SPA_2S1, 18 },
                { WeaponSystems.RCN_BRDM2, 12 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 36 },
            };
            _profiles[IntelProfileTypes.SV_NAV_T80] = navT80;

            #endregion // Soviet naval infantry units

            #region Soviet engineer units

            // Soviet engineer battalion
            var svengineers = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_ENG, 340 },
                { WeaponSystems.APC_BTR70, 20 },
            };
            _profiles[IntelProfileTypes.SV_ENG] = svengineers;

            #endregion // Soviet engineer units

            #region Soviet recon units

            // Soviet recon regiment
            var svreconrgt = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1020 },
                { WeaponSystems.TANK_T55A, 18 },
                { WeaponSystems.IFV_BMP1, 36 },
                { WeaponSystems.APC_BTR70, 42 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.RCN_BRDM2, 54 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 12 },
            };
            _profiles[IntelProfileTypes.SV_RCR] = svreconrgt;

            // Soviet recon regiment AT
            var svreconrgtAT = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1020 },
                { WeaponSystems.TANK_T72A, 18 },
                { WeaponSystems.IFV_BMP2, 36 },
                { WeaponSystems.APC_BTR80, 42 },
                { WeaponSystems.SPAAA_ZSU57, 4},
                { WeaponSystems.RCN_BRDM2AT, 54 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 12 },
            };
            _profiles[IntelProfileTypes.SV_RCR_AT] = svreconrgtAT;

            #endregion // Soviet recon units

            #region Soviet air defence units

            // AAA Regiment- AAA profile
            var svadr_AAA = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 600 },
                { WeaponSystems.GENERIC_AAA, 18 },
                { WeaponSystems.GENERIC_MANPAD, 12 }
            };
            _profiles[IntelProfileTypes.SV_ADR_AAA] = svadr_AAA;

            // AAA Regiment- ZSU-57 profile
            var svadr_ZSU57 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 600 },
                { WeaponSystems.SPAAA_ZSU57, 18 },
                { WeaponSystems.GENERIC_MANPAD, 12 }
            };
            _profiles[IntelProfileTypes.SV_ADR_ZSU57] = svadr_ZSU57;

            // AAA Regiment- ZSU-23 profile
            var svadr_ZSU23 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 600 },
                { WeaponSystems.SPAAA_ZSU23, 18 },
                { WeaponSystems.GENERIC_MANPAD, 12 }
            };
            _profiles[IntelProfileTypes.SV_ADR_ZSU57] = svadr_ZSU23;

            // AAA Regiment- 2K22 profile
            var svadr_2K22 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 600 },
                { WeaponSystems.SPAAA_2K22, 18 },
                { WeaponSystems.GENERIC_MANPAD, 12 }
            };
            _profiles[IntelProfileTypes.SV_ADR_2K22] = svadr_2K22;

            #endregion // Soviet air defence units

            #region Soviet SAM units

            // SAM Regiment- S75 profile
            var samS75 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 750 },
                { WeaponSystems.SAM_S75, 18 },
                { WeaponSystems.GENERIC_MANPAD, 12 }
            };
            _profiles[IntelProfileTypes.SV_SAM_S75] = samS75;

            // SAM Regiment- S125 profile
            var samS125 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 750 },
                { WeaponSystems.SAM_S125, 18 },
                { WeaponSystems.GENERIC_MANPAD, 12 }
            };
            _profiles[IntelProfileTypes.SV_SAM_S125] = samS125;

            // SAM Regiment- S300 profile
            var samS300 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 750 },
                { WeaponSystems.SAM_S300, 18 },
                { WeaponSystems.GENERIC_MANPAD, 12 }
            };
            _profiles[IntelProfileTypes.SV_SAM_S300] = samS300;

            #endregion // Soviet SAM units

            #region Soviet attack helicopter units

            // Attack regiment- Mi-8T profile
            var helo_MI8T = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.HEL_MI8AT, 54 }
            };
            _profiles[IntelProfileTypes.SV_HEL_MI8AT] = helo_MI8T;

            // Attack regiment- Mi-24D profile
            var helo_MI24D = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.HEL_MI24D, 54 }
            };
            _profiles[IntelProfileTypes.SV_HEL_MI24D] = helo_MI24D;

            // Attack regiment- Mi-24V profile
            var helo_MI24V = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.HEL_MI24V, 54 }
            };
            _profiles[IntelProfileTypes.SV_HEL_MI24V] = helo_MI24V;

            // Attack regiment- Mi-28 profile
            var helo_MI28 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.HEL_MI28, 54 }
            };
            _profiles[IntelProfileTypes.SV_HEL_MI28] = helo_MI28;

            #endregion

            #region Spetznaz units

            // Soviet Spetznaz
            var spetz = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_SPEC, 1200 },
                { WeaponSystems.GENERIC_ATGM, 12 },
                { WeaponSystems.GENERIC_MANPAD, 12 },
            };
            _profiles[IntelProfileTypes.SV_GRU] = spetz;

            #endregion // Spetznaz units

            #region Soviet air units

            // Soviet AWACS Regiment
            var awacsRegiment = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.AWACS_A50, 6 }
            };
            _profiles[IntelProfileTypes.SV_AWACS_A50] = awacsRegiment;

            // Fighter Regiment- MiG-21 profile
            var fighterRegiment_Mig21 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ASF_MIG21, 36 }
            };
            _profiles[IntelProfileTypes.SV_FR_MIG21] = fighterRegiment_Mig21;

            // Fighter Regiment- MiG-23 profile
            var fighterRegiment_Mig23 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ASF_MIG23, 36 }
            };
            _profiles[IntelProfileTypes.SV_FR_MIG23] = fighterRegiment_Mig23;

            // Fighter Regiment- MiG-25 profile
            var fighterRegiment_Mig25 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ASF_MIG25, 36 }
            };
            _profiles[IntelProfileTypes.SV_FR_MIG25] = fighterRegiment_Mig25;

            // Fighter Regiment- MiG-29 profile
            var fighterRegiment_Mig29 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ASF_MIG29, 36 }
            };
            _profiles[IntelProfileTypes.SV_FR_MIG29] = fighterRegiment_Mig29;

            // Fighter Regiment- Mig-31 profile
            var fighterRegiment_Mig31 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ASF_MIG31, 36 }
            };
            _profiles[IntelProfileTypes.SV_FR_MIG31] = fighterRegiment_Mig31;

            // Fighter Regiment- Su-27 profile
            var fighterRegiment_Su27 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ASF_SU27, 36 }
            };
            _profiles[IntelProfileTypes.SV_FR_SU27] = fighterRegiment_Su27;

            // Fighter Regiment- SU-47 profile
            var fighterRegiment_Su47 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ASF_SU47, 36 }
            };
            _profiles[IntelProfileTypes.SV_FR_SU47] = fighterRegiment_Su47;

            // Fighter Regiment- Mig-27
            var fighterRegiment_Mig27 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.MRF_MIG27, 36 }
            };
            _profiles[IntelProfileTypes.SV_MR_MIG27] = fighterRegiment_Mig27;

            // Attack Regiment- Su-25 profile
            var attackRegiment_Su25 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ATT_SU25, 36 }
            };
            _profiles[IntelProfileTypes.SV_AR_SU25] = attackRegiment_Su25;

            // Attack Regiment- Su-25B profile
            var attackRegiment_Su25B = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ATT_SU25B, 36 }
            };
            _profiles[IntelProfileTypes.SV_AR_SU25B] = attackRegiment_Su25B;

            // Bomber Regiment- SU-24 profile
            var bomberRegiment_Su24 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.BMB_SU24, 36 }
            };
            _profiles[IntelProfileTypes.SV_BR_SU24] = bomberRegiment_Su24;

            // Bomber Regiment- TU-16
            var bomberRegiment_Tu16 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.BMB_TU16, 24 }
            };
            _profiles[IntelProfileTypes.SV_BR_TU16] = bomberRegiment_Tu16;

            // Bomber Regiment- TU-22
            var bomberRegiment_Tu22 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.BMB_TU22, 24 }
            };
            _profiles[IntelProfileTypes.SV_BR_TU22] = bomberRegiment_Tu22;

            // Bomber Regiment- TU-22M3
            var bomberRegiment_Tu22M3 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.BMB_TU22M3, 24 }
            };
            _profiles[IntelProfileTypes.SV_BR_TU22M3] = bomberRegiment_Tu22M3;

            // Air Recon Regiment- MiG-25R profile
            var airRecon_Mig25R = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.RCNA_MIG25R, 12 }
            };
            _profiles[IntelProfileTypes.SV_RR_MIG25R] = airRecon_Mig25R;

            #endregion // Soviet air units

            #region Soviet bases

            // Suppy Depot profile
            var sv_supplydepot = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 3000 }
            };
            _profiles[IntelProfileTypes.SV_DEPOT] = sv_supplydepot;

            // Airbase profile
            var sv_airbase = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2500 }
            };
            _profiles[IntelProfileTypes.SV_AIRB] = sv_airbase;

            // Regular base profile
            var sv_regularbase = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1500 }
            };
            _profiles[IntelProfileTypes.SV_BASE] = sv_regularbase;

            #endregion // Soviet bases

            //-------------------------------------------------------------//

            #region US Profiles

            // US Armored Brigade - M1A1 Abrams (Division 86 structure)
            var us_armoredBde_M1 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2200 },
                { WeaponSystems.TANK_M1, 116 },  // 2 × Tank BN (58 each)
                { WeaponSystems.IFV_M2, 54 },    // 1 × Mech Inf BN (54 Bradleys)
                { WeaponSystems.IFV_M3, 18 },    // Scout vehicles across battalions + brigade recon
                { WeaponSystems.APC_M113, 32 },  // Command posts, medical, maintenance vehicles
                { WeaponSystems.GENERIC_ATGM, 32 }, // TOW missiles (Bradley + dismounted teams)
                { WeaponSystems.GENERIC_MANPAD, 18 }, // Stinger teams distributed across brigade
                { WeaponSystems.SPA_M109, 18 },  // Direct support artillery battalion
            };
            _profiles[IntelProfileTypes.US_ARMORED_BDE_M1] = us_armoredBde_M1;

            // US Armored Brigade - M60A3 (Division 86 structure)
            var us_armoredBde_M60A3 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2200 },
                { WeaponSystems.TANK_M60A3, 116 },  // 2 × Tank BN (58 each)
                { WeaponSystems.IFV_M2, 54 },       // 1 × Mech Inf BN (54 Bradleys)
                { WeaponSystems.IFV_M3, 18 },       // Scout vehicles across battalions + brigade recon
                { WeaponSystems.APC_M113, 32 },     // Command posts, medical, maintenance vehicles
                { WeaponSystems.GENERIC_ATGM, 32 }, // TOW missiles (Bradley + dismounted teams)
                { WeaponSystems.GENERIC_MANPAD, 18 }, // Stinger teams distributed across brigade
                { WeaponSystems.SPA_M109, 18 },     // Direct support artillery battalion
            };
            _profiles[IntelProfileTypes.US_ARMORED_BDE_M60A3] = us_armoredBde_M60A3;

            // US Heavy Mechanized Brigade - M1A1 Abrams (Division 86 structure)
            var us_heavyMechBde_M1 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2800 },
                { WeaponSystems.TANK_M1, 58 },      // 1 × Tank BN (58 tanks)
                { WeaponSystems.IFV_M2, 108 },      // 2 × Mech Inf BN (54 Bradleys each)
                { WeaponSystems.IFV_M3, 18 },       // Scout vehicles across battalions + brigade recon
                { WeaponSystems.APC_M113, 32 },     // Command posts, medical, maintenance vehicles
                { WeaponSystems.GENERIC_ATGM, 40 }, // TOW missiles (Bradley + dismounted teams)
                { WeaponSystems.GENERIC_MANPAD, 22 }, // Stinger teams distributed across brigade
                { WeaponSystems.SPA_M109, 18 },     // Direct support artillery battalion
            };
            _profiles[IntelProfileTypes.US_HEAVY_MECH_BDE_M1] = us_heavyMechBde_M1;

            // US Heavy Mechanized Brigade - M60A3 (Division 86 structure)
            var us_heavyMechBde_M60A3 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 2800 },
                { WeaponSystems.TANK_M60A3, 58 },   // 1 × Tank BN (58 tanks)
                { WeaponSystems.IFV_M2, 108 },      // 2 × Mech Inf BN (54 Bradleys each)
                { WeaponSystems.IFV_M3, 18 },       // Scout vehicles across battalions + brigade recon
                { WeaponSystems.APC_M113, 32 },     // Command posts, medical, maintenance vehicles
                { WeaponSystems.GENERIC_ATGM, 40 }, // TOW missiles (Bradley + dismounted teams)
                { WeaponSystems.GENERIC_MANPAD, 22 }, // Stinger teams distributed across brigade
                { WeaponSystems.SPA_M109, 18 },     // Direct support artillery battalion
            };
            _profiles[IntelProfileTypes.US_HEAVY_MECH_BDE_M60A3] = us_heavyMechBde_M60A3;

            // US Parachute Infantry Brigade (82nd Airborne)
            var us_paraBde_82nd = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_AB, 1950 },     // 3 × Parachute Infantry BN (650 each)
                { WeaponSystems.TANK_M551, 18 },    // Light armor support (M551 Sheridan)
                { WeaponSystems.GENERIC_ART_LIGHT, 18 }, // 105mm howitzers (air-droppable)
                { WeaponSystems.GENERIC_ATGM, 54 }, // Dragon/TOW missile teams
                { WeaponSystems.GENERIC_MANPAD, 36 }, // Stinger teams
                { WeaponSystems.GENERIC_AAA, 12 },  // Vulcan air defense guns
                { WeaponSystems.APC_M113, 12 },     // Command posts and support vehicles
            };
            _profiles[IntelProfileTypes.US_PARA_BDE_82ND] = us_paraBde_82nd;

            // US Air Assault Infantry Brigade (101st Airborne)
            var us_airAssaultBde_101st = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_AM, 2040 },     // 3 × Air Assault Infantry BN (680 each)
                { WeaponSystems.GENERIC_ART_LIGHT, 18 }, // 105mm howitzers (helicopter-mobile)
                { WeaponSystems.GENERIC_ATGM, 48 }, // TOW missile teams
                { WeaponSystems.GENERIC_MANPAD, 30 }, // Stinger teams
                { WeaponSystems.HEL_AH64, 18 },     // Organic attack helicopters (brigade aviation)
                { WeaponSystems.HEL_OH58, 12 },     // Scout helicopters
                { WeaponSystems.APC_M113, 8 },      // Command posts and support vehicles
            };
            _profiles[IntelProfileTypes.US_AIR_ASSAULT_BDE_101ST] = us_airAssaultBde_101st;

            // US Armored Cavalry Squadron (ACR) - Corps reconnaissance squadron
            var us_armoredCavSqdn = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1500 },    // Cavalry personnel across troops + support
                { WeaponSystems.TANK_M1, 41 },      // M1 Abrams tanks distributed across troops
                { WeaponSystems.IFV_M3, 36 },       // M3 Bradley cavalry fighting vehicles
                { WeaponSystems.APC_M113, 18 },     // Command posts, mortars, support vehicles
                { WeaponSystems.GENERIC_ATGM, 24 }, // TOW missiles (M3 Bradley + ground teams)
                { WeaponSystems.GENERIC_MANPAD, 12 }, // Stinger teams for air defense
                { WeaponSystems.HEL_AH64, 26 },     // AH-64 Apache attack helicopters
                { WeaponSystems.HEL_OH58, 12 },     // OH-58 scout helicopters
                { WeaponSystems.SPA_M109, 8 },      // Organic 155mm artillery battery
                { WeaponSystems.GENERIC_AAA, 4 },   // Vulcan air defense guns
            };
            _profiles[IntelProfileTypes.US_ARMORED_CAV_SQDN] = us_armoredCavSqdn;

            // US Division Artillery Brigade (DIVARTY)
            var us_divisionArtilleryBde = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1920 },    // Artillery personnel across 4 battalions
                { WeaponSystems.SPA_M109, 54 },     // 3 × 155mm SP howitzer battalions (18 each)
                { WeaponSystems.ROC_MLRS, 12 },     // 1 × MLRS battalion
                { WeaponSystems.APC_M113, 48 },     // Fire direction, supply, maintenance vehicles
                { WeaponSystems.GENERIC_MANPAD, 24 }, // Stinger teams for air defense
                { WeaponSystems.IFV_M3, 6 },        // Forward observer vehicles
            };
            _profiles[IntelProfileTypes.US_DIVISION_ARTILLERY_BDE] = us_divisionArtilleryBde;

            // US Aviation Attack Brigade
            var us_aviationAttackBde = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1350 },    // Aviation personnel (pilots, crew, maintenance)
                { WeaponSystems.HEL_AH64, 54 },     // 3 × AH-64 Apache attack battalions (18 each)
                { WeaponSystems.HEL_OH58, 18 },     // OH-58 Kiowa scout helicopters
                { WeaponSystems.APC_M113, 12 },     // Ground support and maintenance vehicles
                { WeaponSystems.GENERIC_MANPAD, 12 }, // Air defense for airfields
            };
            _profiles[IntelProfileTypes.US_AVIATION_ATTACK_BDE] = us_aviationAttackBde;

            // US Engineer Brigade
            var us_engineerBde = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_ENG, 2400 },    // Engineer personnel across battalions
                { WeaponSystems.APC_M113, 72 },     // Engineer vehicles and bridging equipment
                { WeaponSystems.TANK_M60A3, 12 },   // Engineer tanks with dozer blades
                { WeaponSystems.GENERIC_MANPAD, 18 }, // Stinger teams
                { WeaponSystems.GENERIC_ATGM, 24 }, // TOW missiles for defensive positions
            };
            _profiles[IntelProfileTypes.US_ENGINEER_BDE] = us_engineerBde;

            // US Air Defense Brigade
            var us_airDefenseBde = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.INF_REG, 1800 },    // Air defense personnel
                { WeaponSystems.SAM_HAWK, 24 },     // Hawk SAM batteries
                { WeaponSystems.SPSAM_CHAP, 36 },   // Chaparral mobile SAM systems
                { WeaponSystems.SPAAA_M163, 48 },   // Vulcan air defense guns
                { WeaponSystems.GENERIC_MANPAD, 72 }, // Stinger teams distributed throughout
                { WeaponSystems.APC_M113, 24 },     // Command and control vehicles
            };
            _profiles[IntelProfileTypes.US_AIR_DEFENSE_BDE] = us_airDefenseBde;

            // US Fighter Wing - F-15 Eagle
            var us_fighterWing_F15 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ASF_F15, 36 }
            };
            _profiles[IntelProfileTypes.US_FIGHTER_WING_F15] = us_fighterWing_F15;

            // US Fighter Wing - F-4 Phantom II
            var us_fighterWing_F4 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ASF_F4, 36 }
            };
            _profiles[IntelProfileTypes.US_FIGHTER_WING_F4] = us_fighterWing_F4;

            // US Fighter Wing - F-16 Fighting Falcon
            var us_fighterWing_F16 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.MRF_F16, 36 }
            };
            _profiles[IntelProfileTypes.US_FIGHTER_WING_F16] = us_fighterWing_F16;

            // US Tactical Fighter Wing - A-10 Thunderbolt II
            var us_tacticalWing_A10 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.ATT_A10, 36 }
            };
            _profiles[IntelProfileTypes.US_TACTICAL_WING_A10] = us_tacticalWing_A10;

            // US Bomber Wing - Mixed Types
            var us_bomberWing_F111 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.BMB_F111, 18 },
            };
            _profiles[IntelProfileTypes.US_BOMBER_WING_F111] = us_bomberWing_F111;

            // US Bomber Wing - Mixed Types
            var us_bomberWing_F117 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.BMB_F117, 18 },
            };
            _profiles[IntelProfileTypes.US_BOMBER_WING_F117] = us_bomberWing_F117;

            // US Reconnaissance Squadron - SR-71 Blackbird
            var us_reconSqdn_SR71 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.RCNA_SR71, 12 }
            };
            _profiles[IntelProfileTypes.US_RECON_SQDN_SR71] = us_reconSqdn_SR71;

            // US AWACS Squadron - E-3 Sentry
            var us_awacsSqdn_E3 = new Dictionary<WeaponSystems, int>
            {
                { WeaponSystems.AWACS_E3, 6 }
            };
            _profiles[IntelProfileTypes.US_AWACS_E3] = us_awacsSqdn_E3;

            #endregion

            //-------------------------------------------------------------//
        }

        #endregion // Profile Database
    }
}