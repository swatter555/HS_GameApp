namespace HammerAndSickle.Models
{
    public class CUConstants
    {
        public const int MaxPossibleHitPoints = 40;
        public const float MaxDaysSupplyDepot = 100f;  // Max supply a depot can carry
        public const float MaxDaysSupplyUnit = 7f;     // Max supply a unit can carry
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

        // Unit experience constants.
        public const int XP_PER_BATTLE = 25;
        public const int XP_COST_FOR_SENIOR_PROMOTION = 200;
        public const int XP_COST_FOR_TOP_PROMOTION = 500;

        // Tiered skill XP costs.
        public const int TIER1_XP_COST = 50;
        public const int TIER2_XP_COST = 75;
        public const int TIER3_XP_COST = 100;
        public const int TIER4_XP_COST = 150;

        // Bonus values used in skill definitions.
        public const float EXPERIENCE_BONUS_VAL = 0.25f;
        public const int MASKIROVKA_DETECTION_BONUS_VAL = 1;
        public const float SUPPLY_ECONOMY_REDUCTION_VAL = 0.33f;
        public const float PRESTIGE_COST_REDUCTION_VAL = 0.33f;
        public const int GENERIC_STAT_BONUS_VAL = 1; 
        public const int DEFENSE_ATTACK_BONUS_VAL = 5;
        public const int DETECTION_RANGE_BONUS_VAL = 1;
        public const int ENTRENCHMENT_BONUS_VAL = 1;
        public const int INDIRECT_RANGE_BONUS_VAL = 1;
        public const int AIR_DEFENSE_BONUS_VAL = 5;
    }
}