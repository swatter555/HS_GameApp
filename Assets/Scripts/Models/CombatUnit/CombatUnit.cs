using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;

namespace HammerAndSickle.Models
{
  /*─────────────────────────────────────────────────────────────────────────────
  CombatUnit  —  universal runtime model for every maneuver element
  ───────────────────────────────────────────────────────────────────────────────
  Scope & Role
  ────────────
    • Represents tanks, infantry, aircraft, depots, bases, etc.
    • Holds *mutable* per-unit state (HP, XP, supply, position, actions).
    • References shared, immutable templates (WeaponSystemProfile, UnitProfile,
      LandBaseFacility).

  Major Responsibilities
  ──────────────────────
    • Identification & metadata (UnitID, side, nationality, type, class).
    • Action-economy tracking: Move / Combat / Deployment / Opportunity / Intel.
    • Experience & leadership (XP progression, Leader bonuses).
    • Combat-posture state machine:
        Mobile ↔ Deployed ↔ HastyDefense ↔ Entrenched ↔ Fortified.
    • Damage, repair, and supply management.
    • Position & movement on a hex map.
    • Persistence: ISerializable two-phase load; ICloneable deep copy.

  Design Highlights
  ─────────────────
    • All max/current pairs use StatsMaxCurrent for clamping & % queries.
    • Errors funnel through AppService.Instance.HandleException.
    • Reflection used only for deep-clone private setters.
    • No Unity objects serialized—pure data only.
    • Numeric constants centralized in CUConstants.
    • Heavy object—always create via factory/GameDataManager so profile refs
      are shared and properly resolved.

  ─────────────────────────────────────────────────────────────────────────────
  PUBLIC-METHOD CHEAT SHEET
  ─────────────────────────────────────────────────────────────────────────────
  Turn & Detection
    RefreshAllActions()           Reset all action pools to max at turn start.
    RefreshMovementPoints()       Restore movement points to maximum.
    SetSpottedLevel(level)        Update Fog-of-War spotting state.

  Combat Strength
    GetCurrentCombatStrength()    Effective power with state & XP modifiers.

  Experience
    AddExperience(points)         Add XP; returns true if level-up.
    SetExperience(points)         Directly set XP (load/save).
    GetPointsToNextLevel()        XP needed for next tier.
    GetExperienceDisplayString()  Nicely formatted XP/level string.
    GetExperienceProgress()       0–1 progress to next level.
    IsExperienced()               True if Veteran or Elite.
    IsElite()                     True if Elite tier.
    GetExperienceMultiplier()     Combat multiplier from XP tier.

  Leadership
    AssignLeader(leader)          Attach commander, apply bonuses.
    RemoveLeader()                Detach commander.
    GetLeaderBonuses()            Dictionary of all bonus values.
    HasLeaderCapability(type)     Commander grants capability?
    GetLeaderBonus(type)          Numeric value of a bonus.
    HasLeader()                   Commander assigned?
    GetLeaderName()               Commander’s name.
    GetLeaderGrade()              Grade (Junior, Senior…).
    GetLeaderReputation()         Reputation points.
    GetLeaderRank()               Formatted rank string.
    GetLeaderCommandAbility()     Combat-command rating.
    HasLeaderSkill(skill)         Commander has specific skill?
    AwardLeaderReputation(action[, ctx])  Reputation via contextual action.
    AwardLeaderReputation(amount) Direct reputation award.

  Action Economy
    ConsumeMoveAction()           Spend one Move action.
    ConsumeCombatAction()         Spend Combat action + MP cost.
    ConsumeMovementPoints(pts)    Deduct MP if available.
    ConsumeDeploymentAction()     Spend Deployment action.
    ConsumeOpportunityAction()    Spend Opportunity action.
    ConsumeIntelAction()          Spend Intel action + MP cost.
    CanConsumeMoveAction()        Enough Move actions?
    CanConsumeCombatAction()      Enough Combat actions + MP?
    CanConsumeMovementPoints(pts) Enough MP?
    CanConsumeDeploymentAction()  Deployment action available?
    CanConsumeOpportunityAction() Opportunity action available?
    CanConsumeIntelAction()       Intel action available?

  Position & Movement
    SetPosition(pos)              Set hex-map coordinates directly.
    GetPosition()                 Current coordinates.
    GetDistanceTo(target)         Hex distance to point overloads (pos/unit).
    GetDistanceTo(otherUnit)
    CanMoveTo(target)             Validate MP & terrain legality.
    IsAtPosition(pos[, tol])      Position equality within tolerance.

  Damage & Supply
    TakeDamage(dmg)               Apply HP loss.
    Repair(amt)                   Restore HP up to max.
    ConsumeSupplies(amt)          Spend supply days; returns true if ok.
    ReceiveSupplies(amt)          Add supplies; returns actual stored.
    IsDestroyed()                 True when HP ≤ 0.
    CanMove()                     HP/supply/efficiency allow movement?
    GetSupplyStatus()             Supply level 0–1.

  Combat-State Management
    SetCombatState(state)         Legal state switch; consumes resources.
    CanChangeToState(state)       Validate prospective state change.
    BeginEntrenchment()           Convenience → Entrenched.
    CanEntrench()                 Prerequisites for entrenchment.
    GetValidStateTransitions()    List legal next states.

  Persistence & Cloning
    Clone()                       Deep-copy (new UnitID, shared templates).
    GetObjectData(info, ctx)      Custom binary serialization.
    HasUnresolvedReferences()     Profiles/leaders still need resolving?
    GetUnresolvedReferenceIDs()   Return unresolved reference ID list.
    ResolveReferences(mgr)        Reconnect profile/leader objects.

    IMPORTANT: Combat State vs Mounted State Clarification
───────────────────────────────────────────────────────────────────────────────
  
  Combat States and Mounting are related but distinct concepts:
  
  Combat States (CombatState enum):
  ════════════════════════════════
  • Deployed (default): Unit in battle formation, ready for combat
  • Mobile: Unit in movement columns for easier travel
  • HastyDefense/Entrenched/Fortified: Progressive defensive postures
  
  Mounted State (IsMounted boolean):
  ═════════════════════════════════
  • Only relevant for units WITH a MountedProfile (vehicles/transports)
  • True when unit is physically riding in vehicles/transports
  • False when unit is dismounted and fighting on foot
  
  Profile Switching Logic:
  ═══════════════════════
  When transitioning TO Mobile state:
    → Units WITH MountedProfile: Set IsMounted=true, ActiveProfile=MountedProfile
    → Units WITHOUT MountedProfile: Keep IsMounted=false, ActiveProfile=DeployedProfile
      but apply movement bonus (they form march columns, don't mount vehicles)
  
  When transitioning FROM Mobile to ANY other state:
    → All units: Set IsMounted=false, ActiveProfile=DeployedProfile, remove movement bonuses
  
  This design allows both mechanized units (mount vehicles) and foot units (march
  formations) to benefit from Mobile state through the same unified state system.
─────────────────────────────────────────────────────────────────────────────

  ─────────────────────────────────────────────────────────────────────────────
  Keep this comment up to date when adding/removing public APIs!
─────────────────────────────────────────────────────────────────────────────*/
    [Serializable]
    public class CombatUnit : ICloneable, ISerializable, IResolvableReferences
    {
        #region Constants

        private const string CLASS_NAME = nameof(CombatUnit);

        #endregion

        #region Fields

        // Temporary fields for deserialization reference resolution
        private string unresolvedDeployedProfileID = "";
        private string unresolvedMountedProfileID = "";
        private string unresolvedUnitProfileID = "";
        private string unresolvedLandBaseProfileID = "";
        private string unresolvedLeaderID = "";
        private string unresolvedActiveProfileID = "";

        #endregion // Fields


        #region Properties

        // Identification and metadata
        public string UnitName { get; set; }
        public string UnitID { get; private set; }
        public UnitType UnitType { get; private set; }
        public UnitClassification Classification { get; private set; }
        public UnitRole Role { get; private set; }
        public Side Side { get; private set; }
        public Nationality Nationality { get; private set; }
        public bool IsTransportable { get; private set; }
        public bool IsLandBase { get; private set; }

        // Profiles contain unit stats and capabilities.
        public WeaponSystemProfile DeployedProfile { get; private set; }
        public WeaponSystemProfile MountedProfile { get; private set; }
        public WeaponSystemProfile ActiveProfile { get; private set; }
        public UnitProfile UnitProfile { get; private set; }
        public LandBaseFacility LandBaseFacility { get; private set; }

        // The unit's leader.
        public bool IsLeaderAssigned = false;
        public Leader CommandingOfficer { get; internal set; }

        // Action counts using StatsMaxCurrent
        public StatsMaxCurrent MoveActions { get; private set; }
        public StatsMaxCurrent CombatActions { get; private set; }
        public StatsMaxCurrent DeploymentActions { get; private set; }
        public StatsMaxCurrent OpportunityActions { get; private set; }
        public StatsMaxCurrent IntelActions { get; private set; }

        // State data using StatsMaxCurrent where appropriate
        public int ExperiencePoints { get; private set; }
        public ExperienceLevel ExperienceLevel { get; private set; }
        public EfficiencyLevel EfficiencyLevel { get; internal set; }
        public bool IsMounted { get; internal set; }
        public CombatState CombatState { get; internal set; }
        public StatsMaxCurrent HitPoints { get; private set; }
        public StatsMaxCurrent DaysSupply { get; private set; }
        public StatsMaxCurrent MovementPoints { get; private set; }
        public Vector2 MapPos { get; internal set; }
        public SpottedLevel SpottedLevel { get; private set; }

        // TODO: Implement
        public List<Vector2> MovementHistoryLastTurn { get; private set; } = new List<Vector2>(); 

        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public CombatUnit()
        {
            // Set identification and metadata
            UnitName = "default";
            UnitID = Guid.NewGuid().ToString();
            UnitType = UnitType.LandUnitDF;
            Classification = UnitClassification.INF;
            Role = UnitRole.GroundCombat;
            Side = Side.Player;
            Nationality = Nationality.USSR;
            IsTransportable = true;
            IsLandBase = false;
            IsLeaderAssigned = false;

            // Set profiles
            DeployedProfile = null;
            MountedProfile = null;
            UnitProfile = null;
            LandBaseFacility = null;
            ActiveProfile = null;

            // Initialize leader (will be null until assigned)
            CommandingOfficer = null;

            // Initialize action counts based on unit type and classification
            InitializeActionCounts();

            // Initialize state with default values
            ExperiencePoints = 0;
            ExperienceLevel = ExperienceLevel.Raw;
            EfficiencyLevel = EfficiencyLevel.FullyOperational;
            IsMounted = false;
            CombatState = CombatState.Deployed;
            SpottedLevel = SpottedLevel.Level1;

            // Initialize StatsMaxCurrent properties
            HitPoints = new StatsMaxCurrent(CUConstants.MAX_HP);
            DaysSupply = new StatsMaxCurrent(CUConstants.MaxDaysSupplyUnit);

            // Initialize movement based on unit classification
            InitializeMovementPoints();

            // Initialize position to origin (will be set when placed on map)
            MapPos = Vector2.zero;
        }

