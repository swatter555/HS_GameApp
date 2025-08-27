using HammerAndSickle.Services;
using System;
using System.Text.Json.Serialization;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Stores hex edge information with JSON serialization support.
    /// Uses the correct HexDirection order: NE, E, SE, SW, W, NW
    /// </summary>
    [Serializable]
    public class FeatureBorders
    {
        #region Constants
        private const string CLASS_NAME = nameof(FeatureBorders);
        #endregion

        #region Properties

        [JsonInclude]
        public bool Northeast { get; set; }

        [JsonInclude]
        public bool East { get; set; }

        [JsonInclude]
        public bool Southeast { get; set; }

        [JsonInclude]
        public bool Southwest { get; set; }

        [JsonInclude]
        public bool West { get; set; }

        [JsonInclude]
        public bool Northwest { get; set; }

        [JsonInclude]
        public BorderType Type { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Parameterless constructor for JSON serialization.
        /// </summary>
        [JsonConstructor]
        public FeatureBorders()
        {
            Northeast = false;
            East = false;
            Southeast = false;
            Southwest = false;
            West = false;
            Northwest = false;
            Type = BorderType.None;
        }

        /// <summary>
        /// Constructor that initializes the borders with specific directions.
        /// Parameters follow HexDirection order: NE, E, SE, SW, W, NW
        /// </summary>
        /// <param name="type">Border type</param>
        /// <param name="ne">Northeast border value</param>
        /// <param name="e">East border value</param>
        /// <param name="se">Southeast border value</param>
        /// <param name="sw">Southwest border value</param>
        /// <param name="w">West border value</param>
        /// <param name="nw">Northwest border value</param>
        public FeatureBorders(BorderType type, bool ne, bool e, bool se, bool sw, bool w, bool nw)
        {
            try
            {
                Type = type;
                Northeast = ne;
                East = e;
                Southeast = se;
                Southwest = sw;
                West = w;
                Northwest = nw;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(FeatureBorders), ex);
                throw;
            }
        }

        /// <summary>
        /// Constructor that initializes the borders with a specific type.
        /// </summary>
        /// <param name="type">Border type</param>
        public FeatureBorders(BorderType type)
        {
            try
            {
                Type = type;
                Northeast = false;
                East = false;
                Southeast = false;
                Southwest = false;
                West = false;
                Northwest = false;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(FeatureBorders), ex);
                throw;
            }
        }

        /// <summary>
        /// Constructor that initializes the borders from a binary string.
        /// String positions map to HexDirection order: NE, E, SE, SW, W, NW
        /// </summary>
        /// <param name="input">Binary string representing the hex matrix (6 chars)</param>
        /// <param name="type">Border type</param>
        public FeatureBorders(string input, BorderType type = BorderType.None)
        {
            try
            {
                Type = type;
                SetBorderString(input ?? "000000");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(FeatureBorders), ex);
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reset the borders to default value.
        /// </summary>
        public void Reset()
        {
            try
            {
                SetBorderString("000000");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(Reset), ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the value of a specified side.
        /// </summary>
        /// <param name="direction">The hex direction</param>
        /// <returns>Boolean value of the specified direction</returns>
        public bool GetBorder(HexDirection direction)
        {
            try
            {
                return direction switch
                {
                    HexDirection.NE => Northeast,
                    HexDirection.E => East,
                    HexDirection.SE => Southeast,
                    HexDirection.SW => Southwest,
                    HexDirection.W => West,
                    HexDirection.NW => Northwest,
                    _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction")
                };
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetBorder), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets a side to a specific value.
        /// </summary>
        /// <param name="direction">The hex direction</param>
        /// <param name="value">Value to set</param>
        public void SetBorder(HexDirection direction, bool value)
        {
            try
            {
                switch (direction)
                {
                    case HexDirection.NE: Northeast = value; break;
                    case HexDirection.E: East = value; break;
                    case HexDirection.SE: Southeast = value; break;
                    case HexDirection.SW: Southwest = value; break;
                    case HexDirection.W: West = value; break;
                    case HexDirection.NW: Northwest = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetBorder), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the matrix with a 6-character binary string.
        /// String positions map to HexDirection enum order: [0]=NE, [1]=E, [2]=SE, [3]=SW, [4]=W, [5]=NW
        /// </summary>
        /// <param name="input">Binary string representing the hex matrix</param>
        public void SetBorderString(string input)
        {
            try
            {
                if (string.IsNullOrEmpty(input))
                {
                    input = "000000";
                }

                if (input.Length != 6 || !IsBinaryString(input))
                {
                    throw new ArgumentException("Input must be a 6-character binary string", nameof(input));
                }

                // Map string positions to HexDirection enum order
                Northeast = input[0] == '1';  // HexDirection.NE = 0
                East = input[1] == '1';       // HexDirection.E = 1
                Southeast = input[2] == '1';  // HexDirection.SE = 2
                Southwest = input[3] == '1';  // HexDirection.SW = 3
                West = input[4] == '1';       // HexDirection.W = 4
                Northwest = input[5] == '1';  // HexDirection.NW = 5
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetBorderString), ex);
                throw;
            }
        }

        /// <summary>
        /// Converts the hex borders to a binary string representation.
        /// String positions follow HexDirection enum order: NE, E, SE, SW, W, NW
        /// </summary>
        /// <returns>Binary string representing the hex border</returns>
        public string GetBorderString()
        {
            try
            {
                return $"{(Northeast ? "1" : "0")}{(East ? "1" : "0")}{(Southeast ? "1" : "0")}{(Southwest ? "1" : "0")}{(West ? "1" : "0")}{(Northwest ? "1" : "0")}";
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetBorderString), ex);
                return "000000";
            }
        }

        /// <summary>
        /// Checks if any of the borders are active.
        /// </summary>
        /// <returns>True if any border is active, false otherwise</returns>
        public bool HasAnyBorders()
        {
            try
            {
                return Northeast || East || Southeast || Southwest || West || Northwest;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(HasAnyBorders), ex);
                return false;
            }
        }

        /// <summary>
        /// Checks if any of the borders in the given FeatureBorders object are active.
        /// </summary>
        /// <param name="borders">The FeatureBorders object representing the state of each border</param>
        /// <returns>True if any of the borders are active, false otherwise</returns>
        public static bool CheckFeatureBorders(FeatureBorders borders)
        {
            try
            {
                return borders?.HasAnyBorders() ?? false;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CheckFeatureBorders), ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates if a string is a binary string.
        /// </summary>
        /// <param name="input">The string to check</param>
        /// <returns>True if the string is binary, false otherwise</returns>
        private static bool IsBinaryString(string input)
        {
            try
            {
                if (string.IsNullOrEmpty(input)) return false;

                foreach (char c in input)
                {
                    if (c != '0' && c != '1')
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}