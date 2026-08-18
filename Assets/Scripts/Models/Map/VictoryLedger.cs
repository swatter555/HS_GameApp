using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;
using System;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Derived snapshot of the victory-value distribution across the map (§17.2/§17.3): what each side
    /// currently holds, summed over EVERY hex — value is an economic weight on the hex, not gated by any
    /// flag (Bob's ruling, 2026-08-17 V-pass). RECOMPUTED, never accumulated: an incremental counter
    /// desyncs the moment a control change takes a path that forgot to update it, which is exactly how
    /// BattleManager.TotalObjectiveHexes failed. Nothing here is serialized — save/load recomputes from
    /// the restored hex map.
    /// </summary>
    public readonly struct VictoryLedger
    {
        #region Properties

        /// <summary>Σ VictoryValue over hexes under TileControl.Red (the player).</summary>
        public float PlayerValue { get; }

        /// <summary>Σ VictoryValue over hexes under TileControl.Blue.</summary>
        public float EnemyValue { get; }

        /// <summary>Σ VictoryValue over Grey/None hexes — in the denominator, credited to nobody (V5.2).</summary>
        public float NeutralValue { get; }

        /// <summary>Total victory value on the map. Zero is LEGITIMATE (V5.4) — an unscored map, not an error.</summary>
        public float TotalValue => PlayerValue + EnemyValue + NeutralValue;

        /// <summary>
        /// The player's share of <see cref="TotalValue"/>, 0 when the map carries no value. Because neutral
        /// value stays in the denominator, the two sides' shares need not sum to 1.
        /// </summary>
        public float PlayerShare
        {
            get
            {
                float total = TotalValue;
                return total > 0f ? PlayerValue / total : 0f;
            }
        }

        #endregion // Properties

        #region Construction

        public VictoryLedger(float playerValue, float enemyValue, float neutralValue)
        {
            PlayerValue = playerValue;
            EnemyValue = enemyValue;
            NeutralValue = neutralValue;
        }

        /// <summary>
        /// One pass over the live map. Cheap enough to run once per turn and once at battle start — the
        /// ONLY two sanctioned call sites (V5.3). ⚠ Do not cache behind a dirty flag; caching is how the
        /// accumulator class of bug returns.
        /// </summary>
        /// <remarks>
        /// ⚠ Accumulates in DOUBLE (V5.1): HexMap enumerates in dictionary insertion order, which is not
        /// stable across a save/load round-trip, and float addition is not associative — a float
        /// accumulator could grade a threshold boundary differently on reload. Doubles make the sum exact
        /// for any realistic hex count.
        /// ⚠ The <c>v &lt;= 0</c> skip drops the odd-row Impassable filler (victoryValue 0) AND any
        /// negative authored value — MapLoader warns on negatives at load; scoring treats them as 0
        /// (ruled 2026-08-17). A null/disposed map yields the zero ledger, which every consumer already
        /// handles via the TotalValue guard (V5.4/V5.5).
        /// </remarks>
        public static VictoryLedger Compute(HexMap map)
        {
            double player = 0, enemy = 0, neutral = 0;

            try
            {
                if (map != null)
                {
                    foreach (HexTile t in map)
                    {
                        if (t == null) continue;
                        float v = t.VictoryValue;
                        if (v <= 0f) continue;                   // most hexes; also drops filler + negatives

                        switch (t.TileControl)
                        {
                            case TileControl.Red: player += v; break;
                            case TileControl.Blue: enemy += v; break;
                            default: neutral += v; break;        // Grey, None
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(VictoryLedger), nameof(Compute), e);
            }

            return new VictoryLedger((float)player, (float)enemy, (float)neutral);
        }

        #endregion // Construction
    }
}
