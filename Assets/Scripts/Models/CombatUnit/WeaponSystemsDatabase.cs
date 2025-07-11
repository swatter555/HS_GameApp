using HammerAndSickle.Services;
using System;
using System.Collections.Generic;

namespace HammerAndSickle.Models
{
/*───────────────────────────────────────────────────────────────────────────────
  WeaponSystemsDatabase ─ master catalogue of every WeaponSystemProfile
────────────────────────────────────────────────────────────────────────────────
 Summary
 ═══════
 • Static, in-memory dictionary that maps each **WeaponSystems** enum value to an
   immutable **WeaponSystemProfile**.  
 • Guarantees one-time initialisation during game start-up, then provides
   lock-free, read-only access to profile data for all combat calculations.  
 • Central location for combat-balance tuning: modify a profile here and every
   **CombatUnit** that references the same enum reflects the change instantly. :contentReference[oaicite:0]{index=0}

 Public properties
 ═════════════════
   bool IsInitialized   { get; }                // true after successful Initialise()
   int  ProfileCount    { get; }                // total profiles currently stored

 Constructors
 ═════════════
   // none – static class

 Public API (method signatures ⇢ brief purpose)
 ═════════════════════════════════════════════
   public static void Initialize()                                     // build full DB; call once at start-up
   public static WeaponSystemProfile
                       GetWeaponSystemProfile(WeaponSystems id)        // fast lookup; returns null if absent
   public static bool  HasWeaponSystemProfile(WeaponSystems id)        // existence check without retrieval

 Private helpers
 ═══════════════
   static void CreateAllWeaponSystemProfiles()         // instantiates every profile; calls individual builders
   static void CreateT80BProfile()                     // example builder: crafts “T-80B Main Battle Tank”

 Developer notes
 ═══════════════
 • **Template pattern** – Every CombatUnit stores only an enum; the heavy
   profile object lives here exactly once, reducing per-unit RAM and ensuring
   global consistency.  
 • **Initialisation contract** – Always call *WeaponSystemsDatabase.Initialize()*
   during game boot before any unit creation; otherwise look-ups return *null*.  
 • **Error handling** – All public and private methods wrap operations in
   try/catch and delegate to `AppService.HandleException`.  Failed initialisation
   re-throws to abort loading early.  
 • **Extensibility** – Add new weapon systems by writing a `CreateXXXProfile()`
   helper and calling it from *CreateAllWeaponSystemProfiles()*; keep enum,
   profile builder, and upgrade definitions in sync.  
 • **Thread-safety** – After the one-time build (which uses a local dictionary
   before assignment), the underlying `_weaponSystemProfiles` is never mutated,
   enabling lock-free concurrent reads by AI threads or parallel combat sims.
───────────────────────────────────────────────────────────────────────────────*/
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
                //Create Soviet profiles
                CreateT55AProfile();
                CreateT64AProfile();
                CreateT64BProfile();
                CreateT72AProfile();
                CreateT72BProfile();
                CreateT80BProfile();
                CreateT80UProfile();
                CreateT80BVProfile();


            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CreateAllWeaponSystemProfiles", e);
                throw;
            }
        }

        /// <summary>
        /// T55A Profile
        /// </summary>
        private static void CreateT55AProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-55A Main Battle Tank",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T55A,
                    40,
                    landHardAttack:  8,
                    landHardDefense: 7,
                    landSoftAttack:  9,
                    landSoftDefense: 8,
                    landAirAttack:   3,
                    landAirDefense:  5,
                    primaryRange:    1f,
                    indirectRange:   0f,
                    spottingRange:   2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

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
        private static void CreateT64AProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-64A Main Battle Tank",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T64A,
                    80,
                    landHardAttack: 10,
                    landHardDefense: 11,
                    landSoftAttack: 13,
                    landSoftDefense: 13,
                    landAirAttack: 3,
                    landAirDefense: 6,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

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
        private static void CreateT64BProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-64B Main Battle Tank",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T64B,
                    115,
                    landHardAttack: 11,
                    landHardDefense: 13,
                    landSoftAttack: 15,
                    landSoftDefense: 15,
                    landAirAttack: 3,
                    landAirDefense: 8,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small
                );

                profile.AddUpgradeType(UpgradeType.AFV);

                _weaponSystemProfiles[WeaponSystems.TANK_T64B] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateT64BProfile), e);
                throw;
            }
        }

        /// <summary>
        /// Creates the T-72A tank profile.
        /// </summary>
        private static void CreateT72AProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-72A Main Battle Tank",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T72A,
                    60,
                    landHardAttack: 11,
                    landHardDefense: 10,
                    landSoftAttack: 11,
                    landSoftDefense: 10,
                    landAirAttack: 3,
                    landAirDefense: 7,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
                );

                profile.AddUpgradeType(UpgradeType.AFV);

                _weaponSystemProfiles[WeaponSystems.TANK_T72A] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateT72AProfile), e);
                throw;
            }
        }

        /// <summary>
        /// Creates the T-72B tank profile.
        /// </summary>
        private static void CreateT72BProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-72B Main Battle Tank",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T72B,
                    105,
                    landHardAttack: 13,
                    landHardDefense: 12,
                    landSoftAttack: 12,
                    landSoftDefense: 12,
                    landAirAttack: 3,
                    landAirDefense: 8,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium
                );

                profile.AddUpgradeType(UpgradeType.AFV);

                _weaponSystemProfiles[WeaponSystems.TANK_T72B] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateT72BProfile), e);
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
                    90,
                    landHardAttack:  14,
                    landHardDefense: 15,
                    landSoftAttack:  11,
                    landSoftDefense: 12,
                    landAirAttack:   3,
                    landAirDefense:  9,
                    primaryRange:    1f,
                    indirectRange:   0f,
                    spottingRange:   2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
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

        /// <summary>
        /// Creates the T-80U tank profile.
        /// </summary>
        private static void CreateT80UProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-80U Main Battle Tank",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T80U,
                    125,
                    landHardAttack:  17,
                    landHardDefense: 17,
                    landSoftAttack:  11,
                    landSoftDefense: 13,
                    landAirAttack:   3,
                    landAirDefense:  10,
                    primaryRange:    1f,
                    indirectRange:   0f,
                    spottingRange:   2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium
                );

                // Add upgrade types for modernization
                profile.AddUpgradeType(UpgradeType.AFV);

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
        private static void CreateT80BVProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-80BV Main Battle Tank",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T80BV,
                    150,
                    landHardAttack: 18,
                    landHardDefense: 17,
                    landSoftAttack: 12,
                    landSoftDefense: 14,
                    landAirAttack: 3,
                    landAirDefense: 11,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium
                );

                profile.AddUpgradeType(UpgradeType.AFV);

                _weaponSystemProfiles[WeaponSystems.TANK_T80BV] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateT80BVProfile), e);
                throw;
            }
        }



        #endregion // Private Methods
    }
}