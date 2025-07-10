using System;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
 /*────────────────────────────────────────────────────────────────────────────

StatsMaxCurrent ─ bounded numeric container with maximum/current value tracking
──────────────────────────────────────────────────────────────────────────────

Summary

═══════

- Encapsulates a numeric statistic with both maximum and current values, providing
 automatic bounds checking and percentage calculations for game statistics like
 hit points, action pools, movement points, and supply levels.

- Enforces validation bounds (-1000 to +1000) on all operations and integrates
 with AppService error handling for consistent exception management.

- Supports serialization for save game persistence and provides convenience
 methods for common operations like resetting to maximum, incrementing/
 decrementing with clamping, and percentage calculations.

Public properties

═════════════════

float Max { get; private set; }
float Current { get; private set; }

Constructors

═════════════

public StatsMaxCurrent(float maxValue)
public StatsMaxCurrent(float maxValue, float currentValue)

Public method signatures

════════════════════════

void SetCurrent(float value) - Sets current value with bounds validation
void ResetToMax() - Resets current value to maximum
float GetPercentage() - Returns current/max ratio (0.0 to 1.0)
bool IsAtMax() - Checks if current equals maximum (within 0.001f tolerance)
bool IsEmpty() - Checks if current value is zero or negative
void SetMax(float value) - Sets maximum value, clamps current if needed
void IncrementCurrent(float amount = 1) - Increases current by amount, clamped to max
void DecrementCurrent(float amount = 1) - Decreases current by amount, clamped to zero

Important aspects

═════════════════

- **Bounds Enforcement**: All values must be between MIN_VALID_VALUE (-1000f) and 
 MAX_VALID_VALUE (+1000f) to prevent extreme values that could break game balance.

- **Automatic Clamping**: Current value is automatically constrained between 0 and Max
 during increment/decrement operations and when Max is changed.

- **Serialization Ready**: [Serializable] attribute enables binary persistence for
 save game compatibility without requiring ISerializable implementation.

- **Error Integration**: All public methods use try-catch with AppService.HandleException
 for consistent error reporting and debugging support.

- **Floating Point Tolerance**: IsAtMax() uses 0.001f epsilon for floating point
 comparison safety rather than direct equality checks.

────────────────────────────────────────────────────────────────────────────*/
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