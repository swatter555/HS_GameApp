using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HammerAndSickle.Audio;
using UnityEditor;
using UnityEngine;
using SoundEffect = HammerAndSickle.Controllers.GameAudioManager.SoundEffect;

namespace HammerAndSickle.EditorTools.Audio
{
    /// <summary>
    /// Tools/Audio/Audio Catalog Editor — a drag-and-drop authoring surface for the <see cref="AudioCatalog"/>.
    ///
    /// ⚠ THE LIST IS DRIVEN BY THE ENUM, NOT BY THE CATALOG'S ROWS. Every <see cref="SoundEffect"/> member
    /// gets a line whether or not it has a row, because the interesting question at this stage of the
    /// project is "which of the ~85 planned sounds still have no audio?" — and the default Inspector, which
    /// can only show rows that already exist, cannot answer it. Dropping a clip on a member CREATES its row.
    ///
    /// ⚠ IT MOVES STRAY FILES ON PURPOSE. Import settings are PATH-GATED: SfxImportSettings stamps
    /// mono/PCM/preload on anything under Assets/Audio/SFX and nothing else, so a clip assigned from
    /// somewhere else in the project would play correctly but late and at double memory, with no error
    /// anywhere. That is the exact silent failure the postprocessor was chosen over a Preset to avoid, and
    /// a drag-and-drop tool is a brand new way to reintroduce it. Anything dropped from outside the folder
    /// is therefore moved and renamed to the SFX_&lt;SoundEffectName&gt; convention — which also keeps this
    /// window and <see cref="AudioCatalogTools.CreateOrUpdate"/> agreeing about names instead of drifting.
    ///
    /// ⚠ EVERY MUTATION IS DEFERRED TO THE END OF THE FRAME. Adding a row or a clip changes the control
    /// count, and doing that mid-layout produces the "Mismatched LayoutGroup" error; worse, moving a file
    /// triggers an import, whose OnProjectChange would rebuild the SerializedObject underneath a
    /// half-finished write. Drops and removals therefore record what to do and are applied after the GUI
    /// pass, with the file work finished BEFORE the catalog is touched.
    ///
    /// ⚠ THE ROW CACHE IS A PERFORMANCE REQUIREMENT, NOT A TIDINESS ONE. FindPropertyRelative and
    /// GetArrayElementAtIndex are managed→native calls that allocate on every use, and the first cut of
    /// this window called them in nested loops — a full scan of the entries array per sound in the header,
    /// then four more per row — which is O(sounds × entries) EVERY GUI PASS, twice a frame. At 24 sounds
    /// it was already visibly sluggish; at the ~85 in the inventory it would have been unusable. The array
    /// is now walked ONCE per pass into <see cref="RowInfo"/>, and everything drawn reads that.
    /// </summary>
    public class AudioCatalogWindow : EditorWindow
    {
        #region Constants

        private const string SfxFolder = "Assets/Audio/SFX";
        private const string CatalogDir = "Assets/Resources/Audio";
        private const string CatalogPath = CatalogDir + "/AudioCatalog.asset";
        private const string FilePrefix = "SFX_";

        private static readonly string[] AudioExtensions = { ".wav", ".ogg", ".aif", ".aiff", ".mp3" };

        /* Built once. Enum.GetValues is reflection and allocates, and enum ToString() is slow in Mono —
         * both were previously running per row per GUI pass. */
        private static readonly SoundEffect[] Sounds;
        private static readonly string[] SoundNames;
        private static readonly GUIContent[] FileNameLabels;
        private static readonly int HighestSoundValue;

        // Cached so GUILayout/GUIContent do not allocate on every control, every pass.
        private static readonly GUILayoutOption[] NameWidth = { GUILayout.Width(200f) };
        private static readonly GUILayoutOption[] FileNameWidth = { GUILayout.Width(240f) };
        private static readonly GUILayoutOption[] ButtonWidth = { GUILayout.Width(88f) };
        private static readonly GUILayoutOption[] TinyButtonWidth = { GUILayout.Width(22f) };
        private static readonly GUILayoutOption[] ExpandWidth = { GUILayout.ExpandWidth(true) };

