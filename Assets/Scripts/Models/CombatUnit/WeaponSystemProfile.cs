using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace HammerAndSickle.Models
{
    
    public class WeaponSystemProfile
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
        /// <param name="weaponSystemID">Primary weapon system type identifier</param>
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
                WeaponSystemID = weaponSystemID;
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
        /// <param name="weaponSystemID">Primary weapon system type identifier</param>
        public WeaponSystemProfile(string name, Nationality nationality, WeaponSystems weaponSystemID)
            : this(name, nationality, weaponSystemID, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) { }

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

        #endregion // Private Methods
    }
}