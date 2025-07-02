using HammerAndSickle.Services;
using System;
using System.Collections.Generic;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Static database containing all WeaponSystemProfile definitions for the game.
    /// Provides centralized management and lookup of weapon system combat capabilities.
    /// 
    /// This class serves as the master repository for all weapon system profiles used
    /// throughout Hammer & Sickle. Each WeaponSystems enum value maps to a specific
    /// WeaponSystemProfile containing combat ratings, ranges, and tactical capabilities.
    /// 
    /// The database follows a shared template architecture where multiple CombatUnits
    /// reference the same WeaponSystemProfile instances, ensuring consistency and
    /// memory efficiency across large armies.
    /// 
    /// Key Features:
    /// - Static initialization with all game weapon systems
    /// - Fast Dictionary-based lookup by WeaponSystems enum
    /// - Centralized management of combat balance and capabilities
    /// - Memory-efficient shared profile references
    /// - Comprehensive error handling through AppService integration
    /// 
    /// Usage:
    /// var profile = WeaponSystemsDatabase.GetWeaponSystemProfile(WeaponSystems.TANK_T80B);
    /// if (profile != null) { /* use profile for combat calculations */ }
    /// 
    /// Initialization:
    /// WeaponSystemsDatabase.Initialize(); // Called during game startup
    /// </summary>
    public static class WeaponSystemsDatabase
    {
        #region Constants

        private const string CLASS_NAME = nameof(WeaponSystemsDatabase);

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
                // Example profile - T-80B Main Battle Tank
                CreateT80BProfile();

                // TODO: Add all other weapon system profiles here
                // CreateT72AProfile();
                // CreateBMP2Profile();
                // CreateM1AbamsProfile();
                // etc...
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CreateAllWeaponSystemProfiles", e);
                throw;
            }
        }

        /// <summary>
        /// Creates the T-80B main battle tank profile.
        /// Example of a modern Soviet main battle tank with strong armor and firepower.
        /// </summary>
        private static void CreateT80BProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-80B Main Battle Tank",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T80B,
                    landHardAttack: 22,
                    landHardDefense: 20,
                    landSoftAttack: 8,
                    landSoftDefense: 6,
                    landAirAttack: 0,
                    landAirDefense: 3,
                    primaryRange: 3f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1.2f,
                    allWeatherCapability: AllWeatherRating.Night,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Large
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

                _weaponSystemProfiles[WeaponSystems.TANK_T80B] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CreateT80BProfile", e);
                throw;
            }
        }

        #endregion // Private Methods
    }
}