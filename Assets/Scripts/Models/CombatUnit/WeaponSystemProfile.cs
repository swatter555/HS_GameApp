using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Models
{
 /*──────────────────────────────────────────────────────────────────────────
 WeaponSystemProfile ─ immutable unit template defining combat capabilities
 ────────────────────────────────────────────────────────────────────────────
 
 Summary
 ═══════
 • Encapsulates **all** combat-relevant characteristics for one **WeaponSystems** 
   enum value: separate ground and air combat ratings, movement points, ranges, 
   and special capability flags.
 
 • A **profile** is created once during data-load and referenced by potentially 
   hundreds of **CombatUnit** instances, massively reducing per-unit memory 
   footprint while ensuring consistent unit capabilities.
 
 • All public mutators validate their input and route failures through 
   **AppService.HandleException** to guarantee the object can never persist in an 
   invalid state.

 • **Architecture Change**: This implementation uses separate ground and air 
   combat properties instead of the paired attack/defense CombatRating objects 
   described in the original design documents.

 Public properties
 ═════════════════
 
 // Core identity & metadata
 string Name { get; private set; }
 string ShortName { get; private set; }              // UI-friendly abbreviation
 WeaponSystems WeaponSystemID { get; private set; }
 Nationality Nationality { get; private set; }
 List<UpgradeType> UpgradeTypes { get; private set; }
 int TurnAvailable { get; private set; }             // Campaign availability turn
 int PrestigeCost { get; private set; }              // Purchase cost in prestige points
 
 // Special capabilities
 bool IsAmphibious { get; private set; }             // River crossing ability
 bool IsDoubleFire { get; private set; }             // MLRS double-attack capability
 
 // Ground combat ratings (separate values, not paired)
 int HardAttack { get; private set; }                // Anti-armor effectiveness
 int HardDefense { get; private set; }               // Armor protection
 int SoftAttack { get; private set; }                // Anti-infantry effectiveness  
 int SoftDefense { get; private set; }               // Infantry protection
 int GroundAirAttack { get; private set; }           // Surface-to-air capability
 int GroundAirDefense { get; private set; }          // Air defense protection
 
 // Air unit combat ratings
 int Dogfighting { get; private set; }               // Air-to-air combat skill
 int Maneuverability { get; private set; }           // Aircraft agility
 int TopSpeed { get; private set; }                  // Maximum velocity
 int Survivability { get; private set; }             // Damage resistance
 int GroundAttack { get; private set; }              // Air-to-ground striking power
 int OrdinanceLoad { get; private set; }             // Payload capacity
 int Stealth { get; private set; }                   // Detection avoidance
 
 // Range and detection
 float PrimaryRange { get; private set; }            // Direct engagement range
 float IndirectRange { get; private set; }           // Artillery/SAM range
 float SpottingRange { get; private set; }           // Visual detection range
 
 // Movement and mobility
 int MovementPoints { get; private set; }            // Base movement allowance
 
 // Environmental and tactical capabilities
 AllWeatherRating AllWeatherCapability { get; private set; }  // Day/night/all-weather ops
 SIGINT_Rating SIGINT_Rating { get; private set; }            // Signals intelligence level
 NBC_Rating NBC_Rating { get; private set; }                  // Chemical protection
 StrategicMobility StrategicMobility { get; private set; }    // Transport requirements
 NVG_Rating NVGCapability { get; private set; }               // Night vision equipment
 UnitSilhouette Silhouette { get; private set; }              // Detection profile

 Constructors
 ════════════
 public WeaponSystemProfile(string name, Nationality nationality, 
     WeaponSystems weaponSystemID, int prestigeCost = 0,
     int hardAttack = 0, int hardDefense = 0, int softAttack = 0, 
     int softDefense = 0, int groundAirAttack = 0, int groundAirDefense = 0,
     int dogfighting = 0, int maneuverability = 0, int topSpeed = 0, 
     int survivability = 0, int groundAttack = 0, int ordinanceLoad = 0,
     int stealth = 0, int movementPoints = 0, float primaryRange = 0f, 
     float indirectRange = 0f, float spottingRange = 0f,
     AllWeatherRating allWeatherCapability = AllWeatherRating.Day,
     SIGINT_Rating sigintRating = SIGINT_Rating.UnitLevel,
     NBC_Rating nbcRating = NBC_Rating.None,
     StrategicMobility strategicMobility = StrategicMobility.Heavy,
     NVG_Rating nvgCapability = NVG_Rating.None,
     UnitSilhouette silhouette = UnitSilhouette.Medium)

 Public method signatures ⇢ brief purpose
 ═══════════════════════════════════════
 
 ─ Combat rating mutators ─
 void SetHardAttack(int value)              // Anti-armor attack rating
 void SetHardDefense(int value)             // Armor protection rating
 void SetSoftAttack(int value)              // Anti-infantry attack rating
 void SetSoftDefense(int value)             // Infantry protection rating
 void SetGroundAirAttack(int value)         // Surface-to-air attack rating
 void SetGroundAirDefense(int value)        // Air defense rating
 void SetDogfighting(int value)             // Air-to-air combat rating
 void SetManeuverability(int value)         // Aircraft agility rating
 void SetTopSpeed(int value)                // Maximum speed rating
 void SetSurvivability(int value)           // Aircraft damage resistance
 void SetGroundAttack(int value)            // Air-to-ground attack rating
 void SetOrdinanceLoad(int value)           // Payload capacity rating
 void SetStealth(int value)                 // Stealth/detection avoidance
 
 ─ Special capabilities ─
 void SetAmphibiousCapability(bool value)   // Toggle river crossing ability
 void SetDoubleFireCapability(bool value)   // Toggle MLRS double-attack
 
 ─ Range and movement ─
 void SetPrimaryRange(float range)          // Direct engagement range
 void SetIndirectRange(float range)         // Artillery/missile range
 void SetSpottingRange(float range)         // Visual detection range
 void SetMovementPoints(int points)         // Base movement allowance
 
 ─ Metadata and economy ─
 void SetShortName(string shortName)        // UI abbreviation (≤8 chars)
 void SetTurnAvailable(int turn)            // Campaign availability turn
 void SetPrestigeCost(int cost)             // Purchase cost in prestige
 
 ─ Upgrade management ─
 bool AddUpgradeType(UpgradeType type)      // Add upgrade path if unique
 bool RemoveUpgradeType(UpgradeType type)   // Remove upgrade path
 bool HasUpgradeType(UpgradeType type)      // Check upgrade availability
 IReadOnlyList<UpgradeType> GetUpgradeTypes() // Get all upgrade paths
 void ClearUpgradeTypes()                   // Remove all upgrade paths

 Private helpers
 ═══════════════
 int ValidateCombatValue(int value)         // Clamp to MIN/MAX_COMBAT_VALUE
 float ValidateRange(float value)           // Clamp to MIN/MAX_RANGE  
 int ValidatePrestigeCost(int cost)         // Validate economic balance

 Developer notes
 ═══════════════
 • **Combat Rating Architecture**: This implementation uses separate integer 
   properties for each combat aspect rather than CombatRating objects with 
   attack/defense pairs. Ground units use Hard/Soft Attack/Defense plus 
   GroundAirAttack/Defense, while air units use specialized air combat ratings.

 • **Movement Points vs Modifiers**: Uses absolute MovementPoints rather than 
   movement modifiers, providing direct movement allowance values instead of 
   percentage adjustments to base movement.

 • **ShortName Usage**: Intended for HUD counters and map labels; keep ≤8 chars 
   to avoid UI overflow. Used for compact unit identification in tactical displays.

 • **TurnAvailable System**: Represents the first campaign turn the weapon system 
   may be purchased; scenario scripts can override for prototypes or lend-lease 
   equipment availability.

 • **IsDoubleFire Capability**: Marks systems with innate second attack per combat 
   round (e.g. MLRS systems). CombatUnit decides whether the bonus is consumed; 
   the profile merely flags eligibility.

 • **Upgrade Path Management**: UpgradeTypes list defines valid unit progression 
   paths for the prestige economy. **Not thread-safe**; wrap mutations in locks 
   when accessed from async AI threads.

 • **Validation Strategy**: All mutators funnel exceptions through 
   **AppService.HandleException(CLASS_NAME, method, e)** before re-throwing when 
   state corruption is possible. CUConstants define global min/max bounds.

 • **Template Pattern Benefits**: Every CombatUnit stores only a WeaponSystems 
   enum; the heavy profile object lives here exactly once, reducing per-unit RAM 
   usage and ensuring global consistency for balance changes.

 ────────────────────────────────────────────────────────────────────────────*/
    public class WeaponSystemProfile
    {
        #region Constants

        private const string CLASS_NAME = nameof(WeaponSystemProfile);

        #endregion // Constants


        #region Properties

        public string Name { get; private set; }                   = "Default";
        public string ShortName { get; private set; }              = "Default";
        public WeaponSystems WeaponSystemID { get; private set; }  = WeaponSystems.DEFAULT;
        public Nationality Nationality { get; private set; }       = Nationality.USSR;
        public List<UpgradeType> UpgradeTypes { get; private set; } = new List<UpgradeType>();
        public int TurnAvailable { get; private set; } = 0;      // The campaign turn this is available.

        public int PrestigeCost { get; private set; }  = 0;      // Prestige cost for purchasing this unit type
        public bool IsAmphibious { get; private set; } = false;  // Whether this unit can cross rivers easily
        public bool IsDoubleFire { get; private set; } = false;  // MLRS units fire twice per attack.

        // Properties for ground units
        public int HardAttack { get; private set; }       = 0;
        public int HardDefense { get; private set; }      = 0;
        public int SoftAttack { get; private set; }       = 0;
        public int SoftDefense { get; private set; }      = 0;
        public int GroundAirAttack { get; private set; }  = 0;
        public int GroundAirDefense { get; private set; } = 0;

        // Properties for air units
        public int Dogfighting { get; private set; }     = 0;
        public int Maneuverability { get; private set; } = 0;
        public int TopSpeed { get; private set; }        = 0;
        public int Survivability { get; private set; }   = 0;
        public int GroundAttack { get; private set; }    = 0;
        public int OrdinanceLoad { get; private set; }   = 0;
        public int Stealth { get; private set; }         = 0;

        // Range and movement properties
        public float PrimaryRange { get; private set; }  = 0;
        public float IndirectRange { get; private set; } = 0;
        public float SpottingRange { get; private set; } = 0;

        // Movement points
        public int MovementPoints { get; private set; } = 0;       // Movement points for this WeaponSystemProfile

        // Capability enums
        public AllWeatherRating AllWeatherCapability { get; private set; } = AllWeatherRating.AllWeather;
        public SIGINT_Rating SIGINT_Rating { get; private set; }           = SIGINT_Rating.UnitLevel;
        public NBC_Rating NBC_Rating { get; private set; }                 = NBC_Rating.None;
        public StrategicMobility StrategicMobility { get; private set; }   = StrategicMobility.Heavy;
        public NVG_Rating NVGCapability { get; private set; }              = NVG_Rating.None;
        public UnitSilhouette Silhouette { get; private set; }             = UnitSilhouette.Medium;

        #endregion // Properties


        #region Constructors

        public WeaponSystemProfile(
            string name,
            Nationality nationality,
            WeaponSystems weaponSystemID,
            int prestigeCost = 0,
            int hardAttack = 0, int hardDefense = 0,
            int softAttack = 0, int softDefense = 0,
            int groundAirAttack = 0, int groundAirDefense = 0,
            int dogfighting = 0, int maneuverability = 0,
            int topSpeed = 0, int survivability = 0,
            int groundAttack = 0, int ordinanceLoad = 0,
            int stealth = 0, int movementPoints = 0,
            float primaryRange = 0f, 
            float indirectRange = 0f,
            float spottingRange = 0f,
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
                Nationality = nationality;
                WeaponSystemID = weaponSystemID;
                PrestigeCost = ValidatePrestigeCost(prestigeCost);
                IsAmphibious = false;
                IsDoubleFire = false;
                TurnAvailable = 1; // Default to turn 1; can be modified later

                HardAttack = ValidateCombatValue(hardAttack);
                HardDefense = ValidateCombatValue(hardDefense);
                SoftAttack = ValidateCombatValue(softAttack);
                SoftDefense = ValidateCombatValue(softDefense);
                GroundAirAttack = ValidateCombatValue(groundAirAttack);
                GroundAirDefense = ValidateCombatValue(groundAirDefense);
                Dogfighting = ValidateCombatValue(dogfighting);
                Maneuverability = ValidateCombatValue(maneuverability);
                TopSpeed = ValidateCombatValue(topSpeed);
                Survivability = ValidateCombatValue(survivability);
                GroundAttack = ValidateCombatValue(groundAttack);
                OrdinanceLoad = ValidateCombatValue(ordinanceLoad);
                Stealth = ValidateCombatValue(stealth);

                // Ensure movement points are non-negative
                MovementPoints = Mathf.Max(0, movementPoints);

                // Set and validate ranges
                PrimaryRange = ValidateRange(primaryRange);
                IndirectRange = ValidateRange(indirectRange);
                SpottingRange = ValidateRange(spottingRange);

                // Set movement points
                MovementPoints = Mathf.Max(0, movementPoints); // Ensure non-negative

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

        #endregion // Constructors


        #region Accessors

        // Single-value combat ratings.
        public void SetAmphibiousCapability(bool value) { IsAmphibious = value; }
        public void SetDoubleFireCapability(bool value) { IsDoubleFire = value; }
        public void SetHardAttack(int value) { HardAttack = ValidateCombatValue(value); }
        public void SetHardDefense(int value) { HardDefense = ValidateCombatValue(value); }
        public void SetSoftAttack(int value) { SoftAttack = ValidateCombatValue(value); }
        public void SetSoftDefense(int value) { SoftDefense = ValidateCombatValue(value); }
        public void SetGroundAirAttack(int value) { GroundAirAttack = ValidateCombatValue(value); }
        public void SetGroundAirDefense(int value) { GroundAirDefense = ValidateCombatValue(value); }
        public void SetDogfighting(int value) { Dogfighting = ValidateCombatValue(value); }
        public void SetManeuverability(int value) { Maneuverability = ValidateCombatValue(value); }
        public void SetTopSpeed(int value) { TopSpeed = ValidateCombatValue(value); }
        public void SetSurvivability(int value) { Survivability = ValidateCombatValue(value); }
        public void SetGroundAttack(int value) { GroundAttack = ValidateCombatValue(value); }
        public void SetOrdinanceLoad(int value) { OrdinanceLoad = ValidateCombatValue(value); }
        public void SetStealth(int value) { Stealth = ValidateCombatValue(value); }

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

        /// <summary>
        /// Set this profile's movement points.
        /// </summary>
        /// <param name="points"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetMovementPoints(int points)
        {
            if (points < 0)
                throw new ArgumentOutOfRangeException(nameof(points), "Movement points cannot be negative");
            MovementPoints = points;
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