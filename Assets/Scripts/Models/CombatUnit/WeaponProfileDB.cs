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
//     - UpgradePath, turnAvailable
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
            // Phase 3 (Appendix W §16, validated worked line): Gen1 + LOW_PROFILE + NBC_PROTECTED
            // → HA7 HD6 SA5 SD7 GAD7 · ICM 1.00 · MMP10 · PR1.
            WeaponProfile T55A = WeaponProfile.FromProfileDef(
                "T-55A Main Battle Tank", "T-55A", WeaponType.TANK_T55A_SV,
                new ProfileDef(TankArchetypes.Gen1,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.LOW_PROFILE, WeaponTrait.NBC_PROTECTED }),
                UpgradePath.TANK, 240);

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
            // Phase 3 (derived): Gen1 + HA+1 (115mm U-5TS up-gun, off-norm) + LOW_PROFILE + NBC_PROTECTED
            // → HA8 HD6 SA5 SD7 GAD7 · ICM 1.00 · MMP10 · PR1.
            WeaponProfile T62A = WeaponProfile.FromProfileDef(
                "T-62A Main Battle Tank", "T-62A", WeaponType.TANK_T62A_SV,
                new ProfileDef(TankArchetypes.Gen1,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, 1 } },
                    new[] { WeaponTrait.LOW_PROFILE, WeaponTrait.NBC_PROTECTED }),
                UpgradePath.TANK, 276);

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
            // Phase 3 (derived): Gen2 + GUN_125_SMOOTH (2A46, off-norm up-gun) + LOW_PROFILE; early
            // optical rangefinder, no laser FCS → ICM 1.00.
            // → HA12 HD9 SA7 SD7 GAD7 · ICM 1.00 · MMP10 · PR1.
            WeaponProfile T64A = WeaponProfile.FromProfileDef(
                "T-64A Main Battle Tank", "T-64A", WeaponType.TANK_T64A_SV,
                new ProfileDef(TankArchetypes.Gen2,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.GUN_125_SMOOTH, WeaponTrait.LOW_PROFILE }),
                UpgradePath.TANK, 336);

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
            // Phase 3 (Appendix W §16 validated "T-64BV" line; the in-game T-64B is the ERA-equipped
            // service variant): Gen3 + ERA_LIGHT + GUN_LAUNCHED_ATGM (Kobra, standoff PR+1) + LOW_PROFILE
            // + LASER_RANGEFINDER + BALLISTIC_COMPUTER.
            // → HA15 HD14 SA9 SD7 GAD7 · ICM 1.10 · MMP10 · PR2.
            WeaponProfile T64B = WeaponProfile.FromProfileDef(
                "T-64B Main Battle Tank", "T-64B", WeaponType.TANK_T64B_SV,
                new ProfileDef(TankArchetypes.Gen3,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ERA_LIGHT, WeaponTrait.GUN_LAUNCHED_ATGM, WeaponTrait.LOW_PROFILE,
                            WeaponTrait.LASER_RANGEFINDER, WeaponTrait.BALLISTIC_COMPUTER }),
                UpgradePath.TANK, 456);

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
            // Phase 3 (Appendix W §16, validated worked line): Gen2 + GUN_125_SMOOTH + SPACED_ARMOR + LRF.
            // AMPHIBIOUS trait restores the old _isAmph flag (Bob: snorkel deep-wading deployed correctly =
            // amphibious effect in-game) — IsAmphibious now derived from the trait, the flag is retired (R9).
            // → HA12 HD9 SA7 SD6 GAD7 · ICM 1.05 · MMP10 · PR1 · amphibious.
            WeaponProfile T72A = WeaponProfile.FromProfileDef(
                "T-72A Main Battle Tank", "T-72A", WeaponType.TANK_T72A_SV,
                new ProfileDef(TankArchetypes.Gen2,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.GUN_125_SMOOTH, WeaponTrait.SPACED_ARMOR, WeaponTrait.LASER_RANGEFINDER,
                            WeaponTrait.AMPHIBIOUS }),
                UpgradePath.TANK, 420);

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
            // Phase 3 (derived): Gen3 + GUN_LAUNCHED_ATGM (Svir, standoff PR+1) + ERA_HEAVY (Kontakt-5,
            // T-72B late) + LOW_PROFILE + LASER_RANGEFINDER (1A40, simpler FCS than the T-64B's full
            // LRF+BC → ICM 1.05) + AMPHIBIOUS (restores old _isAmph via trait; flag retired, R9).
            // → HA15 HD15 SA9 SD7 GAD7 · ICM 1.05 · MMP10 · PR2 · amphibious.
            WeaponProfile T72B = WeaponProfile.FromProfileDef(
                "T-72B Main Battle Tank", "T-72B", WeaponType.TANK_T72B_SV,
                new ProfileDef(TankArchetypes.Gen3,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.GUN_LAUNCHED_ATGM, WeaponTrait.ERA_HEAVY, WeaponTrait.LOW_PROFILE,
                            WeaponTrait.LASER_RANGEFINDER, WeaponTrait.AMPHIBIOUS }),
                UpgradePath.TANK, 564);

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
            // Phase 3 (Appendix W §16, validated worked line): Gen2 + HA+1 + GUN_LAUNCHED_ATGM (Kobra,
            // standoff PR+1) + COMPOSITE_CERAMIC + LRF + BC + GAS_TURBINE (MMP+2).
            // → HA13 HD10 SA7 SD6 GAD7 · ICM 1.10 · MMP12 · PR2.
            WeaponProfile T80B = WeaponProfile.FromProfileDef(
                "T-80B Main Battle Tank", "T-80B", WeaponType.TANK_T80B_SV,
                new ProfileDef(TankArchetypes.Gen2,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, 1 } },
                    new[] { WeaponTrait.GUN_LAUNCHED_ATGM, WeaponTrait.COMPOSITE_CERAMIC,
                            WeaponTrait.LASER_RANGEFINDER, WeaponTrait.BALLISTIC_COMPUTER, WeaponTrait.GAS_TURBINE }),
                UpgradePath.TANK, 480);

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
            // Phase 3 (derived, Soviet apex): Gen3 + GUN_LAUNCHED_ATGM (Refleks, standoff PR+1) +
            // APFSDS_ADVANCED (3BM-32, off-norm premium round) + ERA_HEAVY (Kontakt-5) + LOW_PROFILE
            // + GAS_TURBINE (MMP+2) + LRF + BC. Out-guns the T-72B; NATO Gen4 still edges it on ICM.
            // → HA17 HD15 SA9 SD7 GAD7 · ICM 1.10 · MMP12 · PR2.
            WeaponProfile T80U = WeaponProfile.FromProfileDef(
                "T-80U Main Battle Tank", "T-80U", WeaponType.TANK_T80U_SV,
                new ProfileDef(TankArchetypes.Gen3,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.GUN_LAUNCHED_ATGM, WeaponTrait.APFSDS_ADVANCED, WeaponTrait.ERA_HEAVY,
                            WeaponTrait.LOW_PROFILE, WeaponTrait.GAS_TURBINE,
                            WeaponTrait.LASER_RANGEFINDER, WeaponTrait.BALLISTIC_COMPUTER }),
                UpgradePath.TANK, 564);

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
            // Phase 3 (derived) — the game's APEX tank ("gen4+", deliberately above T-80U): Gen4 +
            // GUN_LAUNCHED_ATGM (Refleks, standoff PR+1) + APFSDS_ADVANCED (off-norm premium round) +
            // ERA_RELIKT (3rd-gen reactive armor) + ACTIVE_PROTECTION_HARDKILL (hard-kill APS) + LOW_PROFILE
            // + GAS_TURBINE (MMP+2) + LASER_RANGEFINDER + BALLISTIC_COMPUTER + THERMAL_IMAGER (Sosna-U — the
            // modern FCS edge Soviet tanks otherwise lacked). Display renamed T-80BV → T-80BVM (sprites match).
            // → HA20 HD20 SA10 SD7 GAD7 · ICM 1.21 · MMP12 · PR2 · SR3.
            WeaponProfile T80BV = WeaponProfile.FromProfileDef(
                "T-80BVM Main Battle Tank", "T-80BVM", WeaponType.TANK_T80BV_SV,
                new ProfileDef(TankArchetypes.Gen4,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.GUN_LAUNCHED_ATGM, WeaponTrait.APFSDS_ADVANCED, WeaponTrait.ERA_RELIKT,
                            WeaponTrait.ACTIVE_PROTECTION_HARDKILL, WeaponTrait.LOW_PROFILE, WeaponTrait.GAS_TURBINE,
                            WeaponTrait.LASER_RANGEFINDER, WeaponTrait.BALLISTIC_COMPUTER, WeaponTrait.THERMAL_IMAGER }),
                UpgradePath.TANK, 584);

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
            // Phase 3 (derived): Ifv + ATGM_RAIL (Konkurs on the BMP-1P, HA+4) + AMPHIBIOUS.
            // → HA8 HD4 SA8 SD7 GAD7 · ICM 1.00 · MMP10 · SR2 · PR1 · amphibious.
            WeaponProfile BMP1 = WeaponProfile.FromProfileDef(
                "BMP-1P Infantry Fighting Vehicle", "BMP-1P", WeaponType.IFV_BMP1_SV,
                new ProfileDef(FamilyArchetypes.Ifv,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ATGM_RAIL, WeaponTrait.AMPHIBIOUS }),
                UpgradePath.IFV, 336);

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
            // Phase 3 (derived): Ifv + AUTOCANNON_HEAVY (30mm 2A42, SA+1/HA+1) + ATGM_RAIL (Konkurs, HA+4)
            // + AMPHIBIOUS. The premier Soviet tank-killing IFV.
            // → HA9 HD4 SA9 SD7 GAD7 · ICM 1.00 · MMP10 · SR2 · PR1 · amphibious.
            WeaponProfile BMP2 = WeaponProfile.FromProfileDef(
                "BMP-2 Infantry Fighting Vehicle", "BMP-2", WeaponType.IFV_BMP2_SV,
                new ProfileDef(FamilyArchetypes.Ifv,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.AUTOCANNON_HEAVY, WeaponTrait.ATGM_RAIL, WeaponTrait.AMPHIBIOUS }),
                UpgradePath.IFV, 504);

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
            // Phase 3 (derived): Ifv + HD+1/SD+1 (heavier, better-protected hull) + AUTOCANNON_HEAVY (30mm) +
            // ATGM_RAIL (HA+4) + CANISTER_HE (100mm 2A70 HE, SA+1) + AMPHIBIOUS. Best-armed/armoured BMP.
            // (The 100mm Bastion could instead be modelled as GUN_LAUNCHED_ATGM for a PR2 standoff — left as
            // the simpler rail+HE build for now.)
            // → HA9 HD5 SA10 SD8 GAD7 · ICM 1.00 · MMP10 · SR2 · PR1 · amphibious.
            WeaponProfile BMP3 = WeaponProfile.FromProfileDef(
                "BMP-3 Infantry Fighting Vehicle", "BMP-3", WeaponType.IFV_BMP3_SV,
                new ProfileDef(FamilyArchetypes.Ifv,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HD, 1 }, { ProfileStat.SD, 1 } },
                    new[] { WeaponTrait.AUTOCANNON_HEAVY, WeaponTrait.ATGM_RAIL, WeaponTrait.CANISTER_HE,
                            WeaponTrait.AMPHIBIOUS }),
                UpgradePath.IFV, 588);

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
            // Phase 3 (derived): Ifv + HD-1 (thin airborne hull) + AUTOCANNON_HEAVY (30mm) + ATGM_RAIL (Konkurs)
            // + AIR_DROPPABLE + AMPHIBIOUS. BMP-2 firepower on an air-droppable, lightly-armoured chassis.
            // → HA9 HD3 SA9 SD7 GAD7 · ICM 1.00 · MMP10 · SR2 · PR1 · amphibious · air-droppable.
            WeaponProfile BMD2 = WeaponProfile.FromProfileDef(
                "BMD-2 Airborne IFV", "BMD-2", WeaponType.IFV_BMD2_SV,
                new ProfileDef(FamilyArchetypes.Ifv,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HD, -1 } },
                    new[] { WeaponTrait.AUTOCANNON_HEAVY, WeaponTrait.ATGM_RAIL, WeaponTrait.AIR_DROPPABLE,
                            WeaponTrait.AMPHIBIOUS }),
                UpgradePath.IFV, 564);

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
            // Phase 3 (derived): Ifv + HD-1 (thin airborne hull) + SA+1 (added AGS-17 auto-grenade) +
            // AUTOCANNON_HEAVY (30mm) + ATGM_RAIL (Konkurs) + AIR_DROPPABLE + AMPHIBIOUS. Upgraded BMD.
            // → HA9 HD3 SA10 SD7 GAD7 · ICM 1.00 · MMP10 · SR2 · PR1 · amphibious · air-droppable.
            WeaponProfile BMD3 = WeaponProfile.FromProfileDef(
                "BMD-3 Airborne IFV", "BMD-3", WeaponType.IFV_BMD3_SV,
                new ProfileDef(FamilyArchetypes.Ifv,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HD, -1 }, { ProfileStat.SA, 1 } },
                    new[] { WeaponTrait.AUTOCANNON_HEAVY, WeaponTrait.ATGM_RAIL, WeaponTrait.AIR_DROPPABLE,
                            WeaponTrait.AMPHIBIOUS }),
                UpgradePath.IFV, 624);

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
            // Phase 3 (derived): Apc + AMPHIBIOUS. Bare utility carrier (7.62mm MG). MMP 8 = Apc archetype
            // (was 10 — the ratified APC baseline is 8).
            // → HA3 HD4 SA6 SD7 GAD7 · ICM 1.00 · MMP8 · SR2 · PR1 · amphibious.
            WeaponProfile MTLB = WeaponProfile.FromProfileDef(
                "MT-LB Armored Personnel Carrier", "MT-LB", WeaponType.APC_MTLB_SV,
                new ProfileDef(FamilyArchetypes.Apc,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.AMPHIBIOUS }),
                UpgradePath.APC, 312);

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
            // Phase 3 (derived): Apc + SD+1 + AMPHIBIOUS. Wheeled carrier, 14.5mm KPVT HMG.
            // → HA3 HD4 SA6 SD8 GAD7 · ICM 1.00 · MMP8 · SR2 · PR1 · amphibious.
            WeaponProfile BTR70 = WeaponProfile.FromProfileDef(
                "BTR-70 Armored Personnel Carrier", "BTR-70", WeaponType.APC_BTR70_SV,
                new ProfileDef(FamilyArchetypes.Apc,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SD, 1 } },
                    new[] { WeaponTrait.AMPHIBIOUS }),
                UpgradePath.APC, 408);

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
            // Phase 3 (derived): Apc + SA+1/SD+1 + AMPHIBIOUS. Improved wheeled carrier (14.5mm + better hull).
            // → HA3 HD4 SA7 SD8 GAD7 · ICM 1.00 · MMP8 · SR2 · PR1 · amphibious.
            WeaponProfile BTR80 = WeaponProfile.FromProfileDef(
                "BTR-80 Armored Personnel Carrier", "BTR-80", WeaponType.APC_BTR80_SV,
                new ProfileDef(FamilyArchetypes.Apc,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, 1 }, { ProfileStat.SD, 1 } },
                    new[] { WeaponTrait.AMPHIBIOUS }),
                UpgradePath.APC, 576);

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
            // Phase 3 (derived): Recon archetype (hardened HD5/SD9, SR 3) + AMPHIBIOUS + RECON_FRAGILE (ICM ×0.6
            // — the R6 "don't brawl" offense penalty). A pure scout: survives the first hit, sees far, fights poorly.
            // → HA2 HD5 SA5 SD9 GAD7 · ICM 0.60 · MMP10 · SR3 · PR1 · amphibious.
            WeaponProfile BRDM2 = WeaponProfile.FromProfileDef(
                "BRDM-2 Recon Vehicle", "BRDM-2", WeaponType.RCN_BRDM2_SV,
                new ProfileDef(FamilyArchetypes.Recon,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.AMPHIBIOUS, WeaponTrait.RECON_FRAGILE }),
                UpgradePath.RCN, 288);

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
            // Phase 3 (derived): Recon archetype (hardened HD5/SD9, SR 3) + ATGM_RAIL (AT-5 Konkurs, HA+4) +
            // AMPHIBIOUS. NO RECON_FRAGILE — a survivable tank-destroyer scout that fights at range (Hard target,
            // set below) and withdraws. Konkurs gives the standoff AT punch.
            // → HA6 HD5 SA5 SD9 GAD7 · ICM 1.00 · MMP10 · SR3 · PR1 · amphibious · Hard target.
            WeaponProfile BRDM2AT = WeaponProfile.FromProfileDef(
                "BRDM-2 AT-5 Recon Vehicle", "BRDM-2 AT", WeaponType.RCN_BRDM2AT_SV,
                new ProfileDef(FamilyArchetypes.Recon,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ATGM_RAIL, WeaponTrait.AMPHIBIOUS }),
                UpgradePath.RCN, 336);

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
            // W1: armored-car recon fights as a Hard target (§7.4.1.2 override).
            BRDM2AT.SetTargetClass(TargetClass.Hard);
            AddProfile(WeaponType.RCN_BRDM2AT_SV, BRDM2AT);
            //----------------------------------------------
            // Soviet BRDM-2 AT-5 Recon Vehicle
            //----------------------------------------------

            #endregion // Recon

            #region Self-Propelled Artillery

            //----------------------------------------------
            // Soviet 2S1 Gvozdika Self-Propelled Artillery
            //----------------------------------------------
            // Phase 3 (derived): Artillery archetype + SELF_PROPELLED (tracked chassis: MMP+6→10, HD/SD+2, GAD-1)
            // + IR SHORT. 122mm light SP howitzer.
            // → HA5 HD7 SA9 SD7 GAD7 · ICM 1.00 · MMP10 · IR4 · SR2.
            WeaponProfile SPA2S1 = WeaponProfile.FromProfileDef(
                "2S1 Gvozdika Self-Propelled Artillery", "2S1 Gvozdika", WeaponType.SPA_2S1_SV,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SHORT } },
                    new[] { WeaponTrait.SELF_PROPELLED }),
                UpgradePath.ART, 396);

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
            // Phase 3 (derived): Artillery + SELF_PROPELLED + IR MEDIUM + SA+1 (152mm heavier shell).
            // → HA5 HD7 SA10 SD7 GAD7 · ICM 1.00 · MMP10 · IR5 · SR2.
            WeaponProfile SPA2S3 = WeaponProfile.FromProfileDef(
                "2S3 Akatsiya Self-Propelled Artillery", "2S3 Akatsiya", WeaponType.SPA_2S3_SV,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_MEDIUM }, { ProfileStat.SA, 1 } },
                    new[] { WeaponTrait.SELF_PROPELLED }),
                UpgradePath.ART, 396);

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
            // Phase 3 (derived): Artillery + SELF_PROPELLED + IR LONG + SA+1 + HA+1 (long-range 152mm 2A37
            // high-velocity gun — best reach, some counter-battery/direct punch).
            // → HA6 HD7 SA10 SD7 GAD7 · ICM 1.00 · MMP10 · IR6 · SR2.
            WeaponProfile SPA2S5 = WeaponProfile.FromProfileDef(
                "2S5 Giatsint-S Self-Propelled Artillery", "2S5 Giatsint-S", WeaponType.SPA_2S5_SV,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_LONG }, { ProfileStat.SA, 1 }, { ProfileStat.HA, 1 } },
                    new[] { WeaponTrait.SELF_PROPELLED }),
                UpgradePath.ART, 456);

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
            // Phase 3 (derived): Artillery + SELF_PROPELLED + IR MEDIUM + SMART_MUNITION (Krasnopol laser-guided,
            // HA+3/SA+1 — anti-armour bite). Modern apex SP howitzer; precision over raw range (2S5 reaches further).
            // → HA8 HD7 SA10 SD7 GAD7 · ICM 1.00 · MMP10 · IR5 · SR2.
            WeaponProfile SPA2S19 = WeaponProfile.FromProfileDef(
                "2S19 Msta-S Self-Propelled Artillery", "2S19 Msta-S", WeaponType.SPA_2S19_SV,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_MEDIUM } },
                    new[] { WeaponTrait.SELF_PROPELLED, WeaponTrait.SMART_MUNITION }),
                UpgradePath.ART, 612);

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
            // Phase 3 (derived): Artillery archetype bare (towed = foot, MMP 4) + IR SHORT.
            // → HA5 HD5 SA9 SD5 GAD8 · ICM 1.00 · MMP4 · IR4 · SR2.
            WeaponProfile ArtLight = WeaponProfile.FromProfileDef(
                "Light Towed Artillery", "Lt Artillery", WeaponType.ART_LIGHT_SV,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SHORT } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.ART, 60);

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
            // Phase 3 (derived): Artillery archetype (towed = foot, MMP 4) + IR MEDIUM + SA+1 (heavier tube).
            // → HA5 HD5 SA10 SD5 GAD8 · ICM 1.00 · MMP4 · IR5 · SR2.
            WeaponProfile ArtHeavy = WeaponProfile.FromProfileDef(
                "Heavy Towed Artillery", "Hvy Artillery", WeaponType.ART_HEAVY_SV,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_MEDIUM }, { ProfileStat.SA, 1 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.ART, 60);

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
            // Phase 3 (derived): Artillery + TRUCK_MOUNTED (wheeled: MMP+4→8, GAD-2→6, soft/air-vulnerable) +
            // ROCKET_ARTILLERY (salvo → +1 CombatAction, derives IsDoubleFire) + IR ROC_SR. Dumb 122mm area rockets.
            // → HA5 HD5 SA9 SD5 GAD6 · ICM 1.00 · MMP8 · IR4 · SR2 · double-fire.
            WeaponProfile BM21 = WeaponProfile.FromProfileDef(
                "BM-21 Grad Multiple Launch Rocket System", "BM-21 Grad", WeaponType.ROC_BM21_SV,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_ROC_SR } },
                    new[] { WeaponTrait.TRUCK_MOUNTED, WeaponTrait.ROCKET_ARTILLERY }),
                UpgradePath.ROC, 300);

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
            // Phase 3 (derived): Artillery + TRUCK_MOUNTED + ROCKET_ARTILLERY + SMART_MUNITION (220mm DPICM
            // bomblets → anti-armour bite, HA+3/SA+1) + SA+1 + IR ROC_MR.
            // → HA8 HD5 SA11 SD5 GAD6 · ICM 1.00 · MMP8 · IR6 · SR2 · double-fire.
            WeaponProfile BM27 = WeaponProfile.FromProfileDef(
                "BM-27 Uragan Multiple Launch Rocket System", "BM-27 Uragan", WeaponType.ROC_BM27_SV,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_ROC_MR }, { ProfileStat.SA, 1 } },
                    new[] { WeaponTrait.TRUCK_MOUNTED, WeaponTrait.ROCKET_ARTILLERY, WeaponTrait.SMART_MUNITION }),
                UpgradePath.ROC, 444);

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
            // Phase 3 (derived): Artillery + TRUCK_MOUNTED + ROCKET_ARTILLERY + SMART_MUNITION (300mm bomblets) +
            // SA+2 (massive 300mm warheads) + IR ROC_LR (longest reach). Apex Soviet MRL.
            // → HA8 HD5 SA12 SD5 GAD6 · ICM 1.00 · MMP8 · IR10 · SR2 · double-fire.
            WeaponProfile BM30 = WeaponProfile.FromProfileDef(
                "BM-30 Smerch Multiple Launch Rocket System", "BM-30 Smerch", WeaponType.ROC_BM30_SV,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_ROC_LR }, { ProfileStat.SA, 2 } },
                    new[] { WeaponTrait.TRUCK_MOUNTED, WeaponTrait.ROCKET_ARTILLERY, WeaponTrait.SMART_MUNITION }),
                UpgradePath.ROC, 588);

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
            // Phase 3 (derived): Artillery + TRUCK_MOUNTED + HA+6 / SA+6 deltas → the R3 ballistic-missile
            // statline (HA 11 anti-armour, SA 15 — bypasses terrain, threatens dug-in hard targets). NO
            // ROCKET_ARTILLERY: a single big missile, not a salvo (W5 excludes Scud from double-fire).
            // (Large one-off deltas are fine for a unique profile; promote to a BALLISTIC_MISSILE trait if a 2nd Scud appears.)
            // → HA11 HD5 SA15 SD5 GAD6 · ICM 1.00 · MMP8 · IR10 · SR2 (single-fire).
            WeaponProfile SCUD = WeaponProfile.FromProfileDef(
                "9K72 Scud-B Tactical Ballistic Missile Launcher", "9K72 Scud-B", WeaponType.ROC_SCUD_SV,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_ROC_LR }, { ProfileStat.HA, 6 }, { ProfileStat.SA, 6 } },
                    new[] { WeaponTrait.TRUCK_MOUNTED }),
                UpgradePath.ROC, 324);

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
            // Phase 3 (derived): Aaa archetype + SELF_PROPELLED (tracked) + IR AAA. Optically-aimed twin 57mm —
            // no radar, so it stays at the base AAA gunnery (RADAR_GUIDED_GUN is what elevates the Shilka).
            // → HA4 HD6 SA9 SD8 GAD11 · GAT9 · MMP10 · IR3 · SR3.
            WeaponProfile ZSU57 = WeaponProfile.FromProfileDef(
                "ZSU-57-2 Sparka Self-Propelled Anti-Aircraft Gun", "ZSU-57-2 Sparka", WeaponType.SPAAA_ZSU57_SV,
                new ProfileDef(FamilyArchetypes.Aaa,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_AAA } },
                    new[] { WeaponTrait.SELF_PROPELLED }),
                UpgradePath.AAA, 204);

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
            // Phase 3 (derived): Aaa + SELF_PROPELLED + RADAR_GUIDED_GUN (Gun-Dish radar, GAT+2) + IR AAA.
            // → HA4 HD6 SA9 SD8 GAD11 · GAT11 · MMP10 · IR3 · SR3.
            WeaponProfile ZSU23 = WeaponProfile.FromProfileDef(
                "ZSU-23-4 Shilka Self-Propelled Anti-Aircraft Gun", "ZSU-23-4 Shilka", WeaponType.SPAAA_ZSU23_SV,
                new ProfileDef(FamilyArchetypes.Aaa,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_AAA } },
                    new[] { WeaponTrait.SELF_PROPELLED, WeaponTrait.RADAR_GUIDED_GUN }),
                UpgradePath.AAA, 324);

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
            // Phase 3 (derived): Aaa + SELF_PROPELLED + RADAR_GUIDED_GUN (GAT+2) + GUN_MISSILE_COMBO (gun+9M311
            // SAM, GAT+2/IR+2) + IR AAA base. Apex Soviet short-range AD (gun & missile).
            // → HA4 HD6 SA9 SD8 GAD11 · GAT13 · MMP10 · IR5 · SR3.
            WeaponProfile Tunguska = WeaponProfile.FromProfileDef(
                "2K22 Tunguska Self-Propelled Anti-Aircraft System", "2K22 Tunguska", WeaponType.SPSAM_2K22_SV,
                new ProfileDef(FamilyArchetypes.Aaa,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_AAA } },
                    new[] { WeaponTrait.SELF_PROPELLED, WeaponTrait.RADAR_GUIDED_GUN, WeaponTrait.GUN_MISSILE_COMBO }),
                UpgradePath.AAA, 528);

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
            // Phase 3 (derived): Sam archetype (air-only) + SELF_PROPELLED (tracked) + SARH_LONG_RANGE (radar-
            // illuminated medium reach, GAT+3) + MOBILE_SHOOT_SCOOT (relocate after firing) + IR SAM.
            // → HA1 HD5 SA1 SD5 GAD7 · GAT13 · MMP10 · IR6 · SR6 · shoot-scoot.
            WeaponProfile Kub = WeaponProfile.FromProfileDef(
                "2K12 Kub Self-Propelled SAM System", "2K12 Kub", WeaponType.SPSAM_2K12_SV,
                new ProfileDef(FamilyArchetypes.Sam,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SAM } },
                    new[] { WeaponTrait.SELF_PROPELLED, WeaponTrait.SARH_LONG_RANGE, WeaponTrait.MOBILE_SHOOT_SCOOT }),
                UpgradePath.SAM, 348);

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
            // Phase 3 (derived): Sam + TRUCK_MOUNTED (wheeled BRDM chassis, soft/air-vulnerable) + IR_HOMING
            // (passive IR, fire-and-forget, GAT+1) + AMPHIBIOUS (BRDM hull) + short IR. Cheap mobile point SAM.
            // → HA1 HD3 SA1 SD3 GAD6 · GAT11 · MMP8 · IR4 · SR6 · fire-and-forget · amphibious.
            WeaponProfile Strela1 = WeaponProfile.FromProfileDef(
                "9K31 Strela-1 Self-Propelled SAM System", "9K31 Strela-1", WeaponType.SPSAM_9K31_SV,
                new ProfileDef(FamilyArchetypes.Sam,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, 4 } },
                    new[] { WeaponTrait.TRUCK_MOUNTED, WeaponTrait.IR_HOMING, WeaponTrait.AMPHIBIOUS }),
                UpgradePath.SAM, 360);

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
            // Phase 3 (derived): Sam archetype, STATIC (MMP 4→0) + SARH_LONG_RANGE (radar-illuminated long reach,
            // GAT+3) + IR SAM. Classic high-altitude site SAM.
            // → HA1 HD3 SA1 SD3 GAD8 · GAT13 · MMP0 · IR6 · SR6.
            WeaponProfile S75 = WeaponProfile.FromProfileDef(
                "S-75 Dvina Surface-to-Air Missile System", "S-75 Dvina", WeaponType.SAM_S75_SV,
                new ProfileDef(FamilyArchetypes.Sam,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SAM }, { ProfileStat.MMP, -4 } },
                    new[] { WeaponTrait.SARH_LONG_RANGE }),
                UpgradePath.SAM, 228);

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
            // Phase 3 (derived): Sam archetype, STATIC + COMMAND_GUIDANCE (GAT+2) + IR 5. Low/medium-altitude
            // site SAM — shorter reach than the S-75.
            // → HA1 HD3 SA1 SD3 GAD8 · GAT12 · MMP0 · IR5 · SR6.
            WeaponProfile S125 = WeaponProfile.FromProfileDef(
                "S-125 Neva Surface-to-Air Missile System", "S-125 Neva", WeaponType.SAM_S125_SV,
                new ProfileDef(FamilyArchetypes.Sam,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, 5 }, { ProfileStat.MMP, -4 } },
                    new[] { WeaponTrait.COMMAND_GUIDANCE }),
                UpgradePath.SAM, 276);

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
            // Phase 3 (derived): Sam archetype + TVM_GUIDANCE (track-via-missile, GAT+4) + long IR 10 + SR+4
            // (large acquisition radar → SR 10). MMP +4 → MOT 8: the launchers ride integrated TEL trucks
            // (Bob: transported as part of the system, NOT towed/static), keeping the hardened GAD 8.
            // → HA1 HD3 SA1 SD3 GAD8 · GAT14 · MMP8 · IR10 · SR10.
            WeaponProfile S300 = WeaponProfile.FromProfileDef(
                "S-300 Surface-to-Air Missile System", "S-300", WeaponType.SAM_S300_SV,
                new ProfileDef(FamilyArchetypes.Sam,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, 10 }, { ProfileStat.SR, 4 }, { ProfileStat.MMP, GameData.MOT_UNIT - GameData.FOOT_UNIT } },
                    new[] { WeaponTrait.TVM_GUIDANCE }),
                UpgradePath.SAM, 480);

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
            // Phase 3 (derived): bare Aaa archetype (towed = foot, MMP 4) + a small malus (HA/SA/GAT -1) for the
            // cheap generic emplacement + IR AAA. No chassis trait; keeps the AAA GAD 12 (digs in, resists air).
            // → HA3 HD4 SA8 SD6 GAD12 · GAT8 · MMP4 · IR3 · SR3.
            WeaponProfile AAA_GEN = WeaponProfile.FromProfileDef(
                "Generic Anti-Aircraft Artillery Emplacement", "Generic AAA", WeaponType.AAA_GEN_SV,
                new ProfileDef(FamilyArchetypes.Aaa,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, -1 }, { ProfileStat.SA, -1 }, { ProfileStat.GAT, -1 }, { ProfileStat.IR, GameData.INDIRECT_RANGE_AAA } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.AAA, 144);

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
            // Phase 3 (derived): Helicopter archetype + NON_COMBATANT — the AM/MAM lift helo. Door guns only;
            // it carries troops, it doesn't initiate attacks (IsAttackCapable false via the trait).
            // → HA7 HD6 SA10 SD7 GAD10 · MMP24 · SR3 · non-combatant · helo-transport.
            WeaponProfile MI8T = WeaponProfile.FromProfileDef(
                "Mi-8T Hip Transport Helicopter", "Mi-8T Hip", WeaponType.HEL_MI8T_SV,
                new ProfileDef(FamilyArchetypes.Helicopter,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.NON_COMBATANT }),
                UpgradePath.HELT, 348);

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
            // W2: Mi-8T is the Soviet AM/MAM organic helo transport.
            MI8T.SetTransportCategory(TransportCategory.HeloTransport);
            AddProfile(WeaponType.HEL_MI8T_SV, MI8T);
            //----------------------------------------------
            // Soviet Mi-8T Hip Transport Helicopter
            //----------------------------------------------

            //----------------------------------------------
            // Soviet Mi-8AT Hip-C Attack Helicopter
            //----------------------------------------------
            // Phase 3 (derived): Helicopter + ROCKET_PODS + CANNON_HELO — an armed-transport fire-support helo:
            // strong anti-soft (rockets/guns), but no ATGM (HA stays base) — the Hinds are the tank-killers.
            // → HA7 HD6 SA13 SD7 GAD10 · MMP24 · SR3.
            WeaponProfile MI8AT = WeaponProfile.FromProfileDef(
                "Mi-8AT Hip-C Attack Helicopter", "Mi-8AT Hip-C", WeaponType.HEL_MI8AT_SV,
                new ProfileDef(FamilyArchetypes.Helicopter,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ROCKET_PODS, WeaponTrait.CANNON_HELO }),
                UpgradePath.HELT, 444);

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
            // Phase 3 (derived): Helicopter + CANNON_HELO + ROCKET_PODS + ATGM_HELO_SACLOS (Falanga, HA+4) +
            // ARMORED_COCKPIT (HD/SD+1). The classic Hind gunship — anti-armour + anti-soft, armoured.
            // → HA11 HD7 SA13 SD8 GAD10 · MMP24 · SR3.
            WeaponProfile MI24D = WeaponProfile.FromProfileDef(
                "Mi-24D Hind-D Attack Helicopter", "Mi-24D Hind-D", WeaponType.HEL_MI24D_SV,
                new ProfileDef(FamilyArchetypes.Helicopter,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.CANNON_HELO, WeaponTrait.ROCKET_PODS, WeaponTrait.ATGM_HELO_SACLOS,
                            WeaponTrait.ARMORED_COCKPIT }),
                UpgradePath.HEL, 408);

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
            // Phase 3 (derived): Mi-24D loadout + HELO_COUNTERMEASURES (flares/IRCM, GAD+2) + HA+1 (improved
            // Shturm ATGM). The Hind-E — survivable, harder-hitting.
            // → HA12 HD7 SA13 SD8 GAD12 · MMP24 · SR3.
            WeaponProfile MI24V = WeaponProfile.FromProfileDef(
                "Mi-24V Hind-E Attack Helicopter", "Mi-24V Hind-E", WeaponType.HEL_MI24V_SV,
                new ProfileDef(FamilyArchetypes.Helicopter,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, 1 } },
                    new[] { WeaponTrait.CANNON_HELO, WeaponTrait.ROCKET_PODS, WeaponTrait.ATGM_HELO_SACLOS,
                            WeaponTrait.ARMORED_COCKPIT, WeaponTrait.HELO_COUNTERMEASURES }),
                UpgradePath.HEL, 456);

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
            // Phase 3 (derived): Helicopter + ATGM_HELO_FNF (Ataka fire-and-forget, HA+5 + ICM×1.05) + CANNON_HELO
            // + ROCKET_PODS + ARMORED_COCKPIT + HELO_COUNTERMEASURES. Apex Soviet gunship — shoot-and-hide AT.
            // → HA12 HD7 SA13 SD8 GAD12 · ICM 1.05 · MMP24 · SR3.
            WeaponProfile MI28 = WeaponProfile.FromProfileDef(
                "Mi-28 Havoc Attack Helicopter", "Mi-28 Havoc", WeaponType.HEL_MI28_SV,
                new ProfileDef(FamilyArchetypes.Helicopter,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ATGM_HELO_FNF, WeaponTrait.CANNON_HELO, WeaponTrait.ROCKET_PODS,
                            WeaponTrait.ARMORED_COCKPIT, WeaponTrait.HELO_COUNTERMEASURES }),
                UpgradePath.HEL, 600);

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
            // Phase 3 (derived): Bomber archetype + NON_COMBATANT (fixed-wing transport). TS-3 (slow lifter), OL big.
            // → DF1 MAN3 TS7 SUR8 · OL12 · MMP100 · SR4 · non-combatant · fixed-wing transport.
            WeaponProfile AN12 = WeaponProfile.FromProfileDef(
                "An-12 Antonov Transport Plane", "An-12 Antonov", WeaponType.TRN_AN8_SV,
                new ProfileDef(FamilyArchetypes.Bomber,
                    new Dictionary<ProfileStat, int> { { ProfileStat.TS, -3 } },
                    new[] { WeaponTrait.NON_COMBATANT }),
                UpgradePath.TRN, 252);

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
            // W2: An-12 is the Soviet AB/MAB/SPECF fixed-wing transport (organic + inorganic TRN).
            AN12.SetTransportCategory(TransportCategory.FixedWingTransport);
            AddProfile(WeaponType.TRN_AN8_SV, AN12);
            //----------------------------------------------
            // Soviet An-12 Antonov Transport Plane
            //----------------------------------------------

            //----------------------------------------------
            // Soviet A-50 Mainstay AWACS
            //----------------------------------------------
            // Phase 3 (derived): Bomber archetype + NON_COMBATANT + SR+8 → AWACS_SPOTTING_RANGE 12 (the eyes of
            // the air picture; W8). Carries no real strike load itself.
            // → DF1 MAN3 TS10 SUR8 · MMP100 · SR12 · non-combatant.
            WeaponProfile A50 = WeaponProfile.FromProfileDef(
                "A-50 Mainstay AWACS", "A-50 Mainstay", WeaponType.AWACS_A50_SV,
                new ProfileDef(FamilyArchetypes.Bomber,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SR, GameData.AWACS_SPOTTING_RANGE - GameData.AIR_UNIT_SPOTTING_RANGE } },
                    new[] { WeaponTrait.NON_COMBATANT }),
                UpgradePath.AWACS, 552);

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
            // Phase 3 (derived): FighterEarly archetype + small ground load. Basic early air-superiority jet.
            // → DF8 MAN9 TS10 SUR6 · OL6 · MMP100 · SR4.
            WeaponProfile MIG21 = WeaponProfile.FromProfileDef(
                "MiG-21 Fishbed Air Superiority Fighter", "MiG-21 Fishbed", WeaponType.FGT_MIG21_SV,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int>(),
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.FGT, 252);

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
            // Phase 3 (final-intent): FighterEarly + DF+3/TS+1/SUR+2 (radar-armed Flogger). Pure air-superiority
            // → GA Rule-A floor 2. → DF11 MAN9 TS11 SUR8 · GA2 OL6 · MMP100 · SR4.
            WeaponProfile MIG23 = WeaponProfile.FromProfileDef(
                "MiG-23 Flogger Air Superiority Fighter", "MiG-23 Flogger", WeaponType.FGT_MIG23_SV,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 3 }, { ProfileStat.TS, 1 }, { ProfileStat.SUR, 2 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.FGT, 384);

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
            // Phase 3 (final-intent): FighterEarly + TS+7 (Mach-3 dash, AC_HIGHSPEED_RUSSIAN) + HIGH_MACH_DASH.
            // Pure high-speed interceptor → GA Rule-A floor 2. → DF8 MAN9 TS17 SUR6 · GA2 OL6 · MMP100 · SR4.
            WeaponProfile MIG25 = WeaponProfile.FromProfileDef(
                "MiG-25 Foxbat Interceptor", "MiG-25 Foxbat", WeaponType.FGT_MIG25_SV,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.TS, 7 } },
                    new[] { WeaponTrait.HIGH_MACH_DASH }),
                UpgradePath.FGT, 384);

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
            // Phase 3 (final-intent): FighterMid + DF+3/MAN+5/TS+1 (agile Fulcrum) + MULTIROLE_STRIKE (dual-role
            // Fulcrum-A/S: GA+4 → 6). (AGILE_AIRFRAME folds the MAN edge in the later air-trait pass.)
            // → DF13 MAN16 TS11 SUR7 · GA6 OL6 · MMP100 · SR4.
            WeaponProfile MIG29 = WeaponProfile.FromProfileDef(
                "MiG-29 Fulcrum Air Superiority Fighter", "MiG-29 Fulcrum", WeaponType.FGT_MIG29_SV,
                new ProfileDef(FamilyArchetypes.FighterMid,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 3 }, { ProfileStat.MAN, 5 }, { ProfileStat.TS, 1 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE }),
                UpgradePath.FGT, 540);

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
            // Phase 3 (final-intent): FighterMid + DF+3/TS+7 (Mach-2.8 long-range interceptor) + HIGH_MACH_DASH.
            // Pure interceptor → GA Rule-A floor 2. (BVR_RADAR_MISSILE long reach folds in the later air pass.)
            // → DF13 MAN11 TS17 SUR7 · GA2 OL6 · MMP100 · SR4.
            WeaponProfile MIG31 = WeaponProfile.FromProfileDef(
                "MiG-31 Foxhound Interceptor", "MiG-31 Foxhound", WeaponType.FGT_MIG31_SV,
                new ProfileDef(FamilyArchetypes.FighterMid,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 3 }, { ProfileStat.TS, 7 } },
                    new[] { WeaponTrait.HIGH_MACH_DASH }),
                UpgradePath.FGT, 516);

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
            // Phase 3 (final-intent): FighterMid + DF+5/MAN+2/TS+2/SUR+2 (apex Flanker) + MULTIROLE_STRIKE
            // (multirole Flanker: GA+4 → 6). (AGILE_AIRFRAME + BVR fold in the later air pass.)
            // → DF15 MAN13 TS12 SUR9 · GA6 OL6 · MMP100 · SR4.
            WeaponProfile SU27 = WeaponProfile.FromProfileDef(
                "Su-27 Flanker Air Superiority Fighter", "Su-27 Flanker", WeaponType.FGT_SU27_SV,
                new ProfileDef(FamilyArchetypes.FighterMid,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 5 }, { ProfileStat.MAN, 2 }, { ProfileStat.TS, 2 }, { ProfileStat.SUR, 2 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE }),
                UpgradePath.FGT, 564);

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
            // Phase 3 (final-intent): FighterLate + DF+6/MAN+5 (experimental super-maneuverable forward-swept) +
            // MULTIROLE_STRIKE (multirole apex airframe: GA+4 → 6). The what-if super-fighter.
            // → DF18 MAN17 TS10 SUR9 · GA6 OL6 · MMP100 · SR4.
            WeaponProfile SU47 = WeaponProfile.FromProfileDef(
                "Su-47 Berkut Experimental Fighter", "Su-47 Berkut", WeaponType.FGT_SU47_SV,
                new ProfileDef(FamilyArchetypes.FighterLate,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 6 }, { ProfileStat.MAN, 5 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE }),
                UpgradePath.FGT, 708);

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
            // Phase 3 (final-intent): re-homed from the Attack archetype to FighterEarly + SUR+2 (rugged Flogger-D)
            // + MULTIROLE_STRIKE (the §9b dual-role lever off the fighter floor: GA+4 → 6). Dumb-bomb fighter-bomber,
            // deliberately below the precision-PGM F-16 (GA9). → DF8 MAN9 TS10 SUR8 · GA6 OL6 · MMP100 · SR4.
            WeaponProfile MIG27 = WeaponProfile.FromProfileDef(
                "MiG-27 Flogger-D Multi-Role Fighter", "MiG-27 Flogger-D", WeaponType.FGT_MIG27_SV,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SUR, 2 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE }),
                UpgradePath.ATT, 444);

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
            // Phase 3 (final-intent): re-homed from the Attack archetype to FighterEarly + SUR+1 + MULTIROLE_STRIKE
            // (§9b dual-role lever: GA+4 → 6). Swing-wing fighter-bomber, dumb-bomb striker.
            // → DF8 MAN9 TS10 SUR7 · GA6 OL6 · MMP100 · SR4.
            WeaponProfile SU17 = WeaponProfile.FromProfileDef(
                "Su-17 Fitter Attack Aircraft", "Su-17 Fitter", WeaponType.ATT_SU17_SV,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SUR, 1 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE }),
                UpgradePath.ATT, 384);

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
            // Phase 3 (final-intent): Attack + DF-2 (no air-to-air) + HEAVY_AG_CANNON (GSh-30: GA+2, GaVsHard+2) +
            // AT_GUIDED_AIR (Vikhr/Kh-25: GA+3, GaVsHard+1) + CAS_ARMORED (SUR+2) + LOITER_PERSISTENCE. The Soviet
            // A-10 — GA15, GaVsHard 3 stored. → DF2 MAN4 TS7 SUR12 · GA15 OL9 · MMP100 · SR4 · GaVsHard 3.
            WeaponProfile SU25 = WeaponProfile.FromProfileDef(
                "Su-25 Frogfoot Attack Aircraft", "Su-25 Frogfoot", WeaponType.ATT_SU25_SV,
                new ProfileDef(FamilyArchetypes.Attack,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, -2 } },
                    new[] { WeaponTrait.HEAVY_AG_CANNON, WeaponTrait.AT_GUIDED_AIR, WeaponTrait.CAS_ARMORED, WeaponTrait.LOITER_PERSISTENCE }),
                UpgradePath.ATT, 516);

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
            // Phase 3 (final-intent): Attack + SUR+3/OL+1 (heavily-armoured upgrade) + HEAVY_AG_CANNON + AT_GUIDED_AIR
            // + CAS_ARMORED (SUR+2) + LOITER_PERSISTENCE. Apex Soviet CAS — GA15, max armour SUR15, GaVsHard 3.
            // → DF4 MAN4 TS7 SUR15 · GA15 OL10 · MMP100 · SR4 · GaVsHard 3.
            WeaponProfile SU25B = WeaponProfile.FromProfileDef(
                "Su-25B Frogfoot-B Attack Aircraft", "Su-25B Frogfoot-B", WeaponType.ATT_SU25B_SV,
                new ProfileDef(FamilyArchetypes.Attack,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SUR, 3 }, { ProfileStat.OL, 1 } },
                    new[] { WeaponTrait.HEAVY_AG_CANNON, WeaponTrait.AT_GUIDED_AIR, WeaponTrait.CAS_ARMORED, WeaponTrait.LOITER_PERSISTENCE }),
                UpgradePath.ATT, 588);

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
            // Phase 3 (final-intent): Bomber + DF+5/MAN+3/TS+4 (fast low-level Fencer) + HARDENED_STRIKE (GA+3, OL+2)
            // + LASER_GUIDED_MUNITIONS (Kh-29L: GA+2) + BUNKER_PENETRATOR (GaVsBase+4 stored) + TERRAIN_FOLLOW_RADAR.
            // The Soviet F-111 — GA13. → DF6 MAN6 TS14 SUR8 · GA13 OL14 · MMP100 · SR4 · GaVsBase 4.
            WeaponProfile SU24 = WeaponProfile.FromProfileDef(
                "Su-24 Fencer Bomber", "Su-24 Fencer", WeaponType.BMB_SU24_SV,
                new ProfileDef(FamilyArchetypes.Bomber,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 5 }, { ProfileStat.MAN, 3 }, { ProfileStat.TS, 4 } },
                    new[] { WeaponTrait.HARDENED_STRIKE, WeaponTrait.LASER_GUIDED_MUNITIONS, WeaponTrait.BUNKER_PENETRATOR, WeaponTrait.TERRAIN_FOLLOW_RADAR }),
                UpgradePath.ATT, 432);

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
            // Phase 3 (final-intent): Bomber + SUR+2 + CARPET_BOMBING (area anti-soft: GA+1 → 9, GaVsSoft+3 stored)
            // + STRATEGIC_PAYLOAD (OL+4 → 16). Old heavy-lift level bomber — area saturation, not precision.
            // → DF1 MAN3 TS10 SUR10 · GA9 OL16 · MMP100 · SR4 · GaVsSoft 3.
            WeaponProfile TU16 = WeaponProfile.FromProfileDef(
                "Tu-16 Badger Bomber", "Tu-16 Badger", WeaponType.BMB_TU16_SV,
                new ProfileDef(FamilyArchetypes.Bomber,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SUR, 2 } },
                    new[] { WeaponTrait.CARPET_BOMBING, WeaponTrait.STRATEGIC_PAYLOAD }),
                UpgradePath.BMB, 192);

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
            // Phase 3 (final-intent): Bomber + TS+4 (supersonic Blinder) + HIGH_MACH_DASH + CARPET_BOMBING (GA+1,
            // GaVsSoft+3 stored) + STANDOFF_CRUISE_MISSILE (Kh-22 — GA+3 heavy warhead, strike ignores GAD; avoid-GAD
            // hook dormant). Area/standoff bomber. → DF1 MAN3 TS14 SUR8 · GA12 OL12 · MMP100 · SR4 · GaVsSoft 3 · avoid-GAD.
            WeaponProfile TU22 = WeaponProfile.FromProfileDef(
                "Tu-22 Blinder Bomber", "Tu-22 Blinder", WeaponType.BMB_TU22_SV,
                new ProfileDef(FamilyArchetypes.Bomber,
                    new Dictionary<ProfileStat, int> { { ProfileStat.TS, 4 } },
                    new[] { WeaponTrait.HIGH_MACH_DASH, WeaponTrait.CARPET_BOMBING, WeaponTrait.STANDOFF_CRUISE_MISSILE }),
                UpgradePath.BMB, 288);

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
            // Phase 3 (final-intent): Bomber + TS+6 (Mach-1.8 Backfire) + HIGH_MACH_DASH + CARPET_BOMBING (GA+1,
            // GaVsSoft+3) + STRATEGIC_PAYLOAD (OL+4 → 16) + STANDOFF_CRUISE_MISSILE (Kh-22: GA+3 heavy warhead,
            // avoid-GAD dormant) + BUNKER_PENETRATOR (GaVsBase+4). Apex strategic bomber — heavy GA plus riders + payload.
            // → DF1 MAN3 TS16 SUR8 · GA12 OL16 · MMP100 · SR4 · GaVsSoft 3 · GaVsBase 4 · avoid-GAD.
            WeaponProfile TU22M3 = WeaponProfile.FromProfileDef(
                "Tu-22M3 Backfire-C Strategic Bomber", "Tu-22M3 Backfire", WeaponType.BMB_TU22M3_SV,
                new ProfileDef(FamilyArchetypes.Bomber,
                    new Dictionary<ProfileStat, int> { { ProfileStat.TS, 6 } },
                    new[] { WeaponTrait.HIGH_MACH_DASH, WeaponTrait.CARPET_BOMBING, WeaponTrait.STRATEGIC_PAYLOAD, WeaponTrait.STANDOFF_CRUISE_MISSILE, WeaponTrait.BUNKER_PENETRATOR }),
                UpgradePath.BMB, 480);

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
            // Phase 3 (derived): FighterEarly + TS+9 (Mach-3.2 recon dash) + SR+4 → AIR_RECON 8 + HIGH_MACH_DASH
            // + NON_COMBATANT (unarmed photo-recon Foxbat-B). The faction's deep-look fixed-wing recon.
            // → DF8 MAN9 TS19 SUR6 · MMP100 · SR8 · non-combatant.
            WeaponProfile MIG25R = WeaponProfile.FromProfileDef(
                "MiG-25R Foxbat-B Reconnaissance Aircraft", "MiG-25R Foxbat-B", WeaponType.RCNA_MIG25R_SV,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.TS, 9 }, { ProfileStat.SR, GameData.AIR_RECON_SPOTTING_RANGE - GameData.AIR_UNIT_SPOTTING_RANGE } },
                    new[] { WeaponTrait.HIGH_MACH_DASH, WeaponTrait.NON_COMBATANT }),
                UpgradePath.RCNA, 384);

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
            // Phase 3 (derived): Truck archetype (soft, GAD 6, MOT 8) + NON_COMBATANT (unarmed transport).
            // → HA3 HD3 SA3 SD3 GAD6 · MMP8 · SR2 · non-combatant.
            WeaponProfile TRK_GEN = WeaponProfile.FromProfileDef(
                "Generic Transport Truck", "Transport Truck", WeaponType.TRK_GEN_SV,
                new ProfileDef(FamilyArchetypes.Truck,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.NON_COMBATANT }));

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
            // Phase 3 (derived): Truck archetype + NON_COMBATANT + AMPHIBIOUS (moves on water) + MMP+2 → NAVAL 10.
            // Sea-lift flotilla for amphibious moves.
            // → HA3 HD3 SA3 SD3 GAD6 · MMP10 · SR2 · non-combatant · amphibious.
            WeaponProfile NAVAL = WeaponProfile.FromProfileDef(
                "Transport Flotilla", "Transports", WeaponType.TRN_NAVAL,
                new ProfileDef(FamilyArchetypes.Truck,
                    new Dictionary<ProfileStat, int> { { ProfileStat.MMP, GameData.NAVAL_UNIT - GameData.MOT_UNIT } },
                    new[] { WeaponTrait.NON_COMBATANT, WeaponTrait.AMPHIBIOUS }));

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
            // Phase 3 (derived): Infantry archetype (GAD 10, R1) + RPG_LAW (HA+1) + MANPADS_BASIC (Strela, GAT
            // floor 6 + EngageAir). Line infantry with organic AT + short-range air defense.
            // → HA6 HD7 SA7 SD8 GAD10 · GAT6 · MMP4 · SR2.
            WeaponProfile INF_REG = WeaponProfile.FromProfileDef(
                "Regular Infantry", "Regulars", WeaponType.INF_REG_SV,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.MANPADS_BASIC }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + MANPADS_BASIC + AIR_DROPPABLE (VDV — airborne deploy).
            // → HA6 HD7 SA7 SD8 GAD10 · GAT6 · MMP4 · SR2 · air-droppable.
            WeaponProfile INF_AB = WeaponProfile.FromProfileDef(
                "Airborne Infantry", "Airborne", WeaponType.INF_AB_SV,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.MANPADS_BASIC, WeaponTrait.AIR_DROPPABLE }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + MANPADS_BASIC + MOUNTAIN_TRAINED (helo-inserted light
            // infantry — reduced move cost in non-clear terrain; fits the Afghan air-assault role).
            // → HA6 HD7 SA7 SD8 GAD10 · GAT6 · MMP4 · SR2 · mountain movement.
            WeaponProfile INF_AM = WeaponProfile.FromProfileDef(
                "Air-Mobile Infantry", "Air-Mobile", WeaponType.INF_AM_SV,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.MANPADS_BASIC, WeaponTrait.MOUNTAIN_TRAINED }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + MANPADS_BASIC + AMPHIBIOUS (naval infantry — assault swim).
            // → HA6 HD7 SA7 SD8 GAD10 · GAT6 · MMP4 · SR2 · amphibious.
            WeaponProfile INF_MAR = WeaponProfile.FromProfileDef(
                "Marine Infantry", "Marines", WeaponType.INF_MAR_SV,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.MANPADS_BASIC, WeaponTrait.AMPHIBIOUS }));

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
            // Phase 3 (derived): Infantry + SPECIAL_FORCES (SA+2/HA+2/SD+1/ICM×1.10 — R5 brings SF SD to 9) +
            // RPG_LAW + MANPADS_BASIC + SR+1 (recon SR 3). Elite Spetsnaz.
            // → HA8 HD7 SA9 SD9 GAD10 · GAT6 · ICM 1.10 · MMP4 · SR3.
            WeaponProfile INF_SPEC = WeaponProfile.FromProfileDef(
                "Special Forces Infantry", "Spetsnaz", WeaponType.INF_SPEC_SV,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SR, 1 } },
                    new[] { WeaponTrait.SPECIAL_FORCES, WeaponTrait.RPG_LAW, WeaponTrait.MANPADS_BASIC }));

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
            // Phase 3 (derived): Infantry + SA-3/SD-3 (weaker close-combat) + FIELD_FORTIFICATION (build IsFort)
            // + RIVER_ASSAULT (×1.4 across-river attack) + RPG_LAW. Engineering utility over firepower.
            // → HA6 HD7 SA4 SD5 GAD10 · GAT0 · MMP4 · SR2 · field-fortification · river-assault.
            WeaponProfile INF_ENG = WeaponProfile.FromProfileDef(
                "Combat Engineers", "Engineers", WeaponType.INF_ENG_SV,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, -3 }, { ProfileStat.SD, -3 } },
                    new[] { WeaponTrait.FIELD_FORTIFICATION, WeaponTrait.RIVER_ASSAULT, WeaponTrait.RPG_LAW }));

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
            WeaponProfile BASE_AIRBASE = new WeaponProfile(
                _longName: "Miltary Airbase",
                _shortName: "Airbase",
                _type: WeaponType.BASE_AIRBASE,
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
                _isAtt: false                             // Can this profile attack
            );

            // Fill out intel stats for the Large Base
            BASE_AIRBASE.AddIntelReportStat(WeaponType.Personnel, 3000);

            // Handle the icon profile.
            BASE_AIRBASE.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.ME_Airbase
            };

            // Add the Large Base profile to the database
            AddProfile(WeaponType.BASE_AIRBASE, BASE_AIRBASE);
            //----------------------------------------------
            // Large Base (Airbase)
            //----------------------------------------------

            //----------------------------------------------
            // Medium Base
            //----------------------------------------------
            WeaponProfile BASE_DEPOT = new WeaponProfile(
                _longName: "Supply Depot",
                _shortName: "Depot",
                _type: WeaponType.BASE_DEPOT,
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
                _isAtt: false                             // Can this profile attack
            );

            // Fill out intel stats for the Medium Base
            BASE_DEPOT.AddIntelReportStat(WeaponType.Personnel, 2000);

            // Handle the icon profile.
            BASE_DEPOT.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_Depot
            };

            // Add the Medium Base profile to the database
            AddProfile(WeaponType.BASE_DEPOT, BASE_DEPOT);
            //----------------------------------------------
            // Medium Base
            //----------------------------------------------

            //----------------------------------------------
            // Small Base (HQ)
            //----------------------------------------------
            WeaponProfile BASE_HQ = new WeaponProfile(
                _longName: "Intel Base",
                _shortName: "Intel",
                _type: WeaponType.BASE_HQ,
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
                _isAtt: false                             // Can this profile attack
            );

            // Fill out intel stats for the Small Base
            BASE_HQ.AddIntelReportStat(WeaponType.Personnel, 1500);

            // Handle the icon profile.
            BASE_HQ.IconProfile = new RegimentIconProfile(RegimentIconType.Single)
            {
                W = SpriteManager.GEN_Base
            };

            // Add the Small Base profile to the database
            AddProfile(WeaponType.BASE_HQ, BASE_HQ);
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
            // Phase 3 (Appendix W §16, validated worked line) — the M1 (105): Gen3 + HA-3 (105mm L7 is
            // under-gunned on a Gen3 chassis) + SA-1 + COMPOSITE_CERAMIC + LRF + BC + OPTICS_GEN3 + THERMAL +
            // GAS_TURBINE. The single in-game Abrams slot is the 105mm M1 (display "M1 Abrams").
            // → HA10 HD13 SA8 SD6 GAD7 · ICM 1.33 · MMP12 · PR1 · SR4.
            WeaponProfile M1_US = WeaponProfile.FromProfileDef(
                "M1 Abrams Main Battle Tank", "M1 Abrams", WeaponType.TANK_M1_US,
                new ProfileDef(TankArchetypes.Gen3,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, -3 }, { ProfileStat.SA, -1 } },
                    new[] { WeaponTrait.COMPOSITE_CERAMIC, WeaponTrait.LASER_RANGEFINDER, WeaponTrait.BALLISTIC_COMPUTER,
                            WeaponTrait.OPTICS_GEN3, WeaponTrait.THERMAL_IMAGER, WeaponTrait.GAS_TURBINE }),
                UpgradePath.TANK, 504);

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
            // Phase 3 (derived): Gen2 (105mm M68 baked) + SA+1 (NATO SA8) + LASER_RANGEFINDER + THERMAL_IMAGER
            // (M60A3 TTS). A slower Gen2 with the NATO fire-control edge.
            // → HA10 HD8 SA8 SD6 GAD7 · ICM 1.16 · MMP10 · PR1 · SR3.
            WeaponProfile M60_US = WeaponProfile.FromProfileDef(
                "M60A3 Patton Main Battle Tank", "M60A3", WeaponType.TANK_M60_US,
                new ProfileDef(TankArchetypes.Gen2,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, 1 } },
                    new[] { WeaponTrait.LASER_RANGEFINDER, WeaponTrait.THERMAL_IMAGER }),
                UpgradePath.TANK, 264);

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
            // Phase 3 (Appendix W §16, validated worked line): Gen2 + HA-1, HD-1, SA+1, MMP+2 +
            // OPTICS_GEN2 + LASER_RANGEFINDER. Fast, well-sighted, lightly-gunned 105mm.
            // → HA9 HD7 SA8 SD6 GAD7 · ICM 1.10 · MMP12 · PR1 · SR3.
            WeaponProfile LEO1_GE = WeaponProfile.FromProfileDef(
                "Leopard 1 Main Battle Tank", "Leo 1", WeaponType.TANK_LEOPARD1_GE,
                new ProfileDef(TankArchetypes.Gen2,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, -1 }, { ProfileStat.HD, -1 }, { ProfileStat.SA, 1 }, { ProfileStat.MMP, 2 } },
                    new[] { WeaponTrait.OPTICS_GEN2, WeaponTrait.LASER_RANGEFINDER }),
                UpgradePath.TANK, 324);

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
            // Phase 3 (Appendix W §16, validated worked line): Gen3 + HA+1, SA-1, MMP+2 + SPACED_ARMOR +
            // OPTICS_GEN3 + LASER_RANGEFINDER + BALLISTIC_COMPUTER + THERMAL_IMAGER.
            // → HA14 HD12 SA8 SD6 GAD7 · ICM 1.33 · MMP12 · PR1 · SR4.
            WeaponProfile LEO2_GE = WeaponProfile.FromProfileDef(
                "Leopard 2 Main Battle Tank", "Leo 2", WeaponType.TANK_LEOPARD2_GE,
                new ProfileDef(TankArchetypes.Gen3,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, 1 }, { ProfileStat.SA, -1 }, { ProfileStat.MMP, 2 } },
                    new[] { WeaponTrait.SPACED_ARMOR, WeaponTrait.OPTICS_GEN3, WeaponTrait.LASER_RANGEFINDER,
                            WeaponTrait.BALLISTIC_COMPUTER, WeaponTrait.THERMAL_IMAGER }),
                UpgradePath.TANK, 492);

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
            // Phase 3 (Appendix W §16, validated worked line): Gen3 + HD+1, SA-1 + COMPOSITE_CERAMIC +
            // LASER_RANGEFINDER + BALLISTIC_COMPUTER + THERMAL_IMAGER. Heavy-armour, slower (no turbine).
            // → HA13 HD14 SA8 SD6 GAD7 · ICM 1.21 · MMP10 · PR1 · SR3.
            WeaponProfile CHALL1_UK = WeaponProfile.FromProfileDef(
                "Challenger 1 Main Battle Tank", "Challenger 1", WeaponType.TANK_CHALLENGER1_UK,
                new ProfileDef(TankArchetypes.Gen3,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HD, 1 }, { ProfileStat.SA, -1 } },
                    new[] { WeaponTrait.COMPOSITE_CERAMIC, WeaponTrait.LASER_RANGEFINDER, WeaponTrait.BALLISTIC_COMPUTER,
                            WeaponTrait.THERMAL_IMAGER }),
                UpgradePath.TANK, 540);

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
            // Phase 3 (derived): Gen2 + HD-2 (thinnest NATO armour) + SA+1 (NATO SA8) + MMP+2 (light/fast) +
            // LOW_PROFILE. The no-frills fast French 105mm (base AMX-30B; no advanced FCS → ICM 1.00). A
            // glass-cannon scout-MBT identity vs the FCS-heavy Germans/Brits.
            // → HA10 HD7 SA8 SD7 GAD7 · ICM 1.00 · MMP12 · PR1 · SR2.
            WeaponProfile AMX30_FR = WeaponProfile.FromProfileDef(
                "AMX-30 Main Battle Tank", "AMX-30", WeaponType.TANK_AMX30_FR,
                new ProfileDef(TankArchetypes.Gen2,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HD, -2 }, { ProfileStat.SA, 1 }, { ProfileStat.MMP, 2 } },
                    new[] { WeaponTrait.LOW_PROFILE }),
                UpgradePath.TANK, 336);

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
            // Phase 3 (NATO): Ifv + ATGM_RAIL (TOW, HA+4) + AUTOCANNON_LIGHT (25mm Bushmaster, SA+1).
            // → HA8 HD4 SA9 SD7 GAD7 · ICM 1.00 · MMP10 · SR2 · PR1.
            WeaponProfile M2_US = WeaponProfile.FromProfileDef(
                "M2 Bradley Infantry Fighting Vehicle", "M2 Bradley", WeaponType.IFV_M2_US,
                new ProfileDef(FamilyArchetypes.Ifv,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ATGM_RAIL, WeaponTrait.AUTOCANNON_LIGHT }),
                UpgradePath.IFV, 516);

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
            // Phase 3 (NATO): Ifv + AUTOCANNON_HEAVY (30mm RARDEN, HA+1/SA+1); MILAN is dismounted (no vehicle ATGM).
            // → HA5 HD4 SA9 SD7 GAD7 · ICM 1.00 · MMP10 · SR2.
            WeaponProfile WARRIOR_UK = WeaponProfile.FromProfileDef(
                "Warrior Infantry Fighting Vehicle", "Warrior", WeaponType.IFV_WARRIOR_UK,
                new ProfileDef(FamilyArchetypes.Ifv,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.AUTOCANNON_HEAVY }),
                UpgradePath.IFV, 588);

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
            // Phase 3 (NATO): Ifv + ATGM_RAIL (MILAN, HA+4) + AUTOCANNON_LIGHT (20mm, SA+1).
            // → HA8 HD4 SA9 SD7 GAD7 · ICM 1.00 · MMP10 · SR2.
            WeaponProfile MARDER_GE = WeaponProfile.FromProfileDef(
                "Marder Infantry Fighting Vehicle", "Marder", WeaponType.IFV_MARDER_GE,
                new ProfileDef(FamilyArchetypes.Ifv,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ATGM_RAIL, WeaponTrait.AUTOCANNON_LIGHT }),
                UpgradePath.IFV, 396);

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
            // Phase 3 (NATO): bare Apc archetype (tracked, .50-cal only).
            // → HA3 HD4 SA6 SD7 GAD7 · ICM 1.00 · MMP8 · SR2.
            WeaponProfile M113_US = WeaponProfile.FromProfileDef(
                "M113 Armored Personnel Carrier", "M113", WeaponType.APC_M113_US,
                new ProfileDef(FamilyArchetypes.Apc,
                    new Dictionary<ProfileStat, int>(),
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.APC, 264);

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
            // Phase 3 (NATO): Apc + THIN_TOP (open-mount soft-skin, GAD−1). Wheeled utility carrier.
            // → HA3 HD4 SA6 SD7 GAD6 · ICM 1.00 · MMP8 · SR2.
            WeaponProfile HUMVEE_US = WeaponProfile.FromProfileDef(
                "HMMWV Utility Vehicle", "Humvee", WeaponType.APC_HUMVEE_US,
                new ProfileDef(FamilyArchetypes.Apc,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.THIN_TOP }),
                UpgradePath.APC, 552);

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
            // Phase 3 (NATO): Apc + AMPHIBIOUS (Marine assault swimmer).
            // → HA3 HD4 SA6 SD7 GAD7 · ICM 1.00 · MMP8 · SR2 · amphibious.
            WeaponProfile LVTP7_US = WeaponProfile.FromProfileDef(
                "LVTP-7 Amphibious Assault Vehicle", "LVTP-7", WeaponType.APC_LVTP7_US,
                new ProfileDef(FamilyArchetypes.Apc,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.AMPHIBIOUS }),
                UpgradePath.APC, 408);

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
            // Phase 3 (NATO): bare Apc archetype (wheeled 6x6 carrier).
            // → HA3 HD4 SA6 SD7 GAD7 · ICM 1.00 · MMP8 · SR2.
            WeaponProfile VAB_FR = WeaponProfile.FromProfileDef(
                "VAB Armored Personnel Carrier", "VAB", WeaponType.APC_VAB_FR,
                new ProfileDef(FamilyArchetypes.Apc,
                    new Dictionary<ProfileStat, int>(),
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.APC, 456);

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
            // Phase 3 (NATO): Artillery + SELF_PROPELLED (tracked 155mm SP) + SA+1 (calibre), IR medium. Standard SP howitzer (= 2S3 line).
            // → HA5 HD7 SA10 SD7 GAD7 · ICM 1.00 · MMP10 · IR5.
            WeaponProfile M109_US = WeaponProfile.FromProfileDef(
                "M109 Paladin Self-Propelled Artillery", "M109 Paladin", WeaponType.SPA_M109_US,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, 1 }, { ProfileStat.IR, GameData.INDIRECT_RANGE_MEDIUM } },
                    new[] { WeaponTrait.SELF_PROPELLED }),
                UpgradePath.ART, 300);

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
            // Phase 3 (NATO): Artillery + SELF_PROPELLED + SA+1 (calibre), IR medium. Standard SP howitzer.
            // → HA5 HD7 SA10 SD7 GAD7 · ICM 1.00 · MMP10 · IR5.
            WeaponProfile M109_GE = WeaponProfile.FromProfileDef(
                "M109 Self-Propelled Artillery", "M109", WeaponType.SPA_M109_GE,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, 1 }, { ProfileStat.IR, GameData.INDIRECT_RANGE_MEDIUM } },
                    new[] { WeaponTrait.SELF_PROPELLED }),
                UpgradePath.ART, 300);

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
            // Phase 3 (NATO): Artillery + SELF_PROPELLED + SA+1 (calibre), IR medium. Standard SP howitzer.
            // → HA5 HD7 SA10 SD7 GAD7 · ICM 1.00 · MMP10 · IR5.
            WeaponProfile M109_FR = WeaponProfile.FromProfileDef(
                "M109 Self-Propelled Artillery", "M109", WeaponType.SPA_M109_FR,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, 1 }, { ProfileStat.IR, GameData.INDIRECT_RANGE_MEDIUM } },
                    new[] { WeaponTrait.SELF_PROPELLED }),
                UpgradePath.ART, 300);

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
            // Phase 3 (NATO): Artillery + SELF_PROPELLED + SA+1 (calibre), IR medium. Standard SP howitzer.
            // → HA5 HD7 SA10 SD7 GAD7 · ICM 1.00 · MMP10 · IR5.
            WeaponProfile M109_UK = WeaponProfile.FromProfileDef(
                "M109 Self-Propelled Artillery", "M109", WeaponType.SPA_M109_UK,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, 1 }, { ProfileStat.IR, GameData.INDIRECT_RANGE_MEDIUM } },
                    new[] { WeaponTrait.SELF_PROPELLED }),
                UpgradePath.ART, 300);

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
            // Phase 3 (NATO): bare Artillery archetype (105mm towed, foot MMP4), IR short.
            // → HA5 HD5 SA9 SD5 GAD8 · ICM 1.00 · MMP4 · IR4.
            WeaponProfile ArtLightWest = WeaponProfile.FromProfileDef(
                "Light Towed Artillery", "Lt Artillery", WeaponType.ART_LIGHT_WEST,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SHORT } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.ART, 144);

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
            // Phase 3 (NATO): Artillery + SA+1 (155mm calibre), IR medium. Heavy towed (foot MMP4).
            // → HA5 HD5 SA10 SD5 GAD8 · ICM 1.00 · MMP4 · IR5.
            WeaponProfile ArtHeavyWest = WeaponProfile.FromProfileDef(
                "Heavy Towed Artillery", "Hvy Artillery", WeaponType.ART_HEAVY_WEST,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, 1 }, { ProfileStat.IR, GameData.INDIRECT_RANGE_MEDIUM } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.ART, 144);

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
            // Phase 3 (NATO): Artillery + SELF_PROPELLED (tracked M993 chassis) + ROCKET_ARTILLERY (double-fire)
            // + SMART_MUNITION (SADARM, HA+3/SA+1) + SA+1 (warhead), IR ROC_MR. Tracked analog of BM-27.
            // → HA8 HD7 SA11 SD7 GAD7 · ICM 1.00 · MMP10 · IR6 · double-fire.
            WeaponProfile MLRS_US = WeaponProfile.FromProfileDef(
                "M270 MLRS Multiple Launch Rocket System", "M270 MLRS", WeaponType.ROC_MLRS_US,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, 1 }, { ProfileStat.IR, GameData.INDIRECT_RANGE_ROC_MR } },
                    new[] { WeaponTrait.SELF_PROPELLED, WeaponTrait.ROCKET_ARTILLERY, WeaponTrait.SMART_MUNITION }),
                UpgradePath.ROC, 540);

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
            // Phase 3 (NATO): Aaa + SELF_PROPELLED (M113 chassis); optical/ranging 20mm gun, no radar-direction trait (GAT 9, = ZSU-57-2).
            // → HA4 HD6 SA9 SD8 GAD11 · GAT9 · MMP10 · IR3 · SR3.
            WeaponProfile M163_US = WeaponProfile.FromProfileDef(
                "M163 Vulcan Self-Propelled Anti-Aircraft Gun", "M163 Vulcan", WeaponType.SPAAA_M163_US,
                new ProfileDef(FamilyArchetypes.Aaa,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_AAA } },
                    new[] { WeaponTrait.SELF_PROPELLED }),
                UpgradePath.AAA, 372);

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
            // Phase 3 (NATO): Sam + SELF_PROPELLED (M48 chassis) + IR_HOMING (fire-and-forget IR SAM → GAT 11, = Strela line).
            // → HA1 HD5 SA1 SD5 GAD7 · GAT11 · MMP10 · IR6 · SR6.
            WeaponProfile Chaparral = WeaponProfile.FromProfileDef(
                "M48 Chaparral Self-Propelled SAM System", "M48 Chaparral", WeaponType.SPSAM_CHAP_US,
                new ProfileDef(FamilyArchetypes.Sam,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SAM } },
                    new[] { WeaponTrait.SELF_PROPELLED, WeaponTrait.IR_HOMING }),
                UpgradePath.SAM, 372);

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
            // Phase 3 (NATO): Sam + SARH_LONG_RANGE (radar-illuminated medium SAM → GAT 13) + static (MMP→0). = NATO's S-75.
            // → HA1 HD3 SA1 SD3 GAD8 · GAT13 · MMP0 · IR6 · SR6.
            WeaponProfile Hawk_US = WeaponProfile.FromProfileDef(
                "MIM-23 Hawk Strategic SAM System", "MIM-23 Hawk", WeaponType.SAM_HAWK_US,
                new ProfileDef(FamilyArchetypes.Sam,
                    new Dictionary<ProfileStat, int> { { ProfileStat.MMP, GameData.STATIC_UNIT - GameData.FOOT_UNIT }, { ProfileStat.IR, GameData.INDIRECT_RANGE_SAM } },
                    new[] { WeaponTrait.SARH_LONG_RANGE }),
                UpgradePath.SAM, 264);

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
            // Phase 3 (NATO): Aaa + SELF_PROPELLED + RADAR_GUIDED_GUN (35mm radar-directed = NATO's ZSU-23-4 → GAT 11).
            // → HA4 HD6 SA9 SD8 GAD11 · GAT11 · MMP10 · IR4 · SR3. (Classified SPSAM in source but is a GUN — see TODO flag.)
            WeaponProfile Gepard_GE = WeaponProfile.FromProfileDef(
                "Flakpanzer Gepard Self-Propelled Anti-Aircraft Gun", "Gepard", WeaponType.SPSAM_GEPARD_GE,
                new ProfileDef(FamilyArchetypes.Aaa,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SHORT } },
                    new[] { WeaponTrait.SELF_PROPELLED, WeaponTrait.RADAR_GUIDED_GUN }),
                UpgradePath.AAA, 456);

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
            // Phase 3 (NATO): Aaa + SELF_PROPELLED + COMMAND_GUIDANCE (radar-command point SAM → GAT 11). NOTE: Roland is a
            // missile system but is classified SPAAA with AAA (gun) stats in the source — kept the dual-role line; see TODO flag.
            // → HA4 HD6 SA9 SD8 GAD11 · GAT11 · MMP10 · IR3 · SR3.
            WeaponProfile Roland_FR = WeaponProfile.FromProfileDef(
                "Roland Self-Propelled Anti-Aircraft Gun", "Roland", WeaponType.SPAAA_ROLAND_FR,
                new ProfileDef(FamilyArchetypes.Aaa,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_AAA } },
                    new[] { WeaponTrait.SELF_PROPELLED, WeaponTrait.COMMAND_GUIDANCE }),
                UpgradePath.SAM, 468);

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
            // Phase 3 (NATO): Sam + SELF_PROPELLED + COMMAND_GUIDANCE (radar-command point SAM → GAT 12).
            // → HA1 HD5 SA1 SD5 GAD7 · GAT12 · MMP10 · IR6 · SR6.
            WeaponProfile Crotale = WeaponProfile.FromProfileDef(
                "Crotale Self-Propelled SAM System", "Crotale", WeaponType.SPSAM_CROTALE_FR,
                new ProfileDef(FamilyArchetypes.Sam,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SAM } },
                    new[] { WeaponTrait.SELF_PROPELLED, WeaponTrait.COMMAND_GUIDANCE }),
                UpgradePath.SAM, 396);

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
            // Phase 3 (NATO): Sam + SELF_PROPELLED + COMMAND_GUIDANCE (SACLOS point SAM → GAT 12).
            // → HA1 HD5 SA1 SD5 GAD7 · GAT12 · MMP10 · IR6 · SR6.
            WeaponProfile Rapier_SP = WeaponProfile.FromProfileDef(
                "Tracked Rapier Self-Propelled SAM System", "Tracked Rapier", WeaponType.SPSAM_RAPIER_UK,
                new ProfileDef(FamilyArchetypes.Sam,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SAM } },
                    new[] { WeaponTrait.SELF_PROPELLED, WeaponTrait.COMMAND_GUIDANCE }),
                UpgradePath.SAM, 432);

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
            // Phase 3 (NATO): Recon + ATGM_RAIL (TOW, HA+4) + AUTOCANNON_LIGHT (25mm); Bradley-chassis combat scout.
            // → HA6 HD5 SA6 SD9 GAD7 · ICM 1.00 · MMP10 · SR3 · Hard (post-call). Not RECON_FRAGILE (armored cavalry).
            WeaponProfile M3_US = WeaponProfile.FromProfileDef(
                "M3 Bradley Cavalry Fighting Vehicle", "M3 Bradley", WeaponType.RCN_M3_US,
                new ProfileDef(FamilyArchetypes.Recon,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ATGM_RAIL, WeaponTrait.AUTOCANNON_LIGHT }),
                UpgradePath.RCN, 516);

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
            // W1: armored-car recon fights as a Hard target (§7.4.1.2 override).
            M3_US.SetTargetClass(TargetClass.Hard);
            AddProfile(WeaponType.RCN_M3_US, M3_US);
            //----------------------------------------------
            // US M3 Bradley Cavalry Fighting Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // FRG Spähpanzer Luchs Reconnaissance Vehicle
            //----------------------------------------------
            // Phase 3 (NATO): Recon + AUTOCANNON_LIGHT (20mm) + AMPHIBIOUS; 8x8 wheeled scout, light gun.
            // → HA2 HD5 SA6 SD9 GAD7 · ICM 1.00 · MMP10 · SR3 · amphibious · Hard (post-call).
            WeaponProfile LUCHS_GE = WeaponProfile.FromProfileDef(
                "Spähpanzer Luchs Reconnaissance Vehicle", "Luchs", WeaponType.RCN_LUCHS_GE,
                new ProfileDef(FamilyArchetypes.Recon,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.AUTOCANNON_LIGHT, WeaponTrait.AMPHIBIOUS }),
                UpgradePath.RCN, 444);

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
            // W1: armored-car recon fights as a Hard target (§7.4.1.2 override).
            LUCHS_GE.SetTargetClass(TargetClass.Hard);
            AddProfile(WeaponType.RCN_LUCHS_GE, LUCHS_GE);
            //----------------------------------------------
            // FRG Spähpanzer Luchs Reconnaissance Vehicle
            //----------------------------------------------

            //----------------------------------------------
            // UK FV105 Sultan
            //----------------------------------------------
            // Phase 3 (NATO): Recon + AUTOCANNON_HEAVY (30mm RARDEN, CVR(T)-class scout).
            // → HA3 HD5 SA6 SD9 GAD7 · ICM 1.00 · MMP10 · SR3 · Hard (post-call).
            WeaponProfile FV105_UK = WeaponProfile.FromProfileDef(
                "FV105 Sultan", "FV105", WeaponType.RCN_FV105_UK,
                new ProfileDef(FamilyArchetypes.Recon,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.AUTOCANNON_HEAVY }),
                UpgradePath.RCN, 420);

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
            // W1: UK recon vehicle fights as a Hard target (§7.4.1.2 override).
            FV105_UK.SetTargetClass(TargetClass.Hard);
            AddProfile(WeaponType.RCN_FV105_UK, FV105_UK);
            //----------------------------------------------
            // UK FV105 Sultan
            //----------------------------------------------

            //----------------------------------------------
            // French ERC 90 Sagaie Reconnaissance Vehicle
            //----------------------------------------------
            // Phase 3 (NATO): Recon + residual HA+4 (90mm low-pressure gun — one-off; calibre traits are tanks-only).
            // → HA6 HD5 SA5 SD9 GAD7 · ICM 1.00 · MMP10 · SR3 · Hard (post-call). Fire-support scout, mirrors BRDM-2 AT.
            WeaponProfile ERC90_FR = WeaponProfile.FromProfileDef(
                "ERC 90 Sagaie Reconnaissance Vehicle", "ERC 90", WeaponType.RCN_ERC90_FR,
                new ProfileDef(FamilyArchetypes.Recon,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, 4 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.RCN, 492);

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
            // W1: armored-car recon fights as a Hard target (§7.4.1.2 override).
            ERC90_FR.SetTargetClass(TargetClass.Hard);
            AddProfile(WeaponType.RCN_ERC90_FR, ERC90_FR);
            //----------------------------------------------
            // French ERC 90 Sagaie Reconnaissance Vehicle
            //----------------------------------------------

            #endregion // Recon

            #region Helicopters

            //----------------------------------------------
            // US AH-64 Apache Attack Helicopter
            //----------------------------------------------
            // Phase 3 (NATO): Helicopter + ATGM_HELO_FNF (Hellfire, HA+5/ICM1.05) + CANNON_HELO (M230 30mm) + ROCKET_PODS
            // (Hydra-70) + ARMORED_COCKPIT + HELO_COUNTERMEASURES. Apex NATO gunship (= Mi-28 line).
            // → HA12 HD7 SA13 SD8 GAD12 · ICM 1.05 · MMP24 · SR3.
            WeaponProfile AH64 = WeaponProfile.FromProfileDef(
                "AH-64 Apache Attack Helicopter", "AH-64 Apache", WeaponType.HEL_AH64_US,
                new ProfileDef(FamilyArchetypes.Helicopter,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ATGM_HELO_FNF, WeaponTrait.CANNON_HELO, WeaponTrait.ROCKET_PODS, WeaponTrait.ARMORED_COCKPIT, WeaponTrait.HELO_COUNTERMEASURES }),
                UpgradePath.HEL, 576);

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
            // Phase 3 (NATO): Helicopter + NON_COMBATANT; organic AM/MAM lift (= Mi-8T). HeloTransport category (post-call).
            // → bare 7/6/10/7 GAD10 · MMP24 · SR3 · non-combatant.
            WeaponProfile UH60 = WeaponProfile.FromProfileDef(
                "UH-60 Black Hawk Transport Helicopter", "UH-60 Black Hawk", WeaponType.HEL_UH60_US,
                new ProfileDef(FamilyArchetypes.Helicopter,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.NON_COMBATANT }),
                UpgradePath.HELT, 492);

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
            // W2: UH-60 is the US AM/MAM organic helo transport.
            UH60.SetTransportCategory(TransportCategory.HeloTransport);
            AddProfile(WeaponType.HEL_UH60_US, UH60);
            //----------------------------------------------
            // US UH-60 Black Hawk Transport Helicopter
            //----------------------------------------------

            //----------------------------------------------
            // FRG Bo 105 Light Attack Helicopter
            //----------------------------------------------
            // Phase 3 (NATO): Helicopter + ATGM_HELO_SACLOS (HOT, HA+4). Light AT helo — potent missiles, no cannon/armor (glass cannon).
            // → HA11 HD6 SA10 SD7 GAD10 · ICM 1.00 · MMP24 · SR3. (SA10 is archetype-inherited; Bo-105 PAH-1 is AT-only.)
            WeaponProfile BO105 = WeaponProfile.FromProfileDef(
                "Bo 105 Light Attack Helicopter", "Bo 105", WeaponType.HEL_BO105_GE,
                new ProfileDef(FamilyArchetypes.Helicopter,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ATGM_HELO_SACLOS }),
                UpgradePath.HEL, 492);

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
            // Phase 3 (NATO): Helicopter + ATGM_HELO_SACLOS (TOW) + CANNON_HELO (20mm) + ROCKET_PODS. = Mi-24D line minus the armor.
            // → HA11 HD6 SA13 SD7 GAD10 · ICM 1.00 · MMP24 · SR3.
            WeaponProfile AH1 = WeaponProfile.FromProfileDef(
                "AH-1 Cobra Attack Helicopter", "AH-1 Cobra", WeaponType.HEL_AH1,
                new ProfileDef(FamilyArchetypes.Helicopter,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ATGM_HELO_SACLOS, WeaponTrait.CANNON_HELO, WeaponTrait.ROCKET_PODS }),
                UpgradePath.HEL, 348);

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
            // Phase 3 (final-intent): FighterMid + DF+6/MAN+3/TS+2/SUR+4 (preserve the premier-fighter air-combat
            // line). Pure air-superiority → GA stays at the Rule-A floor 2 (DROPPED from old tier-2 GA12; F-15A/C
            // carries no strike fit here) + small load. BVR/AGILE air-stat enrichment deferred.
            // → DF16 MAN14 TS12 SUR11 · GA2 OL6 · MMP100 · SR4.
            WeaponProfile F15 = WeaponProfile.FromProfileDef(
                "F-15 Eagle Air Superiority Fighter", "F-15 Eagle", WeaponType.FGT_F15_US,
                new ProfileDef(FamilyArchetypes.FighterMid,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 6 }, { ProfileStat.MAN, 3 }, { ProfileStat.TS, 2 }, { ProfileStat.SUR, 4 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.FGT, 456);

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
            // Phase 3 (final-intent): FighterMid + DF+4/MAN+4/TS+1/SUR+1 (agile lightweight) + the dual-role lever
            // MULTIROLE_STRIKE (GA+4) + AT_GUIDED_AIR (Maverick: GA+3, GaVsHard+1 stored). Multirole strike GA9
            // (down from old tier-2 GA12, now earned by traits). → DF14 MAN15 TS11 SUR8 · GA9 OL6 · MMP100 · SR4.
            WeaponProfile F16 = WeaponProfile.FromProfileDef(
                "F-16 Fighting Falcon Multi-Role Fighter", "F-16 Falcon", WeaponType.FGT_F16_US,
                new ProfileDef(FamilyArchetypes.FighterMid,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 4 }, { ProfileStat.MAN, 4 }, { ProfileStat.TS, 1 }, { ProfileStat.SUR, 1 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE, WeaponTrait.AT_GUIDED_AIR }),
                UpgradePath.FGT, 480);

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
            // Phase 3 (final-intent): FighterEarly + DF+2/TS+2/SUR+2 (workhorse interceptor). Air-superiority
            // variant → GA at the Rule-A floor 2 (old was NA/0). → DF10 MAN9 TS12 SUR8 · GA2 OL6 · MMP100 · SR4.
            WeaponProfile F4_US = WeaponProfile.FromProfileDef(
                "F-4 Phantom II Fighter", "F-4 Phantom", WeaponType.FGT_F4_US,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 2 }, { ProfileStat.TS, 2 }, { ProfileStat.SUR, 2 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.FGT, 276);

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
            // Phase 3 (final-intent): FighterLate + DF+2/MAN+1/TS+2 (long-range fleet interceptor). Pure air
            // defense → GA at the Rule-A floor 2 (old NA/0); the Phoenix BVR edge is air-stat enrichment (deferred).
            // → DF14 MAN13 TS12 SUR9 · GA2 OL6 · MMP100 · SR4.
            WeaponProfile F14_US = WeaponProfile.FromProfileDef(
                "F-14 Tomcat Fleet Defense Fighter", "F-14 Tomcat", WeaponType.FGT_F14_US,
                new ProfileDef(FamilyArchetypes.FighterLate,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 2 }, { ProfileStat.MAN, 1 }, { ProfileStat.TS, 2 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.FGT, 432);

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
            // Phase 3 (final-intent): FighterMid + DF+2 (decent self-defense) + MULTIROLE_STRIKE (GA+4) +
            // LASER_GUIDED_MUNITIONS (GA+2) + HEAVY_PAYLOAD (OL+3) + RUNWAY_CRATERING (JP233: OcSuppression+20
            // stored) + TERRAIN_FOLLOW_RADAR (low-level penetration, dormant SUR). Heavy all-weather interdictor:
            // GA8 (down from old 12) but big OL9 payload. → DF12 MAN11 TS10 SUR7 · GA8 OL9 · SR4 · OcSuppression 20.
            WeaponProfile TORNADO_IDS = WeaponProfile.FromProfileDef(
                "Tornado IDS Interdictor/Strike Fighter", "Tornado IDS", WeaponType.FGT_TORNADO_IDS_UK,
                new ProfileDef(FamilyArchetypes.FighterMid,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 2 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE, WeaponTrait.LASER_GUIDED_MUNITIONS, WeaponTrait.HEAVY_PAYLOAD, WeaponTrait.RUNWAY_CRATERING, WeaponTrait.TERRAIN_FOLLOW_RADAR }),
                UpgradePath.FGT, 492);

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
            // Phase 3 (final-intent): FighterMid + DF+3 + MULTIROLE_STRIKE (GA+4) + LASER_GUIDED_MUNITIONS (GA+2)
            // + RUNWAY_CRATERING (OcSuppression+20 stored) + TERRAIN_FOLLOW_RADAR (dormant). The lighter UK strike
            // variant (no HEAVY_PAYLOAD → OL6, vs IDS's OL9). → DF13 MAN11 TS10 SUR7 · GA8 OL6 · SR4 · OcSuppression 20.
            WeaponProfile TORNADO_GR1 = WeaponProfile.FromProfileDef(
                "Tornado GR.1 Strike Fighter", "Tornado GR.1", WeaponType.FGT_TORNADO_GR1_US,
                new ProfileDef(FamilyArchetypes.FighterMid,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 3 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE, WeaponTrait.LASER_GUIDED_MUNITIONS, WeaponTrait.RUNWAY_CRATERING, WeaponTrait.TERRAIN_FOLLOW_RADAR }),
                UpgradePath.ATT, 528);

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
            // Phase 3 (final-intent): FighterEarly + DF+2/TS+2/SUR+2 (FRG air-defense Phantom, ICE upgrade era).
            // Pure interceptor → GA Rule-A floor 2 (old NA/0). Mirrors the US F-4. → DF10 MAN9 TS12 SUR8 · GA2 OL6 · SR4.
            WeaponProfile F4_GE = WeaponProfile.FromProfileDef(
                "F-4F Phantom Fighter", "F-4F Phantom", WeaponType.FGT_F4_GE,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 2 }, { ProfileStat.TS, 2 }, { ProfileStat.SUR, 2 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.FGT, 276);

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
            // Phase 3 (final-intent): FighterMid + DF+4/MAN+4/TS+2 (agile delta-wing, the most maneuverable NATO
            // fighter here) + MULTIROLE_STRIKE (GA+4) + LASER_GUIDED_MUNITIONS (GA+2) — the 2000D strike fit.
            // → DF14 MAN15 TS12 SUR7 · GA8 OL6 · SR4.
            WeaponProfile MIRAGE2000 = WeaponProfile.FromProfileDef(
                "Mirage 2000 Multi-Role Fighter", "Mirage 2000", WeaponType.FGT_MIRAGE2000_FR,
                new ProfileDef(FamilyArchetypes.FighterMid,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 4 }, { ProfileStat.MAN, 4 }, { ProfileStat.TS, 2 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE, WeaponTrait.LASER_GUIDED_MUNITIONS }),
                UpgradePath.FGT, 552);

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
            // Phase 3 (final-intent): FighterEarly + DF+3/MAN+1/TS+1/SUR+2 (older multirole) + MULTIROLE_STRIKE
            // (GA+4). Lighter striker than the Mirage 2000 (GA6 vs 8). → DF11 MAN10 TS11 SUR8 · GA6 OL6 · SR4.
            WeaponProfile MIRAGEF1 = WeaponProfile.FromProfileDef(
                "Mirage F1 Fighter", "Mirage F1", WeaponType.FGT_MIRAGEF1_FR,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 3 }, { ProfileStat.MAN, 1 }, { ProfileStat.TS, 1 }, { ProfileStat.SUR, 2 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE }),
                UpgradePath.FGT, 420);

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
            // Phase 3 (final-intent): Attack archetype (GA10/OL9, the apex CAS baseline) + HEAVY_AG_CANNON
            // (GAU-8: GA+2, GaVsHard+2 stored) + AT_GUIDED_AIR (Maverick: GA+3, GaVsHard+1 stored) + CAS_ARMORED
            // (SUR+2) + LOITER_PERSISTENCE (re-attack hook, dormant). SUR+3 / OL+2 residuals preserve the
            // "flying tank" line. GA15 final (down from old 17), with GaVsHard 3 stored for the anti-armour bite.
            // → DF4 MAN4 TS7 SUR15 · GA15 OL11 · MMP100 · SR4 · GaVsHard 3.
            WeaponProfile A10 = WeaponProfile.FromProfileDef(
                "A-10 Thunderbolt II Attack Aircraft", "A-10 Warthog", WeaponType.ATT_A10_US,
                new ProfileDef(FamilyArchetypes.Attack,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SUR, 3 }, { ProfileStat.OL, 2 } },
                    new[] { WeaponTrait.HEAVY_AG_CANNON, WeaponTrait.AT_GUIDED_AIR, WeaponTrait.CAS_ARMORED, WeaponTrait.LOITER_PERSISTENCE }),
                UpgradePath.ATT, 468);

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
            // Phase 3 (final-intent): Bomber archetype (the F-117 air-combat line = bomber: DF1/MAN3/TS10/SUR8)
            // + HARDENED_STRIKE (GA+3, OL+2) + LASER_GUIDED_MUNITIONS (GA+2) + BUNKER_PENETRATOR (GaVsBase+4 stored)
            // + STEALTH_RAM (Stealth cap). STL+15 residual preserves the radar-cross-section; OL−8 residual reflects
            // the tiny internal bay (2 LGBs). GA13 precision strike (down from old 18). → DF1 MAN3 TS10 SUR8 ·
            // GA13 OL6 · STL15 · MMP100 · SR4 · GaVsBase 4 · stealth.
            WeaponProfile F117 = WeaponProfile.FromProfileDef(
                "F-117 Nighthawk Stealth Attack Aircraft", "F-117 Nighthawk", WeaponType.ATT_F117_US,
                new ProfileDef(FamilyArchetypes.Bomber,
                    new Dictionary<ProfileStat, int> { { ProfileStat.OL, -8 }, { ProfileStat.STL, 15 } },
                    new[] { WeaponTrait.HARDENED_STRIKE, WeaponTrait.LASER_GUIDED_MUNITIONS, WeaponTrait.BUNKER_PENETRATOR, WeaponTrait.STEALTH_RAM }),
                UpgradePath.ATT, 540);

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
            // Phase 3 (final-intent): FighterEarly + TS−1 (low-level, not a fast climber) + SUR+3 (rugged twin) +
            // MULTIROLE_STRIKE (GA+4) + LASER_GUIDED_MUNITIONS (GA+2) + HEAVY_PAYLOAD (OL+3) + RUNWAY_CRATERING
            // (BAP-100: OcSuppression+20 stored). Dedicated low-level attack jet. → DF8 MAN9 TS9 SUR9 · GA8 OL9 ·
            // SR4 · OcSuppression 20.
            WeaponProfile JAGUAR = WeaponProfile.FromProfileDef(
                "SEPECAT Jaguar Attack Aircraft", "Jaguar", WeaponType.ATT_JAGUAR_FR,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.TS, -1 }, { ProfileStat.SUR, 3 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE, WeaponTrait.LASER_GUIDED_MUNITIONS, WeaponTrait.HEAVY_PAYLOAD, WeaponTrait.RUNWAY_CRATERING }),
                UpgradePath.ATT, 420);

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
            // Phase 3 (final-intent): Bomber archetype + DF+5/MAN+3/TS+4 (fast swing-wing interdictor) +
            // HARDENED_STRIKE (GA+3, OL+2) + LASER_GUIDED_MUNITIONS (Pave Tack: GA+2) + BUNKER_PENETRATOR
            // (GaVsBase+4 stored) + TERRAIN_FOLLOW_RADAR (SUR vs ground-AD, dormant). GA13 heavy interdiction
            // (down from old 15); OL14 preserved. → DF6 MAN6 TS14 SUR8 · GA13 OL14 · MMP100 · SR4 · GaVsBase 4.
            WeaponProfile F111 = WeaponProfile.FromProfileDef(
                "F-111 Aardvark Bomber", "F-111 Aardvark", WeaponType.BMB_F111_US,
                new ProfileDef(FamilyArchetypes.Bomber,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 5 }, { ProfileStat.MAN, 3 }, { ProfileStat.TS, 4 } },
                    new[] { WeaponTrait.HARDENED_STRIKE, WeaponTrait.LASER_GUIDED_MUNITIONS, WeaponTrait.BUNKER_PENETRATOR, WeaponTrait.TERRAIN_FOLLOW_RADAR }),
                UpgradePath.BMB, 348);

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
            // Phase 3 (final-intent): Bomber archetype + NON_COMBATANT + SR+8 → AWACS_SPOTTING_RANGE 12 (the air
            // picture; W8). Mirrors the Soviet A-50. Carries no strike (GA inert). → DF1 MAN3 TS10 SUR8 · OL12 ·
            // MMP100 · SR12 · non-combatant.
            WeaponProfile E3 = WeaponProfile.FromProfileDef(
                "E-3 Sentry AWACS", "E-3 Sentry", WeaponType.AWACS_E3_US,
                new ProfileDef(FamilyArchetypes.Bomber,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SR, GameData.AWACS_SPOTTING_RANGE - GameData.AIR_UNIT_SPOTTING_RANGE } },
                    new[] { WeaponTrait.NON_COMBATANT }),
                UpgradePath.AWACS, 468);

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
            // Phase 3 (final-intent): FighterEarly + NON_COMBATANT (air recon — no direct engagement, §7A.19) +
            // HIGH_MACH_DASH + TS+11 → AC_HIGHSPEED_WESTERN 21 (Mach-3 dash) + SR+4 → AIR_RECON_SPOTTING_RANGE 8.
            // Mirrors the Soviet MiG-25R. → DF8 MAN9 TS21 SUR6 · MMP100 · SR8 · non-combatant.
            WeaponProfile SR71 = WeaponProfile.FromProfileDef(
                "SR-71 Blackbird Reconnaissance Aircraft", "SR-71 Blackbird", WeaponType.RCNA_SR71_US,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.TS, GameData.AC_HIGHSPEED_WESTERN - GameData.EARLY_FGT_TOPSPEED }, { ProfileStat.SR, GameData.AIR_RECON_SPOTTING_RANGE - GameData.AIR_UNIT_SPOTTING_RANGE } },
                    new[] { WeaponTrait.NON_COMBATANT, WeaponTrait.HIGH_MACH_DASH }),
                UpgradePath.RCNA, 336);

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
                _isAtt: true                              // Can this profile attack
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
            // Phase 3 (derived): Infantry archetype (GAD 10, R1) + RPG_LAW (HA+1) + ATGM_LIGHT (Dragon, HA+2) +
            // MANPADS_STINGER (FIM-92 — GAT floor 8 + ICM ×1.05 + fire-and-forget). NATO line infantry.
            // → HA8 HD7 SA7 SD8 GAD10 · GAT8 · ICM 1.05 · MMP4 · SR2.
            WeaponProfile INF_REG_US_P = WeaponProfile.FromProfileDef(
                "US Regular Infantry", "US Regulars", WeaponType.INF_REG_US,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.ATGM_LIGHT, WeaponTrait.MANPADS_STINGER }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + ATGM_LIGHT + MANPADS_STINGER + AMPHIBIOUS (USMC assault swim).
            // → HA8 HD7 SA7 SD8 GAD10 · GAT8 · ICM 1.05 · MMP4 · SR2 · amphibious.
            WeaponProfile INF_MAR_US_P = WeaponProfile.FromProfileDef(
                "US Marine Infantry", "US Marines", WeaponType.INF_MAR_US,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.ATGM_LIGHT, WeaponTrait.MANPADS_STINGER, WeaponTrait.AMPHIBIOUS }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + ATGM_MEDIUM (TOW/Dragon mix, HA+3 — light forces lean on ATGM) +
            // MANPADS_STINGER + AIR_DROPPABLE (82nd Airborne parachute deploy).
            // → HA9 HD7 SA7 SD8 GAD10 · GAT8 · ICM 1.05 · MMP4 · SR2 · air-droppable.
            WeaponProfile INF_AB_US_P = WeaponProfile.FromProfileDef(
                "US Airborne Infantry", "US Airborne", WeaponType.INF_AB_US,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.ATGM_MEDIUM, WeaponTrait.MANPADS_STINGER, WeaponTrait.AIR_DROPPABLE }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + ATGM_MEDIUM + MANPADS_STINGER + MOUNTAIN_TRAINED
            // (101st Airborne — helo-inserted light infantry, reduced move cost in non-clear terrain).
            // → HA9 HD7 SA7 SD8 GAD10 · GAT8 · ICM 1.05 · MMP4 · SR2 · mountain movement.
            WeaponProfile INF_AM_US_P = WeaponProfile.FromProfileDef(
                "US Air-Mobile Infantry", "US Air-Mobile", WeaponType.INF_AM_US,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.ATGM_MEDIUM, WeaponTrait.MANPADS_STINGER, WeaponTrait.MOUNTAIN_TRAINED }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + ATGM_LIGHT + MANPADS_BASIC (Blowpipe/Javelin — early SACLOS,
            // GAT floor 6; deliberately weaker organic AD than the US/FRG Stinger). Rapier is the brigade's separate AD.
            // → HA8 HD7 SA7 SD8 GAD10 · GAT6 · ICM 1.00 · MMP4 · SR2.
            WeaponProfile INF_REG_UK_P = WeaponProfile.FromProfileDef(
                "UK Regular Infantry", "UK Regulars", WeaponType.INF_REG_UK,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.ATGM_LIGHT, WeaponTrait.MANPADS_BASIC }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + ATGM_MEDIUM + MANPADS_BASIC + AIR_DROPPABLE (Parachute Regiment).
            // → HA9 HD7 SA7 SD8 GAD10 · GAT6 · ICM 1.00 · MMP4 · SR2 · air-droppable.
            WeaponProfile INF_AB_UK_P = WeaponProfile.FromProfileDef(
                "UK Airborne Infantry", "UK Airborne", WeaponType.INF_AB_UK,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.ATGM_MEDIUM, WeaponTrait.MANPADS_BASIC, WeaponTrait.AIR_DROPPABLE }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + ATGM_LIGHT + MANPADS_STINGER (Bundeswehr Fliegerfaust/Stinger).
            // → HA8 HD7 SA7 SD8 GAD10 · GAT8 · ICM 1.05 · MMP4 · SR2.
            WeaponProfile INF_REG_GE_P = WeaponProfile.FromProfileDef(
                "FRG Regular Infantry", "FRG Regulars", WeaponType.INF_REG_GE,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.ATGM_LIGHT, WeaponTrait.MANPADS_STINGER }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + ATGM_MEDIUM + MANPADS_STINGER + AIR_DROPPABLE (Luftlandebrigade).
            // → HA9 HD7 SA7 SD8 GAD10 · GAT8 · ICM 1.05 · MMP4 · SR2 · air-droppable.
            WeaponProfile INF_AB_GE_P = WeaponProfile.FromProfileDef(
                "FRG Airborne Infantry", "FRG Airborne", WeaponType.INF_AB_GE,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.ATGM_MEDIUM, WeaponTrait.MANPADS_STINGER, WeaponTrait.AIR_DROPPABLE }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + ATGM_LIGHT + MANPADS_STINGER (Mistral — all-aspect FNF IR,
            // Stinger-class: GAT floor 8 + ICM ×1.05 + fire-and-forget).
            // → HA8 HD7 SA7 SD8 GAD10 · GAT8 · ICM 1.05 · MMP4 · SR2.
            WeaponProfile INF_REG_FR_P = WeaponProfile.FromProfileDef(
                "French Regular Infantry", "FR Regulars", WeaponType.INF_REG_FR,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.ATGM_LIGHT, WeaponTrait.MANPADS_STINGER }));

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
            // Phase 3 (derived): Infantry + RPG_LAW + ATGM_MEDIUM + MANPADS_STINGER (Mistral) + AIR_DROPPABLE
            // (11e Brigade Parachutiste).
            // → HA9 HD7 SA7 SD8 GAD10 · GAT8 · ICM 1.05 · MMP4 · SR2 · air-droppable.
            WeaponProfile INF_AB_FR_P = WeaponProfile.FromProfileDef(
                "French Airborne Infantry", "FR Airborne", WeaponType.INF_AB_FR,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.ATGM_MEDIUM, WeaponTrait.MANPADS_STINGER, WeaponTrait.AIR_DROPPABLE }));

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
            // Phase 3 (final-intent): the Iraqi T-55A export "monkey-model" = the Soviet T-55A line (Gen1 +
            // LOW_PROFILE + NBC_PROTECTED) + EXPORT_DOWNGRADE (thinner armour HD-2/SD-1 + simpler FCS ICM×0.9).
            // → HA7 HD4 SA5 SD6 GAD7 · ICM 0.90 · MMP10 · SR2.
            WeaponProfile T55A = WeaponProfile.FromProfileDef(
                "T-55A Medium Tank", "T-55A", WeaponType.TANK_T55A_IQ,
                new ProfileDef(TankArchetypes.Gen1,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.LOW_PROFILE, WeaponTrait.NBC_PROTECTED, WeaponTrait.EXPORT_DOWNGRADE }),
                UpgradePath.TANK, 240);

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
            // Phase 3 (final-intent): Iraqi T-62A export "monkey-model" = the Soviet T-62A line (Gen1 + HA+1 for the
            // 115mm up-gun + LOW_PROFILE + NBC_PROTECTED) + EXPORT_DOWNGRADE (HD-2/SD-1, ICM×0.9).
            // → HA8 HD4 SA5 SD6 GAD7 · ICM 0.90 · MMP10 · SR2.
            WeaponProfile T62A = WeaponProfile.FromProfileDef(
                "T-62A Medium Tank", "T-62A", WeaponType.TANK_T62A_IQ,
                new ProfileDef(TankArchetypes.Gen1,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, 1 } },
                    new[] { WeaponTrait.LOW_PROFILE, WeaponTrait.NBC_PROTECTED, WeaponTrait.EXPORT_DOWNGRADE }),
                UpgradePath.TANK, 276);

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
            // Phase 3 (final-intent): Iranian M60A3 = the NATO M60A3 line (Gen2 + SA+1 + LASER_RANGEFINDER) but with
            // THERMAL_IMAGER DROPPED — post-1979 Iran lost US support, so no TTS thermal sight (vs the NATO M60A3's
            // ICM 1.16 / SR 3). Real US tank, NOT a monkey-model (no EXPORT_DOWNGRADE). DESIGN CALL — flag for Bob.
            // → HA10 HD8 SA8 SD6 GAD7 · ICM 1.05 · MMP10 · SR2.
            WeaponProfile M60A3 = WeaponProfile.FromProfileDef(
                "M60A3 Main Battle Tank", "M60A3", WeaponType.TANK_M60A3_IR,
                new ProfileDef(TankArchetypes.Gen2,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, 1 } },
                    new[] { WeaponTrait.LASER_RANGEFINDER }),
                UpgradePath.TANK, 480);

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
            // Phase 3 (final-intent): Iraqi BMP-1 = the Soviet BMP-1P line (Ifv + ATGM_RAIL Malyutka + AMPHIBIOUS).
            // No EXPORT_DOWNGRADE — that armour/FCS downgrade is reserved for the export tanks; standard BMP-1s.
            // → HA8 HD4 SA8 SD7 GAD7 · ICM 1.00 · MMP10 · SR2 · amphibious.
            WeaponProfile BMP1_IQ = WeaponProfile.FromProfileDef(
                "BMP-1 Infantry Fighting Vehicle", "BMP-1", WeaponType.IFV_BMP1_IQ,
                new ProfileDef(FamilyArchetypes.Ifv,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.ATGM_RAIL, WeaponTrait.AMPHIBIOUS }),
                UpgradePath.IFV, 336);

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
            // Phase 3 (final-intent): Iraqi MT-LB = the Soviet MT-LB line (Apc + AMPHIBIOUS). Standard export tractor.
            // → HA3 HD4 SA6 SD7 GAD7 · ICM 1.00 · MMP8 · SR2 · amphibious.
            WeaponProfile MTLB_IQ = WeaponProfile.FromProfileDef(
                "MT-LB Armored Personnel Carrier", "MT-LB", WeaponType.APC_MTLB_IQ,
                new ProfileDef(FamilyArchetypes.Apc,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.AMPHIBIOUS }),
                UpgradePath.APC, 312);

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
            // Phase 3 (final-intent): Iranian M113 = the NATO M113 line (bare Apc archetype). Real US APC, no
            // downgrade. Amphibious DROPPED to match the ratified NATO M113 (marginal swim, NATO Batch A).
            // → HA3 HD4 SA6 SD7 GAD7 · ICM 1.00 · MMP8 · SR2.
            WeaponProfile M113_IR = WeaponProfile.FromProfileDef(
                "M113 Armored Personnel Carrier", "M113", WeaponType.APC_M113_IR,
                new ProfileDef(FamilyArchetypes.Apc,
                    new Dictionary<ProfileStat, int>(),
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.APC, 264);

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
            // Phase 3 (final-intent): Iraqi 2S1 = the Soviet 2S1 line (Artillery + IR SHORT + SELF_PROPELLED). 122mm
            // light SP howitzer; no EXPORT_DOWNGRADE (artillery fires the same shell). IR 5→4 to match the Soviet 2S1.
            // → HA5 HD7 SA9 SD7 GAD7 · ICM 1.00 · MMP10 · IR4 · SR2.
            WeaponProfile SPA_2S1_AR = WeaponProfile.FromProfileDef(
                "2S1 Gvozdika Self-Propelled Howitzer", "2S1 Gvozdika", WeaponType.SPA_2S1_IQ,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SHORT } },
                    new[] { WeaponTrait.SELF_PROPELLED }),
                UpgradePath.ART, 396);

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
            // Phase 3 (final-intent): bare Artillery archetype + IR SHORT (= the Soviet/NATO light towed line). The
            // old Arab-specific stat maluses + MOT mobility are dropped — a towed howitzer is the same gun for anyone.
            // → HA5 HD5 SA9 SD5 GAD8 · ICM 1.00 · MMP4 · IR4 · SR2.
            WeaponProfile ART_LT_AR = WeaponProfile.FromProfileDef(
                "Light Towed Artillery", "Light Artillery", WeaponType.ART_LIGHT_ARAB,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SHORT } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.ART, 144);

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
            // Phase 3 (final-intent): Artillery archetype + IR MEDIUM + SA+1 (= the Soviet/NATO heavy towed line).
            // Old Arab-specific maluses + MOT mobility dropped; IR LONG→MEDIUM to match the standard heavy towed.
            // → HA5 HD5 SA10 SD5 GAD8 · ICM 1.00 · MMP4 · IR5 · SR2.
            WeaponProfile ART_HV_AR = WeaponProfile.FromProfileDef(
                "Heavy Towed Artillery", "Heavy Artillery", WeaponType.ART_HEAVY_ARAB,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_MEDIUM }, { ProfileStat.SA, 1 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.ART, 144);

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
            // Phase 3 (final-intent): improvised Mujahideen AAA (DShK/ZU-23 on technicals). Aaa archetype +
            // {HA-1, SA-1, GAT-2 (no fire control), GAD-2 (not a dug-in battery — on trucks/dispersed)} + IR AAA.
            // DESIGN CALL — no ratified MJ AD line; invented as a weaker generic AAA. Flag/tunable (GAT rebalance pass).
            // → HA3 HD4 SA8 SD6 GAD10 · GAT7 · MMP4 · IR3 · SR3.
            WeaponProfile AAA_MJ = WeaponProfile.FromProfileDef(
                "Mujahideen Anti-Aircraft Artillery", "MJ AAA", WeaponType.AAA_GEN_MJ,
                new ProfileDef(FamilyArchetypes.Aaa,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, -1 }, { ProfileStat.SA, -1 }, { ProfileStat.GAT, -2 }, { ProfileStat.GAD, -2 }, { ProfileStat.IR, GameData.INDIRECT_RANGE_AAA } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.AAA, 144);

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
            // Phase 3 (final-intent): Mujahideen Stinger team — man-portable IR MANPADS (the famous CIA Stingers).
            // Sam archetype (air-only HA/SA 1) + {GAT-2 (→8, the shoulder-Stinger value, not a radar SAM), GAD+2
            // (→10, dispersed infantry team), SR-4 (→2, no radar), IR AAA(3, short reach)}. DESIGN CALL — no ratified
            // MJ AD line; Sam base is a loose fit for a shoulder team. Flag/tunable (GAT rebalance pass).
            // → HA1 HD3 SA1 SD3 GAD10 · GAT8 · MMP4 · IR3 · SR2.
            WeaponProfile SAM_MJ = WeaponProfile.FromProfileDef(
                "Mujahideen Stinger SAM Team", "MJ SAM", WeaponType.SAM_GEN_MJ,
                new ProfileDef(FamilyArchetypes.Sam,
                    new Dictionary<ProfileStat, int> { { ProfileStat.GAT, -2 }, { ProfileStat.GAD, 2 }, { ProfileStat.SR, -4 }, { ProfileStat.IR, GameData.INDIRECT_RANGE_AAA } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.SAM, 264);

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
            // Phase 3 (final-intent): Iraqi ZSU-57-2 = the Soviet ZSU-57-2 line (Aaa + IR AAA + SELF_PROPELLED).
            // Optical twin-57mm SP gun; no EXPORT_DOWNGRADE (gun system). → HA4 HD6 SA9 SD8 GAD11 · GAT9 · MMP10 · IR3 · SR3.
            WeaponProfile ZSU_57_IQ = WeaponProfile.FromProfileDef(
                "ZSU-57 Self-Propelled Anti-Aircraft Gun", "ZSU-57", WeaponType.SPAAA_ZSU57_IQ,
                new ProfileDef(FamilyArchetypes.Aaa,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_AAA } },
                    new[] { WeaponTrait.SELF_PROPELLED }),
                UpgradePath.AAA, 372);

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
            // Phase 3 (final-intent): Iraqi 2K12 Kub = the Soviet Kub line (Sam + IR SAM + SELF_PROPELLED +
            // SARH_LONG_RANGE + MOBILE_SHOOT_SCOOT). Radar-illuminated medium SAM; no EXPORT_DOWNGRADE.
            // → HA1 HD5 SA1 SD5 GAD7 · GAT13 · MMP10 · IR6 · SR6 · shoot-scoot.
            WeaponProfile SPSAM_2k12 = WeaponProfile.FromProfileDef(
                "2K12 Self-Propelled SAM", "2K12", WeaponType.SPSAM_2K12_IQ,
                new ProfileDef(FamilyArchetypes.Sam,
                    new Dictionary<ProfileStat, int> { { ProfileStat.IR, GameData.INDIRECT_RANGE_SAM } },
                    new[] { WeaponTrait.SELF_PROPELLED, WeaponTrait.SARH_LONG_RANGE, WeaponTrait.MOBILE_SHOOT_SCOOT }),
                UpgradePath.AAA, 372);

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
            // Phase 3 (final-intent): export MiG-21 = FighterEarly + DF-1/MAN-1 (downgraded export avionics — a notch
            // below the Soviet MiG-21). Pure interceptor → GA floor 2. No EXPORT_DOWNGRADE (tank-shaped; the air
            // downgrade is the DF/MAN residual). → DF7 MAN8 TS10 SUR6 · GA2 OL6 · MMP100 · SR4.
            WeaponProfile MIG21_IQ = WeaponProfile.FromProfileDef(
                "MiG-21 Fishbed Interceptor", "MiG-21 Fishbed", WeaponType.FGT_MIG21_IQ,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, -1 }, { ProfileStat.MAN, -1 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.FGT, 252);

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
            // Phase 3 (final-intent): export MiG-23 = FighterEarly + DF+2/TS+1/SUR+1 (a notch below the Soviet
            // MiG-23's DF+3/SUR+2). Pure fighter → GA floor 2. → DF10 MAN9 TS11 SUR7 · GA2 OL6 · MMP100 · SR4.
            WeaponProfile MIG23_IQ = WeaponProfile.FromProfileDef(
                "MiG-23 Flogger Fighter", "MiG-23 Flogger", WeaponType.FGT_MIG23_IQ,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 2 }, { ProfileStat.TS, 1 }, { ProfileStat.SUR, 1 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.FGT, 384);

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
            // Phase 3 (final-intent): Iraqi Su-17 = the Soviet Su-17 line (FighterEarly + SUR+1 + MULTIROLE_STRIKE,
            // GA6). Same airframe; dumb-bomb fighter-bomber. → DF8 MAN9 TS10 SUR7 · GA6 OL6 · MMP100 · SR4.
            WeaponProfile SU17_IQ = WeaponProfile.FromProfileDef(
                "Su-17 Fitter Attack Aircraft", "Su-17 Fitter", WeaponType.ATT_SU17_IQ,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SUR, 1 } },
                    new[] { WeaponTrait.MULTIROLE_STRIKE }),
                UpgradePath.ATT, 384);

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
            // Phase 3 (final-intent): Iranian F-4 = FighterEarly + DF+1/MAN-1/TS+1/SUR+1 (real US Phantom; the
            // air-combat line preserved). Pure fighter → GA floor 2 (= the US/FRG F-4 treatment).
            // → DF9 MAN8 TS11 SUR7 · GA2 OL6 · MMP100 · SR4.
            WeaponProfile F4_IR = WeaponProfile.FromProfileDef(
                "F-4 Phantom Fighter", "F-4 Phantom", WeaponType.FGT_F4_IR,
                new ProfileDef(FamilyArchetypes.FighterEarly,
                    new Dictionary<ProfileStat, int> { { ProfileStat.DF, 1 }, { ProfileStat.MAN, -1 }, { ProfileStat.TS, 1 }, { ProfileStat.SUR, 1 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.FGT, 276);

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
            // Phase 3 (final-intent): Iranian F-14 = FighterLate + MAN-1/TS+1/SUR-1 — the prized Tomcats, but a
            // notch below the US F-14 (DF12 vs 14) reflecting Iran's post-1979 isolation/maintenance. Pure fleet
            // interceptor → GA floor 2. → DF12 MAN11 TS11 SUR8 · GA2 OL6 · MMP100 · SR4.
            WeaponProfile F14_IR = WeaponProfile.FromProfileDef(
                "F-14 Tomcat Fleet Defense Fighter", "F-14 Tomcat", WeaponType.FGT_F14_IR,
                new ProfileDef(FamilyArchetypes.FighterLate,
                    new Dictionary<ProfileStat, int> { { ProfileStat.MAN, -1 }, { ProfileStat.TS, 1 }, { ProfileStat.SUR, -1 } },
                    System.Array.Empty<WeaponTrait>()),
                UpgradePath.FGT, 432);

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
            // Phase 3 (final-intent): Arab truck = the Soviet/NATO generic truck line (Truck archetype + NON_COMBATANT).
            // → HA3 HD3 SA3 SD3 GAD6 · MMP8 · SR2 · non-combatant.
            WeaponProfile TRK_AR = WeaponProfile.FromProfileDef(
                "Generic Transport Truck", "Transport Truck", WeaponType.TRK_GEN_ARAB,
                new ProfileDef(FamilyArchetypes.Truck,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.NON_COMBATANT }));

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
            // Phase 3 (final-intent): Iraqi regulars = the Soviet regular-infantry line (Infantry + RPG_LAW +
            // MANPADS_BASIC Strela). Conventional army troops. → HA6 HD7 SA7 SD8 GAD10 · GAT6 · MMP4 · SR2.
            WeaponProfile INF_REG_IQ_P = WeaponProfile.FromProfileDef(
                "Iraqi Regular Infantry", "IQ Regulars", WeaponType.INF_REG_IQ,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.MANPADS_BASIC }));

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
            // Phase 3 (final-intent): Iranian regulars = the same conventional line as the Iraqi regulars
            // (Infantry + RPG_LAW + MANPADS_BASIC). → HA6 HD7 SA7 SD8 GAD10 · GAT6 · MMP4 · SR2.
            WeaponProfile INF_REG_IR_P = WeaponProfile.FromProfileDef(
                "Iranian Regular Infantry", "IR Regulars", WeaponType.INF_REG_IR,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.MANPADS_BASIC }));

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
            // Phase 3 (final-intent): Mujahideen regulars = Infantry + RPG_LAW + MOUNTAIN_TRAINED (Afghan guerrillas —
            // the defining mountain-mobility trait). No MANPADS (the Stinger is the dedicated MJ SAM team).
            // DESIGN CALL — MOUNTAIN_TRAINED added to all MJ infantry as final intent; flag/easy to remove.
            // → HA6 HD7 SA7 SD8 GAD10 · GAT0 · MMP4 · SR2 · mountain movement.
            WeaponProfile INF_MJ_REG = WeaponProfile.FromProfileDef(
                "Mujahideen Regular Infantry", "MJ Regulars", WeaponType.INF_REG_MJ,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.MOUNTAIN_TRAINED }));

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
            // Phase 3 (final-intent): MJ elite = Infantry + SR+1 (→ recon SR 3) + SPECIAL_FORCES (HA+2/SA+2/SD+1/
            // ICM×1.10) + RPG_LAW + MOUNTAIN_TRAINED. No MANPADS here (the Stinger is the dedicated MJ SAM team —
            // avoids stacking the SF and Stinger ICMs). → HA8 HD7 SA9 SD9 GAD10 · GAT0 · ICM 1.10 · MMP4 · SR3 · mountain.
            WeaponProfile INF_MJ_SPEC = WeaponProfile.FromProfileDef(
                "Mujahideen Special Forces", "MJ Elite", WeaponType.INF_SPEC_MJ,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SR, 1 } },
                    new[] { WeaponTrait.SPECIAL_FORCES, WeaponTrait.RPG_LAW, WeaponTrait.MOUNTAIN_TRAINED }));

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
            // Phase 3 (final-intent): MJ horse cavalry = Infantry + MMP+6 (→ CAVALRY_UNIT 10, mounted) + RPG_LAW +
            // MOUNTAIN_TRAINED. Fast raiders. → HA6 HD7 SA7 SD8 GAD10 · GAT0 · MMP10 · SR2 · mountain movement.
            WeaponProfile INF_MJ_CAV = WeaponProfile.FromProfileDef(
                "Mujahideen Horse Cavalry", "MJ Cavalry", WeaponType.INF_CAV_MJ,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int> { { ProfileStat.MMP, GameData.CAVALRY_UNIT - GameData.FOOT_UNIT } },
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.MOUNTAIN_TRAINED }));

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
            // Phase 3 (final-intent): MJ RPG teams = Infantry + HA+2/SA-1 (massed RPG-7 + recoilless rifles — AT
            // specialists; HA+2 is a residual, no clean trait for recoilless) + RPG_LAW (+1 → HA8) + MOUNTAIN_TRAINED.
            // → HA8 HD7 SA6 SD8 GAD10 · GAT0 · MMP4 · SR2 · mountain movement.
            WeaponProfile INF_MJ_RPG = WeaponProfile.FromProfileDef(
                "Mujahideen RPG Teams", "MJ RPG", WeaponType.INF_RPG_MJ,
                new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int> { { ProfileStat.HA, 2 }, { ProfileStat.SA, -1 } },
                    new[] { WeaponTrait.RPG_LAW, WeaponTrait.MOUNTAIN_TRAINED }));

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
            // Phase 3 (final-intent): MJ heavy mortar = Artillery + SA-2 (improvised crew) + GAD+2 (→10, dispersed
            // teams, not a battery) + IR MINIMUM (short mortar reach). DESIGN CALL — invented MJ line; flag/tunable.
            // → HA5 HD5 SA7 SD5 GAD10 · MMP4 · IR3 · SR2.
            WeaponProfile ART_MJ_MORT = WeaponProfile.FromProfileDef(
                "Mujahideen Heavy Mortar", "MJ Mortar", WeaponType.ART_MORTAR_MJ,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, -2 }, { ProfileStat.GAD, 2 }, { ProfileStat.IR, GameData.INDIRECT_RANGE_MINIMUM } },
                    System.Array.Empty<WeaponTrait>()));

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
            // Phase 3 (final-intent): MJ light artillery = Artillery + SA-3 (captured/obsolete field guns, weakest
            // indirect) + GAD+2 (→10, dispersed) + IR MINIMUM. DESIGN CALL — invented MJ line; flag/tunable.
            // → HA5 HD5 SA6 SD5 GAD10 · MMP4 · IR3 · SR2.
            WeaponProfile ART_MJ_LT = WeaponProfile.FromProfileDef(
                "Mujahideen Light Artillery", "MJ Artillery", WeaponType.ART_LIGHT_MJ,
                new ProfileDef(FamilyArchetypes.Artillery,
                    new Dictionary<ProfileStat, int> { { ProfileStat.SA, -3 }, { ProfileStat.GAD, 2 }, { ProfileStat.IR, GameData.INDIRECT_RANGE_MINIMUM } },
                    System.Array.Empty<WeaponTrait>()));

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
            // Phase 3 (final-intent): Type 59 (T-54 copy, 100mm) = Gen1 + LOW_PROFILE. Domestic Chinese design (not a
            // monkey-model), no NBC. = the T-55A line minus the dormant NBC. → HA7 HD6 SA5 SD7 GAD7 · ICM 1.00 · MMP10 · SR2.
            WeaponProfile TYPE59 = WeaponProfile.FromProfileDef(
                "Type 59 Medium Tank", "Type 59", WeaponType.TANK_TYPE59,
                new ProfileDef(TankArchetypes.Gen1,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.LOW_PROFILE }),
                UpgradePath.TANK, 252);

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
            // Phase 3 (final-intent): Type 80 (105mm rifled, China's first modern MBT) = Gen2 + LASER_RANGEFINDER
            // (basic FCS, no thermal). → HA10 HD8 SA7 SD6 GAD7 · ICM 1.05 · MMP10 · SR2.
            WeaponProfile TYPE80 = WeaponProfile.FromProfileDef(
                "Type 80 Main Battle Tank", "Type 80", WeaponType.TANK_TYPE80,
                new ProfileDef(TankArchetypes.Gen2,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.LASER_RANGEFINDER }),
                UpgradePath.TANK, 564);

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
            // Phase 3 (final-intent): Type 95 (modern 125mm Chinese MBT) = Gen3 + LASER_RANGEFINDER + BALLISTIC_COMPUTER.
            // Capable Gen3 hull/gun but NO Western thermal → ICM 1.10 / SR 2, deliberately below the M1/Leo2 (ICM 1.33,
            // SR 3) — Chinese thermal/optics lag of the era. → HA13 HD11 SA9 SD6 GAD7 · ICM 1.10 · MMP10 · SR2.
            WeaponProfile TYPE95 = WeaponProfile.FromProfileDef(
                "Type 95 Main Battle Tank", "Type 95", WeaponType.TANK_TYPE95,
                new ProfileDef(TankArchetypes.Gen3,
                    new Dictionary<ProfileStat, int>(),
                    new[] { WeaponTrait.LASER_RANGEFINDER, WeaponTrait.BALLISTIC_COMPUTER }),
                UpgradePath.TANK, 588);

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
                _isAtt: true                              // Can this profile attack
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
                _isAtt: true                              // Can this profile attack
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
