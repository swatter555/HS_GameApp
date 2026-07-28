using System;
using HammerAndSickle.Persistence;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// The save migration ladder (content pipeline §0.2/§0.5).
    ///
    /// WHY THIS SUITE EXISTS, AND WHY IT INJECTS ITS OWN VERSIONS: with the shipping constants,
    /// MINIMUM_SUPPORTED_SAVE_VERSION == SAVE_VERSION, so every older save is refused by the floor check
    /// and the ladder loop is unreachable. Its guards would therefore first be exercised by the real
    /// migration they exist to protect — the one moment you least want to discover the contract is wrong.
    /// So these drive <see cref="SnapshotMapper.RunMigrationLadder"/> directly with injected versions and
    /// step lookups. Production uses the same implementation with the real constants.
    ///
    /// The contract being pinned: a save below the floor is REFUSED, never guessed at; a version bump
    /// without a migration step THROWS rather than loading mismatched data; and a step must advance
    /// exactly one version, so a step that forgets to stamp its version cannot spin the loop forever.
    /// </summary>
    [TestFixture]
    public class SaveMigrationLadderTests
    {
        #region Helpers

        private static GameStateSnapshot SnapshotAt(int version) =>
            new GameStateSnapshot { SaveVersion = version };

        /// <summary>A well-behaved step: advances the snapshot exactly one version.</summary>
        private static Func<GameStateSnapshot, GameStateSnapshot> GoodStep =>
            snap => { snap.SaveVersion++; return snap; };

        /// <summary>No step is defined for any version.</summary>
        private static Func<GameStateSnapshot, GameStateSnapshot> NoStep(int from) => null;

        #endregion // Helpers

        #region Floor

        [Test]
        public void BelowMinimumSupported_IsRefused()
        {
            var snap = SnapshotAt(2);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                SnapshotMapper.RunMigrationLadder(snap, minimumSupported: 3, currentVersion: 5, stepLookup: _ => GoodStep));

            // The message must name both numbers — "your save is too old" without saying how old is not
            // actionable for a player or for a bug report.
            Assert.That(ex.Message, Does.Contain("2"), "refusal should name the save's version");
            Assert.That(ex.Message, Does.Contain("3"), "refusal should name the minimum supported version");
        }

        [Test]
        public void AtMinimumSupported_IsAccepted()
        {
            var snap = SnapshotAt(3);

            var result = SnapshotMapper.RunMigrationLadder(
                snap, minimumSupported: 3, currentVersion: 3, stepLookup: NoStep);

            // Equal to the floor AND to current: nothing to do, and specifically NOT a refusal.
            Assert.That(result.SaveVersion, Is.EqualTo(3));
        }

        #endregion // Floor

        #region Missing step

        [Test]
        public void MissingMigrationStep_Throws()
        {
            var snap = SnapshotAt(3);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                SnapshotMapper.RunMigrationLadder(snap, minimumSupported: 3, currentVersion: 4, stepLookup: NoStep));

            Assert.That(ex.Message, Does.Contain("No migration step"));
            Assert.That(ex.Message, Does.Contain("3"), "should name the version it could not migrate from");
        }

        [Test]
        public void MissingStepPartWayUpTheLadder_Throws()
        {
            var snap = SnapshotAt(3);

            // 3→4 exists, 4→5 does not. The failure must surface at 4, not be masked by the first
            // step having succeeded.
            var ex = Assert.Throws<InvalidOperationException>(() =>
                SnapshotMapper.RunMigrationLadder(snap, minimumSupported: 3, currentVersion: 5,
                    stepLookup: from => from == 3 ? GoodStep : null));

            Assert.That(ex.Message, Does.Contain("4"), "should name the version whose step is missing");
        }

        #endregion // Missing step

        #region Non-advancing step

        [Test]
        public void StepThatDoesNotAdvanceVersion_Throws()
        {
            var snap = SnapshotAt(3);

            // The classic mistake: a migration transforms the data but forgets to stamp the new version.
            // Without this guard the while-loop would call it forever.
            var ex = Assert.Throws<InvalidOperationException>(() =>
                SnapshotMapper.RunMigrationLadder(snap, minimumSupported: 3, currentVersion: 4,
                    stepLookup: _ => s => s));

            Assert.That(ex.Message, Does.Contain("exactly one version"));
        }

        [Test]
        public void StepThatSkipsAVersion_Throws()
        {
            var snap = SnapshotAt(3);

            // Overshooting is as wrong as not advancing — it silently skips 4's migration.
            var ex = Assert.Throws<InvalidOperationException>(() =>
                SnapshotMapper.RunMigrationLadder(snap, minimumSupported: 3, currentVersion: 5,
                    stepLookup: _ => s => { s.SaveVersion += 2; return s; }));

            Assert.That(ex.Message, Does.Contain("exactly one version"));
        }

        [Test]
        public void StepReturningNull_Throws()
        {
            var snap = SnapshotAt(3);

            Assert.Throws<InvalidOperationException>(() =>
                SnapshotMapper.RunMigrationLadder(snap, minimumSupported: 3, currentVersion: 4,
                    stepLookup: _ => s => null));
        }

        #endregion // Non-advancing step

        #region Happy path

        [Test]
        public void WellFormedLadder_AdvancesOneVersionAtATime()
        {
            var snap = SnapshotAt(3);
            var visited = new System.Collections.Generic.List<int>();

            var result = SnapshotMapper.RunMigrationLadder(snap, minimumSupported: 3, currentVersion: 6,
                stepLookup: from =>
                {
                    visited.Add(from);
                    return GoodStep;
                });

            Assert.That(result.SaveVersion, Is.EqualTo(6));

            // Every intermediate version must be visited — the ladder is a chain, not a jump to current.
            // This is what stops a future v3 save from skipping v4's and v5's transformations.
            Assert.That(visited, Is.EqualTo(new[] { 3, 4, 5 }));
        }

        [Test]
        public void AlreadyCurrent_RunsNoSteps()
        {
            var snap = SnapshotAt(4);
            bool stepRequested = false;

            var result = SnapshotMapper.RunMigrationLadder(snap, minimumSupported: 3, currentVersion: 4,
                stepLookup: _ => { stepRequested = true; return GoodStep; });

            Assert.That(result.SaveVersion, Is.EqualTo(4));
            Assert.That(stepRequested, Is.False, "a current save must not enter the ladder at all");
        }

        #endregion // Happy path
    }
}
