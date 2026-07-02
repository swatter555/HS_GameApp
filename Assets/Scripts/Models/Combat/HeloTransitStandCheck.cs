using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Inputs to the helicopter transit stand check (HS_DesignDoc §11.8.9). A helicopter MOVING on the map
    /// (an attack helo HELO, or an AM/MAM unit in EmbarkedHelo state) that takes ≥1 HP from ground air-defense
    /// opportunity fire (§5.13.2.4) rolls this check. The caller accumulates the HP lost across all damaging
    /// events this move and supplies the running total — Shock grows as the transit takes more fire (§11.8.9).
    /// </summary>
    public struct HeloTransitStandInput
    {
        /// <summary>Helicopter experience → §7.9.4 mod (Raw −2 … Elite +3).</summary>
        public ExperienceLevel Experience;

        /// <summary>Cumulative HP the helo has LOST this move → drives the Shock term (§7.9.1.1).</summary>
        public int HpLostThisMove;
    }

    /// <summary>
    /// The binary helicopter transit stand check (HS_DesignDoc §11.8.9, NARROWED 2026-06-23 to the ground-fire
    /// path — air-to-air interception now resolves in the Aviation Intercept Box, §11.8.10). A pure sibling of
    /// <see cref="AirStandCheck"/>: SV_helo = STAND_BASE(6) + Experience_mod − Shock — NO terrain/posture/leader
    /// terms (none aloft) and NO (TS+MAN)/8 speed term (a helo's air stats are empty, §7A.14). Resolved on a 1d10
    /// into Hold-or-Abort; there is no rout/shatter tier. Pure — dice via <see cref="ICombatRandom"/>, no map or
    /// unit coupling. The caller applies the consequences of an Abort (§11.8.9): free return to the origin hex,
    /// MP/actions → 0, and for an embarked transport the forced disembark to Deployed at the origin. The embarkment
    /// damage malus on the AD fire itself (§7.10.1) is the air-defense lane's concern, not this check.
    /// </summary>
    public static class HeloTransitStandCheck
    {
        private const string CLASS_NAME = nameof(HeloTransitStandCheck);

        /// <summary>
        /// Helo transit Stand Value (§11.8.9): STAND_BASE(6) + Experience_mod − Shock(HpLostThisMove). Not clamped
        /// — a fresh elite helo sits high, a mauled rookie can drop below the 1d10 floor (auto-abort).
        /// </summary>
        public static int ComputeStandValue(in HeloTransitStandInput input)
        {
            try
            {
                return GameData.STAND_BASE
                     + StandCheck.ExperienceStandMod(input.Experience)
                     - StandCheck.Shock(input.HpLostThisMove);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ComputeStandValue), e);
                return GameData.STAND_BASE;
            }
        }

        /// <summary>
        /// Rolls the binary helo transit stand (§11.8.9): 1d10. roll ≤ SV_helo → Hold (continue the move);
        /// roll &gt; SV_helo → Abort (return to origin). No rout/shatter aloft.
        /// </summary>
        public static HeloTransitOutcome ResolveStand(int standValue, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                int roll = rng.RollDie(10);
                return roll <= standValue ? HeloTransitOutcome.Hold : HeloTransitOutcome.Abort;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveStand), e);
                return HeloTransitOutcome.Hold;
            }
        }
    }
}
