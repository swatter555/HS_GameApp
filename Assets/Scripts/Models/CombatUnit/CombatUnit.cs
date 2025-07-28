using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using HammerAndSickle.Controllers;

/*────────────────────────────────────────────────────────────────────────────
 CombatUnit ─ universal military unit representation with modular subsystems
────────────────────────────────────────────────────────────────────────────────

Summary
═══════
• Central game entity representing all military units from tank regiments to supply
  depots through a unified object model. Units operate on sophisticated action
  economy mechanics where tactical decisions consume deployment actions, combat
  actions, movement points, and supplies. The design emphasizes resource trade-offs
  between positioning, offensive capability, and logistical sustainability.

• CombatUnit serves as the universal hub for diverse military capabilities using
  classification-based specialization rather than inheritance hierarchies. Ground
  units, aircraft, helicopters, and base facilities share the same core framework
  while accessing specialized behaviors through partial class extensions and
  profile-based stat systems.

• Five interconnected subsystems provide tactical depth: deployment state machines
  for positioning trade-offs, experience progression for veteran bonuses, facility
  management for base operations, leader integration for skill-based enhancements,
  and profile-driven combat calculations for authentic military effectiveness.

Public properties
═════════════════
string UnitName { get; set; }
string UnitID { get; private set; }
UnitType UnitType { get; private set; }
UnitClassification Classification { get; private set; }
UnitRole Role { get; private set; }
Side Side { get; private set; }
Nationality Nationality { get; private set; }
bool IsBase { get; }

WeaponSystems EmbarkedProfileID { get; private set; }
WeaponSystems MobileProfileID { get; private set; }
WeaponSystems DeployedProfileID { get; private set; }
IntelProfileTypes IntelProfileType { get; internal set; }

StatsMaxCurrent MoveActions { get; private set; }
StatsMaxCurrent CombatActions { get; private set; }
StatsMaxCurrent DeploymentActions { get; private set; }
StatsMaxCurrent OpportunityActions { get; private set; }
StatsMaxCurrent IntelActions { get; private set; }

StatsMaxCurrent HitPoints { get; private set; }
StatsMaxCurrent DaysSupply { get; private set; }
StatsMaxCurrent MovementPoints { get; private set; }
Coordinate2D MapPos { get; internal set; }
SpottedLevel SpottedLevel { get; private set; }

int ExperiencePoints { get; internal set; }
ExperienceLevel ExperienceLevel { get; internal set; }
EfficiencyLevel EfficiencyLevel { get; internal set; }

string LeaderID { get; internal set; }
bool IsLeaderAssigned { get; }
Leader UnitLeader { get; }

DeploymentPosition DeploymentPosition { get; }
bool IsEmbarkable { get; private set; }
bool IsMountable { get; private set; }

int BaseDamage { get; private set; }
OperationalCapacity OperationalCapacity { get; private set; }
FacilityType FacilityType { get; private set; }
DepotSize DepotSize { get; private set; }
float StockpileInDays { get; private set; }
IReadOnlyList<CombatUnit> AirUnitsAttached { get; private set; }

Constructors
════════════
public CombatUnit(string unitName, UnitType unitType, UnitClassification classification,
    UnitRole role, Side side, Nationality nationality, IntelProfileTypes intelProfileType,
    WeaponSystems deployedProfileID, bool isMountable = false, 
    WeaponSystems mobileProfileID = WeaponSystems.DEFAULT, bool isEmbarkable = false,
    WeaponSystems embarkProfileID = WeaponSystems.DEFAULT, 
    DepotCategory category = DepotCategory.Secondary, DepotSize size = DepotSize.Small)

protected CombatUnit(SerializationInfo info, StreamingContext context)

Public methods
══════════════
WeaponSystemProfile GetDeployedProfile()
WeaponSystemProfile GetMobileProfile()
WeaponSystemProfile GetEmbarkedProfile()
WeaponSystemProfile GetActiveWeaponSystemProfile()
WeaponSystemProfile GetCurrentCombatStrength()
IntelReport GenerateIntelReport(SpottedLevel spottedLevel = SpottedLevel.Level1)

void RefreshAllActions()
void RefreshMovementPoints()
void SetSpottedLevel(SpottedLevel spottedLevel)
void TakeDamage(float damage)
void Repair(float repairAmount)
bool ConsumeSupplies(float amount)
float ReceiveSupplies(float amount)
bool IsDestroyed()

bool PerformCombatAction()
bool PerformMoveAction(int movtCost)
bool PerformIntelAction()
bool PerformOpportunityAction()
Dictionary<ActionTypes, float> GetAvailableActions()
float GetDeployActions()
float GetCombatActions()
float GetMoveActions()
float GetOpportunityActions()
float GetIntelActions()

bool CanMove()
float GetSupplyStatus()
void SetEfficiencyLevel(EfficiencyLevel level)
void DecreaseEfficiencyLevelBy1()
void IncreaseEfficiencyLevelBy1()

void SetPosition(Coordinate2D newPos)
bool CanMoveTo(Coordinate2D targetPos)
float GetDistanceTo(Coordinate2D targetPos)
float GetDistanceTo(CombatUnit otherUnit)

object Clone()
void GetObjectData(SerializationInfo info, StreamingContext context)
bool HasUnresolvedReferences()
IReadOnlyList<string> GetUnresolvedReferenceIDs()
void ResolveReferences(GameDataManager manager)
List<string> ValidateInternalConsistency()

Private methods
═══════════════
void InitializeActionCounts()
void InitializeMovementPoints()
float GetFinalCombatRatingModifier()
float GetFinalCombatRatingModifier_Aircraft()
float GetStrengthModifier()
float GetCombatStateModifier()
float GetEfficiencyModifier()
bool ConsumeMovementPoints(float points)
float GetDeployMovementCost()
float GetCombatMovementCost()
float GetIntelMovementCost()

Partial Class Architecture
═════════════════════════
CombatUnit is implemented as a partial class distributed across multiple files for
logical organization and maintainability:

**CombatUnit.DeploymentSystem.cs** - Linear deployment state machine managing six
tactical positions (Fortified to Embarked) with resource-based transitions. Units
progress through defensive entrenchment or mobile deployment consuming deployment
actions, movement points, and supplies. Profile switching occurs automatically
based on state, with special rules for dis-entrenchment and facility-based
embarkation requirements.

**CombatUnit.ExperienceSystem.cs** - Progressive unit advancement through six
experience levels (Raw to Elite) affecting combat effectiveness via multiplicative
bonuses. Units gain experience through combat actions and battlefield achievements,
with level synchronization maintaining consistency between experience points and
derived advancement tiers.

**CombatUnit.Facility.cs** - Base facility operations for HQ, Airbase, and Supply
Depot units including damage-based operational capacity degradation, air unit
attachment management, and supply depot logistics with stockpile generation,
distance-based projection efficiency, and special transport capabilities for main
depots.

**CombatUnit.LeaderSystem.cs** - Command officer integration providing skill-based
bonuses through string ID reference resolution. Leaders provide multiplicative and
additive combat bonuses via unlocked skills while maintaining bidirectional
assignment relationships and reputation flow for advancement systems.

System Integration Architecture
═══════════════════════════════
The CombatUnit ecosystem operates through several key integration patterns:

**Profile-Based Statistics** - Units reference shared WeaponSystemProfile templates
via enum identifiers rather than storing individual combat values. The system
automatically switches between Deployed, Mobile, and Embarked profiles based on
deployment state, ensuring authentic tactical behavior while minimizing memory
usage and maintaining data consistency across identical unit types.

**Action Economy Framework** - All meaningful unit activities consume specific
resources through a unified action system. Combat actions cost 25% maximum movement
points plus supplies, deployment transitions cost 50% movement points plus deployment
actions, and movement consumes both move actions and variable movement points based
on terrain. This creates natural tactical trade-offs between positioning, combat
effectiveness, and resource conservation.

**Modifier Stacking System** - Combat effectiveness emerges from multiplicative
stacking of strength modifiers (hit point percentage), deployment state bonuses
(defensive positioning), efficiency levels (operational readiness), experience
multipliers (veteran bonuses), and leader skill effects. The GetCurrentCombatStrength()
method applies all modifiers to create temporary profiles for immediate calculations
without mutating base statistics.

**Reference Resolution Pattern** - Complex object relationships use string ID
references resolved through GameDataManager lookup to prevent circular serialization
dependencies. Leaders, attached air units, and profile references maintain clean
separation between persistent data storage and runtime object graphs, enabling
robust save/load functionality.

**State Validation Cascade** - Unit capabilities depend on hierarchical validation:
destroyed units cannot act, critically supplied units have restricted operations,
Static Operations efficiency prevents movement from defensive states, and base
facilities follow different action availability rules. This creates emergent
tactical complexity from simple rule interactions.

**Intelligence and Fog of War** - Units generate intelligence reports based on
SpottedLevel and IntelProfileType, providing filtered information about enemy
capabilities. The system balances authentic military uncertainty with gameplay
clarity through configurable error margins and progressive revelation mechanics.

Developer notes
═══════════════
• **Classification-Based Specialization** - Unit behaviors are determined by
  UnitClassification enum values rather than inheritance hierarchies. This allows
  runtime behavior switching and simplified serialization while maintaining type
  safety through validation in the constructor and action methods.

• **StatsMaxCurrent Pattern** - All dynamic unit resources (actions, hit points,
  supplies, movement points) use the StatsMaxCurrent class for consistent maximum/
  current value tracking with automatic validation and progress calculation support.

• **Serialization Strategy** - The class implements custom ISerializable with
  reference resolution support for complex object relationships. Profile references
  are stored as enum values, leader assignments as string IDs, and attached units
  as temporary ID collections resolved during deserialization.

• **Exception Handling Integration** - All public methods use AppService.HandleException
  with appropriate severity levels for consistent error logging and UI messaging.
  Critical operations include defensive validation to prevent invalid state
  transitions and resource corruption.

• **Thread Safety Considerations** - While the class itself is not thread-safe,
  all external dependencies (AppService, GameDataManager, WeaponSystemsDatabase)
  provide thread-safe operations for concurrent access during AI processing and
  UI updates. Modification operations should occur on the main thread only.

────────────────────────────────────────────────────────────────────────────── */
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

   
    [Serializable]
    public partial class CombatUnit : ICloneable, ISerializable, IResolvableReferences
    {
        #region Constants

        private const string CLASS_NAME = nameof(CombatUnit);

        #endregion


        #region Properties

        // Identification and metadata
        public string UnitName { get; set; }
        public string UnitID { get; private set; }
        public UnitType UnitType { get; private set; }
        public UnitClassification Classification { get; private set; }
        public UnitRole Role { get; private set; }
        public Side Side { get; private set; }
        public Nationality Nationality { get; private set; }
        public bool IsBase => Classification.IsBaseType();

        // Profile IDs
        // Profile for air, helo, naval transport, if available.
        public WeaponSystems EmbarkedProfileID { get; private set; }

        // Profile for units that have organic transport (Truck/APC/IFV), if available.
        public WeaponSystems MobileProfileID { get; private set; }

        // The default, core profile that all units have.
        public WeaponSystems DeployedProfileID { get; private set; }

        // The profile used to generate intelligence reports.
        public IntelProfileTypes IntelProfileType { get; internal set; }

        // Action counts using StatsMaxCurrent
        public StatsMaxCurrent MoveActions { get; private set; }
        public StatsMaxCurrent CombatActions { get; private set; }
        public StatsMaxCurrent DeploymentActions { get; private set; }
        public StatsMaxCurrent OpportunityActions { get; private set; }
        public StatsMaxCurrent IntelActions { get; private set; }

        // State data
        public StatsMaxCurrent HitPoints { get; private set; }
        public StatsMaxCurrent DaysSupply { get; private set; }
        public StatsMaxCurrent MovementPoints { get; private set; }
        public Coordinate2D MapPos { get; internal set; }
        public SpottedLevel SpottedLevel { get; private set; }

        #endregion // Properties


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
            WeaponSystems mobileProfileID = WeaponSystems.DEFAULT,
            bool isEmbarkable = false,
            WeaponSystems embarkProfileID = WeaponSystems.DEFAULT,
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
                if (isMountable && mobileProfileID != WeaponSystems.DEFAULT)
                {
                    if (WeaponSystemsDatabase.GetWeaponSystemProfile(mobileProfileID) == null)
                        throw new ArgumentException($"Mounted profile ID {mobileProfileID} not found in database", nameof(mobileProfileID));
                }

                // When not DEFAULT, transport profile ID must point to a valid profile.
                if (isEmbarkable && embarkProfileID != WeaponSystems.DEFAULT)
                {
                    if (WeaponSystemsDatabase.GetWeaponSystemProfile(embarkProfileID) == null)
                        throw new ArgumentException($"Transport profile ID {embarkProfileID} not found in database", nameof(embarkProfileID));
                }

                // Validate intel profile type
                if (!Enum.IsDefined(typeof(IntelProfileTypes), intelProfileType))
                    throw new ArgumentException("Invalid intel profile type", nameof(intelProfileType));

                //---------------------------------
                // Set identification and metadata
                //---------------------------------

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

                // Initialize deployment position based on embarkable and mountable states
                InitializeDeploymentSystem(isEmbarkable, isMountable);

                // Set profile IDs
                EmbarkedProfileID = embarkProfileID;
                DeployedProfileID = deployedProfileID;
                MobileProfileID = mobileProfileID;
                IntelProfileType = intelProfileType;

                // Initialise facility data when appropriate
                if (IsBase)
                {
                    InitializeFacility(category, size);
                }

                // Initialize action counts based on unit type and classification
                InitializeActionCounts();

                // Initialize the leader system
                InitializeLeaderSystem();

                // Set the initial spotted level
                SpottedLevel = SpottedLevel.Level1;

                // Initialize experience system
                InitializeExperienceSystem();

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

        #endregion // Constructors


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
        /// Gets the weapon‑system profile for the mobile state.
        /// </summary>
        /// <returns></returns>
        public WeaponSystemProfile GetMobileProfile()
        {
            try
            {
                // DEFAULT means the unit is foot‑mobile (no transport profile).
                if (MobileProfileID == WeaponSystems.DEFAULT)
                    return null;

                var profile = WeaponSystemsDatabase.GetWeaponSystemProfile(MobileProfileID);
                if (profile == null)
                    throw new InvalidOperationException($"Mobile profile ID '{MobileProfileID}' not found in weapon‑systems database.");

                return profile;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(GetMobileProfile), ex);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the weapon‑system profile the unit employs when in transport (airlift/sea).
        /// </summary>
        /// <returns></returns>
        public WeaponSystemProfile GetEmbarkedProfile()
        {
            try
            {
                // DEFAULT means the unit has no transport profile.
                if (EmbarkedProfileID == WeaponSystems.DEFAULT)
                    return null;

                var profile = WeaponSystemsDatabase.GetWeaponSystemProfile(EmbarkedProfileID);
                if (profile == null)
                    throw new InvalidOperationException($"Embarked profile ID '{EmbarkedProfileID}' not found in weapon‑systems database.");
                
                return profile;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(GetEmbarkedProfile), ex);
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
                    DeploymentPosition,
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
            return DeploymentPosition switch
            {
                DeploymentPosition.Embarked => GetEmbarkedProfile() ?? GetDeployedProfile(),
                DeploymentPosition.Mobile => GetMobileProfile() ?? GetDeployedProfile(),
                _=> GetDeployedProfile()
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
            return DeploymentPosition switch
            {
                DeploymentPosition.Mobile => CUConstants.COMBAT_MOD_MOBILE,
                DeploymentPosition.Deployed => CUConstants.COMBAT_MOD_DEPLOYED,
                DeploymentPosition.HastyDefense => CUConstants.COMBAT_MOD_HASTY_DEFENSE,
                DeploymentPosition.Entrenched => CUConstants.COMBAT_MOD_ENTRENCHED,
                DeploymentPosition.Fortified => CUConstants.COMBAT_MOD_FORTIFIED,
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

        #endregion // Core


        #region CombatUnit Actions

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


        #region Debugging

        /// <summary>
        /// Direct change of combat state for debugging purposes.
        /// </summary>
        /// <param name="newPosition"></param>
        public void DebugSetCombatState(DeploymentPosition newPosition)
        {
            _deploymentPosition = newPosition;
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
                // Create new unit with same basic parameters but fresh state
                var clonedUnit = new CombatUnit(
                    unitName: UnitName,
                    unitType: UnitType,
                    classification: Classification,
                    role: Role,
                    side: Side,
                    nationality: Nationality,
                    intelProfileType: IntelProfileType,
                    deployedProfileID: DeployedProfileID,
                    isMountable: IsMountable,
                    mobileProfileID: MobileProfileID,
                    isEmbarkable: IsEmbarkable,
                    embarkProfileID: EmbarkedProfileID,
                    category: DepotCategory,
                    size: DepotSize
                );

                // Copy deployment system state
                clonedUnit._deploymentPosition = _deploymentPosition;

                // Copy experience system state
                clonedUnit.ExperiencePoints = ExperiencePoints;
                clonedUnit.ExperienceLevel = ExperienceLevel;
                clonedUnit.EfficiencyLevel = EfficiencyLevel;

                // Copy current state values
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

                // Copy position and spotted level
                clonedUnit.MapPos = MapPos;
                clonedUnit.SpottedLevel = SpottedLevel;

                // Copy facility state if applicable (but not attached units - they need separate handling)
                if (IsBase)
                {
                    clonedUnit.BaseDamage = BaseDamage;
                    clonedUnit.OperationalCapacity = OperationalCapacity;

                    if (FacilityType == FacilityType.SupplyDepot)
                    {
                        clonedUnit.StockpileInDays = StockpileInDays;
                        clonedUnit.GenerationRate = GenerationRate;
                        clonedUnit.SupplyProjection = SupplyProjection;
                        clonedUnit.SupplyPenetration = SupplyPenetration;
                        // Note: Air units are not cloned - facilities start empty
                    }
                }

                // Leaders are not cloned - template units have no leaders
                // clonedUnit.LeaderID remains null

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
                // Core identification and metadata
                UnitName = info.GetString("UnitName");
                UnitID = info.GetString("UnitID");
                UnitType = (UnitType)info.GetValue("UnitType", typeof(UnitType));
                Classification = (UnitClassification)info.GetValue("Classification", typeof(UnitClassification));
                Role = (UnitRole)info.GetValue("Role", typeof(UnitRole));
                Side = (Side)info.GetValue("Side", typeof(Side));
                Nationality = (Nationality)info.GetValue("Nationality", typeof(Nationality));

                // Profile IDs
                DeployedProfileID = (WeaponSystems)info.GetValue("DeployedProfileID", typeof(WeaponSystems));
                MobileProfileID = (WeaponSystems)info.GetValue("MobileProfileID", typeof(WeaponSystems));
                EmbarkedProfileID = (WeaponSystems)info.GetValue("EmbarkedProfileID", typeof(WeaponSystems));
                IntelProfileType = (IntelProfileTypes)info.GetValue("IntelProfileType", typeof(IntelProfileTypes));

                // Deployment system data
                _deploymentPosition = (DeploymentPosition)info.GetValue("DeploymentPosition", typeof(DeploymentPosition));
                IsEmbarkable = info.GetBoolean("IsEmbarkable");
                IsMountable = info.GetBoolean("IsMountable");

                // Experience system data
                ExperiencePoints = info.GetInt32("ExperiencePoints");
                ExperienceLevel = (ExperienceLevel)info.GetValue("ExperienceLevel", typeof(ExperienceLevel));
                EfficiencyLevel = (EfficiencyLevel)info.GetValue("EfficiencyLevel", typeof(EfficiencyLevel));

                // Leader system data
                LeaderID = info.GetString("LeaderID"); // Can be null

                // StatsMaxCurrent properties - deserialize both max and current values
                HitPoints = new StatsMaxCurrent(info.GetSingle("HitPointsMax"), info.GetSingle("HitPointsCurrent"));
                DaysSupply = new StatsMaxCurrent(info.GetSingle("DaysSupplyMax"), info.GetSingle("DaysSupplyCurrent"));
                MovementPoints = new StatsMaxCurrent(info.GetSingle("MovementPointsMax"), info.GetSingle("MovementPointsCurrent"));

                // Action counts
                MoveActions = new StatsMaxCurrent(info.GetSingle("MoveActionsMax"), info.GetSingle("MoveActionsCurrent"));
                CombatActions = new StatsMaxCurrent(info.GetSingle("CombatActionsMax"), info.GetSingle("CombatActionsCurrent"));
                DeploymentActions = new StatsMaxCurrent(info.GetSingle("DeploymentActionsMax"), info.GetSingle("DeploymentActionsCurrent"));
                OpportunityActions = new StatsMaxCurrent(info.GetSingle("OpportunityActionsMax"), info.GetSingle("OpportunityActionsCurrent"));
                IntelActions = new StatsMaxCurrent(info.GetSingle("IntelActionsMax"), info.GetSingle("IntelActionsCurrent"));

                // Position and state
                MapPos = (Coordinate2D)info.GetValue("MapPos", typeof(Coordinate2D));
                SpottedLevel = (SpottedLevel)info.GetValue("SpottedLevel", typeof(SpottedLevel));

                // Facility data (if applicable)
                if (Classification.IsBaseType())
                {
                    BaseDamage = info.GetInt32("BaseDamage");
                    OperationalCapacity = (OperationalCapacity)info.GetValue("OperationalCapacity", typeof(OperationalCapacity));
                    FacilityType = (FacilityType)info.GetValue("FacilityType", typeof(FacilityType));

                    if (FacilityType == FacilityType.SupplyDepot)
                    {
                        DepotSize = (DepotSize)info.GetValue("DepotSize", typeof(DepotSize));
                        DepotCategory = (DepotCategory)info.GetValue("DepotCategory", typeof(DepotCategory));
                        StockpileInDays = info.GetSingle("StockpileInDays");
                        GenerationRate = (SupplyGenerationRate)info.GetValue("GenerationRate", typeof(SupplyGenerationRate));
                        SupplyProjection = (SupplyProjection)info.GetValue("SupplyProjection", typeof(SupplyProjection));
                        SupplyPenetration = info.GetBoolean("SupplyPenetration");
                    }

                    if (FacilityType == FacilityType.Airbase)
                    {
                        // Initialize collections
                        _airUnitsAttached = new List<CombatUnit>();
                        AirUnitsAttached = _airUnitsAttached.AsReadOnly();

                        // Load attached unit IDs for later resolution
                        _attachedUnitIDs = new List<string>();
                        int attachedCount = info.GetInt32("AttachedUnitCount");
                        for (int i = 0; i < attachedCount; i++)
                        {
                            string unitId = info.GetString($"AttachedUnitID_{i}");
                            if (!string.IsNullOrEmpty(unitId))
                            {
                                _attachedUnitIDs.Add(unitId);
                            }
                        }
                    }
                }
                else
                {
                    // Initialize facility collections for non-base units
                    _airUnitsAttached = new List<CombatUnit>();
                    AirUnitsAttached = _airUnitsAttached.AsReadOnly();
                    _attachedUnitIDs = new List<string>();
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Deserialization Constructor", e);
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
                // Core identification and metadata
                info.AddValue("UnitName", UnitName);
                info.AddValue("UnitID", UnitID);
                info.AddValue("UnitType", UnitType);
                info.AddValue("Classification", Classification);
                info.AddValue("Role", Role);
                info.AddValue("Side", Side);
                info.AddValue("Nationality", Nationality);

                // Profile IDs
                info.AddValue("DeployedProfileID", DeployedProfileID);
                info.AddValue("MobileProfileID", MobileProfileID);
                info.AddValue("EmbarkedProfileID", EmbarkedProfileID);
                info.AddValue("IntelProfileType", IntelProfileType);

                // Deployment system data
                info.AddValue("DeploymentPosition", _deploymentPosition);
                info.AddValue("IsEmbarkable", IsEmbarkable);
                info.AddValue("IsMountable", IsMountable);

                // Experience system data
                info.AddValue("ExperiencePoints", ExperiencePoints);
                info.AddValue("ExperienceLevel", ExperienceLevel);
                info.AddValue("EfficiencyLevel", EfficiencyLevel);

                // Leader system data
                info.AddValue("LeaderID", LeaderID); // Can be null

                // StatsMaxCurrent properties - serialize both max and current values
                info.AddValue("HitPointsMax", HitPoints.Max);
                info.AddValue("HitPointsCurrent", HitPoints.Current);
                info.AddValue("DaysSupplyMax", DaysSupply.Max);
                info.AddValue("DaysSupplyCurrent", DaysSupply.Current);
                info.AddValue("MovementPointsMax", MovementPoints.Max);
                info.AddValue("MovementPointsCurrent", MovementPoints.Current);

                // Action counts
                info.AddValue("MoveActionsMax", MoveActions.Max);
                info.AddValue("MoveActionsCurrent", MoveActions.Current);
                info.AddValue("CombatActionsMax", CombatActions.Max);
                info.AddValue("CombatActionsCurrent", CombatActions.Current);
                info.AddValue("DeploymentActionsMax", DeploymentActions.Max);
                info.AddValue("DeploymentActionsCurrent", DeploymentActions.Current);
                info.AddValue("OpportunityActionsMax", OpportunityActions.Max);
                info.AddValue("OpportunityActionsCurrent", OpportunityActions.Current);
                info.AddValue("IntelActionsMax", IntelActions.Max);
                info.AddValue("IntelActionsCurrent", IntelActions.Current);

                // Position and state
                info.AddValue("MapPos", MapPos);
                info.AddValue("SpottedLevel", SpottedLevel);

                // Facility data (if applicable)
                if (IsBase)
                {
                    info.AddValue("BaseDamage", BaseDamage);
                    info.AddValue("OperationalCapacity", OperationalCapacity);
                    info.AddValue("FacilityType", FacilityType);

                    if (FacilityType == FacilityType.SupplyDepot)
                    {
                        info.AddValue("DepotSize", DepotSize);
                        info.AddValue("DepotCategory", DepotCategory);
                        info.AddValue("StockpileInDays", StockpileInDays);
                        info.AddValue("GenerationRate", GenerationRate);
                        info.AddValue("SupplyProjection", SupplyProjection);
                        info.AddValue("SupplyPenetration", SupplyPenetration);
                    }

                    if (FacilityType == FacilityType.Airbase)
                    {
                        // Serialize attached unit IDs only (not the actual units)
                        info.AddValue("AttachedUnitCount", _airUnitsAttached.Count);
                        for (int i = 0; i < _airUnitsAttached.Count; i++)
                        {
                            info.AddValue($"AttachedUnitID_{i}", _airUnitsAttached[i].UnitID);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion // ISerializable Implementation


        #region IResolvableReferences

        /// <summary>
        /// Checks if there are unresolved references that need to be resolved.
        /// </summary>
        /// <returns>True if any resolution methods need to be called</returns>
        public bool HasUnresolvedReferences()
        {
            // Check for unresolved leader reference
            if (!string.IsNullOrEmpty(LeaderID))
                return true;

            // Check for unresolved facility references
            if (IsBase && _attachedUnitIDs.Count > 0)
                return true;

            return false;
        }

        /// <summary>
        /// Gets the list of unresolved reference IDs that need to be resolved.
        /// </summary>
        /// <returns>Collection of object IDs that this object references</returns>
        public IReadOnlyList<string> GetUnresolvedReferenceIDs()
        {
            var unresolvedIDs = new List<string>();

            // Include leader ID if assigned
            if (!string.IsNullOrEmpty(LeaderID))
            {
                unresolvedIDs.Add($"Leader:{LeaderID}");
            }

            // Include facility's unresolved references
            if (IsBase && _attachedUnitIDs.Count > 0)
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
                // Resolve leader reference if assigned
                if (!string.IsNullOrEmpty(LeaderID))
                {
                    var leader = manager.GetLeader(LeaderID);
                    if (leader == null)
                    {
                        throw new KeyNotFoundException($"Leader {LeaderID} not found in game data manager");
                    }
                    // Leader resolution is handled via GameDataManager lookup - no additional action needed
                    // The UnitLeader property will resolve it dynamically
                }

                // Resolve facility references if this is a base unit
                if (IsBase && FacilityType == FacilityType.Airbase && _attachedUnitIDs.Count > 0)
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
                            throw new KeyNotFoundException($"Air unit {unitID} not found in game data manager");
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
                // Validate basic properties
                if (string.IsNullOrEmpty(UnitID))
                    errors.Add("Unit ID cannot be null or empty");

                if (string.IsNullOrEmpty(UnitName))
                    errors.Add("Unit name cannot be null or empty");

                // Validate profile IDs exist in database
                if (DeployedProfileID != WeaponSystems.DEFAULT)
                {
                    if (WeaponSystemsDatabase.GetWeaponSystemProfile(DeployedProfileID) == null)
                        errors.Add($"Deployed profile {DeployedProfileID} not found in WeaponSystemsDatabase");
                }
                else
                {
                    errors.Add("Deployed profile cannot be DEFAULT");
                }

                if (MobileProfileID != WeaponSystems.DEFAULT)
                {
                    if (WeaponSystemsDatabase.GetWeaponSystemProfile(MobileProfileID) == null)
                        errors.Add($"Mobile profile {MobileProfileID} not found in WeaponSystemsDatabase");
                }

                if (EmbarkedProfileID != WeaponSystems.DEFAULT)
                {
                    if (WeaponSystemsDatabase.GetWeaponSystemProfile(EmbarkedProfileID) == null)
                        errors.Add($"Embarked profile {EmbarkedProfileID} not found in WeaponSystemsDatabase");
                }

                // Validate intel profile exists
                if (!IntelProfile.HasProfile(IntelProfileType))
                    errors.Add($"Intel profile {IntelProfileType} not found in IntelProfile system");

                // Validate leader exists if assigned
                if (!string.IsNullOrEmpty(LeaderID))
                {
                    if (GameDataManager.Instance?.GetLeader(LeaderID) == null)
                        errors.Add($"Leader {LeaderID} not found in GameDataManager");
                }

                // Validate StatsMaxCurrent consistency
                if (HitPoints.Current > HitPoints.Max)
                    errors.Add("Hit points current cannot exceed maximum");

                if (DaysSupply.Current > DaysSupply.Max)
                    errors.Add("Days supply current cannot exceed maximum");

                if (MovementPoints.Current > MovementPoints.Max)
                    errors.Add("Movement points current cannot exceed maximum");

                // Validate action counts
                if (MoveActions.Current > MoveActions.Max)
                    errors.Add("Move actions current cannot exceed maximum");

                if (CombatActions.Current > CombatActions.Max)
                    errors.Add("Combat actions current cannot exceed maximum");

                if (DeploymentActions.Current > DeploymentActions.Max)
                    errors.Add("Deployment actions current cannot exceed maximum");

                if (OpportunityActions.Current > OpportunityActions.Max)
                    errors.Add("Opportunity actions current cannot exceed maximum");

                if (IntelActions.Current > IntelActions.Max)
                    errors.Add("Intel actions current cannot exceed maximum");

                // Validate facility-specific data
                if (IsBase)
                {
                    if (BaseDamage < CUConstants.MIN_DAMAGE || BaseDamage > CUConstants.MAX_DAMAGE)
                        errors.Add($"Base damage must be between {CUConstants.MIN_DAMAGE} and {CUConstants.MAX_DAMAGE}");

                    if (FacilityType == FacilityType.Airbase)
                    {
                        // Validate attached air units exist and are air units
                        foreach (var airUnit in _airUnitsAttached)
                        {
                            if (airUnit == null)
                                errors.Add("Attached air unit cannot be null");
                            else if (airUnit.UnitType != UnitType.AirUnit)
                                errors.Add($"Attached unit {airUnit.UnitID} is not an air unit (Type: {airUnit.UnitType})");
                        }

                        if (_airUnitsAttached.Count > CUConstants.MAX_AIR_UNITS)
                            errors.Add($"Too many air units attached ({_airUnitsAttached.Count} > {CUConstants.MAX_AIR_UNITS})");
                    }

                    if (FacilityType == FacilityType.SupplyDepot)
                    {
                        var maxStockpile = CUConstants.MaxStockpileBySize[DepotSize];
                        if (StockpileInDays > maxStockpile)
                            errors.Add($"Stockpile exceeds maximum for depot size ({StockpileInDays} > {maxStockpile})");

                        if (StockpileInDays < 0)
                            errors.Add("Stockpile cannot be negative");
                    }
                }

                // Validate deployment position constraints
                if (!CanUnitTypeChangeStates() && _deploymentPosition != DeploymentPosition.Deployed)
                {
                    errors.Add($"Unit type {Classification} cannot be in deployment position {_deploymentPosition}");
                }

                // Validate embarkable/mountable consistency
                if (IsEmbarkable && EmbarkedProfileID == WeaponSystems.DEFAULT)
                    errors.Add("Embarkable unit must have valid embarked profile");

                if (IsMountable && MobileProfileID == WeaponSystems.DEFAULT)
                    errors.Add("Mountable unit must have valid mobile profile");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ValidateInternalConsistency", e);
                errors.Add($"Validation error: {e.Message}");
            }

            return errors;
        }

        #endregion // Validation Methods
    }
}