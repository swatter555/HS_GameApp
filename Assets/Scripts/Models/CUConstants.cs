using System.Collections.Generic;

namespace HammerAndSickle.Models
{
    public class CUConstants
    {
        #region CombatUnit Constants

        // CombatUnit constants.
        public const int MAX_HP = 40;                       // Maximum hit points for a CombatUnit
        public const int ZOC_RANGE = 1;                     // Zone of Control Range
        public const int MAX_EXP_GAIN_PER_ACTION = 10;      // Max XP gain per action

        // Movement constants for different unit types, in movement points.
        public const int MECH_MOV = 12;
        public const int MOT_MOV = 10;
        public const int FOOT_MOV = 8;
        public const int FIXEDWING_MOV = 100;
        public const int HELO_MOV = 24;

        // WeaponSystem constants.
        public const int MAX_COMBAT_VALUE = 25;
        public const int MIN_COMBAT_VALUE = 1;
        public const float MAX_RANGE = 100.0f;
        public const float MIN_RANGE = 0.0f;

        // Experience level modifiers.
        public const float RAW_XP_MODIFIER = 0.8f; // -20% effectiveness
        public const float GREEN_XP_MODIFIER = 0.9f; // -10% effectiveness
        public const float TRAINED_XP_MODIFIER = 1.0f; // Normal effectiveness
        public const float EXPERIENCED_XP_MODIFIER = 1.1f; // +10% effectiveness
        public const float VETERAN_XP_MODIFIER = 1.2f; // +20% effectiveness
        public const float ELITE_XP_MODIFIER = 1.3f; // +30% effectiveness

        public const float MOBILE_MOVEMENT_BONUS = 4.0f;           // Movement point bonus for Mobile units without MountedProfile
        public const float DEPLOYMENT_ACTION_MOVEMENT_COST = 0.5f; // Deployment actions cost 50% of max movement
        public const float COMBAT_ACTION_MOVEMENT_COST = 0.25f;    // Combat actions cost 25% of max movement
        public const float INTEL_ACTION_MOVEMENT_COST = 0.15f;     // Intel actions cost 15% of max movement

        public const float COMBAT_MOD_MOBILE = 0.9f; // Mobile units get 10% combat malus
        public const float COMBAT_MOD_DEPLOYED = 1.0f; // Deployed units have no combat modifier
        public const float COMBAT_MOD_HASTY_DEFENSE = 1.1f; // Hasty defense gives +10% combat bonus
        public const float COMBAT_MOD_ENTRENCHED = 1.2f; // Entrenched units get +20% combat bonus
        public const float COMBAT_MOD_FORTIFIED = 1.3f; // Fortified units get +30% combat bonus

        public const float STRENGTH_MOD_FULL = 1.15f; // Full strength units get +15% combat bonus
        public const float STRENGTH_MOD_DEPLETED = 0.75f; // Depleted strength units get -25% combat malus
        public const float STRENGTH_MOD_LOW = 0.4f;  // Low strength units get -60% combat malus

        public const float EFFICIENCY_MOD_STATIC = 0.5f; // Static units get 50% combat malus
        public const float EFFICIENCY_MOD_DEGRADED = 0.7f; // Degraded units get 30% combat malus
        public const float EFFICIENCY_MOD_OPERATIONAL = 0.8f; // Operational units get 20% combat malus
        public const float EFFICIENCY_MOD_FULL = 0.9f; // Full efficiency units get 10% combat malus
        public const float EFFICIENCY_MOD_PEAK = 1.0f; // Peak efficiency units have no combat modifier

        public const float FULL_STRENGTH_FLOOR = 0.8f; // Minimum strength for full effectiveness
        public const float DEPLETED_STRENGTH_FLOOR = 0.5f; // Minimum strength for depleted effectiveness

        // Combat action defaults
        public const int DEFAULT_MOVE_ACTIONS = 1;
        public const int DEFAULT_COMBAT_ACTIONS = 1;
        public const int DEFAULT_INTEL_ACTIONS = 1;
        public const int DEFAULT_DEPLOYMENT_ACTIONS = 1;
        public const int DEFAULT_OPPORTUNITY_ACTIONS = 1;

        // Unit supply constants.
        public const float LOW_SUPPLY_THRESHOLD = 1f;                   // Threshold for low supply warning
        public const float CRITICAL_SUPPLY_THRESHOLD = 0.5f;            // Threshold for critical supply warning
        public const float COMBAT_STATE_SUPPLY_TRANSITION_COST = 0.25f; // Supply cost for state transitions.

        #endregion // CombatUnit Constants


        #region Leader Constants

        // Leader LeaderID generation
        public const string LEADER_ID_PREFIX = "LDR";
        public const int LEADER_ID_LENGTH = 8; // LDR + 5 random chars

        // Leader validation bounds
        public const int MIN_REPUTATION = 0;
        public const int MAX_REPUTATION = 9999;
        public const int MAX_LEADER_NAME_LENGTH = 50;
        public const int MIN_LEADER_NAME_LENGTH = 2;

