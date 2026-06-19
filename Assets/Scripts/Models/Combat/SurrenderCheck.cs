using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// The "cannot retreat" and Static-collapse resolutions (HS_DesignDoc §7.9.6a / §7.9.7) — both pure
    /// experience rolls, no leader/command terms (§7.9.6a.2.2). Triggered by the map layer once it has
    /// determined a unit must retreat but has no valid candidate hex, or that a Static unit is being forced
    /// to retreat.
    /// </summary>
    public static class SurrenderCheck
    {
        private const string CLASS_NAME = nameof(SurrenderCheck);

        #region Surrender Check (§7.9.6a)

        /// <summary>
        /// The 1d20 check number for a Surrender Check (§7.9.6a.2):
        ///   SURRENDER_CHECK_BASE − SURRENDER_CHECK_EXP_FACTOR × ExperienceStandMod.
        /// Raw 14, Green 12, Trained 10, Experienced 8, Veteran 6, Elite 4 (§7.9.6a.2.1).
        /// </summary>
        public static int SurrenderCheckNumber(ExperienceLevel exp) =>
            GameData.SURRENDER_CHECK_BASE - GameData.SURRENDER_CHECK_EXP_FACTOR * StandCheck.ExperienceStandMod(exp);

        /// <summary>
        /// Resolves a Surrender Check (§7.9.6a.2): roll 1d20. ABOVE the check number → HoldInPlace (forced to
        /// bare Deployed, −SURRENDER_SURVIVAL_LOSS HP); AT OR BELOW → Destroyed (permanent, attacker gains ½ cost).
        /// </summary>
        public static SurrenderOutcome ResolveSurrender(ExperienceLevel exp, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                int roll = rng.RollDie(20);
                return roll > SurrenderCheckNumber(exp)
                    ? SurrenderOutcome.HoldInPlace
                    : SurrenderOutcome.Destroyed;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveSurrender), e);
                return SurrenderOutcome.HoldInPlace; // fail safe — do not auto-destroy on error
            }
        }

        #endregion // Surrender Check

        #region Static Catastrophic Collapse (§7.9.7)

        /// <summary>
        /// The 1d100 destruction threshold for a Static unit forced to retreat (§7.9.7.1):
        ///   STATIC_COLLAPSE_BASE − STATIC_COLLAPSE_PER_EXP × ExperienceStandMod.
        /// Raw 40, Green 35, Trained 30, Experienced 25, Veteran 20, Elite 15.
        /// </summary>
        public static int StaticCollapseThreshold(ExperienceLevel exp) =>
            GameData.STATIC_COLLAPSE_BASE - GameData.STATIC_COLLAPSE_PER_EXP * StandCheck.ExperienceStandMod(exp);

        /// <summary>
        /// Resolves the Static forced-retreat catastrophic-collapse roll (§7.9.7): 1d100 ≤ threshold → the unit
        /// is destroyed in place (returns true); otherwise it survives and the retreat resolves normally.
        /// </summary>
        public static bool ResolveStaticCollapse(ExperienceLevel exp, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                int roll = rng.RollDie(100);
                return roll <= StaticCollapseThreshold(exp);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveStaticCollapse), e);
                return false; // fail safe — survive on error
            }
        }

        #endregion // Static Catastrophic Collapse
    }
}
