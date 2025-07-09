
namespace HammerAndSickle.Models
{
    /* ───────────────────────────────────────────────────────────────────────────────
      IntelReport ─ Immutable snapshot of unit intelligence
      ────────────────────────────────────────────────────────────────────────────────
    
    • This class is a **pure data-transfer object (DTO)** produced exclusively
       by `IntelProfile.GenerateIntelReport(...)`.
    • Once instantiated it should be treated as read-only; UI layers and AI
       consumers must never mutate its fields.
    • A new instance should be created each time fresh intel is required.
    
    SPOTTED‑LEVEL SEMANTICS & FIELD POPULATION
    -----------------------------------------
     The accuracy and completeness of an *IntelReport* depend on the caller’s
     `SpottedLevel` argument.  The generator ensures the following contract:
    
    • **Level 0 (Not spotted)**
         – No `IntelReport` is generated; calling code should handle null.
    • **Level 1 (Name only)**
         – Metadata (UnitName, UnitNationality, UnitState, Exp/Eff levels) is
           populated.
         – Every equipment bucket remains **0**.
    • **Level 2 (Poor intel ±30 %)**
         – All buckets contain values distorted by up to ±30 % error.
    • **Level 3 (Good intel ±10 %)**
         – Buckets distorted by up to ±10 % error.
    • **Level 4 (Perfect intel)**
         – Buckets reflect exact counts; no fog‑of‑war error applied.
    
    BUCKET RULES
    ------------
    • Counts are *integers*.  After fog‑of‑war is applied, any bucket whose
       value falls below **1** is **omitted** (remains 0).
    • Buckets map to weapon‑system prefixes in `IntelProfile`:
       Men, Tanks, IFVs, APCs, Recon, Artillery, Rocket Artillery,
       Surface‑to‑Surface Missiles, SAMs, AAA, MANPADs, ATGMs,
       Attack Helicopters, Fighters, Multirole, Attack Aircraft, Bombers,
       AWACS, Recon Aircraft.
    
    PUBLIC PROPERTIES (all auto‑properties unless noted)
    ---------------------------------------------------
    int Men, Tanks, IFVs, APCs, RCNs, ARTs, ROCs, SSMs, SAMs, AAAs,
        MANPADs, ATGMs, HEL, ASFs, MRFs, ATTs, BMBs, AWACS, RCNAs;

    Nationality   UnitNationality  – nation owning the unit.
    string        UnitName         – display name (unique per combat‑unit).
    DeploymentState   UnitState        – Deployed / Mounted / etc.
    ExperienceLevel UnitExperienceLevel – Raw, Green … Elite.
    EfficiencyLevel UnitEfficiencyLevel – StaticOps … Mobile.
──────────────────────────────────────────────────────────────────────────────── */
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
        public int ASFs { get; set; } = 0;
        public int MRFs { get; set; } = 0;
        public int ATTs { get; set; } = 0;
        public int BMBs { get; set; } = 0;
        public int AWACS { get; set; } = 0;
        public int RCNAs { get; set; } = 0;

        // More intel about parent unit.
        public Nationality UnitNationality = Nationality.USSR;
        public string UnitName { get; set; } = "Default";
        public DeploymentState UnitState { get; set; } = DeploymentState.Deployed;
        public ExperienceLevel UnitExperienceLevel = ExperienceLevel.Raw;
        public EfficiencyLevel UnitEfficiencyLevel = EfficiencyLevel.StaticOperations;

        #endregion // Properties
    }
}