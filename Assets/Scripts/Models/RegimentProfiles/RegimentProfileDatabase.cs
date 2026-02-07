using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Controllers;

namespace HammerAndSickle.Models
{
    public static class RegimentProfileDatabase
    {
        #region Constants

        private const string CLASS_NAME = nameof(RegimentProfileDatabase);

        // Centralized place for combat rating modifiers
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

        private static Dictionary<RegimentProfileType, RegimentProfile> _regimentProfiles;
        private static bool _isInitialized = false;

        #endregion // Private Fields

        #region Public Properties

        /// <summary>
        /// Gets whether the database has been initialized with regiment profiles.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the total number of regiment profiles in the database.
        /// </summary>
        public static int ProfileCount => _regimentProfiles?.Count ?? 0;

        #endregion // Public Properties

        #region Public Methods

        /// <summary>
        /// Initializes the regiment profile database with all game profiles.
        /// Must be called during game startup before any profile lookups.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    AppService.CaptureUiMessage("RegimentProfileDatabase already initialized, skipping");
                    return;
                }

                _regimentProfiles = new Dictionary<RegimentProfileType, RegimentProfile>();

                CreateAllRegimentProfiles();

                _isInitialized = true;
                AppService.CaptureUiMessage($"RegimentProfileDatabase initialized with {ProfileCount} profiles");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Initialize), e);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a regiment profile by its enum identifier.
        /// </summary>
        /// <param name="profileType">The regiment profile type to look up</param>
        /// <returns>The corresponding RegimentProfile, or null if not found</returns>
        public static RegimentProfile GetRegimentProfile(RegimentProfileType profileType)
        {
            try
            {
                if (!_isInitialized)
                {
                    AppService.CaptureUiMessage("RegimentProfileDatabase not initialized - call Initialize() first");
                    return null;
                }

                if (profileType == RegimentProfileType.Default)
                {
                    return null;
                }

                return _regimentProfiles.TryGetValue(profileType, out var profile) ? profile : null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetRegimentProfile), e);
                return null;
            }
        }

        /// <summary>
        /// Checks if a regiment profile exists in the database by its enum identifier.
        /// </summary>
        /// <param name="profileType">The regiment profile type to check</param>
        /// <returns>True if the profile exists, false otherwise</returns>
        public static bool HasRegimentProfile(RegimentProfileType profileType)
        {
            try
            {
                if (!_isInitialized)
                {
                    return false;
                }

                if (profileType == RegimentProfileType.Default)
                {
                    return false;
                }

                return _regimentProfiles.ContainsKey(profileType);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HasRegimentProfile), e);
                return false;
            }
        }

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Creates all regiment profiles used in the game.
        /// This method contains the complete database of regiment configurations.
        /// </summary>
        private static void CreateAllRegimentProfiles()
        {
            try
            {
                // Soviet profiles
                CreateSovietMotorRifleProfiles();
                CreateSovietTankProfiles();
                CreateSovietArtilleryProfiles();
                CreateSovietRocketProfiles();
                CreateSovietAirAssaultProfiles();
                CreateSovietAirborneProfiles();
                CreateSovietNavalInfantryProfiles();
                CreateSovietReconProfiles();
                CreateSovietAirDefenseProfiles();
                CreateSovietSAMProfiles();
                CreateSovietHelicopterProfiles();
                CreateSovietFighterProfiles();
                CreateSovietAttackAviationProfiles();
                CreateSovietBomberProfiles();
                CreateSovietReconAviationProfiles();
                CreateSovietFacilityProfiles();

                // Mujahideen profiles
                CreateMujahideenProfiles();

                // US profiles
                CreateUSArmoredProfiles();
                CreateUSMechanizedProfiles();
                CreateUSAirborneProfiles();
                CreateUSAviationProfiles();
                CreateUSArtilleryProfiles();
                CreateUSAirDefenseProfiles();
                CreateUSFighterProfiles();
                CreateUSAttackAviationProfiles();
                CreateUSBomberProfiles();

                // FRG (West Germany) profiles
                CreateFRGProfiles();

                // UK profiles
                CreateUKProfiles();

                // France profiles
                CreateFranceProfiles();

                // Arab profiles
                CreateArabProfiles();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAllRegimentProfiles), e);
                throw;
            }
        }

        /// <summary>
        /// Adds a regiment profile to the database with validation.
        /// </summary>
        /// <param name="profileType">The profile type identifier</param>
        /// <param name="profile">The regiment profile to add</param>
        private static void AddProfile(RegimentProfileType profileType, RegimentProfile profile)
        {
            try
            {
                if (profileType == RegimentProfileType.Default)
                    throw new ArgumentException("Cannot add Default profile type", nameof(profileType));

                if (profile == null)
                    throw new ArgumentNullException(nameof(profile));

                if (_regimentProfiles.ContainsKey(profileType))
                    throw new InvalidOperationException($"Profile type '{profileType}' already exists in database");

                _regimentProfiles[profileType] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AddProfile), e);
                throw;
            }
        }

        #endregion // Private Methods
        
        #region Soviet Motor Rifle Profiles

        private static void CreateSovietMotorRifleProfiles()
        {
            #region BTR70 MRR

            // Mounted Profile (BTR-70)
            WeaponProfile mountedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,
                _softAtt: GameData.BASE_APC_SOFT_ATTACK,
                _softDef: GameData.BASE_APC_SOFT_DEFENSE + SMALL_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.Gen1,
                _sil: UnitSilhouette.Medium
            );

            // Setup icons for mounted profile
            mountedProfile.IconProfile.IconType = RegimentIconType.Directional;
            mountedProfile.IconProfile.W  = SpriteManager.SV_BTR70_W;
            mountedProfile.IconProfile.NW = SpriteManager.SV_BTR70_NW;
            mountedProfile.IconProfile.SW = SpriteManager.SV_BTR70_SW;

            // Deployed Profile (Infantry)
            WeaponProfile deployedProfile = new WeaponProfile(
                 _hardAtt: GameData.BASE_INF_HARD_ATTACK,
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.FOOT_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.Gen1,
                _sil: UnitSilhouette.Medium
                );

            // Setup icons for deployed profile
            deployedProfile.IconProfile.IconType = RegimentIconType.Single;
            deployedProfile.IconProfile.W  = SpriteManager.SV_Regulars;

            /* Embarked profile will be situational */

            // Create the regiment profile for the Soviet BTR-70 Motor Rifle Regiment
            RegimentProfile regimentProfile = new RegimentProfile();

            regimentProfile.Initialize(
                name: "BTR-70 Motor Rifle Regiment",
                profileType: RegimentProfileType.SV_MRR_BTR70,
                turnAvailable: 408,
                prestigeCost: GameData.PRESTIGE_TIER_0 + LARGE_BONUS,
                upgradePath: UpgradeType.APC,
                mobile: mountedProfile,
                deployed: deployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_MRR_BTR70, regimentProfile);

            #endregion // BTR70 MRR

            #region BTR80 MRR

            // Mounted Profile (BTR-80)
            WeaponProfile btr80MountedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,
                _softAtt: GameData.BASE_APC_SOFT_ATTACK + SMALL_BONUS,
                _softDef: GameData.BASE_APC_SOFT_DEFENSE + SMALL_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.Gen2,
                _sil: UnitSilhouette.Small
            );

            // Setup icons for mounted profile
            btr80MountedProfile.IconProfile.IconType = RegimentIconType.Directional;
            btr80MountedProfile.IconProfile.W  = SpriteManager.SV_BTR80_W;
            btr80MountedProfile.IconProfile.NW = SpriteManager.SV_BTR80_NW;
            btr80MountedProfile.IconProfile.SW = SpriteManager.SV_BTR80_SW;

            // Deployed Profile (Infantry)
            WeaponProfile btr80DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_INFANTRY,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.FOOT_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.Gen2,
                _sil: UnitSilhouette.Medium
            );

            // Setup icons for deployed profile
            btr80DeployedProfile.IconProfile.IconType = RegimentIconType.Single;
            btr80DeployedProfile.IconProfile.W = SpriteManager.SV_Regulars;

            // Create the regiment profile for the Soviet BTR-80 Motor Rifle Regiment
            RegimentProfile btr80RegimentProfile = new RegimentProfile();

            btr80RegimentProfile.Initialize(
                name: "BTR-80 Motor Rifle Regiment",
                profileType: RegimentProfileType.SV_MRR_BTR80,
                turnAvailable: 576,
                prestigeCost: GameData.PRESTIGE_TIER_0 + XLARGE_BONUS,
                upgradePath: UpgradeType.APC,
                mobile: btr80MountedProfile,
                deployed: btr80DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_MRR_BTR80, btr80RegimentProfile);

            #endregion // BTR80 MRR

            #region BMP1 MRR

            // Mounted Profile (BMP-1)
            WeaponProfile bmp1MountedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK + XXLARGE_BONUS,
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK,
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.Gen1,
                _sil: UnitSilhouette.Small
            );

            // Setup icons for mounted profile
            bmp1MountedProfile.IconProfile.IconType = RegimentIconType.Directional;
            bmp1MountedProfile.IconProfile.W  = SpriteManager.SV_BMP1_W;
            bmp1MountedProfile.IconProfile.NW = SpriteManager.SV_BMP1_NW;
            bmp1MountedProfile.IconProfile.SW = SpriteManager.SV_BMP1_SW;

            // Deployed Profile (Infantry)
            WeaponProfile bmp1DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_INFANTRY,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.FOOT_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.Gen1,
                _sil: UnitSilhouette.Medium
            );

            // Setup icons for deployed profile
            bmp1DeployedProfile.IconProfile.IconType = RegimentIconType.Single;
            bmp1DeployedProfile.IconProfile.W = SpriteManager.SV_Regulars;

            // Create the regiment profile for the Soviet BMP-1 Motor Rifle Regiment
            RegimentProfile bmp1RegimentProfile = new RegimentProfile();

            bmp1RegimentProfile.Initialize(
                name: "BMP-1 Motor Rifle Regiment",
                profileType: RegimentProfileType.SV_MRR_BMP1,
                turnAvailable: 336,
                prestigeCost: GameData.PRESTIGE_TIER_0 + MASSIVE_BONUS,
                upgradePath: UpgradeType.IFV,
                mobile: bmp1MountedProfile,
                deployed: bmp1DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_MRR_BMP1, bmp1RegimentProfile);

            #endregion // BMP1 MRR

            #region BMP2 MRR

            // Mounted Profile (BMP-2)
            WeaponProfile bmp2MountedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS,
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK,
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE + SMALL_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.Gen2,
                _sil: UnitSilhouette.Small
            );

            // Setup icons for mounted profile
            bmp2MountedProfile.IconProfile.IconType = RegimentIconType.Directional;
            bmp2MountedProfile.IconProfile.W  = SpriteManager.SV_BMP2_W;
            bmp2MountedProfile.IconProfile.NW = SpriteManager.SV_BMP2_NW;
            bmp2MountedProfile.IconProfile.SW = SpriteManager.SV_BMP2_SW;

            // Deployed Profile (Infantry)
            WeaponProfile bmp2DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_INFANTRY,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.FOOT_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.Gen2,
                _sil: UnitSilhouette.Medium
            );

            // Setup icons for deployed profile
            bmp2DeployedProfile.IconProfile.IconType = RegimentIconType.Single;
            bmp2DeployedProfile.IconProfile.W = SpriteManager.SV_Regulars;

            // Create the regiment profile for the Soviet BMP-2 Motor Rifle Regiment
            RegimentProfile bmp2RegimentProfile = new RegimentProfile();

            bmp2RegimentProfile.Initialize(
                name: "BMP-2 Motor Rifle Regiment",
                profileType: RegimentProfileType.SV_MRR_BMP2,
                turnAvailable: 504,
                prestigeCost: GameData.PRESTIGE_TIER_0 + MASSIVE_BONUS + XXLARGE_BONUS,
                upgradePath: UpgradeType.IFV,
                mobile: bmp2MountedProfile,
                deployed: bmp2DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_MRR_BMP2, bmp2RegimentProfile);

            #endregion // BMP2 MRR

            #region BMP3 MRR

            // Mounted Profile (BMP-3)
            WeaponProfile bmp3MountedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS,
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK + SMALL_BONUS,
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE + MEDIUM_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen2,
                _nvg: NVG_Rating.Gen2,
                _sil: UnitSilhouette.Small
            );

            // Setup icons for mounted profile
            bmp3MountedProfile.IconProfile.IconType = RegimentIconType.Directional;
            bmp3MountedProfile.IconProfile.W  = SpriteManager.SV_BMP3_W;
            bmp3MountedProfile.IconProfile.NW = SpriteManager.SV_BMP3_NW;
            bmp3MountedProfile.IconProfile.SW = SpriteManager.SV_BMP3_SW;

            // Deployed Profile (Infantry)
            WeaponProfile bmp3DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_INFANTRY,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.FOOT_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen2,
                _nvg: NVG_Rating.Gen2,
                _sil: UnitSilhouette.Medium
            );

            // Setup icons for deployed profile
            bmp3DeployedProfile.IconProfile.IconType = RegimentIconType.Single;
            bmp3DeployedProfile.IconProfile.W = SpriteManager.SV_Regulars;

            // Create the regiment profile for the Soviet BMP-3 Motor Rifle Regiment
            RegimentProfile bmp3RegimentProfile = new RegimentProfile();

            bmp3RegimentProfile.Initialize(
                name: "BMP-3 Motor Rifle Regiment",
                profileType: RegimentProfileType.SV_MRR_BMP3,
                turnAvailable: 600,
                prestigeCost: GameData.PRESTIGE_TIER_1 + XXLARGE_MALUS,
                upgradePath: UpgradeType.IFV,
                mobile: bmp3MountedProfile,
                deployed: bmp3DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_MRR_BMP3, bmp3RegimentProfile);

            #endregion // BMP3 MRR
        }

        #endregion // Soviet Motor Rifle Profiles

        #region Soviet Tank Profiles

        private static void CreateSovietTankProfiles()
        {
            #region T-55 Tank Regiment

            WeaponProfile t55DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.GEN1_TANK_HARD_ATTACK,
                _hardDef: GameData.GEN1_TANK_HARD_DEFENSE,
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_ARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.None,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Medium
            );

            t55DeployedProfile.IconProfile.IconType = RegimentIconType.Directional;
            t55DeployedProfile.IconProfile.W  = SpriteManager.SV_T55A_W;
            t55DeployedProfile.IconProfile.NW = SpriteManager.SV_T55A_NW;
            t55DeployedProfile.IconProfile.SW = SpriteManager.SV_T55A_SW;

            RegimentProfile t55RegimentProfile = new RegimentProfile();
            t55RegimentProfile.Initialize(
                name: "T-55 Tank Regiment",
                profileType: RegimentProfileType.SV_TR_T55,
                turnAvailable: 300,
                prestigeCost: GameData.PRESTIGE_TIER_1 + XXLARGE_MALUS,
                upgradePath: UpgradeType.AFV,
                mobile: null,
                deployed: t55DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_TR_T55, t55RegimentProfile);

            #endregion // T-55 Tank Regiment

            #region T-62 Tank Regiment

            WeaponProfile t62DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.GEN1_TANK_HARD_ATTACK + LARGE_BONUS,
                _hardDef: GameData.GEN1_TANK_HARD_DEFENSE + MEDIUM_BONUS,
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_ARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.None,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Medium
            );

            t62DeployedProfile.IconProfile.IconType = RegimentIconType.Directional;
            t62DeployedProfile.IconProfile.W  = SpriteManager.SV_T62_W;
            t62DeployedProfile.IconProfile.NW = SpriteManager.SV_T62_NW;
            t62DeployedProfile.IconProfile.SW = SpriteManager.SV_T62_SW;

            RegimentProfile t62RegimentProfile = new RegimentProfile();
            t62RegimentProfile.Initialize(
                name: "T-62 Tank Regiment",
                profileType: RegimentProfileType.SV_TR_T62,
                turnAvailable: 336,
                prestigeCost: GameData.PRESTIGE_TIER_1,
                upgradePath: UpgradeType.AFV,
                mobile: null,
                deployed: t62DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_TR_T62, t62RegimentProfile);

            #endregion // T-62 Tank Regiment

            #region T-64A Tank Regiment

            WeaponProfile t64aDeployedProfile = new WeaponProfile(
                _hardAtt: GameData.GEN2_TANK_HARD_ATTACK + SMALL_BONUS,
                _hardDef: GameData.GEN2_TANK_HARD_DEFENSE + MEDIUM_BONUS,
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + SMALL_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_ARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.Gen1,
                _sil: UnitSilhouette.Medium
            );

            t64aDeployedProfile.IconProfile.IconType = RegimentIconType.Directional;
            t64aDeployedProfile.IconProfile.W  = SpriteManager.SV_T64A_W;
            t64aDeployedProfile.IconProfile.NW = SpriteManager.SV_T64A_NW;
            t64aDeployedProfile.IconProfile.SW = SpriteManager.SV_T64A_SW;

            RegimentProfile t64aRegimentProfile = new RegimentProfile();
            t64aRegimentProfile.Initialize(
                name: "T-64A Tank Regiment",
                profileType: RegimentProfileType.SV_TR_T64A,
                turnAvailable: 348,
                prestigeCost: GameData.PRESTIGE_TIER_2,
                upgradePath: UpgradeType.AFV,
                mobile: null,
                deployed: t64aDeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_TR_T64A, t64aRegimentProfile);

            #endregion // T-64A Tank Regiment

            #region T-64B Tank Regiment

            WeaponProfile t64bDeployedProfile = new WeaponProfile(
                _hardAtt: GameData.GEN3_TANK_HARD_ATTACK + MEDIUM_BONUS,
                _hardDef: GameData.GEN3_TANK_HARD_DEFENSE + SMALL_BONUS,
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + MEDIUM_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_ARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen2,
                _nvg: NVG_Rating.Gen2,
                _sil: UnitSilhouette.Medium
            );

            t64bDeployedProfile.IconProfile.IconType = RegimentIconType.Directional;
            t64bDeployedProfile.IconProfile.W  = SpriteManager.SV_T64B_W;
            t64bDeployedProfile.IconProfile.NW = SpriteManager.SV_T64B_NW;
            t64bDeployedProfile.IconProfile.SW = SpriteManager.SV_T64B_SW;

            RegimentProfile t64bRegimentProfile = new RegimentProfile();
            t64bRegimentProfile.Initialize(
                name: "T-64B Tank Regiment",
                profileType: RegimentProfileType.SV_TR_T64B,
                turnAvailable: 564,
                prestigeCost: GameData.PRESTIGE_TIER_3,
                upgradePath: UpgradeType.AFV,
                mobile: null,
                deployed: t64bDeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_TR_T64B, t64bRegimentProfile);

            #endregion // T-64B Tank Regiment

            #region T-72A Tank Regiment

            WeaponProfile t72aDeployedProfile = new WeaponProfile(
                _hardAtt: GameData.GEN2_TANK_HARD_ATTACK + MEDIUM_BONUS,
                _hardDef: GameData.GEN2_TANK_HARD_DEFENSE + SMALL_BONUS,
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + SMALL_MALUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_ARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.Gen1,
                _sil: UnitSilhouette.Medium
            );

            t72aDeployedProfile.IconProfile.IconType = RegimentIconType.Directional;
            t72aDeployedProfile.IconProfile.W  = SpriteManager.SV_T72A_W;
            t72aDeployedProfile.IconProfile.NW = SpriteManager.SV_T72A_NW;
            t72aDeployedProfile.IconProfile.SW = SpriteManager.SV_T72A_SW;

            RegimentProfile t72aRegimentProfile = new RegimentProfile();
            t72aRegimentProfile.Initialize(
                name: "T-72A Tank Regiment",
                profileType: RegimentProfileType.SV_TR_T72A,
                turnAvailable: 504,
                prestigeCost: GameData.PRESTIGE_TIER_2 + XXLARGE_MALUS,
                upgradePath: UpgradeType.AFV,
                mobile: null,
                deployed: t72aDeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_TR_T72A, t72aRegimentProfile);

            #endregion // T-72A Tank Regiment

            #region T-72B Tank Regiment

            WeaponProfile t72bDeployedProfile = new WeaponProfile(
                _hardAtt: GameData.GEN3_TANK_HARD_ATTACK,
                _hardDef: GameData.GEN3_TANK_HARD_DEFENSE,
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + SMALL_MALUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_ARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: true,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen2,
                _nvg: NVG_Rating.Gen2,
                _sil: UnitSilhouette.Medium
            );

            t72bDeployedProfile.IconProfile.IconType = RegimentIconType.Directional;
            t72bDeployedProfile.IconProfile.W  = SpriteManager.SV_T72B_W;
            t72bDeployedProfile.IconProfile.NW = SpriteManager.SV_T72B_NW;
            t72bDeployedProfile.IconProfile.SW = SpriteManager.SV_T72B_SW;

            RegimentProfile t72bRegimentProfile = new RegimentProfile();
            t72bRegimentProfile.Initialize(
                name: "T-72B Tank Regiment",
                profileType: RegimentProfileType.SV_TR_T72B,
                turnAvailable: 552,
                prestigeCost: GameData.PRESTIGE_TIER_3,
                upgradePath: UpgradeType.AFV,
                mobile: null,
                deployed: t72bDeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_TR_T72B, t72bRegimentProfile);

            #endregion // T-72B Tank Regiment

            #region T-80B Tank Regiment

            WeaponProfile t80bDeployedProfile = new WeaponProfile(
                _hardAtt: GameData.GEN2_TANK_HARD_ATTACK + SMALL_BONUS,
                _hardDef: GameData.GEN2_TANK_HARD_DEFENSE + MEDIUM_BONUS,
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_ARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.Gen1,
                _sil: UnitSilhouette.Medium
            );

            t80bDeployedProfile.IconProfile.IconType = RegimentIconType.Directional;
            t80bDeployedProfile.IconProfile.W  = SpriteManager.SV_T80B_W;
            t80bDeployedProfile.IconProfile.NW = SpriteManager.SV_T80B_NW;
            t80bDeployedProfile.IconProfile.SW = SpriteManager.SV_T80B_SW;

            RegimentProfile t80bRegimentProfile = new RegimentProfile();
            t80bRegimentProfile.Initialize(
                name: "T-80B Tank Regiment",
                profileType: RegimentProfileType.SV_TR_T80B,
                turnAvailable: 480,
                prestigeCost: GameData.PRESTIGE_TIER_2 + XXLARGE_BONUS,
                upgradePath: UpgradeType.AFV,
                mobile: null,
                deployed: t80bDeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_TR_T80B, t80bRegimentProfile);

            #endregion // T-80B Tank Regiment

            #region T-80U Tank Regiment

            WeaponProfile t80uDeployedProfile = new WeaponProfile(
                _hardAtt: GameData.GEN3_TANK_HARD_ATTACK + MEDIUM_BONUS,
                _hardDef: GameData.GEN3_TANK_HARD_DEFENSE + LARGE_BONUS,
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_ARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen2,
                _nvg: NVG_Rating.Gen2,
                _sil: UnitSilhouette.Medium
            );

            t80uDeployedProfile.IconProfile.IconType = RegimentIconType.Directional;
            t80uDeployedProfile.IconProfile.W  = SpriteManager.SV_T80U_W;
            t80uDeployedProfile.IconProfile.NW = SpriteManager.SV_T80U_NW;
            t80uDeployedProfile.IconProfile.SW = SpriteManager.SV_T80U_SW;

            RegimentProfile t80uRegimentProfile = new RegimentProfile();
            t80uRegimentProfile.Initialize(
                name: "T-80U Tank Regiment",
                profileType: RegimentProfileType.SV_TR_T80U,
                turnAvailable: 564,
                prestigeCost: GameData.PRESTIGE_TIER_3 + XXLARGE_BONUS,
                upgradePath: UpgradeType.AFV,
                mobile: null,
                deployed: t80uDeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_TR_T80U, t80uRegimentProfile);

            #endregion // T-80U Tank Regiment

            #region T-80BV Tank Regiment

            WeaponProfile t80bvDeployedProfile = new WeaponProfile(
                _hardAtt: GameData.GEN4_TANK_HARD_ATTACK + SMALL_BONUS,
                _hardDef: GameData.GEN4_TANK_HARD_DEFENSE,
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK + SMALL_BONUS,
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + SMALL_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_ARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen2,
                _nvg: NVG_Rating.Gen2,
                _sil: UnitSilhouette.Medium
            );

            t80bvDeployedProfile.IconProfile.IconType = RegimentIconType.Directional;
            t80bvDeployedProfile.IconProfile.W  = SpriteManager.SV_T80BVM_W;
            t80bvDeployedProfile.IconProfile.NW = SpriteManager.SV_T80BVM_NW;
            t80bvDeployedProfile.IconProfile.SW = SpriteManager.SV_T80BVM_SW;

            RegimentProfile t80bvRegimentProfile = new RegimentProfile();
            t80bvRegimentProfile.Initialize(
                name: "T-80BV Tank Regiment",
                profileType: RegimentProfileType.SV_TR_T80BV,
                turnAvailable: 615,
                prestigeCost: GameData.PRESTIGE_TIER_4,
                upgradePath: UpgradeType.AFV,
                mobile: null,
                deployed: t80bvDeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_TR_T80BV, t80bvRegimentProfile);

            #endregion // T-80BV Tank Regiment
        }

        #endregion // Soviet Tank Profiles

        #region Standard Profile Notes

        // ─────────────────────────────────────────────────────────────────────
        // Standard Soviet Truck Mobile Profile
        // Used as the Mobile stats profile for towed artillery, towed SAMs,
        // and any other regiment that limbers onto trucks for movement.
        //
        //   HardAttack:       TRUCK_HARD_ATTACK        (3)
        //   HardDefense:      TRUCK_HARD_DEFENSE       (3)
        //   SoftAttack:       TRUCK_SOFT_ATTACK        (3)
        //   SoftDefense:      TRUCK_SOFT_DEFENSE       (3)
        //   GroundAirAttack:  GROUND_AIR_ATTACK_DEFAULT (1)
        //   GroundAirDefense: GROUND_DEFENSE_INFANTRY   (6)
        //   PrimaryRange:     PRIMARY_RANGE_DEFAULT     (1)
        //   IndirectRange:    NOT_APPLICABLE            (0)
        //   SpottingRange:    BASE_UNIT_SPOTTING_RANGE  (2)
        //   MaxMovementPoints: MOT_UNIT                 (8)
        //   IsAmphibious:     false
        //   IsDoubleFire:     false
        //   IsAttackCapable:  false
        //   AllWeatherRating: GroundUnit
        //   SIGINT_Rating:    UnitLevel
        //   NBC_Rating:       None
        //   NVG_Rating:       None
        //   Silhouette:       Small
        //   IconType:         Directional (SV_Truck_W / NW / SW)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a standard Soviet truck mobile profile for towed units.
        /// </summary>
        private static WeaponProfile CreateSovietTruckMobileProfile()
        {
            WeaponProfile truckProfile = new WeaponProfile(
                _hardAtt: GameData.TRUCK_HARD_ATTACK,
                _hardDef: GameData.TRUCK_HARD_DEFENSE,
                _softAtt: GameData.TRUCK_SOFT_ATTACK,
                _softDef: GameData.TRUCK_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_INFANTRY,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.NOT_APPLICABLE,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MOT_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: false,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.None,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Small
            );

            truckProfile.IconProfile.IconType = RegimentIconType.Directional;
            truckProfile.IconProfile.W  = SpriteManager.SV_Truck_W;
            truckProfile.IconProfile.NW = SpriteManager.SV_Truck_NW;
            truckProfile.IconProfile.SW = SpriteManager.SV_Truck_SW;

            return truckProfile;
        }

        #endregion // Standard Profile Notes

        #region Soviet Artillery Profiles

        private static void CreateSovietArtilleryProfiles()
        {
            #region 2S1 Gvozdika SPA Regiment

            WeaponProfile s2s1DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK,
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.INDIRECT_RANGE_SHORT,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Medium
            );

            s2s1DeployedProfile.IconProfile.IconType = RegimentIconType.Directional_Fire;
            s2s1DeployedProfile.IconProfile.W    = SpriteManager.SV_2S1_W;
            s2s1DeployedProfile.IconProfile.NW   = SpriteManager.SV_2S1_NW;
            s2s1DeployedProfile.IconProfile.SW   = SpriteManager.SV_2S1_SW;
            s2s1DeployedProfile.IconProfile.W_F  = SpriteManager.SV_2S1_W_F;
            s2s1DeployedProfile.IconProfile.NW_F = SpriteManager.SV_2S1_NW_F;
            s2s1DeployedProfile.IconProfile.SW_F = SpriteManager.SV_2S1_SW_F;

            RegimentProfile s2s1RegimentProfile = new RegimentProfile();
            s2s1RegimentProfile.Initialize(
                name: "2S1 Gvozdika SPA Regiment",
                profileType: RegimentProfileType.SV_AR_2S1,
                turnAvailable: 408,
                prestigeCost: GameData.PRESTIGE_TIER_1,
                upgradePath: UpgradeType.SPA,
                mobile: null,
                deployed: s2s1DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_AR_2S1, s2s1RegimentProfile);

            #endregion // 2S1 Gvozdika SPA Regiment

            #region 2S3 Akatsiya SPA Regiment

            WeaponProfile s2s3DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + MEDIUM_BONUS,
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.INDIRECT_RANGE_MEDIUM,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Medium
            );

            s2s3DeployedProfile.IconProfile.IconType = RegimentIconType.Directional_Fire;
            s2s3DeployedProfile.IconProfile.W    = SpriteManager.SV_2S3_W;
            s2s3DeployedProfile.IconProfile.NW   = SpriteManager.SV_2S3_NW;
            s2s3DeployedProfile.IconProfile.SW   = SpriteManager.SV_2S3_SW;
            s2s3DeployedProfile.IconProfile.W_F  = SpriteManager.SV_2S3_W_F;
            s2s3DeployedProfile.IconProfile.NW_F = SpriteManager.SV_2S3_NW_F;
            s2s3DeployedProfile.IconProfile.SW_F = SpriteManager.SV_2S3_SW_F;

            RegimentProfile s2s3RegimentProfile = new RegimentProfile();
            s2s3RegimentProfile.Initialize(
                name: "2S3 Akatsiya SPA Regiment",
                profileType: RegimentProfileType.SV_AR_2S3,
                turnAvailable: 420,
                prestigeCost: GameData.PRESTIGE_TIER_1 + MASSIVE_BONUS,
                upgradePath: UpgradeType.SPA,
                mobile: null,
                deployed: s2s3DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_AR_2S3, s2s3RegimentProfile);

            #endregion // 2S3 Akatsiya SPA Regiment

            #region 2S5 Giatsint-S SPA Regiment

            WeaponProfile s2s5DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + MEDIUM_BONUS,
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + LARGE_BONUS,
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.INDIRECT_RANGE_LONG,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Medium
            );

            s2s5DeployedProfile.IconProfile.IconType = RegimentIconType.Directional_Fire;
            s2s5DeployedProfile.IconProfile.W    = SpriteManager.SV_2S5_W;
            s2s5DeployedProfile.IconProfile.NW   = SpriteManager.SV_2S5_NW;
            s2s5DeployedProfile.IconProfile.SW   = SpriteManager.SV_2S5_SW;
            s2s5DeployedProfile.IconProfile.W_F  = SpriteManager.SV_2S5_W_F;
            s2s5DeployedProfile.IconProfile.NW_F = SpriteManager.SV_2S5_NW_F;
            s2s5DeployedProfile.IconProfile.SW_F = SpriteManager.SV_2S5_SW_F;

            RegimentProfile s2s5RegimentProfile = new RegimentProfile();
            s2s5RegimentProfile.Initialize(
                name: "2S5 Giatsint-S SPA Regiment",
                profileType: RegimentProfileType.SV_AR_2S5,
                turnAvailable: 516,
                prestigeCost: GameData.PRESTIGE_TIER_2,
                upgradePath: UpgradeType.SPA,
                mobile: null,
                deployed: s2s5DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_AR_2S5, s2s5RegimentProfile);

            #endregion // 2S5 Giatsint-S SPA Regiment

            #region 2S19 Msta-S SPA Regiment

            WeaponProfile s2s19DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.INDIRECT_RANGE_MEDIUM,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MECH_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Medium
            );

            s2s19DeployedProfile.IconProfile.IconType = RegimentIconType.Directional_Fire;
            s2s19DeployedProfile.IconProfile.W    = SpriteManager.SV_2S19_W;
            s2s19DeployedProfile.IconProfile.NW   = SpriteManager.SV_2S19_NW;
            s2s19DeployedProfile.IconProfile.SW   = SpriteManager.SV_2S19_SW;
            s2s19DeployedProfile.IconProfile.W_F  = SpriteManager.SV_2S19_W_F;
            s2s19DeployedProfile.IconProfile.NW_F = SpriteManager.SV_2S19_NW_F;
            s2s19DeployedProfile.IconProfile.SW_F = SpriteManager.SV_2S19_SW_F;

            RegimentProfile s2s19RegimentProfile = new RegimentProfile();
            s2s19RegimentProfile.Initialize(
                name: "2S19 Msta-S SPA Regiment",
                profileType: RegimentProfileType.SV_AR_2S19,
                turnAvailable: 600,
                prestigeCost: GameData.PRESTIGE_TIER_3,
                upgradePath: UpgradeType.SPA,
                mobile: null,
                deployed: s2s19DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_AR_2S19, s2s19RegimentProfile);

            #endregion // 2S19 Msta-S SPA Regiment

            #region Heavy Towed Artillery Regiment

            WeaponProfile hvyArtDeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE,
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_INFANTRY,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.INDIRECT_RANGE_MEDIUM,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.STATIC_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Medium
            );

            hvyArtDeployedProfile.IconProfile.IconType = RegimentIconType.Single;
            hvyArtDeployedProfile.IconProfile.W = SpriteManager.SV_HeavyArt;

            RegimentProfile hvyArtRegimentProfile = new RegimentProfile();
            hvyArtRegimentProfile.Initialize(
                name: "Heavy Artillery Regiment",
                profileType: RegimentProfileType.SV_AR_HVY,
                turnAvailable: 300,
                prestigeCost: GameData.PRESTIGE_TIER_0 + MASSIVE_BONUS,
                upgradePath: UpgradeType.ART,
                mobile: CreateSovietTruckMobileProfile(),
                deployed: hvyArtDeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_AR_HVY, hvyArtRegimentProfile);

            #endregion // Heavy Towed Artillery Regiment

            #region Light Towed Artillery Regiment

            WeaponProfile lgtArtDeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE,
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK,
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_INFANTRY,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.INDIRECT_RANGE_SHORT,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.STATIC_UNIT,
                _isAmph: false,
                _isDF: false,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Medium
            );

            lgtArtDeployedProfile.IconProfile.IconType = RegimentIconType.Single;
            lgtArtDeployedProfile.IconProfile.W = SpriteManager.SV_LightArt;

            RegimentProfile lgtArtRegimentProfile = new RegimentProfile();
            lgtArtRegimentProfile.Initialize(
                name: "Light Artillery Regiment",
                profileType: RegimentProfileType.SV_AR_LGT,
                turnAvailable: 300,
                prestigeCost: GameData.PRESTIGE_TIER_0,
                upgradePath: UpgradeType.ART,
                mobile: CreateSovietTruckMobileProfile(),
                deployed: lgtArtDeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_AR_LGT, lgtArtRegimentProfile);

            #endregion // Light Towed Artillery Regiment
        }

        #endregion // Soviet Artillery Profiles

        #region Soviet Rocket Profiles

        private static void CreateSovietRocketProfiles()
        {
            #region BM-21 Grad Rocket Artillery Regiment

            WeaponProfile bm21DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK,
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.INDIRECT_RANGE_ROC_SR,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MOT_UNIT,
                _isAmph: false,
                _isDF: true,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Medium
            );

            bm21DeployedProfile.IconProfile.IconType = RegimentIconType.Directional_Fire;
            bm21DeployedProfile.IconProfile.W    = SpriteManager.SV_BM21_W;
            bm21DeployedProfile.IconProfile.NW   = SpriteManager.SV_BM21_NW;
            bm21DeployedProfile.IconProfile.SW   = SpriteManager.SV_BM21_SW;
            bm21DeployedProfile.IconProfile.W_F  = SpriteManager.SV_BM21_W_F;
            bm21DeployedProfile.IconProfile.NW_F = SpriteManager.SV_BM21_NW_F;
            bm21DeployedProfile.IconProfile.SW_F = SpriteManager.SV_BM21_SW_F;

            RegimentProfile bm21RegimentProfile = new RegimentProfile();
            bm21RegimentProfile.Initialize(
                name: "BM-21 Grad Rocket Artillery Regiment",
                profileType: RegimentProfileType.SV_ROC_BM21,
                turnAvailable: 300,
                prestigeCost: GameData.PRESTIGE_TIER_1,
                upgradePath: UpgradeType.ROC,
                mobile: null,
                deployed: bm21DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_ROC_BM21, bm21RegimentProfile);

            #endregion // BM-21 Grad Rocket Artillery Regiment

            #region BM-27 Uragan Rocket Artillery Regiment

            WeaponProfile bm27DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + LARGE_BONUS,
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.INDIRECT_RANGE_ROC_MR,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MOT_UNIT,
                _isAmph: false,
                _isDF: true,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Medium
            );

            bm27DeployedProfile.IconProfile.IconType = RegimentIconType.Directional_Fire;
            bm27DeployedProfile.IconProfile.W    = SpriteManager.SV_BM27_W;
            bm27DeployedProfile.IconProfile.NW   = SpriteManager.SV_BM27_NW;
            bm27DeployedProfile.IconProfile.SW   = SpriteManager.SV_BM27_SW;
            bm27DeployedProfile.IconProfile.W_F  = SpriteManager.SV_BM27_W_F;
            bm27DeployedProfile.IconProfile.NW_F = SpriteManager.SV_BM27_NW_F;
            bm27DeployedProfile.IconProfile.SW_F = SpriteManager.SV_BM27_SW_F;

            RegimentProfile bm27RegimentProfile = new RegimentProfile();
            bm27RegimentProfile.Initialize(
                name: "BM-27 Uragan Rocket Artillery Regiment",
                profileType: RegimentProfileType.SV_ROC_BM27,
                turnAvailable: 444,
                prestigeCost: GameData.PRESTIGE_TIER_2,
                upgradePath: UpgradeType.ROC,
                mobile: null,
                deployed: bm27DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_ROC_BM27, bm27RegimentProfile);

            #endregion // BM-27 Uragan Rocket Artillery Regiment

            #region BM-30 Smerch Rocket Artillery Regiment

            WeaponProfile bm30DeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.INDIRECT_RANGE_ROC_LR,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MOT_UNIT,
                _isAmph: false,
                _isDF: true,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Medium
            );

            bm30DeployedProfile.IconProfile.IconType = RegimentIconType.Directional_Fire;
            bm30DeployedProfile.IconProfile.W    = SpriteManager.SV_BM30_W;
            bm30DeployedProfile.IconProfile.NW   = SpriteManager.SV_BM30_NW;
            bm30DeployedProfile.IconProfile.SW   = SpriteManager.SV_BM30_SW;
            bm30DeployedProfile.IconProfile.W_F  = SpriteManager.SV_BM30_W_F;
            bm30DeployedProfile.IconProfile.NW_F = SpriteManager.SV_BM30_NW_F;
            bm30DeployedProfile.IconProfile.SW_F = SpriteManager.SV_BM30_SW_F;

            RegimentProfile bm30RegimentProfile = new RegimentProfile();
            bm30RegimentProfile.Initialize(
                name: "BM-30 Smerch Rocket Artillery Regiment",
                profileType: RegimentProfileType.SV_ROC_BM30,
                turnAvailable: 588,
                prestigeCost: GameData.PRESTIGE_TIER_3,
                upgradePath: UpgradeType.ROC,
                mobile: null,
                deployed: bm30DeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_ROC_BM30, bm30RegimentProfile);

            #endregion // BM-30 Smerch Rocket Artillery Regiment

            #region Scud-B Ballistic Missile Regiment

            WeaponProfile scudDeployedProfile = new WeaponProfile(
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + XXXLARGE_BONUS,
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,
                _df: GameData.NOT_APPLICABLE,
                _man: GameData.NOT_APPLICABLE,
                _topSpd: GameData.NOT_APPLICABLE,
                _surv: GameData.NOT_APPLICABLE,
                _ga: GameData.NOT_APPLICABLE,
                _ol: GameData.NOT_APPLICABLE,
                _stealth: GameData.NOT_APPLICABLE,
                _pr: GameData.PRIMARY_RANGE_DEFAULT,
                _ir: GameData.INDIRECT_RANGE_ROC_LR,
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,
                _mmp: GameData.MOT_UNIT,
                _isAmph: false,
                _isDF: true,
                _isAtt: true,
                _awr: AllWeatherRating.GroundUnit,
                _sir: SIGINT_Rating.UnitLevel,
                _nbc: NBC_Rating.Gen1,
                _nvg: NVG_Rating.None,
                _sil: UnitSilhouette.Large
            );

            scudDeployedProfile.IconProfile.IconType = RegimentIconType.Directional_Fire;
            scudDeployedProfile.IconProfile.W    = SpriteManager.SV_ScudB_W;
            scudDeployedProfile.IconProfile.NW   = SpriteManager.SV_ScudB_NW;
            scudDeployedProfile.IconProfile.SW   = SpriteManager.SV_ScudB_SW;
            scudDeployedProfile.IconProfile.W_F  = SpriteManager.SV_ScudB_W_F;
            scudDeployedProfile.IconProfile.NW_F = SpriteManager.SV_ScudB_NW_F;
            scudDeployedProfile.IconProfile.SW_F = SpriteManager.SV_ScudB_SW_F;

            RegimentProfile scudRegimentProfile = new RegimentProfile();
            scudRegimentProfile.Initialize(
                name: "9K72 Scud-B Ballistic Missile Regiment",
                profileType: RegimentProfileType.SV_BM_SCUDB,
                turnAvailable: 288,
                prestigeCost: GameData.PRESTIGE_TIER_4,
                upgradePath: UpgradeType.SSM,
                mobile: null,
                deployed: scudDeployedProfile,
                embarked: null
            );

            AddProfile(RegimentProfileType.SV_BM_SCUDB, scudRegimentProfile);

            #endregion // Scud-B Ballistic Missile Regiment
        }

        #endregion // Soviet Rocket Profiles

        #region Soviet Air Assault Profiles

        private static void CreateSovietAirAssaultProfiles()
        {
            // TODO: Implement Soviet Air Assault Regiment profiles
            // SV_AAR_MTLB, SV_AAR_BMD1, SV_AAR_BMD2, SV_AAR_BMD3
        }

        #endregion // Soviet Air Assault Profiles

        #region Soviet Airborne Profiles

        private static void CreateSovietAirborneProfiles()
        {
            // TODO: Implement Soviet Airborne (VDV) Regiment profiles
            // SV_VDV_BMD1, SV_VDV_BMD2, SV_VDV_BMD3, SV_VDV_ART, SV_VDV_SUP
        }

        #endregion // Soviet Airborne Profiles

        #region Soviet Naval Infantry Profiles

        private static void CreateSovietNavalInfantryProfiles()
        {
            // TODO: Implement Soviet Naval Infantry Regiment profiles
            // SV_NAV_BTR70, SV_NAV_BTR80
        }

        #endregion // Soviet Naval Infantry Profiles

        #region Soviet Recon Profiles

        private static void CreateSovietReconProfiles()
        {
            // TODO: Implement Soviet Reconnaissance profiles
            // SV_RCR, SV_RCR_AT, SV_GRU
        }

        #endregion // Soviet Recon Profiles

        #region Soviet Air Defense Profiles

        private static void CreateSovietAirDefenseProfiles()
        {
            // TODO: Implement Soviet Air Defense Regiment profiles
            // SV_ADR_AAA, SV_ADR_ZSU57, SV_ADR_ZSU23, SV_ADR_2K22
        }

        #endregion // Soviet Air Defense Profiles

        #region Soviet SAM Profiles

        private static void CreateSovietSAMProfiles()
        {
            // TODO: Implement Soviet SAM Regiment profiles
            // SV_SPSAM_9K31, SV_SAM_S75, SV_SAM_S125, SV_SAM_S300
        }

        #endregion // Soviet SAM Profiles

        #region Soviet Helicopter Profiles

        private static void CreateSovietHelicopterProfiles()
        {
            // TODO: Implement Soviet Helicopter Regiment profiles
            // SV_HEL_MI8AT, SV_HEL_MI24D, SV_HEL_MI24V, SV_HEL_MI28
        }

        #endregion // Soviet Helicopter Profiles

        #region Soviet Fighter Profiles

        private static void CreateSovietFighterProfiles()
        {
            // TODO: Implement Soviet Fighter Regiment profiles
            // SV_FR_MIG21, SV_FR_MIG23, SV_FR_MIG25, SV_FR_MIG29, SV_FR_MIG31, SV_FR_SU27, SV_FR_SU47
        }

        #endregion // Soviet Fighter Profiles

        #region Soviet Attack Aviation Profiles

        private static void CreateSovietAttackAviationProfiles()
        {
            // TODO: Implement Soviet Attack Aviation Regiment profiles
            // SV_MR_MIG27, SV_AR_SU25, SV_AR_SU25B
        }

        #endregion // Soviet Attack Aviation Profiles

        #region Soviet Bomber Profiles

        private static void CreateSovietBomberProfiles()
        {
            // TODO: Implement Soviet Bomber Regiment profiles
            // SV_BR_SU24, SV_BR_TU16, SV_BR_TU22, SV_BR_TU22M3
        }

        #endregion // Soviet Bomber Profiles

        #region Soviet Recon Aviation Profiles

        private static void CreateSovietReconAviationProfiles()
        {
            // TODO: Implement Soviet Reconnaissance Aviation profiles
            // SV_RR_MIG25R, SV_AWACS_A50
        }

        #endregion // Soviet Recon Aviation Profiles

        #region Soviet Facility Profiles

        private static void CreateSovietFacilityProfiles()
        {
            // TODO: Implement Soviet Facility profiles
            // SV_BASE, SV_AIRB, SV_DEPOT
        }

        #endregion // Soviet Facility Profiles

        #region Mujahideen Profiles

        private static void CreateMujahideenProfiles()
        {
            // TODO: Implement Mujahideen profiles
            // MJ_INF_GUERRILLA, MJ_SPEC_COMMANDO, MJ_CAV_HORSE, MJ_AA, MJ_ART_LIGHT_MORTAR, MJ_ART_HEAVY_MORTAR
        }

        #endregion // Mujahideen Profiles

        #region US Armored Profiles

        private static void CreateUSArmoredProfiles()
        {
            // TODO: Implement US Armored Brigade profiles
            // US_ARMORED_BDE_M1, US_ARMORED_BDE_M60A3
        }

        #endregion // US Armored Profiles

        #region US Mechanized Profiles

        private static void CreateUSMechanizedProfiles()
        {
            // TODO: Implement US Mechanized Brigade profiles
            // US_HEAVY_MECH_BDE_M1, US_HEAVY_MECH_BDE_M60A3
        }

        #endregion // US Mechanized Profiles

        #region US Airborne Profiles

        private static void CreateUSAirborneProfiles()
        {
            // TODO: Implement US Airborne/Air Assault Brigade profiles
            // US_PARA_BDE_82ND, US_AIR_ASSAULT_BDE_101ST
        }

        #endregion // US Airborne Profiles

        #region US Aviation Profiles

        private static void CreateUSAviationProfiles()
        {
            // TODO: Implement US Aviation profiles
            // US_AVIATION_ATTACK_BDE, US_ARMORED_CAV_SQDN
        }

        #endregion // US Aviation Profiles

        #region US Artillery Profiles

        private static void CreateUSArtilleryProfiles()
        {
            // TODO: Implement US Artillery profiles
            // US_ARTILLERY_BDE_M109, US_ARTILLERY_BDE_MLRS, US_ENGINEER_BDE
        }

        #endregion // US Artillery Profiles

        #region US Air Defense Profiles

        private static void CreateUSAirDefenseProfiles()
        {
            // TODO: Implement US Air Defense profiles
            // US_AIR_DEFENSE_BDE_HAWK, US_AIR_DEFENSE_BDE_CHAPARRAL
        }

        #endregion // US Air Defense Profiles

        #region US Fighter Profiles

        private static void CreateUSFighterProfiles()
        {
            // TODO: Implement US Fighter Wing profiles
            // US_FIGHTER_WING_F15, US_FIGHTER_WING_F4, US_FIGHTER_WING_F16
        }

        #endregion // US Fighter Profiles

        #region US Attack Aviation Profiles

        private static void CreateUSAttackAviationProfiles()
        {
            // TODO: Implement US Attack Aviation profiles
            // US_TACTICAL_WING_A10
        }

        #endregion // US Attack Aviation Profiles

        #region US Bomber Profiles

        private static void CreateUSBomberProfiles()
        {
            // TODO: Implement US Bomber/Recon profiles
            // US_BOMBER_WING_F111, US_BOMBER_WING_F117, US_RECON_SQDN_SR71, US_AWACS_E3
        }

        #endregion // US Bomber Profiles

        #region FRG Profiles

        private static void CreateFRGProfiles()
        {
            // TODO: Implement West Germany (FRG) profiles
            // FRG_PANZER_BDE_LEO2, FRG_PANZER_BDE_LEO1, FRG_PZGREN_BDE_MARDER, etc.
        }

        #endregion // FRG Profiles

        #region UK Profiles

        private static void CreateUKProfiles()
        {
            // TODO: Implement UK profiles
            // UK_ARMOURED_BDE_CHALLENGER, UK_MECHANISED_BDE_WARRIOR, etc.
        }

        #endregion // UK Profiles

        #region France Profiles

        private static void CreateFranceProfiles()
        {
            // TODO: Implement France profiles
            // FR_BRIGADE_BLINDEE_AMX30, FR_BRIGADE_INF_MECA_AMX10P, etc.
        }

        #endregion // France Profiles

        #region Arab Profiles

        private static void CreateArabProfiles()
        {
            // TODO: Implement Arab nation profiles
            // ARAB_TANK_REG_T55, ARAB_TANK_REG_T72, ARAB_MECH_REG_BMP1, etc.
        }

        #endregion // Arab Profiles
    }
}
