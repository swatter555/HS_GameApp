
using System;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Represents the operational capacity of a land-based facility based on damage level.
    /// </summary>
    public enum OperationalCapacity
    {
        Full,
        SlightlyDegraded,
        ModeratelyDegraded,
        HeavilyDegraded,
        OutOfOperation
    }

    /// <summary>
    /// Base class for all land-based facilities including airbases and supply depots.
    /// Provides common functionality for damage tracking, repairs, and operational status.
    /// </summary>
    [Serializable]
    public class LandBaseProfile : ISerializable
    {
        #region Constants
        private const string CLASS_NAME = nameof(LandBaseProfile);

        /// <summary>
        /// The maximum possible damage value (100%)
        /// </summary>
        public const int MAX_DAMAGE = 100;

        /// <summary>
        /// The minimum possible damage value (0%)
        /// </summary>
        public const int MIN_DAMAGE = 0;
        #endregion

        #region Fields
        /// <summary>
        /// Current damage level of the facility (0-100)
        /// </summary>
        private int damage = 0;

        /// <summary>
        /// Current operational capacity based on damage
        /// </summary>
        private OperationalCapacity operationalCapacity = OperationalCapacity.Full;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the current damage level of the facility.
        /// </summary>
        public int Damage => damage;

        /// <summary>
        /// Gets the current operational capacity of the facility.
        /// </summary>
        public OperationalCapacity OperationalCapacity => operationalCapacity;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of the LandBaseProfile class with default values.
        /// </summary>
        public LandBaseProfile()
        {
            // Default constructor initializes with no damage and full operational capacity
            damage = MIN_DAMAGE;
            operationalCapacity = OperationalCapacity.Full;
        }

        /// <summary>
        /// Creates a new instance of the LandBaseProfile class with specified initial damage.
        /// </summary>
        /// <param name="initialDamage">The initial damage level (0-100)</param>
        public LandBaseProfile(int initialDamage)
        {
            try
            {
                // Initialize with specified damage
                AddDamage(initialDamage);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected LandBaseProfile(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Retrieve base fields
                damage = info.GetInt32(nameof(damage));
                operationalCapacity = (OperationalCapacity)info.GetValue(nameof(operationalCapacity), typeof(OperationalCapacity));
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
        /// Adds damage to the facility and updates its operational capacity.
        /// </summary>
        /// <param name="incomingDamage">The amount of damage to add (1-100)</param>
        public virtual void AddDamage(int incomingDamage)
        {
            try
            {
                // Clamp the incoming damage to be between 1 and 100
                incomingDamage = Math.Max(1, Math.Min(MAX_DAMAGE, incomingDamage));

                // Add the incoming damage to the current damage
                damage += incomingDamage;

                // Clamp the total damage to be between 0 and 100
                damage = Math.Max(MIN_DAMAGE, Math.Min(MAX_DAMAGE, damage));

                // Update operational capacity based on the new damage level
                UpdateOperationalCapacity();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "AddDamage", e);
                throw;
            }
        }

        /// <summary>
        /// Repairs the facility by removing damage and updating its operational capacity.
        /// </summary>
        /// <param name="repairAmount">The amount of damage to repair (1-100)</param>
        public virtual void RepairDamage(int repairAmount)
        {
            try
            {
                // Clamp the repair amount to be between 1 and 100
                repairAmount = Math.Max(1, Math.Min(MAX_DAMAGE, repairAmount));

                // Remove the repair amount from the current damage
                damage -= repairAmount;

                // Clamp the total damage to be between 0 and 100
                damage = Math.Max(MIN_DAMAGE, Math.Min(MAX_DAMAGE, damage));

                // Update operational capacity based on the new damage level
                UpdateOperationalCapacity();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "RepairDamage", e);
                throw;
            }
        }

        /// <summary>
        /// Gets the efficiency multiplier based on current operational capacity.
        /// This can be used to scale operations based on damage.
        /// </summary>
        /// <returns>An efficiency multiplier between 0.0 and 1.0</returns>
        public virtual float GetEfficiencyMultiplier()
        {
            switch (operationalCapacity)
            {
                case OperationalCapacity.Full:
                    return 1.0f;
                case OperationalCapacity.SlightlyDegraded:
                    return 0.75f;
                case OperationalCapacity.ModeratelyDegraded:
                    return 0.5f;
                case OperationalCapacity.HeavilyDegraded:
                    return 0.25f;
                case OperationalCapacity.OutOfOperation:
                    return 0.0f;
                default:
                    return 0.0f;
            }
        }

        /// <summary>
        /// Checks if the facility is operational at any level.
        /// </summary>
        /// <returns>True if the facility is at least partly operational, false otherwise</returns>
        public bool IsOperational()
        {
            return operationalCapacity != OperationalCapacity.OutOfOperation;
        }

        /// <summary>
        /// Checks if the facility is at full operational capacity.
        /// </summary>
        /// <returns>True if the facility is at full capacity, false otherwise</returns>
        public bool IsFullyOperational()
        {
            return operationalCapacity == OperationalCapacity.Full;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Updates the operational capacity based on the current damage level.
        /// </summary>
        protected virtual void UpdateOperationalCapacity()
        {
            if (damage >= 81 && damage <= 100)
            {
                operationalCapacity = OperationalCapacity.OutOfOperation;
            }
            else if (damage >= 61 && damage <= 80)
            {
                operationalCapacity = OperationalCapacity.HeavilyDegraded;
            }
            else if (damage >= 41 && damage <= 60)
            {
                operationalCapacity = OperationalCapacity.ModeratelyDegraded;
            }
            else if (damage >= 21 && damage <= 40)
            {
                operationalCapacity = OperationalCapacity.SlightlyDegraded;
            }
            else
            {
                operationalCapacity = OperationalCapacity.Full;
            }
        }
        #endregion

        #region ISerializable Implementation
        /// <summary>
        /// Serializes this instance.
        /// </summary>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Store base fields
                info.AddValue(nameof(damage), damage);
                info.AddValue(nameof(operationalCapacity), operationalCapacity);
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