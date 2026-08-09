using System;
using System.Collections.Generic;
using HammerAndSickle.Controllers;
using UnityEngine;

namespace HammerAndSickle.Audio
{
    /// <summary>
    /// The authoring surface for sound effects: one ScriptableObject mapping each
    /// <see cref="GameAudioManager.SoundEffect"/> to its clip(s) and per-sound tuning.
    ///
    /// Adding a sound is: drop the wav into Assets/Audio/SFX, add a row, assign the clip. ⚠ NO FILENAME
    /// STRINGS and no parallel dictionary — the old `SoundEffectFiles` map was a second list that had to be
    /// kept in step with the enum by hand, and tuning a sound's volume meant editing code.
    ///
    /// ⚠ Loaded via Resources (see <see cref="ResourcePath"/>) rather than a serialized reference, because
    /// GameAudioManager self-creates through `new GameObject()` when absent — a [SerializeField] would be
    /// null on that path. Only THIS asset lives in Resources; the clips are ordinary assets it references,
    /// so Unity pulls them into the build automatically without a second Resources folder.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "Hammer and Sickle/Audio Catalog")]
    public class AudioCatalog : ScriptableObject
    {
        #region Constants

        /// <summary>Resources-relative path. The asset must live at Assets/Resources/Audio/AudioCatalog.asset.</summary>
        public const string ResourcePath = "Audio/AudioCatalog";

        #endregion // Constants

        #region Entry

        /// <summary>One sound: its clips and how it should be played.</summary>
        [Serializable]
        public class Entry
        {
            [Tooltip("Which SoundEffect this row provides audio for.")]
            public GameAudioManager.SoundEffect id;

            [Tooltip("Clip variants. One is picked at random per play. Two or three stops a frequently " +
                     "heard sound becoming fatiguing; a single clip is fine for rare ones.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();

            [Tooltip("Base volume for this sound, before the SFX channel and master volume.")]
            [Range(0f, 1f)] public float volume = 1f;

            [Tooltip("Random pitch spread per play (0 = none). Leave at 0 for UI — a wobbling click reads " +
                     "as a defect. Useful on repetitive combat sounds.")]
            [Range(0f, 0.5f)] public float pitchVariation = 0f;

            [Tooltip("Minimum seconds between two plays of THIS sound (0 = no limit, the default). Only " +
                     "raise it for a sound that can legitimately be triggered many times in one instant.")]
            [Min(0f)] public float minRetriggerSeconds = 0f;

            /// <summary>True when this row can actually produce audio.</summary>
            public bool HasClip
            {
                get
                {
                    if (clips == null) return false;
                    foreach (var c in clips)
                        if (c != null) return true;
                    return false;
                }
            }

            /// <summary>
            /// Length of the SHORTEST usable variant, in seconds; 0 when the row has no clip.
            /// </summary>
            /// <remarks>
            /// ⚠ SHORTEST, not longest or average, and the direction is a decision. This is used to ask
            /// "will a standard clip cover this move, or should the long cut play?" — and since any
            /// variant might be the one picked, escalating on the shortest means no variant can leave a
            /// silent gap mid-move. Trailing audio past the end of a move is INTENDED (Bob, 2026-08-04:
            /// the sound frames the action rather than tracking it); silence during a visible move is
            /// the failure worth avoiding, so the tie breaks toward playing too much.
            /// ⚠ Reading the real asset means re-recording a clip retunes this automatically. It replaces
            /// a hard-coded seconds constant that described an asset the code could simply measure.
            /// </remarks>
            public float ShortestClipSeconds
            {
                get
                {
                    float shortest = 0f;
                    if (clips == null) return 0f;

                    foreach (var c in clips)
                    {
                        if (c == null) continue;
                        if (shortest == 0f || c.length < shortest) shortest = c.length;
                    }
                    return shortest;
                }
            }

            /// <summary>A random non-null variant, or null when the row has no usable clip.</summary>
            public AudioClip PickClip()
            {
                if (clips == null || clips.Length == 0) return null;
                if (clips.Length == 1) return clips[0];

                // Retry past empty slots rather than returning null for a half-filled array — an unassigned
                // element in the middle of a variant list is an authoring slip, not a request for silence.
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    var pick = clips[UnityEngine.Random.Range(0, clips.Length)];
                    if (pick != null) return pick;
                }
                foreach (var c in clips)
                    if (c != null) return c;
                return null;
            }
        }

        #endregion // Entry

        #region Fields

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        private Dictionary<GameAudioManager.SoundEffect, Entry> _lookup;

        #endregion // Fields

        #region Public API

        /// <summary>All rows, for the editor audit tool.</summary>
        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>
        /// Finds the row for a sound. Returns false when the sound has no row or no clip — both of which
        /// are SILENT NO-OPS by design, never exceptions. The enum deliberately runs ahead of the audio
        /// (§27.7.5.2), so an unmapped id is a normal intermediate state, not an error.
        /// </summary>
        public bool TryGet(GameAudioManager.SoundEffect id, out Entry entry)
        {
            BuildLookup();
            return _lookup.TryGetValue(id, out entry) && entry.HasClip;
        }

        /// <summary>Discards the cached lookup so edits in the Inspector take effect without a domain reload.</summary>
        public void Invalidate() => _lookup = null;

        #endregion // Public API

        #region Internals

        private void BuildLookup()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<GameAudioManager.SoundEffect, Entry>(entries.Length);
            foreach (var e in entries)
            {
                if (e == null) continue;

                // FIRST row wins on a duplicate id. The audit tool reports duplicates; silently taking the
                // last one would make the Inspector's visible order a lie about which clip actually plays.
                if (!_lookup.ContainsKey(e.id))
                    _lookup.Add(e.id, e);
            }
        }

        private void OnValidate() => Invalidate();

        #endregion // Internals
    }
}
