using HammerAndSickle.Services;
using System;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// The battle-scoped prestige wallet (§18): one spendable balance plus the two lifetime tallies.
    /// Pure C# so the arithmetic is headless-testable — BattleManager owns the instance and raises
    /// OnPrestigeChanged around it (a raise in here would drag EventManager into EditorTests, the
    /// lazy-create trap). Extracted 2026-08-17 (todo_prestige Stage 1 / V8): before this, AddPrestige
    /// fed PrestigeEarned while purchases were meant to come from CurrentPrestige, and the two never met.
    /// </summary>
    public sealed class PrestigeWallet
    {
        #region Properties

        /// <summary>The spendable balance. Seeded from the manifest's prestigePool, then moved only by Add/TrySpend.</summary>
        public int Current { get; private set; }

        /// <summary>Lifetime prestige credited this battle (statistics; never debited).</summary>
        public int Earned { get; private set; }

        /// <summary>Lifetime prestige spent this battle (statistics; never refunded).</summary>
        public int Spent { get; private set; }

        #endregion // Properties

        #region Public Methods

        /// <summary>
        /// Start-of-battle state: <paramref name="pool"/> in hand, nothing earned or spent. Also the
        /// reset path — ResetBattle seeds 0. Negative pools clamp to 0.
        /// </summary>
        public void Seed(int pool)
        {
            Current = Math.Max(0, pool);
            Earned = 0;
            Spent = 0;
        }

        /// <summary>
        /// Credits BOTH the spendable balance and the earned tally (V8.1 — crediting only the tally is
        /// the defect this class replaces). No-op on non-positive amounts. Returns the amount actually
        /// credited so the caller can raise the change event with a true delta.
        /// </summary>
        public int Add(int amount)
        {
            if (amount <= 0) return 0;
            Current += amount;
            Earned += amount;
            return amount;
        }

        /// <summary>
        /// Save-restore path (Stage 5, SAVE_VERSION 7): reinstates a persisted wallet VERBATIM —
        /// unlike <see cref="Seed"/>, the tallies are history, not a fresh start. Negative components
        /// clamp to 0 so a hand-edited save cannot open a debt.
        /// </summary>
        public void Restore(int current, int earned, int spent)
        {
            Current = Math.Max(0, current);
            Earned = Math.Max(0, earned);
            Spent = Math.Max(0, spent);
        }

        /// <summary>
        /// Atomic check-and-debit (V8.2): false — mutating NOTHING — when the balance cannot cover
        /// <paramref name="amount"/> or the amount is non-positive. Callers must not pre-check-then-spend
        /// in two steps; this is the single gate the P4 purchase flow relies on.
        /// </summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0 || amount > Current) return false;
            Current -= amount;
            Spent += amount;
            return true;
        }

        #endregion // Public Methods
    }
}
