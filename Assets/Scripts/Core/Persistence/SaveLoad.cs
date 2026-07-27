using HammerAndSickle.Controllers;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace HammerAndSickle.Persistence
{
    public static class SaveLoad
    {
        /// <summary>
        /// Serialization policy lives in <see cref="JsonPolicy"/> — see the note there on why enums must
        /// persist by NAME. Do not reintroduce a local options object.
        /// </summary>
        private static JsonSerializerOptions Opts => JsonPolicy.Save;

        /// <summary>
        /// Saves the current game state to a file asynchronously.
        /// </summary>
        public static async Task SaveAsync(string path)
        {
            var snap = SnapshotMapper.ToSnapshot(GameDataManager.Instance);
            await using var fs = File.Create(path);
            await JsonSerializer.SerializeAsync(fs, snap, Opts);
        }

        /// <summary>
        /// Loads the game state from a file asynchronously.
        /// </summary>
        public static async Task LoadAsync(string path)
        {
            await using var fs = File.OpenRead(path);
            var snap = await JsonSerializer.DeserializeAsync<GameStateSnapshot>(fs, Opts);
            SnapshotMapper.ApplySnapshot(snap!, GameDataManager.Instance);
        }
    }
}