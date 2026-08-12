using HammerAndSickle.Controllers;
using HammerAndSickle.Models.Map;
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

        private readonly List<CombatUnit> _airUnitsAttached = new();
        private List<string> _attachedUnitIDs = new();

        #endregion // Fields

        #region Properties

        // Identity
        [JsonPropertyName("unitName")]                     public string UnitName { get; set; }
        [JsonInclude] [JsonPropertyName("unitID")]         public string UnitID { get; private set; }
        [JsonInclude] [JsonPropertyName("classification")] public UnitClassification Classification { get; private set; }
        [JsonInclude] [JsonPropertyName("role")]           public UnitRole Role { get; private set; }
        [JsonInclude] [JsonPropertyName("side")]           public Side Side { get; private set; }
        [JsonInclude] [JsonPropertyName("nationality")]    public Nationality Nationality { get; private set; }
        [JsonIgnore]                                       public bool IsBase => IsBaseType(Classification);

        /// <summary>
        /// True for a FIXED-WING aircraft. ⚠ Replaces the old `IsAirUnit` / `IsFixedWingAirUnit` pair
        /// (identical expressions, both short by two members) — see the remarks.
        /// </summary>
        /// <remarks>
        /// ⚠ THIS IS THE DERIVATION, NOT A RULE SITE. Ask <see cref="OccupiesDomain"/> or
        /// <see cref="IsSeenAsAir"/> in rule code; this exists to feed them (§`Domain`).
        ///
        /// ⚠ D9 FIXED HERE (2026-08-10): FOUR disagreeing "is fixed-wing" lists existed —
        /// `GameData.IsAirborneClassification` (7, correct), the old `IsAirUnit` (5, missing WW + TRN),
        /// `IsFixedWingClassification` (4, missing AWACS as well), and one in `GameIconRenderer` (5).
        /// All now defer to the GameData list, which §12.3.7 backs and `SpottingRangeTests` pins.
        /// Two live bugs die with them: a TRANSPORT AIRCRAFT was filed in the GROUND layer (so it
        /// projected zones of control and could be ground-ambushed), and an AWACS could not attach to an
        /// airbase at all because `AddAirUnit` threw on it.
        /// </remarks>
        [JsonIgnore] public bool IsFixedWing => GameData.IsAirborneClassification(Classification);

        [JsonIgnore] public bool IsHelicopter => Classification == UnitClassification.HELO;

        /// <summary>
        /// Which layer of the battlefield this unit occupies right now — the authority for hex sharing,
        /// icon layer, tile-control flips, and who may engage it.
        /// </summary>
        /// <remarks>
        /// ⚠ A HELICOPTER IS `Ground`. It flies, but it stacks, holds ground, projects zones of control
        /// and is ambushed like a ground unit. Only fixed-wing is `Air`, and fixed-wing is the ONLY thing
        /// that may temporarily share a hex with another unit.
        /// </remarks>
        [JsonIgnore] public Domain OccupiesDomain =>
            IsFixedWing ? Domain.Air
            : IsNavalEmbarked ? Domain.Naval
            : Domain.Ground;

        [JsonIgnore] public bool IsReconUnit =>
            Classification is UnitClassification.RECON or UnitClassification.RECONA;

        [JsonIgnore] public bool ProjectsZoC
        {
            get
            {
                if (IsFixedWing || IsBase) return false;
                if (_deploymentPosition == DeploymentPosition.Embarked) return false;
                return true;
            }
        }

        [JsonInclude]
        [JsonPropertyName("equipmentBays")]
        public EquipmentBays EquipmentBays { get; private set; }

        [JsonInclude] [JsonPropertyName("efficiencyLevel")]
        public EfficiencyLevel EfficiencyLevel { get; internal set; }

        // Actions
        [JsonInclude] [JsonPropertyName("moveActions")]        public StatsMaxCurrent MoveActions { get; private set; }
        [JsonInclude] [JsonPropertyName("combatActions")]      public StatsMaxCurrent CombatActions { get; private set; }
        [JsonInclude] [JsonPropertyName("deploymentActions")]  public StatsMaxCurrent DeploymentActions { get; private set; }
        [JsonInclude] [JsonPropertyName("opportunityActions")] public StatsMaxCurrent OpportunityActions { get; private set; }

        /// <summary>
        /// §5.13.2 — this helicopter ended a previous turn over Water and is living on its one turn of
        /// grace. Set at Upkeep when the resting hex is Water, cleared the moment it reaches land; still
        /// true at the NEXT Upkeep while still over water means the unit is lost at sea.
        /// </summary>
        /// <remarks>
        /// ⚠ PERSISTED, and it is the reason for the SAVE_VERSION 5 → 6 bump. It has to survive a save:
        /// without it, saving over open water and reloading would silently reset the grace clock, turning
        /// the rule into "a helicopter may loiter at sea forever, as long as you save occasionally."
        /// ⚠ ONE BOOL IS ENOUGH because the rule is exactly one turn of grace — "was it already over water
        /// at the last Upkeep" is the entire question. A stranded-since turn number would be needed only if
        /// the grace period were ever longer than one turn.
        /// </remarks>
        [JsonInclude] [JsonPropertyName("endedTurnOverWater")] public bool EndedTurnOverWater { get; private set; }

        /// <summary>Sets the §5.13.2 over-water grace flag. Driven by BattleManager's Upkeep pass.</summary>
        public void SetEndedTurnOverWater(bool value) => EndedTurnOverWater = value;
        [JsonInclude] [JsonPropertyName("intelActions")]       public StatsMaxCurrent IntelActions { get; private set; }

        // State
        [JsonInclude] [JsonPropertyName("hitPoints")]      public StatsMaxCurrent HitPoints { get; private set; }
        [JsonInclude] [JsonPropertyName("daysSupply")]     public StatsMaxCurrent DaysSupply { get; private set; }
        [JsonInclude] [JsonPropertyName("movementPoints")] public StatsMaxCurrent MovementPoints { get; private set; }
        [JsonInclude] [JsonPropertyName("mapPos")]         public Position2D MapPos { get; internal set; }
        [JsonInclude] [JsonPropertyName("facing")]         public HexDirection Facing { get; set; }
        [JsonInclude] [JsonPropertyName("spottedLevel")]   public SpottedLevel SpottedLevel { get; private set; }

        // Leader
        [JsonInclude] [JsonPropertyName("leaderID")] public string LeaderID { get; internal set; } = string.Empty;
        [JsonIgnore] public bool IsLeaderAssigned => !string.IsNullOrEmpty(LeaderID);
        public Leader GetAssignedLeader() =>
            IsLeaderAssigned ? GameDataManager.Instance.GetLeader(LeaderID) : null;

        // Deployment
        [JsonIgnore] public DeploymentPosition DeploymentPosition => _deploymentPosition;
        /* `isEmbarkable`/`isMountable`/`currentEmbarkmentState` DELETED 2026-08-08 (P1). The flags had
         * 0 and 2 readers respectively, all redundant with slot-content null checks; the embarkment
         * enum was never written by gameplay. Capacity is derived (EquipmentBays.CanAccept). */

        /// <summary>
        /// The naval transient state (§5.4.2/§9.10.6, todo_profiles §4.5): true while this ground unit
        /// rides the universal sealift. NEVER a possession — the shared TRN_NAVAL profile is drawn at
        /// <see cref="GetActiveWeaponProfile"/> while set. Written ONLY by the P2 naval embark/debark
        /// path (always false until that lands) and by save restore.
        /// </summary>
        [JsonInclude] [JsonPropertyName("isNavalEmbarked")] public bool IsNavalEmbarked { get; private set; }

        // Experience
        [JsonInclude] [JsonPropertyName("experiencePoints")] public int ExperiencePoints { get; internal set; }
        [JsonInclude] [JsonPropertyName("experienceLevel")]  public ExperienceLevel ExperienceLevel { get; internal set; }

        // Facility - common
        [JsonInclude] [JsonPropertyName("attachedUnitIDs")]
        public IReadOnlyList<string> AttachedUnitIDs
        {
            get => _attachedUnitIDs.AsReadOnly();
            private set => _attachedUnitIDs = value?.ToList() ?? new List<string>();
        }
        [JsonInclude] [JsonPropertyName("baseDamage")]          public int BaseDamage { get; private set; }
        [JsonInclude] [JsonPropertyName("operationalCapacity")] public OperationalCapacity OperationalCapacity { get; private set; }
        [JsonInclude] [JsonPropertyName("facilityType")]        public FacilityType FacilityType { get; private set; }

        // Facility - supply depot
        [JsonInclude] [JsonPropertyName("depotSize")]         public DepotSize DepotSize { get; private set; }
        [JsonInclude] [JsonPropertyName("stockpileInDays")]   public float StockpileInDays { get; private set; }
        [JsonInclude] [JsonPropertyName("generationRate")]    public SupplyGenerationRate GenerationRate { get; private set; }
        [JsonInclude] [JsonPropertyName("supplyProjection")]  public SupplyProjection SupplyProjection { get; private set; }
        [JsonInclude] [JsonPropertyName("supplyPenetration")] public bool SupplyPenetration { get; private set; }
        [JsonInclude] [JsonPropertyName("depotCategory")]     public DepotCategory DepotCategory { get; private set; }
        [JsonIgnore] public int ProjectionRadius => IsBase ? GameData.ProjectionRangeValues[SupplyProjection] : 0;
        [JsonIgnore] public bool IsMainDepot => IsBase && DepotCategory == DepotCategory.Main;

        // Facility - airbase
        [JsonIgnore] public IReadOnlyList<CombatUnit> AirUnitsAttached { get; private set; }

        // TODO: Add an event that fires on DeploymentPosition changes, to update icon picking.

        // Range proxy properties from active weapon profile
        [JsonIgnore] public float ActivePrimaryRange => GetActiveWeaponProfile()?.PrimaryRange ?? 0f;
        [JsonIgnore] public float ActiveIndirectRange => GetActiveWeaponProfile()?.IndirectRange ?? 0f;
        [JsonIgnore] public float ActiveSpottingRange => GetActiveWeaponProfile()?.SpottingRange ?? 0f;

        // Dual-domain spotting (§12.3) — classification-driven, decoupled from the profile SR. The spotting sweep
        // picks ground-vs-air by the TARGET's IsAirborneSpottingTarget. (Leader bonus §12.3.11 → M14; SIGINT → M15.)
        [JsonIgnore] public int ActiveGroundSpottingRange => GameData.GroundSpottingRange(Classification);
        [JsonIgnore] public int ActiveAirSpottingRange => GameData.AirSpottingRange(Classification);

        /// <summary>True if a spotter uses its AIR range against THIS unit: any fixed-wing, or a regiment riding
        /// helo lift (§7A.14 — a lift that can't easily hide). Attack helos (HELO) are NOT — they fly
        /// Nap-of-the-Earth and are spotted on the ground range. A dismounted rider is a ground target.
        /// ⚠ The second arm read the never-written EmbarkmentState until 2026-08-08 (P1/D1) and was
        /// permanently false; the ACTIVE PROFILE's medium is what actually says "riding helicopters right now".
        /// </summary>
        /// <remarks>
        /// ⚠ THIS IS DELIBERATELY A DIFFERENT QUESTION FROM <see cref="OccupiesDomain"/>, AND THE
        /// HELO-BORNE LIFT IS THE CASE THAT PROVES IT: in flight it OCCUPIES the ground layer (it stacks
        /// and lands there) but is SEEN as air (it cannot hide like a gunship). One property could never
        /// express that, which is why the vocabulary names the question rather than the answer.
        /// ⚠ Boolean rather than a `Domain`, because there are exactly TWO spotting ranges — tanks,
        /// helicopters and ships all share the ground one, and no ship should have to be labelled
        /// "Ground" to be ranged against.
        /// </remarks>
        [JsonIgnore] public bool IsSeenAsAir =>
            IsFixedWing
            || (Classification != UnitClassification.HELO
                && GetActiveWeaponProfile()?.MovementMedium == MovementMedium.Helo);

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

                InitializeDeploymentSystem();
                EquipmentBays = new EquipmentBays();

                if (IsBase)
                    InitializeFacility(category, size);

                InitializeActionCounts();
                SpottedLevel = SpottedLevel.Level1;
                InitializeExperienceSystem();
                HitPoints = new StatsMaxCurrent(IsBase ? GameData.BASE_MAX_HP : GameData.MAX_HP);
                DaysSupply = new StatsMaxCurrent(IsBase && FacilityType == FacilityType.Airbase
                    ? GameData.MaxDaysSupplyAirbase : GameData.MaxDaysSupplyUnit);
                MovementPoints = new StatsMaxCurrent(GameData.FOOT_UNIT);
                EfficiencyLevel = EfficiencyLevel.FullOperations;
                MapPos = Position2D.Zero;
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
                EquipmentBays = new EquipmentBays();

                _deploymentPosition = DeploymentPosition.Deployed;
                IsNavalEmbarked = false;

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
        /// Constructor with equipment-bay parameters. Used by OOB loading, template cloning, and
        /// snapshot creation. ⚠ Slot order is DEP/MOB/EMB — the pre-P1 signature interleaved
        /// profileType/isMountable/isEmbarkable between the slots AND took mobile before deployed;
        /// those three are deleted (capacity is derived, see EquipmentBays).
        /// </summary>
        public CombatUnit(string unitName,
            UnitClassification classification,
            UnitRole role,
            Side side,
            Nationality nationality,
            WeaponType deployedProfile,
            WeaponType mobileProfile,
            WeaponType embarkedProfile,
            DepotCategory category = DepotCategory.Secondary,
            DepotSize size = DepotSize.Small)
            : this(unitName, classification, role, side, nationality, category, size)
        {
            EquipmentBays.InitializeEquipmentBays(unitName, deployedProfile, mobileProfile, embarkedProfile);
            InitializeMovementPoints();
        }

        #endregion // Constructors

        #region Core

        public WeaponProfile GetDeployedProfile() => EquipmentBays?.GetDeployedProfile();
        public WeaponProfile GetMobileProfile() => EquipmentBays?.GetMobileProfile();
        public WeaponProfile GetEmbarkedProfile() => EquipmentBays?.GetEmbarkedProfile();

        /// <summary>
        /// Returns the active weapon profile based on current deployment state. While
        /// <see cref="IsNavalEmbarked"/> (the §9.10.6 transient sealift state — P2 writes it), the
        /// SHARED naval profile is drawn: never owned, never in a bay.
        /// </summary>
        public WeaponProfile GetActiveWeaponProfile() => DeploymentPosition switch
        {
            DeploymentPosition.Embarked when IsNavalEmbarked =>
                WeaponProfileDB.GetWeaponProfile(WeaponType.TRN_NAVAL) ?? GetDeployedProfile(),
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

            // §11.8.6 — the per-turn anti-dogpile record clears with the budget it guards (see the field).
            _aircraftEngagedThisTurn?.Clear();
        }

        // ----------------------------------------------------------------------------
        // §11.8.6 anti-dogpile — the aircraft this unit has already fired an opportunity
        // shot at THIS TURN.
        //
        // [JsonIgnore] and runtime-only: a move order is atomic with respect to input, so
        // no save can be taken mid-transit and there is nothing here worth persisting.
        // Cleared in RefreshAllActions, the one place per-turn state resets.
        //
        // ⚠ PER TURN, NOT PER MOVE ORDER, and the two scopes are doing different jobs. The
        // §11.8.3 shot BUDGET is spent across every aircraft this unit engages in a turn;
        // this SET stops it engaging the SAME aircraft twice however many hexes that
        // aircraft flies through its envelope. The ground-ambush anti-dogpile is a separate
        // per-move-order set owned by MovementController — same idea, different scope, and
        // deliberately not shared.
        // ----------------------------------------------------------------------------

        [JsonIgnore] private HashSet<string> _aircraftEngagedThisTurn;

        /// <summary>§11.8.6 — has this air-defence unit already engaged that aircraft this turn?</summary>
        public bool HasEngagedAircraftThisTurn(string aircraftId) =>
            aircraftId != null && _aircraftEngagedThisTurn != null && _aircraftEngagedThisTurn.Contains(aircraftId);

        /// <summary>§11.8.6 — records an opportunity shot against that aircraft for the rest of this turn.</summary>
        public void MarkAircraftEngaged(string aircraftId)
        {
            if (aircraftId == null) return;
            (_aircraftEngagedThisTurn ??= new HashSet<string>()).Add(aircraftId);
        }

        /// <summary>
        /// Refreshes movement points to maximum. Called at turn start.
        /// </summary>
        public void RefreshMovementPoints() => MovementPoints.ResetToMax();

        // ----------------------------------------------------------------------------
        // Per-turn activity flags (§3.5.8 efficiency recovery input). Transient — reset
        // at the unit's Refresh (3.3), set during its Turn as it moves / fights, read at
        // its Upkeep to decide recovery (+2 idle / +1 moved / 0 fought). [JsonIgnore]:
        // pure intra-turn bookkeeping, never serialized (a mid-turn save reloads with the
        // flags cleared, which is correct — recovery only matters at Upkeep).
        // ----------------------------------------------------------------------------

        /// <summary>True if this unit moved at least one hex this turn (§7.15.8.2).</summary>
        [JsonIgnore] public bool HasMovedThisTurn { get; private set; }

        /// <summary>True if this unit fought this turn — attacker, defender, ambusher,
        /// opportunity firer, or counter-battery (§7.15.8.3).</summary>
        [JsonIgnore] public bool HasFoughtThisTurn { get; private set; }

        /// <summary>Flags the unit as having moved this turn (called by MovementController per step).</summary>
        public void MarkMovedThisTurn() => HasMovedThisTurn = true;

        /// <summary>Flags the unit as having fought this turn (called by the combat resolver path).</summary>
        public void MarkFoughtThisTurn() => HasFoughtThisTurn = true;

        /// <summary>Clears the per-turn activity flags. Called at the unit's Refresh (§3.3).</summary>
        public void ResetTurnFlags()
        {
            HasMovedThisTurn = false;
            HasFoughtThisTurn = false;
        }

        /// <summary>
        /// Sets the enemy-side intel level on this unit. If the attached leader has Concealed Operations
        /// Base (Underground Bunker, §14.8.7), enemy intel is hard-capped at Level 3.
        ///
        /// ⚠ SKILL RE-REPURPOSE PENDING (2026-07-24, deferred to its own pass): this cap was written against
        /// the OLD five-level ladder, where Level 3 was the penultimate rung and the cap denied "perfect
        /// intel." On the six-rung ladder (§12.2) Level 3 is mid-ladder — reachable by a SIGINT sweep or air
        /// recon alone — so the cap now denies equipment counts AND experience/efficiency, which is a much
        /// bigger effect than the skill was priced for. Behaviour is left EXACTLY as it was rather than
        /// silently retuned; see Claude_TODO and DesignDoc §14.8.7 for the candidate re-home.
        /// </summary>
        public void SetSpottedLevel(SpottedLevel spottedLevel)
        {
            if (spottedLevel > SpottedLevel.Level3 && (GetAssignedLeader()?.HasUndergroundBunker ?? false))
                spottedLevel = SpottedLevel.Level3;

            SpottedLevel = spottedLevel;
        }

        /// <summary>
        /// Hexes by which ENEMY spotting range is reduced against this unit (Superior Camouflage, §14.9.4).
        /// Applied at the §12.3.10 range comparison by SpottingService. 0 if unled.
        /// </summary>
        public int EnemySpottingRangeReduction =>
            GetAssignedLeader() is { } l ? (int)l.ConcealedPositionsReduction : 0;

        /// <summary>
        /// Applies damage to the unit, reducing hit points.
        ///
        /// ⚠ THIS IS THE LOSS-LEDGER CHOKE POINT (printer P5). Every damage source in the game already
        /// funnels through here — direct combat, return fire, ambush, counter-battery, AD opportunity fire,
        /// air strikes, base attacks, and shatter — so booking equipment losses at this one site captures
        /// all of them and, more importantly, CANNOT BE FORGOTTEN when a new damage source is added later.
        /// Hooking "after combat" instead would have silently missed return fire, ambush and AD fire, which
        /// are resolved outside the main exchange.
        /// </summary>
        public void TakeDamage(float damage)
        {
            try
            {
                if (damage < 0f)
                    throw new ArgumentException("Damage cannot be negative", nameof(damage));
                if (damage == 0f)
                    return;

                float previousHitPoints = HitPoints.Current;
                float newHitPoints = Mathf.Max(0f, previousHitPoints - damage);
                HitPoints.SetCurrent(newHitPoints);

                // ⚠ Book the HP ACTUALLY REMOVED, not the damage requested. Overkill must not book
                // equipment the unit no longer had: a unit on 3 HP hit for 20 loses 3 HP of equipment,
                // not 20. The two differ on exactly the blow that destroys a unit, so using `damage`
                // here would over-report losses on every single kill.
                GameDataManager.RecordEquipmentLosses(this, previousHitPoints - newHitPoints);
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

        private void InitializeDeploymentSystem()
        {
            _deploymentPosition = DeploymentPosition.Deployed;
            IsNavalEmbarked = false;
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

            // Action-count overrides per §8.5.8 (authoritative table). Baseline = 1/1/1/0/1
            // (Move/Combat/Deploy/Opportunity/Intel); Opportunity defaults to 0 — granted only
            // to reactive-fire roles (§8.5.4).
            switch (Classification)
            {
                // Ground combat — baseline. Reactive facing is free (§8.5.5), so no Opp.
                case UnitClassification.TANK:
                case UnitClassification.MECH:
                case UnitClassification.MOT:
                case UnitClassification.AB:
                case UnitClassification.MAB:
                case UnitClassification.MAR:
                case UnitClassification.MMAR:
                case UnitClassification.AT:
                case UnitClassification.INF:
                case UnitClassification.CAV:
                case UnitClassification.ENG:
                case UnitClassification.BM:
                    break;
                case UnitClassification.RECON:                  // §10.3a.1
                    moveActions += 1;
                    break;
                case UnitClassification.AM:                     // §10.3a.2
                case UnitClassification.MAM:
                    deploymentActions += 1;
                    break;
                case UnitClassification.SPECF:                  // §10.3a.3
                    intelActions += 1;
                    break;
                // Tube artillery — Opp for counter-battery (§8.5.4 / §7.13.5.7)
                case UnitClassification.ART:
                case UnitClassification.SPA:
                    opportunityActions += 1;
                    break;
                // Rocket artillery — +1 CombatAction (§7.14) + counter-battery Opp
                case UnitClassification.ROC:
                    combatActions += 1;
                    opportunityActions += 1;
                    break;
                // Air defense — keeps baseline CombatAction (restricted to HELO / embarked AM-MAM,
                // §8.5.3, enforced in combat validation) + 2 reactive-fire Opp (§8.5.4 / §10.3a.4)
                case UnitClassification.SAM:
                case UnitClassification.SPSAM:
                case UnitClassification.AAA:
                case UnitClassification.SPAAA:
                    opportunityActions += 2;
                    break;
                // Attack helicopter — cannot entrench/embark (§8.5.6): 0 Deploy, 0 Opp (no op-fire)
                case UnitClassification.HELO:
                    deploymentActions = 0;
                    break;
                // Interceptor fighter — fixed-wing economy + 1 Opp for interception (§8.5.4 / §11.4.7.2)
                case UnitClassification.FGT:
                    moveActions += 2;
                    deploymentActions = 0;
                    intelActions = 0;
                    opportunityActions += 1;
                    break;
                // Wild Weasel — fixed-wing economy + 3 Opp for heavy SEAD counter-fire (§8.5.4 / §10.3a.9)
                case UnitClassification.WW:
                    moveActions += 2;
                    deploymentActions = 0;
                    intelActions = 0;
                    opportunityActions += 3;
                    break;
                // Other fixed-wing — +2 Move, 0 Deploy, 0 Intel (§8.5.2)
                case UnitClassification.ATT:
                case UnitClassification.BMB:
                case UnitClassification.AWACS:
                case UnitClassification.RECONA:
                case UnitClassification.TRN:
                    moveActions += 2;
                    deploymentActions = 0;
                    intelActions = 0;
                    break;
                // HQ — static intelligence hub (§8.5.7): only Intel acts
                case UnitClassification.HQ:
                    moveActions = 0;
                    combatActions = 0;
                    deploymentActions = 0;
                    intelActions += 1;
                    break;
                // Passive facilities — all actions 0 (§8.5.7)
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
                                 (GetActiveWeaponProfile()?.ICM ?? GameData.ICM_DEFAULT);

                // Air units (not helos) skip deployment state modifier
                if (!IsFixedWingClassification(Classification))
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

        #region Combat Engine Integration

        // Lane-aware accessors that feed the §7.7.1 damage engine (Models/Combat/). Unlike the legacy
        // GetCombatRatingTotal (which bakes every modifier into the stats for the UI), these expose RAW stats
        // and the multiplier PIECES separately, so the engine computes Δ on raw stats and applies the
        // multiplier only to the rolled HP — and applies deployment ONLY on a defender's return lane (§7.5.2).

        /// <summary>
        /// The firer's QUALITY multiplier for the damage engine (§7.7.1 step 4 core): Strength × Efficiency ×
        /// Experience × ICM. Deliberately EXCLUDES the deployment COMBAT_MOD (that is a defender-only return-fire
        /// term, §7.5.2, supplied separately by <see cref="GetDeploymentCombatMod"/>).
        /// </summary>
        public float GetCombatQualityMultiplier()
        {
            try
            {
                return GetStrengthModifier()
                     * GetEfficiencyModifier()
                     * GetExperienceMultiplier()
                     * (GetActiveWeaponProfile()?.ICM ?? GameData.ICM_DEFAULT);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetCombatQualityMultiplier), e);
                return 1.0f;
            }
        }

        /// <summary>
        /// The deployment COMBAT_MOD this unit contributes as a DEFENDER returning fire (§7.5.2): Fortified 1.3 /
        /// Entrenched 1.2 / HastyDefense 1.1, else 1.0. Fixed-wing skip deployment entirely (§10.3c.1) → 1.0.
        /// The engine applies this only on the return lane.
        /// </summary>
        public float GetDeploymentCombatMod() =>
            IsFixedWing ? 1.0f : GetCombatStateModifier();

        /// <summary>The active profile's Hard/Soft target class (§7.4.1) — the axis an attacker uses against THIS unit.</summary>
        public TargetClass ActiveTargetClass => GetActiveWeaponProfile()?.TargetClass ?? TargetClass.Soft;

        /// <summary>
        /// Attack stat on the axis selected by the lane's target class (§7.4.1 / §7.7.8): Hard → HardAttack,
        /// Soft → SoftAttack, plus the attached leader's doctrine Δ-delta (§14.10.4, +2 per §14.8).
        /// Builds a lane's firer-attack value (engine step 2).
        /// </summary>
        public int GetAttackStatVsClass(TargetClass axisClass)
        {
            var p = GetActiveWeaponProfile();
            if (p == null) return 0;
            return axisClass == TargetClass.Hard
                ? p.HardAttack + LeaderStatDelta(SkillBonusType.HardAttack)
                : p.SoftAttack + LeaderStatDelta(SkillBonusType.SoftAttack);
        }

        /// <summary>
        /// Defense stat on the axis selected by the lane's target class (§7.4.1): Hard → HardDefense,
        /// Soft → SoftDefense, plus the attached leader's doctrine Δ-delta (§14.10.4).
        /// Builds a lane's target-defense value (engine step 2).
        /// </summary>
        public int GetDefenseStatVsClass(TargetClass axisClass)
        {
            var p = GetActiveWeaponProfile();
            if (p == null) return 0;
            return axisClass == TargetClass.Hard
                ? p.HardDefense + LeaderStatDelta(SkillBonusType.HardDefense)
                : p.SoftDefense + LeaderStatDelta(SkillBonusType.SoftDefense);
        }

        /// <summary>
        /// The attached leader's Δ-side combat-stat delta (§14.10.4 — doctrine bonuses are stat deltas at lane
        /// build, NOT ICM). 0 if unled. Inert on facilities (combat skills do nothing on a base, §35.4.3) and
        /// CLASS-GATED (§14.8, ratified 2026-07-03): each doctrine's deltas apply only within its class family,
        /// so e.g. Infantry Assault Tactics on a TANK grants nothing (blocks the SA+2 end-run around the
        /// 1.8.4 combined-arms pillar). Same enforcement pattern as AdvancedTargetting's ART/SPA gate.
        /// </summary>
        private int LeaderStatDelta(SkillBonusType bonusType)
        {
            if (IsBase) return 0;
            if (!DoctrineDeltaAppliesTo(bonusType, Classification)) return 0;
            var leader = GetAssignedLeader();
            return leader != null ? (int)leader.GetBonusValue(bonusType) : 0;
        }

        /// <summary>The §14.8 doctrine class-gate table. MECH is deliberately in two families (mounted IFV lane
        /// vs dismounted lane); one doctrine per leader, so no double-dip is possible.</summary>
        private static bool DoctrineDeltaAppliesTo(SkillBonusType bonusType, UnitClassification cls)
        {
            switch (bonusType)
            {
                case SkillBonusType.HardAttack:
                case SkillBonusType.HardDefense:
                    return cls == UnitClassification.TANK || cls == UnitClassification.MECH;

                case SkillBonusType.SoftAttack:
                case SkillBonusType.SoftDefense:
                    return cls == UnitClassification.INF || cls == UnitClassification.MECH ||
                           cls == UnitClassification.MOT || cls == UnitClassification.AB ||
                           cls == UnitClassification.MAB || cls == UnitClassification.MAR ||
                           cls == UnitClassification.MMAR || cls == UnitClassification.AM ||
                           cls == UnitClassification.MAM || cls == UnitClassification.CAV ||
                           cls == UnitClassification.SPECF;

                case SkillBonusType.AirAttack:
                case SkillBonusType.AirDefense:
                    return cls == UnitClassification.SAM || cls == UnitClassification.SPSAM ||
                           cls == UnitClassification.AAA || cls == UnitClassification.SPAAA;

                default:
                    return false;
            }
        }

        /// <summary>Active profile's Ordnance Load (the airstrike OL/9 multiplier, §11.6.1). 0 if none.</summary>
        public int ActiveOrdnanceLoad => GetActiveWeaponProfile()?.OrdinanceLoad ?? 0;

        /// <summary>Ground-Air Defense (GAD) — the stat an airstrike attacks (§7.7.5), plus the leader's
        /// Air Defense doctrine Δ-delta (§14.8.4). 0 if no profile.</summary>
        public int ActiveGroundAirDefense =>
            GetActiveWeaponProfile() is { } p ? p.GroundAirDefense + LeaderStatDelta(SkillBonusType.AirDefense) : 0;

        /// <summary>Ground-Air Attack (GAT) — the interdiction stat vs transiting aircraft (§11.8.1), plus the
        /// leader's Air Defense doctrine Δ-delta (§14.8.4). 0 if no profile.</summary>
        public int ActiveGroundAirAttack =>
            GetActiveWeaponProfile() is { } p ? p.GroundAirAttack + LeaderStatDelta(SkillBonusType.AirAttack) : 0;

        /// <summary>Active profile's Maneuverability (MAN) — feeds the fixed-wing ground-to-air defense term (§11.8.1). 0 if none.</summary>
        public int ActiveManeuverability => GetActiveWeaponProfile()?.Maneuverability ?? 0;

        /// <summary>Active profile's Survivability (SUR) — feeds the fixed-wing ground-to-air defense term (§11.8.1). 0 if none.</summary>
        public int ActiveSurvivability => GetActiveWeaponProfile()?.Survivability ?? 0;

        /// <summary>True if the active profile's strike ignores target GAD (STANDOFF_CRUISE_MISSILE → IgnoreAirDefense, §11.6.1.1).</summary>
        public bool IgnoresAirDefense => GetActiveWeaponProfile()?.HasCapability(WeaponCapability.IgnoreAirDefense) ?? false;

        /// <summary>Active profile's effective Ground Attack vs a target's class / base-ness (Rule B riders, §11.6.1.1). 0 if none.</summary>
        public int GetEffectiveGroundAttack(TargetClass targetClass, bool isBase) =>
            GetActiveWeaponProfile()?.EffectiveGroundAttack(targetClass, isBase) ?? 0;

        /// <summary>Active profile's runway-cratering OC-suppression rider (RUNWAY_CRATERING) — extra base OC damage (§11.7.2.2a). 0 if none.</summary>
        public int ActiveOcSuppressionBonus => GetActiveWeaponProfile()?.OcSuppressionBonus ?? 0;

        /// <summary>Active profile's parked-aircraft band bonus (RAMP_STRIKE) — band shift on the parked-aircraft roll (§11.7.2.3). 0 if none.</summary>
        public int ActiveParkedHitBonus => GetActiveWeaponProfile()?.ParkedHitBonus ?? 0;

        #endregion // Combat Engine Integration

        #region Actions

        /// <summary>
        /// Spends the action economy for a combat action: 1 CombatAction + 25% max MP (§8.2.1). Supply is a GATE
        /// (must stay above COMBAT_ACTION_SUPPLY_THRESHOLD) but is NOT deterministically consumed here — the old
        /// flat per-attack supply cost is rescinded (§7.15.7.1). Combat supply loss is now probabilistic (§7.15.5)
        /// and is rolled per side by the combat orchestrator (<see cref="HammerAndSickle.Models.Combat.GroundCombatAction"/>).
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
        [Obsolete("Use BeginMoveOrder + DeductMovementCost. Zero external callers.")]
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
        /// True if an opportunity action is affordable right now — an action left, the supply to pay for it,
        /// and not a facility.
        /// </summary>
        /// <remarks>
        /// ⚠ EXISTS SO A CALLER CAN ASK WITHOUT SPENDING, AND WITHOUT LEAKING. <see cref="PerformOpportunityAction"/>
        /// announces its refusal through <c>AppService.CaptureUiMessage</c>, which is right for a player order
        /// and wrong for a reactive scan: §11.8 air-defence fire tests ENEMY units every time an aircraft
        /// crosses their envelope, and a supply-starved AI battery would print its own name into the player's
        /// message log — a fog leak (§12) from a unit that may be unspotted. Scans ask this; only a shot that
        /// is actually taken calls the spender.
        /// ⚠ ONE SPELLING: <see cref="PerformOpportunityAction"/> gates on this method rather than repeating
        /// the conditions, so the two can never disagree about what "affordable" means.
        /// </remarks>
        public bool CanPerformOpportunityAction() =>
            OpportunityActions.Current >= 1 &&
            DaysSupply.Current >= GameData.OPPORTUNITY_ACTION_SUPPLY_COST + GameData.OPPORTUNITY_ACTION_SUPPLY_THRESHOLD &&
            !IsBase;

        /// <summary>
        /// Consumes the required actions and supplies to perform an opportunity action.
        /// </summary>
        public bool PerformOpportunityAction()
        {
            try
            {
                if (CanPerformOpportunityAction())
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

        // D4 (P2 2026-08-08): the RAW fraction, no rounding — the single deploy-cost formula shared by
        // the CanChangeToState gate and the HUD availability checks. CeilToInt here made the two
        // disagree at odd maxima; display rounding is the UI's business, not the model's.
        private float GetDeployMovementCost() =>
            MovementPoints.Max * GameData.DEPLOYMENT_ACTION_MOVEMENT_COST;

        public float GetCombatMovementCost() =>
            Mathf.CeilToInt(MovementPoints.Max * GameData.COMBAT_ACTION_MOVEMENT_COST);

        public float GetIntelMovementCost() =>
            Mathf.CeilToInt(MovementPoints.Max * GameData.INTEL_ACTION_MOVEMENT_COST);

        #endregion // Actions

        #region Movement Order API

        /// <summary>
        /// Validates and begins a move order. Decrements MoveActions by 1.
        /// MP is NOT deducted here — it is deducted per-hex by MovementController.
        /// </summary>
        /// <summary>
        /// True when this unit could legally begin a move order right now (action/MP economy, mobility,
        /// posture). Read-only twin of <see cref="BeginMoveOrder"/> — the UI keys movement overlays and
        /// path previews off it; BeginMoveOrder runs the SAME gate and then spends the MoveAction.
        /// </summary>
        public bool CanBeginMoveOrder()
        {
            if (!CanMove()) return false;
            if (MoveActions.Current < 1) return false;
            if (MovementPoints.Current <= 0) return false;
            if (IsBase) return false;
            if (_deploymentPosition == DeploymentPosition.Fortified ||
                _deploymentPosition == DeploymentPosition.Entrenched ||
                _deploymentPosition == DeploymentPosition.HastyDefense)
                return false;
            return true;
        }

        public bool BeginMoveOrder()
        {
            try
            {
                if (!CanBeginMoveOrder()) return false;

                MoveActions.DecrementCurrent();
                // TODO: Supply rules pending
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(BeginMoveOrder), e);
                return false;
            }
        }

        /// <summary>
        /// Deducts movement points for a single hex step. Returns false if insufficient MP.
        /// </summary>
        public bool DeductMovementCost(int cost)
        {
            try
            {
                return ConsumeMovementPoints(cost);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(DeductMovementCost), e);
                return false;
            }
        }

        /// <summary>
        /// Force-sets movement points to a specific value. Used by ZoC halt and ambush rules.
        /// </summary>
        public void ForceSetMovementPoints(float value)
        {
            MovementPoints.SetCurrent(Mathf.Max(0f, value));
        }

        /// <summary>
        /// Force-sets action counts. Used by amphibious crossing and ambush zeroing.
        /// </summary>
        public void ForceSetActions(float moveActions, float combatActions, float intelActions)
        {
            MoveActions.SetCurrent(Mathf.Max(0f, moveActions));
            CombatActions.SetCurrent(Mathf.Max(0f, combatActions));
            IntelActions.SetCurrent(Mathf.Max(0f, intelActions));
        }

        /// <summary>
        /// Rotates facing toward a new direction, costing 1 MP per hex-edge step.
        /// Does not consume a MoveAction.
        /// </summary>
        public bool TryRotateFacing(HexDirection newFacing)
        {
            try
            {
                if (newFacing == Facing) return true;

                int currentIdx = (int)Facing;
                int targetIdx = (int)newFacing;

                // Shortest rotation around 6 edges
                int clockwise = ((targetIdx - currentIdx) % 6 + 6) % 6;
                int counterClockwise = 6 - clockwise;
                int steps = Math.Min(clockwise, counterClockwise);

                if (MovementPoints.Current < steps) return false;

                ConsumeMovementPoints(steps);
                Facing = newFacing;
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(TryRotateFacing), e);
                return false;
            }
        }

        #endregion // Movement Order API

        #region Deployment

        /// <summary>
        /// Attempt to change deployment state to a higher level (towards Embarked).
        /// </summary>
        /// <param name="onAirbase">True if unit is adjacent to an active friendly airbase unit.</param>
        /// <param name="onPort">True if unit is on a port hex.</param>
        public bool TryDeployUP(out string errorMsg, bool onAirbase = false, bool onPort = false)
        {
            if (MovementPoints.Max <= 0f)
            {
                errorMsg = "Unit has invalid movement profile; cannot deploy.";
                return false;
            }

            // D2 (P2 2026-08-08): the ladder has a top. Without this clamp, +1 from Embarked wrote the
            // undefined enum value 6, charged full costs, and silently fell back to the deployed profile.
            if (_deploymentPosition == DeploymentPosition.Embarked)
            {
                errorMsg = $"{UnitName} is already embarked — there is no higher deployment state.";
                return false;
            }

            DeploymentPosition oldPosition = _deploymentPosition;
            DeploymentPosition targetPosition = _deploymentPosition + 1;
            bool navalRoute = false;

            /* TARGET SELECTION (generalised 2026-08-04; naval added P2 2026-08-08 — §9.4.5/§9.4.7).
             * A regiment at Deployed with NO ground transport skips Mobile: to its OWN air lift if the
             * Embarked bay is populated, else to the universal NAVAL sealift if it stands on a friendly
             * port. Organic lift wins over naval — owned equipment beats the shared flotilla.
             * Asking the SLOTS (never a class label) covers every shape with no list to maintain;
             * the positional gates (airbase/port) stay in EmbarkmentChecks — what may embark WHERE is a
             * separate ruling from where deploying up should AIM. */
            bool hasGroundTransport = GetMobileProfile() != null;
            if (oldPosition == DeploymentPosition.Deployed && !hasGroundTransport)
            {
                if (GetEmbarkedProfile() != null)
                    targetPosition = DeploymentPosition.Embarked;
                else if (onPort)
                {
                    targetPosition = DeploymentPosition.Embarked;
                    navalRoute = true;
                }
            }

            // D3 (P2 2026-08-08): mounting requires something to mount. Without this, a unit with an
            // empty Mobile bay "mounted" nothing, paid full costs, and kept its deployed profile.
            if (targetPosition == DeploymentPosition.Mobile && !hasGroundTransport)
            {
                errorMsg = $"{UnitName} has no ground transport to mount.";
                return false;
            }

            // From Mobile, +1 is Embarked: organic lift if owned, else naval at a friendly port (§9.4.7).
            if (targetPosition == DeploymentPosition.Embarked && !navalRoute && GetEmbarkedProfile() == null)
            {
                if (onPort)
                    navalRoute = true;
                else
                {
                    errorMsg = $"{UnitName} has no air lift, and naval embarkation needs a friendly port.";
                    return false;
                }
            }

            if (!CanChangeToState(targetPosition, out errorMsg))
                return false;

            if (!EmbarkmentChecks(out errorMsg, targetPosition, onAirbase, onPort, navalRoute))
                return false;

            // Fortified/Entrenched skip directly to Deployed
            if (oldPosition == DeploymentPosition.Fortified || oldPosition == DeploymentPosition.Entrenched)
                _deploymentPosition = DeploymentPosition.Deployed;
            else
                _deploymentPosition = targetPosition;

            /* ⚠ The naval flag is written BEFORE costs, deliberately: ApplyDeploymentTransitionCosts
             * re-maxes movement points from the ACTIVE profile, and while naval-embarked that is the
             * shared TRN_NAVAL — set the flag after and the unit would board ships on helicopter MP. */
            if (_deploymentPosition == DeploymentPosition.Embarked)
                SetNavalEmbarked(navalRoute);

            ApplyDeploymentTransitionCosts();
            return true;
        }

        /// <summary>
        /// Attempt to change deployment state to a lower level (more defensive).
        /// </summary>
        /// <param name="onPort">True if the unit is on a friendly port hex (naval debark site).</param>
        /// <param name="onBeachhead">True if the unit is on a beachhead hex (§9.10.6.2).</param>
        public bool TryDeployDOWN(out string errorMsg, bool onPort = false, bool onBeachhead = false)
        {
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

            /* NAVAL DEBARK GATE (P2 2026-08-08, §9.5.2/§9.10.6.1): a sealifted unit lands at a friendly
             * PORT — except marines (MAR/MMAR), whose ONE naval privilege is landing on a BEACHHEAD.
             * ⚠ This identity check is deliberate doctrine, not the classification rot P1 deleted:
             * §9.10.6.1 grants the privilege to the marine IDENTITY, not to any equipment. */
            if (_deploymentPosition == DeploymentPosition.Embarked && IsNavalEmbarked)
            {
                bool marineBeachLanding = onBeachhead &&
                    (Classification == UnitClassification.MAR || Classification == UnitClassification.MMAR);
                if (!onPort && !marineBeachLanding)
                {
                    errorMsg = Classification is UnitClassification.MAR or UnitClassification.MMAR
                        ? $"{UnitName} must debark at a friendly port or onto a beachhead."
                        : $"{UnitName} must debark at a friendly port.";
                    return false;
                }
            }

            DeploymentPosition targetPosition = GetDownwardTargetPosition(_deploymentPosition);

            if (!CanChangeToState(targetPosition, out errorMsg))
                return false;

            bool leavingEmbarked = _deploymentPosition == DeploymentPosition.Embarked;
            _deploymentPosition = targetPosition;

            // Cleared BEFORE costs for the same active-profile reason as the deploy-up write.
            if (leavingEmbarked)
                SetNavalEmbarked(false);

            ApplyDeploymentTransitionCosts();
            return true;
        }

        private void ApplyDeploymentTransitionCosts()
        {
            /* D7 (P2 2026-08-08, ruled REFUSE): a transition that cannot pay its supply does not happen.
             * In practice this branch is unreachable — CanChangeToState refuses below the CRITICAL
             * threshold (0.5), which exceeds the 0.25 cost — so a false return here means the gates and
             * the costs have drifted apart, which is exactly worth a log. */
            if (!ConsumeSupplies(GameData.COMBAT_STATE_SUPPLY_TRANSITION_COST))
                Debug.LogWarning($"[{CLASS_NAME}] {UnitName} passed the deployment gates but could not " +
                    "pay the supply cost — the CanChangeToState supply gate and the transition cost have drifted.");
            DeploymentActions.DecrementCurrent();

            // Pay the transition out of the OLD profile's budget first.
            float oldMax = MovementPoints.Max;
            float movementPenalty = GameData.DEPLOYMENT_ACTION_MOVEMENT_COST * oldMax;
            float remainingMP = Mathf.Max(0f, MovementPoints.Current - movementPenalty);

            /* ⚠ THE LEFTOVER IS RESCALED, NOT CARRIED ACROSS (Bob's ruling, 2026-08-04). Movement points
             * mean different things either side of a posture change: 2 points is half a foot regiment's
             * day and one twelfth of a helicopter lift. The old code kept the ABSOLUTE figure and merely
             * clamped it, so a full-strength foot regiment paid 2 of its 4 points to board and then flew
             * TWO HEXES on a 24-point profile. Dismounting had the mirror defect. Preserving the FRACTION
             * spent is the only reading that behaves in both directions.
             *
             * ⚠ Deliberately free of any particular cost constant — it needs only the two ceilings — so
             * it survives the action/movement cost rebalance Bob has planned. */
            UpdateMovementPointsForProfile();
            MovementPoints.SetCurrent(
                MovementModeService.ScaleMovementPoints(remainingMP, oldMax, MovementPoints.Max));
        }

        /* ⚠ REWRITTEN P2 2026-08-08 (was SpecialEmbarkmentChecks) — ZERO classification cases. The gate
         * keys on WHAT IS BEING BOARDED, never on who is boarding:
         *   fixed-wing lift  -> needs an active friendly airbase (so AB/MAB keep their airbase rule as a
         *                       CONSEQUENCE of their An-12s, and any future FW-lifted unit inherits it);
         *   helo lift        -> boards anywhere;
         *   naval sealift    -> needs a friendly port (the universal §9.4.7 rule — the old MAR/MMAR
         *                       port case was this rule wearing a classification costume).
         * The deleted cases: AB/MAB by class, the SPECF + literal TRN_AN8_SV check (which left FW-lifted
         * SPECF with NO airbase gate at all — defect D8), the MAR/MMAR port case, and the AM/MAM
         * UpgradePath.HELT check (the bay invariants make a non-transport in the Embarked bay
         * impossible — EquipmentBaysTests enforces it over every template). */
        private bool EmbarkmentChecks(out string errorMsg, DeploymentPosition targetPos,
            bool onAirbase, bool onPort, bool navalRoute)
        {
            errorMsg = string.Empty;

            if (targetPos != DeploymentPosition.Embarked)
                return true;

            if (navalRoute)
            {
                if (!onPort)
                {
                    errorMsg = $"{UnitName} must be at a friendly port to embark on naval transport.";
                    return false;
                }
                return true;
            }

            var embarkedProfile = GetEmbarkedProfile();
            if (embarkedProfile == null)
            {
                errorMsg = $"{UnitName} has no air lift in its Embarked bay.";
                return false;
            }

            return embarkedProfile.TransportCategory switch
            {
                TransportCategory.HeloTransport => true,
                TransportCategory.FixedWingTransport when onAirbase => true,
                TransportCategory.FixedWingTransport => Fail(out errorMsg,
                    $"{UnitName} must be adjacent to an active friendly airbase to board fixed-wing transport."),
                _ => Fail(out errorMsg,
                    $"{UnitName}'s Embarked bay holds {embarkedProfile.WeaponType}, which is not a transport — invalid content.")
            };

            static bool Fail(out string msg, string text) { msg = text; return false; }
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

        /// <summary>
        /// Re-derives the movement-point ceiling from the ACTIVE profile and refills to it. D5 (P2
        /// 2026-08-08): for FRESH spawns positioned directly by a loader — a unit authored at
        /// Mobile/Embarked otherwise started the battle on its foot ceiling, because the constructor
        /// sizes MP from the deployed profile before the loader sets the posture.
        /// ⚠ Loader-only. SnapshotMapper must NOT call this — it restores a SAVED current-MP value,
        /// which a refill would clobber.
        /// </summary>
        public void RefreshMovementPointsForPosture()
        {
            UpdateMovementPointsForProfile();
            MovementPoints.ResetToMax();
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

            // ⚠ THE ACTION ITSELF, added 2026-07-28. This check was MISSING while
            // ApplyDeploymentTransitionCosts decremented a DeploymentAction unconditionally — so nothing
            // stopped a unit deploying up and down all turn, spending an action economy it did not have.
            // The movement-point check below is NOT a substitute: MP is refunded to the new profile's max
            // on every transition, so a unit can hold plenty of MP while having no action left.
            if (DeploymentActions.Current < 1)
            {
                errorMessage = $"{UnitName} has no deployment actions remaining this turn";
                return false;
            }

            // D4 (P2 2026-08-08): this gate and the HUD availability checks share ONE formula now
            // (GetDeployMovementCost). The HUD used CeilToInt while this used the raw fraction, so at
            // odd maxima the button greyed out for a transition the model would have allowed.
            if (MovementPoints.Current < GetDeployMovementCost())
            {
                errorMessage = $"{UnitName} does not have enough movement points to change states ({MovementPoints.Current:F1} available, {GetDeployMovementCost():F1} required)";
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
        /// <summary>
        /// Writes the naval transient state. ⚠ Callers: the P2 naval embark/debark path and
        /// SnapshotMapper restore ONLY — this is a state flag, not a capability toggle.
        /// </summary>
        public void SetNavalEmbarked(bool embarked) => IsNavalEmbarked = embarked;

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

                if (!IsFixedWingClassification(unit.Classification))
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

        /// <summary>
        /// Airbase launch gate (§11.2.3a): true only if the airbase can run air operations AND its stockpile is at
        /// or above the AIRBASE_LAUNCH_FLOOR hard reserve (5 days). Below the floor no aircraft may launch this turn
        /// — the reserve prevents partial-supply launches that would strand a sortie mid-mission. The per-sortie
        /// stockpile deduction (SORTIE_LAUNCH_COST + SORTIE_SHOT_COST, §11.2.3) is applied by the air-mission caller.
        /// </summary>
        public bool CanLaunchSortie() =>
            CanLaunchAirOperations() && DaysSupply.Current >= GameData.AIRBASE_LAUNCH_FLOOR;

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

        /* ⚠ D9 (2026-08-10): this was a FOURTH spelling of "is fixed-wing" and the shortest of them —
         * FGT / ATT / BMB / RECONA only. Because `AddAirUnit` throws on anything it rejects, an AWACS
         * could not be attached to an airbase AT ALL, and neither could a transport (which the fixed-wing
         * staging plan depends on). Now defers to the one canonical list. */
        private bool IsFixedWingClassification(UnitClassification classification) =>
            GameData.IsAirborneClassification(classification);

        #endregion // Airbase Management

        #region Supply Depot Management

        private float GetMaxStockpile() =>
            IsBase && FacilityType == FacilityType.SupplyDepot
                ? GameData.MaxStockpileBySize[DepotSize] : 0f;

        private float GetCurrentGenerationRate()
        {
            if (!IsBase || FacilityType != FacilityType.SupplyDepot) return 0f;
            // GenerationRateValues is a FRACTION of own capacity; scale by max stockpile to get days/turn.
            return GameData.GenerationRateValues[GenerationRate] * GetMaxStockpile() * GetFacilityEfficiencyMultiplier();
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

                EquipmentBays.InitializeEquipmentBays(
                    template.UnitName,
                    template.EquipmentBays.Deployed,
                    template.EquipmentBays.Mobile,
                    template.EquipmentBays.Embarked);

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
                    deployedProfile: EquipmentBays.Deployed,
                    mobileProfile: EquipmentBays.Mobile,
                    embarkedProfile: EquipmentBays.Embarked,
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
        /// Returns an ENEMY-side IntelReport about this unit, filtered by the given SpottedLevel
        /// (§12.2, the six-rung ladder — rewritten 2026-07-24):
        /// Level0: empty report (unspotted).
        /// Level1: nothing but contact — the icon carries everything at this rung, so even the name is withheld.
        /// Level2: + unit name and nationality.
        /// Level3: + deployment position.
        /// Level4: + equipment in the six COARSE buckets at MAX_INTEL_ERROR (16%).
        /// Level5: + experience and efficiency, equipment error down to MODERATE_INTEL_ERROR (8%).
        /// FULL detail is NOT reachable here — it is an ownership fact, see <see cref="GetFullIntelReport"/>.
        /// </summary>
        public IntelReport GetIntelReport(SpottedLevel spottedLevel)
        {
            try
            {
                var report = new IntelReport { IsFullDetail = false };

                if (spottedLevel <= SpottedLevel.Level1)
                    return report;   // Level0 = nothing; Level1 = contact only, the icon is the whole report

                // Level2+: name and nationality
                report.UnitName = UnitName;
                report.UnitNationality = Nationality;

                if (spottedLevel == SpottedLevel.Level2)
                    return report;

                // Level3+: deployment position
                report.DeploymentPosition = DeploymentPosition;

                if (spottedLevel == SpottedLevel.Level3)
                    return report;

                // Level4+: equipment buckets, coarse, with the rung's error band
                float errorRate = spottedLevel switch
                {
                    SpottedLevel.Level4 => GameData.MAX_INTEL_ERROR / 100f,
                    _ => GameData.MODERATE_INTEL_ERROR / 100f,   // Level5
                };

                ApplyEquipmentBuckets(report, errorRate);

                if (spottedLevel == SpottedLevel.Level4)
                    return report;

                // Level5: experience and efficiency
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

        /// <summary>
        /// The FULL view of this unit (§12.2.7): every field, all seventeen equipment buckets, zero error.
        /// This is an OWNERSHIP fact rather than a rung on the spotting ladder — it is unreachable by spotting,
        /// never stored in SpottedLevel, and never decayed. Call it for Side.Player units; call
        /// <see cref="GetIntelReport"/> for everything else.
        /// </summary>
        public IntelReport GetFullIntelReport()
        {
            try
            {
                var report = new IntelReport
                {
                    IsFullDetail = true,
                    UnitName = UnitName,
                    UnitNationality = Nationality,
                    DeploymentPosition = DeploymentPosition,
                    UnitExperienceLevel = ExperienceLevel,
                    UnitEfficiencyLevel = EfficiencyLevel,
                };

                ApplyEquipmentBuckets(report, 0f);
                return report;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetFullIntelReport), e);
                throw;
            }
        }

        private void ApplyEquipmentBuckets(IntelReport report, float errorRate)
        {
            var baseReport = EquipmentBays?.GetIntelReport();
            if (baseReport == null)
                return;

            float strengthRatio = HitPoints.Max > 0 ? HitPoints.Current / HitPoints.Max : 0f;

            // The error seed is keyed on the CURRENT HP so a report changes when the unit actually changes,
            // and only then (§12.5.5). Rounded to a whole percent to keep float drift from re-rolling it.
            int hpKey = Mathf.RoundToInt(strengthRatio * 100f);

            report.Personnel = ApplyIntelError(baseReport.Personnel, strengthRatio, errorRate, hpKey, 0);
            report.TANK = ApplyIntelError(baseReport.TANK, strengthRatio, errorRate, hpKey, 1);
            report.IFV = ApplyIntelError(baseReport.IFV, strengthRatio, errorRate, hpKey, 2);
            report.APC = ApplyIntelError(baseReport.APC, strengthRatio, errorRate, hpKey, 3);
            report.RCN = ApplyIntelError(baseReport.RCN, strengthRatio, errorRate, hpKey, 4);
            report.ART = ApplyIntelError(baseReport.ART, strengthRatio, errorRate, hpKey, 5);
            report.ROC = ApplyIntelError(baseReport.ROC, strengthRatio, errorRate, hpKey, 6);
            report.SAM = ApplyIntelError(baseReport.SAM, strengthRatio, errorRate, hpKey, 7);
            report.AAA = ApplyIntelError(baseReport.AAA, strengthRatio, errorRate, hpKey, 8);
            report.AT = ApplyIntelError(baseReport.AT, strengthRatio, errorRate, hpKey, 9);
            report.HEL = ApplyIntelError(baseReport.HEL, strengthRatio, errorRate, hpKey, 10);
            report.AWACS = ApplyIntelError(baseReport.AWACS, strengthRatio, errorRate, hpKey, 11);
            report.TRN = ApplyIntelError(baseReport.TRN, strengthRatio, errorRate, hpKey, 12);
            report.FGT = ApplyIntelError(baseReport.FGT, strengthRatio, errorRate, hpKey, 13);
            report.ATT = ApplyIntelError(baseReport.ATT, strengthRatio, errorRate, hpKey, 14);
            report.BMB = ApplyIntelError(baseReport.BMB, strengthRatio, errorRate, hpKey, 15);
            report.RCNA = ApplyIntelError(baseReport.RCNA, strengthRatio, errorRate, hpKey, 16);
        }

        private int ApplyIntelError(int baseValue, float strengthRatio, float errorRate, int hpKey, int bucketIndex)
        {
            if (baseValue <= 0) return 0;

            float scaled = baseValue * strengthRatio;

            if (errorRate <= 0f)
                return Mathf.RoundToInt(scaled);

            float offset = scaled * errorRate * IntelErrorNoise(UnitID, hpKey, bucketIndex);
            return Mathf.Max(0, Mathf.RoundToInt(scaled + offset));
        }

        /// <summary>
        /// Deterministic noise in [-1, 1] for one intel bucket (§12.5.5). Keyed on (UnitID, HP, bucket) so
        /// reselecting the same enemy REPRINTS THE SAME NUMBERS. The prior implementation called
        /// UnityEngine.Random per request, which both jittered the display on every click and leaked the true
        /// value to any player willing to sample repeatedly — repeated draws converge on the mean.
        ///
        /// Note the error rate is applied OUTSIDE this function, so a unit's Level-4 and Level-5 reports lean
        /// the same direction and differ only in magnitude. That is deliberate: better intel should tighten an
        /// estimate, not swing it to the other side of the truth.
        ///
        /// Uses an inlined FNV-1a over the ID rather than string.GetHashCode, which is not guaranteed stable
        /// across runtimes or processes and would break the round-trip after a save/load.
        /// </summary>
        private static float IntelErrorNoise(string unitId, int hpKey, int bucketIndex)
        {
            unchecked
            {
                const uint FNV_OFFSET = 2166136261u;
                const uint FNV_PRIME = 16777619u;

                uint h = FNV_OFFSET;
                if (!string.IsNullOrEmpty(unitId))
                {
                    foreach (char c in unitId)
                    {
                        h = (h ^ c) * FNV_PRIME;
                    }
                }

                h = (h ^ (uint)hpKey) * FNV_PRIME;
                h = (h ^ (uint)bucketIndex) * FNV_PRIME;

                // Final avalanche so adjacent keys don't produce adjacent outputs.
                h ^= h >> 16;
                h *= 0x7feb352du;
                h ^= h >> 15;
                h *= 0x846ca68bu;
                h ^= h >> 16;

                return (h / (float)uint.MaxValue) * 2f - 1f;
            }
        }

        #endregion // Intel Reports

        #region Debugging

        public float DebugGetCombatMovementCost() =>
            Mathf.CeilToInt(MovementPoints.Max * GameData.COMBAT_ACTION_MOVEMENT_COST);

        #endregion // Debugging
    }
}
