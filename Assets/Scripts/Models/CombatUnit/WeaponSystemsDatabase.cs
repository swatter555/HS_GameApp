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
                CreateRegInfSVProfile();
                CreateAbInfSVProfile();
                CreateAmInfSVProfile();
                CreateMarInfSVProfile();
                CreateSpecInfSVProfile();
                CreateEngInfSVProfile();
                CreateApcMtlbProfile();
                CreateApcBtr70Profile();
                CreateApcBtr80Profile();
                CreateIfvBmp1Profile();
                CreateIfvBmp2Profile();
                CreateIfvBmp3Profile();
                CreateIfvBmd1Profile();
                CreateIfvBmd2Profile();
                CreateIfvBmd3Profile();
                CreateRcnBrdm2Profile();
                CreateRcnBrdm2AtProfile();
                CreateSpa2S1Profile();
                CreateSpa2S3Profile();
                CreateSpa2S5Profile();
                CreateSpa2S19Profile();
                CreateRocBm21Profile();
                CreateRocBm27Profile();
                CreateRocBm30Profile();


            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CreateAllWeaponSystemProfiles", e);
                throw;
            }
        }

        #endregion // Private Methods


        #region Soviet Tanks

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
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.AirLift,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small
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
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small
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
        private static void CreateT64BProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "T-64B Main Battle Tank",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.TANK_T64B,
                    110,
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
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small
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
                    65,
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
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
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
                    100,
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
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium
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
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
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
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium
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
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium
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
                AppService.HandleException(CLASS_NAME, nameof(CreateT80BVProfile), e);
                throw;
            }
        }

        #endregion // Soviet Tanks


        #region Soviet Infantry

        private static void CreateRegInfSVProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Soviet Regular Infantry",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.REG_INF_SV,
                    15,
                    landHardAttack: 4,
                    landHardDefense: 3,
                    landSoftAttack: 9,
                    landSoftDefense: 10,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift
                );

                // Upgrade paths
                profile.AddUpgradeType(UpgradeType.Infantry);

                // Set short name for UI display
                profile.SetShortName("Infantry");

                // Set turn availability in months.
                profile.SetTurnAvailable(300);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.REG_INF_SV] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateRegInfSVProfile), e);
                throw;
            }
        }

        private static void CreateAbInfSVProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Soviet Airborne Infantry",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.AB_INF_SV,
                    18,
                    landHardAttack: 4,
                    landHardDefense: 3,
                    landSoftAttack: 9,
                    landSoftDefense: 10,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop
                );

                // Upgrade paths
                profile.AddUpgradeType(UpgradeType.Infantry);

                // Set short name for UI display
                profile.SetShortName("Airborne");

                // Set turn availability in months.
                profile.SetTurnAvailable(300);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.AB_INF_SV] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAbInfSVProfile), e);
                throw;
            }
        }

        private static void CreateAmInfSVProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Soviet Air‑Mobile Infantry",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.AM_INF_SV,
                    18,
                    landHardAttack: 4,
                    landHardDefense: 3,
                    landSoftAttack: 9,
                    landSoftDefense: 10,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirMobile
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.Infantry);

                // Set short name for UI display
                profile.SetShortName("Air-Mobile");

                // Set turn availability in months.
                profile.SetTurnAvailable(476);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.AM_INF_SV] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAmInfSVProfile), e);
                throw;
            }
        }

        private static void CreateMarInfSVProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Soviet Naval Infantry",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.MAR_INF_SV,
                    18,
                    landHardAttack: 4,
                    landHardDefense: 3,
                    landSoftAttack: 9,
                    landSoftDefense: 10,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.NavalAssault
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.Infantry);

                // Set short name for UI display
                profile.SetShortName("Marines");

                // Set turn availability in months.
                profile.SetTurnAvailable(300);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.MAR_INF_SV] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateMarInfSVProfile), e);
                throw;
            }
        }

        private static void CreateSpecInfSVProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Soviet Spetsnaz Infantry",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPEC_INF_SV,
                    30,
                    landHardAttack: 8,
                    landHardDefense: 4,
                    landSoftAttack: 11,
                    landSoftDefense:12,
                    landAirAttack: 3,
                    landAirDefense: 6,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 3f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirMobile
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.Infantry);

                // Set short name for UI display
                profile.SetShortName("Spetsnaz");

                // Set turn availability in months.
                profile.SetTurnAvailable(300);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPEC_INF_SV] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSpecInfSVProfile), e);
                throw;
            }
        }

        private static void CreateEngInfSVProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Combat Engineers",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ENG_INF_SV,
                    20,
                    landHardAttack: 4,
                    landHardDefense: 3,
                    landSoftAttack: 9,
                    landSoftDefense: 12,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.Infantry);

                // Set short name for UI display
                profile.SetShortName("Engineers");

                // Set turn availability in months.
                profile.SetTurnAvailable(300);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ENG_INF_SV] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateEngInfSVProfile), e);
                throw;
            }
        }


        #endregion


        #region Soviet APCs

        private static void CreateApcMtlbProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MT‑LB Armored Personnel Carrier",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.APC_MTLB,
                    20,
                    landHardAttack: 2,
                    landHardDefense: 3,
                    landSoftAttack: 5,
                    landSoftDefense: 5,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop
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
                AppService.HandleException(CLASS_NAME, nameof(CreateApcMtlbProfile), e);
                throw;
            }
        }

        private static void CreateApcBtr70Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BTR‑70 Armored Personnel Carrier",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.APC_BTR70,
                    24,
                    landHardAttack: 3,
                    landHardDefense: 4,
                    landSoftAttack: 6,
                    landSoftDefense: 6,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift
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
                AppService.HandleException(CLASS_NAME, nameof(CreateApcBtr70Profile), e);
                throw;
            }
        }

        private static void CreateApcBtr80Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BTR‑80 Armored Personnel Carrier",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.APC_BTR80,
                    28,
                    landHardAttack: 3,
                    landHardDefense: 4,
                    landSoftAttack: 7,
                    landSoftDefense: 7,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift
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
                AppService.HandleException(CLASS_NAME, nameof(CreateApcBtr80Profile), e);
                throw;
            }
        }

        #endregion // Soviet APCs


        #region Soviet IFVs

        private static void CreateIfvBmp1Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMP‑1 Infantry Fighting Vehicle",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMP1,
                    30,
                    landHardAttack: 8,
                    landHardDefense: 4,
                    landSoftAttack: 7,
                    landSoftDefense: 8,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift
                );

                // Set upgrade paths
                profile.AddUpgradeType(UpgradeType.IFV);

                // Set amphibious capability
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("BMP-1");

                // Set turn availability in months.
                profile.SetTurnAvailable(336);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.IFV_BMP1] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateIfvBmp1Profile), e);
                throw;
            }
        }

        private static void CreateIfvBmp2Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMP‑2 Infantry Fighting Vehicle",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMP2,
                    36,
                    landHardAttack: 10,
                    landHardDefense: 4,
                    landSoftAttack: 8,
                    landSoftDefense: 8,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirLift
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
                AppService.HandleException(CLASS_NAME, nameof(CreateIfvBmp2Profile), e);
                throw;
            }
        }

        private static void CreateIfvBmp3Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMP‑3 Infantry Fighting Vehicle",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMP3,
                    42,
                    landHardAttack: 11,
                    landHardDefense: 4,
                    landSoftAttack: 9,
                    landSoftDefense: 8,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirLift
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
                AppService.HandleException(CLASS_NAME, nameof(CreateIfvBmp3Profile), e);
                throw;
            }
        }

        private static void CreateIfvBmd1Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMD‑1 Airborne Infantry Fighting Vehicle",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMD1,
                    25,
                    landHardAttack: 5,
                    landHardDefense: 4,
                    landSoftAttack: 7,
                    landSoftDefense: 7,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop
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
                AppService.HandleException(CLASS_NAME, nameof(CreateIfvBmd1Profile), e);
                throw;
            }
        }

        private static void CreateIfvBmd2Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMD‑2 Airborne Infantry Fighting Vehicle",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMD2,
                    34,
                    landHardAttack: 5,
                    landHardDefense: 4,
                    landSoftAttack: 7,
                    landSoftDefense: 7,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop
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
                AppService.HandleException(CLASS_NAME, nameof(CreateIfvBmd2Profile), e);
                throw;
            }
        }

        private static void CreateIfvBmd3Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BMD‑3 Airborne Infantry Fighting Vehicle",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.IFV_BMD3,
                    38,
                    landHardAttack: 7,
                    landHardDefense: 5,
                    landSoftAttack: 9,
                    landSoftDefense: 7,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1.25f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirDrop
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
                AppService.HandleException(CLASS_NAME, nameof(CreateIfvBmd3Profile), e);
                throw;
            }
        }

        #endregion // Soviet IFVs


        #region Soviet Recon

        private static void CreateRcnBrdm2Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BRDM‑2 Recon Vehicle",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.RCN_BRDM2,
                    22,
                    landHardAttack: 4,
                    landHardDefense: 3,
                    landSoftAttack: 4,
                    landSoftDefense: 4,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 3f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop
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
                AppService.HandleException(CLASS_NAME, nameof(CreateRcnBrdm2Profile), e);
                throw;
            }
        }

        private static void CreateRcnBrdm2AtProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BRDM‑2 AT‑5 Recon Vehicle",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.RCN_BRDM2AT,
                    28,
                    landHardAttack: 10,
                    landHardDefense: 4,
                    landSoftAttack: 3,
                    landSoftDefense: 4,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 3f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Small,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.NavalAssault
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
                AppService.HandleException(CLASS_NAME, nameof(CreateRcnBrdm2AtProfile), e);
                throw;
            }
        }

        #endregion // Soviet Recon


        #region Soviet Artillery & Rockets

        /// <summary>
        /// 2S1 "Gvozdika" 122 mm self‑propelled howitzer.
        /// </summary>
        private static void CreateSpa2S1Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "2S1 Gvozdika Self Propelled Artillery",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPA_2S1,
                    40,
                    landHardAttack: 4,
                    landHardDefense: 3,
                    landSoftAttack: 12,
                    landSoftDefense: 4,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    primaryRange: 1f,
                    indirectRange: 3f,
                    spottingRange: 2f,
                    movementModifier: 1,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy
                );

                //Set the upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPART);

                // Set short name for UI display
                profile.SetShortName("2S1 Gvozdika");

                // Set turn availability in months.
                profile.SetTurnAvailable(408);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPA_2S1] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSpa2S1Profile), e);
                throw;
            }
        }

        /// <summary>
        /// 2S3 "Akatsiya" 152 mm self‑propelled gun‑howitzer.
        /// </summary>
        private static void CreateSpa2S3Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "2S3 Akatsiya Self Propelled Artillery",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPA_2S3,
                    55,
                    landHardAttack: 6,
                    landHardDefense: 3,
                    landSoftAttack: 14,
                    landSoftDefense: 4,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    primaryRange: 1f,
                    indirectRange: 4f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPART);

                // Set short name for UI display
                profile.SetShortName("2S3 Akatsiya");

                // Set turn availability in months.
                profile.SetTurnAvailable(420);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPA_2S3] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSpa2S3Profile), e);
                throw;
            }
        }

        /// <summary>
        /// 2S5 "Giatsint‑S" 152 mm long‑range gun.
        /// </summary>
        private static void CreateSpa2S5Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "2S5 Giatsint‑S Self Propelled Artillery",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPA_2S5,
                    70,
                    landHardAttack: 8,
                    landHardDefense: 3,
                    landSoftAttack: 16,
                    landSoftDefense: 4,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    primaryRange: 1f,
                    indirectRange: 5f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPART);

                // Set short name for UI display
                profile.SetShortName("2S5 Giatsint-S");

                // Set turn availability in months.
                profile.SetTurnAvailable(516);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPA_2S5] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSpa2S5Profile), e);
                throw;
            }
        }

        /// <summary>
        /// 2S19 "Msta‑S" 152 mm modern SP howitzer.
        /// </summary>
        private static void CreateSpa2S19Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "2S19 Msta‑S Self Propelled Artillery",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPA_2S19,
                    80,
                    landHardAttack: 10,
                    landHardDefense: 4,
                    landSoftAttack: 18,
                    landSoftDefense: 6,
                    landAirAttack: 1,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 5f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPART);

                // Set short name for UI display
                profile.SetShortName("2S19 Msta-S");

                // Set turn availability in months.
                profile.SetTurnAvailable(600);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPA_2S19] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSpa2S19Profile), e);
                throw;
            }
        }

        /// <summary>
        /// BM‑21 "Grad" 122 mm 40‑tube MLRS.
        /// </summary>
        private static void CreateRocBm21Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BM‑21 Grad MLRS",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ROC_BM21,
                    45,
                    landHardAttack: 4,
                    landHardDefense: 2,
                    landSoftAttack: 10,
                    landSoftDefense: 3,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    primaryRange: 1f,
                    indirectRange: 3f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy
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
                AppService.HandleException(CLASS_NAME, nameof(CreateRocBm21Profile), e);
                throw;
            }
        }

        /// <summary>
        /// BM‑27 "Uragan" 220 mm 16‑tube MLRS.
        /// </summary>
        private static void CreateRocBm27Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BM‑27 Uragan MLRS",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ROC_BM27,
                    60,
                    landHardAttack: 8,
                    landHardDefense: 2,
                    landSoftAttack: 12,
                    landSoftDefense: 4,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    primaryRange: 1f,
                    indirectRange: 4f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy
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
                AppService.HandleException(CLASS_NAME, nameof(CreateRocBm27Profile), e);
                throw;
            }
        }

        /// <summary>
        /// BM‑30 "Smerch" 300 mm 12‑tube MLRS.
        /// </summary>
        private static void CreateRocBm30Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "BM‑30 Smerch MLRS",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ROC_BM30,
                    85,
                    landHardAttack: 9,
                    landHardDefense: 3,
                    landSoftAttack: 14,
                    landSoftDefense: 5,
                    landAirAttack: 1,
                    landAirDefense: 3,
                    primaryRange: 1f,
                    indirectRange: 5f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy
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
                AppService.HandleException(CLASS_NAME, nameof(CreateRocBm30Profile), e);
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
                    200,
                    landHardAttack: 10,
                    landHardDefense: 1,
                    landSoftAttack: 15,
                    landSoftDefense: 2,
                    landAirAttack: 0,
                    landAirDefense: 1,
                    primaryRange: 1f,
                    indirectRange: 99f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy
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
        private static void CreateSpaaa_Zsu57Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "ZSU-57-2 Sparka Self-Propelled Anti-Aircraft Artillery",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPAAA_ZSU57,
                    35,
                    landHardAttack: 3,
                    landHardDefense: 3,
                    landSoftAttack: 10,
                    landSoftDefense: 5,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    airAttack: 10,
                    airDefense: 7,
                    primaryRange: 1f,
                    indirectRange: 2f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.AirDrop,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPAAA);

                // Set short name for UI display
                profile.SetShortName("ZSU-57-2");

                // Set turn availability - entered service in 1957
                profile.SetTurnAvailable(228);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPAAA_ZSU57] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSpaaa_Zsu57Profile), e);
                throw;
            }
        }

        /// <summary>
        /// ZSU-23-4 "Shilka" 23mm quad anti-aircraft gun with radar.
        /// </summary>
        private static void CreateSpaaa_Zsu23Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "ZSU-23-4 Shilka Self-Propelled Anti-Aircraft Artillery",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPAAA_ZSU23,
                    45,
                    landHardAttack: 4,
                    landHardDefense: 4,
                    landSoftAttack: 11,
                    landSoftDefense: 6,
                    landAirAttack: 2,
                    landAirDefense: 3,
                    airAttack: 13,
                    airDefense: 10,
                    primaryRange: 1f,
                    indirectRange: 3f,
                    spottingRange: 3f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPAAA);

                // Set short name for UI display
                profile.SetShortName("ZSU-23-4");

                // Set turn availability - entered service in 1965
                profile.SetTurnAvailable(324);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPAAA_ZSU23] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSpaaa_Zsu23Profile), e);
                throw;
            }
        }

        /// <summary>
        /// 2K22 "Tunguska" combined gun/missile air defense system.
        /// </summary>
        private static void CreateSpaaa_2K22Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "2K22 Tunguska Self-Propelled Air Defense System",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPAAA_2K22,
                    120,
                    landHardAttack: 6,
                    landHardDefense: 6,
                    landSoftAttack: 10,
                    landSoftDefense: 8,
                    landAirAttack: 3,
                    landAirDefense: 5,
                    airAttack: 16,
                    airDefense: 13,
                    primaryRange: 1f,
                    indirectRange: 4f,
                    spottingRange: 4f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPAAA);

                // Set short name for UI display
                profile.SetShortName("2K22");

                // Set turn availability - entered service in 1982
                profile.SetTurnAvailable(528);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPAAA_2K22] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSpaaa_2K22Profile), e);
                throw;
            }
        }

        /// <summary>
        /// 9K31 "Strela-1" (SA-9) mobile short-range SAM system.
        /// </summary>
        private static void CreateSpsam_9K31Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "9K31 Strela-1 Self-Propelled SAM System",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SPSAM_9K31,
                    40,
                    landHardAttack: 2,
                    landHardDefense: 3,
                    landSoftAttack: 4,
                    landSoftDefense: 4,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    airAttack: 10,
                    airDefense: 10,
                    primaryRange: 1f,
                    indirectRange: 3f,
                    spottingRange: 3f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirDrop,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SPSAM);

                // Set amphibious capability (BRDM-2 chassis)
                profile.SetAmphibiousCapability(true);

                // Set short name for UI display
                profile.SetShortName("9K31");

                // Set turn availability - entered service in 1968
                profile.SetTurnAvailable(360);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SPSAM_9K31] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSpsam_9K31Profile), e);
                throw;
            }
        }

        /// <summary>
        /// S-75 "Dvina" (SA-2) medium-range strategic SAM system.
        /// </summary>
        private static void CreateSam_S75Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "S-75 Dvina Strategic SAM System",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SAM_S75,
                    80,
                    landHardAttack: 0,
                    landHardDefense: 2,
                    landSoftAttack: 0,
                    landSoftDefense: 3,
                    landAirAttack: 0,
                    landAirDefense: 1,
                    airAttack: 12,
                    airDefense: 9,
                    primaryRange: 1f,
                    indirectRange: 5f,
                    spottingRange: 5f,
                    movementModifier: 0f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SAM);

                // Set short name for UI display
                profile.SetShortName("S-75");

                // Set turn availability - entered service in 1957
                profile.SetTurnAvailable(228);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SAM_S75] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSam_S75Profile), e);
                throw;
            }
        }

        /// <summary>
        /// S-125 "Neva" (SA-3) low-altitude strategic SAM system.
        /// </summary>
        private static void CreateSam_S125Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "S-125 Neva Strategic SAM System",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SAM_S125,
                    90,
                    landHardAttack: 0,
                    landHardDefense: 3,
                    landSoftAttack: 0,
                    landSoftDefense: 4,
                    landAirAttack: 0,
                    landAirDefense: 2,
                    airAttack: 14,
                    airDefense: 11,
                    primaryRange: 1f,
                    indirectRange: 6f,
                    spottingRange: 6f,
                    movementModifier: 0f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SAM);

                // Set short name for UI display
                profile.SetShortName("S-125");

                // Set turn availability - entered service in 1961
                profile.SetTurnAvailable(276);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SAM_S125] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSam_S125Profile), e);
                throw;
            }
        }

        /// <summary>
        /// S-300 "Favorit" (SA-10/20) advanced long-range strategic SAM system.
        /// </summary>
        private static void CreateSam_S300Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "S-300 Strategic SAM System",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.SAM_S300,
                    200,
                    landHardAttack: 0,
                    landHardDefense: 5,
                    landSoftAttack: 0,
                    landSoftDefense: 6,
                    landAirAttack: 0,
                    landAirDefense: 4,
                    airAttack: 20,
                    airDefense: 15,
                    primaryRange: 1f,
                    indirectRange: 8f,
                    spottingRange: 8f,
                    movementModifier: 0f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Heavy,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.SAM);

                // Set short name for UI display
                profile.SetShortName("S-300");

                // Set turn availability - entered service in 1978
                profile.SetTurnAvailable(480);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.SAM_S300] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateSam_S300Profile), e);
                throw;
            }
        }

        #endregion // Soviet Air Defense


        #region Soviet Helicopters

        /// <summary>
        /// Mi-8AT "Hip-C" armed transport helicopter with anti-tank missiles.
        /// </summary>
        private static void CreateHel_Mi8ATProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Mi-8AT Hip-C Attack Helicopter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.HEL_MI8AT,
                    55,
                    landHardAttack: 8,
                    landHardDefense: 4,
                    landSoftAttack: 7,
                    landSoftDefense: 4,
                    landAirAttack: 1,
                    landAirDefense: 2,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Attack);

                // Set short name for UI display
                profile.SetShortName("Mi-8AT");

                // Set turn availability - entered service in 1975
                profile.SetTurnAvailable(444);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.HEL_MI8AT] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateHel_Mi8ATProfile), e);
                throw;
            }
        }

        /// <summary>
        /// Mi-24D "Hind-D" attack helicopter with rockets and anti-tank missiles.
        /// </summary>
        private static void CreateHel_Mi24DProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Mi-24D Hind-D Attack Helicopter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.HEL_MI24D,
                    85,
                    landHardAttack: 10,
                    landHardDefense: 7,
                    landSoftAttack: 12,
                    landSoftDefense: 8,
                    landAirAttack: 2,
                    landAirDefense: 4,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 3f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Attack);

                // Set short name for UI display
                profile.SetShortName("Mi-24D");

                // Set turn availability - entered service in 1973
                profile.SetTurnAvailable(420);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.HEL_MI24D] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateHel_Mi24DProfile), e);
                throw;
            }
        }

        /// <summary>
        /// Mi-24V "Hind-E" improved attack helicopter with better sensors and weapons.
        /// </summary>
        private static void CreateHel_Mi24VProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Mi-24V Hind-E Attack Helicopter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.HEL_MI24V,
                    80,
                    landHardAttack: 14,
                    landHardDefense: 7,
                    landSoftAttack: 13,
                    landSoftDefense: 8,
                    landAirAttack: 3,
                    landAirDefense: 5,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 3f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Night,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Attack);

                // Set short name for UI display
                profile.SetShortName("Mi-24V");

                // Set turn availability - entered service in 1976
                profile.SetTurnAvailable(456);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.HEL_MI24V] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateHel_Mi24VProfile), e);
                throw;
            }
        }

        /// <summary>
        /// Mi-28 "Havoc" dedicated attack helicopter with advanced avionics.
        /// </summary>
        private static void CreateHel_Mi28Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Mi-28 Havoc Attack Helicopter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.HEL_MI28,
                    120,
                    landHardAttack: 18,
                    landHardDefense: 8,
                    landSoftAttack: 15,
                    landSoftDefense: 9,
                    landAirAttack: 4,
                    landAirDefense: 7,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 3f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aviation,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Attack);

                // Set short name for UI display
                profile.SetShortName("Mi-28");

                // Set turn availability - entered service in 1987
                profile.SetTurnAvailable(588);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.HEL_MI28] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateHel_Mi28Profile), e);
                throw;
            }
        }

        #endregion


        #region Soviet Aircraft

        /// <summary>
        /// A-50 "Mainstay" airborne early warning and control aircraft.
        /// </summary>
        private static void CreateAwacs_A50Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "A-50 Mainstay AWACS Aircraft",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.AWACS_A50,
                    150,
                    landAirDefense: 15,
                    airDefense: 15,
                    airAvionics: 20,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 15f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Large
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Fighter);

                // Set short name for UI display
                profile.SetShortName("A-50");

                // Set turn availability - entered service in 1984
                profile.SetTurnAvailable(552);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.AWACS_A50] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAwacs_A50Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-21 "Fishbed" lightweight interceptor fighter.
        /// </summary>
        private static void CreateAsf_Mig21Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-21 Fishbed Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ASF_MIG21,
                    35,
                    landAirDefense: 7,
                    airAttack: 10,
                    airDefense: 7,
                    airAvionics: 7,
                    airStrategicAttack: 0,
                    airGroundAttack: 6,
                    airGroundDefense: 4,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 2f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Small
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Fighter);

                // Set short name for UI display
                profile.SetShortName("MiG-21");

                // Set turn availability - entered service in 1959
                profile.SetTurnAvailable(252);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ASF_MIG21] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAsf_Mig21Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-23 "Flogger" variable-geometry wing fighter.
        /// </summary>
        private static void CreateAsf_Mig23Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-23 Flogger Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ASF_MIG23,
                    50,
                    landAirDefense: 9,
                    airAttack: 12,
                    airDefense: 9,
                    airAvionics: 10,
                    airGroundAttack: 8,
                    airGroundDefense: 6,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 4f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Night,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.Aircraft,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Fighter);

                // Set short name for UI display
                profile.SetShortName("MiG-23");

                // Set turn availability - entered service in 1970
                profile.SetTurnAvailable(384);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ASF_MIG23] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAsf_Mig23Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-25 "Foxbat" high-speed interceptor.
        /// </summary>
        private static void CreateAsf_Mig25Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-25 Foxbat Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ASF_MIG25,
                    70,
                    landHardAttack: 1,
                    landHardDefense: 2,
                    landSoftAttack: 2,
                    landSoftDefense: 3,
                    landAirAttack: 1,
                    landAirDefense: 4,
                    airAttack: 18,
                    airDefense: 12,
                    airAvionics: 15,
                    airStrategicAttack: 0,
                    airGroundAttack: 3,
                    airGroundDefense: 8,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 8f,
                    movementModifier: 1.5f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Fighter);

                // Set short name for UI display
                profile.SetShortName("MiG-25");

                // Set turn availability - entered service in 1970
                profile.SetTurnAvailable(384);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ASF_MIG25] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAsf_Mig25Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-29 "Fulcrum" modern air superiority fighter.
        /// </summary>
        private static void CreateAsf_Mig29Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-29 Fulcrum Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ASF_MIG29,
                    85,
                    landHardAttack: 4,
                    landHardDefense: 3,
                    landSoftAttack: 8,
                    landSoftDefense: 4,
                    landAirAttack: 3,
                    landAirDefense: 6,
                    airAttack: 20,
                    airDefense: 15,
                    airAvionics: 18,
                    airStrategicAttack: 0,
                    airGroundAttack: 12,
                    airGroundDefense: 10,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 6f,
                    movementModifier: 1.25f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Fighter);

                // Set short name for UI display
                profile.SetShortName("MiG-29");

                // Set turn availability - entered service in 1983
                profile.SetTurnAvailable(540);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ASF_MIG29] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAsf_Mig29Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-31 "Foxhound" long-range interceptor.
        /// </summary>
        private static void CreateAsf_Mig31Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-31 Foxhound Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ASF_MIG31,
                    100,
                    landHardAttack: 2,
                    landHardDefense: 4,
                    landSoftAttack: 4,
                    landSoftDefense: 5,
                    landAirAttack: 2,
                    landAirDefense: 8,
                    airAttack: 22,
                    airDefense: 18,
                    airAvionics: 22,
                    airStrategicAttack: 0,
                    airGroundAttack: 6,
                    airGroundDefense: 12,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 10f,
                    movementModifier: 1.5f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Large
                );

                // Set double fire capability (multiple long-range missiles)
                profile.SetDoubleFireCapability(true);

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Fighter);

                // Set short name for UI display
                profile.SetShortName("MiG-31");

                // Set turn availability - entered service in 1981
                profile.SetTurnAvailable(516);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ASF_MIG31] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAsf_Mig31Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Su-27 "Flanker" advanced air superiority fighter.
        /// </summary>
        private static void CreateAsf_Su27Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Su-27 Flanker Air Superiority Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ASF_SU27,
                    95,
                    landHardAttack: 4,
                    landHardDefense: 4,
                    landSoftAttack: 8,
                    landSoftDefense: 5,
                    landAirAttack: 3,
                    landAirDefense: 7,
                    airAttack: 23,
                    airDefense: 16,
                    airAvionics: 20,
                    airStrategicAttack: 0,
                    airGroundAttack: 10,
                    airGroundDefense: 12,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 7f,
                    movementModifier: 1.25f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Fighter);

                // Set short name for UI display
                profile.SetShortName("Su-27");

                // Set turn availability - entered service in 1985
                profile.SetTurnAvailable(564);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ASF_SU27] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAsf_Su27Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Su-47 "Berkut" experimental forward-swept wing fighter.
        /// </summary>
        private static void CreateAsf_Su47Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Su-47 Berkut Experimental Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ASF_SU47,
                    200,
                    landHardAttack: 5,
                    landHardDefense: 5,
                    landSoftAttack: 10,
                    landSoftDefense: 6,
                    landAirAttack: 4,
                    landAirDefense: 8,
                    airAttack: 25,
                    airDefense: 20,
                    airAvionics: 25,
                    airStrategicAttack: 0,
                    airGroundAttack: 15,
                    airGroundDefense: 15,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 8f,
                    movementModifier: 1.5f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.Gen3,
                    silhouette: UnitSilhouette.Small
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Fighter);

                // Set short name for UI display
                profile.SetShortName("Su-47");

                // Set turn availability - first flight in 1997 (but prototype development began earlier)
                profile.SetTurnAvailable(708);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ASF_SU47] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAsf_Su47Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-27 "Flogger-D" ground attack variant of MiG-23.
        /// </summary>
        private static void CreateMrf_Mig27Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-27 Flogger-D Multi-Role Fighter",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.MRF_MIG27,
                    55,
                    landHardAttack: 6,
                    landHardDefense: 3,
                    landSoftAttack: 12,
                    landSoftDefense: 4,
                    landAirAttack: 2,
                    landAirDefense: 4,
                    airAttack: 10,
                    airDefense: 8,
                    airAvionics: 10,
                    airStrategicAttack: 0,
                    airGroundAttack: 16,
                    airGroundDefense: 6,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 4f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Attack);

                // Set short name for UI display
                profile.SetShortName("MiG-27");

                // Set turn availability - entered service in 1975
                profile.SetTurnAvailable(444);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.MRF_MIG27] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateMrf_Mig27Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Su-25 "Frogfoot" close air support aircraft.
        /// </summary>
        private static void CreateAtt_Su25Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Su-25 Frogfoot Attack Aircraft",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ATT_SU25,
                    60,
                    landHardAttack: 8,
                    landHardDefense: 6,
                    landSoftAttack: 16,
                    landSoftDefense: 8,
                    landAirAttack: 2,
                    landAirDefense: 4,
                    airAttack: 6,
                    airDefense: 10,
                    airAvionics: 8,
                    airStrategicAttack: 0,
                    airGroundAttack: 20,
                    airGroundDefense: 12,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 3f,
                    movementModifier: 0.75f,
                    allWeatherCapability: AllWeatherRating.Day,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Attack);

                // Set short name for UI display
                profile.SetShortName("Su-25");

                // Set turn availability - entered service in 1981
                profile.SetTurnAvailable(516);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ATT_SU25] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAtt_Su25Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Su-25B "Frogfoot-B" improved close air support aircraft.
        /// </summary>
        private static void CreateAtt_Su25BProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Su-25B Frogfoot-B Attack Aircraft",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.ATT_SU25B,
                    75,
                    landHardAttack: 10,
                    landHardDefense: 7,
                    landSoftAttack: 18,
                    landSoftDefense: 9,
                    landAirAttack: 3,
                    landAirDefense: 5,
                    airAttack: 8,
                    airDefense: 12,
                    airAvionics: 12,
                    airStrategicAttack: 0,
                    airGroundAttack: 22,
                    airGroundDefense: 14,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 4f,
                    movementModifier: 0.75f,
                    allWeatherCapability: AllWeatherRating.Night,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Attack);

                // Set short name for UI display
                profile.SetShortName("Su-25B");

                // Set turn availability - entered service in 1986
                profile.SetTurnAvailable(576);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.ATT_SU25B] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateAtt_Su25BProfile), e);
                throw;
            }
        }

        /// <summary>
        /// Su-24 "Fencer" variable-geometry wing bomber.
        /// </summary>
        private static void CreateBmb_Su24Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Su-24 Fencer Bomber",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.BMB_SU24,
                    80,
                    landHardAttack: 8,
                    landHardDefense: 4,
                    landSoftAttack: 18,
                    landSoftDefense: 6,
                    landAirAttack: 2,
                    landAirDefense: 4,
                    airAttack: 6,
                    airDefense: 8,
                    airAvionics: 15,
                    airStrategicAttack: 20,
                    airGroundAttack: 22,
                    airGroundDefense: 8,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 5f,
                    movementModifier: 1.25f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.Gen1,
                    silhouette: UnitSilhouette.Medium
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Bomber);

                // Set short name for UI display
                profile.SetShortName("Su-24");

                // Set turn availability - entered service in 1974
                profile.SetTurnAvailable(432);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.BMB_SU24] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateBmb_Su24Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Tu-16 "Badger" medium-range bomber.
        /// </summary>
        private static void CreateBmb_Tu16Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Tu-16 Badger Bomber",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.BMB_TU16,
                    70,
                    landHardAttack: 6,
                    landHardDefense: 3,
                    landSoftAttack: 16,
                    landSoftDefense: 5,
                    landAirAttack: 1,
                    landAirDefense: 3,
                    airAttack: 4,
                    airDefense: 6,
                    airAvionics: 6,
                    airStrategicAttack: 18,
                    airGroundAttack: 20,
                    airGroundDefense: 6,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 4f,
                    movementModifier: 1f,
                    allWeatherCapability: AllWeatherRating.Day,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.None,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Bomber);

                // Set short name for UI display
                profile.SetShortName("Tu-16");

                // Set turn availability - entered service in 1954
                profile.SetTurnAvailable(192);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.BMB_TU16] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateBmb_Tu16Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Tu-22 "Blinder" supersonic bomber.
        /// </summary>
        private static void CreateBmb_Tu22Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Tu-22 Blinder Bomber",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.BMB_TU22,
                    90,
                    landHardAttack: 8,
                    landHardDefense: 4,
                    landSoftAttack: 18,
                    landSoftDefense: 6,
                    landAirAttack: 2,
                    landAirDefense: 4,
                    airAttack: 6,
                    airDefense: 8,
                    airAvionics: 10,
                    airStrategicAttack: 22,
                    airGroundAttack: 20,
                    airGroundDefense: 8,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 5f,
                    movementModifier: 1.5f,
                    allWeatherCapability: AllWeatherRating.Night,
                    sigintRating: SIGINT_Rating.UnitLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.None,
                    silhouette: UnitSilhouette.Large
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Bomber);

                // Set short name for UI display
                profile.SetShortName("Tu-22");

                // Set turn availability - entered service in 1962
                profile.SetTurnAvailable(288);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.BMB_TU22] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateBmb_Tu22Profile), e);
                throw;
            }
        }

        /// <summary>
        /// Tu-22M3 "Backfire-C" variable-geometry strategic bomber.
        /// </summary>
        private static void CreateBmb_Tu22M3Profile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "Tu-22M3 Backfire-C Strategic Bomber",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.BMB_TU22M3,
                    120,
                    landHardAttack: 10,
                    landHardDefense: 6,
                    landSoftAttack: 22,
                    landSoftDefense: 8,
                    landAirAttack: 3,
                    landAirDefense: 6,
                    airAttack: 8,
                    airDefense: 12,
                    airAvionics: 18,
                    airStrategicAttack: 25,
                    airGroundAttack: 24,
                    airGroundDefense: 12,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 6f,
                    movementModifier: 1.5f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen2,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Large
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Bomber);

                // Set short name for UI display
                profile.SetShortName("Tu-22M3");

                // Set turn availability - entered service in 1983
                profile.SetTurnAvailable(540);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.BMB_TU22M3] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateBmb_Tu22M3Profile), e);
                throw;
            }
        }

        /// <summary>
        /// MiG-25R "Foxbat-B" reconnaissance variant.
        /// </summary>
        private static void CreateRcna_Mig25RProfile()
        {
            try
            {
                var profile = new WeaponSystemProfile(
                    name: "MiG-25R Foxbat-B Reconnaissance Aircraft",
                    nationality: Nationality.USSR,
                    weaponSystemID: WeaponSystems.RCNA_MIG25R,
                    75,
                    landHardAttack: 0,
                    landHardDefense: 2,
                    landSoftAttack: 0,
                    landSoftDefense: 3,
                    landAirAttack: 1,
                    landAirDefense: 4,
                    airAttack: 8,
                    airDefense: 15,
                    airAvionics: 20,
                    airStrategicAttack: 0,
                    airGroundAttack: 0,
                    airGroundDefense: 12,
                    primaryRange: 1f,
                    indirectRange: 0f,
                    spottingRange: 12f,
                    movementModifier: 1.75f,
                    allWeatherCapability: AllWeatherRating.AllWeather,
                    sigintRating: SIGINT_Rating.SpecializedLevel,
                    nbcRating: NBC_Rating.Gen1,
                    strategicMobility: StrategicMobility.AirMobile,
                    nvgCapability: NVG_Rating.Gen2,
                    silhouette: UnitSilhouette.Small
                );

                // Set upgrade paths for modernization
                profile.AddUpgradeType(UpgradeType.Fighter);

                // Set short name for UI display
                profile.SetShortName("MiG-25R");

                // Set turn availability - entered service in 1970
                profile.SetTurnAvailable(384);

                // Store in master dictionary
                _weaponSystemProfiles[WeaponSystems.RCNA_MIG25R] = profile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateRcna_Mig25RProfile), e);
                throw;
            }
        }

        #endregion // Soviet Aircraft
    }
}