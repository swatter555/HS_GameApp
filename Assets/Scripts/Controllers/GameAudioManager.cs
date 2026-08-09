using HammerAndSickle.Audio;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
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
                        GameObject go = new("GameAudioManager");
                        _instance = go.AddComponent<GameAudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// The existing instance, or null. NEVER creates one.
        /// </summary>
        /// <remarks>
        /// ⚠ This exists so <see cref="Audio.GameAudio"/> can be called from anywhere without the
        /// side effect that makes <see cref="Instance"/> dangerous: its getter BUILDS a GameObject, so a
        /// call from model code or a headless EditorTest would silently spawn an audio manager. Playing a
        /// sound must never construct anything. Use this for every read-only or fire-and-forget access.
        /// </remarks>
        public static GameAudioManager Existing => _instance;

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
        /// Clips are supplied by the AudioCatalog (Assets/Resources/Audio/AudioCatalog.asset).
        /// </summary>
        /// <remarks>
        /// ⚠ APPEND ONLY. NEVER INSERT OR REORDER A MEMBER. Unity serializes an enum field by its
        /// INTEGER VALUE, not its name, and UIButtonAudio exposes two of them (clickSound, hoverSound)
        /// as [SerializeField] — the scene YAML literally reads "clickSound: 1". Inserting a member
        /// mid-enum therefore silently repoints EVERY Inspector-assigned button sound in every scene
        /// and prefab, with no compile error and no warning. Same hazard as the persisted-enum rule in
        /// CLAUDE.md item 11, but the payload here is scene YAML rather than save files.
        /// RENAMING a member IS safe (the value is unchanged) — MeduimSnareDrum -> MediumSnareDrum was
        /// fixed that way on 2026-08-03. Add new effects at the END of the list.
        ///
        /// ⚠ Most members below have NO CLIP YET, which is intentional and safe: a sound with no catalog
        /// row is a SILENT NO-OP, never an error, so call sites may be wired before the audio is authored.
        /// Tools/Audio/Audit Catalog reports which members are still unbacked.
        /// </remarks>
        public enum SoundEffect
        {
            None,
            ButtonClick,
            // ⚠ RETIRED 2026-08-03 — hover audio is deleted (§27.7, UIButtonAudio) and SFX_ButtonHover.wav
            // is gone. THE MEMBER STAYS: this enum is append-only because Unity serializes enum fields as
            // INTEGERS, so removing value 2 would shift MenuOpen 3→2, MenuClose 4→3 and so on, silently
            // repointing every Inspector-assigned button sound in every scene. A retired member is cheap;
            // a renumbered enum is a silent, project-wide defect. Maps to no file and preloads nothing.
            ButtonHover,
            MenuOpen,
            MenuClose,
            RadioButtonClick,
            MediumSnareDrum,
            PrinterTick,

            // Movement SFX
            UnitSelect,
            UnitDeselect,
            MoveOrderConfirm,
            MoveOrderCancel,
            UnitMoveTracked,
            UnitMoveWheeled,
            UnitMoveFoot,
            UnitMoveHelo,
            UnitMoveJet,
            UnitMoveBlocked,
            OutOfMP,
            UnitSpotted,
            AmbushTriggered,
            AmbushDetected,
            FacingChange,
            NextUnit,
            PrevUnit,

            // ─── Appended 2026-08-04 by the Phase 3 wiring pass. ⚠ APPEND ONLY, see the remarks above. ───

            // §24.8.5 — a refused order is FEEDBACK, not a dispatch. One sound covers every refusal:
            // illegal Ctrl+click, an attack the orchestrator rejects, a blocked deploy, a spent intel action.
            ButtonDenied,

            /* Long-form movement cuts (§27.7.7). ONE clip length cannot serve a 3-hex mountain crawl and a
             * 24-hex helicopter transit: the long clip overhangs the short move, the short one ends halfway
             * through the long one. The cut is chosen from the PREDICTED duration, which is known before the
             * move starts. Foot needs no long cut — its longest possible move is 0.7 s. */
            UnitMoveWheeledLong,
            UnitMoveTrackedLong,
            UnitMoveHeloLong,
            UnitMoveJetLong,

            /* Weapon fire — ONE PER WeaponSoundFamily (§27.7.5), never per profile: 177 profiles collapse
             * to these 13. GameAudio.SoundEffectFor owns the mapping and is the only place it lives. */
            FireSmallArms,
            FireHeavyMachineGun,
            FireAutocannon,
            FireTankGun,
            FireAntiTankMissile,
            FireArtilleryGun,
            FireRocketArtillery,
            FireSurfaceToAirMissile,
            FireAntiAircraftGun,
            FireHelicopterAttack,
            FireAircraftCannon,
            FireAircraftGroundAttack,
            FireAircraftBombs,

            /* Impacts. ⚠ The impact is attributed to the TARGET, not the firer (§27.7.4.2) — that is what
             * lets an unseen battery shell the player audibly without identifying itself. Armour/soft is
             * decided by the SHARED classifier, so audio cannot disagree with the loss report. */
            ImpactSoft,
            ImpactArmour,
            ImpactStructure,

            // Outcomes and objectives.
            UnitDestroyed,
            ObjectiveCaptured,
            ObjectiveLost
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
        private static readonly Dictionary<MusicTrack, string> MusicTrackFiles = new()
        {
            { MusicTrack.MainMenu, "Music_MainMenu.ogg" }
        };

        /// <summary>
        /// Maps AmbientSound enum values to their corresponding OGG filenames.
        /// Used for loading ambient audio files from StreamingAssets.
        /// </summary>
        private static readonly Dictionary<AmbientSound, string> AmbientSoundFiles = new()
        {
            { AmbientSound.AmbientCombat, "Ambient_DistantCombat.ogg" }
        };

        /// <summary>
        /// Maps BriefingNarration enum values to their corresponding OGG filenames.
        /// Used for loading briefing audio files from StreamingAssets.
        /// </summary>
        private static readonly Dictionary<BriefingNarration, string> BriefingFiles = new()
        {
            { BriefingNarration.Khost, "Briefing_Khost.ogg" }
        };

        /// <summary>
        /// The movement sound for a medium. <paramref name="longCut"/> selects the extended recording for
        /// the media whose maximum move can outrun a standard clip.
        /// </summary>
        /// <remarks>
        /// ⚠ REPLACES A `UnitClassification` SWITCH, AND THE AXIS CHANGE IS THE POINT. Classification says
        /// what a regiment IS, not what is carrying it: an air-assault regiment is `MAM` whether it is
        /// walking, riding its MT-LBs or flying, so it sounded like infantry in all three postures. Medium
        /// comes off the ACTIVE PROFILE, so the three postures are three different sounds with no
        /// posture special-case anywhere. The earlier "is it dismounted?" rule is gone with the switch —
        /// it was a patch for asking the wrong object.
        /// ⚠ Foot has NO long cut: its longest possible move is well inside any recorded clip.
        /// ⚠ Static and Naval return None deliberately — a base does not move, and naval movement (§5.4.2)
        /// is unbuilt. None is silent, which is the correct failure.
        /// </remarks>
        public static SoundEffect GetMovementSFX(MovementMedium medium, bool longCut = false) => medium switch
        {
            MovementMedium.Foot      => SoundEffect.UnitMoveFoot,
            MovementMedium.Wheeled   => longCut ? SoundEffect.UnitMoveWheeledLong : SoundEffect.UnitMoveWheeled,
            MovementMedium.Tracked   => longCut ? SoundEffect.UnitMoveTrackedLong : SoundEffect.UnitMoveTracked,
            MovementMedium.Helo      => longCut ? SoundEffect.UnitMoveHeloLong : SoundEffect.UnitMoveHelo,
            MovementMedium.FixedWing => longCut ? SoundEffect.UnitMoveJetLong : SoundEffect.UnitMoveJet,
            _ => SoundEffect.None
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
        /* SFX sources, split by whether the sound needs pitch variation. Pitch is a per-SOURCE property,
         * so retuning a source warps any one-shot still ringing on it — UI sounds therefore get a source
         * whose pitch is NEVER touched, and varied sounds round-robin the rest. See SfxPlayer. */
        private AudioSource _sfxFlatSource;
        private AudioSource[] _sfxPitchPool;

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

        /* Clip caching for the STREAMED channels only. ⚠ There is no SFX cache any more, and its absence is
         * the point: sound effects are imported project assets with Preload Audio Data on (§7.1a), so they
         * are resident before anything asks for them. The cache, negative cache, in-flight guard, preload
         * step, UI-vs-gameplay API split and drop-if-not-resident rule that used to live here ALL existed
         * only to manage clips not being in memory, and all deleted together on 2026-08-03. */
        private readonly Dictionary<MusicTrack, AudioClip> _musicCache = new();
        private readonly Dictionary<AmbientSound, AudioClip> _ambientCache = new();
        private readonly Dictionary<BriefingNarration, AudioClip> _briefingCache = new();

        // SFX: the catalog is the data, SfxPlayer is the playback. Both resolved once at Awake.
        private AudioCatalog _sfxCatalog;
        private SfxPlayer _sfxPlayer;

        // Configuration constants
        private const float DEFAULT_CROSSFADE_DURATION = 1.5f; // Industry standard crossfade time in seconds
        private const int SFX_POOL_SIZE = 10;                  // Number of simultaneous sound effects supported
        private const string MUSIC_FOLDER = "Audio/Music";
        private const string AMBIENT_FOLDER = "Audio/Ambient";
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

                InitializeSfx();
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

                if (_sfxPitchPool != null)
                {
                    foreach (var source in _sfxPitchPool)
                    {
                        if (source != null) source.Stop();
                    }
                }

                // Clear cached audio clips to free memory
                _musicCache?.Clear();
                _ambientCache?.Clear();
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
                GameObject musicObj = new("MusicSource");
                musicObj.transform.parent = transform;
                _musicSource = musicObj.AddComponent<AudioSource>();
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;

                // Create secondary music source for crossfade transitions
                GameObject crossfadeObj = new("CrossfadeSource");
                crossfadeObj.transform.parent = transform;
                _crossfadeSource = crossfadeObj.AddComponent<AudioSource>();
                _crossfadeSource.loop = true;
                _crossfadeSource.playOnAwake = false;

                // Create ambient sound source for environmental atmosphere
                GameObject ambientObj = new("AmbientSource");
                ambientObj.transform.parent = transform;
                _ambientSource = ambientObj.AddComponent<AudioSource>();
                _ambientSource.loop = true;  // Ambient sounds loop by default
                _ambientSource.playOnAwake = false;

                // Create dedicated source for briefing narration (non-looping)
                GameObject briefingObj = new("BriefingSource");
                briefingObj.transform.parent = transform;
                _briefingSource = briefingObj.AddComponent<AudioSource>();
                _briefingSource.loop = false;
                _briefingSource.playOnAwake = false;

                // The FLAT source: one AudioSource whose pitch is never modified, carrying every sound with
                // no pitch variation (all UI). ⚠ It needs no pool because PlayOneShot MIXES — one source
                // hosts any number of overlapping one-shots. The pool below exists only because pitch is
                // per-source, not because concurrency demands it.
                GameObject flatObj = new("SFXSource_Flat");
                flatObj.transform.parent = transform;
                _sfxFlatSource = flatObj.AddComponent<AudioSource>();
                _sfxFlatSource.loop = false;
                _sfxFlatSource.playOnAwake = false;

                // Pitch-varied sources, round-robined so a new pitch rarely lands on a source that is
                // still ringing.
                _sfxPitchPool = new AudioSource[SFX_POOL_SIZE];
                for (int i = 0; i < SFX_POOL_SIZE; i++)
                {
                    GameObject sfxObj = new($"SFXSource_Pitch_{i}");
                    sfxObj.transform.parent = transform;
                    _sfxPitchPool[i] = sfxObj.AddComponent<AudioSource>();
                    _sfxPitchPool[i].loop = false;
                    _sfxPitchPool[i].playOnAwake = false;
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
        /// Resolves the catalog and builds the player. Called from Awake, after the AudioSources exist.
        /// </summary>
        private void InitializeSfx()
        {
            _sfxCatalog = Resources.Load<AudioCatalog>(AudioCatalog.ResourcePath);
            if (_sfxCatalog == null)
            {
                // Loud, and it names the fix — with no catalog EVERY sound effect is silent, and silence
                // is otherwise indistinguishable from "the audio just is not wired yet".
                Debug.LogError($"[{nameof(GameAudioManager)}] No AudioCatalog at Resources/{AudioCatalog.ResourcePath}. " +
                               "All sound effects will be silent. Create it via Tools/Audio/Create Or Update Audio Catalog.");
            }

            _sfxPlayer = new SfxPlayer(_sfxFlatSource, _sfxPitchPool);
        }

        /// <summary>
        /// Length in seconds of the shortest usable clip backing a sound; 0 when it has no clip yet.
        /// Lets a caller ask whether a recording will cover the event it is being played for, instead of
        /// comparing against a hard-coded duration that describes an asset the code can simply measure.
        /// </summary>
        public float ClipSecondsFor(SoundEffect id)
        {
            if (id == SoundEffect.None || _sfxCatalog == null) return 0f;
            return _sfxCatalog.TryGet(id, out AudioCatalog.Entry entry) ? entry.ShortestClipSeconds : 0f;
        }

        /// <summary>
        /// Plays a sound effect. ⚠ INTERNAL PLUMBING — call <see cref="Audio.GameAudio"/> instead, which
        /// carries the fog gate (§27.7.4). This method is deliberately NOT fog-aware: putting the gate here
        /// would silently apply it to UI and turn sounds that have no source unit, and hide from call sites
        /// the question of which unit a sound is attributed to.
        /// </summary>
        public void PlaySfx(SoundEffect id, float volumeScale = 1f)
        {
            try
            {
                if (id == SoundEffect.None || _sfxCatalog == null || _sfxPlayer == null) return;
                if (!_sfxCatalog.TryGet(id, out AudioCatalog.Entry entry)) return;

                // Unscaled so audio still behaves if the game is ever paused at timeScale 0.
                _sfxPlayer.Play(entry, GetEffectiveSFXVolume(), volumeScale, Time.unscaledTime);
            }
            catch (Exception e)
            {
                AppService.HandleException("GameAudioManager", "PlaySfx", e);
            }
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
            if (_sfxPitchPool != null)
            {
                foreach (var source in _sfxPitchPool)
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

        /* NEGATIVE CACHING — the loaders below cache a NULL on failure so a missing file is reported
         * ONCE rather than re-requested on every play attempt. ⚠ Scoped to the STREAMED channels now:
         * sound effects left this path entirely on 2026-08-03 (§7.1a) and are imported assets. */

        /// <summary>
        /// Loads a music track from StreamingAssets and caches it for future use.
        /// Caches null on failure so a missing file is reported once, not once per play attempt.
        /// </summary>
        private IEnumerator LoadMusicTrack(MusicTrack track)
        {
            if (!MusicTrackFiles.TryGetValue(track, out string filename))
            {
                _musicCache[track] = null;
                AppService.HandleException("GameAudioManager", "LoadMusicTrack",
                    new Exception($"No file mapping for music track: {track}"));
                yield break;
            }

            string path = Path.Combine(Application.streamingAssetsPath, MUSIC_FOLDER, filename);
            string url = "file:///" + path.Replace("\\", "/");

            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS);
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
                _musicCache[track] = null;
                AppService.HandleException("GameAudioManager", "LoadMusicTrack",
                    new Exception($"Failed to load {filename}: {www.error}"));
            }
        }

        /// <summary>
        /// Loads an ambient sound from StreamingAssets and caches it for future use.
        /// </summary>
        private IEnumerator LoadAmbientSound(AmbientSound ambient)
        {
            if (!AmbientSoundFiles.TryGetValue(ambient, out string filename))
            {
                _ambientCache[ambient] = null;
                AppService.HandleException("GameAudioManager", "LoadAmbientSound",
                    new Exception($"No file mapping for ambient sound: {ambient}"));
                yield break;
            }

            string path = Path.Combine(Application.streamingAssetsPath, AMBIENT_FOLDER, filename);
            string url = "file:///" + path.Replace("\\", "/");

            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS);
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
                _ambientCache[ambient] = null;
                AppService.HandleException("GameAudioManager", "LoadAmbientSound",
                    new Exception($"Failed to load {filename}: {www.error}"));
            }
        }

        /// <summary>
        /// Loads a briefing narration from StreamingAssets and caches it for future use.
        /// Caches null on failure so a missing file is reported once, not once per play attempt.
        /// </summary>
        /// <remarks>
        /// ⚠ FLAGGED, NOT CHANGED (2026-08-03): these two failure branches still report through
        /// HandleException, but DesignDoc 20.4.2 ratifies that briefing NARRATION is campaign-scenario
        /// ONLY and that an ABSENT narration asset is the NORMAL case for a standalone scenario, never an
        /// error. So the missing-file path here will eventually need to be a clean no-op rather than a
        /// logged exception. It is not changed now because nothing calls PlayBriefing yet — narration is
        /// dormant, with no manifest field — so the semantics are moot until it is wired, and the right
        /// shape (silent-absent vs. warn-on-corrupt) is a decision for that pass, not this one.
        /// Negative caching at least means the log cannot repeat per attempt in the meantime.
        /// </remarks>
        private IEnumerator LoadBriefingNarration(BriefingNarration briefing)
        {
            if (!BriefingFiles.TryGetValue(briefing, out string filename))
            {
                _briefingCache[briefing] = null;
                AppService.HandleException("GameAudioManager", "LoadBriefingNarration",
                    new Exception($"No file mapping for briefing: {briefing}"));
                yield break;
            }

            string path = Path.Combine(Application.streamingAssetsPath, BRIEFING_FOLDER, filename);
            string url = "file:///" + path.Replace("\\", "/");

            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS);
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
                _briefingCache[briefing] = null;
                AppService.HandleException("GameAudioManager", "LoadBriefingNarration",
                    new Exception($"Failed to load {filename}: {www.error}"));
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

                // ⚠ SFX are NOT unloaded: they are imported assets held resident by their import
                // settings (§7.1a), not runtime-cached, so there is nothing here to free and nothing to
                // re-warm. Only the streamed channels have a cache to clear.

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
