using System;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Represents a combat rating with attack and defense values.
    /// Provides validation and helper methods for managing paired combat statistics.
    /// Used for different combat types like hard/soft attack, air combat, etc.
    /// </summary>
    [Serializable]
    public class CombatRating : ISerializable
    {
        #region Constants

        private const string CLASS_NAME = nameof(CombatRating);

        #endregion // Constants

        #region Properties

        public int Attack { get; private set; }
        public int Defense { get; private set; }

        #endregion // Properties


        #region Constructors

        /// <summary>
        /// Creates a new CombatRating with the specified attack and defense values.
        /// Values are automatically clamped to valid ranges.
        /// </summary>
        /// <param name="attack">The attack value</param>
        /// <param name="defense">The defense value</param>
        public CombatRating(int attack, int defense)
        {
            try
            {
                Attack = ValidateCombatValue(attack);
                Defense = ValidateCombatValue(defense);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a new CombatRating with both attack and defense set to the same value.
        /// </summary>
        /// <param name="value">The value for both attack and defense</param>
        public CombatRating(int value) : this(value, value)
        {
        }

        /// <summary>
        /// Creates a new CombatRating with zero attack and defense values.
        /// </summary>
        public CombatRating() : this(0, 0)
        {
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected CombatRating(SerializationInfo info, StreamingContext context)
        {
            try
            {
                Attack = info.GetInt32(nameof(Attack));
                Defense = info.GetInt32(nameof(Defense));
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion // Constructors


        #region Public Methods

        /// <summary>
        /// Sets the attack value with validation.
        /// </summary>
        /// <param name="value">The new attack value</param>
        public void SetAttack(int value)
        {
            try
            {
                Attack = ValidateCombatValue(value);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetAttack", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the defense value with validation.
        /// </summary>
        /// <param name="value">The new defense value</param>
        public void SetDefense(int value)
        {
            try
            {
                Defense = ValidateCombatValue(value);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetDefense", e);
                throw;
            }
        }

        /// <summary>
        /// Sets both attack and defense values with validation.
        /// </summary>
        /// <param name="attack">The new attack value</param>
        /// <param name="defense">The new defense value</param>
        public void SetValues(int attack, int defense)
        {
            try
            {
                Attack = ValidateCombatValue(attack);
                Defense = ValidateCombatValue(defense);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetValues", e);
                throw;
            }
        }

        /// <summary>
        /// Gets the total combat value (attack + defense).
        /// </summary>
        /// <returns>The sum of attack and defense values</returns>
        public int GetTotalCombatValue()
        {
            return Attack + Defense;
        }

        /// <summary>
        /// Gets the attack-to-defense ratio.
        /// Returns 0 if defense is 0.
        /// </summary>
        /// <returns>The ratio of attack to defense</returns>
        public float GetAttackDefenseRatio()
        {
            return Defense > 0 ? (float)Attack / Defense : 0f;
        }

        /// <summary>
        /// Creates a copy of this CombatRating.
        /// </summary>
        /// <returns>A new CombatRating with identical values</returns>
        public CombatRating Clone()
        {
            try
            {
                return new CombatRating(Attack, Defense);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        /// <summary>
        /// Returns a string representation of the combat rating.
        /// </summary>
        /// <returns>Formatted string showing attack and defense values</returns>
        public override string ToString()
        {
            return $"Attack: {Attack}, Defense: {Defense}";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current CombatRating.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if the objects are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is CombatRating other)
            {
                return Attack == other.Attack && Defense == other.Defense;
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this CombatRating.
        /// </summary>
        /// <returns>A hash code for the current object</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Attack, Defense);
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
            return Math.Clamp(value, CUConstants.MIN_COMBAT_VALUE, CUConstants.MAX_COMBAT_VALUE);
        }

        #endregion // Private Methods


        #region ISerializable Implementation

        /// <summary>
        /// Populates a SerializationInfo object with the data needed to serialize the CombatRating.
        /// </summary>
        /// <param name="info">The SerializationInfo object to populate</param>
        /// <param name="context">The StreamingContext structure</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                info.AddValue(nameof(Attack), Attack);
                info.AddValue(nameof(Defense), Defense);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion // ISerializable Implementation
    }
}