        /// <summary>
        /// Creates a new CombatUnit with the specified core properties.
        /// </summary>
        /// <param name="unitName">Display name of the unit</param>
        /// <param name="unitType">Type of unit (land, air, naval)</param>
        /// <param name="classification">Unit classification (tank, infantry, etc.)</param>
        /// <param name="role">Primary role of the unit</param>
        /// <param name="side">Which side controls this unit</param>
        /// <param name="nationality">National affiliation</param>
        /// <param name="deployedProfile">Combat profile when deployed</param>
        /// <param name="mountedProfile">Combat profile when mounted (can be null)</param>
        /// <param name="unitProfile">Organizational profile for tracking losses</param>
        /// <param name="isTransportable">Whether this unit can be transported</param>
        /// <param name="isLandBase">Whether this unit is a land-based facility</param>
        /// <param name="landBaseProfile">Land base profile if applicable (can be null)</param>
        public CombatUnit(
            string unitName,
            UnitType unitType,
            UnitClassification classification,
            UnitRole role,
            Side side,
            Nationality nationality,
            WeaponSystemProfile deployedProfile,
            WeaponSystemProfile mountedProfile,
            UnitProfile unitProfile,
            bool isTransportable,
            bool isLandBase = false,
            LandBaseFacility landBaseProfile = null)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(unitName))
                    throw new ArgumentException("Unit name cannot be null or empty", nameof(unitName));

                if (deployedProfile == null)
                    throw new ArgumentNullException(nameof(deployedProfile), "Deployed profile is required");

                if (unitProfile == null)
                    throw new ArgumentNullException(nameof(unitProfile), "Unit profile is required");

                // Validate land base requirements
                if (isLandBase && landBaseProfile == null)
                    throw new ArgumentException("Land base profile is required when isLandBase is true", nameof(landBaseProfile));

                // Set identification and metadata
                UnitName = unitName;
                UnitID = Guid.NewGuid().ToString();
                UnitType = unitType;
                Classification = classification;
                Role = role;
                Side = side;
                Nationality = nationality;
                IsTransportable = isTransportable;
                IsLandBase = isLandBase;
                IsLeaderAssigned = false; // Default to no leader assigned

                // Set profiles
                DeployedProfile = deployedProfile;
                MountedProfile = mountedProfile;
                ActiveProfile = deployedProfile; // Default to deployed profile
                UnitProfile = unitProfile;
                LandBaseFacility = landBaseProfile;

                // Initialize leader (will be null until assigned)
                CommandingOfficer = null;

                // Initialize action counts based on unit type and classification
                InitializeActionCounts();

                // Initialize state with default values
                ExperiencePoints = 0;
                ExperienceLevel = ExperienceLevel.Raw;
                EfficiencyLevel = EfficiencyLevel.FullyOperational;
                IsMounted = false;
                CombatState = CombatState.Deployed;
                SpottedLevel = SpottedLevel.Level1;

                // Initialize StatsMaxCurrent properties
                HitPoints = new StatsMaxCurrent(CUConstants.MAX_HP);
                DaysSupply = new StatsMaxCurrent(CUConstants.MaxDaysSupplyUnit);

                // Initialize movement based on unit classification
                InitializeMovementPoints();

