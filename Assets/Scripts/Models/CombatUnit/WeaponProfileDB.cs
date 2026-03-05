// =============================================================================
// WeaponProfileDB.cs
// =============================================================================
//
// PURPOSE:
//   Static database containing all weapon/regiment profiles used in the game.
//   Each profile defines the complete combat characteristics, intel report data,
//   and icon configuration for a specific weapon system or unit type.
//
// STRUCTURE:
//   - Constants (lines ~11-30): Combat rating modifier constants (MALUS/BONUS)
//   - Private Fields (lines ~32-37): Dictionary storage and init flag
//   - Public Properties (lines ~39-51): IsInitialized, ProfileCount
//   - Public Methods (lines ~53-140): InitializeRegimentProfile(), GetWeaponProfile(), HasWeaponProfile()
//   - Private Methods (lines ~142-192): CreateAllWeaponProfiles(), AddProfile()
//   - Profile Definitions (lines ~197-10048): All 173 weapon profiles
//
// PROFILE SECTIONS:
//   CreateSovietProfiles()  (line ~197)  - Soviet equipment
//     Tanks, IFVs/APCs, Recon, SP Artillery, Artillery, Rocket Artillery,
//     Air Defense, Helicopters, Jets, Trucks/Naval, Infantry
//
//   CreateGenericProfiles() (line ~4170) - Generic/shared equipment (Bases)
//
//   CreateWesternProfiles() (line ~4321) - Western/NATO equipment
//     MBTs, IFVs/APCs, SP Artillery, Artillery, Rocket Artillery,
//     Air Defense, Recon, Helicopters, Jets, Trucks, Infantry
//
//   CreateArabProfiles()    (line ~7779) - Arab nation equipment
//     MBTs, IFVs/APCs, Artillery, Air Defense, Jets, Trucks, Mujahideen Infantry
//
//   CreateChineseProfiles() (line ~9055) - Chinese equipment
//     MBTs, IFV, Artillery, Air Defense, Helicopters, Jets, Infantry
//
// PROFILE ANATOMY:
//   Each profile is a WeaponProfile instance configured with:
//     - Names (long/short) for UI display
//     - WeaponType enum identifier
//     - Combat stats: hard/soft attack/defense, ground-air, dogfighting, etc.
//     - Range stats: primary, indirect, spotting
//     - Movement points and amphibious capability
//     - Ratings: AllWeather, SIGINT, NBC, NVG
//     - UnitSilhouette, UpgradePath, turnAvailable
//     - Prestige cost (tier + type)
//     - Intel report stats (equipment quantities via AddIntelReportStat)
//     - Icon profile (RegimentIconProfile + sprite assignments)
//     - Optional combat rating modifiers (via AddCombatRatingModifier)
//
// USAGE:
//   Call WeaponProfileDB.InitializeRegimentProfile() at startup.
//   Retrieve profiles via WeaponProfileDB.GetWeaponProfile(WeaponType.TANK_T55A_SV).
//
// SIZE: ~10,048 lines
//
// To AI Agents: only read beyond this block unless explicitly told to do so.
// =============================================================================

