using HammerAndSickle.Controllers;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace HammerAndSickle.Persistence
{
    public static class SaveLoad
    {
        private static readonly JsonSerializerOptions _opts = new()

        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true
        };

        public static async Task SaveAsync(string path)
        {
            var snap = SnapshotMapper.ToSnapshot(GameDataManager.Instance);
            await using var fs = File.Create(path);
            await JsonSerializer.SerializeAsync(fs, snap, _opts);
        }

        public static async Task LoadAsync(string path)
        {
            await using var fs = File.OpenRead(path);
            var snap = await JsonSerializer.DeserializeAsync<GameStateSnapshot>(fs, _opts);
            SnapshotMapper.ApplySnapshot(snap!, GameDataManager.Instance);
        }
    }
}