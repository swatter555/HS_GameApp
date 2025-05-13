using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// UnitProfile is a mechanism to define a unit in terms of men, tanks, artillery, etc.,
    /// while the combat values in the WeaponSystemProfile are used to resolve combat. This
    /// mechanism is meant only for informational purposes, displayed in the GUI to the user.
    /// This allows for the tracking of losses during a scenario and/or campaign.
    /// </summary>
    [Serializable]
    public class UnitProfile : ISerializable, ICloneable
    {
        #region Constants
        private const string CLASS_NAME = nameof(UnitProfile);
        #endregion

        #region Properties

        public string Name { get; private set; }
        public Nationality Nationality { get; private set; }

        // Max values used to generate the current profile
        private Dictionary<WeaponSystems, int> maxValues;

        // The current profile, reflecting the paper strength of the unit
        public Dictionary<WeaponSystems, int> CurrentProfile { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of the UnitProfile class.
        /// </summary>
        /// <param name="name">The name of the unit profile</param>
        /// <param name="nationality">The nationality of the unit</param>
        public UnitProfile(string name, Nationality nationality)
        {
            Name = name;
            Nationality = nationality;
            maxValues = new Dictionary<WeaponSystems, int>();
            CurrentProfile = new Dictionary<WeaponSystems, int>();
        }

        /// <summary>
        /// Creates a new instance of UnitProfile as a copy of an existing profile.
        /// </summary>
        /// <param name="source">The UnitProfile to copy from</param>
        private UnitProfile(UnitProfile source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Name = source.Name;
            Nationality = source.Nationality;

            // Deep copy the dictionaries
            maxValues = new Dictionary<WeaponSystems, int>(source.maxValues);
            CurrentProfile = new Dictionary<WeaponSystems, int>(source.CurrentProfile);
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected UnitProfile(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Retrieve basic properties
                Name = info.GetString(nameof(Name));
                Nationality = (Nationality)info.GetValue(nameof(Nationality), typeof(Nationality));

                // Retrieve dictionaries
                // First get the count of elements in each dictionary
                int maxValuesCount = info.GetInt32("MaxValuesCount");
                int currentProfileCount = info.GetInt32("CurrentProfileCount");

                // Initialize dictionaries
                maxValues = new Dictionary<WeaponSystems, int>();
                CurrentProfile = new Dictionary<WeaponSystems, int>();

                // Deserialize maxValues
                for (int i = 0; i < maxValuesCount; i++)
                {
                    WeaponSystems weapon = (WeaponSystems)info.GetValue($"MaxValuesKey_{i}", typeof(WeaponSystems));
                    int value = info.GetInt32($"MaxValuesValue_{i}");
                    maxValues[weapon] = value;
                }

                // Deserialize CurrentProfile
                for (int i = 0; i < currentProfileCount; i++)
                {
                    WeaponSystems weapon = (WeaponSystems)info.GetValue($"CurrentProfileKey_{i}", typeof(WeaponSystems));
                    int value = info.GetInt32($"CurrentProfileValue_{i}");
                    CurrentProfile[weapon] = value;
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the maximum value for a specific weapon system in this unit profile.
        /// Creates a new entry if the weapon system doesn't exist in this profile.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to configure</param>
        /// <param name="maxValue">The maximum number of this weapon system in the unit</param>
        public void SetWeaponSystemValue(WeaponSystems weaponSystem, int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentException("Max value cannot be negative", nameof(maxValue));
            }

            maxValues[weaponSystem] = maxValue;
        }

        /// <summary>
        /// Updates the current profile based on the current hit points of the unit.
        /// This should be called whenever the unit's hit points change.
        /// </summary>
        /// <param name="currentHitPoints">The current hit points of the unit</param>
        public void UpdateCurrentProfile(int currentHitPoints)
        {
            if (currentHitPoints < 0)
            {
                currentHitPoints = 0;
            }
            else if (currentHitPoints > CombatUnitConstants.MaxPossibleHitPoints)
            {
                currentHitPoints = CombatUnitConstants.MaxPossibleHitPoints;
            }

            // Calculate the current multiplier based on hit points
            float multiplier = (float)currentHitPoints / CombatUnitConstants.MaxPossibleHitPoints;

            // Clear and regenerate the current profile
            CurrentProfile.Clear();

            foreach (var weaponSystem in maxValues.Keys)
            {
                int currentValue = (int)Math.Round(maxValues[weaponSystem] * multiplier);
                CurrentProfile[weaponSystem] = currentValue;
            }
        }

        /// <summary>
        /// Creates a deep copy of this UnitProfile.
        /// </summary>
        /// <returns>A new UnitProfile with identical values</returns>
        public UnitProfile Clone()
        {
            return new UnitProfile(this);
        }

        /// <summary>
        /// Creates a deep copy of this UnitProfile with a new name.
        /// </summary>
        /// <param name="newName">The name for the cloned profile</param>
        /// <returns>A new UnitProfile with identical values but a different name</returns>
        public UnitProfile Clone(string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentException("New name cannot be null or empty", nameof(newName));
            }

            var clone = new UnitProfile(this)
            {
                Name = newName
            };
            return clone;
        }

        /// <summary>
        /// Creates a copy of this UnitProfile with a different nationality.
        /// Useful for creating variants of units for different factions.
        /// </summary>
        /// <param name="newName">The name for the cloned profile</param>
        /// <param name="newNationality">The nationality for the cloned profile</param>
        /// <returns>A new UnitProfile with the specified name and nationality</returns>
        public UnitProfile Clone(string newName, Nationality newNationality)
        {
            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentException("New name cannot be null or empty", nameof(newName));
            }

            var clone = new UnitProfile(this)
            {
                Name = newName,
                Nationality = newNationality
            };
            return clone;
        }

        #endregion

        #region ISerializable Implementation

        /// <summary>
        /// Serializes this UnitProfile instance.
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Store basic properties
                info.AddValue(nameof(Name), Name);
                info.AddValue(nameof(Nationality), Nationality);

                // Store dictionaries
                // First store the count of elements in each dictionary
                info.AddValue("MaxValuesCount", maxValues.Count);
                info.AddValue("CurrentProfileCount", CurrentProfile.Count);

                // Serialize maxValues
                int i = 0;
                foreach (var kvp in maxValues)
                {
                    info.AddValue($"MaxValuesKey_{i}", kvp.Key);
                    info.AddValue($"MaxValuesValue_{i}", kvp.Value);
                    i++;
                }

                // Serialize CurrentProfile
                i = 0;
                foreach (var kvp in CurrentProfile)
                {
                    info.AddValue($"CurrentProfileKey_{i}", kvp.Key);
                    info.AddValue($"CurrentProfileValue_{i}", kvp.Value);
                    i++;
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion

        #region ICloneable Implementation

        /// <summary>
        /// Creates a deep copy of this UnitProfile.
        /// </summary>
        /// <returns>A new UnitProfile with identical values</returns>
        object ICloneable.Clone()
        {
            return new UnitProfile(this);
        }

        #endregion
    }
}