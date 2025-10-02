using System.Collections.Generic;

namespace HammerAndSickle.Core.GameData
{
    public class CUConstants
    {
        #region General Constants

        public const int SAVE_VERSION = 1;

        #endregion

        #region CombatUnit Constants

        // Individual Combat Modifiers for unit types.
        public const float ICM_MIN          =   0.1f;
        public const float ICM_MAX          =  10.0f;
        public const float ICM_DEFAULT      =   1.0f;
        public const float ICM_LARGE_UNIT   =  1.25f;
        public const float ICM_SMALL_UNIT   =  0.75f;

        // CombatUnit constants.
        public const int MAX_HP                  = 40; // Maximum hit points for a CombatUnit
        public const int MIN_HP                  = 1;  // Minimum hit points for a CombatUnit
        public const int ZOC_RANGE               = 1;  // Zone of Control Range
        public const int MAX_EXP_GAIN_PER_ACTION = 10; // Max XP gain per action

        // Experience level modifiers.
        public const float RAW_XP_MODIFIER         = 0.8f; // -20% effectiveness
        public const float GREEN_XP_MODIFIER       = 0.9f; // -10% effectiveness
        public const float TRAINED_XP_MODIFIER     = 1.0f; // Normal effectiveness
        public const float EXPERIENCED_XP_MODIFIER = 1.1f; // +10% effectiveness
        public const float VETERAN_XP_MODIFIER     = 1.2f; // +20% effectiveness
        public const float ELITE_XP_MODIFIER       = 1.3f; // +30% effectiveness

        public const float MOBILE_MOVEMENT_BONUS           = 2.0f;  // Movement point bonus for Mobile units without MountedProfile
        public const float DEPLOYMENT_ACTION_MOVEMENT_COST = 0.5f;  // Deployment actions cost 50% of max movement
        public const float COMBAT_ACTION_MOVEMENT_COST     = 0.25f; // Combat actions cost 25% of max movement
        public const float INTEL_ACTION_MOVEMENT_COST      = 0.15f; // Intel actions cost 15% of max movement

        public const float COMBAT_MOD_MOBILE        = 0.9f;  // Mobile units get 10% combat malus
        public const float COMBAT_MOD_DEPLOYED      = 1.0f;  // Deployed units have no combat modifier
        public const float COMBAT_MOD_HASTY_DEFENSE = 1.1f;  // Hasty defense gives +10% combat bonus
        public const float COMBAT_MOD_ENTRENCHED    = 1.2f;  // Entrenched units get +20% combat bonus
        public const float COMBAT_MOD_FORTIFIED     = 1.3f;  // Fortified units get +30% combat bonus

        public const float STRENGTH_MOD_FULL     = 1.15f; // Full strength units get +15% combat bonus
        public const float STRENGTH_MOD_DEPLETED = 0.75f; // Depleted strength units get -25% combat malus
        public const float STRENGTH_MOD_LOW      = 0.4f;  // Low strength units get -60% combat malus

        public const float EFFICIENCY_MOD_STATIC      = 0.5f; // Static units get 50% combat malus
        public const float EFFICIENCY_MOD_DEGRADED    = 0.7f; // Degraded units get 30% combat malus
        public const float EFFICIENCY_MOD_OPERATIONAL = 0.8f; // NormalOperations units get 20% combat malus
        public const float EFFICIENCY_MOD_FULL        = 0.9f; // Full efficiency units get 10% combat malus
        public const float EFFICIENCY_MOD_PEAK        = 1.0f; // Peak efficiency units have no combat modifier

        public const float FULL_STRENGTH_FLOOR     = 0.8f; // Minimum strength for full effectiveness
        public const float DEPLETED_STRENGTH_FLOOR = 0.5f; // Minimum strength for depleted effectiveness

