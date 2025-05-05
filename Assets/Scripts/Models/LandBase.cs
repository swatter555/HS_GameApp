using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Reflects the ability of the airbase to operate.
    /// </summary>
    public enum AirbaseOperationsCapacity
    {
        Full,
        SlightlyDegraded,
        ModeratelyDegraded,
        HeavilyDegraded,
        OutOfOperation
    }

    /// <summary>
    /// This class holds specialized information regarding airbases, supply depots, and facilities.
    /// </summary>
    public class LandBase
    {
        #region Airbase Information
        public const int MaxBaseAirUnits = 4;

        // Field to hold the list of attached air units.
        private List<CombatUnit> airUnitsAttached = new List<CombatUnit>();

        // Field to hold the damage level of the airbase.
        private int damage = 0;

        // Field to hold the operational capacity of the airbase.
        private AirbaseOperationsCapacity airbaseCapacity = AirbaseOperationsCapacity.Full;

        /// <summary>
        /// Property to get the list of BaseUnit objects. There is no setter for this property.
        /// </summary>
        public IReadOnlyList<CombatUnit> AirUnitsAttached => airUnitsAttached;

        /// <summary>
        /// Property to get the damage level of the airbase. There is no setter for this property.
        /// </summary>
        public int Damage => damage;

        /// <summary>
        /// Property to get the operational capacity of the airbase. There is no setter for this property.
        /// </summary>
        public AirbaseOperationsCapacity AirbaseCapacity => airbaseCapacity;

        /// <summary>
        /// Adds a BaseUnit to the list.
        /// </summary>
        /// <param name="unit">The BaseUnit to add.</param>
        public void AddAirUnit(CombatUnit unit)
        {
            // Check for null.
            if (unit == null)
            {
                
            }

            // Make sure we don't add more than 4.
            if (airUnitsAttached.Count > MaxBaseAirUnits)
            {
                
            }

            // Make sure its an aircraft BaseUnitValueType unit.
            if (unit.UnitType != UnitType.AirUnit)
            {
                
            }
            else airUnitsAttached.Add(unit);
        }

        /// <summary>
        /// Removes a BaseUnit from the list.
        /// </summary>
        /// <param name="unit">The BaseUnit to remove.</param>
        /// <returns>True if the unit was successfully removed, otherwise false.</returns>
        public bool RemoveAirUnit(CombatUnit unit)
        {
            if (unit == null)
            {
                
            }

            return airUnitsAttached.Remove(unit);
        }

        /// <summary>
        /// Adds damage to the airbase. This method is currently empty and will be implemented later.
        /// </summary>
        public void AddDamage(int incomingDamage)
        {
            // Clamp the incoming damage to be between 1 and 100.
            incomingDamage = Math.Max(1, Math.Min(100, incomingDamage));

            // Add the incoming damage to the current damage.
            damage += incomingDamage;

            // Clamp the total damage to be between 1 and 100.
            damage = Math.Max(1, Math.Min(100, damage));

            // Update the airbase's operational capacity based on the new damage level.
            if (damage >= 81 && damage <= 100)
            {
                airbaseCapacity = AirbaseOperationsCapacity.OutOfOperation;
            }
            else if (damage >= 61 && damage <= 80)
            {
                airbaseCapacity = AirbaseOperationsCapacity.HeavilyDegraded;
            }
            else if (damage >= 41 && damage <= 60)
            {
                airbaseCapacity = AirbaseOperationsCapacity.ModeratelyDegraded;
            }
            else if (damage >= 21 && damage <= 40)
            {
                airbaseCapacity = AirbaseOperationsCapacity.SlightlyDegraded;
            }
            else
            {
                airbaseCapacity = AirbaseOperationsCapacity.Full;
            }
        }

        /// <summary>
        /// Removes damage from the airbase. This method is currently empty and will be implemented later.
        /// </summary>
        public void RemoveDamage(int repairAmount)
        {
            // Clamp the repair amount to be between 1 and 100.
            repairAmount = Math.Max(1, Math.Min(100, repairAmount));

            // Remove the repair amount from the current damage.
            damage -= repairAmount;

            // Clamp the total damage to be between 1 and 100.
            damage = Math.Max(1, Math.Min(100, damage));

            // Update the airbase's operational capacity based on the new damage level.
            if (damage >= 81 && damage <= 100)
            {
                airbaseCapacity = AirbaseOperationsCapacity.OutOfOperation;
            }
            else if (damage >= 61 && damage <= 80)
            {
                airbaseCapacity = AirbaseOperationsCapacity.HeavilyDegraded;
            }
            else if (damage >= 41 && damage <= 60)
            {
                airbaseCapacity = AirbaseOperationsCapacity.ModeratelyDegraded;
            }
            else if (damage >= 21 && damage <= 40)
            {
                airbaseCapacity = AirbaseOperationsCapacity.SlightlyDegraded;
            }
            else
            {
                airbaseCapacity = AirbaseOperationsCapacity.Full;
            }
        }
        #endregion

        #region Supply Depot Information

        #endregion
    }
}