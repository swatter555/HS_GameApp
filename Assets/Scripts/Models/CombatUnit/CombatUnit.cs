using HammerAndSickle.Controllers;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using UnityEngine;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Contains modified combat stats from a CombatUnit's active WeaponProfile.
    /// All values reflect the application of strength, deployment, efficiency, experience, and ICM modifiers.
    /// </summary>
    public struct CombatRatingTotal
    {
        // Ground combat
        public float HardAttack;
        public float HardDefense;
        public float SoftAttack;
        public float SoftDefense;
        public float GroundAirAttack;
        public float GroundAirDefense;

        // Air combat
        public float Dogfighting;
        public float Maneuverability;
        public float TopSpeed;
        public float Survivability;
        public float GroundAttack;
        public float OrdinanceLoad;
        public float Stealth;
    }

    /// <summary>
    /// The main model for all combat units in the game, including ground units, air units, and facilities.
    /// </summary>
    [Serializable]
    public class CombatUnit
    {
        #region Constants

        private const string CLASS_NAME = nameof(CombatUnit);

        #endregion // Constants

        #region Fields

        [JsonInclude] [JsonPropertyName("deploymentPosition")]
        private DeploymentPosition _deploymentPosition = DeploymentPosition.Deployed;

        private List<CombatUnit> _airUnitsAttached = new List<CombatUnit>();
        private List<string> _attachedUnitIDs = new List<string>();

        #endregion // Fields

        #region Properties

        // Identity
        [JsonPropertyName("unitName")] public string UnitName { get; set; }
        [JsonInclude] [JsonPropertyName("unitID")] public string UnitID { get; private set; }
        [JsonInclude] [JsonPropertyName("classification")] public UnitClassification Classification { get; private set; }
        [JsonInclude] [JsonPropertyName("role")] public UnitRole Role { get; private set; }
        [JsonInclude] [JsonPropertyName("side")] public Side Side { get; private set; }
        [JsonInclude] [JsonPropertyName("nationality")] public Nationality Nationality { get; private set; }
        [JsonIgnore] public bool IsBase => IsBaseType(Classification);

        [JsonInclude]
        [JsonPropertyName("regimentProfile")]
        public RegimentProfile RegimentProfile { get; private set; }

        [JsonInclude] [JsonPropertyName("efficiencyLevel")]
        public EfficiencyLevel EfficiencyLevel { get; internal set; }

        // Actions
        [JsonInclude] [JsonPropertyName("moveActions")] public StatsMaxCurrent MoveActions { get; private set; }
        [JsonInclude] [JsonPropertyName("combatActions")] public StatsMaxCurrent CombatActions { get; private set; }
        [JsonInclude] [JsonPropertyName("deploymentActions")] public StatsMaxCurrent DeploymentActions { get; private set; }
        [JsonInclude] [JsonPropertyName("opportunityActions")] public StatsMaxCurrent OpportunityActions { get; private set; }
        [JsonInclude] [JsonPropertyName("intelActions")] public StatsMaxCurrent IntelActions { get; private set; }

        // State
        [JsonInclude] [JsonPropertyName("hitPoints")] public StatsMaxCurrent HitPoints { get; private set; }
        [JsonInclude] [JsonPropertyName("daysSupply")] public StatsMaxCurrent DaysSupply { get; private set; }
        [JsonInclude] [JsonPropertyName("movementPoints")] public StatsMaxCurrent MovementPoints { get; private set; }
        [JsonInclude] [JsonPropertyName("mapPos")] public Position2D MapPos { get; internal set; }
        [JsonInclude] [JsonPropertyName("facing")] public HexDirection Facing { get; set; }
        [JsonInclude] [JsonPropertyName("spottedLevel")] public SpottedLevel SpottedLevel { get; private set; }
        [JsonInclude] [JsonPropertyName("individualCombatModifier")] public float IndividualCombatModifier { get; private set; }

        // Leader
        [JsonInclude] [JsonPropertyName("leaderID")] public string LeaderID { get; internal set; } = string.Empty;
        [JsonIgnore] public bool IsLeaderAssigned => !string.IsNullOrEmpty(LeaderID);
        public Leader GetAssignedLeader() =>
            IsLeaderAssigned ? GameDataManager.Instance.GetLeader(LeaderID) : null;

        // Deployment
        [JsonIgnore] public DeploymentPosition DeploymentPosition => _deploymentPosition;
        [JsonInclude] [JsonPropertyName("isEmbarkable")] public bool IsEmbarkable { get; private set; }
        [JsonInclude] [JsonPropertyName("isMountable")] public bool IsMountable { get; private set; }
        [JsonInclude] [JsonPropertyName("currentEmbarkmentState")] public EmbarkmentState CurrentEmbarkmentState { get; private set; } = EmbarkmentState.NotEmbarked;

        // Experience
        [JsonInclude] [JsonPropertyName("experiencePoints")] public int ExperiencePoints { get; internal set; }
        [JsonInclude] [JsonPropertyName("experienceLevel")] public ExperienceLevel ExperienceLevel { get; internal set; }

        // Facility - common
        [JsonInclude] [JsonPropertyName("attachedUnitIDs")]
        public IReadOnlyList<string> AttachedUnitIDs
        {
            get => _attachedUnitIDs.AsReadOnly();
            private set => _attachedUnitIDs = value?.ToList() ?? new List<string>();
        }
        [JsonInclude] [JsonPropertyName("baseDamage")] public int BaseDamage { get; private set; }
        [JsonInclude] [JsonPropertyName("operationalCapacity")] public OperationalCapacity OperationalCapacity { get; private set; }
        [JsonInclude] [JsonPropertyName("facilityType")] public FacilityType FacilityType { get; private set; }

        // Facility - supply depot
        [JsonInclude] [JsonPropertyName("depotSize")] public DepotSize DepotSize { get; private set; }
        [JsonInclude] [JsonPropertyName("stockpileInDays")] public float StockpileInDays { get; private set; }
        [JsonInclude] [JsonPropertyName("generationRate")] public SupplyGenerationRate GenerationRate { get; private set; }
        [JsonInclude] [JsonPropertyName("supplyProjection")] public SupplyProjection SupplyProjection { get; private set; }
        [JsonInclude] [JsonPropertyName("supplyPenetration")] public bool SupplyPenetration { get; private set; }
        [JsonInclude] [JsonPropertyName("depotCategory")] public DepotCategory DepotCategory { get; private set; }
        [JsonIgnore] public int ProjectionRadius => IsBase ? GameData.ProjectionRangeValues[SupplyProjection] : 0;
        [JsonIgnore] public bool IsMainDepot => IsBase && DepotCategory == DepotCategory.Main;

        // Facility - airbase
        [JsonIgnore] public IReadOnlyList<CombatUnit> AirUnitsAttached { get; private set; }

        // TODO: Add an event that fires on DeploymentPosition changes, to update icon picking.

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// Primary constructor for creating new CombatUnit instances.
        /// </summary>
        public CombatUnit(string unitName,
            UnitClassification classification,
            UnitRole role,
            Side side,
            Nationality nationality,
            bool isMountable = false,
            bool isEmbarkable = false,
            DepotCategory category = DepotCategory.Secondary,
            DepotSize size = DepotSize.Small)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(unitName))
                    throw new ArgumentException("Unit name cannot be null or whitespace", nameof(unitName));

                UnitName = unitName.Trim();
                UnitID = Guid.NewGuid().ToString();
                Classification = classification;
                Role = role;
                Side = side;
                Nationality = nationality;
                Facing = side == Side.Player ? HexDirection.W : HexDirection.E;

                InitializeDeploymentSystem(isEmbarkable, isMountable);
                RegimentProfile = new RegimentProfile();

                if (IsBase)
                    InitializeFacility(category, size);

                InitializeActionCounts();
                SpottedLevel = SpottedLevel.Level1;
                InitializeExperienceSystem();
                HitPoints = new StatsMaxCurrent(GameData.MAX_HP);
                DaysSupply = new StatsMaxCurrent(GameData.MaxDaysSupplyUnit);
                MovementPoints = new StatsMaxCurrent(GameData.FOOT_UNIT);
                EfficiencyLevel = EfficiencyLevel.FullOperations;
                MapPos = Position2D.Zero;
                IndividualCombatModifier = GameData.ICM_DEFAULT;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Parameterless constructor for JSON deserialization.
        /// All [JsonInclude] properties are overwritten by the deserializer after construction.
        /// </summary>
        [JsonConstructor]
        public CombatUnit()
        {
            try
            {
                UnitID = Guid.NewGuid().ToString();
                UnitName = string.Empty;
                Classification = UnitClassification.INF;
                Role = UnitRole.GroundCombat;
                Side = Side.Player;
                Nationality = Nationality.USSR;
                Facing = HexDirection.W;
                RegimentProfile = new RegimentProfile();

                IsEmbarkable = false;
                IsMountable = false;
                _deploymentPosition = DeploymentPosition.Deployed;
                CurrentEmbarkmentState = EmbarkmentState.NotEmbarked;

                EfficiencyLevel = EfficiencyLevel.FullOperations;

                HitPoints = new StatsMaxCurrent(1f);
                DaysSupply = new StatsMaxCurrent(1f);
                MovementPoints = new StatsMaxCurrent(1f);
                MoveActions = new StatsMaxCurrent(1f);
                CombatActions = new StatsMaxCurrent(1f);
                DeploymentActions = new StatsMaxCurrent(1f);
                OpportunityActions = new StatsMaxCurrent(1f);
                IntelActions = new StatsMaxCurrent(1f);

                MapPos = Position2D.Zero;
                SpottedLevel = SpottedLevel.Level1;
                LeaderID = string.Empty;
                IndividualCombatModifier = GameData.ICM_DEFAULT;

                ExperienceLevel = ExperienceLevel.Raw;
                ExperiencePoints = 0;

                BaseDamage = 0;
                OperationalCapacity = OperationalCapacity.Full;
                FacilityType = FacilityType.HQ;
                DepotSize = DepotSize.Small;
                DepotCategory = DepotCategory.Secondary;
                StockpileInDays = 0f;
                GenerationRate = SupplyGenerationRate.Basic;
                SupplyProjection = SupplyProjection.Local;
                SupplyPenetration = false;

                _airUnitsAttached = new List<CombatUnit>();
                _attachedUnitIDs = new List<string>();
                AirUnitsAttached = _airUnitsAttached.AsReadOnly();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "JsonConstructor", e);
                throw;
            }
        }

        /// <summary>
        /// Constructor with regiment profile parameters.
        /// Used by OOB loading, template cloning, and snapshot creation.
        /// </summary>
        public CombatUnit(string unitName,
            UnitClassification classification,
            UnitRole role,
            Side side,
            Nationality nationality,
            RegimentProfileType profileType,
            WeaponType deployedProfile,
            bool isMountable,
            WeaponType mobileProfile,
            bool isEmbarkable,
            WeaponType embarkedProfile,
            DepotCategory category = DepotCategory.Secondary,
            DepotSize size = DepotSize.Small)
            : this(unitName, classification, role, side, nationality, isMountable, isEmbarkable, category, size)
        {
            RegimentProfile.InitializeRegimentProfile(unitName, profileType, mobileProfile, deployedProfile, embarkedProfile);
            InitializeMovementPoints();
        }

        #endregion // Constructors

        #region Core

        public WeaponProfile GetDeployedProfile() => RegimentProfile?.GetDeployedProfile();
        public WeaponProfile GetMobileProfile() => RegimentProfile?.GetMobileProfile();
        public WeaponProfile GetEmbarkedProfile() => RegimentProfile?.GetEmbarkedProfile();

        /// <summary>
        /// Returns the active weapon profile based on current deployment state.
        /// </summary>
        public WeaponProfile GetActiveWeaponProfile() => DeploymentPosition switch
        {
            DeploymentPosition.Embarked => GetEmbarkedProfile() ?? GetDeployedProfile(),
            DeploymentPosition.Mobile => GetMobileProfile() ?? GetDeployedProfile(),
            _ => GetDeployedProfile()
        };

        /// <summary>
        /// Refreshes all action counts to maximum. Called at turn start.
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
        /// Refreshes movement points to maximum. Called at turn start.
        /// </summary>
        public void RefreshMovementPoints() => MovementPoints.ResetToMax();

        public void SetSpottedLevel(SpottedLevel spottedLevel) => SpottedLevel = spottedLevel;

        /// <summary>
        /// Applies damage to the unit, reducing hit points.
        /// </summary>
        public void TakeDamage(float damage)
        {
            try
            {
                if (damage < 0f)
                    throw new ArgumentException("Damage cannot be negative", nameof(damage));
                if (damage == 0f)
                    return;

                float newHitPoints = Mathf.Max(0f, HitPoints.Current - damage);
                HitPoints.SetCurrent(newHitPoints);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(TakeDamage), e);
                throw;
            }
        }

        /// <summary>
        /// Repairs damage to the unit, restoring hit points.
        /// </summary>
        public void Repair(float repairAmount)
        {
            try
            {
                if (repairAmount < 0f)
                    throw new ArgumentException("Repair amount cannot be negative", nameof(repairAmount));
                if (repairAmount == 0f)
                    return;

                float newHitPoints = Mathf.Min(HitPoints.Max, HitPoints.Current + repairAmount);
                HitPoints.SetCurrent(newHitPoints);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Repair), e);
                throw;
            }
        }

        /// <summary>
        /// Consumes supplies for unit operations.
        /// </summary>
        public bool ConsumeSupplies(float amount)
        {
            try
            {
                if (amount < 0f)
                    throw new ArgumentException("Supply amount cannot be negative", nameof(amount));
                if (amount == 0f)
                    return true;

                if (DaysSupply.Current >= amount)
                {
                    DaysSupply.SetCurrent(DaysSupply.Current - amount);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ConsumeSupplies), e);
                return false;
            }
        }

        /// <summary>
        /// Receives supplies from external source. Returns actual amount received.
        /// </summary>
        public float ReceiveSupplies(float amount)
        {
            try
            {
                if (amount <= 0f) return 0f;

                float availableCapacity = DaysSupply.Max - DaysSupply.Current;
                float actualAmount = Mathf.Min(amount, availableCapacity);
                DaysSupply.SetCurrent(DaysSupply.Current + actualAmount);
                return actualAmount;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ReceiveSupplies), e);
                return 0f;
            }
        }

        public bool IsDestroyed() => HitPoints.Current < GameData.MIN_HP;

        /// <summary>
        /// Checks if the unit can move based on HP, supply, and efficiency.
        /// </summary>
        public bool CanMove()
        {
            if (IsDestroyed()) return false;
            if (DaysSupply.Current < 1f) return false;
            if (EfficiencyLevel == EfficiencyLevel.StaticOperations) return false;
            return true;
        }

        public float GetSupplyStatus() =>
            DaysSupply.Max > 0f ? DaysSupply.Current / DaysSupply.Max : 0f;

        /// <summary>
        /// Sets the efficiency level for the unit.
        /// </summary>
        public void SetEfficiencyLevel(EfficiencyLevel level)
        {
            if (!Enum.IsDefined(typeof(EfficiencyLevel), level))
                throw new ArgumentOutOfRangeException(nameof(level), "Invalid efficiency level");
            EfficiencyLevel = level;
        }

        public void DecreaseEfficiencyLevelBy1()
        {
            if (EfficiencyLevel > EfficiencyLevel.StaticOperations)
                EfficiencyLevel--;
        }

        public void IncreaseEfficiencyLevelBy1()
        {
            if (EfficiencyLevel < EfficiencyLevel.FullOperations)
                EfficiencyLevel++;
        }

        public bool IsBaseType(UnitClassification classification) =>
            classification == UnitClassification.HQ ||
            classification == UnitClassification.DEPOT ||
            classification == UnitClassification.AIRB;

        /// <summary>
        /// Sets the unit ID. Used for snapshot restoration to preserve ID consistency.
        /// </summary>
        public void SetUnitID(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId))
                throw new ArgumentException("Unit ID cannot be null or empty", nameof(unitId));
            UnitID = unitId.Trim();
        }

        /// <summary>
        /// Sets the Individual Combat Modifier (ICM).
        /// </summary>
        public void SetICM(float icm)
        {
            if (icm < GameData.ICM_MIN || icm > GameData.ICM_MAX)
                throw new ArgumentOutOfRangeException(nameof(icm),
                    $"ICM must be between {GameData.ICM_MIN} and {GameData.ICM_MAX}");
            IndividualCombatModifier = icm;
        }

        public void SetNationality(Nationality nationality) => Nationality = nationality;

        /// <summary>
        /// Sets unit side and updates facing direction accordingly.
        /// </summary>
        public void SetSide(Side side)
        {
            Side = side;
            Facing = side == Side.Player ? HexDirection.W : HexDirection.E;
        }

        public void SetRole(UnitRole role) => Role = role;
        public void SetPosition(Position2D pos) => MapPos = pos;

        #endregion // Core

        #region Initialization

        private void InitializeDeploymentSystem(bool embarkable, bool mountable)
        {
            IsEmbarkable = embarkable;
            IsMountable = mountable;
            _deploymentPosition = DeploymentPosition.Deployed;
            CurrentEmbarkmentState = EmbarkmentState.NotEmbarked;
        }

        private void InitializeExperienceSystem()
        {
            ExperiencePoints = 0;
            ExperienceLevel = ExperienceLevel.Raw;
        }

        private void InitializeActionCounts()
        {
            int moveActions = GameData.DEFAULT_MOVE_ACTIONS;
            int combatActions = GameData.DEFAULT_COMBAT_ACTIONS;
            int deploymentActions = GameData.DEFAULT_DEPLOYMENT_ACTIONS;
            int opportunityActions = GameData.DEFAULT_OPPORTUNITY_ACTIONS;
            int intelActions = GameData.DEFAULT_INTEL_ACTIONS;

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
                case UnitClassification.FGT:
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

            MoveActions = new StatsMaxCurrent(moveActions);
            CombatActions = new StatsMaxCurrent(combatActions);
            DeploymentActions = new StatsMaxCurrent(deploymentActions);
            OpportunityActions = new StatsMaxCurrent(opportunityActions);
            IntelActions = new StatsMaxCurrent(intelActions);
        }

        private void InitializeMovementPoints()
        {
            try
            {
                var deployedProfile = GetDeployedProfile();
                if (deployedProfile == null)
                    throw new InvalidOperationException("Unit must have a valid deployed profile");

                MovementPoints = new StatsMaxCurrent(deployedProfile.MaxMovementPoints);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(InitializeMovementPoints), e);
                MovementPoints = new StatsMaxCurrent(GameData.FOOT_UNIT);
            }
        }

        #endregion // Initialization

        #region Combat Rating

        private float GetFinalCombatRatingModifier()
        {
            try
            {
                float modifier = GetStrengthModifier() *
                                 GetEfficiencyModifier() *
                                 GetExperienceMultiplier() *
                                 IndividualCombatModifier;

                // Air units (not helos) skip deployment state modifier
                if (!IsAirUnitClassification(Classification))
                    modifier *= GetCombatStateModifier();

                return modifier;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetFinalCombatRatingModifier), e);
                return 1.0f;
            }
        }

        private float GetStrengthModifier()
        {
            if (HitPoints.Current >= HitPoints.Max * GameData.FULL_STRENGTH_FLOOR)
                return GameData.STRENGTH_MOD_FULL;
            if (HitPoints.Current >= HitPoints.Max * GameData.DEPLETED_STRENGTH_FLOOR)
                return GameData.STRENGTH_MOD_DEPLETED;
            return GameData.STRENGTH_MOD_LOW;
        }

        private float GetCombatStateModifier() => DeploymentPosition switch
        {
            DeploymentPosition.HastyDefense => GameData.COMBAT_MOD_HASTY_DEFENSE,
            DeploymentPosition.Entrenched => GameData.COMBAT_MOD_ENTRENCHED,
            DeploymentPosition.Fortified => GameData.COMBAT_MOD_FORTIFIED,
            _ => 1.0f,
        };

        private float GetEfficiencyModifier() => EfficiencyLevel switch
        {
            EfficiencyLevel.FullOperations => GameData.EFFICIENCY_MOD_PEAK,
            EfficiencyLevel.CombatOperations => GameData.EFFICIENCY_MOD_FULL,
            EfficiencyLevel.NormalOperations => GameData.EFFICIENCY_MOD_OPERATIONAL,
            EfficiencyLevel.DegradedOperations => GameData.EFFICIENCY_MOD_DEGRADED,
            _ => GameData.EFFICIENCY_MOD_STATIC,
        };

        /// <summary>
        /// Returns the active WeaponProfile's combat stats modified by all combat rating modifiers.
        /// </summary>
        public CombatRatingTotal GetCombatRatingTotal()
        {
            try
            {
                var profile = GetActiveWeaponProfile();
                if (profile == null)
                    throw new InvalidOperationException("No active weapon system profile available");

                float modifier = GetFinalCombatRatingModifier();

                return new CombatRatingTotal
                {
                    HardAttack = profile.HardAttack * modifier,
                    HardDefense = profile.HardDefense * modifier,
                    SoftAttack = profile.SoftAttack * modifier,
                    SoftDefense = profile.SoftDefense * modifier,
                    GroundAirAttack = profile.GroundAirAttack * modifier,
                    GroundAirDefense = profile.GroundAirDefense * modifier,
                    Dogfighting = profile.Dogfighting * modifier,
                    Maneuverability = profile.Maneuverability * modifier,
                    TopSpeed = profile.TopSpeed * modifier,
                    Survivability = profile.Survivability * modifier,
                    GroundAttack = profile.GroundAttack * modifier,
                    OrdinanceLoad = profile.OrdinanceLoad * modifier,
                    Stealth = profile.Stealth * modifier
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetCombatRatingTotal), e);
                throw;
            }
        }

        #endregion // Combat Rating

        #region Actions

        /// <summary>
        /// Consumes actions, movement points, and supplies to perform a combat action.
        /// </summary>
        public bool PerformCombatAction()
        {
            try
            {
                if (CombatActions.Current >= 1 &&
                    MovementPoints.Current >= GetCombatMovementCost() &&
                    DaysSupply.Current >= GameData.COMBAT_ACTION_SUPPLY_THRESHOLD &&
                    !IsBase)
                {
                    CombatActions.DecrementCurrent();
                    ConsumeMovementPoints(GetCombatMovementCost());
                    ConsumeSupplies(GameData.COMBAT_ACTION_SUPPLY_COST);
                    return true;
                }

                AppService.CaptureUiMessage($"{UnitName} does not have enough combat actions, movement points, or supplies to perform a combat action.");
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(PerformCombatAction), e);
                return false;
            }
        }

        /// <summary>
        /// Consumes the required actions, movement points, and supplies to perform a move action.
        /// </summary>
        public bool PerformMoveAction(int movtCost)
        {
            try
            {
                if (MoveActions.Current >= 1 &&
                    MovementPoints.Current >= movtCost &&
                    DaysSupply.Current >= (movtCost * GameData.MOVE_ACTION_SUPPLY_COST) + GameData.MOVE_ACTION_SUPPLY_THRESHOLD &&
                    !IsBase)
                {
                    MoveActions.DecrementCurrent();
                    ConsumeMovementPoints(movtCost);
                    ConsumeSupplies(movtCost * GameData.MOVE_ACTION_SUPPLY_COST);
                    return true;
                }

                AppService.CaptureUiMessage($"{UnitName} does not have enough move actions, movement points, or supplies to perform a move action.");
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(PerformMoveAction), e);
                return false;
            }
        }

        /// <summary>
        /// Consumes the required actions, movement points, and supplies to perform an intel action.
        /// </summary>
        public bool PerformIntelAction()
        {
            try
            {
                if (IntelActions.Current >= 1 &&
                    MovementPoints.Current >= GetIntelMovementCost() &&
                    DaysSupply.Current >= GameData.INTEL_ACTION_SUPPLY_COST)
                {
                    IntelActions.DecrementCurrent();
                    ConsumeMovementPoints(GetIntelMovementCost());
                    ConsumeSupplies(GameData.INTEL_ACTION_SUPPLY_COST);
                    return true;
                }

                AppService.CaptureUiMessage($"{UnitName} does not have enough intel actions, movement points, or supplies to perform an intel action.");
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(PerformIntelAction), e);
                return false;
            }
        }

        /// <summary>
        /// Consumes the required actions and supplies to perform an opportunity action.
        /// </summary>
        public bool PerformOpportunityAction()
        {
            try
            {
                if (OpportunityActions.Current >= 1 &&
                    DaysSupply.Current >= GameData.OPPORTUNITY_ACTION_SUPPLY_COST + GameData.OPPORTUNITY_ACTION_SUPPLY_THRESHOLD &&
                    !IsBase)
                {
                    OpportunityActions.DecrementCurrent();
                    ConsumeSupplies(GameData.OPPORTUNITY_ACTION_SUPPLY_COST);
                    return true;
                }

                AppService.CaptureUiMessage($"{UnitName} does not have enough opportunity actions to perform an opportunity action.");
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(PerformOpportunityAction), e);
                return false;
            }
        }

        /// <summary>
        /// Returns a dictionary of available action counts after validating prerequisites.
        /// </summary>
        public Dictionary<ActionTypes, float> GetAvailableActions()
        {
            float moveAvailable = (MoveActions.Current >= 1 && MovementPoints.Current > 0f)
                ? MoveActions.Current : 0f;
            float combatAvailable = CombatActions.Current >= 1 ? CombatActions.Current : 0f;
            float opportunityAvailable = OpportunityActions.Current;
            float intelAvailable = IntelActions.Current >= 1 ? IntelActions.Current : 0f;
            float deploymentAvailable = IsBase ? 0f :
                (MovementPoints.Current >= GetDeployMovementCost() && DeploymentActions.Current >= 1)
                    ? DeploymentActions.Current : 0f;

            return new Dictionary<ActionTypes, float>
            {
                [ActionTypes.MoveAction] = moveAvailable,
                [ActionTypes.CombatAction] = combatAvailable,
                [ActionTypes.DeployAction] = deploymentAvailable,
                [ActionTypes.OpportunityAction] = opportunityAvailable,
                [ActionTypes.IntelAction] = intelAvailable
            };
        }

        public float GetDeployActions() =>
            !CanUnitTypeChangeStates() ? 0 :
            MovementPoints.Current >= GetDeployMovementCost() ? DeploymentActions.Current : 0f;

        public float GetCombatActions() =>
            IsBase ? 0 :
            MovementPoints.Current >= GetCombatMovementCost() ? CombatActions.Current : 0;

        public float GetMoveActions() =>
            IsBase ? 0 :
            MovementPoints.Current > 0 ? MoveActions.Current : 0;

        public float GetOpportunityActions() => IsBase ? 0 : OpportunityActions.Current;

        public float GetIntelActions() =>
            MovementPoints.Current >= GetIntelMovementCost() ? IntelActions.Current : 0;

        private bool ConsumeMovementPoints(float points)
        {
            if (points <= 0f) return true;
            if (MovementPoints.Current >= points)
            {
                MovementPoints.SetCurrent(MovementPoints.Current - points);
                return true;
            }
            return false;
        }

        private float GetDeployMovementCost() =>
            Mathf.CeilToInt(MovementPoints.Max * GameData.DEPLOYMENT_ACTION_MOVEMENT_COST);

        private float GetCombatMovementCost() =>
            Mathf.CeilToInt(MovementPoints.Max * GameData.COMBAT_ACTION_MOVEMENT_COST);

        private float GetIntelMovementCost() =>
            Mathf.CeilToInt(MovementPoints.Max * GameData.INTEL_ACTION_MOVEMENT_COST);

        #endregion // Actions

        #region Deployment

        /// <summary>
        /// Attempt to change deployment state to a higher level (towards Embarked).
        /// </summary>
        public bool TryDeployUP(out string errorMsg, bool onAirbase = false, bool onPort = false)
        {
            errorMsg = string.Empty;

            if (MovementPoints.Max <= 0f)
            {
                errorMsg = "Unit has invalid movement profile; cannot deploy.";
                return false;
            }

            DeploymentPosition oldPosition = _deploymentPosition;
            DeploymentPosition targetPosition = _deploymentPosition + 1;

            if (!CanChangeToState(targetPosition, out errorMsg))
                return false;

            if (!SpecialEmbarkmentChecks(out errorMsg, targetPosition, onAirbase, onPort))
                return false;

            // Fortified/Entrenched skip directly to Deployed
            if (oldPosition == DeploymentPosition.Fortified || oldPosition == DeploymentPosition.Entrenched)
                _deploymentPosition = DeploymentPosition.Deployed;
            else
                _deploymentPosition = targetPosition;

            ApplyDeploymentTransitionCosts();
            return true;
        }

        /// <summary>
        /// Attempt to change deployment state to a lower level (more defensive).
        /// </summary>
        public bool TryDeployDOWN(out string errorMsg, bool isBeachhead = false)
        {
            errorMsg = string.Empty;

            if (MovementPoints.Max <= 0f)
            {
                errorMsg = "Unit has invalid movement profile; cannot deploy.";
                return false;
            }

            if (DeploymentPosition == DeploymentPosition.Fortified)
            {
                errorMsg = $"{UnitName} is already at minimum deployment level (Fortified).";
                return false;
            }

            DeploymentPosition targetPosition = GetDownwardTargetPosition(_deploymentPosition);

            if (!CanChangeToState(targetPosition, out errorMsg))
                return false;

            _deploymentPosition = targetPosition;
            ApplyDeploymentTransitionCosts();
            return true;
        }

        private void ApplyDeploymentTransitionCosts()
        {
            ConsumeSupplies(GameData.COMBAT_STATE_SUPPLY_TRANSITION_COST);
            DeploymentActions.DecrementCurrent();

            // Subtract 50% of pre-transition max MP from current MP
            float movementPenalty = GameData.DEPLOYMENT_ACTION_MOVEMENT_COST * MovementPoints.Max;
            float remainingMP = Mathf.Max(0f, MovementPoints.Current - movementPenalty);

            // Set new max from the now-active profile, clamp leftover to new max
            UpdateMovementPointsForProfile();
            MovementPoints.SetCurrent(Mathf.Min(remainingMP, MovementPoints.Max));
        }

        private bool SpecialEmbarkmentChecks(out string errorMsg, DeploymentPosition targetPos,
            bool onAirbase, bool onPort)
        {
            errorMsg = string.Empty;

            if (targetPos != DeploymentPosition.Embarked)
                return true;

            if (!IsEmbarkable)
            {
                errorMsg = $"{UnitName} is not configured as embarkable.";
                AppService.HandleException(CLASS_NAME, nameof(SpecialEmbarkmentChecks),
                    new InvalidOperationException(errorMsg));
                return false;
            }

            var embarkedProfile = GetEmbarkedProfile();
            if (embarkedProfile == null)
            {
                errorMsg = $"{UnitName} has no embarked profile and cannot deploy to Embarked position.";
                return false;
            }

            // Airborne units must be on an airbase
            if ((Classification == UnitClassification.AB || Classification == UnitClassification.MAB) && !onAirbase)
            {
                errorMsg = $"{UnitName} must be on an airbase to deploy to Embarked position.";
                return false;
            }

            // Special forces with aircraft transport must be on an airbase
            if (Classification == UnitClassification.SPECF &&
                embarkedProfile.WeaponType == WeaponType.TRN_AN8_SV && !onAirbase)
            {
                errorMsg = $"{UnitName} must be on an airbase to deploy to Embarked position with AN-12 transport.";
                return false;
            }

            // Marines must be on a port
            if ((Classification == UnitClassification.MAR || Classification == UnitClassification.MMAR) && !onPort)
            {
                errorMsg = $"{UnitName} must be on a port to deploy to Embarked position.";
                return false;
            }

            // Airmobile units require helicopter transport profile
            if ((Classification == UnitClassification.AM || Classification == UnitClassification.MAM) &&
                embarkedProfile.UpgradePath != UpgradePath.HELT)
            {
                errorMsg = $"{UnitName} must have a valid helicopter transport profile (TRNHELO) to deploy to Embarked position.";
                return false;
            }

            return true;
        }

        private void UpdateMovementPointsForProfile()
        {
            try
            {
                var activeProfile = GetActiveWeaponProfile();
                if (activeProfile == null)
                    throw new InvalidOperationException("No active weapon system profile available");

                MovementPoints.SetMax(activeProfile.MaxMovementPoints);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(UpdateMovementPointsForProfile), e);
            }
        }

        private bool CanChangeToState(DeploymentPosition targetState, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (DeploymentPosition == targetState)
            {
                errorMessage = $"Already in target state {targetState}";
                return false;
            }

            if (IsDestroyed())
            {
                errorMessage = $"{UnitName} is destroyed and cannot change states";
                throw new InvalidOperationException(errorMessage);
            }

            if (!CanUnitTypeChangeStates())
            {
                errorMessage = $"{UnitName} cannot change combat states (unit type: {Classification})";
                return false;
            }

            if (DaysSupply.Current <= GameData.CRITICAL_SUPPLY_THRESHOLD)
            {
                errorMessage = $"Cannot change state with critical supply levels ({DaysSupply.Current:F1} days remaining)";
                return false;
            }

            if (EfficiencyLevel == EfficiencyLevel.StaticOperations)
            {
                if (DeploymentPosition == DeploymentPosition.Fortified ||
                    DeploymentPosition == DeploymentPosition.Entrenched ||
                    DeploymentPosition == DeploymentPosition.HastyDefense)
                {
                    errorMessage = $"Cannot change from defensive states in Static Operations (current efficiency: {EfficiencyLevel})";
                    return false;
                }

                if (targetState == DeploymentPosition.Mobile)
                {
                    errorMessage = $"Cannot change to Mobile state in Static Operations (current efficiency: {EfficiencyLevel})";
                    return false;
                }
            }

            if (MovementPoints.Current < GameData.DEPLOYMENT_ACTION_MOVEMENT_COST * MovementPoints.Max)
            {
                errorMessage = $"{UnitName} does not have enough movement points to change states ({MovementPoints.Current:F1} available, {GameData.DEPLOYMENT_ACTION_MOVEMENT_COST} required)";
                return false;
            }

            return true;
        }

        private bool CanUnitTypeChangeStates()
        {
            if (Classification == UnitClassification.FGT ||
                Classification == UnitClassification.ATT ||
                Classification == UnitClassification.BMB ||
                Classification == UnitClassification.RECONA)
                return false;

            if (Classification == UnitClassification.HQ ||
                Classification == UnitClassification.DEPOT ||
                Classification == UnitClassification.AIRB)
                return false;

            return true;
        }

        // Embarked always goes directly to Deployed, bypassing Mobile.
        private DeploymentPosition GetDownwardTargetPosition(DeploymentPosition currentPosition) =>
            currentPosition == DeploymentPosition.Embarked
                ? DeploymentPosition.Deployed
                : currentPosition - 1;

        public void SetDeploymentPosition(DeploymentPosition newPosition) => _deploymentPosition = newPosition;
        public void SetCurrentEmbarkmentState(EmbarkmentState state) => CurrentEmbarkmentState = state;

        #endregion // Deployment

        #region Experience

        /// <summary>
        /// Adds experience points to the unit. Capped per action and at Elite level.
        /// </summary>
        public bool AddExperience(int points)
        {
            try
            {
                if (points <= 0) return false;

                if (points > GameData.MAX_EXP_GAIN_PER_ACTION)
                    points = GameData.MAX_EXP_GAIN_PER_ACTION;

                ExperiencePoints += points;

                if (ExperiencePoints > (int)ExperiencePointLevels.Elite)
                    ExperiencePoints = (int)ExperiencePointLevels.Elite;

                var previousLevel = ExperienceLevel;
                var newLevel = CalculateExperienceLevel(ExperiencePoints);

                if (newLevel != previousLevel)
                    ExperienceLevel = newLevel;

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AddExperience), e);
                return false;
            }
        }

        /// <summary>
        /// Sets the unit's cumulative XP and synchronizes ExperienceLevel. Values are clamped.
        /// </summary>
        public int SetExperience(int points)
        {
            try
            {
                int clamped = Math.Clamp(points, 0, (int)ExperiencePointLevels.Elite);
                if (clamped == ExperiencePoints) return clamped;

                ExperiencePoints = clamped;
                ExperienceLevel = CalculateExperienceLevel(clamped);
                return clamped;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetExperience), ex);
                return ExperiencePoints;
            }
        }

        /// <summary>
        /// Sets the experience level and updates XP to the minimum for that level.
        /// </summary>
        public void SetExperienceLevel(ExperienceLevel level)
        {
            if (level == ExperienceLevel) return;

            if (level < ExperienceLevel.Raw || level > ExperienceLevel.Elite)
                throw new ArgumentOutOfRangeException(nameof(level), "Invalid experience level");

            ExperiencePoints = GetMinPointsForLevel(level);
            ExperienceLevel = level;
        }

        /// <summary>
        /// Gets XP needed for the next level. Returns 0 if at Elite.
        /// </summary>
        public int GetPointsToNextLevel() => ExperienceLevel switch
        {
            ExperienceLevel.Raw => (int)ExperiencePointLevels.Green - ExperiencePoints,
            ExperienceLevel.Green => (int)ExperiencePointLevels.Trained - ExperiencePoints,
            ExperienceLevel.Trained => (int)ExperiencePointLevels.Experienced - ExperiencePoints,
            ExperienceLevel.Experienced => (int)ExperiencePointLevels.Veteran - ExperiencePoints,
            ExperienceLevel.Veteran => (int)ExperiencePointLevels.Elite - ExperiencePoints,
            _ => 0,
        };

        /// <summary>
        /// Gets experience progress as a percentage towards next level (0.0 to 1.0).
        /// </summary>
        public float GetExperienceProgress()
        {
            if (ExperienceLevel == ExperienceLevel.Elite) return 1.0f;

            int currentLevelMin = GetMinPointsForLevel(ExperienceLevel);
            int nextLevelMin = GetMinPointsForLevel(GetNextLevel(ExperienceLevel));

            if (nextLevelMin == currentLevelMin) return 1.0f;

            float progress = (float)(ExperiencePoints - currentLevelMin) / (nextLevelMin - currentLevelMin);
            return Mathf.Clamp01(progress);
        }

        private ExperienceLevel CalculateExperienceLevel(int totalPoints)
        {
            if (totalPoints >= (int)ExperiencePointLevels.Elite) return ExperienceLevel.Elite;
            if (totalPoints >= (int)ExperiencePointLevels.Veteran) return ExperienceLevel.Veteran;
            if (totalPoints >= (int)ExperiencePointLevels.Experienced) return ExperienceLevel.Experienced;
            if (totalPoints >= (int)ExperiencePointLevels.Trained) return ExperienceLevel.Trained;
            if (totalPoints >= (int)ExperiencePointLevels.Green) return ExperienceLevel.Green;
            return ExperienceLevel.Raw;
        }

        private int GetMinPointsForLevel(ExperienceLevel level) => level switch
        {
            ExperienceLevel.Raw => (int)ExperiencePointLevels.Raw,
            ExperienceLevel.Green => (int)ExperiencePointLevels.Green,
            ExperienceLevel.Trained => (int)ExperiencePointLevels.Trained,
            ExperienceLevel.Experienced => (int)ExperiencePointLevels.Experienced,
            ExperienceLevel.Veteran => (int)ExperiencePointLevels.Veteran,
            ExperienceLevel.Elite => (int)ExperiencePointLevels.Elite,
            _ => 0,
        };

        private ExperienceLevel GetNextLevel(ExperienceLevel currentLevel) => currentLevel switch
        {
            ExperienceLevel.Raw => ExperienceLevel.Green,
            ExperienceLevel.Green => ExperienceLevel.Trained,
            ExperienceLevel.Trained => ExperienceLevel.Experienced,
            ExperienceLevel.Experienced => ExperienceLevel.Veteran,
            ExperienceLevel.Veteran => ExperienceLevel.Elite,
            _ => ExperienceLevel.Elite,
        };

        private float GetExperienceMultiplier() => ExperienceLevel switch
        {
            ExperienceLevel.Raw => GameData.RAW_XP_MODIFIER,
            ExperienceLevel.Green => GameData.GREEN_XP_MODIFIER,
            ExperienceLevel.Trained => GameData.TRAINED_XP_MODIFIER,
            ExperienceLevel.Experienced => GameData.EXPERIENCED_XP_MODIFIER,
            ExperienceLevel.Veteran => GameData.VETERAN_XP_MODIFIER,
            ExperienceLevel.Elite => GameData.ELITE_XP_MODIFIER,
            _ => 1.0f,
        };

        #endregion // Experience

        #region Facility

        private void InitializeFacility(DepotCategory category = DepotCategory.Secondary, DepotSize size = DepotSize.Small)
        {
            try
            {
                if (!IsBase) return;

                BaseDamage = 0;
                OperationalCapacity = OperationalCapacity.Full;
                SupplyPenetration = false;
                AirUnitsAttached = _airUnitsAttached.AsReadOnly();

                switch (Classification)
                {
                    case UnitClassification.HQ:
                        FacilityType = FacilityType.HQ;
                        break;
                    case UnitClassification.DEPOT:
                        FacilityType = FacilityType.SupplyDepot;
                        DepotCategory = category;
                        SetDepotSize(size);
                        break;
                    case UnitClassification.AIRB:
                        FacilityType = FacilityType.Airbase;
                        break;
                    default:
                        throw new ArgumentException($"Unit classification {Classification} is not a valid base type");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(InitializeFacility), e);
                throw;
            }
        }

        /// <summary>
        /// Applies damage to the facility, reducing operational capacity.
        /// </summary>
        public void AddFacilityDamage(int incomingDamage)
        {
            try
            {
                if (!IsBase)
                    throw new InvalidOperationException("Cannot add facility damage to non-base units");
                if (incomingDamage < 0)
                    throw new ArgumentException("Incoming damage cannot be negative", nameof(incomingDamage));

                int newDamage = BaseDamage + incomingDamage;
                BaseDamage = Math.Max(GameData.MIN_DAMAGE, Math.Min(GameData.MAX_DAMAGE, newDamage));
                UpdateOperationalCapacity();

                AppService.CaptureUiMessage($"{UnitName} has suffered {incomingDamage} facility damage. Current damage level: {BaseDamage}.");
                AppService.CaptureUiMessage($"{UnitName} current operational capacity is: {OperationalCapacity}");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AddFacilityDamage), e);
                throw;
            }
        }

        /// <summary>
        /// Repairs facility damage, restoring operational capacity.
        /// </summary>
        public void RepairFacilityDamage(int repairAmount)
        {
            try
            {
                if (!IsBase)
                    throw new InvalidOperationException("Cannot repair facility damage on non-base units");
                if (repairAmount < 0)
                    throw new ArgumentException("Repair amount cannot be negative", nameof(repairAmount));

                repairAmount = Math.Max(0, Math.Min(GameData.MAX_DAMAGE, repairAmount));
                BaseDamage -= repairAmount;
                BaseDamage = Math.Max(GameData.MIN_DAMAGE, Math.Min(GameData.MAX_DAMAGE, BaseDamage));
                UpdateOperationalCapacity();

                AppService.CaptureUiMessage($"{UnitName} has been repaired by {repairAmount}. Current damage level: {BaseDamage}.");
                AppService.CaptureUiMessage($"{UnitName} current operational capacity is: {OperationalCapacity}");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RepairFacilityDamage), e);
                throw;
            }
        }

        /// <summary>
        /// Sets facility damage to a specific level (0-100).
        /// </summary>
        public void SetFacilityDamage(int newDamageLevel)
        {
            try
            {
                if (!IsBase)
                    throw new InvalidOperationException("Cannot set facility damage on non-base units");
                if (newDamageLevel < GameData.MIN_DAMAGE || newDamageLevel > GameData.MAX_DAMAGE)
                    throw new ArgumentOutOfRangeException(nameof(newDamageLevel),
                        $"Damage level must be between {GameData.MIN_DAMAGE} and {GameData.MAX_DAMAGE}");

                BaseDamage = newDamageLevel;
                UpdateOperationalCapacity();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetFacilityDamage), e);
                throw;
            }
        }

        public float GetFacilityEfficiencyMultiplier()
        {
            if (!IsBase) return 0.0f;
            return OperationalCapacity switch
            {
                OperationalCapacity.Full => GameData.BASE_CAPACITY_LVL5,
                OperationalCapacity.SlightlyDegraded => GameData.BASE_CAPACITY_LVL4,
                OperationalCapacity.ModeratelyDegraded => GameData.BASE_CAPACITY_LVL3,
                OperationalCapacity.HeavilyDegraded => GameData.BASE_CAPACITY_LVL2,
                OperationalCapacity.OutOfOperation => GameData.BASE_CAPACITY_LVL1,
                _ => 0.0f,
            };
        }

        public bool IsFacilityOperational() =>
            IsBase && OperationalCapacity != OperationalCapacity.OutOfOperation;

        private void UpdateOperationalCapacity()
        {
            if (BaseDamage >= 81)
                OperationalCapacity = OperationalCapacity.OutOfOperation;
            else if (BaseDamage >= 61)
                OperationalCapacity = OperationalCapacity.HeavilyDegraded;
            else if (BaseDamage >= 41)
                OperationalCapacity = OperationalCapacity.ModeratelyDegraded;
            else if (BaseDamage >= 21)
                OperationalCapacity = OperationalCapacity.SlightlyDegraded;
            else
                OperationalCapacity = OperationalCapacity.Full;
        }

        #endregion // Facility

        #region Airbase Management

        /// <summary>
        /// Attaches an air unit to this airbase.
        /// </summary>
        public bool AddAirUnit(CombatUnit unit)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.Airbase)
                    throw new InvalidOperationException("Cannot add air units to non-airbase facilities");
                if (unit == null)
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");

                if (_airUnitsAttached.Count >= GameData.MAX_AIR_UNITS)
                {
                    AppService.CaptureUiMessage($"{UnitName} is already at maximum air unit capacity.");
                    return false;
                }

                if (!IsAirUnitClassification(unit.Classification))
                    throw new InvalidOperationException($"Only air units can be attached to an airbase. {unit.UnitName} is {unit.Classification}");

                if (_airUnitsAttached.Contains(unit) || _attachedUnitIDs.Contains(unit.UnitID))
                {
                    AppService.CaptureUiMessage($"{unit.UnitName} is already attached to this airbase");
                    return false;
                }

                _airUnitsAttached.Add(unit);
                _attachedUnitIDs.Add(unit.UnitID);

                AppService.CaptureUiMessage($"{unit.UnitName} has been attached to {UnitName}.");
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AddAirUnit), e);
                return false;
            }
        }

        /// <summary>
        /// Removes an air unit from this airbase.
        /// </summary>
        public bool RemoveAirUnit(CombatUnit unit)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.Airbase)
                    throw new InvalidOperationException("Cannot remove air units from non-airbase facilities");
                if (unit == null)
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");

                bool removedFromList = _airUnitsAttached.Remove(unit);
                bool removedFromIds = _attachedUnitIDs.Remove(unit.UnitID);

                if (removedFromList || removedFromIds)
                {
                    AppService.CaptureUiMessage($"Unit {unit.UnitName} has been removed from {UnitName}.");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RemoveAirUnit), e);
                return false;
            }
        }

        /// <summary>
        /// Removes an air unit by ID from this airbase.
        /// </summary>
        public bool RemoveAirUnitByID(string unitID)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.Airbase)
                    throw new InvalidOperationException("Cannot remove air units from non-airbase facilities");
                if (string.IsNullOrEmpty(unitID))
                    throw new ArgumentException("Unit ID cannot be null or empty", nameof(unitID));

                var unit = _airUnitsAttached.FirstOrDefault(u => u.UnitID == unitID);

                bool removedFromIds = _attachedUnitIDs.Remove(unitID);
                bool removedFromList = unit != null && _airUnitsAttached.Remove(unit);

                if (removedFromList || removedFromIds)
                {
                    string unitName = unit?.UnitName ?? unitID;
                    AppService.CaptureUiMessage($"Unit {unitName} has been removed from {UnitName}.");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RemoveAirUnitByID), e);
                return false;
            }
        }

        public CombatUnit GetAirUnitByID(string unitID)
        {
            if (!IsBase || FacilityType != FacilityType.Airbase || string.IsNullOrEmpty(unitID))
                return null;
            return _airUnitsAttached.FirstOrDefault(u => u.UnitID == unitID);
        }

        public int GetAttachedAirUnitCount() =>
            IsBase && FacilityType == FacilityType.Airbase ? _airUnitsAttached.Count : 0;

        public int GetAirUnitCapacity() =>
            IsBase && FacilityType == FacilityType.Airbase
                ? GameData.MAX_AIR_UNITS - _airUnitsAttached.Count : 0;

        public bool HasAirUnit(CombatUnit unit) =>
            IsBase && FacilityType == FacilityType.Airbase && unit != null && _airUnitsAttached.Contains(unit);

        public bool HasAirUnitByID(string unitID) =>
            IsBase && FacilityType == FacilityType.Airbase &&
            !string.IsNullOrEmpty(unitID) && _attachedUnitIDs.Contains(unitID);

        /// <summary>
        /// Removes all air units from this airbase.
        /// </summary>
        public void ClearAllAirUnits()
        {
            try
            {
                if (IsBase && FacilityType == FacilityType.Airbase)
                {
                    int count = _airUnitsAttached.Count;
                    _airUnitsAttached.Clear();
                    _attachedUnitIDs.Clear();

                    if (count > 0)
                        AppService.CaptureUiMessage($"All {count} air units have been removed from {UnitName}.");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ClearAllAirUnits), e);
            }
        }

        public bool CanLaunchAirOperations() =>
            IsBase && FacilityType == FacilityType.Airbase &&
            OperationalCapacity != OperationalCapacity.OutOfOperation &&
            _airUnitsAttached.Count > 0;

        public bool CanRepairAircraft() =>
            IsBase && FacilityType == FacilityType.Airbase &&
            OperationalCapacity != OperationalCapacity.OutOfOperation;

        public bool CanAcceptNewAircraft() =>
            IsBase && FacilityType == FacilityType.Airbase &&
            GetAirUnitCapacity() > 0 &&
            OperationalCapacity != OperationalCapacity.OutOfOperation;

        /// <summary>
        /// Gets all operational air units attached to this airbase.
        /// </summary>
        public List<CombatUnit> GetOperationalAirUnits()
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.Airbase)
                    return new List<CombatUnit>();

                return _airUnitsAttached.Where(unit => unit != null &&
                    !unit.IsDestroyed() &&
                    unit.EfficiencyLevel != EfficiencyLevel.StaticOperations)
                    .ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetOperationalAirUnits), e);
                return new List<CombatUnit>();
            }
        }

        public int GetOperationalAirUnitCount() => GetOperationalAirUnits().Count;

        /// <summary>
        /// Synchronizes the attached unit IDs list with the current air units list.
        /// </summary>
        public void SynchronizeAirUnitLists()
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.Airbase)
                    return;

                var currentIds = _airUnitsAttached.Select(u => u.UnitID).ToList();
                var idsNotInUnits = _attachedUnitIDs.Except(currentIds).ToList();
                var unitsNotInIds = currentIds.Except(_attachedUnitIDs).ToList();

                if (idsNotInUnits.Any() || unitsNotInIds.Any())
                {
                    AppService.CaptureUiMessage($"Synchronizing air unit lists for {UnitName}");
                    _attachedUnitIDs.Clear();
                    _attachedUnitIDs.AddRange(currentIds);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SynchronizeAirUnitLists), e);
            }
        }

        private bool IsAirUnitClassification(UnitClassification classification) =>
            classification == UnitClassification.FGT ||
            classification == UnitClassification.ATT ||
            classification == UnitClassification.BMB ||
            classification == UnitClassification.RECONA;

        #endregion // Airbase Management

        #region Supply Depot Management

        private float GetMaxStockpile() =>
            IsBase && FacilityType == FacilityType.SupplyDepot
                ? GameData.MaxStockpileBySize[DepotSize] : 0f;

        private float GetCurrentGenerationRate()
        {
            if (!IsBase || FacilityType != FacilityType.SupplyDepot) return 0f;
            return GameData.GenerationRateValues[GenerationRate] * GetFacilityEfficiencyMultiplier();
        }

        /// <summary>
        /// Adds supplies directly to the depot stockpile.
        /// </summary>
        public bool AddSupplies(float amount)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return false;
                if (amount <= 0)
                    throw new ArgumentException("Supply amount must be positive", nameof(amount));

                if (StockpileInDays >= GetMaxStockpile())
                {
                    AppService.CaptureUiMessage($"{UnitName} stockpile is already full. Cannot add more supplies.");
                    return false;
                }

                float maxCapacity = GetMaxStockpile();
                StockpileInDays = Math.Min(StockpileInDays + amount, maxCapacity);

                AppService.CaptureUiMessage($"{UnitName} has added {amount} days of supply. Current stockpile: {StockpileInDays} days.");
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AddSupplies), e);
                return false;
            }
        }

        /// <summary>
        /// Removes supplies from the depot stockpile.
        /// </summary>
        public void RemoveSupplies(float amount)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return;
                if (amount <= 0)
                    throw new ArgumentException("Supply amount must be positive", nameof(amount));

                float actualAmount = Math.Min(amount, StockpileInDays);
                StockpileInDays -= actualAmount;

                AppService.CaptureUiMessage($"{UnitName} has removed {actualAmount} days of supply. Current stockpile: {StockpileInDays} days.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RemoveSupplies), e);
            }
        }

        /// <summary>
        /// Generates supplies based on the depot's generation rate (called once per turn).
        /// </summary>
        public bool GenerateSupplies()
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return false;
                if (!IsFacilityOperational())
                {
                    AppService.CaptureUiMessage($"{UnitName} is not operational and cannot generate supplies.");
                    return false;
                }

                float generatedAmount = GetCurrentGenerationRate();
                float maxCapacity = GetMaxStockpile();
                float amountToAdd = Math.Min(generatedAmount, maxCapacity - StockpileInDays);
                StockpileInDays += amountToAdd;

                AppService.CaptureUiMessage($"{UnitName} has generated {amountToAdd} days of supply. Current stockpile: {StockpileInDays} days.");
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GenerateSupplies), e);
                return false;
            }
        }

        /// <summary>
        /// Checks if this depot can supply a unit at the specified distance and ZOC conditions.
        /// </summary>
        public bool CanSupplyUnitAt(int distanceInHexes, int enemyZOCsCrossed)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return false;
                if (!IsFacilityOperational()) return false;
                if (distanceInHexes > ProjectionRadius) return false;

                if (enemyZOCsCrossed > 0)
                {
                    if (!SupplyPenetration) return false;
                    if (enemyZOCsCrossed > GameData.ZOC_RANGE) return false;
                }
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CanSupplyUnitAt), e);
                return false;
            }
        }

        /// <summary>
        /// Supplies a unit with calculated efficiency based on distance and ZOCs.
        /// </summary>
        public float SupplyUnit(int distanceInHexes, int enemyZOCsCrossed)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return 0f;
                if (!CanSupplyUnitAt(distanceInHexes, enemyZOCsCrossed)) return 0f;
                if (StockpileInDays <= GameData.MaxDaysSupplyUnit) return 0f;

                float distanceEfficiency = 1f - (distanceInHexes / (float)ProjectionRadius * GameData.DISTANCE_EFF_MULT);
                float zocEfficiency = 1f - (enemyZOCsCrossed * GameData.ZOC_EFF_MULT);
                float operationalEfficiency = GetFacilityEfficiencyMultiplier();
                float totalEfficiency = Math.Max(distanceEfficiency * zocEfficiency * operationalEfficiency, 0.1f);

                float amountToDeliver = GameData.MaxDaysSupplyUnit * totalEfficiency;
                StockpileInDays -= GameData.MaxDaysSupplyUnit;
                return amountToDeliver;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SupplyUnit), e);
                return 0f;
            }
        }

        /// <summary>
        /// Performs air supply operation (main depot only).
        /// </summary>
        public float PerformAirSupply(int distanceInHexes) =>
            PerformRemoteSupply(GameData.AirSupplyMaxRange, distanceInHexes, nameof(PerformAirSupply));

        /// <summary>
        /// Performs naval supply operation (main depot only).
        /// </summary>
        public float PerformNavalSupply(int distanceInHexes) =>
            PerformRemoteSupply(GameData.NavalSupplyMaxRange, distanceInHexes, nameof(PerformNavalSupply));

        private float PerformRemoteSupply(int maxRange, int distanceInHexes, string methodName)
        {
            try
            {
                if (!IsFacilityOperational() || !IsMainDepot || FacilityType != FacilityType.SupplyDepot)
                    return 0f;
                if (distanceInHexes > maxRange || StockpileInDays <= GameData.MaxDaysSupplyUnit)
                    return 0f;

                float distanceEfficiency = 1f - (distanceInHexes / (float)maxRange * GameData.DISTANCE_EFF_MULT);
                float totalEfficiency = Math.Max(distanceEfficiency * GetFacilityEfficiencyMultiplier(), 0.1f);

                StockpileInDays -= GameData.MaxDaysSupplyUnit;
                return GameData.MaxDaysSupplyUnit * totalEfficiency;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, methodName, e);
                return 0f;
            }
        }

        public float GetStockpilePercentage()
        {
            if (!IsBase || FacilityType != FacilityType.SupplyDepot) return 0f;
            float maxCapacity = GetMaxStockpile();
            return maxCapacity > 0 ? StockpileInDays / maxCapacity : 0f;
        }

        public bool IsStockpileEmpty() =>
            !IsBase || FacilityType != FacilityType.SupplyDepot || StockpileInDays <= 0f;

        public float GetRemainingSupplyCapacity() =>
            IsBase && FacilityType == FacilityType.SupplyDepot
                ? GetMaxStockpile() - StockpileInDays : 0f;

        /// <summary>
        /// Upgrades the depot to the next size tier.
        /// </summary>
        public bool UpgradeDepotSize()
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return false;

                return DepotSize switch
                {
                    DepotSize.Small => SetDepotSizeAndReturn(DepotSize.Medium),
                    DepotSize.Medium => SetDepotSizeAndReturn(DepotSize.Large),
                    DepotSize.Large => SetDepotSizeAndReturn(DepotSize.Huge),
                    _ => false,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(UpgradeDepotSize), e);
                return false;
            }
        }

        private bool SetDepotSizeAndReturn(DepotSize size)
        {
            SetDepotSize(size);
            return true;
        }

        /// <summary>
        /// Sets supply penetration capability (typically controlled by leader skills).
        /// </summary>
        public void SetSupplyPenetration(bool enabled)
        {
            if (IsBase && FacilityType == FacilityType.SupplyDepot)
                SupplyPenetration = enabled;
        }

        private void SetDepotSize(DepotSize depotSize)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return;

                switch (depotSize)
                {
                    case DepotSize.Small:
                        DepotSize = DepotSize.Small;
                        StockpileInDays = GetMaxStockpile();
                        GenerationRate = SupplyGenerationRate.Minimal;
                        SupplyProjection = SupplyProjection.Local;
                        break;
                    case DepotSize.Medium:
                        DepotSize = DepotSize.Medium;
                        StockpileInDays = GetMaxStockpile();
                        GenerationRate = SupplyGenerationRate.Basic;
                        SupplyProjection = SupplyProjection.Extended;
                        break;
                    case DepotSize.Large:
                        DepotSize = DepotSize.Large;
                        StockpileInDays = GetMaxStockpile();
                        GenerationRate = SupplyGenerationRate.Standard;
                        SupplyProjection = SupplyProjection.Regional;
                        break;
                    case DepotSize.Huge:
                        DepotSize = DepotSize.Huge;
                        StockpileInDays = GetMaxStockpile();
                        GenerationRate = SupplyGenerationRate.Enhanced;
                        SupplyProjection = SupplyProjection.Strategic;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(depotSize), "Invalid depot size specified");
                }

                if (IsBase)
                    AppService.CaptureUiMessage($"{UnitName} depot has been upgraded to {DepotSize} size. Stockpile: {StockpileInDays} days.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetDepotSize), e);
            }
        }

        #endregion // Supply Depot Management

        #region Template Copying

        /// <summary>
        /// Creates a template copy of this CombatUnit with a new unique ID.
        /// Leaders are not cloned and must be assigned separately.
        /// </summary>
        public object Clone()
        {
            try
            {
                return CreateTemplateClone();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Clone), e);
                throw;
            }
        }

        /// <summary>
        /// Copies template characteristics from another CombatUnit to this instance.
        /// Only copies defining template properties, not runtime state, positions, or assignments.
        /// </summary>
        public void CopyTemplateFrom(CombatUnit template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            try
            {
                UnitName = template.UnitName;
                Classification = template.Classification;
                Role = template.Role;
                Side = template.Side;
                Nationality = template.Nationality;

                RegimentProfile.InitializeRegimentProfile(
                    template.UnitName,
                    template.RegimentProfile.ProfileType,
                    template.RegimentProfile.Mobile,
                    template.RegimentProfile.Deployed,
                    template.RegimentProfile.Embarked);

                IsEmbarkable = template.IsEmbarkable;
                IsMountable = template.IsMountable;

                if (template.IsBase)
                {
                    DepotCategory = template.DepotCategory;
                    DepotSize = template.DepotSize;
                    FacilityType = template.FacilityType;
                }

                InitializeActionCounts();
                InitializeMovementPoints();

                HitPoints.ResetToMax();
                DaysSupply.ResetToMax();
                MovementPoints.ResetToMax();
                EfficiencyLevel = EfficiencyLevel.FullOperations;
                ExperienceLevel = ExperienceLevel.Trained;

                SpottedLevel = SpottedLevel.Level1;
                MapPos = Position2D.Zero;

                if (IsBase)
                {
                    BaseDamage = 0;
                    OperationalCapacity = OperationalCapacity.Full;
                    if (FacilityType == FacilityType.SupplyDepot)
                        StockpileInDays = 0f;
                    ClearAllAirUnits();
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CopyTemplateFrom), e);
                throw;
            }
        }

        /// <summary>
        /// Creates a new CombatUnit that is a template copy of this unit with fresh state.
        /// </summary>
        public CombatUnit CreateTemplateClone()
        {
            try
            {
                return new CombatUnit(
                    unitName: UnitName,
                    classification: Classification,
                    role: Role,
                    side: Side,
                    nationality: Nationality,
                    profileType: RegimentProfile.ProfileType,
                    deployedProfile: RegimentProfile.Deployed,
                    isMountable: IsMountable,
                    mobileProfile: RegimentProfile.Mobile,
                    isEmbarkable: IsEmbarkable,
                    embarkedProfile: RegimentProfile.Embarked,
                    category: IsBase ? DepotCategory : DepotCategory.Secondary,
                    size: IsBase ? DepotSize : DepotSize.Small
                );
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateTemplateClone), e);
                throw;
            }
        }

        #endregion // Template Copying

        #region Intel Reports

        /// <summary>
        /// Returns an IntelReport about this unit filtered by the given SpottedLevel.
        /// Level0: Empty report (not spotted).
        /// Level1: Unit name and nationality only.
        /// Level2: Above plus deployment status and equipment buckets with ~30% error.
        /// Level3: Above plus experience/efficiency levels and equipment with ~10% error.
        /// Level4: Full intel, no error.
        /// </summary>
        public IntelReport GetIntelReport(SpottedLevel spottedLevel)
        {
            try
            {
                var report = new IntelReport();

                if (spottedLevel == SpottedLevel.Level0)
                    return report;

                // Level1+: Name and nationality
                report.UnitName = UnitName;
                report.UnitNationality = Nationality;

                if (spottedLevel == SpottedLevel.Level1)
                    return report;

                // Level2+: Deployment status and equipment buckets with error
                report.DeploymentPosition = DeploymentPosition;

                float errorRate = spottedLevel switch
                {
                    SpottedLevel.Level2 => GameData.MAX_INTEL_ERROR / 100f,
                    SpottedLevel.Level3 => GameData.MIN_INTEL_ERROR / 100f,
                    _ => 0f
                };

                ApplyEquipmentBuckets(report, errorRate);

                if (spottedLevel == SpottedLevel.Level2)
                    return report;

                // Level3+: Experience and efficiency levels
                report.UnitExperienceLevel = ExperienceLevel;
                report.UnitEfficiencyLevel = EfficiencyLevel;

                return report;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetIntelReport), e);
                throw;
            }
        }

        private void ApplyEquipmentBuckets(IntelReport report, float errorRate)
        {
            var baseReport = RegimentProfile?.GetIntelReport();
            if (baseReport == null)
                return;

            float strengthRatio = HitPoints.Max > 0 ? HitPoints.Current / HitPoints.Max : 0f;

            report.Personnel = ApplyIntelError(baseReport.Personnel, strengthRatio, errorRate);
            report.TANK = ApplyIntelError(baseReport.TANK, strengthRatio, errorRate);
            report.IFV = ApplyIntelError(baseReport.IFV, strengthRatio, errorRate);
            report.APC = ApplyIntelError(baseReport.APC, strengthRatio, errorRate);
            report.RCN = ApplyIntelError(baseReport.RCN, strengthRatio, errorRate);
            report.ART = ApplyIntelError(baseReport.ART, strengthRatio, errorRate);
            report.ROC = ApplyIntelError(baseReport.ROC, strengthRatio, errorRate);
            report.SAM = ApplyIntelError(baseReport.SAM, strengthRatio, errorRate);
            report.AAA = ApplyIntelError(baseReport.AAA, strengthRatio, errorRate);
            report.AT = ApplyIntelError(baseReport.AT, strengthRatio, errorRate);
            report.HEL = ApplyIntelError(baseReport.HEL, strengthRatio, errorRate);
            report.AWACS = ApplyIntelError(baseReport.AWACS, strengthRatio, errorRate);
            report.TRN = ApplyIntelError(baseReport.TRN, strengthRatio, errorRate);
            report.FGT = ApplyIntelError(baseReport.FGT, strengthRatio, errorRate);
            report.ATT = ApplyIntelError(baseReport.ATT, strengthRatio, errorRate);
            report.BMB = ApplyIntelError(baseReport.BMB, strengthRatio, errorRate);
            report.RCNA = ApplyIntelError(baseReport.RCNA, strengthRatio, errorRate);
        }

        private int ApplyIntelError(int baseValue, float strengthRatio, float errorRate)
        {
            if (baseValue <= 0) return 0;

            float scaled = baseValue * strengthRatio;

            if (errorRate <= 0f)
                return Mathf.RoundToInt(scaled);

            float offset = scaled * UnityEngine.Random.Range(-errorRate, errorRate);
            return Mathf.Max(0, Mathf.RoundToInt(scaled + offset));
        }

        #endregion // Intel Reports

        #region Debugging

        public float DebugGetCombatMovementCost() =>
            Mathf.CeilToInt(MovementPoints.Max * GameData.COMBAT_ACTION_MOVEMENT_COST);

        #endregion // Debugging
    }
}
