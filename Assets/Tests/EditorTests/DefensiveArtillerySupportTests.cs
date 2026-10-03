using System;
using System.Collections.Generic;
using System.Linq;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.AI;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    [TestFixture]
    public class DefensiveArtillerySupportTests
    {
        private TestHandler _previousHandler;
        private TestHandler _handler;
        private HexMap _map;
        private readonly List<CombatUnit> _units = new List<CombatUnit>();
        private readonly List<string> _removed = new List<string>();
        private AIPerceptionState _perception;
        private Position2D _previousMapSize;
        private Dictionary<WeaponType, float>[] _losses;
        private Dictionary<WeaponType, float>[] _dailyLosses;

        [SetUp]
        public void SetUp()
        {
            _previousHandler = AppService.GetTestHandler();
            _handler = new TestHandler();
            AppService.SetTestHandler(_handler);
            _previousMapSize = GameDataManager.CurrentMapSize;
            try
            {
                if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
                Assert.That(WeaponProfileDB.IsInitialized, Is.True);
                Assert.That(_handler.Exceptions, Is.Empty);
                _units.Clear();
                _removed.Clear();
                _perception = new AIPerceptionState();
                _map = MapFixtures.UniformMap();
                _losses = new[] { CopyLedger(Side.Player, false), CopyLedger(Side.AI, false) };
                _dailyLosses = new[] { CopyLedger(Side.Player, true), CopyLedger(Side.AI, true) };
                GameDataManager.ClearLossLedger();
            }
            catch { GameDataManager.CurrentMapSize = _previousMapSize; AppService.SetTestHandler(_previousHandler); throw; }
        }

        [TearDown]
        public void TearDown()
        {
            try { Assert.That(_handler.Exceptions, Is.Empty, string.Join("\n", _handler.Exceptions.Select(error =>
                error.ClassName + "." + error.MethodName + ": " + error.ExceptionType + ": " + error.Message))); }
            finally
            {
                if (_losses != null)
                {
                    RestoreLedger(Side.Player, false, _losses[0]); RestoreLedger(Side.AI, false, _losses[1]);
                    RestoreLedger(Side.Player, true, _dailyLosses[0]); RestoreLedger(Side.AI, true, _dailyLosses[1]);
                }
                AppService.SetTestHandler(_previousHandler);
                GameDataManager.CurrentMapSize = _previousMapSize;
            }
        }

        private static Dictionary<WeaponType, float> CopyLedger(Side side, bool daily) => new Dictionary<WeaponType, float>(
            (IDictionary<WeaponType, float>)(daily ? GameDataManager.GetDailyLossLedger(side) : GameDataManager.GetLossLedger(side)));
        private static void RestoreLedger(Side side, bool daily, Dictionary<WeaponType, float> saved)
        {
            var ledger = (IDictionary<WeaponType, float>)(daily ? GameDataManager.GetDailyLossLedger(side) : GameDataManager.GetLossLedger(side));
            ledger.Clear();
            foreach (var pair in saved) ledger.Add(pair.Key, pair.Value);
        }

        private CombatUnit Unit(string id, Side side, Position2D position,
            UnitClassification classification = UnitClassification.INF, WeaponType profile = WeaponType.INF_REG_SV,
            Nationality nationality = Nationality.USSR)
        {
            var unit = new CombatUnit(id, classification, UnitRole.GroundCombat, side, nationality,
                deployedProfile: profile, mobileProfile: WeaponType.NONE, embarkedProfile: WeaponType.NONE);
            unit.SetUnitID(id);
            unit.SetPosition(position);
            unit.SetDeploymentPosition(DeploymentPosition.Deployed);
            unit.SetExperienceLevel(ExperienceLevel.Trained);
            unit.SetEfficiencyLevel(EfficiencyLevel.FullOperations);
            // Already-established contact keeps managed rule tests out of the native intel-event publisher.
            // The separate printer integration fixture covers that publisher with real Unity objects.
            unit.SetSpottedLevel(SpottedLevel.Level4);
            unit.DaysSupply.SetCurrent(5);
            _units.Add(unit);
            return unit;
        }
        private CombatUnit Battery(string id = "battery", Side side = Side.AI, int x = 7, int y = 5) =>
            Unit(id, side, new Position2D(x, y), UnitClassification.SPA, WeaponType.SPA_2S19_SV);
        private GroundCombatContext Context(Side phasing = Side.Player) =>
            new GroundCombatContext(phasing, _units, _perception, 3, unit => _removed.Add(unit.UnitID));
        private (CombatUnit attacker, CombatUnit defender) Pair(Side phasing = Side.Player) =>
            (Unit("attacker", phasing, new Position2D(5, 5)),
             Unit("defender", phasing == Side.Player ? Side.AI : Side.Player, new Position2D(6, 5)));

        private sealed class Dice : ICombatRandom
        {
            public readonly List<int> Sides = new List<int>();
            public Func<int, int, int> Roll;
            public int RollDie(int sides)
            {
                Sides.Add(sides);
                return Roll != null ? Roll(sides, Sides.Count) : sides == 100 ? 100 : 1;
            }
        }

        [Test]
        public void NoBattery_PreservesOrdinaryDirectEngagement()
        {
            var (attacker, defender) = Pair();
            var result = GroundCombatAction.Execute(attacker, defender, _map, new Dice(), Context());
            Assert.That(result.Executed && result.MainAttackResolved, Is.True);
            Assert.That(result.Support.Selected, Is.False);
            Assert.That(attacker.CombatActions.Current, Is.Zero);
        }

        [TestCase(Side.Player)]
        [TestCase(Side.AI)]
        public void HoldingAttacker_ContinuesAfterOneSupportShot_WithCorrectCostsAndReveal(Side phasing)
        {
            var (attacker, defender) = Pair(phasing);
            var battery = Battery(side: defender.Side);
            battery.SetSpottedLevel(SpottedLevel.Level0);
            float combat = battery.CombatActions.Current, mp = battery.MovementPoints.Current;
            float move = battery.MoveActions.Current, deploy = battery.DeploymentActions.Current, intel = battery.IntelActions.Current;
            var result = GroundCombatAction.Execute(attacker, defender, _map, new Dice(), Context(phasing));
            Assert.That(result.MainAttackResolved, Is.True);
            Assert.That(result.Support.Fired, Is.True);
            Assert.That(result.Support.Combat.CounterBatteryFired, Is.False);
            Assert.That(battery.OpportunityActions.Current, Is.Zero);
            Assert.That(battery.DaysSupply.Current, Is.EqualTo(4.5f));
            Assert.That(battery.CombatActions.Current, Is.EqualTo(combat));
            Assert.That(battery.MovementPoints.Current, Is.EqualTo(mp));
            Assert.That(battery.MoveActions.Current, Is.EqualTo(move));
            Assert.That(battery.DeploymentActions.Current, Is.EqualTo(deploy));
            Assert.That(battery.IntelActions.Current, Is.EqualTo(intel));
            Assert.That(battery.HasInitiatedCombatThisTurn, Is.False);
            Assert.That(battery.HasFoughtThisTurn && battery.HasEmittedThisTurn, Is.True);
            Assert.That(attacker.HasInitiatedCombatThisTurn, Is.True);
            Assert.That(attacker.OpportunityActions.Current, Is.EqualTo(attacker.OpportunityActions.Max));
            Assert.That(battery.Side == Side.AI ? battery.SpottedLevel : _perception.LevelOf(battery.UnitID), Is.EqualTo(SpottedLevel.Level1));
        }

        [Test]
        public void SupportKill_CancelsMain_RemovesAttackerAndKeepsCommittedCost()
        {
            var (attacker, defender) = Pair();
            var battery = Battery();
            attacker.HitPoints.SetCurrent(1);
            float defenderHp = defender.HitPoints.Current, attackerSupply = attacker.DaysSupply.Current;
            var result = GroundCombatAction.Execute(attacker, defender, _map, new Dice(), Context());
            Assert.That(result.Executed, Is.True);
            Assert.That(result.MainAttackResolved, Is.False);
            Assert.That(result.AttackerDestroyed && result.AttackerRemovedFromMap, Is.True);
            Assert.That(_removed, Is.EquivalentTo(new[] { attacker.UnitID }));
            Assert.That(defender.HitPoints.Current, Is.EqualTo(defenderHp));
            Assert.That(defender.HasFoughtThisTurn || defender.HasEmittedThisTurn, Is.False);
            Assert.That(attacker.HasEmittedThisTurn, Is.False);
            Assert.That(attacker.HasInitiatedCombatThisTurn, Is.True);
            Assert.That(attacker.CombatActions.Current, Is.Zero);
            Assert.That(attacker.DaysSupply.Current, Is.EqualTo(attackerSupply));
            Assert.That(result.AutomaticAdvanceAvailable, Is.False);
            Assert.That(result.Support.Combat.PrestigeOwedToFirer, Is.GreaterThan(0));
            Assert.That(GameDataManager.GetLossLedger(Side.Player).Values.Sum(), Is.GreaterThan(0));
        }

        [Test]
        public void Selection_IsStableUniqueAndUniformOverEligiblePool()
        {
            var (attacker, defender) = Pair();
            var c = Battery("c"); var a = Battery("a"); var b = Battery("b");
            _units.Add(a); // duplicate reference must not increase its chance
            var empty = Battery("unavailable"); empty.OpportunityActions.SetCurrent(0);
            var context = Context();
            var expected = new[] { a, b, c };
            for (int index = 0; index < 3; index++)
            {
                int chosen = index + 1;
                var dice = new Dice { Roll = (sides, call) => chosen };
                Assert.That(DefensiveArtillerySupport.Select(attacker, defender, _map, context, dice), Is.SameAs(expected[index]));
                Assert.That(dice.Sides, Is.EqualTo(new[] { 3 }));
            }
            Assert.That(_units.All(unit => unit.OpportunityActions.Current == unit.OpportunityActions.Max || unit == empty), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ZeroOrOneEligibleBattery_ConsumesNoSelectionDie(int count)
        {
            var (attacker, defender) = Pair();
            var battery = count == 1 ? Battery() : null;
            var dice = new Dice();
            Assert.That(DefensiveArtillerySupport.Select(attacker, defender, _map, Context(), dice), Is.SameAs(battery));
            Assert.That(dice.Sides, Is.Empty);
        }

        [TestCase("empty")]
        [TestCase("supply")]
        [TestCase("wrong_side")]
        [TestCase("destroyed")]
        [TestCase("off_map")]
        [TestCase("out_of_range")]
        [TestCase("embarked")]
        public void IneligibleBattery_IsNotSelectedOrCharged(string reason)
        {
            var (attacker, defender) = Pair();
            var battery = Battery(side: reason == "wrong_side" ? Side.Player : Side.AI);
            if (reason == "empty") battery.OpportunityActions.SetCurrent(0);
            if (reason == "supply") battery.DaysSupply.SetCurrent(1.99f);
            if (reason == "destroyed") battery.HitPoints.SetCurrent(0);
            if (reason == "off_map") battery.SetPosition(new Position2D(-1, -1));
            if (reason == "out_of_range") battery.SetPosition(new Position2D(11, 11));
            if (reason == "embarked") battery.SetDeploymentPosition(DeploymentPosition.Embarked);
            float supply = battery.DaysSupply.Current, opportunity = battery.OpportunityActions.Current;
            int messagesBefore = _handler.UiMessageCount;
            Assert.That(DefensiveArtillerySupport.Select(attacker, defender, _map, Context(), new Dice()), Is.Null);
            Assert.That(battery.DaysSupply.Current, Is.EqualTo(supply));
            Assert.That(battery.OpportunityActions.Current, Is.EqualTo(opportunity));
            Assert.That(_handler.UiMessageCount, Is.EqualTo(messagesBefore));
        }

        [Test]
        public void ExactlyTwoSupplyAndZeroCombatOrMovement_StillAllowsSupport()
        {
            var (attacker, defender) = Pair();
            var battery = Battery();
            battery.DaysSupply.SetCurrent(2); battery.CombatActions.SetCurrent(0); battery.MovementPoints.SetCurrent(0);
            var result = GroundCombatAction.Execute(attacker, defender, _map, new Dice(), Context());
            Assert.That(result.Support.Fired, Is.True);
            Assert.That(battery.DaysSupply.Current, Is.EqualTo(1.5f));
        }

        [Test]
        public void DefenderCannotSupportItself_ButRetainsNormalReturnFire()
        {
            var attacker = Unit("attacker", Side.Player, new Position2D(5, 5));
            var defender = Battery("defender", Side.AI, 6, 5);
            var result = GroundCombatAction.Execute(attacker, defender, _map, new Dice(), Context());
            Assert.That(result.MainAttackResolved, Is.True);
            Assert.That(result.Support.Selected, Is.False);
            Assert.That(defender.OpportunityActions.Current, Is.EqualTo(1));
            Assert.That(defender.HasEmittedThisTurn, Is.True);
        }

        [Test]
        public void InitiatingArtillery_MustUseIndirectPipeline_WithoutSupport()
        {
            var attacker = Battery("attacker", Side.Player, 5, 5);
            var defender = Unit("defender", Side.AI, new Position2D(6, 5));
            var battery = Battery();
            var result = GroundCombatAction.Execute(attacker, defender, _map, new Dice(), Context());
            Assert.That(result.Executed, Is.False);
            Assert.That(attacker.CombatActions.Current, Is.EqualTo(1));
            Assert.That(battery.OpportunityActions.Current, Is.EqualTo(1));
            var indirect = IndirectCombatAction.Execute(attacker, defender, _map, new Dice());
            Assert.That(indirect.Executed, Is.True);
            Assert.That(battery.OpportunityActions.Current, Is.EqualTo(1));
        }

        [Test]
        public void PhaseMismatch_RejectsBeforeAnyCostsOrDice()
        {
            var (attacker, defender) = Pair(); Battery();
            var dice = new Dice();
            var result = GroundCombatAction.Execute(attacker, defender, _map, dice, Context(Side.AI));
            Assert.That(result.Executed, Is.False);
            Assert.That(attacker.CombatActions.Current, Is.EqualTo(1));
            Assert.That(dice.Sides, Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void BlockedRetreat_CancelsEvenWhenSurrenderCheckHoldsPosition(bool survives)
        {
            var (attacker, defender) = Pair(); Battery();
            foreach (HexDirection direction in HexArc.RearArc(HexMapUtil.GetGeneralDirection(attacker.MapPos, new Position2D(7, 5))))
                _map.GetHexAt(HexMapUtil.GetNeighborPosition(attacker.MapPos, direction)).SetTerrain(TerrainType.Impassable);
            var dice = new Dice { Roll = (sides, call) => sides == 20 ? (survives ? 20 : 1) : sides == 10 ? 8 : sides == 100 ? 100 : 1 };
            var result = GroundCombatAction.Execute(attacker, defender, _map, dice, Context());
            Assert.That(result.Support.Combat.TargetOutcome, Is.Not.EqualTo(StandOutcome.Hold));
            Assert.That(result.Support.Combat.TargetHeldInPlace, Is.EqualTo(survives));
            Assert.That(result.AttackerDestroyed, Is.EqualTo(!survives));
            Assert.That(result.AttackerRemovedFromMap, Is.EqualTo(!survives));
            Assert.That(result.MainAttackResolved, Is.False);
            Assert.That(attacker.MapPos, Is.EqualTo(new Position2D(5, 5)));
            Assert.That(attacker.CombatActions.Current, Is.Zero);
            Assert.That(defender.HasFoughtThisTurn, Is.False);
        }

        [Test]
        public void SelectionFailure_DoesNotChargeAnyBatteryOrRunMainAttack()
        {
            var (attacker, defender) = Pair(); var a = Battery("a"); var b = Battery("b");
            var result = GroundCombatAction.Execute(attacker, defender, _map, new Dice { Roll = (sides, call) => 0 }, Context());
            Assert.That(result.Executed && result.ResolutionFailed, Is.True);
            Assert.That(result.MainAttackResolved || result.Support.Selected, Is.False);
            Assert.That(attacker.CombatActions.Current, Is.Zero);
            Assert.That(a.OpportunityActions.Current + b.OpportunityActions.Current, Is.EqualTo(2));
            Assert.That(_handler.ExceptionCount, Is.EqualTo(1));
            _handler.Clear();
        }

        [Test]
        public void Visibility_UsesObserverSideKnowledgeAndCurrentPosition_WithoutGlobalReveal()
        {
            var target = Unit("distant", Side.Player, new Position2D(10, 10));
            var battery = Battery(x: 0, y: 0);
            target.SetSpottedLevel(SpottedLevel.Level5); // player's view is meaningless for AI-side knowledge
            var context = Context();
            Assert.That(context.CanObserve(Side.AI, target, _map), Is.False);
            _perception.RecordSpot(target.UnitID, new Position2D(9, 10), 1, target.Classification, 100, 4);
            Assert.That(context.CanObserve(Side.AI, target, _map), Is.False, "Stale contact position cannot authorize fire.");
            _perception.RecordSpot(target.UnitID, target.MapPos, 3, target.Classification, 100, 4);
            Assert.That(context.CanObserve(Side.AI, target, _map), Is.True);
            Assert.That(battery.SpottedLevel, Is.EqualTo(SpottedLevel.Level4));
        }

        [Test]
        public void CurrentPassiveObserver_CanAuthorizeSupportWithoutPromotingItsTargetIntel()
        {
            var (attacker, defender) = Pair(); var battery = Battery();
            Assert.That(_perception.LevelOf(attacker.UnitID), Is.EqualTo(SpottedLevel.Level0));
            Assert.That(DefensiveArtillerySupport.IsEligible(battery, attacker, defender, _map, Context()), Is.True);
            Assert.That(_perception.LevelOf(attacker.UnitID), Is.EqualTo(SpottedLevel.Level0));
        }

        [TestCase(UnitClassification.ART, WeaponType.ART_LIGHT_SV, true)]
        [TestCase(UnitClassification.SPA, WeaponType.SPA_2S19_SV, true)]
        [TestCase(UnitClassification.ROC, WeaponType.ROC_BM21_SV, true)]
        [TestCase(UnitClassification.BM, WeaponType.ROC_SCUD_SV, false)]
        [TestCase(UnitClassification.INF, WeaponType.SPA_2S19_SV, false)]
        public void ArtilleryClassAndExistingOpportunityAllowance_BothMatter(UnitClassification classification, WeaponType profile, bool eligible)
        {
            var (attacker, defender) = Pair();
            var battery = Unit("battery", Side.AI, new Position2D(7, 5), classification, profile);
            Assert.That(DefensiveArtillerySupport.IsEligible(battery, attacker, defender, _map, Context()), Is.EqualTo(eligible));
        }

        [TestCase(DeploymentPosition.Deployed, true)]
        [TestCase(DeploymentPosition.Mobile, false)]
        public void TowedBattery_UsesActiveGunOrTruckRange(DeploymentPosition posture, bool eligible)
        {
            var (attacker, defender) = Pair();
            var battery = Unit("towed", Side.AI, new Position2D(7, 5), UnitClassification.ART, WeaponType.ART_LIGHT_SV);
            battery.EquipmentBays.InitializeEquipmentBays("towed", WeaponType.ART_LIGHT_SV, WeaponType.TRK_GEN_SV, WeaponType.NONE);
            battery.SetDeploymentPosition(posture);
            Assert.That(DefensiveArtillerySupport.IsEligible(battery, attacker, defender, _map, Context()), Is.EqualTo(eligible));
        }

        [Test]
        public void SupportRange_UsesAttackerPosition_IncludingMaximumRangeButExcludingZero()
        {
            var (attacker, defender) = Pair(); var battery = Battery();
            battery.SetPosition(new Position2D(5, 5 + (int)battery.ActiveIndirectRange));
            Assert.That(DefensiveArtillerySupport.IsEligible(battery, attacker, defender, _map, Context()), Is.True);
            battery.SetPosition(new Position2D(5, 6 + (int)battery.ActiveIndirectRange));
            Assert.That(DefensiveArtillerySupport.IsEligible(battery, attacker, defender, _map, Context()), Is.False);
            battery.SetPosition(attacker.MapPos);
            Assert.That(DefensiveArtillerySupport.IsEligible(battery, attacker, defender, _map, Context()), Is.False);
        }

        [Test]
        public void AircraftAndNavalTargets_DoNotTriggerGroundSupport()
        {
            var (attacker, defender) = Pair(); var battery = Battery();
            defender.SetNavalEmbarked(true);
            Assert.That(DefensiveArtillerySupport.IsEligible(battery, attacker, defender, _map, Context()), Is.False);
            defender.SetNavalEmbarked(false);
            defender.EquipmentBays.InitializeEquipmentBays("defender", WeaponType.INF_AM_SV, WeaponType.NONE, WeaponType.HEL_MI8T_SV);
            defender.SetDeploymentPosition(DeploymentPosition.Embarked);
            Assert.That(DefensiveArtillerySupport.IsEligible(battery, attacker, defender, _map, Context()), Is.False);
        }

        [Test]
        public void MainLane_IsBuiltFromAttackerStrengthAfterSupport()
        {
            var (attacker, defender) = Pair(); var battery = Battery();
            var dice = new Dice { Roll = (sides, call) => sides == 100 ? 100 : sides == 10 ? 1 : sides };
            float startingHp = attacker.HitPoints.Current;
            var result = GroundCombatAction.Execute(attacker, defender, _map, dice, Context());
            Assert.That(result.MainAttackResolved, Is.True);
            var comparison = Unit("comparison", Side.Player, new Position2D(5, 5));
            comparison.HitPoints.SetCurrent(startingHp - result.Support.Combat.DamageToTarget);
            comparison.Facing = attacker.Facing;
            var lane = CombatResolver.BuildForwardLane(comparison, defender,
                new DirectAttackContext { DefenderTerrain = TerrainType.Clear }, CombatResolver.ComputeFlank(comparison, defender));
            int expectedDamage = CombatEngine.ResolveLane(lane, new Dice { Roll = (sides, call) => sides == 10 ? 1 : sides });
            Assert.That(result.DamageToDefender, Is.EqualTo(expectedDamage));
            comparison.HitPoints.SetCurrent(startingHp);
            var unhurtLane = CombatResolver.BuildForwardLane(comparison, defender,
                new DirectAttackContext { DefenderTerrain = TerrainType.Clear }, CombatResolver.ComputeFlank(comparison, defender));
            Assert.That(lane.FirerQualityMult, Is.LessThan(unhurtLane.FirerQualityMult));
        }

        [Test]
        public void OrdinaryCounterBattery_KeepsItsSeparateOpportunityAndSupplyRule()
        {
            var firer = Battery("firer", Side.Player, 5, 5);
            var target = Battery("target", Side.AI, 7, 5);
            var result = IndirectCombatAction.Execute(firer, target, _map, new Dice());
            Assert.That(result.Executed && result.CounterBatteryFired, Is.True);
            Assert.That(target.OpportunityActions.Current, Is.Zero);
            Assert.That(target.DaysSupply.Current, Is.EqualTo(5), "A 100 supply roll avoids the CB-specific loss; generic .5 cost must not leak in.");
            Assert.That(target.HasInitiatedCombatThisTurn, Is.False);
        }

        [Test]
        public void DistinctNationalitiesDoNotOverrideSideAllegiance_AndUnregisteredBatteryCannotFire()
        {
            var (attacker, defender) = Pair();
            var battery = Unit("allied battery", Side.AI, new Position2D(7, 5), UnitClassification.SPA,
                WeaponType.SPA_2S19_SV, Nationality.MJ);
            Assert.That(battery.Nationality, Is.Not.EqualTo(defender.Nationality));
            Assert.That(DefensiveArtillerySupport.IsEligible(battery, attacker, defender, _map, Context()), Is.True);
            _units.Remove(battery);
            Assert.That(DefensiveArtillerySupport.IsEligible(battery, attacker, defender, _map, Context()), Is.False);
        }

        [Test]
        public void EachCommittedAttack_HasItsOwnSingleReaction_AndSpentBatteryStaysUnavailable()
        {
            var (attacker, defender) = Pair();
            var a = Battery("a"); var b = Battery("b");
            attacker.CombatActions.SetMax(2); attacker.CombatActions.SetCurrent(2);
            var first = GroundCombatAction.Execute(attacker, defender, _map, new Dice(), Context());
            var second = GroundCombatAction.Execute(attacker, defender, _map, new Dice(), Context());
            Assert.That(first.Support.Battery, Is.SameAs(a));
            Assert.That(second.Support.Battery, Is.SameAs(b));
            Assert.That(a.OpportunityActions.Current + b.OpportunityActions.Current, Is.Zero);
        }

        [Test]
        public void RngFailureAfterCommit_CancelsWithoutPhantomShotOrRefund()
        {
            var (attacker, defender) = Pair(); var battery = Battery();
            var result = GroundCombatAction.Execute(attacker, defender, _map,
                new Dice { Roll = (sides, call) => throw new InvalidOperationException("Test dice failure") }, Context());
            Assert.That(result.Executed && result.ResolutionFailed, Is.True);
            Assert.That(result.MainAttackResolved || result.Support.Fired, Is.False);
            Assert.That(attacker.CombatActions.Current, Is.Zero);
            Assert.That(battery.OpportunityActions.Current, Is.Zero);
            Assert.That(battery.DaysSupply.Current, Is.EqualTo(4.5f));
            Assert.That(attacker.HitPoints.Current, Is.EqualTo(attacker.HitPoints.Max));
            Assert.That(attacker.HasEmittedThisTurn || battery.HasEmittedThisTurn || defender.HasEmittedThisTurn, Is.False);
            Assert.That(_handler.ExceptionCount, Is.GreaterThan(0));
            _handler.Clear(); // this test explicitly expects the failed injected dice
        }

        [Test]
        public void SelectedBatteryBecomesInvalid_NoReselectionOrHiddenUnitMessage()
        {
            var (attacker, defender) = Pair(); var a = Battery("hidden a"); var b = Battery("hidden b");
            var dice = new Dice { Roll = (sides, call) => { a.DaysSupply.SetCurrent(0); return 1; } };
            var result = GroundCombatAction.Execute(attacker, defender, _map, dice, Context());
            Assert.That(result.ResolutionFailed, Is.True);
            Assert.That(result.MainAttackResolved || result.Support.Fired, Is.False);
            Assert.That(dice.Sides, Is.EqualTo(new[] { 2 }));
            Assert.That(a.OpportunityActions.Current + b.OpportunityActions.Current, Is.EqualTo(2));
            Assert.That(_handler.UiMessages.Any(message => message.Contains("hidden")), Is.False);
            Assert.That(_handler.ExceptionCount, Is.EqualTo(1));
            _handler.Clear();
        }

        [Test]
        public void RemovalFailure_RetainsResolvedSupportAndCost_ButDoesNotClaimSuccessfulRemoval()
        {
            var (attacker, defender) = Pair(); var battery = Battery();
            attacker.HitPoints.SetCurrent(1);
            var context = new GroundCombatContext(Side.Player, _units, _perception, 1,
                unit => throw new InvalidOperationException("Injected roster failure"));
            var result = GroundCombatAction.Execute(attacker, defender, _map, new Dice(), context);
            Assert.That(result.ResolutionFailed && result.Support.Fired && result.AttackerDestroyed, Is.True);
            Assert.That(result.MainAttackResolved || result.AttackerRemovedFromMap, Is.False);
            Assert.That(attacker.CombatActions.Current, Is.Zero);
            Assert.That(battery.OpportunityActions.Current, Is.Zero);
            Assert.That(_handler.ExceptionCount, Is.EqualTo(2));
            _handler.Clear();
        }

        [Test]
        public void SeededSelection_IgnoresRosterEnumerationOrder()
        {
            var (attacker, defender) = Pair(); Battery("a"); Battery("b"); Battery("c");
            var original = Context();
            _units.Reverse();
            var reversed = Context();
            var diceA = new CombatRandom(29); var diceB = new CombatRandom(29);
            var first = Enumerable.Range(0, 30).Select(index => DefensiveArtillerySupport.Select(attacker, defender, _map, original, diceA).UnitID).ToArray();
            var second = Enumerable.Range(0, 30).Select(index => DefensiveArtillerySupport.Select(attacker, defender, _map, reversed, diceB).UnitID).ToArray();
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void FailedSurrenderDie_DoesNotApplyFallbackDamageOrPostureChange()
        {
            var (attacker, defender) = Pair(); var battery = Battery();
            attacker.SetDeploymentPosition(DeploymentPosition.HastyDefense);
            var indirect = new IndirectAttackContext { TargetTerrain = TerrainType.Clear, SuppressCounterBattery = true };
            var probe = new Dice();
            int damage = CombatEngine.ResolveLane(CombatResolver.BuildIndirectForwardLane(battery, attacker, indirect), probe);
            int stand = StandCheck.ComputeStandValue(CombatResolver.BuildIndirectTargetStand(battery, attacker, indirect, damage)) + 1;
            Assert.That(stand, Is.InRange(1, 10));
            foreach (HexDirection direction in HexArc.RearArc(HexMapUtil.GetGeneralDirection(attacker.MapPos, battery.MapPos)))
                _map.GetHexAt(HexMapUtil.GetNeighborPosition(attacker.MapPos, direction)).SetTerrain(TerrainType.Impassable);
            float hp = attacker.HitPoints.Current;
            var result = GroundCombatAction.Execute(attacker, defender, _map, new Dice { Roll = (sides, call) =>
                sides == 20 ? throw new InvalidOperationException("Late surrender die")
                : call == probe.Sides.Count + 1 ? stand : sides == 100 ? 100 : 1 }, Context());
            Assert.That(result.Support.Combat.TargetOutcome, Is.EqualTo(StandOutcome.Retreat));
            Assert.That(result.ResolutionFailed && result.Support.Fired, Is.True);
            Assert.That(result.MainAttackResolved || result.Support.Combat.TargetHeldInPlace || result.Support.Combat.TargetMoved, Is.False);
            Assert.That(attacker.HitPoints.Current, Is.EqualTo(hp - damage));
            Assert.That(attacker.DeploymentPosition, Is.EqualTo(DeploymentPosition.HastyDefense));
            Assert.That(attacker.MapPos, Is.EqualTo(new Position2D(5, 5)));
            Assert.That(attacker.CombatActions.Current, Is.Zero);
            Assert.That(battery.DaysSupply.Current, Is.EqualTo(4.5f));
            Assert.That(_handler.Exceptions.Any(error => error.Message == "Late surrender die"), Is.True);
            Assert.That(_handler.Exceptions.All(error => error.Message == "Late surrender die" ||
                error.Message == "Combat dice failed; the committed attack has stopped." ||
                error.Message == "The support mission did not complete; the main attack was stopped."), Is.True);
            _handler.Clear();
        }

        [Test]
        public void SharedRetreatFailureMode_RejectsRawDiceFallbackBeforeDamage()
        {
            var (unit, enemy) = Pair();
            unit.SetDeploymentPosition(DeploymentPosition.HastyDefense);
            foreach (HexDirection direction in HexArc.RearArc(HexMapUtil.GetGeneralDirection(unit.MapPos, enemy.MapPos)))
                _map.GetHexAt(HexMapUtil.GetNeighborPosition(unit.MapPos, direction)).SetTerrain(TerrainType.Impassable);
            float hp = unit.HitPoints.Current;
            Assert.Throws<InvalidOperationException>(() => RetreatResolver.ResolveDisplacement(enemy, unit,
                StandOutcome.Retreat, _map, new Dice { Roll = (sides, call) => throw new InvalidOperationException("Raw retreat dice") }, true, Context()));
            Assert.That(unit.HitPoints.Current, Is.EqualTo(hp));
            Assert.That(unit.MapPos, Is.EqualTo(new Position2D(5, 5)));
            Assert.That(unit.DeploymentPosition, Is.EqualTo(DeploymentPosition.HastyDefense));
            Assert.That(_handler.ExceptionCount, Is.EqualTo(1));
            Assert.That(_handler.Exceptions.Single().Message, Is.EqualTo("Raw retreat dice"));
            _handler.Clear();
        }

        [Test]
        public void SupportEfficiencyLoss_AppliesToBothParticipantsBeforeMainLane()
        {
            var (attacker, defender) = Pair(); var battery = Battery();
            int efficiencyDice = 0;
            var result = GroundCombatAction.Execute(attacker, defender, _map, new Dice { Roll = (sides, call) =>
                sides == 100 ? ++efficiencyDice <= 2 ? 1 : 100 : 1 }, Context());
            Assert.That(result.MainAttackResolved, Is.True);
            var reduced = (EfficiencyLevel)((int)EfficiencyLevel.FullOperations - 1);
            Assert.That(battery.EfficiencyLevel, Is.EqualTo(reduced));
            Assert.That(attacker.EfficiencyLevel, Is.EqualTo(reduced));
            var comparison = Unit("comparison", Side.Player, new Position2D(5, 5));
            comparison.HitPoints.SetCurrent(comparison.HitPoints.Max - result.Support.Combat.DamageToTarget);
            comparison.SetEfficiencyLevel(reduced);
            comparison.Facing = attacker.Facing;
            var context = new DirectAttackContext { DefenderTerrain = TerrainType.Clear };
            int expected = CombatEngine.ResolveLane(CombatResolver.BuildForwardLane(comparison, defender, context,
                CombatResolver.ComputeFlank(comparison, defender)), new Dice());
            Assert.That(result.DamageToDefender, Is.EqualTo(expected));
            Assert.That(battery.DaysSupply.Current, Is.EqualTo(4.5f));
        }

        [TestCase(Side.Player)]
        [TestCase(Side.AI)]
        public void SupportReveal_DoesNotExceedLevelFive(Side phasing)
        {
            var (attacker, defender) = Pair(phasing); var battery = Battery(side: defender.Side);
            battery.SetSpottedLevel(SpottedLevel.Level5);
            _perception.RecordSpot(battery.UnitID, battery.MapPos, 3, battery.Classification, 100, 4, SpottedLevel.Level5);
            var result = GroundCombatAction.Execute(attacker, defender, _map, new Dice(), Context(phasing));
            Assert.That(result.Support.Fired, Is.True);
            Assert.That(battery.Side == Side.AI ? battery.SpottedLevel : _perception.LevelOf(battery.UnitID), Is.EqualTo(SpottedLevel.Level5));
        }
    }
}
