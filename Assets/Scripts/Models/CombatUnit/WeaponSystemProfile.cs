using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// The WeaponSystemProfile defines the combat capabilities of a unit.
    /// It provides values for offensive and defensive capabilities against different target types,
    /// as well as movement and range parameters. Units can have different profiles when deployed
    /// or mounted on transports.
    /// 
    /// Methods:
    /// - Constructor: Creates a new weapon system profile with specified parameters
    /// - Combat Rating Accessors: Get/set methods for individual combat values
    /// - Upgrade Management: Add/remove/check upgrade types
    /// - Validation: Ensures all values are within valid ranges
    /// - Clone: Creates deep copy of the profile
    /// - Serialization: Complete ISerializable implementation
    /// 
    /// Key Features:
    /// - Uses CombatRating objects for paired attack/defense values
    /// - Validates all combat and range values against constants
    /// - Supports multiple upgrade types per profile
    /// - Comprehensive error handling and logging
    /// </summary>
    [Serializable]
    public class WeaponSystemProfile : ISerializable, ICloneable
    {
        #region Constants

        private const string CLASS_NAME = nameof(WeaponSystemProfile);

        #endregion // Constants

        #region Properties

        public string Name { get; private set; }
        public string WeaponSystemID { get; private set; }
        public Nationality Nationality { get; private set; }
        public WeaponSystems WeaponSystem { get; private set; }
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
                WeaponSystem = weaponSystem;
                WeaponSystemID = WeaponSystem.ToString();
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
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
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
                WeaponSystem = (WeaponSystems)info.GetValue(nameof(WeaponSystem), typeof(WeaponSystems));

                // Handle WeaponSystemID with fallback for backward compatibility
                try
                {
                    WeaponSystemID = info.GetString(nameof(WeaponSystemID));
                }
                catch (SerializationException)
                {
                    // Backward compatibility: if WeaponSystemID not found, generate from WeaponSystem
                    WeaponSystemID = WeaponSystem.ToString();
                }

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
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion // Constructors

        #region Combat Value Accessors

        /// <summary>
        /// Gets the land hard attack value.
        /// </summary>
        public int GetLandHardAttack() => LandHard.Attack;

        /// <summary>
        /// Gets the land hard defense value.
        /// </summary>
        public int GetLandHardDefense() => LandHard.Defense;

        /// <summary>
        /// Sets the land hard attack value with validation.
        /// </summary>
        public void SetLandHardAttack(int value)
        {
            try
            {
                LandHard.SetAttack(value);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetLandHardAttack", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the land hard defense value with validation.
        /// </summary>
        public void SetLandHardDefense(int value)
        {
            try
            {
                LandHard.SetDefense(value);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetLandHardDefense", e);
                throw;
            }
        }

        /// <summary>
        /// Gets the land soft attack value.
        /// </summary>
        public int GetLandSoftAttack() => LandSoft.Attack;

        /// <summary>
        /// Gets the land soft defense value.
        /// </summary>
        public int GetLandSoftDefense() => LandSoft.Defense;

        /// <summary>
        /// Sets the land soft attack value with validation.
        /// </summary>
        public void SetLandSoftAttack(int value)
        {
            try
            {
                LandSoft.SetAttack(value);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetLandSoftAttack", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the land soft defense value with validation.
        /// </summary>
        public void SetLandSoftDefense(int value)
        {
            try
            {
                LandSoft.SetDefense(value);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetLandSoftDefense", e);
                throw;
            }
        }

        /// <summary>
        /// Gets the air attack value for air-to-air combat.
        /// </summary>
        public int GetAirAttack() => Air.Attack;

        /// <summary>
        /// Gets the air defense value for air-to-air combat.
        /// </summary>
        public int GetAirDefense() => Air.Defense;

        /// <summary>
        /// Sets the air attack value with validation.
        /// </summary>
        public void SetAirAttack(int value)
        {
            try
            {
                Air.SetAttack(value);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetAirAttack", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the air defense value with validation.
        /// </summary>
        public void SetAirDefense(int value)
        {
            try
            {
                Air.SetDefense(value);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetAirDefense", e);
                throw;
            }
        }

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
                AppService.Instance.HandleException(CLASS_NAME, "AddUpgradeType", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "RemoveUpgradeType", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "HasUpgradeType", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "ClearUpgradeTypes", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "SetPrimaryRange", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "SetIndirectRange", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "SetSpottingRange", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "SetMovementModifier", e);
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
                    WeaponSystem,
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
                AppService.Instance.HandleException(CLASS_NAME, "Clone", e);
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
                // Use reflection to set the name since it's private set
                typeof(WeaponSystemProfile).GetProperty(nameof(Name)).SetValue(clone, newName);
                return clone;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Clone", e);
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
                info.AddValue(nameof(WeaponSystem), WeaponSystem);
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
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
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