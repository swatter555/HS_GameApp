using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace HammerAndSickle.Models
{
    /*───────────────────────────────────────────────────────────────────────────────
    WeaponSystemProfile  —  combat capability definition and tactical effectiveness
    ────────────────────────────────────────────────────────────────────────────────
    Overview
    ════════
    **WeaponSystemProfile** defines the combat capabilities and tactical effectiveness
    of military units in Hammer & Sickle. Unlike UnitProfile which tracks organizational
    composition, WeaponSystemProfile provides the actual combat values used for battle
    resolution, range calculations, and tactical interactions.

    Each profile represents a specific weapons configuration that units can employ,
    with separate mounted and deployed variants possible for mechanized forces. The
    system supports complex multi-domain warfare through specialized combat ratings
    for different target types and engagement scenarios.

    Major Responsibilities
    ══════════════════════
    • Multi-domain combat rating management
        - Land-based combat: Hard/soft target engagement and defense
        - Air operations: Air-to-air combat and ground-based air defense
        - Air-ground operations: Close air support and strategic bombing
    • Range and detection systems
        - Primary weapon range for direct engagement
        - Indirect fire range for artillery and missile systems
        - Spotting range for reconnaissance and target acquisition
    • Tactical capability modeling
        - All-weather operational ratings for adverse conditions
        - Night vision, NBC protection, and SIGINT capabilities
        - Strategic mobility classifications and unit silhouette
    • Equipment upgrade tracking
        - Multiple simultaneous upgrade types per profile
        - Upgrade validation and compatibility management
    • Profile template system
        - Deep cloning with name/ID override capabilities
        - Shared profile references for consistent unit definitions
        - Comprehensive validation and bounds checking

    Design Highlights
    ═════════════════
    • **Combat Domain Separation**: Distinct attack/defense pairs for different
      warfare domains prevent unrealistic cross-domain effectiveness.
    • **CombatRating Integration**: Paired attack/defense values ensure balanced
      combat mechanics with proper offensive/defensive distinctions.
    • **Comprehensive Validation**: All values clamped to realistic bounds (1-25
      combat, 0-100 range) with automatic correction and error handling.
    • **Flexible Capability System**: Enum-based special capabilities allow for
      complex tactical interactions without hard-coded special cases.
    • **Template Architecture**: Cloning system supports both exact duplication
      and parameterized variants for different unit configurations.

    Public-Method Reference
    ═══════════════════════
      ── Combat Value Management ────────────────────────────────────────────────────
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
      public void SetAirDefense(int value){ Air.SetDefense(value); }

      // AirGround properties.
      public int GetAirGroundAttack() => AirGround.Attack;
      public int GetAirGroundDefense() => AirGround.Defense;
      public void SetAirGroundAttack(int value) { AirGround.SetAttack(value); }
      public void SetAirGroundDefense(int value) { AirGround.SetDefense(value); }

      ── Range and Movement Control ─────────────────────────────────────────────────
      SetPrimaryRange(range)                 Sets primary weapon range (0-100).
      SetIndirectRange(range)                Sets indirect fire range (0-100).
      SetSpottingRange(range)                Sets visual detection range (0-100).
      SetMovementModifier(modifier)          Sets movement speed modifier (0.1-10).

      ── Upgrade System Management ──────────────────────────────────────────────────
      AddUpgradeType(upgradeType)            Adds upgrade if not present.
      RemoveUpgradeType(upgradeType)         Removes specific upgrade type.
      HasUpgradeType(upgradeType)            Checks for upgrade presence.
      GetUpgradeTypes()                      Returns read-only upgrade list.
      ClearUpgradeTypes()                    Removes all upgrades.

      ── Profile Analysis ───────────────────────────────────────────────────────────
      GetTotalCombatValue()                  Sums all combat values for comparison.
      ToString()                             Returns formatted profile summary.

      ── Cloning and Templates ──────────────────────────────────────────────────────
      Clone()                                Creates identical copy.
      Clone(newName)                         Creates copy with new name and ID.

      ── Persistence ────────────────────────────────────────────────────────────────
      GetObjectData(info, context)          ISerializable save implementation.

    Combat Domain Architecture
    ══════════════════════════
    WeaponSystemProfile models five distinct combat domains with specialized ratings:

      **Land Hard Combat** (Armored Warfare)
      • **Attack**: Anti-tank effectiveness vs heavily armored targets
      • **Defense**: Armor protection vs kinetic energy penetrators
      • **Usage**: Tank vs tank, ATGM vs armor, heavy weapons engagement
      • **Examples**: T-80B main gun (22 attack), Challenger-1 armor (20 defense)

      **Land Soft Combat** (Anti-Personnel Operations)
      • **Attack**: Effectiveness vs unarmored personnel and light vehicles
      • **Defense**: Protection vs small arms, artillery fragments, blast
      • **Usage**: Infantry combat, suppression, area denial weapons
      • **Examples**: BMP-2 autocannon (15 attack), entrenchment (12 defense)

      **Land Air Combat** (Ground-Based Air Defense)
      • **Attack**: Surface-to-air missile and AAA effectiveness
      • **Defense**: Protection vs air-to-ground attacks and bombing
      • **Usage**: SAM engagement, AAA coverage, point defense systems
      • **Examples**: S-300 system (25 attack), hardened bunker (18 defense)

      **Air Combat** (Air-to-Air Operations)
      • **Attack**: Air superiority fighter effectiveness vs aircraft
      • **Defense**: Aircraft survivability vs enemy fighters and missiles
      • **Usage**: Fighter vs fighter, interceptor operations, air superiority
      • **Examples**: F-15 Eagle (24 attack), MiG-29 agility (19 defense)

      **Air-Ground Combat** (Close Air Support)
      • **Attack**: Aircraft effectiveness vs ground targets
      • **Defense**: Aircraft survivability vs ground-based threats
      • **Usage**: CAS missions, interdiction, precision strikes
      • **Examples**: A-10 GAU-8 (23 attack), Su-25 armor (16 defense)

    Specialized Combat Values
    ═════════════════════════
      **Air Avionics** (0-25 range)
      • Electronic warfare capability and sensor effectiveness
      • Affects radar detection, jamming resistance, target acquisition
      • Modern fighters: 15-25, older aircraft: 5-12, bombers: 8-18

      **Air Strategic Attack** (0-25 range)  
      • Strategic bombing capability vs infrastructure and industry
      • Represents payload, accuracy, and penetration capability
      • Strategic bombers: 18-25, tactical fighters: 8-15, helicopters: 0-5

    Range and Detection Systems
    ═══════════════════════════
      **Primary Range** (0-100 hexes)
      • Direct fire weapon engagement range
      • Tank main guns: 3-5 hexes, small arms: 1-2 hexes, naval guns: 8-12 hexes
      • Used for direct combat resolution and line-of-sight attacks

      **Indirect Range** (0-100 hexes)
      • Artillery, mortar, and missile system range
      • Field artillery: 8-15 hexes, MLRS: 20-40 hexes, ballistic missiles: 80-100 hexes
      • Allows beyond-visual-range engagement with spotting requirements

      **Spotting Range** (0-100 hexes)
      • Visual and sensor detection range for enemy units
      • Infantry: 2-3 hexes, reconnaissance: 4-6 hexes, AWACS: 20-30 hexes
      • Determines automatic enemy detection without action expenditure

      **Movement Modifier** (0.1-10.0 multiplier)
      • Speed adjustment relative to base movement points
      • Fast units: 1.2-1.5x, standard: 1.0x, slow/damaged: 0.5-0.8x
      • Applied to base movement for final movement point calculation

    Tactical Capability Enumerations
    ════════════════════════════════
      **All-Weather Rating**
      • Day: Fair weather operations only, limited night capability
      • Night: Basic night operations, weather restrictions apply  
      • All-Weather: Full capability in adverse conditions and darkness

      **SIGINT Rating** (Signals Intelligence)
      • Unit Level: Basic communications and local electronic warfare
      • HQ Level: Enhanced signals intelligence and coordination capability
      • Specialized Level: Advanced electronic warfare and signal analysis

      **NBC Rating** (Nuclear, Biological, Chemical Protection)
      • None: No protection vs NBC weapons, full vulnerability
      • Gen1: Basic protection suits and detection equipment
      • Gen2: Advanced NBC systems with filtered air and decontamination

      **Strategic Mobility**
      • Heavy: Road/rail transport only, no air mobility
      • AirDrop: Paratrooper capability, light equipment only
      • AirMobile: Helicopter transport capability, medium equipment
      • AirLift: Strategic airlift capability, heavy equipment possible
      • Amphibious: Naval transport and beach assault capability

      **NVG Rating** (Night Vision Capability)
      • None: Daylight operations only, night penalties apply
      • Gen1: Basic starlight scopes, limited night capability
      • Gen2: Advanced thermal and starlight systems, full night ops

      **Unit Silhouette** (Detection Profile)
      • Tiny: Individual soldiers, small teams, minimal detection signature
      • Small: Light vehicles, small aircraft, reduced visibility
      • Medium: Standard vehicles and aircraft, normal detection
      • Large: Heavy vehicles, large aircraft, increased visibility

    Upgrade System Architecture
    ═══════════════════════════
    WeaponSystemProfile supports multiple simultaneous upgrades from 15 categories:

      **Vehicle Upgrades**
      • AFV: Armored Fighting Vehicle improvements (armor, firepower)
      • IFV: Infantry Fighting Vehicle enhancements (sensors, weapons)
      • APC: Armored Personnel Carrier modifications (protection, mobility)
      • RECON: Reconnaissance vehicle upgrades (sensors, stealth, speed)

      **Artillery Upgrades**  
      • SPART: Self-Propelled Artillery improvements (range, accuracy, mobility)
      • ART: Towed Artillery enhancements (firepower, counter-battery)
      • ROC: Rocket Artillery upgrades (range, payload, reload speed)
      • SSM: Surface-to-Surface Missile improvements (range, accuracy, warhead)

      **Air Defense Upgrades**
      • SAM: Surface-to-Air Missile enhancements (range, guidance, mobility)
      • SPSAM: Self-Propelled SAM improvements (radar, missiles, survivability)
      • AAA: Anti-Aircraft Artillery upgrades (rate of fire, tracking, range)
      • SPAAA: Self-Propelled AAA enhancements (mobility, firepower, sensors)

      **Aviation Upgrades**
      • Fighter: Air superiority improvements (avionics, missiles, maneuverability)
      • Attack: Ground attack enhancements (weapons, armor, targeting systems)
      • Bomber: Strategic bombing upgrades (payload, range, survivability)

    Combat Value Validation
    ═══════════════════════
    **Bounds Checking**
    All combat values automatically clamped to valid ranges:
    ```
    Combat Values: 1-25 (MIN_COMBAT_VALUE to MAX_COMBAT_VALUE)
    Range Values: 0-100 (MIN_RANGE to MAX_RANGE)  
    Movement Modifier: 0.1-10.0 (prevents invalid speed calculations)
    ```

    **Validation Flow**
    • Constructor validation: All parameters checked during profile creation
    • Setter validation: Individual value changes validated and clamped
    • CombatRating delegation: Attack/defense pairs validated by CombatRating class
    • Error handling: Invalid values logged and corrected, exceptions for critical errors

    **Design Rationale**
    • Prevents impossible combat values that would break game balance
    • Ensures mathematical stability in combat resolution algorithms
    • Maintains data integrity across save/load and template operations
    • Provides predictable behavior for AI and player expectations

    Template and Cloning System
    ═══════════════════════════
    **Profile Sharing Architecture**
    WeaponSystemProfile instances are shared references between units of the same type:
    • Base profiles: Master definitions for each weapon system type
    • Unit references: Multiple CombatUnits point to same profile instance
    • Memory efficiency: Thousands of units share dozens of profile templates
    • Consistency: All T-80B tanks use identical combat characteristics

    **Cloning Scenarios**
    • `Clone()`: Template duplication for profile libraries and testing
    • `Clone(newName)`: Custom variants with unique IDs for special units
    • Profile modification: Create variants without affecting base templates
    • Scenario-specific profiles: Modified versions for campaign or mission needs

    **ID Generation Strategy**
    • Base profiles: WeaponSystemID matches WeaponSystems enum name
    • Cloned profiles: Append GUID suffix for uniqueness (T80B_a1b2c3d4)
    • Backward compatibility: Fallback ID generation for legacy save files
    • Reference resolution: Profiles resolved by ID during deserialization

    ───────────────────────────────────────────────────────────────────────────────
    KEEP THIS COMMENT BLOCK IN SYNC WITH COMBAT SYSTEM AND PROFILE CHANGES!
    ───────────────────────────────────────────────────────────────────────────── */
    [Serializable]
    public class WeaponSystemProfile : ISerializable, ICloneable
    {
        #region Constants

        private const string CLASS_NAME = nameof(WeaponSystemProfile);

        #endregion // Constants

        #region Properties

        public string Name { get; private set; }
        public WeaponSystems WeaponSystemID { get; private set; }
        public Nationality Nationality { get; private set; }
        public List<UpgradeType> UpgradeTypes { get; private set; }

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
        /// <param name="weaponSystem">Primary weapon system type</param>
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
            WeaponSystems weaponSystem,
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
                Nationality = nationality;
                WeaponSystemID = weaponSystem;
                UpgradeTypes = new List<UpgradeType>();

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
        /// <param name="weaponSystem">Primary weapon system type</param>
        public WeaponSystemProfile(string name, Nationality nationality, WeaponSystems weaponSystem)
            : this(name, nationality, weaponSystem, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
        {
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected WeaponSystemProfile(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Basic properties
                Name = info.GetString(nameof(Name));
                Nationality = (Nationality)info.GetValue(nameof(Nationality), typeof(Nationality));
                WeaponSystemID = (WeaponSystems)info.GetValue(nameof(WeaponSystemID), typeof(WeaponSystems));

                // Deserialize CombatRating objects
                LandHard = (CombatRating)info.GetValue(nameof(LandHard), typeof(CombatRating));
                LandSoft = (CombatRating)info.GetValue(nameof(LandSoft), typeof(CombatRating));
                LandAir = (CombatRating)info.GetValue(nameof(LandAir), typeof(CombatRating));
                Air = (CombatRating)info.GetValue(nameof(Air), typeof(CombatRating));
                AirGround = (CombatRating)info.GetValue(nameof(AirGround), typeof(CombatRating));

                // Single combat values
                AirAvionics = info.GetInt32(nameof(AirAvionics));
                AirStrategicAttack = info.GetInt32(nameof(AirStrategicAttack));

                // Range and movement
                PrimaryRange = info.GetSingle(nameof(PrimaryRange));
                IndirectRange = info.GetSingle(nameof(IndirectRange));
                SpottingRange = info.GetSingle(nameof(SpottingRange));
                MovementModifier = info.GetSingle(nameof(MovementModifier));

                // Capability enums
                AllWeatherCapability = (AllWeatherRating)info.GetValue(nameof(AllWeatherCapability), typeof(AllWeatherRating));
                SIGINT_Rating = (SIGINT_Rating)info.GetValue(nameof(SIGINT_Rating), typeof(SIGINT_Rating));
                NBC_Rating = (NBC_Rating)info.GetValue(nameof(NBC_Rating), typeof(NBC_Rating));
                StrategicMobility = (StrategicMobility)info.GetValue(nameof(StrategicMobility), typeof(StrategicMobility));
                NVGCapability = (NVG_Rating)info.GetValue(nameof(NVGCapability), typeof(NVG_Rating));
                Silhouette = (UnitSilhouette)info.GetValue(nameof(Silhouette), typeof(UnitSilhouette));

                // Deserialize upgrade types list
                int upgradeCount = info.GetInt32("UpgradeTypesCount");
                UpgradeTypes = new List<UpgradeType>();
                for (int i = 0; i < upgradeCount; i++)
                {
                    UpgradeType upgradeType = (UpgradeType)info.GetValue($"UpgradeType_{i}", typeof(UpgradeType));
                    UpgradeTypes.Add(upgradeType);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion // Constructors


        #region Combat Value Accessors

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
        public void SetAirDefense(int value){ Air.SetDefense(value); }

        // AirGround properties.
        public int GetAirGroundAttack() => AirGround.Attack;
        public int GetAirGroundDefense() => AirGround.Defense;
        public void SetAirGroundAttack(int value) { AirGround.SetAttack(value); }
        public void SetAirGroundDefense(int value) { AirGround.SetDefense(value); }

        #endregion // Combat Value Accessors


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


        #region Public Methods

        /// <summary>
        /// Creates a deep copy of this WeaponSystemProfile.
        /// </summary>
        /// <returns>A new WeaponSystemProfile with identical values</returns>
        public WeaponSystemProfile Clone()
        {
            try
            {
                var clone = new WeaponSystemProfile(
                    Name,
                    Nationality,
                    WeaponSystemID,
                    LandHard.Attack, LandHard.Defense,
                    LandSoft.Attack, LandSoft.Defense,
                    LandAir.Attack, LandAir.Defense,
                    Air.Attack, Air.Defense,
                    AirAvionics,
                    AirGround.Attack, AirGround.Defense,
                    AirStrategicAttack,
                    PrimaryRange, IndirectRange, SpottingRange, MovementModifier,
                    AllWeatherCapability, SIGINT_Rating, NBC_Rating,
                    StrategicMobility, NVGCapability, Silhouette
                );

                // Copy upgrade types
                foreach (var upgradeType in UpgradeTypes)
                {
                    clone.AddUpgradeType(upgradeType);
                }

                return clone;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a deep copy of this WeaponSystemProfile with a new name.
        /// </summary>
        /// <param name="newName">The name for the cloned profile</param>
        /// <returns>A new WeaponSystemProfile with identical values but different name</returns>
        public WeaponSystemProfile Clone(string newName)
        {
            try
            {
                if (string.IsNullOrEmpty(newName))
                    throw new ArgumentException("New name cannot be null or empty", nameof(newName));

                var clone = Clone();

                // Use reflection to set both name and generate new ID
                var nameProperty = typeof(WeaponSystemProfile).GetProperty(nameof(Name));
                var idProperty = typeof(WeaponSystemProfile).GetProperty(nameof(WeaponSystemID));

                nameProperty?.SetValue(clone, newName);
                
                return clone;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

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

        /// <summary>
        /// Returns a string representation of the weapon system profile.
        /// </summary>
        /// <returns>Formatted string with profile details</returns>
        public override string ToString()
        {
            return $"{Name} ({Nationality}) - Total Combat: {GetTotalCombatValue()}";
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

        #endregion // Private Methods


        #region ISerializable Implementation

        /// <summary>
        /// Populates a SerializationInfo object with the data needed to serialize the WeaponSystemProfile.
        /// </summary>
        /// <param name="info">The SerializationInfo object to populate</param>
        /// <param name="context">The StreamingContext structure</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Basic properties
                info.AddValue(nameof(Name), Name);
                info.AddValue(nameof(Nationality), Nationality);
                info.AddValue(nameof(WeaponSystemID), WeaponSystemID);

                // CombatRating objects
                info.AddValue(nameof(LandHard), LandHard);
                info.AddValue(nameof(LandSoft), LandSoft);
                info.AddValue(nameof(LandAir), LandAir);
                info.AddValue(nameof(Air), Air);
                info.AddValue(nameof(AirGround), AirGround);

                // Single combat values
                info.AddValue(nameof(AirAvionics), AirAvionics);
                info.AddValue(nameof(AirStrategicAttack), AirStrategicAttack);

                // Range and movement
                info.AddValue(nameof(PrimaryRange), PrimaryRange);
                info.AddValue(nameof(IndirectRange), IndirectRange);
                info.AddValue(nameof(SpottingRange), SpottingRange);
                info.AddValue(nameof(MovementModifier), MovementModifier);

                // Capability enums
                info.AddValue(nameof(AllWeatherCapability), AllWeatherCapability);
                info.AddValue(nameof(SIGINT_Rating), SIGINT_Rating);
                info.AddValue(nameof(NBC_Rating), NBC_Rating);
                info.AddValue(nameof(StrategicMobility), StrategicMobility);
                info.AddValue(nameof(NVGCapability), NVGCapability);
                info.AddValue(nameof(Silhouette), Silhouette);

                // Serialize upgrade types list
                info.AddValue("UpgradeTypesCount", UpgradeTypes.Count);
                for (int i = 0; i < UpgradeTypes.Count; i++)
                {
                    info.AddValue($"UpgradeType_{i}", UpgradeTypes[i]);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion // ISerializable Implementation


        #region ICloneable Implementation

        /// <summary>
        /// Creates a deep copy of this WeaponSystemProfile.
        /// </summary>
        /// <returns>A new WeaponSystemProfile with identical values</returns>
        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion // ICloneable Implementation
    }
}