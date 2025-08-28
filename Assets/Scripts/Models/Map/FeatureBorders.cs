using HammerAndSickle.Services;
using System;
using System.Text.Json.Serialization;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Stores hex edge information with explicit mapping to HexDirection.
    /// Canonical order is the enum order: NW, NE, E, SE, SW, W.
    /// Provides legacy-safe parsing for NE-first strings.
    /// </summary>
    [Serializable]
    public class FeatureBorders
    {
        #region Constants / Helpers
        private const string CLASS_NAME = nameof(FeatureBorders);

        /// <summary>
        /// Supported string orders for 6-bit border strings.
        /// </summary>
        public enum BorderStringOrder
        {
            /// <summary>Enum order: NW, NE, E, SE, SW, W</summary>
            EnumOrder,
            /// <summary>Legacy NE-first order: NE, E, SE, SW, W, NW</summary>
            LegacyNEFirst
        }
        #endregion

        #region Properties
        [JsonInclude] public bool Northwest { get; set; }
        [JsonInclude] public bool Northeast { get; set; }
        [JsonInclude] public bool East { get; set; }
        [JsonInclude] public bool Southeast { get; set; }
        [JsonInclude] public bool Southwest { get; set; }
        [JsonInclude] public bool West { get; set; }
        [JsonInclude] public BorderType Type { get; set; }
        #endregion

        #region Constructors
        [JsonConstructor]
        public FeatureBorders()
        {
            Reset();
            Type = BorderType.None;
        }

        public FeatureBorders(BorderType type)
        {
            Reset();
            Type = type;
        }

        /// <summary>
        /// Constructor in enum order: NW, NE, E, SE, SW, W.
        /// </summary>
        public FeatureBorders(BorderType type, bool nw, bool ne, bool e, bool se, bool sw, bool w)
        {
            try
            {
                Type = type;
                Northwest = nw; Northeast = ne; East = e; Southeast = se; Southwest = sw; West = w;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(FeatureBorders), ex);
                throw;
            }
        }

        /// <summary>
        /// Construct from a 6-char binary string. Defaults to enum order.
        /// Use <see cref="BorderStringOrder.LegacyNEFirst"/> for NE-first legacy strings.
        /// </summary>
        public FeatureBorders(string input, BorderType type = BorderType.None, BorderStringOrder order = BorderStringOrder.EnumOrder)
        {
            try
            {
                Type = type;
                SetBorderString(input, order);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(FeatureBorders), ex);
                throw;
            }
        }
        #endregion

        #region Public API
        public void Reset()
        {
            Northwest = Northeast = East = Southeast = Southwest = West = false;
        }

        public bool GetBorder(HexDirection direction)
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

        public void SetBorder(HexDirection direction, bool value)
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

        /// <summary>
        /// Parse a 6-bit string into borders.
        /// </summary>
        public void SetBorderString(string input, BorderStringOrder order = BorderStringOrder.EnumOrder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input)) input = "000000";
                if (input.Length != 6 || !IsBinaryString(input))
                    throw new ArgumentException("Input must be a 6-character binary string", nameof(input));

                // Normalize to enum order bits: [NW, NE, E, SE, SW, W]
                string bitsEnumOrder = order switch
                {
                    BorderStringOrder.EnumOrder => input,
                    BorderStringOrder.LegacyNEFirst => ReorderBits(input, from: BorderStringOrder.LegacyNEFirst, to: BorderStringOrder.EnumOrder),
                    _ => input
                };

                Northwest = bitsEnumOrder[0] == '1';
                Northeast = bitsEnumOrder[1] == '1';
                East = bitsEnumOrder[2] == '1';
                Southeast = bitsEnumOrder[3] == '1';
                Southwest = bitsEnumOrder[4] == '1';
                West = bitsEnumOrder[5] == '1';
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetBorderString), ex);
                throw;
            }
        }

        /// <summary>
        /// Return a 6-bit string in the requested order.
        /// </summary>
        public string GetBorderString(BorderStringOrder order = BorderStringOrder.EnumOrder)
        {
            try
            {
                string enumBits = $"{(Northwest ? "1" : "0")}{(Northeast ? "1" : "0")}{(East ? "1" : "0")}{(Southeast ? "1" : "0")}{(Southwest ? "1" : "0")}{(West ? "1" : "0")}";
                return order switch
                {
                    BorderStringOrder.EnumOrder => enumBits,
                    BorderStringOrder.LegacyNEFirst => ReorderBits(enumBits, from: BorderStringOrder.EnumOrder, to: BorderStringOrder.LegacyNEFirst),
                    _ => enumBits
                };
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetBorderString), ex);
                return "000000";
            }
        }

        public bool HasAnyBorders() => Northwest || Northeast || East || Southeast || Southwest || West;

        public static bool CheckFeatureBorders(FeatureBorders borders) => borders?.HasAnyBorders() ?? false;
        #endregion

        #region Private helpers
        private static bool IsBinaryString(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c != '0' && c != '1') return false;
            }
            return true;
        }

        /// <summary>
        /// Reorders a 6-bit string between supported orders.
        /// </summary>
        private static string ReorderBits(string src, BorderStringOrder from, BorderStringOrder to)
        {
            // EnumOrder indices:   0=NW,1=NE,2=E,3=SE,4=SW,5=W
            // LegacyNEFirst idx:   0=NE,1=E,2=SE,3=SW,4=W,5=NW
            if (from == to) return src;

            if (from == BorderStringOrder.LegacyNEFirst && to == BorderStringOrder.EnumOrder)
            {
                // src[0..5] = NE,E,SE,SW,W,NW -> dst = NW,NE,E,SE,SW,W
                return new string(new[]
                {
                    src[5], // NW
                    src[0], // NE
                    src[1], // E
                    src[2], // SE
                    src[3], // SW
                    src[4]  // W
                });
            }

            if (from == BorderStringOrder.EnumOrder && to == BorderStringOrder.LegacyNEFirst)
            {
                // src[0..5] = NW,NE,E,SE,SW,W -> dst = NE,E,SE,SW,W,NW
                return new string(new[]
                {
                    src[1], // NE
                    src[2], // E
                    src[3], // SE
                    src[4], // SW
                    src[5], // W
                    src[0]  // NW
                });
            }

            // Unsupported mapping (should not occur with current enum)
            return src;
        }
        #endregion
    }
}



