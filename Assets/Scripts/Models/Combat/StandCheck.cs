using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Inputs to the defender-only Stand Value (HS_DesignDoc §7.9.1). All terms are small integers on the
    /// same scale as the 1d10 roll. Leader/command terms come from the attached leaders (0 if none); the
    /// caller supplies them so this stays free of Leader/CombatUnit coupling.
    /// </summary>
    public struct StandValueInput
    {
        /// <summary>Defender's deployment posture → §7.9.2 mod (Embarked −2 … Fortified +3).</summary>
        public DeploymentPosition Deployment;

        /// <summary>Defender's hex terrain → §7.9.3 mod (Clear 0 … Mountains/MajorCity +2).</summary>
        public TerrainType Terrain;

        /// <summary>Defender's experience → §7.9.4 mod (Raw −2 … Elite +3).</summary>
        public ExperienceLevel Experience;

        /// <summary>Defender's skill-tier Leader_mod (§14.13), already counted and capped at +3. 0 if unled.</summary>
        public int LeaderMod;

        /// <summary>Defender's CommandAbility value 0..3 (§7.9.4b), added. 0 if unled.</summary>
        public int DefenderCommand;

        /// <summary>Attacker's CommandAbility value 0..3 (§7.9.4a), subtracted. 0 if attacker unled.</summary>
        public int AttackerCommand;

        /// <summary>True if the resolving attack came through the defender's flank arc (§7.9.4c) — subtracts FLANK_SV_PENALTY.</summary>
        public bool FlankAttack;

        /// <summary>Final post-mitigation HP the defender lost this attack → drives the Shock term (§7.9.1.1).</summary>
        public int HpDealtThisAttack;
    }

    /// <summary>
    /// The defender-only stand check (HS_DesignDoc §7.9): Shock, the per-tier SV mods, Stand Value assembly,
    /// and the 1d10 resolution into hold / retreat / rout / shatter. Pure — dice via <see cref="ICombatRandom"/>,
    /// no map or unit coupling. Displacement (retreat path, posture drop, Automatic Advance) is the map layer's job.
    /// </summary>
    public static class StandCheck
    {
        private const string CLASS_NAME = nameof(StandCheck);

        #region Shock (§7.9.1.1)

        /// <summary>Shock = ceil(HP_dealt / SHOCK_DIVISOR), clamped to [0, SHOCK_MAX]. (§7.9.1.1)</summary>
        public static int Shock(int hpDealt)
        {
            if (hpDealt <= 0) return 0;
            int shock = (hpDealt + GameData.SHOCK_DIVISOR - 1) / GameData.SHOCK_DIVISOR; // ceil
            return Math.Min(shock, GameData.SHOCK_MAX);
        }

        #endregion // Shock

        #region Per-tier SV mods (§7.9.2–§7.9.4)

        /// <summary>Deployment posture SV mod (§7.9.2): Embarked −2, Mobile/Deployed 0, Hasty +1, Entrenched +2, Fortified +3.</summary>
        public static int DeploymentStandMod(DeploymentPosition position) => position switch
        {
            DeploymentPosition.Embarked     => -2,
            DeploymentPosition.Mobile       => 0,
            DeploymentPosition.Deployed     => 0,
            DeploymentPosition.HastyDefense => 1,
            DeploymentPosition.Entrenched   => 2,
            DeploymentPosition.Fortified    => 3,
            _                               => 0,
        };

        /// <summary>Terrain SV mod (§7.9.3): Clear/Water 0; Forest/Rough/Marsh/MinorCity +1; Mountains/MajorCity +2.</summary>
        public static int TerrainStandMod(TerrainType terrain) => terrain switch
        {
            TerrainType.Forest    => 1,
            TerrainType.Rough     => 1,
            TerrainType.Marsh     => 1,
            TerrainType.MinorCity => 1,
            TerrainType.Mountains => 2,
            TerrainType.MajorCity => 2,
            _                     => 0,   // Clear, Water, Impassable
        };

        /// <summary>Experience SV mod (§7.9.4): Raw −2, Green −1, Trained 0, Experienced +1, Veteran +2, Elite +3.</summary>
        public static int ExperienceStandMod(ExperienceLevel exp) => exp switch
        {
            ExperienceLevel.Raw         => -2,
            ExperienceLevel.Green       => -1,
            ExperienceLevel.Trained     => 0,
            ExperienceLevel.Experienced => 1,
            ExperienceLevel.Veteran     => 2,
            ExperienceLevel.Elite       => 3,
            _                           => 0,
        };

        #endregion // Per-tier SV mods

        #region Stand Value + resolution (§7.9.1 / §7.9.5)

        /// <summary>
        /// Assembles the defender's Stand Value (§7.9.1):
        ///   SV = STAND_BASE + Deployment + Terrain + Experience + LeaderMod + DefenderCommand
        ///        − AttackerCommand − (FlankAttack ? FLANK_SV_PENALTY : 0) − Shock(HpDealtThisAttack).
        /// SV is not clamped — a hammered/exposed unit can go below 0 (shatter reachable); a dug-in veteran
        /// can sit high enough that every 1d10 holds.
        /// </summary>
        public static int ComputeStandValue(in StandValueInput input)
        {
            try
            {
                int sv = GameData.STAND_BASE
                       + DeploymentStandMod(input.Deployment)
                       + TerrainStandMod(input.Terrain)
                       + ExperienceStandMod(input.Experience)
                       + input.LeaderMod
                       + input.DefenderCommand
                       - input.AttackerCommand
                       - (input.FlankAttack ? GameData.FLANK_SV_PENALTY : 0)
                       - Shock(input.HpDealtThisAttack);
                return sv;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ComputeStandValue), e);
                return GameData.STAND_BASE;
            }
        }

        /// <summary>
        /// Rolls the stand check (§7.9.5): 1d10 vs Stand Value. roll ≤ SV holds; ≤ SV+RETREAT_GAP retreats;
        /// ≤ SV+ROUT_GAP routs; anything higher shatters.
        /// </summary>
        public static StandOutcome ResolveStand(int standValue, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                int roll = rng.RollDie(10);
                if (roll <= standValue) return StandOutcome.Hold;
                if (roll <= standValue + GameData.STAND_RETREAT_GAP) return StandOutcome.Retreat;
                if (roll <= standValue + GameData.STAND_ROUT_GAP) return StandOutcome.Rout;
                return StandOutcome.Shatter;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveStand), e);
                return StandOutcome.Hold;
            }
        }

        #endregion // Stand Value + resolution
    }
}
