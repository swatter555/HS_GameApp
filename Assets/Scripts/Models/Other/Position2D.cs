using System;
using System.Text.Json.Serialization;
using UnityEngine;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Serializable 2D coordinate structure that provides seamless interoperability 
    /// with Unity Vector2 and Vector2Int while supporting binary serialization for save/load operations.
    /// </summary>
    [Serializable]
    public struct Position2D : IEquatable<Position2D>
    {
        #region Constants

        private const float EPSILON = 1e-5f;

        #endregion //Constants

        #region Fields

        /// <summary>
        /// X component of the coordinate.
        /// </summary>
        public float x;

        /// <summary>
        /// Y component of the coordinate.
        /// </summary>
        public float y;

        #endregion //Fields

        #region Static Properties

        /// <summary>
        /// Position2D with components (0, 0).
        /// </summary>
        public static Position2D Zero => new(0f, 0f);

        /// <summary>
        /// Position2D with components (1, 1).
        /// </summary>
        public static Position2D One => new(1f, 1f);

        /// <summary>
        /// Position2D with components (0, 1).
        /// </summary>
        public static Position2D Up => new(0f, 1f);

        /// <summary>
        /// Position2D with components (0, -1).
        /// </summary>
        public static Position2D Down => new(0f, -1f);

        /// <summary>
        /// Position2D with components (-1, 0).
        /// </summary>
        public static Position2D Left => new(-1f, 0f);

        /// <summary>
        /// Position2D with components (1, 0).
        /// </summary>
        public static Position2D Right => new(1f, 0f);

        #endregion //Static Properties

        #region Properties

        /// <summary>
        /// Gets the magnitude (length) of this coordinate vector.
        /// </summary>
        [JsonIgnore]
        public readonly float Magnitude => Mathf.Sqrt(x * x + y * y);

        /// <summary>
        /// Gets the squared magnitude of this coordinate vector.
        /// More efficient than magnitude when you only need to compare lengths.
        /// </summary>
        [JsonIgnore]
        public readonly float SqrMagnitude => x * x + y * y;

        /// <summary>
        /// Gets a unit vector in the same direction as this coordinate.
        /// Returns Zero if this vector has zero length.
        /// </summary>
        [JsonIgnore]
        public readonly Position2D Normalized
        {
            get
            {
                float mag = Magnitude;
                return mag > EPSILON ? new Position2D(x / mag, y / mag) : Zero;
            }
        }

        /// <summary>
        /// Gets the integer x component, rounded to nearest integer.
        /// </summary>
        [JsonIgnore]
        public readonly int IntX => Mathf.RoundToInt(x);

        /// <summary>
        /// Gets the integer y component, rounded to nearest integer.
        /// </summary>
        [JsonIgnore]
        public readonly int IntY => Mathf.RoundToInt(y);

        #endregion //Properties

        #region Constructors

        /// <summary>
        /// Creates a new Position2D with the specified x and y components.
        /// </summary>
        /// <param name="x">The x component</param>
        /// <param name="y">The y component</param>
        public Position2D(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Creates a new Position2D with both components set to the same value.
        /// </summary>
        /// <param name="value">Value for both x and y components</param>
        public Position2D(float value)
        {
            this.x = value;
            this.y = value;
        }

        /// <summary>
        /// Creates a new Position2D from integer components.
        /// </summary>
        /// <param name="x">The x component</param>
        /// <param name="y">The y component</param>
        public Position2D(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Creates a new Position2D with both components set to the same integer value.
        /// </summary>
        /// <param name="value">Integer value for both x and y components</param>
        public Position2D(int value)
        {
            this.x = value;
            this.y = value;
        }

        #endregion //Constructors

        #region Conversion Operators

        /// <summary>
        /// Implicitly converts a Unity Vector2 to a Position2D.
        /// </summary>
        /// <param name="vector">The Vector2 to convert</param>
        /// <returns>A new Position2D with the same x and y values</returns>
        public static implicit operator Position2D(Vector2 vector)
        {
            return new Position2D(vector.x, vector.y);
        }

        /// <summary>
        /// Implicitly converts a Position2D to a Unity Vector2.
        /// </summary>
        /// <param name="coord">The Position2D to convert</param>
        /// <returns>A new Vector2 with the same x and y values</returns>
        public static implicit operator Vector2(Position2D coord)
        {
            return new Vector2(coord.x, coord.y);
        }

        /// <summary>
        /// Implicitly converts a Unity Vector2Int to a Position2D.
        /// </summary>
        /// <param name="vector">The Vector2Int to convert</param>
        /// <returns>A new Position2D with the same x and y values</returns>
        public static implicit operator Position2D(Vector2Int vector)
        {
            return new Position2D(vector.x, vector.y);
        }

        /// <summary>
        /// Explicitly converts a Position2D to a Unity Vector2Int.
        /// Components are rounded to nearest integers.
        /// </summary>
        /// <param name="coord">The Position2D to convert</param>
        /// <returns>A new Vector2Int with rounded x and y values</returns>
        public static explicit operator Vector2Int(Position2D coord)
        {
            return new Vector2Int(Mathf.RoundToInt(coord.x), Mathf.RoundToInt(coord.y));
        }

        #endregion //Conversion Operators

        #region Arithmetic Operators

        /// <summary>
        /// Adds two coordinates component-wise.
        /// </summary>
        public static Position2D operator +(Position2D a, Position2D b)
        {
            return new Position2D(a.x + b.x, a.y + b.y);
        }

        /// <summary>
        /// Subtracts one coordinate from another component-wise.
        /// </summary>
        public static Position2D operator -(Position2D a, Position2D b)
        {
            return new Position2D(a.x - b.x, a.y - b.y);
        }

        /// <summary>
        /// Negates a coordinate (flips direction).
        /// </summary>
        public static Position2D operator -(Position2D coord)
        {
            return new Position2D(-coord.x, -coord.y);
        }

        /// <summary>
        /// Multiplies a coordinate by a scalar value.
        /// </summary>
        public static Position2D operator *(Position2D coord, float scalar)
        {
            return new Position2D(coord.x * scalar, coord.y * scalar);
        }

        /// <summary>
        /// Multiplies a scalar value by a coordinate.
        /// </summary>
        public static Position2D operator *(float scalar, Position2D coord)
        {
            return new Position2D(coord.x * scalar, coord.y * scalar);
        }

        /// <summary>
        /// Multiplies a coordinate by an integer scalar value.
        /// </summary>
        public static Position2D operator *(Position2D coord, int scalar)
        {
            return new Position2D(coord.x * scalar, coord.y * scalar);
        }

        /// <summary>
        /// Multiplies an integer scalar value by a coordinate.
        /// </summary>
        public static Position2D operator *(int scalar, Position2D coord)
        {
            return new Position2D(coord.x * scalar, coord.y * scalar);
        }

        /// <summary>
        /// Multiplies two coordinates component-wise.
        /// </summary>
        public static Position2D operator *(Position2D a, Position2D b)
        {
            return new Position2D(a.x * b.x, a.y * b.y);
        }

        /// <summary>
        /// Divides a coordinate by a scalar value.
        /// </summary>
        public static Position2D operator /(Position2D coord, float scalar)
        {
            return new Position2D(coord.x / scalar, coord.y / scalar);
        }

        /// <summary>
        /// Divides a coordinate by an integer scalar value.
        /// </summary>
        public static Position2D operator /(Position2D coord, int scalar)
        {
            return new Position2D(coord.x / scalar, coord.y / scalar);
        }

        /// <summary>
        /// Divides two coordinates component-wise.
        /// </summary>
        public static Position2D operator /(Position2D a, Position2D b)
        {
            return new Position2D(a.x / b.x, a.y / b.y);
        }

        #endregion //Arithmetic Operators

        #region Equality Operators

        /// <summary>
        /// Checks if two coordinates are approximately equal within floating-point tolerance.
        /// </summary>
        public static bool operator ==(Position2D a, Position2D b)
        {
            return Mathf.Abs(a.x - b.x) < EPSILON && Mathf.Abs(a.y - b.y) < EPSILON;
        }

        /// <summary>
        /// Checks if two coordinates are not approximately equal within floating-point tolerance.
        /// </summary>
        public static bool operator !=(Position2D a, Position2D b)
        {
            return !(a == b);
        }

        #endregion //Equality Operators

        #region Static Methods

        /// <summary>
        /// Calculates the distance between two coordinates.
        /// </summary>
        /// <param name="a">First coordinate</param>
        /// <param name="b">Second coordinate</param>
        /// <returns>The distance between the coordinates</returns>
        public static float Distance(Position2D a, Position2D b)
        {
            float deltaX = a.x - b.x;
            float deltaY = a.y - b.y;
            return Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        /// <summary>
        /// Calculates the squared distance between two coordinates.
        /// More efficient than Distance when you only need to compare distances.
        /// </summary>
        /// <param name="a">First coordinate</param>
        /// <param name="b">Second coordinate</param>
        /// <returns>The squared distance between the coordinates</returns>
        public static float SqrDistance(Position2D a, Position2D b)
        {
            float deltaX = a.x - b.x;
            float deltaY = a.y - b.y;
            return deltaX * deltaX + deltaY * deltaY;
        }

        /// <summary>
        /// Calculates the Manhattan distance between two coordinates.
        /// Useful for grid-based calculations.
        /// </summary>
        /// <param name="a">First coordinate</param>
        /// <param name="b">Second coordinate</param>
        /// <returns>The Manhattan distance between the coordinates</returns>
        public static float ManhattanDistance(Position2D a, Position2D b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        /// <summary>
        /// Calculates the dot product of two coordinates.
        /// </summary>
        /// <param name="a">First coordinate</param>
        /// <param name="b">Second coordinate</param>
        /// <returns>The dot product</returns>
        public static float Dot(Position2D a, Position2D b)
        {
            return a.x * b.x + a.y * b.y;
        }

        /// <summary>
        /// Linearly interpolates between two coordinates.
        /// </summary>
        /// <param name="a">Start coordinate</param>
        /// <param name="b">End coordinate</param>
        /// <param name="t">Interpolation factor (0 = a, 1 = b)</param>
        /// <returns>Interpolated coordinate</returns>
        public static Position2D Lerp(Position2D a, Position2D b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Position2D(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t
            );
        }

        /// <summary>
        /// Linearly interpolates between two coordinates without clamping t.
        /// </summary>
        /// <param name="a">Start coordinate</param>
        /// <param name="b">End coordinate</param>
        /// <param name="t">Interpolation factor</param>
        /// <returns>Interpolated coordinate</returns>
        public static Position2D LerpUnclamped(Position2D a, Position2D b, float t)
        {
            return new Position2D(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t
            );
        }

        /// <summary>
        /// Returns a coordinate with the minimum x and y components from two coordinates.
        /// </summary>
        public static Position2D Min(Position2D a, Position2D b)
        {
            return new Position2D(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
        }

        /// <summary>
        /// Returns a coordinate with the maximum x and y components from two coordinates.
        /// </summary>
        public static Position2D Max(Position2D a, Position2D b)
        {
            return new Position2D(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>
        /// Clamps a coordinate to be within the specified minimum and maximum bounds.
        /// </summary>
        public static Position2D Clamp(Position2D value, Position2D min, Position2D max)
        {
            return new Position2D(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y)
            );
        }

        /// <summary>
        /// Rounds a coordinate to the nearest integer values.
        /// </summary>
        public static Position2D Round(Position2D coord)
        {
            return new Position2D(Mathf.RoundToInt(coord.x), Mathf.RoundToInt(coord.y));
        }

        /// <summary>
        /// Floors a coordinate to the nearest lower integer values.
        /// </summary>
        public static Position2D Floor(Position2D coord)
        {
            return new Position2D(Mathf.FloorToInt(coord.x), Mathf.FloorToInt(coord.y));
        }

        /// <summary>
        /// Ceils a coordinate to the nearest higher integer values.
        /// </summary>
        public static Position2D Ceil(Position2D coord)
        {
            return new Position2D(Mathf.CeilToInt(coord.x), Mathf.CeilToInt(coord.y));
        }

        #endregion //Static Methods

        #region Instance Methods

        /// <summary>
        /// Normalizes this coordinate to have a magnitude of 1.
        /// If this coordinate has zero length, it remains unchanged.
        /// </summary>
        public void Normalize()
        {
            float mag = Magnitude;
            if (mag > EPSILON)
            {
                x /= mag;
                y /= mag;
            }
        }

        /// <summary>
        /// Sets the x and y components of this coordinate.
        /// </summary>
        /// <param name="newX">New x component</param>
        /// <param name="newY">New y component</param>
        public void Set(float newX, float newY)
        {
            x = newX;
            y = newY;
        }

        /// <summary>
        /// Sets the x and y components of this coordinate from integer values.
        /// </summary>
        /// <param name="newX">New x component</param>
        /// <param name="newY">New y component</param>
        public void Set(int newX, int newY)
        {
            x = newX;
            y = newY;
        }

        /// <summary>
        /// Scales this coordinate by the given scale factor.
        /// </summary>
        /// <param name="scale">Scale factor to apply</param>
        public void Scale(float scale)
        {
            x *= scale;
            y *= scale;
        }

        /// <summary>
        /// Scales this coordinate by an integer scale factor.
        /// </summary>
        /// <param name="scale">Integer scale factor to apply</param>
        public void Scale(int scale)
        {
            x *= scale;
            y *= scale;
        }

        /// <summary>
        /// Scales this coordinate by another coordinate component-wise.
        /// </summary>
        /// <param name="scale">Scale coordinate</param>
        public void Scale(Position2D scale)
        {
            x *= scale.x;
            y *= scale.y;
        }

        /// <summary>
        /// Converts this coordinate to a Vector2Int by rounding components to nearest integers.
        /// </summary>
        /// <returns>A Vector2Int with rounded x and y values</returns>
        public readonly Vector2Int ToVector2Int()
        {
            return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        }

        /// <summary>
        /// Converts this coordinate to a Vector2Int by flooring components.
        /// </summary>
        /// <returns>A Vector2Int with floored x and y values</returns>
        public readonly Vector2Int ToVector2IntFloor()
        {
            return new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
        }

        /// <summary>
        /// Converts this coordinate to a Vector2Int by ceiling components.
        /// </summary>
        /// <returns>A Vector2Int with ceiling x and y values</returns>
        public readonly Vector2Int ToVector2IntCeil()
        {
            return new Vector2Int(Mathf.CeilToInt(x), Mathf.CeilToInt(y));
        }

        #endregion //Instance Methods

        #region Object Overrides

        /// <summary>
        /// Determines whether this coordinate equals another coordinate.
        /// </summary>
        /// <param name="other">The coordinate to compare with</param>
        /// <returns>True if the coordinates are approximately equal</returns>
        public readonly bool Equals(Position2D other)
        {
            return this == other;
        }

        /// <summary>
        /// Determines whether this coordinate equals another object.
        /// </summary>
        /// <param name="obj">The object to compare with</param>
        /// <returns>True if the object is a Position2D and approximately equal to this one</returns>
        public readonly override bool Equals(object obj)
        {
            return obj is Position2D other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for this coordinate.
        /// </summary>
        /// <returns>A hash code</returns>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }

        /// <summary>
        /// Returns a formatted string representation of this coordinate.
        /// </summary>
        /// <returns>String in format "(x, y)"</returns>
        public readonly override string ToString()
        {
            return $"({x:F2}, {y:F2})";
        }

        /// <summary>
        /// Returns a formatted string representation of this coordinate with specified precision.
        /// </summary>
        /// <param name="format">Numeric format string</param>
        /// <returns>Formatted string representation</returns>
        public readonly string ToString(string format)
        {
            return $"({x.ToString(format)}, {y.ToString(format)})";
        }

        #endregion //Object Overrides
    }
}