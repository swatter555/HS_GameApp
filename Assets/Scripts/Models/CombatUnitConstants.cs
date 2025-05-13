namespace HammerAndSickle.Models
{
    public class CombatUnitConstants
    {
        public const int MaxPossibleHitPoints = 40;
        public const float MaxDaysSupplyDepot = 100f;  // Max supply a depot can carry
        public const float MaxDaysSupplyUnit = 7f;     // Max supply a unit can carry
        public const int ZOCRange = 1;                 // Zone of Control Range

        // Movement constants for different unit types, in movement points.
        private const int MechanizedMovt = 12;
        private const int MotorizedMovt = 10;
        private const int NonMechanizedMovt = 8;
        private const int AirMovt = 100;
        private const int AviationMovt = 24;
    }
}