        // Combat action defaults
        public const int DEFAULT_MOVE_ACTIONS        = 1;
        public const int DEFAULT_COMBAT_ACTIONS      = 1;
        public const int DEFAULT_INTEL_ACTIONS       = 1;
        public const int DEFAULT_DEPLOYMENT_ACTIONS  = 1;
        public const int DEFAULT_OPPORTUNITY_ACTIONS = 1;

        // Unit supply constants.
        public const float LOW_SUPPLY_THRESHOLD                = 1f;    // Threshold for low supply warning
        public const float CRITICAL_SUPPLY_THRESHOLD           = 0.5f;  // Threshold for critical supply warning
        public const float COMBAT_STATE_SUPPLY_TRANSITION_COST = 0.25f; // Supply cost for state transitions.
        public const float COMBAT_ACTION_SUPPLY_COST           = 1f;    // Supply cost for combat actions.
        public const float COMBAT_ACTION_SUPPLY_THRESHOLD      = 2f;    // Threshold for combat action supply cost.
        public const float MOVE_ACTION_SUPPLY_THRESHOLD        = 1.5f;  // Threshold for move action supply cost.
        public const float MOVE_ACTION_SUPPLY_COST             = 0.2f;  // Supply cost for move actions.
        public const float INTEL_ACTION_SUPPLY_COST            = 0.25f; // Supply cost for intel actions.
        public const float OPPORTUNITY_ACTION_SUPPLY_THRESHOLD = 1.5f;  // Threshold for opportunity action supply cost.
        public const float OPPORTUNITY_ACTION_SUPPLY_COST      = 0.5f;  // Supply cost for opportunity actions.

        // Intel error margins
        public const float MIN_INTEL_ERROR      = 4f;   // Minimum intel error margin
        public const float MODERATE_INTEL_ERROR = 8f;   // Maximum intel error margin
        public const float MAX_INTEL_ERROR      = 12f;  // Maximum intel error margin

        #endregion // CombatUnit Constants

        #region WeaponSystem Constants

        /* Note: Attacker targets defenders hardness/softness type. Defender targets attackers hardness/softness type */

        // WeaponSystem constants.
        public const int MAX_COMBAT_VALUE = 25;
        public const int MIN_COMBAT_VALUE = 1;
        public const float MAX_RANGE      = 100.0f;
        public const float MIN_RANGE      = 0.0f;

        // Movement constants for WeaponSystems,in movement points.
        public const int STATIC_UNIT    =   0;
        public const int FOOT_UNIT      =   4;
        public const int MOT_UNIT       =   8;
        public const int MECH_UNIT      =  10;
        public const int CAVALRY_UNIT   =  10;
        public const int NAVAL_UNIT     =  10;
        public const int HELO_UNIT      =  24;
        public const int FIXEDWING_UNIT = 100;
        
        // Ground defense against air attack.
        public const int GROUND_DEFENSE_LIGHTARMOR =  5;  // Base ground defense for APCs units
        public const int GROUND_DEFENSE_INFANTRY   =  6;  // Base ground defense for Infantry units
        public const int GROUND_DEFENSE_ARMOR      =  8;  // Base ground defense for tank units
        public const int GROUND_DEFENSE_SAM        =  8;  // Base ground defense for SAM units
        public const int GROUND_DEFENSE_HELO       = 10;  // Base air defense for helo units
        public const int GROUND_DEFENSE_AAA        = 12;  // Base ground defense for AAA units
        

        // Standard spotting range values
        public const float BASE_UNIT_SPOTTING_RANGE  = 2;
        public const float RECON_UNIT_SPOTTING_RANGE = 3;
        public const float BASE_AAA_SPOTTING_RANGE   = 3;
        public const float FACILITY_SPOTTING_RANGE   = 4;
        public const float BASE_SAM_SPOTTING_RANGE   = 6;
        public const float INTEL_UNIT_SPOTTING_RANGE = 6;

        // Standard primary range values.
        public const float PRIMARY_RANGE_DEFAULT = 1;

