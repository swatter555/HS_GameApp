using HammerAndSickle.Services;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Stores hex edge information with JSON serialization support.
    /// Border strings use the order: NW, NE, E, SE, SW, W
    /// </summary>
    [Serializable]
    public class JSONFeatureBorders
    {
        #region Constants

        private const string CLASS_NAME = nameof(JSONFeatureBorders);

        #endregion // Constants

        #region Properties

        [JsonInclude]
        public bool Northwest { get; set; }

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
        public BorderType Type { get; set; }

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// Parameterless constructor for JSON serialization.
        /// </summary>
        [JsonConstructor]
        public JSONFeatureBorders()
        {
            Northwest = false;
            Northeast = false;
            East = false;
            Southeast = false;
            Southwest = false;
            West = false;
            Type = BorderType.None;
        }

        /// <summary>
        /// Constructor that initializes the borders with specific directions.
        /// </summary>
        /// <param name="type">Border type</param>
        /// <param name="nw">Northwest border value</param>
        /// <param name="ne">Northeast border value</param>
        /// <param name="e">East border value</param>
        /// <param name="se">Southeast border value</param>
        /// <param name="sw">Southwest border value</param>
        /// <param name="w">West border value</param>
        public JSONFeatureBorders(BorderType type, bool nw, bool ne, bool e, bool se, bool sw, bool w)
        {
            try
            {
                Type = type;
                Northwest = nw;
                Northeast = ne;
                East = e;
                Southeast = se;
                Southwest = sw;
                West = w;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(JSONFeatureBorders), ex);
                throw;
            }
        }

        /// <summary>
        /// Constructor that initializes the borders with a specific type.
        /// </summary>
        /// <param name="type">Border type</param>
        public JSONFeatureBorders(BorderType type)
        {
            try
            {
                Type = type;
                Northwest = false;
                Northeast = false;
                East = false;
                Southeast = false;
                Southwest = false;
                West = false;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(JSONFeatureBorders), ex);
                throw;
            }
        }

        /// <summary>
        /// Constructor that initializes the borders from a binary string.
        /// String positions map to: [0]=NW, [1]=NE, [2]=E, [3]=SE, [4]=SW, [5]=W
        /// </summary>
        /// <param name="input">Binary string representing the hex matrix (6 chars)</param>
        /// <param name="type">Border type</param>
        public JSONFeatureBorders(string input, BorderType type = BorderType.None)
        {
            try
            {
                Type = type;
                SetBorderString(input ?? "000000");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(JSONFeatureBorders), ex);
                throw;
            }
        }

        #endregion // Constructors

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
                    HexDirection.NW => Northwest,
                    HexDirection.NE => Northeast,
                    HexDirection.E => East,
                    HexDirection.SE => Southeast,
                    HexDirection.SW => Southwest,
                    HexDirection.W => West,
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
                    case HexDirection.NW: Northwest = value; break;
                    case HexDirection.NE: Northeast = value; break;
                    case HexDirection.E: East = value; break;
                    case HexDirection.SE: Southeast = value; break;
                    case HexDirection.SW: Southwest = value; break;
                    case HexDirection.W: West = value; break;
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
        /// String positions map to: [0]=NW, [1]=NE, [2]=E, [3]=SE, [4]=SW, [5]=W
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

                Northwest = input[0] == '1';
                Northeast = input[1] == '1';
                East = input[2] == '1';
                Southeast = input[3] == '1';
                Southwest = input[4] == '1';
                West = input[5] == '1';
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetBorderString), ex);
                throw;
            }
        }

        /// <summary>
        /// Converts the hex borders to a binary string representation.
        /// String positions follow order: NW, NE, E, SE, SW, W
        /// </summary>
        /// <returns>Binary string representing the hex border</returns>
        public string GetBorderString()
        {
            try
            {
                return $"{(Northwest ? "1" : "0")}{(Northeast ? "1" : "0")}{(East ? "1" : "0")}{(Southeast ? "1" : "0")}{(Southwest ? "1" : "0")}{(West ? "1" : "0")}";
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
                return Northwest || Northeast || East || Southeast || Southwest || West;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(HasAnyBorders), ex);
                return false;
            }
        }

        /// <summary>
        /// Checks if any of the borders in the given JSONFeatureBorders object are active.
        /// </summary>
        /// <param name="borders">The JSONFeatureBorders object representing the state of each border</param>
        /// <returns>True if any of the borders are active, false otherwise</returns>
        public static bool CheckFeatureBorders(JSONFeatureBorders borders)
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

        #endregion // Public Methods

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

                return input.All(c => c == '0' || c == '1');
            }
            catch
            {
                return false;
            }
        }

        #endregion // Private Methods
    }
}