                // Initialize position to origin (will be set when placed on map)
                MapPos = Vector2.zero;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Deserialization constructor for loading CombatUnit from saved data.
        /// </summary>
        /// <param name="info">Serialization info containing saved data</param>
        /// <param name="context">Streaming context for deserialization</param>
        protected CombatUnit(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Load basic properties
                UnitName = info.GetString(nameof(UnitName));
                UnitID = info.GetString(nameof(UnitID));
                UnitType = (UnitType)info.GetValue(nameof(UnitType), typeof(UnitType));
                Classification = (UnitClassification)info.GetValue(nameof(Classification), typeof(UnitClassification));
                Role = (UnitRole)info.GetValue(nameof(Role), typeof(UnitRole));
                Side = (Side)info.GetValue(nameof(Side), typeof(Side));
                Nationality = (Nationality)info.GetValue(nameof(Nationality), typeof(Nationality));
                IsTransportable = info.GetBoolean(nameof(IsTransportable));
                IsLandBase = info.GetBoolean(nameof(IsLandBase));
                IsLeaderAssigned = info.GetBoolean(nameof(IsLeaderAssigned));
                SpottedLevel = (SpottedLevel)info.GetValue(nameof(SpottedLevel), typeof(SpottedLevel));
                
                // Store profile IDs for later resolution (don't resolve objects yet)
                unresolvedDeployedProfileID = info.GetString("DeployedProfileID");
                unresolvedMountedProfileID = info.GetString("MountedProfileID");
                unresolvedUnitProfileID = info.GetString("UnitProfileID");
                unresolvedLandBaseProfileID = info.GetString("LandBaseFacilityID");
                unresolvedLeaderID = info.GetString("LeaderID");
                unresolvedActiveProfileID = info.GetString("ActiveProfileID");
                
                // Deserialize owned StatsMaxCurrent objects
                HitPoints = new StatsMaxCurrent(
                    info.GetSingle("HitPoints_Max"),
                    info.GetSingle("HitPoints_Current")
                );

                DaysSupply = new StatsMaxCurrent(
                    info.GetSingle("DaysSupply_Max"),
                    info.GetSingle("DaysSupply_Current")
                );

                MovementPoints = new StatsMaxCurrent(
                    info.GetSingle("MovementPoints_Max"),
                    info.GetSingle("MovementPoints_Current")
                );

                MoveActions = new StatsMaxCurrent(
                    info.GetSingle("MoveActions_Max"),
                    info.GetSingle("MoveActions_Current")
                );

                CombatActions = new StatsMaxCurrent(
                    info.GetSingle("CombatActions_Max"),
                    info.GetSingle("CombatActions_Current")
                );

                DeploymentActions = new StatsMaxCurrent(
                    info.GetSingle("DeploymentActions_Max"),
                    info.GetSingle("DeploymentActions_Current")
                );

                OpportunityActions = new StatsMaxCurrent(
                    info.GetSingle("OpportunityActions_Max"),
                    info.GetSingle("OpportunityActions_Current")
                );

                IntelActions = new StatsMaxCurrent(
                    info.GetSingle("IntelActions_Max"),
                    info.GetSingle("IntelActions_Current")
                );

                // Load simple properties
                ExperiencePoints = info.GetInt32(nameof(ExperiencePoints));
                ExperienceLevel = (ExperienceLevel)info.GetValue(nameof(ExperienceLevel), typeof(ExperienceLevel));
                EfficiencyLevel = (EfficiencyLevel)info.GetValue(nameof(EfficiencyLevel), typeof(EfficiencyLevel));
                IsMounted = info.GetBoolean(nameof(IsMounted));
                CombatState = (CombatState)info.GetValue(nameof(CombatState), typeof(CombatState));
                MapPos = (Vector2)info.GetValue(nameof(MapPos), typeof(Vector2));

                // Leave all object references null - they will be resolved later
                DeployedProfile = null;
                MountedProfile = null;
                UnitProfile = null;
                LandBaseFacility = null;
                CommandingOfficer = null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CombatUnit), e);
                throw;
            }
        }

        #endregion


        #region Initialization Methods

        /// <summary>
        /// Initializes action counts based on unit type and classification.
        /// Most units get standard action counts, with variations for special cases.
        /// </summary>
        private void InitializeActionCounts()
        {
            // Standard action counts for most units
            int moveActions = CUConstants.DEFAULT_MOVE_ACTIONS;
            int combatActions = CUConstants.DEFAULT_COMBAT_ACTIONS;
            int deploymentActions = CUConstants.DEFAULT_DEPLOYMENT_ACTIONS;
            int opportunityActions = CUConstants.DEFAULT_OPPORTUNITY_ACTIONS;
            int intelActions = CUConstants.DEFAULT_INTEL_ACTIONS;


            switch (Classification)
            {
                case UnitClassification.TANK:
                case UnitClassification.MECH:
                case UnitClassification.MOT:
                case UnitClassification.AB:
                case UnitClassification.MAB:
                case UnitClassification.MAR:
                case UnitClassification.MMAR:
                case UnitClassification.AT:
                case UnitClassification.INF:
                case UnitClassification.ART:
                case UnitClassification.SPA:
                case UnitClassification.ROC:
                case UnitClassification.BM:
                case UnitClassification.ENG:
                case UnitClassification.HELO:
                    break;

                case UnitClassification.RECON:
                    moveActions += 1;
                    break;
                
                case UnitClassification.AM:
                case UnitClassification.MAM:
                    deploymentActions += 1;
                    break;
                
                case UnitClassification.SPECF:
                case UnitClassification.SPECM:
                case UnitClassification.SPECH:
                    intelActions += 1;
                    break;
                
                case UnitClassification.SAM:
                case UnitClassification.SPSAM:
                case UnitClassification.AAA:
                case UnitClassification.SPAAA:
                    opportunityActions += 1;
                    break;

                case UnitClassification.ASF:
                case UnitClassification.MRF:
                case UnitClassification.ATT:
                case UnitClassification.BMB:
                case UnitClassification.RECONA:
                    moveActions += 2;
                    combatActions = 0;
                    deploymentActions = 0;
                    intelActions = 0;
                    break;

                case UnitClassification.HQ:
                    intelActions += 1;
                    break;

                case UnitClassification.DEPOT:
                case UnitClassification.AIRB:
                    moveActions = 0;
                    combatActions = 0;
                    deploymentActions = 0;
                    opportunityActions = 0;
                    intelActions = 0;
                    break;

                default:
                    break;
            }

            // Create StatsMaxCurrent instances
            MoveActions = new StatsMaxCurrent(moveActions);
            CombatActions = new StatsMaxCurrent(combatActions);
            DeploymentActions = new StatsMaxCurrent(deploymentActions);
            OpportunityActions = new StatsMaxCurrent(opportunityActions);
            IntelActions = new StatsMaxCurrent(intelActions);
        }

        /// <summary>
        /// Initializes movement points based on unit classification.
        /// </summary>
        private void InitializeMovementPoints()
        {
            var maxMovement = Classification switch
            {
                UnitClassification.TANK or 
                UnitClassification.MECH or 
                UnitClassification.RECON or
                UnitClassification.MAB or
                UnitClassification.MAM or
                UnitClassification.MMAR or
                UnitClassification.SPECM or
                UnitClassification.SPA or 
                UnitClassification.SPAAA or 
                UnitClassification.SPSAM => CUConstants.MECH_MOV,

                UnitClassification.AT or
                UnitClassification.MOT or
                UnitClassification.ROC => CUConstants.MOT_MOV,

                UnitClassification.INF or 
                UnitClassification.AB or
                UnitClassification.AM or
                UnitClassification.MAR or
                UnitClassification.ART or
                UnitClassification.SAM or
                UnitClassification.AAA or
                UnitClassification.SPECF or 
                UnitClassification.ENG => CUConstants.FOOT_MOV,

                UnitClassification.ASF or 
                UnitClassification.MRF or 
                UnitClassification.ATT or 
                UnitClassification.BMB or 
                UnitClassification.RECONA => CUConstants.FIXEDWING_MOV,

                UnitClassification.HELO or 
                UnitClassification.SPECH => CUConstants.HELO_MOV,

                UnitClassification.HQ or 
                UnitClassification.DEPOT or 
                UnitClassification.AIRB => 0,// Bases don't move

                _ => CUConstants.FOOT_MOV,// Default to foot movement
            };
            MovementPoints = new StatsMaxCurrent(maxMovement);
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Refreshes all action counts to their maximum values.
        /// Called at the start of each turn.
        /// </summary>
        public void RefreshAllActions()
        {
            MoveActions.ResetToMax();
            CombatActions.ResetToMax();
            DeploymentActions.ResetToMax();
            OpportunityActions.ResetToMax();
            IntelActions.ResetToMax();
        }

        /// <summary>
        /// Refreshes movement points to maximum.
        /// Called at the start of each turn.
        /// </summary>
        public void RefreshMovementPoints()
        {
            MovementPoints.ResetToMax();
        }

        /// <summary>
        /// Sets the spotted level for the current instance.
        /// </summary>
        /// <param name="spottedLevel">The new spotted level to assign.</param>
        public void SetSpottedLevel(SpottedLevel spottedLevel)
        {
            SpottedLevel = spottedLevel;
        }

        /// <summary>
        /// Get the adjusted combat strength based on current mounted state and all applicable modifiers.
        /// </summary>
        /// <returns>Modified values for current profile</returns>
        public WeaponSystemProfile GetCurrentCombatStrength()
        {
            try
            {
                // Sanity check.
                if (ActiveProfile == null)
                    throw new InvalidOperationException("Active profile is not set.");

                // Clone the active profile.
                WeaponSystemProfile combatProfile = ActiveProfile.Clone();         

                // Compute all modifiers that can effect a combat rating.
                float finalModifier = GetFinalCombatRatingModifier();

                // Apply the final modifier to the active profile's stats.
                combatProfile.LandHard.SetAttack(Mathf.CeilToInt(combatProfile.LandHard.Attack * finalModifier));
                combatProfile.LandHard.SetDefense(Mathf.CeilToInt(combatProfile.LandHard.Defense * finalModifier));

                combatProfile.LandSoft.SetAttack(Mathf.CeilToInt(combatProfile.LandSoft.Attack * finalModifier));
                combatProfile.LandSoft.SetDefense(Mathf.CeilToInt(combatProfile.LandSoft.Defense * finalModifier));

                combatProfile.LandAir.SetAttack(Mathf.CeilToInt(combatProfile.LandAir.Attack * finalModifier));
                combatProfile.LandAir.SetDefense(Mathf.CeilToInt(combatProfile.LandAir.Defense * finalModifier));

                combatProfile.Air.SetAttack(Mathf.CeilToInt(combatProfile.Air.Attack * finalModifier));
                combatProfile.Air.SetDefense(Mathf.CeilToInt(combatProfile.Air.Defense * finalModifier));

                combatProfile.AirGround.SetAttack(Mathf.CeilToInt(combatProfile.AirGround.Attack * finalModifier));
                combatProfile.AirGround.SetDefense(Mathf.CeilToInt(combatProfile.AirGround.Defense * finalModifier));

                return combatProfile;

            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetCurrentCombatStrength", e);
                return null; // Return null if an error occurs
            }
        }



        #endregion // Public Methods


        #region Private Methods

        /// <summary>
        /// Calculates the final combat rating modifier by combining various contributing factors.
        /// </summary>
        /// <remarks>This method aggregates multiple modifiers, including strength, combat state,
        /// efficiency, and experience, to compute the final combat rating modifier. If an error occurs during
        /// calculation, the method returns a default neutral modifier of 1.0.</remarks>
        /// <returns>A <see cref="float"/> representing the final combat rating modifier. The value is the product of all
        /// contributing modifiers, or 1.0 if an error occurs.</returns>
        private float GetFinalCombatRatingModifier()
        {
            try
            {
                // Calculate the final combat rating modifier based on all factors.
                float strengthModifier = GetStrengthModifier();
                float combatStateModifier = GetCombatStateModifier();
                float efficiencyModifier = GetEfficiencyModifier();
                float experienceModifier = GetExperienceMultiplier();

                // Combine all modifiers
                return strengthModifier * combatStateModifier * efficiencyModifier * experienceModifier;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetFinalCombatRatingModifier", e);
                return 1.0f; // Default to neutral modifier on error
            }   
        }

        /// <summary>
        /// Gets the combat effectiveness as a percentage based on current hit points.
        /// </summary>
        /// <returns>Combat effectiveness from 0.0 to 1.0</returns>
        private float GetStrengthModifier()
        {
            try
            {
                // Compute the combat strength multiplier based on hit points.
                if (HitPoints.Current >= (HitPoints.Max * CUConstants.FULL_STRENGTH_FLOOR))
                {
                    return CUConstants.STRENGTH_MOD_FULL;
                }
                else if (HitPoints.Current >= (HitPoints.Max * CUConstants.DEPLETED_STRENGTH_FLOOR))
                {
                    return CUConstants.STRENGTH_MOD_DEPLETED;
                }
                else
                {
                    return CUConstants.STRENGTH_MOD_LOW;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetStrengthModifier", e);
                return CUConstants.STRENGTH_MOD_LOW;
            }
        }

        /// <summary>
        /// Calculates the combat state modifier based on the current combat state.
        /// </summary>
        /// <returns>A <see cref="float"/> representing the combat state multiplier. The value corresponds to the current combat
        /// state, with predefined modifiers for specific states. Returns <c>1.0f</c> if the combat state is not
        /// recognized.</returns>
        private float GetCombatStateModifier()
        {
            // Returns the combat state multiplier based on current combat state.
            return CombatState switch
            {
                CombatState.Mobile => CUConstants.COMBAT_MOD_MOBILE,
                CombatState.Deployed => CUConstants.COMBAT_MOD_DEPLOYED,
                CombatState.HastyDefense => CUConstants.COMBAT_MOD_HASTY_DEFENSE,
                CombatState.Entrenched => CUConstants.COMBAT_MOD_ENTRENCHED,
                CombatState.Fortified => CUConstants.COMBAT_MOD_FORTIFIED,
                _ => 1.0f, // Default multiplier for other states
            };
        }

        /// <summary>
        /// Calculates the efficiency modifier based on the current efficiency level.
        /// </summary>
        /// <remarks>The returned modifier is determined by the current <c>EfficiencyLevel</c> and maps to
        /// specific  constants defined in <c>CUConstants</c>. If the efficiency level is unrecognized, a default 
        /// static modifier is returned.</remarks>
        /// <returns>A <see cref="float"/> representing the efficiency modifier. The value corresponds to the current 
        /// operational state, with predefined constants for each efficiency level.</returns>
        private float GetEfficiencyModifier()
        {
            // Returns the efficiency modifier based on current efficiency level.
            return EfficiencyLevel switch
            {
                EfficiencyLevel.PeakOperational => CUConstants.EFFICIENCY_MOD_PEAK,
                EfficiencyLevel.FullyOperational => CUConstants.EFFICIENCY_MOD_FULL,
                EfficiencyLevel.Operational => CUConstants.EFFICIENCY_MOD_OPERATIONAL,
                EfficiencyLevel.DegradedOperations => CUConstants.EFFICIENCY_MOD_DEGRADED,
                _ => CUConstants.EFFICIENCY_MOD_STATIC, // Default multiplier for other states
            };
        }

        /// <summary>
        /// Gets the display name of a unit by its ID, with fallback handling.
        /// </summary>
        /// <param name="unitId">The unit ID to look up</param>
        /// <returns>The unit's display name or a fallback string</returns>
        private string GetUnitDisplayName(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
            {
                return "Unknown Unit";
            }

            try
            {
                var unit = GameDataManager.Instance.GetCombatUnit(unitId);
                return unit?.UnitName ?? $"Unit {unitId}";
            }
            catch (Exception e)
            {
                // Log the query failure but return fallback name
                AppService.HandleException(CLASS_NAME, "GetUnitDisplayName", e, ExceptionSeverity.Minor);
                return $"Unit {unitId}";
            }
        }

        #endregion // Private Methods


        #region Experience System Methods

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
                    throw new ArgumentOutOfRangeException(nameof(points), "Experience points must be positive.");

                // Validate points do not exceed maximum gain per action.
                if (points > CUConstants.MAX_EXP_GAIN_PER_ACTION)
                    throw new ArgumentOutOfRangeException(nameof(points), $"Experience points cannot exceed {CUConstants.MAX_EXP_GAIN_PER_ACTION} per action.");

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
        /// Sets the unit's experience points directly and updates the level accordingly.
        /// Used for loading saved games or manual experience setting.
        /// </summary>
        /// <param name="points">Total experience points</param>
        public void SetExperience(int points)
        {
            try
            {
                if (points < 0)
                    points = 0;

                ExperiencePoints = Math.Min(points, (int)ExperiencePointLevels.Elite);
                ExperienceLevel = CalculateExperienceLevel(points);
                
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetExperience", e);
            }
        }

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
        /// Gets the experience level as a human-readable string with progress information.
        /// </summary>
        /// <returns>Formatted string showing level and progress</returns>
        public string GetExperienceDisplayString()
        {
            var pointsToNext = GetPointsToNextLevel();
            if (pointsToNext > 0)
            {
                return $"{ExperienceLevel} ({ExperiencePoints} XP, {pointsToNext} to next)";
            }
            else
            {
                return $"{ExperienceLevel} (Max Level - {ExperiencePoints} XP)";
            }
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
        /// Checks if the unit is considered experienced (Veteran or Elite).
        /// Used for various game mechanics that benefit experienced units.
        /// </summary>
        /// <returns>True if unit is Veteran or Elite</returns>
        public bool IsExperienced()
        {
            return ExperienceLevel == ExperienceLevel.Veteran || ExperienceLevel == ExperienceLevel.Elite;
        }

        /// <summary>
        /// Checks if the unit is considered elite level.
        /// </summary>
        /// <returns>True if unit is Elite</returns>
        public bool IsElite()
        {
            return ExperienceLevel == ExperienceLevel.Elite;
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
        public float GetExperienceMultiplier()
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

        #endregion // Experience System Methods


        #region Leader Assignment System

        /// <summary>
        /// Assigns a leader to command this unit.
        /// Handles unassigning any current leader and updating all necessary state.
        /// Validates that the leader is available and manages bidirectional assignment.
        /// </summary>
        /// <param name="leader">The leader to assign to this unit</param>
        /// <returns>True if assignment was successful, false otherwise</returns>
        public bool AssignLeader(Leader leader)
        {
            try
            {
                // 1. Parameter validation
                if (leader == null)
                {
                    AppService.CaptureUiMessage("Cannot assign commander: No leader specified.");
                    return false;
                }

                // 2. Prevent redundant assignment - check if same leader already assigned
                if (CommandingOfficer != null && CommandingOfficer.LeaderID == leader.LeaderID)
                {
                    AppService.CaptureUiMessage($"{leader.FormattedRank} {leader.Name} is already commanding {UnitName}.");
                    return false;
                }

                // 3. Check if the incoming leader is already assigned to another unit
                if (leader.IsAssigned)
                {
                    string assignedUnitName = GetUnitDisplayName(leader.UnitID);
                    AppService.CaptureUiMessage($"Cannot assign {leader.Name}: Already commanding {assignedUnitName}.");
                    return false;
                }

                // 4. Store current leader for potential rollback
                Leader previousLeader = CommandingOfficer;
                bool hadPreviousLeader = IsLeaderAssigned;

                try
                {
                    // 5. Unassign current leader if one exists
                    if (CommandingOfficer != null)
                    {
                        string currentLeaderName = CommandingOfficer.Name;
                        CommandingOfficer.UnassignFromUnit();

                        // Enhanced UI message with unit context
                        AppService.CaptureUiMessage($"{currentLeaderName} has been relieved of command of {UnitName} and is now available in the leader pool.");

                        CommandingOfficer = null;
                    }

                    // 6. Assign the new leader using Leader class method
                    // This handles setting IsAssigned = true and UnitID properly
                    leader.AssignToUnit(UnitID);

                    // 7. Set our reference to the new leader
                    CommandingOfficer = leader;

                    // 8. Update our assignment flag
                    IsLeaderAssigned = true;

                    // 9. Capture UI message about successful assignment
                    AppService.CaptureUiMessage($"{leader.FormattedRank} {leader.Name} has been assigned to command {UnitName}.");

                    // 10. Validate consistency
                    if (!ValidateLeaderAssignmentConsistency())
                    {
                        throw new InvalidOperationException("Leader assignment consistency validation failed");
                    }

                    return true;
                }
                catch (Exception innerException)
                {
                    // Rollback changes if anything went wrong
                    try
                    {
                        // Restore previous leader if we had one
                        if (hadPreviousLeader && previousLeader != null)
                        {
                            previousLeader.AssignToUnit(UnitID);
                            CommandingOfficer = previousLeader;
                            IsLeaderAssigned = true;

                            AppService.CaptureUiMessage($"Assignment failed - {previousLeader.Name} remains in command of {UnitName}.");
                        }
                        else
                        {
                            // No previous leader, ensure clean state
                            CommandingOfficer = null;
                            IsLeaderAssigned = false;
                        }

                        // Ensure the new leader is unassigned after failed attempt
                        if (leader.IsAssigned && leader.UnitID == UnitID)
                        {
                            leader.UnassignFromUnit();
                        }
                    }
                    catch (Exception rollbackException)
                    {
                        // Log rollback failure but don't throw - we're already in error handling
                        AppService.HandleException(CLASS_NAME, "AssignLeader",
                            new InvalidOperationException("Failed to rollback leader assignment", rollbackException),
                            ExceptionSeverity.Critical);
                    }

                    // Log the original error and notify user
                    AppService.HandleException(CLASS_NAME, "AssignLeader", innerException);
                    AppService.CaptureUiMessage($"Failed to assign {leader.Name} to {UnitName}.");
                    return false;
                }
            }
            catch (Exception e)
            {
                // Handle any unexpected errors
                AppService.HandleException(CLASS_NAME, "AssignLeader", e);
                AppService.CaptureUiMessage("Leader assignment failed due to an unexpected error.");
                return false;
            }
        }

        /// <summary>
        /// Removes the commanding officer from this unit.
        /// Handles proper state management and cleanup for both unit and leader.
        /// </summary>
        /// <returns>True if removal was successful, false if no leader was assigned or removal failed</returns>
        public bool RemoveLeader()
        {
            try
            {
                // 1. Check if there's actually a leader to remove
                if (CommandingOfficer == null || !IsLeaderAssigned)
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have a commanding officer to remove.");
                    return false;
                }

                // 1a. Additional safety check - verify leader thinks it's assigned to this unit
                if (CommandingOfficer.UnitID != UnitID)
                {
                    AppService.HandleException(CLASS_NAME, "RemoveLeader",
                        new InvalidOperationException($"{CommandingOfficer.Name} thinks it's assigned to {CommandingOfficer.UnitID} but unit thinks it's {UnitID}"),
                        ExceptionSeverity.Minor);

                    // Fix the inconsistency and continue
                    FixLeaderAssignmentConsistency();

                    // Re-check after consistency fix
                    if (CommandingOfficer == null || !IsLeaderAssigned)
                    {
                        AppService.CaptureUiMessage($"{UnitName} does not have a commanding officer to remove after consistency fix.");
                        return false;
                    }
                }

                // 2. Store current leader info for UI messaging and potential rollback
                Leader currentLeader = CommandingOfficer;
                string leaderName = currentLeader.Name;
                string leaderRank = currentLeader.FormattedRank; // Capture rank before unassignment

                try
                {
                    // 3. Unassign the leader using Leader class method
                    // This handles setting IsAssigned = false and UnitID = null properly
                    currentLeader.UnassignFromUnit();

                    // 4. Clear our reference to the leader
                    CommandingOfficer = null;

                    // 5. Update our assignment flag
                    IsLeaderAssigned = false;

                    // 6. Capture UI message about successful removal - using captured rank
                    AppService.CaptureUiMessage($"{leaderRank} {leaderName} has been relieved of command of {UnitName} and is now available for reassignment.");

                    // 7. Validate consistency
                    if (!ValidateLeaderAssignmentConsistency())
                    {
                        throw new InvalidOperationException("Leader removal consistency validation failed");
                    }

                    return true;
                }
                catch (Exception innerException)
                {
                    // Rollback changes if anything went wrong
                    try
                    {
                        // Restore the leader assignment
                        currentLeader.AssignToUnit(UnitID);
                        CommandingOfficer = currentLeader;
                        IsLeaderAssigned = true;

                        // Consistent messaging - using captured name and rank
                        AppService.CaptureUiMessage($"Removal failed - {leaderRank} {leaderName} remains in command of {UnitName}.");
                    }
                    catch (Exception rollbackException)
                    {
                        // Log rollback failure but don't throw - we're already in error handling
                        AppService.HandleException(CLASS_NAME, "RemoveLeader",
                            new InvalidOperationException("Failed to rollback leader removal", rollbackException),
                            ExceptionSeverity.Critical);

                        // Force consistency fix since rollback failed
                        FixLeaderAssignmentConsistency();
                    }

                    // Log the original error and notify user - consistent with rollback message
                    AppService.HandleException(CLASS_NAME, "RemoveLeader", innerException);
                    AppService.CaptureUiMessage($"Failed to remove {leaderRank} {leaderName} from command of {UnitName}.");
                    return false;
                }
            }
            catch (Exception e)
            {
                // Handle any unexpected errors
                AppService.HandleException(CLASS_NAME, "RemoveLeader", e);
                AppService.CaptureUiMessage("Leader removal failed due to an unexpected error.");

                // Attempt to fix any consistency issues that might have occurred
                try
                {
                    FixLeaderAssignmentConsistency();
                }
                catch (Exception consistencyException)
                {
                    AppService.HandleException(CLASS_NAME, "RemoveLeader", consistencyException, ExceptionSeverity.Minor);
                }

                return false;
            }
        }

        /// <summary>
        /// Gets all bonuses provided by the commanding officer's skills.
        /// Returns an empty dictionary if no leader is assigned.
        /// </summary>
        /// <returns>Dictionary mapping skill bonus types to their values</returns>
        public Dictionary<SkillBonusType, float> GetLeaderBonuses()
        {
            var bonuses = new Dictionary<SkillBonusType, float>();

            try
            {
                // Return empty dictionary if no leader assigned
                if (CommandingOfficer == null)
                {
                    return bonuses;
                }

                // Iterate through all skill bonus types and get non-zero values
                foreach (SkillBonusType bonusType in (SkillBonusType[])Enum.GetValues(typeof(SkillBonusType)))
                {
                    if (bonusType == SkillBonusType.None) continue;

                    float bonusValue = CommandingOfficer.GetBonusValue(bonusType);
                    if (bonusValue != 0f)
                    {
                        bonuses[bonusType] = bonusValue;
                    }
                }

                return bonuses;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetLeaderBonuses", e);
                return bonuses; // Return empty dictionary on error
            }
        }

        /// <summary>
        /// Checks if the unit has a specific leader capability/bonus.
        /// </summary>
        /// <param name="bonusType">The bonus type to check for</param>
        /// <returns>True if the leader provides this capability</returns>
        public bool HasLeaderCapability(SkillBonusType bonusType) =>
            CommandingOfficer != null && CommandingOfficer.HasCapability(bonusType);

        /// <summary>
        /// Gets a specific leader bonus value.
        /// Returns 0 if no leader assigned or bonus not present.
        /// </summary>
        /// <param name="bonusType">The type of bonus to retrieve</param>
        /// <returns>The bonus value, or 0 if not present</returns>
        public float GetLeaderBonus(SkillBonusType bonusType) =>
            CommandingOfficer != null && bonusType != SkillBonusType.None
            ? CommandingOfficer.GetBonusValue(bonusType)
            : 0f;

        /// <summary>
        /// Checks if a leader is currently assigned to this unit.
        /// </summary>
        /// <returns>True if a leader is assigned</returns>
        public bool HasLeader()
        {
            return CommandingOfficer != null;
        }

        /// <summary>
        /// Gets the leader's name for display purposes.
        /// Returns empty string if no leader assigned.
        /// </summary>
        /// <returns>Leader name or empty string</returns>
        public string GetLeaderName() => CommandingOfficer?.Name ?? string.Empty;

        /// <summary>
        /// Gets the leader's command grade for display and bonus calculations.
        /// Returns JuniorGrade if no leader assigned.
        /// </summary>
        /// <returns>Leader's command grade</returns>
        public CommandGrade GetLeaderGrade() => CommandingOfficer?.CommandGrade ?? CommandGrade.JuniorGrade;

        /// <summary>
        /// Gets the leader's reputation points for display purposes.
        /// Returns 0 if no leader assigned.
        /// </summary>
        /// <returns>Leader's reputation points</returns>
        public int GetLeaderReputation() => CommandingOfficer?.ReputationPoints ?? 0;

        /// <summary>
        /// Gets the leader's formatted rank based on nationality.
        /// Returns empty string if no leader assigned.
        /// </summary>
        /// <returns>Formatted rank string</returns>
        public string GetLeaderRank() => CommandingOfficer?.FormattedRank ?? "";

        /// <summary>
        /// Gets the leader's combat command ability modifier.
        /// Returns Average if no leader assigned.
        /// </summary>
        /// <returns>Leader's combat command ability</returns>
        public CommandAbility GetLeaderCommandAbility() =>
            CommandingOfficer?.CombatCommand ?? CommandAbility.Average;

        /// <summary>
        /// Checks if the leader has unlocked a specific skill.
        /// Returns false if no leader assigned.
        /// </summary>
        /// <param name="skillEnum">The skill to check</param>
        /// <returns>True if the skill is unlocked</returns>
        public bool HasLeaderSkill(Enum skill) =>
            CommandingOfficer != null && CommandingOfficer.IsSkillUnlocked(skill);

        /// <summary>
        /// Awards reputation to the leader for unit actions.
        /// Does nothing if no leader assigned.
        /// </summary>
        /// <param name="actionType">Type of action performed</param>
        /// <param name="contextMultiplier">Context-based multiplier (default 1.0)</param>
        public void AwardLeaderReputation(CUConstants.ReputationAction actionType, float contextMultiplier = 1.0f)
        {
            try
            {
                if (CommandingOfficer == null)
                {
                    return;
                }

                CommandingOfficer.AwardReputationForAction(actionType, contextMultiplier);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AwardLeaderReputation", e);
            }
        }

        /// <summary>
        /// Awards reputation points directly to the leader.
        /// Does nothing if no leader assigned.
        /// </summary>
        /// <param name="amount">Amount of reputation to award</param>
        public void AwardLeaderReputation(int amount)
        {
            try
            {
                if (CommandingOfficer == null || amount <= 0)
                {
                    return;
                }

                CommandingOfficer.AwardReputation(amount);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AwardLeaderReputation", e);
            }
        }

        /// <summary>
        /// Validates that IsLeaderAssigned flag is consistent with CommandingOfficer state.
        /// </summary>
        /// <returns>True if consistent, false if there's a mismatch</returns>
        private bool ValidateLeaderAssignmentConsistency()
        {
            return (CommandingOfficer == null && !IsLeaderAssigned) ||
                   (CommandingOfficer != null && IsLeaderAssigned &&
                    CommandingOfficer.IsAssigned && CommandingOfficer.UnitID == UnitID);
        }

        /// <summary>
        /// Fixes any inconsistency between IsLeaderAssigned flag and CommandingOfficer state.
        /// </summary>
        private void FixLeaderAssignmentConsistency()
        {
            bool hasLeader = CommandingOfficer != null;
            if (IsLeaderAssigned != hasLeader)
            {
                AppService.HandleException(CLASS_NAME, "FixLeaderAssignmentConsistency",
                    new InvalidOperationException($"Leader assignment inconsistency fixed for unit {UnitID}"),
                    ExceptionSeverity.Minor);
                IsLeaderAssigned = hasLeader;
            }
        }

        #endregion // Leader Assignment System


        #region Action Consumption System

        /// <summary>
        /// Consumes one move action if available.
        /// </summary>
        /// <returns>True if a move action was consumed, false if none available</returns>
        public bool ConsumeMoveAction()
        {
            try
            {
                if (MoveActions.Current >= 1f)
                {
                    MoveActions.SetCurrent(MoveActions.Current - 1f);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ConsumeMoveAction", e);
                return false;
            }
        }

        /// <summary>
        /// Consumes one combat action and associated movement points if available.
        /// </summary>
        /// <returns>True if a combat action was consumed, false if insufficient resources</returns>
        public bool ConsumeCombatAction()
        {
            try
            {
                // Check combat action availability
                if (CombatActions.Current < 1f)
                    return false;

                // Calculate and consume movement points first
                float movementCost = GetCombatActionMovementCost();
                if (!ConsumeMovementPoints(movementCost))
                    return false;

                // Only consume combat action if movement was successful
                CombatActions.SetCurrent(CombatActions.Current - 1f);
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ConsumeCombatAction", e);
                return false;
            }
        }

        /// <summary>
        /// Consumes movement points if available.
        /// </summary>
        /// <param name="points">Number of movement points to consume</param>
        /// <returns>True if movement points were consumed, false if insufficient</returns>
        public bool ConsumeMovementPoints(float points)
        {
            try
            {
                if (points <= 0f)
                {
                    throw new ArgumentException("Movement points must be positive", nameof(points));
                }

                if (MovementPoints.Current >= points)
                {
                    MovementPoints.SetCurrent(MovementPoints.Current - points);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ConsumeMovementPoints", e);
                return false;
            }
        }

        /// <summary>
        /// Consumes one deployment action if available.
        /// </summary>
        /// <returns>True if a deployment action was consumed, false if none available</returns>
        public bool ConsumeDeploymentAction()
        {
            try
            {
                if (DeploymentActions.Current >= 1f)
                {
                    DeploymentActions.SetCurrent(DeploymentActions.Current - 1f);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ConsumeDeploymentAction", e);
                return false;
            }
        }

        /// <summary>
        /// Consumes one opportunity action if available.
        /// </summary>
        /// <returns>True if an opportunity action was consumed, false if none available</returns>
        public bool ConsumeOpportunityAction()
        {
            try
            {
                if (OpportunityActions.Current >= 1f)
                {
                    OpportunityActions.SetCurrent(OpportunityActions.Current - 1f);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ConsumeOpportunityAction", e);
                return false;
            }
        }

        /// <summary>
        /// Consumes one intelligence action and associated movement points if available.
        /// Bases don't consume movement points for intel gathering.
        /// </summary>
        /// <returns>True if an intelligence action was consumed, false if insufficient resources</returns>
        public bool ConsumeIntelAction()
        {
            try
            {
                // Check intel action availability
                if (IntelActions.Current < 1f)
                    return false;

                // Bases don't consume movement points for intel gathering
                if (!IsLandBase)
                {
                    // Calculate and consume movement points first
                    float movementCost = GetIntelActionMovementCost();
                    if (!ConsumeMovementPoints(movementCost))
                        return false;
                }

                // Only consume intel action if movement was successful (or not needed for bases)
                IntelActions.SetCurrent(IntelActions.Current - 1f);
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ConsumeIntelAction", e);
                return false;
            }
        }

        /// <summary>
        /// Checks if the unit can consume a move action.
        /// </summary>
        /// <returns>True if at least one move action is available</returns>
        public bool CanConsumeMoveAction()
        {
            return MoveActions.Current >= 1f;
        }

        /// <summary>
        /// Checks if the unit can consume a combat action and has sufficient movement points.
        /// </summary>
        /// <returns>True if at least one combat action and sufficient movement are available</returns>
        public bool CanConsumeCombatAction()
        {
            // Check if we have a combat action available
            if (CombatActions.Current < 1f)
            {
                return false;
            }

            // Check if we have sufficient movement points
            float requiredMovement = GetCombatActionMovementCost();
            if (MovementPoints.Current < requiredMovement)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the unit can consume the specified movement points.
        /// </summary>
        /// <param name="points">Number of movement points needed</param>
        /// <returns>True if sufficient movement points are available</returns>
        public bool CanConsumeMovementPoints(float points)
        {
            if (points <= 0f) return false;
            return MovementPoints.Current >= points;
        }

        /// <summary>
        /// Checks if the unit can consume a deployment action.
        /// </summary>
        /// <returns>True if at least one deployment action is available</returns>
        public bool CanConsumeDeploymentAction()
        {
            return DeploymentActions.Current >= 1f;
        }

        /// <summary>
        /// Checks if the unit can consume an opportunity action.
        /// </summary>
        /// <returns>True if at least one opportunity action is available</returns>
        public bool CanConsumeOpportunityAction()
        {
            return OpportunityActions.Current >= 1f;
        }

        /// <summary>
        /// Checks if the unit can consume an intelligence action and has sufficient movement points.
        /// Bases don't require movement points for intel actions.
        /// </summary>
        /// <returns>True if at least one intelligence action and sufficient movement are available</returns>
        public bool CanConsumeIntelAction()
        {
            // Check if we have an intel action available
            if (IntelActions.Current < 1f)
            {
                return false;
            }

            // Bases don't need movement points for intel gathering
            if (IsLandBase)
            {
                return true;
            }

            // Check if we have sufficient movement points
            float requiredMovement = GetIntelActionMovementCost();
            if (MovementPoints.Current < requiredMovement)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a dictionary mapping each action type to the number of **truly** available tokens
        /// after validating both action counters and movement‑point prerequisites.
        /// </summary>
        public Dictionary<string, float> GetAvailableActions()
        {
            // Move – must have a token and at least 1 movement point remaining.
            float moveAvailable = (CanConsumeMoveAction() && MovementPoints.Current > 0f)
                ? MoveActions.Current : 0f;

            // Combat – existing validation already checks movement cost.
            float combatAvailable = CanConsumeCombatAction() ? CombatActions.Current : 0f;

            // Deployment – needs token **and** 50 % of max movement (unless immobile base).
            float deployMpCost = MovementPoints.Max * CUConstants.DEPLOYMENT_ACTION_MOVEMENT_COST;
            bool canDeploy = MovementPoints.Max == 0f || MovementPoints.Current >= deployMpCost;
            float deploymentAvailable = (CanConsumeDeploymentAction() && canDeploy)
                ? DeploymentActions.Current : 0f;

            // Opportunity – purely reactive, no validation.
            float opportunityAvailable = OpportunityActions.Current;

            // Intel – existing validation already handles base / movement logic.
            float intelAvailable = CanConsumeIntelAction() ? IntelActions.Current : 0f;

            return new Dictionary<string, float>
            {
                ["Move"] = moveAvailable,
                ["Combat"] = combatAvailable,
                ["Deployment"] = deploymentAvailable,
                ["Opportunity"] = opportunityAvailable,
                ["Intelligence"] = intelAvailable,
                ["MovementPoints"] = MovementPoints.Current
            };
        }

        #endregion // Action Consumption System


        #region Position and Movement Management

        /// <summary>
        /// Sets the unit's position on the map.
        /// </summary>
        /// <param name="newPos">The new position coordinates</param>
        public void SetPosition(Vector2 newPos)
        {
            try
            {
                MapPos = newPos; // Direct assignment instead of reflection
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetPosition", e);
                throw;
            }
        }

        /// <summary>
        /// Checks if the unit can move to the specified position.
        /// This is a basic validation - full movement rules will be implemented later.
        /// </summary>
        /// <param name="targetPos">The target position to validate</param>
        /// <returns>True if movement appears valid</returns>
        public bool CanMoveTo(Vector2 targetPos)
        {
            throw new NotImplementedException(); // Placeholder for future movement validation logic
        }

        /// <summary>
        /// Gets the distance between this unit and a target position in Unity units.
        /// </summary>
        /// <param name="targetPos">The target position</param>
        /// <returns>Distance in Unity units</returns>
        public float GetDistanceTo(Vector2 targetPos)
        {
            throw new NotImplementedException(); // Placeholder for future distance calculation logic
        }

        /// <summary>
        /// Gets the distance between this unit and another unit.
        /// </summary>
        /// <param name="otherUnit">The other unit</param>
        /// <returns>Distance in Unity units</returns>
        public float GetDistanceTo(CombatUnit otherUnit)
        {
            throw new NotImplementedException(); // Placeholder for future distance calculation logic
        }

        /// <summary>
        /// Checks if the unit is at the specified position (within tolerance).
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="tolerance">Distance tolerance (default 0.01f)</param>
        /// <returns>True if unit is at the position</returns>
        public bool IsAtPosition(Vector2 position, float tolerance = 0.01f)
        {
            throw new NotImplementedException(); // Placeholder for future position checking logic
        }

        /// <summary>
        /// Gets the unit's current map position.
        /// </summary>
        /// <returns>Current position on the map</returns>
        public Vector2 GetPosition()
        {
            return MapPos;
        }

        #endregion // Position and Movement Management


        #region Damage and Supply Systems

        /// <summary>
        /// Applies damage to the unit, reducing hit points and updating combat effectiveness.
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        public void TakeDamage(float damage)
        {
            try
            {
                if (damage < 0f)
                {
                    throw new ArgumentException("Damage cannot be negative", nameof(damage));
                }

                if (damage == 0f)
                {
                    return; // No damage to apply
                }

                // Apply damage to hit points
                float newHitPoints = Mathf.Max(0f, HitPoints.Current - damage);
                HitPoints.SetCurrent(newHitPoints);

                // Update unit profile to reflect current strength
                if (UnitProfile != null)
                {
                    UnitProfile.UpdateCurrentHP(HitPoints.Current);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "TakeDamage", e);
                throw;
            }
        }

        /// <summary>
        /// Repairs damage to the unit, restoring hit points.
        /// </summary>
        /// <param name="repairAmount">Amount of damage to repair</param>
        public void Repair(float repairAmount)
        {
            try
            {
                if (repairAmount < 0f)
                {
                    throw new ArgumentException("Repair amount cannot be negative", nameof(repairAmount));
                }

                if (repairAmount == 0f)
                {
                    return; // No repair to apply
                }

                // Apply repair to hit points (clamped to maximum)
                float newHitPoints = Mathf.Min(HitPoints.Max, HitPoints.Current + repairAmount);
                HitPoints.SetCurrent(newHitPoints);

                // Update unit profile to reflect current strength
                if (UnitProfile != null)
                {
                    UnitProfile.UpdateCurrentHP(HitPoints.Current);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Repair", e);
                throw;
            }
        }

        /// <summary>
        /// Consumes supplies for unit operations.
        /// </summary>
        /// <param name="amount">Amount of supplies to consume</param>
        /// <returns>True if supplies were consumed, false if insufficient</returns>
        public bool ConsumeSupplies(float amount)
        {
            try
            {
                if (amount < 0f)
                {
                    throw new ArgumentException("Supply amount cannot be negative", nameof(amount));
                }

                if (amount == 0f)
                {
                    return true; // No supplies to consume
                }

                if (DaysSupply.Current >= amount)
                {
                    DaysSupply.SetCurrent(DaysSupply.Current - amount);
                    return true;
                }

                return false; // Insufficient supplies
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ConsumeSupplies", e);
                return false;
            }
        }

        /// <summary>
        /// Receives supplies from external source (depot, transport, etc.).
        /// </summary>
        /// <param name="amount">Amount of supplies offered</param>
        /// <returns>Actual amount of supplies received (may be less than offered due to capacity)</returns>
        public float ReceiveSupplies(float amount)
        {
            try
            {
                if (amount < 0f)
                {
                    throw new ArgumentException("Supply amount cannot be negative", nameof(amount));
                }

                if (amount == 0f)
                {
                    return 0f; // No supplies offered
                }

                // Calculate how much we can actually receive
                float availableCapacity = DaysSupply.Max - DaysSupply.Current;
                float actualAmount = Mathf.Min(amount, availableCapacity);

                // Add supplies
                DaysSupply.SetCurrent(DaysSupply.Current + actualAmount);

                return actualAmount;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ReceiveSupplies", e);
                return 0f;
            }
        }

        /// <summary>
        /// Checks if the unit is destroyed (no hit points remaining).
        /// </summary>
        /// <returns>True if the unit is destroyed</returns>
        public bool IsDestroyed()
        {
            return HitPoints.Current <= 0f;
        }

        /// <summary>
        /// Checks if the unit can move based on various factors.
        /// </summary>
        /// <returns></returns>
        public bool CanMove()
        {
            try
            {
                // Must have hit points to operate
                if (IsDestroyed())
                {
                    return false;
                }

                // Must have some supplies for most operations.
                if (DaysSupply.Current < 1f)
                {
                    return false;
                }

                // Low efficiency level units cannot move.
                if (EfficiencyLevel == EfficiencyLevel.StaticOperations)
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CanOperate", e);
                return false;
            }
        }

        /// <summary>
        /// Gets the supply status as a percentage of maximum capacity.
        /// </summary>
        /// <returns>Supply status from 0.0 to 1.0</returns>
        public float GetSupplyStatus()
        {
            try
            {
                if (DaysSupply.Max <= 0f)
                {
                    return 0f;
                }

                return DaysSupply.Current / DaysSupply.Max;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetSupplyStatus", e);
                return 0f;
            }
        }

        #endregion // Damage and Supply Systems


        #region Combat State Management

        /// <summary>
        /// Direct change of combat state for debugging purposes.
        /// </summary>
        /// <param name="newState"></param>
        public void DebugSetCombatState(CombatState newState)
        {
            CombatState = newState;
        }

        /// <summary>
        /// Changes the unit's combat state if transition is valid and resources are available.
        /// Handles profile switching and movement point costs automatically.
        /// </summary>
        /// <param name="newState">The target combat state</param>
        /// <returns>True if state change was successful</returns>
        public bool SetCombatState(CombatState newState)
        {
            try
            {
                // Check if state change is allowed. If not allowed, UI message already sent to player.
                if (!CanChangeToState(newState))
                    return false;

                // Cost in movement points.
                float movementCost = GetDeploymentActionMovementCost();

                // Make double sure the points are there.
                if(ConsumeMovementPoints(movementCost))
                {
                    // Use DeploymentAction.
                    ConsumeDeploymentAction();

                    // Update combat state directly
                    CombatState = newState;

                    // Apply profile changes for new state
                    UpdateStateAndProfiles(newState);

                    return true;
                }
                else
                {
                    AppService.CaptureUiMessage($"Insufficient movement points to change to {newState} state.");
                    return false;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetCombatState", e);
                return false;
            }
        }

        /// <summary>
        /// Checks if the unit can transition to the specified combat state.
        /// Validates unit type restrictions, adjacency rules, and resource requirements.
        /// </summary>
        /// <param name="targetState">The desired combat state</param>
        /// <returns>True if transition is allowed</returns>
        public bool CanChangeToState(CombatState targetState)
        {
            try
            {
                // Capture the UI message if needed.
                string errorMessage = $"Cannot change from {CombatState} to {targetState}: ";

                // Same state - no change needed
                if (CombatState == targetState)
                {
                    errorMessage += "Already in target state.";
                    AppService.CaptureUiMessage(errorMessage);
                    return false;
                }
                    
                // Air units and bases cannot change states.
                if (!CanUnitTypeChangeStates())
                {
                    errorMessage += "Unit type cannot change combat states.";
                    AppService.CaptureUiMessage(errorMessage);
                    return false;
                }

                // Check if transition is adjacent
                if (!IsAdjacentStateTransition(CombatState, targetState))
                {
                    errorMessage += $"Transition from {CombatState} to {targetState} is not adjacent.";
                    AppService.CaptureUiMessage(errorMessage);
                    return false;
                }

                // Make sure you have a deployment action to spend.
                if (!CanConsumeDeploymentAction())
                {
                    errorMessage += "No deployment actions available for state change.";
                    AppService.CaptureUiMessage(errorMessage);
                    return false;
                }

                // You need sufficient movement points for deployment actions.
                if (!HasSufficientMovementForDeployment())
                {
                    errorMessage += "Insufficient movement points for deployment action.";
                    AppService.CaptureUiMessage(errorMessage);
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CanChangeToState", e);
                return false;
            }
        }

        /// <summary>
        /// Begins entrenchment process by transitioning to HastyDefense.
        /// Convenience method for defensive positioning.
        /// </summary>
        /// <returns>True if entrenchment began successfully</returns>
        public bool BeginEntrenchment()
        {
            try
            {
                if (CombatState != CombatState.Deployed)
                {
                    return false; // Can only start entrenchment from Deployed
                }

                return SetCombatState(CombatState.HastyDefense);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "BeginEntrenchment", e);
                return false;
            }
        }

        /// <summary>
        /// Checks if the unit can begin entrenchment (transition to defensive states).
        /// </summary>
        /// <returns>True if entrenchment is possible</returns>
        public bool CanEntrench()
        {
            try
            {
                return CanChangeToState(CombatState.HastyDefense);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CanEntrench", e);
                return false;
            }
        }

        /// <summary>
        /// Gets all valid combat states this unit can transition to from current state.
        /// </summary>
        /// <returns>List of valid target states</returns>
        public List<CombatState> GetValidStateTransitions()
        {
            var validStates = new List<CombatState>();

            try
            {
                if (!CanUnitTypeChangeStates())
                {
                    return validStates; // Return empty list
                }

                // Check each possible state
                foreach (CombatState state in (CombatState[])Enum.GetValues(typeof(CombatState)))
                {
                    if (CanChangeToState(state))
                    {
                        validStates.Add(state);
                    }
                }

                return validStates;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetValidStateTransitions", e);
                return validStates;
            }
        }

        #endregion // Combat State Management


        #region Private Combat State Helpers

        /// <summary>
        /// Checks if this unit classification can change combat states.
        /// Fixed-wing aircraft and bases cannot change states.
        /// </summary>
        /// <returns>True if unit can change states</returns>
        private bool CanUnitTypeChangeStates()
        {
            // Fixed-wing aircraft cannot change states
            if (Classification == UnitClassification.ASF ||
                Classification == UnitClassification.MRF ||
                Classification == UnitClassification.ATT ||
                Classification == UnitClassification.BMB ||
                Classification == UnitClassification.RECONA)
            {
                return false;
            }

            // Bases cannot change states
            if (Classification == UnitClassification.HQ ||
                Classification == UnitClassification.DEPOT ||
                Classification == UnitClassification.AIRB)
            {
                return false;
            }

            // All other units (including helicopters) can change states
            return true;
        }

        /// <summary>
        /// Checks if the transition between two states is adjacent (one step).
        /// </summary>
        /// <param name="currentState">Current combat state</param>
        /// <param name="targetState">Target combat state</param>
        /// <returns>True if transition is adjacent</returns>
        private bool IsAdjacentStateTransition(CombatState currentState, CombatState targetState)
        {
            // Define the state order: Mobile ← Deployed → HastyDefense → Entrenched → Fortified
            var stateOrder = new Dictionary<CombatState, int>
            {
                { CombatState.Mobile, 0 },
                { CombatState.Deployed, 1 },
                { CombatState.HastyDefense, 2 },
                { CombatState.Entrenched, 3 },
                { CombatState.Fortified, 4 }
            };

            if (!stateOrder.ContainsKey(currentState) || !stateOrder.ContainsKey(targetState))
            {
                return false;
            }

            int currentIndex = stateOrder[currentState];
            int targetIndex = stateOrder[targetState];

            // Adjacent means difference of exactly 1
            return Math.Abs(currentIndex - targetIndex) == 1;
        }

        /// <summary>
        /// Checks if unit has sufficient movement points for a deployment action.
        /// Deployment actions cost 50% of max movement points.
        /// </summary>
        /// <returns>True if sufficient movement points available</returns>
        private bool HasSufficientMovementForDeployment()
        {
            float requiredMovement = GetDeploymentActionMovementCost();
            return MovementPoints.Current >= requiredMovement;
        }

        /// <summary>
        /// Calculates the movement point cost for a deployment action.
        /// </summary>
        /// <returns>Movement points required (50% of max)</returns>
        private float GetDeploymentActionMovementCost()
        {
            return MovementPoints.Max * CUConstants.DEPLOYMENT_ACTION_MOVEMENT_COST;
        }
       
        /// <summary>
        /// Updates the combat state and adjusts the active profile and movement points accordingly.
        /// </summary>
        /// <remarks>This method updates the unit's state and associated profiles based on the provided
        /// combat state.  If the state is <see cref="CombatState.Mobile"/>, the method applies movement bonuses and
        /// sets the active profile  to the mounted profile if available; otherwise, it uses the deployed profile. For
        /// other states, the unit is unmounted,  movement bonuses are removed, and the deployed profile is set as
        /// active. <para> Preconditions: <list type="bullet"> <item><description>If the unit is mounted, <see
        /// cref="MountedProfile"/> must not be <see langword="null"/>.</description></item> <item><description><see
        /// cref="DeployedProfile"/> must not be <see langword="null"/>.</description></item> </list> </para> <para>
        /// Postconditions: <list type="bullet"> <item><description>The active profile is updated to match the specified
        /// state.</description></item> <item><description>Movement points are adjusted based on the state and any
        /// applicable bonuses.</description></item> <item><description>The mounted state is toggled as
        /// necessary.</description></item> </list> </para></remarks>
        /// <param name="state">The desired combat state to transition to. Must be a valid <see cref="CombatState"/> value.</param>
        private void UpdateStateAndProfiles(CombatState state)
        {
            try
            {
                // Sanity check.
                if ((IsMounted && MountedProfile == null) || DeployedProfile == null)
                    throw new InvalidOperationException("Cannot update state with null profiles.");

                // Start with CombatState.Mobile.
                if (state == CombatState.Mobile)
                {
                    // Check for a mounted profile.
                    if (MountedProfile != null)
                    {
                        // If we are not already mounted, mount now.
                        if (!IsMounted) IsMounted = true; // Set mounted state

                        // Set active profile to the mounted one.
                        ActiveProfile = MountedProfile; // Switch to mounted profile
                    }
                    else
                    {
                        // No MountedProfile available - add movement bonus to current movement
                        float movementBonus = CUConstants.MOBILE_MOVEMENT_BONUS;
                        float newMaxMovement = MovementPoints.Max + movementBonus;

                        // Update max movement while preserving current percentage
                        float currentPercentage = MovementPoints.GetPercentage();
                        MovementPoints.SetMax(newMaxMovement);
                        MovementPoints.SetCurrent(newMaxMovement * currentPercentage);

                        // Set active profile to deployed profile.
                        ActiveProfile = DeployedProfile;
                    }
                }
                // Handle other states with deployed profile and unmounted.
                else
                {
                    // Unmount unit.
                    if (IsMounted) IsMounted = false;

                    // Remove movement bonus -revert to base movement
                    InitializeMovementPoints();

                    // Set the deployed profile as active.
                    ActiveProfile = DeployedProfile;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpdateStateAndProfiles", e);
            }
        }

        /// <summary>
        /// Calculates the movement‑point cost for a combat action.
        /// Immobile units (Max == 0) pay nothing.
        /// </summary>
        private float GetCombatActionMovementCost()
        {
            if (MovementPoints.Max <= 0f) return 0f;
            return MovementPoints.Max * CUConstants.COMBAT_ACTION_MOVEMENT_COST;
        }

        /// <summary>
        /// Calculates the movement‑point cost for an intelligence action.
        /// Immobile units (Max == 0) pay nothing.
        /// </summary>
        private float GetIntelActionMovementCost()
        {
            if (MovementPoints.Max <= 0f) return 0f;
            return MovementPoints.Max * CUConstants.INTEL_ACTION_MOVEMENT_COST;
        }

        #endregion // Private Combat State Helpers


        #region ICloneable Implementation

        public object Clone()
        {
            try
            {
                // Create new unit using constructor with same core properties
                // This ensures proper initialization and generates a new UnitID
                var clone = new CombatUnit(
                    this.UnitName,
                    this.UnitType,
                    this.Classification,
                    this.Role,
                    this.Side,
                    this.Nationality,
                    this.DeployedProfile,      // Shared reference
                    this.MountedProfile,       // Shared reference  
                    this.UnitProfile,          // Shared reference
                    this.IsTransportable,
                    this.IsLandBase,
                    // Deep clone LandBaseFacility if this unit is not a land base itself
                    this.IsLandBase ? this.LandBaseFacility : this.LandBaseFacility?.Clone()
                )
                {
                    // Deep copy all StatsMaxCurrent objects by reconstructing them
                    // This overwrites the default values set by the constructor
                    HitPoints = new StatsMaxCurrent(this.HitPoints.Max, this.HitPoints.Current),
                    DaysSupply = new StatsMaxCurrent(this.DaysSupply.Max, this.DaysSupply.Current),
                    MovementPoints = new StatsMaxCurrent(this.MovementPoints.Max, this.MovementPoints.Current),
                    MoveActions = new StatsMaxCurrent(this.MoveActions.Max, this.MoveActions.Current),
                    CombatActions = new StatsMaxCurrent(this.CombatActions.Max, this.CombatActions.Current),
                    DeploymentActions = new StatsMaxCurrent(this.DeploymentActions.Max, this.DeploymentActions.Current),
                    OpportunityActions = new StatsMaxCurrent(this.OpportunityActions.Max, this.OpportunityActions.Current),
                    IntelActions = new StatsMaxCurrent(this.IntelActions.Max, this.IntelActions.Current)
                };

                // Copy per-unit state data
                clone.SetExperience(this.ExperiencePoints); // This also sets ExperienceLevel correctly

                // Copy properties with private setters using reflection
                var cloneType = typeof(CombatUnit);

                // Copy CommandingOfficer (shared reference)
                cloneType.GetProperty("CommandingOfficer")
                    ?.SetValue(clone, this.CommandingOfficer);
                clone.IsLeaderAssigned = this.IsLeaderAssigned;

                // Copy state properties
                cloneType.GetProperty("EfficiencyLevel")
                    ?.SetValue(clone, this.EfficiencyLevel);
                cloneType.GetProperty("IsMounted")
                    ?.SetValue(clone, this.IsMounted);
                cloneType.GetProperty("CombatState")
                    ?.SetValue(clone, this.CombatState);
                cloneType.GetProperty("MapPos")
                    ?.SetValue(clone, this.MapPos);
                cloneType.GetProperty("ActiveProfile")
                    ?.SetValue(clone, this.ActiveProfile);
                cloneType.GetProperty("SpottedLevel")
                    ?.SetValue(clone, this.SpottedLevel);

                // Copy unresolved reference fields (should be empty in normal cloning scenarios)
                cloneType.GetField("unresolvedDeployedProfileID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(clone, this.unresolvedDeployedProfileID);
                cloneType.GetField("unresolvedMountedProfileID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(clone, this.unresolvedMountedProfileID);
                cloneType.GetField("unresolvedUnitProfileID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(clone, this.unresolvedUnitProfileID);
                cloneType.GetField("unresolvedLandBaseProfileID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(clone, this.unresolvedLandBaseProfileID);
                cloneType.GetField("unresolvedLeaderID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(clone, this.unresolvedLeaderID);
                cloneType.GetField("unresolvedActiveProfileID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(clone, this.unresolvedActiveProfileID);

                // Important: Don't copy the LandBaseFacility reference for non-land-base units
                // The clone should start unattached to any base
                if (!clone.IsLandBase)
                {
                    clone.CommandingOfficer = null; // Will need to be reassigned
                                                    // Note: LandBaseFacility reference handled in constructor above
                }

                return clone;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        #endregion // ICloneable Implementation


        #region ISerializable Implementation

        /// <summary>
        /// Serializes CombatUnit data for saving to file.
        /// </summary>
        /// <param name="info">Serialization info to store data</param>
        /// <param name="context">Streaming context for serialization</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Serialize basic properties
                info.AddValue(nameof(UnitName), UnitName);
                info.AddValue(nameof(UnitID), UnitID);
                info.AddValue(nameof(UnitType), UnitType);
                info.AddValue(nameof(Classification), Classification);
                info.AddValue(nameof(Role), Role);
                info.AddValue(nameof(Side), Side);
                info.AddValue(nameof(Nationality), Nationality);
                info.AddValue(nameof(IsTransportable), IsTransportable);
                info.AddValue(nameof(IsLandBase), IsLandBase);
                info.AddValue(nameof(IsLeaderAssigned), IsLeaderAssigned);
                info.AddValue(nameof(SpottedLevel), SpottedLevel);

                // Serialize profile references as IDs/names (not the objects themselves)
                info.AddValue("DeployedProfileID", DeployedProfile?.WeaponSystemID ?? "");
                info.AddValue("MountedProfileID", MountedProfile?.WeaponSystemID ?? "");
                info.AddValue("UnitProfileID", UnitProfile?.UnitProfileID ?? "");
                info.AddValue("LandBaseFacilityID", LandBaseFacility?.BaseID ?? "");
                info.AddValue("LeaderID", CommandingOfficer?.LeaderID ?? "");
                info.AddValue("ActiveProfileID", ActiveProfile?.WeaponSystemID ?? "");

                // Serialize owned StatsMaxCurrent objects as Max/Current pairs
                info.AddValue("HitPoints_Max", HitPoints.Max);
                info.AddValue("HitPoints_Current", HitPoints.Current);
                info.AddValue("DaysSupply_Max", DaysSupply.Max);
                info.AddValue("DaysSupply_Current", DaysSupply.Current);
                info.AddValue("MovementPoints_Max", MovementPoints.Max);
                info.AddValue("MovementPoints_Current", MovementPoints.Current);
                info.AddValue("MoveActions_Max", MoveActions.Max);
                info.AddValue("MoveActions_Current", MoveActions.Current);
                info.AddValue("CombatActions_Max", CombatActions.Max);
                info.AddValue("CombatActions_Current", CombatActions.Current);
                info.AddValue("DeploymentActions_Max", DeploymentActions.Max);
                info.AddValue("DeploymentActions_Current", DeploymentActions.Current);
                info.AddValue("OpportunityActions_Max", OpportunityActions.Max);
                info.AddValue("OpportunityActions_Current", OpportunityActions.Current);
                info.AddValue("IntelActions_Max", IntelActions.Max);
                info.AddValue("IntelActions_Current", IntelActions.Current);

                // Serialize simple properties
                info.AddValue(nameof(ExperiencePoints), ExperiencePoints);
                info.AddValue(nameof(ExperienceLevel), ExperienceLevel);
                info.AddValue(nameof(EfficiencyLevel), EfficiencyLevel);
                info.AddValue(nameof(IsMounted), IsMounted);
                info.AddValue(nameof(CombatState), CombatState);
                info.AddValue(nameof(MapPos), MapPos);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetObjectData), e);
                throw;
            }
        }

        /// <summary>
        /// Checks if there are unresolved references that need to be resolved.
        /// </summary>
        /// <returns>True if any resolution methods need to be called</returns>
        public bool HasUnresolvedReferences()
        {
            return !string.IsNullOrEmpty(unresolvedDeployedProfileID) ||
                !string.IsNullOrEmpty(unresolvedMountedProfileID) ||
                !string.IsNullOrEmpty(unresolvedUnitProfileID) ||
                !string.IsNullOrEmpty(unresolvedLandBaseProfileID) ||
                !string.IsNullOrEmpty(unresolvedLeaderID) ||
                !string.IsNullOrEmpty(unresolvedActiveProfileID);
        }

        #endregion // ISerializable Implementation


        #region IResolvableReferences

        /// <summary>
        /// Gets the list of unresolved reference IDs that need to be resolved.
        /// </summary>
        /// <returns>Collection of object IDs that this object references</returns>
        public IReadOnlyList<string> GetUnresolvedReferenceIDs()
        {
            var unresolvedIDs = new List<string>();

            if (!string.IsNullOrEmpty(unresolvedDeployedProfileID))
                unresolvedIDs.Add($"DeployedProfile:{unresolvedDeployedProfileID}");

            if (!string.IsNullOrEmpty(unresolvedMountedProfileID))
                unresolvedIDs.Add($"MountedProfile:{unresolvedMountedProfileID}");

            if (!string.IsNullOrEmpty(unresolvedUnitProfileID))
                unresolvedIDs.Add($"UnitProfile:{unresolvedUnitProfileID}");

            if (!string.IsNullOrEmpty(unresolvedLandBaseProfileID))
                unresolvedIDs.Add($"LandBase:{unresolvedLandBaseProfileID}");

            if (!string.IsNullOrEmpty(unresolvedLeaderID))
                unresolvedIDs.Add($"Leader:{unresolvedLeaderID}");

            if (!string.IsNullOrEmpty(unresolvedActiveProfileID))
                unresolvedIDs.Add($"ActiveProfile:{unresolvedActiveProfileID}");

            return unresolvedIDs.AsReadOnly();
        }

        /// <summary>
        /// Resolves object references using the provided data manager.
        /// Called after all objects have been deserialized.
        /// </summary>
        /// <param name="manager">Game data manager containing all loaded objects</param>
        public void ResolveReferences(GameDataManager manager)
        {
            try
            {
                bool activeProfileWasResolved = false;

                // Resolve WeaponSystemProfile references
                if (!string.IsNullOrEmpty(unresolvedDeployedProfileID))
                {
                    if (Enum.TryParse<WeaponSystems>(unresolvedDeployedProfileID, out WeaponSystems deployedWeapon))
                    {
                        var deployedProfile = manager.GetWeaponProfile(deployedWeapon, Nationality);
                        if (deployedProfile != null)
                        {
                            DeployedProfile = deployedProfile;
                            unresolvedDeployedProfileID = "";
                        }
                        else
                        {
                            AppService.HandleException(CLASS_NAME, "ResolveReferences",
                                new KeyNotFoundException($"Deployed profile {deployedWeapon}_{Nationality} not found"));
                        }
                    }
                    else
                    {
                        AppService.HandleException(CLASS_NAME, "ResolveReferences",
                            new ArgumentException($"Invalid weapon system ID: {unresolvedDeployedProfileID}"));
                    }
                }

                if (!string.IsNullOrEmpty(unresolvedMountedProfileID))
                {
                    if (Enum.TryParse<WeaponSystems>(unresolvedMountedProfileID, out WeaponSystems mountedWeapon))
                    {
                        var mountedProfile = manager.GetWeaponProfile(mountedWeapon, Nationality);
                        if (mountedProfile != null)
                        {
                            MountedProfile = mountedProfile;
                            unresolvedMountedProfileID = "";
                        }
                        else
                        {
                            AppService.HandleException(CLASS_NAME, "ResolveReferences",
                                new KeyNotFoundException($"Mounted profile {mountedWeapon}_{Nationality} not found"));
                        }
                    }
                    else
                    {
                        AppService.HandleException(CLASS_NAME, "ResolveReferences",
                            new ArgumentException($"Invalid weapon system ID: {unresolvedMountedProfileID}"));
                    }
                }

                // Resolve UnitProfile reference
                if (!string.IsNullOrEmpty(unresolvedUnitProfileID))
                {
                    var unitProfile = manager.GetUnitProfile(unresolvedUnitProfileID, Nationality);
                    if (unitProfile != null)
                    {
                        UnitProfile = unitProfile;
                        unresolvedUnitProfileID = "";
                    }
                    else
                    {
                        AppService.HandleException(CLASS_NAME, "ResolveReferences",
                            new KeyNotFoundException($"Unit profile {unresolvedUnitProfileID}_{Nationality} not found"));
                    }
                }

                // Resolve LandBaseFacility reference
                if (!string.IsNullOrEmpty(unresolvedLandBaseProfileID))
                {
                    var landBase = manager.GetLandBase(unresolvedLandBaseProfileID);
                    if (landBase != null)
                    {
                        LandBaseFacility = landBase;
                        unresolvedLandBaseProfileID = "";
                    }
                    else
                    {
                        AppService.HandleException(CLASS_NAME, "ResolveReferences",
                            new KeyNotFoundException($"Land base {unresolvedLandBaseProfileID} not found"));
                    }
                }

                // Resolve Leader reference
                if (!string.IsNullOrEmpty(unresolvedLeaderID))
                {
                    var leader = manager.GetLeader(unresolvedLeaderID);
                    if (leader != null)
                    {
                        CommandingOfficer = leader;
                        unresolvedLeaderID = "";

                        // ADD THIS CONSISTENCY CHECK:
                        // Ensure IsLeaderAssigned is consistent with resolved leader
                        if (!IsLeaderAssigned)
                        {
                            AppService.HandleException(CLASS_NAME, "ResolveReferences",
                                new InvalidDataException($"Unit {UnitID} has leader but IsLeaderAssigned is false"),
                                ExceptionSeverity.Minor);
                            IsLeaderAssigned = true; // Fix the inconsistency
                        }
                    }
                    else
                    {
                        AppService.HandleException(CLASS_NAME, "ResolveReferences",
                            new KeyNotFoundException($"Leader {unresolvedLeaderID} not found"));

                        // ADD THIS CLEANUP:
                        // If leader couldn't be resolved, ensure flag is cleared
                        IsLeaderAssigned = false;
                    }
                }
                else if (IsLeaderAssigned)
                {
                    // ADD THIS CONSISTENCY CHECK:
                    // Flag says we have a leader but no leader ID was saved
                    AppService.HandleException(CLASS_NAME, "ResolveReferences",
                        new InvalidDataException($"Unit {UnitID} has IsLeaderAssigned=true but no leader ID"),
                        ExceptionSeverity.Minor);
                    IsLeaderAssigned = false; // Fix the inconsistency
                }

                // Resolve ActiveProfile reference
                if (!string.IsNullOrEmpty(unresolvedActiveProfileID))
                {
                    if (Enum.TryParse<WeaponSystems>(unresolvedActiveProfileID, out WeaponSystems activeWeapon))
                    {
                        var activeProfile = manager.GetWeaponProfile(activeWeapon, Nationality);
                        if (activeProfile != null)
                        {
                            ActiveProfile = activeProfile;
                            unresolvedActiveProfileID = "";
                            activeProfileWasResolved = true; // Track successful resolution
                        }
                        else
                        {
                            AppService.HandleException(CLASS_NAME, "ResolveReferences",
                                new KeyNotFoundException($"Active profile {activeWeapon}_{Nationality} not found"));
                        }
                    }
                    else
                    {
                        AppService.HandleException(CLASS_NAME, "ResolveReferences",
                            new ArgumentException($"Invalid weapon system ID: {unresolvedActiveProfileID}"));
                    }
                }

                // Only recompute ActiveProfile if it wasn't explicitly resolved from save data
                // This preserves the exact saved state while handling cases where ActiveProfile wasn't saved
                // or when we have the core profiles needed to recompute it
                if (!activeProfileWasResolved &&
                    string.IsNullOrEmpty(unresolvedDeployedProfileID) &&
                    string.IsNullOrEmpty(unresolvedMountedProfileID))
                {
                    UpdateStateAndProfiles(CombatState);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ResolveReferences", e);
                throw;
            }
        }

        #endregion // IResolvableReferences


        #region Debugging

        /// <summary>
        /// Configures the unit as an airbase with default settings for debugging purposes.
        /// </summary>
        /// <remarks>This method initializes the unit with predefined values, including its
        /// identification,  metadata, profiles, and state. It sets the unit's type, classification, role, and other 
        /// properties to represent an airbase. This method is intended for debugging scenarios and  should not be used
        /// in production code.</remarks>
        public void DebugSetupAirbase()
        {
            // Set identification and metadata
            UnitName = "Airbase";
            UnitID = Guid.NewGuid().ToString();
            UnitType = UnitType.LandUnitDF;
            Classification = UnitClassification.AIRB;
            Role = UnitRole.GroundCombat;
            Side = Side.Player;
            Nationality = Nationality.USSR;
            IsTransportable = false;
            IsLandBase = true;
            IsLeaderAssigned = false;

            // Set profiles
            DeployedProfile = null;
            MountedProfile = null;
            UnitProfile = null;
            LandBaseFacility = new AirbaseFacility();
            ActiveProfile = null;

            // Initialize leader (will be null until assigned)
            CommandingOfficer = null;

            // Initialize action counts based on unit type and classification
            InitializeActionCounts();

            // Initialize state with default values
            ExperiencePoints = 0;
            ExperienceLevel = ExperienceLevel.Raw;
            EfficiencyLevel = EfficiencyLevel.FullyOperational;
            IsMounted = false;
            CombatState = CombatState.Deployed;
            SpottedLevel = SpottedLevel.Level1;

            // Initialize StatsMaxCurrent properties
            HitPoints = new StatsMaxCurrent(CUConstants.MAX_HP);
            DaysSupply = new StatsMaxCurrent(CUConstants.MaxDaysSupplyUnit);

            // Initialize movement based on unit classification
            InitializeMovementPoints();

            // Initialize position to origin (will be set when placed on map)
            MapPos = Vector2.zero;
        }

        /// <summary>
        /// Configures the unit as a supply depot with default settings for debugging purposes.
        /// </summary>
        /// <remarks>This method initializes the unit with predefined attributes, profiles, and state
        /// values  specific to a supply depot. It sets up metadata, classification, and operational parameters, 
        /// including supply capacity, combat state, and movement points. The unit is not assigned a  commanding officer
        /// or a specific position on the map until further configuration.</remarks>
        public void DebugSetupSupplyDepot()
        {
            // Set identification and metadata
            UnitName = "Supply Depot";
            UnitID = Guid.NewGuid().ToString();
            UnitType = UnitType.LandUnitDF;
            Classification = UnitClassification.DEPOT;
            Role = UnitRole.GroundCombat;
            Side = Side.Player;
            Nationality = Nationality.USSR;
            IsTransportable = false;
            IsLandBase = true;
            IsLeaderAssigned = false;

            // Set profiles
            DeployedProfile = null;
            MountedProfile = null;
            UnitProfile = null;
            LandBaseFacility = new SupplyDepotFacility("SupplyDepot",Side.Player, DepotSize.Huge, true);
            ActiveProfile = null;

            // Initialize leader (will be null until assigned)
            CommandingOfficer = null;

            // Initialize action counts based on unit type and classification
            InitializeActionCounts();

            // Initialize state with default values
            ExperiencePoints = 0;
            ExperienceLevel = ExperienceLevel.Raw;
            EfficiencyLevel = EfficiencyLevel.FullyOperational;
            IsMounted = false;
            CombatState = CombatState.Deployed;
            SpottedLevel = SpottedLevel.Level1;

            // Initialize StatsMaxCurrent properties
            HitPoints = new StatsMaxCurrent(CUConstants.MAX_HP);
            DaysSupply = new StatsMaxCurrent(CUConstants.MaxDaysSupplyUnit);

            // Initialize movement based on unit classification
            InitializeMovementPoints();

            // Initialize position to origin (will be set when placed on map)
            MapPos = Vector2.zero;
        }

            #endregion
        }
}