//using HammerAndSickle.Services;
//using System;
//using System.Text.Json.Serialization;

//namespace HammerAndSickle.Models.Map
//{
//    /// <summary>
//    /// Stores hex edge information with JSON serialization support.
//    /// Uses the correct HexDirection order: NE, E, SE, SW, W, NW
//    /// </summary>
//    [Serializable]
//    public class FeatureBorders
//    {
//        #region Constants
//        private const string CLASS_NAME = nameof(FeatureBorders);
//        #endregion

//        #region Properties

//        [JsonInclude]
//        public bool Northeast { get; set; }

//        [JsonInclude]
//        public bool East { get; set; }

//        [JsonInclude]
//        public bool Southeast { get; set; }

//        [JsonInclude]
//        public bool Southwest { get; set; }

//        [JsonInclude]
//        public bool West { get; set; }

//        [JsonInclude]
//        public bool Northwest { get; set; }

//        [JsonInclude]
//        public BorderType Type { get; set; }

//        #endregion

//        #region Constructors

//        /// <summary>
//        /// Parameterless constructor for JSON serialization.
//        /// </summary>
//        [JsonConstructor]
//        public FeatureBorders()
//        {
//            Northeast = false;
//            East = false;
//            Southeast = false;
//            Southwest = false;
//            West = false;
//            Northwest = false;
//            Type = BorderType.None;
//        }

//        /// <summary>
//        /// Constructor that initializes the borders with specific directions.
//        /// Parameters follow HexDirection order: NE, E, SE, SW, W, NW
//        /// </summary>
//        /// <param name="type">Border type</param>
//        /// <param name="ne">Northeast border value</param>
//        /// <param name="e">East border value</param>
//        /// <param name="se">Southeast border value</param>
//        /// <param name="sw">Southwest border value</param>
//        /// <param name="w">West border value</param>
//        /// <param name="nw">Northwest border value</param>
//        public FeatureBorders(BorderType type, bool ne, bool e, bool se, bool sw, bool w, bool nw)
//        {
//            try
//            {
//                Type = type;
//                Northeast = ne;
//                East = e;
//                Southeast = se;
//                Southwest = sw;
//                West = w;
//                Northwest = nw;
//            }
//            catch (Exception ex)
//            {
//                AppService.HandleException(CLASS_NAME, nameof(FeatureBorders), ex);
//                throw;
//            }
//        }

//        /// <summary>
//        /// Constructor that initializes the borders with a specific type.
//        /// </summary>
//        /// <param name="type">Border type</param>
//        public FeatureBorders(BorderType type)
//        {
//            try
//            {
//                Type = type;
//                Northeast = false;
//                East = false;
//                Southeast = false;
//                Southwest = false;
//                West = false;
//                Northwest = false;
//            }
//            catch (Exception ex)
//            {
//                AppService.HandleException(CLASS_NAME, nameof(FeatureBorders), ex);
//                throw;
//            }
//        }

//        /// <summary>
//        /// Constructor that initializes the borders from a binary string.
//        /// String positions map to HexDirection order: NE, E, SE, SW, W, NW
//        /// </summary>
//        /// <param name="input">Binary string representing the hex matrix (6 chars)</param>
//        /// <param name="type">Border type</param>
//        public FeatureBorders(string input, BorderType type = BorderType.None)
//        {
//            try
//            {
//                Type = type;
//                SetBorderString(input ?? "000000");
//            }
//            catch (Exception ex)
//            {
//                AppService.HandleException(CLASS_NAME, nameof(FeatureBorders), ex);
//                throw;
//            }
//        }

//        #endregion

//        #region Public Methods

//        /// <summary>
//        /// Reset the borders to default value.
//        /// </summary>
//        public void Reset()
//        {
//            try
//            {
//                SetBorderString("000000");
//            }
//            catch (Exception ex)
//            {
//                AppService.HandleException(CLASS_NAME, nameof(Reset), ex);
//                throw;
//            }
//        }

