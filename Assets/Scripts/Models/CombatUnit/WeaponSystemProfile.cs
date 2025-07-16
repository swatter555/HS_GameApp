using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Models
{
    /*───────────────────────────────────────────────────────────────────────────────
      WeaponSystemProfile ─ immutable "stat card" for any vehicle / aircraft / gun
    ───────────────────────────────────────────────────────────────────────────────
     Summary
     ═══════
     • Encapsulates **all** combat‑relevant characteristics for one
       **WeaponSystems** enum value: paired attack/defence ratings, single‑value air
       ratings, ranges, mobility modifiers, sensor suites and special capability
       flags.
     • A **profile** is created once during data‑load and referenced by potentially
       hundreds of **CombatUnit** instances, massively reducing per‑unit memory
       footprint.
     • All public mutators validate their input and route failures through
       `AppService.HandleException` to guarantee the object can never persist in an
       invalid state.

     Public properties
     ═════════════════
       string              Name                 { get; private set; }
       string              ShortName            { get; private set; }   // UI‑friendly label
       WeaponSystems       WeaponSystemID       { get; private set; }
       Nationality         Nationality          { get; private set; }
       List<UpgradeType>   UpgradeTypes         { get; private set; }
       int                 TurnAvailable        { get; private set; }   // Campaign tech‑date
       int                 PrestigeCost         { get; private set; }
       bool                IsAmphibious         { get; private set; }
       bool                IsDoubleFire         { get; private set; }   // 2× attack/turn flag

       // Paired combat ratings
       CombatRating        LandHard             { get; private set; }
       CombatRating        LandSoft             { get; private set; }
       CombatRating        LandAir              { get; private set; }
       CombatRating        Air                  { get; private set; }
       CombatRating        AirGround            { get; private set; }

       // Single‑value aerial ratings
       int                 AirAvionics          { get; private set; }
       int                 AirStrategicAttack  { get; private set; }

       // Ranges & mobility
       float               PrimaryRange         { get; private set; }
       float               IndirectRange        { get; private set; }
       float               SpottingRange        { get; private set; }
       float               MovementModifier     { get; private set; }

       // Capability enums / flags
       AllWeatherRating    AllWeatherCapability { get; private set; }
       SIGINT_Rating       SIGINT_Rating        { get; private set; }
       NBC_Rating          NBC_Rating           { get; private set; }
       StrategicMobility   StrategicMobility    { get; private set; }
       NVG_Rating          NVGCapability        { get; private set; }
       UnitSilhouette      Silhouette           { get; private set; }

     Constructors
     ═════════════
       public WeaponSystemProfile(string name,
                                  Nationality nationality,
                                  WeaponSystems weaponSystemID,
                                  int prestigeCost = 0,
                                  int landHardAttack = 0,  int landHardDefense = 0,
                                  int landSoftAttack = 0,  int landSoftDefense = 0,
                                  int landAirAttack  = 0,  int landAirDefense  = 0,
                                  int airAttack      = 0,  int airDefense      = 0,
                                  int airAvionics = 0,
                                  int airGroundAttack = 0, int airGroundDefense = 0,
                                  int airStrategicAttack = 0,
                                  float primaryRange = 0f, float indirectRange = 0f,
                                  float spottingRange = 0f, float movementModifier = 1f,
                                  AllWeatherRating allWeatherCapability = AllWeatherRating.Day,
                                  SIGINT_Rating sigintRating = SIGINT_Rating.UnitLevel,
                                  NBC_Rating nbcRating = NBC_Rating.None,
                                  StrategicMobility strategicMobility = StrategicMobility.Heavy,
                                  NVG_Rating nvgCapability = NVG_Rating.None,
                                  UnitSilhouette silhouette = UnitSilhouette.Medium)

       public WeaponSystemProfile(string name,
                                  Nationality nationality,
                                  WeaponSystems weaponSystemID,
                                  int prestigeCost = 0)            // delegates to main ctor

     Public API (method signatures ⇢ purpose)
     ═════════════════════════════════════════════
     ― Combat‑rating accessors ―
       int  GetLandHardAttack()            // read Vs hard land
       int  GetLandHardDefense()
       void SetLandHardAttack(int v)       // validate & set
       void SetLandHardDefense(int v)

       int  GetLandSoftAttack()
       int  GetLandSoftDefense()
       void SetLandSoftAttack(int v)
       void SetLandSoftDefense(int v)

       int  GetLandAirAttack()
       int  GetLandAirDefense()
       void SetLandAirAttack(int v)
       void SetLandAirDefense(int v)

       int  GetAirAttack()
       int  GetAirDefense()
       void SetAirAttack(int v)
       void SetAirDefense(int v)

       int  GetAirGroundAttack()
       int  GetAirGroundDefense()
       void SetAirGroundAttack(int v)
       void SetAirGroundDefense(int v)

     ― Upgrade management ―
       bool                      AddUpgradeType(UpgradeType t)      // append if unique & ≠ None
       bool                      RemoveUpgradeType(UpgradeType t)
       bool                      HasUpgradeType(UpgradeType t)
       IReadOnlyList<UpgradeType> GetUpgradeTypes()
       void                      ClearUpgradeTypes()

     ― Range & mobility setters ―
       void SetPrimaryRange(float r)
       void SetIndirectRange(float r)
       void SetSpottingRange(float r)
       void SetMovementModifier(float m)

     ― Prestige / capability setters ―
       void SetPrestigeCost(int cost)
       void SetAmphibiousCapability(bool val)      // toggle IsAmphibious
       void SetDoubleFireCapability(bool val)      // toggle IsDoubleFire
       void SetShortName(string shortName)         // UI label (1‑6 chars recommended)
       void SetTurnAvailable(int turn)             // campaign availability tweak

     ― Aggregate metrics ―
       int  GetTotalCombatValue()                  // quick “power score” for AI ranking

     Private helpers
     ═══════════════
       int   ValidateCombatValue(int v)            // clamp to CUConstants
       float ValidateRange(float v)                // clamp to CUConstants
       int   ValidatePrestigeCost(int c)           // economy sanity guard

     Developer notes
     ═══════════════
     • **ShortName** is intended for HUD counters and map labels; keep ≤ 8 chars to
       avoid UI overflow.
     • **TurnAvailable** represents the first campaign turn the weapon system may be
       purchased; scenario scripts can override for early prototypes or lend‑lease.
     • **IsDoubleFire** marks MLRS systems only.
     • **StrategicMobility** Determines how units can be transported by other forms of movement.
     • UpgradeTypes list is **not** thread‑safe; wrap mutations in a *lock* when
       accessed from async AI threads.
     • All mutators funnel exceptions through `AppService.HandleException(CLASS_NAME,
       method, e)` before re‑throwing when state corruption is possible.
     • CUConstants.M* constants define the global min/max bounds for combat values,
       ranges and movement modifiers – tweak those for balance, not individual
       validation logic.

    Notes on Ratings and what they are for:

    // Flags
    IsAmphibious- Marks whether this unit can cross rivers easily.
    IsDoubleFire- Marks whether this unit can fire twice per round, only MLRS systems.
    
    // Paired combat ratings
    CombatRating LandHard ATT/Defense- Attacking and defending against armored vehicles in ground combat.
    CombatRating LandSoft ATT/Defense- Attacking and defending against soft targets like infantry or unarmored vehicles in ground combat.
    CombatRating LandAir ATT/Defense- Attacking and defending against air units from land-based units,used to attack/defend transiting aircraft.
    CombatRating Air ATT/Defense- Air-to-air combat ratings, used for dogfights and intercepts.  
    CombatRating AirGround ATT/Defense- Air-to-ground combat ratings, used for attacking ground targets from the air.

    AirAvionics- Avionics rating for air units, affects targeting and sensor capabilities.
    AirStrategicAttack- Strategic bombing capability, used for long-range attacks on high-value targets.
    
    // Ranges & mobility
    PrimaryRange- The primary weapon range, used for direct fire engagements, ground combat only, usually 1.
    IndirectRange- The range of indirect fire weapons, such as artillery and air defense weapons.
    SpottingRange- The visual spotting range, used for detecting enemy units.
    MovementModifier- A modifier for movement speed, used to adjust how fast units can move across terrain or in the air.
    
    // Capability enums
    AllWeatherRating- Day equals effective only during daylight, Night equals some effectiveness at night, All-Weather equals no penalties.
    SIGINT_Rating- Ability to collect intelligence on enemy units.
    NBC_Rating- Nuclear, Biological, and Chemical protection rating, used to determine how well a unit can operate in contaminated environments.
    StrategicMobility- Indicates what kind of transport can be used to move this unit type.
    NVG_Rating- Night Vision Goggles capability, used to determine how well a ground unit can operate at night, or ground attack aircraft at night.
    UnitSilhouette- Unit silhouette size, used to determine how easily a unit can be spotted by enemy units.
    ────────────────────────────────────────────────────────────────────────────*/
    public class WeaponSystemProfile
    {
        #region Constants

        private const string CLASS_NAME = nameof(WeaponSystemProfile);

        #endregion // Constants


        #region Properties

        public string Name { get; private set; }                // Full descriptive name
        public string ShortName { get; private set; }           // Abbreviated name for smaller fields
        public WeaponSystems WeaponSystemID { get; private set; }
        public Nationality Nationality { get; private set; }
        public List<UpgradeType> UpgradeTypes { get; private set; }
        public int TurnAvailable { get; private set; }          // The campaign turn this is available.
        
        public int PrestigeCost { get; private set; }           // Prestige cost for purchasing this unit type
        public bool IsAmphibious { get; private set; }          // Whether this unit can cross rivers easily
        public bool IsDoubleFire { get; private set; }          // Whether this unit can fire twice per round

        // Combat ratings using CombatRating objects for paired values
        public CombatRating LandHard { get; private set; }      // Hard attack/defense vs land targets
        public CombatRating LandSoft { get; private set; }      // Soft attack/defense vs land targets  
        public CombatRating LandAir { get; private set; }       // Air attack/defense from land units
        public CombatRating Air { get; private set; }           // Air-to-air attack/defense
        public CombatRating AirGround { get; private set; }     // Air-to-ground attack/defense

        // Single-value combat ratings
        public int AirAvionics { get; private set; }            // Avionics rating for air units
        public int AirStrategicAttack { get; private set; }     // Strategic bombing capability

        // Range and movement properties
        public float PrimaryRange { get; private set; }
        public float IndirectRange { get; private set; }
        public float SpottingRange { get; private set; }
        public float MovementModifier { get; private set; }

        // Capability enums
        public AllWeatherRating AllWeatherCapability { get; private set; }
        public SIGINT_Rating SIGINT_Rating { get; private set; }
        public NBC_Rating NBC_Rating { get; private set; }
        public StrategicMobility StrategicMobility { get; private set; }
        public NVG_Rating NVGCapability { get; private set; }
        public UnitSilhouette Silhouette { get; private set; }

        #endregion // Properties


        #region Constructors

        /// <summary>
        /// Creates a new WeaponSystemProfile with the specified parameters.
        /// All combat values are validated and ranges are clamped to valid bounds.
        /// </summary>
        /// <param name="name">Display name of the weapon system profile</param>
        /// <param name="nationality">National affiliation</param>
        /// <param name="weaponSystemID">Primary weapon system type identifier</param>
        /// <param name="prestigeCost">Campaign prestige cost to purchase this unit type</param>
        /// <param name="landHardAttack">Hard attack value vs land targets</param>
        /// <param name="landHardDefense">Hard defense value vs land attacks</param>
        /// <param name="landSoftAttack">Soft attack value vs land targets</param>
        /// <param name="landSoftDefense">Soft defense value vs land attacks</param>
        /// <param name="landAirAttack">Air attack value from land unit</param>
        /// <param name="landAirDefense">Air defense value for land unit</param>
        /// <param name="airAttack">Air-to-air attack value</param>
        /// <param name="airDefense">Air-to-air defense value</param>
        /// <param name="airAvionics">Avionics rating</param>
        /// <param name="airGroundAttack">Air-to-ground attack value</param>
        /// <param name="airGroundDefense">Air-to-ground defense value</param>
        /// <param name="airStrategicAttack">Strategic bombing capability</param>
        /// <param name="primaryRange">Primary weapon range</param>
        /// <param name="indirectRange">Indirect fire range</param>
        /// <param name="spottingRange">Visual spotting range</param>
        /// <param name="movementModifier">Movement speed modifier</param>
        /// <param name="allWeatherCapability">All-weather operational capability</param>
        /// <param name="sigintRating">Signals intelligence rating</param>
        /// <param name="nbcRating">NBC protection rating</param>
        /// <param name="strategicMobility">Strategic mobility type</param>
        /// <param name="nvgCapability">Night vision capability</param>
        /// <param name="silhouette">Unit silhouette size</param>
        public WeaponSystemProfile(
            string name,
            Nationality nationality,
            WeaponSystems weaponSystemID,
            int prestigeCost = 0,
            int landHardAttack = 0, int landHardDefense = 0,
            int landSoftAttack = 0, int landSoftDefense = 0,
            int landAirAttack = 0, int landAirDefense = 0,
            int airAttack = 0, int airDefense = 0,
            int airAvionics = 0,
            int airGroundAttack = 0, int airGroundDefense = 0,
            int airStrategicAttack = 0,
            float primaryRange = 0f, float indirectRange = 0f,
            float spottingRange = 0f, float movementModifier = 1f,
            AllWeatherRating allWeatherCapability = AllWeatherRating.Day,
            SIGINT_Rating sigintRating = SIGINT_Rating.UnitLevel,
            NBC_Rating nbcRating = NBC_Rating.None,
            StrategicMobility strategicMobility = StrategicMobility.Heavy,
            NVG_Rating nvgCapability = NVG_Rating.None,
            UnitSilhouette silhouette = UnitSilhouette.Medium)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("Profile name cannot be null or empty", nameof(name));

                // Set basic properties
                Name = name;
                ShortName = "Default";
                Nationality = nationality;
                WeaponSystemID = weaponSystemID;
                UpgradeTypes = new List<UpgradeType>();
                PrestigeCost = ValidatePrestigeCost(prestigeCost);
                IsAmphibious = false;
                IsDoubleFire = false;
                TurnAvailable = 1; // Default to turn 1; can be modified later

                // Create CombatRating objects with validation
                LandHard = new CombatRating(landHardAttack, landHardDefense);
                LandSoft = new CombatRating(landSoftAttack, landSoftDefense);
                LandAir = new CombatRating(landAirAttack, landAirDefense);
                Air = new CombatRating(airAttack, airDefense);
                AirGround = new CombatRating(airGroundAttack, airGroundDefense);

                // Set and validate single combat values
                AirAvionics = ValidateCombatValue(airAvionics);
                AirStrategicAttack = ValidateCombatValue(airStrategicAttack);

                // Set and validate ranges
                PrimaryRange = ValidateRange(primaryRange);
                IndirectRange = ValidateRange(indirectRange);
                SpottingRange = ValidateRange(spottingRange);
                MovementModifier = Mathf.Clamp(movementModifier, 0.1f, 10f);

                // Set capability enums
                AllWeatherCapability = allWeatherCapability;
                SIGINT_Rating = sigintRating;
                NBC_Rating = nbcRating;
                StrategicMobility = strategicMobility;
                NVGCapability = nvgCapability;
                Silhouette = silhouette;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a basic WeaponSystemProfile with minimal parameters.
        /// All combat values default to zero.
        /// </summary>
        /// <param name="name">Display name of the profile</param>
        /// <param name="nationality">National affiliation</param>
        /// <param name="weaponSystemID">Primary weapon system type identifier</param>
        /// <param name="prestigeCost">Campaign prestige cost to purchase this unit type</param>
        public WeaponSystemProfile(string name, Nationality nationality, WeaponSystems weaponSystemID, int prestigeCost = 0)
            : this(name, nationality, weaponSystemID, prestigeCost, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) 
        { 
            ShortName = "Default";
            IsAmphibious = false;
            IsDoubleFire = false;
            TurnAvailable = 1; // Default to turn 1; can be modified later
        }

        #endregion // Constructors


        #region Accessors

        // LandHard properties.
        public int GetLandHardAttack() => LandHard.Attack;
        public int GetLandHardDefense() => LandHard.Defense;
        public void SetLandHardAttack(int value) { LandHard.SetAttack(value); }
        public void SetLandHardDefense(int value) { LandHard.SetDefense(value); }

        // LandSoft properties.
        public int GetLandSoftAttack() => LandSoft.Attack;
        public int GetLandSoftDefense() => LandSoft.Defense;
        public void SetLandSoftAttack(int value) { LandSoft.SetAttack(value); }
        public void SetLandSoftDefense(int value) { LandSoft.SetDefense(value); }

        // LandAir properties.
        public int GetLandAirAttack() => LandAir.Attack;
        public int GetLandAirDefense() => LandAir.Defense;
        public void SetLandAirAttack(int value) { LandAir.SetAttack(value); }
        public void SetLandAirDefense(int value) { LandAir.SetDefense(value); }

        // Air properties.
        public int GetAirAttack() => Air.Attack;
        public int GetAirDefense() => Air.Defense;
        public void SetAirAttack(int value) { Air.SetAttack(value); }
        public void SetAirDefense(int value) { Air.SetDefense(value); }

        // AirGround properties.
        public int GetAirGroundAttack() => AirGround.Attack;
        public int GetAirGroundDefense() => AirGround.Defense;
        public void SetAirGroundAttack(int value) { AirGround.SetAttack(value); }
        public void SetAirGroundDefense(int value) { AirGround.SetDefense(value); }

        // Single-value combat ratings.
        public void SetAmphibiousCapability(bool value) { IsAmphibious = value; }
        public void SetDoubleFireCapability(bool value) { IsDoubleFire = value; }

        // Short name accessor/mutator
        public void SetShortName(string shortName)
        {
            if (string.IsNullOrEmpty(shortName))
                throw new ArgumentException("Short name cannot be null or empty", nameof(shortName));
            ShortName = shortName;
        }

        // Turn available accessor/mutator
        public void SetTurnAvailable(int turn)
        {
            if (turn < 1)
                throw new ArgumentOutOfRangeException(nameof(turn), "Turn available must be >= 1");
            TurnAvailable = turn;
        }

        #endregion // Accessors


        #region Upgrade Management

        /// <summary>
        /// Adds an upgrade type to this profile if it doesn't already exist.
        /// </summary>
        /// <param name="upgradeType">The upgrade type to add</param>
        /// <returns>True if the upgrade type was added, false if it already existed or was None</returns>
        public bool AddUpgradeType(UpgradeType upgradeType)
        {
            try
            {
                if (upgradeType == UpgradeType.None)
                {
                    return false;
                }

                if (!UpgradeTypes.Contains(upgradeType))
                {
                    UpgradeTypes.Add(upgradeType);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AddUpgradeType", e);
                return false;
            }
        }

        /// <summary>
        /// Removes an upgrade type from this profile.
        /// </summary>
        /// <param name="upgradeType">The upgrade type to remove</param>
        /// <returns>True if the upgrade type was removed, false if it wasn't found</returns>
        public bool RemoveUpgradeType(UpgradeType upgradeType)
        {
            try
            {
                return UpgradeTypes.Remove(upgradeType);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RemoveUpgradeType", e);
                return false;
            }
        }

        /// <summary>
        /// Checks if this profile has a specific upgrade type.
        /// </summary>
        /// <param name="upgradeType">The upgrade type to check for</param>
        /// <returns>True if the upgrade type is present</returns>
        public bool HasUpgradeType(UpgradeType upgradeType)
        {
            try
            {
                return UpgradeTypes.Contains(upgradeType);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "HasUpgradeType", e);
                return false;
            }
        }

        /// <summary>
        /// Gets a read-only copy of the upgrade types list.
        /// </summary>
        /// <returns>Read-only list of upgrade types</returns>
        public IReadOnlyList<UpgradeType> GetUpgradeTypes()
        {
            return UpgradeTypes.AsReadOnly();
        }

        /// <summary>
        /// Clears all upgrade types from this profile.
        /// </summary>
        public void ClearUpgradeTypes()
        {
            try
            {
                UpgradeTypes.Clear();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ClearUpgradeTypes", e);
            }
        }

        #endregion // Upgrade Management


        #region Range and Movement Methods

        /// <summary>
        /// Sets the primary range with validation.
        /// </summary>
        /// <param name="range">The new primary range value</param>
        public void SetPrimaryRange(float range)
        {
            try
            {
                PrimaryRange = ValidateRange(range);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetPrimaryRange", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the indirect range with validation.
        /// </summary>
        /// <param name="range">The new indirect range value</param>
        public void SetIndirectRange(float range)
        {
            try
            {
                IndirectRange = ValidateRange(range);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetIndirectRange", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the spotting range with validation.
        /// </summary>
        /// <param name="range">The new spotting range value</param>
        public void SetSpottingRange(float range)
        {
            try
            {
                SpottingRange = ValidateRange(range);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetSpottingRange", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the movement modifier with validation.
        /// </summary>
        /// <param name="modifier">The new movement modifier (0.1 to 10.0)</param>
        public void SetMovementModifier(float modifier)
        {
            try
            {
                MovementModifier = Mathf.Clamp(modifier, 0.1f, 10f);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetMovementModifier", e);
                throw;
            }
        }

        #endregion // Range and Movement Methods


        #region Prestige Cost Management

        /// <summary>
        /// Sets the prestige cost for purchasing this unit type.
        /// </summary>
        /// <param name="cost">The new prestige cost (must be >= 0)</param>
        public void SetPrestigeCost(int cost)
        {
            try
            {
                PrestigeCost = ValidatePrestigeCost(cost);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetPrestigeCost", e);
                throw;
            }
        }

        #endregion // Prestige Cost Management


        #region Public Methods

        /// <summary>
        /// Gets the total combat effectiveness as a rough estimate.
        /// Sums all attack and defense values for comparison purposes.
        /// </summary>
        /// <returns>Total combat value</returns>
        public int GetTotalCombatValue()
        {
            return LandHard.GetTotalCombatValue() +
                   LandSoft.GetTotalCombatValue() +
                   LandAir.GetTotalCombatValue() +
                   Air.GetTotalCombatValue() +
                   AirGround.GetTotalCombatValue() +
                   AirAvionics +
                   AirStrategicAttack;
        }

        #endregion // Public Methods


        #region Private Methods

        /// <summary>
        /// Validates and clamps a combat value to the allowed range.
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <returns>The clamped value within valid range</returns>
        private int ValidateCombatValue(int value)
        {
            return Mathf.Clamp(value, CUConstants.MIN_COMBAT_VALUE, CUConstants.MAX_COMBAT_VALUE);
        }

        /// <summary>
        /// Validates and clamps a range value to the allowed range.
        /// </summary>
        /// <param name="value">The range value to validate</param>
        /// <returns>The clamped value within valid range</returns>
        private float ValidateRange(float value)
        {
            return Mathf.Clamp(value, CUConstants.MIN_RANGE, CUConstants.MAX_RANGE);
        }

        /// <summary>
        /// Validates prestige cost to ensure it's a reasonable value.
        /// </summary>
        /// <param name="cost">The prestige cost to validate</param>
        /// <returns>The validated prestige cost</returns>
        private int ValidatePrestigeCost(int cost)
        {
            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), "Prestige cost cannot be negative");
            }

            // Clamp to reasonable maximum to prevent economic balance issues
            return Mathf.Clamp(cost, 0, 9999);
        }

        #endregion // Private Methods
    }
}