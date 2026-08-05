using System.Collections.Generic;
using HammerAndSickle.Controllers;
using UnityEngine;

namespace HammerAndSickle.Audio
{
    /// <summary>
    /// Sound-effect playback. A plain C# class, not a MonoBehaviour: it is owned by
    /// <see cref="GameAudioManager"/>, which hands it the AudioSources it created, so there is no extra
    /// scene object and no growth of an already-large file.
    ///
    /// ⚠ FULLY SYNCHRONOUS. No coroutines, no loading, no cache — clips are imported project assets with
    /// Preload Audio Data on (§7.1a), so they are resident before anything asks for them. This class is
    /// what the old cache / negative-cache / in-flight-guard / preload apparatus collapsed into once that
    /// became true.
    /// </summary>
    public sealed class SfxPlayer
    {
        #region Fields

        /* TWO SOURCE GROUPS, AND THE SPLIT IS NOT COSMETIC. Pitch is a per-SOURCE property, so changing it
         * for a new one-shot also warps any one-shot still ringing on that source. Sounds with no pitch
         * variation — every UI sound — therefore play on a dedicated FLAT source whose pitch is never
         * touched, and can never be detuned by a gameplay sound landing on top of them. Varied sounds
         * round-robin the pool, where the worst case is one already-playing sound bending slightly. */
        private readonly AudioSource _flatSource;
        private readonly AudioSource[] _pitchPool;
        private int _nextPitchSource;

        private readonly Dictionary<GameAudioManager.SoundEffect, float> _lastPlayed = new();

        #endregion // Fields

        #region Construction

        public SfxPlayer(AudioSource flatSource, AudioSource[] pitchPool)
        {
            _flatSource = flatSource;
            _pitchPool = pitchPool;
        }

        #endregion // Construction

        #region Public API

        /// <summary>
        /// Plays an entry. <paramref name="channelVolume"/> is the already-computed master x SFX x mute
        /// figure; <paramref name="volumeScale"/> is the caller's per-play multiplier.
        /// </summary>
        public void Play(AudioCatalog.Entry entry, float channelVolume, float volumeScale, float now)
        {
            if (entry == null) return;
            if (!ShouldPlay(entry.id, entry.minRetriggerSeconds, now)) return;

            AudioClip clip = entry.PickClip();
            if (clip == null) return;

            float volume = channelVolume * entry.volume * volumeScale;

            if (entry.pitchVariation <= 0f)
            {
                // PlayOneShot MIXES rather than replacing the source's clip, which is why overlapping
                // sounds layer instead of cutting each other off (§27.7.7.2). No Stop(), ever.
                if (_flatSource != null) _flatSource.PlayOneShot(clip, volume);
            }
            else
            {
                if (_pitchPool == null || _pitchPool.Length == 0) return;

                AudioSource source = _pitchPool[_nextPitchSource];
                _nextPitchSource = (_nextPitchSource + 1) % _pitchPool.Length;

                source.pitch = 1f + Random.Range(-entry.pitchVariation, entry.pitchVariation);
                source.PlayOneShot(clip, volume);
            }

            _lastPlayed[entry.id] = now;
        }

        /// <summary>
        /// Whether a sound may retrigger yet. Pure and separated from playback so the debounce is testable
        /// without AudioSources — the rest of this class needs a live Unity audio system, this does not.
        /// </summary>
        /// <remarks>
        /// ⚠ Default is 0 = NO LIMIT, deliberately. A blanket debounce suppresses legitimate audio: a
        /// double-click is two events, and several units firing at once is several sounds. Only a sound
        /// that can genuinely fire many times in one instant should opt in.
        /// </remarks>
        public bool ShouldPlay(GameAudioManager.SoundEffect id, float minRetriggerSeconds, float now)
        {
            if (minRetriggerSeconds <= 0f) return true;
            if (!_lastPlayed.TryGetValue(id, out float last)) return true;
            return now - last >= minRetriggerSeconds;
        }

        /// <summary>Clears retrigger history. Called when a battle ends so timings do not leak across scenes.</summary>
        public void Reset() => _lastPlayed.Clear();

        #endregion // Public API
    }
}
