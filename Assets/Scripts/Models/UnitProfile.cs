using System;
using System.Collections;
using System.Collections.Generic;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// UnitProfile is a mechanism to define a unit in terms of men, tanks, artillery, etc.,
    /// while the combat values in the WeaponSystemProfile are used to resolve combat. This
    /// mechanism is meant only for informational purposes, displayed in the GUI to the user.
    /// This allows for the tracking of losses during a scenario and/or campaign.
    /// </summary>
    public class UnitProfile
    {
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
            else if (currentHitPoints > CombatUnit.MaxPossibleHitPoints)
            {
                currentHitPoints = CombatUnit.MaxPossibleHitPoints;
            }

            // Calculate the current multiplier based on hit points
            float multiplier = (float)currentHitPoints / CombatUnit.MaxPossibleHitPoints;

            // Clear and regenerate the current profile
            CurrentProfile.Clear();

            foreach (var weaponSystem in maxValues.Keys)
            {
                int currentValue = (int)Math.Round(maxValues[weaponSystem] * multiplier);
                CurrentProfile[weaponSystem] = currentValue;
            }
        }

        #endregion
    }
}