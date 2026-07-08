using System;
using System.Collections.Generic;
using System.Linq;

namespace HammerAndSickle.Models.AI
{
    /// <summary>
    /// Probability mass function over integers — the AI oracle's currency (AI-Design-Supplement Part 5).
    /// Combat dice are small discrete distributions, so every forecast is computed EXACTLY by convolution
    /// and pointwise transform; no Monte Carlo. Immutable: every operation returns a new Pmf. Pure C#,
    /// no UnityEngine dependency (the headless harness runs on this).
    /// </summary>
    public sealed class Pmf
    {
        private readonly Dictionary<int, double> _p;

        private Pmf(Dictionary<int, double> p) => _p = p;

        #region Factories

        /// <summary>All mass on a single value.</summary>
        public static Pmf Constant(int value) =>
            new Pmf(new Dictionary<int, double> { { value, 1.0 } });

        /// <summary>Uniform die: 1..sides, each 1/sides.</summary>
        public static Pmf Die(int sides)
        {
            if (sides < 1) throw new ArgumentOutOfRangeException(nameof(sides), sides, "Die must have at least 1 side.");
            var p = new Dictionary<int, double>(sides);
            double each = 1.0 / sides;
            for (int face = 1; face <= sides; face++) p[face] = each;
            return new Pmf(p);
        }

        /// <summary>NdS+K as a Pmf — e.g. DicePlus(2, 6, 5) = 2d6+5, DicePlus(1, 8, -1) = 1d8−1.</summary>
        public static Pmf DicePlus(int count, int sides, int plus)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), count, "Need at least 1 die.");
            Pmf result = Constant(plus);
            for (int i = 0; i < count; i++) result = result.Add(Die(sides));
            return result;
        }

        /// <summary>Builds a Pmf from explicit value→probability weights (must be non-negative).</summary>
        public static Pmf FromWeights(IReadOnlyDictionary<int, double> weights)
        {
            var p = new Dictionary<int, double>(weights.Count);
            foreach (KeyValuePair<int, double> kv in weights)
            {
                if (kv.Value < 0.0) throw new ArgumentException($"Negative probability for value {kv.Key}.", nameof(weights));
                if (kv.Value > 0.0) p[kv.Key] = kv.Value;
            }
            if (p.Count == 0) throw new ArgumentException("Pmf must have at least one outcome with positive mass.", nameof(weights));
            return new Pmf(p);
        }

        #endregion // Factories

        #region Operations

        /// <summary>Distribution of the SUM of two independent draws (convolution).</summary>
        public Pmf Add(Pmf other) => Combine(other, (a, b) => a + b);

        /// <summary>Pointwise transform; colliding outputs merge their mass.</summary>
        public Pmf Map(Func<int, int> f)
        {
            var p = new Dictionary<int, double>(_p.Count);
            foreach (KeyValuePair<int, double> kv in _p)
                Accumulate(p, f(kv.Key), kv.Value);
            return new Pmf(p);
        }

        /// <summary>Joint transform of two independent Pmfs through <paramref name="f"/>.</summary>
        public Pmf Combine(Pmf other, Func<int, int, int> f)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            var p = new Dictionary<int, double>(_p.Count * other._p.Count);
            foreach (KeyValuePair<int, double> a in _p)
                foreach (KeyValuePair<int, double> b in other._p)
                    Accumulate(p, f(a.Key, b.Key), a.Value * b.Value);
            return new Pmf(p);
        }

        internal static void Accumulate(Dictionary<int, double> target, int value, double mass)
        {
            target.TryGetValue(value, out double existing);
            target[value] = existing + mass;
        }

        #endregion // Operations

        #region Queries

        /// <summary>P(X == value).</summary>
        public double Prob(int value) => _p.TryGetValue(value, out double p) ? p : 0.0;

        /// <summary>P(X ≥ value).</summary>
        public double ProbAtLeast(int value) => _p.Where(kv => kv.Key >= value).Sum(kv => kv.Value);

        /// <summary>P(X ≤ value).</summary>
        public double ProbAtMost(int value) => _p.Where(kv => kv.Key <= value).Sum(kv => kv.Value);

        /// <summary>E[X].</summary>
        public double ExpectedValue => _p.Sum(kv => kv.Key * kv.Value);

        /// <summary>Total mass — 1.0 within float noise for any properly constructed Pmf.</summary>
        public double TotalMass => _p.Values.Sum();

        public int MinValue => _p.Keys.Min();

        public int MaxValue => _p.Keys.Max();

        /// <summary>Outcomes in ascending value order (deterministic iteration for tests/serialization).</summary>
        public IEnumerable<KeyValuePair<int, double>> Outcomes => _p.OrderBy(kv => kv.Key);

        #endregion // Queries
    }
}
