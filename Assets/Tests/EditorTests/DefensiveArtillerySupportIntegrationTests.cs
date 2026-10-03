using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Models;
using HammerAndSickle.Models.AI;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>Native Unity tests: real occupancy/retreat and the EventManager -> PrinterControl -> TMP consumer.</summary>
    [TestFixture]
    public class DefensiveArtillerySupportIntegrationTests : BaseTestFixture
    {
        private TestHandler _priorHandler;
        private EventManager _priorEvents;
        private Dictionary<string, int> _priorEventCounts;
        private bool _priorAttached, _priorVerbose;
        private Func<int> _priorTurnProvider;
        private GameObject _eventRoot, _printerRoot, _panel;
        private PrinterControl _printer;
        private TextMeshProUGUI _text;
        private EventManager _events;
        private static readonly FieldInfo EventInstance = typeof(EventManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo Attached = typeof(PrinterDispatch).GetField("_attached", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly string[] OwnedEvents = { nameof(EventManager.OnPrinterMessage),
            nameof(EventManager.OnPrinterPreviousRequested), nameof(EventManager.OnUnitSpottedLevelChanged) };

        public override void OneTimeSetUp()
        {
            _priorHandler = AppService.GetTestHandler();
            try
            {
                base.OneTimeSetUp();
                if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
                Assert.That(TestHandler.Exceptions, Is.Empty);
            }
            catch { AppService.SetTestHandler(_priorHandler); throw; }
        }

        public override void OneTimeTearDown()
        {
            try { base.OneTimeTearDown(); }
            finally { AppService.SetTestHandler(_priorHandler); }
        }

        public override void SetUp()
        {
            base.SetUp();
            GameManager.ClearAll();
            GameManager.InvalidateOccupancy();
            GameDataManager.CurrentHexMap = MapFixtures.UniformMap();
            _priorEvents = (EventManager)EventInstance.GetValue(null);
            _priorEventCounts = OwnedEvents.ToDictionary(name => name, name => SubscriptionCount(_priorEvents, name));
            _priorAttached = (bool)Attached.GetValue(null);
            _priorVerbose = PrinterDispatch.Verbose;
            _priorTurnProvider = PrinterMessage.TurnProvider;
            try
            {
                // Preserve any scene-owned singleton and its subscriptions. No scene or asset is saved.
                EventInstance.SetValue(null, null);
                Attached.SetValue(null, false);
                _eventRoot = new GameObject("Support test events");
                _events = _eventRoot.AddComponent<EventManager>();
                // EditMode AddComponent need not run this MonoBehaviour's Awake. Bind the test publisher
                // explicitly: otherwise Instance may find the scene publisher while navigation uses _events.
                EventInstance.SetValue(null, _events);
                _printerRoot = new GameObject("Support test printer");
                _printer = _printerRoot.AddComponent<PrinterControl>();
                // Match saved BattleScene: same-object panel, one TMP display, no navigation/chrome wiring.
                _panel = _printerRoot;
                _text = new GameObject("Message", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                _text.transform.SetParent(_panel.transform);
                _text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/xFont Assets/VT323/VT323-Regular SDF.asset");
                Assert.That(_text.font, Is.Not.Null);
                _text.rectTransform.sizeDelta = new Vector2(488.23f, 252.27f);
                SetField(_printer, "_panelRoot", _panel);
                SetField(_printer, "_messageText", _text);
                SetField(_printer, "_fontSize", 25f);
                SetField(_printer, "_verbose", true);
                SetField(_printer, "_printerVolume", 0f);
                _printer.Initialize();
                PrinterMessage.TurnProvider = () => 3;
                Assert.That(EventManager.Instance, Is.SameAs(_events));
                foreach (string name in OwnedEvents)
                    Assert.That(SubscriptionCount(_events, name), Is.EqualTo(1), name);
                Assert.That(TestHandler.Exceptions, Is.Empty);
            }
            catch { CleanupPrinter(); throw; }
        }

        public override void TearDown()
        {
            try
            {
                CleanupPrinter();
                Assert.That(TestHandler.Exceptions, Is.Empty, string.Join("\n", TestHandler.Exceptions.Select(error =>
                    error.ClassName + "." + error.MethodName + ": " + error.Message)));
            }
            finally { base.TearDown(); }
        }

        private void CleanupPrinter()
        {
            try
            {
                // Match explicit Initialize with explicit teardown. EditMode destruction is not a guarantee
                // that OnDestroy ran; leaked static dispatch delegates duplicate subsequent intel reports.
                if (_printer != null)
                    typeof(PrinterControl).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_printer, null);
                foreach (string name in OwnedEvents)
                    Assert.That(SubscriptionCount(_events, name), Is.Zero, "Fixture leaked " + name);
                foreach (string name in OwnedEvents)
                    Assert.That(SubscriptionCount(_priorEvents, name), Is.EqualTo(_priorEventCounts[name]),
                        "Fixture changed the scene publisher's " + name);
            }
            finally
            {
                try
                {
                    if (_printerRoot != null) UnityEngine.Object.DestroyImmediate(_printerRoot);
                    if (_eventRoot != null) UnityEngine.Object.DestroyImmediate(_eventRoot);
                }
                finally
                {
                    EventInstance.SetValue(null, _priorEvents);
                    Attached.SetValue(null, _priorAttached);
                    PrinterDispatch.Verbose = _priorVerbose;
                    PrinterMessage.TurnProvider = _priorTurnProvider;
                }
            }
        }
        private static int SubscriptionCount(EventManager publisher, string eventName) => publisher == null ? 0 :
            ((Delegate)typeof(EventManager).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(publisher))?.GetInvocationList().Length ?? 0;
        private static void SetField(object owner, string name, object value) =>
            owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        private List<PrinterMessage> History => (List<PrinterMessage>)typeof(PrinterControl)
            .GetField("_history", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_printer);
        private PrinterMessage CombatReport => History.Last(message => message.Category == PrinterCategory.Combat);
        private void AcceptExpectedDiceFailure(string cause)
        {
            Assert.That(TestHandler.Exceptions.Any(error => error.Message == cause), Is.True);
            Assert.That(TestHandler.Exceptions.All(error => error.Message == cause ||
                error.Message == "Combat dice failed; the committed attack has stopped." ||
                error.Message == "The support mission did not complete; the main attack was stopped."), Is.True,
                string.Join("\n", TestHandler.Exceptions.Select(error => error.Message)));
            TestHandler.Clear();
        }
        private void FinishTypewriter()
        {
            // Advance the existing timer to completion; no navigation event or fabricated output text.
            SetField(_printer, "_revealed", float.MaxValue);
            typeof(PrinterControl).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_printer, null);
            Assert.That(typeof(PrinterControl).GetField("_isRevealing", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_printer), Is.False, "The native printer reveal must finish before navigation.");
        }

        [Test]
        public void RepeatedFixtureSetup_KeepsExactlyOnePublisherSubscription()
        {
            for (int iteration = 0; iteration < 3; iteration++)
            {
                Assert.That(EventManager.Instance, Is.SameAs(_events));
                var unit = Unit("intel probe", Side.AI, new Position2D(5, 5));
                _events.RaiseUnitSpottedLevelChanged(unit, SpottedLevel.Level1, SpottedLevel.Level2);
                Assert.That(History.Count, Is.EqualTo(1), "One intel event must produce exactly one report.");
                Assert.That(History[0].Category, Is.EqualTo(PrinterCategory.Intel));
                if (iteration < 2)
                {
                    CleanupPrinter();
                    SetUp();
                }
            }
        }
        private void AssertFitsCurrentWindow(PrinterMessage report)
        {
            Assert.That(report.Lines.Length, Is.LessThanOrEqualTo(4));
            Vector2 size = _text.GetPreferredValues(report.FullText + "_", _text.rectTransform.rect.width, float.PositiveInfinity);
            Assert.That(size.y, Is.LessThanOrEqualTo(_text.rectTransform.rect.height), report.FullText);
        }

        private CombatUnit Unit(string name, Side side, Position2D pos, bool artillery = false)
        {
            var unit = new CombatUnit(name, artillery ? UnitClassification.SPA : UnitClassification.INF,
                UnitRole.GroundCombat, side, Nationality.USSR,
                artillery ? WeaponType.SPA_2S19_SV : WeaponType.INF_REG_SV, WeaponType.NONE, WeaponType.NONE);
            unit.SetUnitID(name); unit.SetPosition(pos); unit.SetDeploymentPosition(DeploymentPosition.Deployed);
            unit.SetExperienceLevel(ExperienceLevel.Trained); unit.SetEfficiencyLevel(EfficiencyLevel.FullOperations);
            unit.SetSpottedLevel(SpottedLevel.Level2);
            GameManager.RegisterCombatUnit(unit);
            return unit;
        }
        private GroundCombatContext Context(Side phasing = Side.Player) => new GroundCombatContext(phasing,
            GameManager.GetAllCombatUnits(), new AIPerceptionState(), 3, unit =>
            { GameManager.UnregisterCombatUnit(unit.UnitID); GameManager.InvalidateOccupancy(); });
        private sealed class Dice : ICombatRandom
        {
            public int Count;
            public Func<int, int, int> Roll;
            public int RollDie(int sides) { Count++; return Roll?.Invoke(sides, Count) ?? (sides == 100 ? 100 : 1); }
        }

        private Leader AssignLeader(CombatUnit unit, bool bunker = false, bool camouflage = false)
        {
            var leader = new Leader("Support test leader", unit.Side, unit.Nationality, CommandAbility.Average);
            if (bunker || camouflage)
            {
                leader.AwardReputation(5000);
                Assert.That(leader.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1), Is.True);
                Assert.That(leader.UnlockSkill(LeadershipFoundation.PromotionToSeniorGrade_SeniorPromotion), Is.True);
            }
            if (bunker)
            {
                Assert.That(leader.UnlockSkill(IntelligenceDoctrine.EnhancedIntelligenceCollection_ImprovedGathering), Is.True);
                Assert.That(leader.UnlockSkill(IntelligenceDoctrine.ConcealedOperationsBase_UndergroundBunker), Is.True);
            }
            if (camouflage)
            {
                Assert.That(leader.UnlockSkill(LeadershipFoundation.SeniorOfficerTraining_CommandTier2), Is.True);
                Assert.That(leader.UnlockSkill(LeadershipFoundation.PromotionToTopGrade_TopPromotion), Is.True);
                Assert.That(leader.UnlockSkill(SpecialForcesSpecialization.TerrainExpert_TerrainMastery), Is.True);
                Assert.That(leader.UnlockSkill(SpecialForcesSpecialization.InfiltrationTactics_InfiltrationMovement), Is.True);
                Assert.That(leader.UnlockSkill(SpecialForcesSpecialization.SuperiorCamouflage_ConcealedPositions), Is.True);
            }
            GameManager.RegisterLeader(leader);
            Assert.That(GameManager.AssignLeaderToUnit(leader.LeaderID, unit.UnitID), Is.True);
            return leader;
        }

        private void WithController(CombatUnit attacker, Action<MovementController> run)
        {
            Type[] types = { typeof(MovementController), typeof(BattleManager), typeof(HexDetectionService) };
            var fields = types.Select(type => type.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)).ToArray();
            var prior = fields.Select(field => field.GetValue(null)).ToArray();
            var root = new GameObject("Isolated combat controllers");
            root.SetActive(false); // Explicit fixture setup; do not run scene startup or subscribe user-scene components.
            try
            {
                var movement = root.AddComponent<MovementController>();
                var battle = root.AddComponent<BattleManager>();
                var detection = root.AddComponent<HexDetectionService>();
                fields[0].SetValue(null, movement); fields[1].SetValue(null, battle); fields[2].SetValue(null, detection);
                typeof(MovementController).GetProperty(nameof(MovementController.CurrentUnit)).SetValue(movement, attacker);
                typeof(MovementController).GetProperty(nameof(MovementController.State)).SetValue(movement, MovementState.UnitSelected);
                SetField(movement, "_currentPhase", BattlePhase.PlayerTurn);
                GameDataManager.SelectedHex = attacker.MapPos;
                run(movement);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                for (int i = 0; i < fields.Length; i++) fields[i].SetValue(null, prior[i]);
            }
        }

        private Dice ForceRetreat(CombatUnit battery, CombatUnit attacker, bool failOnHundred = false)
        {
            var indirect = new IndirectAttackContext { TargetTerrain = TerrainType.Clear, SuppressCounterBattery = true };
            var probe = new Dice();
            int damage = CombatEngine.ResolveLane(CombatResolver.BuildIndirectForwardLane(battery, attacker, indirect), probe);
            int stand = StandCheck.ComputeStandValue(CombatResolver.BuildIndirectTargetStand(battery, attacker, indirect, damage)) + 1;
            Assert.That(stand, Is.InRange(1, 10));
            return new Dice { Roll = (sides, count) => sides == 100 && failOnHundred
                ? throw new InvalidOperationException("Late displacement/degradation die")
                : count == probe.Count + 1 ? stand : sides == 100 ? 100 : sides == 20 ? 20 : 1 };
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LateFailure_PreservesOnlyCompletedDisplacement(bool staticCollapse)
        {
            var attacker = Unit("attacker", Side.Player, new Position2D(5, 5));
            var defender = Unit("defender", Side.AI, new Position2D(6, 5));
            var battery = Unit("battery", Side.AI, new Position2D(7, 5), true);
            attacker.SetExperienceLevel(ExperienceLevel.Raw);
            if (staticCollapse) attacker.SetEfficiencyLevel(EfficiencyLevel.StaticOperations);
            float hp = attacker.HitPoints.Current;
            var result = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap,
                ForceRetreat(battery, attacker, true), Context());
            Assert.That(result.ResolutionFailed && result.Support.Fired, Is.True);
            Assert.That(result.MainAttackResolved, Is.False);
            Assert.That(result.Support.Combat.TargetMoved, Is.EqualTo(!staticCollapse));
            Assert.That(result.Support.Combat.TargetHexesRetreated, Is.EqualTo(staticCollapse ? 0 : 1));
            Assert.That(result.Support.Combat.TargetFinalPosition, Is.EqualTo(attacker.MapPos));
            Assert.That(attacker.MapPos == new Position2D(5, 5), Is.EqualTo(staticCollapse));
            Assert.That(GameManager.GetUnitAtPosition(attacker.MapPos), Is.SameAs(attacker));
            Assert.That(attacker.HitPoints.Current, Is.EqualTo(hp - result.Support.Combat.DamageToTarget));
            Assert.That(attacker.CombatActions.Current, Is.Zero);
            Assert.That(battery.DaysSupply.Current, Is.EqualTo(4.5f));
            AcceptExpectedDiceFailure("Late displacement/degradation die");
        }

        [TestCase(Side.Player, "hidden")]
        [TestCase(Side.Player, "known")]
        [TestCase(Side.AI, "hidden")]
        [TestCase(Side.AI, "known")]
        [TestCase(Side.AI, "stale")]
        public void RetreatZoC_UsesRetreatingSidesCurrentKnowledge(Side side, string knowledge)
        {
            var map = GameDataManager.CurrentHexMap;
            var unit = Unit("retreating", side, new Position2D(5, 5));
            Side opposing = side == Side.Player ? Side.AI : Side.Player;
            var firer = Unit("firer", opposing, new Position2D(7, 5), true);
            var projector = Unit("concealed projector", opposing, new Position2D(3, 5));
            AssignLeader(projector, camouflage: true);
            projector.SetSpottedLevel(side == Side.AI || knowledge == "known" ? SpottedLevel.Level4 : SpottedLevel.Level0);
            var context = Context(side);
            if (side == Side.AI && knowledge != "hidden")
                context.AIPerception.RecordSpot(projector.UnitID, knowledge == "stale" ? new Position2D(1, 1) : projector.MapPos,
                    3, projector.Classification, 100, 4, SpottedLevel.Level1);
            var escape = new Position2D(4, 5);
            foreach (HexDirection dir in HexArc.RearArc(HexMapUtil.GetGeneralDirection(unit.MapPos, firer.MapPos)))
            {
                Position2D pos = HexMapUtil.GetNeighborPosition(unit.MapPos, dir);
                if (pos != escape) map.GetHexAt(pos).SetTerrain(TerrainType.Impassable);
            }
            Assert.That(context.CanObserve(side, projector, map), Is.EqualTo(knowledge == "known"));
            var result = RetreatResolver.ResolveDisplacement(firer, unit, StandOutcome.Retreat, map,
                new Dice { Roll = (sides, count) => 20 }, true, context);
            Assert.That(result.Moved, Is.EqualTo(knowledge != "known"));
            Assert.That(result.SurrenderHeldInPlace, Is.EqualTo(knowledge == "known"));
            Assert.That(unit.MapPos, Is.EqualTo(knowledge == "known" ? new Position2D(5, 5) : escape));
        }

        [TestCase(Side.Player)]
        [TestCase(Side.AI)]
        public void SupportReveal_RespectsBunkerInOpponentsStore(Side phasing)
        {
            var attacker = Unit("attacker", phasing, new Position2D(5, 5));
            var defender = Unit("defender", phasing == Side.Player ? Side.AI : Side.Player, new Position2D(6, 5));
            var battery = Unit("battery", defender.Side, new Position2D(7, 5), true);
            AssignLeader(battery, bunker: true);
            battery.SetSpottedLevel(SpottedLevel.Level3);
            var context = Context(phasing);
            context.AIPerception.RecordSpot(battery.UnitID, battery.MapPos, 3, battery.Classification, 100, 4, SpottedLevel.Level3);
            var result = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, new Dice(), context);
            Assert.That(result.Support.Fired, Is.True);
            Assert.That(battery.Side == Side.AI ? battery.SpottedLevel : context.AIPerception.LevelOf(battery.UnitID), Is.EqualTo(SpottedLevel.Level3));
        }

        [TestCase(false, 3)]
        [TestCase(true, 11)]
        public void SupportAwardsBatteryLeaderForActualCombatAndKill(bool lethal, int expected)
        {
            var attacker = Unit("attacker", Side.Player, new Position2D(5, 5));
            var defender = Unit("defender", Side.AI, new Position2D(6, 5));
            var battery = Unit("battery", Side.AI, new Position2D(7, 5), true);
            var leader = AssignLeader(battery);
            if (lethal) attacker.HitPoints.SetCurrent(1);
            int before = leader.ReputationPoints;
            var result = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, new Dice(), Context());
            Assert.That(result.Support.Fired, Is.True);
            Assert.That(result.AttackerDestroyed, Is.EqualTo(lethal));
            Assert.That(leader.ReputationPoints - before, Is.EqualTo(expected));
        }

        [TestCase("hold")]
        [TestCase("kill")]
        [TestCase("retreat")]
        public void ProductionController_ReportsOnceRejectsReentryAndUpdatesSelection(string fate)
        {
            var attacker = Unit("Motor Rifle Regiment (BTR-70)", Side.Player, new Position2D(5, 5));
            var defender = Unit("defender", Side.AI, new Position2D(6, 5));
            var battery = Unit("SECRET battery", Side.AI, new Position2D(7, 5), true);
            battery.SetSpottedLevel(SpottedLevel.Level0);
            if (fate == "kill") attacker.HitPoints.SetCurrent(1);
            Dice dice = fate == "retreat" ? ForceRetreat(battery, attacker) : new Dice();
            var reentryDice = new Dice();
            WithController(attacker, controller =>
            {
                Action<PrinterMessage> reenter = message => controller.TryAttack(defender, reentryDice);
                _events.OnPrinterMessage += reenter;
                try { controller.TryAttack(defender, dice); }
                finally { _events.OnPrinterMessage -= reenter; }
                Assert.That(reentryDice.Count, Is.Zero);
                Assert.That(History.Count(message => message.Category == PrinterCategory.Combat), Is.EqualTo(1));
                Assert.That(controller.CurrentUnit, fate == "kill" ? Is.Null : Is.SameAs(attacker));
                Assert.That(controller.State, Is.EqualTo(fate == "kill" ? MovementState.Idle : MovementState.UnitSelected));
                Assert.That(GameDataManager.SelectedHex, Is.EqualTo(fate == "kill" ? GameDataManager.NoHexSelected : attacker.MapPos));
                if (fate == "retreat") Assert.That(attacker.MapPos, Is.Not.EqualTo(new Position2D(5, 5)));
                if (fate != "hold") Assert.That(defender.HasFoughtThisTurn || defender.HasEmittedThisTurn, Is.False);
                Assert.That(attacker.CombatActions.Current, Is.Zero);
                Assert.That(battery.OpportunityActions.Current, Is.Zero);
                FinishTypewriter();
                StringAssert.Contains(CombatReport.FullText, _text.text);
                AssertFitsCurrentWindow(CombatReport);
                Assert.That(CombatReport.FullText, Does.Not.Contain("SECRET"));
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ProductionController_ReportsCommittedIndirectFailure(bool afterShot)
        {
            var firer = Unit("our artillery", Side.Player, new Position2D(5, 5), true);
            var target = Unit("SECRET enemy", Side.AI, new Position2D(7, 5));
            float hp = target.HitPoints.Current;
            var dice = new Dice { Roll = (sides, count) => !afterShot || sides == 100
                ? throw new InvalidOperationException("Injected ordinary mission failure") : 1 };
            WithController(firer, controller => controller.TryAttack(target, dice));
            AcceptExpectedDiceFailure("Injected ordinary mission failure");
            Assert.That(History.Count(message => message.Category == PrinterCategory.Combat), Is.EqualTo(1));
            StringAssert.Contains(afterShot ? "Shot resolved" : "shot did not resolve", CombatReport.FullText);
            StringAssert.Contains("Paid costs", CombatReport.FullText);
            Assert.That(CombatReport.FullText, Does.Not.Contain("SECRET"));
            Assert.That(firer.CombatActions.Current, Is.Zero);
            Assert.That(target.HitPoints.Current < hp, Is.EqualTo(afterShot));
            FinishTypewriter(); StringAssert.Contains(CombatReport.FullText, _text.text);
            AssertFitsCurrentWindow(CombatReport);
        }

        [Test]
        public void CompositeHistory_NavigatesBetweenOrdersWithoutDuplicateSupportEntries()
        {
            PrinterDispatch.Verbose = false;
            Unit("a", Side.AI, new Position2D(7, 5), true);
            Unit("b", Side.AI, new Position2D(7, 6), true);
            for (int i = 0; i < 2; i++)
            {
                var attacker = Unit("attacker " + i, Side.Player, new Position2D(5, 5 + i));
                var defender = Unit("defender " + i, Side.AI, new Position2D(6, 5 + i));
                var result = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, new Dice(), Context());
                Assert.That(result.Support.Fired && result.MainAttackResolved, Is.True);
                PrinterDispatch.ReportGroundCombat(attacker, defender, defender.MapPos, result);
            }
            Assert.That(History.Count, Is.EqualTo(2));
            Assert.That(History[0].FullText, Is.Not.EqualTo(History[1].FullText));
            FinishTypewriter();
            // Navigation API remains valid for future user wiring; visibility above never depends on it.
            _events.RaisePrinterPreviousRequested(); FinishTypewriter();
            StringAssert.Contains(History[0].FullText, _text.text);
            _events.RaisePrinterLatestRequested(); FinishTypewriter();
            StringAssert.Contains(History[1].FullText, _text.text);
            _events.RaisePrinterFilterCycleRequested();
            Assert.That(History.Count, Is.EqualTo(2));
        }

        [TestCase(false, Side.Player, false)]
        [TestCase(true, Side.Player, false)]
        [TestCase(false, Side.AI, false)]
        [TestCase(true, Side.AI, false)]
        [TestCase(false, Side.Player, true)]
        [TestCase(true, Side.Player, true)]
        [TestCase(false, Side.AI, true)]
        [TestCase(true, Side.AI, true)]
        public void CompositeSupportReport_ReachesCurrentWindowWithoutNavigation(bool killed, Side phasing, bool verbose)
        {
            PrinterDispatch.Verbose = verbose;
            var attacker = Unit("Attacking regiment", phasing, new Position2D(5, 5));
            var defender = Unit("Defending regiment", phasing == Side.Player ? Side.AI : Side.Player, new Position2D(6, 5));
            var battery = Unit("SECRET battery identity", defender.Side, new Position2D(7, 5), true);
            battery.SetSpottedLevel(SpottedLevel.Level0);
            if (killed) attacker.HitPoints.SetCurrent(1);
            _panel.SetActive(false);
            var result = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, new Dice(), Context(phasing));
            Assert.That(result.Support.Fired, Is.True);
            PrinterDispatch.ReportGroundCombat(attacker, defender, new Position2D(6, 5), result);
            Assert.That(History.Count(message => message.Category == PrinterCategory.Combat), Is.EqualTo(1));
            Assert.That(History.Last(), Is.SameAs(CombatReport), "Verbose contact intelligence must not overwrite the final combat report.");
            Assert.That(History.Count(message => message.Category == PrinterCategory.Intel), Is.EqualTo(verbose && !killed ? 1 : 0));
            StringAssert.Contains("artillery", CombatReport.FullText);
            StringAssert.Contains(killed ? (phasing == Side.Player ? "cancelled" : "stopped")
                : phasing == Side.Player ? "Main attack" : "Enemy attack", CombatReport.FullText);
            Assert.That(CombatReport.FullText.IndexOf("artillery", StringComparison.Ordinal),
                Is.LessThan(CombatReport.FullText.IndexOf(phasing == Side.Player ? "Main attack" : "Enemy attack", StringComparison.Ordinal)));
            if (phasing == Side.Player)
                Assert.That(History.All(message => !message.FullText.Contains(battery.UnitName)), Is.True);
            Assert.That(_panel.activeSelf, Is.True);
            FinishTypewriter();
            StringAssert.Contains(CombatReport.FullText, _text.text);
            AssertFitsCurrentWindow(CombatReport);
            Assert.That(result.MainAttackResolved, Is.EqualTo(!killed));
        }

        [TestCase(StandOutcome.Retreat)]
        [TestCase(StandOutcome.Rout)]
        [TestCase(StandOutcome.Shatter)]
        public void FailedStand_UsesRealRetreatAndRemoval_AndCancelsMain(StandOutcome stand)
        {
            var attacker = Unit("attacker", Side.Player, new Position2D(5, 5));
            var defender = Unit("defender", Side.AI, new Position2D(6, 5));
            var battery = Unit("battery", Side.AI, new Position2D(7, 5), true);
            attacker.SetExperienceLevel(ExperienceLevel.Raw);
            var indirect = new IndirectAttackContext { TargetTerrain = TerrainType.Clear, SuppressCounterBattery = true };
            var laneDice = new Dice();
            int damage = CombatEngine.ResolveLane(CombatResolver.BuildIndirectForwardLane(battery, attacker, indirect), laneDice);
            int value = StandCheck.ComputeStandValue(CombatResolver.BuildIndirectTargetStand(battery, attacker, indirect, damage));
            int standRoll = value + (stand == StandOutcome.Retreat ? 1 : stand == StandOutcome.Rout ? 4 : 7);
            Assert.That(standRoll, Is.InRange(1, 10), "Fixture must reach this stand outcome with legal dice.");
            var dice = new Dice { Roll = (sides, count) => count == laneDice.Count + 1 ? standRoll : sides == 100 ? 100 : sides == 20 ? 20 : 1 };
            var result = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, dice, Context());
            Assert.That(result.Support.Combat.TargetOutcome, Is.EqualTo(stand));
            Assert.That(result.MainAttackResolved, Is.False);
            Assert.That(result.ResolutionFailed, Is.False);
            Assert.That(attacker.CombatActions.Current, Is.Zero);
            Assert.That(defender.HasFoughtThisTurn || defender.HasEmittedThisTurn, Is.False);
            if (stand == StandOutcome.Shatter)
            {
                Assert.That(result.AttackerRemovedFromMap, Is.True);
                Assert.That(result.AttackerDestroyed, Is.False);
                Assert.That(GameManager.GetCombatUnit(attacker.UnitID), Is.Null);
            }
            else
            {
                Assert.That(result.Support.Combat.TargetHexesRetreated, Is.EqualTo(stand == StandOutcome.Retreat ? 1 : 2));
                Assert.That(GameManager.GetUnitAtPosition(new Position2D(5, 5)), Is.Null);
                Assert.That(GameManager.GetUnitAtPosition(attacker.MapPos), Is.SameAs(attacker));
            }
        }

        [Test]
        public void SupportStaticCollapse_BooksAllRemainingEquipmentExactlyOnce()
        {
            var attacker = Unit("attacker", Side.Player, new Position2D(5, 5));
            var defender = Unit("defender", Side.AI, new Position2D(6, 5));
            Unit("battery", Side.AI, new Position2D(7, 5), true);
            attacker.SetEfficiencyLevel(EfficiencyLevel.StaticOperations);
            float equipment = attacker.EquipmentBays.TotalIntelStats.Values.Sum();
            var dice = new Dice { Roll = (sides, count) => sides == 10 ? 8 : 1 };
            var result = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, dice, Context());
            Assert.That(result.MainAttackResolved, Is.False);
            Assert.That(result.AttackerDestroyed && result.AttackerRemovedFromMap, Is.True);
            Assert.That(GameDataManager.GetLossLedger(Side.Player).Values.Sum(), Is.EqualTo(equipment).Within(0.001f));
            Assert.That(GameDataManager.GetDailyLossLedger(Side.Player).Values.Sum(), Is.EqualTo(equipment).Within(0.001f));
        }

        [Test]
        public void ResolutionFailure_IsDeliveredToPrinterWithoutPretendingMainAttackFired()
        {
            var attacker = Unit("attacker", Side.Player, new Position2D(5, 5));
            var defender = Unit("defender", Side.AI, new Position2D(6, 5));
            Unit("SECRET battery", Side.AI, new Position2D(7, 5), true);
            var result = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap,
                new Dice { Roll = (sides, count) => throw new InvalidOperationException("Injected dice failure") }, Context());
            AcceptExpectedDiceFailure("Injected dice failure");
            PrinterDispatch.ReportGroundCombat(attacker, defender, defender.MapPos, result);
            Assert.That(History.Count, Is.EqualTo(1));
            StringAssert.Contains("did not resolve", CombatReport.FullText);
            StringAssert.Contains("internal error", CombatReport.FullText);
            Assert.That(History.Any(message => message.FullText.Contains("SECRET")), Is.False);
            FinishTypewriter();
            StringAssert.Contains("internal error", _text.text);
            AssertFitsCurrentWindow(CombatReport);
            Assert.That(result.MainAttackResolved || result.Support.Fired, Is.False);
        }

        [Test]
        public void BlockedRetreatSurvivor_PrintsHoldingInPlaceThenCancellation()
        {
            var attacker = Unit("attacker", Side.Player, new Position2D(5, 5));
            var defender = Unit("defender", Side.AI, new Position2D(6, 5));
            var battery = Unit("battery", Side.AI, new Position2D(7, 5), true);
            foreach (HexDirection direction in HexArc.RearArc(HexMapUtil.GetGeneralDirection(attacker.MapPos, battery.MapPos)))
                GameDataManager.CurrentHexMap.GetHexAt(HexMapUtil.GetNeighborPosition(attacker.MapPos, direction)).SetTerrain(TerrainType.Impassable);
            var dice = new Dice { Roll = (sides, count) => sides == 20 ? 20 : sides == 10 ? 8 : sides == 100 ? 100 : 1 };
            var result = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, dice, Context());
            PrinterDispatch.ReportGroundCombat(attacker, defender, defender.MapPos, result);
            Assert.That(History.Count, Is.EqualTo(1));
            StringAssert.Contains("holding in place", CombatReport.FullText);
            StringAssert.Contains("cancelled", CombatReport.FullText);
            FinishTypewriter();
            StringAssert.Contains(CombatReport.FullText, _text.text);
            AssertFitsCurrentWindow(CombatReport);
            Assert.That(result.MainAttackResolved, Is.False);
        }
    }
}