        private static readonly GUIContent VolumeLabel = new("Volume");
        private static readonly GUIContent PitchLabel = new("Pitch spread",
            "Leave at 0 for UI — a wobbling click reads as a defect. 0 also routes the sound to the flat " +
            "AudioSource, where a gameplay sound landing on top can never detune it.");
        private static readonly GUIContent RetriggerLabel = new("Retrigger (s)",
            "0 = no limit, and that is the correct default. A blanket debounce suppresses legitimate audio; " +
            "only raise it for a sound that can genuinely fire many times in one instant.");
        private static readonly GUIContent SingleClipLabel = new("Clip");
        private static readonly GUIContent DropToCreate = new("Drop audio here to create this row");
        private static readonly GUIContent DropToAdd = new("Drop audio here to add a variant");
        private static readonly GUIContent RemoveRowLabel = new("Remove Row");
        private static readonly GUIContent RemoveClipLabel = new("X");

        private static GUIContent[] _variantLabels = Array.Empty<GUIContent>();

        static AudioCatalogWindow()
        {
            Sounds = Enum.GetValues(typeof(SoundEffect))
                .Cast<SoundEffect>()
                .Where(id => id != SoundEffect.None)
                .ToArray();

            SoundNames = Sounds.Select(id => id.ToString()).ToArray();

            /* The filename the convention expects for each sound, shown beside the row so authoring a wav
             * needs no cross-referencing against AudioCatalogTools' parser. Built once — this is a fixed
             * string per sound, and formatting it per row per GUI pass is exactly the kind of allocation
             * the row cache exists to avoid. */
            FileNameLabels = Sounds.Select(id => new GUIContent(
                $"({FilePrefix}{id}.wav)",
                $"Expected filename. A wav dropped on this row is renamed to {FilePrefix}{id}.wav " +
                $"automatically; extra variants become {FilePrefix}{id}_2.wav, _3 and so on. Naming a file " +
                "this way up front also lets Scan Folder pick it up without a drop.")).ToArray();

            HighestSoundValue = Sounds.Length == 0 ? 0 : Sounds.Max(id => (int)id);
        }

        #endregion // Constants

        #region State

        private AudioCatalog _catalog;
        private SerializedObject _so;
        private SerializedProperty _entries;

        private Vector2 _scroll;
        private string _search = string.Empty;
        private bool _onlyUnbacked;

        /// <summary>What one sound's row looks like, indexed by enum value. Refilled once per GUI pass.</summary>
        private struct RowInfo
        {
            public int EntryIndex;   // -1 when the sound has no row at all
            public int Duplicates;
            public int ClipSlots;
            public int UsableClips;

            public bool HasRow => EntryIndex >= 0;
            public bool Warn => Duplicates > 1 || (HasRow && UsableClips < ClipSlots) || (HasRow && UsableClips == 0);
        }

        private RowInfo[] _rowCache;
        private int _backedCount;
        private GUIStyle _warnStyle;

        // Deferred mutations — see the class remarks.
        private bool _hasPendingRemove;
        private SoundEffect _pendingRemove;
        private bool _hasPendingDrop;
        private SoundEffect _pendingDropTarget;
        private UnityEngine.Object[] _pendingDropObjects;
        private string[] _pendingDropPaths;

        #endregion // State

        #region Window lifecycle

        [MenuItem("Tools/Audio/Audio Catalog Editor")]
        public static void Open()
        {
            var window = GetWindow<AudioCatalogWindow>("Audio Catalog");

            // Wide enough for name + expected filename + status without the three colliding. The longest
            // pair today is FireAircraftGroundAttack / (SFX_FireAircraftGroundAttack.wav).
            window.minSize = new Vector2(760f, 320f);
        }

        private void OnEnable()
        {
            _warnStyle = null;   // EditorStyles is only valid during a GUI pass, so this builds lazily there
            Reload();
        }

        /// <summary>Reload after Scan Folder, an import, or the asset being created elsewhere.</summary>
        private void OnProjectChange() => Reload();

        private void Reload()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
            _so = _catalog != null ? new SerializedObject(_catalog) : null;
            _entries = _so?.FindProperty("entries");

            if (_rowCache == null || _rowCache.Length != HighestSoundValue + 1)
                _rowCache = new RowInfo[HighestSoundValue + 1];

            Repaint();
        }

        #endregion // Window lifecycle

        #region GUI

        private void OnGUI()
        {
            if (_catalog == null || _so == null || _entries == null)
            {
                DrawNoCatalog();
                return;
            }

            _so.Update();
            RefreshRowCache();

            DrawToolbar();
            DrawRows();

            // ApplyModifiedProperties gives Undo and marks the asset dirty. Writing to DISK happens only on
            // a structural change (see ApplyDrop/RemoveRow) — saving on every slider tick would hit the
            // disk continuously while dragging a volume.
            _so.ApplyModifiedProperties();

            ProcessPendingMutations();
        }

