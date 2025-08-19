using HammerAndSickle.Services;
using System;
using System.Collections.Generic;

namespace HammerAndSickle.Models
{
    public static class WeaponSystemsDatabase
    {
        #region Constants

        private const string CLASS_NAME = nameof(WeaponSystemsDatabase);
        
        // Centralized place to combat rating modifiers
        private const int MASSIVE_MALUS  = -10;
        private const int XXLARGE_MALUS  = -5;
        private const int XLARGE_MALUS   = -4;
        private const int LARGE_MALUS    = -3;
        private const int MEDIUM_MALUS   = -2;
        private const int SMALL_MALUS    = -1;
        private const int SMALL_BONUS    =  1;
        private const int MEDIUM_BONUS   =  2;
        private const int LARGE_BONUS    =  3;
        private const int XLARGE_BONUS   =  4;
        private const int XXLARGE_BONUS  =  5;
        private const int XXXLARGE_BONUS =  6;
        private const int MASSIVE_BONUS  =  10;

        #endregion // Constants

        #region Private Fields

        private static Dictionary<WeaponSystems, WeaponSystemProfile> _weaponSystemProfiles;
        private static bool _isInitialized = false;

        #endregion // Private Fields

        #region Public Properties

        /// <summary>
        /// Gets whether the database has been initialized with weapon system profiles.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the total number of weapon system profiles in the database.
        /// </summary>
        public static int ProfileCount => _weaponSystemProfiles?.Count ?? 0;

        #endregion // Public Properties

        #region Public Methods

        /// <summary>
        /// Initializes the weapon systems database with all game profiles.
        /// Must be called during game startup before any profile lookups.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    AppService.CaptureUiMessage("WeaponSystemsDatabase already initialized, skipping");
                    return;
                }

                _weaponSystemProfiles = new Dictionary<WeaponSystems, WeaponSystemProfile>();

                CreateAllWeaponSystemProfiles();

                _isInitialized = true;
                AppService.CaptureUiMessage($"WeaponSystemsDatabase initialized with {ProfileCount} profiles");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Initialize", e);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a weapon system profile by its enum identifier.
        /// </summary>
        /// <param name="weaponSystemID">The weapon system identifier to look up</param>
        /// <returns>The corresponding WeaponSystemProfile, or null if not found</returns>
        public static WeaponSystemProfile GetWeaponSystemProfile(WeaponSystems weaponSystemID)
        {
            try
            {
                if (!_isInitialized)
                {
                    AppService.CaptureUiMessage("WeaponSystemsDatabase not initialized - call Initialize() first");
                    return null;
                }

                if (weaponSystemID == WeaponSystems.DEFAULT)
                {
                    return null;
                }

                return _weaponSystemProfiles.TryGetValue(weaponSystemID, out var profile) ? profile : null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetWeaponSystemProfile", e);
                return null;
            }
        }

