using System;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Represents a game statistic that has both a maximum and current value.
    /// Provides validation and helper methods for managing these paired values.
    /// Used for stats like hit points, movement points, action counts, and supply levels.
    /// </summary>
    [Serializable]
    public class StatsMaxCurrent
    {
        #region Constants

        private const string CLASS_NAME = nameof(StatsMaxCurrent);
        private const float MIN_VALID_VALUE = -1000f;
        private const float MAX_VALID_VALUE = 1000f;

        #endregion

        #region Properties

        public float Max { get; private set; }
        public float Current { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new StatsMaxCurrent with the specified maximum value.
        /// Current value is initialized to the maximum.
        /// </summary>
        /// <param name="maxValue">The maximum value for this statistic</param>
        public StatsMaxCurrent(float maxValue)
        {
            try
            {
                if (maxValue < MIN_VALID_VALUE || maxValue > MAX_VALID_VALUE)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxValue),
                        $"Max value must be between {MIN_VALID_VALUE} and {MAX_VALID_VALUE}");
                }

                Max = maxValue;
                Current = maxValue;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a new StatsMaxCurrent with specified maximum and current values.
        /// Used for deserialization when restoring saved state.
        /// </summary>
        /// <param name="maxValue">The maximum value for this statistic</param>
        /// <param name="currentValue">The current value for this statistic</param>
        public StatsMaxCurrent(float maxValue, float currentValue)
        {
            try
            {
                if (maxValue < MIN_VALID_VALUE || maxValue > MAX_VALID_VALUE)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxValue),
                        $"Max value must be between {MIN_VALID_VALUE} and {MAX_VALID_VALUE}");
                }

                if (currentValue < MIN_VALID_VALUE || currentValue > MAX_VALID_VALUE)
                {
                    throw new ArgumentOutOfRangeException(nameof(currentValue),
                        $"Current value must be between {MIN_VALID_VALUE} and {MAX_VALID_VALUE}");
                }

                Max = maxValue;
                Current = currentValue;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the current value with validation bounds checking.
        /// </summary>
        /// <param name="value">The new current value</param>
        public void SetCurrent(float value)
        {
            try
            {
                if (value < MIN_VALID_VALUE || value > MAX_VALID_VALUE)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"Current value must be between {MIN_VALID_VALUE} and {MAX_VALID_VALUE}");
                }

                Current = value;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetCurrent", e);
                throw;
            }
        }

        /// <summary>
        /// Resets current value to maximum.
        /// </summary>
        public void ResetToMax()
        {
            Current = Max;
        }

        /// <summary>
        /// Returns the percentage of current relative to maximum (0.0 to 1.0).
        /// </summary>
        public float GetPercentage()
        {
            if (Max == 0)
                return 0f;

            return Current / Max;
        }

        /// <summary>
        /// Checks if current value equals maximum value.
        /// </summary>
        public bool IsAtMax()
        {
            return Math.Abs(Current - Max) < 0.001f;
        }

        /// <summary>
        /// Checks if current value is zero or negative.
        /// </summary>
        public bool IsEmpty()
        {
            return Current <= 0f;
        }

        /// <summary>
        /// Sets the maximum allowable value for the current setting.
        /// </summary>
        /// <remarks>If the current value exceeds the newly set maximum, it will be adjusted to match the
        /// maximum.</remarks>
        /// <param name="value">The maximum value to set. Must be between <see langword="MIN_VALID_VALUE"/> and <see
        /// langword="MAX_VALID_VALUE"/>.</param>
        public void SetMax(float value)
        {
            try
            {
                if (value < MIN_VALID_VALUE || value > MAX_VALID_VALUE)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"Max value must be between {MIN_VALID_VALUE} and {MAX_VALID_VALUE}");
                }
                Max = value;
                // Ensure current does not exceed new max
                if (Current > Max)
                {
                    Current = Max;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetMax", e);
                throw;
            }
        }

        /// <summary>
        /// Increase current value by the specified amount or 1.
        /// </summary>
        /// <param name="amount"></param>
        public void IncrementCurrent(float amount = 1)
        {
            try
            {
                if (amount < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(amount), "Increment amount must be non-negative");
                }
                Current += amount;
                if (Current > Max)
                {
                    Current = Max; // Clamp to max
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "IncrementCurrent", e);
                throw;
            }
        }

        /// <summary>
        /// Decrease current value by the specified amount or 1.
        /// </summary>
        /// <param name="amount"></param>
        public void DecrementCurrent(float amount = 1)
        {
            try
            {
                if (amount < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(amount), "Decrement amount must be non-negative");
                }
                Current -= amount;
                if (Current < 0)
                {
                    Current = 0; // Clamp to zero
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DecrementCurrent", e);
                throw;
            }
        }

        #endregion
    }
}