using System;
using System.Reflection;
using HammerAndSickle.Audio;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using NUnit.Framework;
using UnityEngine;
using SoundEffect = HammerAndSickle.Controllers.GameAudioManager.SoundEffect;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// The Phase 2 SFX plumbing — <see cref="AudioCatalog"/> lookup, <see cref="SfxPlayer"/>'s retrigger
    /// window, and the <see cref="GameAudio"/> facade's never-lazy-create guarantee.
    /// (<c>AudioPolicyTests</c> covers the RULES — fog gating and family mapping; this covers the machinery.)
    ///
    /// ⚠ EVERY FAILURE MODE HERE IS SILENCE, which is why the suite exists. A missing row, a duplicate row,
    /// a clip slot left unassigned and a debounce that swallows a legitimate second sound all produce
    /// exactly the same symptom in play — nothing happens — and none of them throw. Silence is also the
    /// CORRECT behaviour for an unmapped id (§27.7.5.2: the enum deliberately runs ahead of the audio, so
    /// call sites can be wired before the wav is recorded), so "no sound" cannot be treated as a bug
    /// report. The only way to tell the intended silence from the accidental kind is to pin them.
    ///
    /// ⚠ NO AudioSources ARE CREATED. `SfxPlayer` takes its sources by constructor injection and its
    /// retrigger predicate is pure, which is what lets the window be exercised headlessly.
    /// </summary>
    [TestFixture]
    public class AudioSystemTests
    {
        #region Fixture

        /* ⚠ ONE reflection point, deliberately. `entries` is a private [SerializeField], so a test has no
         * other way to author a catalog in memory. AudioCatalogTools.CreateOrUpdate reaches the same field
         * by the same string through SerializedObject, so a rename breaks both together rather than one
         * silently — and SetUp asserts on it so that break reads as "the field was renamed" instead of a
         * NullReferenceException twenty lines later. */
        private static readonly FieldInfo EntriesField =
            typeof(AudioCatalog).GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);

        private AudioCatalog _catalog;
        private AudioClip _clipA;
        private AudioClip _clipB;

        [SetUp]
        public void SetUp()
        {
            Assert.That(EntriesField, Is.Not.Null,
                "AudioCatalog.entries was renamed — update this fixture AND AudioCatalogTools.CreateOrUpdate, " +
                "which finds the same field by the same name.");

            _catalog = ScriptableObject.CreateInstance<AudioCatalog>();

            // A few milliseconds of silence each. Nothing here plays them; they exist so HasClip/PickClip
            // have something real to return, since a Unity object reference cannot be faked.
            _clipA = AudioClip.Create("TestClipA", 128, 1, 44100, false);
            _clipB = AudioClip.Create("TestClipB", 128, 1, 44100, false);

            Assert.That(_clipA, Is.Not.Null, "AudioClip.Create returned null — the clip tests below are meaningless");
        }

        [TearDown]
        public void TearDown()
        {
            if (_catalog != null) UnityEngine.Object.DestroyImmediate(_catalog);
            if (_clipA != null) UnityEngine.Object.DestroyImmediate(_clipA);
            if (_clipB != null) UnityEngine.Object.DestroyImmediate(_clipB);
        }

        /// <summary>A default-tuned row. Pass no clips for the "row exists but is silent" case.</summary>
        private static AudioCatalog.Entry Row(SoundEffect id, params AudioClip[] clips) =>
            new AudioCatalog.Entry
            {
                id = id,
                clips = clips,
                volume = 1f,
                pitchVariation = 0f,
                minRetriggerSeconds = 0f
            };

        /// <summary>Replaces the catalog's rows and drops the cached lookup, as an Inspector edit would.</summary>
        private void SetRows(params AudioCatalog.Entry[] rows)
        {
            EntriesField.SetValue(_catalog, rows);
            _catalog.Invalidate();
        }

        #endregion // Fixture

        #region Catalog lookup

        [Test]
        public void TryGet_ReturnsTheRow_WithItsTuningIntact()
        {
            // The tuning has to survive the lookup because SfxPlayer reads volume/pitch/retrigger off the
            // returned row — that is what makes "tuning a sound needs no code change" true.
            var row = Row(SoundEffect.ButtonClick, _clipA);
            row.volume = 0.5f;
            row.pitchVariation = 0.1f;
            row.minRetriggerSeconds = 0.25f;
            SetRows(row);

            Assert.That(_catalog.TryGet(SoundEffect.ButtonClick, out AudioCatalog.Entry found), Is.True);
            Assert.That(found.volume, Is.EqualTo(0.5f));
            Assert.That(found.pitchVariation, Is.EqualTo(0.1f));
            Assert.That(found.minRetriggerSeconds, Is.EqualTo(0.25f));
        }

        [Test]
        public void UnmappedId_IsASilentNoOp_NotAThrow()
        {
            SetRows(Row(SoundEffect.ButtonClick, _clipA));

            Assert.That(_catalog.TryGet(SoundEffect.PrinterTick, out AudioCatalog.Entry found), Is.False,
                "a sound with no row must report false, never throw — §27.7.5.2");
            Assert.That(found, Is.Null, "a false lookup must not hand back a row the caller might use");
        }

        [Test]
        public void EverySoundEffectMember_IsSilent_AgainstAnEmptyCatalog()
        {
            // The state the project is actually in: 24 enum members, 6 clips, ~85 sounds planned. Wiring a
            // call site ahead of its audio has to be safe, or the inventory in todo_audio §4b is unbuildable.
            SetRows();

            foreach (SoundEffect id in Enum.GetValues(typeof(SoundEffect)))
                Assert.That(_catalog.TryGet(id, out _), Is.False, $"{id} against an empty catalog");
        }

        [Test]
        public void RowWithNoUsableClip_ReportsFalse_RatherThanLookingWired()
        {
            // The two shapes Tools/Audio/Audit Catalog warns about. Both must fail the lookup: a row that
            // resolves but cannot produce audio is worse than no row, because the audit is then the only
            // thing standing between an unassigned Inspector slot and a permanently silent sound.
            var noClips = Row(SoundEffect.MenuOpen);
            var nullArray = new AudioCatalog.Entry { id = SoundEffect.MenuClose, clips = null };
            var allNull = new AudioCatalog.Entry
            {
                id = SoundEffect.RadioButtonClick,
                clips = new AudioClip[] { null, null }
            };
            SetRows(noClips, nullArray, allNull);

            Assert.That(_catalog.TryGet(SoundEffect.MenuOpen, out _), Is.False, "empty clip array");
            Assert.That(_catalog.TryGet(SoundEffect.MenuClose, out _), Is.False, "null clip array");
            Assert.That(_catalog.TryGet(SoundEffect.RadioButtonClick, out _), Is.False, "all slots unassigned");
        }

        [Test]
        public void DuplicateRows_TheFirstOneWins()
        {
            // Inspector order must be the truth about which clip plays. Taking the LAST duplicate would
            // make the visible order a lie, and the audit tool reports duplicates on the same assumption.
            var first = Row(SoundEffect.ButtonClick, _clipA);
            first.volume = 0.25f;
            var second = Row(SoundEffect.ButtonClick, _clipB);
            second.volume = 0.75f;
            SetRows(first, second);

            Assert.That(_catalog.TryGet(SoundEffect.ButtonClick, out AudioCatalog.Entry found), Is.True);
            Assert.That(found.volume, Is.EqualTo(0.25f), "the FIRST row must win");
        }

        [Test]
        public void NullRowInTheArray_IsSkipped_NotAThrow()
        {
            // A deleted element in the Inspector array leaves a null. One bad row must not take the whole
            // catalog down — every OTHER sound in the game would go silent with it.
            SetRows(null, Row(SoundEffect.ButtonClick, _clipA), null);

            Assert.That(_catalog.TryGet(SoundEffect.ButtonClick, out _), Is.True);
            Assert.That(_catalog.TryGet(SoundEffect.MenuOpen, out _), Is.False);
        }

        [Test]
        public void Invalidate_MakesALaterEditVisible()
        {
            // The lookup is cached on first use, so without Invalidate an Inspector edit would not take
            // effect until a domain reload — the sort of thing that gets diagnosed as "the clip is broken".
            SetRows(Row(SoundEffect.ButtonClick, _clipA));
            Assert.That(_catalog.TryGet(SoundEffect.ButtonClick, out _), Is.True, "builds the cache");

            SetRows(Row(SoundEffect.ButtonClick, _clipA), Row(SoundEffect.MenuOpen, _clipB));

            Assert.That(_catalog.TryGet(SoundEffect.MenuOpen, out _), Is.True,
                "a row added after the cache was built must be visible once the catalog is invalidated");
        }

        #endregion // Catalog lookup

        #region Variant selection (R4)

        [Test]
        public void PickClip_ReturnsNull_WhenThereIsNothingToPlay()
        {
            Assert.That(new AudioCatalog.Entry { clips = null }.PickClip(), Is.Null);
            Assert.That(Row(SoundEffect.ButtonClick).PickClip(), Is.Null);
            Assert.That(new AudioCatalog.Entry { clips = new AudioClip[] { null, null } }.PickClip(), Is.Null);
        }

        [Test]
        public void PickClip_SkipsAnUnassignedSlot_RatherThanReturningSilence()
        {
            // A half-filled variant array is an authoring slip, not a request for silence — returning null
            // would turn one forgotten slot into an intermittently silent sound, the worst kind to diagnose.
            var row = Row(SoundEffect.ButtonClick, null, _clipA, null);

            Assert.That(row.PickClip(), Is.SameAs(_clipA));
            Assert.That(row.PickClip(), Is.SameAs(_clipA));
        }

        [Test]
        public void PickClip_UsesEveryVariant_NotJustTheFirst()
        {
            // R4: variants exist because a sound heard dozens of times a turn fatigues from one clip. A
            // PickClip that always returned clips[0] would defeat that with no visible symptom.
            // Seeded so the result is deterministic rather than merely overwhelmingly likely.
            UnityEngine.Random.State state = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(20260804);
                var row = Row(SoundEffect.ButtonClick, _clipA, _clipB);

                bool sawA = false, sawB = false;
                for (int i = 0; i < 32; i++)
                {
                    AudioClip pick = row.PickClip();
                    sawA |= ReferenceEquals(pick, _clipA);
                    sawB |= ReferenceEquals(pick, _clipB);
                }

                Assert.That(sawA && sawB, Is.True, "both variants must be reachable");
            }
            finally
            {
                UnityEngine.Random.state = state;
            }
        }

        #endregion // Variant selection (R4)

        #region Retrigger window (D5)

        /* ⚠ NULL SOURCES ON PURPOSE. SfxPlayer takes its AudioSources by constructor injection, and the
         * flat (pitch-variation-0) path books the retrigger timestamp whether or not a source exists — so
         * the whole debounce can be driven headlessly. That is the point of keeping ShouldPlay pure and
         * separate from playback: the window is the part with rules, and it needs no audio system to test. */
        private static SfxPlayer NewHeadlessPlayer() => new SfxPlayer(null, null);

        [Test]
        public void DefaultWindow_IsOff_SoRepeatedPlaysAllPass()
        {
            // D5: the default is 0 = NO LIMIT. A blanket debounce suppresses legitimate audio — a
            // double-click is two events, and several units firing at once is several sounds.
            var player = NewHeadlessPlayer();
            var row = Row(SoundEffect.ButtonClick, _clipA);

            player.Play(row, 1f, 1f, 100f);
            player.Play(row, 1f, 1f, 100f);

            Assert.That(player.ShouldPlay(row.id, row.minRetriggerSeconds, 100f), Is.True,
                "an un-tuned sound must never be debounced");
        }

        [Test]
        public void Window_SuppressesInside_AndAllowsFromTheBoundary()
        {
            // Times are powers of two so the boundary comparison is exact and this cannot fail on float
            // fuzz: 10.25f - 10f is precisely 0.25f.
            var player = NewHeadlessPlayer();
            var row = Row(SoundEffect.PrinterTick, _clipA);
            row.minRetriggerSeconds = 0.25f;

            player.Play(row, 1f, 1f, 10f);

            Assert.That(player.ShouldPlay(row.id, row.minRetriggerSeconds, 10.1f), Is.False, "inside the window");
            Assert.That(player.ShouldPlay(row.id, row.minRetriggerSeconds, 10.25f), Is.True, "the boundary is inclusive");
            Assert.That(player.ShouldPlay(row.id, row.minRetriggerSeconds, 10.5f), Is.True, "past the window");
        }

        [Test]
        public void Window_IsPerSound_NotGlobal()
        {
            // The defect the old global SFX_UI_RETRIGGER_SECONDS constant had: one noisy sound muting
            // every other sound in the game for the length of its window.
            var player = NewHeadlessPlayer();
            var tick = Row(SoundEffect.PrinterTick, _clipA);
            tick.minRetriggerSeconds = 0.25f;
            var click = Row(SoundEffect.ButtonClick, _clipA);
            click.minRetriggerSeconds = 0.25f;

            player.Play(tick, 1f, 1f, 10f);

            Assert.That(player.ShouldPlay(tick.id, tick.minRetriggerSeconds, 10.1f), Is.False);
            Assert.That(player.ShouldPlay(click.id, click.minRetriggerSeconds, 10.1f), Is.True,
                "a different sound must be unaffected");
        }

        [Test]
        public void UnplayedSound_IsAlwaysAllowed_EvenWithAWindowSet()
        {
            var player = NewHeadlessPlayer();

            Assert.That(player.ShouldPlay(SoundEffect.PrinterTick, 5f, 0f), Is.True,
                "no history means nothing to debounce against");
        }

        [Test]
        public void Reset_ClearsTheHistory()
        {
            // Called at a battle boundary so a window opened in one scene cannot swallow the first sound
            // of the next.
            var player = NewHeadlessPlayer();
            var row = Row(SoundEffect.PrinterTick, _clipA);
            row.minRetriggerSeconds = 0.25f;

            player.Play(row, 1f, 1f, 10f);
            Assert.That(player.ShouldPlay(row.id, row.minRetriggerSeconds, 10.1f), Is.False);

            player.Reset();

            Assert.That(player.ShouldPlay(row.id, row.minRetriggerSeconds, 10.1f), Is.True);
        }

        [Test]
        public void Play_ToleratesANullEntry_AndAnEntryWithNoClip()
        {
            // PlaySfx already screens both, but SfxPlayer must not depend on its caller for that — it is
            // the piece a future call site will reach.
            var player = NewHeadlessPlayer();

            Assert.DoesNotThrow(() => player.Play(null, 1f, 1f, 0f));
            Assert.DoesNotThrow(() => player.Play(Row(SoundEffect.ButtonClick), 1f, 1f, 0f));
        }

        #endregion // Retrigger window (D5)

        #region The facade constructs nothing

        [Test]
        public void GameAudio_NeverLazyCreatesAManager()
        {
            /* The claim that makes audio safe to call from model code and from this very suite: GameAudio
             * reaches the manager through `Existing` (a plain static field) and never `Instance` (whose
             * getter BUILDS a GameObject). If someone "simplifies" that back to Instance, every headless
             * test that makes a noise starts spawning an audio manager — the exact trap EventManager and
             * GameDataManager still carry. */
            Assert.That(GameAudioManager.Existing, Is.Null,
                "precondition: nothing in a headless test should have constructed a GameAudioManager");

            var friendly = new CombatUnit("AudioTestUnit", UnitClassification.INF, UnitRole.GroundCombat,
                Side.Player, Nationality.USSR);

            // A friendly source passes the fog gate, so these reach the manager lookup rather than
            // stopping at CanHear — which is the path that would construct something.
            Assert.DoesNotThrow(() => GameAudio.Play(SoundEffect.ButtonClick));
            Assert.DoesNotThrow(() => GameAudio.PlayFrom(SoundEffect.UnitSelect, friendly));
            Assert.DoesNotThrow(() => GameAudio.PlayWeaponFire(friendly));
            Assert.DoesNotThrow(() => GameAudio.PlayFrom(SoundEffect.UnitSelect, null));

            Assert.That(GameAudioManager.Existing, Is.Null,
                "GameAudio must never lazy-create a GameAudioManager");
        }

        #endregion // The facade constructs nothing
    }
}
