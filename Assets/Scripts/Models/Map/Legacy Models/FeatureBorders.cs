using System;
using System.Linq;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Legacy.Map
{ 
    /// <summary>
    /// Stores hex edge information.
    /// </summary>
    public class FeatureBorders
    {
        // The border strings have the characters in this order.
        public bool Northwest { get; set; } = false;
        public bool Northeast { get; set; } = false;
        public bool East { get; set; } = false;
        public bool Southeast { get; set; } = false;
        public bool Southwest { get; set; } = false;
        public bool West { get; set; } = false;
        BorderType Type { get; set; } = BorderType.None;

        /// <summary>
        /// Constructor that initializes the borders with specific directions.
        /// </summary>
        /// <param name="NW">Northwest border value.</param>
        /// <param name="NE">Northeast border value.</param>
        /// <param name="E">East border value.</param>
        /// <param name="SE">Southeast border value.</param>
        /// <param name="SW">Southwest border value.</param>
        /// <param name="W">West border value.</param>
        public FeatureBorders(BorderType type, bool NW, bool NE, bool E, bool SE, bool SW, bool W)
        {
            Northwest = NW;
            Northeast = NE;
            East = E;
            Southeast = SE;
            Southwest = SW;
            West = W;
        }

        /// <summary>
        /// Constructor that initializes the borders with a specific type.
        /// </summary>
        /// <param name="type"></param>
        public FeatureBorders(BorderType type)
        {
            Type = type;
        }

        /// <summary>
        /// Constructor that initializes the borders from a binary string.
        /// </summary>
        /// <param name="input">Binary string representing the hex matrix.</param>
        /// <exception cref="ArgumentException">Thrown when the input is not a valid 6-character binary string.</exception>
        public FeatureBorders(string input = "000000")
        {
            SetBorderString(input);
        }

        /// <summary>
        /// Reset the borders to default value.
        /// </summary>
        public void Reset()
        {
            SetBorderString("000000");
        }

        /// <summary>
        /// Gets the value of a specified side.
        /// </summary>
        /// <param name="direction">The hex direction.</param>
        /// <returns>Boolean value of the specified direction.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the direction is invalid.</exception>
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
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "FeatureBorders.GetBorder: Error, invalid direction.")
            };
        }

        /// <summary>
        /// Sets a side to a specific value.
        /// </summary>
        /// <param name="direction">The hex direction.</param>
        /// <param name="value">Value to set.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the direction is invalid.</exception>
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
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, "FeatureBorders.SetBorder: Error, invalid direction.");
            }
        }

        /// <summary>
        /// Sets the matrix with a 6-character binary string.
        /// </summary>
        /// <param name="input">Binary string representing the hex matrix.</param>
        /// <exception cref="ArgumentException">Thrown when the input is not a valid 6-character binary string.</exception>
        public void SetBorderString(string input)
        {
            if (input.Length != 6 || !IsBinaryString(input))
            {
                throw new ArgumentException("FeatureBorders.SetMatrixString: Error, input must be a 6-character binary string.");
            }

            Northwest = input[0] == '1';
            Northeast = input[1] == '1';
            East = input[2] == '1';
            Southeast = input[3] == '1';
            Southwest = input[4] == '1';
            West = input[5] == '1';
        }

        /// <summary>
        /// Validates if a string is a binary string.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <returns>True if the string is binary; otherwise, false.</returns>
        private bool IsBinaryString(string input)
        {
            return input.All(c => c == '0' || c == '1');
        }

        /// <summary>
        /// Converts the hex borders to a binary string representation.
        /// </summary>
        /// <returns>Binary string representing the hex border.</returns>
        public string GetBorderString()
        {
            return $"{(Northwest ? "1" : "0")}{(Northeast ? "1" : "0")}{(East ? "1" : "0")}{(Southeast ? "1" : "0")}{(Southwest ? "1" : "0")}{(West ? "1" : "0")}";
        }

        /// <summary>
        /// Checks if any of the borders in the given FeatureBorders object are active.
        /// </summary>
        /// <param name="borders">The FeatureBorders object representing the state of each border.</param>
        /// <returns>Returns true if any of the borders are active; otherwise, false.</returns>
        public static bool CheckFeatureBorders(FeatureBorders borders)
        {
            // Check each border in the FeatureBorders object. 
            // If any border is active (true), return true.
            return borders.Northwest || borders.Northeast || borders.East ||
                   borders.Southeast || borders.Southwest || borders.West;
        }
    }
}