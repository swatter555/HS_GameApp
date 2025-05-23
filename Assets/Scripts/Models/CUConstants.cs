using System.Collections.Generic;

namespace HammerAndSickle.Models
{
    public class CUConstants
    {
        //========================
        //====== CombatUnit ======
        //========================

        // CombatUnit constants.
        public const int MaxPossibleHitPoints = 40;
        public const int ZOCRange = 1;                 // Zone of Control Range

        // Movement constants for different unit types, in movement points.
        public const int MechanizedMovt = 12;
        public const int MotorizedMovt = 10;
        public const int NonMechanizedMovt = 8;
        public const int AirMovt = 100;
        public const int AviationMovt = 24;

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

        // Special terrain combat bonuses
        public const int URBAN_COMBAT_BONUS_VAL = 3;
        public const int ROUGH_TERRAIN_BONUS_VAL = 3;
        public const int NIGHT_COMBAT_BONUS_VAL = 3;

        // Percentage-based multipliers - some already defined
        public const float EXPERIENCE_BONUS_VAL = 0.25f;         // Replacement experience at 75% current.
        public const float SUPPLY_ECONOMY_REDUCTION_VAL = 0.33f; // 33% supply cost reduction
        public const float PRESTIGE_COST_REDUCTION_VAL = 0.33f;  // 33% prestige cost reduction

        // EngineeringSpecialization specific
        public const float RIVER_CROSSING_BONUS_VAL = 0.5f; // 50% movement cost reduction
        public const float RIVER_ASSAULT_BONUS_VAL = 0.25f; // 25% combat penalty reduction

        // Special forces bonuses
        public const float TERRAIN_MASTERY_BONUS_VAL = 0.33f; // 33% movement cost reduction
        public const float INFILTRATION_BONUS_VAL = 0.5f;     // 50% ZOC penalty reduction
        public const float AMBUSH_BONUS_MULTIPLIER = 1.5f;    // 50% combat bonus

        // Air and naval operation bonuses
        public const float AIR_MOBILE_SPEED_BONUS = 0.25f;    // 25% faster helicopter operations


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