using HammerAndSickle.Models;
using System.Collections.Generic;

namespace HammerAndSickle.Utils
{
    /*────────────────────────────────────────────────────────────────────────────
     NationalityUtils ─ enum→display‑name helper 
    ──────────────────────────────────────────────────────────────────────────────

    Summary
    ═══════
    • Static utility that maps Nationality enum values to short, localised display
      names for UI labels, tooltips, and save‑file human readability.
    • Eliminates scattered switch statements and string literals; single source of
      truth for human‑friendly country names.
    • Dictionary‑backed lookup ensures O(1) access and easy extension as new
      Nationality values are introduced.

    Private fields
    ══════════════
      Dictionary<Nationality, string> _displayNames  ─ enum→string lookup (static, RO)

    Public methods
    ══════════════
      string                     GetDisplayName(Nationality)      // primary lookup
      bool                       HasDisplayName(Nationality)      // existence check
      IReadOnlyDictionary<Nationality,string> GetAllDisplayNames()// expose full map

    Developer notes
    ═══════════════
    • When adding a new Nationality enum value, update _displayNames and run the
      editor‑only NameTableValidation unit test (see UnitTestingGuidelines §4.2).
    • Keep display names concise (≤ 12 chars) to avoid UI overflow on HUD panels.
    • The dictionary is intentionally read‑only after static initialisation; any
      mutation must go through a dedicated migration script and version bump.
   ────────────────────────────────────────────────────────────────────────────*/
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