using System;
using System.Reflection;
using HammerAndSickle.Audio;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
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

        #region Movement sound follows POSTURE, not classification

        /* ⚠ THESE PIN A DEFECT FOUND BY EAR, NOT BY A FAILURE. GetMovementSFX originally mapped
         * classification alone, so a dismounted Motor Rifle regiment walked to the sound of its parked
         * BTRs and a towed artillery regiment to the sound of the trucks it had just unhitched from.
         * Nothing throws, nothing logs — it just sounds wrong, which is why it survived to a play test.
         * Same species as the weapon-family rule (§9.10.4): posture decides, never the class label. */

        /// <summary>How the unit is travelling right now — the ACTIVE profile's medium.</summary>
        private static MovementMedium Medium(CombatUnit unit) => MovementModeService.CurrentMedium(unit);

        /// <summary>
        /// The movement sound for a unit's current posture, standard cut.
        /// ⚠ Whether the LONG cut plays is decided at play time by measuring the real clip
        /// (GameAudio.PlayMovement), so it is deliberately not part of this helper.
        /// </summary>
        private static SoundEffect Sound(CombatUnit unit) =>
            GameAudioManager.GetMovementSFX(Medium(unit));

        private static CombatUnit MakeMountable(string name, UnitClassification classification,
            WeaponType deployed, WeaponType mobile)
        {
            var unit = new CombatUnit(name, classification, UnitRole.GroundCombat, Side.Player,
                Nationality.USSR, deployedProfile: deployed,
                mobileProfile: mobile, embarkedProfile: WeaponType.NONE);
            return unit;
        }

        private static CombatUnit MakeSelfPropelled(string name, UnitClassification classification,
            WeaponType deployed)
        {
            // Mobile bay empty AND closed by derivation — the unit IS its vehicle (deployed medium is
            // Tracked/Wheeled), exactly as CombatUnitDB authors TANK/SPA/SPAAA.
            return new CombatUnit(name, classification, UnitRole.GroundCombat, Side.Player,
                Nationality.USSR, deployedProfile: deployed,
                mobileProfile: WeaponType.NONE, embarkedProfile: WeaponType.NONE);
        }

        [Test]
        public void DismountedMotorRifle_MovesOnFoot_MountedMovesWheeled()
        {
            var mot = MakeMountable("Motor Rifle", UnitClassification.MOT,
                WeaponType.INF_REG_SV, WeaponType.APC_BTR80_SV);

            mot.SetDeploymentPosition(DeploymentPosition.Mobile);
            Assert.That(Sound(mot), Is.EqualTo(SoundEffect.UnitMoveWheeled),
                "mounted in its BTRs");

            mot.SetDeploymentPosition(DeploymentPosition.Deployed);
            Assert.That(Sound(mot), Is.EqualTo(SoundEffect.UnitMoveFoot),
                "dismounted — the carriers are parked");
        }

        [Test]
        public void DismountedMechanized_MovesOnFoot_MountedMovesTracked()
        {
            var mech = MakeMountable("Mech Rifle", UnitClassification.MECH,
                WeaponType.INF_REG_SV, WeaponType.IFV_BMP1_SV);

            mech.SetDeploymentPosition(DeploymentPosition.Mobile);
            Assert.That(Sound(mech), Is.EqualTo(SoundEffect.UnitMoveTracked));

            mech.SetDeploymentPosition(DeploymentPosition.Deployed);
            Assert.That(Sound(mech), Is.EqualTo(SoundEffect.UnitMoveFoot));
        }

        [Test]
        public void DismountedTowedArtillery_MovesOnFoot()
        {
            // The clearest case in the data: a towed regiment's MOBILE profile is literally TRK_GEN_SV,
            // a truck. Unhitch and there is nothing left to make a vehicle noise.
            var art = MakeMountable("Artillery", UnitClassification.ART,
                WeaponType.ART_LIGHT_SV, WeaponType.TRK_GEN_SV);

            art.SetDeploymentPosition(DeploymentPosition.Mobile);
            Assert.That(Sound(art), Is.Not.EqualTo(SoundEffect.UnitMoveFoot),
                "limbered and under tow");

            art.SetDeploymentPosition(DeploymentPosition.Deployed);
            Assert.That(Sound(art), Is.EqualTo(SoundEffect.UnitMoveFoot),
                "emplaced — the guns are manhandled, not driven");
        }

        [Test]
        public void EveryDugInPosture_CountsAsDismounted_NotJustDeployed()
        {
            // Fortified/Entrenched/HastyDefense all sit BELOW Mobile. A rule written as
            // "== Deployed" would pass the obvious test and leave three postures wrong.
            var mot = MakeMountable("Motor Rifle", UnitClassification.MOT,
                WeaponType.INF_REG_SV, WeaponType.APC_BTR80_SV);

            foreach (var posture in new[] { DeploymentPosition.Fortified, DeploymentPosition.Entrenched,
                                            DeploymentPosition.HastyDefense, DeploymentPosition.Deployed })
            {
                mot.SetDeploymentPosition(posture);
                Assert.That(Sound(mot), Is.EqualTo(SoundEffect.UnitMoveFoot),
                    $"{posture} is below Mobile, so the unit is off its carriers");
            }
        }

        [Test]
        public void SelfPropelledUnits_AreNeverOnFoot_InAnyPosture()
        {
            // The other half of the rule, and the reason it keys on the MODEL rather than a class list:
            // tanks and SP guns do not dismount, so a dug-in tank must still sound tracked.
            var tank = MakeSelfPropelled("Tank", UnitClassification.TANK, WeaponType.TANK_T55A_SV);
            var spa = MakeSelfPropelled("SP Artillery", UnitClassification.SPA, WeaponType.SPA_2S1_SV);

            foreach (DeploymentPosition posture in Enum.GetValues(typeof(DeploymentPosition)))
            {
                tank.SetDeploymentPosition(posture);
                spa.SetDeploymentPosition(posture);

                Assert.That(Sound(tank), Is.EqualTo(SoundEffect.UnitMoveTracked),
                    $"a tank at {posture} is still a tank");
                Assert.That(Sound(spa), Is.EqualTo(SoundEffect.UnitMoveTracked),
                    $"an SP gun at {posture} carries its own gun");
            }
        }

        [Test]
        public void AirAssaultRegiment_SoundsDifferentInAllThreePostures()
        {
            /* ⚠ THE BUG BOB HEARD, PINNED. An air-assault regiment is `MAM` whether it is walking, riding
             * its MT-LBs or flying — so the old classification switch sounded it as infantry in all three
             * postures. Reading the ACTIVE PROFILE gives three different answers with no special case. */
            var mam = new CombatUnit("Air Assault", UnitClassification.MAM, UnitRole.GroundCombat,
                Side.Player, Nationality.USSR, deployedProfile: WeaponType.INF_AM_SV,
                mobileProfile: WeaponType.APC_MTLB_SV, embarkedProfile: WeaponType.HEL_MI8T_SV);

            mam.SetDeploymentPosition(DeploymentPosition.Deployed);
            Assert.That(Medium(mam), Is.EqualTo(MovementMedium.Foot), "dismounted infantry");

            mam.SetDeploymentPosition(DeploymentPosition.Mobile);
            Assert.That(Medium(mam), Is.EqualTo(MovementMedium.Tracked), "riding MT-LBs — TRACKED, not wheeled");

            mam.SetDeploymentPosition(DeploymentPosition.Embarked);
            Assert.That(Medium(mam), Is.EqualTo(MovementMedium.Helo), "flying");
        }

        [Test]
        public void EmbarkedInfantry_IsAirborneNow_EvenThoughItIsNotAnAirUnit()
        {
            /* ⚠ THE GAMEPLAY HALF, AND THE MORE SERIOUS ONE. `IsAirUnit`/`IsHelicopter` are classification
             * tests, so a regiment flying on Mi-8s reports FALSE and pays ground terrain costs, is halted
             * by zones of control it is flying over, and is checked for ground ambush. M4 routes those
             * decisions through IsAirborneNow; this pins the answer the moment the medium exists. */
            var mam = new CombatUnit("Air Assault", UnitClassification.MAM, UnitRole.GroundCombat,
                Side.Player, Nationality.USSR, deployedProfile: WeaponType.INF_AM_SV,
                mobileProfile: WeaponType.APC_MTLB_SV, embarkedProfile: WeaponType.HEL_MI8T_SV);

            mam.SetDeploymentPosition(DeploymentPosition.Embarked);

            Assert.That(MovementModeService.IsAirborneNow(mam), Is.True, "it is on helicopters");
            Assert.That(mam.IsFixedWing || mam.IsHelicopter, Is.False,
                "and classification still says otherwise — which is exactly why the two must not be conflated");

            mam.SetDeploymentPosition(DeploymentPosition.Mobile);
            Assert.That(MovementModeService.IsAirborneNow(mam), Is.False, "back on the ground in its carriers");
        }

        [Test]
        public void LongCut_IsSelectedByMedium_NotByClassification()
        {
            // The clip CHOICE is now a pure medium→sound mapping; WHETHER to escalate is decided at play
            // time by measuring the real clip (GameAudio.PlayMovement), not by a constant in here.
            Assert.That(GameAudioManager.GetMovementSFX(MovementMedium.Wheeled),
                Is.EqualTo(SoundEffect.UnitMoveWheeled));
            Assert.That(GameAudioManager.GetMovementSFX(MovementMedium.Wheeled, longCut: true),
                Is.EqualTo(SoundEffect.UnitMoveWheeledLong));
            Assert.That(GameAudioManager.GetMovementSFX(MovementMedium.Helo, longCut: true),
                Is.EqualTo(SoundEffect.UnitMoveHeloLong));

            // Foot has no long cut — its longest possible move fits inside any recorded clip.
            Assert.That(GameAudioManager.GetMovementSFX(MovementMedium.Foot, longCut: true),
                Is.EqualTo(SoundEffect.UnitMoveFoot));

            // A base does not move; naval movement (§5.4.2) is unbuilt. Silent, not mis-sounded.
            Assert.That(GameAudioManager.GetMovementSFX(MovementMedium.Static), Is.EqualTo(SoundEffect.None));
            Assert.That(GameAudioManager.GetMovementSFX(MovementMedium.None), Is.EqualTo(SoundEffect.None));
        }

        [Test]
        public void NullUnit_IsSilent_RatherThanThrowing()
        {
            Assert.That(MovementModeService.CurrentMedium(null), Is.EqualTo(MovementMedium.None));
            Assert.That(MovementModeService.IsAirborneNow(null), Is.False);
            Assert.That(GameAudioManager.GetMovementSFX(MovementMedium.None), Is.EqualTo(SoundEffect.None));
        }

        #endregion // Movement sound follows POSTURE, not classification

        #region The facade constructs nothing

        [Test]
        public void GameAudio_NeverLazyCreatesAManager()
        {
            /* The claim that makes audio safe to call from model code and from this very suite: GameAudio
             * reaches the manager through `Existing` (a plain static field) and never `Instance` (whose
             * getter BUILDS a GameObject). If someone "simplifies" that back to Instance, every headless
             * test that makes a noise starts spawning an audio manager — the exact trap EventManager and
             * GameDataManager still carry.
             *
             * ⚠ THE INSTANCE FIELD IS FORCED TO NULL FOR THE DURATION, and that is the only way to test
             * this at all. "Constructs nothing" is observable ONLY when nothing exists — with a manager
             * already present, every call legitimately finds it and the property is untestable. An
             * earlier version simply asserted that none existed, which is not a fact about GameAudio: an
             * EDIT-MODE test runs against whatever scene is open, and this project's scenes carry a
             * GameAudioManager on their Controllers object, so the test failed on Bob's machine while the
             * code was entirely correct. Restored in a finally, so the editor's real manager survives. */
            FieldInfo instanceField = typeof(GameAudioManager)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(instanceField, Is.Not.Null,
                "GameAudioManager._instance was renamed — this test can no longer isolate the singleton");

            object saved = instanceField.GetValue(null);
            try
            {
                instanceField.SetValue(null, null);

                var friendly = new CombatUnit("AudioTestUnit", UnitClassification.INF, UnitRole.GroundCombat,
                    Side.Player, Nationality.USSR);

                // A friendly source passes the fog gate, so these reach the manager lookup rather than
                // stopping at CanHear — which is the path that would construct something.
                Assert.DoesNotThrow(() => GameAudio.Play(SoundEffect.ButtonClick));
                Assert.DoesNotThrow(() => GameAudio.PlayFrom(SoundEffect.UnitSelect, friendly));
                Assert.DoesNotThrow(() => GameAudio.PlayWeaponFire(friendly));
                Assert.DoesNotThrow(() => GameAudio.PlayImpact(friendly));
                Assert.DoesNotThrow(() => GameAudio.PlayFrom(SoundEffect.UnitSelect, null));

                Assert.That(instanceField.GetValue(null), Is.Null,
                    "GameAudio built a GameAudioManager — it must reach the manager through Existing, " +
                    "never Instance, whose getter constructs one");
            }
            finally
            {
                // If the assert above FIRED, something was constructed and would otherwise be left behind
                // as a DontDestroyOnLoad object — which every later test in the run would then see,
                // turning one readable failure into a cascade.
                var created = instanceField.GetValue(null) as GameAudioManager;
                if (created != null && !ReferenceEquals(created, saved))
                    UnityEngine.Object.DestroyImmediate(created.gameObject);

                instanceField.SetValue(null, saved);
            }
        }

        #endregion // The facade constructs nothing
    }
}
