using HammerAndSickle.Controllers;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HammerAndSickle.Persistence
{
    public static class SaveLoad
    {
        /// <summary>
        /// JSON serialization options to handle object references and formatting.
        /// </summary>
        private static readonly JsonSerializerOptions _opts = new()

        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true
        };

        /// <summary>
        /// Saves the current game state to a file asynchronously.
        /// </summary>
        public static async Task SaveAsync(string path)
        {
            var snap = SnapshotMapper.ToSnapshot(GameDataManager.Instance);
            await using var fs = File.Create(path);
            await JsonSerializer.SerializeAsync(fs, snap, _opts);
        }

        /// <summary>
        /// Loads the game state from a file asynchronously.
        /// </summary>
        public static async Task LoadAsync(string path)
        {
            await using var fs = File.OpenRead(path);
            var snap = await JsonSerializer.DeserializeAsync<GameStateSnapshot>(fs, _opts);
            SnapshotMapper.ApplySnapshot(snap!, GameDataManager.Instance);
        }
    }
}