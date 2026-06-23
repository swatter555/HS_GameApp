using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Inputs to the air-unit stand check (HS_DesignDoc §7.9.8 / §11.4.8.2a). Parallels the ground
    /// <see cref="StandCheck"/> but uses a different, BINARY formula — no terrain / posture / leader / flank
    /// terms, no rout or shatter. The caller supplies the aircraft's air stats and the HP it lost this pass.
    /// </summary>
    public struct AirStandInput
    {
        /// <summary>Aircraft top speed (TS) — the speed-survivability term.</summary>
        public int TopSpeed;

        /// <summary>Aircraft maneuverability (MAN).</summary>
        public int Maneuverability;

        /// <summary>Aircraft experience → §7.9.4 mod (Raw −2 … Elite +3).</summary>
        public ExperienceLevel Experience;

        /// <summary>HP the aircraft LOST this pass → drives the Shock term (§7.9.1.1).</summary>
        public int HpLostThisPass;
    }

    /// <summary>
    /// The binary air-unit stand check (HS_DesignDoc §7.9.8): SV_air = 6 + Experience_mod + floor((TS + MAN) / 8)
    /// − Shock, resolved on a 1d10 into hold-or-disengage. Pure — dice via <see cref="ICombatRandom"/>, no map or
    /// unit coupling. There is NO rout/shatter tier (§7.9.8.4); a disengaging aircraft leaves the AOB and flies
    /// home — the caller applies the fixed-wing auto-return (§5.13.5). Shares STAND_BASE and the Shock formula
    /// with the ground check (§7.9.1.1) by design.
    /// </summary>
    public static class AirStandCheck
    {
        private const string CLASS_NAME = nameof(AirStandCheck);

        /// <summary>
        /// Air Stand Value (§7.9.8.2): STAND_BASE(6) + Experience_mod + floor((TS + MAN) / 8) − Shock(HpLost).
        /// Not clamped — a fresh elite interceptor sits high, a mauled rookie can drop below the 1d10 floor.
        /// </summary>
        public static int ComputeStandValue(in AirStandInput input)
        {
            try
            {
                return GameData.STAND_BASE
                     + StandCheck.ExperienceStandMod(input.Experience)
                     + (input.TopSpeed + input.Maneuverability) / 8     // floor — ints
                     - StandCheck.Shock(input.HpLostThisPass);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ComputeStandValue), e);
                return GameData.STAND_BASE;
            }
        }

        /// <summary>
        /// Rolls the binary air stand (§7.9.8.3): 1d10. roll ≤ SV_air → Hold; roll &gt; SV_air → Disengage.
        /// No rout/shatter — air is hold-or-leave (§7.9.8.4).
        /// </summary>
        public static AirStandOutcome ResolveStand(int standValue, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                int roll = rng.RollDie(10);
                return roll <= standValue ? AirStandOutcome.Hold : AirStandOutcome.Disengage;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveStand), e);
                return AirStandOutcome.Hold;
            }
        }
    }
}