        // Standard indirect range values.
        public const float INDIRECT_RANGE_DEFAULT = 0;
        public const float INDIRECT_RANGE_120MM   = 4;
        public const float INDIRECT_RANGE_155MM   = 5;
        public const float INDIRECT_RANGE_203MM   = 6;
        public const float INDIRECT_RANGE_ROC_SR  = 4;
        public const float INDIRECT_RANGE_ROC_MR  = 6;
        public const float INDIRECT_RANGE_ROC_LR  = 10;

        // Standard anti-air range values.
        public const float INDIRECT_RANGE_AAA = 3;
        public const float INDIRECT_RANGE_SAM = 6;

        // Standard infantry values
        public const int BASE_INF_HARD_ATTACK  = 5;
        public const int BASE_INF_HARD_DEFENSE = 7;

        public const int BASE_INF_SOFT_ATTACK  = 7;
        public const int BASE_INF_SOFT_DEFENSE = 8;

        // Standard APC values
        public const int BASE_APC_HARD_ATTACK  = 3;
        public const int BASE_APC_HARD_DEFENSE = 4;
        public const int BASE_APC_SOFT_ATTACK  = 6;
        public const int BASE_APC_SOFT_DEFENSE = 7;

        // Standard IFV values
        public const int BASE_IFV_HARD_ATTACK  = 4;
        public const int BASE_IFV_HARD_DEFENSE = 4;
        public const int BASE_IFV_SOFT_ATTACK  = 7;
        public const int BASE_IFV_SOFT_DEFENSE = 7;

        // Standard tank soft combat values
        public const int BASE_TANK_SOFT_ATTACK  = 8;
        public const int BASE_TANK_SOFT_DEFENSE = 6;

        // Gen1 standard tank values
        public const int GEN1_TANK_HARD_ATTACK  = 7;
        public const int GEN1_TANK_HARD_DEFENSE = 5;

        // Gen2 standard tank values
        public const int GEN2_TANK_HARD_ATTACK  = 10;
        public const int GEN2_TANK_HARD_DEFENSE = 8;

        // Gen3 standard tank values
        public const int GEN3_TANK_HARD_ATTACK  = 13;
        public const int GEN3_TANK_HARD_DEFENSE = 11;

        // Gen4 standard tank values
        public const int GEN4_TANK_HARD_ATTACK  = 16;
        public const int GEN4_TANK_HARD_DEFENSE = 14;

        // Standard artillery values
        public const int BASE_ARTY_HARD_ATTACK  = 5;
        public const int BASE_ARTY_HARD_DEFENSE = 5;
        public const int BASE_ARTY_SOFT_ATTACK  = 9;
        public const int BASE_ARTY_SOFT_DEFENSE = 5;

        // AAA standard values
        public const int BASE_AAA_HARD_ATTACK       = 4;
        public const int BASE_AAA_HARD_DEFENSE      = 4;
        public const int BASE_AAA_SOFT_ATTACK       = 9;
        public const int BASE_AAA_SOFT_DEFENSE      = 6;
        public const int BASE_AAA_GROUND_AIR_ATTACK = 9;

        // SAM standard values
        public const int BASE_SAM_HARD_ATTACK       =  1;
        public const int BASE_SAM_HARD_DEFENSE      =  3;
        public const int BASE_SAM_SOFT_ATTACK       =  1;
        public const int BASE_SAM_SOFT_DEFENSE      =  3;
        public const int BASE_SAM_GROUND_AIR_ATTACK = 10;

        // Standard helo values
        public const int BASE_HEL_HARD_ATTACK = 7;
        public const int BASE_HEL_HARD_DEFENSE = 6;
        public const int BASE_HEL_SOFT_ATTACK = 10;
        public const int BASE_HEL_SOFT_DEFENSE = 7;

