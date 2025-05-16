using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Represents an airbase facility that can host and support air units.
    /// Inherits base damage and operational capacity functionality from LandBaseProfile.
    /// </summary>
    [Serializable]
    public class AirbaseSubProfile : LandBaseProfile, ISerializable
    {
        #region Constants
        private const string CLASS_NAME = nameof(AirbaseSubProfile);

        public const int MaxBaseAirUnits = 4;

        #endregion // Constants


        #region Fields

        private List<CombatUnit> airUnitsAttached = new List<CombatUnit>();

        #endregion // Fields


        #region Properties

        public IReadOnlyList<CombatUnit> AirUnitsAttached => airUnitsAttached;

        #endregion // Properties


        #region Constructors

        public AirbaseSubProfile() : base()
        {
            // Default constructor uses base class initialization
        }


        public AirbaseSubProfile(int initialDamage) : base(initialDamage)
        {
            // Uses base class initialization with initial damage
        }

        protected AirbaseSubProfile(SerializationInfo info, StreamingContext context) : base(info, context)
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
        #endregion // Constructors


        #region Air Unit Management
       
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

        public int GetAttachedUnitCount()
        {
            return airUnitsAttached.Count;
        }

        public bool HasCapacityForMoreUnits()
        {
            return airUnitsAttached.Count < MaxBaseAirUnits;
        }

        #endregion // Air Unit Management


        #region Operational Methods

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

        public bool CanLaunchAirOperations()
        {
            // Air operations require at least moderately degraded status
            return OperationalCapacity != OperationalCapacity.HeavilyDegraded &&
                   OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        public bool CanReceiveAircraft()
        {
            // Even heavily damaged airbases can receive aircraft in an emergency
            return OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        #endregion // Operational Methods

        #region ISerializable Implementation

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

        #endregion // ISerializable Implementation
    }
}