using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// The fixed-wing air-ambush DETECTION roll (HS_DesignDoc §6.10.3/.4). When a fixed-wing aircraft transits
    /// within engagement range of an UNSPOTTED SAM/SPSAM/AAA/SPAAA, it gets one 1d6-vs-experience check to spot
    /// the threat first; on success the ambush attack is averted (§6.10.5). Pure and seedable — dice via
    /// <see cref="ICombatRandom"/>, no map or unit coupling — so the per-tier table can be asserted exactly.
    /// The caller (SpottingService) owns finding the unspotted ambusher, the range gate, and the reveal/attack
    /// consequences; this type owns only the roll. Helicopters do NOT use this — they take ground ambush (§6.9).
    /// </summary>
    public static class AirAmbushCheck
    {
        private const string CLASS_NAME = nameof(AirAmbushCheck);

        /// <summary>
        /// Minimum 1d6 face needed to DETECT the ambush, keyed by the transiting aircraft's experience (§6.10.4:
        /// Raw 16.7% [6] → Elite 100% [1–6]). A lower threshold = a wider success window.
        /// </summary>
        public static int DetectionThreshold(ExperienceLevel experience) => experience switch
        {
            ExperienceLevel.Raw => 6,
            ExperienceLevel.Green => 5,
            ExperienceLevel.Trained => 4,
            ExperienceLevel.Experienced => 3,
            ExperienceLevel.Veteran => 2,
            ExperienceLevel.Elite => 1,
            _ => 6
        };

        /// <summary>
        /// Rolls the detection check (§6.10.3): 1d6 ≥ <see cref="DetectionThreshold"/> → detected (ambush averted,
        /// the aircraft proceeds); below → not detected (the ambusher resolves its attack). Pure boolean — the
        /// caller applies the §6.10 reveal and continuation rules.
        /// </summary>
        public static bool RollDetection(ExperienceLevel experience, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                return rng.RollDie(6) >= DetectionThreshold(experience);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RollDetection), e);
                return false; // safe default: treat as undetected (the caller resolves the ambush)
            }
        }
    }
}