        // Command ability validation (matches enum range)
        public const int MIN_COMMAND_ABILITY = -2; // CommandAbility.Poor
        public const int MAX_COMMAND_ABILITY = 3;  // CommandAbility.Genius

        // Reputation constants.
        public const int REP_COST_FOR_SENIOR_PROMOTION = 100;
        public const int REP_COST_FOR_TOP_PROMOTION = 250;

        // Tiered skill XP costs.
        public const int TIER1_REP_COST = 60;
        public const int TIER2_REP_COST = 80;
        public const int TIER3_REP_COST = 120;
        public const int TIER4_REP_COST = 180;
        public const int TIER5_REP_COST = 260;

        // Skill cost validation bounds
        public const int MIN_SKILL_REP_COST = 50;
        public const int MAX_SKILL_REP_COST = 500;

        // Command and Operation bonuses (typically +1 for actions)
        public const int COMMAND_BONUS_VAL = 1;
        public const int DEPLOYMENT_ACTION_BONUS_VAL = 1;
        public const int MOVEMENT_ACTION_BONUS_VAL = 1;
        public const int COMBAT_ACTION_BONUS_VAL = 1;
        public const int OPPORTUNITY_ACTION_BONUS_VAL = 1;

        // Combat rating bonuses.
        public const int HARD_ATTACK_BONUS_VAL = 5;
        public const int HARD_DEFENSE_BONUS_VAL = 5;
        public const int SOFT_ATTACK_BONUS_VAL = 5;
        public const int SOFT_DEFENSE_BONUS_VAL = 5;
        public const int AIR_ATTACK_BONUS_VAL = 5;
        public const int AIR_DEFENSE_BONUS_VAL = 5;

        // Bonus value validation bounds
        public const int MIN_COMBAT_BONUS = 1;
        public const int MAX_COMBAT_BONUS = 10;
        public const int MIN_ACTION_BONUS = 1;
        public const int MAX_ACTION_BONUS = 3;

        // Spotting and range bonuses.
        public const int SMALL_SPOTTING_RANGE_BONUS_VAL = 1;
        public const int MEDIUM_SPOTTING_RANGE_BONUS_VAL = 2;
        public const int LARGE_SPOTTING_RANGE_BONUS_VAL = 3;
        public const int INDIRECT_RANGE_BONUS_VAL = 1;

        // Silouette bonuses.
        public const int SMALL_SILHOUETTE_REDUCTION_VAL = 1;
        public const int MEDIUM_SILHOUETTE_REDUCTION_VAL = 2;
        public const int MAX_SILHOUETTE_REDUCTION_VAL = 3;

        // General multiplier bounds (for any positive effect)
        public const float MIN_MULTIPLIER = 0.01f;    // 1% of original value (extreme reduction)
        public const float MAX_MULTIPLIER = 10.0f;    // 10x original value (extreme boost)

        // Common decrease modifiers (what you multiply by to get the reduction)
        public const float TINY_DECREASE_MULT = 0.99f;     // 1% decrease (keep 99%)
        public const float SMALL_DECREASE_MULT = 0.90f;    // 10% decrease (keep 90%) 
        public const float MEDIUM_DECREASE_MULT = 0.80f;   // 20% decrease (keep 80%)
        public const float LARGE_DECREASE_MULT = 0.50f;    // 50% decrease (keep 50%)
        public const float HUGE_DECREASE_MULT = 0.01f;     // 99% decrease (keep 1%)

        // Common increase modifiers (what you multiply by to get the boost)
        public const float TINY_INCREASE_MULT = 1.01f;     // 1% increase (101% of original)
        public const float SMALL_INCREASE_MULT = 1.10f;    // 10% increase (110% of original)
        public const float MEDIUM_INCREASE_MULT = 1.25f;   // 25% increase (125% of original)
        public const float LARGE_INCREASE_MULT = 1.50f;    // 50% increase (150% of original)
        public const float HUGE_INCREASE_MULT = 2.00f;     // 100% increase (200% of original)

        // Validation: ensure multipliers stay within sane bounds
        public static bool IsValidMultiplier(float multiplier)
        {
            return multiplier >= MIN_MULTIPLIER && multiplier <= MAX_MULTIPLIER;
        }

        // Helper: convert percentage to multiplier
        public static float PercentToMultiplier(float percent)
        {
            return 1.0f + (percent / 100.0f);
        }

        // Helper: convert multiplier to percentage change
        public static float MultiplierToPercent(float multiplier)
        {
            return (multiplier - 1.0f) * 100.0f;
        }

        // Infantry doctrine multiplier.
        public const float RTO_MOVE_MULT = 0.8f;           // 20% movement cost reduction for RTOs.

        // Politically connected bonuses and multipliers.
        public const int REPLACEMENT_XP_LEVEL_VAL = 1;    // Replacements get +1 XP level.
        public const float SUPPLY_ECONOMY_MULT = 0.8f; // Supply consumption gets 20% cost reduction.
        public const float PRESTIGE_COST_MULT = 0.7f; // Unit upgrades get 30% price reduction.

