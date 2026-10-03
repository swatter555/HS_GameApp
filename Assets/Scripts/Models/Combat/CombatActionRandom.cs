using System;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Remembers failed dice across legacy math helpers that log and return defaults. A committed action must
    /// stop on such a failure, not mistake the default for a missed shot or a successful stand check.
    /// </summary>
    internal sealed class CombatActionRandom : ICombatRandom
    {
        private readonly ICombatRandom _source;
        private Exception _failure;
        public CombatActionRandom(ICombatRandom source) => _source = source ?? throw new ArgumentNullException(nameof(source));
        public int RollDie(int sides)
        {
            ThrowIfFailed(this);
            try { return _source.RollDie(sides); }
            catch (Exception e) { _failure = e; throw; }
        }
        internal static void ThrowIfFailed(ICombatRandom rng)
        {
            if (rng is CombatActionRandom guarded && guarded._failure != null)
                throw new InvalidOperationException("Combat dice failed; the committed attack has stopped.", guarded._failure);
        }
    }
}
