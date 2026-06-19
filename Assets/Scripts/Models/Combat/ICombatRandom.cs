using System;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Dice source for the combat engine. The single primitive is <see cref="RollDie"/> (uniform 1..sides);
    /// every band roll, terrain block, and stand-check roll is composed from it. Injected into the engine so
    /// tests can supply a seeded or scripted sequence and assert exact pipeline output (HS_DesignDoc §31.4a.12).
    /// </summary>
    public interface ICombatRandom
    {
        /// <summary>Uniform roll in [1, sides]. sides must be ≥ 1.</summary>
        int RollDie(int sides);
    }

    /// <summary>
    /// Default <see cref="ICombatRandom"/> over <see cref="System.Random"/>. Construct with an explicit seed
    /// for reproducibility (tests) or seedless for play. Not thread-safe — one instance per resolution context.
    /// </summary>
    public sealed class CombatRandom : ICombatRandom
    {
        private const string CLASS_NAME = nameof(CombatRandom);

        private readonly Random _rng;

        public CombatRandom() => _rng = new Random();

        public CombatRandom(int seed) => _rng = new Random(seed);

        public int RollDie(int sides)
        {
            try
            {
                if (sides < 1)
                    throw new ArgumentOutOfRangeException(nameof(sides), sides, "Die must have at least 1 side.");

                return _rng.Next(1, sides + 1);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RollDie), e);
                return 1; // safe minimum
            }
        }
    }
}
