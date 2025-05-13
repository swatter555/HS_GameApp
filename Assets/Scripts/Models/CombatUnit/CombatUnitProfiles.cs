namespace HammerAndSickle.Models
{
    public class CombatUnitProfiles
    {
        // Combat profiles
        public WeaponSystemProfile DeployedProfile { get; private set; }
        public WeaponSystemProfile MountedProfile { get; private set; }
        public UnitProfile UnitProfile { get; private set; }
        public LandBaseProfile LandBaseProfile { get; private set; }
    }
}
