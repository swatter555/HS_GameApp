using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Efficiency and supply degradation (HS_DesignDoc §7.15) — the probabilistic system that replaced the old
    /// deterministic Suppression mechanic and per-hex/per-combat supply costs. Movement and combat each roll
    /// 1d100 against an experience-keyed threshold to drop an Efficiency tier or burn a DaysSupply; Upkeep
    /// recovers Efficiency by rest. All pure: dice via <see cref="ICombatRandom"/>, tier math via the helpers
    /// below; the caller owns the unit state and applies the result.
    ///
    /// EfficiencyLevel ordering (best→worst by value): FullOperations 4, CombatOperations 3, NormalOperations 2,
    /// DegradedOperations 1, StaticOperations 0. "Drop a tier" = one step toward Static; "recover" = toward Full.
    /// </summary>
    public static class DegradationCheck
    {
        private const string CLASS_NAME = nameof(DegradationCheck);

        #region Floors & flat chances

        /// <summary>Movement EL loss cannot push below this (§7.15.2.3) — only combat reaches Static.</summary>
        public const EfficiencyLevel MOVE_EFFICIENCY_FLOOR = EfficiencyLevel.DegradedOperations;

        /// <summary>Combat EL loss floor (§7.15.3.3) — combat can grind a unit all the way down.</summary>
        public const EfficiencyLevel COMBAT_EFFICIENCY_FLOOR = EfficiencyLevel.StaticOperations;

        /// <summary>Counter-battery supply loss is a flat 1d100 chance, no experience modifier (§7.15.6).</summary>
        public const int COUNTER_BATTERY_SUPPLY_CHANCE = 50;

        /// <summary>Upkeep Efficiency recovery cap (§7.15.8.4).</summary>
        public const EfficiencyLevel RECOVERY_CAP = EfficiencyLevel.FullOperations;

        #endregion // Floors & flat chances

        #region Threshold tables (§7.15.2 / §7.15.3 / §7.15.4 / §7.15.5)

        /// <summary>Per-hex movement Efficiency-loss threshold (§7.15.2.2): Raw/Green 20, Trained 18, Exp 15, Vet 12, Elite 10.</summary>
        public static int MoveEfficiencyThreshold(ExperienceLevel exp) => exp switch
        {
            ExperienceLevel.Raw         => 20,
            ExperienceLevel.Green       => 20,
            ExperienceLevel.Trained     => 18,
            ExperienceLevel.Experienced => 15,
            ExperienceLevel.Veteran     => 12,
            ExperienceLevel.Elite       => 10,
            _                           => 20,
        };

        /// <summary>Per-combat Efficiency-loss threshold (§7.15.3.2): Raw/Green 50, Trained 48, Exp 45, Vet 40, Elite 35.</summary>
        public static int CombatEfficiencyThreshold(ExperienceLevel exp) => exp switch
        {
            ExperienceLevel.Raw         => 50,
            ExperienceLevel.Green       => 50,
            ExperienceLevel.Trained     => 48,
            ExperienceLevel.Experienced => 45,
            ExperienceLevel.Veteran     => 40,
            ExperienceLevel.Elite       => 35,
            _                           => 50,
        };

        /// <summary>Per-hex movement supply-loss threshold (§7.15.4.2 — matches the move EL table): Raw/Green 20 … Elite 10.</summary>
        public static int MoveSupplyThreshold(ExperienceLevel exp) => MoveEfficiencyThreshold(exp);

        /// <summary>Per-combat supply-loss threshold (§7.15.5.2): Raw/Green 60, Trained 58, Exp 55, Vet 50, Elite 45.</summary>
        public static int CombatSupplyThreshold(ExperienceLevel exp) => exp switch
        {
            ExperienceLevel.Raw         => 60,
            ExperienceLevel.Green       => 60,
            ExperienceLevel.Trained     => 58,
            ExperienceLevel.Experienced => 55,
            ExperienceLevel.Veteran     => 50,
            ExperienceLevel.Elite       => 45,
            _                           => 60,
        };

        #endregion // Threshold tables

        #region Roll resolvers (1d100 ≤ threshold)

        private static bool RollUnderOrEqual(int threshold, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));
                return rng.RollDie(100) <= threshold;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RollUnderOrEqual), e);
                return false; // fail safe — no degradation on error
            }
        }

        /// <summary>True if a hex of movement drops an Efficiency tier (§7.15.2.1).</summary>
        public static bool RollMoveEfficiencyLoss(ExperienceLevel exp, ICombatRandom rng) =>
            RollUnderOrEqual(MoveEfficiencyThreshold(exp), rng);

        /// <summary>True if a combat resolution drops this side's Efficiency tier (§7.15.3.1 — rolled per side).</summary>
        public static bool RollCombatEfficiencyLoss(ExperienceLevel exp, ICombatRandom rng) =>
            RollUnderOrEqual(CombatEfficiencyThreshold(exp), rng);

        /// <summary>True if a hex of movement burns 1 DaysSupply (§7.15.4.1).</summary>
        public static bool RollMoveSupplyLoss(ExperienceLevel exp, ICombatRandom rng) =>
            RollUnderOrEqual(MoveSupplyThreshold(exp), rng);

        /// <summary>True if a combat resolution burns this side's 1 DaysSupply (§7.15.5.1 — rolled per side).</summary>
        public static bool RollCombatSupplyLoss(ExperienceLevel exp, ICombatRandom rng) =>
            RollUnderOrEqual(CombatSupplyThreshold(exp), rng);

        /// <summary>True if a counter-battery shot burns 1 DaysSupply — flat 50%, no experience modifier (§7.15.6).</summary>
        public static bool RollCounterBatterySupplyLoss(ICombatRandom rng) =>
            RollUnderOrEqual(COUNTER_BATTERY_SUPPLY_CHANCE, rng);

        #endregion // Roll resolvers

        #region Tier transitions

        /// <summary>Drops one Efficiency tier toward Static, not below <paramref name="floor"/> (§7.15.2.3 / §7.15.3.3).</summary>
        public static EfficiencyLevel DropOneTier(EfficiencyLevel current, EfficiencyLevel floor)
        {
            int cur = (int)current;
            int flr = (int)floor;
            return cur > flr ? (EfficiencyLevel)(cur - 1) : current;
        }

        /// <summary>Raises Efficiency by <paramref name="tiers"/>, capped at Full (§7.15.8.4).</summary>
        public static EfficiencyLevel Recover(EfficiencyLevel current, int tiers)
        {
            if (tiers <= 0) return current;
            int next = (int)current + tiers;
            int cap = (int)RECOVERY_CAP;
            return (EfficiencyLevel)(next > cap ? cap : next);
        }

        #endregion // Tier transitions

        #region Combined appliers

        /// <summary>Rolls a movement EL check and returns the resulting Efficiency level (floor Degraded, §7.15.2).</summary>
        public static EfficiencyLevel ApplyMoveEfficiencyLoss(EfficiencyLevel current, ExperienceLevel exp, ICombatRandom rng) =>
            RollMoveEfficiencyLoss(exp, rng) ? DropOneTier(current, MOVE_EFFICIENCY_FLOOR) : current;

        /// <summary>Rolls a combat EL check and returns the resulting Efficiency level (floor Static, §7.15.3).</summary>
        public static EfficiencyLevel ApplyCombatEfficiencyLoss(EfficiencyLevel current, ExperienceLevel exp, ICombatRandom rng) =>
            RollCombatEfficiencyLoss(exp, rng) ? DropOneTier(current, COMBAT_EFFICIENCY_FLOOR) : current;

        #endregion // Combined appliers

        #region Upkeep recovery (§7.15.8)

        /// <summary>
        /// Efficiency tiers recovered at the next friendly Upkeep (§7.15.8): +2 if the unit neither moved nor
        /// fought, +1 if it moved but did not fight, 0 if it fought (attacker, defender, ambusher, opportunity
        /// firer, or counter-battery). Supply never recovers from rest (§7.15.8.5).
        /// </summary>
        public static int RecoveryTiers(bool moved, bool fought)
        {
            if (fought) return 0;
            return moved ? 1 : 2;
        }

        /// <summary>Convenience: the Efficiency level after Upkeep recovery, given whether the unit moved / fought.</summary>
        public static EfficiencyLevel ApplyUpkeepRecovery(EfficiencyLevel current, bool moved, bool fought) =>
            Recover(current, RecoveryTiers(moved, fought));

        #endregion // Upkeep recovery
    }
}
