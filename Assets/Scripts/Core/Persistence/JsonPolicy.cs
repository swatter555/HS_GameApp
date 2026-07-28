using System.Text.Json;
using System.Text.Json.Serialization;

namespace HammerAndSickle.Persistence
{
    /// <summary>
    /// The single source of truth for JSON serialization policy across saves and shipped content.
    ///
    /// WHY THIS EXISTS: before 2026-07-27 there were four independently-constructed
    /// <see cref="JsonSerializerOptions"/> in the project — SaveLoad, MapLoader, OOBFileLoader and
    /// MapChecksumUtility — and NONE of them registered a string enum converter. System.Text.Json writes
    /// enums as NUMBERS by default, so every WeaponType, UnitClassification, Nationality and TerrainType
    /// on disk was an ordinal. Inserting a new member in the middle of any of those enums — a routine
    /// patch for this genre — silently reinterprets every existing save AND every shipped .map/.oob:
    /// the numbers still parse, so there is no exception and no warning, and a veteran T-72 regiment
    /// quietly becomes something else. Four copies of the policy also meant the fix had to be remembered
    /// in four places. One place now.
    ///
    /// (The fourth, MapChecksumUtility, was DELETED 2026-07-28 — it had never validated anything. The
    /// checksum field stays in the .map header as the scenario editor's content fingerprint. See
    /// Claude_Project §7.1; do not restore game-side validation.)
    /// </summary>
    public static class JsonPolicy
    {
        #region Options

        /// <summary>
        /// For SAVE FILES. Requires <see cref="ReferenceHandler.Preserve"/> because the snapshot is an
        /// object GRAPH (units and leaders cross-reference), which emits $id/$ref metadata.
        /// ⚠ Never use this for content files — Preserve expects that metadata and plain content trees
        /// do not carry it.
        /// </summary>
        public static readonly JsonSerializerOptions Save = new()
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// For SHIPPED CONTENT (.map, .oob, and the manifests to come). A plain tree, no reference
        /// metadata, parsed leniently because these files are authored and hand-edited.
        /// Deliberately the union of the settings the individual loaders used to carry, so it is strictly
        /// MORE permissive than either — no file that parsed before can stop parsing now.
        /// </summary>
        public static readonly JsonSerializerOptions Content = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter() }
        };

        #endregion // Options

        // ─────────────────────────────────────────────────────────────────────────────────────────────
        // READING OLD FILES IS SAFE. JsonStringEnumConverter accepts BOTH a string and a number on read,
        // so every .map/.oob and save written before this change still loads. Writes are strings from now
        // on.
        //
        // ⚠ CONSEQUENCE WORTH HOLDING ONTO: shipped content does not actually GAIN the protection until
        // it is re-emitted with string enums. Until then the ordinal fragility is still latent in those
        // files — the reader just tolerates both forms.
        //
        // ✅ DONE for everything currently shipped (2026-07-28): the scenario editor re-exported .map and
        // .oob in name-form, and the .manifest was converted with them. Any NEW content type added here
        // must be emitted name-form from the start, and any hand-authored file must not regress to
        // ordinals — the reader will not complain, which is exactly the problem.
        // (Re-emitting does NOT invalidate the .map checksum: the editor hashes map DATA, not file text.)
        // ─────────────────────────────────────────────────────────────────────────────────────────────
    }
}
