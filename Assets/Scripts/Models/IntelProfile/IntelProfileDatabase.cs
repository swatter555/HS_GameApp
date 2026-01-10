using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models
{
    public static class IntelProfileDatabase
    {
        #region Constants

        private const string CLASS_NAME = nameof(IntelProfileDatabase);

        #endregion

        #region Fields

        private static readonly Dictionary<IntelProfileTypes, Dictionary<Intel_WeaponTypes, int>> _intelProfiles = new();
        private static readonly object _initializationLock = new();
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
        public static void Initialize()
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
                    AppService.HandleException(CLASS_NAME, nameof(Initialize), e);
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
                return _intelProfiles.ContainsKey(profileType);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HasProfile), e);
                return false;
            }
        }

        /// <summary>
        /// Gets the maximum count for a specific weapon type in a profile type.
        /// </summary>
        /// <param name="profileType">The profile type to query</param>
        /// <param name="weaponType">The weapon type to look up</param>
        /// <returns>Maximum count, or 0 if not found</returns>
        public static int GetWeaponTypeCount(IntelProfileTypes profileType, Intel_WeaponTypes weaponType)
        {
            try
            {
                EnsureInitialized();

                if (_intelProfiles.TryGetValue(profileType, out var weaponTypes))
                {
                    return weaponTypes.TryGetValue(weaponType, out int count) ? count : 0;
                }

                return 0;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetWeaponTypeCount), e);
                return 0;
            }
        }

        /// <summary>
        /// Gets all weapon types defined for a specific profile type.
        /// </summary>
        /// <param name="profileType">The profile type to query</param>
        /// <returns>Dictionary of weapon types and their maximum counts</returns>
        public static IReadOnlyDictionary<Intel_WeaponTypes, int> GetDefinedWeaponTypes(IntelProfileTypes profileType)
        {
            try
            {
                EnsureInitialized();

                if (_intelProfiles.TryGetValue(profileType, out var weaponTypes))
                {
                    return weaponTypes;
                }

                return new Dictionary<Intel_WeaponTypes, int>();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetDefinedWeaponTypes), e);
                return new Dictionary<Intel_WeaponTypes, int>();
            }
        }

        /// <summary>
        /// Generate an intel report based on the provided parameters.
        /// </summary>
        /// <param name="profileType"></param>
        /// <param name="unitName"></param>
        /// <param name="currentHitPoints"></param>
        /// <param name="nationality"></param>
        /// <param name="deploymentPosition"></param>
        /// <param name="xpLevel"></param>
        /// <param name="effLevel"></param>
        /// <param name="spottedLevel"></param>
        /// <returns></returns>
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
                if (!_intelProfiles.TryGetValue(profileType, out var weaponTypes))
                {
                    AppService.HandleException(CLASS_NAME, nameof(GenerateIntelReport),
                        new KeyNotFoundException($"Profile type {profileType} not found"));
                    return intelReport; // Return basic report with no equipment
                }

                // Calculate strength multiplier, guard against divide by zero
                float safeHitPoints = Math.Max(currentHitPoints, 1);
                float currentMultiplier = safeHitPoints / GameData.MAX_HP;

                // Step 1: Scale weapon types by current strength
                var scaledWeaponTypes = new Dictionary<Intel_WeaponTypes, float>();
                foreach (var kvp in weaponTypes)
                {
                    float scaledValue = kvp.Value * currentMultiplier;
                    if (scaledValue > 0f)
                    {
                        scaledWeaponTypes[kvp.Key] = scaledValue;
                    }
                }

                // Step 2: Aggregate into display buckets
                var bucketAccumulators = new Dictionary<string, float>();
                foreach (var kvp in scaledWeaponTypes)
                {
                    string bucketName = MapWeaponTypeToBucket(kvp.Key);

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
        /// Maps Intel_WeaponTypes to display bucket categories.
        /// </summary>
        /// <param name="weaponType">The weapon type</param>
        /// <returns>The display bucket name, or null if not mapped</returns>
        private static string MapWeaponTypeToBucket(Intel_WeaponTypes weaponType)
        {
            return weaponType switch
            {
                // Infantry
                Intel_WeaponTypes.Infantry => "Men",

                // Tanks
                Intel_WeaponTypes.T55A or Intel_WeaponTypes.T64A or Intel_WeaponTypes.T64B or
                Intel_WeaponTypes.T72A or Intel_WeaponTypes.T72B or Intel_WeaponTypes.T80B or
                Intel_WeaponTypes.T80U or Intel_WeaponTypes.T80BV or Intel_WeaponTypes.M1 or
                Intel_WeaponTypes.M60A3 or Intel_WeaponTypes.M551 or Intel_WeaponTypes.LEOPARD1 or
                Intel_WeaponTypes.LEOPARD2 or Intel_WeaponTypes.CHALLENGER1 or Intel_WeaponTypes.AMX30
                    => "Tanks",

                // IFVs
                Intel_WeaponTypes.BMP1 or Intel_WeaponTypes.BMP2 or Intel_WeaponTypes.BMP3 or
                Intel_WeaponTypes.BMD1 or Intel_WeaponTypes.BMD2 or Intel_WeaponTypes.BMD3 or
                Intel_WeaponTypes.M2 or Intel_WeaponTypes.M3 or Intel_WeaponTypes.MARDER or
                Intel_WeaponTypes.WARRIOR or Intel_WeaponTypes.AMX10P
                    => "IFVs",

                // APCs
                Intel_WeaponTypes.MTLB or Intel_WeaponTypes.BTR70 or Intel_WeaponTypes.BTR80 or
                Intel_WeaponTypes.M113 or Intel_WeaponTypes.LVTP7 or Intel_WeaponTypes.FV432 or
                Intel_WeaponTypes.VAB
                    => "APCs",

                // Recon
                Intel_WeaponTypes.BRDM2 or Intel_WeaponTypes.BRDM2AT or Intel_WeaponTypes.LUCHS or
                Intel_WeaponTypes.SCIMITAR or Intel_WeaponTypes.ERC90
                    => "Recon",

                // Artillery (towed and self-propelled)
                Intel_WeaponTypes.SPA_2S1 or Intel_WeaponTypes.SPA_2S3 or Intel_WeaponTypes.SPA_2S5 or
                Intel_WeaponTypes.SPA_2S19 or Intel_WeaponTypes.M109 or Intel_WeaponTypes.AUF1 or
                Intel_WeaponTypes.LightArtillery or Intel_WeaponTypes.HeavyArtillery
                    => "Artillery",

                // Rocket Artillery
                Intel_WeaponTypes.BM21 or Intel_WeaponTypes.BM27 or Intel_WeaponTypes.BM30 or
                Intel_WeaponTypes.MLRS
                    => "Rocket Artillery",

                // SSMs
                Intel_WeaponTypes.SCUD => "Surface To Surface Missiles",

                // SAMs (including self-propelled)
                Intel_WeaponTypes.S75 or Intel_WeaponTypes.S125 or Intel_WeaponTypes.S300 or
                Intel_WeaponTypes.HAWK or Intel_WeaponTypes.SAM_RAPIER or Intel_WeaponTypes.SPSAM_2K22 or
                Intel_WeaponTypes.SPSAM_9K31 or Intel_WeaponTypes.CHAP or Intel_WeaponTypes.ROLAND
                    => "SAMs",

                // AAA (including self-propelled)
                Intel_WeaponTypes.ZSU57 or Intel_WeaponTypes.ZSU23 or Intel_WeaponTypes.M163 or
                Intel_WeaponTypes.GEPARD or Intel_WeaponTypes.AAA
                    => "Anti-aircraft Artillery",

                // MANPADs
                Intel_WeaponTypes.Manpad => "MANPADs",

                // ATGMs
                Intel_WeaponTypes.ATGM => "ATGMs",

                // Attack Helicopters
                Intel_WeaponTypes.MI8AT or Intel_WeaponTypes.MI24D or Intel_WeaponTypes.MI24V or
                Intel_WeaponTypes.MI28 or Intel_WeaponTypes.AH64 or Intel_WeaponTypes.BO105
                    => "Attack Helicopters",

                // Transport Helicopters
                Intel_WeaponTypes.MI8T or Intel_WeaponTypes.UH60 or Intel_WeaponTypes.LYNX or
                Intel_WeaponTypes.OH58
                    => "Transport Helicopters",

                // Fighters
                Intel_WeaponTypes.MIG21 or Intel_WeaponTypes.MIG23 or Intel_WeaponTypes.MIG25 or
                Intel_WeaponTypes.MIG29 or Intel_WeaponTypes.MIG31 or Intel_WeaponTypes.SU27 or
                Intel_WeaponTypes.SU47 or Intel_WeaponTypes.MIG27 or Intel_WeaponTypes.F15 or
                Intel_WeaponTypes.F4 or Intel_WeaponTypes.F16 or Intel_WeaponTypes.TORNADO_IDS or
                Intel_WeaponTypes.TORNADO_GR1 or Intel_WeaponTypes.MIRAGE2000
                    => "Fighters",

                // Attack Aircraft
                Intel_WeaponTypes.SU25 or Intel_WeaponTypes.SU25B or Intel_WeaponTypes.A10 or
                Intel_WeaponTypes.JAGUAR
                    => "Attack",

                // Bombers
                Intel_WeaponTypes.SU24 or Intel_WeaponTypes.TU16 or Intel_WeaponTypes.TU22 or
                Intel_WeaponTypes.TU22M3 or Intel_WeaponTypes.F111 or Intel_WeaponTypes.F117
                    => "Bombers",

                // AWACS
                Intel_WeaponTypes.A50 or Intel_WeaponTypes.E3 => "AWACS",

                // Recon Aircraft
                Intel_WeaponTypes.MIG25R or Intel_WeaponTypes.SR71 => "Recon Aircraft",

                // Light AT (RPG)
                Intel_WeaponTypes.RPG7 => "LightAT",

                // Mortars
                Intel_WeaponTypes.LightMortar or Intel_WeaponTypes.HeavyMortar => "Mortars",

                // Recoilless Rifles
                Intel_WeaponTypes.RecoilessRifle => "Recoilless Rifle",

                // Trucks (not tracked)
                Intel_WeaponTypes.Truck => null,

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
                SpottedLevel.Level3 => GetRandomMultiplier(GameData.MIN_INTEL_ERROR, GameData.MODERATE_INTEL_ERROR),
                SpottedLevel.Level2 => GetRandomMultiplier(GameData.MIN_INTEL_ERROR, GameData.MAX_INTEL_ERROR), 
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
                    report.AttackHelos = value;
                    break;
                case "Transport Helicopters":
                    report.TransportHelos = value;
                    break;
                case "Fighters":
                    report.FGTs = value;
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
                case "Transport Aircraft":
                    report.TRANAIR = value;
                    break;
                case "Recon Aircraft":
                    report.RCNAs = value;
                    break;
                case "Naval Transport":
                    report.TransportAir = value;
                    break;
                case "LightAT":
                    report.LightAT = value;
                    break;
                case "Mortars":
                    report.Mortars = value;
                    break;
                case "Recoilless Rifle":
                    report.Recoilless = value;
                    break;

                // Note- not tracking trucks or bases.
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
            var mrrBTR70 = new Dictionary<Intel_WeaponTypes, int>
            {
                {Intel_WeaponTypes.Infantry, 2523 },
                { Intel_WeaponTypes.T55A, 40 },
                { Intel_WeaponTypes.BTR70, 129 },
                { Intel_WeaponTypes.BMP1, 26 },
                { Intel_WeaponTypes.ZSU57, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 16 },
                { Intel_WeaponTypes.Manpad, 30 },
            };
            _intelProfiles[IntelProfileTypes.SV_MRR_BTR70] = mrrBTR70;

            // Motor Rifle Regiment- BTR80 profile
            var mrrBTR80 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 2523 },
                { Intel_WeaponTypes.T72A, 40 },
                { Intel_WeaponTypes.BTR80, 129 },
                { Intel_WeaponTypes.BMP2, 26 },
                { Intel_WeaponTypes.ZSU23, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 16 },
                { Intel_WeaponTypes.Manpad, 30 },
            };
            _intelProfiles[IntelProfileTypes.SV_MRR_BTR80] = mrrBTR80;
            
            #endregion // Soviet APCs

            #region Soviet IFVs

            // Motor Rifle Regiment- BMP1 profile
            var mrrBMP1 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 2424 },
                { Intel_WeaponTypes.T55A, 40 },
                { Intel_WeaponTypes.BMP1, 129 },
                { Intel_WeaponTypes.BTR70, 26 },
                { Intel_WeaponTypes.ZSU57, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 16 },
                { Intel_WeaponTypes.Manpad, 30 },
            };
            _intelProfiles[IntelProfileTypes.SV_MRR_BMP1] = mrrBMP1;

            // Motor Rifle Regiment- BMP2 profile
            var mrrBMP2 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 2424 },
                { Intel_WeaponTypes.T72A, 40 },
                { Intel_WeaponTypes.BMP2, 129 },
                { Intel_WeaponTypes.BTR70, 26 },
                { Intel_WeaponTypes.ZSU23, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 16 },
                { Intel_WeaponTypes.Manpad, 30 },
            };
            _intelProfiles[IntelProfileTypes.SV_MRR_BMP2] = mrrBMP2;

            // Motor Rifle Regiment- BMP3 profile
            var mrrBMP3 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 2424 },
                { Intel_WeaponTypes.T80B, 40 },
                { Intel_WeaponTypes.BMP3, 129 },
                { Intel_WeaponTypes.BTR80, 26 },
                { Intel_WeaponTypes.ZSU23, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 16 },
                { Intel_WeaponTypes.Manpad, 30 },
            };
            _intelProfiles[IntelProfileTypes.SV_MRR_BMP3] = mrrBMP3;

            #endregion // Soviet IFVs

            #region Soviet Tank Units

            // Tank Regiment- T55 profile
            var tr_T55 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1143 },
                { Intel_WeaponTypes.T55A, 94 },
                { Intel_WeaponTypes.BMP1, 45 },
                { Intel_WeaponTypes.BTR70, 21 },
                { Intel_WeaponTypes.ZSU57, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 12 },
            };
            _intelProfiles[IntelProfileTypes.SV_TR_T55] = tr_T55;

            // Tank Regiment- T64A profile
            var tr_T64A = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1143 },
                { Intel_WeaponTypes.T64A, 94 },
                { Intel_WeaponTypes.BMP2, 45 },
                { Intel_WeaponTypes.BTR70, 21 },
                { Intel_WeaponTypes.ZSU57, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 12 },
            };
            _intelProfiles[IntelProfileTypes.SV_TR_T64A] = tr_T64A;

            // Tank Regiment- T64B profile
            var tr_T64B = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1143 },
                { Intel_WeaponTypes.T64B, 94 },
                { Intel_WeaponTypes.BMP2, 45 },
                { Intel_WeaponTypes.BTR80, 21 },
                { Intel_WeaponTypes.ZSU23, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 12 },
            };
            _intelProfiles[IntelProfileTypes.SV_TR_T64B] = tr_T64B;

            // Tank Regiment- T72A profile
            var tr_T72A = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1143 },
                { Intel_WeaponTypes.T72A, 94 },
                { Intel_WeaponTypes.BMP1, 45 },
                { Intel_WeaponTypes.BTR70, 21 },
                { Intel_WeaponTypes.ZSU57, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 12 },
            };
            _intelProfiles[IntelProfileTypes.SV_TR_T72A] = tr_T72A;

            // Tank Regiment- T72B profile
            var tr_T72B = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1143 },
                { Intel_WeaponTypes.T72B, 94 },
                { Intel_WeaponTypes.BMP2, 45 },
                { Intel_WeaponTypes.BTR80, 21 },
                { Intel_WeaponTypes.SPSAM_2K22, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 12 },
            };
            _intelProfiles[IntelProfileTypes.SV_TR_T72B] = tr_T72B;

            // Tank Regiment- T80B profile
            var tr_T80B = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1143 },
                { Intel_WeaponTypes.T80B, 94 },
                { Intel_WeaponTypes.BMP2, 45 },
                { Intel_WeaponTypes.BTR80, 21 },
                { Intel_WeaponTypes.ZSU23, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 12 },
            };
            _intelProfiles[IntelProfileTypes.SV_TR_T80B] = tr_T80B;

            // Tank Regiment- T80U profile
            var tr_T80U = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1143 },
                { Intel_WeaponTypes.T80U, 94 },
                { Intel_WeaponTypes.BMP2, 45 },
                { Intel_WeaponTypes.BTR80, 21 },
                { Intel_WeaponTypes.SPSAM_2K22, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 12 },
            };
            _intelProfiles[IntelProfileTypes.SV_TR_T80U] = tr_T80U;

            // Tank Regiment- T80BV profile
            var tr_T80BV = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1143 },
                { Intel_WeaponTypes.T80BV, 94 },
                { Intel_WeaponTypes.BMP3, 45 },
                { Intel_WeaponTypes.BTR80, 21 },
                { Intel_WeaponTypes.SPSAM_2K22, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 12 },
            };
            _intelProfiles[IntelProfileTypes.SV_TR_T80BV] = tr_T80BV;

            #endregion // Soviet Tank Units

            #region Soviet Artillery Units

            // Soviet heavy towed artillery
            var sv_heavyart = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1100 },
                { Intel_WeaponTypes.HeavyArtillery, 72 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.BTR70, 24 },
                { Intel_WeaponTypes.Manpad, 21 },
            };
            _intelProfiles[IntelProfileTypes.SV_AR_HVY] = sv_heavyart;

            // Soviet light towed artillery
            var sv_lightart = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1100 },
                { Intel_WeaponTypes.LightArtillery, 72 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.BTR70, 24 },
                { Intel_WeaponTypes.Manpad, 21 },
            };
            _intelProfiles[IntelProfileTypes.SV_AR_LGT] = sv_lightart;

            // Soviet artillery regiment 2S1
            var sv_2s1 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1062 },
                { Intel_WeaponTypes.SPA_2S1, 36 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.BTR70, 24 },
                { Intel_WeaponTypes.Manpad, 21 },
            };
            _intelProfiles[IntelProfileTypes.SV_AR_2S1] = sv_2s1;

            // Soviet artillery regiment 2S3
            var sv_2s3 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1062 },
                { Intel_WeaponTypes.SPA_2S3, 36 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.BTR70, 24 },
                { Intel_WeaponTypes.Manpad, 21 },
            };
            _intelProfiles[IntelProfileTypes.SV_AR_2S3] = sv_2s3;

            // Soviet artillery regiment 2S5
            var sv_2s5 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1262 },
                { Intel_WeaponTypes.SPA_2S5, 36 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.BTR70, 44 },
                { Intel_WeaponTypes.Manpad, 21 },
            };
            _intelProfiles[IntelProfileTypes.SV_AR_2S5] = sv_2s5;

            // Soviet artillery regiment 2S19
            var sv_2s19 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1162 },
                { Intel_WeaponTypes.SPA_2S19, 36 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.BTR70, 36 },
                { Intel_WeaponTypes.Manpad, 21 },
            };
            _intelProfiles[IntelProfileTypes.SV_AR_2S19] = sv_2s19;

            // Soviet rocket artillery regiment BM-21
            var sv_bm21 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1200 },
                { Intel_WeaponTypes.BM21, 48 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.BTR70, 24 },
                { Intel_WeaponTypes.Manpad, 21 },
            };
            _intelProfiles[IntelProfileTypes.SV_ROC_BM21] = sv_bm21;

            // Soviet rocket artillery regiment BM-27
            var sv_bm27 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1200 },
                { Intel_WeaponTypes.BM27, 24 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.BTR70, 24 },
                { Intel_WeaponTypes.Manpad, 21 },
            };
            _intelProfiles[IntelProfileTypes.SV_ROC_BM27] = sv_bm27;

            // Soviet rocket artillery regiment BM-30
            var sv_bm30 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1200 },
                { Intel_WeaponTypes.BM30, 24 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.BTR70, 24 },
                { Intel_WeaponTypes.Manpad, 21 },
            };
            _intelProfiles[IntelProfileTypes.SV_ROC_BM30] = sv_bm30;

            // Soviet ballistic missile regiment SCUD
            var sv_scud = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 800 },
                { Intel_WeaponTypes.SCUD, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.BTR70, 24 },
                { Intel_WeaponTypes.Manpad, 21 },
            };
            _intelProfiles[IntelProfileTypes.SV_BM_SCUDB] = sv_scud;

            #endregion // Soviet Artillery Units

            #region Soviet air mobile units

            // Soviet air mobile regiment MTLB
            var aar_MTLB = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,           2300 },   // 3× air‑assault battalions + HQ & support
                { Intel_WeaponTypes.MTLB,           68 },  // 31 per battalion (3 rifle coys + HQ)
                { Intel_WeaponTypes.BRDM2,          13 },  // recon platoon
                { Intel_WeaponTypes.LightArtillery,  18 },  // 122 mm artillery battery
                { Intel_WeaponTypes.LightMortar,        12 },
                { Intel_WeaponTypes.HeavyMortar,       12 },
                { Intel_WeaponTypes.ATGM,       14 },  // mixed AT‑4/AT‑5 sections
                { Intel_WeaponTypes.Manpad,     45 },  // SA‑14/16 squads
                { Intel_WeaponTypes.AAA,         2 },  // ZSU‑23‑4 Shilka (reduced strength)
                { Intel_WeaponTypes.MI8T,     166 },  // 2× transport helicopter squadrons
            };
            _intelProfiles[IntelProfileTypes.SV_AAR_MTLB] = aar_MTLB;

            // Soviet air mobile regiment BMD1
            var aar_BMD1 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,           2300 },   // 3× air‑assault battalions + HQ & support
                { Intel_WeaponTypes.BMD1,           68 },  // 31 per battalion (3 rifle coys + HQ)
                { Intel_WeaponTypes.BRDM2,          13 },  // recon platoon
                { Intel_WeaponTypes.LightArtillery,  18 },  // 122 mm artillery battery
                { Intel_WeaponTypes.LightMortar,        12 },
                { Intel_WeaponTypes.HeavyMortar,       12 },
                { Intel_WeaponTypes.ATGM,       14 },  // mixed AT‑4/AT‑5 sections
                { Intel_WeaponTypes.Manpad,     45 },  // SA‑14/16 squads
                { Intel_WeaponTypes.AAA,         2 },  // ZSU‑23‑4 Shilka (reduced strength)
                { Intel_WeaponTypes.MI8T,     166 },  // 2× transport helicopter squadrons
            };
            _intelProfiles[IntelProfileTypes.SV_AAR_BMD1] = aar_BMD1;

            // Soviet air mobile regiment BTR80
            var aar_BMD2 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,           2300 },   // 3× air‑assault battalions + HQ & support
                { Intel_WeaponTypes.BMD2,           68 },  // 31 per battalion (3 rifle coys + HQ)
                { Intel_WeaponTypes.BRDM2,          13 },  // recon platoon
                { Intel_WeaponTypes.LightArtillery,  18 },  // 122 mm artillery battery
                { Intel_WeaponTypes.LightMortar,        12 },
                { Intel_WeaponTypes.HeavyMortar,       12 },
                { Intel_WeaponTypes.ATGM,       14 },  // mixed AT‑4/AT‑5 sections
                { Intel_WeaponTypes.Manpad,     45 },  // SA‑14/16 squads
                { Intel_WeaponTypes.AAA,         2 },  // ZSU‑23‑4 Shilka (reduced strength)
                { Intel_WeaponTypes.MI8T,      166 },  // 2× transport helicopter squadrons
            };
            _intelProfiles[IntelProfileTypes.SV_AAR_BMD2] = aar_BMD2;

            // Soviet air mobile regiment BMD3
            var aar_BMD3 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,           2300 },   // 3× air‑assault battalions + HQ & support
                { Intel_WeaponTypes.BMD3,           68 },  // 31 per battalion (3 rifle coys + HQ)
                { Intel_WeaponTypes.BRDM2,          13 },  // recon platoon
                { Intel_WeaponTypes.LightArtillery,  18 },  // 122 mm artillery battery
                { Intel_WeaponTypes.LightMortar,        12 },
                { Intel_WeaponTypes.HeavyMortar,       12 },
                { Intel_WeaponTypes.ATGM,       14 },  // mixed AT‑4/AT‑5 sections
                { Intel_WeaponTypes.Manpad,     45 },  // SA‑14/16 squads
                { Intel_WeaponTypes.AAA,         2 },  // ZSU‑23‑4 Shilka (reduced strength)
                { Intel_WeaponTypes.MI8T,     166 },  // 2× transport helicopter squadrons
            };
            _intelProfiles[IntelProfileTypes.SV_AAR_BMD3] = aar_BMD3;

            #endregion // Soviet air mobile units

            #region Soviet VDV units

            // VDV airborne regiment – BMD‑1 (mid‑1980s baseline)
            // Sources: TO&E 38‑500 series; typical regiment strength ~1 800 men, three
            // BMD battalions (31 vehicles each) plus regimental assets.
            var vdv_BMD1 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,             2250 }, // 3× airborne battalions + regt HQ/support
                { Intel_WeaponTypes.BMD1,             93 }, // 31 per battalion (3 rifle coys + HQ)
                { Intel_WeaponTypes.BRDM2,             6 }, // recon platoon (BRDM‑2)
                { Intel_WeaponTypes.HeavyMortar,         18 }, // 120 mm 2S9 Nona‑S battery
                { Intel_WeaponTypes.LightMortar,          18 },
                { Intel_WeaponTypes.ATGM,         12 }, // AT‑4/AT‑5 sections
                { Intel_WeaponTypes.Manpad,       45 }, // SA‑14/16 squads
                { Intel_WeaponTypes.AAA,           6 },
            };
            _intelProfiles[IntelProfileTypes.SV_VDV_BMD1] = vdv_BMD1;

            // VDV airborne regiment – BMD‑2 (mid‑1980s baseline)
            var vdv_BMD2 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,             2250 }, // 3× airborne battalions + regt HQ/support
                { Intel_WeaponTypes.BMD2,             93 }, // 31 per battalion (3 rifle coys + HQ)
                { Intel_WeaponTypes.BRDM2,             6 }, // recon platoon (BRDM‑2)
                { Intel_WeaponTypes.HeavyMortar,         18 }, // 120 mm 2S9 Nona‑S battery
                { Intel_WeaponTypes.LightMortar,          18 },
                { Intel_WeaponTypes.ATGM,         12 }, // AT‑4/AT‑5 sections
                { Intel_WeaponTypes.Manpad,       45 }, // SA‑14/16 squads
                { Intel_WeaponTypes.AAA,           6 },
            };
            _intelProfiles[IntelProfileTypes.SV_VDV_BMD2] = vdv_BMD2;

            // VDV airborne regiment – BMD‑3 (mid‑1980s baseline)
            var vdv_BMD3 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,             2250 }, // 3× airborne battalions + regt HQ/support
                { Intel_WeaponTypes.BMD3,             93 }, // 31 per battalion (3 rifle coys + HQ)
                { Intel_WeaponTypes.BRDM2,             6 }, // recon platoon (BRDM‑2)
                { Intel_WeaponTypes.HeavyMortar,         18 }, // 120 mm 2S9 Nona‑S battery
                { Intel_WeaponTypes.LightMortar,          18 },
                { Intel_WeaponTypes.ATGM,         12 }, // AT‑4/AT‑5 sections
                { Intel_WeaponTypes.Manpad,       45 }, // SA‑14/16 squads
                { Intel_WeaponTypes.AAA,           6 },
            };
            _intelProfiles[IntelProfileTypes.SV_VDV_BMD3] = vdv_BMD3;

            // VDV artillery regiment
            var vdv_artreg = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,             1200 }, // regt HQ/support
                { Intel_WeaponTypes.BMD1,             12 },
                { Intel_WeaponTypes.BRDM2,             6 },
                { Intel_WeaponTypes.HeavyMortar,         36 }, // 120 mm 2S9 Nona‑S battery
                { Intel_WeaponTypes.LightMortar,          18 },
            };
            _intelProfiles[IntelProfileTypes.SV_VDV_ART] = vdv_artreg;

            // VDV airborne regiment – support (mid‑1980s baseline)
            var vdv_sup = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,             1150 },
                { Intel_WeaponTypes.T55A,            31 },
                { Intel_WeaponTypes.BRDM2AT,          18 }, 
                { Intel_WeaponTypes.HeavyMortar,          6 }, 
                { Intel_WeaponTypes.ATGM,         12 }, 
                { Intel_WeaponTypes.Manpad,       12 }, 
                { Intel_WeaponTypes.AAA,           2 },
            };
            _intelProfiles[IntelProfileTypes.SV_VDV_SUP] = vdv_sup;

            #endregion // Soviet VDV units

            #region Soviet naval infantry units

            // Naval Assault Brigade- BTR70
            var navBTR70 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 2750 },
                { Intel_WeaponTypes.T55A, 44 },
                { Intel_WeaponTypes.BMP1, 44 },
                { Intel_WeaponTypes.BTR70, 145 },
                { Intel_WeaponTypes.ZSU57, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 36 },
            };
            _intelProfiles[IntelProfileTypes.SV_NAV_BTR70] = navBTR70;

            // Naval Assault Brigade- BTR80 profile
            var navBTR80 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 2750 },
                { Intel_WeaponTypes.T72A, 44 },
                { Intel_WeaponTypes.BMP2, 44 },
                { Intel_WeaponTypes.BTR80, 145 },
                { Intel_WeaponTypes.ZSU23, 4},
                { Intel_WeaponTypes.SPSAM_9K31, 4 },
                { Intel_WeaponTypes.SPA_2S1, 18 },
                { Intel_WeaponTypes.LightMortar, 12 },
                { Intel_WeaponTypes.HeavyMortar, 12 },
                { Intel_WeaponTypes.BRDM2, 12 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 36 },
            };
            _intelProfiles[IntelProfileTypes.SV_NAV_BTR80] = navBTR80;

            #endregion // Soviet naval infantry units

            #region Soviet engineer units

            // Soviet engineer battalion
            var svengineers = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 340 },
                { Intel_WeaponTypes.BTR70, 20 },
            };
            _intelProfiles[IntelProfileTypes.SV_ENG] = svengineers;

            #endregion // Soviet engineer units

            #region Soviet recon units

            // Soviet recon regiment
            var svreconrgt = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1020 },
                { Intel_WeaponTypes.T55A, 18 },
                { Intel_WeaponTypes.BMP1, 36 },
                { Intel_WeaponTypes.BTR70, 42 },
                { Intel_WeaponTypes.ZSU57, 4},
                { Intel_WeaponTypes.BRDM2, 54 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 12 },
            };
            _intelProfiles[IntelProfileTypes.SV_RCR] = svreconrgt;

            // Soviet recon regiment AT
            var svreconrgtAT = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1020 },
                { Intel_WeaponTypes.T72A, 18 },
                { Intel_WeaponTypes.BMP2, 36 },
                { Intel_WeaponTypes.BTR80, 42 },
                { Intel_WeaponTypes.ZSU57, 4},
                { Intel_WeaponTypes.BRDM2AT, 54 },
                { Intel_WeaponTypes.ATGM, 12 },
                { Intel_WeaponTypes.Manpad, 12 },
            };
            _intelProfiles[IntelProfileTypes.SV_RCR_AT] = svreconrgtAT;

            #endregion // Soviet recon units

            #region Soviet air defence units

            // AAA Regiment- AAA profile
            var svadr_AAA = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 600 },
                { Intel_WeaponTypes.AAA, 18 },
                { Intel_WeaponTypes.Manpad, 21 },
                { Intel_WeaponTypes.BTR70, 22 }
            };
            _intelProfiles[IntelProfileTypes.SV_ADR_AAA] = svadr_AAA;

            // AAA Regiment- ZSU-57 profile
            var svadr_ZSU57 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 600 },
                { Intel_WeaponTypes.ZSU57, 18 },
                { Intel_WeaponTypes.Manpad, 21 },
                { Intel_WeaponTypes.BTR70, 22 }
            };
            _intelProfiles[IntelProfileTypes.SV_ADR_ZSU57] = svadr_ZSU57;

            // AAA Regiment- ZSU-23 profile
            var svadr_ZSU23 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 600 },
                { Intel_WeaponTypes.ZSU23, 18 },
                { Intel_WeaponTypes.Manpad, 21 },
                { Intel_WeaponTypes.BTR70, 22 }
            };
            _intelProfiles[IntelProfileTypes.SV_ADR_ZSU57] = svadr_ZSU23;

            // AAA Regiment- 2K22 profile
            var svadr_2K22 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 600 },
                { Intel_WeaponTypes.SPSAM_2K22, 18 },
                { Intel_WeaponTypes.Manpad, 21 },
                { Intel_WeaponTypes.BTR70, 22 }
            };
            _intelProfiles[IntelProfileTypes.SV_ADR_2K22] = svadr_2K22;

            #endregion // Soviet air defence units

            #region Soviet SAM units

            // SAM Regiment- 9K31 profile
            var sam9k31 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 750 },
                { Intel_WeaponTypes.SPSAM_9K31, 18 },
                { Intel_WeaponTypes.Manpad, 21 },
                { Intel_WeaponTypes.BTR70, 22 }
            };
            _intelProfiles[IntelProfileTypes.SV_SPSAM_9K31] = sam9k31;

            // SAM Regiment- S75 profile
            var samS75 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 750 },
                { Intel_WeaponTypes.S75, 18 },
                { Intel_WeaponTypes.Manpad, 21 },
                { Intel_WeaponTypes.BTR70, 48 }
            };
            _intelProfiles[IntelProfileTypes.SV_SAM_S75] = samS75;

            // SAM Regiment- S125 profile
            var samS125 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 750 },
                { Intel_WeaponTypes.S125, 18 },
                { Intel_WeaponTypes.Manpad, 21 },
                { Intel_WeaponTypes.BTR70, 48 }
            };
            _intelProfiles[IntelProfileTypes.SV_SAM_S125] = samS125;

            // SAM Regiment- S300 profile
            var samS300 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 750 },
                { Intel_WeaponTypes.S300, 18 },
                { Intel_WeaponTypes.Manpad, 21 },
                { Intel_WeaponTypes.BTR70, 48 }
            };
            _intelProfiles[IntelProfileTypes.SV_SAM_S300] = samS300;

            #endregion // Soviet SAM units

            #region Soviet attack helicopter units

            // Attack regiment- Mi-8T profile
            var helo_MI8T = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MI8AT, 54 }
            };
            _intelProfiles[IntelProfileTypes.SV_HEL_MI8AT] = helo_MI8T;

            // Attack regiment- Mi-24D profile
            var helo_MI24D = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MI24D, 54 }
            };
            _intelProfiles[IntelProfileTypes.SV_HEL_MI24D] = helo_MI24D;

            // Attack regiment- Mi-24V profile
            var helo_MI24V = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MI24V, 54 }
            };
            _intelProfiles[IntelProfileTypes.SV_HEL_MI24V] = helo_MI24V;

            // Attack regiment- Mi-28 profile
            var helo_MI28 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MI28, 54 }
            };
            _intelProfiles[IntelProfileTypes.SV_HEL_MI28] = helo_MI28;

            #endregion

            #region Spetznaz units

            // Soviet Spetznaz
            var spetz = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,     1200 },
                { Intel_WeaponTypes.ATGM,   12 },
                { Intel_WeaponTypes.Manpad, 12 },
                { Intel_WeaponTypes.LightMortar,    18 },
            };
            _intelProfiles[IntelProfileTypes.SV_GRU] = spetz;

            #endregion // Spetznaz units

            #region Soviet air units

            // Soviet AWACS Regiment
            var awacsRegiment = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.A50, 6 }
            };
            _intelProfiles[IntelProfileTypes.SV_AWACS_A50] = awacsRegiment;

            // Fighter Regiment- MiG-21 profile
            var fighterRegiment_Mig21 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MIG21, 36 }
            };
            _intelProfiles[IntelProfileTypes.SV_FR_MIG21] = fighterRegiment_Mig21;

            // Fighter Regiment- MiG-23 profile
            var fighterRegiment_Mig23 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MIG23, 36 }
            };
            _intelProfiles[IntelProfileTypes.SV_FR_MIG23] = fighterRegiment_Mig23;

            // Fighter Regiment- MiG-25 profile
            var fighterRegiment_Mig25 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MIG25, 36 }
            };
            _intelProfiles[IntelProfileTypes.SV_FR_MIG25] = fighterRegiment_Mig25;

            // Fighter Regiment- MiG-29 profile
            var fighterRegiment_Mig29 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MIG29, 36 }
            };
            _intelProfiles[IntelProfileTypes.SV_FR_MIG29] = fighterRegiment_Mig29;

            // Fighter Regiment- Mig-31 profile
            var fighterRegiment_Mig31 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MIG31, 36 }
            };
            _intelProfiles[IntelProfileTypes.SV_FR_MIG31] = fighterRegiment_Mig31;

            // Fighter Regiment- Su-27 profile
            var fighterRegiment_Su27 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.SU27, 36 }
            };
            _intelProfiles[IntelProfileTypes.SV_FR_SU27] = fighterRegiment_Su27;

            // Fighter Regiment- SU-47 profile
            var fighterRegiment_Su47 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.SU47, 36 }
            };
            _intelProfiles[IntelProfileTypes.SV_FR_SU47] = fighterRegiment_Su47;

            // Fighter Regiment- Mig-27
            var fighterRegiment_Mig27 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MIG27, 36 }
            };
            _intelProfiles[IntelProfileTypes.SV_MR_MIG27] = fighterRegiment_Mig27;

            // Attack Regiment- Su-25 profile
            var attackRegiment_Su25 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.SU25, 36 }
            };
            _intelProfiles[IntelProfileTypes.SV_AR_SU25] = attackRegiment_Su25;

            // Attack Regiment- Su-25B profile
            var attackRegiment_Su25B = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.SU25B, 36 }
            };
            _intelProfiles[IntelProfileTypes.SV_AR_SU25B] = attackRegiment_Su25B;

            // Bomber Regiment- SU-24 profile
            var bomberRegiment_Su24 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.SU24, 36 }
            };
            _intelProfiles[IntelProfileTypes.SV_BR_SU24] = bomberRegiment_Su24;

            // Bomber Regiment- TU-16
            var bomberRegiment_Tu16 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.TU16, 24 }
            };
            _intelProfiles[IntelProfileTypes.SV_BR_TU16] = bomberRegiment_Tu16;

            // Bomber Regiment- TU-22
            var bomberRegiment_Tu22 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.TU22, 24 }
            };
            _intelProfiles[IntelProfileTypes.SV_BR_TU22] = bomberRegiment_Tu22;

            // Bomber Regiment- TU-22M3
            var bomberRegiment_Tu22M3 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.TU22M3, 24 }
            };
            _intelProfiles[IntelProfileTypes.SV_BR_TU22M3] = bomberRegiment_Tu22M3;

            // Air Recon Regiment- MiG-25R profile
            var airRecon_Mig25R = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MIG25R, 12 }
            };
            _intelProfiles[IntelProfileTypes.SV_RR_MIG25R] = airRecon_Mig25R;

            #endregion // Soviet air units

            #region Soviet bases

            // Suppy Depot profile
            var sv_supplydepot = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 3000 }
            };
            _intelProfiles[IntelProfileTypes.SV_DEPOT] = sv_supplydepot;

            // Airbase profile
            var sv_airbase = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 2500 }
            };
            _intelProfiles[IntelProfileTypes.SV_AIRB] = sv_airbase;

            // Regular base profile
            var sv_regularbase = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1500 }
            };
            _intelProfiles[IntelProfileTypes.SV_BASE] = sv_regularbase;

            #endregion // Soviet bases

            //-------------------------------------------------------------//

            #region US Profiles

            // US Armored Brigade - M1A1 Abrams (Division 86 structure)
            var us_armoredBde_M1 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      2200 },
                { Intel_WeaponTypes.M1,       116 },  // 2 × Tank BN (58 each)
                { Intel_WeaponTypes.M2,         54 },  // 1 × Mech Inf BN (54 Bradleys)
                { Intel_WeaponTypes.M3,         18 },  // Scout vehicles across battalions + brigade recon
                { Intel_WeaponTypes.M113,       32 },  // Command posts, medical, maintenance vehicles
                { Intel_WeaponTypes.ATGM,   32 },  // TOW missiles (Bradley + dismounted teams)
                { Intel_WeaponTypes.Manpad, 18 },  // Stinger teams distributed across brigade
                { Intel_WeaponTypes.M109,       18 },  // Direct support artillery battalion
                { Intel_WeaponTypes.HeavyMortar,   18 },
                { Intel_WeaponTypes.M163,      4 },  // Vulcan air defense guns
            };
            _intelProfiles[IntelProfileTypes.US_ARMORED_BDE_M1] = us_armoredBde_M1;

            // US Armored Brigade - M60A3 (Division 86 structure)
            var us_armoredBde_M60A3 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      2200 },
                { Intel_WeaponTypes.M60A3,    116 },  // 2 × Tank BN (58 each)
                { Intel_WeaponTypes.M2,         54 },  // 1 × Mech Inf BN (54 Bradleys)
                { Intel_WeaponTypes.M3,         18 },  // Scout vehicles across battalions + brigade recon
                { Intel_WeaponTypes.M113,       32 },  // Command posts, medical, maintenance vehicles
                { Intel_WeaponTypes.ATGM,   32 },  // TOW missiles (Bradley + dismounted teams)
                { Intel_WeaponTypes.Manpad, 18 },  // Stinger teams distributed across brigade
                { Intel_WeaponTypes.M109,       18 },  // Direct support artillery battalion
                { Intel_WeaponTypes.HeavyMortar,   18 },
                { Intel_WeaponTypes.M163,      4 },  // Vulcan air defense guns
            };
            _intelProfiles[IntelProfileTypes.US_ARMORED_BDE_M60A3] = us_armoredBde_M60A3;

            // US Heavy Mechanized Brigade - M1A1 Abrams (Division 86 structure)
            var us_heavyMechBde_M1 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      2800 },
                { Intel_WeaponTypes.M1,        58 }, // 1 × Tank BN (58 tanks)
                { Intel_WeaponTypes.M2,        108 }, // 2 × Mech Inf BN (54 Bradleys each)
                { Intel_WeaponTypes.M3,         18 }, // Scout vehicles across battalions + brigade recon
                { Intel_WeaponTypes.M113,       32 }, // Command posts, medical, maintenance vehicles
                { Intel_WeaponTypes.ATGM,   40 }, // TOW missiles (Bradley + dismounted teams)
                { Intel_WeaponTypes.Manpad, 22 }, // Stinger teams distributed across brigade
                { Intel_WeaponTypes.M109,       18 }, // Direct support artillery battalion
                { Intel_WeaponTypes.HeavyMortar,   18 },
                { Intel_WeaponTypes.M163,      4 }, // Vulcan air defense guns
            };
            _intelProfiles[IntelProfileTypes.US_HEAVY_MECH_BDE_M1] = us_heavyMechBde_M1;

            // US Heavy Mechanized Brigade - M60A3 (Division 86 structure)
            var us_heavyMechBde_M60A3 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      2800 },
                { Intel_WeaponTypes.M60A3,     58 }, // 1 × Tank BN (58 tanks)
                { Intel_WeaponTypes.M2,        108 }, // 2 × Mech Inf BN (54 Bradleys each)
                { Intel_WeaponTypes.M3,         18 }, // Scout vehicles across battalions + brigade recon
                { Intel_WeaponTypes.M113,       32 }, // Command posts, medical, maintenance vehicles
                { Intel_WeaponTypes.ATGM,   40 }, // TOW missiles (Bradley + dismounted teams)
                { Intel_WeaponTypes.Manpad, 22 }, // Stinger teams distributed across brigade
                { Intel_WeaponTypes.M109,       18 }, // Direct support artillery battalion
                { Intel_WeaponTypes.HeavyMortar,   18 },
                { Intel_WeaponTypes.M163,      4 }, // Vulcan air defense guns
            };
            _intelProfiles[IntelProfileTypes.US_HEAVY_MECH_BDE_M60A3] = us_heavyMechBde_M60A3;

            // US Parachute Infantry Brigade (82nd Airborne)
            var us_paraBde_82nd = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,          1950 }, // 3 × Parachute Infantry BN (650 each)
                { Intel_WeaponTypes.M551,         18 }, // Light armor support (M551 Sheridan)
                { Intel_WeaponTypes.LightArtillery, 18 }, // 105mm howitzers (air-droppable)
                { Intel_WeaponTypes.ATGM,      54 }, // Dragon/TOW missile teams
                { Intel_WeaponTypes.Manpad,    36 }, // Stinger teams
                { Intel_WeaponTypes.AAA,       12 }, // Vulcan air defense guns
                { Intel_WeaponTypes.M113,          12 }, // Command posts and support vehicles
                { Intel_WeaponTypes.HeavyMortar,      18 }
            };
            _intelProfiles[IntelProfileTypes.US_PARA_BDE_82ND] = us_paraBde_82nd;

            // US Air Assault Infantry Brigade (101st Airborne)
            var us_airAssaultBde_101st = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,          2040 }, // 3 × Air Assault Infantry BN (680 each)
                { Intel_WeaponTypes.LightArtillery, 18 }, // 105mm howitzers (helicopter-mobile)
                { Intel_WeaponTypes.HeavyMortar,      18 },
                { Intel_WeaponTypes.ATGM,      48 }, // TOW missile teams
                { Intel_WeaponTypes.Manpad,    30 }, // Stinger teams
                { Intel_WeaponTypes.AH64,          18 }, // Organic attack helicopters (brigade aviation)
                { Intel_WeaponTypes.OH58,          12 }, // Scout helicopters
                { Intel_WeaponTypes.M113,           8 }, // Command posts and support vehicles
            };
            _intelProfiles[IntelProfileTypes.US_AIR_ASSAULT_BDE_101ST] = us_airAssaultBde_101st;

            // US Armored Cavalry Squadron (ACR) - Corps reconnaissance squadron
            var us_armoredCavSqdn = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1500 }, // Cavalry personnel across troops + support
                { Intel_WeaponTypes.M1,        41 }, // M1 Abrams tanks distributed across troops
                { Intel_WeaponTypes.M3,         36 }, // M3 Bradley cavalry fighting vehicles
                { Intel_WeaponTypes.M113,       18 }, // Command posts, mortars, support vehicles
                { Intel_WeaponTypes.ATGM,   24 }, // TOW missiles (M3 Bradley + ground teams)
                { Intel_WeaponTypes.Manpad, 12 }, // Stinger teams for air defense
                { Intel_WeaponTypes.AH64,       26 }, // AH-64 Apache attack helicopters
                { Intel_WeaponTypes.OH58,       12 }, // OH-58 scout helicopters
                { Intel_WeaponTypes.M109,        8 }, // Organic 155mm artillery battery
                { Intel_WeaponTypes.M163,      4 }, // Vulcan air defense guns
            };
            _intelProfiles[IntelProfileTypes.US_ARMORED_CAV_SQDN] = us_armoredCavSqdn;

            // US Division Artillery Battalion (DIVARTY)
            var us_divisionArtilleryBde_M109 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1050 }, // Artillery personnel across 4 battalions
                { Intel_WeaponTypes.M109,       54 }, // 3 × 155mm SP howitzer battalions (18 each)
                { Intel_WeaponTypes.M113,       48 }, // Fire direction, supply, maintenance vehicles
                { Intel_WeaponTypes.Manpad, 12 }, // Stinger teams for air defense
                { Intel_WeaponTypes.M3,          6 },          // Forward observer vehicles
            };
            _intelProfiles[IntelProfileTypes.US_ARTILLERY_BDE_M109] = us_divisionArtilleryBde_M109;

            // US Division Artillery Battalion (DIVARTY) - MLRS focused
            var us_divisionArtilleryBde_MLRS = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1020 }, // Artillery personnel across 4 battalions
                { Intel_WeaponTypes.MLRS,       18 }, // 1 × MLRS battalion
                { Intel_WeaponTypes.M113,       48 }, // Fire direction, supply, maintenance vehicles
                { Intel_WeaponTypes.Manpad, 12 }, // Stinger teams for air defense
                { Intel_WeaponTypes.M3,          6 }, // Forward observer vehicles
            };
            _intelProfiles[IntelProfileTypes.US_ARTILLERY_BDE_MLRS] = us_divisionArtilleryBde_MLRS;

            // US Aviation Attack Brigade
            var us_aviationAttackBde = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1350 },  // Aviation personnel (pilots, crew, maintenance)
                { Intel_WeaponTypes.AH64,  54 },  // 3 × AH-64 Apache attack battalions (18 each)
                { Intel_WeaponTypes.OH58,  18 },  // OH-58 Kiowa scout helicopters
                { Intel_WeaponTypes.M113,  48 },  // Ground support and maintenance vehicles
            };
            _intelProfiles[IntelProfileTypes.US_AVIATION_ATTACK_BDE] = us_aviationAttackBde;

            // US Engineer Brigade
            var us_engineerBde = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      2400 }, // Engineer personnel across battalions
                { Intel_WeaponTypes.M113,       72 }, // Engineer vehicles and bridging equipment
                { Intel_WeaponTypes.M60A3,     12 }, // Engineer tanks with dozer blades
                { Intel_WeaponTypes.Manpad, 18 }, // Stinger teams
                { Intel_WeaponTypes.ATGM,   24 }, // TOW missiles for defensive positions
            };
            _intelProfiles[IntelProfileTypes.US_ENGINEER_BDE] = us_engineerBde;

            // US Air Defense Brigade- Hawk SAMs
            var us_airDefenseBde_Hawk = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1100 }, // Air defense personnel
                { Intel_WeaponTypes.HAWK,       18 }, // Hawk SAM batteries
                { Intel_WeaponTypes.M163,      4 }, // Vulcan air defense guns
                { Intel_WeaponTypes.Manpad, 12 }, // Stinger teams distributed throughout
                { Intel_WeaponTypes.M113,       24 }, // Command and control vehicles
            };
            _intelProfiles[IntelProfileTypes.US_AIR_DEFENSE_BDE_HAWK] = us_airDefenseBde_Hawk;

            // US Air Defense Brigade- Chaparral
            var us_airDefenseBde_Chap = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,       900 }, // Air defense personnel
                { Intel_WeaponTypes.CHAP,     18 }, // Chaparral mobile SAM systems
                { Intel_WeaponTypes.M163,      4 }, // Vulcan air defense guns
                { Intel_WeaponTypes.Manpad, 12 }, // Stinger teams distributed throughout
                { Intel_WeaponTypes.M113,       24 }, // Command and control vehicles
            };
            _intelProfiles[IntelProfileTypes.US_AIR_DEFENSE_BDE_CHAPARRAL] = us_airDefenseBde_Chap;

            // US Fighter Wing - F-15 Eagle
            var us_fighterWing_F15 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.F15, 36 }
            };
            _intelProfiles[IntelProfileTypes.US_FIGHTER_WING_F15] = us_fighterWing_F15;

            // US Fighter Wing - F-4 Phantom II
            var us_fighterWing_F4 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.F4, 36 }
            };
            _intelProfiles[IntelProfileTypes.US_FIGHTER_WING_F4] = us_fighterWing_F4;

            // US Fighter Wing - F-16 Fighting Falcon
            var us_fighterWing_F16 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.F16, 36 }
            };
            _intelProfiles[IntelProfileTypes.US_FIGHTER_WING_F16] = us_fighterWing_F16;

            // US Tactical Fighter Wing - A-10 Thunderbolt II
            var us_tacticalWing_A10 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.A10, 36 }
            };
            _intelProfiles[IntelProfileTypes.US_TACTICAL_WING_A10] = us_tacticalWing_A10;

            // US Bomber Wing - Mixed Types
            var us_bomberWing_F111 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.F111, 18 },
            };
            _intelProfiles[IntelProfileTypes.US_BOMBER_WING_F111] = us_bomberWing_F111;

            // US Bomber Wing - Mixed Types
            var us_bomberWing_F117 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.F117, 18 },
            };
            _intelProfiles[IntelProfileTypes.US_BOMBER_WING_F117] = us_bomberWing_F117;

            // US Reconnaissance Squadron - SR-71 Blackbird
            var us_reconSqdn_SR71 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.SR71, 12 }
            };
            _intelProfiles[IntelProfileTypes.US_RECON_SQDN_SR71] = us_reconSqdn_SR71;

            // US AWACS Squadron - E-3 Sentry
            var us_awacsSqdn_E3 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.E3, 6 }
            };
            _intelProfiles[IntelProfileTypes.US_AWACS_E3] = us_awacsSqdn_E3;

            #endregion

            //-------------------------------------------------------------//

            #region FRG Profiles

            // FRG Panzer Brigade - Leopard 2
            var frg_panzerBde_Leo2 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      2200 }, // Brigade personnel across 4 battalions + support
                { Intel_WeaponTypes.LEOPARD2, 116 }, // 2× Panzer BN (44 each) + Mixed BN tank companies (28) = 116 tanks
                { Intel_WeaponTypes.MARDER,     58 }, // 1× PzGren BN (44) + Mixed BN mech company (14) = 58 Marders  
                { Intel_WeaponTypes.M113,       24 }, // Command posts, medical, maintenance, mortar carriers
                { Intel_WeaponTypes.LUCHS,      12 }, // Brigade reconnaissance platoon (German equivalent)
                { Intel_WeaponTypes.ATGM,   32 }, // Milan AT teams (Marder-mounted + dismounted)
                { Intel_WeaponTypes.Manpad, 24 }, // Roland/Stinger air defense sections
                { Intel_WeaponTypes.M109,       18 }, // Organic artillery battalion (155mm SP)
                { Intel_WeaponTypes.GEPARD,    8 }, // Gepard air defense guns (brigade level)
                { Intel_WeaponTypes.HeavyMortar,   12 }, // 120mm mortars distributed across battalions
            };
            _intelProfiles[IntelProfileTypes.FRG_PANZER_BDE_LEO2] = frg_panzerBde_Leo2;

            // FRG Panzer Brigade - Leopard 1
            var frg_panzerBde_Leo1 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      2200 }, // Brigade personnel across 4 battalions + support
                { Intel_WeaponTypes.LEOPARD1, 116 }, // 2× Panzer BN (44 each) + Mixed BN tank companies (28) = 116 tanks
                { Intel_WeaponTypes.MARDER,     58 }, // 1× PzGren BN (44) + Mixed BN mech company (14) = 58 Marders  
                { Intel_WeaponTypes.M113,       24 }, // Command posts, medical, maintenance, mortar carriers
                { Intel_WeaponTypes.LUCHS,      12 }, // Brigade reconnaissance platoon (Luchs 8x8)
                { Intel_WeaponTypes.ATGM,   32 }, // Milan AT teams (Marder-mounted + dismounted)
                { Intel_WeaponTypes.Manpad, 24 }, // Roland/Stinger air defense sections
                { Intel_WeaponTypes.M109,       18 }, // Organic artillery battalion (155mm SP)
                { Intel_WeaponTypes.GEPARD,    8 }, // Gepard air defense guns (brigade level)
                { Intel_WeaponTypes.HeavyMortar,   12 }, // 120mm mortars distributed across battalions
            };
            _intelProfiles[IntelProfileTypes.FRG_PANZER_BDE_LEO1] = frg_panzerBde_Leo1;

            // FRG Panzergrenadier Brigade
            var frg_pzgrenBde = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      2600 }, // Brigade personnel across 4 battalions + support
                { Intel_WeaponTypes.LEOPARD1,  58 }, // 1× Panzer BN (44) + Mixed PzGren BN tank company (14) = 58 tanks
                { Intel_WeaponTypes.MARDER,    102 }, // 2× PzGren BN (44 each) + Mixed PzGren BN mech companies (14) = 102 Marders
                { Intel_WeaponTypes.M113,       32 }, // Command posts, medical, maintenance, MTW carriers for lighter companies
                { Intel_WeaponTypes.LUCHS,      12 }, // Brigade reconnaissance platoon (Luchs 8x8)
                { Intel_WeaponTypes.ATGM,   40 }, // Milan AT teams (higher count due to infantry emphasis)
                { Intel_WeaponTypes.Manpad, 28 }, // Roland/Stinger air defense sections (more infantry coverage)
                { Intel_WeaponTypes.M109,       18 }, // Organic artillery battalion (155mm SP)
                { Intel_WeaponTypes.GEPARD,    8 }, // Gepard air defense guns (brigade level)
                { Intel_WeaponTypes.HeavyMortar,   18 }, // 120mm mortars (higher count for infantry support)
            };
            _intelProfiles[IntelProfileTypes.FRG_PZGREN_BDE_MARDER] = frg_pzgrenBde;

            // FRG Artillery Brigade
            var frg_artilleryBde_M109 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1300 }, // Artillery personnel (gunners, fire direction, logistics)
                { Intel_WeaponTypes.M109,       48 }, // 3× Artillery BN (24× M109 155mm SP howitzers each)
                { Intel_WeaponTypes.M113,       48 }, // Fire direction centers, survey, meteorological, ammunition carriers
                { Intel_WeaponTypes.LUCHS,       8 }, // Artillery reconnaissance and forward observer teams
                { Intel_WeaponTypes.Manpad, 16 }, // Roland/Stinger air defense (counter-battery protection)
                { Intel_WeaponTypes.GEPARD,    4 }, // Enhanced air defense for high-value artillery assets
            };
            _intelProfiles[IntelProfileTypes.FRG_ARTILLERY_BDE_M109] = frg_artilleryBde_M109;

            // FRG Artillery Brigade
            var frg_artilleryBde_MLRS = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1350 }, // Artillery personnel (gunners, fire direction, logistics)
                { Intel_WeaponTypes.MLRS,       24 }, // 3× MLRS BN
                { Intel_WeaponTypes.M113,       48 }, // Fire direction centers, survey, meteorological, ammunition carriers
                { Intel_WeaponTypes.LUCHS,       8 }, // Artillery reconnaissance and forward observer teams
                { Intel_WeaponTypes.Manpad, 16 }, // Roland/Stinger air defense (counter-battery protection)
                { Intel_WeaponTypes.GEPARD,    4 }, // Enhanced air defense for high-value artillery assets
            };
            _intelProfiles[IntelProfileTypes.FRG_ARTILLERY_BDE_MLRS] = frg_artilleryBde_MLRS;

            // FRG Airborne Brigade
            var frg_luftlandeBde = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,       1900 }, // Fallschirmjäger personnel across 3 parachute battalions + support
                { Intel_WeaponTypes.M113,       18 }, // Limited M113 for command posts (helicopter-transportable)
                { Intel_WeaponTypes.LUCHS,       6 }, // Reduced reconnaissance (air mobility constraints)
                { Intel_WeaponTypes.ATGM,   54 }, // Heavy Milan AT emphasis (anti-tank role)
                { Intel_WeaponTypes.Manpad, 36 }, // Stinger teams for immediate air defense
                { Intel_WeaponTypes.HeavyMortar,   24 }, // 120mm mortars (air-droppable fire support)
                { Intel_WeaponTypes.AAA,     8 }, // Light air defense guns (20mm)
                { Intel_WeaponTypes.BO105,      72 }, // Organic utility helicopters for mobility
            };
            _intelProfiles[IntelProfileTypes.FRG_LUFTLANDE_BDE] = frg_luftlandeBde;

            // FRG Mountain Brigade
            var frg_mountainBde = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,         2150 }, // Gebirgsjäger personnel (mountain warfare specialists)
                { Intel_WeaponTypes.M113,          72 }, // Limited M113 for command posts and support
                { Intel_WeaponTypes.LUCHS,          8 }, // Reconnaissance (limited by terrain constraints)
                { Intel_WeaponTypes.ATGM,      42 }, // Milan AT teams (defensive emphasis in mountains)
                { Intel_WeaponTypes.Manpad,    32 }, // Stinger teams (air threat in confined terrain)
                { Intel_WeaponTypes.LightArtillery, 36 }, // Pack howitzers and mountain mortars (105mm/120mm)
                { Intel_WeaponTypes.AAA,       12 }, // Light air defense (20mm for valley defense)
                { Intel_WeaponTypes.BO105,          8 }, // Utility helicopters for mountain resupply
            };
            _intelProfiles[IntelProfileTypes.FRG_MOUNTAIN_BDE] = frg_mountainBde;

            // FRG Air Defense Brigade
            var frg_airDefenseBde_HAWK = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1100 }, // Air defense personnel (radar, missile, gun crews)
                { Intel_WeaponTypes.HAWK,       18 }, // 1× Hawk SAM battalion (medium-range area defense)
                { Intel_WeaponTypes.GEPARD,    3 }, // 2× Gepard battalions (radar-guided 35mm guns)
                { Intel_WeaponTypes.M113,       16 }, // Command posts, radar vehicles, support systems
                { Intel_WeaponTypes.LUCHS,       6 }, // Forward air defense reconnaissance
                { Intel_WeaponTypes.Manpad, 12 }, // Stinger teams for gap coverage and mobility
            };
            _intelProfiles[IntelProfileTypes.FRG_AIR_DEFENSE_BDE_HAWK] = frg_airDefenseBde_HAWK;

            // FRG Air Defense Brigade
            var frg_airDefenseBde_ROLAND = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,       950 }, // Air defense personnel (radar, missile, gun crews)
                { Intel_WeaponTypes.ROLAND,   18 }, // 1× Hawk SAM battalion (medium-range area defense)
                { Intel_WeaponTypes.GEPARD,    3 }, // 2× Gepard battalions (radar-guided 35mm guns)
                { Intel_WeaponTypes.M113,       32 }, // Command posts, radar vehicles, support systems
                { Intel_WeaponTypes.LUCHS,       6 }, // Forward air defense reconnaissance
                { Intel_WeaponTypes.Manpad, 12 }, // Stinger teams for gap coverage and mobility
            };
            _intelProfiles[IntelProfileTypes.FRG_AIR_DEFENSE_BDE_ROLAND] = frg_airDefenseBde_ROLAND;

            // FRG Air Defense Brigade
            var frg_airDefenseBde_GEPARD = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,       850 }, // Air defense personnel (radar, missile, gun crews)
                { Intel_WeaponTypes.GEPARD,   18 }, // 2× Gepard battalions (radar-guided 35mm guns)
                { Intel_WeaponTypes.M113,       32 }, // Command posts, radar vehicles, support systems
                { Intel_WeaponTypes.LUCHS,       6 }, // Forward air defense reconnaissance
                { Intel_WeaponTypes.Manpad, 12 }, // Stinger teams for gap coverage and mobility
            };
            _intelProfiles[IntelProfileTypes.FRG_AIR_DEFENSE_BDE_GEPARD] = frg_airDefenseBde_GEPARD;

            // FRG Air Defense Brigade
            var frg_aviation_Bde = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.BO105, 54 },
            };
            _intelProfiles[IntelProfileTypes.FRG_AVIATION_BDE_BO105] = frg_aviation_Bde;

            // FRG Air Defense Brigade
            var frg_fighterWing = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.TORNADO_IDS, 36 },
            };
            _intelProfiles[IntelProfileTypes.FRG_FIGHTER_WING_TORNADO_IDS] = frg_fighterWing;

            #endregion

            //-------------------------------------------------------------//

            #region UK Profiles

            // UK Armoured Brigade - Challenger 1 (Heavy Type)
            var uk_armouredBde_Challenger = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,         1880 }, // Brigade personnel (580+720+580 for units + support)
                { Intel_WeaponTypes.CHALLENGER1, 116 }, // 2× Armoured Regiments (58 each) = 116 tanks
                { Intel_WeaponTypes.WARRIOR,       45 }, // 1× Mechanised Infantry Battalion 
                { Intel_WeaponTypes.FV432,         26 }, // Command, medical, support vehicles across brigade
                { Intel_WeaponTypes.SCIMITAR,       8 }, // Brigade reconnaissance troop (CVR(T))
                { Intel_WeaponTypes.ATGM,      30 }, // Milan AT (Warrior-mounted + dismounted teams)
                { Intel_WeaponTypes.Manpad,    16 }, // Javelin SAM teams across battalions
                { Intel_WeaponTypes.M109,          18 }, // Organic Royal Artillery regiment (155mm SP)
                { Intel_WeaponTypes.LightMortar,       18 }, // 81mm mortars distributed across battalions
                { Intel_WeaponTypes.SAM_RAPIER,         6 }, // Rapier air defense missiles (brigade level)
            };
            _intelProfiles[IntelProfileTypes.UK_ARMOURED_BDE_CHALLENGER] = uk_armouredBde_Challenger;

            // UK Mechanised Brigade - Warrior IFV (Infantry-heavy: 1 tank + 2 mech infantry)
            var uk_mechanisedBde_Warrior = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,        2020 },  // Brigade personnel (580+720+720 for units + support)
                { Intel_WeaponTypes.CHALLENGER1, 58 },  // 1× Armoured Regiment
                { Intel_WeaponTypes.WARRIOR,      90 },  // 2× Mechanised Infantry Battalions (45 each)
                { Intel_WeaponTypes.FV432,        18 },  // Command, medical, support vehicles
                { Intel_WeaponTypes.SCIMITAR,      8 },  // Brigade reconnaissance troop
                { Intel_WeaponTypes.ATGM,     54 },  // Milan AT (Warrior-mounted + dismounted)
                { Intel_WeaponTypes.Manpad,   24 },  // Javelin SAM teams
                { Intel_WeaponTypes.M109,         18 },  // Organic Royal Artillery regiment
                { Intel_WeaponTypes.LightMortar,      18 },  // 81mm mortars
                { Intel_WeaponTypes.SAM_RAPIER,        6 },  // Brigade air defense
            };
            _intelProfiles[IntelProfileTypes.UK_MECHANISED_BDE_WARRIOR] = uk_mechanisedBde_Warrior;

            // UK Infantry Brigade - FV432 APC (Traditional infantry with vehicular mobility)
            var uk_infantryBde_FV432 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      2040 }, // Brigade personnel for 3× infantry battalions + support
                { Intel_WeaponTypes.FV432,     156 }, // 3× Mechanised Infantry Battalions (52 each)
                { Intel_WeaponTypes.SCIMITAR,    8 }, // Brigade reconnaissance troop
                { Intel_WeaponTypes.ATGM,   48 }, // Milan AT teams (dismounted focus)
                { Intel_WeaponTypes.Manpad, 24 }, // Javelin SAM teams
                { Intel_WeaponTypes.M109,       18 }, // Organic Royal Artillery regiment
                { Intel_WeaponTypes.LightMortar,    18 }, // 81mm mortars
                { Intel_WeaponTypes.SAM_RAPIER,      6 }, // Brigade air defense
            };
            _intelProfiles[IntelProfileTypes.UK_INFANTRY_BDE_FV432] = uk_infantryBde_FV432;

            // UK Airmobile Brigade - Enhanced AT (1983-1988 experimental formation)
            var uk_airmobileBde = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1360 }, // 2× Infantry battalions + support (lighter structure)
                { Intel_WeaponTypes.ATGM,   72 }, // Heavy Milan AT load (primary AT capability)
                { Intel_WeaponTypes.Manpad, 24 }, // Javelin SAM teams
                { Intel_WeaponTypes.LYNX,       72 }, // RAF helicopter lift assets
                { Intel_WeaponTypes.LightMortar,    12 }, // 81mm mortars (air-portable)
                { Intel_WeaponTypes.FV432,       8 }, // Command vehicles only
            };
            _intelProfiles[IntelProfileTypes.UK_AIRMOBILE_BDE] = uk_airmobileBde;

            // UK Artillery Brigade - Royal Artillery
            var uk_artilleryBde = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1350 }, // 3× Artillery regiments + support personnel
                { Intel_WeaponTypes.M109,       54 }, // 3× Artillery regiments (18 each) - 155mm SP
                { Intel_WeaponTypes.FV432,      36 }, // Fire direction, supply, maintenance vehicles
                { Intel_WeaponTypes.SCIMITAR,    6 }, // Artillery reconnaissance
                { Intel_WeaponTypes.Manpad, 12 }, // Self-defense air defense
            };
            _intelProfiles[IntelProfileTypes.UK_ARTILLERY_BDE] = uk_artilleryBde;

            // UK Air Defense Brigade - Royal Artillery Air Defense
            var uk_airDefenseBde = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,   1200 }, // Air defense personnel + support
                { Intel_WeaponTypes.SAM_RAPIER,  12 }, // 3× Rapier squadrons (12 each)
                { Intel_WeaponTypes.AAA,  8 }, // Light AA guns (40mm Bofors)
                { Intel_WeaponTypes.FV432,   24 }, // Command, radar, support vehicles
                { Intel_WeaponTypes.SCIMITAR, 4 }, // Air defense reconnaissance
            };
            _intelProfiles[IntelProfileTypes.UK_AIR_DEFENSE_BDE] = uk_airDefenseBde;

            #endregion

            //-------------------------------------------------------------//

            #region French

            // French Division Blindée - AMX-30B (Armored Division)
            var fr_brigadeBlindee_AMX30 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1750 }, // Personnel (5-6 regiments + support)
                { Intel_WeaponTypes.AMX30,     80 }, // 2× Armored regiments (40 each)
                { Intel_WeaponTypes.AMX10P,     36 }, // 1× Mechanized infantry regiments (36 each)
                { Intel_WeaponTypes.VAB,        18 }, // Command, support vehicles
                { Intel_WeaponTypes.ERC90,      12 }, // Reconnaissance regiment
                { Intel_WeaponTypes.ATGM,    8 }, // Milan AT teams
                { Intel_WeaponTypes.Manpad, 18 }, // Mistral SAM teams
                { Intel_WeaponTypes.AUF1,       18 }, // Organic artillery regiment
                { Intel_WeaponTypes.HeavyMortar,   18 }, // 120mm mortars
            };
            _intelProfiles[IntelProfileTypes.FR_BRIGADE_BLINDEE_AMX30] = fr_brigadeBlindee_AMX30;

            // French Division d'Infanterie Mécanisée (Mechanized Infantry Division)
            var fr_brigadeInfMeca_AMX10P = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1950 }, // Division personnel (4 regiments + support)
                { Intel_WeaponTypes.ERC90,      12},  // Reconnaissance regiment
                { Intel_WeaponTypes.AMX10P,     72 }, // 3× Mechanized infantry regiments (36 each)
                { Intel_WeaponTypes.AMX30,     40 }, // 1× Light armor regiment (support)
                { Intel_WeaponTypes.VAB,        12 }, // Command, medical vehicles
                { Intel_WeaponTypes.ATGM,   18 }, // Milan AT teams
                { Intel_WeaponTypes.Manpad, 12 }, // Mistral SAM teams
                { Intel_WeaponTypes.AUF1,       18 }, // Organic artillery regiment
                { Intel_WeaponTypes.HeavyMortar,   18 }, // 120mm mortars
            };
            _intelProfiles[IntelProfileTypes.FR_BRIGADE_INF_MECA_AMX10P] = fr_brigadeInfMeca_AMX10P;

            // French Division d'Infanterie Motorisée (Motorized Infantry Division)
            var fr_brigadeInfMoto_VAB = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      1900 }, // Division personnel (4 regiments + support)
                { Intel_WeaponTypes.ERC90,      12 }, // Reconnaissance regiment
                { Intel_WeaponTypes.VAB,       135 }, // 3× Motorized infantry regiments (45 each)
                { Intel_WeaponTypes.AMX30,     20 }, // Light armor support squadron
                { Intel_WeaponTypes.ATGM,   18 }, // Milan AT teams
                { Intel_WeaponTypes.Manpad, 12 }, // Mistral SAM teams
                { Intel_WeaponTypes.AUF1,       18 }, // Organic artillery regiment
                { Intel_WeaponTypes.HeavyMortar,   18 }, // 120mm mortars
            };
            _intelProfiles[IntelProfileTypes.FR_BRIGADE_INF_MOTO_VAB] = fr_brigadeInfMoto_VAB;

            // 11e Division Parachutiste (Airborne Division)
            var fr_brigadePara = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,       2200 }, // Parachute infantry personnel (largest French division)
                { Intel_WeaponTypes.VAB,        24 }, // Light support vehicles (air-portable)
                { Intel_WeaponTypes.ERC90,      12 }, // Light armored cavalry regiment (air-droppable)
                { Intel_WeaponTypes.ATGM,   36 }, // Milan AT teams
                { Intel_WeaponTypes.Manpad, 24 }, // Mistral SAM teams
                { Intel_WeaponTypes.HeavyMortar,   36 }, // 120mm mortars (air-droppable)
            };
            _intelProfiles[IntelProfileTypes.FR_BRIGADE_PARACHUTISTE] = fr_brigadePara;

            // French Régiment d'Artillerie (Artillery Regiment)
            var fr_regimentArtillerie = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      700 }, // Artillery personnel
                { Intel_WeaponTypes.AUF1,      18 }, // AUF1 155mm SP howitzers
                { Intel_WeaponTypes.VAB,       10 }, // Fire direction, supply vehicles
                { Intel_WeaponTypes.Manpad, 6 }, // Self-defense Mistral teams
            };
            _intelProfiles[IntelProfileTypes.FR_REGIMENT_ARTILLERIE] = fr_regimentArtillerie;

            // French Régiment de Défense Antiaérienne (Air Defense Regiment)
            var fr_regimentDefenseAA = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,       650 }, // Air defense personnel
                { Intel_WeaponTypes.ROLAND,   18 }, // Roland air defense missiles
                { Intel_WeaponTypes.AAA,    12 }, // Light AA guns (20mm)
                { Intel_WeaponTypes.Manpad, 36 }, // Mistral SAM teams
                { Intel_WeaponTypes.VAB,        12 }, // Command, radar vehicles
            };
            _intelProfiles[IntelProfileTypes.FR_REGIMENT_DEFENSE_AA] = fr_regimentDefenseAA;

            // French Escadron de Chasse (Fighter Squadron) - Mirage 2000
            var fr_fighterSquadron_Mirage2000 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.MIRAGE2000, 36 },
            };
            _intelProfiles[IntelProfileTypes.FR_FIGHTER_WING_MIRAGE2000] = fr_fighterSquadron_Mirage2000;

            // French Escadron de Chasse (Fighter Squadron) - Jaguar
            var fr_fighterSquadron_Jaguar = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.JAGUAR, 36 },
            };
            _intelProfiles[IntelProfileTypes.FR_ATTACK_WING_JAGUAR] = fr_fighterSquadron_Jaguar;

            #endregion

            //-------------------------------------------------------------//

            #region Arab Irregulars Profiles

            // Mujahideen Guerrilla Infantry Regiment
            var mj_infGuerrilla = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,    1200 }, // Regiment personnel (guerrilla fighters)
                { Intel_WeaponTypes.RPG7,          36 }, // RPG-7 anti-tank teams
                { Intel_WeaponTypes.LightMortar,   18 }, // 82mm mortar teams
                { Intel_WeaponTypes.RecoilessRifle,12 }, // Recoilless rifle teams
                { Intel_WeaponTypes.Manpad,         8 }, // SA-7/Stinger teams
                { Intel_WeaponTypes.AAA,            6 }, // DShK/ZU-23 positions
            };
            _intelProfiles[IntelProfileTypes.MJ_INF_GUERRILLA] = mj_infGuerrilla;

            // Mujahideen Special Forces/Commando Regiment
            var mj_specCommando = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,   800 }, // Regiment personnel (elite fighters)
                { Intel_WeaponTypes.RPG7,        48 }, // More RPG-7 teams (better equipped)
                { Intel_WeaponTypes.LightMortar, 18 }, // More mortar teams
                { Intel_WeaponTypes.Manpad,      20 }, // Better air defense
                { Intel_WeaponTypes.ATGM,        12 }, // Limited advanced ATGMs (TOW/Dragon)
            };
            _intelProfiles[IntelProfileTypes.MJ_SPEC_COMMANDO] = mj_specCommando;

            // Mujahideen Horse Cavalry Regiment
            var mj_cavHorse = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,    1075 }, // Regiment personnel (mounted infantry)
                { Intel_WeaponTypes.RPG7,          48 }, // Portable anti-tank weapons
                { Intel_WeaponTypes.LightMortar,   12 }, // Light mortars (packable)
                { Intel_WeaponTypes.RecoilessRifle, 8 }, // Limited heavy weapons
                { Intel_WeaponTypes.Manpad,        10 }, // Air defense teams
            };
            _intelProfiles[IntelProfileTypes.MJ_CAV_HORSE] = mj_cavHorse;

            // Mujahideen Air Defense Regiment
            var mj_adManpad = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,700 }, // Regiment personnel (AD specialists)
                { Intel_WeaponTypes.Manpad,   24 }, // Primary air defense (SA-7/Stinger)
                { Intel_WeaponTypes.AAA,      24 }, // Heavy machine guns/AAA
            };
            _intelProfiles[IntelProfileTypes.MJ_AA] = mj_adManpad;

            // Mujahideen Mortar Regiment
            var mj_artMortarLight = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,     700 }, // Regiment personnel (mortar crews)
                { Intel_WeaponTypes.LightMortar,   54 }, // Primary fire support
                { Intel_WeaponTypes.RecoilessRifle, 8 }, // Direct fire support
                { Intel_WeaponTypes.RPG7,          12 }, // Infantry protection
                { Intel_WeaponTypes.Manpad,         6 }, // Air defense
            };
            _intelProfiles[IntelProfileTypes.MJ_ART_LIGHT_MORTAR] = mj_artMortarLight;

            // Mujahideen Heavy Mortar Regiment
            var mj_artMortarHeavy = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,      800 }, // Regiment personnel (artillery crews)
                { Intel_WeaponTypes.HeavyMortar,    36 }, // Light howitzers/mountain guns
                { Intel_WeaponTypes.LightMortar,    12 }, // Supplemental mortars
                { Intel_WeaponTypes.RecoilessRifle, 12 }, // Direct fire capability
                { Intel_WeaponTypes.RPG7,           18 }, // Infantry defense
                { Intel_WeaponTypes.Manpad,          8 }, // Limited air defense
            };
            _intelProfiles[IntelProfileTypes.MJ_ART_HEAVY_MORTAR] = mj_artMortarHeavy;

            #endregion

            //-------------------------------------------------------------//

            #region Arab Army Tank Regiments

            // Arab Tank Regiment - T-55
            var arab_tankReg_T55 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 950 }, // Regiment personnel (tank crews + support)
                { Intel_WeaponTypes.T55A, 95 }, // T-55 main battle tanks (3 battalions)
                { Intel_WeaponTypes.MTLB, 12 }, // Command post, medical vehicles
                { Intel_WeaponTypes.BRDM2, 8 }, // Reconnaissance vehicles
                { Intel_WeaponTypes.ATGM, 6 }, // AT-3 Sagger teams
                { Intel_WeaponTypes.Manpad, 8 }, // SA-7 teams for air defense
                { Intel_WeaponTypes.AAA, 4 }, // ZU-23 air defense guns
            };
            _intelProfiles[IntelProfileTypes.ARAB_TANK_REG_T55] = arab_tankReg_T55;

            // Arab Tank Regiment - T-72
            var arab_tankReg_T72 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 950 }, // Regiment personnel (tank crews + support)
                { Intel_WeaponTypes.T72A, 95 }, // T-72 main battle tanks (3 battalions)
                { Intel_WeaponTypes.BTR70, 12 }, // Command post, medical vehicles
                { Intel_WeaponTypes.BRDM2, 8 }, // Reconnaissance vehicles
                { Intel_WeaponTypes.ATGM, 8 }, // AT-5 Spandrel teams
                { Intel_WeaponTypes.Manpad, 10 }, // SA-14 teams for air defense
                { Intel_WeaponTypes.ZSU23, 4 }, // ZSU-23-4 air defense guns
            };
            _intelProfiles[IntelProfileTypes.ARAB_TANK_REG_T72] = arab_tankReg_T72;

            // Arab Tank Regiment - M60A3
            var arab_tankReg_M60A3 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 880 }, // Regiment personnel (tank crews + support)
                { Intel_WeaponTypes.M60A3, 95 }, // M60A3 main battle tanks (3 battalions)
                { Intel_WeaponTypes.M113, 12 }, // Command post, medical vehicles
                { Intel_WeaponTypes.M3, 8 }, // Reconnaissance vehicles
                { Intel_WeaponTypes.ATGM, 8 }, // TOW missile teams
                { Intel_WeaponTypes.Manpad, 6 }, // Stinger teams for air defense
                { Intel_WeaponTypes.AAA, 4 }, // M163 Vulcan air defense guns
            };
            _intelProfiles[IntelProfileTypes.ARAB_TANK_REG_M60A3] = arab_tankReg_M60A3;

            // Arab Tank Regiment - M1 Abrams
            var arab_tankReg_M1 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 920 }, // Regiment personnel (tank crews + support)
                { Intel_WeaponTypes.M1, 95 }, // M1 Abrams main battle tanks (3 battalions)
                { Intel_WeaponTypes.M113, 35 }, // Command post, medical vehicles
                { Intel_WeaponTypes.M3, 8 }, // Reconnaissance vehicles
                { Intel_WeaponTypes.ATGM, 10 }, // TOW missile teams
                { Intel_WeaponTypes.Manpad, 8 }, // Stinger teams for air defense
                { Intel_WeaponTypes.M163, 4 }, // M163 Vulcan air defense guns
            };
            _intelProfiles[IntelProfileTypes.ARAB_TANK_REG_M1] = arab_tankReg_M1;

            #endregion // Arab Army Tank Regiments

            #region Arab Army Mechanized Infantry Regiments

            // Arab Mechanized Infantry Regiment - BMP-1
            var arab_mechReg_BMP1 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1800 }, // Regiment personnel (infantry + crews)
                { Intel_WeaponTypes.BMP1, 90 }, // BMP-1 infantry fighting vehicles
                { Intel_WeaponTypes.T55A, 31 }, // Tank support battalion
                { Intel_WeaponTypes.MTLB, 8 }, // Command, medical vehicles
                { Intel_WeaponTypes.ATGM, 18 }, // AT-3 Sagger teams
                { Intel_WeaponTypes.Manpad, 12 }, // SA-7 teams
                { Intel_WeaponTypes.LightArtillery, 18 }, // 120mm howitzers
                { Intel_WeaponTypes.LightMortar, 24 }, // 82mm mortars

            };
            _intelProfiles[IntelProfileTypes.ARAB_MECH_REG_BMP1] = arab_mechReg_BMP1;

            // Arab Mechanized Infantry Regiment - M2 Bradley
            var arab_mechReg_M2 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1900 }, // Regiment personnel (infantry + crews)
                { Intel_WeaponTypes.M2, 90 }, // M2 Bradley infantry fighting vehicles
                { Intel_WeaponTypes.M60A3, 31 }, // Tank support battalion
                { Intel_WeaponTypes.M113, 8 }, // Command, medical vehicles
                { Intel_WeaponTypes.ATGM, 20 }, // TOW missile teams
                { Intel_WeaponTypes.Manpad, 10 }, // Stinger teams
                { Intel_WeaponTypes.LightArtillery, 18 }, // Howitzers
                { Intel_WeaponTypes.LightMortar, 24 }, // 82mm mortars
            };
            _intelProfiles[IntelProfileTypes.ARAB_MECH_REG_M2] = arab_mechReg_M2;

            // Arab Mechanized Infantry Regiment - BTR-70
            var arab_mechReg_BTR70 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1800 }, // Regiment personnel (infantry + crews)
                { Intel_WeaponTypes.BTR70, 90 }, // BTR-70 armoured personnel carriers
                { Intel_WeaponTypes.T55A, 31 }, // Tank support battalion
                { Intel_WeaponTypes.ATGM, 14 }, // AT-3 Sagger teams
                { Intel_WeaponTypes.Manpad, 10 }, // SA-7 teams
                { Intel_WeaponTypes.LightArtillery, 18 }, // 120mm mortars
                { Intel_WeaponTypes.LightMortar, 24 }, // 82mm mortars
            };
            _intelProfiles[IntelProfileTypes.ARAB_MECH_REG_BTR70] = arab_mechReg_BTR70;

            // Arab Mechanized Infantry Regiment - M113
            var arab_mechReg_M113 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1900 }, // Regiment personnel (infantry + crews)
                { Intel_WeaponTypes.M113, 90 }, // M113 armoured personnel carriers
                { Intel_WeaponTypes.M60A3, 31 }, // Tank support battalion
                { Intel_WeaponTypes.ATGM, 16 }, // TOW missile teams
                { Intel_WeaponTypes.Manpad, 8 }, // Air defense teams
                { Intel_WeaponTypes.LightArtillery, 18 }, // 120mm mortars
                { Intel_WeaponTypes.LightMortar, 24 }, // 82mm mortars
            };
            _intelProfiles[IntelProfileTypes.ARAB_MECH_REG_M113] = arab_mechReg_M113;

            #endregion // Arab Army Mechanized Infantry Regiments

            #region Arab Army Infantry Regiments

            // Arab Motorized Infantry Regiment
            var arab_regMot = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1600 }, // Regiment personnel (motorized infantry)
                { Intel_WeaponTypes.ATGM, 12 }, // Anti-tank missile teams
                { Intel_WeaponTypes.Manpad, 8 }, // Portable air defense
                { Intel_WeaponTypes.AAA, 6 }, // Anti-aircraft guns
                { Intel_WeaponTypes.LightArtillery, 18 }, // 120mm mortars
                { Intel_WeaponTypes.LightMortar, 24 }, // 82mm mortars
            };
            _intelProfiles[IntelProfileTypes.ARAB_REG_MOT] = arab_regMot;

            // Arab Infantry Regiment
            var arab_regInf = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 1700 }, // Regiment personnel (foot infantry)
                { Intel_WeaponTypes.ATGM, 8 }, // Anti-tank missile teams
                { Intel_WeaponTypes.Manpad, 6 }, // Portable air defense
                { Intel_WeaponTypes.AAA, 4 }, // Anti-aircraft guns
                { Intel_WeaponTypes.LightArtillery, 18 }, // 120mm mortars
                { Intel_WeaponTypes.LightMortar, 24 }, // 82mm mortars
            };
            _intelProfiles[IntelProfileTypes.ARAB_REG_INF] = arab_regInf;

            #endregion // Arab Army Infantry Regiments

            #region Arab Army Artillery Regiments

            // Arab Heavy Artillery Regiment
            var arab_regHvyArt = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry,750 }, // Artillery personnel
                { Intel_WeaponTypes.HeavyArtillery, 36 }, // 152mm/155mm towed howitzers
                { Intel_WeaponTypes.MTLB, 18 }, // Fire direction, supply vehicles
                { Intel_WeaponTypes.Manpad, 8 }, // Air defense teams
                { Intel_WeaponTypes.AAA, 4 }, // Anti-aircraft guns
            };
            _intelProfiles[IntelProfileTypes.ARAB_REG_HVY_ART] = arab_regHvyArt;

            // Arab Light Artillery Regiment
            var arab_regLgtArt = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 700 }, // Artillery personnel
                { Intel_WeaponTypes.LightArtillery, 48 }, // 122mm towed howitzers
                { Intel_WeaponTypes.MTLB, 12 }, // Fire direction, supply vehicles
                { Intel_WeaponTypes.Manpad, 6 }, // Air defense teams
                { Intel_WeaponTypes.AAA, 4 }, // Anti-aircraft guns
            };
            _intelProfiles[IntelProfileTypes.ARAB_REG_LGT_ART] = arab_regLgtArt;

            // Arab Self-Propelled Artillery Regiment - 2S1
            var arab_spaReg_2S1 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 720 }, // Artillery personnel
                { Intel_WeaponTypes.SPA_2S1, 36 }, // 2S1 122mm self-propelled howitzers
                { Intel_WeaponTypes.BTR70, 12 }, // Fire direction, supply vehicles
                { Intel_WeaponTypes.Manpad, 8 }, // Air defense teams
                { Intel_WeaponTypes.ZSU23, 4 }, // ZSU-23-4 air defense
            };
            _intelProfiles[IntelProfileTypes.ARAB_SPA_REG_2S1] = arab_spaReg_2S1;

            // Arab Self-Propelled Artillery Regiment - M109
            var arab_spaReg_M109 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 750 }, // Artillery personnel
                { Intel_WeaponTypes.M109, 36 }, // M109 155mm self-propelled howitzers
                { Intel_WeaponTypes.M113, 12 }, // Fire direction, supply vehicles
                { Intel_WeaponTypes.Manpad, 6 }, // Air defense teams
                { Intel_WeaponTypes.M163, 4 }, // M163 Vulcan air defense
            };
            _intelProfiles[IntelProfileTypes.ARAB_SPA_REG_M109] = arab_spaReg_M109;

            // Arab Rocket Artillery Regiment - BM-21
            var arab_rocReg_BM21 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 680 }, // Artillery personnel
                { Intel_WeaponTypes.BM21, 18 }, // BM-21 Grad rocket launchers
                { Intel_WeaponTypes.BTR70, 18 }, // Fire direction, supply vehicles
                { Intel_WeaponTypes.Manpad, 8 }, // Air defense teams
                { Intel_WeaponTypes.ZSU23, 4 }, // ZSU-23-4 air defense
            };
            _intelProfiles[IntelProfileTypes.ARAB_ROC_REG_BM21] = arab_rocReg_BM21;

            // Arab Rocket Artillery Regiment - MLRS
            var arab_rocReg_MLRS = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 720 }, // Artillery personnel
                { Intel_WeaponTypes.MLRS, 18 }, // MLRS rocket launchers
                { Intel_WeaponTypes.M113, 18 }, // Fire direction, supply vehicles
                { Intel_WeaponTypes.Manpad, 6 }, // Air defense teams
                { Intel_WeaponTypes.M163, 4 }, // M163 Vulcan air defense
            };
            _intelProfiles[IntelProfileTypes.ARAB_ROC_REG_MLRS] = arab_rocReg_MLRS;

            #endregion // Arab Army Artillery Regiments

            #region Arab Army Reconnaissance Regiments

            // Arab Reconnaissance Regiment - BRDM
            var arab_rcnReg_BRDM = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 650 }, // Regiment personnel
                { Intel_WeaponTypes.BRDM2, 48 }, // BRDM-2 reconnaissance vehicles
                { Intel_WeaponTypes.BRDM2AT, 12 }, // BRDM-2 AT variant
                { Intel_WeaponTypes.BTR70, 8 }, // Support vehicles
                { Intel_WeaponTypes.Manpad, 8 }, // Air defense teams
            };
            _intelProfiles[IntelProfileTypes.ARAB_RCN_REG_BRDM] = arab_rcnReg_BRDM;

            // Arab Reconnaissance Regiment - M3 Bradley
            var arab_rcnReg_M3 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 680 }, // Regiment personnel
                { Intel_WeaponTypes.M3, 48 }, // M3 Bradley cavalry fighting vehicles
                { Intel_WeaponTypes.M113, 8 }, // Support vehicles
                { Intel_WeaponTypes.ATGM, 12 }, // TOW missile teams
                { Intel_WeaponTypes.Manpad, 6 }, // Air defense teams
            };

            _intelProfiles[IntelProfileTypes.ARAB_RCN_REG_M3] = arab_rcnReg_M3;

            #endregion // Arab Army Reconnaissance Regiments

            #region Arab Army Air Defense Regiments

            // Arab Self-Propelled Anti-Aircraft Regiment - ZSU-23-4
            var arab_spaaaReg_ZSU23 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 700 }, // Air defense personnel
                { Intel_WeaponTypes.ZSU23, 36 }, // ZSU-23-4 Shilka
                { Intel_WeaponTypes.BTR70, 12 }, // Command, supply vehicles
                { Intel_WeaponTypes.Manpad, 24 }, // SA-7/SA-14 teams
            };
            _intelProfiles[IntelProfileTypes.ARAB_SPAAA_REG_ZSU23] = arab_spaaaReg_ZSU23;

            // Arab SAM Regiment - S-75 Dvina (SA-2)
            var arab_samReg_S75 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 750 }, // SAM personnel
                { Intel_WeaponTypes.S75, 12 }, // S-75 Dvina SAM launchers
                { Intel_WeaponTypes.MTLB, 12 }, // Command, radar vehicles
                { Intel_WeaponTypes.AAA, 12 }, // Supporting AAA guns
            };
            _intelProfiles[IntelProfileTypes.ARAB_SAM_REG_S75] = arab_samReg_S75;

            // Arab SAM Regiment - S-125 Neva (SA-3)
            var arab_samReg_S125 = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 800 }, // SAM personnel
                { Intel_WeaponTypes.S125, 24 }, // S-125 Neva SAM launchers
                { Intel_WeaponTypes.BTR70, 12 }, // Command, radar vehicles
                { Intel_WeaponTypes.ZSU23, 8 }, // Supporting SPAAA
            };
            _intelProfiles[IntelProfileTypes.ARAB_SAM_REG_S125] = arab_samReg_S125;

            // Arab SAM Regiment - Hawk
            var arab_samReg_Hawk = new Dictionary<Intel_WeaponTypes, int>
            {
                { Intel_WeaponTypes.Infantry, 780 }, // SAM personnel
                { Intel_WeaponTypes.HAWK, 24 }, // MIM-23 Hawk SAM launchers
                { Intel_WeaponTypes.M113, 12 }, // Command, radar vehicles
                { Intel_WeaponTypes.M163, 8 }, // Supporting M163 Vulcan
            };
            _intelProfiles[IntelProfileTypes.ARAB_SAM_REG_HAWK] = arab_samReg_Hawk;

            #endregion // Arab Army Air Defense Regiments

            //-------------------------------------------------------------//
        }

        #endregion // Profile Database
    }
}
