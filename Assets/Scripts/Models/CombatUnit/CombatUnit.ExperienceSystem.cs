using System;
using UnityEngine;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
/*────────────────────────────────────────────────────────────────────────────
 CombatUnit.ExperienceSystem — unit progression and combat effectiveness scaling
 ────────────────────────────────────────────────────────────────────────────────

Summary
═══════
Partial class extension managing unit experience progression from Raw recruits to Elite
veterans through combat actions and battlefield achievements. Units accumulate experience
points that advance through six experience levels, each providing multiplicative combat
effectiveness bonuses ranging from -20% (Raw) to +30% (Elite). The system maintains
tight synchronization between experience points and derived levels while providing
progress tracking and advancement notifications.

Experience progression follows traditional wargaming advancement: Raw (0) → Green (50) →
Trained (120) → Experienced (220) → Veteran (330) → Elite (400). Units gain experience
through combat, movement under threat, and scenario objectives, with advancement providing
tangible tactical benefits through GetExperienceMultiplier() integration with combat
calculations.

Public Properties
═════════════════
public int ExperiencePoints { get; internal set; }
public ExperienceLevel ExperienceLevel { get; internal set; }
public EfficiencyLevel EfficiencyLevel { get; internal set; }

Constructors
════════════
private void InitializeExperienceSystem()

Public Methods
══════════════
public bool AddExperience(int points) - Adds experience points with validation, level progression, and advancement notifications
public int SetExperience(int points) - Direct experience assignment with automatic level synchronization and clamping
public int GetPointsToNextLevel() - Calculates experience points required for next advancement level
public float GetExperienceProgress() - Returns advancement progress as percentage (0.0-1.0) for UI progress bars

Private Methods
═══════════════
private ExperienceLevel CalculateExperienceLevel(int totalPoints) - Converts experience points to appropriate experience level enum
private int GetMinPointsForLevel(ExperienceLevel level) - Returns minimum experience points required for specified level
private ExperienceLevel GetNextLevel(ExperienceLevel currentLevel) - Determines next advancement level in progression sequence
protected virtual void OnExperienceLevelChanged(ExperienceLevel previousLevel, ExperienceLevel newLevel) - Level advancement notification with UI messaging
private float GetExperienceMultiplier() - Combat effectiveness multiplier based on current experience level for integration with combat calculations

Important Design Notes
══════════════════════
• **Point-Level Synchronization**: SetExperience() and AddExperience() maintain strict consistency
  between ExperiencePoints and ExperienceLevel by recalculating level from the same clamped value.

• **Progressive Combat Bonuses**: Experience multipliers range from 0.8× (Raw) to 1.3× (Elite),
  providing meaningful tactical progression that scales with GetFinalCombatRatingModifier() calculations.

• **Clamped Advancement**: Experience points are constrained to 0-400 range with automatic capping
  at Elite level to prevent overflow while maintaining progression incentives.

• **Gain Limiting**: AddExperience() enforces MAX_EXP_GAIN_PER_ACTION limit to prevent exploitation
  while allowing natural battlefield advancement through repeated engagements.

• **UI Integration**: OnExperienceLevelChanged() automatically generates advancement messages through
  AppService.CaptureUiMessage() for player feedback on unit development.

• **Progress Tracking**: GetExperienceProgress() provides normalized advancement percentage for
  UI progress bars and advancement indicators, returning 1.0 for Elite units.

• **Efficiency Independence**: Experience system operates independently from EfficiencyLevel
  (operational readiness) to allow veteran units with temporary efficiency penalties.

• **Advancement Thresholds**: Experience level boundaries match ExperiencePointLevels enum values
  with carefully tuned progression curve balancing advancement rate with tactical impact.

────────────────────────────────────────────────────────────────────────────── */
    public partial class CombatUnit
    {
        #region Properties

        public int ExperiencePoints { get; internal set; }
        public ExperienceLevel ExperienceLevel { get; internal set; }
        public EfficiencyLevel EfficiencyLevel { get; internal set; }

        #endregion // Properties


        #region Initialization

        /// <summary>
        /// Initializes the experience system for the combat unit.
        /// </summary>
        private void InitializeExperienceSystem()
        {
            ExperiencePoints = 0;
            ExperienceLevel = ExperienceLevel.Raw;
            EfficiencyLevel = EfficiencyLevel.FullyOperational;
        }

        #endregion


        #region Experience System

        /// <summary>
        /// Adds experience points to the unit and returns true if successful.
        /// </summary>
        /// <param name="points">Experience points to add</param>
        /// <returns>Returns true if successful</returns>
        public bool AddExperience(int points)
        {
            try
            {
                // Units cannot gain negative experience.
                if (points <= 0)
                    return false;

                // Validate points do not exceed maximum gain per action.
                if (points > CUConstants.MAX_EXP_GAIN_PER_ACTION)
                {
                    points = CUConstants.MAX_EXP_GAIN_PER_ACTION;
                }

                // Add experience points to total.
                ExperiencePoints += points;

                // Cap at Elite level.
                if (ExperiencePoints > (int)ExperiencePointLevels.Elite)
                {
                    ExperiencePoints = (int)ExperiencePointLevels.Elite;
                }

                // Store the previous level for comparison.
                var previousLevel = ExperienceLevel;

                // Get the new experience level based on updated points.
                var newLevel = CalculateExperienceLevel(ExperiencePoints);

                // If the level has changed, update and notify.
                if (newLevel != previousLevel)
                {
                    ExperienceLevel = newLevel;
                    OnExperienceLevelChanged(previousLevel, newLevel);
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AddExperience", e);
                return false;
            }
        }

        /// <summary>
        /// Safely sets the unit’s cumulative experience points (XP) and keeps
        /// <see cref="ExperienceLevel"/> tightly synchronized.
        /// </summary>
        /// <param name="points">The new total XP to apply.  Values outside
        /// <c>0 … CUConstants.EXPERIENCE_MAX</c> are automatically clamped.</param>
        /// <returns>The clamped XP value that was actually stored.</returns>
        /// <remarks>
        /// <para>
        /// Both <see cref="ExperiencePoints"/> and the derived
        /// <see cref="ExperienceLevel"/> are updated from the same clamped value,
        /// eliminating drift.  The method never throws for invalid input; any
        /// unexpected runtime errors are logged and the prior XP value is
        /// preserved.
        /// </para>
        /// </remarks>
        public int SetExperience(int points)
        {
            try
            {
                // 1) Constrain to legal range.
                int clamped = Math.Clamp(points, 0, (int)ExperiencePointLevels.Elite);

                // 2) Skip work if nothing changed – avoids redundant UI refresh.
                if (clamped == ExperiencePoints)
                    return clamped;

                // 3) Persist XP and recompute level from the same source value.
                ExperiencePoints = clamped;
                ExperienceLevel = CalculateExperienceLevel(clamped);

                return clamped;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(SetExperience), ex);
                return ExperiencePoints; // return state that survived the error
            }
        }

        /// <summary>
        /// Gets the experience points required for the next level.
        /// Returns 0 if already at maximum level (Elite).
        /// </summary>
        /// <returns>Points needed for next level, or 0 if at max level</returns>
        public int GetPointsToNextLevel()
        {
            return ExperienceLevel switch
            {
                ExperienceLevel.Raw => (int)ExperiencePointLevels.Green - ExperiencePoints,
                ExperienceLevel.Green => (int)ExperiencePointLevels.Trained - ExperiencePoints,
                ExperienceLevel.Trained => (int)ExperiencePointLevels.Experienced - ExperiencePoints,
                ExperienceLevel.Experienced => (int)ExperiencePointLevels.Veteran - ExperiencePoints,
                ExperienceLevel.Veteran => (int)ExperiencePointLevels.Elite - ExperiencePoints,
                ExperienceLevel.Elite => 0,// Already at max level
                _ => 0,
            };
        }

        /// <summary>
        /// Gets the experience progress as a percentage towards the next level (0.0 to 1.0).
        /// Returns 1.0 if at maximum level.
        /// </summary>
        /// <returns>Progress percentage towards next level</returns>
        public float GetExperienceProgress()
        {
            if (ExperienceLevel == ExperienceLevel.Elite)
                return 1.0f;

            int currentLevelMin = GetMinPointsForLevel(ExperienceLevel);
            int nextLevelMin = GetMinPointsForLevel(GetNextLevel(ExperienceLevel));

            if (nextLevelMin == currentLevelMin)
                return 1.0f;

            float progress = (float)(ExperiencePoints - currentLevelMin) / (nextLevelMin - currentLevelMin);
            return Mathf.Clamp01(progress);
        }

        #endregion // Experience System


        #region Experience System Helpers

        /// <summary>
        /// Calculates the experience level based on total experience points.
        /// </summary>
        /// <param name="totalPoints">Total experience points</param>
        /// <returns>The appropriate experience level</returns>
        private ExperienceLevel CalculateExperienceLevel(int totalPoints)
        {
            if (totalPoints >= (int)ExperiencePointLevels.Elite)
                return ExperienceLevel.Elite;
            else if (totalPoints >= (int)ExperiencePointLevels.Veteran)
                return ExperienceLevel.Veteran;
            else if (totalPoints >= (int)ExperiencePointLevels.Experienced)
                return ExperienceLevel.Experienced;
            else if (totalPoints >= (int)ExperiencePointLevels.Trained)
                return ExperienceLevel.Trained;
            else if (totalPoints >= (int)ExperiencePointLevels.Green)
                return ExperienceLevel.Green;
            else
                return ExperienceLevel.Raw;
        }

        /// <summary>
        /// Gets the minimum experience points required for a specific level.
        /// </summary>
        /// <param name="level">The experience level</param>
        /// <returns>Minimum points required for that level</returns>
        private int GetMinPointsForLevel(ExperienceLevel level)
        {
            return level switch
            {
                ExperienceLevel.Raw => (int)ExperiencePointLevels.Raw,
                ExperienceLevel.Green => (int)ExperiencePointLevels.Green,
                ExperienceLevel.Trained => (int)ExperiencePointLevels.Trained,
                ExperienceLevel.Experienced => (int)ExperiencePointLevels.Experienced,
                ExperienceLevel.Veteran => (int)ExperiencePointLevels.Veteran,
                ExperienceLevel.Elite => (int)ExperiencePointLevels.Elite,
                _ => 0,
            };
        }

        /// <summary>
        /// Gets the next experience level after the specified level.
        /// Returns Elite if already at Elite.
        /// </summary>
        /// <param name="currentLevel">Current experience level</param>
        /// <returns>Next experience level</returns>
        private ExperienceLevel GetNextLevel(ExperienceLevel currentLevel)
        {
            return currentLevel switch
            {
                ExperienceLevel.Raw => ExperienceLevel.Green,
                ExperienceLevel.Green => ExperienceLevel.Trained,
                ExperienceLevel.Trained => ExperienceLevel.Experienced,
                ExperienceLevel.Experienced => ExperienceLevel.Veteran,
                ExperienceLevel.Veteran => ExperienceLevel.Elite,
                ExperienceLevel.Elite => ExperienceLevel.Elite,// Already at max
                _ => ExperienceLevel.Green,
            };
        }

        /// <summary>
        /// Called when the unit's experience level changes.
        /// Can be overridden or used to trigger events/notifications.
        /// </summary>
        /// <param name="previousLevel">The previous experience level</param>
        /// <param name="newLevel">The new experience level</param>
        protected virtual void OnExperienceLevelChanged(ExperienceLevel previousLevel, ExperienceLevel newLevel)
        {
            AppService.CaptureUiMessage($"{UnitName} has advanced from {previousLevel} to {newLevel}!");
        }

        /// <summary>
        /// Gets the combat effectiveness multiplier based on experience level.
        /// Used to modify combat values based on unit experience.
        /// </summary>
        /// <returns>Multiplier for combat effectiveness (1.0 = normal)</returns>
        private float GetExperienceMultiplier()
        {
            return ExperienceLevel switch
            {
                ExperienceLevel.Raw => CUConstants.RAW_XP_MODIFIER,                // -20% effectiveness
                ExperienceLevel.Green => CUConstants.GREEN_XP_MODIFIER,            // -10% effectiveness
                ExperienceLevel.Trained => CUConstants.TRAINED_XP_MODIFIER,        // Normal effectiveness
                ExperienceLevel.Experienced => CUConstants.EXPERIENCED_XP_MODIFIER,// +10% effectiveness
                ExperienceLevel.Veteran => CUConstants.VETERAN_XP_MODIFIER,        // +20% effectiveness
                ExperienceLevel.Elite => CUConstants.ELITE_XP_MODIFIER,            // +30% effectiveness
                _ => 1.0f,
            };
        }

        #endregion // Experience System Helpers
    }
}