        /// <summary>
        /// Checks if a weapon system profile exists in the database by its enum identifier.
        /// </summary>
        /// <param name="weaponSystemID">The weapon system identifier to check</param>
        /// <returns>True if the profile exists, false otherwise</returns>
        public static bool HasWeaponSystemProfile(WeaponSystems weaponSystemID)
        {
            try
            {
                if (!_isInitialized)
                {
                    return false;
                }

                if (weaponSystemID == WeaponSystems.DEFAULT)
                {
                    return false;
                }

                return _weaponSystemProfiles.ContainsKey(weaponSystemID);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "HasWeaponSystemProfile", e);
                return false;
            }
        }

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Creates all weapon system profiles used in the game.
        /// This method contains the complete database of combat capabilities.
        /// </summary>
        private static void CreateAllWeaponSystemProfiles()
        {
            try
            {
                //Create Soviet profiles
                Create_TANK_T55A_Profile();
                Create_TANK_T64A_Profile();
                Create_TANK_T64B_Profile();
                Create_TANK_T72A_Profile();
                Create_TANK_T72B_Profile();
                Create_TANK_T80B_Profile();
                Create_TANK_T80U_Profile();
                Create_TANK_T80BV_Profile();

                Create_APC_MTLB_Profile();
                Create_APC_BTR70_Profile();
                Create_APC_BTR80_Profile();

                Create_IFV_BMP1_Profile();
                Create_IFV_BMP2_Profile();
                Create_IFV_BMP3_Profile();
                Create_IFV_BMD1_Profile();
                Create_IFV_BMD2_Profile();
                Create_IFV_BMD3_Profile();

                Create_RCN_BRDM2_Profile();
                Create_RCN_BRDM2AT_Profile();

                Create_SPA_2S1_Profile();
                Create_SPA_2S3_Profile();
                Create_SPA_2S5_Profile();
                Create_SPA_2S19_Profile();
                Create_ROC_BM21_Profile();
                Create_ROC_BM27_Profile();
                Create_ROC_BM30_Profile();

                Create_SPAAA_ZSU57_Profile();
                Create_SPAAA_ZSU23_Profile();
                Create_SPAAA_2K22_Profile();

                Create_SPSAM_9K31_Profile();

                Create_SAM_S75_Profile();
                Create_SAM_S125_Profile();
                Create_SAM_S300_Profile();

                Create_HEL_MI8T_Profile();
                Create_HEL_MI8AT_Profile();
                Create_HEL_MI24D_Profile();
                Create_HEL_MI24V_Profile();
                Create_HEL_MI28_Profile();

                Create_TRA_AN12_Profile();
                Create_AWACS_A50_Profile();
                Create_ASF_MIG21_Profile();
                Create_ASF_MIG23_Profile();
                Create_ASF_MIG25_Profile();
                Create_ASF_MIG29_Profile();
                Create_ASF_MIG31_Profile();
                Create_ASF_SU27_Profile();
                Create_ASF_SU47_Profile();

                Create_MRF_MIG27_Profile();

                Create_ATT_SU25_Profile();
                Create_ATT_SU25B_Profile();

                Create_BMB_SU24_Profile();
                Create_BMB_TU16_Profile();
                CreateBMB_TU22_Profile();
                Create_BMB_TU22M3_Profile();

                Create_RCNA_Mig25R_Profile();

                Create__TRA_TransportFlotilla_Profile();

                // US Tanks
                Create_TANK_M1_Profile();
                Create_TANK_M60A3_Profile();
                Create_TANK_M551_Profile();

                // US IFVs and APCs  
                Create_IFV_M2_Profile();
                Create_IFV_M3_Profile();
                Create_APC_M113_Profile();
                Create_APC_LVTP7_Profile();

                // US Artillery
                Create_SPA_M109_Profile();
                Create_ROC_MLRS_Profile();

                // US Air Defense
                Create_SPAAA_M163_Profile();
                Create_SPSAM_Chap_Profile();
                Create_SAM_Hawk_Profile();

                // US Helicopters
                Create_HEL_AH64_Profile();
                Create_HEL_OH58_Profile();

                // US Aircraft
                Create_AWACS_E3_Profile();
                Create_ASF_F15_Profile();
                Create_ASF_F4_Profile();
                Create_MRF_F16_Profile();
                Create_ATT_A10_Profile();
                Create_BMB_F111_Profile();
                Create_BMB_F117_Profile();
                Create_RCNA_SR71_Profile();

                // FRG
                Create_TANK_LEOPARD1_Profile();
                Create_TANK_LEOPARD2_Profile();
                Create_IFV_MARDER_Profile();
                Create_RCN_LUCHS_Profile();
                Create_SPAAA_Gepard_Profile();
                Create_HEL_BO105_Profile();
                Create_MRF_TORNADO_IDS_Profile();

                // UK
                Create_TANK_CHALLENGER1_Profile();
                Create_IFV_WARRIOR_Profile();
                Create_APC_FV432_Profile();
                Create_RCN_Scimitar_Profile();
                Create_SAM_Rapier_Profile();
                Create_HEL_LYNX_Profile();
                Create_MRF_GR1_Profile();

                // FRA
                Create_TANK_AMX30_Profile();
                Create_IFV_AMX10P_Profile();
                Create_APC_VAB_Profile();
                Create_RCN_ERC90_Profile();
                Create_SPA_AUF1_Profile();
                Create_SPSAM_Roland_Profile();
                Create_ASF_M2000_Profile();
                Create_ATT_JAGUAR_Profile();

                // Generic Profiles
                Create_GENERIC_AAA_Profile();
                Create_GENERIC_LightArt_Profile();
                Create_GENERIC_HeavytArt_Profile();
                Create_GENERIC_MANPAD_Profile();
                Create_GENERIC_ATGM_Profile();
                Create_GENERIC_RPG7_Profile();
                Create_GENERIC_LightMortar_Profile();
                Create_GENERIC_HeavyMortar_Profile();
                Create_GENERIC_RECOILLESS_RIFLE_Profile();
                Create_GENERIC_CAVALRY_Profile();
                Create_GENERIC_TRUCK_Profile();

                // Generic land bases
                Create_BASE_Landbase_Profile();
                Create_BASE_Airbase_Profile();
                Create_BASE_Depot_Profile();
                Create_BASE_Intel_Profile();

                // Generic infantry profiles
                Create_INF_REG_Profile();
                Create_INF_AB_Profile();
                Create_INF_AM_Profile();
                Create_INF_MAR_Profile();
                Create_INF_SPEC_Profile();
                Create_INF_ENG_Profile();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CreateAllWeaponSystemProfiles", e);
                throw;
            }
        }

        #endregion // Private Methods

        //-----------------------------------------------------------------------------------------

        #region Soviet Tanks

        /// <summary>
        /// T55A Profile
        /// </summary>
        private static void Create_TANK_T55A_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-55A MBT",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T55A,
                    CUConstants.PRESTIGE_TIER_1 + XXLARGE_MALUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack:  CUConstants.GEN1_TANK_HARD_ATTACK,
                    hardDefense: CUConstants.GEN1_TANK_HARD_DEFENSE,
                    softAttack:  CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange:    CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange:   CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange:   CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                // Set short name for UI display
                profile.SetShortName("T-55A");

                // Set turn availability in months.
                profile.SetTurnAvailable(300);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TANK_T55A] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CreateT80BProfile", e);
                throw;
            }
        }

        /// <summary>
        /// Creates the T-64A tank profile.
        /// </summary>
        private static void Create_TANK_T64A_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-64A MBT",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T64A,
                    CUConstants.PRESTIGE_TIER_2,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN2_TANK_HARD_ATTACK + SMALL_BONUS,
                    hardDefense: CUConstants.GEN2_TANK_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                // Set short name for UI display
                profile.SetShortName("T-64A");

                // Set turn availability in months.
                profile.SetTurnAvailable(348);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TANK_T64A] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CreateT64AProfile", e);
                throw;
            }
        }

        /// <summary>
        /// Creates the T-64B tank profile.
        /// </summary>
        private static void Create_TANK_T64B_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-64B MBT",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T64B,
                    CUConstants.PRESTIGE_TIER_3,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN3_TANK_HARD_ATTACK + MEDIUM_BONUS,
                    hardDefense: CUConstants.GEN3_TANK_HARD_DEFENSE + SMALL_BONUS,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                // Set short name for UI display
                profile.SetShortName("T-64B");

                // Set turn availability in months.
                profile.SetTurnAvailable(564);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TANK_T64B] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_TANK_T64B_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Creates the T-72A tank profile.
        /// </summary>
        private static void Create_TANK_T72A_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-72A MBT",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T72A,
                    CUConstants.PRESTIGE_TIER_2 + XXLARGE_MALUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN2_TANK_HARD_ATTACK + MEDIUM_BONUS,
                    hardDefense: CUConstants.GEN2_TANK_HARD_DEFENSE + SMALL_BONUS,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE + SMALL_MALUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                // Set short name for UI display
                profile.SetShortName("T-72A");

                // Set turn availability in months.
                profile.SetTurnAvailable(492);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TANK_T72A] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_TANK_T72A_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Creates the T-72B tank profile.
        /// </summary>
        private static void Create_TANK_T72B_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-72B MBT",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T72B,
                    CUConstants.PRESTIGE_TIER_3,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN3_TANK_HARD_ATTACK,
                    hardDefense: CUConstants.GEN3_TANK_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE + SMALL_MALUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                // Set short name for UI display
                profile.SetShortName("T-72B");

                // Set turn availability in months.
                profile.SetTurnAvailable(552);

                _weaponSystemProfiles[WeaponSystems.TANK_T72B] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_TANK_T72B_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Creates the T-80B main battle tank profile.
        /// Example of a modern Soviet main battle tank with strong armor and firepower.
        /// </summary>
        private static void Create_TANK_T80B_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-80B MBT",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T80B,
                    CUConstants.PRESTIGE_TIER_2 + XXLARGE_BONUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN2_TANK_HARD_ATTACK + SMALL_BONUS,
                    hardDefense: CUConstants.GEN2_TANK_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange:    CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange:   CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange:   CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                // Set short name for UI display
                profile.SetShortName("T-80B");

                // Set turn availability in months.
                profile.SetTurnAvailable(480);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TANK_T80B] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CreateT80BProfile", e);
                throw;
            }
        }

        /// <summary>
        /// Creates the T-80U tank profile.
        /// </summary>
        private static void Create_TANK_T80U_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-80U MBT",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T80U,
                    CUConstants.PRESTIGE_TIER_3 + XXLARGE_BONUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN3_TANK_HARD_ATTACK + MEDIUM_BONUS,
                    hardDefense: CUConstants.GEN3_TANK_HARD_DEFENSE + LARGE_BONUS,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange:    CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange:   CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange:   CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                // Set short name for UI display
                profile.SetShortName("T-80U");

                // Set turn availability in months.
                profile.SetTurnAvailable(564);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TANK_T80U] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CreateT80UProfile", e);
                throw;
            }
        }

        /// <summary>
        /// Creates the T-80BV tank profile.
        /// </summary>
        private static void Create_TANK_T80BV_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-80BV MBT",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T80BV,
                    CUConstants.PRESTIGE_TIER_4,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN4_TANK_HARD_ATTACK + SMALL_BONUS,
                    hardDefense: CUConstants.GEN4_TANK_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK + SMALL_BONUS,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                // Set short name for UI display
                profile.SetShortName("T-80BV");

                // Set turn availability in months.
                profile.SetTurnAvailable(615);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TANK_T80BV] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_TANK_T80BV_Profile), e);
                throw;
            }
        }

        #endregion // Soviet Tanks

        #region Soviet APCs

        private static void Create_APC_MTLB_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MT‑LB APC",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.APC_MTLB,
                    CUConstants.PRESTIGE_TIER_0,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_APC_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_APC_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_APC_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_APC_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.APC);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("MT-LB");

                // Set turn availability in months.
                profile.SetTurnAvailable(384);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.APC_MTLB] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_APC_MTLB_Profile), e);
                throw;
            }
        }

        private static void Create_APC_BTR70_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BTR‑70 APC",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.APC_BTR70,
                    CUConstants.PRESTIGE_TIER_0 + LARGE_BONUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_APC_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_APC_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_APC_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_APC_SOFT_DEFENSE + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.APC);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("BTR-70");

                // Set turn availability in months.
                profile.SetTurnAvailable(408);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.APC_BTR70] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_APC_BTR70_Profile), e);
                throw;
            }
        }

        private static void Create_APC_BTR80_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BTR‑80 APC",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.APC_BTR80,
                    CUConstants.PRESTIGE_TIER_0 + XLARGE_BONUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_APC_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_APC_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_APC_SOFT_ATTACK + SMALL_BONUS,
                    softDefense: CUConstants.BASE_APC_SOFT_DEFENSE + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.APC);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("BTR-80");

                // Set turn availability in months.
                profile.SetTurnAvailable(576);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.APC_BTR80] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_APC_BTR80_Profile), e);
                throw;
            }
        }

        #endregion // Soviet APCs

        #region Soviet IFVs

        private static void Create_IFV_BMP1_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMP‑1P IFV",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMP1,
                    CUConstants.PRESTIGE_TIER_0 + MASSIVE_BONUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK + XXLARGE_BONUS,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.IFV);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("BMP-1P");

                // Set turn availability in months.
                profile.SetTurnAvailable(336);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.IFV_BMP1] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_IFV_BMP1_Profile), e);
                throw;
            }
        }

        private static void Create_IFV_BMP2_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMP‑2 IFV",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMP2,
                    CUConstants.PRESTIGE_TIER_0 + MASSIVE_BONUS + XXLARGE_BONUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.IFV);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("BMP-2");

                // Set turn availability in months.
                profile.SetTurnAvailable(504);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.IFV_BMP2] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_IFV_BMP2_Profile), e);
                throw;
            }
        }

        private static void Create_IFV_BMP3_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMP‑3 IFV",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMP3,
                    CUConstants.PRESTIGE_TIER_1 + XXLARGE_MALUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK + SMALL_BONUS,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirLift,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.IFV);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("BMP-3");

                // Set turn availability in months.
                profile.SetTurnAvailable(600);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.IFV_BMP3] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_IFV_BMP3_Profile), e);
                throw;
            }
        }

        private static void Create_IFV_BMD1_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMD‑1 IFV",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMD1,
                    CUConstants.PRESTIGE_TIER_0 + LARGE_BONUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.IFV);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("BMD-1");

                // Set turn availability in months.
                profile.SetTurnAvailable(372);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.IFV_BMD1] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_IFV_BMD1_Profile), e);
                throw;
            }
        }

        private static void Create_IFV_BMD2_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMD‑2 IFV",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMD2,
                    CUConstants.PRESTIGE_TIER_0 + MASSIVE_BONUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK + SMALL_BONUS,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.IFV);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("BMD-2");

                // Set turn availability in months.
                profile.SetTurnAvailable(564);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.IFV_BMD2] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_IFV_BMD2_Profile), e);
                throw;
            }
        }

        private static void Create_IFV_BMD3_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMD‑3 IFV",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMD3,
                    CUConstants.PRESTIGE_TIER_1 + MASSIVE_MALUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK + MEDIUM_BONUS,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.IFV);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("BMD-3");

                // Set turn availability in months.
                profile.SetTurnAvailable(600);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.IFV_BMD3] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_IFV_BMD3_Profile), e);
                throw;
            }
        }

        #endregion // Soviet IFVs

        #region Soviet Recon

        private static void Create_RCN_BRDM2_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BRDM‑2 Recon Vehicle",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.RCN_BRDM2,
                    CUConstants.PRESTIGE_TIER_0,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_APC_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_APC_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_APC_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_APC_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.RECON);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("BRDM-2");

                // Set turn availability in months.
                profile.SetTurnAvailable(288);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.RCN_BRDM2] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_RCN_BRDM2_Profile), e);
                throw;
            }
        }

        private static void Create_RCN_BRDM2AT_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BRDM‑2 AT‑5 Recon Vehicle",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.RCN_BRDM2AT,
                    CUConstants.PRESTIGE_TIER_1,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_APC_HARD_ATTACK + XXLARGE_BONUS,
                    hardDefense: CUConstants.BASE_APC_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_APC_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_APC_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.RECON);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("BRDM-2 AT");

                // Set turn availability in months.
                profile.SetTurnAvailable(432);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.RCN_BRDM2AT] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_RCN_BRDM2AT_Profile), e);
                throw;
            }
        }

        #endregion // Soviet Recon

        #region Soviet Artillery & Rockets

        /// <summary>
        /// 2S1 "Gvozdika" 122 mm self‑propelled howitzer.
        /// </summary>
        private static void Create_SPA_2S1_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "2S1 Gvozdika SPA",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPA_2S1,
                    CUConstants.PRESTIGE_TIER_1,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_120MM,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.MECH_UNIT
                );

                //Set the upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPA);

                // Set short name for UI display
                profile.SetShortName("2S1 Gvozdika");

                // Set turn availability in months.
                profile.SetTurnAvailable(408);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPA_2S1] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPA_2S1_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// 2S3 "Akatsiya" 152 mm self‑propelled gun‑howitzer.
        /// </summary>
        private static void Create_SPA_2S3_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "2S3 Akatsiya SPA",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPA_2S3,
                    CUConstants.PRESTIGE_TIER_1 + MASSIVE_BONUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + MEDIUM_BONUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_155MM,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPA);

                // Set short name for UI display
                profile.SetShortName("2S3 Akatsiya");

                // Set turn availability in months.
                profile.SetTurnAvailable(420);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPA_2S3] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPA_2S3_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// 2S5 "Giatsint‑S" 152 mm long‑range gun.
        /// </summary>
        private static void Create_SPA_2S5_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "2S5 Giatsint‑S SPA",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPA_2S5,
                    CUConstants.PRESTIGE_TIER_2,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK + MEDIUM_BONUS,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + LARGE_BONUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_203MM,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPA);

                // Set short name for UI display
                profile.SetShortName("2S5 Giatsint-S");

                // Set turn availability in months.
                profile.SetTurnAvailable(516);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPA_2S5] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPA_2S5_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// 2S19 "Msta‑S" 152 mm modern SP howitzer.
        /// </summary>
        private static void Create_SPA_2S19_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "2S19 Msta‑S SPA",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPA_2S19,
                    CUConstants.PRESTIGE_TIER_3,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_155MM,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPA);

                // Set short name for UI display
                profile.SetShortName("2S19 Msta-S");

                // Set turn availability in months.
                profile.SetTurnAvailable(600);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPA_2S19] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPA_2S19_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// BM‑21 "Grad" 122 mm 40‑tube MLRS.
        /// </summary>
        private static void Create_ROC_BM21_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BM‑21 Grad MLRS",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ROC_BM21,
                    CUConstants.PRESTIGE_TIER_1,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_ROC_SR,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.MOT_UNIT
                );

                // Set double fire capability (two salvos per turn)
                profile.SetDoubleFireCapability(true);

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ROC);

                // Set short name for UI display
                profile.SetShortName("BM-21 Grad");

                // Set turn availability in months.
                profile.SetTurnAvailable(300);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ROC_BM21] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ROC_BM21_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// BM‑27 "Uragan" 220 mm 16‑tube MLRS.
        /// </summary>
        private static void Create_ROC_BM27_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BM‑27 Uragan MLRS",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ROC_BM27,
                    CUConstants.PRESTIGE_TIER_2,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + LARGE_BONUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_ROC_MR,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.MOT_UNIT
                );

                // Set double fire capability (two salvos per turn)
                profile.SetDoubleFireCapability(true);

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ROC);

                // Set short name for UI display
                profile.SetShortName("BM-27 Uragan");

                // Set turn availability in months.
                profile.SetTurnAvailable(444);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ROC_BM27] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ROC_BM27_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// BM‑30 "Smerch" 300 mm 12‑tube MLRS.
        /// </summary>
        private static void Create_ROC_BM30_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BM‑30 Smerch MLRS",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ROC_BM30,
                    CUConstants.PRESTIGE_TIER_3,
                     weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_ROC_LR,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.MOT_UNIT
                );

                // Set double fire capability (two salvos per turn)
                profile.SetDoubleFireCapability(true);

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ROC);

                // Set short name for UI display
                profile.SetShortName("BM-30 Smerch");

                // Set turn availability in months.
                profile.SetTurnAvailable(588);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ROC_BM30] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ROC_BM30_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// 9K72 "Elbrus" (NATO: Scud‑B) tactical ballistic missile system.
        /// </summary>
        private static void CreateSsmScudProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "9K72 Scud‑B Tactical Ballistic Missile Launcher",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SSM_SCUD,
                    CUConstants.PRESTIGE_TIER_4,
                     weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + XXXLARGE_BONUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_ROC_LR,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.MOT_UNIT
                );

                // Set double fire capability (two salvos per turn)
                profile.SetDoubleFireCapability(true);

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SSM);

                // Set short name for UI display
                profile.SetShortName("9K72 Scud-B");

                // Set turn availability in months.
                profile.SetTurnAvailable(288);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SSM_SCUD] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSsmScudProfile), e);
                throw;
            }
        }

        #endregion // Soviet Artillery & Rockets

        #region Soviet Air Defense

        /// <summary>
        /// ZSU-57-2 "Sparka" 57mm twin anti-aircraft gun.
        /// </summary>
        private static void Create_SPAAA_ZSU57_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "ZSU-57-2 Sparka SPAAA",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPAAA_ZSU57,
                    CUConstants.PRESTIGE_TIER_0,
                    WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_AAA_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_AAA_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_AAA_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_AAA_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_AAA_GROUND_AIR_ATTACK,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_AAA,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_AAA,
                    spottingRange: CUConstants.BASE_AAA_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.AirDrop,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPAAA);

                // Set short name for UI display
                profile.SetShortName("ZSU-57-2 Sparka");

                // Set turn availability - entered service in 1957
                profile.SetTurnAvailable(228);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPAAA_ZSU57] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPAAA_ZSU57_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// ZSU-23-4 "Shilka" 23mm quad anti-aircraft gun with radar.
        /// </summary>
        private static void Create_SPAAA_ZSU23_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "ZSU-23-4 Shilka SPAAA",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPAAA_ZSU23,
                    CUConstants.PRESTIGE_TIER_1,
                    WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_AAA_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_AAA_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_AAA_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_AAA_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_AAA_GROUND_AIR_ATTACK + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_AAA,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_AAA,
                    spottingRange: CUConstants.BASE_AAA_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPAAA);

                // Set short name for UI display
                profile.SetShortName("ZSU-23-4 Shilka");

                // Set turn availability - entered service in 1965
                profile.SetTurnAvailable(324);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPAAA_ZSU23] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPAAA_ZSU23_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// 2K22 "Tunguska" combined gun/missile air defense system.
        /// </summary>
        private static void Create_SPAAA_2K22_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "2K22 Tunguska SPAAA",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPAAA_2K22,
                    CUConstants.PRESTIGE_TIER_2 + MASSIVE_BONUS,
                    WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_AAA_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_AAA_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_AAA_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_AAA_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_AAA_GROUND_AIR_ATTACK + LARGE_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_AAA + SMALL_BONUS,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_AAA + SMALL_BONUS,
                    spottingRange: CUConstants.BASE_AAA_SPOTTING_RANGE + SMALL_BONUS,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPAAA);

                // Set short name for UI display
                profile.SetShortName("2K22 Tunguska");

                // Set turn availability - entered service in 1982
                profile.SetTurnAvailable(528);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPAAA_2K22] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPAAA_2K22_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// 9K31 "Strela-1" (SA-9) mobile short-range SAM system.
        /// </summary>
        private static void Create_SPSAM_9K31_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "9K31 Strela-1 SPSAM",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPSAM_9K31,
                    CUConstants.PRESTIGE_TIER_2,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_SAM_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_SAM_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SAM_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SAM_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_SAM_GROUND_AIR_ATTACK,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_SAM,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_SAM,
                    spottingRange: CUConstants.BASE_SAM_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPSAM);

                // Set amphibious capability (BRDM-2 chassis)
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("9K31 Strela");

                // Set turn availability - entered service in 1968
                profile.SetTurnAvailable(360);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPSAM_9K31] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPSAM_9K31_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// S-75 "Dvina" (SA-2) medium-range strategic SAM system.
        /// </summary>
        private static void Create_SAM_S75_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "S-75 Dvina SAM System",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SAM_S75,
                    CUConstants.PRESTIGE_TIER_1,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_SAM_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_SAM_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SAM_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SAM_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_SAM_GROUND_AIR_ATTACK,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_SAM,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_SAM,
                    spottingRange: CUConstants.BASE_SAM_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    movementPoints: CUConstants.STATIC_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SAM);

                // Set short name for UI display
                profile.SetShortName("S-75 Dvina");

                // Set turn availability - entered service in 1957
                profile.SetTurnAvailable(228);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SAM_S75] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SAM_S75_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// S-125 "Neva" (SA-3) low-altitude strategic SAM system.
        /// </summary>
        private static void Create_SAM_S125_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "S-125 Neva SAM System",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SAM_S125,
                    CUConstants.PRESTIGE_TIER_2,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_SAM_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_SAM_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SAM_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SAM_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_SAM_GROUND_AIR_ATTACK + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_SAM,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_SAM,
                    spottingRange: CUConstants.BASE_SAM_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    movementPoints: CUConstants.STATIC_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SAM);

                // Set short name for UI display
                profile.SetShortName("S-125 Neva");

                // Set turn availability - entered service in 1961
                profile.SetTurnAvailable(276);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SAM_S125] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SAM_S125_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// S-300 "Favorit" (SA-10/20) advanced long-range strategic SAM system.
        /// </summary>
        private static void Create_SAM_S300_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "S-300 SAM System",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SAM_S300,
                    CUConstants.PRESTIGE_TIER_4,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_SAM_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_SAM_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SAM_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SAM_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_SAM_GROUND_AIR_ATTACK + XXLARGE_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_SAM,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_SAM + XLARGE_BONUS,
                    spottingRange: CUConstants.BASE_SAM_SPOTTING_RANGE + XLARGE_BONUS,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    movementPoints: CUConstants.STATIC_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SAM);

                // Set short name for UI display
                profile.SetShortName("S-300");

                // Set turn availability - entered service in 1984
                profile.SetTurnAvailable(552);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SAM_S300] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SAM_S300_Profile), e);
                throw;
            }
        }

        #endregion // Soviet Air Defense

        #region Soviet Helicopters

        /// <summary>
        /// The MI-8 "Hip" transport helicopter.
        /// </summary>
        private static void Create_HEL_MI8T_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Mi-8 Hip Transport Helicopter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TRANHEL_MI8T,
                    CUConstants.PRESTIGE_TIER_1,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HEL_HARD_ATTACK + MEDIUM_MALUS,
                    hardDefense: CUConstants.BASE_HEL_HARD_DEFENSE + MEDIUM_MALUS,
                    softAttack: CUConstants.BASE_HEL_SOFT_ATTACK + MEDIUM_MALUS,
                    softDefense: CUConstants.BASE_HEL_SOFT_DEFENSE + MEDIUM_MALUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_HELO,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.HELO_UNIT

                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.TRNHELO);

                // Set short name for UI display
                profile.SetShortName("Mi-8T");

                // Set turn availability
                profile.SetTurnAvailable(250);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TRANHEL_MI8T] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_HEL_MI8T_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Mi-8AT "Hip-C" armed transport helicopter with anti-tank missiles.
        /// </summary>
        private static void Create_HEL_MI8AT_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Mi-8AT Hip-C Attack Helicopter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.HEL_MI8AT,
                    CUConstants.PRESTIGE_TIER_1,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HEL_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_HEL_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_HEL_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_HEL_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_HELO,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.HELO_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ATTHELO);

                // Set short name for UI display
                profile.SetShortName("Mi-8AT");

                // Set turn availability - entered service in 1975
                profile.SetTurnAvailable(444);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.HEL_MI8AT] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_HEL_MI8AT_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Mi-24D "Hind-D" attack helicopter with rockets and anti-tank missiles.
        /// </summary>
        private static void Create_HEL_MI24D_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Mi-24D Hind-D Attack Helicopter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.HEL_MI24D,
                    CUConstants.PRESTIGE_TIER_2,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HEL_HARD_ATTACK + SMALL_BONUS,
                    hardDefense: CUConstants.BASE_HEL_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_HEL_SOFT_ATTACK + SMALL_BONUS,
                    softDefense: CUConstants.BASE_HEL_SOFT_DEFENSE + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_HELO,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.HELO_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ATTHELO);

                // Set short name for UI display
                profile.SetShortName("Mi-24D");

                // Set turn availability - entered service in 1973
                profile.SetTurnAvailable(420);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.HEL_MI24D] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_HEL_MI24D_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Mi-24V "Hind-E" improved attack helicopter with better sensors and weapons.
        /// </summary>
        private static void Create_HEL_MI24V_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Mi-24V Hind-E Attack Helicopter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.HEL_MI24V,
                    CUConstants.PRESTIGE_TIER_3,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HEL_HARD_ATTACK + XLARGE_BONUS,
                    hardDefense: CUConstants.BASE_HEL_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_HEL_SOFT_ATTACK + SMALL_BONUS,
                    softDefense: CUConstants.BASE_HEL_SOFT_DEFENSE + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_HELO,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.HELO_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ATTHELO);

                // Set short name for UI display
                profile.SetShortName("Mi-24V");

                // Set turn availability - entered service in 1976
                profile.SetTurnAvailable(456);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.HEL_MI24V] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_HEL_MI24V_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Mi-28 "Havoc" dedicated attack helicopter with advanced avionics.
        /// </summary>
        private static void Create_HEL_MI28_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Mi-28 Havoc Attack Helicopter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.HEL_MI28,
                    CUConstants.PRESTIGE_TIER_4,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HEL_HARD_ATTACK + XXXLARGE_BONUS,
                    hardDefense: CUConstants.BASE_HEL_HARD_DEFENSE + SMALL_BONUS,
                    softAttack: CUConstants.BASE_HEL_SOFT_ATTACK + MEDIUM_BONUS,
                    softDefense: CUConstants.BASE_HEL_SOFT_DEFENSE + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_HELO,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.HELO_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ATTHELO);

                // Set short name for UI display
                profile.SetShortName("Mi-28");

                // Set turn availability - entered service in 1987
                profile.SetTurnAvailable(588);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.HEL_MI28] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_HEL_MI28_Profile), e);
                throw;
            }
        }

        #endregion

        #region Soviet Aircraft

        /// <summary>
        /// An-12 medium-range transport.
        /// </summary>
        private static void Create_TRA_AN12_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "An-12 Antonov Transport Plane",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TRANAIR_AN12,
                    CUConstants.PRESTIGE_TIER_2,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.AC_BOMBER_DOGFIGHT,
                    maneuverability: CUConstants.AC_BOMBER_MANEUVER,
                    topSpeed: CUConstants.AC_BOMBER_TOPSPEED + LARGE_MALUS,
                    survivability: CUConstants.AC_BOMBER_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_NA,
                    ordinanceLoad: CUConstants.LARGE_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.TRNAIR);

                // Set short name for UI display
                profile.SetShortName("An-12 Antonov");

                // Set turn availability
                profile.SetTurnAvailable(250);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TRANAIR_AN12] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_TRA_AN12_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// A-50 "Mainstay" airborne early warning and control aircraft.
        /// </summary>
        private static void Create_AWACS_A50_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "A-50 Mainstay AWACS",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.AWACS_A50,
                    CUConstants.PRESTIGE_TIER_5,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.AC_BOMBER_DOGFIGHT,
                    maneuverability: CUConstants.AC_BOMBER_MANEUVER,
                    topSpeed: CUConstants.AC_BOMBER_TOPSPEED,
                    survivability: CUConstants.AC_BOMBER_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_NA,
                    ordinanceLoad: CUConstants.LARGE_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.AWACS);

                // Set short name for UI display
                profile.SetShortName("A-50");

                // Set turn availability - entered service in 1984
                profile.SetTurnAvailable(552);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.AWACS_A50] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_AWACS_A50_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-21 "Fishbed" lightweight interceptor fighter.
        /// </summary>
        private static void Create_ASF_MIG21_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-21 Fishbed Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.FGT_MIG21,
                    CUConstants.PRESTIGE_TIER_1,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.EARLY_FGT_DOGFIGHT,
                    maneuverability: CUConstants.EARLY_FGT_MANEUVER,
                    topSpeed: CUConstants.EARLY_FGT_TOPSPEED,
                    survivability: CUConstants.EARLY_FGT_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_NA,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.Day,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("MiG-21");

                // Set turn availability - entered service in 1959
                profile.SetTurnAvailable(252);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_MIG21] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ASF_MIG21_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-23 "Flogger" variable-geometry wing fighter.
        /// </summary>
        private static void Create_ASF_MIG23_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-23 Flogger Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.FGT_MIG23,
                    CUConstants.PRESTIGE_TIER_1 + XXLARGE_BONUS,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.EARLY_FGT_DOGFIGHT + LARGE_BONUS,
                    maneuverability: CUConstants.EARLY_FGT_MANEUVER,
                    topSpeed: CUConstants.EARLY_FGT_TOPSPEED + SMALL_BONUS,
                    survivability: CUConstants.EARLY_FGT_SURVIVE + MEDIUM_BONUS,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_0,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ENHANCED,
                    allWeatherCapability: AllWeatherRating.Night,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("MiG-23");

                // Set turn availability - entered service in 1970
                profile.SetTurnAvailable(384);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_MIG23] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ASF_MIG23_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-25 "Foxbat" high-speed interceptor.
        /// </summary>
        private static void Create_ASF_MIG25_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-25 Foxbat Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.FGT_MIG25,
                    CUConstants.PRESTIGE_TIER_2,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.EARLY_FGT_DOGFIGHT,
                    maneuverability: CUConstants.EARLY_FGT_MANEUVER,
                    topSpeed: CUConstants.AC_HIGHSPEED_RUSSIAN,
                    survivability: CUConstants.EARLY_FGT_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_0,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ENHANCED,
                    allWeatherCapability: AllWeatherRating.Night,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("MiG-25");

                // Set turn availability - entered service in 1970
                profile.SetTurnAvailable(384);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_MIG25] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ASF_MIG25_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-29 "Fulcrum" modern air superiority fighter.
        /// </summary>
        private static void Create_ASF_MIG29_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-29 Fulcrum Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.FGT_MIG29,
                    CUConstants.PRESTIGE_TIER_2,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.MID_FGT_DOGFIGHT + LARGE_BONUS,
                    maneuverability: CUConstants.MID_FGT_MANEUVER + XXLARGE_BONUS,
                    topSpeed: CUConstants.MID_FGT_TOPSPEED + SMALL_BONUS,
                    survivability: CUConstants.MID_FGT_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_1,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ENHANCED,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("MiG-29");

                // Set turn availability - entered service in 1983
                profile.SetTurnAvailable(540);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_MIG29] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ASF_MIG29_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-31 "Foxhound" long-range interceptor.
        /// </summary>
        private static void Create_ASF_MIG31_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-31 Foxhound Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.FGT_MIG31,
                    CUConstants.PRESTIGE_TIER_2 + XXLARGE_BONUS,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.MID_FGT_DOGFIGHT + LARGE_BONUS,
                    maneuverability: CUConstants.MID_FGT_MANEUVER,
                    topSpeed: CUConstants.AC_HIGHSPEED_RUSSIAN,
                    survivability: CUConstants.MID_FGT_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_1 + LARGE_MALUS,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ADVANCED,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("MiG-31");

                // Set turn availability
                profile.SetTurnAvailable(550);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_MIG31] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ASF_MIG31_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Su-27 "Flanker" advanced air superiority fighter.
        /// </summary>
        private static void Create_ASF_SU27_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Su-27 Flanker Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.FGT_SU27,
                    CUConstants.PRESTIGE_TIER_3,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.MID_FGT_DOGFIGHT + XXLARGE_BONUS,
                    maneuverability: CUConstants.MID_FGT_MANEUVER + MEDIUM_BONUS,
                    topSpeed: CUConstants.MID_FGT_TOPSPEED + MEDIUM_BONUS,
                    survivability: CUConstants.MID_FGT_SURVIVE + MEDIUM_BONUS,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_1,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ADVANCED,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("Su-27");

                // Set turn availability - entered service in 1985
                profile.SetTurnAvailable(564);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_SU27] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ASF_SU27_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Su-47 "Berkut" experimental forward-swept wing fighter.
        /// </summary>
        private static void Create_ASF_SU47_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Su-47 Berkut Experimental Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.FGT_SU47,
                    CUConstants.PRESTIGE_TIER_3 + XXXLARGE_BONUS + XXXLARGE_BONUS + XXXLARGE_BONUS,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.LATE_FGT_DOGFIGHT + XXXLARGE_BONUS,
                    maneuverability: CUConstants.LATE_FGT_MANEUVER + XXLARGE_BONUS,
                    topSpeed: CUConstants.LATE_FGT_TOPSPEED,
                    survivability: CUConstants.LATE_FGT_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_2,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ENHANCED,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("Su-47");

                // Set turn availability
                profile.SetTurnAvailable(620);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_SU47] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ASF_SU47_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-27 "Flogger-D" ground attack variant of MiG-23.
        /// </summary>
        private static void Create_MRF_MIG27_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-27 Flogger-D Multi-Role Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.FGT_MIG27,
                    CUConstants.PRESTIGE_TIER_1 + XXLARGE_BONUS,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.AC_ATTACK_DOGFIGHT + LARGE_BONUS,
                    maneuverability: CUConstants.AC_ATTACK_MANEUVER + MEDIUM_BONUS,
                    topSpeed: CUConstants.AC_ATTACK_TOPSPEED + MEDIUM_BONUS,
                    survivability: CUConstants.AC_ATTACK_SURVIVE + SMALL_BONUS,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_0 + XLARGE_BONUS,
                    ordinanceLoad: CUConstants.MEDIUM_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.Night,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ATT);

                // Set short name for UI display
                profile.SetShortName("MiG-27");

                // Set turn availability - entered service in 1975
                profile.SetTurnAvailable(444);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_MIG27] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_MRF_MIG27_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Su-25 "Frogfoot" close air support aircraft.
        /// </summary>
        private static void Create_ATT_SU25_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Su-25 Frogfoot Attack Aircraft",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ATT_SU25,
                    CUConstants.PRESTIGE_TIER_2 + XXLARGE_MALUS,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.AC_ATTACK_DOGFIGHT + MEDIUM_MALUS,
                    maneuverability: CUConstants.AC_ATTACK_MANEUVER,
                    topSpeed: CUConstants.AC_ATTACK_TOPSPEED,
                    survivability: CUConstants.AC_ATTACK_SURVIVE + MEDIUM_BONUS,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_1 + LARGE_BONUS,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD + LARGE_BONUS,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.Night,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ATT);

                // Set short name for UI display
                profile.SetShortName("Su-25");

                // Set turn availability - entered service in 1981
                profile.SetTurnAvailable(516);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ATT_SU25] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ATT_SU25_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Su-25B "Frogfoot-B" improved close air support aircraft.
        /// </summary>
        private static void Create_ATT_SU25B_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Su-25B Frogfoot-B Attack Aircraft",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ATT_SU25B,
                    CUConstants.PRESTIGE_TIER_2 + XXLARGE_BONUS,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.AC_ATTACK_DOGFIGHT,
                    maneuverability: CUConstants.AC_ATTACK_MANEUVER,
                    topSpeed: CUConstants.AC_ATTACK_TOPSPEED,
                    survivability: CUConstants.AC_ATTACK_SURVIVE + XXLARGE_BONUS,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_2 + LARGE_BONUS,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD + XLARGE_BONUS,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ATT);

                // Set short name for UI display
                profile.SetShortName("Su-25B");

                // Set turn availability - entered service in 1986
                profile.SetTurnAvailable(576);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ATT_SU25B] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ATT_SU25B_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Su-24 "Fencer" variable-geometry wing bomber.
        /// </summary>
        private static void Create_BMB_SU24_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Su-24 Fencer Bomber",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.BMB_SU24,
                    CUConstants.PRESTIGE_TIER_3 + XXLARGE_MALUS,
                    dogfighting: CUConstants.AC_BOMBER_DOGFIGHT + XXLARGE_BONUS,
                    maneuverability: CUConstants.AC_BOMBER_MANEUVER + LARGE_BONUS,
                    topSpeed: CUConstants.AC_BOMBER_TOPSPEED + XLARGE_BONUS,
                    survivability: CUConstants.AC_BOMBER_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_1 + LARGE_BONUS,
                    ordinanceLoad: CUConstants.LARGE_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.BMB);

                // Set short name for UI display
                profile.SetShortName("Su-24");

                // Set turn availability - entered service in 1974
                profile.SetTurnAvailable(432);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.BMB_SU24] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_BMB_SU24_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Tu-16 "Badger" medium-range bomber.
        /// </summary>
        private static void Create_BMB_TU16_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Tu-16 Badger Bomber",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.BMB_TU16,
                    CUConstants.PRESTIGE_TIER_3 + XXLARGE_BONUS,
                    dogfighting: CUConstants.AC_BOMBER_DOGFIGHT,
                    maneuverability: CUConstants.AC_BOMBER_MANEUVER,
                    topSpeed: CUConstants.AC_BOMBER_TOPSPEED,
                    survivability: CUConstants.AC_BOMBER_SURVIVE + MEDIUM_BONUS,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_1,
                    ordinanceLoad: CUConstants.XLARGE_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.Day,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.BMB);

                // Set short name for UI display
                profile.SetShortName("Tu-16");

                // Set turn availability - entered service in 1954
                profile.SetTurnAvailable(192);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.BMB_TU16] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_BMB_TU16_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Tu-22 "Blinder" supersonic bomber.
        /// </summary>
        private static void CreateBMB_TU22_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Tu-22 Blinder Bomber",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.BMB_TU22,
                    CUConstants.PRESTIGE_TIER_4,
                    dogfighting: CUConstants.AC_BOMBER_DOGFIGHT,
                    maneuverability: CUConstants.AC_BOMBER_MANEUVER,
                    topSpeed: CUConstants.AC_BOMBER_TOPSPEED + XLARGE_BONUS,
                    survivability: CUConstants.AC_BOMBER_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_1 + XLARGE_BONUS,
                    ordinanceLoad: CUConstants.LARGE_AC_LOAD + SMALL_MALUS,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.Night,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.BMB);

                // Set short name for UI display
                profile.SetShortName("Tu-22");

                // Set turn availability - entered service in 1962
                profile.SetTurnAvailable(288);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.BMB_TU22] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateBMB_TU22_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Tu-22M3 "Backfire-C" variable-geometry strategic bomber.
        /// </summary>
        private static void Create_BMB_TU22M3_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Tu-22M3 Backfire-C Strategic Bomber",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.BMB_TU22M3,
                    CUConstants.PRESTIGE_TIER_5,
                    dogfighting: CUConstants.AC_BOMBER_DOGFIGHT,
                    maneuverability: CUConstants.AC_BOMBER_MANEUVER,
                    topSpeed: CUConstants.AC_HIGHSPEED_RUSSIAN + SMALL_MALUS,
                    survivability: CUConstants.AC_BOMBER_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_2 + LARGE_BONUS,
                    ordinanceLoad: CUConstants.LARGE_AC_LOAD + LARGE_BONUS,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.BMB);

                // Set short name for UI display
                profile.SetShortName("Tu-22M3");

                // Set turn availability - entered service in 1983
                profile.SetTurnAvailable(540);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.BMB_TU22M3] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_BMB_TU22M3_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-25R "Foxbat-B" reconnaissance variant.
        /// </summary>
        private static void Create_RCNA_Mig25R_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-25R Foxbat-B Reconnaissance Aircraft",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.RCNA_MIG25R,
                    CUConstants.PRESTIGE_TIER_2,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.EARLY_FGT_DOGFIGHT,
                    maneuverability: CUConstants.EARLY_FGT_MANEUVER,
                    topSpeed: CUConstants.AC_HIGHSPEED_RUSSIAN + MEDIUM_BONUS,
                    survivability: CUConstants.EARLY_FGT_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_0,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.RCN);

                // Set short name for UI display
                profile.SetShortName("MiG-25R");

                // Set turn availability 
                profile.SetTurnAvailable(440);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.RCNA_MIG25R] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_RCNA_Mig25R_Profile), e);
                throw;
            }
        }

        #endregion // Soviet Aircraft

        #region Soviet Transports

        /// <summary>
        /// Soviet transport flotilla
        /// </summary>
        private static void Create__TRA_TransportFlotilla_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Transport Flotilla",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TRANNAV_NAVAL,
                    CUConstants.PRESTIGE_TIER_5,
                    groundAirDefense: 12,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: 4f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.NavalAssault,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    movementPoints: CUConstants.NAVAL_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.TRNNAVAL);

                // Set short name for UI display
                profile.SetShortName("Transports");

                // Set turn availability
                profile.SetTurnAvailable(250);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TRANNAV_NAVAL] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create__TRA_TransportFlotilla_Profile), e);
                throw;
            }
        }

        #endregion

        //-----------------------------------------------------------------------------------------

        #region US Tanks

        /// <summary>
        /// M1 Abrams Main Battle Tank
        /// </summary>
        private static void Create_TANK_M1_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "M1 Abrams MBT",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.TANK_M1,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN2_TANK_HARD_ATTACK,
                    hardDefense: CUConstants.GEN2_TANK_HARD_DEFENSE + XXLARGE_BONUS,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Large
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                // Set short name for UI display
                profile.SetShortName("M1 Abrams");

                // Set turn availability - entered service in 1980
                profile.SetTurnAvailable(504);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TANK_M1] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_TANK_M1_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// M-60A3 Patton Main Battle Tank
        /// </summary>
        private static void Create_TANK_M60A3_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "M60-A3 Patton MBT",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.TANK_M60A3,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN1_TANK_HARD_ATTACK + SMALL_BONUS,
                    hardDefense: CUConstants.GEN1_TANK_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                // Set short name for UI display
                profile.SetShortName("M-60A3 Abrams");

                // Set turn availability - entered service in 1980
                profile.SetTurnAvailable(275);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TANK_M60A3] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_TANK_M60A3_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// M551 Sheridan
        /// </summary>
        private static void Create_TANK_M551_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "M551 Sheridan Light Tank",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.TANK_M551,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN1_TANK_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.GEN1_TANK_HARD_DEFENSE + MEDIUM_MALUS,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK + LARGE_MALUS,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.AirDrop,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                // Set short name for UI display
                profile.SetShortName("M551 Sheridan");

                // Set turn availability - entered service in 1980
                profile.SetTurnAvailable(250);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.TANK_M551] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_TANK_M551_Profile), e);
                throw;
            }
        }

        #endregion // US Tanks
        
        #region US IFVs and APCs

        /// <summary>
        /// M2 Bradley INF Fighting Vehicle
        /// </summary>
        private static void Create_IFV_M2_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "M2 Bradley IFV",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.IFV_M2,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK + SMALL_BONUS,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.IFV);

                // Set short name for UI display
                profile.SetShortName("M2 Bradley");

                // Set turn availability - entered service in 1981
                profile.SetTurnAvailable(516);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.IFV_M2] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_IFV_M2_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// M3 Bradley Cavalry Fighting Vehicle
        /// </summary>
        private static void Create_IFV_M3_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "M3 Bradley Scout IFV",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.IFV_M3,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK + MEDIUM_BONUS,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.Gen3,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.RECON);

                // Set short name for UI display
                profile.SetShortName("M3 Bradley");

                // Set turn availability - entered service in 1981
                profile.SetTurnAvailable(516);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.IFV_M3] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_IFV_M3_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// M113 Armored Personnel Carrier
        /// </summary>
        private static void Create_APC_M113_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "M113 APC",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.APC_M113,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_APC_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_APC_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_APC_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_APC_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.APC);

                // Set short name for UI display
                profile.SetShortName("M113");

                // Set turn availability - entered service in 1960
                profile.SetTurnAvailable(264);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.APC_M113] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_APC_M113_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// LVTP-7 Amphibious Assault Vehicle
        /// </summary>
        private static void Create_APC_LVTP7_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "LVTP-7 AAV",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.APC_LVTP7,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_APC_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_APC_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_APC_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_APC_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.NavalAssault,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.APC);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("LVTP-7");

                // Set turn availability - entered service in 1972
                profile.SetTurnAvailable(408);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.APC_LVTP7] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_APC_LVTP7_Profile), e);
                throw;
            }
        }

        #endregion // US IFVs and APCs

        #region US Artillery

        /// <summary>
        /// M109 Paladin Self-Propelled Howitzer
        /// </summary>
        private static void Create_SPA_M109_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "M109 Paladin Self-Propelled Artillery",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.SPA_M109,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_155MM,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Large
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.SPA);

                // Set short name for UI display
                profile.SetShortName("M109");

                // Set turn availability - entered service in 1963
                profile.SetTurnAvailable(300);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPA_M109] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPA_M109_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// M270 MLRS Multiple Launch Rocket System
        /// </summary>
        private static void Create_ROC_MLRS_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "M270 MLRS Multiple Launch Rocket System",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.ROC_MLRS,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + LARGE_BONUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_ROC_MR,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Large
                );

                // Set double fire capability
                profile.SetDoubleFireCapability(true);

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.ROC);

                // Set short name for UI display
                profile.SetShortName("MLRS");

                // Set turn availability - entered service in 1983
                profile.SetTurnAvailable(540);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ROC_MLRS] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ROC_MLRS_Profile), e);
                throw;
            }
        }

        #endregion // US Artillery

        #region US Air Defense

        /// <summary>
        /// M163 Vulcan Air Defense System
        /// </summary>
        private static void Create_SPAAA_M163_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "M163 Vulcan Self-Propelled Anti-Aircraft Artillery",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.SPAAA_M163,
                    CUConstants.PRESTIGE_TIER_FREE,
                    WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_AAA_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_AAA_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_AAA_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_AAA_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_AAA_GROUND_AIR_ATTACK + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_AAA,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_AAA,
                    spottingRange: CUConstants.BASE_AAA_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.SPAAA);

                // Set short name for UI display
                profile.SetShortName("M163");

                // Set turn availability - entered service in 1969
                profile.SetTurnAvailable(372);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPAAA_M163] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPAAA_M163_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// M48 Chaparral Self-Propelled SAM System
        /// </summary>
        private static void Create_SPSAM_Chap_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "M48 Chaparral Self-Propelled SAM System",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.SPSAM_CHAP,
                    CUConstants.PRESTIGE_TIER_FREE,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_SAM_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_SAM_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SAM_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SAM_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_SAM_GROUND_AIR_ATTACK,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_SAM,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_SAM,
                    spottingRange: CUConstants.BASE_SAM_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.SPSAM);

                // Set short name for UI display
                profile.SetShortName("Chaparral");

                // Set turn availability - entered service in 1969
                profile.SetTurnAvailable(372);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPSAM_CHAP] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPSAM_Chap_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MIM-23 Hawk Medium-Range SAM System
        /// </summary>
        private static void Create_SAM_Hawk_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MIM-23 Hawk Strategic SAM System",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.SAM_HAWK,
                    CUConstants.PRESTIGE_TIER_FREE,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_SAM_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_SAM_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SAM_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SAM_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_SAM_GROUND_AIR_ATTACK + LARGE_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_SAM,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_SAM + LARGE_BONUS,
                    spottingRange: CUConstants.BASE_SAM_SPOTTING_RANGE + LARGE_BONUS,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.SAM);

                // Set short name for UI display
                profile.SetShortName("Hawk");

                // Set turn availability - entered service in 1960
                profile.SetTurnAvailable(264);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SAM_HAWK] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SAM_Hawk_Profile), e);
                throw;
            }
        }

        #endregion // US Air Defense

        #region US Helicopters

        /// <summary>
        /// AH-64 Apache ATT Helicopter
        /// </summary>
        private static void Create_HEL_AH64_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "AH-64 Apache Attack Helicopter",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.HEL_AH64,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HEL_HARD_ATTACK + XXXLARGE_BONUS,
                    hardDefense: CUConstants.BASE_HEL_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_HEL_SOFT_ATTACK + MEDIUM_BONUS,
                    softDefense: CUConstants.BASE_HEL_SOFT_DEFENSE + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_HELO,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.Gen3,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.ATTHELO);

                // Set short name for UI display
                profile.SetShortName("AH-64");

                // Set turn availability - entered service in 1986
                profile.SetTurnAvailable(576);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.HEL_AH64] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_HEL_AH64_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// OH-58 Recon Helicopter
        /// </summary>
        private static void Create_HEL_OH58_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "OH-58 Kiowa Recon Helicopter",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.HEL_OH58,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HEL_HARD_ATTACK + SMALL_MALUS,
                    hardDefense: CUConstants.BASE_HEL_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_HEL_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_HEL_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_HELO,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.Gen3,
                    silhouette: UnitSilhouette.Small
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.ATTHELO);

                // Set short name for UI display
                profile.SetShortName("OH-58");

                // Set turn availability - entered service in 1986
                profile.SetTurnAvailable(500);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.HEL_OH58] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_HEL_OH58_Profile), e);
                throw;
            }
        }

        #endregion // US Helicopters

        #region US Aircraft

        /// <summary>
        /// E-3 Sentry airborne early warning and control aircraft.
        /// </summary>
        private static void Create_AWACS_E3_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "E-3 Sentry Airborne Early Warning",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.AWACS_E3,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.AC_BOMBER_DOGFIGHT,
                    maneuverability: CUConstants.AC_BOMBER_MANEUVER,
                    topSpeed: CUConstants.AC_BOMBER_TOPSPEED,
                    survivability: CUConstants.AC_BOMBER_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_NA,
                    ordinanceLoad: CUConstants.LARGE_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    movementPoints: CUConstants.FIXEDWING_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.AWACS);

                // Set short name for UI display
                profile.SetShortName("E-3 Sentry");

                // Set turn availability
                profile.SetTurnAvailable(500);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.AWACS_E3] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_AWACS_E3_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// F-15 Eagle Air Superiority FGT
        /// </summary>
        private static void Create_ASF_F15_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "F-15 Eagle Air Superiority Fighter",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.FGT_F15,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.MID_FGT_DOGFIGHT + XXXLARGE_BONUS,
                    maneuverability: CUConstants.MID_FGT_MANEUVER + LARGE_BONUS,
                    topSpeed: CUConstants.MID_FGT_TOPSPEED + MEDIUM_BONUS,
                    survivability: CUConstants.MID_FGT_SURVIVE + XLARGE_BONUS,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_2,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD + LARGE_BONUS,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_SUPERIOR,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("F-15 Eagle");

                // Set turn availability - entered service in 1976
                profile.SetTurnAvailable(456);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_F15] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ASF_F15_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// F-4 Phantom Multi-Role FGT
        /// </summary>
        private static void Create_ASF_F4_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "F-4 Phantom Air Superiority Fighter",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.FGT_F4,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.EARLY_FGT_DOGFIGHT + MEDIUM_BONUS,
                    maneuverability: CUConstants.EARLY_FGT_MANEUVER,
                    topSpeed: CUConstants.EARLY_FGT_TOPSPEED + MEDIUM_BONUS,
                    survivability: CUConstants.EARLY_FGT_SURVIVE + MEDIUM_BONUS,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_0,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ENHANCED,
                    allWeatherCapability: AllWeatherRating.Night,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("F-4 Phantom");

                // Set turn availability - entered service in 1960
                profile.SetTurnAvailable(264);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_F4] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ASF_F4_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// F-16 Fighting Falcon Multi-Role FGT
        /// </summary>
        private static void Create_MRF_F16_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "F-16 Fighting Falcon Multi-Role Fighter",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.FGT_F16,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.MID_FGT_DOGFIGHT + XLARGE_BONUS,
                    maneuverability: CUConstants.MID_FGT_MANEUVER + XLARGE_BONUS,
                    topSpeed: CUConstants.MID_FGT_TOPSPEED + SMALL_BONUS,
                    survivability: CUConstants.MID_FGT_SURVIVE + SMALL_BONUS,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_2,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD + MEDIUM_BONUS,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ADVANCED,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("F-16 Falcon");

                // Set turn availability - entered service in 1978
                profile.SetTurnAvailable(480);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_F16] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_MRF_F16_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// A-10 Thunderbolt II ATT Aircraft
        /// </summary>
        private static void Create_ATT_A10_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "A-10 Thunderbolt II Attack Aircraft",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.ATT_A10,
                     CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.AC_ATTACK_DOGFIGHT,
                    maneuverability: CUConstants.AC_ATTACK_MANEUVER,
                    topSpeed: CUConstants.AC_ATTACK_TOPSPEED,
                    survivability: CUConstants.AC_ATTACK_SURVIVE + XXLARGE_BONUS,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_2 + XXLARGE_BONUS,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD + XXLARGE_BONUS,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.ATT);

                // Set short name for UI display
                profile.SetShortName("A-10 Thunderbolt II");

                // Set turn availability - entered service in 1977
                profile.SetTurnAvailable(468);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ATT_A10] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ATT_A10_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// F-111 Aardvark Strike FGT
        /// </summary>
        private static void Create_BMB_F111_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "F-111 Aardvark Strike Fighter",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.BMB_F111,
                    CUConstants.PRESTIGE_TIER_FREE,
                    dogfighting: CUConstants.AC_BOMBER_DOGFIGHT + XXLARGE_BONUS,
                    maneuverability: CUConstants.AC_BOMBER_MANEUVER + LARGE_BONUS,
                    topSpeed: CUConstants.AC_BOMBER_TOPSPEED + XLARGE_BONUS,
                    survivability: CUConstants.AC_BOMBER_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_2 + LARGE_BONUS,
                    ordinanceLoad: CUConstants.LARGE_AC_LOAD + MEDIUM_BONUS,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ENHANCED,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.BMB);

                // Set short name for UI display
                profile.SetShortName("F-111");

                // Set turn availability - entered service in 1967
                profile.SetTurnAvailable(348);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.BMB_F111] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_BMB_F111_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// F-117 Nighthawk Stealth ATT Aircraft
        /// </summary>
        private static void Create_BMB_F117_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "F-117 Nighthawk Stealth Attack Aircraft",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.BMB_F117,
                    CUConstants.PRESTIGE_TIER_FREE,
                    dogfighting: CUConstants.AC_BOMBER_DOGFIGHT,
                    maneuverability: CUConstants.AC_BOMBER_MANEUVER,
                    topSpeed: CUConstants.AC_BOMBER_TOPSPEED,
                    survivability: CUConstants.AC_BOMBER_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_3 + LARGE_BONUS,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 15,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Tiny
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.BMB);

                // Set short name for UI display
                profile.SetShortName("F-117 Nighthawk");

                // Set turn availability - entered service in 1983
                profile.SetTurnAvailable(540);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.BMB_F117] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_BMB_F117_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// SR-71 Blackbird Strategic Reconnaissance Aircraft
        /// </summary>
        private static void Create_RCNA_SR71_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "SR-71 Blackbird Strategic Reconnaissance Aircraft",
                    nationality: Nationality.USA,
                    weaponSystemID: WeaponSystems.RCNA_SR71,
                    CUConstants.PRESTIGE_TIER_2,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.EARLY_FGT_DOGFIGHT,
                    maneuverability: CUConstants.EARLY_FGT_MANEUVER,
                    topSpeed: CUConstants.AC_HIGHSPEED_WESTERN,
                    survivability: CUConstants.EARLY_FGT_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_0,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.RCN);

                // Set short name for UI display
                profile.SetShortName("SR-71 Blackbird");

                // Set turn availability - entered service in 1966
                profile.SetTurnAvailable(336);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.RCNA_SR71] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_RCNA_SR71_Profile), e);
                throw;
            }
        }

        #endregion // US Aircraft

        //-----------------------------------------------------------------------------------------

        #region West Germany (FRG)

        /// <summary>
        /// Leopard 1 Main Battle Tank (MBT)
        /// </summary>
        private static void Create_TANK_LEOPARD1_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Leopard 1 MBT",
                    nationality: Nationality.FRG,
                    weaponSystemID: WeaponSystems.TANK_LEOPARD1,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN1_TANK_HARD_ATTACK + MEDIUM_BONUS,
                    hardDefense: CUConstants.GEN1_TANK_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT);

                profile.AddUpgradeType(UpgradeType.AFV);
                profile.SetShortName("Leo 1");
                profile.SetTurnAvailable(336); // 1965
                _weaponSystemProfiles[WeaponSystems.TANK_LEOPARD1] = profile;
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(Create_TANK_LEOPARD1_Profile), e); throw; }
        }

        /// <summary>
        /// Leopard 2 Main Battle Tank (MBT)
        /// </summary>
        private static void Create_TANK_LEOPARD2_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Leopard 2 MBT",
                    nationality: Nationality.FRG,
                    weaponSystemID: WeaponSystems.TANK_LEOPARD2,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN2_TANK_HARD_ATTACK + XLARGE_BONUS,
                    hardDefense: CUConstants.GEN2_TANK_HARD_DEFENSE + XXLARGE_BONUS,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT);

                profile.AddUpgradeType(UpgradeType.AFV);
                profile.SetShortName("Leo 2");
                profile.SetTurnAvailable(540); // 1983
                _weaponSystemProfiles[WeaponSystems.TANK_LEOPARD2] = profile;
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(Create_TANK_LEOPARD2_Profile), e); throw; }
        }

        /// <summary>
        /// Marder Infantry Fighting Vehicle (IFV)
        /// </summary>
        private static void Create_IFV_MARDER_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Marder IFV",
                    nationality: Nationality.FRG,
                    weaponSystemID: WeaponSystems.IFV_MARDER,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    movementPoints: CUConstants.MECH_UNIT);

                profile.AddUpgradeType(UpgradeType.IFV);
                profile.SetShortName("Marder");
                profile.SetTurnAvailable(396); // 1973
                _weaponSystemProfiles[WeaponSystems.IFV_MARDER] = profile;
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(Create_IFV_MARDER_Profile), e); throw; }
        }

        /// <summary>
        /// Luchs Reconnaissance Vehicle
        /// </summary>
        private static void Create_RCN_LUCHS_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Luchs Reconnaissance Vehicle",
                    nationality: Nationality.FRG,
                    weaponSystemID: WeaponSystems.RCN_LUCHS,
                    CUConstants.PRESTIGE_TIER_0 + MASSIVE_BONUS,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.Night,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.HQLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirLift,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.RECON);

                // Set amphibious capability (Luchs is fully amphibious)
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("Luchs");

                // Set turn availability - Luchs entered service 1975, fully deployed by 1982
                profile.SetTurnAvailable(276);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.RCN_LUCHS] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_RCN_LUCHS_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Gepard Self-Propelled Anti-Aircraft Artillery (SPAAA)
        /// </summary>
        private static void Create_SPAAA_Gepard_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Flakpanzer Gepard SPAAA",
                    nationality: Nationality.FRG,
                    weaponSystemID: WeaponSystems.SPAAA_GEPARD,
                    hardAttack: CUConstants.BASE_AAA_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_AAA_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_AAA_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_AAA_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_AAA_GROUND_AIR_ATTACK + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_AAA,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_AAA,
                    spottingRange: CUConstants.BASE_AAA_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT);

                profile.AddUpgradeType(UpgradeType.SPAAA);
                profile.SetShortName("Gepard");
                profile.SetTurnAvailable(444); // 1975
                _weaponSystemProfiles[WeaponSystems.SPAAA_GEPARD] = profile;
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(Create_SPAAA_Gepard_Profile), e); throw; }
        }

        /// <summary>
        /// Bo 105P PAH‑1 Attack Helicopter
        /// </summary>
        private static void Create_HEL_BO105_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Bo 105P PAH‑1 Attack Helicopter",
                    nationality: Nationality.FRG,
                    weaponSystemID: WeaponSystems.HEL_BO105,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HEL_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_HEL_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_HEL_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_HEL_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_HELO,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.Day,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    movementPoints: CUConstants.HELO_UNIT);

                profile.AddUpgradeType(UpgradeType.ATTHELO);
                profile.SetShortName("Bo105");
                profile.SetTurnAvailable(444); // 1975
                _weaponSystemProfiles[WeaponSystems.HEL_BO105] = profile;
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(Create_HEL_BO105_Profile), e); throw; }
        }

        /// <summary>
        /// Tornado IDS Multi-Role FGT
        /// </summary>
        private static void Create_MRF_TORNADO_IDS_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Tornado IDS Multirole Fighter",
                    nationality: Nationality.FRG,
                    weaponSystemID: WeaponSystems.FGT_TORNADO_IDS,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.MID_FGT_DOGFIGHT + MEDIUM_BONUS,
                    maneuverability: CUConstants.MID_FGT_MANEUVER,
                    topSpeed: CUConstants.MID_FGT_TOPSPEED,
                    survivability: CUConstants.MID_FGT_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_2,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD + MEDIUM_BONUS,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ENHANCED,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("Tornado IDS");

                // Set turn availability - entered service in 1960
                profile.SetTurnAvailable(264);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_TORNADO_IDS] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_MRF_TORNADO_IDS_Profile), e);
                throw;
            }
        }

        #endregion

        //-----------------------------------------------------------------------------------------
        
        #region United Kingdom (UK)

        private static void Create_TANK_CHALLENGER1_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Challenger 1 MBT",
                    nationality: Nationality.UK,
                    weaponSystemID: WeaponSystems.TANK_CHALLENGER1,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN2_TANK_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.GEN2_TANK_HARD_DEFENSE + XXLARGE_BONUS,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT);

                profile.AddUpgradeType(UpgradeType.AFV);
                profile.SetShortName("Challenger 1");
                profile.SetTurnAvailable(552); // 1984
                _weaponSystemProfiles[WeaponSystems.TANK_CHALLENGER1] = profile;
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(Create_TANK_CHALLENGER1_Profile), e); throw; }
        }

        private static void Create_IFV_WARRIOR_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Warrior IFV",
                    nationality: Nationality.UK,
                    weaponSystemID: WeaponSystems.IFV_WARRIOR,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK + MEDIUM_BONUS,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    movementPoints: CUConstants.MECH_UNIT);

                profile.AddUpgradeType(UpgradeType.IFV);
                profile.SetShortName("Warrior");
                profile.SetTurnAvailable(576); // 1986
                _weaponSystemProfiles[WeaponSystems.IFV_WARRIOR] = profile;
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(Create_IFV_WARRIOR_Profile), e); throw; }
        }

        /// <summary>
        /// FV432 Armored Personnel Carrier
        /// </summary>
        private static void Create_APC_FV432_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "FV 432",
                    nationality: Nationality.UK,
                    weaponSystemID: WeaponSystems.APC_FV432,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_APC_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_APC_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_APC_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_APC_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.APC);

                // Set short name for UI display
                profile.SetShortName("FV 432");

                // Set turn availability - entered service in 1960
                profile.SetTurnAvailable(264);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.APC_FV432] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_APC_FV432_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// FV432 Armored Personnel Carrier
        /// </summary>
        private static void Create_RCN_Scimitar_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Scimitar Recon Vehicle",
                    nationality: Nationality.UK,
                    weaponSystemID: WeaponSystems.RCN_SCIMITAR,
                   CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK + XLARGE_BONUS,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.RCN);

                // Set short name for UI display
                profile.SetShortName("Scimitar");

                // Set turn availability
                profile.SetTurnAvailable(320);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.RCN_SCIMITAR] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_RCN_Scimitar_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Rapier Medium-Range SAM System
        /// </summary>
        private static void Create_SAM_Rapier_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Rapier SAM System",
                    nationality: Nationality.UK,
                    weaponSystemID: WeaponSystems.SAM_RAPIER,
                    CUConstants.PRESTIGE_TIER_FREE,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_SAM_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_SAM_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SAM_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SAM_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_SAM_GROUND_AIR_ATTACK + SMALL_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_SAM,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_SAM,
                    spottingRange: CUConstants.BASE_SAM_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.SAM);

                // Set short name for UI display
                profile.SetShortName("Rapier");

                // Set turn availability - entered service in 1960
                profile.SetTurnAvailable(300);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SAM_RAPIER] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SAM_Rapier_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Lynx Transport Helicopter
        /// </summary>
        private static void Create_HEL_LYNX_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Westland Lynx",
                    nationality: Nationality.UK,
                    weaponSystemID: WeaponSystems.HEL_LYNX,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HEL_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_HEL_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_HEL_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_HEL_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_HELO,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.ATTHELO);

                // Set short name for UI display
                profile.SetShortName("Lynx");

                // Set turn availability - entered service in 1986
                profile.SetTurnAvailable(400);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.HEL_LYNX] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_HEL_LYNX_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Tornado GR1 Multi-Role FGT
        /// </summary>
        private static void Create_MRF_GR1_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Tornado GR.1 Multirole Fighter",
                    nationality: Nationality.UK,
                    weaponSystemID: WeaponSystems.FGT_TORNADO_GR1,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.MID_FGT_DOGFIGHT + LARGE_BONUS,
                    maneuverability: CUConstants.MID_FGT_MANEUVER,
                    topSpeed: CUConstants.MID_FGT_TOPSPEED,
                    survivability: CUConstants.MID_FGT_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_2,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ENHANCED,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("Tornado GR.1");

                // Set turn availability - entered service in 1960
                profile.SetTurnAvailable(264);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_TORNADO_GR1] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_MRF_GR1_Profile), e);
                throw;
            }
        }

        #endregion

        //-----------------------------------------------------------------------------------------

        #region France

        /// <summary>
        /// AMX-30 Main Battle Tank (MBT)
        /// </summary>
        private static void Create_TANK_AMX30_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "AMX‑30 MBT",
                    nationality: Nationality.FRA,
                    weaponSystemID: WeaponSystems.TANK_AMX30,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.GEN1_TANK_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.GEN1_TANK_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_TANK_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_TANK_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_ARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT);

                profile.AddUpgradeType(UpgradeType.AFV);
                profile.SetShortName("AMX‑30");
                profile.SetTurnAvailable(324); // 1965
                _weaponSystemProfiles[WeaponSystems.TANK_AMX30] = profile;
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(Create_TANK_AMX30_Profile), e); throw; }
        }

        /// <summary>
        /// Roland SAM
        /// </summary>
        private static void Create_SPSAM_Roland_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Roland SAM",
                    nationality: Nationality.FRA,
                    weaponSystemID: WeaponSystems.SPSAM_ROLAND,
                    CUConstants.PRESTIGE_TIER_FREE,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_SAM_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_SAM_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SAM_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SAM_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_SAM_GROUND_AIR_ATTACK + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_SAM,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_SAM,
                    spottingRange: CUConstants.BASE_SAM_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.MECH_UNIT);

                profile.AddUpgradeType(UpgradeType.SPSAM);
                profile.SetShortName("Roland");
                profile.SetTurnAvailable(456); // 1976
                _weaponSystemProfiles[WeaponSystems.SPSAM_ROLAND] = profile;
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(Create_SPSAM_Roland_Profile), e); throw; }
        }

        /// <summary>
        /// AMX-10P IFV
        /// </summary>
        private static void Create_IFV_AMX10P_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "AMX-10P",
                    nationality: Nationality.FRA,
                    weaponSystemID: WeaponSystems.IFV_AMX10P,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE + LARGE_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    movementPoints: CUConstants.MECH_UNIT);

                profile.AddUpgradeType(UpgradeType.IFV);
                profile.SetShortName("AMX-10P");
                profile.SetTurnAvailable(300); // 1973
                _weaponSystemProfiles[WeaponSystems.IFV_AMX10P] = profile;
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(Create_IFV_AMX10P_Profile), e); throw; }
        }

        /// <summary>
        /// VAB Armored Personnel Carrier
        /// </summary>
        private static void Create_APC_VAB_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "VAB APC",
                    nationality: Nationality.FRA,
                    weaponSystemID: WeaponSystems.APC_VAB,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_APC_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_APC_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_APC_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_APC_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.APC);

                // Set short name for UI display
                profile.SetShortName("VAB");

                // Set turn availability - entered service in 1960
                profile.SetTurnAvailable(264);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.APC_VAB] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_APC_VAB_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// ERC-90 Recon Vehicle
        /// </summary>
        private static void Create_RCN_ERC90_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "ERC-90 Recon Vehicle",
                    nationality: Nationality.FRA,
                    weaponSystemID: WeaponSystems.RCN_ERC90,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_IFV_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_IFV_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_IFV_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_IFV_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.MECH_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.RECON);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("ERC-90");

                // Set turn availability in months.
                profile.SetTurnAvailable(288);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.RCN_ERC90] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_RCN_ERC90_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// AUF-1 Self-Propelled Howitzer
        /// </summary>
        private static void Create_SPA_AUF1_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "AMX-30 AuF1 Self-Propelled Artillery",
                    nationality: Nationality.FRA,
                    weaponSystemID: WeaponSystems.SPA_AUF1,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + MEDIUM_BONUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_LIGHTARMOR,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_155MM,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Large
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.SPA);

                // Set short name for UI display
                profile.SetShortName("AuF1");

                // Set turn availability - entered service in 1963
                profile.SetTurnAvailable(300);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPA_AUF1] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_SPA_AUF1_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Mirage 2000 FGT
        /// </summary>
        private static void Create_ASF_M2000_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Mirage 2000 Air Superiority Fighter",
                    nationality: Nationality.FRA,
                    weaponSystemID: WeaponSystems.FGT_MIRAGE2000,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.MID_FGT_DOGFIGHT + XLARGE_BONUS,
                    maneuverability: CUConstants.MID_FGT_MANEUVER + XLARGE_BONUS,
                    topSpeed: CUConstants.MID_FGT_TOPSPEED + MEDIUM_BONUS,
                    survivability: CUConstants.MID_FGT_SURVIVE,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_2,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_ENHANCED,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("Mirage 2000");

                // Set turn availability - entered service in 1976
                profile.SetTurnAvailable(456);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.FGT_F15] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ASF_M2000_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// SEPECAT Jaguar ATT
        /// </summary>
        private static void Create_ATT_JAGUAR_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "SEPECAT Jaguar Attack Aircraft",
                    nationality: Nationality.FRA,
                    weaponSystemID: WeaponSystems.ATT_JAGUAR,
                    CUConstants.PRESTIGE_TIER_FREE,
                    weaponSystemType: WeaponSystemType.Air,
                    dogfighting: CUConstants.EARLY_FGT_DOGFIGHT,
                    maneuverability: CUConstants.EARLY_FGT_MANEUVER,
                    topSpeed: CUConstants.EARLY_FGT_TOPSPEED + MEDIUM_MALUS,
                    survivability: CUConstants.MID_FGT_SURVIVE + MEDIUM_BONUS,
                    groundAttack: CUConstants.GROUND_ATTACK_TIER_2,
                    ordinanceLoad: CUConstants.SMALL_AC_LOAD + LARGE_BONUS,
                    stealth: 0,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.AC_SPOTTING_BASIC,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.FGT);

                // Set short name for UI display
                profile.SetShortName("SEPECAT Jaguar");

                // Set turn availability - entered service in 1976
                profile.SetTurnAvailable(456);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ATT_JAGUAR] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_ATT_JAGUAR_Profile), e);
                throw;
            }
        }

        #endregion

        //-----------------------------------------------------------------------------------------

        #region Generic Weapons

        /// <summary>
        /// Generic AAA profile
        /// </summary>
        private static void Create_GENERIC_AAA_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Anti-Aircraft Artillery",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_AAA,
                    CUConstants.PRESTIGE_TIER_0,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_AAA_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_AAA_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_AAA_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_AAA_SOFT_DEFENSE,
                    groundAirAttack: CUConstants.BASE_AAA_GROUND_AIR_ATTACK,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_AAA,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_AAA,
                    spottingRange: CUConstants.BASE_AAA_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.AirDrop,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.AAA);

                // Set short name for UI display
                profile.SetShortName("AAA");

                // Set turn availability - entered service in 1957
                profile.SetTurnAvailable(228);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_AAA] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_GENERIC_AAA_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Generic Man-portable Air Defense profile
        /// </summary>
        private static void Create_GENERIC_MANPAD_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Man-Portable Air Defense",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_MANPAD,
                    CUConstants.PRESTIGE_TIER_0,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_INF_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_INF_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_INF_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_INF_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SAM);

                // Set short name for UI display
                profile.SetShortName("MANPAD");

                // Set turn availability - entered service in 1968
                profile.SetTurnAvailable(360);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_MANPAD] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_GENERIC_MANPAD_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic light towed artillery profile.
        /// </summary>
        private static void Create_GENERIC_LightArt_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Light Towed Artillery",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_ART_LIGHT,
                    CUConstants.PRESTIGE_TIER_0,
                    weaponSystemType: WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_120MM,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.STATIC_UNIT
                );

                //Set the upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ART);

                // Set short name for UI display
                profile.SetShortName("Light Artillery");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_ART_LIGHT] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_GENERIC_LightArt_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic light towed artillery profile.
        /// </summary>
        private static void Create_GENERIC_HeavytArt_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Heavy Towed Artillery",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_ART_HEAVY,
                    CUConstants.PRESTIGE_TIER_0 + MASSIVE_BONUS,
                    weaponSystemType: WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_155MM,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.STATIC_UNIT
                );

                //Set the upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ART);

                // Set short name for UI display
                profile.SetShortName("Heavy Artillery");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_ART_HEAVY] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_GENERIC_HeavytArt_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic Anti-Tank Guided Missile (ATGM) profile.
        /// </summary>
        private static void Create_GENERIC_ATGM_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Anti-Tank Guided Missile",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_ATGM,
                    CUConstants.PRESTIGE_TIER_0,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_INF_HARD_ATTACK + XXLARGE_BONUS,
                    hardDefense: CUConstants.BASE_INF_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_INF_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_INF_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.ATGM);

                // Set short name for UI display
                profile.SetShortName("ATGM");

                // Set turn availability in months.
                profile.SetTurnAvailable(432);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_ATGM] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_GENERIC_ATGM_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic light mortar profile.
        /// </summary>
        private static void Create_GENERIC_LightMortar_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "82mm Light Mortar",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_MORTAR_82MM,
                    CUConstants.PRESTIGE_TIER_0 + MASSIVE_MALUS,
                    weaponSystemType: WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK + MEDIUM_MALUS,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + LARGE_MALUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_120MM + MEDIUM_MALUS,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                //Set the upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ART);

                // Set short name for UI display
                profile.SetShortName("82mm Mortar");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_MORTAR_82MM] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_GENERIC_LightMortar_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic heavy mortar profile.
        /// </summary>
        private static void Create_GENERIC_HeavyMortar_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "120mm Heavy Mortar",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_MORTAR_120MM,
                    CUConstants.PRESTIGE_TIER_0 + LARGE_MALUS,
                    weaponSystemType: WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_ARTY_HARD_ATTACK + SMALL_MALUS,
                    hardDefense: CUConstants.BASE_ARTY_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_ARTY_SOFT_ATTACK + SMALL_MALUS,
                    softDefense: CUConstants.BASE_ARTY_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_120MM + SMALL_MALUS,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                //Set the upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.ART);

                // Set short name for UI display
                profile.SetShortName("120mm Mortar");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_MORTAR_120MM] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_GENERIC_HeavyMortar_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic RPG-7 profile.
        /// </summary>
        private static void Create_GENERIC_RPG7_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "RPG-7",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_RPG7,
                    CUConstants.PRESTIGE_TIER_0 + MASSIVE_MALUS,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_INF_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.BASE_INF_HARD_DEFENSE + MEDIUM_MALUS,
                    softAttack: CUConstants.BASE_INF_SOFT_ATTACK + MEDIUM_MALUS,
                    softDefense: CUConstants.BASE_INF_SOFT_DEFENSE + MEDIUM_MALUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.ATGM);

                // Set short name for UI display
                profile.SetShortName("RPG-7");

                // Set turn availability in months.
                profile.SetTurnAvailable(250);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_RPG7] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_GENERIC_RPG7_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic Recoiless Rifle profile.
        /// </summary>
        private static void Create_GENERIC_RECOILLESS_RIFLE_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Recoilless Rifle",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_RECOILLESS_RIFLE,
                    CUConstants.PRESTIGE_TIER_0 + MASSIVE_MALUS,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_INF_HARD_ATTACK + LARGE_BONUS,
                    hardDefense: CUConstants.BASE_INF_HARD_DEFENSE + MEDIUM_MALUS,
                    softAttack: CUConstants.BASE_INF_SOFT_ATTACK + MEDIUM_MALUS,
                    softDefense: CUConstants.BASE_INF_SOFT_DEFENSE + MEDIUM_MALUS,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.ATGM);

                // Set short name for UI display
                profile.SetShortName("Recoilless Rifle");

                // Set turn availability in months.
                profile.SetTurnAvailable(250);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_RECOILLESS_RIFLE] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_GENERIC_RECOILLESS_RIFLE_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic mounted cavalry profile.
        /// </summary>
        private static void Create_GENERIC_CAVALRY_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Mounted Cavalry Unit",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_CAVALRY,
                    CUConstants.PRESTIGE_TIER_0,
                    WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_INF_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_INF_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_INF_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_INF_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    movementPoints: CUConstants.CAVALRY_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.INF);

                // Set short name for UI display
                profile.SetShortName("Cavalry");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_CAVALRY] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_GENERIC_CAVALRY_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic truck profile.
        /// </summary>
        private static void Create_GENERIC_TRUCK_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Truck",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_TRUCK,
                    CUConstants.PRESTIGE_TIER_0,
                    weaponSystemType: WeaponSystemType.Soft,
                    hardAttack: CUConstants.TRUCK_HARD_ATTACK,
                    hardDefense: CUConstants.TRUCK_HARD_DEFENSE,
                    softAttack: CUConstants.TRUCK_SOFT_ATTACK,
                    softDefense: CUConstants.TRUCK_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.AirLift,
                    movementPoints: CUConstants.MOT_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.APC);

                // Set short name for UI display
                profile.SetShortName("Truck");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_TRUCK] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_GENERIC_TRUCK_Profile), e);
                throw;
            }
        }

        #endregion

        //-----------------------------------------------------------------------------------------

        #region Bases

        /// <summary>
        /// Create a generic land base profile.
        /// </summary>
        private static void Create_BASE_Landbase_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "HQ LandBase",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_LANDBASE,
                    CUConstants.PRESTIGE_TIER_2,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.FACILITY_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.HQLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.STATIC_UNIT
                );

                // Upgrade paths
                profile.AddUpgradeType(UpgradeType.BASE);

                // Set short name for UI display
                profile.SetShortName("HQ");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_LANDBASE] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_BASE_Landbase_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic airbase profile.
        /// </summary>
        private static void Create_BASE_Airbase_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Airbase",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_AIRBASE,
                    CUConstants.PRESTIGE_TIER_4,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.FACILITY_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.STATIC_UNIT
                );

                // Upgrade paths
                profile.AddUpgradeType(UpgradeType.BASE);

                // Set short name for UI display
                profile.SetShortName("Airbase");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_AIRBASE] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_BASE_Airbase_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic supply depot profile.
        /// </summary>
        private static void Create_BASE_Depot_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Suppy Depot",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_SUPPLYDEPOT,
                    CUConstants.PRESTIGE_TIER_3,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.FACILITY_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.STATIC_UNIT
                );

                // Upgrade paths
                profile.AddUpgradeType(UpgradeType.BASE);

                // Set short name for UI display
                profile.SetShortName("Suppy Depot");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_SUPPLYDEPOT] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_BASE_Depot_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Create a generic intelligence gathering base profile.
        /// </summary>
        private static void Create_BASE_Intel_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Intelligence Gathering Base",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.GENERIC_LANDBASE,
                    CUConstants.PRESTIGE_TIER_3,
                    weaponSystemType: WeaponSystemType.Hard,
                    hardAttack: CUConstants.BASE_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.INTEL_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    movementPoints: CUConstants.STATIC_UNIT
                );

                // Upgrade paths
                profile.AddUpgradeType(UpgradeType.BASE);

                // Set short name for UI display
                profile.SetShortName("Intel Base");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.GENERIC_LANDBASE] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_BASE_Intel_Profile), e);
                throw;
            }
        }

        #endregion

        //-----------------------------------------------------------------------------------------

        #region Infantry

        /// <summary>
        /// Regular Infantry
        /// </summary>
        private static void Create_INF_REG_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Regular Infantry",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.INF_REG,
                    CUConstants.PRESTIGE_TIER_0,
                    weaponSystemType: WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_INF_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_INF_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_INF_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_INF_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                // Upgrade paths
                profile.AddUpgradeType(UpgradeType.INF);

                // Set short name for UI display
                profile.SetShortName("Regulars");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.INF_REG] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_INF_REG_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Airborne infantry
        /// </summary>
        private static void Create_INF_AB_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Airborne Infantry",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.INF_AB,
                    CUConstants.PRESTIGE_TIER_0 + MASSIVE_BONUS,
                    weaponSystemType: WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_INF_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_INF_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_INF_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_INF_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                // Upgrade paths
                profile.AddUpgradeType(UpgradeType.INF);

                // Set short name for UI display
                profile.SetShortName("Airborne");

                // Set turn availability in months.
                profile.SetTurnAvailable(50);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.INF_AB] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_INF_AB_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Air-Mobile Infantry
        /// </summary>
        private static void Create_INF_AM_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Air‑Mobile Infantry",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.INF_AM,
                    CUConstants.PRESTIGE_TIER_1,
                    weaponSystemType: WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_INF_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_INF_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_INF_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_INF_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirMobile,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.INF);

                // Set short name for UI display
                profile.SetShortName("Air-Mobile");

                // Set turn availability in months.
                profile.SetTurnAvailable(376);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.INF_AM] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_INF_AM_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Marine infantry
        /// </summary>
        private static void Create_INF_MAR_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Marine Infantry",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.INF_MAR,
                    CUConstants.PRESTIGE_TIER_1,
                    weaponSystemType: WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_INF_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_INF_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_INF_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_INF_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.NavalAssault,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.INF);

                // Set short name for UI display
                profile.SetShortName("Marines");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.INF_MAR] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_INF_MAR_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Special Forces Infantry
        /// </summary>
        private static void Create_INF_SPEC_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Special Forces Infantry",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.INF_SPEC,
                    CUConstants.PRESTIGE_TIER_0,
                    weaponSystemType: WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_INF_HARD_ATTACK + MEDIUM_BONUS,
                    hardDefense: CUConstants.BASE_INF_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_INF_SOFT_ATTACK + MEDIUM_BONUS,
                    softDefense: CUConstants.BASE_INF_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.RECON_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirMobile,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.INF);

                // Set short name for UI display
                profile.SetShortName("Special Forces");

                // Set turn availability in months.
                profile.SetTurnAvailable(1);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.INF_SPEC] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_INF_SPEC_Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Combat Engineers Infantry
        /// </summary>
        private static void Create_INF_ENG_Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Combat Engineers",
                    nationality: Nationality.GENERIC,
                    weaponSystemID: WeaponSystems.INF_ENG,
                    CUConstants.PRESTIGE_TIER_0,
                    weaponSystemType: WeaponSystemType.Soft,
                    hardAttack: CUConstants.BASE_INF_HARD_ATTACK,
                    hardDefense: CUConstants.BASE_INF_HARD_DEFENSE,
                    softAttack: CUConstants.BASE_INF_SOFT_ATTACK,
                    softDefense: CUConstants.BASE_INF_SOFT_DEFENSE,
                    groundAirDefense: CUConstants.GROUND_DEFENSE_INFANTRY,
                    primaryRange: CUConstants.PRIMARY_RANGE_DEFAULT,
                    indirectRange: CUConstants.INDIRECT_RANGE_DEFAULT,
                    spottingRange: CUConstants.BASE_UNIT_SPOTTING_RANGE,
                    allWeatherCapability: AllWeatherRating.GroundUnit,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    movementPoints: CUConstants.FOOT_UNIT
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.INF);

                // Set short name for UI display
                profile.SetShortName("Engineers");

                // Set turn availability in months.
                profile.SetTurnAvailable(300);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.INF_ENG] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Create_INF_ENG_Profile), e);
                throw;
            }
        }

        #endregion

        //-----------------------------------------------------------------------------------------
    }
}