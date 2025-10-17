using HammerAndSickle.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Centralized audio management system for Hammer & Sickle, handling music playback,
    /// ambient sounds, sound effects, briefing narration, volume control, and audio 
    /// persistence across gameplay sessions. Implements a persistent singleton pattern 
    /// to maintain audio state across scene transitions.
    /// </summary>
    public class GameAudioManager : MonoBehaviour
    {
        #region Singleton

        private static GameAudioManager _instance;

        /// <summary>
        /// Gets the singleton instance of GameAudioManager, creating it if necessary.
        /// Instance persists across scene loads via DontDestroyOnLoad.
        /// </summary>
        public static GameAudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameAudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameAudioManager");
                        _instance = go.AddComponent<GameAudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion // Singleton

        #region Audio Enums

        /// <summary>
        /// Enumeration of all music tracks available in the game.
        /// Maps to OGG files in StreamingAssets/Audio/Music folder.
        /// </summary>
        public enum MusicTrack
        {
            None,
            MainMenu
        }

        /// <summary>
        /// Enumeration of all ambient environmental sounds.
        /// Maps to OGG files in StreamingAssets/Audio/Ambient folder.
        /// These sounds loop by default for continuous atmosphere.
        /// </summary>
        public enum AmbientSound
        {
            None,
            AmbientCombat
        }

        /// <summary>
        /// Enumeration of all sound effects available in the game.
        /// Maps to WAV files in StreamingAssets/Audio/SFX folder.
        /// </summary>
        public enum SoundEffect
        {
            None,
            ButtonClick,
            ButtonHover,
            MenuOpen,
            MenuClose,
            RadioButtonClick,
            MeduimSnareDrum
        }

        /// <summary>
        /// Enumeration of all briefing narration audio tracks.
        /// Maps to OGG files in StreamingAssets/Audio/Briefings folder.
        /// Used for mission briefings that can be skipped by the player.
        /// </summary>
        public enum BriefingNarration
        {
            None,
            Khost
        }

        #endregion // Audio Enums

        #region Static Mappings

        /// <summary>
        /// Maps MusicTrack enum values to their corresponding OGG filenames.
        /// Used for loading music files from StreamingAssets.
        /// </summary>
        private static readonly Dictionary<MusicTrack, string> MusicTrackFiles = new Dictionary<MusicTrack, string>
        {
            { MusicTrack.MainMenu, "Music_MainMenu.ogg" }
        };

        /// <summary>
        /// Maps AmbientSound enum values to their corresponding OGG filenames.
        /// Used for loading ambient audio files from StreamingAssets.
        /// </summary>
        private static readonly Dictionary<AmbientSound, string> AmbientSoundFiles = new Dictionary<AmbientSound, string>
        {
            { AmbientSound.AmbientCombat, "Ambient_DistantCombat.ogg" }
        };

        /// <summary>
        /// Maps SoundEffect enum values to their corresponding WAV filenames.
        /// Used for loading sound effect files from StreamingAssets.
        /// </summary>
        private static readonly Dictionary<SoundEffect, string> SoundEffectFiles = new Dictionary<SoundEffect, string>
        {
            { SoundEffect.ButtonClick, "SFX_ButtonClick.wav" },
            { SoundEffect.MenuOpen, "SFX_MenuOpen.wav" },
            { SoundEffect.MenuClose, "SFX_MenuClose.wav" },
            { SoundEffect.RadioButtonClick, "SFX_RadioButtonClick.wav" },
            { SoundEffect.ButtonHover, "SFX_ButtonHover.wav" },
            { SoundEffect.MeduimSnareDrum, "SFX_MediumSnareDrum.wav"}
        };

        /// <summary>
        /// Maps BriefingNarration enum values to their corresponding OGG filenames.
        /// Used for loading briefing audio files from StreamingAssets.
        /// </summary>
        private static readonly Dictionary<BriefingNarration, string> BriefingFiles = new Dictionary<BriefingNarration, string>
        {
            { BriefingNarration.Khost, "Briefing_Khost.ogg" }
        };

        #endregion // Static Mappings

        #region Audio Settings

        /// <summary>
        /// Serializable class containing all audio volume settings.
        /// Persisted to disk as JSON for maintaining user preferences across sessions.
        /// </summary>
        [Serializable]
        public class AudioSettings
        {
            public float MasterVolume { get; set; } = 1.0f;        // Global volume multiplier (0-1)
            public float MusicVolume { get; set; } = 0.7f;         // Background music volume (0-1)
            public float AmbientVolume { get; set; } = 0.6f;       // Ambient environment volume (0-1)
            public float SFXVolume { get; set; } = 1.0f;           // Sound effects volume (0-1)
            public float BriefingVolume { get; set; } = 1.0f;      // Briefing narration volume (0-1)
            public bool MuteMusic { get; set; } = false;           // Mute flag for music
            public bool MuteAmbient { get; set; } = false;         // Mute flag for ambient sounds
            public bool MuteSFX { get; set; } = false;             // Mute flag for sound effects
            public bool MuteBriefing { get; set; } = false;        // Mute flag for briefing narration
            public bool MuteAll { get; set; } = false;             // Global mute flag
        }

        #endregion // Audio Settings

        #region Private Fields

        // Audio source components for different audio channels
        private AudioSource _musicSource;          // Primary music playback channel
        private AudioSource _crossfadeSource;      // Secondary music channel for crossfading
        private AudioSource _ambientSource;        // Environmental ambient sounds channel
        private AudioSource _briefingSource;       // Dedicated channel for briefing narration
        private AudioSource[] _sfxPool;            // Pool of audio sources for simultaneous sound effects
        private int _nextSfxIndex = 0;             // Round-robin index for SFX pool allocation

        // Current playback state tracking
        private MusicTrack _currentMusicTrack = MusicTrack.None;
        private AmbientSound _currentAmbient = AmbientSound.None;
        private BriefingNarration _currentBriefing = BriefingNarration.None;
        private bool _isCrossfading = false;
        private Coroutine _fadeCoroutine;
        private Coroutine _briefingCoroutine;
        private System.Action _briefingCompleteCallback;   // Callback invoked when briefing completes naturally

        // Settings management
        private AudioSettings _settings;
        private string _settingsPath;

        // Audio clip caching to avoid repeated loading
        private Dictionary<MusicTrack, AudioClip> _musicCache = new Dictionary<MusicTrack, AudioClip>();
        private Dictionary<AmbientSound, AudioClip> _ambientCache = new Dictionary<AmbientSound, AudioClip>();
        private Dictionary<SoundEffect, AudioClip> _sfxCache = new Dictionary<SoundEffect, AudioClip>();
        private Dictionary<BriefingNarration, AudioClip> _briefingCache = new Dictionary<BriefingNarration, AudioClip>();

        // Configuration constants
        private const float DEFAULT_CROSSFADE_DURATION = 1.5f; // Industry standard crossfade time in seconds
        private const int SFX_POOL_SIZE = 10;                  // Number of simultaneous sound effects supported
        private const string MUSIC_FOLDER = "Audio/Music";
        private const string AMBIENT_FOLDER = "Audio/Ambient";
        private const string SFX_FOLDER = "Audio/SFX";
        private const string BRIEFING_FOLDER = "Audio/Briefings";

        #endregion // Private Fields

        #region Unity Lifecycle

        /// <summary>
        /// Unity Awake callback. Initializes the singleton instance, creates audio sources,
        /// loads saved settings, and applies volume configurations.
        /// </summary>
        private void Awake()
        {
            try
            {
                // Enforce singleton pattern
                if (_instance != null && _instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                _instance = this;
                DontDestroyOnLoad(gameObject);

                #if UNITY_EDITOR
                // Hide from scene cleanup check in Editor
                gameObject.hideFlags = HideFlags.DontSave;
                #endif

                InitializeAudioSources();
                LoadSettings();
                ApplyVolumeSettings();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "Awake", e);
            }
        }

        /// <summary>
        /// Unity OnDestroy callback. Properly cleans up all resources, stops audio playback,
        /// saves settings, and clears singleton reference.
        /// </summary>
        private void OnDestroy()
        {
            try
            {
                // Stop all active coroutines first
                StopAllCoroutines();

                // Stop any fade/crossfade coroutines explicitly
                if (_fadeCoroutine != null)
                {
                    StopCoroutine(_fadeCoroutine);
                    _fadeCoroutine = null;
                }

                if (_briefingCoroutine != null)
                {
                    StopCoroutine(_briefingCoroutine);
                    _briefingCoroutine = null;
                }

                // Stop all audio playback
                if (_musicSource != null) _musicSource.Stop();
                if (_crossfadeSource != null) _crossfadeSource.Stop();
                if (_ambientSource != null) _ambientSource.Stop();
                if (_briefingSource != null) _briefingSource.Stop();

                if (_sfxPool != null)
                {
                    foreach (var source in _sfxPool)
                    {
                        if (source != null) source.Stop();
                    }
                }

                // Clear cached audio clips to free memory
                _musicCache?.Clear();
                _ambientCache?.Clear();
                _sfxCache?.Clear();
                _briefingCache?.Clear();

                // Clear callbacks
                _briefingCompleteCallback = null;

                // Save settings before cleanup
                SaveSettings();

                // Clear singleton reference if this is the instance
                if (_instance == this)
                {
                    _instance = null;
                }
            }
            catch (Exception e)
            {
                // Use Debug.LogError in OnDestroy since AppService might be gone
                Debug.LogError($"GameAudioManager.OnDestroy error: {e.Message}");
            }
        }

        #endregion // Unity Lifecycle

        #region Initialization

        /// <summary>
        /// Creates and configures all AudioSource components needed for the audio system.
        /// Sets up music sources, ambient source, briefing source, and SFX pool.
        /// </summary>
        private void InitializeAudioSources()
        {
            try
            {
                // Create primary music source for continuous background music
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.parent = transform;
                _musicSource = musicObj.AddComponent<AudioSource>();
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;

                // Create secondary music source for crossfade transitions
                GameObject crossfadeObj = new GameObject("CrossfadeSource");
                crossfadeObj.transform.parent = transform;
                _crossfadeSource = crossfadeObj.AddComponent<AudioSource>();
                _crossfadeSource.loop = true;
                _crossfadeSource.playOnAwake = false;

                // Create ambient sound source for environmental atmosphere
                GameObject ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.parent = transform;
                _ambientSource = ambientObj.AddComponent<AudioSource>();
                _ambientSource.loop = true;  // Ambient sounds loop by default
                _ambientSource.playOnAwake = false;

                // Create dedicated source for briefing narration (non-looping)
                GameObject briefingObj = new GameObject("BriefingSource");
                briefingObj.transform.parent = transform;
                _briefingSource = briefingObj.AddComponent<AudioSource>();
                _briefingSource.loop = false;
                _briefingSource.playOnAwake = false;

                // Create pool of SFX sources for simultaneous sound effects
                _sfxPool = new AudioSource[SFX_POOL_SIZE];
                for (int i = 0; i < SFX_POOL_SIZE; i++)
                {
                    GameObject sfxObj = new GameObject($"SFXSource_{i}");
                    sfxObj.transform.parent = transform;
                    _sfxPool[i] = sfxObj.AddComponent<AudioSource>();
                    _sfxPool[i].loop = false;
                    _sfxPool[i].playOnAwake = false;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "InitializeAudioSources", e);
            }
        }

        /// <summary>
        /// Ensures the GameAudioManager singleton exists. Call this at game startup
        /// to force creation before any audio operations are attempted.
        /// </summary>
        public static void EnsureExists()
        {
            var instance = Instance; // Forces creation if doesn't exist
        }

        #endregion // Initialization

        #region Music Control

        /// <summary>
        /// Plays the specified music track with optional looping and fade-in.
        /// If a track is already playing, it will be stopped first unless it's the same track.
        /// </summary>
        /// <param name="track">The music track to play</param>
        /// <param name="loop">Whether the track should loop continuously</param>
        /// <param name="fadeInTime">Duration of fade-in effect in seconds (0 for immediate)</param>
        public void PlayMusic(MusicTrack track, bool loop = true, float fadeInTime = 0f)
        {
            try
            {
                if (track == MusicTrack.None)
                {
                    StopMusic(fadeInTime);
                    return;
                }

                // Don't restart if already playing the same track
                if (_currentMusicTrack == track && _musicSource.isPlaying)
                    return;

                StartCoroutine(PlayMusicCoroutine(track, loop, fadeInTime));
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "PlayMusic", e);
            }
        }

        /// <summary>
        /// Coroutine that handles music playback, including loading from cache or disk.
        /// </summary>
        private IEnumerator PlayMusicCoroutine(MusicTrack track, bool loop, float fadeInTime)
        {
            AudioClip clip = null;

            // Check cache first for faster loading
            if (_musicCache.ContainsKey(track))
            {
                clip = _musicCache[track];
            }
            else
            {
                // Load from disk if not cached
                yield return LoadMusicTrack(track);
                if (_musicCache.ContainsKey(track))
                    clip = _musicCache[track];
            }

            if (clip == null)
            {
                AppService.CaptureUiMessage($"Failed to load music track: {track}");
                yield break;
            }

            _currentMusicTrack = track;
            _musicSource.clip = clip;
            _musicSource.loop = loop;

            // Apply fade-in if requested
            if (fadeInTime > 0f)
            {
                _musicSource.volume = 0f;
                _musicSource.Play();
                yield return FadeAudioSource(_musicSource, 0f, GetEffectiveMusicVolume(), fadeInTime);
            }
            else
            {
                _musicSource.volume = GetEffectiveMusicVolume();
                _musicSource.Play();
            }
        }

        /// <summary>
        /// Stops the currently playing music with optional fade-out effect.
        /// </summary>
        /// <param name="fadeOutTime">Duration of fade-out in seconds (0 for immediate stop)</param>
        public void StopMusic(float fadeOutTime = 0f)
        {
            try
            {
                if (_fadeCoroutine != null)
                    StopCoroutine(_fadeCoroutine);

                _fadeCoroutine = StartCoroutine(StopMusicCoroutine(fadeOutTime));
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "StopMusic", e);
            }
        }

        /// <summary>
        /// Coroutine that handles stopping music with optional fade-out.
        /// </summary>
        private IEnumerator StopMusicCoroutine(float fadeOutTime)
        {
            if (fadeOutTime > 0f && _musicSource.isPlaying)
            {
                yield return FadeAudioSource(_musicSource, _musicSource.volume, 0f, fadeOutTime);
            }

            _musicSource.Stop();
            _currentMusicTrack = MusicTrack.None;
        }

        /// <summary>
        /// Smoothly transitions from the current music track to a new one using crossfade.
        /// The old track is unloaded from cache after transition to save memory.
        /// </summary>
        /// <param name="newTrack">The new music track to transition to</param>
        /// <param name="duration">Duration of the crossfade in seconds</param>
        public void CrossfadeToMusic(MusicTrack newTrack, float duration = DEFAULT_CROSSFADE_DURATION)
        {
            try
            {
                // Prevent multiple simultaneous crossfades
                if (_isCrossfading || newTrack == _currentMusicTrack)
                    return;

                StartCoroutine(CrossfadeCoroutine(newTrack, duration));
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "CrossfadeToMusic", e);
            }
        }

        /// <summary>
        /// Coroutine that performs crossfade between two music tracks.
        /// Swaps audio sources to enable smooth transition.
        /// </summary>
        private IEnumerator CrossfadeCoroutine(MusicTrack newTrack, float duration)
        {
            _isCrossfading = true;

            // Load new track if not in cache
            AudioClip newClip = null;
            if (_musicCache.ContainsKey(newTrack))
            {
                newClip = _musicCache[newTrack];
            }
            else
            {
                yield return LoadMusicTrack(newTrack);
                if (_musicCache.ContainsKey(newTrack))
                    newClip = _musicCache[newTrack];
            }

            if (newClip == null)
            {
                _isCrossfading = false;
                yield break;
            }

            // Swap source references for crossfade
            AudioSource oldSource = _musicSource;
            AudioSource newSource = _crossfadeSource;

            // Start playing new track at zero volume
            newSource.clip = newClip;
            newSource.volume = 0f;
            newSource.loop = true;
            newSource.Play();

            // Perform crossfade
            float elapsedTime = 0f;
            float startVolume = oldSource.volume;
            float targetVolume = GetEffectiveMusicVolume();

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                oldSource.volume = Mathf.Lerp(startVolume, 0f, t);
                newSource.volume = Mathf.Lerp(0f, targetVolume, t);

                yield return null;
            }

            // Clean up old source
            oldSource.Stop();
            oldSource.volume = targetVolume;

            // Swap references for next crossfade
            _musicSource = newSource;
            _crossfadeSource = oldSource;

            // Unload old track from cache to save memory
            if (_musicCache.ContainsKey(_currentMusicTrack))
            {
                _musicCache.Remove(_currentMusicTrack);
            }

            _currentMusicTrack = newTrack;
            _isCrossfading = false;
        }

        /// <summary>
        /// Pauses the currently playing music. Can be resumed with ResumeMusic().
        /// </summary>
        public void PauseMusic()
        {
            try
            {
                _musicSource.Pause();
                _crossfadeSource.Pause();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "PauseMusic", e);
            }
        }

        /// <summary>
        /// Resumes previously paused music playback.
        /// </summary>
        public void ResumeMusic()
        {
            try
            {
                _musicSource.UnPause();
                _crossfadeSource.UnPause();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "ResumeMusic", e);
            }
        }

        #endregion // Music Control

        #region Ambient Control

        /// <summary>
        /// Plays an ambient environmental sound. Ambient sounds loop by default
        /// to create continuous atmosphere.
        /// </summary>
        /// <param name="ambient">The ambient sound to play</param>
        /// <param name="fadeInTime">Duration of fade-in effect in seconds (0 for immediate)</param>
        public void PlayAmbient(AmbientSound ambient, float fadeInTime = 0f)
        {
            try
            {
                if (ambient == AmbientSound.None)
                {
                    StopAmbient(fadeInTime);
                    return;
                }

                // Don't restart if already playing the same ambient
                if (_currentAmbient == ambient && _ambientSource.isPlaying)
                    return;

                StartCoroutine(PlayAmbientCoroutine(ambient, fadeInTime));
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "PlayAmbient", e);
            }
        }

        /// <summary>
        /// Coroutine that handles ambient playback with loading and fade-in.
        /// </summary>
        private IEnumerator PlayAmbientCoroutine(AmbientSound ambient, float fadeInTime)
        {
            AudioClip clip = null;

            // Check cache first
            if (_ambientCache.ContainsKey(ambient))
            {
                clip = _ambientCache[ambient];
            }
            else
            {
                // Load from disk if not cached
                yield return LoadAmbientSound(ambient);
                if (_ambientCache.ContainsKey(ambient))
                    clip = _ambientCache[ambient];
            }

            if (clip == null)
            {
                AppService.CaptureUiMessage($"Failed to load ambient sound: {ambient}");
                yield break;
            }

            _currentAmbient = ambient;
            _ambientSource.clip = clip;
            _ambientSource.loop = true;  // Ambient always loops

            // Apply fade-in if requested
            if (fadeInTime > 0f)
            {
                _ambientSource.volume = 0f;
                _ambientSource.Play();
                yield return FadeAudioSource(_ambientSource, 0f, GetEffectiveAmbientVolume(), fadeInTime);
            }
            else
            {
                _ambientSource.volume = GetEffectiveAmbientVolume();
                _ambientSource.Play();
            }
        }

        /// <summary>
        /// Stops the currently playing ambient sound with optional fade-out.
        /// </summary>
        /// <param name="fadeOutTime">Duration of fade-out in seconds (0 for immediate stop)</param>
        public void StopAmbient(float fadeOutTime = 0f)
        {
            try
            {
                StartCoroutine(StopAmbientCoroutine(fadeOutTime));
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "StopAmbient", e);
            }
        }

        /// <summary>
        /// Coroutine that handles stopping ambient sound with optional fade-out.
        /// </summary>
        private IEnumerator StopAmbientCoroutine(float fadeOutTime)
        {
            if (fadeOutTime > 0f && _ambientSource.isPlaying)
            {
                yield return FadeAudioSource(_ambientSource, _ambientSource.volume, 0f, fadeOutTime);
            }

            _ambientSource.Stop();
            _currentAmbient = AmbientSound.None;
        }

        /// <summary>
        /// Crossfades from current ambient sound to a new one.
        /// Useful for smooth environment transitions.
        /// </summary>
        /// <param name="newAmbient">The new ambient sound to transition to</param>
        /// <param name="duration">Duration of the crossfade in seconds</param>
        public void CrossfadeToAmbient(AmbientSound newAmbient, float duration = DEFAULT_CROSSFADE_DURATION)
        {
            try
            {
                if (newAmbient == _currentAmbient)
                    return;

                StartCoroutine(CrossfadeAmbientCoroutine(newAmbient, duration));
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "CrossfadeToAmbient", e);
            }
        }

        /// <summary>
        /// Coroutine that performs crossfade between ambient sounds.
        /// </summary>
        private IEnumerator CrossfadeAmbientCoroutine(AmbientSound newAmbient, float duration)
        {
            // Load new ambient if needed
            AudioClip newClip = null;
            if (newAmbient != AmbientSound.None)
            {
                if (_ambientCache.ContainsKey(newAmbient))
                {
                    newClip = _ambientCache[newAmbient];
                }
                else
                {
                    yield return LoadAmbientSound(newAmbient);
                    if (_ambientCache.ContainsKey(newAmbient))
                        newClip = _ambientCache[newAmbient];
                }

                if (newClip == null)
                {
                    yield break;
                }
            }

            // Fade out current ambient
            if (_ambientSource.isPlaying)
            {
                yield return FadeAudioSource(_ambientSource, _ambientSource.volume, 0f, duration / 2f);
                _ambientSource.Stop();
            }

            // Start new ambient if not None
            if (newAmbient != AmbientSound.None && newClip != null)
            {
                _currentAmbient = newAmbient;
                _ambientSource.clip = newClip;
                _ambientSource.loop = true;
                _ambientSource.volume = 0f;
                _ambientSource.Play();
                yield return FadeAudioSource(_ambientSource, 0f, GetEffectiveAmbientVolume(), duration / 2f);
            }
            else
            {
                _currentAmbient = AmbientSound.None;
            }
        }

        #endregion // Ambient Control

        #region SFX Control

        /// <summary>
        /// Plays a sound effect at default volume and pitch.
        /// Uses round-robin allocation from the SFX pool.
        /// </summary>
        /// <param name="sfx">The sound effect to play</param>
        public void PlaySFX(SoundEffect sfx)
        {
            try
            {
                if (sfx == SoundEffect.None)
                    return;

                StartCoroutine(PlaySFXCoroutine(sfx, 1f, 0f));
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "PlaySFX", e);
            }
        }

        /// <summary>
        /// Plays a sound effect with volume scaling and pitch variation.
        /// Useful for adding variety to repetitive sounds like gunfire.
        /// </summary>
        /// <param name="sfx">The sound effect to play</param>
        /// <param name="volumeScale">Volume multiplier (0-1)</param>
        /// <param name="pitchVariation">Random pitch variation range (0 = no variation)</param>
        public void PlaySFXWithVariation(SoundEffect sfx, float volumeScale = 1f, float pitchVariation = 0.1f)
        {
            try
            {
                if (sfx == SoundEffect.None)
                    return;

                StartCoroutine(PlaySFXCoroutine(sfx, volumeScale, pitchVariation));
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "PlaySFXWithVariation", e);
            }
        }

        /// <summary>
        /// Coroutine that handles SFX playback, including loading and pool allocation.
        /// If all sources are busy, it steals the oldest playing source.
        /// </summary>
        private IEnumerator PlaySFXCoroutine(SoundEffect sfx, float volumeScale, float pitchVariation)
        {
            AudioClip clip = null;

            // Check cache first
            if (_sfxCache.ContainsKey(sfx))
            {
                clip = _sfxCache[sfx];
            }
            else
            {
                // Load SFX from disk
                yield return LoadSFX(sfx);
                if (_sfxCache.ContainsKey(sfx))
                    clip = _sfxCache[sfx];
            }

            if (clip == null)
            {
                yield break;
            }

            // Get next source from pool (round-robin)
            AudioSource source = _sfxPool[_nextSfxIndex];
            _nextSfxIndex = (_nextSfxIndex + 1) % SFX_POOL_SIZE;

            // Stop current sound if source is already playing (stealing)
            if (source.isPlaying)
                source.Stop();

            // Configure and play the sound effect
            source.clip = clip;
            source.volume = GetEffectiveSFXVolume() * volumeScale;
            source.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            source.Play();
        }

        #endregion // SFX Control

        #region Briefing Control

        /// <summary>
        /// Plays a briefing narration audio track. Briefings do not loop and can be skipped.
        /// Only one briefing can play at a time - starting a new one stops the current one.
        /// </summary>
        /// <param name="briefing">The briefing narration to play</param>
        /// <param name="onComplete">Optional callback invoked when briefing completes naturally (not when skipped)</param>
        public void PlayBriefing(BriefingNarration briefing, System.Action onComplete = null)
        {
            try
            {
                if (briefing == BriefingNarration.None)
                {
                    StopBriefing();
                    return;
                }

                // Stop any currently playing briefing
                StopBriefing();

                _briefingCompleteCallback = onComplete;
                _briefingCoroutine = StartCoroutine(PlayBriefingCoroutine(briefing));
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "PlayBriefing", e);
            }
        }

        /// <summary>
        /// Coroutine that handles briefing playback and monitors for natural completion.
        /// </summary>
        private IEnumerator PlayBriefingCoroutine(BriefingNarration briefing)
        {
            AudioClip clip = null;

            // Try cache first
            if (_briefingCache.ContainsKey(briefing))
            {
                clip = _briefingCache[briefing];
            }
            else
            {
                // Load briefing audio from disk
                yield return LoadBriefingNarration(briefing);
                if (_briefingCache.ContainsKey(briefing))
                    clip = _briefingCache[briefing];
            }

            if (clip == null)
            {
                AppService.CaptureUiMessage($"Failed to load briefing narration: {briefing}");
                yield break;
            }

            _currentBriefing = briefing;
            _briefingSource.clip = clip;
            _briefingSource.volume = GetEffectiveBriefingVolume();
            _briefingSource.Play();

            // Wait for briefing to complete naturally
            while (_briefingSource.isPlaying)
            {
                yield return null;
            }

            // Briefing completed without being skipped
            _currentBriefing = BriefingNarration.None;

            // Invoke completion callback if provided
            _briefingCompleteCallback?.Invoke();
            _briefingCompleteCallback = null;
        }

        /// <summary>
        /// Immediately stops the currently playing briefing narration.
        /// This is called when the player skips the briefing.
        /// The completion callback is NOT invoked when manually stopped.
        /// </summary>
        public void StopBriefing()
        {
            try
            {
                // Stop the coroutine if running
                if (_briefingCoroutine != null)
                {
                    StopCoroutine(_briefingCoroutine);
                    _briefingCoroutine = null;
                }

                // Stop audio playback
                if (_briefingSource != null && _briefingSource.isPlaying)
                {
                    _briefingSource.Stop();
                }

                _currentBriefing = BriefingNarration.None;

                // Clear callback without invoking (briefing was skipped)
                _briefingCompleteCallback = null;
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "StopBriefing", e);
            }
        }

        /// <summary>
        /// Checks if a briefing narration is currently playing.
        /// </summary>
        /// <returns>True if briefing audio is playing, false otherwise</returns>
        public bool IsBriefingPlaying()
        {
            return _briefingSource != null && _briefingSource.isPlaying;
        }

        /// <summary>
        /// Gets the currently playing briefing narration.
        /// </summary>
        /// <returns>The current briefing enum value, or None if no briefing is playing</returns>
        public BriefingNarration GetCurrentBriefing()
        {
            return _currentBriefing;
        }

        #endregion // Briefing Control

        #region Volume Control

        /// <summary>
        /// Sets the master volume that affects all audio categories.
        /// </summary>
        /// <param name="volume">Volume level from 0 (silent) to 1 (full volume)</param>
        public void SetMasterVolume(float volume)
        {
            try
            {
                _settings.MasterVolume = Mathf.Clamp01(volume);
                ApplyVolumeSettings();
                SaveSettings();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "SetMasterVolume", e);
            }
        }

        /// <summary>
        /// Sets the music volume level.
        /// </summary>
        /// <param name="volume">Volume level from 0 (silent) to 1 (full volume)</param>
        public void SetMusicVolume(float volume)
        {
            try
            {
                _settings.MusicVolume = Mathf.Clamp01(volume);
                ApplyVolumeSettings();
                SaveSettings();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "SetMusicVolume", e);
            }
        }

        /// <summary>
        /// Sets the ambient sound volume level.
        /// </summary>
        /// <param name="volume">Volume level from 0 (silent) to 1 (full volume)</param>
        public void SetAmbientVolume(float volume)
        {
            try
            {
                _settings.AmbientVolume = Mathf.Clamp01(volume);
                ApplyVolumeSettings();
                SaveSettings();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "SetAmbientVolume", e);
            }
        }

        /// <summary>
        /// Sets the sound effects volume level.
        /// </summary>
        /// <param name="volume">Volume level from 0 (silent) to 1 (full volume)</param>
        public void SetSFXVolume(float volume)
        {
            try
            {
                _settings.SFXVolume = Mathf.Clamp01(volume);
                ApplyVolumeSettings();
                SaveSettings();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "SetSFXVolume", e);
            }
        }

        /// <summary>
        /// Sets the briefing narration volume level.
        /// </summary>
        /// <param name="volume">Volume level from 0 (silent) to 1 (full volume)</param>
        public void SetBriefingVolume(float volume)
        {
            try
            {
                _settings.BriefingVolume = Mathf.Clamp01(volume);
                ApplyVolumeSettings();
                SaveSettings();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "SetBriefingVolume", e);
            }
        }

        /// <summary>
        /// Toggles music mute state on/off.
        /// </summary>
        public void ToggleMuteMusic()
        {
            try
            {
                _settings.MuteMusic = !_settings.MuteMusic;
                ApplyVolumeSettings();
                SaveSettings();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "ToggleMuteMusic", e);
            }
        }

        /// <summary>
        /// Toggles ambient sound mute state on/off.
        /// </summary>
        public void ToggleMuteAmbient()
        {
            try
            {
                _settings.MuteAmbient = !_settings.MuteAmbient;
                ApplyVolumeSettings();
                SaveSettings();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "ToggleMuteAmbient", e);
            }
        }

        /// <summary>
        /// Toggles sound effects mute state on/off.
        /// </summary>
        public void ToggleMuteSFX()
        {
            try
            {
                _settings.MuteSFX = !_settings.MuteSFX;
                ApplyVolumeSettings();
                SaveSettings();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "ToggleMuteSFX", e);
            }
        }

        /// <summary>
        /// Toggles briefing narration mute state on/off.
        /// </summary>
        public void ToggleMuteBriefing()
        {
            try
            {
                _settings.MuteBriefing = !_settings.MuteBriefing;
                ApplyVolumeSettings();
                SaveSettings();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "ToggleMuteBriefing", e);
            }
        }

        /// <summary>
        /// Toggles global mute state affecting all audio.
        /// </summary>
        public void ToggleMuteAll()
        {
            try
            {
                _settings.MuteAll = !_settings.MuteAll;
                ApplyVolumeSettings();
                SaveSettings();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "ToggleMuteAll", e);
            }
        }

        /// <summary>
        /// Applies current volume settings to all active audio sources.
        /// Updates volumes immediately for playing sounds.
        /// </summary>
        private void ApplyVolumeSettings()
        {
            float musicVolume = GetEffectiveMusicVolume();
            float ambientVolume = GetEffectiveAmbientVolume();
            float sfxVolume = GetEffectiveSFXVolume();
            float briefingVolume = GetEffectiveBriefingVolume();

            // Apply music volume
            if (_musicSource != null)
                _musicSource.volume = musicVolume;
            if (_crossfadeSource != null)
                _crossfadeSource.volume = _isCrossfading ? _crossfadeSource.volume : musicVolume;

            // Apply ambient volume
            if (_ambientSource != null && !_ambientSource.isPlaying)
                _ambientSource.volume = ambientVolume;

            // Apply briefing volume (only if not actively playing to avoid interruption)
            if (_briefingSource != null && !_briefingSource.isPlaying)
                _briefingSource.volume = briefingVolume;

            // Apply SFX volume to inactive sources (active ones keep their current volume)
            if (_sfxPool != null)
            {
                foreach (var source in _sfxPool)
                {
                    if (source != null && !source.isPlaying)
                        source.volume = sfxVolume;
                }
            }
        }

        /// <summary>
        /// Calculates the effective music volume considering master volume and mute states.
        /// </summary>
        /// <returns>Final music volume from 0 to 1</returns>
        private float GetEffectiveMusicVolume()
        {
            if (_settings.MuteAll || _settings.MuteMusic)
                return 0f;
            return _settings.MusicVolume * _settings.MasterVolume;
        }

        /// <summary>
        /// Calculates the effective ambient volume considering master volume and mute states.
        /// </summary>
        /// <returns>Final ambient volume from 0 to 1</returns>
        private float GetEffectiveAmbientVolume()
        {
            if (_settings.MuteAll || _settings.MuteAmbient)
                return 0f;
            return _settings.AmbientVolume * _settings.MasterVolume;
        }

        /// <summary>
        /// Calculates the effective SFX volume considering master volume and mute states.
        /// </summary>
        /// <returns>Final SFX volume from 0 to 1</returns>
        private float GetEffectiveSFXVolume()
        {
            if (_settings.MuteAll || _settings.MuteSFX)
                return 0f;
            return _settings.SFXVolume * _settings.MasterVolume;
        }

        /// <summary>
        /// Calculates the effective briefing volume considering master volume and mute states.
        /// </summary>
        /// <returns>Final briefing volume from 0 to 1</returns>
        private float GetEffectiveBriefingVolume()
        {
            if (_settings.MuteAll || _settings.MuteBriefing)
                return 0f;
            return _settings.BriefingVolume * _settings.MasterVolume;
        }

        /// <summary>
        /// Gets a copy of the current audio settings.
        /// </summary>
        /// <returns>Current AudioSettings object</returns>
        public AudioSettings GetCurrentSettings()
        {
            return _settings;
        }

        #endregion // Volume Control

        #region Audio Loading

        /// <summary>
        /// Loads a music track from StreamingAssets and caches it for future use.
        /// </summary>
        private IEnumerator LoadMusicTrack(MusicTrack track)
        {
            if (!MusicTrackFiles.TryGetValue(track, out string filename))
            {
                AppService.HandleException("GameAudioManager", "LoadMusicTrack",
                    new Exception($"No file mapping for music track: {track}"));
                yield break;
            }

            string path = Path.Combine(Application.streamingAssetsPath, MUSIC_FOLDER, filename);
            string url = "file:///" + path.Replace("\\", "/");

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
            {
                // Load complete file for PC platform (no streaming needed)
                ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = false;
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    _musicCache[track] = clip;
                }
                else
                {
                    AppService.HandleException("GameAudioManager", "LoadMusicTrack",
                        new Exception($"Failed to load {filename}: {www.error}"));
                }
            }
        }

        /// <summary>
        /// Loads an ambient sound from StreamingAssets and caches it for future use.
        /// </summary>
        private IEnumerator LoadAmbientSound(AmbientSound ambient)
        {
            if (!AmbientSoundFiles.TryGetValue(ambient, out string filename))
            {
                AppService.HandleException("GameAudioManager", "LoadAmbientSound",
                    new Exception($"No file mapping for ambient sound: {ambient}"));
                yield break;
            }

            string path = Path.Combine(Application.streamingAssetsPath, AMBIENT_FOLDER, filename);
            string url = "file:///" + path.Replace("\\", "/");

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
            {
                // Load complete file for reliable looping
                ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = false;
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    _ambientCache[ambient] = clip;
                }
                else
                {
                    AppService.HandleException("GameAudioManager", "LoadAmbientSound",
                        new Exception($"Failed to load {filename}: {www.error}"));
                }
            }
        }

        /// <summary>
        /// Loads a sound effect from StreamingAssets and caches it for future use.
        /// </summary>
        private IEnumerator LoadSFX(SoundEffect sfx)
        {
            if (!SoundEffectFiles.TryGetValue(sfx, out string filename))
            {
                AppService.HandleException("GameAudioManager", "LoadSFX",
                    new Exception($"No file mapping for SFX: {sfx}"));
                yield break;
            }

            string path = Path.Combine(Application.streamingAssetsPath, SFX_FOLDER, filename);
            string url = "file:///" + path.Replace("\\", "/");

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    _sfxCache[sfx] = clip;
                }
                else
                {
                    AppService.HandleException("GameAudioManager", "LoadSFX",
                        new Exception($"Failed to load {filename}: {www.error}"));
                }
            }
        }

        /// <summary>
        /// Loads a briefing narration from StreamingAssets and caches it for future use.
        /// </summary>
        private IEnumerator LoadBriefingNarration(BriefingNarration briefing)
        {
            if (!BriefingFiles.TryGetValue(briefing, out string filename))
            {
                AppService.HandleException("GameAudioManager", "LoadBriefingNarration",
                    new Exception($"No file mapping for briefing: {briefing}"));
                yield break;
            }

            string path = Path.Combine(Application.streamingAssetsPath, BRIEFING_FOLDER, filename);
            string url = "file:///" + path.Replace("\\", "/");

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
            {
                // Don't stream briefings - load them completely for reliable playback
                ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = false;
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    _briefingCache[briefing] = clip;
                }
                else
                {
                    AppService.HandleException("GameAudioManager", "LoadBriefingNarration",
                        new Exception($"Failed to load {filename}: {www.error}"));
                }
            }
        }

        /// <summary>
        /// Preloads multiple music tracks into cache for instant playback.
        /// Useful during loading screens to prepare audio for upcoming scenes.
        /// </summary>
        /// <param name="tracks">Array of music tracks to preload</param>
        public void PreloadMusic(params MusicTrack[] tracks)
        {
            try
            {
                foreach (var track in tracks)
                {
                    if (!_musicCache.ContainsKey(track))
                        StartCoroutine(LoadMusicTrack(track));
                }
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "PreloadMusic", e);
            }
        }

        /// <summary>
        /// Preloads multiple ambient sounds into cache for instant playback.
        /// </summary>
        /// <param name="ambients">Array of ambient sounds to preload</param>
        public void PreloadAmbient(params AmbientSound[] ambients)
        {
            try
            {
                foreach (var ambient in ambients)
                {
                    if (!_ambientCache.ContainsKey(ambient))
                        StartCoroutine(LoadAmbientSound(ambient));
                }
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "PreloadAmbient", e);
            }
        }

        /// <summary>
        /// Preloads multiple sound effects into cache for instant playback.
        /// Essential for frequently used sounds like UI clicks and combat effects.
        /// </summary>
        /// <param name="effects">Array of sound effects to preload</param>
        public void PreloadSFX(params SoundEffect[] effects)
        {
            try
            {
                foreach (var sfx in effects)
                {
                    if (!_sfxCache.ContainsKey(sfx))
                        StartCoroutine(LoadSFX(sfx));
                }
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "PreloadSFX", e);
            }
        }

        /// <summary>
        /// Preloads briefing narration files into cache for instant playback.
        /// Useful for loading all briefings for a mission during loading screen.
        /// </summary>
        /// <param name="briefings">Array of briefing narrations to preload</param>
        public void PreloadBriefings(params BriefingNarration[] briefings)
        {
            try
            {
                foreach (var briefing in briefings)
                {
                    if (!_briefingCache.ContainsKey(briefing))
                        StartCoroutine(LoadBriefingNarration(briefing));
                }
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "PreloadBriefings", e);
            }
        }

        /// <summary>
        /// Unloads cached audio that isn't currently playing to free memory.
        /// Keeps current music, ambient, and common UI sounds in cache.
        /// </summary>
        public void UnloadUnusedAudio()
        {
            try
            {
                // Clear music cache except current track
                var currentTrack = _currentMusicTrack;
                _musicCache.Clear();
                if (currentTrack != MusicTrack.None)
                    StartCoroutine(LoadMusicTrack(currentTrack));

                // Clear ambient cache except current ambient
                var currentAmbientSound = _currentAmbient;
                _ambientCache.Clear();
                if (currentAmbientSound != AmbientSound.None)
                    StartCoroutine(LoadAmbientSound(currentAmbientSound));

                // Clear SFX cache completely (they're small and quick to reload)
                _sfxCache.Clear();

                // Clear briefing cache completely (briefings are scene-specific)
                _briefingCache.Clear();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "UnloadUnusedAudio", e);
            }
        }

        #endregion // Audio Loading

        #region Settings Persistence

        /// <summary>
        /// Loads audio settings from persistent storage.
        /// Creates default settings file if none exists.
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                _settingsPath = Path.Combine(Application.persistentDataPath, "audio_settings.json");

                if (File.Exists(_settingsPath))
                {
                    string json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<AudioSettings>(json);
                }
                else
                {
                    // Create default settings on first run
                    _settings = new AudioSettings();
                    SaveSettings();
                }
            }
            catch (Exception e)
            {
                // Fall back to defaults if loading fails
                AppService.HandleException("GameAudioManager", "LoadSettings", e);
                _settings = new AudioSettings();
            }
        }

        /// <summary>
        /// Saves current audio settings to persistent storage as JSON.
        /// Settings persist across game sessions.
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                if (_settings == null || string.IsNullOrEmpty(_settingsPath))
                    return;

                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "SaveSettings", e);
            }
        }

        #endregion // Settings Persistence

        #region Helper Methods

        /// <summary>
        /// Smoothly fades an audio source between two volume levels over time.
        /// Used for fade-in/fade-out effects and crossfading.
        /// </summary>
        /// <param name="source">The audio source to fade</param>
        /// <param name="startVolume">Starting volume level</param>
        /// <param name="endVolume">Target volume level</param>
        /// <param name="duration">Fade duration in seconds</param>
        private IEnumerator FadeAudioSource(AudioSource source, float startVolume, float endVolume, float duration)
        {
            float elapsedTime = 0f;
            source.volume = startVolume;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                source.volume = Mathf.Lerp(startVolume, endVolume, t);
                yield return null;
            }

            source.volume = endVolume;
        }

        /// <summary>
        /// Manually releases all audio resources. Call before scene transitions
        /// or when audio system is no longer needed.
        /// </summary>
        public void ReleaseAllResources()
        {
            try
            {
                // Stop all playback
                StopMusic(0f);
                StopAmbient(0f);
                StopBriefing();

                // Clear all caches
                _musicCache.Clear();
                _ambientCache.Clear();
                _sfxCache.Clear();
                _briefingCache.Clear();

                // Force garbage collection for large audio cleanup
                System.GC.Collect();
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "ReleaseAllResources", e);
            }
        }

        #endregion // Helper Methods
    }
}