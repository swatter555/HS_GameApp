using HammerAndSickle.Models;
using System.Collections.Generic;

namespace HammerAndSickle.Utils
{
    /// <summary>
    /// Utility class for converting nationality enums to descriptive strings.
    /// Provides localized and readable nationality names for display purposes.
    /// </summary>
    public static class NationalityUtils
    {
        #region Constants

        private static readonly Dictionary<Nationality, string> NationalityDisplayNames = new()
        {
            { Nationality.USSR, "Soviet" },
            { Nationality.USA, "US" },
            { Nationality.FRG, "German" },
            { Nationality.UK, "British" },
            { Nationality.FRA, "French" },
            { Nationality.MJ, "Mujahideen" },
            { Nationality.IR, "Iranian" },
            { Nationality.IQ, "Iraqi" },
            { Nationality.SAUD, "Saudi" }
        };

        #endregion // Constants

        #region Public Methods

        /// <summary>
        /// Gets the descriptive display name for a nationality.
        /// </summary>
        /// <param name="nationality">The nationality enum value</param>
        /// <returns>The descriptive string representation</returns>
        public static string GetDisplayName(Nationality nationality)
        {
            return NationalityDisplayNames.TryGetValue(nationality, out string displayName)
                ? displayName
                : nationality.ToString();
        }

        /// <summary>
        /// Checks if a nationality has a defined display name.
        /// </summary>
        /// <param name="nationality">The nationality to check</param>
        /// <returns>True if a display name is defined</returns>
        public static bool HasDisplayName(Nationality nationality)
        {
            return NationalityDisplayNames.ContainsKey(nationality);
        }

        /// <summary>
        /// Gets all supported nationalities with their display names.
        /// </summary>
        /// <returns>Dictionary mapping nationality enums to display names</returns>
        public static IReadOnlyDictionary<Nationality, string> GetAllDisplayNames()
        {
            return NationalityDisplayNames;
        }

        #endregion // Public Methods
    }
}