//        /// <summary>
//        /// Gets the value of a specified side.
//        /// </summary>
//        /// <param name="direction">The hex direction</param>
//        /// <returns>Boolean value of the specified direction</returns>
//        public bool GetBorder(HexDirection direction)
//        {
//            try
//            {
//                return direction switch
//                {
//                    HexDirection.NE => Northeast,
//                    HexDirection.E => East,
//                    HexDirection.SE => Southeast,
//                    HexDirection.SW => Southwest,
//                    HexDirection.W => West,
//                    HexDirection.NW => Northwest,
//                    _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction")
//                };
//            }
//            catch (Exception ex)
//            {
//                AppService.HandleException(CLASS_NAME, nameof(GetBorder), ex);
//                throw;
//            }
//        }

//        /// <summary>
//        /// Sets a side to a specific value.
//        /// </summary>
//        /// <param name="direction">The hex direction</param>
//        /// <param name="value">Value to set</param>
//        public void SetBorder(HexDirection direction, bool value)
//        {
//            try
//            {
//                switch (direction)
//                {
//                    case HexDirection.NE: Northeast = value; break;
//                    case HexDirection.E: East = value; break;
//                    case HexDirection.SE: Southeast = value; break;
//                    case HexDirection.SW: Southwest = value; break;
//                    case HexDirection.W: West = value; break;
//                    case HexDirection.NW: Northwest = value; break;
//                    default: throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction");
//                }
//            }
//            catch (Exception ex)
//            {
//                AppService.HandleException(CLASS_NAME, nameof(SetBorder), ex);
//                throw;
//            }
//        }

//        /// <summary>
//        /// Sets the matrix with a 6-character binary string.
//        /// String positions map to HexDirection enum order: [0]=NE, [1]=E, [2]=SE, [3]=SW, [4]=W, [5]=NW
//        /// </summary>
//        /// <param name="input">Binary string representing the hex matrix</param>
//        public void SetBorderString(string input)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(input))
//                {
//                    input = "000000";
//                }

//                if (input.Length != 6 || !IsBinaryString(input))
//                {
//                    throw new ArgumentException("Input must be a 6-character binary string", nameof(input));
//                }

//                // Map string positions to HexDirection enum order
//                Northeast = input[0] == '1';  // HexDirection.NE = 0
//                East = input[1] == '1';       // HexDirection.E = 1
//                Southeast = input[2] == '1';  // HexDirection.SE = 2
//                Southwest = input[3] == '1';  // HexDirection.SW = 3
//                West = input[4] == '1';       // HexDirection.W = 4
//                Northwest = input[5] == '1';  // HexDirection.NW = 5
//            }
//            catch (Exception ex)
//            {
//                AppService.HandleException(CLASS_NAME, nameof(SetBorderString), ex);
//                throw;
//            }
//        }

//        /// <summary>
//        /// Converts the hex borders to a binary string representation.
//        /// String positions follow HexDirection enum order: NE, E, SE, SW, W, NW
//        /// </summary>
//        /// <returns>Binary string representing the hex border</returns>
//        public string GetBorderString()
//        {
//            try
//            {
//                return $"{(Northeast ? "1" : "0")}{(East ? "1" : "0")}{(Southeast ? "1" : "0")}{(Southwest ? "1" : "0")}{(West ? "1" : "0")}{(Northwest ? "1" : "0")}";
//            }
//            catch (Exception ex)
//            {
//                AppService.HandleException(CLASS_NAME, nameof(GetBorderString), ex);
//                return "000000";
//            }
//        }

//        /// <summary>
//        /// Checks if any of the borders are active.
//        /// </summary>
//        /// <returns>True if any border is active, false otherwise</returns>
//        public bool HasAnyBorders()
//        {
//            try
//            {
//                return Northeast || East || Southeast || Southwest || West || Northwest;
//            }
//            catch (Exception ex)
//            {
//                AppService.HandleException(CLASS_NAME, nameof(HasAnyBorders), ex);
//                return false;
//            }
//        }

//        /// <summary>
//        /// Checks if any of the borders in the given FeatureBorders object are active.
//        /// </summary>
//        /// <param name="borders">The FeatureBorders object representing the state of each border</param>
//        /// <returns>True if any of the borders are active, false otherwise</returns>
//        public static bool CheckFeatureBorders(FeatureBorders borders)
//        {
//            try
//            {
//                return borders?.HasAnyBorders() ?? false;
//            }
//            catch (Exception ex)
//            {
//                AppService.HandleException(CLASS_NAME, nameof(CheckFeatureBorders), ex);
//                return false;
//            }
//        }

//        #endregion

//        #region Private Methods

//        /// <summary>
//        /// Validates if a string is a binary string.
//        /// </summary>
//        /// <param name="input">The string to check</param>
//        /// <returns>True if the string is binary, false otherwise</returns>
//        private static bool IsBinaryString(string input)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(input)) return false;

//                foreach (char c in input)
//                {
//                    if (c != '0' && c != '1')
//                    {
//                        return false;
//                    }
//                }
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        #endregion
//    }
//}