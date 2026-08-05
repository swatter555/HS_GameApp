using System;
using UnityEditor;
using UnityEngine;

namespace HammerAndSickle.EditorTools.Audio
{
    /// <summary>
    /// Stamps the ratified SFX import settings (HS_DesignDoc §27.7, todo_audio Phase 1.2) onto every clip
    /// under Assets/Audio/SFX/ as it is imported.
    ///
    /// ⚠ AN AssetPostprocessor RATHER THAN A PRESET, DELIBERATELY. A Preset has to be REMEMBERED — someone
    /// drops in a wav, forgets to apply it, and gets a streamed stereo clip that loads on demand, which is
    /// exactly the behaviour this whole audio pass exists to remove. It would also fail silently: the sound
    /// still plays, just late and at twice the memory. With ~80 more SFX to author, "remember to apply the
    /// preset" is a defect waiting to happen 80 times. This runs automatically and cannot be skipped.
    ///
    /// Editor-only; never ships.
    /// </summary>
    public class SfxImportSettings : AssetPostprocessor
    {
        #region Constants

        /// <summary>
        /// The one folder these settings govern. ⚠ Scoped deliberately: music, ambience and briefing
        /// narration live in StreamingAssets and are STREAMED, so decompress-on-load and force-to-mono
        /// would be actively wrong for them. The split is by ROLE — see §7.1 / Claude_Project §3.7b.
        /// </summary>
        private const string SfxFolder = "Assets/Audio/SFX/";

        #endregion // Constants

        #region Import Hooks

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(SfxFolder, StringComparison.OrdinalIgnoreCase)) return;

            if (assetImporter is not AudioImporter importer)
                return;

            // 2D UI and map blips are identical in both ears; stereo doubles memory and mixing cost for
            // nothing. There is no positional audio in the game (§27.7 / D10), so nothing wants a stereo field.
            importer.forceToMono = true;

            // Must be false, and it is the load-latency setting that matters: background loading is what
            // makes a clip arrive AFTER the click that asked for it.
            importer.loadInBackground = false;

            var settings = importer.defaultSampleSettings;

            // Fully decompressed and resident. These are short — the whole SFX set is ~3 MB at the full
            // ~85-sound inventory — so there is nothing to gain by streaming and a first-play stall to lose.
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;

            // The point of the entire Phase 1 move: the clip is in memory before anything asks to play it,
            // which is what lets the runtime load path be deleted rather than optimised.
            settings.preloadAudioData = true;

            importer.defaultSampleSettings = settings;
        }

        #endregion // Import Hooks

        #region Menu

        /// <summary>
        /// Forces a reimport of every SFX so the settings above are applied to clips that were already in
        /// the project when this postprocessor was added. Needed exactly once, but harmless to re-run.
        /// </summary>
        [MenuItem("Tools/Audio/Reimport SFX With Ratified Settings")]
        public static void ReimportAllSfx()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX" });
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[{nameof(SfxImportSettings)}] No AudioClips found under Assets/Audio/SFX.");
                return;
            }

            foreach (string guid in guids)
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);

            AssetDatabase.Refresh();
            Debug.Log($"[{nameof(SfxImportSettings)}] Reimported {guids.Length} SFX with the ratified settings " +
                      "(mono, PCM, decompress on load, preloaded, no background loading).");
        }

        #endregion // Menu
    }
}
