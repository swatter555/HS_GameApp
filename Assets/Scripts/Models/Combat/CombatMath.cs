using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Pure combat math: the Δ→band ladder, band/terrain dice, and band-shift clamp (HS_DesignDoc §7.6 / §7.5.6.2).
    /// No state, no RNG ownership — dice come in via <see cref="ICombatRandom"/> so every roll is reproducible.
    /// </summary>
    public static class CombatMath
    {
        private const string CLASS_NAME = nameof(CombatMath);

        #region Band Ladder

        /// <summary>
        /// Maps Δ (attacker stat − defender stat) to its damage band (HS_DesignDoc §7.6.1–§7.6.11).
        /// </summary>
        public static DamageBand DeltaBand(int delta)
        {
            if (delta <= -13) return DamageBand.Hopeless;       // §7.6.1
            if (delta <= -11) return DamageBand.Forlorn;        // −12..−11
            if (delta <= -8)  return DamageBand.Difficult;      // −10..−8
            if (delta <= -5)  return DamageBand.Grim;           // −7..−5
            if (delta <= -2)  return DamageBand.Disadvantaged;  // −4..−2
            if (delta <= 1)   return DamageBand.Even;           // −1..+1
            if (delta <= 4)   return DamageBand.Favorable;      // +2..+4
            if (delta <= 7)   return DamageBand.Advantaged;     // +5..+7
            if (delta <= 10)  return DamageBand.Strong;         // +8..+10
            if (delta <= 13)  return DamageBand.Commanding;     // +11..+13
            return DamageBand.Crushing;                         // ≥ +14
        }

        /// <summary>
        /// Shifts a band up (+) or down (−) by <paramref name="steps"/> rungs, clamped to the ladder ends.
        /// The only +1 shifts in the model are the embarkment malus (§7.10.1.1) and WW survival (§11.1.2.4).
        /// </summary>
        public static DamageBand ShiftBand(DamageBand band, int steps)
        {
            int idx = (int)band + steps;
            if (idx < (int)DamageBand.Hopeless) idx = (int)DamageBand.Hopeless;
            if (idx > (int)DamageBand.Crushing) idx = (int)DamageBand.Crushing;
            return (DamageBand)idx;
        }

        #endregion // Band Ladder

        #region Dice

        /// <summary>
        /// Rolls a band's dice into straight hit points (HS_DesignDoc §7.6.0 — direct HP against MAX_HP, no
        /// %-conversion). A "natural 0" (Hopeless, or a low roll on a bands with a −1, e.g. Even 1d8−1) returns
        /// 0; the engine reads that as a miss for the connecting-hit floor (§7.5.6.4).
        /// </summary>
        public static int RollBandDamage(DamageBand band, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                return band switch
                {
                    DamageBand.Hopeless      => 0,
                    DamageBand.Forlorn       => rng.RollDie(2) - 1,                 // 1d2−1
                    DamageBand.Difficult     => rng.RollDie(3) - 1,                 // 1d3−1
                    DamageBand.Grim          => rng.RollDie(3),                     // 1d3
                    DamageBand.Disadvantaged => rng.RollDie(4),                     // 1d4
                    DamageBand.Even          => rng.RollDie(8) - 1,                 // 1d8−1
                    DamageBand.Favorable     => rng.RollDie(6) + 2,                 // 1d6+2
                    DamageBand.Advantaged    => rng.RollDie(8) + 3,                 // 1d8+3
                    DamageBand.Strong        => rng.RollDie(10) + 4,                // 1d10+4
                    DamageBand.Commanding    => rng.RollDie(6) + rng.RollDie(6) + 5,// 2d6+5
                    DamageBand.Crushing      => rng.RollDie(8) + rng.RollDie(8) + 6,// 2d8+6
                    _                        => 0,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RollBandDamage), e);
                return 0;
            }
        }

        /// <summary>
        /// Terrain mitigation tier for a hex's terrain (HS_DesignDoc §7.5.6.2). Impassable maps to None — no
        /// ground combat occurs there.
        /// </summary>
        public static TerrainBlockTier BlockTier(TerrainType terrain) => terrain switch
        {
            TerrainType.Forest    => TerrainBlockTier.Light,
            TerrainType.MinorCity => TerrainBlockTier.Light,
            TerrainType.Rough     => TerrainBlockTier.Medium,
            TerrainType.Marsh     => TerrainBlockTier.Medium,
            TerrainType.MajorCity => TerrainBlockTier.Heavy,
            TerrainType.Mountains => TerrainBlockTier.Heavy,
            _                     => TerrainBlockTier.None,   // Clear, Water, Impassable
        };

        /// <summary>
        /// Rolls the flat-HP terrain block for a hex (HS_DesignDoc §7.5.6.2): None 0, Light 1d2, Medium 1d4,
        /// Heavy 1d4+2. Subtracted from incoming damage after the multiplier stack (engine step 5).
        /// </summary>
        public static int RollTerrainBlock(TerrainType terrain, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                return BlockTier(terrain) switch
                {
                    TerrainBlockTier.Light  => rng.RollDie(2),       // 1d2
                    TerrainBlockTier.Medium => rng.RollDie(4),       // 1d4
                    TerrainBlockTier.Heavy  => rng.RollDie(4) + 2,   // 1d4+2
                    _                       => 0,                    // None
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RollTerrainBlock), e);
                return 0;
            }
        }

        #endregion // Dice

        #region Rounding

        /// <summary>Round-half-up to the nearest integer (HS_DesignDoc §7.7.6, round_half_up = away from zero).</summary>
        public static int RoundHalfUp(double value) =>
            (int)Math.Round(value, MidpointRounding.AwayFromZero);

        #endregion // Rounding
    }
}