        // Standard fixed wing values, early generation
        public const int EARLY_FGT_DOGFIGHT  =  8;
        public const int EARLY_FGT_MANEUVER  =  9;
        public const int EARLY_FGT_TOPSPEED  = 10;
        public const int EARLY_FGT_SURVIVE   =  6;

        // Standard fixed wing values, mid generation
        public const int MID_FGT_DOGFIGHT =  10;
        public const int MID_FGT_MANEUVER =  11;
        public const int MID_FGT_TOPSPEED =  10;
        public const int MID_FGT_SURVIVE  =   7;

        // Standard fixed wing values, late generation
        public const int LATE_FGT_DOGFIGHT =  12;
        public const int LATE_FGT_MANEUVER =  12;
        public const int LATE_FGT_TOPSPEED =  10;
        public const int LATE_FGT_SURVIVE  =   9;

        // Ordinance Loads
        public const int SMALL_AC_LOAD    =  6;  // Small air-to-ground load
        public const int MEDIUM_AC_LOAD   =  9;  // Medium air-to-ground load
        public const int LARGE_AC_LOAD    = 12;  // Large air-to-ground load
        public const int XLARGE_AC_LOAD   = 16;  // Extra large air-to-ground load

        // Spotting in the AC context express ability for long range engagements
        public const int AC_SPOTTING_BASIC    = 1;
        public const int AC_SPOTTING_ENHANCED = 2;
        public const int AC_SPOTTING_ADVANCED = 3;
        public const int AC_SPOTTING_SUPERIOR = 4;

        // Air unit prestige costs
        public const int PRESTIGE_TIER_FREE  =   1;
        public const int PRESTIGE_TIER_0     =  25;
        public const int PRESTIGE_TIER_1     =  50;
        public const int PRESTIGE_TIER_2     =  75;
        public const int PRESTIGE_TIER_3     = 100;
        public const int PRESTIGE_TIER_4     = 125;
        public const int PRESTIGE_TIER_5     = 150;

        // Standard attack aircraft values
        public const int AC_ATTACK_DOGFIGHT   = 4;
        public const int AC_ATTACK_MANEUVER   = 4;
        public const int AC_ATTACK_TOPSPEED   = 7;
        public const int AC_ATTACK_SURVIVE    = 10;

        // Standard bomber values
        public const int AC_BOMBER_DOGFIGHT   = 1;
        public const int AC_BOMBER_MANEUVER   = 3;
        public const int AC_BOMBER_TOPSPEED   = 10;
        public const int AC_BOMBER_SURVIVE    = 8;

        // High mach aircraft speeds
        public const int AC_HIGHSPEED_RUSSIAN =  17;
        public const int AC_HIGHSPEED_WESTERN =  21;

        // Standard fixed wing values, attack aircraft
        public const int GROUND_ATTACK_NA      =  0;
        public const int GROUND_ATTACK_TIER_0  =  6;
        public const int GROUND_ATTACK_TIER_1  =  9;
        public const int GROUND_ATTACK_TIER_2  = 12;
        public const int GROUND_ATTACK_TIER_3  = 15;

        // Standard values for truck transport units
        public const int TRUCK_HARD_ATTACK  = 3;
        public const int TRUCK_HARD_DEFENSE = 3;
        public const int TRUCK_SOFT_ATTACK  = 3;
        public const int TRUCK_SOFT_DEFENSE = 3;

        // Standard values for facilities
        public const int BASE_HARD_ATTACK  = 4;
        public const int BASE_HARD_DEFENSE = 6;
        public const int BASE_SOFT_ATTACK  = 6;
        public const int BASE_SOFT_DEFENSE = 7;

        #endregion

        #region Deployment Constants

        public const int Embarked     = 5; // Embarked on air, helicopter, and naval transport
        public const int Mobile       = 4; // Deployed in columns for movment.
        public const int Deployed     = 3; // Deployed in combat formation.
        public const int HastyDefense = 2; // Deployed defensively, intially prepared.
        public const int Entrenched   = 1; // Deployed defensively, digging in.
        public const int Fortified    = 0; // Deployed defensively, fortified position.