        // EngineeringSpecialization specific
        public const float RIVER_CROSSING_MOVE_MULT = 0.5f; // x% movement cost reduction
        public const float RIVER_ASSAULT_MULT = 1.4f; // x% combat bonus when attacking across a river.

        // Special forces bonuses
        public const float TMASTERY_MOVE_MULT = 0.8f; // x% movement cost reduction in non-clear terrain.
        public const float INFILTRATION_MULT = 0.5f; // x% ZOC penalty reduction
        public const float AMBUSH_BONUS_MULT = 1.5f; // x% combat bonus

        // Combined arms bonus.
        public const float NIGHT_COMBAT_MULT = 1.25f;// x% combat bonus at night

        /// <summary>
        /// Types of actions that can award reputation to leaders
        /// </summary>
        public enum ReputationAction
        {
            Move,
            MountDismount,
            IntelGather,
            Combat,
            AirborneJump,
            ForcedRetreat,
            UnitDestroyed
        }

        // Base REP gain per action type
        public const int REP_PER_MOVE_ACTION = 1;              // Routine movement
        public const int REP_PER_MOUNT_DISMOUNT = 1;           // Mounting/dismounting transport
        public const int REP_PER_INTEL_GATHER = 2;             // Intelligence gathering (requires positioning)
        public const int REP_PER_COMBAT_ACTION = 3;            // Attacking (risk involved)
        public const int REP_PER_AIRBORNE_JUMP = 3;            // Paratrooper insertion (high risk)
        public const int REP_PER_FORCED_RETREAT = 5;           // Causing enemy to retreat (tactical success)
        public const int REP_PER_UNIT_DESTROYED = 8;           // Destroying enemy unit (major victory)

        // REP action validation bounds
        public const int MIN_REP_PER_ACTION = 1;
        public const int MAX_REP_PER_ACTION = 15;

        // Bonus REP multipliers
        public const float REP_EXPERIENCE_MULTIPLIER = 1.5f;   // Veteran/Elite units gain more REP
        public const float REP_ELITE_DIFFICULTY_BONUS = 2.0f;  // Bonus for destroying elite enemy units

        // REP multiplier bounds
        public const float MIN_REP_MULTIPLIER = 1.0f;
        public const float MAX_REP_MULTIPLIER = 3.0f;

        // For Leader.RandomlyGenerateMe() dice roll
        public const int COMMAND_DICE_COUNT = 3;         // Roll 3d6
        public const int COMMAND_DICE_SIDES = 6;         // 6-sided dice
        public const int COMMAND_DICE_MODIFIER = -10;    // Subtract 10 from total
        public const int COMMAND_CLAMP_MIN = -2;         // Minimum CommandAbility value
        public const int COMMAND_CLAMP_MAX = 0;          // Maximum CommandAbility value

        #endregion // Leader Constants


        #region Facility Constants

        // Maximum stockpile capacities by depot size
        public static readonly Dictionary<DepotSize, float> MaxStockpileBySize = new()
        {
            { DepotSize.Small, 30f },
            { DepotSize.Medium, 50f },
            { DepotSize.Large, 80f },
            { DepotSize.Huge, 110f }
        };

        // Supply generation rates by level
        public static readonly Dictionary<SupplyGenerationRate, float> GenerationRateValues = new()
        {
            { SupplyGenerationRate.Minimal, 10.0f },
            { SupplyGenerationRate.Basic, 20.0f },
            { SupplyGenerationRate.Standard, 40.0f },
            { SupplyGenerationRate.Enhanced, 80.0f }
        };

        // Supply projection ranges in hexes
        public static readonly Dictionary<SupplyProjection, int> ProjectionRangeValues = new()
        {
            { SupplyProjection.Local, 4 },
            { SupplyProjection.Extended, 8 },
            { SupplyProjection.Regional, 12 },
            { SupplyProjection.Strategic, 16 }
        };

        // Amount any unit can stockpile
        public const float MaxDaysSupplyDepot = 100f;       // Max supply a depot can carry
        public const float MaxDaysSupplyUnit = 7f;          // Max supply a unit can carry

        // Supply efficiency multipliers
        public const float DISTANCE_EFF_MULT = 0.4f;
        public const float ZOC_EFF_MULT = 0.3f;

        // Constants for special abilities
        public const int AirSupplyMaxRange = 16;
        public const int NavalSupplyMaxRange = 12;

        // Efficientcy multipliers for base operations, both Airbase and Supply Depot
        public const float BASE_CAPACITY_LVL5 = 1f;    // Full operations capacity of an airbase
        public const float BASE_CAPACITY_LVL4 = 0.75f; // 75% operations capacity
        public const float BASE_CAPACITY_LVL3 = 0.5f;  // 50% operations capacity
        public const float BASE_CAPACITY_LVL2 = 0.25f; // 25% operations capacity
        public const float BASE_CAPACITY_LVL1 = 0f;    // 0% operations capacity

        // Base damage constants
        public const int MAX_DAMAGE = 100;
        public const int MIN_DAMAGE = 0;

        // Airbase constants
        public const int MAX_AIR_UNITS = 4;        // Max air units that can be attached to an airbase.

        #endregion // Facility Constants
    }
}