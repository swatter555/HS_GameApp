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
    /// 
    /// Methods:
    /// - Constructor: Creates new unit profiles with validation
    /// - SetWeaponSystemValue: Configures maximum values for weapon systems
    /// - UpdateCurrentProfile: Recalculates current strength based on hit points
    /// - Clone: Creates deep copies with optional name/nationality changes
    /// - Serialization: Complete ISerializable implementation for save/load
    /// 
    /// Key Features:
    /// - Tracks both maximum and current weapon system counts
    /// - Automatically scales current values based on unit hit points
    /// - Supports deep cloning with parameter overrides
    /// - Comprehensive validation and error handling
    /// - Efficient dictionary-based storage for weapon systems
    /// </summary>
    [Serializable]
    public class UnitProfile : ISerializable, ICloneable
    {
        #region Constants

        private const string CLASS_NAME = nameof(UnitProfile);

        #endregion // Constants

        #region Properties

        public string Name { get; private set; }
        public Nationality Nationality { get; private set; }

        // Max values used to generate the current profile
        private readonly Dictionary<WeaponSystems, int> maxValues;

        // The current profile, reflecting the paper strength of the unit
        public Dictionary<WeaponSystems, int> CurrentProfile { get; private set; }

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// Creates a new instance of the UnitProfile class with validation.
        /// </summary>
        /// <param name="name">The name of the unit profile</param>
        /// <param name="nationality">The nationality of the unit</param>
        public UnitProfile(string name, Nationality nationality)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("Profile name cannot be null or empty", nameof(name));

                Name = name;
                Nationality = nationality;
                maxValues = new Dictionary<WeaponSystems, int>();
                CurrentProfile = new Dictionary<WeaponSystems, int>();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a new instance of UnitProfile as a copy of an existing profile.
        /// </summary>
        /// <param name="source">The UnitProfile to copy from</param>
        private UnitProfile(UnitProfile source)
        {
            try
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));

                Name = source.Name;
                Nationality = source.Nationality;

                // Deep copy the dictionaries
                maxValues = new Dictionary<WeaponSystems, int>(source.maxValues);
                CurrentProfile = new Dictionary<WeaponSystems, int>(source.CurrentProfile);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "CopyConstructor", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a new instance of UnitProfile as a copy with a new name.
        /// </summary>
        /// <param name="source">The UnitProfile to copy from</param>
        /// <param name="newName">The new name for the profile</param>
        private UnitProfile(UnitProfile source, string newName) : this(source)
        {
            try
            {
                if (string.IsNullOrEmpty(newName))
                    throw new ArgumentException("New name cannot be null or empty", nameof(newName));

                Name = newName;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "CopyWithNameConstructor", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a new instance of UnitProfile as a copy with a new name and nationality.
        /// </summary>
        /// <param name="source">The UnitProfile to copy from</param>
        /// <param name="newName">The new name for the profile</param>
        /// <param name="newNationality">The new nationality for the profile</param>
        private UnitProfile(UnitProfile source, string newName, Nationality newNationality) : this(source, newName)
        {
            try
            {
                Nationality = newNationality;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "CopyWithNameAndNationalityConstructor", e);
                throw;
            }
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

        #endregion // Constructors

        #region Public Methods

        /// <summary>
        /// Sets the maximum value for a specific weapon system in this unit profile.
        /// Creates a new entry if the weapon system doesn't exist in this profile.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to configure</param>
        /// <param name="maxValue">The maximum number of this weapon system in the unit</param>
        public void SetWeaponSystemValue(WeaponSystems weaponSystem, int maxValue)
        {
            try
            {
                if (maxValue < 0)
                    throw new ArgumentException("Max value cannot be negative", nameof(maxValue));

                maxValues[weaponSystem] = maxValue;

                // Update current profile to reflect the change
                UpdateCurrentProfileForWeapon(weaponSystem);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetWeaponSystemValue", e);
                throw;
            }
        }

        /// <summary>
        /// Gets the maximum value for a specific weapon system.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to query</param>
        /// <returns>The maximum value, or 0 if not found</returns>
        public int GetWeaponSystemMaxValue(WeaponSystems weaponSystem)
        {
            try
            {
                return maxValues.TryGetValue(weaponSystem, out int value) ? value : 0;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetWeaponSystemMaxValue", e);
                return 0;
            }
        }

        /// <summary>
        /// Gets the current value for a specific weapon system.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to query</param>
        /// <returns>The current value, or 0 if not found</returns>
        public int GetWeaponSystemCurrentValue(WeaponSystems weaponSystem)
        {
            try
            {
                return CurrentProfile.TryGetValue(weaponSystem, out int value) ? value : 0;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetWeaponSystemCurrentValue", e);
                return 0;
            }
        }

        /// <summary>
        /// Removes a weapon system from this profile entirely.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to remove</param>
        /// <returns>True if the weapon system was removed, false if it wasn't found</returns>
        public bool RemoveWeaponSystem(WeaponSystems weaponSystem)
        {
            try
            {
                bool removedMax = maxValues.Remove(weaponSystem);
                bool removedCurrent = CurrentProfile.Remove(weaponSystem);
                return removedMax || removedCurrent;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "RemoveWeaponSystem", e);
                return false;
            }
        }

        /// <summary>
        /// Checks if this profile contains a specific weapon system.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to check for</param>
        /// <returns>True if the weapon system is present</returns>
        public bool HasWeaponSystem(WeaponSystems weaponSystem)
        {
            return maxValues.ContainsKey(weaponSystem);
        }

        /// <summary>
        /// Gets all weapon systems in this profile.
        /// </summary>
        /// <returns>Collection of weapon systems</returns>
        public IEnumerable<WeaponSystems> GetWeaponSystems()
        {
            return maxValues.Keys;
        }

        /// <summary>
        /// Gets the total number of weapon systems in this profile.
        /// </summary>
        /// <returns>Count of weapon systems</returns>
        public int GetWeaponSystemCount()
        {
            return maxValues.Count;
        }

        /// <summary>
        /// Updates the current profile based on the current hit points of the unit.
        /// This should be called whenever the unit's hit points change.
        /// </summary>
        /// <param name="currentHitPoints">The current hit points of the unit</param>
        public void UpdateCurrentProfile(int currentHitPoints)
        {
            try
            {
                if (currentHitPoints < 0)
                    currentHitPoints = 0;
                else if (currentHitPoints > CUConstants.MAX_HP)
                    currentHitPoints = CUConstants.MAX_HP;

                // Calculate the current multiplier based on hit points
                float multiplier = (float)currentHitPoints / CUConstants.MAX_HP;

                // Clear and regenerate the current profile
                CurrentProfile.Clear();

                foreach (var kvp in maxValues)
                {
                    int currentValue = (int)Math.Round(kvp.Value * multiplier);
                    CurrentProfile[kvp.Key] = currentValue;
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "UpdateCurrentProfile", e);
                throw;
            }
        }

        /// <summary>
        /// Gets the overall combat effectiveness as a percentage (0.0 to 1.0).
        /// Based on the ratio of current to maximum weapon system values.
        /// </summary>
        /// <returns>Combat effectiveness percentage</returns>
        public float GetCombatEffectiveness()
        {
            try
            {
                if (maxValues.Count == 0)
                    return 0f;

                int totalMax = 0;
                int totalCurrent = 0;

                foreach (var kvp in maxValues)
                {
                    totalMax += kvp.Value;
                    if (CurrentProfile.TryGetValue(kvp.Key, out int currentValue))
                        totalCurrent += currentValue;
                }

                return totalMax > 0 ? (float)totalCurrent / totalMax : 0f;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetCombatEffectiveness", e);
                return 0f;
            }
        }

        /// <summary>
        /// Clears all weapon systems from this profile.
        /// </summary>
        public void Clear()
        {
            try
            {
                maxValues.Clear();
                CurrentProfile.Clear();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Clear", e);
            }
        }

        /// <summary>
        /// Creates a deep copy of this UnitProfile.
        /// </summary>
        /// <returns>A new UnitProfile with identical values</returns>
        public UnitProfile Clone()
        {
            try
            {
                return new UnitProfile(this);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a deep copy of this UnitProfile with a new name.
        /// </summary>
        /// <param name="newName">The name for the cloned profile</param>
        /// <returns>A new UnitProfile with identical values but a different name</returns>
        public UnitProfile Clone(string newName)
        {
            try
            {
                return new UnitProfile(this, newName);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "CloneWithName", e);
                throw;
            }
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
            try
            {
                return new UnitProfile(this, newName, newNationality);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "CloneWithNameAndNationality", e);
                throw;
            }
        }

        /// <summary>
        /// Returns a string representation of the unit profile.
        /// </summary>
        /// <returns>Formatted string showing profile details</returns>
        public override string ToString()
        {
            return $"{Name} ({Nationality}) - {GetWeaponSystemCount()} weapon systems, {GetCombatEffectiveness():P0} effective";
        }

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Updates the current profile for a specific weapon system based on the last known multiplier.
        /// This is called when a weapon system max value is changed.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to update</param>
        private void UpdateCurrentProfileForWeapon(WeaponSystems weaponSystem)
        {
            try
            {
                if (!maxValues.TryGetValue(weaponSystem, out int maxValue))
                    return;

                // Calculate multiplier from existing data if possible
                float multiplier = 1.0f;
                if (maxValues.Count > 1)
                {
                    // Find another weapon system to calculate current multiplier
                    foreach (var kvp in maxValues)
                    {
                        if (kvp.Key != weaponSystem && kvp.Value > 0 &&
                            CurrentProfile.TryGetValue(kvp.Key, out int currentValue))
                        {
                            multiplier = (float)currentValue / kvp.Value;
                            break;
                        }
                    }
                }

                // Update the current value for this weapon system
                int newCurrentValue = (int)Math.Round(maxValue * multiplier);
                CurrentProfile[weaponSystem] = newCurrentValue;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "UpdateCurrentProfileForWeapon", e);
            }
        }

        #endregion // Private Methods

        #region ISerializable Implementation

        /// <summary>
        /// Serializes this UnitProfile instance.
        /// </summary>
        /// <param name="info">The SerializationInfo object to populate</param>
        /// <param name="context">The StreamingContext structure</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Store basic properties
                info.AddValue(nameof(Name), Name);
                info.AddValue(nameof(Nationality), Nationality);

                // Store dictionaries count
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

        #endregion // ISerializable Implementation

        #region ICloneable Implementation

        /// <summary>
        /// Creates a deep copy of this UnitProfile.
        /// </summary>
        /// <returns>A new UnitProfile with identical values</returns>
        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion // ICloneable Implementation
    }
}