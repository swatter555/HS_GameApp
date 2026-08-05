using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HammerAndSickle.Audio;
using HammerAndSickle.Controllers;
using UnityEditor;
using UnityEngine;

namespace HammerAndSickle.EditorTools.Audio
{
    /// <summary>
    /// Creates, populates and audits the <see cref="AudioCatalog"/>.
    ///
    ///   Tools/Audio/Create Or Update Audio Catalog — scan Assets/Audio/SFX and fill in rows by name
    ///   Tools/Audio/Audit Catalog                  — report what is unbacked, empty or duplicated
    ///
    /// ⚠ THE AUDIT EXISTS BECAUSE A CATALOG FAILS SILENTLY. A sound with no row is a no-op by design
    /// (§27.7.5.2), which is what makes it safe to wire call sites ahead of the audio — but it also means
    /// a genuine authoring mistake, a typo'd filename or an unassigned clip slot, is indistinguishable
    /// from "not recorded yet" without a detector. Same reasoning as Tools/UI/Audit Button Wiring.
    /// </summary>
    public static class AudioCatalogTools
    {
        #region Constants

        private const string SfxFolder = "Assets/Audio/SFX";
        private const string CatalogDir = "Assets/Resources/Audio";
        private const string CatalogPath = CatalogDir + "/AudioCatalog.asset";
        private const string FilePrefix = "SFX_";

        #endregion // Constants

        #region Create / Update

        /// <summary>
        /// Creates the catalog if absent, then adds a row for every clip whose filename maps to a
        /// SoundEffect member. Existing rows are LEFT ALONE — this never overwrites tuning you have set.
        /// </summary>
        /// <remarks>
        /// NAMING CONVENTION: <c>SFX_&lt;SoundEffectName&gt;.wav</c>, with optional <c>_1</c>, <c>_2</c>
        /// suffixes for variants of the same sound, which are grouped into one row's clip array.
        /// </remarks>
        [MenuItem("Tools/Audio/Create Or Update Audio Catalog")]
        public static void CreateOrUpdate()
        {
            Directory.CreateDirectory(CatalogDir);

            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
            bool created = catalog == null;
            if (created)
            {
                catalog = ScriptableObject.CreateInstance<AudioCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            SerializedProperty entries = so.FindProperty("entries");

            var existing = new HashSet<int>();
            for (int i = 0; i < entries.arraySize; i++)
                existing.Add(entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex);

            // Group clips by the SoundEffect their filename names.
            var byEffect = new Dictionary<GameAudioManager.SoundEffect, List<AudioClip>>();
            var unmatched = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SfxFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (!name.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase)) { unmatched.Add(name); continue; }

                string bare = name.Substring(FilePrefix.Length);
                // Strip a trailing _<digits> variant suffix.
                int us = bare.LastIndexOf('_');
                if (us > 0 && int.TryParse(bare.Substring(us + 1), out _)) bare = bare.Substring(0, us);

                if (!Enum.TryParse(bare, out GameAudioManager.SoundEffect id) || id == GameAudioManager.SoundEffect.None)
                {
                    unmatched.Add(name);
                    continue;
                }

                if (!byEffect.TryGetValue(id, out var list)) byEffect[id] = list = new List<AudioClip>();
                list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }

            int added = 0;
            foreach (var kvp in byEffect.OrderBy(k => (int)k.Key))
            {
                if (existing.Contains((int)kvp.Key)) continue;

                entries.arraySize++;
                SerializedProperty row = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                row.FindPropertyRelative("id").enumValueIndex = (int)kvp.Key;
                row.FindPropertyRelative("volume").floatValue = 1f;
                row.FindPropertyRelative("pitchVariation").floatValue = 0f;
                row.FindPropertyRelative("minRetriggerSeconds").floatValue = 0f;

                SerializedProperty clips = row.FindPropertyRelative("clips");
                clips.arraySize = kvp.Value.Count;
                for (int i = 0; i < kvp.Value.Count; i++)
                    clips.GetArrayElementAtIndex(i).objectReferenceValue = kvp.Value[i];

                added++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AudioCatalog] {(created ? "Created" : "Updated")} {CatalogPath}: " +
                      $"{added} row(s) added, {entries.arraySize} total. Existing rows untouched.");

            if (unmatched.Count > 0)
                Debug.LogWarning($"[AudioCatalog] {unmatched.Count} clip(s) did not match a SoundEffect member " +
                                 $"and were SKIPPED: {string.Join(", ", unmatched)}. Expected SFX_<SoundEffectName>[_n].wav.");

            Selection.activeObject = catalog;
        }

        #endregion // Create / Update

        #region Audit

        [MenuItem("Tools/Audio/Audit Catalog")]
        public static void Audit()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[AudioCatalog] No catalog at {CatalogPath}. Run Tools/Audio/Create Or Update Audio Catalog.");
                return;
            }

            var seen = new HashSet<GameAudioManager.SoundEffect>();
            var duplicates = new List<string>();
            var empty = new List<string>();

            foreach (var e in catalog.Entries)
            {
                if (e == null) continue;
                if (!seen.Add(e.id)) duplicates.Add(e.id.ToString());
                if (!e.HasClip) empty.Add(e.id.ToString());
            }

            var unbacked = Enum.GetValues(typeof(GameAudioManager.SoundEffect))
                .Cast<GameAudioManager.SoundEffect>()
                .Where(id => id != GameAudioManager.SoundEffect.None && !seen.Contains(id))
                .Select(id => id.ToString())
                .ToList();

            Debug.Log($"[AudioCatalog] {catalog.Entries.Count} row(s); {seen.Count} distinct sound(s) backed.");

            // ⚠ Unbacked is INFO, not an error: the enum deliberately runs ahead of the audio.
            if (unbacked.Count > 0)
                Debug.Log($"[AudioCatalog] {unbacked.Count} SoundEffect member(s) have no row yet (silent by design): " +
                          string.Join(", ", unbacked));

            // These two ARE mistakes — a row that exists but cannot make a sound, or two rows fighting.
            if (empty.Count > 0)
                Debug.LogWarning($"[AudioCatalog] {empty.Count} row(s) have NO CLIP assigned — these look wired but are silent: " +
                                 string.Join(", ", empty));
            if (duplicates.Count > 0)
                Debug.LogWarning($"[AudioCatalog] Duplicate row(s) — the FIRST wins, later ones never play: " +
                                 string.Join(", ", duplicates));
        }

        #endregion // Audit
    }
}