        #endregion // Deployment Constants

        #region Leader Constants

        // Leader LeaderID generation
        public const string LEADER_ID_PREFIX = "LDR";
        public const int LEADER_ID_LENGTH    = 8; // LDR + 5 random chars

        // Leader validation bounds
        public const int MIN_REPUTATION         = 0;
        public const int MAX_REPUTATION         = 9999;
        public const int MAX_LEADER_NAME_LENGTH = 50;
        public const int MIN_LEADER_NAME_LENGTH = 2;

        // Reputation constants.
        public const int REP_COST_FOR_SENIOR_PROMOTION = 100;
        public const int REP_COST_FOR_TOP_PROMOTION    = 250;

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
        public const int COMMAND_BONUS_VAL            = 1;
        public const int DEPLOYMENT_ACTION_BONUS_VAL  = 1;
        public const int MOVEMENT_ACTION_BONUS_VAL    = 1;
        public const int COMBAT_ACTION_BONUS_VAL      = 1;
        public const int OPPORTUNITY_ACTION_BONUS_VAL = 1;

        // Combat rating bonuses.
        public const int HARD_ATTACK_BONUS_VAL  = 5;
        public const int HARD_DEFENSE_BONUS_VAL = 5;
        public const int SOFT_ATTACK_BONUS_VAL  = 5;
        public const int SOFT_DEFENSE_BONUS_VAL = 5;
        public const int AIR_ATTACK_BONUS_VAL   = 5;
        public const int AIR_DEFENSE_BONUS_VAL  = 5;

        // Bonus value validation bounds
        public const int MIN_COMBAT_BONUS = 1;
        public const int MAX_COMBAT_BONUS = 10;
        public const int MIN_ACTION_BONUS = 1;
        public const int MAX_ACTION_BONUS = 3;

        // Spotting and range bonuses.
        public const int SMALL_SPOTTING_RANGE_BONUS_VAL  = 1;
        public const int MEDIUM_SPOTTING_RANGE_BONUS_VAL = 2;
        public const int LARGE_SPOTTING_RANGE_BONUS_VAL  = 3;
        public const int INDIRECT_RANGE_BONUS_VAL        = 1;

        // Silouette bonuses.
        public const int SMALL_SILHOUETTE_REDUCTION_VAL  = 1;
        public const int MEDIUM_SILHOUETTE_REDUCTION_VAL = 2;
        public const int MAX_SILHOUETTE_REDUCTION_VAL    = 3;

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

        // INF doctrine multiplier.
        public const float RTO_MOVE_MULT = 0.8f;           // 20% movement cost reduction for RTOs.

        // Politically connected bonuses and multipliers.
        public const int REPLACEMENT_XP_LEVEL_VAL = 1;    // Replacements get +1 XP level.
        public const float SUPPLY_ECONOMY_MULT = 0.8f; // Supply consumption gets 20% cost reduction.
        public const float PRESTIGE_COST_MULT = 0.7f; // Unit upgrades get 30% price reduction.

        // EngineeringSpecialization specific
        public const float RIVER_CROSSING_MOVE_MULT = 0.5f; // X% movement cost reduction
        public const float RIVER_ASSAULT_MULT = 1.4f; // X% combat bonus when attacking across a river.

        // Special forces bonuses
        public const float TMASTERY_MOVE_MULT = 0.8f; // X% movement cost reduction in non-clear terrain.
        public const float INFILTRATION_MULT = 0.5f; // X% ZOC penalty reduction
        public const float AMBUSH_BONUS_MULT = 1.5f; // X% combat bonus

        // Combined arms bonus.
        public const float NIGHT_COMBAT_MULT = 1.25f;// X% combat bonus at night

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