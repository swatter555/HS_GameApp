using System.Collections.Generic;

namespace HammerAndSickle.Models
{
    public class CUConstants
    {
        //========================
        //====== CombatUnit ======
        //========================

        // CombatUnit constants.
        public const int MAX_HP = 40;
        public const int ZOC_RANGE = 1;                 // Zone of Control Range

        // Movement constants for different unit types, in movement points.
        public const int MECH_MOV = 12;
        public const int MOT_MOV = 10;
        public const int FOOT_MOV = 8;
        public const int FIXEDWING_MOV = 100;
        public const int HELO_MOV = 24;

        // WeaponSystem constants.
        public const int MAX_COMBAT_VALUE = 10;
        public const int MIN_COMBAT_VALUE = 0;
        public const float MAX_RANGE = 25.0f;
        public const float MIN_RANGE = 0.0f;

        //================================
        //====== Leader Skills/Tree ======
        //================================

        // Reputation constants.
        public const int REP_PER_BATTLE = 25;
        public const int REP_COST_FOR_SENIOR_PROMOTION = 200;
        public const int REP_COST_FOR_TOP_PROMOTION = 500;

        // Tiered skill XP costs.
        public const int TIER1_REP_COST = 50;
        public const int TIER2_REP_COST = 75;
        public const int TIER3_REP_COST = 100;
        public const int TIER4_REP_COST = 150;
        public const int TIER5_REP_COST = 225;

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

        // Spotting and range bonuses.
        public const int SMALL_SPOTTING_RANGE_BONUS_VAL = 1;
        public const int MEDIUM_SPOTTING_RANGE_BONUS_VAL = 2;
        public const int LARGE_SPOTTING_RANGE_BONUS_VAL = 3;
        public const int INDIRECT_RANGE_BONUS_VAL = 1;

        // Silouette bonuses.
        public const int SMALL_SILOUETTE_REDUCTION_VAL = 1;
        public const int MEDIUM_SILOUETTE_REDUCTION_VAL = 2;

        // Infantry doctrine multiplier.
        public const float RTO_MOVE_MULT = 0.8f;           // 20% movement cost reduction for RTOs.

        // Politically connected bonuses and multipliers.
        public const int REPLACEMENT_XP_LEVEL_BONUS = 1;    // Replacements get +1 XP level.
        public const float SUPPLY_ECONOMY_MULT      = 0.8f; // Supply consumption gets 20% cost reduction.
        public const float PRESTIGE_COST_MULT       = 0.7f; // Unit upgrades get 30% price reduction.

        // EngineeringSpecialization specific
        public const float RIVER_CROSSING_MOVE_MULT = 0.5f; // x% movement cost reduction
        public const float RIVER_ASSAULT_MULT       = 1.4f; // x% combat bonus when attacking across a river.

        // Special forces bonuses
        public const float TMASTERY_MOVE_MULT  = 0.8f; // x% movement cost reduction in non-clear terrain.
        public const float INFILTRATION_MULT   = 0.5f; // x% ZOC penalty reduction
        public const float AMBUSH_BONUS_MULT   = 1.5f; // x% combat bonus

        // Combined arms bonus.
        public const float NIGHT_COMBAT_MULT   = 1.25f;// x% combat bonus at night


        //=======================
        //====== LandBases ======
        //=======================

        // Maximum stockpile capacities by depot size
        public static readonly Dictionary<DepotSize, float> MaxStockpileBySize = new Dictionary<DepotSize, float>
        {
            { DepotSize.Small, 30f },
            { DepotSize.Medium, 50f },
            { DepotSize.Large, 80f },
            { DepotSize.Huge, 110f }
        };

        // Supply generation rates by level
        public static readonly Dictionary<SupplyGenerationRate, float> GenerationRateValues = new Dictionary<SupplyGenerationRate, float>
        {
            { SupplyGenerationRate.Minimal, 1.0f },
            { SupplyGenerationRate.Basic, 2.0f },
            { SupplyGenerationRate.Standard, 3.0f },
            { SupplyGenerationRate.Enhanced, 4.0f },
            { SupplyGenerationRate.Industrial, 5.0f }
        };

        // Supply projection ranges in hexes
        public static readonly Dictionary<SupplyProjection, int> ProjectionRangeValues = new Dictionary<SupplyProjection, int>
        {
            { SupplyProjection.Local, 2 },
            { SupplyProjection.Extended, 4 },
            { SupplyProjection.Regional, 6 },
            { SupplyProjection.Strategic, 9 },
            { SupplyProjection.Theater, 12 }
        };

        // Amount any unit can stockpile
        public const float MaxDaysSupplyDepot = 100f;  // Max supply a depot can carry
        public const float MaxDaysSupplyUnit = 7f;     // Max supply a unit can carry

        // Constants for special abilities
        public const int AirSupplyMaxRange = 16;
        public const int NavalSupplyMaxRange = 12;
    }
}