        private void DrawNoCatalog()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                $"No AudioCatalog at {CatalogPath}.\n\n" +
                "Without it EVERY sound effect is silent, and GameAudioManager.InitializeSfx logs an error " +
                "at startup naming this fix.", MessageType.Error);

            if (GUILayout.Button($"Create Catalog (scans {SfxFolder})", GUILayout.Height(28f)))
            {
                AudioCatalogTools.CreateOrUpdate();
                Reload();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(46f)))
                {
                    _search = string.Empty;
                    GUI.FocusControl(null);
                }

                _onlyUnbacked = GUILayout.Toggle(_onlyUnbacked, "Unbacked only",
                    EditorStyles.toolbarButton, GUILayout.Width(100f));

                GUILayout.FlexibleSpace();

                // Reuses the existing tools rather than re-implementing them — two scanners that could
                // disagree about the filename convention is precisely what this window must not become.
                if (GUILayout.Button("Scan Folder", EditorStyles.toolbarButton, GUILayout.Width(84f)))
                {
                    AudioCatalogTools.CreateOrUpdate();
                    Reload();
                }
                if (GUILayout.Button("Audit", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                    AudioCatalogTools.Audit();
                if (GUILayout.Button("Select Asset", EditorStyles.toolbarButton, GUILayout.Width(88f)))
                    Selection.activeObject = _catalog;
            }

            EditorGUILayout.LabelField(
                $"{_backedCount} of {Sounds.Length} sounds have audio.  A member with no row is SILENT BY " +
                "DESIGN — the enum deliberately runs ahead of the audio.", EditorStyles.miniLabel);
        }

        private void DrawRows()
        {
            using var scope = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scope.scrollPosition;

            bool drewAny = false;
            for (int i = 0; i < Sounds.Length; i++)
            {
                if (!PassesFilter(i)) continue;
                DrawRow(i);
                drewAny = true;
            }

            if (!drewAny)
                EditorGUILayout.HelpBox("Nothing matches the current filter.", MessageType.Info);
        }

        private void DrawRow(int soundIndex)
        {
            SoundEffect id = Sounds[soundIndex];
            RowInfo info = _rowCache[(int)id];

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // ⚠ The ONE SerializedProperty fetch per row. Everything else on this row reads the cache.
                SerializedProperty row = info.HasRow ? _entries.GetArrayElementAtIndex(info.EntryIndex) : null;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(SoundNames[soundIndex], EditorStyles.boldLabel, NameWidth);
                    EditorGUILayout.LabelField(FileNameLabels[soundIndex], EditorStyles.miniLabel, FileNameWidth);
                    EditorGUILayout.LabelField(StatusText(info),
                        info.Warn ? WarnStyle : EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();

                    if (row != null &&
                        GUILayout.Button(RemoveRowLabel, EditorStyles.miniButton, ButtonWidth))
                    {
                        _hasPendingRemove = true;
                        _pendingRemove = id;
                    }
                }

                if (row != null)
                {
                    EditorGUI.indentLevel++;
                    DrawClips(row);
                    DrawTuning(row);
                    EditorGUI.indentLevel--;
                }

                Rect drop = GUILayoutUtility.GetRect(0f, 22f, ExpandWidth);
                GUI.Box(drop, row == null ? DropToCreate : DropToAdd, EditorStyles.helpBox);
                HandleDrop(drop, id);
            }
        }

        private static void DrawClips(SerializedProperty row)
        {
            SerializedProperty clips = row.FindPropertyRelative("clips");
            int count = clips.arraySize;

            for (int i = 0; i < count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    SerializedProperty element = clips.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(element, count > 1 ? VariantLabel(i) : SingleClipLabel);

                    if (GUILayout.Button(RemoveClipLabel, EditorStyles.miniButton, TinyButtonWidth))
                    {
                        /* ⚠ TWO STEPS, AND THE FIRST IS NOT OPTIONAL. On an array of object references,
                         * DeleteArrayElementAtIndex NULLS a populated element instead of removing it — so a
                         * single call leaves a phantom empty slot, which then reads as an authoring mistake
                         * in the audit and makes PickClip retry past it for nothing. */
                        element.objectReferenceValue = null;
                        clips.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }
        }

        private static void DrawTuning(SerializedProperty row)
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 108f;

            EditorGUILayout.PropertyField(row.FindPropertyRelative("volume"), VolumeLabel);
            EditorGUILayout.PropertyField(row.FindPropertyRelative("pitchVariation"), PitchLabel);
            EditorGUILayout.PropertyField(row.FindPropertyRelative("minRetriggerSeconds"), RetriggerLabel);

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        #endregion // GUI

        #region Drag and drop

        /// <summary>
        /// Records a drop for the end of the frame. ⚠ The payload must be READ here — DragAndDrop's
        /// contents are only valid during the event — but nothing is imported or written until the GUI
        /// pass is over.
        /// </summary>
        private void HandleDrop(Rect area, SoundEffect id)
        {
            Event evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
            if (!area.Contains(evt.mousePosition)) return;

            bool acceptable = DragAndDrop.objectReferences.Any(o => o is AudioClip)
                              || DragAndDrop.paths.Any(IsAudioPath);

            DragAndDrop.visualMode = acceptable ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

            if (evt.type == EventType.DragPerform && acceptable)
            {
                DragAndDrop.AcceptDrag();

                _hasPendingDrop = true;
                _pendingDropTarget = id;
                _pendingDropObjects = DragAndDrop.objectReferences.ToArray();
                _pendingDropPaths = DragAndDrop.paths.ToArray();

                evt.Use();
            }
        }

        private void ProcessPendingMutations()
        {
            if (_hasPendingRemove)
            {
                _hasPendingRemove = false;
                RemoveRow(_pendingRemove);
                Repaint();
            }

            if (_hasPendingDrop)
            {
                _hasPendingDrop = false;
                ApplyDrop(_pendingDropTarget, _pendingDropObjects, _pendingDropPaths);
                _pendingDropObjects = null;
                _pendingDropPaths = null;
                Repaint();
            }
        }

        private void ApplyDrop(SoundEffect id, UnityEngine.Object[] objects, string[] paths)
        {
            // STEP 1 — all file work first. Moving or importing fires OnProjectChange, which rebuilds
            // _so; doing this after taking the SerializedProperty would write into a stale object.
            List<AudioClip> resolved = ResolveDroppedClips(id, objects, paths);
            if (resolved.Count == 0) return;

            // STEP 2 — now, against whatever state step 1 left behind, touch the catalog.
            Reload();
            if (_so == null || _entries == null) return;

            _so.Update();
            SerializedProperty row = FindRow(id) ?? CreateRow(id);
            SerializedProperty clipArray = row.FindPropertyRelative("clips");

            foreach (AudioClip clip in resolved)
            {
                if (ContainsClip(clipArray, clip)) continue;

                clipArray.arraySize++;
                clipArray.GetArrayElementAtIndex(clipArray.arraySize - 1).objectReferenceValue = clip;
            }

            _so.ApplyModifiedProperties();

            // A drop is a real authoring action and is worth a disk write; a slider drag is not.
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();
            _catalog.Invalidate();
        }

        /// <summary>
        /// Turns a drop payload into clips that LIVE under <see cref="SfxFolder"/>. Project-window drags
        /// arrive as object references; a drag from Explorer arrives as OS paths only, and those files are
        /// copied in.
        /// </summary>
        private static List<AudioClip> ResolveDroppedClips(SoundEffect id, UnityEngine.Object[] objects,
                                                           string[] paths)
        {
            var resolved = new List<AudioClip>();

            if (objects != null)
            {
                foreach (AudioClip clip in objects.OfType<AudioClip>())
                {
                    AudioClip placed = EnsureUnderSfxFolder(clip, id);
                    if (placed != null) resolved.Add(placed);
                }
            }

            if (resolved.Count > 0 || paths == null) return resolved;

            foreach (string path in paths)
            {
                if (!IsAudioPath(path)) continue;

                // A Project-window drag populates paths too, but those already came back as object
                // references above — so anything reaching here is genuinely from outside the project.
                if (IsInsideProject(path)) continue;

                AudioClip imported = ImportExternalFile(path, id);
                if (imported != null) resolved.Add(imported);
            }

            return resolved;
        }

        #endregion // Drag and drop

        #region File placement

        /// <summary>
        /// Guarantees the clip sits under <see cref="SfxFolder"/> with a convention filename, moving and
        /// renaming it otherwise. Returns the clip at its final location, or null if the move failed.
        /// </summary>
        private static AudioClip EnsureUnderSfxFolder(AudioClip clip, SoundEffect id)
        {
            string path = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(path)) return null;

            bool inFolder = path.StartsWith(SfxFolder + "/", StringComparison.OrdinalIgnoreCase);
            bool namedByConvention = MatchesConvention(Path.GetFileNameWithoutExtension(path), id);
            if (inFolder && namedByConvention) return clip;

            Directory.CreateDirectory(SfxFolder);
            string target = UniqueConventionPath(id, Path.GetExtension(path));

            // MoveAsset preserves the GUID, so anything already referencing this clip keeps working.
            string error = AssetDatabase.MoveAsset(path, target);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[AudioCatalog] Could not move '{path}' to '{target}': {error}. The clip was " +
                               "NOT assigned — move it by hand, because outside Assets/Audio/SFX it imports " +
                               "without the ratified settings and would play late at double memory.");
                return null;
            }

            /* ⚠ FORCE THE REIMPORT. A move does not necessarily re-run the importer, and SfxImportSettings
             * is the ONLY thing applying mono/PCM/preload. Skipping this leaves the file in the right folder
             * with the wrong settings — the silent half-failure this whole path exists to prevent. */
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[AudioCatalog] Moved '{path}' → '{target}' and reimported with the ratified SFX settings.");

            return AssetDatabase.LoadAssetAtPath<AudioClip>(target);
        }

        private static AudioClip ImportExternalFile(string osPath, SoundEffect id)
        {
            try
            {
                if (!File.Exists(osPath)) return null;

                Directory.CreateDirectory(SfxFolder);
                string target = UniqueConventionPath(id, Path.GetExtension(osPath));

                File.Copy(osPath, target);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[AudioCatalog] Copied '{osPath}' → '{target}'.");

                return AssetDatabase.LoadAssetAtPath<AudioClip>(target);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioCatalog] Could not import '{osPath}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// First free path of the form SFX_&lt;Name&gt;.ext, then SFX_&lt;Name&gt;_2.ext, _3 and so on.
        /// ⚠ Deliberately NOT AssetDatabase.GenerateUniqueAssetPath, whose " 1" suffix does not match the
        /// _&lt;digits&gt; variant convention that AudioCatalogTools parses.
        /// </summary>
        private static string UniqueConventionPath(SoundEffect id, string extension)
        {
            if (string.IsNullOrEmpty(extension)) extension = ".wav";

            string first = $"{SfxFolder}/{FilePrefix}{id}{extension}";
            if (!File.Exists(first)) return first;

            for (int n = 2; n < 100; n++)
            {
                string candidate = $"{SfxFolder}/{FilePrefix}{id}_{n}{extension}";
                if (!File.Exists(candidate)) return candidate;
            }

            return $"{SfxFolder}/{FilePrefix}{id}_{DateTime.Now.Ticks}{extension}";
        }

        private static bool MatchesConvention(string fileName, SoundEffect id)
        {
            if (!fileName.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase)) return false;

            string bare = fileName.Substring(FilePrefix.Length);
            int underscore = bare.LastIndexOf('_');
            if (underscore > 0 && int.TryParse(bare.Substring(underscore + 1), out _))
                bare = bare.Substring(0, underscore);

            return string.Equals(bare, id.ToString(), StringComparison.Ordinal);
        }

        private static bool IsAudioPath(string path) =>
            !string.IsNullOrEmpty(path) &&
            AudioExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

        private static bool IsInsideProject(string path)
        {
            try
            {
                return Path.GetFullPath(path)
                    .StartsWith(Path.GetFullPath("Assets"), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        #endregion // File placement

        #region Row access

        /* ⚠ enumValueIndex is the enum's DECLARATION INDEX, not its underlying value. The two agree here
         * because SoundEffect declares no explicit values — which the append-only rule already requires —
         * and AudioCatalogTools makes the same assumption. If a member ever gets an explicit value, both
         * break together rather than one silently. */
        private SerializedProperty FindRow(SoundEffect id)
        {
            for (int i = 0; i < _entries.arraySize; i++)
            {
                SerializedProperty row = _entries.GetArrayElementAtIndex(i);
                if (row.FindPropertyRelative("id").enumValueIndex == (int)id) return row;
            }
            return null;
        }

        /// <summary>
        /// Walks the entries array ONCE and records everything the GUI needs about every sound.
        /// ⚠ This exists because the alternative — querying per row while drawing — is O(sounds × entries)
        /// per GUI pass against the slowest API in the editor. See the class remarks.
        /// </summary>
        private void RefreshRowCache()
        {
            for (int i = 0; i < _rowCache.Length; i++)
            {
                _rowCache[i].EntryIndex = -1;
                _rowCache[i].Duplicates = 0;
                _rowCache[i].ClipSlots = 0;
                _rowCache[i].UsableClips = 0;
            }

            int entryCount = _entries.arraySize;
            for (int i = 0; i < entryCount; i++)
            {
                SerializedProperty entry = _entries.GetArrayElementAtIndex(i);
                int id = entry.FindPropertyRelative("id").enumValueIndex;
                if (id < 0 || id >= _rowCache.Length) continue;

                _rowCache[id].Duplicates++;

                // ⚠ FIRST row wins, matching AudioCatalog.BuildLookup — the stats shown must describe the
                // row that will actually play, not whichever duplicate happens to come last.
                if (_rowCache[id].EntryIndex >= 0) continue;

                SerializedProperty clips = entry.FindPropertyRelative("clips");
                int slots = clips.arraySize;
                int usable = 0;
                for (int c = 0; c < slots; c++)
                    if (clips.GetArrayElementAtIndex(c).objectReferenceValue != null) usable++;

                _rowCache[id].EntryIndex = i;
                _rowCache[id].ClipSlots = slots;
                _rowCache[id].UsableClips = usable;
            }

            _backedCount = 0;
            for (int i = 0; i < Sounds.Length; i++)
                if (_rowCache[(int)Sounds[i]].UsableClips > 0) _backedCount++;
        }

        private SerializedProperty CreateRow(SoundEffect id)
        {
            _entries.arraySize++;
            SerializedProperty row = _entries.GetArrayElementAtIndex(_entries.arraySize - 1);

            // ⚠ Every field is set explicitly: growing a serialized array COPIES the previous element, so a
            // new row would otherwise silently inherit the last one's volume and retrigger window.
            row.FindPropertyRelative("id").enumValueIndex = (int)id;
            row.FindPropertyRelative("clips").arraySize = 0;
            row.FindPropertyRelative("volume").floatValue = 1f;
            row.FindPropertyRelative("pitchVariation").floatValue = 0f;
            row.FindPropertyRelative("minRetriggerSeconds").floatValue = 0f;

            return row;
        }

        private void RemoveRow(SoundEffect id)
        {
            _so.Update();

            for (int i = _entries.arraySize - 1; i >= 0; i--)
            {
                if (_entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex != (int)id)
                    continue;

                _entries.DeleteArrayElementAtIndex(i);
                break;
            }

            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();
            _catalog.Invalidate();
        }

        private static bool ContainsClip(SerializedProperty clipArray, AudioClip clip)
        {
            for (int i = 0; i < clipArray.arraySize; i++)
                if (clipArray.GetArrayElementAtIndex(i).objectReferenceValue == clip) return true;
            return false;
        }

        #endregion // Row access

        #region Presentation helpers

        private bool PassesFilter(int soundIndex)
        {
            if (_onlyUnbacked && _rowCache[(int)Sounds[soundIndex]].UsableClips > 0) return false;

            return string.IsNullOrEmpty(_search)
                   || SoundNames[soundIndex].IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// ⚠ Reads the cache only — no SerializedProperty access. The strings still allocate, but only for
        /// the handful of rows that are actually in an interesting state.
        /// </summary>
        private static string StatusText(RowInfo info)
        {
            if (info.Duplicates > 1)
                return $"⚠ {info.Duplicates} DUPLICATE ROWS — the first one wins, the rest never play";
            if (!info.HasRow)
                return "no row — silent by design";
            if (info.UsableClips == 0)
                return "⚠ ROW WITH NO CLIP — looks wired, is silent";
            if (info.UsableClips < info.ClipSlots)
                return $"⚠ {info.UsableClips} clip(s), {info.ClipSlots - info.UsableClips} empty slot(s)";

            return info.UsableClips == 1 ? "1 clip" : $"{info.UsableClips} variants";
        }

        /// <summary>Built once per window, not per row per pass. EditorStyles is only valid inside OnGUI.</summary>
        private GUIStyle WarnStyle => _warnStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            normal =
            {
                textColor = EditorGUIUtility.isProSkin
                    ? new Color(1f, 0.78f, 0.28f)
                    : new Color(0.62f, 0.34f, 0f)
            }
        };

        private static GUIContent VariantLabel(int index)
        {
            if (index >= _variantLabels.Length)
            {
                var grown = new GUIContent[index + 4];
                Array.Copy(_variantLabels, grown, _variantLabels.Length);
                for (int i = _variantLabels.Length; i < grown.Length; i++)
                    grown[i] = new GUIContent($"Variant {i + 1}");
                _variantLabels = grown;
            }
            return _variantLabels[index];
        }

        #endregion // Presentation helpers
    }
}
