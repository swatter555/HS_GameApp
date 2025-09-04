using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;

namespace HammerAndSickle.Utils
{
    /// <summary>
    /// Static utility class for calculating and validating map data checksums.
    /// Uses SHA256 hashing of serialized hex data for data integrity verification.
    /// </summary>
    public static class MapChecksumUtility
    {
        #region Constants
        private const string CLASS_NAME = nameof(MapChecksumUtility);
        #endregion

        #region Public Methods
        /// <summary>
        /// Calculates a SHA256 checksum for an array of HexTile objects.
        /// </summary>
        /// <param name="hexes">Array of hex data to checksum</param>
        /// <returns>Hexadecimal string representation of the checksum</returns>
        public static string CalculateChecksum(HexTile[] hexes)
        {
            try
            {
                if (hexes == null)
                {
                    throw new ArgumentNullException(nameof(hexes));
                }

                // Create JSON options for consistent serialization
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false, // Compact format for checksum consistency
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Serialize hex array to JSON bytes
                byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(hexes, options);

                // Calculate SHA256 hash
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(jsonBytes);

                    // Convert to hexadecimal string
                    var sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }

                    string checksum = sb.ToString();
                    AppService.CaptureUiMessage($"Calculated checksum for {hexes.Length} hexes: {checksum[..8]}...");
                    return checksum;
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CalculateChecksum), ex);
                throw;
            }
        }

        /// <summary>
        /// Validates the checksum of JsonMapData against its hex data.
        /// </summary>
        /// <param name="mapData">Map data to validate</param>
        /// <returns>True if checksum is valid, false otherwise</returns>
        public static bool ValidateChecksum(JsonMapData mapData)
        {
            try
            {
                if (mapData == null)
                {
                    throw new ArgumentNullException(nameof(mapData));
                }

                if (mapData.Header == null)
                {
                    AppService.CaptureUiMessage("Checksum validation failed: Header is null");
                    return false;
                }

                if (mapData.Hexes == null)
                {
                    AppService.CaptureUiMessage("Checksum validation failed: Hexes array is null");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(mapData.Header.Checksum))
                {
                    AppService.CaptureUiMessage("Checksum validation failed: Header checksum is null or empty");
                    return false;
                }

                // Calculate current checksum
                string calculatedChecksum = CalculateChecksum(mapData.Hexes);

                // Compare with stored checksum
                bool isValid = string.Equals(calculatedChecksum, mapData.Header.Checksum, StringComparison.OrdinalIgnoreCase);

                if (isValid)
                {
                    AppService.CaptureUiMessage($"Checksum validation passed for map '{mapData.Header.MapName}'");
                }
                else
                {
                    AppService.CaptureUiMessage($"Checksum validation FAILED for map '{mapData.Header.MapName}'");
                    AppService.CaptureUiMessage($"Expected: {mapData.Header.Checksum[..8]}..., Calculated: {calculatedChecksum[..8]}...");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateChecksum), ex);
                return false;
            }
        }

        /// <summary>
        /// Generates and updates the checksum for JsonMapData.
        /// </summary>
        /// <param name="mapData">Map data to update with new checksum</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool UpdateChecksum(JsonMapData mapData)
        {
            try
            {
                if (mapData == null)
                {
                    throw new ArgumentNullException(nameof(mapData));
                }

                if (mapData.Header == null)
                {
                    AppService.CaptureUiMessage("Checksum update failed: Header is null");
                    return false;
                }

                if (mapData.Hexes == null)
                {
                    AppService.CaptureUiMessage("Checksum update failed: Hexes array is null");
                    return false;
                }

                // Calculate new checksum
                string newChecksum = CalculateChecksum(mapData.Hexes);

                // Update header
                mapData.Header.UpdateChecksum(newChecksum);

                AppService.CaptureUiMessage($"Updated checksum for map '{mapData.Header.MapName}': {newChecksum[..8]}...");
                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(UpdateChecksum), ex);
                return false;
            }
        }

        /// <summary>
        /// Calculates a quick checksum for performance comparison (uses MD5).
        /// Not recommended for security purposes, but faster for large datasets.
        /// </summary>
        /// <param name="hexes">Array of hex data to checksum</param>
        /// <returns>Hexadecimal string representation of the MD5 checksum</returns>
        public static string CalculateQuickChecksum(HexTile[] hexes)
        {
            try
            {
                if (hexes == null)
                {
                    throw new ArgumentNullException(nameof(hexes));
                }

                // Create JSON options for consistent serialization
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Serialize hex array to JSON bytes
                byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(hexes, options);

                // Calculate MD5 hash (faster but less secure)
                using (var md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(jsonBytes);

                    // Convert to hexadecimal string
                    var sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }

                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CalculateQuickChecksum), ex);
                throw;
            }
        }

        /// <summary>
        /// Gets information about the checksum algorithm being used.
        /// </summary>
        /// <returns>Algorithm information string</returns>
        public static string GetAlgorithmInfo()
        {
            return "SHA256 - Cryptographic hash function producing 64-character hexadecimal checksums";
        }
        #endregion
    }
}