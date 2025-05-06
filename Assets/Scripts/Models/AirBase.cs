using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Represents an airbase facility that can host and support air units.
    /// Inherits base damage and operational capacity functionality from LandBase.
    /// </summary>
    [Serializable]
    public class Airbase : LandBase, ISerializable
    {
        #region Constants
        private const string CLASS_NAME = nameof(Airbase);

        /// <summary>
        /// The maximum number of air units that can be attached to this airbase.
        /// </summary>
        public const int MaxBaseAirUnits = 4;
        #endregion

        #region Fields
        /// <summary>
        /// List of air units attached to this airbase.
        /// </summary>
        private List<CombatUnit> airUnitsAttached = new List<CombatUnit>();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the read-only list of air units attached to this airbase.
        /// </summary>
        public IReadOnlyList<CombatUnit> AirUnitsAttached => airUnitsAttached;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of the Airbase class with default values.
        /// </summary>
        public Airbase() : base()
        {
            // Default constructor uses base class initialization
        }

        /// <summary>
        /// Creates a new instance of the Airbase class with specified initial damage.
        /// </summary>
        /// <param name="initialDamage">The initial damage level (0-100)</param>
        public Airbase(int initialDamage) : base(initialDamage)
        {
            // Uses base class initialization with initial damage
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected Airbase(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            try
            {
                // Deserialize airbase-specific fields
                // First determine how many air units are attached
                int airUnitCount = info.GetInt32("AirUnitCount");

                // Clear the current list
                airUnitsAttached.Clear();

                // Deserialize each air unit
                for (int i = 0; i < airUnitCount; i++)
                {
                    CombatUnit unit = (CombatUnit)info.GetValue($"AirUnit_{i}", typeof(CombatUnit));
                    if (unit != null)
                    {
                        airUnitsAttached.Add(unit);
                    }
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }
        #endregion

        #region Air Unit Management
        /// <summary>
        /// Adds an air unit to the airbase.
        /// </summary>
        /// <param name="unit">The air unit to add</param>
        /// <exception cref="ArgumentNullException">Thrown when unit is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when airbase is full or unit is not an air unit</exception>
        public void AddAirUnit(CombatUnit unit)
        {
            try
            {
                // Check for null
                if (unit == null)
                {
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");
                }

                // Make sure we don't add more than the maximum allowed
                if (airUnitsAttached.Count >= MaxBaseAirUnits)
                {
                    throw new InvalidOperationException($"Airbase is at maximum capacity ({MaxBaseAirUnits} units)");
                }

                // Make sure it's an aircraft unit
                if (unit.UnitType != UnitType.AirUnit)
                {
                    throw new InvalidOperationException($"Only air units can be attached to an airbase. Unit type: {unit.UnitType}");
                }

                // Add the unit to the airbase
                airUnitsAttached.Add(unit);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "AddAirUnit", e);
                throw;
            }
        }

        /// <summary>
        /// Removes an air unit from the airbase.
        /// </summary>
        /// <param name="unit">The air unit to remove</param>
        /// <returns>True if the unit was successfully removed, otherwise false</returns>
        /// <exception cref="ArgumentNullException">Thrown when unit is null</exception>
        public bool RemoveAirUnit(CombatUnit unit)
        {
            try
            {
                if (unit == null)
                {
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");
                }

                return airUnitsAttached.Remove(unit);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "RemoveAirUnit", e);
                throw;
            }
        }

        /// <summary>
        /// Gets the number of air units currently attached to this airbase.
        /// </summary>
        /// <returns>The number of attached air units</returns>
        public int GetAttachedUnitCount()
        {
            return airUnitsAttached.Count;
        }

        /// <summary>
        /// Checks if the airbase has room for additional air units.
        /// </summary>
        /// <returns>True if the airbase can accept more air units, otherwise false</returns>
        public bool HasCapacityForMoreUnits()
        {
            return airUnitsAttached.Count < MaxBaseAirUnits;
        }
        #endregion

        #region Operational Methods
        /// <summary>
        /// Gets an operational efficiency multiplier based on current damage level.
        /// This affects how effectively the airbase can support air operations.
        /// </summary>
        /// <returns>An efficiency multiplier between 0.0 and 1.0</returns>
        public override float GetEfficiencyMultiplier()
        {
            // Air operations can be more sensitive to damage than other facilities
            switch (OperationalCapacity)
            {
                case OperationalCapacity.Full:
                    return 1.0f;
                case OperationalCapacity.SlightlyDegraded:
                    return 0.7f;  // Slightly worse than base class
                case OperationalCapacity.ModeratelyDegraded:
                    return 0.4f;  // Slightly worse than base class
                case OperationalCapacity.HeavilyDegraded:
                    return 0.2f;  // Slightly worse than base class
                case OperationalCapacity.OutOfOperation:
                    return 0.0f;
                default:
                    return 0.0f;
            }
        }

        /// <summary>
        /// Determines if the airbase can launch air operations based on its current status.
        /// </summary>
        /// <returns>True if air operations are possible, otherwise false</returns>
        public bool CanLaunchAirOperations()
        {
            // Air operations require at least moderately degraded status
            return OperationalCapacity != OperationalCapacity.HeavilyDegraded &&
                   OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        /// <summary>
        /// Determines if the airbase can receive and land aircraft based on its current status.
        /// Landing operations can generally continue at worse damage levels than takeoff operations.
        /// </summary>
        /// <returns>True if landing operations are possible, otherwise false</returns>
        public bool CanReceiveAircraft()
        {
            // Even heavily damaged airbases can receive aircraft in an emergency
            return OperationalCapacity != OperationalCapacity.OutOfOperation;
        }
        #endregion

        #region ISerializable Implementation
        /// <summary>
        /// Serializes this instance.
        /// </summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Call base class serialization first
                base.GetObjectData(info, context);

                // Serialize airbase-specific fields
                // Store the number of air units
                info.AddValue("AirUnitCount", airUnitsAttached.Count);

                // Store each air unit
                for (int i = 0; i < airUnitsAttached.Count; i++)
                {
                    info.AddValue($"AirUnit_{i}", airUnitsAttached[i]);
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }
        #endregion
    }
}