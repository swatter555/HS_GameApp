using System;
using System.IO;
using System.Text.Json.Serialization;
using HammerAndSickle.Services;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// JSON-serializable map header containing metadata and data integrity information.
    /// Used for map identification, versioning, and checksum validation.
    /// </summary>
    [Serializable]
    public class JsonMapHeader
    {
        #region Constants

        private const string CLASS_NAME = nameof(JsonMapHeader);

        #endregion // Constants

        #region Properties

        /// <summary>
        /// Display name of the map.
        /// </summary>
        [JsonPropertyName("mapName")]
        public string MapName { get; set; }

        /// <summary>
        /// Map size configuration (Small, Large, None).
        /// </summary>
        /// <remarks>
        /// ⚠ VESTIGIAL TAG — NOTHING MAY DERIVE GEOMETRY FROM THIS (G3, 2026-08-12). It survives only
        /// because removing it would cost a `.map` format bump, an editor change and a re-export of every
        /// map for zero functional gain — the same reasoning that kept `checksum`. Dimensions come from
        /// <see cref="MapColumns"/>/<see cref="MapRows"/> via <see cref="ResolveMapDimensions"/>; the ONLY
        /// remaining reader of this field is that method's legacy fallback for files written before those
        /// keys existed. The editor writes <c>None</c> once G1 ships in a build Bob runs.
        /// </remarks>
        [JsonPropertyName("mapConfiguration")]
        public MapConfig MapConfiguration { get; set; }

        /// <summary>
        /// Map width in hex columns. Authoritative — this is where a map's geometry lives.
        /// </summary>
        /// <remarks>
        /// ⚠ A `.map` IS SELF-DESCRIBING. Its own dimensions are not metadata about the file, they are the
        /// file; a map whose geometry can only be understood by consulting a sibling manifest is one that
        /// can be mis-paired with nothing to detect it. The editor has been writing this key on every save
        /// for months — before 2026-08-12 there was simply no property to receive it.
        /// 0 means a legacy file with no explicit dimensions; see <see cref="ResolveMapDimensions"/>.
        /// </remarks>
        [JsonPropertyName("mapColumns")]
        public int MapColumns { get; set; }

        /// <summary>Map height in hex rows. See <see cref="MapColumns"/>.</summary>
        [JsonPropertyName("mapRows")]
        public int MapRows { get; set; }

        /// <summary>
        /// Save format version for compatibility tracking.
        /// </summary>
        [JsonPropertyName("saveVersion")]
        public int SaveVersion { get; set; }

        /// <summary>
        /// Data integrity checksum for validation.
        /// </summary>
        [JsonPropertyName("checksum")]
        public string Checksum { get; set; }

        /// <summary>
        /// Creation timestamp for the JSON file.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// JSON deserialization constructor with explicit parameters for all serializable properties.
        /// System.Text.Json uses this constructor to create objects with all data available at construction time.
        /// </summary>
        [JsonConstructor]
        public JsonMapHeader(
            string mapName,
            MapConfig mapConfiguration,
            int saveVersion,
            string checksum,
            DateTime createdAt,
            int mapColumns = 0,
            int mapRows = 0)
        {
            MapName = mapName;
            MapConfiguration = mapConfiguration;
            SaveVersion = saveVersion;
            Checksum = checksum;
            CreatedAt = createdAt;
            MapColumns = mapColumns;
            MapRows = mapRows;
        }

        /// <summary>
        /// Creates a new JsonMapHeader with specified values.
        /// Used for manual header creation (not JSON deserialization).
        /// </summary>
        /// <param name="mapName">Name of the map</param>
        /// <param name="mapConfiguration">Size configuration of the map (vestigial — see the property)</param>
        /// <param name="checksum">Data integrity checksum</param>
        /// <param name="mapColumns">Map width in hex columns</param>
        /// <param name="mapRows">Map height in hex rows</param>
        /// <remarks>
        /// ⚠ DIMENSIONS ARE REQUIRED HERE, AND THAT IS THE POINT. This constructor's only caller is the SAVE
        /// path (`SnapshotMapper.ToSnapshot`), which embeds a header into the snapshot. Before 2026-08-12 it
        /// could not supply dimensions because the parameters did not exist — so a save of an explicitly-sized
        /// map wrote `mapConfiguration: None` with no columns/rows, and reloading it hit the legacy fallback
        /// with nothing to fall back to. That resurrected the exact silent-geometry failure G1 exists to kill,
        /// one save/load cycle later. Making them required parameters means the writer cannot forget again.
        /// </remarks>
        public JsonMapHeader(string mapName, MapConfig mapConfiguration, string checksum,
            int mapColumns, int mapRows)
        {
            try
            {
                MapName = mapName ?? throw new ArgumentNullException(nameof(mapName));
                MapConfiguration = mapConfiguration;
                SaveVersion = GameData.CurrentMapDataVersion;
                Checksum = checksum ?? throw new ArgumentNullException(nameof(checksum));
                CreatedAt = DateTime.UtcNow;
                MapColumns = mapColumns;
                MapRows = mapRows;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(JsonMapHeader), ex);
                throw;
            }
        }

        #endregion // Constructors

        #region Public Methods

        /// <summary>
        /// Validates the header data for consistency and completeness.
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(MapName))
                {
                    AppService.CaptureUiMessage("JsonMapHeader validation failed: MapName is null or empty");
                    return false;
                }

                if (!Enum.IsDefined(typeof(MapConfig), MapConfiguration))
                {
                    AppService.CaptureUiMessage($"JsonMapHeader validation failed: Invalid MapSize value {MapConfiguration}");
                    return false;
                }

                if (SaveVersion <= 0)
                {
                    AppService.CaptureUiMessage($"JsonMapHeader validation failed: Invalid SaveVersion {SaveVersion}");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Checksum))
                {
                    AppService.CaptureUiMessage("JsonMapHeader validation failed: Checksum is null or empty");
                    return false;
                }

                if (CreatedAt == default)
                {
                    AppService.CaptureUiMessage("JsonMapHeader validation failed: CreatedAt is default value");
                    return false;
                }

                /* Dimensions: either BOTH absent (a legacy file, resolved from MapConfiguration later) or
                 * BOTH at/above the 10x10 floor. The mixed and nonsense cases are rejected here rather than
                 * at ResolveMapDimensions, so a corrupt header fails validation with a clear reason instead
                 * of throwing later from inside the load. */
                bool legacyAbsent = MapColumns == 0 && MapRows == 0;
                bool explicitValid = MapColumns >= 10 && MapRows >= 10;

                if (!legacyAbsent && !explicitValid)
                {
                    AppService.CaptureUiMessage(
                        $"JsonMapHeader validation failed: map dimensions {MapColumns}x{MapRows} are " +
                        "invalid — supply both (>= 10 each) or neither.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsValid), ex);
                return false;
            }
        }

        /// <summary>
        /// THE SINGLE AUTHORITY ON A MAP'S DIMENSIONS. Returns the explicit
        /// <see cref="MapColumns"/>x<see cref="MapRows"/> when present, falls back to the legacy
        /// <see cref="MapConfiguration"/> table (with a loud warning) for files written before those keys
        /// existed, and THROWS when neither can answer.
        /// </summary>
        /// <exception cref="InvalidDataException">
        /// The header carries no usable geometry — no explicit dimensions and no legacy configuration to
        /// derive them from.
        /// </exception>
        /// <remarks>
        /// ⚠ ONE SPELLING, CALLED BY EVERY READER (`MapLoader` and `SnapshotMapper`). The same logic written
        /// twice at two call sites is how the last divergence went unnoticed for months; this is the same
        /// rule as `JsonPolicy` owning serializer options.
        ///
        /// ⚠ IT THROWS RATHER THAN GUESSING, and that is the entire point of G1. The failure this replaces
        /// was SILENT: a 44x21 map built a 32x21 `HexMap`, `SetHexAt` refused every hex past column 31 with
        /// a UI message and a `false` return, the loader counted those into a `failCount` it then discarded,
        /// and the map loaded, played, and was wrong. A map whose geometry cannot be established must fail
        /// to load, not load at a size nobody chose.
        ///
        /// ⚠ THE 10x10 FLOOR IS THE PROJECT'S EXISTING INVARIANT, not a new constant — `HexMap`'s explicit
        /// constructor already throws below it and `ScenarioManifest.IsValid` already tests it.
        /// </remarks>
        public UnityEngine.Vector2Int ResolveMapDimensions()
        {
            // The normal path: the file says how big it is.
            if (MapColumns >= 10 && MapRows >= 10)
                return new UnityEngine.Vector2Int(MapColumns, MapRows);

            /* Legacy path: a `.map` written before mapColumns/mapRows existed. The two blessed sizes are
             * inlined HERE, deliberately, rather than living on as GameData constants — as project-level
             * constants they read as two sanctioned map sizes, which is exactly the abandoned premise this
             * whole pass is removing. In this method they are what they actually are: a compatibility
             * table for old files. */
            if (MapColumns == 0 && MapRows == 0)
            {
                UnityEngine.Vector2Int legacy = MapConfiguration switch
                {
                    MapConfig.Small => new UnityEngine.Vector2Int(32, 21),
                    MapConfig.Large => new UnityEngine.Vector2Int(32, 42),
                    _ => UnityEngine.Vector2Int.zero
                };

                if (legacy != UnityEngine.Vector2Int.zero)
                {
                    UnityEngine.Debug.LogWarning(
                        $"{CLASS_NAME}: '{MapName}' carries no mapColumns/mapRows; falling back to " +
                        $"MapConfig.{MapConfiguration} = {legacy.x}x{legacy.y}. Re-export it from the " +
                        "Scenario Editor — this fallback is for pre-2026-08 files only.");
                    return legacy;
                }
            }

            /* Everything else is unresolvable, and each case is a real defect rather than an old file:
             *   - None/undefined with no dimensions — including a SAVE written before the writer supplied
             *     them, which is precisely the gap that made this method necessary;
             *   - one dimension set and not the other, or a positive value under the 10x10 floor. */
            throw new InvalidDataException(
                $"Map '{MapName}' carries no usable dimensions: mapColumns={MapColumns}, mapRows={MapRows}, " +
                $"mapConfiguration={MapConfiguration}. A map must state its own geometry (>= 10x10); " +
                "regenerate it from the Scenario Editor.");
        }

        /// <summary>
        /// Updates the checksum value.
        /// </summary>
        /// <param name="newChecksum">New checksum value</param>
        public void UpdateChecksum(string newChecksum)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newChecksum))
                {
                    throw new ArgumentException("Checksum cannot be null or empty", nameof(newChecksum));
                }

                Checksum = newChecksum;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(UpdateChecksum), ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the current save version constant.
        /// </summary>
        /// <returns>Current save version</returns>
        public static int GetCurrentSaveVersion()
        {
            return GameData.CurrentMapDataVersion;
        }

        /// <summary>
        /// Checks if this header version is compatible with the current version.
        /// </summary>
        /// <returns>True if compatible, false otherwise</returns>
        public bool IsCompatibleVersion()
        {
            try
            {
                // For now, only exact version matches are compatible
                // Future versions can implement backward compatibility logic
                return SaveVersion == GameData.CurrentMapDataVersion;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsCompatibleVersion), ex);
                return false;
            }
        }

        /// <summary>
        /// Creates a summary string of the header information.
        /// </summary>
        /// <returns>Header summary</returns>
        public string GetSummary()
        {
            try
            {
                string dims = MapColumns > 0 && MapRows > 0 ? $"{MapColumns}x{MapRows}" : $"legacy:{MapConfiguration}";
                return $"'{MapName}' ({dims}) v{SaveVersion} created {CreatedAt:yyyy-MM-dd HH:mm} UTC";
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetSummary), ex);
                return "JsonMapHeader: Error generating summary";
            }
        }

        #endregion // Public Methods
    }
}
