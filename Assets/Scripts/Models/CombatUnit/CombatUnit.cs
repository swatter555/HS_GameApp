﻿using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using HammerAndSickle.Controllers;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Extension helpers for UnitClassification.
    /// </summary>
    public static class UnitClassificationExtensions
    {
        /// <summary>
        /// Returns <c>true</c> if the classification represents a fixed facility (HQ, DEPOT, AIRB).
        /// </summary>
        public static bool IsBaseType(this UnitClassification classification) =>
            classification == UnitClassification.HQ ||
            classification == UnitClassification.DEPOT ||
            classification == UnitClassification.AIRB;
    }

    /*────────────────────────────────────────────────────────────────────────────
     CombatUnit ─ universal representation of every force element on the battlefield
     ────────────────────────────────────────────────────────────────────────────────

     Summary
     ═══════
     **CombatUnit** serves as the unified model for every tangible military asset in 
     Hammer & Sickle, from tank battalions and infantry squads to fighter wings, supply 
     depots, headquarters, and airbases. Each instance combines three data layers: 
     (1) **Identity** (immutable IDs, nationality, role, side), (2) **Templates** 
     (read-only WeaponSystemProfile & IntelProfile objects shared across units for 
     base capabilities), and (3) **Runtime state** (hit points, supplies, movement 
     points, five independent action pools, experience, efficiency, deployment state, 
     map position, and optional leader assignment).

     The core gameplay revolves around the **five-action framework** where Move, Combat, 
     Deployment, Opportunity, and Intel actions each consume specific tokens plus movement 
     points as a secondary cost, forcing players into constant tactical trade-offs and 
     preventing action-spamming exploits. Units progress through a sophisticated 
     deployment ladder (see detailed explanation below) with each transition costing 
     deployment actions and movement points while providing defensive bonuses or 
     mobility advantages.

     **Hybrid Profile-State Architecture**: Units reference shared WeaponSystemProfile 
     templates via enum IDs (DeployedProfileID, MountedProfileID, TransportProfileID) 
     for base capabilities, but maintain their own runtime state through StatsMaxCurrent 
     objects for hit points, supplies, movement points, and action pools. This 
     dramatically reduces memory usage for shared data while allowing individual unit 
     progression and state management.

     **Experience & Leadership Integration**: Units gain experience through combat and 
     movement under threat, progressing from Raw through Elite levels with corresponding 
     combat effectiveness multipliers. Leaders can be assigned to provide skill-based 
     bonuses, reputation management, and specialized capabilities that enhance unit 
     performance without breaking the action economy.

     **Facility Operations**: Base units (HQ, DEPOT, AIRB) extend the core framework 
     through a partial class system with specialized capabilities including supply 
     generation/projection, air unit attachment management, and operational capacity 
     scaling based on battle damage. The facility code is implemented in 
     CombatUnit.Facility.cs as a partial class extension.

     THE DEPLOYMENT LADDER SYSTEM
     ═══════════════════════════

     The deployment ladder represents a unit's tactical posture, balancing mobility 
     against defensive preparation. Units can only transition between adjacent states 
     on this ladder, with specific exceptions detailed below:

     **Ladder Structure (Mobility → Entrenchment):**

     InTransit ──→ Mobile ──→ Deployed ──→ HastyDefense ──→ Entrenched ──→ Fortified
        ↑            ↑           ↑             ↑              ↑            ↑
      Special      +2 MP      Standard      +10% def       +20% def    +30% def
     Transport    Mounted     Dismounted    Quick dig      Prepared    Maximum
      Profile     Profile      Profile        in          positions   fortification

     **State Descriptions:**

     • **InTransit**: Unit is aboard air or naval transport. Cannot engage in combat 
       but can move vast distances. Uses TransportProfile for capabilities. During
       InTranist movement, movement points are used from the TransportProfile. Once
       the unit attempt to deploy down one level, it must go directly to Deployed.
       Max movement points are set to the DeployedProfile maximum. Current movement 
       points are set to 1/2 deployed profile maximum.

     • **Mobile**: Unit is mounted on ground transport (APC/IFV) or has inherent high 
       mobility. Receives +2 movement point bonus and uses MountedProfile if available. 
       Suffers -10% combat effectiveness penalty due to reduced tactical positioning.

     • **Deployed**: Standard tactical posture. Unit is dismounted and ready for combat 
       with no modifiers. Uses DeployedProfile for all capabilities. This is the 
       baseline state for most tactical operations.

     • **HastyDefense**: Unit has quickly prepared defensive positions. +10% combat 
       effectiveness bonus. Represents immediate battlefield entrenchment without 
       extensive preparation.

     • **Entrenched**: Unit occupies prepared defensive positions with overhead cover 
       and improved fields of fire. +20% combat effectiveness bonus. Requires time 
       and engineering effort to achieve.

     • **Fortified**: Maximum defensive preparation with hardened positions, pre-sited 
       weapons, and extensive obstacle networks. +30% combat effectiveness bonus. 
       Represents the highest level of defensive preparation.

     **Transition Rules and Exceptions:**

     1. **Adjacent Ladder Movement**: Most transitions must be between adjacent states 
        (e.g., Deployed ↔ HastyDefense, Mobile ↔ Deployed).

     2. **InTransit Landing Exception**: Units leaving InTransit MUST go directly to 
        Deployed, bypassing Mobile. This represents the disorganization inherent in 
        air/naval landings. Movement points are set to 50% of DeployedProfile maximum.

     3. **Dis-entrenchment Exception**: Units in defensive states (HastyDefense, 
        Entrenched, Fortified) may jump directly to Deployed to rapidly abandon 
        positions when tactical situation changes.

     4. **Type Restrictions**: Fixed-wing aircraft and base facilities cannot change 
        states. Helicopters follow normal ladder rules.

     **Resource Costs for Transitions:**
     • 1 Deployment Action (from action pool)
     • Movement Points (50% of maximum per transition)
     • Supply Cost (0.25 days supply per transition)
     • Movement point availability gates action availability

     **Profile Switching During Transitions:**
     When changing states, units automatically switch between weapon profiles:
     • InTransit: Uses TransportProfile (helicopter/aircraft capabilities)
     • Mobile + Mounted: Uses MountedProfile (APC/IFV capabilities)
     • All Other States: Uses DeployedProfile (dismounted capabilities)

     **Special Entry Requirements:**
     • **InTransit Entry**: Unit must be transportable, have valid transport profile, 
       airborne units need airbase access, marine units need port access.
     • **Mobile Entry**: Unit becomes mounted if MountedProfile available, receives 
       +2 movement bonus, profile switches to mounted capabilities.

     Public Properties
     ═════════════════
     // Identity & Metadata
     public string UnitName { get; set; }
     public string UnitID { get; private set; }
     public UnitType UnitType { get; private set; }
     public UnitClassification Classification { get; private set; }
     public UnitRole Role { get; private set; }
     public Side Side { get; private set; }
     public Nationality Nationality { get; private set; }
     public bool IsTransportable { get; private set; }
     public bool IsMountable { get; private set; }
     public bool IsBase { get; }

     // Profile References (Shared Templates)
     public WeaponSystems TransportProfileID { get; private set; }
     public WeaponSystems DeployedProfileID { get; private set; }
     public WeaponSystems MountedProfileID { get; private set; }
     public IntelProfileTypes IntelProfileType { get; internal set; }

     // Action Economy (Individual Unit State)
     public StatsMaxCurrent MoveActions { get; private set; }
     public StatsMaxCurrent CombatActions { get; private set; }
     public StatsMaxCurrent DeploymentActions { get; private set; }
     public StatsMaxCurrent OpportunityActions { get; private set; }
     public StatsMaxCurrent IntelActions { get; private set; }

     // Combat State & Resources (Individual Unit State)
     public int ExperiencePoints { get; internal set; }
     public ExperienceLevel ExperienceLevel { get; internal set; }
     public EfficiencyLevel EfficiencyLevel { get; internal set; }
     public bool IsMounted { get; internal set; }
     public DeploymentState DeploymentState { get; internal set; }
     public StatsMaxCurrent HitPoints { get; private set; }
     public StatsMaxCurrent DaysSupply { get; private set; }
     public StatsMaxCurrent MovementPoints { get; private set; }
     public Coordinate2D MapPos { get; internal set; }
     public SpottedLevel SpottedLevel { get; private set; }

     // Leader Integration
     public string LeaderID { get; internal set; }
     public bool IsLeaderAssigned { get; }
     public Leader UnitLeader { get; }

     Constructor Signature
     ═════════════════════
     public CombatUnit(string unitName,
         UnitType unitType,
         UnitClassification classification,
         UnitRole role,
         Side side,
         Nationality nationality,
         IntelProfileTypes intelProfileType,
         WeaponSystems deployedProfileID,
         bool isMountable = false,
         WeaponSystems mountedProfileID = WeaponSystems.DEFAULT,
         bool isTransportable = false,
         WeaponSystems transportProfileID = WeaponSystems.DEFAULT,
         DepotCategory category = DepotCategory.Secondary,
         DepotSize size = DepotSize.Small)

     Includes extensive validation: profile existence checking, argument validation,
     enum value validation, and automatic facility initialization for base units.

     Public Methods
     ══════════════
     // Profile Access
     public WeaponSystemProfile GetDeployedProfile() - retrieves deployed combat profile
     public WeaponSystemProfile GetMountedProfile() - retrieves mounted transport profile  
     public WeaponSystemProfile GetTransportProfile() - retrieves air/sea transport profile
     public WeaponSystemProfile GetActiveWeaponSystemProfile() - current profile based on state
     public WeaponSystemProfile GetCurrentCombatStrength() - temporary profile with all modifiers

     // Intelligence & Spotting
     public IntelReport GenerateIntelReport(SpottedLevel spottedLevel = SpottedLevel.Level1) - fog-of-war intelligence data
     public void SetSpottedLevel(SpottedLevel spottedLevel) - update detection level

     // Turn Management
     public void RefreshAllActions() - reset all action pools to maximum
     public void RefreshMovementPoints() - reset movement points to maximum

     // Combat & Damage
     public void TakeDamage(float damage) - apply battle damage with clamping
     public void Repair(float repairAmount) - restore hit points with limits
     public bool IsDestroyed() - check if unit is eliminated (HP ≤ 1)

     // Supply Management  
     public bool ConsumeSupplies(float amount) - deduct supplies with validation
     public float ReceiveSupplies(float amount) - add supplies respecting capacity
     public float GetSupplyStatus() - current supply as percentage of maximum
     public bool CanMove() - movement legality based on HP, supplies, efficiency

     // Efficiency & Experience
     public void SetEfficiencyLevel(EfficiencyLevel level) - direct efficiency assignment
     public void DecreaseEfficiencyLevelBy1() - step down operational effectiveness  
     public void IncreaseEfficiencyLevelBy1() - step up operational effectiveness
     public bool AddExperience(int points) - award XP with level progression
     public int SetExperience(int points) - direct XP assignment with validation
     public int GetPointsToNextLevel() - XP needed for advancement
     public float GetExperienceProgress() - advancement progress as percentage

     // Leader Assignment
     public bool AssignLeader(string leaderID) - attach commanding officer
     public bool RemoveLeader() - detach commanding officer
     public Dictionary<SkillBonusType, float> GetLeaderBonuses() - all active skill bonuses
     public bool HasLeaderCapability(SkillBonusType bonusType) - specific capability check
     public float GetLeaderBonus(SkillBonusType bonusType) - specific bonus value
     public string GetLeaderName() - leader display name
     public CommandGrade GetLeaderGrade() - leader command level
     public int GetLeaderReputation() - leader reputation points
     public string GetLeaderRank() - formatted rank string
     public CommandAbility GetLeaderCommandAbility() - leader combat modifier
     public bool HasLeaderSkill(Enum skill) - specific skill unlock check
     public void AwardLeaderReputation(CUConstants.ReputationAction actionType, float contextMultiplier = 1.0f) - REP for actions
     public void AwardLeaderReputation(int amount) - direct REP award

     // Action Execution
     public bool DeployUpOneLevel() - move toward Mobile on deployment ladder
     public bool DeployDownOneLevel() - move toward Fortified on deployment ladder  
     public bool PerformCombatAction() - execute attack with resource consumption
     public bool PerformMoveAction(int movtCost) - execute movement with cost validation
     public bool PerformIntelAction() - execute reconnaissance with resource consumption
     public bool PerformOpportunityAction() - execute reactive defensive action
     public Dictionary<ActionTypes, float> GetAvailableActions() - validated action counts after MP gating
     public float GetDeployActions() - deployment actions available after MP validation
     public float GetCombatActions() - combat actions available after MP validation
     public float GetMoveActions() - move actions available after MP validation
     public float GetOpportunityActions() - opportunity actions (no MP validation)
     public float GetIntelActions() - intel actions available after MP validation

     // Position & Movement
     public void SetPosition(Coordinate2D newPos) - teleport unit to map position
     public bool CanMoveTo(Coordinate2D targetPos) - movement legality validation **TODO: Not implemented**
     public float GetDistanceTo(Coordinate2D targetPos) - distance calculation to position **TODO: Not implemented**
     public float GetDistanceTo(CombatUnit otherUnit) - distance calculation to unit **TODO: Not implemented**

     // Debugging Support  
     public void DebugSetCombatState(DeploymentState newState) - direct state override
     public void DebugSetMounted(bool isMounted) - direct mounting override
     public float DebugGetCombatMovementCost() - expose internal MP calculation

     // Validation & Persistence
     public List<string> ValidateInternalConsistency() - comprehensive error checking
     public object Clone() - deep copy for template spawning
     public void GetObjectData(SerializationInfo info, StreamingContext context) - ISerializable writer
     public bool HasUnresolvedReferences() - IResolvableReferences check
     public IReadOnlyList<string> GetUnresolvedReferenceIDs() - reference ID enumeration  
     public void ResolveReferences(GameDataManager manager) - second-phase reference resolution

     Private Methods
     ═══════════════
     // Initialization
     void InitializeActionCounts() - set action pool maxima based on classification
     void InitializeMovementPoints() - derive MPs from active weapon profile
     void InitializeFacility(DepotCategory category, DepotSize size) - base facility setup

     // Combat Calculations
     float GetFinalCombatRatingModifier() - multiplicative modifier for ground units
     float GetFinalCombatRatingModifier_Aircraft() - multiplicative modifier for air units  
     float GetStrengthModifier() - HP-based effectiveness multiplier
     float GetCombatStateModifier() - deployment state combat multiplier
     float GetEfficiencyModifier() - operational efficiency multiplier
     void UpdateMovementPointsForProfile() - sync MPs when profile changes

     // Action System Internals
     bool ConsumeMovementPoints(float points) - MP deduction with validation
     float GetDeployMovementCost() - MP cost for deployment transitions (50% of max MP)
     float GetCombatMovementCost() - MP cost for combat actions (25% of max MP)
     float GetIntelMovementCost() - MP cost for reconnaissance actions (15% of max MP)

     // Deployment State Machine
     bool TryDeployUpOneState(bool onAirbase = false, bool onPort = false) - ladder movement toward Mobile
     bool TryDeployDownOneState() - ladder movement toward Fortified
     bool CanUnitTypeChangeStates() - type-based state transition validation
     bool IsAdjacentStateTransition(DeploymentState currentState, DeploymentState targetState) - ladder adjacency check
     void UpdateMobilityState(DeploymentState newState, DeploymentState previousState) - mounting/profile transitions
     void ApplyMovementBonus(float delta) - mobile movement bonus application
     bool CanChangeToState(DeploymentState targetState) - comprehensive transition validation

     // Experience System Internals  
     ExperienceLevel CalculateExperienceLevel(int totalPoints) - XP to level conversion
     int GetMinPointsForLevel(ExperienceLevel level) - level threshold lookup
     ExperienceLevel GetNextLevel(ExperienceLevel currentLevel) - advancement progression
     void OnExperienceLevelChanged(ExperienceLevel previousLevel, ExperienceLevel newLevel) - level-up notification
     float GetExperienceMultiplier() - XP-based combat effectiveness multiplier

     Important Design Notes
     ══════════════════════
     • **Movement Point Gating System**: All major actions consume both action tokens 
       and movement points as a secondary cost (percentages of maximum MP). Actions 
       become unavailable when insufficient MPs remain, creating natural turn limitations 
       and preventing action-spamming exploits.

     • **Hybrid Profile-State System**: Base capabilities come from shared WeaponSystemProfile 
       templates (accessed via enum IDs) while individual progression and current state 
       are stored in StatsMaxCurrent objects. Always use GetActiveWeaponSystemProfile() 
       for current calculations as it handles mounting/deployment transitions automatically.

     • **Deployment State Ladder**: Units must transition through adjacent states with 
       specific exceptions (InTransit→Deployed bypass, defensive dis-entrenchment). 
       The UpdateMobilityState() method handles complex mounting logic, profile switching, 
       and movement point adjustments automatically.

     • **Leader Reference Pattern**: Leaders are referenced by string ID and resolved via 
       GameDataManager.GetLeader() lookup rather than direct object references, eliminating 
       circular serialization issues while maintaining clean API access through the UnitLeader property.

     • **Partial Class Architecture**: Base units automatically initialize facility-specific 
       state through the partial class system (CombatUnit.Facility.cs). The IsBase property 
       drives this behavior based on UnitClassification values.

     • **Serialization Contract**: Profile IDs serialize directly as enums (no reference 
       resolution needed) while complex object relationships use the IResolvableReferences 
       pattern. When adding fields, update GetObjectData(), deserialization constructor, 
       Clone(), and ValidateInternalConsistency().

     • **Experience & Efficiency Integration**: Both systems provide multiplicative combat 
       modifiers that stack with strength and deployment state bonuses. Use GetCurrentCombatStrength() 
       for final combat calculations rather than accessing profiles directly.

     • **Action Economy Balance**: The combination of action tokens + movement point costs + 
       supply consumption creates meaningful resource management decisions. Units must balance 
       immediate tactical needs against turn-long operational capacity.

     ────────────────────────────────────────────────────────────────────────────────
     KEEP THIS COMMENT BLOCK IN SYNC WITH CLASS ARCHITECTURE CHANGES!
     ────────────────────────────────────────────────────────────────────────────────*/
    [Serializable]
    public partial class CombatUnit : ICloneable, ISerializable, IResolvableReferences
    {
        #region Constants

        private const string CLASS_NAME = nameof(CombatUnit);

        #endregion


        #region Fields

        private bool _mobileBonusApplied = false; // Persisted runtime flag, true when MOBILE_MOVEMENT_BONUS active.
        private const string SERIAL_KEY_MOBILE_BONUS = "mobBonus"; // Serialization identifier for _mobileBonusApplied
        
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
        public bool IsTransportable{ get; private set; }       // Can use helicopter/airlift transport/naval.
        public bool IsMountable { get; private set; }          // Can mount onto APCs/IFVs.
        public bool IsBase => Classification.IsBaseType();

        // Profile IDs
        public WeaponSystems TransportProfileID { get; private set; }
        public WeaponSystems DeployedProfileID { get; private set; }
        public WeaponSystems MountedProfileID { get; private set; }
        public IntelProfileTypes IntelProfileType { get; internal set; }

        // Action counts using StatsMaxCurrent
        public StatsMaxCurrent MoveActions { get; private set; }
        public StatsMaxCurrent CombatActions { get; private set; }
        public StatsMaxCurrent DeploymentActions { get; private set; }
        public StatsMaxCurrent OpportunityActions { get; private set; }
        public StatsMaxCurrent IntelActions { get; private set; }

        // State data using StatsMaxCurrent where appropriate
        public int ExperiencePoints { get; internal set; }
        public ExperienceLevel ExperienceLevel { get; internal set; }
        public EfficiencyLevel EfficiencyLevel { get; internal set; }
        public bool IsMounted { get; internal set; }
        public DeploymentState DeploymentState { get; internal set; }
        public StatsMaxCurrent HitPoints { get; private set; }
        public StatsMaxCurrent DaysSupply { get; private set; }
        public StatsMaxCurrent MovementPoints { get; private set; }
        public Coordinate2D MapPos { get; internal set; }
        public SpottedLevel SpottedLevel { get; private set; }

        // The ID of unit's Leader, if assigned.
        public string LeaderID { get; internal set; }
        public bool IsLeaderAssigned => !string.IsNullOrEmpty(LeaderID);
        public Leader UnitLeader
        {
            get
            {
                try
                {
                    if (!IsLeaderAssigned)
                        throw new InvalidOperationException("No leader assigned to this unit.");

                    return GameDataManager.Instance.GetLeader(LeaderID);
                }
                catch (Exception e)
                {
                    AppService.HandleException(CLASS_NAME, "UnitLeader.get", e, ExceptionSeverity.Minor);
                    return null;
                }
            }
        }

        #endregion


        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public CombatUnit(string unitName,
            UnitType unitType,
            UnitClassification classification,
            UnitRole role,
            Side side,
            Nationality nationality,
            IntelProfileTypes intelProfileType,
            WeaponSystems deployedProfileID,
            bool isMountable = false,
            WeaponSystems mountedProfileID = WeaponSystems.DEFAULT,
            bool isTransportable = false,
            WeaponSystems transportProfileID = WeaponSystems.DEFAULT,
            DepotCategory category = DepotCategory.Secondary,
            DepotSize size = DepotSize.Small)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(unitName))
                    throw new ArgumentException("Unit name cannot be null or empty", nameof(unitName));

                // Deployed profile ID must point to a valid profile.
                if (deployedProfileID == WeaponSystems.DEFAULT)
                    throw new ArgumentException("Deployed profile ID cannot be DEFAULT", nameof(deployedProfileID));

                // Check that deployed profile exists in database.
                if (WeaponSystemsDatabase.GetWeaponSystemProfile(deployedProfileID) == null)
                    throw new ArgumentException($"Deployed profile ID {deployedProfileID} not found in database", nameof(deployedProfileID));

                // When not DEFAULT, mounted profile ID must point to a valid profile.
                if (isMountable && mountedProfileID != WeaponSystems.DEFAULT)
                {
                    if (WeaponSystemsDatabase.GetWeaponSystemProfile(mountedProfileID) == null)
                        throw new ArgumentException($"Mounted profile ID {mountedProfileID} not found in database", nameof(mountedProfileID));
                }

                // When not DEFAULT, transport profile ID must point to a valid profile.
                if (isTransportable && transportProfileID != WeaponSystems.DEFAULT)
                {
                    if (WeaponSystemsDatabase.GetWeaponSystemProfile(transportProfileID) == null)
                        throw new ArgumentException($"Transport profile ID {transportProfileID} not found in database", nameof(transportProfileID));
                }

                // Validate intel profile type
                if (!Enum.IsDefined(typeof(IntelProfileTypes), intelProfileType))
                    throw new ArgumentException("Invalid intel profile type", nameof(intelProfileType));

                // Set identification and metadata

                // Basic argument validation
                if (string.IsNullOrWhiteSpace(unitName))
                    throw new ArgumentException("Unit name cannot be null or whitespace", nameof(unitName));
                UnitName = unitName.Trim();
                UnitID = Guid.NewGuid().ToString();
                UnitType = unitType;
                Classification = classification;
                Role = role;
                Side = side;
                Nationality = nationality;
                IsTransportable = isTransportable;
                IsMountable = isMountable;

                // Set profile IDs
                TransportProfileID = transportProfileID;
                DeployedProfileID = deployedProfileID;
                MountedProfileID = mountedProfileID;
                IntelProfileType = intelProfileType;

                // Initialise facility data when appropriate
                if (IsBase)
                {
                    InitializeFacility(category, size);
                }

                // Initialize action counts based on unit type and classification
                InitializeActionCounts();

                // Initialize state with default values
                ExperiencePoints = 0;
                ExperienceLevel = ExperienceLevel.Raw;
                EfficiencyLevel = EfficiencyLevel.FullyOperational;
                IsMounted = false;
                DeploymentState = DeploymentState.Deployed;
                SpottedLevel = SpottedLevel.Level1;
                LeaderID = null;

                // Initialize StatsMaxCurrent properties
                HitPoints = new StatsMaxCurrent(CUConstants.MAX_HP);
                DaysSupply = new StatsMaxCurrent(CUConstants.MaxDaysSupplyUnit);

                // Initialize movement based on unit classification
                InitializeMovementPoints();

                // Initialize position to origin (will be set when placed on map)
                MapPos = Coordinate2D.Zero;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        #endregion


        #region Core

        /// <summary>
        /// Retrieves the weapon‑system profile the unit employs in its deployed state.
        /// </summary>
        /// <returns></returns>
        public WeaponSystemProfile GetDeployedProfile()
        {
            try
            {
                // "No profile" is a valid state.
                if (DeployedProfileID == WeaponSystems.DEFAULT)
                    return null;

                var profile = WeaponSystemsDatabase.GetWeaponSystemProfile(DeployedProfileID);
                if (profile == null)
                    throw new InvalidOperationException($"Deployed profile ID '{DeployedProfileID}' not found in weapon‑systems database.");

                return profile;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(GetDeployedProfile), ex);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the weapon‑system profile of the mounted transport (APC/IFV) the unit employs when mounted.
        /// </summary>
        /// <returns></returns>
        public WeaponSystemProfile GetMountedProfile()
        {
            try
            {
                // DEFAULT means the unit is foot‑mobile (no transport profile).
                if (MountedProfileID == WeaponSystems.DEFAULT)
                    return null;

                var profile = WeaponSystemsDatabase.GetWeaponSystemProfile(MountedProfileID);
                if (profile == null)
                    throw new InvalidOperationException($"Mounted profile ID '{MountedProfileID}' not found in weapon‑systems database.");

                return profile;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(GetMountedProfile), ex);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the weapon‑system profile the unit employs when in transport (airlift/sea).
        /// </summary>
        /// <returns></returns>
        public WeaponSystemProfile GetTransportProfile()
        {
            try
            {
                // DEFAULT means the unit has no transport profile.
                if (TransportProfileID == WeaponSystems.DEFAULT)
                    return null;

                var profile = WeaponSystemsDatabase.GetWeaponSystemProfile(TransportProfileID);
                if (profile == null)
                    throw new InvalidOperationException($"Transport profile ID '{TransportProfileID}' not found in weapon‑systems database.");
                
                return profile;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(GetTransportProfile), ex);
                return null;
            }
        }

        /// <summary>
        /// Generates an intelligence report for this unit based on the specified spotted level.
        /// Uses the static IntelProfile system to create fog-of-war filtered intelligence data.
        /// </summary>
        /// <param name="spottedLevel">Intelligence accuracy level (default Level1)</param>
        /// <returns>IntelReport with unit intelligence data, or null if not spotted</returns>
        public IntelReport GenerateIntelReport(SpottedLevel spottedLevel = SpottedLevel.Level1)
        {
            try
            {
                return IntelProfile.GenerateIntelReport(
                    IntelProfileType,
                    UnitName,
                    (int)HitPoints.Current,
                    Nationality,
                    DeploymentState,
                    ExperienceLevel,
                    EfficiencyLevel,
                    spottedLevel);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GenerateIntelReport", e);
                return null;
            }
        }

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
        /// Retrieves the active weapon system profile based on the current mounted state.
        /// </summary>
        /// <returns>The active <see cref="WeaponSystemProfile"/>. Returns the <see cref="MountedProfile"/> if the system is
        /// mounted; otherwise, returns the <see cref="DeployedProfile"/>.</returns>
        public WeaponSystemProfile GetActiveWeaponSystemProfile()
        {
            // Priority: InTransit > Mobile (if mounted) > Deployed
            return DeploymentState switch
            {
                DeploymentState.InTransit => GetTransportProfile() ?? GetDeployedProfile(),
                DeploymentState.Mobile when IsMounted => GetMountedProfile() ?? GetDeployedProfile(),
                _ => GetDeployedProfile()
            };
        }

        /// <summary>
        /// Get the adjusted combat strength based on current mounted state and all applicable modifiers.
        /// </summary>
        /// <returns>Temporary WeaponSystemProfile with modified combat values for immediate calculation</returns>
        public WeaponSystemProfile GetCurrentCombatStrength()
        {
            try
            {
                // Ensure we have a valid active weapon system profile
                var activeProfile = GetActiveWeaponSystemProfile();
                if (activeProfile == null)
                    throw new InvalidOperationException("Active weapon system profile is null");

                // Modify the constructor call to specify the correct parameters for WeaponSystemProfile
                var tempProfile = new WeaponSystemProfile(
                    activeProfile.Name + "_Combat",
                    activeProfile.Nationality,
                    WeaponSystems.COMBAT,
                    0
                );

                // Compute all modifiers that can affect a combat rating
                float finalModifier = GetFinalCombatRatingModifier();
                float finalModifier_Air = GetFinalCombatRatingModifier_Aircraft();

                // Copy and apply modifiers to combat ratings for ground units.
                tempProfile.SetHardAttack(Mathf.CeilToInt(activeProfile.HardAttack * finalModifier));
                tempProfile.SetHardDefense(Mathf.CeilToInt(activeProfile.HardDefense * finalModifier));
                tempProfile.SetSoftAttack(Mathf.CeilToInt(activeProfile.SoftAttack * finalModifier));
                tempProfile.SetSoftDefense(Mathf.CeilToInt(activeProfile.SoftDefense * finalModifier));
                tempProfile.SetGroundAirAttack(Mathf.CeilToInt(activeProfile.GroundAirAttack * finalModifier));
                tempProfile.SetGroundAirDefense(Mathf.CeilToInt(activeProfile.GroundAirDefense * finalModifier));

                // Copy and apply modifiers to combat ratings for air units.
                tempProfile.SetDogfighting(Mathf.CeilToInt(activeProfile.Dogfighting * finalModifier_Air));
                tempProfile.SetManeuverability(activeProfile.Maneuverability);
                tempProfile.SetTopSpeed(activeProfile.TopSpeed);
                tempProfile.SetSurvivability(activeProfile.Survivability);
                tempProfile.SetGroundAttack(activeProfile.GroundAttack);
                tempProfile.SetOrdinanceLoad(Mathf.CeilToInt(activeProfile.OrdinanceLoad * finalModifier_Air));
                tempProfile.SetStealth(activeProfile.Stealth);

                return tempProfile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetCurrentCombatStrength", e);
                return null;
            }
        }

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
                float newHitPoints = Mathf.Max(CUConstants.MIN_HP, HitPoints.Current - damage);
                HitPoints.SetCurrent(newHitPoints);
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
            return HitPoints.Current <= 1f;
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
                AppService.HandleException(CLASS_NAME, "CanMove", e);
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

        /// <summary>
        /// Sets the efficiency level for the application.
        /// </summary>
        /// <remarks>This method updates the application's efficiency level to the specified value. 
        /// Ensure that the provided <paramref name="level"/> is a valid enumeration value to avoid
        /// exceptions.</remarks>
        /// <param name="level">The desired efficiency level to set. Must be a valid value of the <see cref="EfficiencyLevel"/> enumeration.</param>
        public void SetEfficiencyLevel(EfficiencyLevel level)
        {
            try
            {
                // Validate the new level
                if (!Enum.IsDefined(typeof(EfficiencyLevel), level))
                {
                    throw new ArgumentOutOfRangeException(nameof(level), "Invalid efficiency level");
                }
                // Set the new efficiency level
                EfficiencyLevel = level;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetEfficiencyLevel", e);
            }
        }

        /// <summary>
        /// Decreases the efficiency level by 1, if possible.
        /// </summary>
        public void DecreaseEfficiencyLevelBy1()
        {
            try
            {
                if (EfficiencyLevel > EfficiencyLevel.StaticOperations)
                    EfficiencyLevel--;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DecreaseEfficiencyLevel", e);
            }
        }

        /// <summary>
        /// Increases the efficiency level by 1, if possible.
        /// </summary>
        public void IncreaseEfficiencyLevelBy1()
        {
            try
            {
                if (EfficiencyLevel < EfficiencyLevel.PeakOperational)
                    EfficiencyLevel++;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "IncreaseEfficiencyLevel", e);
            }
        }

        #endregion // Core


        #region Experience System

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
                    return false;

                // Validate points do not exceed maximum gain per action.
                if (points > CUConstants.MAX_EXP_GAIN_PER_ACTION)
                {
                    points = CUConstants.MAX_EXP_GAIN_PER_ACTION;
                }
                    
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
        /// Safely sets the unit’s cumulative experience points (XP) and keeps
        /// <see cref="ExperienceLevel"/> tightly synchronized.
        /// </summary>
        /// <param name="points">The new total XP to apply.  Values outside
        /// <c>0 … CUConstants.EXPERIENCE_MAX</c> are automatically clamped.</param>
        /// <returns>The clamped XP value that was actually stored.</returns>
        /// <remarks>
        /// <para>
        /// Both <see cref="ExperiencePoints"/> and the derived
        /// <see cref="ExperienceLevel"/> are updated from the same clamped value,
        /// eliminating drift.  The method never throws for invalid input; any
        /// unexpected runtime errors are logged and the prior XP value is
        /// preserved.
        /// </para>
        /// </remarks>
        public int SetExperience(int points)
        {
            try
            {
                // 1) Constrain to legal range.
                int clamped = Math.Clamp(points, 0, (int)ExperiencePointLevels.Elite);

                // 2) Skip work if nothing changed – avoids redundant UI refresh.
                if (clamped == ExperiencePoints)
                    return clamped;

                // 3) Persist XP and recompute level from the same source value.
                ExperiencePoints = clamped;
                ExperienceLevel = CalculateExperienceLevel(clamped);

                return clamped;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(SetExperience), ex);
                return ExperiencePoints; // return state that survived the error
            }
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

        #endregion // Experience System


        #region Leader System

        /// <summary>
        /// Assigns a leader to this unit by their ID, removing any existing leader.
        /// </summary>
        /// <param name="leaderID">ID of the new leader</param>
        /// <returns>success/failure</returns>
        public bool AssignLeader(string leaderID)
        {
            try
            {
                // Validate the incoming LeaderID.
                Leader newLeader = GameDataManager.Instance.GetLeader(leaderID);
                if (newLeader == null)
                    throw new ArgumentException($"Leader with ID {leaderID} does not exist in the game data.", nameof(leaderID));

                // Check if the new leader is already assigned to another unit.
                if (newLeader.IsAssigned)
                    throw new InvalidOperationException($"Leader {newLeader.Name} is already assigned to another unit.");

                // If there is already a leader assigned, we must remove them first.
                if (IsLeaderAssigned)
                {
                    // Make sure current leader is valid before proceeding.
                    if (UnitLeader == null)
                        throw new InvalidOperationException("Current leader is null, cannot unassign.");

                    // Capture UI message about the leader being unassigned.
                    AppService.CaptureUiMessage($"{UnitLeader.FormattedRank} {UnitLeader.Name} has been unassigned from {UnitName}.");

                    // Reach in and let the Leader know it isn't assigned to this unit anymore.
                    UnitLeader.UnassignFromUnit();
                }

                // Assign the new leader ID.
                LeaderID = leaderID;

                // Now reach into new leader and assign him from there.
                newLeader.AssignToUnit(UnitID);

                // Capture UI message about the new leader being assigned.
                AppService.CaptureUiMessage($"{newLeader.FormattedRank} {newLeader.Name} has been assigned to command {UnitName}.");

                return true;
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
        /// Removes the current leader from this unit, if one is assigned.
        /// </summary>
        /// <returns>Success/Failure</returns>
        public bool RemoveLeader()
        {
            try
            {
                // Check if there is actually a leader to remove.
                if (!IsLeaderAssigned)
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have a commanding officer to remove.");
                    return false;
                }

                // Capture UI message about the leader being removed.
                AppService.CaptureUiMessage($"{UnitLeader.FormattedRank} {UnitLeader.Name} has been relieved of command of {UnitName} and is now available for reassignment.");

                // Reach in and let the Leader know it isn't assigned to this unit anymore.
                UnitLeader.UnassignFromUnit();

                // Clear our reference to the leader.
                LeaderID = null;

                return true;
            }
            catch (Exception e)
            {
                // Handle any unexpected errors
                AppService.HandleException(CLASS_NAME, "RemoveLeader", e);
                AppService.CaptureUiMessage("Leader removal failed due to an unexpected error.");
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
                // Check if there is a leader assigned
                if (!IsLeaderAssigned)
                    return bonuses;

                // Iterate through all skill bonus types and get non-zero values
                foreach (SkillBonusType bonusType in (SkillBonusType[])Enum.GetValues(typeof(SkillBonusType)))
                {
                    if (bonusType == SkillBonusType.None) continue;

                    float bonusValue = UnitLeader.GetBonusValue(bonusType);
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
            UnitLeader != null && UnitLeader.HasCapability(bonusType);

        /// <summary>
        /// Gets a specific leader bonus value.
        /// Returns 0 if no leader assigned or bonus not present.
        /// </summary>
        /// <param name="bonusType">The type of bonus to retrieve</param>
        /// <returns>The bonus value, or 0 if not present</returns>
        public float GetLeaderBonus(SkillBonusType bonusType) =>
            UnitLeader != null && bonusType != SkillBonusType.None
            ? UnitLeader.GetBonusValue(bonusType)
            : 0f;

        /// <summary>
        /// Gets the leader's name for display purposes.
        /// Returns empty string if no leader assigned.
        /// </summary>
        /// <returns>Leader name or empty string</returns>
        public string GetLeaderName() => UnitLeader?.Name ?? string.Empty;

        /// <summary>
        /// Gets the leader's command grade for display and bonus calculations.
        /// Returns JuniorGrade if no leader assigned.
        /// </summary>
        /// <returns>Leader's command grade</returns>
        public CommandGrade GetLeaderGrade() => UnitLeader?.CommandGrade ?? CommandGrade.JuniorGrade;

        /// <summary>
        /// Gets the leader's reputation points for display purposes.
        /// Returns 0 if no leader assigned.
        /// </summary>
        /// <returns>Leader's reputation points</returns>
        public int GetLeaderReputation() => UnitLeader?.ReputationPoints ?? 0;

        /// <summary>
        /// Gets the leader's formatted rank based on nationality.
        /// Returns empty string if no leader assigned.
        /// </summary>
        /// <returns>Formatted rank string</returns>
        public string GetLeaderRank() => UnitLeader?.FormattedRank ?? "";

        /// <summary>
        /// Gets the leader's combat command ability modifier.
        /// Returns Average if no leader assigned.
        /// </summary>
        /// <returns>Leader's combat command ability</returns>
        public CommandAbility GetLeaderCommandAbility() =>
            UnitLeader?.CombatCommand ?? CommandAbility.Average;

        /// <summary>
        /// Checks if the leader has unlocked a specific skill.
        /// Returns false if no leader assigned.
        /// </summary>
        /// <param name="skillEnum">The skill to check</param>
        /// <returns>True if the skill is unlocked</returns>
        public bool HasLeaderSkill(Enum skill) =>
            UnitLeader != null && UnitLeader.IsSkillUnlocked(skill);

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
                if (UnitLeader == null)
                {
                    return;
                }

                UnitLeader.AwardReputationForAction(actionType, contextMultiplier);
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
                if (UnitLeader == null || amount <= 0)
                {
                    return;
                }

                UnitLeader.AwardReputation(amount);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AwardLeaderReputation", e);
            }
        }

        #endregion // Leader System


        #region CombatUnit Actions

        /// <summary>
        /// Attempts to deploy the combat unit to a higher deployment level.
        /// </summary>
        /// <remarks>This method checks whether the combat unit has sufficient deployment actions,
        /// movement points, and supplies to perform the operation. If the deployment is successful, the required
        /// resources are consumed. If the deployment fails, an appropriate message is captured for the user
        /// interface.</remarks>
        /// <returns><see langword="true"/> if the deployment to a higher level is successful; otherwise, <see
        /// langword="false"/>.</returns>
        public bool DeployUpOneLevel()
        {
            try
            {
                // Check actions and movement points before deploying.
                if (GetDeployActions() >= 1 && MovementPoints.Current >= GetDeployMovementCost())
                {
                    // Atempt to deploy up one level.
                    bool result = TryDeployUpOneState();

                    // If we have made it this far, we can consume supplies and actions.
                    if (result)
                    {
                        ConsumeSupplies(CUConstants.COMBAT_STATE_SUPPLY_TRANSITION_COST);
                        DeploymentActions.DecrementCurrent();
                        return true;
                    }
                    else
                    {
                        // Deployment failed, notify the user.
                        AppService.CaptureUiMessage($"{UnitName} cannot deploy up one level due to insufficient conditions.");
                        return false; // Deployment failed
                    }
                }
                // Not enough actions or movement points to deploy
                AppService.CaptureUiMessage($"{UnitName} does not have enough deployment actions or movement points to deploy up one level.");
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DeployUpOneLevel", e);
                return false;
            }
        }

        /// <summary>
        /// Attempts to deploy the combat unit to a lower deployment level.
        /// </summary>
        /// <remarks>This method checks whether the combat unit has sufficient deployment actions,
        /// movement points, and supplies to perform the operation. If the deployment is successful, the required
        /// resources are consumed. If the deployment fails, an appropriate message is captured for the user
        /// interface.</remarks>
        /// <returns><see langword="true"/> if the deployment to a lower level is successful; otherwise, <see
        /// langword="false"/>.</returns>
        public bool DeployDownOneLevel()
        {
            try
            {
                // Check actions and movement points before deploying.
                if (GetDeployActions() >= 1 && MovementPoints.Current >= GetDeployMovementCost())
                {
                    // Atempt to deploy down one level.
                    bool result = TryDeployDownOneState();

                    // If we have made it this far, we can consume supplies and actions.
                    if (result)
                    {
                        ConsumeSupplies(CUConstants.COMBAT_STATE_SUPPLY_TRANSITION_COST);
                        DeploymentActions.DecrementCurrent();
                        return true;
                    }
                    else
                    {
                        // Deployment failed, notify the user.
                        AppService.CaptureUiMessage($"{UnitName} cannot deploy down one level due to insufficient conditions.");
                        return false; // Deployment failed
                    }
                }
                // Not enough actions or movement points to deploy
                AppService.CaptureUiMessage($"{UnitName} does not have enough deployment actions or movement points to deploy down one level.");
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DeployDownOneLevel", e);
                return false;
            }
        }

        /// <summary>
        /// Consume actions, movement points, and supplies to perform a combat action.
        /// </summary>
        /// <returns>Success/Failure</returns>
        public bool PerformCombatAction()
        {
            try
            {
                // Make sure the unit has enough combat actions, movement points, and supplies to perform a combat action.
                if (CombatActions.Current >= 1 && 
                    MovementPoints.Current >= GetCombatMovementCost() &&
                    DaysSupply.Current >= CUConstants.COMBAT_ACTION_SUPPLY_THRESHOLD &&
                    !IsBase)
                {
                    // Deduct one combat action
                    CombatActions.DecrementCurrent();

                    // Consume movement points
                    ConsumeMovementPoints(GetCombatMovementCost());

                    // Consume supplies for the combat action
                    ConsumeSupplies(CUConstants.COMBAT_ACTION_SUPPLY_COST);

                    return true; // Combat action performed successfully
                }
                else
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have enough combat actions, movement points, or supplies to perform a combat action.");
                    return false; // Not enough resources to perform combat action
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "PerformCombatAction", e);
                return false;
            }
        }

        /// <summary>
        /// Consume the required actions, movement points, and supplies to perform a move action.
        /// </summary>
        /// <param name="movtCost"></param>
        /// <returns>Success/Failure</returns>
        public bool PerformMoveAction(int movtCost)
        {
            try
            {
                // Make sure the unit has enough move actions, movement points, and supplies to perform a move action.
                if (MoveActions.Current >= 1 &&
                    MovementPoints.Current >= movtCost &&
                    DaysSupply.Current >= (movtCost * CUConstants.MOVE_ACTION_SUPPLY_COST) + CUConstants.MOVE_ACTION_SUPPLY_THRESHOLD &&
                    !IsBase)
                {
                    // Decrement move actions
                    MoveActions.DecrementCurrent();

                    // Consume movement points
                    ConsumeMovementPoints(movtCost);

                    // Consume supplies for the move action
                    ConsumeSupplies(movtCost * CUConstants.MOVE_ACTION_SUPPLY_COST);

                    return true; // Move action performed successfully
                }
                else
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have enough move actions, movement points, or supplies to perform a move action.");
                    return false; // Not enough resources to perform move action
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "PerformMoveAction", e);
                return false;
            }
        }
       
       /// <summary>
       /// Attempts to perform an intel action for the unit, consuming the required resources.
       /// </summary>
       /// <remarks>This method checks whether the unit has sufficient intel actions, movement points, and
       /// supplies to perform an intel action. If the required resources are available, they are consumed accordingly,
       /// and the action is performed. If the resources are insufficient, the method logs a message and returns <see
       /// langword="false"/>.</remarks>
       /// <returns><see langword="true"/> if the intel action is successfully performed; otherwise, <see langword="false"/>.</returns>
       public bool PerformIntelAction()
        {
            try
            {
                // Make sure the unit has enough intel actions, movement points, and supplies to perform an intel action.
                if (IntelActions.Current >= 1 &&
                    MovementPoints.Current >= GetIntelMovementCost() &&
                    DaysSupply.Current >= CUConstants.INTEL_ACTION_SUPPLY_COST)
                {
                    // Decrement intel actions
                    IntelActions.DecrementCurrent();

                    // Consume movement points
                    ConsumeMovementPoints(GetIntelMovementCost());

                    // Consume supplies for the intel action
                    ConsumeSupplies(CUConstants.INTEL_ACTION_SUPPLY_COST);

                    return true; // Intel action performed successfully
                }
                else
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have enough intel actions, movement points, or supplies to perform an intel action.");
                    return false; // Not enough resources to perform intel action
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "PerformIntelAction", e);
                return false;
            }
        }

       /// <summary>
       /// Attempts to perform an opportunity action for the unit.
       /// </summary>
       /// <remarks>An opportunity action can only be performed if the unit has sufficient opportunity
       /// actions, adequate supply levels, and is not designated as a base. If the action is successfully performed,
       /// the unit's opportunity actions are decremented, and the required supplies are consumed.</remarks>
       /// <returns><see langword="true"/> if the opportunity action was successfully performed; otherwise, <see
       /// langword="false"/>.</returns>
       public bool PerformOpportunityAction()
       {
            try
            {
                // Make sure the unit has enough opportunity actions and is not a base.
                if (OpportunityActions.Current >= 1 &&
                    DaysSupply.Current >= CUConstants.OPPORTUNITY_ACTION_SUPPLY_COST + CUConstants.OPPORTUNITY_ACTION_SUPPLY_THRESHOLD && 
                    !IsBase)
                {
                    // Decrement opportunity actions
                    OpportunityActions.DecrementCurrent();

                    // Consume supplies for the opportunity action
                    ConsumeSupplies(CUConstants.OPPORTUNITY_ACTION_SUPPLY_COST);

                    return true; // Opportunity action performed successfully
                }
                else
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have enough opportunity actions to perform an opportunity action.");
                    return false; // Not enough resources to perform opportunity action
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "PerformOpportunityAction", e);
                return false;
            }
       }

        /// <summary>
        /// Returns a dictionary mapping each action type to the number of **truly** available tokens
        /// after validating both action counters and movement‑point prerequisites.
        /// </summary>
        public Dictionary<ActionTypes, float> GetAvailableActions()
        {
            // Move – must have a token and at least 1 movement point remaining.
            float moveAvailable = (MoveActions.Current >= 1 && MovementPoints.Current > 0f)
                ? MoveActions.Current : 0f;

            // Combat – existing validation already checks movement cost.
            float combatAvailable = CombatActions.Current >= 1 ? CombatActions.Current : 0f;

            // Opportunity – purely reactive, no validation.
            float opportunityAvailable = OpportunityActions.Current;

            // Intel – existing validation already handles base / movement logic.
            float intelAvailable = IntelActions.Current >= 1 ? IntelActions.Current : 0f;

            // Deployment – not available for bases, needs movement points.
            float deploymentAvailable = 0f;
            if (IsBase)
            {
                deploymentAvailable = 0f;
            }
            else
            {
                if (MovementPoints.Current >= GetDeployMovementCost() && DeploymentActions.Current >= 1)
                    deploymentAvailable = DeploymentActions.Current;
            }

            return new Dictionary<ActionTypes, float>
            {
                [ActionTypes.MoveAction] = moveAvailable,
                [ActionTypes.CombatAction] = combatAvailable,
                [ActionTypes.DeployAction] = deploymentAvailable,
                [ActionTypes.OpportunityAction] = opportunityAvailable,
                [ActionTypes.IntelAction] = intelAvailable
            };
        }

        /// <summary>
        /// Determines the number of deployment actions available for the unit.
        /// </summary>
        /// <remarks>This method checks whether the unit has sufficient movement points to perform a
        /// deployment. If the movement points are greater than or equal to the deployment movement cost, the current
        /// deployment actions are returned; otherwise, the method returns 0.</remarks>
        /// <returns>The number of deployment actions available. Returns <see langword="0"/> if the unit does not have enough
        /// movement points to deploy.</returns>
        public float GetDeployActions()
        {
            // If the unit is a base, it cannot deploy.
            if (!CanUnitTypeChangeStates())
                return 0;

            // Check if the unit can deploy based on movement points
            if (MovementPoints.Current >= GetDeployMovementCost())
                return DeploymentActions.Current;
            else return 0f;
        }

        /// <summary>
        /// Determines the number of combat actions available for the unit based on its current movement points.
        /// </summary>
        /// <returns>The number of combat actions</returns>
        public float GetCombatActions()
        {
            // If the unit is a base, it cannot perform combat actions.
            if (IsBase)
                return 0;

            // Check if the unit can perform combat actions based on movement points
            if (MovementPoints.Current >= GetCombatMovementCost())
                return CombatActions.Current;
            else return 0;
        }

        /// <summary>
        /// Determines the number of move actions available for the unit.
        /// </summary>
        /// <remarks>A unit's ability to perform move actions depends on its current movement points. If
        /// the unit is a base,  it cannot move under any circumstances.</remarks>
        /// <returns>The number of move actions the unit can perform. Returns <see langword="0"/> if the unit is a base  or if
        /// the unit has no remaining movement points.</returns>
        public float GetMoveActions()
        {
            // If the unit is a base, it cannot move.
            if (IsBase)
                return 0;

            // Check if the unit can perform move actions based on movement points
            if (MovementPoints.Current > 0)
                return MoveActions.Current;
            else
                return 0;
        }

        /// <summary>
        /// Retrieves the current number of opportunity actions available for the unit.
        /// </summary>
        /// <remarks>Opportunity actions represent actions that the unit can perform in specific
        /// situations. If the unit is a base, it cannot perform opportunity actions, and the method returns
        /// 0.</remarks>
        /// <returns>The current number of opportunity actions available for the unit. Returns 0 if the unit is a base.</returns>
        public float GetOpportunityActions()
        {
            // If the unit is a base, it cannot perform opportunity actions.
            if (IsBase)
                return 0;

            return OpportunityActions.Current;
        }

        /// <summary>
        /// Retrieves the number of available intelligence actions for the unit.
        /// </summary>
        /// <remarks>The number of intelligence actions returned depends on whether the unit has
        /// sufficient movement points  to perform an intelligence action. If the unit's current movement points are
        /// less than the required  movement cost, the method returns 0.</remarks>
        /// <returns>The current number of intelligence actions available if the unit has sufficient movement points; otherwise,
        /// 0.</returns>
        public float GetIntelActions()
        {
            // Check if the unit has enough movement points to perform an intel action.
            if (MovementPoints.Current >= GetIntelMovementCost())
                return IntelActions.Current;
            else
                return 0; // Not enough movement points for intel action
        }

        #endregion // CombatUnit Actions


        #region Position and Movement

        /// <summary>
        /// Sets the unit's position on the map.
        /// </summary>
        /// <param name="newPos">The new position coordinates</param>
        public void SetPosition(Coordinate2D newPos)
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
        public bool CanMoveTo(Coordinate2D targetPos)
        {
            try
            {
                throw new NotImplementedException("Movement validation logic not implemented yet.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CanMoveTo", e);
                return false;
            }
        }

        /// <summary>
        /// Gets the distance between this unit and a target position in Unity units.
        /// </summary>
        /// <param name="targetPos">The target position</param>
        /// <returns>Distance in Unity units</returns>
        public float GetDistanceTo(Coordinate2D targetPos)
        {
            try
            {
                throw new NotImplementedException("Distance calculation logic not implemented yet.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetDistanceTo", e);
                return 0f;
            }
        }

        /// <summary>
        /// Gets the distance between this unit and another unit.
        /// </summary>
        /// <param name="otherUnit">The other unit</param>
        /// <returns>Distance in Unity units</returns>
        public float GetDistanceTo(CombatUnit otherUnit)
        {
            try
            {
                throw new NotImplementedException("Distance calculation logic not implemented yet.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetDistanceTo", e);
                return 0f;
            }
        }

        #endregion // Position and Movement


        #region Core Helpers

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
                    deploymentActions = 0;
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
            try
            {
                // Start with deployed profile movement points
                var deployedProfile = GetDeployedProfile();
                if (deployedProfile == null)
                    throw new InvalidOperationException("Unit must have a valid deployed profile");

                MovementPoints = new StatsMaxCurrent(deployedProfile.MovementPoints);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "InitializeMovementPoints", e);
                // Fallback to foot movement if profile access fails
                MovementPoints = new StatsMaxCurrent(CUConstants.FOOT_UNIT);
            }
        }

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
        /// Calculates the final combat rating modifier specifically for aircraft units.
        /// </summary>
        /// <returns>Multiplier</returns>
        private float GetFinalCombatRatingModifier_Aircraft()
        {
            try
            {
                // Calculate the final combat rating modifier based on all factors.
                float strengthModifier = GetStrengthModifier();
                float efficiencyModifier = GetEfficiencyModifier();
                float experienceModifier = GetExperienceMultiplier();
                // Combine all modifiers
                return strengthModifier * efficiencyModifier * experienceModifier;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetFinalCombatRatingModifier_Aircraft", e);
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
            return DeploymentState switch
            {
                DeploymentState.Mobile => CUConstants.COMBAT_MOD_MOBILE,
                DeploymentState.Deployed => CUConstants.COMBAT_MOD_DEPLOYED,
                DeploymentState.HastyDefense => CUConstants.COMBAT_MOD_HASTY_DEFENSE,
                DeploymentState.Entrenched => CUConstants.COMBAT_MOD_ENTRENCHED,
                DeploymentState.Fortified => CUConstants.COMBAT_MOD_FORTIFIED,
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
        /// Updates the movement points for the active weapon system profile.
        /// </summary>
        /// <remarks>This method retrieves the active weapon system profile and updates the maximum
        /// movement points based on the profile's settings. The current movement points are adjusted proportionally to
        /// maintain the same percentage of the new maximum. If no active profile is available, an <see
        /// cref="InvalidOperationException"/> is thrown.</remarks>
        private void UpdateMovementPointsForProfile()
        {
            try
            {
                var activeProfile = GetActiveWeaponSystemProfile();
                if (activeProfile == null)
                    throw new InvalidOperationException("No active weapon system profile available");

                int newMaxMovement = activeProfile.MovementPoints;

                // Calculate current movement as percentage of old max
                float movementPercentage = MovementPoints.Max > 0
                    ? MovementPoints.Current / MovementPoints.Max
                    : 0f;

                // Set new max and scale current proportionally
                MovementPoints.SetMax(newMaxMovement);
                float newCurrent = newMaxMovement * movementPercentage;
                MovementPoints.SetCurrent(newCurrent);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpdateMovementPointsForProfile", e);
            }
        }

        #endregion // Core Helpers


        #region CombatUnit Actions Helpers

        /// <summary>
        /// Consumes movement points if available.
        /// </summary>
        /// <param name="points">Number of movement points to consume</param>
        /// <returns>True if movement points were consumed, false if insufficient</returns>
        private bool ConsumeMovementPoints(float points)
        {
            try
            {
                if (points <= 0f)
                {
                    return true; // No points to consume
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
        /// Calculates the movement-point (MP) cost for this unit to perform a <b>Deploy</b> action.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <see cref="MovementPoints.Max"/> is <c>0</c> the method returns <c>0</c>.  
        /// This indicates the unit is <em>immobile</em>, <u>not</u> that the action is free.  
        /// Callers must still verify that <see cref="MovementPoints.Remaining"/> is at least
        /// the value returned before enabling the action.
        /// </para>
        /// </remarks>
        /// <returns>The whole-number MP cost for deploying.  Always non-negative.</returns>
        private float GetDeployMovementCost()
        {
            return Mathf.CeilToInt(MovementPoints.Max * CUConstants.DEPLOYMENT_ACTION_MOVEMENT_COST);
        }

        /// <summary>
        /// Calculates the MP cost required to execute a <b>Combat</b> (attack) action.
        /// </summary>
        /// <remarks>
        /// Returns <c>0</c> when the unit’s maximum MPs are zero, signalling that the unit
        /// cannot pay any MP cost at all.  Client code should still check
        /// <see cref="MovementPoints.Remaining"/> ≥ returned value before allowing the attack.
        /// </remarks>
        /// <returns>The MP cost as an integer.</returns>
        private float GetCombatMovementCost()
        {
            return Mathf.CeilToInt(MovementPoints.Max * CUConstants.COMBAT_ACTION_MOVEMENT_COST);
        }

        /// <summary>
        /// Calculates the MP cost for an <b>Intel</b> (recon/spotting) action.
        /// </summary>
        /// <remarks>
        /// A result of <c>0</c> means the unit is immobile (<see cref="MovementPoints.Max"/> == 0);
        /// the action is <em>not</em> considered gratis.  
        /// UI or AI callers must independently confirm there are enough MPs remaining.
        /// </remarks>
        /// <returns>Integer MP cost for the intel action.</returns>
        private float GetIntelMovementCost()
        {
            return Mathf.CeilToInt(MovementPoints.Max * CUConstants.INTEL_ACTION_MOVEMENT_COST);
        }

        /// <summary>
        /// Attempts to move the unit up one step on the posture ladder.
        /// </summary>
        /// <param name="onAirfieldPort">Is unit on an airfield or port</param>
        /// <returns></returns>
        private bool TryDeployUpOneState(bool onAirbase = false, bool onPort = false)
        {
            try
            {
                DeploymentState currentState = DeploymentState;

                // Check if already at maximum mobility (InTransit)
                if (currentState == DeploymentState.InTransit)
                {
                    AppService.CaptureUiMessage($"{UnitName} is already in InTransit state - cannot move to higher mobility.");
                    return false;
                }

                // Calculate target state (one step toward InTransit)
                int targetIndex = (int)currentState - 1;
                DeploymentState targetState = (DeploymentState)targetIndex;

                // Special checks for entering InTransit state
                if (targetState == DeploymentState.InTransit)
                {
                    // Must be transportable to enter InTransit
                    if (!IsTransportable)
                    {
                        AppService.CaptureUiMessage($"{UnitName} is not transportable and cannot enter InTransit state.");
                        return false;
                    }

                    // Must have a valid transport profile
                    if (GetTransportProfile() == null)
                    {
                        AppService.CaptureUiMessage($"{UnitName} has no transport profile and cannot enter InTransit state.");
                        return false;
                    }

                    // Airborne units must be on an airbase
                    if (Classification == UnitClassification.AB || Classification == UnitClassification.MAB)
                    {
                        if (!onAirbase)
                        {
                            AppService.CaptureUiMessage($"{UnitName} must be on an airbase to enter InTransit state.");
                            return false;
                        }
                    }

                    // Marine units must be on a port
                    if (Classification == UnitClassification.MAR || Classification == UnitClassification.MMAR)
                    {
                        if (!onPort)
                        {
                            AppService.CaptureUiMessage($"{UnitName} must be on a port to enter InTransit state.");
                            return false;
                        }
                    }
                }

                // Comprehensive check for valid state transition
                if (!CanChangeToState(targetState, out string errorMsg))
                {
                    AppService.CaptureUiMessage($"{UnitName}: {errorMsg}");
                    return false;
                }
                
                // Save previous state for comparison
                var previousState = DeploymentState;

                // Set the new combat state
                DeploymentState = targetState;

                // Update the unit's mobility state based on the new combat state
                UpdateMobilityState(targetState, previousState);

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpOneState", e);
                return false;
            }
        }

        /// <summary>
        /// Attempts to move the unit one step down on the posture ladder.
        /// </summary>
        /// <returns></returns>
        private bool TryDeployDownOneState(bool validDeploymentHex = false)
        {
            try
            {
                DeploymentState currentState = DeploymentState;

                // Check if already at maximum entrenchment (Fortified state)
                if (currentState == DeploymentState.Fortified)
                {
                    AppService.CaptureUiMessage($"{UnitName} is already in Fortified state - cannot entrench further.");
                    return false;
                }

                // A unit InTransit cannot move down the ladder without a valid deployment hex.
                if (currentState == DeploymentState.InTransit && !validDeploymentHex)
                {
                    AppService.CaptureUiMessage($"{UnitName} is in InTransit state - cannot move down the ladder without a valid deployment hex.");
                    return false;
                }

                // Calculate target state (one step toward fortified)
                int targetIndex = (int)currentState + 1;
                DeploymentState targetState = (DeploymentState)targetIndex;

                // Comprehensive check for valid state transition
                if (!CanChangeToState(targetState, out string errorMsg))
                {
                    AppService.CaptureUiMessage($"{UnitName}: {errorMsg}");
                    return false;
                }

                // Save previous state for comparison.
                var previousState = DeploymentState;

                // Set the new combat state
                DeploymentState = targetState;

                // Update the unit's mobility state based on the new combat state
                UpdateMobilityState(targetState, previousState);

                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(TryDeployDownOneState), ex);
                return false;
            }
        }

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
        /// Determines whether two <see cref="DeploymentState"/> values are <em>adjacent rungs</em> on the posture ladder.
        /// </summary>
        /// <remarks>
        /// The method assumes the enum values are contiguous integers in mobility order.  If that ordering ever changes
        /// (e.g., inserting a new state or switching to bit‑flags), this helper must be updated accordingly.
        /// </remarks>
        private bool IsAdjacentStateTransition(DeploymentState currentState, DeploymentState targetState)
        {
            int currentIndex = (int)currentState;
            int targetIndex = (int)targetState;

            // Adjacent means difference of exactly 1
            return Math.Abs(currentIndex - targetIndex) == 1;
        }

        /// <summary>
        /// Handles the transition logic for changing the mobility state of the unit.
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="previousState"></param>
        private void UpdateMobilityState(DeploymentState newState, DeploymentState previousState)
        {
            try
            {
                if (GetDeployedProfile() == null)
                    throw new InvalidOperationException("Unit lacks a deployed weapon profile – cannot change mobility state.");

                bool enteringInTransit = newState == DeploymentState.InTransit;
                bool leavingInTransit = previousState == DeploymentState.InTransit;
                bool enteringMobile = newState == DeploymentState.Mobile && previousState != DeploymentState.Mobile;
                bool leavingMobile = newState != DeploymentState.Mobile && previousState == DeploymentState.Mobile;

                // Handle transitions TO InTransit
                if (enteringInTransit)
                {
                    IsMounted = false; // Can't be mounted while in transport
                    if (_mobileBonusApplied)
                    {
                        _mobileBonusApplied = false;
                    }
                    // Movement points lost when entering transit
                    MovementPoints.SetCurrent(0f);
                    UpdateMovementPointsForProfile(); // Update to transport profile
                }
                // Handle transitions FROM InTransit (special exception to ladder rule)
                else if (leavingInTransit)
                {
                    // Exception: InTransit must go directly to Deployed
                    DeploymentState = DeploymentState.Deployed;
                    IsMounted = false;

                    // Set to 1/2 deployed profile movement points,regardless of how far it traveled InTransit.
                    var deployedProfile = GetDeployedProfile();
                    int halfDeployedMovement = Mathf.CeilToInt(deployedProfile.MovementPoints * 0.5f);
                    MovementPoints.SetMax(deployedProfile.MovementPoints);
                    MovementPoints.SetCurrent(halfDeployedMovement);
                }
                // Handle transitions TO Mobile (normal ladder progression)
                else if (enteringMobile)
                {
                    // Set mounted status based on available transport profile
                    bool hasTransport = GetMountedProfile() != null;
                    IsMounted = hasTransport;

                    // ALL units get mobile bonus when entering Mobile state
                    if (!_mobileBonusApplied)
                    {
                        ApplyMovementBonus(+CUConstants.MOBILE_MOVEMENT_BONUS);
                        _mobileBonusApplied = true;
                    }

                    UpdateMovementPointsForProfile();
                }
                // Handle transitions FROM Mobile (normal ladder progression)
                else if (leavingMobile)
                {
                    IsMounted = false;
                    if (_mobileBonusApplied)
                    {
                        ApplyMovementBonus(-CUConstants.MOBILE_MOVEMENT_BONUS);
                        _mobileBonusApplied = false;
                    }
                    UpdateMovementPointsForProfile();
                }
                // Handle dis-entrenchment exception (HastyDefense/Entrenched/Fortified -> Deployed)
                else if ((previousState == DeploymentState.HastyDefense ||
                          previousState == DeploymentState.Entrenched ||
                          previousState == DeploymentState.Fortified) &&
                         newState == DeploymentState.Deployed)
                {
                    // This is allowed as an exception to the ladder rule
                    IsMounted = false;
                    UpdateMovementPointsForProfile();
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(UpdateMobilityState), ex);
            }
        }

        /// <summary>
        /// Adjusts Max and Current movement points by <paramref name="delta"/>, clamping current to
        /// the [0, newMax] range.
        /// </summary>
        private void ApplyMovementBonus(float delta)
        {
            // Prevent stacking by checking current state vs intended operation
            if (delta > 0 && _mobileBonusApplied)
            {
                AppService.HandleException(CLASS_NAME, "ApplyMovementBonus",
                    new InvalidOperationException("Attempted to apply mobile bonus when already applied"),
                    ExceptionSeverity.Minor);
                return;
            }

            if (delta < 0 && !_mobileBonusApplied)
            {
                AppService.HandleException(CLASS_NAME, "ApplyMovementBonus",
                    new InvalidOperationException("Attempted to remove mobile bonus when not applied"),
                    ExceptionSeverity.Minor);
                return;
            }

            float newMax = Mathf.Max(0f, MovementPoints.Max + delta);
            float newCurrent = Mathf.Clamp(MovementPoints.Current + delta, 0f, newMax);

            MovementPoints.SetMax(newMax);
            MovementPoints.SetCurrent(newCurrent);
        }

        /// <summary>
        /// Checks if the unit can transition to the specified combat state.
        /// Validates unit type restrictions, adjacency rules, and resource requirements.
        /// </summary>
        /// <param name="targetState">The desired combat state</param>
        /// <returns>True if transition is allowed</returns>
        private bool CanChangeToState(DeploymentState targetState, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Same state - no change needed
            if (DeploymentState == targetState)
            {
                errorMessage = $"Already in target state {targetState}";
                return false;
            }

            // Check if the unit is destroyed
            if (IsDestroyed())
            {
                errorMessage = $"{UnitName} is destroyed and cannot change states";
                throw new InvalidOperationException(errorMessage);
            }

            // Air units and bases cannot change states
            if (!CanUnitTypeChangeStates())
            {
                errorMessage = $"{UnitName} cannot change combat states (unit type: {Classification})";
                return false;
            }

            // Check if transition is adjacent
            if (!IsAdjacentStateTransition(DeploymentState, targetState))
            {
                errorMessage = $"Transition from {DeploymentState} to {targetState} is not adjacent";
                return false;
            }

            // Check if the unit has critical supply levels
            if (DaysSupply.Current <= CUConstants.CRITICAL_SUPPLY_THRESHOLD)
            {
                errorMessage = $"Cannot change state with critical supply levels ({DaysSupply.Current:F1} days remaining)";
                return false;
            }

            // Only limited DeploymentState transitions are allowed based on efficiency level
            if (EfficiencyLevel == EfficiencyLevel.StaticOperations)
            {
                if (DeploymentState == DeploymentState.Fortified ||
                    DeploymentState == DeploymentState.Entrenched ||
                    DeploymentState == DeploymentState.HastyDefense)
                {
                    errorMessage = $"Cannot change from defensive states in Static Operations (current efficiency: {EfficiencyLevel})";
                    return false;
                }

                if (targetState == DeploymentState.Mobile)
                {
                    errorMessage = $"Cannot change to Mobile state in Static Operations (current efficiency: {EfficiencyLevel})";
                    return false;
                }
            }

            return true;
        }

        #endregion // CombatUnit Actions Helper Methods


        #region Experience System Helpers

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
        private float GetExperienceMultiplier()
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

        #endregion // Experience System Helpers


        #region Debugging

        /// <summary>
        /// Direct change of combat state for debugging purposes.
        /// </summary>
        /// <param name="newState"></param>
        public void DebugSetCombatState(DeploymentState newState)
        {
            DeploymentState = newState;
        }

        /// <summary>
        /// Sets the mounted state of the object for debugging purposes.
        /// </summary>
        /// <param name="isMounted">A value indicating whether the object should be marked as mounted.  <see langword="true"/> to mark the
        /// object as mounted; otherwise, <see langword="false"/>.</param>
        public void DebugSetMounted(bool isMounted)
        {
            IsMounted = isMounted;
        }

        /// <summary>
        /// Calculates the movement cost for a combat action.
        /// </summary>
        /// <returns>The movement cost as a floating-point number, representing the ceiling of the maximum movement points
        /// multiplied by the combat action movement cost constant.</returns>
        public float DebugGetCombatMovementCost()
        {
            return Mathf.CeilToInt(MovementPoints.Max * CUConstants.COMBAT_ACTION_MOVEMENT_COST);
        }

        #endregion // Debugging


        #region ICloneable Implementation

        /// <summary>
        /// Creates a deep copy of this CombatUnit with a new unique ID.
        /// Used for spawning fresh units from templates - never used on live state objects.
        /// Leaders are not cloned and must be assigned separately.
        /// </summary>
        /// <returns>A new CombatUnit instance with identical properties but unique ID</returns>
        public object Clone()
        {
            try
            {
                // Create new unit with same basic parameters
                var clonedUnit = new CombatUnit(
                    unitName: UnitName + "_Clone",
                    unitType: UnitType,
                    classification: Classification,
                    role: Role,
                    side: Side,
                    nationality: Nationality,
                    intelProfileType: IntelProfileType,
                    deployedProfileID: DeployedProfileID,
                    isMountable: IsMountable,
                    mountedProfileID: MountedProfileID,
                    isTransportable: IsTransportable,
                    transportProfileID: TransportProfileID,
                    category: IsBase ? DepotCategory : DepotCategory.Secondary,
                    size: IsBase ? DepotSize : DepotSize.Small
                );

                // Copy state data
                clonedUnit.ExperiencePoints = ExperiencePoints;
                clonedUnit.ExperienceLevel = ExperienceLevel;
                clonedUnit.EfficiencyLevel = EfficiencyLevel;
                clonedUnit.IsMounted = IsMounted;
                clonedUnit.DeploymentState = DeploymentState;
                clonedUnit.SpottedLevel = SpottedLevel;
                clonedUnit._mobileBonusApplied = _mobileBonusApplied;

                // Copy StatsMaxCurrent values
                clonedUnit.HitPoints.SetMax(HitPoints.Max);
                clonedUnit.HitPoints.SetCurrent(HitPoints.Current);

                clonedUnit.DaysSupply.SetMax(DaysSupply.Max);
                clonedUnit.DaysSupply.SetCurrent(DaysSupply.Current);

                clonedUnit.MovementPoints.SetMax(MovementPoints.Max);
                clonedUnit.MovementPoints.SetCurrent(MovementPoints.Current);

                // Copy action counts
                clonedUnit.MoveActions.SetMax(MoveActions.Max);
                clonedUnit.MoveActions.SetCurrent(MoveActions.Current);

                clonedUnit.CombatActions.SetMax(CombatActions.Max);
                clonedUnit.CombatActions.SetCurrent(CombatActions.Current);

                clonedUnit.DeploymentActions.SetMax(DeploymentActions.Max);
                clonedUnit.DeploymentActions.SetCurrent(DeploymentActions.Current);

                clonedUnit.OpportunityActions.SetMax(OpportunityActions.Max);
                clonedUnit.OpportunityActions.SetCurrent(OpportunityActions.Current);

                clonedUnit.IntelActions.SetMax(IntelActions.Max);
                clonedUnit.IntelActions.SetCurrent(IntelActions.Current);

                // Copy position
                clonedUnit.MapPos = MapPos;

                // Copy facility data if this is a base unit
                if (IsBase)
                {
                    clonedUnit.BaseDamage = BaseDamage;
                    clonedUnit.OperationalCapacity = OperationalCapacity;
                    clonedUnit.FacilityType = FacilityType;
                    clonedUnit.DepotSize = DepotSize;
                    clonedUnit.StockpileInDays = StockpileInDays;
                    clonedUnit.GenerationRate = GenerationRate;
                    clonedUnit.SupplyProjection = SupplyProjection;
                    clonedUnit.SupplyPenetration = SupplyPenetration;
                    clonedUnit.DepotCategory = DepotCategory;

                    // Note: Air unit attachments are NOT cloned - they represent specific unit relationships
                    // that should not be duplicated. The cloned facility starts with no attached units.
                }

                // Note: LeaderID is intentionally NOT cloned
                // Leaders must be assigned separately to avoid duplicate assignments
                clonedUnit.LeaderID = null;

                return clonedUnit;
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
                IsMountable = info.GetBoolean(nameof(IsMountable));
                SpottedLevel = (SpottedLevel)info.GetValue(nameof(SpottedLevel), typeof(SpottedLevel));

                // Load IntelProfileType directly as enum value
                IntelProfileType = (IntelProfileTypes)info.GetValue(nameof(IntelProfileType), typeof(IntelProfileTypes));

                // Load profile IDs directly as enum values (no reference resolution needed)
                DeployedProfileID = (WeaponSystems)info.GetValue(nameof(DeployedProfileID), typeof(WeaponSystems));
                MountedProfileID = (WeaponSystems)info.GetValue(nameof(MountedProfileID), typeof(WeaponSystems));
                TransportProfileID = (WeaponSystems)info.GetValue(nameof(TransportProfileID), typeof(WeaponSystems));

                // Load leader ID directly - no temporary storage needed
                LeaderID = info.GetString("LeaderID");
                if (string.IsNullOrEmpty(LeaderID))
                    LeaderID = null;

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
                DeploymentState = (DeploymentState)info.GetValue(nameof(DeploymentState), typeof(DeploymentState));
                MapPos = (Coordinate2D)info.GetValue(nameof(MapPos), typeof(Coordinate2D));
                _mobileBonusApplied = info.GetBoolean(SERIAL_KEY_MOBILE_BONUS);

                // Deserialize facility data if this is a base unit
                if (IsBase)
                {
                    // Initialize facility collections first
                    _airUnitsAttached.Clear();
                    _attachedUnitIDs.Clear();

                    // Load common facility properties
                    BaseDamage = info.GetInt32(nameof(BaseDamage));
                    OperationalCapacity = (OperationalCapacity)info.GetValue(nameof(OperationalCapacity), typeof(OperationalCapacity));
                    FacilityType = (FacilityType)info.GetValue(nameof(FacilityType), typeof(FacilityType));

                    // Load supply depot properties
                    DepotSize = (DepotSize)info.GetValue(nameof(DepotSize), typeof(DepotSize));
                    StockpileInDays = info.GetSingle(nameof(StockpileInDays));
                    GenerationRate = (SupplyGenerationRate)info.GetValue(nameof(GenerationRate), typeof(SupplyGenerationRate));
                    SupplyProjection = (SupplyProjection)info.GetValue(nameof(SupplyProjection), typeof(SupplyProjection));
                    SupplyPenetration = info.GetBoolean(nameof(SupplyPenetration));
                    DepotCategory = (DepotCategory)info.GetValue(nameof(DepotCategory), typeof(DepotCategory));

                    // Load air unit attachments for later resolution
                    int airUnitCount = info.GetInt32("AirUnitCount");
                    for (int i = 0; i < airUnitCount; i++)
                    {
                        string unitID = info.GetString($"AirUnitID_{i}");
                        if (!string.IsNullOrEmpty(unitID))
                        {
                            _attachedUnitIDs.Add(unitID);
                        }
                    }

                    // Initialize readonly property
                    AirUnitsAttached = _airUnitsAttached.AsReadOnly();
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CombatUnit), e);
                throw;
            }
        }

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
                info.AddValue(nameof(IsMountable), IsMountable);
                info.AddValue(nameof(IsBase), IsBase);
                info.AddValue(nameof(SpottedLevel), SpottedLevel);

                // Serialize IntelProfileType directly as enum value
                info.AddValue(nameof(IntelProfileType), IntelProfileType);

                // Serialize profile IDs directly as enum values (no reference resolution needed)
                info.AddValue(nameof(DeployedProfileID), DeployedProfileID);
                info.AddValue(nameof(MountedProfileID), MountedProfileID);
                info.AddValue(nameof(TransportProfileID), TransportProfileID);

                // Serialize leader reference as ID (simple string)
                info.AddValue("LeaderID", LeaderID ?? "");

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
                info.AddValue(nameof(DeploymentState), DeploymentState);
                info.AddValue(nameof(MapPos), MapPos);
                info.AddValue(SERIAL_KEY_MOBILE_BONUS, _mobileBonusApplied);

                // Serialize facility data if this is a base unit
                if (IsBase)
                {
                    // Serialize common facility properties
                    info.AddValue(nameof(BaseDamage), BaseDamage);
                    info.AddValue(nameof(OperationalCapacity), OperationalCapacity);
                    info.AddValue(nameof(FacilityType), FacilityType);

                    // Serialize supply depot properties
                    info.AddValue(nameof(DepotSize), DepotSize);
                    info.AddValue(nameof(StockpileInDays), StockpileInDays);
                    info.AddValue(nameof(GenerationRate), GenerationRate);
                    info.AddValue(nameof(SupplyProjection), SupplyProjection);
                    info.AddValue(nameof(SupplyPenetration), SupplyPenetration);
                    info.AddValue(nameof(DepotCategory), DepotCategory);

                    // Serialize air unit attachments as Unit IDs to avoid circular references
                    info.AddValue("AirUnitCount", _airUnitsAttached.Count);
                    for (int i = 0; i < _airUnitsAttached.Count; i++)
                    {
                        info.AddValue($"AirUnitID_{i}", _airUnitsAttached[i].UnitID);
                    }
                }
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
            // Only facilities attach other units
            return IsBase && _attachedUnitIDs.Count > 0;
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

            // Include facility's unresolved references
            if (IsBase)
            {
                unresolvedIDs.AddRange(_attachedUnitIDs.Select(unitID => $"AirUnit:{unitID}"));
            }

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
                // Leader resolution is no longer needed - handled via GameDataManager lookup

                // Resolve facility references if this is a base unit
                if (IsBase && FacilityType == FacilityType.Airbase)
                {
                    _airUnitsAttached.Clear();

                    foreach (string unitID in _attachedUnitIDs)
                    {
                        var unit = manager.GetCombatUnit(unitID);
                        if (unit != null)
                        {
                            if (unit.UnitType == UnitType.AirUnit)
                            {
                                _airUnitsAttached.Add(unit);
                            }
                            else
                            {
                                AppService.HandleException(CLASS_NAME, "ResolveReferences",
                                    new InvalidOperationException($"Unit {unitID} is not an air unit (Type: {unit.UnitType})"),
                                    ExceptionSeverity.Minor);
                            }
                        }
                        else
                        {
                            AppService.HandleException(CLASS_NAME, "ResolveReferences",
                                new KeyNotFoundException($"Air unit {unitID} not found in game data manager"),
                                ExceptionSeverity.Minor);
                        }
                    }

                    _attachedUnitIDs.Clear(); // Clean up temporary storage
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ResolveReferences", e);
                throw;
            }
        }

        #endregion // IResolvableReferences


        #region Validation Methods

        /// <summary>
        /// Validates internal consistency of the unit, including facility relationships.
        /// </summary>
        /// <returns>List of validation errors found</returns>
        public List<string> ValidateInternalConsistency()
        {
            var errors = new List<string>();

            try
            {
                // Validate leader assignment consistency
                if (IsLeaderAssigned && UnitLeader == null)
                {
                    errors.Add($"Unit {UnitName} has IsLeaderAssigned=true but no CommandingOfficer");
                }
                else if (!IsLeaderAssigned && UnitLeader != null)
                {
                    errors.Add($"Unit {UnitName} has CommandingOfficer but IsLeaderAssigned=false");
                }

                // Validate facility consistency for base units
                if (IsBase)
                {
                    // Validate facility type matches classification
                    switch (Classification)
                    {
                        case UnitClassification.HQ:
                            if (FacilityType != FacilityType.HQ)
                                errors.Add($"HQ unit {UnitName} has incorrect facility type: {FacilityType}");
                            break;
                        case UnitClassification.DEPOT:
                            if (FacilityType != FacilityType.SupplyDepot)
                                errors.Add($"Depot unit {UnitName} has incorrect facility type: {FacilityType}");
                            break;
                        case UnitClassification.AIRB:
                            if (FacilityType != FacilityType.Airbase)
                                errors.Add($"Airbase unit {UnitName} has incorrect facility type: {FacilityType}");
                            break;
                    }

                    // Validate airbase attachments
                    if (FacilityType == FacilityType.Airbase)
                    {
                        foreach (var attachedUnit in AirUnitsAttached)
                        {
                            if (attachedUnit.UnitType != UnitType.AirUnit)
                            {
                                errors.Add($"Airbase {UnitName} has non-air unit {attachedUnit.UnitName} attached");
                            }
                        }

                        if (AirUnitsAttached.Count > CUConstants.MAX_AIR_UNITS)
                        {
                            errors.Add($"Airbase {UnitName} has {AirUnitsAttached.Count} attached units, exceeding maximum of {CUConstants.MAX_AIR_UNITS}");
                        }
                    }
                }
                else if (!IsBase && FacilityType != FacilityType.HQ) // HQ can be on non-base units
                {
                    errors.Add($"Non-base unit {UnitName} has FacilityType set to {FacilityType}");
                }

                // Validate profile references
                if (GetDeployedProfile() == null)
                {
                    errors.Add($"Unit {UnitName} has invalid DeployedProfileID: {DeployedProfileID}");
                }

                if (IsMounted && MountedProfileID != WeaponSystems.DEFAULT && GetMountedProfile() == null)
                {
                    errors.Add($"Unit {UnitName} is mounted but has invalid MountedProfileID: {MountedProfileID}");
                }

                // Validate state consistency
                if (HitPoints.Current > HitPoints.Max)
                {
                    errors.Add($"Unit {UnitName} has current HP ({HitPoints.Current}) greater than max HP ({HitPoints.Max})");
                }

                if (MovementPoints.Current > MovementPoints.Max)
                {
                    errors.Add($"Unit {UnitName} has current movement points ({MovementPoints.Current}) greater than max ({MovementPoints.Max})");
                }

                // Validate action counts
                if (MoveActions.Current > MoveActions.Max)
                {
                    errors.Add($"Unit {UnitName} has current move actions ({MoveActions.Current}) greater than max ({MoveActions.Max})");
                }

                if (CombatActions.Current > CombatActions.Max)
                {
                    errors.Add($"Unit {UnitName} has current combat actions ({CombatActions.Current}) greater than max ({CombatActions.Max})");
                }

            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ValidateInternalConsistency", e);
                errors.Add($"Validation failed with exception: {e.Message}");
            }

            return errors;
        }

        #endregion // Validation Methods
    }
}