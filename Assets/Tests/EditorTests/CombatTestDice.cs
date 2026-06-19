using System.Collections.Generic;
using HammerAndSickle.Models.Combat;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Scripted <see cref="ICombatRandom"/> for combat tests — returns a fixed sequence of die results in call
    /// order, so a test can assert the engine's exact HP output. The engine rolls in a known order: band die(s)
    /// first, then the terrain block die, then the contested-crossing die. Throws if more dice are requested
    /// than were queued (a miscount fails the test loudly rather than silently).
    /// </summary>
    public sealed class QueueRollRandom : ICombatRandom
    {
        private readonly Queue<int> _rolls;

        public QueueRollRandom(params int[] rolls) => _rolls = new Queue<int>(rolls);

        public int RollDie(int sides)
        {
            if (_rolls.Count == 0)
                throw new System.InvalidOperationException(
                    $"QueueRollRandom exhausted: engine requested a d{sides} with no scripted rolls left.");
            return _rolls.Dequeue();
        }
    }

    /// <summary>
    /// <see cref="ICombatRandom"/> that returns the same value for every die — convenient for min/max range
    /// checks of a band or terrain-block expression (feed the die's max face, then its min face).
    /// </summary>
    public sealed class FixedRollRandom : ICombatRandom
    {
        private readonly int _value;

        public FixedRollRandom(int value) => _value = value;

        public int RollDie(int sides) => _value;
    }
}
