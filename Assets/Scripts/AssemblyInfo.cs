using System.Runtime.CompilerServices;

// Grants the EditorTests assembly access to `internal` members of Main.
//
// ⚠ USED SPARINGLY AND ON PURPOSE. This is not an invitation to test private implementation detail — the
// EditorTest suites drive public APIs everywhere else. The one thing it exists for today is
// SnapshotMapper.RunMigrationLadder: the ladder's guards are UNREACHABLE through the production entry
// point while MINIMUM_SUPPORTED_SAVE_VERSION tracks SAVE_VERSION, so without a seam their first real
// exercise would be the migration they exist to protect. See the comment at that method.
[assembly: InternalsVisibleTo("EditorTests")]