using System;
using System.Collections.Generic;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    public static class WeaponProfileDB
    {
        #region Constants

        private const string CLASS_NAME = nameof(WeaponProfileDB);

        // Centralized place for combat rating modifiers
        //private const int MASSIVE_MALUS = -10;
        //private const int XXLARGE_MALUS = -5;
        private const int XLARGE_MALUS = -4;
        private const int LARGE_MALUS = -3;
        private const int MEDIUM_MALUS = -2;
        private const int SMALL_MALUS = -1;
        private const int SMALL_BONUS = 1;
        private const int MEDIUM_BONUS = 2;
        private const int LARGE_BONUS = 3;
        private const int XLARGE_BONUS = 4;
        private const int XXLARGE_BONUS = 5;
        private const int XXXLARGE_BONUS = 6;
        //private const int MASSIVE_BONUS = 10;

        #endregion // Constants

        #region Private Fields

        private static Dictionary<WeaponType, WeaponProfile> _weaponProfiles;
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
        public static int ProfileCount => _weaponProfiles?.Count ?? 0;

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
                    AppService.CaptureUiMessage("WeaponProfileDB already initialized, skipping");
                    return;
                }

                _weaponProfiles = new Dictionary<WeaponType, WeaponProfile>();

                CreateAllWeaponProfiles();

                _isInitialized = true;
                AppService.CaptureUiMessage($"WeaponProfileDB initialized with {ProfileCount} profiles");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Initialize), e);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a weapon profile by its enum identifier.
        /// </summary>
        /// <param name="profileType">The weapon profile type to look up</param>
        /// <returns>The corresponding WeaponProfile, or null if not found</returns>
        public static WeaponProfile GetWeaponProfile(WeaponType profileType)
        {
            try
            {
                if (!_isInitialized)
                {
                    AppService.CaptureUiMessage("WeaponProfileDB not initialized - call Initialize() first");
                    return null;
                }

                if (profileType == WeaponType.NONE)
                {
                    return null;
                }

                return _weaponProfiles.TryGetValue(profileType, out var profile) ? profile : null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetWeaponProfile), e);
                return null;
            }
        }

        /// <summary>
        /// Checks if a weapon profile exists in the database by its enum identifier.
        /// </summary>
        /// <param name="profileType">The weapon profile type to check</param>
        /// <returns>True if the profile exists, false otherwise</returns>
        public static bool HasWeaponProfile(WeaponType profileType)
        {
            try
            {
                if (!_isInitialized)
                {
                    return false;
                }

                if (profileType == WeaponType.NONE)
                {
                    return false;
                }

                return _weaponProfiles.ContainsKey(profileType);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HasWeaponProfile), e);
                return false;
            }
        }

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Creates all weapon profiles used in the game.
        /// This method contains the complete database of weapon configurations.
        /// </summary>
        private static void CreateAllWeaponProfiles()
        {
            try
            {
                CreateSovietProfiles();
                CreateGenericProfiles();
                CreateWesternProfiles();
                CreateArabProfiles();
                CreateChineseProfiles();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAllWeaponProfiles), e);
                throw;
            }
        }

        /// <summary>
        /// Adds a weapon profile to the database with validation.
        /// </summary>
        /// <param name="profileType">The profile type identifier</param>
        /// <param name="profile">The weapon profile to add</param>
        private static void AddProfile(WeaponType profileType, WeaponProfile profile)
        {
            try
            {
                if (profileType == WeaponType.NONE)
                    throw new ArgumentException("Cannot add NONE profile type", nameof(profileType));

                if (profile == null)
                    throw new ArgumentNullException(nameof(profile));

                if (_weaponProfiles.ContainsKey(profileType))
                    throw new InvalidOperationException($"Profile type '{profileType}' already exists in database");

                _weaponProfiles[profileType] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AddProfile), e);
                throw;
            }
        }

        #endregion // Private Methods

        /// <summary>
        /// Create Soviet WeaponProfiles
        /// </summary>
        private static void CreateSovietProfiles()
        {
            #region Tanks

            //----------------------------------------------
            // Soviet T-55A Main Battle Tank
            //----------------------------------------------
            WeaponProfile T55A = new WeaponProfile(
                _longName: "T-55A Main Battle Tank",       // Full name for UI display and intel reports
                _shortName: "T-55A",                       // Short name for UI display and intel reports
                _type: WeaponType.TANK_T55A_SV,            // Enum identifier for this profile
                _hardAtt: GameData.GEN1_TANK_HARD_ATTACK,  // Hard Attack Rating
                _hardDef: GameData.GEN1_TANK_HARD_DEFENSE, // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE, // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 240                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            T55A.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.TANK);

            // Fill out intel stats for the T-55A profile
            T55A.AddIntelReportStat(WeaponType.Personnel,      1143);
            T55A.AddIntelReportStat(WeaponType.TANK_T55A_SV,     94);
            T55A.AddIntelReportStat(WeaponType.IFV_BMP1_SV,      45);
            T55A.AddIntelReportStat(WeaponType.APC_BTR70_SV,     21);
            T55A.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            T55A.AddIntelReportStat(WeaponType.SPAAA_ZSU57_SV,    4);
            T55A.AddIntelReportStat(WeaponType.SPSAM_2K12_SV,     4);
            T55A.AddIntelReportStat(WeaponType.SPA_2S1_SV,       18);
            T55A.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  12);
            T55A.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 12);
            T55A.AddIntelReportStat(WeaponType.AT_ATGM,          12);
            T55A.AddIntelReportStat(WeaponType.MANPAD_STRELA,    12);

            // Handle the icon profile.
            T55A.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_T55A_W,
                NW = SpriteManager.SV_T55A_NW,
                SW = SpriteManager.SV_T55A_SW
            };


            // Add the T-55A profile to the database
            AddProfile(WeaponType.TANK_T55A_SV, T55A);
            //----------------------------------------------
            // Soviet T-55A Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // Soviet T-62A Main Battle Tank
            //----------------------------------------------
            WeaponProfile T62A = new WeaponProfile(
                _longName: "T-62A Main Battle Tank",       // Full name for UI display and intel reports
                _shortName: "T-62A",                       // Short name for UI display and intel reports
                _type: WeaponType.TANK_T62A_SV,            // Enum identifier for this profile
                _hardAtt: GameData.GEN1_TANK_HARD_ATTACK + SMALL_BONUS,  // Hard Attack Rating
                _hardDef: GameData.GEN1_TANK_HARD_DEFENSE + SMALL_BONUS, // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 276                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            T62A.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.TANK);

            // Fill out intel stats for the T-62A profile
            T62A.AddIntelReportStat(WeaponType.Personnel,      1143);
            T62A.AddIntelReportStat(WeaponType.TANK_T62A_SV,     94);
            T62A.AddIntelReportStat(WeaponType.IFV_BMP1_SV,      45);
            T62A.AddIntelReportStat(WeaponType.APC_BTR70_SV,     21);
            T62A.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            T62A.AddIntelReportStat(WeaponType.SPAAA_ZSU57_SV,    4);
            T62A.AddIntelReportStat(WeaponType.SPSAM_2K12_SV,     4);
            T62A.AddIntelReportStat(WeaponType.SPA_2S1_SV,       18);
            T62A.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  12);
            T62A.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 12);
            T62A.AddIntelReportStat(WeaponType.AT_ATGM,          12);
            T62A.AddIntelReportStat(WeaponType.MANPAD_STRELA,       12);

            // Handle the icon profile.
            T62A.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_T62_W,
                NW = SpriteManager.SV_T62_NW,
                SW = SpriteManager.SV_T62_SW
            };

            // Add the T-62A profile to the database
            AddProfile(WeaponType.TANK_T62A_SV, T62A);
            //----------------------------------------------
            // Soviet T-62A Main Battle Tank
            //----------------------------------------------
            
            //----------------------------------------------
            // Soviet T-64A Main Battle Tank
            //----------------------------------------------
            WeaponProfile T64A = new WeaponProfile(
                _longName: "T-64A Main Battle Tank",       // Full name for UI display and intel reports
                _shortName: "T-64A",                       // Short name for UI display and intel reports
                _type: WeaponType.TANK_T64A_SV,            // Enum identifier for this profile
                _hardAtt: GameData.GEN2_TANK_HARD_ATTACK + SMALL_BONUS,   // Hard Attack Rating
                _hardDef: GameData.GEN2_TANK_HARD_DEFENSE + MEDIUM_BONUS, // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                 // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + SMALL_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Small,                // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 336                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            T64A.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.TANK);

            // Fill out intel stats for the T-64A profile
            T64A.AddIntelReportStat(WeaponType.Personnel,      1143);
            T64A.AddIntelReportStat(WeaponType.TANK_T64A_SV,     94);
            T64A.AddIntelReportStat(WeaponType.IFV_BMP2_SV,      45);
            T64A.AddIntelReportStat(WeaponType.APC_BTR70_SV,     21);
            T64A.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            T64A.AddIntelReportStat(WeaponType.SPAAA_ZSU57_SV,    4);
            T64A.AddIntelReportStat(WeaponType.SPSAM_9K31_SV,     4);
            T64A.AddIntelReportStat(WeaponType.SPA_2S1_SV,       18);
            T64A.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  12);
            T64A.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 12);
            T64A.AddIntelReportStat(WeaponType.AT_ATGM,          12);
            T64A.AddIntelReportStat(WeaponType.MANPAD_STRELA,       12);

            // Handle the icon profile.
            T64A.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_T64A_W,
                NW = SpriteManager.SV_T64A_NW,
                SW = SpriteManager.SV_T64A_SW
            };

            // Add the T-64A profile to the database
            AddProfile(WeaponType.TANK_T64A_SV, T64A);
            //----------------------------------------------
            // Soviet T-64A Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // Soviet T-64B Main Battle Tank
            //----------------------------------------------
            WeaponProfile T64B = new WeaponProfile(
                _longName: "T-64B Main Battle Tank",       // Full name for UI display and intel reports
                _shortName: "T-64B",                       // Short name for UI display and intel reports
                _type: WeaponType.TANK_T64B_SV,            // Enum identifier for this profile
                _hardAtt: GameData.GEN3_TANK_HARD_ATTACK + MEDIUM_BONUS,  // Hard Attack Rating
                _hardDef: GameData.GEN3_TANK_HARD_DEFENSE + SMALL_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                 // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + MEDIUM_BONUS, // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Small,                // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 456                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            T64B.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.TANK);

            // Fill out intel stats for the T-64B profile
            T64B.AddIntelReportStat(WeaponType.Personnel,      1143);
            T64B.AddIntelReportStat(WeaponType.TANK_T64B_SV,     94);
            T64B.AddIntelReportStat(WeaponType.IFV_BMP2_SV,      45);
            T64B.AddIntelReportStat(WeaponType.APC_BTR80_SV,     21);
            T64B.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            T64B.AddIntelReportStat(WeaponType.SPAAA_ZSU23_SV,    4);
            T64B.AddIntelReportStat(WeaponType.SPSAM_9K31_SV,     4);
            T64B.AddIntelReportStat(WeaponType.SPA_2S1_SV,       18);
            T64B.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  12);
            T64B.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 12);
            T64B.AddIntelReportStat(WeaponType.AT_ATGM,          12);
            T64B.AddIntelReportStat(WeaponType.MANPAD_STRELA,       12);

            // Handle the icon profile.
            T64B.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_T64B_W,
                NW = SpriteManager.SV_T64B_NW,
                SW = SpriteManager.SV_T64B_SW
            };

            // Add the T-64B profile to the database
            AddProfile(WeaponType.TANK_T64B_SV, T64B);
            //----------------------------------------------
            // Soviet T-64B Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // Soviet T-72A Main Battle Tank
            //----------------------------------------------
            WeaponProfile T72A = new WeaponProfile(
                _longName: "T-72A Main Battle Tank",       // Full name for UI display and intel reports
                _shortName: "T-72A",                       // Short name for UI display and intel reports
                _type: WeaponType.TANK_T72A_SV,            // Enum identifier for this profile
                _hardAtt: GameData.GEN2_TANK_HARD_ATTACK + MEDIUM_BONUS,  // Hard Attack Rating
                _hardDef: GameData.GEN2_TANK_HARD_DEFENSE + SMALL_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                 // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + SMALL_MALUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: true,                             // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 420                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            T72A.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.TANK);

            // Fill out intel stats for the T-72A profile
            T72A.AddIntelReportStat(WeaponType.Personnel,      1143);
            T72A.AddIntelReportStat(WeaponType.TANK_T72A_SV,     94);
            T72A.AddIntelReportStat(WeaponType.IFV_BMP1_SV,      45);
            T72A.AddIntelReportStat(WeaponType.APC_BTR70_SV,     21);
            T72A.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            T72A.AddIntelReportStat(WeaponType.SPAAA_ZSU57_SV,    4);
            T72A.AddIntelReportStat(WeaponType.SPSAM_9K31_SV,     4);
            T72A.AddIntelReportStat(WeaponType.SPA_2S1_SV,       18);
            T72A.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  12);
            T72A.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 12);
            T72A.AddIntelReportStat(WeaponType.AT_ATGM,          12);
            T72A.AddIntelReportStat(WeaponType.MANPAD_STRELA,       12);

            // Handle the icon profile.
            T72A.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_T72A_W,
                NW = SpriteManager.SV_T72A_NW,
                SW = SpriteManager.SV_T72A_SW
            };

            // Add the T-72A profile to the database
            AddProfile(WeaponType.TANK_T72A_SV, T72A);
            //----------------------------------------------
            // Soviet T-72A Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // Soviet T-72B Main Battle Tank
            //----------------------------------------------
            WeaponProfile T72B = new WeaponProfile(
                _longName: "T-72B Main Battle Tank",       // Full name for UI display and intel reports
                _shortName: "T-72B",                       // Short name for UI display and intel reports
                _type: WeaponType.TANK_T72B_SV,            // Enum identifier for this profile
                _hardAtt: GameData.GEN3_TANK_HARD_ATTACK,  // Hard Attack Rating
                _hardDef: GameData.GEN3_TANK_HARD_DEFENSE, // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                 // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + SMALL_MALUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: true,                             // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 564                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            T72B.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.TANK);

            // Fill out intel stats for the T-72B profile
            T72B.AddIntelReportStat(WeaponType.Personnel,      1143);
            T72B.AddIntelReportStat(WeaponType.TANK_T72B_SV,     94);
            T72B.AddIntelReportStat(WeaponType.IFV_BMP2_SV,      45);
            T72B.AddIntelReportStat(WeaponType.APC_BTR80_SV,     21);
            T72B.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            T72B.AddIntelReportStat(WeaponType.SPSAM_2K22_SV,     4);
            T72B.AddIntelReportStat(WeaponType.SPSAM_9K31_SV,     4);
            T72B.AddIntelReportStat(WeaponType.SPA_2S1_SV,       18);
            T72B.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  12);
            T72B.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 12);
            T72B.AddIntelReportStat(WeaponType.AT_ATGM,          12);
            T72B.AddIntelReportStat(WeaponType.MANPAD_STRELA,       12);

            // Handle the icon profile.
            T72B.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_T72B_W,
                NW = SpriteManager.SV_T72B_NW,
                SW = SpriteManager.SV_T72B_SW
            };

            // Add the T-72B profile to the database
            AddProfile(WeaponType.TANK_T72B_SV, T72B);
            //----------------------------------------------
            // Soviet T-72B Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // Soviet T-80B Main Battle Tank
            //----------------------------------------------
            WeaponProfile T80B = new WeaponProfile(
                _longName: "T-80B Main Battle Tank",       // Full name for UI display and intel reports
                _shortName: "T-80B",                       // Short name for UI display and intel reports
                _type: WeaponType.TANK_T80B_SV,            // Enum identifier for this profile
                _hardAtt: GameData.GEN2_TANK_HARD_ATTACK + SMALL_BONUS,   // Hard Attack Rating
                _hardDef: GameData.GEN2_TANK_HARD_DEFENSE + MEDIUM_BONUS, // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE, // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 480                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            T80B.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.TANK);

            // Fill out intel stats for the T-80B profile
            T80B.AddIntelReportStat(WeaponType.Personnel,      1143);
            T80B.AddIntelReportStat(WeaponType.TANK_T80B_SV,     94);
            T80B.AddIntelReportStat(WeaponType.IFV_BMP2_SV,      45);
            T80B.AddIntelReportStat(WeaponType.APC_BTR80_SV,     21);
            T80B.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            T80B.AddIntelReportStat(WeaponType.SPAAA_ZSU23_SV,    4);
            T80B.AddIntelReportStat(WeaponType.SPSAM_9K31_SV,     4);
            T80B.AddIntelReportStat(WeaponType.SPA_2S1_SV,       18);
            T80B.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  12);
            T80B.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 12);
            T80B.AddIntelReportStat(WeaponType.AT_ATGM,          12);
            T80B.AddIntelReportStat(WeaponType.MANPAD_STRELA,       12);

            // Handle the icon profile.
            T80B.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_T80B_W,
                NW = SpriteManager.SV_T80B_NW,
                SW = SpriteManager.SV_T80B_SW
            };

            // Add the T-80B profile to the database
            AddProfile(WeaponType.TANK_T80B_SV, T80B);
            //----------------------------------------------
            // Soviet T-80B Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // Soviet T-80U Main Battle Tank
            //----------------------------------------------
            WeaponProfile T80U = new WeaponProfile(
                _longName: "T-80U Main Battle Tank",       // Full name for UI display and intel reports
                _shortName: "T-80U",                       // Short name for UI display and intel reports
                _type: WeaponType.TANK_T80U_SV,            // Enum identifier for this profile
                _hardAtt: GameData.GEN3_TANK_HARD_ATTACK + MEDIUM_BONUS, // Hard Attack Rating
                _hardDef: GameData.GEN3_TANK_HARD_DEFENSE + LARGE_BONUS, // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE, // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 564                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            T80U.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.TANK);

            // Fill out intel stats for the T-80U profile
            T80U.AddIntelReportStat(WeaponType.Personnel,      1143);
            T80U.AddIntelReportStat(WeaponType.TANK_T80U_SV,     94);
            T80U.AddIntelReportStat(WeaponType.IFV_BMP2_SV,      45);
            T80U.AddIntelReportStat(WeaponType.APC_BTR80_SV,     21);
            T80U.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            T80U.AddIntelReportStat(WeaponType.SPSAM_2K22_SV,     4);
            T80U.AddIntelReportStat(WeaponType.SPSAM_9K31_SV,     4);
            T80U.AddIntelReportStat(WeaponType.SPA_2S1_SV,       18);
            T80U.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  12);
            T80U.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 12);
            T80U.AddIntelReportStat(WeaponType.AT_ATGM,          12);
            T80U.AddIntelReportStat(WeaponType.MANPAD_STRELA,       12);

            // Handle the icon profile.
            T80U.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_T80U_W,
                NW = SpriteManager.SV_T80U_NW,
                SW = SpriteManager.SV_T80U_SW
            };

            // Add the T-80U profile to the database
            AddProfile(WeaponType.TANK_T80U_SV, T80U);
            //----------------------------------------------
            // Soviet T-80U Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // Soviet T-80BV Main Battle Tank
            //----------------------------------------------
            WeaponProfile T80BV = new WeaponProfile(
                _longName: "T-80BV Main Battle Tank",      // Full name for UI display and intel reports
                _shortName: "T-80BV",                      // Short name for UI display and intel reports
                _type: WeaponType.TANK_T80BV_SV,           // Enum identifier for this profile
                _hardAtt: GameData.GEN4_TANK_HARD_ATTACK + SMALL_BONUS,   // Hard Attack Rating
                _hardDef: GameData.GEN4_TANK_HARD_DEFENSE,                // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK + SMALL_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + SMALL_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 584                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            T80BV.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.TANK);

            // Fill out intel stats for the T-80BV profile
            T80BV.AddIntelReportStat(WeaponType.Personnel,      1143);
            T80BV.AddIntelReportStat(WeaponType.TANK_T80BV_SV,    94);
            T80BV.AddIntelReportStat(WeaponType.IFV_BMP3_SV,      45);
            T80BV.AddIntelReportStat(WeaponType.APC_BTR80_SV,     21);
            T80BV.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            T80BV.AddIntelReportStat(WeaponType.SPSAM_2K22_SV,     4);
            T80BV.AddIntelReportStat(WeaponType.SPSAM_9K31_SV,     4);
            T80BV.AddIntelReportStat(WeaponType.SPA_2S1_SV,       18);
            T80BV.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  12);
            T80BV.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 12);
            T80BV.AddIntelReportStat(WeaponType.AT_ATGM,          12);
            T80BV.AddIntelReportStat(WeaponType.MANPAD_STRELA,       12);

            // Handle the icon profile.
            T80BV.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_T80BVM_W,
                NW = SpriteManager.SV_T80BVM_NW,
                SW = SpriteManager.SV_T80BVM_SW
            };

            // Add the T-80BV profile to the database
            AddProfile(WeaponType.TANK_T80BV_SV, T80BV);
            //----------------------------------------------
            // Soviet T-80BV Main Battle Tank
            //----------------------------------------------

            #endregion // Tanks

            #region IFVs and APCs

            //----------------------------------------------
            // Soviet BMP-1P Infantry Fighting Vehicle
            //----------------------------------------------
            WeaponProfile BMP1 = new WeaponProfile(
                _longName: "BMP-1P Infantry Fighting Vehicle",  // Full name for UI display and intel reports
                _shortName: "BMP-1P",                           // Short name for UI display and intel reports
                _type: WeaponType.IFV_BMP1_SV,                 // Enum identifier for this profile
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK + XXLARGE_BONUS,  // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK,       // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,      // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: true,                                 // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.IFV,                 // Upgrade Path
                _turnAvailable: 336                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BMP1.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.IFV);

            // Fill out intel stats for the BMP-1P profile
            BMP1.AddIntelReportStat(WeaponType.TANK_T55A_SV,     40);
            BMP1.AddIntelReportStat(WeaponType.IFV_BMP1_SV,     129);
            BMP1.AddIntelReportStat(WeaponType.APC_BTR70_SV,     26);
            BMP1.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);

            // Handle the icon profile.
            BMP1.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_BMP1_W,
                NW = SpriteManager.SV_BMP1_NW,
                SW = SpriteManager.SV_BMP1_SW
            };

            // Add the BMP-1P profile to the database
            AddProfile(WeaponType.IFV_BMP1_SV, BMP1);
            //----------------------------------------------
            // Soviet BMP-1P Infantry Fighting Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // Soviet BMP-2 Infantry Fighting Vehicle
            //----------------------------------------------
            WeaponProfile BMP2 = new WeaponProfile(
                _longName: "BMP-2 Infantry Fighting Vehicle",  // Full name for UI display and intel reports
                _shortName: "BMP-2",                           // Short name for UI display and intel reports
                _type: WeaponType.IFV_BMP2_SV,                // Enum identifier for this profile
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS, // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK,       // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE + SMALL_BONUS,   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: true,                                 // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen2,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.IFV,                 // Upgrade Path
                _turnAvailable: 504                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BMP2.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.IFV);

            // Fill out intel stats for the BMP-2 profile
            BMP2.AddIntelReportStat(WeaponType.TANK_T72A_SV,     40);
            BMP2.AddIntelReportStat(WeaponType.IFV_BMP2_SV,     129);
            BMP2.AddIntelReportStat(WeaponType.APC_BTR70_SV,     26);
            BMP2.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);

            // Handle the icon profile.
            BMP2.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_BMP2_W,
                NW = SpriteManager.SV_BMP2_NW,
                SW = SpriteManager.SV_BMP2_SW
            };

            // Add the BMP-2 profile to the database
            AddProfile(WeaponType.IFV_BMP2_SV, BMP2);
            //----------------------------------------------
            // Soviet BMP-2 Infantry Fighting Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // Soviet BMP-3 Infantry Fighting Vehicle
            //----------------------------------------------
            WeaponProfile BMP3 = new WeaponProfile(
                _longName: "BMP-3 Infantry Fighting Vehicle",  // Full name for UI display and intel reports
                _shortName: "BMP-3",                           // Short name for UI display and intel reports
                _type: WeaponType.IFV_BMP3_SV,                // Enum identifier for this profile
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS,  // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK + SMALL_BONUS,    // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: true,                                 // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                         // NBC Rating
                _nvg: NVG_Rating.Gen2,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.IFV,                 // Upgrade Path
                _turnAvailable: 588                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BMP3.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.IFV);

            // Fill out intel stats for the BMP-3 profile
            BMP3.AddIntelReportStat(WeaponType.TANK_T80B_SV,     40);
            BMP3.AddIntelReportStat(WeaponType.IFV_BMP3_SV,     129);
            BMP3.AddIntelReportStat(WeaponType.APC_BTR80_SV,     26);
            BMP3.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);

            // Handle the icon profile.
            BMP3.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_BMP3_W,
                NW = SpriteManager.SV_BMP3_NW,
                SW = SpriteManager.SV_BMP3_SW
            };

            // Add the BMP-3 profile to the database
            AddProfile(WeaponType.IFV_BMP3_SV, BMP3);
            //----------------------------------------------
            // Soviet BMP-3 Infantry Fighting Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // Soviet BMD-2 Airborne Infantry Fighting Vehicle
            //----------------------------------------------
            // Note- should only be a transport for airborne infantry.
            WeaponProfile BMD2 = new WeaponProfile(
                _longName: "BMD-2 Airborne IFV",               // Full name for UI display and intel reports
                _shortName: "BMD-2",                            // Short name for UI display and intel reports
                _type: WeaponType.IFV_BMD2_SV,                 // Enum identifier for this profile
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK,       // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK + SMALL_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,      // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: true,                                 // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.IFV,                 // Upgrade Path
                _turnAvailable: 564                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BMD2.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.IFV);

            // Fill out intel stats for the BMD-2 profile
            BMD2.AddIntelReportStat(WeaponType.IFV_BMD2_SV,      68);
            BMD2.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     13);

            // Handle the icon profile.
            BMD2.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_BMD2_W,
                NW = SpriteManager.SV_BMD2_NW,
                SW = SpriteManager.SV_BMD2_SW
            };

            // Add the BMD-2 profile to the database
            AddProfile(WeaponType.IFV_BMD2_SV, BMD2);
            //----------------------------------------------
            // Soviet BMD-2 Airborne Infantry Fighting Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // Soviet BMD-3 Airborne Infantry Fighting Vehicle
            //----------------------------------------------
            // Note- should only be a transport for airborne infantry.
            WeaponProfile BMD3 = new WeaponProfile(
                _longName: "BMD-3 Airborne IFV",               // Full name for UI display and intel reports
                _shortName: "BMD-3",                            // Short name for UI display and intel reports
                _type: WeaponType.IFV_BMD3_SV,                 // Enum identifier for this profile
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK,       // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK + MEDIUM_BONUS,  // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,      // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: true,                                 // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                         // NBC Rating
                _nvg: NVG_Rating.Gen2,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.IFV,                 // Upgrade Path
                _turnAvailable: 624                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BMD3.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.IFV);

            // Fill out intel stats for the BMD-3 profile
            BMD3.AddIntelReportStat(WeaponType.IFV_BMD3_SV,      68);
            BMD3.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     13);

            // Handle the icon profile.
            BMD3.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_BMD3_W,
                NW = SpriteManager.SV_BMD3_NW,
                SW = SpriteManager.SV_BMD3_SW
            };

            // Add the BMD-3 profile to the database
            AddProfile(WeaponType.IFV_BMD3_SV, BMD3);
            //----------------------------------------------
            // Soviet BMD-3 Airborne Infantry Fighting Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // Soviet MT-LB Armored Personnel Carrier
            //----------------------------------------------
            // Note- should only be a transport for air mobile infantry.  
            WeaponProfile MTLB = new WeaponProfile(
                _longName: "MT-LB Armored Personnel Carrier",  // Full name for UI display and intel reports
                _shortName: "MT-LB",                            // Short name for UI display and intel reports
                _type: WeaponType.APC_MTLB_SV,                 // Enum identifier for this profile
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,       // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK,       // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE,      // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: true,                                 // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.APC,                 // Upgrade Path
                _turnAvailable: 312                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MTLB.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.APC);

            // Fill out intel stats for the MT-LB profile
            MTLB.AddIntelReportStat(WeaponType.APC_MTLB_SV,      68);
            MTLB.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     13);

            // Handle the icon profile.
            MTLB.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_MTLB_W,
                NW = SpriteManager.SV_MTLB_NW,
                SW = SpriteManager.SV_MTLB_SW
            };

            // Add the MT-LB profile to the database
            AddProfile(WeaponType.APC_MTLB_SV, MTLB);
            //----------------------------------------------
            // Soviet MT-LB Armored Personnel Carrier
            //----------------------------------------------

            //----------------------------------------------
            // Soviet BTR-70 Armored Personnel Carrier
            //----------------------------------------------
            WeaponProfile BTR70 = new WeaponProfile(
                _longName: "BTR-70 Armored Personnel Carrier",  // Full name for UI display and intel reports
                _shortName: "BTR-70",                            // Short name for UI display and intel reports
                _type: WeaponType.APC_BTR70_SV,                 // Enum identifier for this profile
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,        // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,       // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK,        // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE + SMALL_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,       // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,       // Ground Defense Armor Rating
                _df: 0,                                         // Dogfighting Rating
                _man: 0,                                        // Maneuverability Rating
                _topSpd: 0,                                     // Top Speed Rating
                _surv: 0,                                       // Survivability Rating
                _ga: 0,                                         // Ground Attack Rating
                _ol: 0,                                         // Ordinance Rating
                _stealth: 0,                                    // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,            // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,           // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,         // Spotting Range
                _mmp: GameData.MECH_UNIT,                       // Max Movement Points
                _isAmph: true,                                  // Is Amphibious
                _isDF: false,                                   // Is DoubleFire
                _isAtt: true,                                   // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                  // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                          // NBC Rating
                _nvg: NVG_Rating.Gen1,                          // NVG Rating
                _sil: UnitSilhouette.Small,                     // Unit Silhouette
                _upgradePath: UpgradePath.APC,                  // Upgrade Path
                _turnAvailable: 408                             // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BTR70.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.APC);

            // Fill out intel stats for the BTR-70 profile
            BTR70.AddIntelReportStat(WeaponType.TANK_T55A_SV,     40);
            BTR70.AddIntelReportStat(WeaponType.APC_BTR70_SV,    129);
            BTR70.AddIntelReportStat(WeaponType.IFV_BMP1_SV,      26);
            BTR70.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);

            // Handle the icon profile.
            BTR70.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_BTR70_W,
                NW = SpriteManager.SV_BTR70_NW,
                SW = SpriteManager.SV_BTR70_SW
            };

            // Add the BTR-70 profile to the database
            AddProfile(WeaponType.APC_BTR70_SV, BTR70);
            //----------------------------------------------
            // Soviet BTR-70 Armored Personnel Carrier
            //----------------------------------------------

            //----------------------------------------------
            // Soviet BTR-80 Armored Personnel Carrier
            //----------------------------------------------
            WeaponProfile BTR80 = new WeaponProfile(
                _longName: "BTR-80 Armored Personnel Carrier",  // Full name for UI display and intel reports
                _shortName: "BTR-80",                            // Short name for UI display and intel reports
                _type: WeaponType.APC_BTR80_SV,                 // Enum identifier for this profile
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,        // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,       // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK + SMALL_BONUS,  // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE + SMALL_BONUS, // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,       // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,       // Ground Defense Armor Rating
                _df: 0,                                         // Dogfighting Rating
                _man: 0,                                        // Maneuverability Rating
                _topSpd: 0,                                     // Top Speed Rating
                _surv: 0,                                       // Survivability Rating
                _ga: 0,                                         // Ground Attack Rating
                _ol: 0,                                         // Ordinance Rating
                _stealth: 0,                                    // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,            // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,           // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,         // Spotting Range
                _mmp: GameData.MECH_UNIT,                       // Max Movement Points
                _isAmph: true,                                  // Is Amphibious
                _isDF: false,                                   // Is DoubleFire
                _isAtt: true,                                   // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                  // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                          // NBC Rating
                _nvg: NVG_Rating.Gen2,                          // NVG Rating
                _sil: UnitSilhouette.Small,                     // Unit Silhouette
                _upgradePath: UpgradePath.APC,                  // Upgrade Path
                _turnAvailable: 576                             // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BTR80.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.APC);

            // Fill out intel stats for the BTR-80 profile
            BTR80.AddIntelReportStat(WeaponType.TANK_T72A_SV,     40);
            BTR80.AddIntelReportStat(WeaponType.APC_BTR80_SV,    129);
            BTR80.AddIntelReportStat(WeaponType.IFV_BMP2_SV,      26);
            BTR80.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);

            // Handle the icon profile.
            BTR80.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_BTR80_W,
                NW = SpriteManager.SV_BTR80_NW,
                SW = SpriteManager.SV_BTR80_SW
            };

            // Add the BTR-80 profile to the database
            AddProfile(WeaponType.APC_BTR80_SV, BTR80);
            //----------------------------------------------
            // Soviet BTR-80 Armored Personnel Carrier
            //----------------------------------------------

            #endregion // IFVs and APCs

            #region Recon

            //----------------------------------------------
            // Soviet BRDM-2 Recon Vehicle
            //----------------------------------------------
            WeaponProfile BRDM2 = new WeaponProfile(
                _longName: "BRDM-2 Recon Vehicle",             // Full name for UI display and intel reports
                _shortName: "BRDM-2",                           // Short name for UI display and intel reports
                _type: WeaponType.RCN_BRDM2_SV,                // Enum identifier for this profile
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,       // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK,       // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE,      // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,       // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: true,                                 // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.RCN,                 // Upgrade Path
                _turnAvailable: 288                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BRDM2.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.RCN);

            // Fill out intel stats for the BRDM-2 profile
            BRDM2.AddIntelReportStat(WeaponType.Personnel,       800);
            BRDM2.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     36);
            BRDM2.AddIntelReportStat(WeaponType.TANK_T62A_SV,     12);
            BRDM2.AddIntelReportStat(WeaponType.APC_BTR70_SV,     21);
            BRDM2.AddIntelReportStat(WeaponType.SPSAM_2K22_SV,     2);
            BRDM2.AddIntelReportStat(WeaponType.SPSAM_9K31_SV,     2);
            BRDM2.AddIntelReportStat(WeaponType.SPA_2S1_SV,        6);
            BRDM2.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,   4);
            BRDM2.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,  4);
            BRDM2.AddIntelReportStat(WeaponType.AT_ATGM,          24);
            BRDM2.AddIntelReportStat(WeaponType.MANPAD_STRELA,       12);
            // Handle the icon profile.
            BRDM2.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_BRDM2_W,
                NW = SpriteManager.SV_BRDM2_NW,
                SW = SpriteManager.SV_BRDM2_SW
            };

            // Add the BRDM-2 profile to the database
            AddProfile(WeaponType.RCN_BRDM2_SV, BRDM2);
            //----------------------------------------------
            // Soviet BRDM-2 Recon Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // Soviet BRDM-2 AT-5 Recon Vehicle
            //----------------------------------------------
            WeaponProfile BRDM2AT = new WeaponProfile(
                _longName: "BRDM-2 AT-5 Recon Vehicle",        // Full name for UI display and intel reports
                _shortName: "BRDM-2 AT",                        // Short name for UI display and intel reports
                _type: WeaponType.RCN_BRDM2AT_SV,              // Enum identifier for this profile
                _hardAtt: GameData.BASE_APC_HARD_ATTACK + XXLARGE_BONUS,  // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK,       // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE,      // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,       // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: true,                                 // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.RCN,                 // Upgrade Path
                _turnAvailable: 336                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BRDM2AT.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.RCN);

            // Fill out intel stats for the BRDM-2 AT profile
            BRDM2AT.AddIntelReportStat(WeaponType.Personnel, 800);
            BRDM2AT.AddIntelReportStat(WeaponType.RCN_BRDM2AT_SV,  48);
            BRDM2AT.AddIntelReportStat(WeaponType.TANK_T62A_SV, 12);
            BRDM2AT.AddIntelReportStat(WeaponType.APC_BTR70_SV, 21);
            BRDM2AT.AddIntelReportStat(WeaponType.SPSAM_2K22_SV, 2);
            BRDM2AT.AddIntelReportStat(WeaponType.SPSAM_9K31_SV, 2);
            BRDM2AT.AddIntelReportStat(WeaponType.SPA_2S1_SV, 6);
            BRDM2AT.AddIntelReportStat(WeaponType.ART_81MM_MORTAR, 4);
            BRDM2AT.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 4);
            BRDM2AT.AddIntelReportStat(WeaponType.AT_ATGM, 12);
            BRDM2AT.AddIntelReportStat(WeaponType.MANPAD_STRELA, 12);

            // Handle the icon profile.
            BRDM2AT.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_BRDM2AT_W,
                NW = SpriteManager.SV_BRDM2AT_NW,
                SW = SpriteManager.SV_BRDM2AT_SW
            };

            // Add the BRDM-2 AT profile to the database
            AddProfile(WeaponType.RCN_BRDM2AT_SV, BRDM2AT);
            //----------------------------------------------
            // Soviet BRDM-2 AT-5 Recon Vehicle
            //----------------------------------------------

            #endregion // Recon

            #region Self-Propelled Artillery

            //----------------------------------------------
            // Soviet 2S1 Gvozdika Self-Propelled Artillery
            //----------------------------------------------
            WeaponProfile SPA2S1 = new WeaponProfile(
                _longName: "2S1 Gvozdika Self-Propelled Artillery",  // Full name for UI display and intel reports
                _shortName: "2S1 Gvozdika",                          // Short name for UI display and intel reports
                _type: WeaponType.SPA_2S1_SV,              // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,                  // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SHORT,        // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 396                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SPA2S1.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPA);

            // Fill out intel stats for the 2S1 profile
            SPA2S1.AddIntelReportStat(WeaponType.Personnel,      1100);
            SPA2S1.AddIntelReportStat(WeaponType.SPA_2S1_SV,       36);
            SPA2S1.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            SPA2S1.AddIntelReportStat(WeaponType.APC_BTR70_SV,     24);
            SPA2S1.AddIntelReportStat(WeaponType.MANPAD_STRELA,       21);

            // Handle the icon profile.
            SPA2S1.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_2S1_W,
                NW = SpriteManager.SV_2S1_NW,
                SW = SpriteManager.SV_2S1_SW,
                W_F = SpriteManager.SV_2S1_W_F,
                NW_F = SpriteManager.SV_2S1_NW_F,
                SW_F = SpriteManager.SV_2S1_SW_F
            };

            // Add the 2S1 profile to the database
            AddProfile(WeaponType.SPA_2S1_SV, SPA2S1);
            //----------------------------------------------
            // Soviet 2S1 Gvozdika Self-Propelled Artillery
            //----------------------------------------------

            //----------------------------------------------
            // Soviet 2S3 Akatsiya Self-Propelled Artillery
            //----------------------------------------------
            WeaponProfile SPA2S3 = new WeaponProfile(
                _longName: "2S3 Akatsiya Self-Propelled Artillery",  // Full name for UI display and intel reports
                _shortName: "2S3 Akatsiya",                          // Short name for UI display and intel reports
                _type: WeaponType.SPA_2S3_SV,              // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,                  // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + MEDIUM_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_MEDIUM,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 396                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SPA2S3.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPA);

            // Fill out intel stats for the 2S3 profile
            SPA2S3.AddIntelReportStat(WeaponType.Personnel,      1100);
            SPA2S3.AddIntelReportStat(WeaponType.SPA_2S3_SV,       36);
            SPA2S3.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            SPA2S3.AddIntelReportStat(WeaponType.APC_BTR70_SV,     24);
            SPA2S3.AddIntelReportStat(WeaponType.MANPAD_STRELA,       21);

            // Handle the icon profile.
            SPA2S3.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_2S3_W,
                NW = SpriteManager.SV_2S3_NW,
                SW = SpriteManager.SV_2S3_SW,
                W_F = SpriteManager.SV_2S3_W_F,
                NW_F = SpriteManager.SV_2S3_NW_F,
                SW_F = SpriteManager.SV_2S3_SW_F
            };

            // Add the 2S3 profile to the database
            AddProfile(WeaponType.SPA_2S3_SV, SPA2S3);
            //----------------------------------------------
            // Soviet 2S3 Akatsiya Self-Propelled Artillery
            //----------------------------------------------

            //----------------------------------------------
            // Soviet 2S5 Giatsint-S Self-Propelled Artillery
            //----------------------------------------------
            WeaponProfile SPA2S5 = new WeaponProfile(
                _longName: "2S5 Giatsint-S Self-Propelled Artillery",  // Full name for UI display and intel reports
                _shortName: "2S5 Giatsint-S",                          // Short name for UI display and intel reports
                _type: WeaponType.SPA_2S5_SV,              // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + MEDIUM_BONUS,   // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + LARGE_BONUS,    // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_LONG,         // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 456                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SPA2S5.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.SPA);

            // Fill out intel stats for the 2S5 profile
            SPA2S5.AddIntelReportStat(WeaponType.Personnel,      1100);
            SPA2S5.AddIntelReportStat(WeaponType.SPA_2S5_SV,       36);
            SPA2S5.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            SPA2S5.AddIntelReportStat(WeaponType.APC_BTR70_SV,     44);
            SPA2S5.AddIntelReportStat(WeaponType.MANPAD_STRELA,       21);

            // Handle the icon profile.
            SPA2S5.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_2S5_W,
                NW = SpriteManager.SV_2S5_NW,
                SW = SpriteManager.SV_2S5_SW,
                W_F = SpriteManager.SV_2S5_W_F,
                NW_F = SpriteManager.SV_2S5_NW_F,
                SW_F = SpriteManager.SV_2S5_SW_F
            };

            // Add the 2S5 profile to the database
            AddProfile(WeaponType.SPA_2S5_SV, SPA2S5);
            //----------------------------------------------
            // Soviet 2S5 Giatsint-S Self-Propelled Artillery
            //----------------------------------------------

            //----------------------------------------------
            // Soviet 2S19 Msta-S Self-Propelled Artillery
            //----------------------------------------------
            WeaponProfile SPA2S19 = new WeaponProfile(
                _longName: "2S19 Msta-S Self-Propelled Artillery",  // Full name for UI display and intel reports
                _shortName: "2S19 Msta-S",                          // Short name for UI display and intel reports
                _type: WeaponType.SPA_2S19_SV,             // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_MEDIUM,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 612                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SPA2S19.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.SPA);

            // Fill out intel stats for the 2S19 profile
            SPA2S19.AddIntelReportStat(WeaponType.Personnel,     1100);
            SPA2S19.AddIntelReportStat(WeaponType.SPA_2S19_SV,     36);
            SPA2S19.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,    12);
            SPA2S19.AddIntelReportStat(WeaponType.APC_BTR70_SV,    36);
            SPA2S19.AddIntelReportStat(WeaponType.MANPAD_STRELA,      21);

            // Handle the icon profile.
            SPA2S19.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_2S19_W,
                NW = SpriteManager.SV_2S19_NW,
                SW = SpriteManager.SV_2S19_SW,
                W_F = SpriteManager.SV_2S19_W_F,
                NW_F = SpriteManager.SV_2S19_NW_F,
                SW_F = SpriteManager.SV_2S19_SW_F
            };

            // Add the 2S19 profile to the database
            AddProfile(WeaponType.SPA_2S19_SV, SPA2S19);
            //----------------------------------------------
            // Soviet 2S19 Msta-S Self-Propelled Artillery
            //----------------------------------------------

            #endregion // Self-Propelled Artillery

            #region Artillery

            //----------------------------------------------
            // Soviet Light Towed Artillery
            //----------------------------------------------
            WeaponProfile ArtLight = new WeaponProfile(
                _longName: "Light Towed Artillery",            // Full name for UI display and intel reports
                _shortName: "Lt Artillery",                    // Short name for UI display and intel reports
                _type: WeaponType.ART_LIGHT_SV,                // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,      // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE,     // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK,      // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE,     // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_SHORT,            // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.FOOT_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.None,                         // NBC Rating
                _nvg: NVG_Rating.None,                         // NVG Rating
                _sil: UnitSilhouette.Large,                    // Unit Silhouette
                _upgradePath: UpgradePath.ART,                 // Upgrade Path
                _turnAvailable: 60                             // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ArtLight.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.ART);

            // Fill out intel stats for the Light Artillery profile
            ArtLight.AddIntelReportStat(WeaponType.Personnel,      1100);
            ArtLight.AddIntelReportStat(WeaponType.ART_LIGHT_SV,     72);
            ArtLight.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            ArtLight.AddIntelReportStat(WeaponType.APC_BTR70_SV,     24);
            ArtLight.AddIntelReportStat(WeaponType.MANPAD_STRELA,       21);

            // Handle the icon profile.
            ArtLight.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_LightArt
            };

            // Add the Light Artillery profile to the database
            AddProfile(WeaponType.ART_LIGHT_SV, ArtLight);
            //----------------------------------------------
            // Soviet Light Towed Artillery
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Heavy Towed Artillery
            //----------------------------------------------
            WeaponProfile ArtHeavy = new WeaponProfile(
                _longName: "Heavy Towed Artillery",            // Full name for UI display and intel reports
                _shortName: "Hvy Artillery",                   // Short name for UI display and intel reports
                _type: WeaponType.ART_HEAVY_SV,                // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,      // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE,     // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + MEDIUM_BONUS,  // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE,     // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_MEDIUM,           // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.FOOT_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.None,                         // NBC Rating
                _nvg: NVG_Rating.None,                         // NVG Rating
                _sil: UnitSilhouette.Large,                    // Unit Silhouette
                _upgradePath: UpgradePath.ART,                 // Upgrade Path
                _turnAvailable: 60                             // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ArtHeavy.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.ART);

            // Fill out intel stats for the Heavy Artillery profile
            ArtHeavy.AddIntelReportStat(WeaponType.Personnel,      1100);
            ArtHeavy.AddIntelReportStat(WeaponType.ART_HEAVY_SV,     72);
            ArtHeavy.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            ArtHeavy.AddIntelReportStat(WeaponType.APC_BTR70_SV,     24);
            ArtHeavy.AddIntelReportStat(WeaponType.MANPAD_STRELA,       21);

            // Handle the icon profile.
            ArtHeavy.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_HeavyArt
            };

            // Add the Heavy Artillery profile to the database
            AddProfile(WeaponType.ART_HEAVY_SV, ArtHeavy);
            //----------------------------------------------
            // Soviet Heavy Towed Artillery
            //----------------------------------------------

            #endregion // Artillery

            #region Rocket Artillery and Ballistic Missiles Vehicles

            //----------------------------------------------
            // Soviet BM-21 Grad MLRS
            //----------------------------------------------
            WeaponProfile BM21 = new WeaponProfile(
                _longName: "BM-21 Grad Multiple Launch Rocket System",  // Full name for UI display and intel reports
                _shortName: "BM-21 Grad",                               // Short name for UI display and intel reports
                _type: WeaponType.ROC_BM21_SV,             // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,                  // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_ROC_SR,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MOT_UNIT,                   // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: true,                               // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ROC,             // Upgrade Path
                _turnAvailable: 300                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BM21.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.ROC);

            // Fill out intel stats for the BM-21 profile
            BM21.AddIntelReportStat(WeaponType.Personnel,      1200);
            BM21.AddIntelReportStat(WeaponType.ROC_BM21_SV,      48);
            BM21.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            BM21.AddIntelReportStat(WeaponType.APC_BTR70_SV,     24);
            BM21.AddIntelReportStat(WeaponType.MANPAD_STRELA,       21);

            // Handle the icon profile.
            BM21.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_BM21_W,
                NW = SpriteManager.SV_BM21_NW,
                SW = SpriteManager.SV_BM21_SW,
                W_F = SpriteManager.SV_BM21_W_F,
                NW_F = SpriteManager.SV_BM21_NW_F,
                SW_F = SpriteManager.SV_BM21_SW_F
            };

            // Add the BM-21 profile to the database
            AddProfile(WeaponType.ROC_BM21_SV, BM21);
            //----------------------------------------------
            // Soviet BM-21 Grad MLRS
            //----------------------------------------------

            //----------------------------------------------
            // Soviet BM-27 Uragan MLRS
            //----------------------------------------------
            WeaponProfile BM27 = new WeaponProfile(
                _longName: "BM-27 Uragan Multiple Launch Rocket System",  // Full name for UI display and intel reports
                _shortName: "BM-27 Uragan",                               // Short name for UI display and intel reports
                _type: WeaponType.ROC_BM27_SV,             // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + LARGE_BONUS,    // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_ROC_MR,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MOT_UNIT,                   // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: true,                               // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ROC,             // Upgrade Path
                _turnAvailable: 444                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BM27.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.ROC);

            // Fill out intel stats for the BM-27 profile
            BM27.AddIntelReportStat(WeaponType.Personnel,      1200);
            BM27.AddIntelReportStat(WeaponType.ROC_BM27_SV,      24);
            BM27.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            BM27.AddIntelReportStat(WeaponType.APC_BTR70_SV,     24);
            BM27.AddIntelReportStat(WeaponType.MANPAD_STRELA,       21);

            // Handle the icon profile.
            BM27.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_BM27_W,
                NW = SpriteManager.SV_BM27_NW,
                SW = SpriteManager.SV_BM27_SW,
                W_F = SpriteManager.SV_BM27_W_F,
                NW_F = SpriteManager.SV_BM27_NW_F,
                SW_F = SpriteManager.SV_BM27_SW_F
            };

            // Add the BM-27 profile to the database
            AddProfile(WeaponType.ROC_BM27_SV, BM27);
            //----------------------------------------------
            // Soviet BM-27 Uragan MLRS
            //----------------------------------------------

            //----------------------------------------------
            // Soviet BM-30 Smerch MLRS
            //----------------------------------------------
            WeaponProfile BM30 = new WeaponProfile(
                _longName: "BM-30 Smerch Multiple Launch Rocket System",  // Full name for UI display and intel reports
                _shortName: "BM-30 Smerch",                               // Short name for UI display and intel reports
                _type: WeaponType.ROC_BM30_SV,             // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_ROC_LR,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MOT_UNIT,                   // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: true,                               // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ROC,             // Upgrade Path
                _turnAvailable: 588                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BM30.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.ROC);

            // Fill out intel stats for the BM-30 profile
            BM30.AddIntelReportStat(WeaponType.Personnel,      1200);
            BM30.AddIntelReportStat(WeaponType.ROC_BM30_SV,      24);
            BM30.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            BM30.AddIntelReportStat(WeaponType.APC_BTR70_SV,     24);
            BM30.AddIntelReportStat(WeaponType.MANPAD_STRELA,       21);

            // Handle the icon profile.
            BM30.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_BM30_W,
                NW = SpriteManager.SV_BM30_NW,
                SW = SpriteManager.SV_BM30_SW,
                W_F = SpriteManager.SV_BM30_W_F,
                NW_F = SpriteManager.SV_BM30_NW_F,
                SW_F = SpriteManager.SV_BM30_SW_F
            };

            // Add the BM-30 profile to the database
            AddProfile(WeaponType.ROC_BM30_SV, BM30);
            //----------------------------------------------
            // Soviet BM-30 Smerch MLRS
            //----------------------------------------------

            //----------------------------------------------
            // Soviet 9K72 Scud-B Tactical Ballistic Missile
            //----------------------------------------------
            WeaponProfile SCUD = new WeaponProfile(
                _longName: "9K72 Scud-B Tactical Ballistic Missile Launcher",  // Full name for UI display and intel reports
                _shortName: "9K72 Scud-B",                                      // Short name for UI display and intel reports
                _type: WeaponType.ROC_SCUD_SV,             // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + XXXLARGE_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,    // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_ROC_LR,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MOT_UNIT,                   // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: true,                               // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,                    // Unit Silhouette
                _upgradePath: UpgradePath.ROC,                 // Upgrade Path
                _turnAvailable: 324                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SCUD.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.BMS);

            // Fill out intel stats for the Scud-B profile
            SCUD.AddIntelReportStat(WeaponType.Personnel,      800);
            SCUD.AddIntelReportStat(WeaponType.ROC_SCUD_SV,     12);
            SCUD.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,    12);
            SCUD.AddIntelReportStat(WeaponType.APC_BTR70_SV,    24);
            SCUD.AddIntelReportStat(WeaponType.MANPAD_STRELA,      21);

            // Handle the icon profile.
            SCUD.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_ScudB_W,
                NW = SpriteManager.SV_ScudB_NW,
                SW = SpriteManager.SV_ScudB_SW,
                W_F = SpriteManager.SV_ScudB_W_F,
                NW_F = SpriteManager.SV_ScudB_NW_F,
                SW_F = SpriteManager.SV_ScudB_SW_F
            };

            // Add the Scud-B profile to the database
            AddProfile(WeaponType.ROC_SCUD_SV, SCUD);
            //----------------------------------------------
            // Soviet 9K72 Scud-B Tactical Ballistic Missile
            //----------------------------------------------

            #endregion // Rocket Artillery and Ballistic Missiles Vehicles

            #region Air Defense

            //----------------------------------------------
            // Soviet ZSU-57-2 Sparka SPAAA
            //----------------------------------------------
            WeaponProfile ZSU57 = new WeaponProfile(
                _longName: "ZSU-57-2 Sparka Self-Propelled Anti-Aircraft Gun",
                _shortName: "ZSU-57-2 Sparka",
                _type: WeaponType.SPAAA_ZSU57_SV,
                _hardAtt: GameData.BASE_AAA_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_AAA_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_AAA_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_AAA_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_AAA_GROUND_AIR_ATTACK,                  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_AAA,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_AAA,          // Indirect Range
                _sr: GameData.BASE_AAA_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.AAA,             // Upgrade Path
                _turnAvailable: 204                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ZSU57.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.SPAAA);

            // Fill out intel stats for the ZSU-57-2 profile
            ZSU57.AddIntelReportStat(WeaponType.Personnel,         600);
            ZSU57.AddIntelReportStat(WeaponType.SPAAA_ZSU57_SV,    18);
            ZSU57.AddIntelReportStat(WeaponType.MANPAD_STRELA,        21);
            ZSU57.AddIntelReportStat(WeaponType.APC_BTR70_SV,      22);

            // Handle the icon profile.
            ZSU57.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_ZSU57_W,
                NW = SpriteManager.SV_ZSU57_NW,
                SW = SpriteManager.SV_ZSU57_SW,
                W_F = SpriteManager.SV_ZSU57_W_F,
                NW_F = SpriteManager.SV_ZSU57_NW_F,
                SW_F = SpriteManager.SV_ZSU57_SW_F
            };

            // Add the ZSU-57-2 profile to the database
            AddProfile(WeaponType.SPAAA_ZSU57_SV, ZSU57);
            //----------------------------------------------
            // Soviet ZSU-57-2 Sparka SPAAA
            //----------------------------------------------

            //----------------------------------------------
            // Soviet ZSU-23-4 Shilka SPAAA
            //----------------------------------------------
            WeaponProfile ZSU23 = new WeaponProfile(
                _longName: "ZSU-23-4 Shilka Self-Propelled Anti-Aircraft Gun",
                _shortName: "ZSU-23-4 Shilka",
                _type: WeaponType.SPAAA_ZSU23_SV,
                _hardAtt: GameData.BASE_AAA_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_AAA_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_AAA_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_AAA_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_AAA_GROUND_AIR_ATTACK + MEDIUM_BONUS,   // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_AAA,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_AAA,          // Indirect Range
                _sr: GameData.BASE_AAA_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.AAA,             // Upgrade Path
                _turnAvailable: 324                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ZSU23.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPAAA);

            // Fill out intel stats for the ZSU-23-4 profile
            ZSU23.AddIntelReportStat(WeaponType.Personnel,         600);
            ZSU23.AddIntelReportStat(WeaponType.SPAAA_ZSU23_SV,    18);
            ZSU23.AddIntelReportStat(WeaponType.MANPAD_STRELA,        21);
            ZSU23.AddIntelReportStat(WeaponType.APC_BTR70_SV,      22);

            // Handle the icon profile.
            ZSU23.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_ZSU23_W,
                NW = SpriteManager.SV_ZSU23_NW,
                SW = SpriteManager.SV_ZSU23_SW,
                W_F = SpriteManager.SV_ZSU23_W_F,
                NW_F = SpriteManager.SV_ZSU23_NW_F,
                SW_F = SpriteManager.SV_ZSU23_SW_F
            };

            // Add the ZSU-23-4 profile to the database
            AddProfile(WeaponType.SPAAA_ZSU23_SV, ZSU23);
            //----------------------------------------------
            // Soviet ZSU-23-4 Shilka SPAAA
            //----------------------------------------------

            //----------------------------------------------
            // Soviet 2K22 Tunguska SPAAA
            //----------------------------------------------
            WeaponProfile Tunguska = new WeaponProfile(
                _longName: "2K22 Tunguska Self-Propelled Anti-Aircraft System",
                _shortName: "2K22 Tunguska",
                _type: WeaponType.SPSAM_2K22_SV,
                _hardAtt: GameData.BASE_AAA_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_AAA_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_AAA_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_AAA_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_AAA_GROUND_AIR_ATTACK + LARGE_BONUS,    // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_AAA + SMALL_BONUS,            // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_AAA + SMALL_BONUS,             // Indirect Range
                _sr: GameData.BASE_AAA_SPOTTING_RANGE + SMALL_BONUS,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.AAA,             // Upgrade Path
                _turnAvailable: 528                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            Tunguska.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.SPSAM);

            // Fill out intel stats for the 2K22 Tunguska profile
            Tunguska.AddIntelReportStat(WeaponType.Personnel,       600);
            Tunguska.AddIntelReportStat(WeaponType.SPSAM_2K22_SV,    18);
            Tunguska.AddIntelReportStat(WeaponType.MANPAD_STRELA,       21);
            Tunguska.AddIntelReportStat(WeaponType.APC_BTR70_SV,     22);

            // Handle the icon profile.
            Tunguska.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_2K22_W,
                NW = SpriteManager.SV_2K22_NW,
                SW = SpriteManager.SV_2K22_SW,
                W_F = SpriteManager.SV_2K22_W_F,
                NW_F = SpriteManager.SV_2K22_NW_F,
                SW_F = SpriteManager.SV_2K22_SW_F
            };

            // Add the 2K22 Tunguska profile to the database
            AddProfile(WeaponType.SPSAM_2K22_SV, Tunguska);
            //----------------------------------------------
            // Soviet 2K22 Tunguska SPAAA
            //----------------------------------------------

            //----------------------------------------------
            // Soviet 2K12 Kub SPSAM
            //----------------------------------------------
            WeaponProfile Kub = new WeaponProfile(
                _longName: "2K12 Kub Self-Propelled SAM System",
                _shortName: "2K12 Kub",
                _type: WeaponType.SPSAM_2K12_SV,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK + SMALL_BONUS,    // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_SAM,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SAM,          // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 348                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            Kub.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPSAM);

            // Fill out intel stats for the 2K12 Kub profile
            Kub.AddIntelReportStat(WeaponType.Personnel,         750);
            Kub.AddIntelReportStat(WeaponType.SPSAM_2K12_SV,     18);
            Kub.AddIntelReportStat(WeaponType.MANPAD_STRELA,        21);
            Kub.AddIntelReportStat(WeaponType.APC_BTR70_SV,      22);

            // Handle the icon profile.
            Kub.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_2K12_W,
                NW = SpriteManager.SV_2K12_NW,
                SW = SpriteManager.SV_2K12_SW,
                W_F = SpriteManager.SV_2K12_W_F,
                NW_F = SpriteManager.SV_2K12_NW_F,
                SW_F = SpriteManager.SV_2K12_SW_F
            };

            // Add the 2K12 Kub profile to the database
            AddProfile(WeaponType.SPSAM_2K12_SV, Kub);
            //----------------------------------------------
            // Soviet 2K12 Kub SPSAM
            //----------------------------------------------

            //----------------------------------------------
            // Soviet 9K31 Strela-1 SPSAM
            //----------------------------------------------
            WeaponProfile Strela1 = new WeaponProfile(
                _longName: "9K31 Strela-1 Self-Propelled SAM System",
                _shortName: "9K31 Strela-1",
                _type: WeaponType.SPSAM_9K31_SV,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK,                  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_SAM,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SAM,          // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: true,                             // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small,                // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 360                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            Strela1.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPSAM);

            // Fill out intel stats for the 9K31 Strela-1 profile
            Strela1.AddIntelReportStat(WeaponType.Personnel,        750);
            Strela1.AddIntelReportStat(WeaponType.SPSAM_9K31_SV,    18);
            Strela1.AddIntelReportStat(WeaponType.MANPAD_STRELA,       21);
            Strela1.AddIntelReportStat(WeaponType.APC_BTR70_SV,     22);

            // Handle the icon profile.
            Strela1.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_9K31_W,
                NW = SpriteManager.SV_9K31_NW,
                SW = SpriteManager.SV_9K31_SW,
                W_F = SpriteManager.SV_9K31_W_F,
                NW_F = SpriteManager.SV_9K31_NW_F,
                SW_F = SpriteManager.SV_9K31_SW_F
            };

            // Add the 9K31 Strela-1 profile to the database
            AddProfile(WeaponType.SPSAM_9K31_SV, Strela1);
            //----------------------------------------------
            // Soviet 9K31 Strela-1 SPSAM
            //----------------------------------------------

            //----------------------------------------------
            // Soviet S-75 Dvina SAM System
            //----------------------------------------------
            WeaponProfile S75 = new WeaponProfile(
                _longName: "S-75 Dvina Surface-to-Air Missile System",
                _shortName: "S-75 Dvina",
                _type: WeaponType.SAM_S75_SV,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK,                  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_SAM,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SAM,          // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.STATIC_UNIT,                // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 228                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            S75.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.SAM);

            // Fill out intel stats for the S-75 Dvina profile
            S75.AddIntelReportStat(WeaponType.Personnel,      750);
            S75.AddIntelReportStat(WeaponType.SAM_S75_SV,      18);
            S75.AddIntelReportStat(WeaponType.MANPAD_STRELA,      21);
            S75.AddIntelReportStat(WeaponType.APC_BTR70_SV,    48);

            // Handle the icon profile.
            S75.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_S75
            };

            // Add the S-75 Dvina profile to the database
            AddProfile(WeaponType.SAM_S75_SV, S75);
            //----------------------------------------------
            // Soviet S-75 Dvina SAM System
            //----------------------------------------------

            //----------------------------------------------
            // Soviet S-125 Neva SAM System
            //----------------------------------------------
            WeaponProfile S125 = new WeaponProfile(
                _longName: "S-125 Neva Surface-to-Air Missile System",
                _shortName: "S-125 Neva",
                _type: WeaponType.SAM_S125_SV,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK + MEDIUM_BONUS,   // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_SAM,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SAM,          // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.STATIC_UNIT,                // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 276                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            S125.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.SAM);

            // Fill out intel stats for the S-125 Neva profile
            S125.AddIntelReportStat(WeaponType.Personnel,      750);
            S125.AddIntelReportStat(WeaponType.SAM_S125_SV,     18);
            S125.AddIntelReportStat(WeaponType.MANPAD_STRELA,      21);
            S125.AddIntelReportStat(WeaponType.APC_BTR70_SV,    48);

            // Handle the icon profile.
            S125.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_S125
            };

            // Add the S-125 Neva profile to the database
            AddProfile(WeaponType.SAM_S125_SV, S125);
            //----------------------------------------------
            // Soviet S-125 Neva SAM System
            //----------------------------------------------

            //----------------------------------------------
            // Soviet S-300 SAM System
            //----------------------------------------------
            WeaponProfile S300 = new WeaponProfile(
                _longName: "S-300 Surface-to-Air Missile System",
                _shortName: "S-300",
                _type: WeaponType.SAM_S300_SV,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK + XXLARGE_BONUS,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_SAM,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SAM + XLARGE_BONUS,            // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE + XLARGE_BONUS,       // Spotting Range
                _mmp: GameData.STATIC_UNIT,                // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 480                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            S300.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.SAM);

            // Fill out intel stats for the S-300 profile
            S300.AddIntelReportStat(WeaponType.Personnel,      750);
            S300.AddIntelReportStat(WeaponType.SAM_S300_SV,     18);
            S300.AddIntelReportStat(WeaponType.MANPAD_STRELA,      21);
            S300.AddIntelReportStat(WeaponType.APC_BTR70_SV,    48);

            // Handle the icon profile.
            S300.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.SV_S300_W,
                NW = SpriteManager.SV_S300_NW,
                SW = SpriteManager.SV_S300_SW,
                W_F = SpriteManager.SV_S300_W_F,
                NW_F = SpriteManager.SV_S300_NW_F,
                SW_F = SpriteManager.SV_S300_SW_F
            };

            // Add the S-300 profile to the database
            AddProfile(WeaponType.SAM_S300_SV, S300);
            //----------------------------------------------
            // Soviet S-300 SAM System
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Generic AAA Platform
            //----------------------------------------------
            WeaponProfile AAA_GEN = new WeaponProfile(
                _longName: "Generic Anti-Aircraft Artillery Emplacement",
                _shortName: "Generic AAA",
                _type: WeaponType.AAA_GEN_SV,
                _hardAtt: GameData.BASE_AAA_HARD_ATTACK + SMALL_MALUS,      // Hard Attack Rating
                _hardDef: GameData.BASE_AAA_HARD_DEFENSE + SMALL_MALUS,     // Hard Defense Rating
                _softAtt: GameData.BASE_AAA_SOFT_ATTACK + SMALL_MALUS,      // Soft Attack Rating
                _softDef: GameData.BASE_AAA_SOFT_DEFENSE + SMALL_MALUS,     // Soft Defense Rating
                _gat: GameData.BASE_AAA_GROUND_AIR_ATTACK + SMALL_MALUS,    // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,                     // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_AAA,          // Indirect Range
                _sr: GameData.BASE_AAA_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.AAA,             // Upgrade Path
                _turnAvailable: 144                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            AAA_GEN.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.AAA);

            // Fill out intel stats for the Generic AAA profile
            AAA_GEN.AddIntelReportStat(WeaponType.Personnel,      500);
            AAA_GEN.AddIntelReportStat(WeaponType.AAA_GEN_SV,      18);
            AAA_GEN.AddIntelReportStat(WeaponType.MANPAD_STRELA,      21);
            AAA_GEN.AddIntelReportStat(WeaponType.APC_BTR70_SV,    22);

            // Handle the icon profile.
            AAA_GEN.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_AA
            };

            // Add the Generic AAA profile to the database
            AddProfile(WeaponType.AAA_GEN_SV, AAA_GEN);
            //----------------------------------------------
            // Soviet Generic AAA Platform
            //----------------------------------------------

            #endregion // Air Defense

            #region Helicopters

            //----------------------------------------------
            // Soviet Mi-8T Hip Transport Helicopter
            //----------------------------------------------
            WeaponProfile MI8T = new WeaponProfile(
                _longName: "Mi-8T Hip Transport Helicopter",
                _shortName: "Mi-8T Hip",
                _type: WeaponType.HEL_MI8T_SV,
                _hardAtt: GameData.BASE_HEL_HARD_ATTACK + MEDIUM_MALUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_HEL_HARD_DEFENSE + MEDIUM_MALUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_HEL_SOFT_ATTACK + MEDIUM_MALUS,     // Soft Attack Rating
                _softDef: GameData.BASE_HEL_SOFT_DEFENSE + MEDIUM_MALUS,    // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_HELO,        // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.HELO_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.HELT,            // Upgrade Path
                _turnAvailable: 348                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MI8T.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.HELT);

            // Fill out intel stats for the Mi-8T profile
            MI8T.AddIntelReportStat(WeaponType.HEL_MI8T_SV,       109);

            // Handle the icon profile.
            MI8T.IconProfile = new RegimentIconProfile(RegimentIconType.Helo_Animation)
            {
                W = SpriteManager.SV_MI8_Frame0,
                NW = SpriteManager.SV_MI8_Frame1,
                SW = SpriteManager.SV_MI8_Frame2,
                W_F = SpriteManager.SV_MI8_Frame3,
                NW_F = SpriteManager.SV_MI8_Frame4,
                SW_F = SpriteManager.SV_MI8_Frame5
            };

            // Add the Mi-8T profile to the database
            AddProfile(WeaponType.HEL_MI8T_SV, MI8T);
            //----------------------------------------------
            // Soviet Mi-8T Hip Transport Helicopter
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Mi-8AT Hip-C Attack Helicopter
            //----------------------------------------------
            WeaponProfile MI8AT = new WeaponProfile(
                _longName: "Mi-8AT Hip-C Attack Helicopter",
                _shortName: "Mi-8AT Hip-C",
                _type: WeaponType.HEL_MI8AT_SV,
                _hardAtt: GameData.BASE_HEL_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_HEL_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_HEL_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_HEL_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_HELO,        // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.HELO_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.HELT,            // Upgrade Path
                _turnAvailable: 444                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MI8AT.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.HELT);

            // Fill out intel stats for the Mi-8AT profile
            MI8AT.AddIntelReportStat(WeaponType.Personnel, 475);
            MI8AT.AddIntelReportStat(WeaponType.HEL_MI8AT_SV,     54);

            // Handle the icon profile.
            MI8AT.IconProfile = new RegimentIconProfile(RegimentIconType.Helo_Animation)
            {
                W = SpriteManager.SV_MI8AT_Frame0,
                NW = SpriteManager.SV_MI8AT_Frame1,
                SW = SpriteManager.SV_MI8AT_Frame2,
                W_F = SpriteManager.SV_MI8AT_Frame3,
                NW_F = SpriteManager.SV_MI8AT_Frame4,
                SW_F = SpriteManager.SV_MI8AT_Frame5
            };

            // Add the Mi-8AT profile to the database
            AddProfile(WeaponType.HEL_MI8AT_SV, MI8AT);
            //----------------------------------------------
            // Soviet Mi-8AT Hip-C Attack Helicopter
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Mi-24D Hind-D Attack Helicopter
            //----------------------------------------------
            WeaponProfile MI24D = new WeaponProfile(
                _longName: "Mi-24D Hind-D Attack Helicopter",
                _shortName: "Mi-24D Hind-D",
                _type: WeaponType.HEL_MI24D_SV,
                _hardAtt: GameData.BASE_HEL_HARD_ATTACK + SMALL_BONUS,      // Hard Attack Rating
                _hardDef: GameData.BASE_HEL_HARD_DEFENSE + MEDIUM_BONUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_HEL_SOFT_ATTACK + SMALL_BONUS,      // Soft Attack Rating
                _softDef: GameData.BASE_HEL_SOFT_DEFENSE + SMALL_BONUS,     // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_HELO,        // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.HELO_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.HEL,             // Upgrade Path
                _turnAvailable: 408                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MI24D.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.HEL);

            // Fill out intel stats for the Mi-24D profile
            MI24D.AddIntelReportStat(WeaponType.Personnel,       475);
            MI24D.AddIntelReportStat(WeaponType.HEL_MI24D_SV,     54);

            // Handle the icon profile.
            MI24D.IconProfile = new RegimentIconProfile(RegimentIconType.Helo_Animation)
            {
                W = SpriteManager.SV_MI24D_Frame0,
                NW = SpriteManager.SV_MI24D_Frame1,
                SW = SpriteManager.SV_MI24D_Frame2,
                W_F = SpriteManager.SV_MI24D_Frame3,
                NW_F = SpriteManager.SV_MI24D_Frame4,
                SW_F = SpriteManager.SV_MI24D_Frame5
            };

            // Add the Mi-24D profile to the database
            AddProfile(WeaponType.HEL_MI24D_SV, MI24D);
            //----------------------------------------------
            // Soviet Mi-24D Hind-D Attack Helicopter
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Mi-24V Hind-E Attack Helicopter
            //----------------------------------------------
            WeaponProfile MI24V = new WeaponProfile(
                _longName: "Mi-24V Hind-E Attack Helicopter",
                _shortName: "Mi-24V Hind-E",
                _type: WeaponType.HEL_MI24V_SV,
                _hardAtt: GameData.BASE_HEL_HARD_ATTACK + XLARGE_BONUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_HEL_HARD_DEFENSE + MEDIUM_BONUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_HEL_SOFT_ATTACK + SMALL_BONUS,      // Soft Attack Rating
                _softDef: GameData.BASE_HEL_SOFT_DEFENSE + SMALL_BONUS,     // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_HELO,        // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.HELO_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.HEL,             // Upgrade Path
                _turnAvailable: 456                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MI24V.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.HEL);

            // Fill out intel stats for the Mi-24V profile
            MI24V.AddIntelReportStat(WeaponType.Personnel,       475);
            MI24V.AddIntelReportStat(WeaponType.HEL_MI24V_SV,     54);

            // Handle the icon profile.
            MI24V.IconProfile = new RegimentIconProfile(RegimentIconType.Helo_Animation)
            {
                W = SpriteManager.SV_MI24V_Frame0,
                NW = SpriteManager.SV_MI24V_Frame1,
                SW = SpriteManager.SV_MI24V_Frame2,
                W_F = SpriteManager.SV_MI24V_Frame3,
                NW_F = SpriteManager.SV_MI24V_Frame4,
                SW_F = SpriteManager.SV_MI24V_Frame5
            };

            // Add the Mi-24V profile to the database
            AddProfile(WeaponType.HEL_MI24V_SV, MI24V);
            //----------------------------------------------
            // Soviet Mi-24V Hind-E Attack Helicopter
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Mi-28 Havoc Attack Helicopter
            //----------------------------------------------
            WeaponProfile MI28 = new WeaponProfile(
                _longName: "Mi-28 Havoc Attack Helicopter",
                _shortName: "Mi-28 Havoc",
                _type: WeaponType.HEL_MI28_SV,
                _hardAtt: GameData.BASE_HEL_HARD_ATTACK + XXXLARGE_BONUS,   // Hard Attack Rating
                _hardDef: GameData.BASE_HEL_HARD_DEFENSE + SMALL_BONUS,     // Hard Defense Rating
                _softAtt: GameData.BASE_HEL_SOFT_ATTACK + MEDIUM_BONUS,     // Soft Attack Rating
                _softDef: GameData.BASE_HEL_SOFT_DEFENSE + SMALL_BONUS,     // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_HELO,        // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.HELO_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.HEL,             // Upgrade Path
                _turnAvailable: 600                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MI28.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.HEL);

            // Fill out intel stats for the Mi-28 profile
            MI28.AddIntelReportStat(WeaponType.Personnel,        475);
            MI28.AddIntelReportStat(WeaponType.HEL_MI28_SV,       54);

            // Handle the icon profile.
            MI28.IconProfile = new RegimentIconProfile(RegimentIconType.Helo_Animation)
            {
                W = SpriteManager.SV_MI28_Frame0,
                NW = SpriteManager.SV_MI28_Frame1,
                SW = SpriteManager.SV_MI28_Frame2,
                W_F = SpriteManager.SV_MI28_Frame3,
                NW_F = SpriteManager.SV_MI28_Frame4,
                SW_F = SpriteManager.SV_MI28_Frame5
            };

            // Add the Mi-28 profile to the database
            AddProfile(WeaponType.HEL_MI28_SV, MI28);
            //----------------------------------------------
            // Soviet Mi-28 Havoc Attack Helicopter
            //----------------------------------------------

            #endregion // Helicopters

            #region Jets

            //----------------------------------------------
            // Soviet An-12 Antonov Transport Plane
            //----------------------------------------------
            WeaponProfile AN12 = new WeaponProfile(
                _longName: "An-12 Antonov Transport Plane",
                _shortName: "An-12 Antonov",
                _type: WeaponType.TRN_AN8_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_BOMBER_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_BOMBER_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_BOMBER_TOPSPEED + LARGE_MALUS,        // Top Speed Rating
                _surv: GameData.AC_BOMBER_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.LARGE_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,               // Unit Silhouette
                _upgradePath: UpgradePath.TRN,            // Upgrade Path
                _turnAvailable: 252                       // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            AN12.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.TRN);

            // Fill out intel stats for the An-12 profile
            AN12.AddIntelReportStat(WeaponType.TRN_AN8_SV,        48);

            // Handle the icon profile.
            AN12.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_AN8
            };

            // Add the An-12 profile to the database
            AddProfile(WeaponType.TRN_AN8_SV, AN12);
            //----------------------------------------------
            // Soviet An-12 Antonov Transport Plane
            //----------------------------------------------

            //----------------------------------------------
            // Soviet A-50 Mainstay AWACS
            //----------------------------------------------
            WeaponProfile A50 = new WeaponProfile(
                _longName: "A-50 Mainstay AWACS",
                _shortName: "A-50 Mainstay",
                _type: WeaponType.AWACS_A50_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_BOMBER_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_BOMBER_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_BOMBER_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.AC_BOMBER_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.LARGE_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,               // Unit Silhouette
                _upgradePath: UpgradePath.AWACS,          // Upgrade Path
                _turnAvailable: 552                       // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            A50.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.AWACS);

            // Fill out intel stats for the A-50 profile
            A50.AddIntelReportStat(WeaponType.AWACS_A50_SV,        6);

            // Handle the icon profile.
            A50.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_A50
            };

            // Add the A-50 profile to the database
            AddProfile(WeaponType.AWACS_A50_SV, A50);
            //----------------------------------------------
            // Soviet A-50 Mainstay AWACS
            //----------------------------------------------

            //----------------------------------------------
            // Soviet MiG-21 Fishbed Air Superiority Fighter
            //----------------------------------------------
            WeaponProfile MIG21 = new WeaponProfile(
                _longName: "MiG-21 Fishbed Air Superiority Fighter",
                _shortName: "MiG-21 Fishbed",
                _type: WeaponType.FGT_MIG21_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.EARLY_FGT_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Day,                // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,            // Upgrade Path
                _turnAvailable: 252                       // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MIG21.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.FGT);

            // Fill out intel stats for the MiG-21 profile
            MIG21.AddIntelReportStat(WeaponType.FGT_MIG21_SV,     36);

            // Handle the icon profile.
            MIG21.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Mig21
            };

            // Add the MiG-21 profile to the database
            AddProfile(WeaponType.FGT_MIG21_SV, MIG21);
            //----------------------------------------------
            // Soviet MiG-21 Fishbed Air Superiority Fighter
            //----------------------------------------------

            //----------------------------------------------
            // Soviet MiG-23 Flogger Air Superiority Fighter
            //----------------------------------------------
            WeaponProfile MIG23 = new WeaponProfile(
                _longName: "MiG-23 Flogger Air Superiority Fighter",
                _shortName: "MiG-23 Flogger",
                _type: WeaponType.FGT_MIG23_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT + LARGE_BONUS,            // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.EARLY_FGT_TOPSPEED + SMALL_BONUS,        // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE + MEDIUM_BONUS,          // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_0,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 384                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MIG23.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.FGT);

            // Fill out intel stats for the MiG-23 profile
            MIG23.AddIntelReportStat(WeaponType.FGT_MIG23_SV,     36);

            // Handle the icon profile.
            MIG23.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Mig23
            };

            // Add the MiG-23 profile to the database
            AddProfile(WeaponType.FGT_MIG23_SV, MIG23);
            //----------------------------------------------
            // Soviet MiG-23 Flogger Air Superiority Fighter
            //----------------------------------------------

            //----------------------------------------------
            // Soviet MiG-25 Foxbat Interceptor
            //----------------------------------------------
            WeaponProfile MIG25 = new WeaponProfile(
                _longName: "MiG-25 Foxbat Interceptor",
                _shortName: "MiG-25 Foxbat",
                _type: WeaponType.FGT_MIG25_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_HIGHSPEED_RUSSIAN,                    // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_0,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 384                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MIG25.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.FGT);

            // Fill out intel stats for the MiG-25 profile
            MIG25.AddIntelReportStat(WeaponType.FGT_MIG25_SV,     36);

            // Handle the icon profile.
            MIG25.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Mig25
            };

            // Add the MiG-25 profile to the database
            AddProfile(WeaponType.FGT_MIG25_SV, MIG25);
            //----------------------------------------------
            // Soviet MiG-25 Foxbat Interceptor
            //----------------------------------------------

            //----------------------------------------------
            // Soviet MiG-29 Fulcrum Air Superiority Fighter
            //----------------------------------------------
            WeaponProfile MIG29 = new WeaponProfile(
                _longName: "MiG-29 Fulcrum Air Superiority Fighter",
                _shortName: "MiG-29 Fulcrum",
                _type: WeaponType.FGT_MIG29_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.MID_FGT_DOGFIGHT + LARGE_BONUS,              // Dogfighting Rating
                _man: GameData.MID_FGT_MANEUVER + XXLARGE_BONUS,           // Maneuverability Rating
                _topSpd: GameData.MID_FGT_TOPSPEED + SMALL_BONUS,          // Top Speed Rating
                _surv: GameData.MID_FGT_SURVIVE,                           // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_1,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 540                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MIG29.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.FGT);

            // Fill out intel stats for the MiG-29 profile
            MIG29.AddIntelReportStat(WeaponType.FGT_MIG29_SV,     36);

            // Handle the icon profile.
            MIG29.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Mig29
            };

            // Add the MiG-29 profile to the database
            AddProfile(WeaponType.FGT_MIG29_SV, MIG29);
            //----------------------------------------------
            // Soviet MiG-29 Fulcrum Air Superiority Fighter
            //----------------------------------------------

            //----------------------------------------------
            // Soviet MiG-31 Foxhound Interceptor
            //----------------------------------------------
            WeaponProfile MIG31 = new WeaponProfile(
                _longName: "MiG-31 Foxhound Interceptor",
                _shortName: "MiG-31 Foxhound",
                _type: WeaponType.FGT_MIG31_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.MID_FGT_DOGFIGHT + LARGE_BONUS,              // Dogfighting Rating
                _man: GameData.MID_FGT_MANEUVER,                           // Maneuverability Rating
                _topSpd: GameData.AC_HIGHSPEED_RUSSIAN,                    // Top Speed Rating
                _surv: GameData.MID_FGT_SURVIVE,                           // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_1 + LARGE_MALUS,          // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ADVANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 516                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MIG31.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.FGT);

            // Fill out intel stats for the MiG-31 profile
            MIG31.AddIntelReportStat(WeaponType.FGT_MIG31_SV,     36);

            // Handle the icon profile.
            MIG31.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Mig31
            };

            // Add the MiG-31 profile to the database
            AddProfile(WeaponType.FGT_MIG31_SV, MIG31);
            //----------------------------------------------
            // Soviet MiG-31 Foxhound Interceptor
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Su-27 Flanker Air Superiority Fighter
            //----------------------------------------------
            WeaponProfile SU27 = new WeaponProfile(
                _longName: "Su-27 Flanker Air Superiority Fighter",
                _shortName: "Su-27 Flanker",
                _type: WeaponType.FGT_SU27_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.MID_FGT_DOGFIGHT + XXLARGE_BONUS,            // Dogfighting Rating
                _man: GameData.MID_FGT_MANEUVER + MEDIUM_BONUS,            // Maneuverability Rating
                _topSpd: GameData.MID_FGT_TOPSPEED + MEDIUM_BONUS,         // Top Speed Rating
                _surv: GameData.MID_FGT_SURVIVE + MEDIUM_BONUS,            // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_1,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ADVANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 564                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SU27.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.FGT);

            // Fill out intel stats for the Su-27 profile
            SU27.AddIntelReportStat(WeaponType.FGT_SU27_SV,       36);

            // Handle the icon profile.
            SU27.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_SU27
            };

            // Add the Su-27 profile to the database
            AddProfile(WeaponType.FGT_SU27_SV, SU27);
            //----------------------------------------------
            // Soviet Su-27 Flanker Air Superiority Fighter
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Su-47 Berkut Experimental Fighter
            //----------------------------------------------
            WeaponProfile SU47 = new WeaponProfile(
                _longName: "Su-47 Berkut Experimental Fighter",
                _shortName: "Su-47 Berkut",
                _type: WeaponType.FGT_SU47_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.LATE_FGT_DOGFIGHT + XXXLARGE_BONUS,          // Dogfighting Rating
                _man: GameData.LATE_FGT_MANEUVER + XXLARGE_BONUS,          // Maneuverability Rating
                _topSpd: GameData.LATE_FGT_TOPSPEED,                       // Top Speed Rating
                _surv: GameData.LATE_FGT_SURVIVE,                          // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_2,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 708                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SU47.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.FGT);

            // Fill out intel stats for the Su-47 profile
            SU47.AddIntelReportStat(WeaponType.FGT_SU47_SV,       36);

            // Handle the icon profile.
            SU47.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_SU47
            };

            // Add the Su-47 profile to the database
            AddProfile(WeaponType.FGT_SU47_SV, SU47);
            //----------------------------------------------
            // Soviet Su-47 Berkut Experimental Fighter
            //----------------------------------------------

            //----------------------------------------------
            // Soviet MiG-27 Flogger-D Multi-Role Fighter
            //----------------------------------------------
            WeaponProfile MIG27 = new WeaponProfile(
                _longName: "MiG-27 Flogger-D Multi-Role Fighter",
                _shortName: "MiG-27 Flogger-D",
                _type: WeaponType.FGT_MIG27_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_ATTACK_DOGFIGHT + LARGE_BONUS,            // Dogfighting Rating
                _man: GameData.AC_ATTACK_MANEUVER + MEDIUM_BONUS,          // Maneuverability Rating
                _topSpd: GameData.AC_ATTACK_TOPSPEED + MEDIUM_BONUS,       // Top Speed Rating
                _surv: GameData.AC_ATTACK_SURVIVE + SMALL_BONUS,           // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_0 + XLARGE_BONUS,         // Ground Attack Rating
                _ol: GameData.MEDIUM_AC_LOAD,                              // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ATT,             // Upgrade Path
                _turnAvailable: 444                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MIG27.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.ATT);

            // Fill out intel stats for the MiG-27 profile
            MIG27.AddIntelReportStat(WeaponType.FGT_MIG27_SV,     36);

            // Handle the icon profile.
            MIG27.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Mig27
            };

            // Add the MiG-27 profile to the database
            AddProfile(WeaponType.FGT_MIG27_SV, MIG27);
            //----------------------------------------------
            // Soviet MiG-27 Flogger-D Multi-Role Fighter
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Su-17 Fitter Attack Aircraft
            //----------------------------------------------
            WeaponProfile SU17 = new WeaponProfile(
                _longName: "Su-17 Fitter Attack Aircraft",
                _shortName: "Su-17 Fitter",
                _type: WeaponType.ATT_SU17_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_ATTACK_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_ATTACK_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_ATTACK_TOPSPEED + SMALL_BONUS,        // Top Speed Rating
                _surv: GameData.AC_ATTACK_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_0 + LARGE_BONUS,          // Ground Attack Rating
                _ol: GameData.MEDIUM_AC_LOAD,                              // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ATT,             // Upgrade Path
                _turnAvailable: 384                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SU17.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.ATT);

            // Fill out intel stats for the Su-17 profile
            SU17.AddIntelReportStat(WeaponType.ATT_SU17_SV,       36);

            // Handle the icon profile.
            SU17.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_SU17
            };

            // Add the Su-17 profile to the database
            AddProfile(WeaponType.ATT_SU17_SV, SU17);
            //----------------------------------------------
            // Soviet Su-17 Fitter Attack Aircraft
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Su-25 Frogfoot Attack Aircraft
            //----------------------------------------------
            WeaponProfile SU25 = new WeaponProfile(
                _longName: "Su-25 Frogfoot Attack Aircraft",
                _shortName: "Su-25 Frogfoot",
                _type: WeaponType.ATT_SU25_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_ATTACK_DOGFIGHT + MEDIUM_MALUS,           // Dogfighting Rating
                _man: GameData.AC_ATTACK_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_ATTACK_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.AC_ATTACK_SURVIVE + MEDIUM_BONUS,          // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_1 + LARGE_BONUS,          // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD + LARGE_BONUS,                 // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ATT,             // Upgrade Path
                _turnAvailable: 516                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SU25.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.ATT);

            // Fill out intel stats for the Su-25 profile
            SU25.AddIntelReportStat(WeaponType.ATT_SU25_SV,       36);

            // Handle the icon profile.
            SU25.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_SU25
            };

            // Add the Su-25 profile to the database
            AddProfile(WeaponType.ATT_SU25_SV, SU25);
            //----------------------------------------------
            // Soviet Su-25 Frogfoot Attack Aircraft
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Su-25B Frogfoot-B Attack Aircraft
            //----------------------------------------------
            WeaponProfile SU25B = new WeaponProfile(
                _longName: "Su-25B Frogfoot-B Attack Aircraft",
                _shortName: "Su-25B Frogfoot-B",
                _type: WeaponType.ATT_SU25B_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_ATTACK_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_ATTACK_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_ATTACK_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.AC_ATTACK_SURVIVE + XXLARGE_BONUS,         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_2 + LARGE_BONUS,          // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD + XLARGE_BONUS,                // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ATT,             // Upgrade Path
                _turnAvailable: 588                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SU25B.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.ATT);

            // Fill out intel stats for the Su-25B profile
            SU25B.AddIntelReportStat(WeaponType.ATT_SU25B_SV,     36);

            // Handle the icon profile.
            SU25B.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_SU25B
            };

            // Add the Su-25B profile to the database
            AddProfile(WeaponType.ATT_SU25B_SV, SU25B);
            //----------------------------------------------
            // Soviet Su-25B Frogfoot-B Attack Aircraft
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Su-24 Fencer Bomber
            //----------------------------------------------
            WeaponProfile SU24 = new WeaponProfile(
                _longName: "Su-24 Fencer Bomber",
                _shortName: "Su-24 Fencer",
                _type: WeaponType.BMB_SU24_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_BOMBER_DOGFIGHT + XXLARGE_BONUS,          // Dogfighting Rating
                _man: GameData.AC_BOMBER_MANEUVER + LARGE_BONUS,           // Maneuverability Rating
                _topSpd: GameData.AC_BOMBER_TOPSPEED + XLARGE_BONUS,       // Top Speed Rating
                _surv: GameData.AC_BOMBER_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_1 + LARGE_BONUS,          // Ground Attack Rating
                _ol: GameData.LARGE_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ATT,             // Upgrade Path
                _turnAvailable: 432                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SU24.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.ATT);

            // Fill out intel stats for the Su-24 profile
            SU24.AddIntelReportStat(WeaponType.BMB_SU24_SV,       36);

            // Handle the icon profile.
            SU24.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_SU24
            };

            // Add the Su-24 profile to the database
            AddProfile(WeaponType.BMB_SU24_SV, SU24);
            //----------------------------------------------
            // Soviet Su-24 Fencer Bomber
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Tu-16 Badger Bomber
            //----------------------------------------------
            WeaponProfile TU16 = new WeaponProfile(
                _longName: "Tu-16 Badger Bomber",
                _shortName: "Tu-16 Badger",
                _type: WeaponType.BMB_TU16_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_BOMBER_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_BOMBER_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_BOMBER_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.AC_BOMBER_SURVIVE + MEDIUM_BONUS,          // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_1,                        // Ground Attack Rating
                _ol: GameData.XLARGE_AC_LOAD,                              // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Day,                // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,               // Unit Silhouette
                _upgradePath: UpgradePath.BMB,            // Upgrade Path
                _turnAvailable: 192                       // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            TU16.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.BMB);

            // Fill out intel stats for the Tu-16 profile
            TU16.AddIntelReportStat(WeaponType.BMB_TU16_SV,       24);

            // Handle the icon profile.
            TU16.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_TU16
            };

            // Add the Tu-16 profile to the database
            AddProfile(WeaponType.BMB_TU16_SV, TU16);
            //----------------------------------------------
            // Soviet Tu-16 Badger Bomber
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Tu-22 Blinder Bomber
            //----------------------------------------------
            WeaponProfile TU22 = new WeaponProfile(
                _longName: "Tu-22 Blinder Bomber",
                _shortName: "Tu-22 Blinder",
                _type: WeaponType.BMB_TU22_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_BOMBER_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_BOMBER_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_BOMBER_TOPSPEED + XLARGE_BONUS,       // Top Speed Rating
                _surv: GameData.AC_BOMBER_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_1 + XLARGE_BONUS,         // Ground Attack Rating
                _ol: GameData.LARGE_AC_LOAD + SMALL_MALUS,                 // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,               // Unit Silhouette
                _upgradePath: UpgradePath.BMB,            // Upgrade Path
                _turnAvailable: 288                       // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            TU22.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.BMB);

            // Fill out intel stats for the Tu-22 profile
            TU22.AddIntelReportStat(WeaponType.BMB_TU22_SV,       24);

            // Handle the icon profile.
            TU22.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_TU22
            };

            // Add the Tu-22 profile to the database
            AddProfile(WeaponType.BMB_TU22_SV, TU22);
            //----------------------------------------------
            // Soviet Tu-22 Blinder Bomber
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Tu-22M3 Backfire-C Strategic Bomber
            //----------------------------------------------
            WeaponProfile TU22M3 = new WeaponProfile(
                _longName: "Tu-22M3 Backfire-C Strategic Bomber",
                _shortName: "Tu-22M3 Backfire",
                _type: WeaponType.BMB_TU22M3_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_BOMBER_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_BOMBER_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_HIGHSPEED_RUSSIAN + SMALL_MALUS,      // Top Speed Rating
                _surv: GameData.AC_BOMBER_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_2 + LARGE_BONUS,          // Ground Attack Rating
                _ol: GameData.LARGE_AC_LOAD + LARGE_BONUS,                 // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,               // Unit Silhouette
                _upgradePath: UpgradePath.BMB,            // Upgrade Path
                _turnAvailable: 480                       // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            TU22M3.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.BMB);

            // Fill out intel stats for the Tu-22M3 profile
            TU22M3.AddIntelReportStat(WeaponType.BMB_TU22M3_SV,   24);

            // Handle the icon profile.
            TU22M3.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_TU22M3
            };

            // Add the Tu-22M3 profile to the database
            AddProfile(WeaponType.BMB_TU22M3_SV, TU22M3);
            //----------------------------------------------
            // Soviet Tu-22M3 Backfire-C Strategic Bomber
            //----------------------------------------------

            //----------------------------------------------
            // Soviet MiG-25R Foxbat-B Reconnaissance Aircraft
            //----------------------------------------------
            WeaponProfile MIG25R = new WeaponProfile(
                _longName: "MiG-25R Foxbat-B Reconnaissance Aircraft",
                _shortName: "MiG-25R Foxbat-B",
                _type: WeaponType.RCNA_MIG25R_SV,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_HIGHSPEED_RUSSIAN + MEDIUM_BONUS,     // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_0,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.RCNA,            // Upgrade Path
                _turnAvailable: 384                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MIG25R.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.RCNA);

            // Fill out intel stats for the MiG-25R profile
            MIG25R.AddIntelReportStat(WeaponType.RCNA_MIG25R_SV,  12);

            // Handle the icon profile.
            MIG25R.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Mig25R
            };

            // Add the MiG-25R profile to the database
            AddProfile(WeaponType.RCNA_MIG25R_SV, MIG25R);
            //----------------------------------------------
            // Soviet MiG-25R Foxbat-B Reconnaissance Aircraft
            //----------------------------------------------

            #endregion // Jets

            #region Trucks and Naval

            //----------------------------------------------
            // Soviet Generic Truck
            //----------------------------------------------
            WeaponProfile TRK_GEN = new WeaponProfile(
                _longName: "Generic Transport Truck",
                _shortName: "Transport Truck",
                _type: WeaponType.TRK_GEN_SV,
                _hardAtt: GameData.BASE_APC_HARD_ATTACK + LARGE_MALUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE + LARGE_MALUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK + LARGE_MALUS,     // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE + LARGE_MALUS,    // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,                  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,                  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MOT_UNIT,                   // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium                // Unit Silhouette
            );

            // Handle the icon profile.
            TRK_GEN.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.SV_Truck_W,
                NW = SpriteManager.SV_Truck_NW,
                SW = SpriteManager.SV_Truck_SW
            };

            // Add the Truck profile to the database
            AddProfile(WeaponType.TRK_GEN_SV, TRK_GEN);
            //----------------------------------------------
            // Soviet Generic Truck
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Naval Transport Flotilla
            //----------------------------------------------
            WeaponProfile NAVAL = new WeaponProfile(
                _longName: "Transport Flotilla",
                _shortName: "Transports",
                _type: WeaponType.TRN_NAVAL,
                _hardAtt: GameData.BASE_APC_HARD_ATTACK + LARGE_MALUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE + LARGE_MALUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK + LARGE_MALUS,     // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE + LARGE_MALUS,    // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,                  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,                  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.NAVAL_UNIT,                 // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large                 // Unit Silhouette
            );

            // Handle the icon profile.
            NAVAL.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_NavalTransport
            };

            // Add the Naval Transport profile to the database
            AddProfile(WeaponType.TRN_NAVAL, NAVAL);
            //----------------------------------------------
            // Soviet Naval Transport Flotilla
            //----------------------------------------------

            #endregion

            #region Infantry Units

            //----------------------------------------------
            // Soviet Regular Infantry
            //----------------------------------------------
            WeaponProfile INF_REG = new WeaponProfile(
                _longName: "Regular Infantry",
                _shortName: "Regulars",
                _type: WeaponType.INF_REG_SV,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Fill out intel stats for the Regular Infantry profile (Soviet MRR BTR-70)
            INF_REG.AddIntelReportStat(WeaponType.Personnel,         2523);
            INF_REG.AddIntelReportStat(WeaponType.SPAAA_ZSU57_SV,       4);
            INF_REG.AddIntelReportStat(WeaponType.SPSAM_9K31_SV,        4);
            INF_REG.AddIntelReportStat(WeaponType.SPA_2S1_SV,          18);
            INF_REG.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,     12);
            INF_REG.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,    12);
            INF_REG.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,        12);
            INF_REG.AddIntelReportStat(WeaponType.AT_ATGM,             16);
            INF_REG.AddIntelReportStat(WeaponType.MANPAD_STRELA,          30);

            // Handle the icon profile.
            INF_REG.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Regulars
            };

            // Add the Regular Infantry profile to the database
            AddProfile(WeaponType.INF_REG_SV, INF_REG);
            //----------------------------------------------
            // Soviet Regular Infantry
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Airborne Infantry
            //----------------------------------------------
            WeaponProfile INF_AB = new WeaponProfile(
                _longName: "Airborne Infantry",
                _shortName: "Airborne",
                _type: WeaponType.INF_AB_SV,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Fill out intel stats for the Airborne Infantry profile (Soviet VDV BMD-1)
            INF_AB.AddIntelReportStat(WeaponType.Personnel,          2250);
            INF_AB.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,     18);
            INF_AB.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,      18);
            INF_AB.AddIntelReportStat(WeaponType.AT_ATGM,              12);
            INF_AB.AddIntelReportStat(WeaponType.MANPAD_STRELA,           45);
            INF_AB.AddIntelReportStat(WeaponType.AAA_GEN_SV,            6);

            // Handle the icon profile.
            INF_AB.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Airborne
            };

            // Add the Airborne Infantry profile to the database
            AddProfile(WeaponType.INF_AB_SV, INF_AB);
            //----------------------------------------------
            // Soviet Airborne Infantry
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Air-Mobile Infantry
            //----------------------------------------------
            WeaponProfile INF_AM = new WeaponProfile(
                _longName: "Air-Mobile Infantry",
                _shortName: "Air-Mobile",
                _type: WeaponType.INF_AM_SV,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Fill out intel stats for the Air-Mobile Infantry profile (Soviet AAR MT-LB)
            INF_AM.AddIntelReportStat(WeaponType.Personnel,          2300);
            INF_AM.AddIntelReportStat(WeaponType.ART_LIGHT_SV,        18);
            INF_AM.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,     12);
            INF_AM.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,    12);
            INF_AM.AddIntelReportStat(WeaponType.AT_ATGM,             14);
            INF_AM.AddIntelReportStat(WeaponType.MANPAD_STRELA,          45);
            INF_AM.AddIntelReportStat(WeaponType.AAA_GEN_SV,           2);
            INF_AM.AddIntelReportStat(WeaponType.HEL_MI8T_SV,        166);

            // Handle the icon profile.
            INF_AM.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_AirMobile
            };

            // Add the Air-Mobile Infantry profile to the database
            AddProfile(WeaponType.INF_AM_SV, INF_AM);
            //----------------------------------------------
            // Soviet Air-Mobile Infantry
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Marine Infantry
            //----------------------------------------------
            WeaponProfile INF_MAR = new WeaponProfile(
                _longName: "Marine Infantry",
                _shortName: "Marines",
                _type: WeaponType.INF_MAR_SV,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Fill out intel stats for the Marine Infantry profile (Soviet Naval BTR-70)
            INF_MAR.AddIntelReportStat(WeaponType.Personnel,         2750);
            INF_MAR.AddIntelReportStat(WeaponType.SPAAA_ZSU57_SV,       4);
            INF_MAR.AddIntelReportStat(WeaponType.SPSAM_9K31_SV,        4);
            INF_MAR.AddIntelReportStat(WeaponType.SPA_2S1_SV,          18);
            INF_MAR.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,     12);
            INF_MAR.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,    12);
            INF_MAR.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,        12);
            INF_MAR.AddIntelReportStat(WeaponType.AT_ATGM,             12);
            INF_MAR.AddIntelReportStat(WeaponType.MANPAD_STRELA,          36);

            // Handle the icon profile.
            // Note- No Marines-specific sprite exists yet.
            INF_MAR.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Marines
            };

            // Add the Marine Infantry profile to the database
            AddProfile(WeaponType.INF_MAR_SV, INF_MAR);
            //----------------------------------------------
            // Soviet Marine Infantry
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Special Forces (Spetsnaz)
            //----------------------------------------------
            WeaponProfile INF_SPEC = new WeaponProfile(
                _longName: "Special Forces Infantry",
                _shortName: "Spetsnaz",
                _type: WeaponType.INF_SPEC_SV,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK + MEDIUM_BONUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK + MEDIUM_BONUS,     // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Fill out intel stats for the Spetsnaz profile (Soviet AAR MT-LB)
            INF_SPEC.AddIntelReportStat(WeaponType.Personnel,       2300);
            INF_SPEC.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,   12);
            INF_SPEC.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,  12);
            INF_SPEC.AddIntelReportStat(WeaponType.AT_ATGM,           14);
            INF_SPEC.AddIntelReportStat(WeaponType.MANPAD_STRELA,        45);
            INF_SPEC.AddIntelReportStat(WeaponType.AAA_GEN_SV,         2);

            // Handle the icon profile.
            INF_SPEC.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Spetsnaz
            };

            // Add the Spetsnaz profile to the database
            AddProfile(WeaponType.INF_SPEC_SV, INF_SPEC);
            //----------------------------------------------
            // Soviet Special Forces (Spetsnaz)
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Combat Engineers
            //----------------------------------------------
            WeaponProfile INF_ENG = new WeaponProfile(
                _longName: "Combat Engineers",
                _shortName: "Engineers",
                _type: WeaponType.INF_ENG_SV,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK + LARGE_MALUS,      // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE + LARGE_MALUS,     // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Fill out intel stats for the Combat Engineers profile
            INF_ENG.AddIntelReportStat(WeaponType.Personnel,          340);
            INF_ENG.AddIntelReportStat(WeaponType.APC_BTR70_SV,        20);

            // Handle the icon profile.
            INF_ENG.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.SV_Engineers
            };

            // Add the Combat Engineers profile to the database
            AddProfile(WeaponType.INF_ENG_SV, INF_ENG);
            //----------------------------------------------
            // Soviet Combat Engineers
            //----------------------------------------------

            #endregion // Infantry Units
        }

        /// <summary>
        /// Add generic weapon profiles that don't fit into a specific faction category.
        /// </summary>
        private static void CreateGenericProfiles()
        {
            #region Bases

            //----------------------------------------------
            // Large Base (Airbase)
            //----------------------------------------------
            WeaponProfile BASE_LRG = new WeaponProfile(
                _longName: "Miltary Airbase",
                _shortName: "Airbase",
                _type: WeaponType.BASE_LARGE,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK + LARGE_MALUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE + LARGE_MALUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK + LARGE_MALUS,     // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE + LARGE_MALUS,    // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.STATIC_UNIT,                // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: false,                             // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large                 // Unit Silhouette
            );

            // Fill out intel stats for the Large Base
            BASE_LRG.AddIntelReportStat(WeaponType.Personnel, 3000);

            // Handle the icon profile.
            BASE_LRG.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_Airbase
            };

            // Add the Large Base profile to the database
            AddProfile(WeaponType.BASE_LARGE, BASE_LRG);
            //----------------------------------------------
            // Large Base (Airbase)
            //----------------------------------------------

            //----------------------------------------------
            // Medium Base
            //----------------------------------------------
            WeaponProfile BASE_MED = new WeaponProfile(
                _longName: "Supply Depot",
                _shortName: "Depot",
                _type: WeaponType.BASE_MEDIUM,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK + LARGE_MALUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE + LARGE_MALUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK + LARGE_MALUS,     // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE + LARGE_MALUS,    // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.STATIC_UNIT,                // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: false,                             // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.HQLevel,               // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large                 // Unit Silhouette
            );

            // Fill out intel stats for the Medium Base
            BASE_MED.AddIntelReportStat(WeaponType.Personnel, 2000);

            // Handle the icon profile.
            BASE_MED.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_Base
            };

            // Add the Medium Base profile to the database
            AddProfile(WeaponType.BASE_MEDIUM, BASE_MED);
            //----------------------------------------------
            // Medium Base
            //----------------------------------------------

            //----------------------------------------------
            // Small Base (Depot)
            //----------------------------------------------
            WeaponProfile BASE_SML = new WeaponProfile(
                _longName: "Intel Base",
                _shortName: "Intel",
                _type: WeaponType.BASE_SMALL,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK + LARGE_MALUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE + LARGE_MALUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK + LARGE_MALUS,     // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE + LARGE_MALUS,    // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.STATIC_UNIT,                // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: false,                             // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large                 // Unit Silhouette
            );

            // Fill out intel stats for the Small Base
            BASE_SML.AddIntelReportStat(WeaponType.Personnel, 1500);

            // Handle the icon profile.
            BASE_SML.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_Depot
            };

            // Add the Small Base profile to the database
            AddProfile(WeaponType.BASE_SMALL, BASE_SML);
            //----------------------------------------------
            // Small Base (Depot)
            //----------------------------------------------

            #endregion // Bases
        }
        
        /// <summary>
        /// Add Western WeaponProfiles
        /// </summary>
        private static void CreateWesternProfiles()
        {
            #region MBTs

            //----------------------------------------------
            // US M1 Abrams Main Battle Tank
            //----------------------------------------------
            WeaponProfile M1_US = new WeaponProfile(
                _longName: "M1 Abrams Main Battle Tank",       // Full name for UI display and intel reports
                _shortName: "M1 Abrams",                       // Short name for UI display and intel reports
                _type: WeaponType.TANK_M1_US,                  // Enum identifier for this profile
                _hardAtt: GameData.GEN2_TANK_HARD_ATTACK,                       // Hard Attack Rating
                _hardDef: GameData.GEN2_TANK_HARD_DEFENSE + XXLARGE_BONUS,      // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                       // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + MEDIUM_BONUS,       // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,           // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                         // NBC Rating
                _nvg: NVG_Rating.Gen2,                         // NVG Rating
                _sil: UnitSilhouette.Large,                    // Unit Silhouette
                _upgradePath: UpgradePath.TANK,                // Upgrade Path
                _turnAvailable: 504                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M1_US.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.TANK);

            // Intel stats: US Armored Brigade - M1A1 Abrams (Division 86 structure)
            M1_US.AddIntelReportStat(WeaponType.Personnel,       2200);
            M1_US.AddIntelReportStat(WeaponType.TANK_M1_US,       116);  // 2 x Tank BN (58 each)
            M1_US.AddIntelReportStat(WeaponType.IFV_M2_US,         54);  // 1 x Mech Inf BN (54 Bradleys)
            M1_US.AddIntelReportStat(WeaponType.RCN_M3_US,         18);  // Scout vehicles across battalions + brigade recon
            M1_US.AddIntelReportStat(WeaponType.APC_M113_US,       32);  // Command posts, medical, maintenance vehicles
            M1_US.AddIntelReportStat(WeaponType.AT_ATGM,           32);  // TOW missiles (Bradley + dismounted teams)
            M1_US.AddIntelReportStat(WeaponType.MANPAD_STINGER,    18);  // Stinger teams distributed across brigade
            M1_US.AddIntelReportStat(WeaponType.SPA_M109_US,       18);  // Direct support artillery battalion
            M1_US.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,  18);
            M1_US.AddIntelReportStat(WeaponType.SPAAA_M163_US,      4);  // Vulcan air defense guns

            // Handle the icon profile.
            M1_US.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.US_M1_W,
                NW = SpriteManager.US_M1_NW,
                SW = SpriteManager.US_M1_SW
            };

            // Add the M1 Abrams profile to the database
            AddProfile(WeaponType.TANK_M1_US, M1_US);
            //----------------------------------------------
            // US M1 Abrams Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // US M60A3 Patton Main Battle Tank
            //----------------------------------------------
            WeaponProfile M60_US = new WeaponProfile(
                _longName: "M60A3 Patton Main Battle Tank",    // Full name for UI display and intel reports
                _shortName: "M60A3",                           // Short name for UI display and intel reports
                _type: WeaponType.TANK_M60_US,                 // Enum identifier for this profile
                _hardAtt: GameData.GEN1_TANK_HARD_ATTACK + SMALL_BONUS,    // Hard Attack Rating
                _hardDef: GameData.GEN1_TANK_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,           // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Medium,                   // Unit Silhouette
                _upgradePath: UpgradePath.TANK,                // Upgrade Path
                _turnAvailable: 264                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M60_US.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.TANK);

            // Intel stats: US Armored Cavalry Squadron (ACR) - Corps reconnaissance squadron
            M60_US.AddIntelReportStat(WeaponType.Personnel,       1500);
            M60_US.AddIntelReportStat(WeaponType.TANK_M60_US,       41); // M60A3 tanks distributed across troops
            M60_US.AddIntelReportStat(WeaponType.RCN_M3_US,         36);  // M3 Bradley cavalry fighting vehicles
            M60_US.AddIntelReportStat(WeaponType.APC_M113_US,       18);  // Command posts, mortars, support vehicles
            M60_US.AddIntelReportStat(WeaponType.AT_ATGM,           24);  // TOW missiles (M3 Bradley + ground teams)
            M60_US.AddIntelReportStat(WeaponType.MANPAD_STINGER,    12);  // Stinger teams for air defense
            M60_US.AddIntelReportStat(WeaponType.HEL_AH64_US,       26);  // AH-64 Apache attack helicopters
            M60_US.AddIntelReportStat(WeaponType.HEL_OH58,          12);  // OH-58 scout helicopters
            M60_US.AddIntelReportStat(WeaponType.SPA_M109_US,        8);  // Organic 155mm artillery battery
            M60_US.AddIntelReportStat(WeaponType.SPAAA_M163_US,      4);  // Vulcan air defense guns

            // Handle the icon profile.
            M60_US.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.US_M60_W,
                NW = SpriteManager.US_M60_NW,
                SW = SpriteManager.US_M60_SW
            };

            // Add the M60A3 profile to the database
            AddProfile(WeaponType.TANK_M60_US, M60_US);
            //----------------------------------------------
            // US M60A3 Patton Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // FRG Leopard 1 Main Battle Tank
            //----------------------------------------------
            WeaponProfile LEO1_GE = new WeaponProfile(
                _longName: "Leopard 1 Main Battle Tank",       // Full name for UI display and intel reports
                _shortName: "Leo 1",                           // Short name for UI display and intel reports
                _type: WeaponType.TANK_LEOPARD1_GE,            // Enum identifier for this profile
                _hardAtt: GameData.GEN1_TANK_HARD_ATTACK + MEDIUM_BONUS,   // Hard Attack Rating
                _hardDef: GameData.GEN1_TANK_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,           // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.None,                         // NVG Rating
                _sil: UnitSilhouette.Medium,                   // Unit Silhouette
                _upgradePath: UpgradePath.TANK,                // Upgrade Path
                _turnAvailable: 324                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            LEO1_GE.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.TANK);

            // Intel stats: FRG Panzer Brigade - Leopard 1
            LEO1_GE.AddIntelReportStat(WeaponType.Personnel,          2200);
            LEO1_GE.AddIntelReportStat(WeaponType.TANK_LEOPARD1_GE,    116);  // 2x Panzer BN (44 each) + Mixed BN (28)
            LEO1_GE.AddIntelReportStat(WeaponType.IFV_MARDER_GE,       58);  // 1x PzGren BN (44) + Mixed BN mech company (14)
            LEO1_GE.AddIntelReportStat(WeaponType.APC_M113_US,         24);  // Command posts, medical, maintenance
            LEO1_GE.AddIntelReportStat(WeaponType.RCN_LUCHS_GE,        12);  // Brigade reconnaissance platoon
            LEO1_GE.AddIntelReportStat(WeaponType.AT_ATGM,             32);  // Milan AT teams
            LEO1_GE.AddIntelReportStat(WeaponType.MANPAD_STINGER,      24);  // Roland/Stinger air defense sections
            LEO1_GE.AddIntelReportStat(WeaponType.SPA_M109_GE,         18);  // Organic artillery battalion (155mm SP)
            LEO1_GE.AddIntelReportStat(WeaponType.SPSAM_GEPARD_GE,      8);  // Gepard air defense guns
            LEO1_GE.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,    12);  // 120mm mortars

            // Handle the icon profile.
            LEO1_GE.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.GE_Leopard1_W,
                NW = SpriteManager.GE_Leopard1_NW,
                SW = SpriteManager.GE_Leopard1_SW
            };

            // Add the Leopard 1 profile to the database
            AddProfile(WeaponType.TANK_LEOPARD1_GE, LEO1_GE);
            //----------------------------------------------
            // FRG Leopard 1 Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // FRG Leopard 2 Main Battle Tank
            //----------------------------------------------
            WeaponProfile LEO2_GE = new WeaponProfile(
                _longName: "Leopard 2 Main Battle Tank",       // Full name for UI display and intel reports
                _shortName: "Leo 2",                           // Short name for UI display and intel reports
                _type: WeaponType.TANK_LEOPARD2_GE,            // Enum identifier for this profile
                _hardAtt: GameData.GEN2_TANK_HARD_ATTACK + XLARGE_BONUS,    // Hard Attack Rating
                _hardDef: GameData.GEN2_TANK_HARD_DEFENSE + XXLARGE_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + MEDIUM_BONUS,   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,           // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                         // NBC Rating
                _nvg: NVG_Rating.Gen2,                         // NVG Rating
                _sil: UnitSilhouette.Medium,                   // Unit Silhouette
                _upgradePath: UpgradePath.TANK,                // Upgrade Path
                _turnAvailable: 492                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            LEO2_GE.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.TANK);

            // Intel stats: FRG Panzer Brigade - Leopard 2
            LEO2_GE.AddIntelReportStat(WeaponType.Personnel,          2200);
            LEO2_GE.AddIntelReportStat(WeaponType.TANK_LEOPARD2_GE,    116);  // 2x Panzer BN (44 each) + Mixed BN (28)
            LEO2_GE.AddIntelReportStat(WeaponType.IFV_MARDER_GE,       58);  // 1x PzGren BN (44) + Mixed BN mech company (14)
            LEO2_GE.AddIntelReportStat(WeaponType.APC_M113_US,         24);  // Command posts, medical, maintenance
            LEO2_GE.AddIntelReportStat(WeaponType.RCN_LUCHS_GE,        12);  // Brigade reconnaissance platoon
            LEO2_GE.AddIntelReportStat(WeaponType.AT_ATGM,             32);  // Milan AT teams
            LEO2_GE.AddIntelReportStat(WeaponType.MANPAD_STINGER,      24);  // Roland/Stinger air defense sections
            LEO2_GE.AddIntelReportStat(WeaponType.SPA_M109_GE,         18);  // Organic artillery battalion (155mm SP)
            LEO2_GE.AddIntelReportStat(WeaponType.SPSAM_GEPARD_GE,      8);  // Gepard air defense guns
            LEO2_GE.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,    12);  // 120mm mortars

            // Handle the icon profile.
            LEO2_GE.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.GE_Leopard2_W,
                NW = SpriteManager.GE_Leopard2_NW,
                SW = SpriteManager.GE_Leopard2_SW
            };

            // Add the Leopard 2 profile to the database
            AddProfile(WeaponType.TANK_LEOPARD2_GE, LEO2_GE);
            //----------------------------------------------
            // FRG Leopard 2 Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // UK Challenger 1 Main Battle Tank
            //----------------------------------------------
            WeaponProfile CHALL1_UK = new WeaponProfile(
                _longName: "Challenger 1 Main Battle Tank",    // Full name for UI display and intel reports
                _shortName: "Challenger 1",                    // Short name for UI display and intel reports
                _type: WeaponType.TANK_CHALLENGER1_UK,         // Enum identifier for this profile
                _hardAtt: GameData.GEN2_TANK_HARD_ATTACK + LARGE_BONUS,     // Hard Attack Rating
                _hardDef: GameData.GEN2_TANK_HARD_DEFENSE + XXLARGE_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE + MEDIUM_BONUS,   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,           // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                         // NBC Rating
                _nvg: NVG_Rating.Gen2,                         // NVG Rating
                _sil: UnitSilhouette.Medium,                   // Unit Silhouette
                _upgradePath: UpgradePath.TANK,                // Upgrade Path
                _turnAvailable: 540                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            CHALL1_UK.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.TANK);

            // Intel stats: UK Armoured Brigade - Challenger 1 (Heavy Type)
            CHALL1_UK.AddIntelReportStat(WeaponType.Personnel,          1880);
            CHALL1_UK.AddIntelReportStat(WeaponType.TANK_CHALLENGER1_UK,116);  // 2x Armoured Regiments (58 each)
            CHALL1_UK.AddIntelReportStat(WeaponType.IFV_WARRIOR_UK,      45);  // 1x Mechanised Infantry Battalion
            CHALL1_UK.AddIntelReportStat(WeaponType.APC_FV432,           26);  // Command, medical, support vehicles
            CHALL1_UK.AddIntelReportStat(WeaponType.SPSAM_RAPIER_UK,      8);  // Brigade reconnaissance troop (CVR(T))
            CHALL1_UK.AddIntelReportStat(WeaponType.AT_ATGM,             30);  // Milan AT (Warrior-mounted + dismounted)
            CHALL1_UK.AddIntelReportStat(WeaponType.MANPAD_STINGER,      16);  // Javelin SAM teams across battalions
            CHALL1_UK.AddIntelReportStat(WeaponType.SPA_M109_UK,         18);  // Organic Royal Artillery regiment (155mm SP)
            CHALL1_UK.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,     18);  // 81mm mortars distributed across battalions
            CHALL1_UK.AddIntelReportStat(WeaponType.MANPAD_RAPIER,        6);  // Rapier air defense missiles

            // Handle the icon profile.
            CHALL1_UK.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.UK_Challenger1_W,
                NW = SpriteManager.UK_Challenger1_NW,
                SW = SpriteManager.UK_Challenger1_SW
            };

            // Add the Challenger 1 profile to the database
            AddProfile(WeaponType.TANK_CHALLENGER1_UK, CHALL1_UK);
            //----------------------------------------------
            // UK Challenger 1 Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // French AMX-30 Main Battle Tank
            //----------------------------------------------
            WeaponProfile AMX30_FR = new WeaponProfile(
                _longName: "AMX-30 Main Battle Tank",          // Full name for UI display and intel reports
                _shortName: "AMX-30",                          // Short name for UI display and intel reports
                _type: WeaponType.TANK_AMX30_FR,               // Enum identifier for this profile
                _hardAtt: GameData.GEN1_TANK_HARD_ATTACK + LARGE_BONUS,    // Hard Attack Rating
                _hardDef: GameData.GEN1_TANK_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,           // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.None,                         // NVG Rating
                _sil: UnitSilhouette.Medium,                   // Unit Silhouette
                _upgradePath: UpgradePath.TANK,                // Upgrade Path
                _turnAvailable: 336                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            AMX30_FR.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.TANK);

            // Intel stats: French Division Blindee - AMX-30B (Armored Division)
            AMX30_FR.AddIntelReportStat(WeaponType.Personnel,        1750);
            AMX30_FR.AddIntelReportStat(WeaponType.TANK_AMX30_FR,      80);  // 2x Armored regiments (40 each)
            AMX30_FR.AddIntelReportStat(WeaponType.IFV_AMX10P,         36);  // 1x Mechanized infantry regiment
            AMX30_FR.AddIntelReportStat(WeaponType.APC_VAB_FR,         18);  // Command, support vehicles
            AMX30_FR.AddIntelReportStat(WeaponType.RCN_ERC90_FR,       12);  // Reconnaissance regiment
            AMX30_FR.AddIntelReportStat(WeaponType.AT_ATGM,             8);  // Milan AT teams
            AMX30_FR.AddIntelReportStat(WeaponType.MANPAD_MISTRAL,     18);  // Mistral SAM teams
            AMX30_FR.AddIntelReportStat(WeaponType.SPA_AUF1,           18);  // Organic artillery regiment
            AMX30_FR.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,   18);  // 120mm mortars

            // Handle the icon profile.
            AMX30_FR.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.FR_AMX30_W,
                NW = SpriteManager.FR_AMX30_NW,
                SW = SpriteManager.FR_AMX30_SW
            };

            // Add the AMX-30 profile to the database
            AddProfile(WeaponType.TANK_AMX30_FR, AMX30_FR);
            //----------------------------------------------
            // French AMX-30 Main Battle Tank
            //----------------------------------------------

            #endregion // MBTs

            #region IFVs and APCs

            //----------------------------------------------
            // US M2 Bradley Infantry Fighting Vehicle
            //----------------------------------------------
            WeaponProfile M2_US = new WeaponProfile(
                _longName: "M2 Bradley Infantry Fighting Vehicle",
                _shortName: "M2 Bradley",
                _type: WeaponType.IFV_M2_US,
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS,  // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK + SMALL_BONUS,     // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                         // NBC Rating
                _nvg: NVG_Rating.Gen2,                         // NVG Rating
                _sil: UnitSilhouette.Medium,                   // Unit Silhouette
                _upgradePath: UpgradePath.IFV,                 // Upgrade Path
                _turnAvailable: 516                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M2_US.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.IFV);

            // Add intel report stats
            M2_US.AddIntelReportStat(WeaponType.TANK_M1_US,       58);
            M2_US.AddIntelReportStat(WeaponType.IFV_M2_US,       108);
            M2_US.AddIntelReportStat(WeaponType.RCN_M3_US,        18);
            M2_US.AddIntelReportStat(WeaponType.APC_M113_US,      32);

            // Handle the icon profile.
            M2_US.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.US_M2_W,
                NW = SpriteManager.US_M2_NW,
                SW = SpriteManager.US_M2_SW
            };

            // Add the M2 Bradley profile to the database
            AddProfile(WeaponType.IFV_M2_US, M2_US);
            //----------------------------------------------
            // US M2 Bradley Infantry Fighting Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // UK Warrior Infantry Fighting Vehicle
            //----------------------------------------------
            WeaponProfile WARRIOR_UK = new WeaponProfile(
                _longName: "Warrior Infantry Fighting Vehicle",
                _shortName: "Warrior",
                _type: WeaponType.IFV_WARRIOR_UK,
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK + MEDIUM_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                         // NBC Rating
                _nvg: NVG_Rating.Gen2,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.IFV,                 // Upgrade Path
                _turnAvailable: 588                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            WARRIOR_UK.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.IFV);

            // Intel report stats
            WARRIOR_UK.AddIntelReportStat(WeaponType.TANK_CHALLENGER1_UK,     58);
            WARRIOR_UK.AddIntelReportStat(WeaponType.IFV_WARRIOR_UK,          58);
            WARRIOR_UK.AddIntelReportStat(WeaponType.APC_FV432,               18);
            WARRIOR_UK.AddIntelReportStat(WeaponType.RCN_FV105_UK,             8);

            // Handle the icon profile.
            WARRIOR_UK.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.UK_Warrior_W,
                NW = SpriteManager.UK_Warrior_NW,
                SW = SpriteManager.UK_Warrior_SW
            };

            // Add the Warrior profile to the database
            AddProfile(WeaponType.IFV_WARRIOR_UK, WARRIOR_UK);
            //----------------------------------------------
            // UK Warrior Infantry Fighting Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // FRG Marder Infantry Fighting Vehicle
            //----------------------------------------------
            WeaponProfile MARDER_GE = new WeaponProfile(
                _longName: "Marder Infantry Fighting Vehicle",
                _shortName: "Marder",
                _type: WeaponType.IFV_MARDER_GE,
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK + LARGE_BONUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.IFV,                 // Upgrade Path
                _turnAvailable: 396                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MARDER_GE.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.IFV);

            // Intel report stats
            MARDER_GE.AddIntelReportStat(WeaponType.TANK_LEOPARD1_GE,  58);
            MARDER_GE.AddIntelReportStat(WeaponType.IFV_MARDER_GE,    102);
            MARDER_GE.AddIntelReportStat(WeaponType.APC_M113_US,       32);
            MARDER_GE.AddIntelReportStat(WeaponType.RCN_LUCHS_GE,      12);

            // Handle the icon profile.
            MARDER_GE.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.GE_Marder_W,
                NW = SpriteManager.GE_Marder_NW,
                SW = SpriteManager.GE_Marder_SW
            };

            // Add the Marder profile to the database
            AddProfile(WeaponType.IFV_MARDER_GE, MARDER_GE);
            //----------------------------------------------
            // FRG Marder Infantry Fighting Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // US M113 Armored Personnel Carrier
            //----------------------------------------------
            WeaponProfile M113_US = new WeaponProfile(
                _longName: "M113 Armored Personnel Carrier",
                _shortName: "M113",
                _type: WeaponType.APC_M113_US,
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,       // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK,       // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE,      // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.APC,                 // Upgrade Path
                _turnAvailable: 264                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M113_US.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.APC);

            // Add intel report stats
            M113_US.AddIntelReportStat(WeaponType.TANK_M1_US,   58);
            M113_US.AddIntelReportStat(WeaponType.APC_M113_US, 108);
            M113_US.AddIntelReportStat(WeaponType.RCN_M3_US,    18);
            M113_US.AddIntelReportStat(WeaponType.IFV_M2_US,    32);

            // Handle the icon profile.
            M113_US.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.US_M113_W,
                NW = SpriteManager.US_M113_NW,
                SW = SpriteManager.US_M113_SW
            };

            // Add the M113 profile to the database
            AddProfile(WeaponType.APC_M113_US, M113_US);
            //----------------------------------------------
            // US M113 Armored Personnel Carrier
            //----------------------------------------------

            //----------------------------------------------
            // US HMMWV (Humvee)
            //----------------------------------------------
            WeaponProfile HUMVEE_US = new WeaponProfile(
                _longName: "HMMWV Utility Vehicle",
                _shortName: "Humvee",
                _type: WeaponType.APC_HUMVEE_US,
                _hardAtt: GameData.BASE_APC_HARD_ATTACK + SMALL_MALUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE + SMALL_MALUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK + SMALL_MALUS,     // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE + SMALL_MALUS,    // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MOT_UNIT,                       // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.None,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.APC,                 // Upgrade Path
                _turnAvailable: 552                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            HUMVEE_US.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.APC);

            // Add intel report stats
            M113_US.AddIntelReportStat(WeaponType.TANK_M60_US, 58);
            M113_US.AddIntelReportStat(WeaponType.APC_HUMVEE_US, 108);
            M113_US.AddIntelReportStat(WeaponType.RCN_M3_US, 18);
            M113_US.AddIntelReportStat(WeaponType.APC_M113_US, 32);

            // Handle the icon profile.
            HUMVEE_US.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.US_Humvee_W,
                NW = SpriteManager.US_Humvee_NW,
                SW = SpriteManager.US_Humvee_SW
            };

            // Add the Humvee profile to the database
            AddProfile(WeaponType.APC_HUMVEE_US, HUMVEE_US);
            //----------------------------------------------
            // US HMMWV (Humvee)
            //----------------------------------------------

            //----------------------------------------------
            // US LVTP-7 Amphibious Assault Vehicle
            //----------------------------------------------
            WeaponProfile LVTP7_US = new WeaponProfile(
                _longName: "LVTP-7 Amphibious Assault Vehicle",
                _shortName: "LVTP-7",
                _type: WeaponType.APC_LVTP7_US,
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,       // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK,       // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE,      // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: true,                                 // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Medium,                   // Unit Silhouette
                _upgradePath: UpgradePath.APC,                 // Upgrade Path
                _turnAvailable: 408                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            LVTP7_US.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.APC);

            // Intel report stats
            LVTP7_US.AddIntelReportStat(WeaponType.APC_LVTP7_US, 18);
            LVTP7_US.AddIntelReportStat(WeaponType.APC_HUMVEE_US, 120);
            LVTP7_US.AddIntelReportStat(WeaponType.HEL_AH1, 12);

            // Handle the icon profile.
            LVTP7_US.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.US_LVTP_W,
                NW = SpriteManager.US_LVTP_NW,
                SW = SpriteManager.US_LVTP_SW
            };

            // Add the LVTP-7 profile to the database
            AddProfile(WeaponType.APC_LVTP7_US, LVTP7_US);
            //----------------------------------------------
            // US LVTP-7 Amphibious Assault Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // French VAB Armored Personnel Carrier
            //----------------------------------------------
            WeaponProfile VAB_FR = new WeaponProfile(
                _longName: "VAB Armored Personnel Carrier",
                _shortName: "VAB",
                _type: WeaponType.APC_VAB_FR,
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,       // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK,       // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE,      // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.APC,                 // Upgrade Path
                _turnAvailable: 456                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            VAB_FR.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.APC);

            // Intel report stats
            VAB_FR.AddIntelReportStat(WeaponType.TANK_AMX30_FR, 20);
            VAB_FR.AddIntelReportStat(WeaponType.APC_VAB_FR,   135);
            VAB_FR.AddIntelReportStat(WeaponType.RCN_ERC90_FR,     12);

            // Handle the icon profile.
            VAB_FR.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.FR_M113_W,
                NW = SpriteManager.FR_M113_NW,
                SW = SpriteManager.FR_M113_SW
            };

            // Add the VAB profile to the database
            AddProfile(WeaponType.APC_VAB_FR, VAB_FR);
            //----------------------------------------------
            // French VAB Armored Personnel Carrier
            //----------------------------------------------

            #endregion // IFVs and APCs

            #region Self-Propelled Artillery

            //----------------------------------------------
            // US M109 Paladin Self-Propelled Artillery
            //----------------------------------------------
            WeaponProfile M109_US = new WeaponProfile(
                _longName: "M109 Paladin Self-Propelled Artillery",  // Full name for UI display and intel reports
                _shortName: "M109 Paladin",                          // Short name for UI display and intel reports
                _type: WeaponType.SPA_M109_US,             // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_MEDIUM,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 300                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M109_US.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPA);

            // Intel report stats
            M109_US.AddIntelReportStat(WeaponType.Personnel,  1050);
            M109_US.AddIntelReportStat(WeaponType.SPA_M109_US,  54);
            M109_US.AddIntelReportStat(WeaponType.APC_M113_US,  48);
            M109_US.AddIntelReportStat(WeaponType.MANPAD_STINGER,  12);
            M109_US.AddIntelReportStat(WeaponType.RCN_M3_US,        6);

            // Handle the icon profile.
            M109_US.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.US_M109_W,
                NW = SpriteManager.US_M109_NW,
                SW = SpriteManager.US_M109_SW,
                W_F = SpriteManager.US_M109_W_F,
                NW_F = SpriteManager.US_M109_NW_F,
                SW_F = SpriteManager.US_M109_SW_F
            };

            // Add the M109 US profile to the database
            AddProfile(WeaponType.SPA_M109_US, M109_US);
            //----------------------------------------------
            // US M109 Paladin Self-Propelled Artillery
            //----------------------------------------------

            //----------------------------------------------
            // German M109 Self-Propelled Artillery
            //----------------------------------------------
            WeaponProfile M109_GE = new WeaponProfile(
                _longName: "M109 Self-Propelled Artillery",          // Full name for UI display and intel reports
                _shortName: "M109",                                  // Short name for UI display and intel reports
                _type: WeaponType.SPA_M109_GE,             // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_MEDIUM,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 300                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M109_GE.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPA);

            // Intel report stats
            M109_GE.AddIntelReportStat(WeaponType.Personnel,    950);
            M109_GE.AddIntelReportStat(WeaponType.SPA_M109_GE,   48);
            M109_GE.AddIntelReportStat(WeaponType.IFV_MARDER_GE, 24);
            M109_GE.AddIntelReportStat(WeaponType.MANPAD_STINGER,   12);
            M109_GE.AddIntelReportStat(WeaponType.RCN_LUCHS_GE,      6);

            // Handle the icon profile.
            M109_GE.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.GE_M109_W,
                NW = SpriteManager.GE_M109_NW,
                SW = SpriteManager.GE_M109_SW,
                W_F = SpriteManager.GE_M109_W_F,
                NW_F = SpriteManager.GE_M109_NW_F,
                SW_F = SpriteManager.GE_M109_SW_F
            };

            // Add the M109 GE profile to the database
            AddProfile(WeaponType.SPA_M109_GE, M109_GE);
            //----------------------------------------------
            // German M109 Self-Propelled Artillery
            //----------------------------------------------

            //----------------------------------------------
            // French M109 Self-Propelled Artillery
            //----------------------------------------------
            WeaponProfile M109_FR = new WeaponProfile(
                _longName: "M109 Self-Propelled Artillery",          // Full name for UI display and intel reports
                _shortName: "M109",                                  // Short name for UI display and intel reports
                _type: WeaponType.SPA_M109_FR,             // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_MEDIUM,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 300                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M109_FR.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPA);

            // Intel report stats
            M109_FR.AddIntelReportStat(WeaponType.Personnel, 1050);
            M109_FR.AddIntelReportStat(WeaponType.SPA_AUF1,    48);
            M109_FR.AddIntelReportStat(WeaponType.APC_VAB_FR,  28);
            M109_FR.AddIntelReportStat(WeaponType.MANPAD_MISTRAL, 12);
            M109_FR.AddIntelReportStat(WeaponType.RCN_ERC90_FR,    6);

            // Handle the icon profile. (No dedicated French M109 sprites, using US M109)
            M109_FR.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.US_M109_W,
                NW = SpriteManager.US_M109_NW,
                SW = SpriteManager.US_M109_SW,
                W_F = SpriteManager.US_M109_W_F,
                NW_F = SpriteManager.US_M109_NW_F,
                SW_F = SpriteManager.US_M109_SW_F
            };
            // Add the M109 FR profile to the database
            AddProfile(WeaponType.SPA_M109_FR, M109_FR);
            //----------------------------------------------
            // French M109 Self-Propelled Artillery
            //----------------------------------------------

            //----------------------------------------------
            // British M109 Self-Propelled Artillery
            //----------------------------------------------
            WeaponProfile M109_UK = new WeaponProfile(
                _longName: "M109 Self-Propelled Artillery",          // Full name for UI display and intel reports
                _shortName: "M109",                                  // Short name for UI display and intel reports
                _type: WeaponType.SPA_M109_UK,             // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + XLARGE_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_MEDIUM,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 300                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M109_UK.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPA);

            // Intel report stats
            M109_UK.AddIntelReportStat(WeaponType.Personnel, 1050);
            M109_UK.AddIntelReportStat(WeaponType.SPA_M109_UK, 54);
            M109_UK.AddIntelReportStat(WeaponType.APC_FV432,   48);
            M109_UK.AddIntelReportStat(WeaponType.MANPAD_RAPIER, 12);
            M109_UK.AddIntelReportStat(WeaponType.RCN_FV105_UK, 6);

            // Handle the icon profile.
            M109_UK.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.UK_M109_W,
                NW = SpriteManager.UK_M109_NW,
                SW = SpriteManager.UK_M109_SW,
                W_F = SpriteManager.UK_M109_W_F,
                NW_F = SpriteManager.UK_M109_NW_F,
                SW_F = SpriteManager.UK_M109_SW_F
            };

            // Add the M109 UK profile to the database
            AddProfile(WeaponType.SPA_M109_UK, M109_UK);
            //----------------------------------------------
            // British M109 Self-Propelled Artillery
            //----------------------------------------------

            #endregion // Self-Propelled Artillery

            #region Artillery

            //----------------------------------------------
            // Western Light Towed Artillery
            //----------------------------------------------
            WeaponProfile ArtLightWest = new WeaponProfile(
                _longName: "Light Towed Artillery",            // Full name for UI display and intel reports
                _shortName: "Lt Artillery",                    // Short name for UI display and intel reports
                _type: WeaponType.ART_LIGHT_WEST,              // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,      // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE,     // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK,      // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE,     // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_SHORT,            // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.FOOT_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.None,                         // NBC Rating
                _nvg: NVG_Rating.None,                         // NVG Rating
                _sil: UnitSilhouette.Large,                    // Unit Silhouette
                _upgradePath: UpgradePath.ART,                 // Upgrade Path
                _turnAvailable: 144                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ArtLightWest.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.ART);

            // Intel report stats
            ArtLightWest.AddIntelReportStat(WeaponType.Personnel,   950);
            ArtLightWest.AddIntelReportStat(WeaponType.ART_105MM_FG, 54);
            ArtLightWest.AddIntelReportStat(WeaponType.ART_155MM_FG, 18);
            ArtLightWest.AddIntelReportStat(WeaponType.APC_HUMVEE_US,12);

            // Handle the icon profile.
            ArtLightWest.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_LightArt
            };

            // Add the Light Artillery profile to the database
            AddProfile(WeaponType.ART_LIGHT_WEST, ArtLightWest);
            //----------------------------------------------
            // Western Light Towed Artillery
            //----------------------------------------------

            //----------------------------------------------
            // Western Heavy Towed Artillery
            //----------------------------------------------
            WeaponProfile ArtHeavyWest = new WeaponProfile(
                _longName: "Heavy Towed Artillery",            // Full name for UI display and intel reports
                _shortName: "Hvy Artillery",                   // Short name for UI display and intel reports
                _type: WeaponType.ART_HEAVY_WEST,              // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,      // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE,     // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + MEDIUM_BONUS,  // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE,     // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_MEDIUM,           // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,        // Spotting Range
                _mmp: GameData.FOOT_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.None,                         // NBC Rating
                _nvg: NVG_Rating.None,                         // NVG Rating
                _sil: UnitSilhouette.Large,                    // Unit Silhouette
                _upgradePath: UpgradePath.ART,                 // Upgrade Path
                _turnAvailable: 144                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ArtHeavyWest.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.ART);

            // Intel report stats
            ArtHeavyWest.AddIntelReportStat(WeaponType.Personnel,   950);
            ArtHeavyWest.AddIntelReportStat(WeaponType.ART_105MM_FG, 18);
            ArtHeavyWest.AddIntelReportStat(WeaponType.ART_155MM_FG, 54);
            ArtHeavyWest.AddIntelReportStat(WeaponType.APC_HUMVEE_US,12);

            // Handle the icon profile.
            ArtHeavyWest.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_HeavyArt
            };

            // Add the Heavy Artillery profile to the database
            AddProfile(WeaponType.ART_HEAVY_WEST, ArtHeavyWest);
            //----------------------------------------------
            // Western Heavy Towed Artillery
            //----------------------------------------------

            #endregion // Artillery

            #region Rocket Artillery

            //----------------------------------------------
            // US M270 MLRS Multiple Launch Rocket System
            //----------------------------------------------
            WeaponProfile MLRS_US = new WeaponProfile(
                _longName: "M270 MLRS Multiple Launch Rocket System",  // Full name for UI display and intel reports
                _shortName: "M270 MLRS",                               // Short name for UI display and intel reports
                _type: WeaponType.ROC_MLRS_US,             // Enum identifier for this profile
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + LARGE_BONUS,    // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_BONUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_ROC_MR,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: true,                               // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.ROC,             // Upgrade Path
                _turnAvailable: 540                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MLRS_US.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.ROC);

            // Intel report stats
            MLRS_US.AddIntelReportStat(WeaponType.Personnel,  1020);
            MLRS_US.AddIntelReportStat(WeaponType.ROC_MLRS_US,  18);
            MLRS_US.AddIntelReportStat(WeaponType.APC_M113_US,  48);
            MLRS_US.AddIntelReportStat(WeaponType.MANPAD_STINGER,  12);
            MLRS_US.AddIntelReportStat(WeaponType.RCN_M3_US,        6);

            // Handle the icon profile.
            MLRS_US.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.US_MLRS_W,
                NW = SpriteManager.US_MLRS_NW,
                SW = SpriteManager.US_MLRS_SW,
                W_F = SpriteManager.US_MLRS_W_F,
                NW_F = SpriteManager.US_MLRS_NW_F,
                SW_F = SpriteManager.US_MLRS_SW_F
            };

            // Add the MLRS US profile to the database
            AddProfile(WeaponType.ROC_MLRS_US, MLRS_US);
            //----------------------------------------------
            // US M270 MLRS Multiple Launch Rocket System
            //----------------------------------------------

            #endregion // Rocket Artillery

            #region Air Defense

            //----------------------------------------------
            // US M163 Vulcan Self-Propelled Anti-Aircraft Gun
            //----------------------------------------------
            WeaponProfile M163_US = new WeaponProfile(
                _longName: "M163 Vulcan Self-Propelled Anti-Aircraft Gun",
                _shortName: "M163 Vulcan",
                _type: WeaponType.SPAAA_M163_US,
                _hardAtt: GameData.BASE_AAA_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_AAA_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_AAA_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_AAA_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_AAA_GROUND_AIR_ATTACK + SMALL_BONUS,   // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_AAA,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_AAA,          // Indirect Range
                _sr: GameData.BASE_AAA_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.AAA,             // Upgrade Path
                _turnAvailable: 372                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M163_US.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.SPAAA);

            // Intel report stats
            M163_US.AddIntelReportStat(WeaponType.Personnel,      900); // Air defense personnel
            M163_US.AddIntelReportStat(WeaponType.SPAAA_M163_US,   18); // M163 Vulcan SPAAA units
            M163_US.AddIntelReportStat(WeaponType.APC_M113_US,     24); // Command and control vehicles
            M163_US.AddIntelReportStat(WeaponType.MANPAD_STINGER,     12); // Chaparral mobile SAM systems

            // Handle the icon profile.
            M163_US.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.US_M163_W,
                NW = SpriteManager.US_M163_NW,
                SW = SpriteManager.US_M163_SW,
                W_F = SpriteManager.US_M163_W_F,
                NW_F = SpriteManager.US_M163_NW_F,
                SW_F = SpriteManager.US_M163_SW_F
            };

            // Add the M163 US profile to the database
            AddProfile(WeaponType.SPAAA_M163_US, M163_US);
            //----------------------------------------------
            // US M163 Vulcan Self-Propelled Anti-Aircraft Gun
            //----------------------------------------------

            //----------------------------------------------
            // US M48 Chaparral Self-Propelled SAM
            //----------------------------------------------
            WeaponProfile Chaparral = new WeaponProfile(
                _longName: "M48 Chaparral Self-Propelled SAM System",
                _shortName: "M48 Chaparral",
                _type: WeaponType.SPSAM_CHAP_US,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                   // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK,                 // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_SAM,                         // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SAM,          // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 372                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            Chaparral.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPSAM);

            // Intel report stats
            Chaparral.AddIntelReportStat(WeaponType.Personnel,    900); // Air defense personnel
            Chaparral.AddIntelReportStat(WeaponType.SPSAM_CHAP_US, 18); // M163 Vulcan SPAAA units
            Chaparral.AddIntelReportStat(WeaponType.APC_M113_US,   24); // Command and control vehicles
            Chaparral.AddIntelReportStat(WeaponType.SPAAA_M163_US,  4); // Chaparral mobile SAM systems

            // Handle the icon profile.
            Chaparral.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.US_Chaparral_W,
                NW = SpriteManager.US_Chaparral_NW,
                SW = SpriteManager.US_Chaparral_SW,
                W_F = SpriteManager.US_Chaparral_W_F,
                NW_F = SpriteManager.US_Chaparral_NW_F,
                SW_F = SpriteManager.US_Chaparral_SW_F
            };

            // Add the Chaparral profile to the database
            AddProfile(WeaponType.SPSAM_CHAP_US, Chaparral);
            //----------------------------------------------
            // US M48 Chaparral Self-Propelled SAM
            //----------------------------------------------

            //----------------------------------------------
            // US MIM-23 Hawk Strategic SAM System
            //----------------------------------------------
            WeaponProfile Hawk_US = new WeaponProfile(
                _longName: "MIM-23 Hawk Strategic SAM System",
                _shortName: "MIM-23 Hawk",
                _type: WeaponType.SAM_HAWK_US,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                           // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE,                          // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                           // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE,                          // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK + LARGE_BONUS,           // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_SAM,                                 // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SAM + LARGE_BONUS,    // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE + LARGE_BONUS,  // Spotting Range
                _mmp: GameData.STATIC_UNIT,                // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 264                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            Hawk_US.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.SAM);

            // Intel report stats
            Hawk_US.AddIntelReportStat(WeaponType.Personnel,    1100); // Air defense personnel
            Hawk_US.AddIntelReportStat(WeaponType.SAM_HAWK_US,    18); // Hawk SAM batteries
            Hawk_US.AddIntelReportStat(WeaponType.SPAAA_M163_US,   4); // Vulcan air defense guns
            Hawk_US.AddIntelReportStat(WeaponType.APC_M113_US,    24); // Chaparral mobile SAM systems

            // Handle the icon profile.
            Hawk_US.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_Hawk
            };

            // Add the Hawk US profile to the database
            AddProfile(WeaponType.SAM_HAWK_US, Hawk_US);
            //----------------------------------------------
            // US MIM-23 Hawk Strategic SAM System
            //----------------------------------------------

            //----------------------------------------------
            // German Flakpanzer Gepard SPSAM
            //----------------------------------------------
            WeaponProfile Gepard_GE = new WeaponProfile(
                _longName: "Flakpanzer Gepard Self-Propelled Anti-Aircraft Gun",
                _shortName: "Gepard",
                _type: WeaponType.SPSAM_GEPARD_GE,
                _hardAtt: GameData.BASE_AAA_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_AAA_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_AAA_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_AAA_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_AAA_GROUND_AIR_ATTACK + SMALL_BONUS,   // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_AAA,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_MEDIUM + SMALL_MALUS,       // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE + SMALL_MALUS,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.AAA,             // Upgrade Path
                _turnAvailable: 456                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            Gepard_GE.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.SPAAA);

            // Intel report stats
            Gepard_GE.AddIntelReportStat(WeaponType.Personnel,      1100);
            Gepard_GE.AddIntelReportStat(WeaponType.SAM_HAWK_US,       4);
            Gepard_GE.AddIntelReportStat(WeaponType.SPSAM_GEPARD_GE,  18);
            Gepard_GE.AddIntelReportStat(WeaponType.IFV_MARDER_GE,    24);
            Gepard_GE.AddIntelReportStat(WeaponType.RCN_LUCHS_GE,        12);

            // Handle the icon profile.
            Gepard_GE.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.GE_Gepard_W,
                NW = SpriteManager.GE_Gepard_NW,
                SW = SpriteManager.GE_Gepard_SW,
                W_F = SpriteManager.GE_Gepard_W_F,
                NW_F = SpriteManager.GE_Gepard_NW_F,
                SW_F = SpriteManager.GE_Gepard_SW_F
            };

            // Add the Gepard GE profile to the database
            AddProfile(WeaponType.SPSAM_GEPARD_GE, Gepard_GE);
            //----------------------------------------------
            // German Flakpanzer Gepard SPSAM
            //----------------------------------------------

            //----------------------------------------------
            // French Roland SPAAA
            //----------------------------------------------
            WeaponProfile Roland_FR = new WeaponProfile(
                _longName: "Roland Self-Propelled Anti-Aircraft Gun",
                _shortName: "Roland",
                _type: WeaponType.SPAAA_ROLAND_FR,
                _hardAtt: GameData.BASE_AAA_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_AAA_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_AAA_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_AAA_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_AAA_GROUND_AIR_ATTACK + SMALL_BONUS,   // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_AAA,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_AAA,          // Indirect Range
                _sr: GameData.BASE_AAA_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 468                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            Roland_FR.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.SPSAM);

            // Intel report stats
            Roland_FR.AddIntelReportStat(WeaponType.Personnel,      950);
            Roland_FR.AddIntelReportStat(WeaponType.SPAAA_ROLAND_FR, 18);
            Roland_FR.AddIntelReportStat(WeaponType.APC_VAB_FR,      24);
            Roland_FR.AddIntelReportStat(WeaponType.RCN_ERC90_FR,       12);

            // Handle the icon profile.
            Roland_FR.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.FR_Roland_W,
                NW = SpriteManager.FR_Roland_NW,
                SW = SpriteManager.FR_Roland_SW,
                W_F = SpriteManager.FR_Roland_W_F,
                NW_F = SpriteManager.FR_Roland_NW_F,
                SW_F = SpriteManager.FR_Roland_SW_F
            };

            // Add the Gepard FR profile to the database
            AddProfile(WeaponType.SPAAA_ROLAND_FR, Roland_FR);
            //----------------------------------------------
            // French Roland SPAAA
            //----------------------------------------------

            //----------------------------------------------
            // French Crotale Self-Propelled SAM
            //----------------------------------------------
            WeaponProfile Crotale = new WeaponProfile(
                _longName: "Crotale Self-Propelled SAM System",
                _shortName: "Crotale",
                _type: WeaponType.SPSAM_CROTALE_FR,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK + MEDIUM_BONUS,   // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_SAM,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SAM,          // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 396                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            Crotale.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPSAM);

            // Intel report stats
            Crotale.AddIntelReportStat(WeaponType.Personnel,    1050);
            Crotale.AddIntelReportStat(WeaponType.SPSAM_CROTALE_FR,  18);
            Crotale.AddIntelReportStat(WeaponType.APC_VAB_FR,     24);
            Crotale.AddIntelReportStat(WeaponType.SPAAA_ROLAND_FR, 4);
            Crotale.AddIntelReportStat(WeaponType.RCN_ERC90_FR,      12);

            // Handle the icon profile. (Using FR Roland sprites)
            Crotale.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.US_Chaparral_W,
                NW = SpriteManager.US_Chaparral_NW,
                SW = SpriteManager.US_Chaparral_SW,
                W_F = SpriteManager.US_Chaparral_W_F,
                NW_F = SpriteManager.US_Chaparral_NW_F,
                SW_F = SpriteManager.US_Chaparral_SW_F
            };

            // Add the Crotale profile to the database
            AddProfile(WeaponType.SPSAM_CROTALE_FR, Crotale);
            //----------------------------------------------
            // French Crotale Self-Propelled SAM
            //----------------------------------------------

            //----------------------------------------------
            // British Tracked Rapier Self-Propelled SAM
            //----------------------------------------------
            WeaponProfile Rapier_SP = new WeaponProfile(
                _longName: "Tracked Rapier Self-Propelled SAM System",
                _shortName: "Tracked Rapier",
                _type: WeaponType.SPSAM_RAPIER_UK,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK + SMALL_BONUS,    // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_SAM,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SAM,          // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 432                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            Rapier_SP.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPSAM);

            // Intel report stats
            Rapier_SP.AddIntelReportStat(WeaponType.Personnel,      1050);
            Rapier_SP.AddIntelReportStat(WeaponType.SPSAM_RAPIER_UK,     18);
            Rapier_SP.AddIntelReportStat(WeaponType.APC_FV432,        24);
            Rapier_SP.AddIntelReportStat(WeaponType.SPAAA_M163_US,     4);
            Rapier_SP.AddIntelReportStat(WeaponType.RCN_FV105_UK,     12);

            // Handle the icon profile. (No dedicated UK Rapier sprites, using FR Roland)
            Rapier_SP.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.US_Chaparral_W,
                NW = SpriteManager.US_Chaparral_NW,
                SW = SpriteManager.US_Chaparral_SW,
                W_F = SpriteManager.US_Chaparral_W_F,
                NW_F = SpriteManager.US_Chaparral_NW_F,
                SW_F = SpriteManager.US_Chaparral_SW_F
            };

            // Add the Tracked Rapier profile to the database
            AddProfile(WeaponType.SPSAM_RAPIER_UK, Rapier_SP);
            //----------------------------------------------
            // British Tracked Rapier Self-Propelled SAM
            //----------------------------------------------

            #endregion // Air Defense

            #region Recon

            //----------------------------------------------
            // US M3 Bradley Cavalry Fighting Vehicle
            //----------------------------------------------
            WeaponProfile M3_US = new WeaponProfile(
                _longName: "M3 Bradley Cavalry Fighting Vehicle",
                _shortName: "M3 Bradley",
                _type: WeaponType.RCN_M3_US,
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS,  // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK + SMALL_BONUS,     // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,       // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                         // NBC Rating
                _nvg: NVG_Rating.Gen2,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.RCN,                 // Upgrade Path
                _turnAvailable: 516                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M3_US.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.RCN);

            // Intel stats
            M3_US.AddIntelReportStat(WeaponType.Personnel,       680);
            M3_US.AddIntelReportStat(WeaponType.RCN_M3_US,        48);
            M3_US.AddIntelReportStat(WeaponType.APC_M113_US,       8);
            M3_US.AddIntelReportStat(WeaponType.AT_ATGM,          12);
            M3_US.AddIntelReportStat(WeaponType.MANPAD_STINGER,    6);

            // Handle the icon profile. (Using M2 Bradley sprites)
            M3_US.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.US_M2_W,
                NW = SpriteManager.US_M2_NW,
                SW = SpriteManager.US_M2_SW
            };

            // Add the M3 Bradley profile to the database
            AddProfile(WeaponType.RCN_M3_US, M3_US);
            //----------------------------------------------
            // US M3 Bradley Cavalry Fighting Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // FRG Spähpanzer Luchs Reconnaissance Vehicle
            //----------------------------------------------
            WeaponProfile LUCHS_GE = new WeaponProfile(
                _longName: "Spähpanzer Luchs Reconnaissance Vehicle",
                _shortName: "Luchs",
                _type: WeaponType.RCN_LUCHS_GE,
                 _hardAtt: GameData.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS,  // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK + SMALL_BONUS,     // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,       // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: true,                                 // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.RCN,                 // Upgrade Path
                _turnAvailable: 444                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            LUCHS_GE.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.RCN);

            // Intel stats
            LUCHS_GE.AddIntelReportStat(WeaponType.Personnel,       650);
            LUCHS_GE.AddIntelReportStat(WeaponType.RCN_LUCHS_GE,     36);
            LUCHS_GE.AddIntelReportStat(WeaponType.IFV_MARDER_GE,    12);
            LUCHS_GE.AddIntelReportStat(WeaponType.AT_ATGM,           8);
            LUCHS_GE.AddIntelReportStat(WeaponType.MANPAD_STINGER,    6);

            // Handle the icon profile. (Using Marder sprites as stand-in)
            LUCHS_GE.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.GE_Luchs_W,
                NW = SpriteManager.GE_Luchs_NW,
                SW = SpriteManager.GE_Luchs_SW
            };

            // Add the Luchs profile to the database
            AddProfile(WeaponType.RCN_LUCHS_GE, LUCHS_GE);
            //----------------------------------------------
            // FRG Spähpanzer Luchs Reconnaissance Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // UK FV105 Sultan
            //----------------------------------------------
            WeaponProfile FV105_UK = new WeaponProfile(
                _longName: "FV105 Sultan",
                _shortName: "FV105",
                _type: WeaponType.RCN_FV105_UK,
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,       // Hard Attack Rating (30mm RARDEN)
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,      // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK,       // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE,      // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,       // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.Gen1,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.RCN,                 // Upgrade Path
                _turnAvailable: 420                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            FV105_UK.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.RCN);

            // Intel stats
            FV105_UK.AddIntelReportStat(WeaponType.Personnel,       600);
            FV105_UK.AddIntelReportStat(WeaponType.RCN_FV105_UK,     36);
            FV105_UK.AddIntelReportStat(WeaponType.APC_FV432,        12);
            FV105_UK.AddIntelReportStat(WeaponType.AT_ATGM,           8);
            FV105_UK.AddIntelReportStat(WeaponType.MANPAD_RAPIER,     6);

            // Handle the icon profile. (Using Warrior sprites as stand-in)
            FV105_UK.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.UK_FV105_W,
                NW = SpriteManager.UK_FV105_NW,
                SW = SpriteManager.UK_FV105_SW
            };

            // Add the FV105 Sultan profile to the database
            AddProfile(WeaponType.RCN_FV105_UK, FV105_UK);
            //----------------------------------------------
            // UK FV105 Sultan
            //----------------------------------------------

            //----------------------------------------------
            // French ERC 90 Sagaie Reconnaissance Vehicle
            //----------------------------------------------
            WeaponProfile ERC90_FR = new WeaponProfile(
                _longName: "ERC 90 Sagaie Reconnaissance Vehicle",
                _shortName: "ERC 90",
                _type: WeaponType.RCN_ERC90_FR,
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK + XXXLARGE_BONUS, // Hard Attack Rating (90mm gun)
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,      // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,      // Ground Defense Armor Rating
                _df: 0,                                        // Dogfighting Rating
                _man: 0,                                       // Maneuverability Rating
                _topSpd: 0,                                    // Top Speed Rating
                _surv: 0,                                      // Survivability Rating
                _ga: 0,                                        // Ground Attack Rating
                _ol: 0,                                        // Ordinance Rating
                _stealth: 0,                                   // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,           // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,          // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,       // Spotting Range
                _mmp: GameData.MECH_UNIT,                      // Max Movement Points
                _isAmph: false,                                // Is Amphibious
                _isDF: false,                                  // Is DoubleFire
                _isAtt: true,                                  // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,             // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,                 // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                         // NBC Rating
                _nvg: NVG_Rating.None,                         // NVG Rating
                _sil: UnitSilhouette.Small,                    // Unit Silhouette
                _upgradePath: UpgradePath.RCN,                 // Upgrade Path
                _turnAvailable: 492                            // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ERC90_FR.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.RCN);

            // Intel stats
            ERC90_FR.AddIntelReportStat(WeaponType.Personnel,       600);
            ERC90_FR.AddIntelReportStat(WeaponType.RCN_ERC90_FR,        36);
            ERC90_FR.AddIntelReportStat(WeaponType.APC_VAB_FR,       12);
            ERC90_FR.AddIntelReportStat(WeaponType.AT_ATGM,           8);
            ERC90_FR.AddIntelReportStat(WeaponType.MANPAD_MISTRAL,      6);

            // Handle the icon profile. (Using French M113 sprites as stand-in)
            ERC90_FR.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.FR_ERC90_W,
                NW = SpriteManager.FR_ERC90_NW,
                SW = SpriteManager.FR_ERC90_SW
            };

            // Add the ERC 90 profile to the database
            AddProfile(WeaponType.RCN_ERC90_FR, ERC90_FR);
            //----------------------------------------------
            // French ERC 90 Sagaie Reconnaissance Vehicle
            //----------------------------------------------

            #endregion // Recon

            #region Helicopters

            //----------------------------------------------
            // US AH-64 Apache Attack Helicopter
            //----------------------------------------------
            WeaponProfile AH64 = new WeaponProfile(
                _longName: "AH-64 Apache Attack Helicopter",
                _shortName: "AH-64 Apache",
                _type: WeaponType.HEL_AH64_US,
                _hardAtt: GameData.BASE_HEL_HARD_ATTACK + XXXLARGE_BONUS,  // Hard Attack Rating (Hellfire missiles)
                _hardDef: GameData.BASE_HEL_HARD_DEFENSE + SMALL_BONUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_HEL_SOFT_ATTACK + MEDIUM_BONUS,    // Soft Attack Rating
                _softDef: GameData.BASE_HEL_SOFT_DEFENSE + SMALL_BONUS,    // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_HELO,        // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.HELO_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.HEL,            // Upgrade Path
                _turnAvailable: 576                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            AH64.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.HEL);

            // Intel stats
            AH64.AddIntelReportStat(WeaponType.Personnel,       475);
            AH64.AddIntelReportStat(WeaponType.HEL_AH64_US,      54);
            AH64.AddIntelReportStat(WeaponType.HEL_OH58,         24);

            // Handle the icon profile.
            AH64.IconProfile = new RegimentIconProfile(RegimentIconType.Helo_Animation)
            {
                W = SpriteManager.US_AH64_Frame0,
                NW = SpriteManager.US_AH64_Frame1,
                SW = SpriteManager.US_AH64_Frame2,
                W_F = SpriteManager.US_AH64_Frame3,
                NW_F = SpriteManager.US_AH64_Frame4,
                SW_F = SpriteManager.US_AH64_Frame5
            };

            // Add the AH-64 Apache profile to the database
            AddProfile(WeaponType.HEL_AH64_US, AH64);
            //----------------------------------------------
            // US AH-64 Apache Attack Helicopter
            //----------------------------------------------

            //----------------------------------------------
            // US UH-60 Black Hawk Transport Helicopter
            //----------------------------------------------
            WeaponProfile UH60 = new WeaponProfile(
                _longName: "UH-60 Black Hawk Transport Helicopter",
                _shortName: "UH-60 Black Hawk",
                _type: WeaponType.HEL_UH60_US,
                _hardAtt: GameData.BASE_HEL_HARD_ATTACK + MEDIUM_MALUS,   // Hard Attack Rating
                _hardDef: GameData.BASE_HEL_HARD_DEFENSE + MEDIUM_MALUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_HEL_SOFT_ATTACK + MEDIUM_MALUS,   // Soft Attack Rating
                _softDef: GameData.BASE_HEL_SOFT_DEFENSE + MEDIUM_MALUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_HELO,        // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.HELO_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.HELT,            // Upgrade Path
                _turnAvailable: 492                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            UH60.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.HELT);

            // Intel stats
            UH60.AddIntelReportStat(WeaponType.HEL_UH60_US,  109);
            UH60.AddIntelReportStat(WeaponType.HEL_AH64_US,   18);
            UH60.AddIntelReportStat(WeaponType.HEL_OH58,      24);

            // Handle the icon profile.
            UH60.IconProfile = new RegimentIconProfile(RegimentIconType.Helo_Animation)
            {
                W = SpriteManager.US_UH60_Frame0,
                NW = SpriteManager.US_UH60_Frame1,
                SW = SpriteManager.US_UH60_Frame2,
                W_F = SpriteManager.US_UH60_Frame3,
                NW_F = SpriteManager.US_UH60_Frame4,
                SW_F = SpriteManager.US_UH60_Frame5
            };

            // Add the UH-60 Black Hawk profile to the database
            AddProfile(WeaponType.HEL_UH60_US, UH60);
            //----------------------------------------------
            // US UH-60 Black Hawk Transport Helicopter
            //----------------------------------------------

            //----------------------------------------------
            // FRG Bo 105 Light Attack Helicopter
            //----------------------------------------------
            WeaponProfile BO105 = new WeaponProfile(
                _longName: "Bo 105 Light Attack Helicopter",
                _shortName: "Bo 105",
                _type: WeaponType.HEL_BO105_GE,
                _hardAtt: GameData.BASE_HEL_HARD_ATTACK + MEDIUM_BONUS,    // Hard Attack Rating (HOT missiles)
                _hardDef: GameData.BASE_HEL_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_HEL_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_HEL_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_HELO,        // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.HELO_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.HEL,            // Upgrade Path
                _turnAvailable: 492                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BO105.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.HEL);

            // Intel stats
            BO105.AddIntelReportStat(WeaponType.Personnel,       475);
            BO105.AddIntelReportStat(WeaponType.HEL_BO105_GE,     54);

            // Handle the icon profile.
            BO105.IconProfile = new RegimentIconProfile(RegimentIconType.Helo_Animation)
            {
                W = SpriteManager.GE_BO105_Frame0,
                NW = SpriteManager.GE_BO105_Frame1,
                SW = SpriteManager.GE_BO105_Frame2,
                W_F = SpriteManager.GE_BO105_Frame3,
                NW_F = SpriteManager.GE_BO105_Frame4,
                SW_F = SpriteManager.GE_BO105_Frame5
            };

            // Add the Bo 105 profile to the database
            AddProfile(WeaponType.HEL_BO105_GE, BO105);
            //----------------------------------------------
            // FRG Bo 105 Light Attack Helicopter
            //----------------------------------------------

            //----------------------------------------------
            // US AH-1 Cobra Attack Helicopter
            //----------------------------------------------
            WeaponProfile AH1 = new WeaponProfile(
                _longName: "AH-1 Cobra Attack Helicopter",
                _shortName: "AH-1 Cobra",
                _type: WeaponType.HEL_AH1,
                _hardAtt: GameData.BASE_HEL_HARD_ATTACK + SMALL_BONUS,    // Hard Attack Rating (TOW missiles)
                _hardDef: GameData.BASE_HEL_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_HEL_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_HEL_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_HELO,        // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.HELO_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.HEL,            // Upgrade Path
                _turnAvailable: 348                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            AH1.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.HEL);

            // Intel stats
            AH1.AddIntelReportStat(WeaponType.Personnel,   475);
            AH1.AddIntelReportStat(WeaponType.HEL_AH1,      54);

            // Handle the icon profile.
            // Note- No dedicated AH-1 sprites exist yet.
            AH1.IconProfile = new RegimentIconProfile(RegimentIconType.Helo_Animation)
            {
                W = SpriteManager.US_AH64_Frame0,
                NW = SpriteManager.US_AH64_Frame1,
                SW = SpriteManager.US_AH64_Frame2,
                W_F = SpriteManager.US_AH64_Frame3,
                NW_F = SpriteManager.US_AH64_Frame4,
                SW_F = SpriteManager.US_AH64_Frame5
            };

            // Add the AH-1 Cobra profile to the database
            AddProfile(WeaponType.HEL_AH1, AH1);
            //----------------------------------------------
            // US AH-1 Cobra Attack Helicopter
            //----------------------------------------------

            #endregion // Helicopters

            #region Jets

            //----------------------------------------------
            // US F-15 Eagle Air Superiority Fighter
            //----------------------------------------------
            WeaponProfile F15 = new WeaponProfile(
                _longName: "F-15 Eagle Air Superiority Fighter",
                _shortName: "F-15 Eagle",
                _type: WeaponType.FGT_F15_US,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.MID_FGT_DOGFIGHT + XXXLARGE_BONUS,           // Dogfighting Rating
                _man: GameData.MID_FGT_MANEUVER + LARGE_BONUS,             // Maneuverability Rating
                _topSpd: GameData.MID_FGT_TOPSPEED + MEDIUM_BONUS,         // Top Speed Rating
                _surv: GameData.MID_FGT_SURVIVE + XLARGE_BONUS,            // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_2,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD + LARGE_BONUS,                 // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_SUPERIOR,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,            // Upgrade Path
                _turnAvailable: 456                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            F15.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.FGT);

            // Intel stats
            F15.AddIntelReportStat(WeaponType.FGT_F15_US,     36);

            // Handle the icon profile.
            F15.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_F15
            };

            // Add the F-15 Eagle profile to the database
            AddProfile(WeaponType.FGT_F15_US, F15);
            //----------------------------------------------
            // US F-15 Eagle Air Superiority Fighter
            //----------------------------------------------

            //----------------------------------------------
            // US F-16 Fighting Falcon Multi-Role Fighter
            //----------------------------------------------
            WeaponProfile F16 = new WeaponProfile(
                _longName: "F-16 Fighting Falcon Multi-Role Fighter",
                _shortName: "F-16 Falcon",
                _type: WeaponType.FGT_F16_US,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.MID_FGT_DOGFIGHT + XLARGE_BONUS,             // Dogfighting Rating
                _man: GameData.MID_FGT_MANEUVER + XLARGE_BONUS,            // Maneuverability Rating
                _topSpd: GameData.MID_FGT_TOPSPEED + SMALL_BONUS,          // Top Speed Rating
                _surv: GameData.MID_FGT_SURVIVE + SMALL_BONUS,             // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_2,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD + MEDIUM_BONUS,                // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ADVANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,            // Upgrade Path
                _turnAvailable: 480                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            F16.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.FGT);

            // Intel stats
            F16.AddIntelReportStat(WeaponType.FGT_F16_US,     36);

            // Handle the icon profile.
            F16.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_F16
            };

            // Add the F-16 Fighting Falcon profile to the database
            AddProfile(WeaponType.FGT_F16_US, F16);
            //----------------------------------------------
            // US F-16 Fighting Falcon Multi-Role Fighter
            //----------------------------------------------

            //----------------------------------------------
            // US F-4 Phantom II Fighter
            //----------------------------------------------
            WeaponProfile F4_US = new WeaponProfile(
                _longName: "F-4 Phantom II Fighter",
                _shortName: "F-4 Phantom",
                _type: WeaponType.FGT_F4_US,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT + MEDIUM_BONUS,           // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.EARLY_FGT_TOPSPEED + MEDIUM_BONUS,       // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE + MEDIUM_BONUS,          // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,            // Upgrade Path
                _turnAvailable: 276                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            F4_US.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.FGT);

            // Intel stats
            F4_US.AddIntelReportStat(WeaponType.FGT_F4_US,     36);

            // Handle the icon profile.
            F4_US.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_F4
            };

            // Add the F-4 Phantom profile to the database
            AddProfile(WeaponType.FGT_F4_US, F4_US);
            //----------------------------------------------
            // US F-4 Phantom II Fighter
            //----------------------------------------------

            //----------------------------------------------
            // US F-14 Tomcat Fleet Defense Fighter
            //----------------------------------------------
            WeaponProfile F14_US = new WeaponProfile(
                _longName: "F-14 Tomcat Fleet Defense Fighter",
                _shortName: "F-14 Tomcat",
                _type: WeaponType.FGT_F14_US,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.LATE_FGT_DOGFIGHT + MEDIUM_BONUS,            // Dogfighting Rating
                _man: GameData.LATE_FGT_MANEUVER + SMALL_BONUS,            // Maneuverability Rating
                _topSpd: GameData.LATE_FGT_TOPSPEED + MEDIUM_BONUS,        // Top Speed Rating
                _surv: GameData.LATE_FGT_SURVIVE,                          // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_SUPERIOR,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,            // Upgrade Path
                _turnAvailable: 432                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            F14_US.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.FGT);

            // Intel stats
            F14_US.AddIntelReportStat(WeaponType.FGT_F14_US,     36);

            // Handle the icon profile.
            F14_US.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_F14
            };

            // Add the F-14 Tomcat profile to the database
            AddProfile(WeaponType.FGT_F14_US, F14_US);
            //----------------------------------------------
            // US F-14 Tomcat Fleet Defense Fighter
            //----------------------------------------------

            //----------------------------------------------
            // UK Tornado IDS Interdictor/Strike Fighter
            //----------------------------------------------
            WeaponProfile TORNADO_IDS = new WeaponProfile(
                _longName: "Tornado IDS Interdictor/Strike Fighter",
                _shortName: "Tornado IDS",
                _type: WeaponType.FGT_TORNADO_IDS_UK,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.MID_FGT_DOGFIGHT + MEDIUM_BONUS,             // Dogfighting Rating
                _man: GameData.MID_FGT_MANEUVER,                           // Maneuverability Rating
                _topSpd: GameData.MID_FGT_TOPSPEED,                        // Top Speed Rating
                _surv: GameData.MID_FGT_SURVIVE,                           // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_2,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD + MEDIUM_BONUS,                // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,            // Upgrade Path
                _turnAvailable: 492                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            TORNADO_IDS.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.FGT);

            // Intel stats
            TORNADO_IDS.AddIntelReportStat(WeaponType.FGT_TORNADO_IDS_UK,     36);

            // Handle the icon profile.
            TORNADO_IDS.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GE_Tornado
            };

            // Add the Tornado IDS profile to the database
            AddProfile(WeaponType.FGT_TORNADO_IDS_UK, TORNADO_IDS);
            //----------------------------------------------
            // UK Tornado IDS Interdictor/Strike Fighter
            //----------------------------------------------

            //----------------------------------------------
            // UK Tornado GR.1 Strike Fighter
            //----------------------------------------------
            WeaponProfile TORNADO_GR1 = new WeaponProfile(
                _longName: "Tornado GR.1 Strike Fighter",
                _shortName: "Tornado GR.1",
                _type: WeaponType.FGT_TORNADO_GR1_US,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.MID_FGT_DOGFIGHT + LARGE_BONUS,              // Dogfighting Rating
                _man: GameData.MID_FGT_MANEUVER,                           // Maneuverability Rating
                _topSpd: GameData.MID_FGT_TOPSPEED,                        // Top Speed Rating
                _surv: GameData.MID_FGT_SURVIVE,                           // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_2,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ATT,            // Upgrade Path
                _turnAvailable: 528                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            TORNADO_GR1.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.ATT);

            // Intel stats
            TORNADO_GR1.AddIntelReportStat(WeaponType.FGT_TORNADO_GR1_US,     36);

            // Handle the icon profile.
            TORNADO_GR1.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.UK_TornadoGR1
            };

            // Add the Tornado GR.1 profile to the database
            AddProfile(WeaponType.FGT_TORNADO_GR1_US, TORNADO_GR1);
            //----------------------------------------------
            // UK Tornado GR.1 Strike Fighter
            //----------------------------------------------

            //----------------------------------------------
            // FRG F-4F Phantom Fighter
            //----------------------------------------------
            WeaponProfile F4_GE = new WeaponProfile(
                _longName: "F-4F Phantom Fighter",
                _shortName: "F-4F Phantom",
                _type: WeaponType.FGT_F4_GE,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT + MEDIUM_BONUS,           // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.EARLY_FGT_TOPSPEED + MEDIUM_BONUS,       // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE + MEDIUM_BONUS,          // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,            // Upgrade Path
                _turnAvailable: 276                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            F4_GE.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.FGT);

            // Intel stats
            F4_GE.AddIntelReportStat(WeaponType.FGT_F4_GE,     36);

            // Handle the icon profile.
            F4_GE.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GE_F4
            };

            // Add the F-4F Phantom profile to the database
            AddProfile(WeaponType.FGT_F4_GE, F4_GE);
            //----------------------------------------------
            // FRG F-4F Phantom Fighter
            //----------------------------------------------

            //----------------------------------------------
            // French Mirage 2000 Multi-Role Fighter
            //----------------------------------------------
            WeaponProfile MIRAGE2000 = new WeaponProfile(
                _longName: "Mirage 2000 Multi-Role Fighter",
                _shortName: "Mirage 2000",
                _type: WeaponType.FGT_MIRAGE2000_FR,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.MID_FGT_DOGFIGHT + XLARGE_BONUS,             // Dogfighting Rating
                _man: GameData.MID_FGT_MANEUVER + XLARGE_BONUS,            // Maneuverability Rating
                _topSpd: GameData.MID_FGT_TOPSPEED + MEDIUM_BONUS,         // Top Speed Rating
                _surv: GameData.MID_FGT_SURVIVE,                           // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_2,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,            // Upgrade Path
                _turnAvailable: 552                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MIRAGE2000.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.FGT);

            // Intel stats
            MIRAGE2000.AddIntelReportStat(WeaponType.FGT_MIRAGE2000_FR,     36);

            // Handle the icon profile.
            MIRAGE2000.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.FR_Mirage2000
            };

            // Add the Mirage 2000 profile to the database
            AddProfile(WeaponType.FGT_MIRAGE2000_FR, MIRAGE2000);
            //----------------------------------------------
            // French Mirage 2000 Multi-Role Fighter
            //----------------------------------------------

            //----------------------------------------------
            // French Mirage F1 Fighter
            //----------------------------------------------
            WeaponProfile MIRAGEF1 = new WeaponProfile(
                _longName: "Mirage F1 Fighter",
                _shortName: "Mirage F1",
                _type: WeaponType.FGT_MIRAGEF1_FR,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT + LARGE_BONUS,            // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER + SMALL_BONUS,           // Maneuverability Rating
                _topSpd: GameData.EARLY_FGT_TOPSPEED + SMALL_BONUS,        // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE + MEDIUM_BONUS,          // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_1,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,            // Upgrade Path
                _turnAvailable: 420                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MIRAGEF1.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.FGT);

            // Intel stats
            MIRAGEF1.AddIntelReportStat(WeaponType.FGT_MIRAGEF1_FR,     36);

            // Handle the icon profile.
            MIRAGEF1.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.FR_MirageF1
            };

            // Add the Mirage F1 profile to the database
            AddProfile(WeaponType.FGT_MIRAGEF1_FR, MIRAGEF1);
            //----------------------------------------------
            // French Mirage F1 Fighter
            //----------------------------------------------

            //----------------------------------------------
            // US A-10 Thunderbolt II Attack Aircraft
            //----------------------------------------------
            WeaponProfile A10 = new WeaponProfile(
                _longName: "A-10 Thunderbolt II Attack Aircraft",
                _shortName: "A-10 Warthog",
                _type: WeaponType.ATT_A10_US,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_ATTACK_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_ATTACK_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_ATTACK_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.AC_ATTACK_SURVIVE + XXLARGE_BONUS,         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_2 + XXLARGE_BONUS,        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD + XXLARGE_BONUS,               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ATT,            // Upgrade Path
                _turnAvailable: 468                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            A10.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.ATT);

            // Intel stats
            A10.AddIntelReportStat(WeaponType.ATT_A10_US,     36);

            // Handle the icon profile.
            A10.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_A10
            };

            // Add the A-10 Thunderbolt profile to the database
            AddProfile(WeaponType.ATT_A10_US, A10);
            //----------------------------------------------
            // US A-10 Thunderbolt II Attack Aircraft
            //----------------------------------------------

            //----------------------------------------------
            // US F-117 Nighthawk Stealth Attack Aircraft
            //----------------------------------------------
            WeaponProfile F117 = new WeaponProfile(
                _longName: "F-117 Nighthawk Stealth Attack Aircraft",
                _shortName: "F-117 Nighthawk",
                _type: WeaponType.ATT_F117_US,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_BOMBER_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_BOMBER_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_BOMBER_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.AC_BOMBER_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_3 + LARGE_BONUS,          // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 15,                                              // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Tiny,               // Unit Silhouette
                _upgradePath: UpgradePath.ATT,            // Upgrade Path
                _turnAvailable: 540                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            F117.SetPrestigeCost(PrestigeTierCost.Gen4, PrestigeTypeCost.ATT);

            // Intel stats
            F117.AddIntelReportStat(WeaponType.ATT_F117_US,     36);

            // Handle the icon profile.
            F117.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_F117
            };

            // Add the F-117 Nighthawk profile to the database
            AddProfile(WeaponType.ATT_F117_US, F117);
            //----------------------------------------------
            // US F-117 Nighthawk Stealth Attack Aircraft
            //----------------------------------------------

            //----------------------------------------------
            // French SEPECAT Jaguar Attack Aircraft
            //----------------------------------------------
            WeaponProfile JAGUAR = new WeaponProfile(
                _longName: "SEPECAT Jaguar Attack Aircraft",
                _shortName: "Jaguar",
                _type: WeaponType.ATT_JAGUAR_FR,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.EARLY_FGT_TOPSPEED + SMALL_MALUS,        // Top Speed Rating
                _surv: GameData.MID_FGT_SURVIVE + MEDIUM_BONUS,            // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_2,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD + LARGE_BONUS,                 // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ATT,            // Upgrade Path
                _turnAvailable: 420                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            JAGUAR.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.ATT);

            // Intel stats
            JAGUAR.AddIntelReportStat(WeaponType.ATT_JAGUAR_FR,     36);

            // Handle the icon profile.
            JAGUAR.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.FR_Jaguar
            };

            // Add the Jaguar profile to the database
            AddProfile(WeaponType.ATT_JAGUAR_FR, JAGUAR);
            //----------------------------------------------
            // French SEPECAT Jaguar Attack Aircraft
            //----------------------------------------------

            //----------------------------------------------
            // US F-111 Aardvark Bomber
            //----------------------------------------------
            WeaponProfile F111 = new WeaponProfile(
                _longName: "F-111 Aardvark Bomber",
                _shortName: "F-111 Aardvark",
                _type: WeaponType.BMB_F111_US,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_BOMBER_DOGFIGHT + XXLARGE_BONUS,          // Dogfighting Rating
                _man: GameData.AC_BOMBER_MANEUVER + LARGE_BONUS,           // Maneuverability Rating
                _topSpd: GameData.AC_BOMBER_TOPSPEED + XLARGE_BONUS,       // Top Speed Rating
                _surv: GameData.AC_BOMBER_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_2 + LARGE_BONUS,          // Ground Attack Rating
                _ol: GameData.LARGE_AC_LOAD + MEDIUM_BONUS,                // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.BMB,            // Upgrade Path
                _turnAvailable: 348                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            F111.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.BMB);

            // Intel stats
            F111.AddIntelReportStat(WeaponType.BMB_F111_US,     24);

            // Handle the icon profile.
            F111.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_F111
            };

            // Add the F-111 Aardvark profile to the database
            AddProfile(WeaponType.BMB_F111_US, F111);
            //----------------------------------------------
            // US F-111 Aardvark Bomber
            //----------------------------------------------

            //----------------------------------------------
            // US E-3 Sentry AWACS
            //----------------------------------------------
            WeaponProfile E3 = new WeaponProfile(
                _longName: "E-3 Sentry AWACS",
                _shortName: "E-3 Sentry",
                _type: WeaponType.AWACS_E3_US,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_BOMBER_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_BOMBER_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_BOMBER_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.AC_BOMBER_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.LARGE_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,               // Unit Silhouette
                _upgradePath: UpgradePath.AWACS,            // Upgrade Path
                _turnAvailable: 468                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            E3.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.AWACS);

            // Intel stats
            E3.AddIntelReportStat(WeaponType.AWACS_E3_US,     12);

            // Handle the icon profile.
            E3.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_E3
            };

            // Add the E-3 Sentry profile to the database
            AddProfile(WeaponType.AWACS_E3_US, E3);
            //----------------------------------------------
            // US E-3 Sentry AWACS
            //----------------------------------------------

            //----------------------------------------------
            // US SR-71 Blackbird Reconnaissance Aircraft
            //----------------------------------------------
            WeaponProfile SR71 = new WeaponProfile(
                _longName: "SR-71 Blackbird Reconnaissance Aircraft",
                _shortName: "SR-71 Blackbird",
                _type: WeaponType.RCNA_SR71_US,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_HIGHSPEED_WESTERN,                    // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_0,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.RCNA,            // Upgrade Path
                _turnAvailable: 336                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SR71.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.RCNA);

            // Intel stats
            SR71.AddIntelReportStat(WeaponType.RCNA_SR71_US,     12);

            // Handle the icon profile.
            SR71.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_SR71
            };

            // Add the SR-71 Blackbird profile to the database
            AddProfile(WeaponType.RCNA_SR71_US, SR71);
            //----------------------------------------------
            // US SR-71 Blackbird Reconnaissance Aircraft
            //----------------------------------------------

            #endregion // Jets

            #region Trucks

            //----------------------------------------------
            // Western Generic Truck
            //----------------------------------------------
            WeaponProfile TRK_W = new WeaponProfile(
                _longName: "Generic Transport Truck",
                _shortName: "Transport Truck",
                _type: WeaponType.TRK_WEST,
                _hardAtt: GameData.BASE_APC_HARD_ATTACK + LARGE_MALUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE + LARGE_MALUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK + LARGE_MALUS,     // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE + LARGE_MALUS,    // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,                  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,                  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MOT_UNIT,                   // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium                // Unit Silhouette
            );

            // Handle the icon profile.
            TRK_W.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.GEN_Truck_W,
                NW = SpriteManager.GEN_Truck_NW,
                SW = SpriteManager.GEN_Truck_SW
            };

            // Add the Western Truck profile to the database
            AddProfile(WeaponType.TRK_WEST, TRK_W);
            //----------------------------------------------
            // Western Generic Truck
            //----------------------------------------------

            #endregion // Trucks

            #region Infantry Units

            //----------------------------------------------
            // US Regular Infantry
            //----------------------------------------------
            WeaponProfile INF_REG_US_P = new WeaponProfile(
                _longName: "US Regular Infantry",
                _shortName: "US Regulars",
                _type: WeaponType.INF_REG_US,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_REG_US_P.AddIntelReportStat(WeaponType.Personnel,           2200);
            INF_REG_US_P.AddIntelReportStat(WeaponType.SPA_M109_US,           18);
            INF_REG_US_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,      18);
            INF_REG_US_P.AddIntelReportStat(WeaponType.SPAAA_M163_US,          4);
            INF_REG_US_P.AddIntelReportStat(WeaponType.AT_ATGM,               40);
            INF_REG_US_P.AddIntelReportStat(WeaponType.MANPAD_STINGER,        22);

            // Handle the icon profile.
            INF_REG_US_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_Regulars
            };

            // Add the US Regular Infantry profile to the database
            AddProfile(WeaponType.INF_REG_US, INF_REG_US_P);
            //----------------------------------------------
            // US Regular Infantry
            //----------------------------------------------

            //----------------------------------------------
            // US Marine Infantry
            //----------------------------------------------
            WeaponProfile INF_MAR_US_P = new WeaponProfile(
                _longName: "US Marine Infantry",
                _shortName: "US Marines",
                _type: WeaponType.INF_MAR_US,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_MAR_US_P.AddIntelReportStat(WeaponType.Personnel,           2750);
            INF_MAR_US_P.AddIntelReportStat(WeaponType.ART_105MM_FG,          18);
            INF_MAR_US_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,      18);
            INF_MAR_US_P.AddIntelReportStat(WeaponType.AT_ATGM,               40);
            INF_MAR_US_P.AddIntelReportStat(WeaponType.MANPAD_STINGER,        22);

            // Handle the icon profile.
            INF_MAR_US_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_Marines
            };

            // Add the US Marine Infantry profile to the database
            AddProfile(WeaponType.INF_MAR_US, INF_MAR_US_P);
            //----------------------------------------------
            // US Marine Infantry
            //----------------------------------------------

            //----------------------------------------------
            // US Airborne Infantry
            //----------------------------------------------
            WeaponProfile INF_AB_US_P = new WeaponProfile(
                _longName: "US Airborne Infantry",
                _shortName: "US Airborne",
                _type: WeaponType.INF_AB_US,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats (82nd Airborne composition)
            INF_AB_US_P.AddIntelReportStat(WeaponType.Personnel,           1950);
            INF_AB_US_P.AddIntelReportStat(WeaponType.ART_105MM_FG,          18);
            INF_AB_US_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,      18);
            INF_AB_US_P.AddIntelReportStat(WeaponType.AT_ATGM,               54);
            INF_AB_US_P.AddIntelReportStat(WeaponType.MANPAD_STINGER,        36);

            // Handle the icon profile.
            INF_AB_US_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_Airborne
            };

            // Add the US Airborne Infantry profile to the database
            AddProfile(WeaponType.INF_AB_US, INF_AB_US_P);
            //----------------------------------------------
            // US Airborne Infantry
            //----------------------------------------------

            //----------------------------------------------
            // US Air-Mobile Infantry
            //----------------------------------------------
            WeaponProfile INF_AM_US_P = new WeaponProfile(
                _longName: "US Air-Mobile Infantry",
                _shortName: "US Air-Mobile",
                _type: WeaponType.INF_AM_US,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats (101st Airborne composition)
            INF_AM_US_P.AddIntelReportStat(WeaponType.Personnel,           2040);
            INF_AM_US_P.AddIntelReportStat(WeaponType.AT_ATGM,               48);
            INF_AM_US_P.AddIntelReportStat(WeaponType.MANPAD_STINGER,        30);
            INF_AM_US_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,      18);
            INF_AM_US_P.AddIntelReportStat(WeaponType.ART_82MM_MORTAR,       18);

            // Handle the icon profile.
            INF_AM_US_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.US_Airborne
            };

            // Add the US Air-Mobile Infantry profile to the database
            AddProfile(WeaponType.INF_AM_US, INF_AM_US_P);
            //----------------------------------------------
            // US Air-Mobile Infantry
            //----------------------------------------------

            //----------------------------------------------
            // UK Regular Infantry
            //----------------------------------------------
            WeaponProfile INF_REG_UK_P = new WeaponProfile(
                _longName: "UK Regular Infantry",
                _shortName: "UK Regulars",
                _type: WeaponType.INF_REG_UK,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_REG_UK_P.AddIntelReportStat(WeaponType.Personnel,           2040);
            INF_REG_UK_P.AddIntelReportStat(WeaponType.SPA_M109_UK,           18);
            INF_REG_UK_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,      18);
            INF_REG_UK_P.AddIntelReportStat(WeaponType.AT_ATGM,               48);
            INF_REG_UK_P.AddIntelReportStat(WeaponType.MANPAD_RAPIER,         24);

            // Handle the icon profile.
            INF_REG_UK_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.UK_Regulars
            };

            // Add the UK Regular Infantry profile to the database
            AddProfile(WeaponType.INF_REG_UK, INF_REG_UK_P);
            //----------------------------------------------
            // UK Regular Infantry
            //----------------------------------------------

            //----------------------------------------------
            // UK Airborne Infantry
            //----------------------------------------------
            WeaponProfile INF_AB_UK_P = new WeaponProfile(
                _longName: "UK Airborne Infantry",
                _shortName: "UK Airborne",
                _type: WeaponType.INF_AB_UK,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_AB_UK_P.AddIntelReportStat(WeaponType.Personnel,           1860);
            INF_AB_UK_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,      18);
            INF_AB_UK_P.AddIntelReportStat(WeaponType.ART_82MM_MORTAR,       18);
            INF_AB_UK_P.AddIntelReportStat(WeaponType.AT_ATGM,               48);
            INF_AB_UK_P.AddIntelReportStat(WeaponType.MANPAD_RAPIER,         24);

            // Handle the icon profile.
            INF_AB_UK_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.UK_Airborne
            };

            // Add the UK Airborne Infantry profile to the database
            AddProfile(WeaponType.INF_AB_UK, INF_AB_UK_P);
            //----------------------------------------------
            // UK Airborne Infantry
            //----------------------------------------------

            //----------------------------------------------
            // FRG Regular Infantry
            //----------------------------------------------
            WeaponProfile INF_REG_GE_P = new WeaponProfile(
                _longName: "FRG Regular Infantry",
                _shortName: "FRG Regulars",
                _type: WeaponType.INF_REG_GE,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats (Panzergrenadier Brigade composition)
            INF_REG_GE_P.AddIntelReportStat(WeaponType.Personnel,           2600);
            INF_REG_GE_P.AddIntelReportStat(WeaponType.SPA_M109_GE,           18);
            INF_REG_GE_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,      18);
            INF_REG_GE_P.AddIntelReportStat(WeaponType.AT_ATGM,               40);
            INF_REG_GE_P.AddIntelReportStat(WeaponType.MANPAD_STINGER,        28);

            // Handle the icon profile.
            INF_REG_GE_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GER_Regulars
            };

            // Add the FRG Regular Infantry profile to the database
            AddProfile(WeaponType.INF_REG_GE, INF_REG_GE_P);
            //----------------------------------------------
            // FRG Regular Infantry
            //----------------------------------------------

            //----------------------------------------------
            // FRG Airborne Infantry
            //----------------------------------------------
            WeaponProfile INF_AB_GE_P = new WeaponProfile(
                _longName: "FRG Airborne Infantry",
                _shortName: "FRG Airborne",
                _type: WeaponType.INF_AB_GE,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats (Luftlandebrigade composition)
            INF_AB_GE_P.AddIntelReportStat(WeaponType.Personnel,           1900);
            INF_AB_GE_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,      24);
            INF_AB_GE_P.AddIntelReportStat(WeaponType.AT_ATGM,               54);
            INF_AB_GE_P.AddIntelReportStat(WeaponType.MANPAD_STINGER,        36);

            // Handle the icon profile.
            INF_AB_GE_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GER_Airborne
            };

            // Add the FRG Airborne Infantry profile to the database
            AddProfile(WeaponType.INF_AB_GE, INF_AB_GE_P);
            //----------------------------------------------
            // FRG Airborne Infantry
            //----------------------------------------------

            //----------------------------------------------
            // French Regular Infantry
            //----------------------------------------------
            WeaponProfile INF_REG_FR_P = new WeaponProfile(
                _longName: "French Regular Infantry",
                _shortName: "FR Regulars",
                _type: WeaponType.INF_REG_FR,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats (Brigade d'Infanterie Mecanisee composition)
            INF_REG_FR_P.AddIntelReportStat(WeaponType.Personnel,           1950);
            INF_REG_FR_P.AddIntelReportStat(WeaponType.SPA_AUF1,              18);
            INF_REG_FR_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,      18);
            INF_REG_FR_P.AddIntelReportStat(WeaponType.AT_ATGM,               18);
            INF_REG_FR_P.AddIntelReportStat(WeaponType.MANPAD_MISTRAL,        12);

            // Handle the icon profile.
            INF_REG_FR_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.FR_Regulars
            };

            // Add the French Regular Infantry profile to the database
            AddProfile(WeaponType.INF_REG_FR, INF_REG_FR_P);
            //----------------------------------------------
            // French Regular Infantry
            //----------------------------------------------

            //----------------------------------------------
            // French Airborne Infantry
            //----------------------------------------------
            WeaponProfile INF_AB_FR_P = new WeaponProfile(
                _longName: "French Airborne Infantry",
                _shortName: "FR Airborne",
                _type: WeaponType.INF_AB_FR,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats (11e Division Parachutiste composition)
            INF_AB_FR_P.AddIntelReportStat(WeaponType.Personnel,           2200);
            INF_AB_FR_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,      36);
            INF_AB_FR_P.AddIntelReportStat(WeaponType.AT_ATGM,               36);
            INF_AB_FR_P.AddIntelReportStat(WeaponType.MANPAD_MISTRAL,        24);

            // Handle the icon profile.
            INF_AB_FR_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.FR_Airborne
            };

            // Add the French Airborne Infantry profile to the database
            AddProfile(WeaponType.INF_AB_FR, INF_AB_FR_P);
            //----------------------------------------------
            // French Airborne Infantry
            //----------------------------------------------

            #endregion // Infantry Units
        }

        /// <summary>
        /// Add Arab WeaponProfiles
        /// </summary>
        private static void CreateArabProfiles()
        {
            #region MBTs

            //----------------------------------------------
            // Iraqi T-55A Medium Tank
            //----------------------------------------------
            WeaponProfile T55A = new WeaponProfile(
                _longName: "T-55A Medium Tank",
                _shortName: "T-55A",
                _type: WeaponType.TANK_T55A_IQ,
                _hardAtt: GameData.GEN1_TANK_HARD_ATTACK,                  // Hard Attack Rating
                _hardDef: GameData.GEN1_TANK_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 240                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            T55A.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.TANK);

            // Intel stats
            T55A.AddIntelReportStat(WeaponType.Personnel,        950);
            T55A.AddIntelReportStat(WeaponType.TANK_T55A_IQ,     105);
            T55A.AddIntelReportStat(WeaponType.IFV_BMP1_IQ,       40);
            T55A.AddIntelReportStat(WeaponType.SPA_2S1_IQ,        18);
            T55A.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,      12);
            T55A.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,  18);
            T55A.AddIntelReportStat(WeaponType.AT_ATGM,            6);
            T55A.AddIntelReportStat(WeaponType.MANPAD_STRELA,      8);
            T55A.AddIntelReportStat(WeaponType.SPAAA_ZSU57_IQ,     4);
            T55A.AddIntelReportStat(WeaponType.SPSAM_2K12_IQ,      4);

            // Handle the icon profile.
            T55A.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.AR_T55_W,
                NW = SpriteManager.AR_T55_NW,
                SW = SpriteManager.AR_T55_SW
            };

            // Add the T-55A profile to the database
            AddProfile(WeaponType.TANK_T55A_IQ, T55A);
            //----------------------------------------------
            // Iraqi T-55A Medium Tank
            //----------------------------------------------

            //----------------------------------------------
            // Iraqi T-62A Medium Tank
            //----------------------------------------------
            WeaponProfile T62A = new WeaponProfile(
                _longName: "T-62A Medium Tank",
                _shortName: "T-62A",
                _type: WeaponType.TANK_T62A_IQ,
                _hardAtt: GameData.GEN1_TANK_HARD_ATTACK + MEDIUM_BONUS,   // Hard Attack Rating
                _hardDef: GameData.GEN1_TANK_HARD_DEFENSE + MEDIUM_BONUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 276                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            T62A.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.TANK);

            // Intel stats
            T62A.AddIntelReportStat(WeaponType.Personnel,       950);
            T62A.AddIntelReportStat(WeaponType.TANK_T62A_IQ,    104);
            T62A.AddIntelReportStat(WeaponType.TANK_T55A_IQ,    105);
            T62A.AddIntelReportStat(WeaponType.IFV_BMP1_IQ,      40);
            T62A.AddIntelReportStat(WeaponType.RCN_BRDM2_SV,     12);
            T62A.AddIntelReportStat(WeaponType.SPA_2S1_IQ,       18);
            T62A.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 18);
            T62A.AddIntelReportStat(WeaponType.AT_ATGM,           6);
            T62A.AddIntelReportStat(WeaponType.MANPAD_STRELA,     8);
            T62A.AddIntelReportStat(WeaponType.SPAAA_ZSU57_IQ,    4);
            T62A.AddIntelReportStat(WeaponType.SPSAM_2K12_IQ,     4);

            // Handle the icon profile. (Using T-55 sprites as stand-in)
            T62A.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.AR_T55_W,
                NW = SpriteManager.AR_T55_NW,
                SW = SpriteManager.AR_T55_SW
            };

            // Add the T-62A profile to the database
            AddProfile(WeaponType.TANK_T62A_IQ, T62A);
            //----------------------------------------------
            // Iraqi T-62A Medium Tank
            //----------------------------------------------

            //----------------------------------------------
            // Iranian M60A3 Main Battle Tank
            //----------------------------------------------
            WeaponProfile M60A3 = new WeaponProfile(
                _longName: "M60A3 Main Battle Tank",
                _shortName: "M60A3",
                _type: WeaponType.TANK_M60A3_IR,
                _hardAtt: GameData.GEN2_TANK_HARD_ATTACK,                  // Hard Attack Rating
                _hardDef: GameData.GEN2_TANK_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 480                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M60A3.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.TANK);

            // Intel stats
            M60A3.AddIntelReportStat(WeaponType.Personnel,       1100);
            M60A3.AddIntelReportStat(WeaponType.TANK_M60A3_IR,    100);
            M60A3.AddIntelReportStat(WeaponType.APC_M113_IR,       52);
            M60A3.AddIntelReportStat(WeaponType.RCN_FV105_UK,      12);
            M60A3.AddIntelReportStat(WeaponType.ART_155MM_FG,      18);
            M60A3.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,  18);
            M60A3.AddIntelReportStat(WeaponType.AT_ATGM,            8);
            M60A3.AddIntelReportStat(WeaponType.MANPAD_STINGER,     6);
            M60A3.AddIntelReportStat(WeaponType.SPAAA_ZSU23_SV,     4);

            // Handle the icon profile.
            M60A3.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.AR_M60_W,
                NW = SpriteManager.AR_M60_NW,
                SW = SpriteManager.AR_M60_SW
            };

            // Add the M60A3 profile to the database
            AddProfile(WeaponType.TANK_M60A3_IR, M60A3);
            //----------------------------------------------
            // Iranian M60A3 Main Battle Tank
            //----------------------------------------------

            #endregion // MBTs

            #region IFVs and APCs

            //----------------------------------------------
            // Iraqi BMP-1 Infantry Fighting Vehicle
            //----------------------------------------------
            WeaponProfile BMP1_IQ = new WeaponProfile(
                _longName: "BMP-1 Infantry Fighting Vehicle",
                _shortName: "BMP-1",
                _type: WeaponType.IFV_BMP1_IQ,
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK,                   // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: true,                             // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small,                // Unit Silhouette
                _upgradePath: UpgradePath.IFV,             // Upgrade Path
                _turnAvailable: 336                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            BMP1_IQ.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.IFV);

            // Intel stats
            BMP1_IQ.AddIntelReportStat(WeaponType.IFV_BMP1_IQ,       90);
            BMP1_IQ.AddIntelReportStat(WeaponType.TANK_T55A_IQ,      31);
            BMP1_IQ.AddIntelReportStat(WeaponType.APC_MTLB_IQ,        8);

            // Handle the icon profile.
            BMP1_IQ.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.AR_BMP1_W,
                NW = SpriteManager.AR_BMP1_NW,
                SW = SpriteManager.AR_BMP1_SW
            };

            // Add the BMP-1 profile to the database
            AddProfile(WeaponType.IFV_BMP1_IQ, BMP1_IQ);
            //----------------------------------------------
            // Iraqi BMP-1 Infantry Fighting Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // Iraqi MT-LB Armored Personnel Carrier
            //----------------------------------------------
            WeaponProfile MTLB_IQ = new WeaponProfile(
                _longName: "MT-LB Armored Personnel Carrier",
                _shortName: "MT-LB",
                _type: WeaponType.APC_MTLB_IQ,
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,                   // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: true,                             // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small,                // Unit Silhouette
                _upgradePath: UpgradePath.APC,             // Upgrade Path
                _turnAvailable: 312                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MTLB_IQ.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.APC);

            // Intel stats
            MTLB_IQ.AddIntelReportStat(WeaponType.APC_MTLB_IQ,       90);
            MTLB_IQ.AddIntelReportStat(WeaponType.TANK_T55A_IQ,      31);

            // Handle the icon profile.
            MTLB_IQ.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.AR_MTLB_W,
                NW = SpriteManager.AR_MTLB_NW,
                SW = SpriteManager.AR_MTLB_SW
            };

            // Add the MT-LB profile to the database
            AddProfile(WeaponType.APC_MTLB_IQ, MTLB_IQ);
            //----------------------------------------------
            // Iraqi MT-LB Armored Personnel Carrier
            //----------------------------------------------

            //----------------------------------------------
            // Iranian M113 Armored Personnel Carrier
            //----------------------------------------------
            WeaponProfile M113_IR = new WeaponProfile(
                _longName: "M113 Armored Personnel Carrier",
                _shortName: "M113",
                _type: WeaponType.APC_M113_IR,
                _hardAtt: GameData.BASE_APC_HARD_ATTACK,                   // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: true,                             // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small,                // Unit Silhouette
                _upgradePath: UpgradePath.APC,             // Upgrade Path
                _turnAvailable: 264                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            M113_IR.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.APC);

            // Intel stats
            M113_IR.AddIntelReportStat(WeaponType.APC_M113_IR,       90);
            M113_IR.AddIntelReportStat(WeaponType.TANK_M60A3_IR,     31);

            // Handle the icon profile.
            M113_IR.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.AR_M113_W,
                NW = SpriteManager.AR_M113_NW,
                SW = SpriteManager.AR_M113_SW
            };

            // Add the M113 profile to the database
            AddProfile(WeaponType.APC_M113_IR, M113_IR);
            //----------------------------------------------
            // Iranian M113 Armored Personnel Carrier
            //----------------------------------------------

            #endregion // IFVs and APCs

            #region Artillery

            //----------------------------------------------
            // Iraqi 2S1 Gvozdika Self-Propelled Artillery
            //----------------------------------------------
            WeaponProfile SPA_2S1_AR = new WeaponProfile(
                _longName: "2S1 Gvozdika Self-Propelled Howitzer",
                _shortName: "2S1 Gvozdika",
                _type: WeaponType.SPA_2S1_IQ,
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,                  // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_MEDIUM,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 396                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SPA_2S1_AR.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.SPA);

            // Intel stats
            SPA_2S1_AR.AddIntelReportStat(WeaponType.Personnel,       720);
            SPA_2S1_AR.AddIntelReportStat(WeaponType.SPA_2S1_IQ,       36);
            SPA_2S1_AR.AddIntelReportStat(WeaponType.APC_MTLB_IQ,      12);
            SPA_2S1_AR.AddIntelReportStat(WeaponType.MANPAD_STRELA,      8);

            // Handle the icon profile.
            SPA_2S1_AR.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.AR_2S1_W,
                NW = SpriteManager.AR_2S1_NW,
                SW = SpriteManager.AR_2S1_SW,
                W_F = SpriteManager.AR_2S1_W_F,
                NW_F = SpriteManager.AR_2S1_NW_F,
                SW_F = SpriteManager.AR_2S1_SW_F
            };

            // Add the 2S1 Gvozdika profile to the database
            AddProfile(WeaponType.SPA_2S1_IQ, SPA_2S1_AR);
            //----------------------------------------------
            // Iraqi 2S1 Gvozdika Self-Propelled Artillery
            //----------------------------------------------

            //----------------------------------------------
            // Arab Light Towed Artillery
            //----------------------------------------------
            WeaponProfile ART_LT_AR = new WeaponProfile(
                _longName: "Light Towed Artillery",
                _shortName: "Light Artillery",
                _type: WeaponType.ART_LIGHT_ARAB,
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + SMALL_MALUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_MALUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + SMALL_MALUS,    // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_MALUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SHORT,        // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MOT_UNIT,                   // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 144                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ART_LT_AR.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.ART);

            // Intel stats
            ART_LT_AR.AddIntelReportStat(WeaponType.Personnel,         700);
            ART_LT_AR.AddIntelReportStat(WeaponType.ART_LIGHT_ARAB,     48);
            ART_LT_AR.AddIntelReportStat(WeaponType.APC_MTLB_IQ,        12);
            ART_LT_AR.AddIntelReportStat(WeaponType.MANPAD_STRELA,        6);

            // Handle the icon profile.
            ART_LT_AR.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_LightArt
            };

            // Add the Arab Light Artillery profile to the database
            AddProfile(WeaponType.ART_LIGHT_ARAB, ART_LT_AR);
            //----------------------------------------------
            // Arab Light Towed Artillery
            //----------------------------------------------

            //----------------------------------------------
            // Arab Heavy Towed Artillery
            //----------------------------------------------
            WeaponProfile ART_HV_AR = new WeaponProfile(
                _longName: "Heavy Towed Artillery",
                _shortName: "Heavy Artillery",
                _type: WeaponType.ART_HEAVY_ARAB,
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + SMALL_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_MALUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + MEDIUM_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_MALUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_LONG,         // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MOT_UNIT,                   // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 144                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ART_HV_AR.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.ART);

            // Intel stats
            ART_HV_AR.AddIntelReportStat(WeaponType.Personnel,         750);
            ART_HV_AR.AddIntelReportStat(WeaponType.ART_HEAVY_ARAB,     36);
            ART_HV_AR.AddIntelReportStat(WeaponType.APC_MTLB_IQ,        18);
            ART_HV_AR.AddIntelReportStat(WeaponType.MANPAD_STRELA,        8);

            // Handle the icon profile.
            ART_HV_AR.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_HeavyArt
            };

            // Add the Arab Heavy Artillery profile to the database
            AddProfile(WeaponType.ART_HEAVY_ARAB, ART_HV_AR);
            //----------------------------------------------
            // Arab Heavy Towed Artillery
            //----------------------------------------------

            #endregion // Artillery

            #region Air Defense

            //----------------------------------------------
            // Mujahideen Anti-Aircraft Artillery
            //----------------------------------------------
            WeaponProfile AAA_MJ = new WeaponProfile(
                _longName: "Mujahideen Anti-Aircraft Artillery",
                _shortName: "MJ AAA",
                _type: WeaponType.AAA_GEN_MJ,
                _hardAtt: GameData.BASE_AAA_HARD_ATTACK + LARGE_MALUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_AAA_HARD_DEFENSE + LARGE_MALUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_AAA_SOFT_ATTACK + LARGE_MALUS,     // Soft Attack Rating
                _softDef: GameData.BASE_AAA_SOFT_DEFENSE + LARGE_MALUS,    // Soft Defense Rating
                _gat: GameData.BASE_AAA_GROUND_AIR_ATTACK + LARGE_MALUS,   // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_AAA,          // Indirect Range
                _sr: GameData.BASE_AAA_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small,                // Unit Silhouette
                _upgradePath: UpgradePath.AAA,             // Upgrade Path
                _turnAvailable: 144                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            AAA_MJ.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.AAA);

            // Intel stats
            AAA_MJ.AddIntelReportStat(WeaponType.Personnel,       700);
            AAA_MJ.AddIntelReportStat(WeaponType.MANPAD_STINGER,    24);
            AAA_MJ.AddIntelReportStat(WeaponType.AAA_GEN_MJ,        24);

            // Handle the icon profile.
            AAA_MJ.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.MJ_AA
            };

            // Add the Mujahideen AAA profile to the database
            AddProfile(WeaponType.AAA_GEN_MJ, AAA_MJ);
            //----------------------------------------------
            // Mujahideen Anti-Aircraft Artillery
            //----------------------------------------------

            //----------------------------------------------
            // Mujahideen Stinger SAM Team
            //----------------------------------------------
            WeaponProfile SAM_MJ = new WeaponProfile(
                _longName: "Mujahideen Stinger SAM Team",
                _shortName: "MJ SAM",
                _type: WeaponType.SAM_GEN_MJ,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                   // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE + MEDIUM_MALUS,   // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE + MEDIUM_MALUS,   // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK + LARGE_MALUS,   // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_AAA,          // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small,                // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 264                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SAM_MJ.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.SAM);

            // Intel stats
            SAM_MJ.AddIntelReportStat(WeaponType.Personnel,       600);
            SAM_MJ.AddIntelReportStat(WeaponType.SAM_GEN_MJ,        36);
            SAM_MJ.AddIntelReportStat(WeaponType.MANPAD_STINGER,    12);

            // Handle the icon profile.
            SAM_MJ.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.MJ_Stinger
            };

            // Add the Mujahideen SAM profile to the database
            AddProfile(WeaponType.SAM_GEN_MJ, SAM_MJ);
            //----------------------------------------------
            // Mujahideen Stinger SAM Team
            //----------------------------------------------

            //----------------------------------------------
            // IQ ZSU-57 Self-Propelled Anti-Aircraft Gun
            //----------------------------------------------
            WeaponProfile ZSU_57_IQ = new WeaponProfile(
                _longName: "ZSU-57 Self-Propelled Anti-Aircraft Gun",
                _shortName: "ZSU-57",
                _type: WeaponType.SPAAA_ZSU57_IQ,
                _hardAtt: GameData.BASE_AAA_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_AAA_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_AAA_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_AAA_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_AAA_GROUND_AIR_ATTACK + SMALL_BONUS,   // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_AAA,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_AAA,          // Indirect Range
                _sr: GameData.BASE_AAA_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.AAA,             // Upgrade Path
                _turnAvailable: 372                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ZSU_57_IQ.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.SPAAA);

            // Intel report stats
            ZSU_57_IQ.AddIntelReportStat(WeaponType.Personnel, 900); // Air defense personnel
            ZSU_57_IQ.AddIntelReportStat(WeaponType.SPAAA_ZSU57_IQ, 18); // ZSU-57 SPAAA units
            ZSU_57_IQ.AddIntelReportStat(WeaponType.APC_MTLB_IQ, 24); // Command and control vehicles
            ZSU_57_IQ.AddIntelReportStat(WeaponType.MANPAD_STRELA, 12); // Strela mobile SAM systems

            // Handle the icon profile.
            ZSU_57_IQ.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.AR_ZSU57_W,
                NW = SpriteManager.AR_ZSU57_NW,
                SW = SpriteManager.AR_ZSU57_SW,
                W_F = SpriteManager.AR_ZSU57_W_F,
                NW_F = SpriteManager.AR_ZSU57_NW_F,
                SW_F = SpriteManager.AR_ZSU57_SW_F
            };

            // Add the ZSU-57 IQ profile to the database
            AddProfile(WeaponType.SPAAA_ZSU57_IQ, ZSU_57_IQ);
            //----------------------------------------------
            // IQ ZSU-57 Self-Propelled Anti-Aircraft Gun
            //----------------------------------------------

            //----------------------------------------------
            // IQ 2k12 Self-Propelled SAM System
            //----------------------------------------------
            WeaponProfile SPSAM_2k12 = new WeaponProfile(
                _longName: "2K12 Self-Propelled SAM",
                _shortName: "2K12",
                _type: WeaponType.SPSAM_2K12_IQ,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK + SMALL_BONUS,    // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_SAM,                          // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SAM,          // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.AAA,             // Upgrade Path
                _turnAvailable: 372                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SPSAM_2k12.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.SPAAA);

            // Intel report stats
            SPSAM_2k12.AddIntelReportStat(WeaponType.Personnel, 900); // Air defense personnel
            SPSAM_2k12.AddIntelReportStat(WeaponType.SPSAM_2K12_IQ, 18); // 2K12 SPAAA units
            SPSAM_2k12.AddIntelReportStat(WeaponType.APC_MTLB_IQ, 24); // Command and control vehicles
            SPSAM_2k12.AddIntelReportStat(WeaponType.MANPAD_STRELA, 12); // Strela mobile SAM systems

            // Handle the icon profile.
            SPSAM_2k12.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.AR_2K12_W,
                NW = SpriteManager.AR_2K12_NW,
                SW = SpriteManager.AR_2K12_SW,
                W_F = SpriteManager.AR_2K12_W_F,
                NW_F = SpriteManager.AR_2K12_NW_F,
                SW_F = SpriteManager.AR_2K12_SW_F
            };

            // Add the 2K12 IQ profile to the database
            AddProfile(WeaponType.SPSAM_2K12_IQ, SPSAM_2k12);
            //----------------------------------------------
            // IQ 2k12 Self-Propelled SAM
            //----------------------------------------------

            #endregion // Air Defense

            #region Jets

            //----------------------------------------------
            // Iraqi MiG-21 Fishbed Interceptor
            //----------------------------------------------
            WeaponProfile MIG21_IQ = new WeaponProfile(
                _longName: "MiG-21 Fishbed Interceptor",
                _shortName: "MiG-21 Fishbed",
                _type: WeaponType.FGT_MIG21_IQ,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT + SMALL_MALUS,            // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER + SMALL_MALUS,           // Maneuverability Rating
                _topSpd: GameData.EARLY_FGT_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Day,                // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 252                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MIG21_IQ.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.FGT);

            // Intel stats
            MIG21_IQ.AddIntelReportStat(WeaponType.FGT_MIG21_IQ,     36);

            // Handle the icon profile.
            MIG21_IQ.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.AR_Mig21
            };

            // Add the MiG-21 profile to the database
            AddProfile(WeaponType.FGT_MIG21_IQ, MIG21_IQ);
            //----------------------------------------------
            // Iraqi MiG-21 Fishbed Interceptor
            //----------------------------------------------

            //----------------------------------------------
            // Iraqi MiG-23 Flogger Fighter
            //----------------------------------------------
            WeaponProfile MIG23_IQ = new WeaponProfile(
                _longName: "MiG-23 Flogger Fighter",
                _shortName: "MiG-23 Flogger",
                _type: WeaponType.FGT_MIG23_IQ,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT + MEDIUM_BONUS,           // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.EARLY_FGT_TOPSPEED + SMALL_BONUS,        // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE + SMALL_BONUS,           // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 384                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            MIG23_IQ.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.FGT);

            // Intel stats
            MIG23_IQ.AddIntelReportStat(WeaponType.FGT_MIG23_IQ,     36);

            // Handle the icon profile.
            MIG23_IQ.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.AR_Mig23
            };

            // Add the MiG-23 profile to the database
            AddProfile(WeaponType.FGT_MIG23_IQ, MIG23_IQ);
            //----------------------------------------------
            // Iraqi MiG-23 Flogger Fighter
            //----------------------------------------------

            //----------------------------------------------
            // Iraqi Su-17 Fitter Attack Aircraft
            //----------------------------------------------
            WeaponProfile SU17_IQ = new WeaponProfile(
                _longName: "Su-17 Fitter Attack Aircraft",
                _shortName: "Su-17 Fitter",
                _type: WeaponType.ATT_SU17_IQ,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_ATTACK_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_ATTACK_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_ATTACK_TOPSPEED + SMALL_BONUS,        // Top Speed Rating
                _surv: GameData.AC_ATTACK_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_0 + LARGE_BONUS,          // Ground Attack Rating
                _ol: GameData.MEDIUM_AC_LOAD,                              // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ATT,             // Upgrade Path
                _turnAvailable: 384                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SU17_IQ.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.ATT);

            // Intel stats
            SU17_IQ.AddIntelReportStat(WeaponType.ATT_SU17_IQ,     36);

            // Handle the icon profile.
            SU17_IQ.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.AR_SU17
            };

            // Add the Su-17 profile to the database
            AddProfile(WeaponType.ATT_SU17_IQ, SU17_IQ);
            //----------------------------------------------
            // Iraqi Su-17 Fitter Attack Aircraft
            //----------------------------------------------

            //----------------------------------------------
            // Iranian F-4 Phantom Fighter
            //----------------------------------------------
            WeaponProfile F4_IR = new WeaponProfile(
                _longName: "F-4 Phantom Fighter",
                _shortName: "F-4 Phantom",
                _type: WeaponType.FGT_F4_IR,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT + SMALL_BONUS,            // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER + SMALL_MALUS,           // Maneuverability Rating
                _topSpd: GameData.EARLY_FGT_TOPSPEED + SMALL_BONUS,        // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE + SMALL_BONUS,           // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 276                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            F4_IR.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.FGT);

            // Intel stats
            F4_IR.AddIntelReportStat(WeaponType.FGT_F4_IR,     48);

            // Handle the icon profile.
            F4_IR.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.AR_F4
            };

            // Add the Iranian F-4 profile to the database
            AddProfile(WeaponType.FGT_F4_IR, F4_IR);
            //----------------------------------------------
            // Iranian F-4 Phantom Fighter
            //----------------------------------------------

            //----------------------------------------------
            // Iranian F-14 Tomcat Fleet Defense Fighter
            //----------------------------------------------
            WeaponProfile F14_IR = new WeaponProfile(
                _longName: "F-14 Tomcat Fleet Defense Fighter",
                _shortName: "F-14 Tomcat",
                _type: WeaponType.FGT_F14_IR,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.LATE_FGT_DOGFIGHT,                           // Dogfighting Rating
                _man: GameData.LATE_FGT_MANEUVER + SMALL_MALUS,            // Maneuverability Rating
                _topSpd: GameData.LATE_FGT_TOPSPEED + SMALL_BONUS,         // Top Speed Rating
                _surv: GameData.LATE_FGT_SURVIVE + SMALL_MALUS,            // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ADVANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 432                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            F14_IR.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.FGT);

            // Intel stats
            F14_IR.AddIntelReportStat(WeaponType.FGT_F14_IR,     48);

            // Handle the icon profile.
            F14_IR.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.AR_F14
            };

            // Add the Iranian F-14 profile to the database
            AddProfile(WeaponType.FGT_F14_IR, F14_IR);
            //----------------------------------------------
            // Iranian F-14 Tomcat Fleet Defense Fighter
            //----------------------------------------------

            #endregion // Jets

            #region Trucks

            //----------------------------------------------
            // Arab Generic Truck
            //----------------------------------------------
            WeaponProfile TRK_AR = new WeaponProfile(
                _longName: "Generic Transport Truck",
                _shortName: "Transport Truck",
                _type: WeaponType.TRK_GEN_ARAB,
                _hardAtt: GameData.BASE_APC_HARD_ATTACK + LARGE_MALUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_APC_HARD_DEFENSE + LARGE_MALUS,    // Hard Defense Rating
                _softAtt: GameData.BASE_APC_SOFT_ATTACK + LARGE_MALUS,     // Soft Attack Rating
                _softDef: GameData.BASE_APC_SOFT_DEFENSE + LARGE_MALUS,    // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,                  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,                  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MOT_UNIT,                   // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium                // Unit Silhouette
            );

            // Handle the icon profile.
            TRK_AR.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.AR_Truck_W,
                NW = SpriteManager.AR_Truck_NW,
                SW = SpriteManager.AR_Truck_SW
            };

            // Add the Arab Truck profile to the database
            AddProfile(WeaponType.TRK_GEN_ARAB, TRK_AR);
            //----------------------------------------------
            // Arab Generic Truck
            //----------------------------------------------

            #endregion // Trucks

            #region Infantry

            //----------------------------------------------
            // IQ Regular Infantry
            //----------------------------------------------
            WeaponProfile INF_REG_IQ_P = new WeaponProfile(
                _longName: "Iraqi Regular Infantry",
                _shortName: "IQ Regulars",
                _type: WeaponType.INF_REG_IQ,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_REG_IQ_P.AddIntelReportStat(WeaponType.Personnel, 2040);
            INF_REG_IQ_P.AddIntelReportStat(WeaponType.ART_HEAVY_ARAB, 18);
            INF_REG_IQ_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 18);
            INF_REG_IQ_P.AddIntelReportStat(WeaponType.AT_ATGM, 12);
            INF_REG_IQ_P.AddIntelReportStat(WeaponType.MANPAD_STRELA, 24);

            // Handle the icon profile.
            INF_REG_IQ_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.IQ_Regulars
            };

            // Add the IQ Regular Infantry profile to the database
            AddProfile(WeaponType.INF_REG_IQ, INF_REG_IQ_P);

            //----------------------------------------------
            // IQ Regular Infantry
            //----------------------------------------------

            //----------------------------------------------
            // IR Regular Infantry
            //----------------------------------------------
            WeaponProfile INF_REG_IR_P = new WeaponProfile(
                _longName: "Iranian Regular Infantry",
                _shortName: "IR Regulars",
                _type: WeaponType.INF_REG_IR,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_REG_IR_P.AddIntelReportStat(WeaponType.Personnel, 2040);
            INF_REG_IR_P.AddIntelReportStat(WeaponType.ART_HEAVY_ARAB, 18);
            INF_REG_IR_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 18);
            INF_REG_IR_P.AddIntelReportStat(WeaponType.AT_ATGM, 12);
            INF_REG_IR_P.AddIntelReportStat(WeaponType.MANPAD_STRELA, 24);

            // Handle the icon profile.
            INF_REG_IR_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.IR_Regulars
            };

            // Add the IR Regular Infantry profile to the database
            AddProfile(WeaponType.INF_REG_IR, INF_REG_IR_P);

            //----------------------------------------------
            // IR Regular Infantry
            //----------------------------------------------

            #endregion // Infantry

            #region Mujahideen Infantry

            //----------------------------------------------
            // Mujahideen Regular Infantry
            //----------------------------------------------
            WeaponProfile INF_MJ_REG = new WeaponProfile(
                _longName: "Mujahideen Regular Infantry",
                _shortName: "MJ Regulars",
                _type: WeaponType.INF_REG_MJ,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_MJ_REG.AddIntelReportStat(WeaponType.Personnel,       1200);
            INF_MJ_REG.AddIntelReportStat(WeaponType.INF_RPG_MJ,        36);
            INF_MJ_REG.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,   18);
            INF_MJ_REG.AddIntelReportStat(WeaponType.AT_RecoilessRifle, 12);
            INF_MJ_REG.AddIntelReportStat(WeaponType.AAA_20MM,           8);

            // Handle the icon profile.
            INF_MJ_REG.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.MJ_Regulars
            };

            // Add the Mujahideen Regular Infantry profile to the database
            AddProfile(WeaponType.INF_REG_MJ, INF_MJ_REG);
            //----------------------------------------------
            // Mujahideen Regular Infantry
            //----------------------------------------------

            //----------------------------------------------
            // Mujahideen Special Forces
            //----------------------------------------------
            WeaponProfile INF_MJ_SPEC = new WeaponProfile(
                _longName: "Mujahideen Special Forces",
                _shortName: "MJ Elite",
                _type: WeaponType.INF_SPEC_MJ,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK + MEDIUM_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK + MEDIUM_BONUS,    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_MJ_SPEC.AddIntelReportStat(WeaponType.Personnel,       800);
            INF_MJ_SPEC.AddIntelReportStat(WeaponType.INF_RPG_MJ,       36);
            INF_MJ_SPEC.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  18);
            INF_MJ_SPEC.AddIntelReportStat(WeaponType.MANPAD_STINGER,   12);
            INF_MJ_SPEC.AddIntelReportStat(WeaponType.AT_ATGM,          12);

            // Handle the icon profile.
            INF_MJ_SPEC.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.MJ_Elite
            };

            // Add the Mujahideen Special Forces profile to the database
            AddProfile(WeaponType.INF_SPEC_MJ, INF_MJ_SPEC);
            //----------------------------------------------
            // Mujahideen Special Forces
            //----------------------------------------------

            //----------------------------------------------
            // Mujahideen Horse Cavalry
            //----------------------------------------------
            WeaponProfile INF_MJ_CAV = new WeaponProfile(
                _longName: "Mujahideen Horse Cavalry",
                _shortName: "MJ Cavalry",
                _type: WeaponType.INF_CAV_MJ,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.CAVALRY_UNIT,               // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_MJ_CAV.AddIntelReportStat(WeaponType.Personnel,       1075);
            INF_MJ_CAV.AddIntelReportStat(WeaponType.INF_RPG_MJ,        48);
            INF_MJ_CAV.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,  12);
            INF_MJ_CAV.AddIntelReportStat(WeaponType.AT_RecoilessRifle,  8);
            INF_MJ_CAV.AddIntelReportStat(WeaponType.AAA_20MM,           8);

            // Handle the icon profile.
            INF_MJ_CAV.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.MJ_Mounted
            };

            // Add the Mujahideen Horse Cavalry profile to the database
            AddProfile(WeaponType.INF_CAV_MJ, INF_MJ_CAV);
            //----------------------------------------------
            // Mujahideen Horse Cavalry
            //----------------------------------------------

            //----------------------------------------------
            // Mujahideen RPG Teams
            //----------------------------------------------
            WeaponProfile INF_MJ_RPG = new WeaponProfile(
                _longName: "Mujahideen RPG Teams",
                _shortName: "MJ RPG",
                _type: WeaponType.INF_RPG_MJ,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK + LARGE_BONUS,     // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK + SMALL_MALUS,     // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_MJ_RPG.AddIntelReportStat(WeaponType.Personnel,       800);
            INF_MJ_RPG.AddIntelReportStat(WeaponType.INF_RPG_MJ,       48);
            INF_MJ_RPG.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  12);
            INF_MJ_RPG.AddIntelReportStat(WeaponType.AT_RecoilessRifle, 8);
            INF_MJ_RPG.AddIntelReportStat(WeaponType.AAA_20MM,          6);

            // Handle the icon profile.
            INF_MJ_RPG.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.MJ_RPG
            };

            // Add the Mujahideen RPG Teams profile to the database
            AddProfile(WeaponType.INF_RPG_MJ, INF_MJ_RPG);
            //----------------------------------------------
            // Mujahideen RPG Teams
            //----------------------------------------------

            //----------------------------------------------
            // Mujahideen Heavy Mortar
            //----------------------------------------------
            WeaponProfile ART_MJ_MORT = new WeaponProfile(
                _longName: "Mujahideen Heavy Mortar",
                _shortName: "MJ Mortar",
                _type: WeaponType.ART_MORTAR_MJ,
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + LARGE_MALUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + LARGE_MALUS,   // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + MEDIUM_MALUS,   // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + LARGE_MALUS,   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_MINIMUM,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            ART_MJ_MORT.AddIntelReportStat(WeaponType.Personnel,       700);
            ART_MJ_MORT.AddIntelReportStat(WeaponType.ART_120MM_MORTAR, 12);
            ART_MJ_MORT.AddIntelReportStat(WeaponType.ART_81MM_MORTAR,  12);
            ART_MJ_MORT.AddIntelReportStat(WeaponType.AAA_20MM,          6);

            // Handle the icon profile.
            ART_MJ_MORT.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.MJ_Mortar
            };

            // Add the Mujahideen Heavy Mortar profile to the database
            AddProfile(WeaponType.ART_MORTAR_MJ, ART_MJ_MORT);
            //----------------------------------------------
            // Mujahideen Heavy Mortar
            //----------------------------------------------

            //----------------------------------------------
            // Mujahideen Light Artillery
            //----------------------------------------------
            WeaponProfile ART_MJ_LT = new WeaponProfile(
                _longName: "Mujahideen Light Artillery",
                _shortName: "MJ Artillery",
                _type: WeaponType.ART_LIGHT_MJ,
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + XLARGE_MALUS,   // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + XLARGE_MALUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + LARGE_MALUS,    // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + XLARGE_MALUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_MINIMUM,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            ART_MJ_LT.AddIntelReportStat(WeaponType.Personnel,       750);
            ART_MJ_LT.AddIntelReportStat(WeaponType.ART_105MM_FG,     24);
            ART_MJ_LT.AddIntelReportStat(WeaponType.AAA_20MM,          6);

            // Handle the icon profile.
            ART_MJ_LT.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.MJ_Artillery
            };

            // Add the Mujahideen Light Artillery profile to the database
            AddProfile(WeaponType.ART_LIGHT_MJ, ART_MJ_LT);
            //----------------------------------------------
            // Mujahideen Light Artillery
            //----------------------------------------------

            #endregion // Mujahideen Infantry
        }

        /// <summary>
        /// Add Chinese WeaponProfiles
        /// </summary>
        private static void CreateChineseProfiles()
        {
            #region MBTs

            //----------------------------------------------
            // Chinese Type 59 Medium Tank
            //----------------------------------------------
            WeaponProfile TYPE59 = new WeaponProfile(
                _longName: "Type 59 Medium Tank",
                _shortName: "Type 59",
                _type: WeaponType.TANK_TYPE59,
                _hardAtt: GameData.GEN1_TANK_HARD_ATTACK,                  // Hard Attack Rating
                _hardDef: GameData.GEN1_TANK_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 252                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            TYPE59.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.TANK);

            // Intel stats
            TYPE59.AddIntelReportStat(WeaponType.Personnel,      1050);
            TYPE59.AddIntelReportStat(WeaponType.TANK_TYPE59,      80);
            TYPE59.AddIntelReportStat(WeaponType.IFV_TYPE86,       40);
            TYPE59.AddIntelReportStat(WeaponType.SPA_TYPE82,       18);
            TYPE59.AddIntelReportStat(WeaponType.ART_122MM_FG,     18);
            TYPE59.AddIntelReportStat(WeaponType.AT_ATGM,          12);
            TYPE59.AddIntelReportStat(WeaponType.SPAAA_TYPE53,      6);
            TYPE59.AddIntelReportStat(WeaponType.AAA_20MM,          6);
            TYPE59.AddIntelReportStat(WeaponType.MANPAD_STRELA,     18);

            // Handle the icon profile.
            TYPE59.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.CH_Type59_W,
                NW = SpriteManager.CH_Type59_NW,
                SW = SpriteManager.CH_Type59_SW
            };

            // Add the Type 59 profile to the database
            AddProfile(WeaponType.TANK_TYPE59, TYPE59);
            //----------------------------------------------
            // Chinese Type 59 Medium Tank
            //----------------------------------------------

            //----------------------------------------------
            // Chinese Type 80 Main Battle Tank
            //----------------------------------------------
            WeaponProfile TYPE80 = new WeaponProfile(
                _longName: "Type 80 Main Battle Tank",
                _shortName: "Type 80",
                _type: WeaponType.TANK_TYPE80,
                _hardAtt: GameData.GEN2_TANK_HARD_ATTACK,                  // Hard Attack Rating
                _hardDef: GameData.GEN2_TANK_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 564                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            TYPE80.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.TANK);

            // Intel stats
            TYPE80.AddIntelReportStat(WeaponType.Personnel, 1050);
            TYPE80.AddIntelReportStat(WeaponType.TANK_TYPE80, 80);
            TYPE80.AddIntelReportStat(WeaponType.IFV_TYPE86,  40);
            TYPE80.AddIntelReportStat(WeaponType.SPA_TYPE82,  18);
            TYPE80.AddIntelReportStat(WeaponType.ART_122MM_FG,18);
            TYPE80.AddIntelReportStat(WeaponType.AT_ATGM,     12);
            TYPE80.AddIntelReportStat(WeaponType.SPAAA_TYPE53, 6);
            TYPE80.AddIntelReportStat(WeaponType.AAA_20MM,     6);
            TYPE80.AddIntelReportStat(WeaponType.MANPAD_STRELA, 18);

            // Handle the icon profile.
            TYPE80.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.CH_Type80_W,
                NW = SpriteManager.CH_Type80_NW,
                SW = SpriteManager.CH_Type80_SW
            };

            // Add the Type 80 profile to the database
            AddProfile(WeaponType.TANK_TYPE80, TYPE80);
            //----------------------------------------------
            // Chinese Type 80 Main Battle Tank
            //----------------------------------------------

            //----------------------------------------------
            // Chinese Type 95 Main Battle Tank
            //----------------------------------------------
            WeaponProfile TYPE95 = new WeaponProfile(
                _longName: "Type 95 Main Battle Tank",
                _shortName: "Type 95",
                _type: WeaponType.TANK_TYPE95,
                _hardAtt: GameData.GEN3_TANK_HARD_ATTACK,                  // Hard Attack Rating
                _hardDef: GameData.GEN3_TANK_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_TANK_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_TANK_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_ARMOR,       // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen2,                     // NBC Rating
                _nvg: NVG_Rating.Gen2,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.TANK,            // Upgrade Path
                _turnAvailable: 588                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            TYPE95.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.TANK);

            // Intel stats
            TYPE95.AddIntelReportStat(WeaponType.Personnel, 1050);
            TYPE95.AddIntelReportStat(WeaponType.TANK_TYPE95, 80);
            TYPE95.AddIntelReportStat(WeaponType.IFV_TYPE86, 40);
            TYPE95.AddIntelReportStat(WeaponType.SPA_TYPE82, 18);
            TYPE95.AddIntelReportStat(WeaponType.ART_122MM_FG, 18);
            TYPE95.AddIntelReportStat(WeaponType.AT_ATGM, 12);
            TYPE95.AddIntelReportStat(WeaponType.SPAAA_TYPE53, 6);
            TYPE95.AddIntelReportStat(WeaponType.AAA_20MM, 6);
            TYPE95.AddIntelReportStat(WeaponType.MANPAD_STRELA, 18);

            // Handle the icon profile.
            TYPE95.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.CH_Type95_W,
                NW = SpriteManager.CH_Type95_NW,
                SW = SpriteManager.CH_Type95_SW
            };

            // Add the Type 95 profile to the database
            AddProfile(WeaponType.TANK_TYPE95, TYPE95);
            //----------------------------------------------
            // Chinese Type 95 Main Battle Tank
            //----------------------------------------------

            #endregion // MBTs

            #region IFV

            //----------------------------------------------
            // Chinese Type 86 Infantry Fighting Vehicle
            //----------------------------------------------
            WeaponProfile TYPE86 = new WeaponProfile(
                _longName: "Type 86 Infantry Fighting Vehicle",
                _shortName: "Type 86",
                _type: WeaponType.IFV_TYPE86,
                _hardAtt: GameData.BASE_IFV_HARD_ATTACK,                   // Hard Attack Rating
                _hardDef: GameData.BASE_IFV_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_IFV_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_IFV_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: true,                             // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small,                // Unit Silhouette
                _upgradePath: UpgradePath.IFV,             // Upgrade Path
                _turnAvailable: 576                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            TYPE86.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.IFV);

            // Intel stats
            TYPE86.AddIntelReportStat(WeaponType.IFV_TYPE86,        90);
            TYPE86.AddIntelReportStat(WeaponType.TANK_TYPE59,       40);

            // Handle the icon profile.
            TYPE86.IconProfile = new RegimentIconProfile(RegimentIconType.Directional)
            {
                W = SpriteManager.CH_Type86_W,
                NW = SpriteManager.CH_Type86_NW,
                SW = SpriteManager.CH_Type86_SW
            };

            // Add the Type 86 profile to the database
            AddProfile(WeaponType.IFV_TYPE86, TYPE86);
            //----------------------------------------------
            // Chinese Type 86 Infantry Fighting Vehicle
            //----------------------------------------------

            #endregion // IFV

            #region Artillery

            //----------------------------------------------
            // Chinese Type 82 Self-Propelled Howitzer
            //----------------------------------------------
            WeaponProfile SPA_TYPE82 = new WeaponProfile(
                _longName: "Type 82 Self-Propelled Howitzer",
                _shortName: "Type 82",
                _type: WeaponType.SPA_TYPE82,
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK,                  // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK,                  // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_MEDIUM,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 552                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            SPA_TYPE82.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.SPA);

            // Intel stats
            SPA_TYPE82.AddIntelReportStat(WeaponType.Personnel,       700);
            SPA_TYPE82.AddIntelReportStat(WeaponType.SPA_TYPE82,       36);
            SPA_TYPE82.AddIntelReportStat(WeaponType.IFV_TYPE86,       12);
            SPA_TYPE82.AddIntelReportStat(WeaponType.MANPAD_STRELA,      8);

            // Handle the icon profile.
            SPA_TYPE82.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.CH_Type82_W,
                NW = SpriteManager.CH_Type82_NW,
                SW = SpriteManager.CH_Type82_SW,
                W_F = SpriteManager.CH_Type82_W_F,
                NW_F = SpriteManager.CH_Type82_NW_F,
                SW_F = SpriteManager.CH_Type82_SW_F
            };

            // Add the Type 82 profile to the database
            AddProfile(WeaponType.SPA_TYPE82, SPA_TYPE82);
            //----------------------------------------------
            // Chinese Type 82 Self-Propelled Howitzer
            //----------------------------------------------

            //----------------------------------------------
            // Chinese PHZ-89 Multiple Rocket Launcher
            //----------------------------------------------
            WeaponProfile PHZ89 = new WeaponProfile(
                _longName: "PHZ-89 Multiple Rocket Launcher",
                _shortName: "PHZ-89",
                _type: WeaponType.ROC_PHZ89,
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + SMALL_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE,                 // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + LARGE_BONUS,    // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE,                 // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_LIGHTARMOR,  // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_ROC_MR,       // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ROC,             // Upgrade Path
                _turnAvailable: 500                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            PHZ89.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.ROC);

            // Intel stats
            PHZ89.AddIntelReportStat(WeaponType.Personnel,       650);
            PHZ89.AddIntelReportStat(WeaponType.ROC_PHZ89,        24);
            PHZ89.AddIntelReportStat(WeaponType.IFV_TYPE86,       12);
            PHZ89.AddIntelReportStat(WeaponType.MANPAD_STRELA,      8);

            // Handle the icon profile.
            PHZ89.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.CH_PHZ89_W,
                NW = SpriteManager.CH_PHZ89_NW,
                SW = SpriteManager.CH_PHZ89_SW,
                W_F = SpriteManager.CH_PHZ89_W_F,
                NW_F = SpriteManager.CH_PHZ89_NW_F,
                SW_F = SpriteManager.CH_PHZ89_SW_F
            };

            // Add the PHZ-89 profile to the database
            AddProfile(WeaponType.ROC_PHZ89, PHZ89);
            //----------------------------------------------
            // Chinese PHZ-89 Multiple Rocket Launcher
            //----------------------------------------------

            //----------------------------------------------
            // Chinese Light Towed Artillery
            //----------------------------------------------
            WeaponProfile ART_LT_CH = new WeaponProfile(
                _longName: "Light Towed Artillery",
                _shortName: "Light Artillery",
                _type: WeaponType.ART_LIGHT_CH,
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + SMALL_MALUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_MALUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + SMALL_MALUS,    // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_MALUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SHORT,        // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MOT_UNIT,                   // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 144                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ART_LT_CH.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.ART);

            // Intel stats
            ART_LT_CH.AddIntelReportStat(WeaponType.Personnel,       1050);
            ART_LT_CH.AddIntelReportStat(WeaponType.ART_LIGHT_CH,     72);
            ART_LT_CH.AddIntelReportStat(WeaponType.IFV_TYPE86,       12);
            ART_LT_CH.AddIntelReportStat(WeaponType.MANPAD_STRELA,      6);

            // Handle the icon profile.
            ART_LT_CH.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_LightArt
            };

            // Add the Chinese Light Artillery profile to the database
            AddProfile(WeaponType.ART_LIGHT_CH, ART_LT_CH);
            //----------------------------------------------
            // Chinese Light Towed Artillery
            //----------------------------------------------

            //----------------------------------------------
            // Chinese Heavy Towed Artillery
            //----------------------------------------------
            WeaponProfile ART_HV_CH = new WeaponProfile(
                _longName: "Heavy Towed Artillery",
                _shortName: "Heavy Artillery",
                _type: WeaponType.ART_HEAVY_CH,
                _hardAtt: GameData.BASE_ARTY_HARD_ATTACK + SMALL_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_ARTY_HARD_DEFENSE + MEDIUM_MALUS,  // Hard Defense Rating
                _softAtt: GameData.BASE_ARTY_SOFT_ATTACK + MEDIUM_BONUS,   // Soft Attack Rating
                _softDef: GameData.BASE_ARTY_SOFT_DEFENSE + MEDIUM_MALUS,  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_LONG,         // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.MOT_UNIT,                   // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.ART,             // Upgrade Path
                _turnAvailable: 144                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            ART_HV_CH.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.ART);

            // Intel stats
            ART_HV_CH.AddIntelReportStat(WeaponType.Personnel,       1100);
            ART_HV_CH.AddIntelReportStat(WeaponType.ART_HEAVY_CH,     72);
            ART_HV_CH.AddIntelReportStat(WeaponType.IFV_TYPE86,       18);
            ART_HV_CH.AddIntelReportStat(WeaponType.MANPAD_STRELA,      8);

            // Handle the icon profile.
            ART_HV_CH.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_HeavyArt
            };

            // Add the Chinese Heavy Artillery profile to the database
            AddProfile(WeaponType.ART_HEAVY_CH, ART_HV_CH);
            //----------------------------------------------
            // Chinese Heavy Towed Artillery
            //----------------------------------------------

            #endregion // Artillery

            #region Air Defense

            //----------------------------------------------
            // Chinese Type 53 Self-Propelled AAA
            //----------------------------------------------
            WeaponProfile TYPE53 = new WeaponProfile(
                _longName: "Type 53 Self-Propelled Anti-Aircraft Gun",
                _shortName: "Type 53",
                _type: WeaponType.SPAAA_TYPE53,
                _hardAtt: GameData.BASE_AAA_HARD_ATTACK,                   // Hard Attack Rating
                _hardDef: GameData.BASE_AAA_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_AAA_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_AAA_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.BASE_AAA_GROUND_AIR_ATTACK,                 // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_AAA,         // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_AAA,          // Indirect Range
                _sr: GameData.BASE_AAA_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.AAA,             // Upgrade Path
                _turnAvailable: 204                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            TYPE53.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.AAA);

            // Intel stats
            TYPE53.AddIntelReportStat(WeaponType.Personnel,         700);
            TYPE53.AddIntelReportStat(WeaponType.SPAAA_TYPE53,       36);
            TYPE53.AddIntelReportStat(WeaponType.IFV_TYPE86,         12);
            TYPE53.AddIntelReportStat(WeaponType.MANPAD_STRELA,       24);

            // Handle the icon profile.
            TYPE53.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.CH_Type53_W,
                NW = SpriteManager.CH_Type53_NW,
                SW = SpriteManager.CH_Type53_SW,
                W_F = SpriteManager.CH_Type53_W_F,
                NW_F = SpriteManager.CH_Type53_NW_F,
                SW_F = SpriteManager.CH_Type53_SW_F
            };

            // Add the Type 53 profile to the database
            AddProfile(WeaponType.SPAAA_TYPE53, TYPE53);
            //----------------------------------------------
            // Chinese Type 53 Self-Propelled AAA
            //----------------------------------------------

            //----------------------------------------------
            // Chinese HQ-7 Self-Propelled SAM
            //----------------------------------------------
            WeaponProfile HQ7 = new WeaponProfile(
                _longName: "HQ-7 Self-Propelled SAM System",
                _shortName: "HQ-7",
                _type: WeaponType.SPSAM_HQ7,
                _hardAtt: GameData.BASE_SAM_HARD_ATTACK,                   // Hard Attack Rating
                _hardDef: GameData.BASE_SAM_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_SAM_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_SAM_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.BASE_SAM_GROUND_AIR_ATTACK + SMALL_BONUS,   // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_SAM,         // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_SAM,          // Indirect Range
                _sr: GameData.BASE_SAM_SPOTTING_RANGE,     // Spotting Range
                _mmp: GameData.MECH_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.AllWeather,         // All-Weather Capability
                _sir: SIGINT_Rating.SpecializedLevel,      // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.SAM,             // Upgrade Path
                _turnAvailable: 564                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            HQ7.SetPrestigeCost(PrestigeTierCost.Gen3, PrestigeTypeCost.SAM);

            // Intel stats
            HQ7.AddIntelReportStat(WeaponType.Personnel,       750);
            HQ7.AddIntelReportStat(WeaponType.SPSAM_HQ7,        18);
            HQ7.AddIntelReportStat(WeaponType.IFV_TYPE86,        24);
            HQ7.AddIntelReportStat(WeaponType.SPAAA_TYPE53,       4);

            // Handle the icon profile.
            HQ7.IconProfile = new RegimentIconProfile(RegimentIconType.Directional_Fire)
            {
                W = SpriteManager.CH_HQ7_W,
                NW = SpriteManager.CH_HQ7_NW,
                SW = SpriteManager.CH_HQ7_SW,
                W_F = SpriteManager.CH_HQ7_W_F,
                NW_F = SpriteManager.CH_HQ7_NW_F,
                SW_F = SpriteManager.CH_HQ7_SW_F
            };

            // Add the HQ-7 profile to the database
            AddProfile(WeaponType.SPSAM_HQ7, HQ7);
            //----------------------------------------------
            // Chinese HQ-7 Self-Propelled SAM
            //----------------------------------------------

            #endregion // Air Defense

            #region Helicopters

            //----------------------------------------------
            // Chinese H-9 Attack Helicopter
            //----------------------------------------------
            WeaponProfile H9 = new WeaponProfile(
                _longName: "H-9 Attack Helicopter",
                _shortName: "H-9",
                _type: WeaponType.HEL_H9,
                _hardAtt: GameData.BASE_HEL_HARD_ATTACK + MEDIUM_BONUS,    // Hard Attack Rating
                _hardDef: GameData.BASE_HEL_HARD_DEFENSE,                  // Hard Defense Rating
                _softAtt: GameData.BASE_HEL_SOFT_ATTACK,                   // Soft Attack Rating
                _softDef: GameData.BASE_HEL_SOFT_DEFENSE,                  // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_HELO,        // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.RECON_UNIT_SPOTTING_RANGE,   // Spotting Range
                _mmp: GameData.HELO_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.HEL,             // Upgrade Path
                _turnAvailable: 528                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            H9.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.HEL);

            // Intel stats
            H9.AddIntelReportStat(WeaponType.Personnel,       475);
            H9.AddIntelReportStat(WeaponType.HEL_H9,           54);

            // Handle the icon profile.
            H9.IconProfile = new RegimentIconProfile(RegimentIconType.Helo_Animation)
            {
                W = SpriteManager.CH_H9_Frame0,
                NW = SpriteManager.CH_H9_Frame1,
                SW = SpriteManager.CH_H9_Frame2,
                W_F = SpriteManager.CH_H9_Frame3,
                NW_F = SpriteManager.CH_H9_Frame4,
                SW_F = SpriteManager.CH_H9_Frame5
            };

            // Add the H-9 profile to the database
            AddProfile(WeaponType.HEL_H9, H9);
            //----------------------------------------------
            // Chinese H-9 Attack Helicopter
            //----------------------------------------------

            #endregion // Helicopters

            #region Jets

            //----------------------------------------------
            // Chinese J-7 Fighter
            //----------------------------------------------
            WeaponProfile J7 = new WeaponProfile(
                _longName: "J-7 Fighter",
                _shortName: "J-7",
                _type: WeaponType.FGT_J7,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.EARLY_FGT_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.EARLY_FGT_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.EARLY_FGT_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.EARLY_FGT_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_NA,                            // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Day,                // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 324                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            J7.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.FGT);

            // Intel stats
            J7.AddIntelReportStat(WeaponType.FGT_J7,     36);

            // Handle the icon profile.
            J7.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.CH_J7
            };

            // Add the J-7 profile to the database
            AddProfile(WeaponType.FGT_J7, J7);
            //----------------------------------------------
            // Chinese J-7 Fighter
            //----------------------------------------------

            //----------------------------------------------
            // Chinese J-8 Interceptor
            //----------------------------------------------
            WeaponProfile J8 = new WeaponProfile(
                _longName: "J-8 Interceptor",
                _shortName: "J-8",
                _type: WeaponType.FGT_J8,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.MID_FGT_DOGFIGHT,                            // Dogfighting Rating
                _man: GameData.MID_FGT_MANEUVER,                           // Maneuverability Rating
                _topSpd: GameData.MID_FGT_TOPSPEED + SMALL_BONUS,          // Top Speed Rating
                _surv: GameData.MID_FGT_SURVIVE,                           // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_1,                        // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD,                               // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_ENHANCED,        // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Night,              // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.FGT,             // Upgrade Path
                _turnAvailable: 504                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            J8.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.FGT);

            // Intel stats
            J8.AddIntelReportStat(WeaponType.FGT_J8,     36);

            // Handle the icon profile.
            J8.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.CH_J8
            };

            // Add the J-8 profile to the database
            AddProfile(WeaponType.FGT_J8, J8);
            //----------------------------------------------
            // Chinese J-8 Interceptor
            //----------------------------------------------

            //----------------------------------------------
            // Chinese Q-5 Fantan Attack Aircraft
            //----------------------------------------------
            WeaponProfile Q5 = new WeaponProfile(
                _longName: "Q-5 Fantan Attack Aircraft",
                _shortName: "Q-5 Fantan",
                _type: WeaponType.ATT_Q5,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_ATTACK_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_ATTACK_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_ATTACK_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.AC_ATTACK_SURVIVE,                         // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_0 + MEDIUM_BONUS,         // Ground Attack Rating
                _ol: GameData.SMALL_AC_LOAD + SMALL_BONUS,                 // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Day,                // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Medium,               // Unit Silhouette
                _upgradePath: UpgradePath.ATT,             // Upgrade Path
                _turnAvailable: 384                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            Q5.SetPrestigeCost(PrestigeTierCost.Gen2, PrestigeTypeCost.ATT);

            // Intel stats
            Q5.AddIntelReportStat(WeaponType.ATT_Q5,     36);

            // Handle the icon profile.
            Q5.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.CH_Q5
            };

            // Add the Q-5 profile to the database
            AddProfile(WeaponType.ATT_Q5, Q5);
            //----------------------------------------------
            // Chinese Q-5 Fantan Attack Aircraft
            //----------------------------------------------

            //----------------------------------------------
            // Chinese H-6 Bomber
            //----------------------------------------------
            WeaponProfile H6 = new WeaponProfile(
                _longName: "H-6 Bomber",
                _shortName: "H-6",
                _type: WeaponType.BMB_H6,
                _hardAtt: 0,                                               // Hard Attack Rating
                _hardDef: 0,                                               // Hard Defense Rating
                _softAtt: 0,                                               // Soft Attack Rating
                _softDef: 0,                                               // Soft Defense Rating
                _gat: 0,                                                   // Ground-to-Air Attack Rating
                _gad: 0,                                                   // Ground Defense Armor Rating
                _df: GameData.AC_BOMBER_DOGFIGHT,                          // Dogfighting Rating
                _man: GameData.AC_BOMBER_MANEUVER,                         // Maneuverability Rating
                _topSpd: GameData.AC_BOMBER_TOPSPEED,                      // Top Speed Rating
                _surv: GameData.AC_BOMBER_SURVIVE + MEDIUM_BONUS,          // Survivability Rating
                _ga: GameData.GROUND_ATTACK_TIER_1,                        // Ground Attack Rating
                _ol: GameData.XLARGE_AC_LOAD,                              // Ordinance Rating
                _stealth: 0,                                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.AC_SPOTTING_BASIC,           // Spotting Range
                _mmp: GameData.FIXEDWING_UNIT,             // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.Day,                // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.None,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Large,                // Unit Silhouette
                _upgradePath: UpgradePath.BMB,             // Upgrade Path
                _turnAvailable: 372                        // How many months past Jan. 1938
            );

            // Set the prestige cost for the profile.
            H6.SetPrestigeCost(PrestigeTierCost.Gen1, PrestigeTypeCost.BMB);

            // Intel stats
            H6.AddIntelReportStat(WeaponType.BMB_H6,     24);

            // Handle the icon profile.
            H6.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.CH_H6
            };

            // Add the H-6 profile to the database
            AddProfile(WeaponType.BMB_H6, H6);
            //----------------------------------------------
            // Chinese H-6 Bomber
            //----------------------------------------------

            #endregion // Jets

            #region Infantry Units

            //----------------------------------------------
            // Chinese Regular Infantry
            //----------------------------------------------
            WeaponProfile INF_REG_CH_P = new WeaponProfile(
                _longName: "Chinese Regular Infantry",
                _shortName: "PLA Regulars",
                _type: WeaponType.INF_REG_CH,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.None,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_REG_CH_P.AddIntelReportStat(WeaponType.Personnel,       2200);
            INF_REG_CH_P.AddIntelReportStat(WeaponType.ART_155MM_FG,      18);
            INF_REG_CH_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,  12);
            INF_REG_CH_P.AddIntelReportStat(WeaponType.ART_82MM_MORTAR,   12);
            INF_REG_CH_P.AddIntelReportStat(WeaponType.AT_ATGM,           18);
            INF_REG_CH_P.AddIntelReportStat(WeaponType.MANPAD_STRELA,     24);
            INF_REG_CH_P.AddIntelReportStat(WeaponType.SPAAA_TYPE53,       6);

            // Handle the icon profile.
            INF_REG_CH_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.CH_Infantry
            };

            // Add the Chinese Regular Infantry profile to the database
            AddProfile(WeaponType.INF_REG_CH, INF_REG_CH_P);
            //----------------------------------------------
            // Chinese Regular Infantry
            //----------------------------------------------

            //----------------------------------------------
            // Chinese Airborne Infantry
            //----------------------------------------------
            WeaponProfile INF_AB_CH_P = new WeaponProfile(
                _longName: "Chinese Airborne Infantry",
                _shortName: "PLA Airborne",
                _type: WeaponType.INF_AB_CH,
                _hardAtt: GameData.BASE_INF_HARD_ATTACK,                    // Hard Attack Rating
                _hardDef: GameData.BASE_INF_HARD_DEFENSE,                   // Hard Defense Rating
                _softAtt: GameData.BASE_INF_SOFT_ATTACK,                    // Soft Attack Rating
                _softDef: GameData.BASE_INF_SOFT_DEFENSE,                   // Soft Defense Rating
                _gat: GameData.GROUND_AIR_ATTACK_DEFAULT,  // Ground-to-Air Attack Rating
                _gad: GameData.GROUND_DEFENSE_INFANTRY,    // Ground Defense Armor Rating
                _df: 0,                                    // Dogfighting Rating
                _man: 0,                                   // Maneuverability Rating
                _topSpd: 0,                                // Top Speed Rating
                _surv: 0,                                  // Survivability Rating
                _ga: 0,                                    // Ground Attack Rating
                _ol: 0,                                    // Ordinance Rating
                _stealth: 0,                               // Stealth Rating
                _pr: GameData.PRIMARY_RANGE_DEFAULT,       // Primary Range
                _ir: GameData.INDIRECT_RANGE_DEFAULT,      // Indirect Range
                _sr: GameData.BASE_UNIT_SPOTTING_RANGE,    // Spotting Range
                _mmp: GameData.FOOT_UNIT,                  // Max Movement Points
                _isAmph: false,                            // Is Amphibious
                _isDF: false,                              // Is DoubleFire
                _isAtt: true,                              // Can this profile attack
                _awr: AllWeatherRating.GroundUnit,         // All-Weather Capability
                _sir: SIGINT_Rating.UnitLevel,             // SIGINT Rating
                _nbc: NBC_Rating.Gen1,                     // NBC Rating
                _nvg: NVG_Rating.Gen1,                     // NVG Rating
                _sil: UnitSilhouette.Small                 // Unit Silhouette
            );

            // Intel stats
            INF_AB_CH_P.AddIntelReportStat(WeaponType.Personnel,       1800);
            INF_AB_CH_P.AddIntelReportStat(WeaponType.ART_122MM_FG,      18);
            INF_AB_CH_P.AddIntelReportStat(WeaponType.ART_120MM_MORTAR,  12);
            INF_AB_CH_P.AddIntelReportStat(WeaponType.ART_82MM_MORTAR,   12);
            INF_AB_CH_P.AddIntelReportStat(WeaponType.AT_ATGM,           36);
            INF_AB_CH_P.AddIntelReportStat(WeaponType.MANPAD_STRELA,     30);

            // Handle the icon profile.
            INF_AB_CH_P.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.CH_Airborne
            };

            // Add the Chinese Airborne Infantry profile to the database
            AddProfile(WeaponType.INF_AB_CH, INF_AB_CH_P);
            //----------------------------------------------
            // Chinese Airborne Infantry
            //----------------------------------------------

            #endregion // Infantry Units
        }
    }
}
