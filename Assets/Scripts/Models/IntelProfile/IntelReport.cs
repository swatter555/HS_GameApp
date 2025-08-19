namespace HammerAndSickle.Models
{
    public class IntelReport
    {
        #region Properties

        // Bucketted numbers for each unit type.
        public int Men { get; set; } = 0;
        public int Tanks { get; set; } = 0;
        public int IFVs { get; set; } = 0;
        public int APCs { get; set; } = 0;
        public int RCNs { get; set; } = 0;
        public int ARTs { get; set; } = 0;
        public int ROCs { get; set; } = 0;
        public int SSMs { get; set; } = 0;
        public int SAMs { get; set; } = 0;
        public int AAAs { get; set; } = 0;
        public int MANPADs { get; set; } = 0;
        public int ATGMs { get; set; } = 0;
        public int HEL { get; set; } = 0;
        public int TRANHEL { get; set; } = 0;
        public int FGTs { get; set; } = 0;
        public int ATTs { get; set; } = 0;
        public int BMBs { get; set; } = 0;
        public int AWACS { get; set; } = 0;
        public int TRANAIR { get; set; } = 0;
        public int RCNAs { get; set; } = 0;
        public int TRANNAV { get; set; } = 0;

        // More intel about parent unit.
        public Nationality UnitNationality = Nationality.USSR;
        public string UnitName { get; set; } = "Default";
        public DeploymentPosition DeploymentPosition { get; set; } = DeploymentPosition.Deployed;
        public ExperienceLevel UnitExperienceLevel = ExperienceLevel.Raw;
        public EfficiencyLevel UnitEfficiencyLevel = EfficiencyLevel.StaticOperations;

        #endregion // Properties
    }
}