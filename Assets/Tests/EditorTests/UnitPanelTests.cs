using System.Linq;
using System.Reflection;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HammerAndSickle.Tests
{
    /// <summary>Exercises the authored action bar in preview scenes, preserving live selection and managers.</summary>
    public class UnitPanelTests
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly FieldInfo SpriteInstance = typeof(SpriteManager).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo Handler = typeof(AppService).GetField("_testHandler", BindingFlags.NonPublic | BindingFlags.Static);
        private Scene _battle;
        private Scene _menu;
        private Prefab_UnitPanel _panel;
        private SpriteManager _previousSpriteManager;
        private CombatUnit _previousSelection;
        private TestHandler _previousHandler;
        private TestHandler _handler;

        private UnityEngine.UI.Image Flag => Field<UnityEngine.UI.Image>("_flagImage");
        private UnityEngine.UI.Image Nato => Field<UnityEngine.UI.Image>("_natoImage");
        private T Field<T>(string name) => (T)typeof(Prefab_UnitPanel).GetField(name, PrivateInstance).GetValue(_panel);
        private void Bind(string field, string path) => typeof(Prefab_UnitPanel).GetField(field, PrivateInstance)
            .SetValue(_panel, _panel.transform.Find(path).GetComponent(field.EndsWith("Image") ? typeof(UnityEngine.UI.Image) : typeof(TextMeshProUGUI)));
        private string[] Counts() => new[] { "_moveActionsText", "_combatActionsText", "_deployActionsText", "_intelActionsText", "_oppActionsText" }
            .Select(field => Field<TextMeshProUGUI>(field).text).ToArray();
        private static string SpriteName(Sprite sprite) => sprite == null ? null : sprite.name.Replace("(Clone)", "");
        private static CombatUnit Unit(Side side = Side.Player, Nationality nationality = Nationality.USSR, UnitClassification classification = UnitClassification.INF)
            => new CombatUnit("Panel test unit", classification, UnitRole.GroundCombat, side, nationality);
        private void Select(CombatUnit unit) { GameDataManager.SelectedUnit = unit; _panel.UpdateUnitPanel(); }

        [SetUp]
        public void SetUp()
        {
            _previousSpriteManager = (SpriteManager)SpriteInstance.GetValue(null);
            _previousSelection = GameDataManager.SelectedUnit;
            _previousHandler = (TestHandler)Handler.GetValue(null);
            _handler = new TestHandler();
            AppService.SetTestHandler(_handler);
            try
            {
                _menu = EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainMenu.unity");
                var manager = _menu.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SpriteManager>(true)).Single();
                SpriteInstance.SetValue(null, manager);
                _battle = EditorSceneManager.OpenPreviewScene("Assets/Scenes/BattleScene.unity");
                _panel = _battle.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Prefab_UnitPanel>(true)).Single();
                Bind("_flagImage", "Action Bar/Flag");
                Bind("_natoImage", "Action Bar/NATO Icon");
                Bind("_moveActionsText", "Action Bar/Text/Move Actions");
                Bind("_combatActionsText", "Action Bar/Text/Combat Actions");
                Bind("_deployActionsText", "Action Bar/Text/Deploy Actions");
                Bind("_intelActionsText", "Action Bar/Text/Recon Actions");
                Bind("_oppActionsText", "Action Bar/Text/Opp Actions");
                _panel.Clear();
            }
            catch { Cleanup(); throw; }
        }

        [TearDown]
        public void TearDown()
        {
            try { Assert.That(_handler.ExceptionCount, Is.Zero, "The panel must not conceal an application exception."); }
            finally { Cleanup(); }
        }

        private void Cleanup()
        {
            GameDataManager.SelectedUnit = _previousSelection;
            SpriteInstance.SetValue(null, _previousSpriteManager);
            if (_battle.IsValid()) EditorSceneManager.ClosePreviewScene(_battle);
            if (_menu.IsValid()) EditorSceneManager.ClosePreviewScene(_menu);
            AppService.SetTestHandler(_previousHandler);
        }

        [TestCase(Nationality.USSR, "Flag_SV")]
        [TestCase(Nationality.USA, "Flag_US")]
        [TestCase(Nationality.FRG, "Flag_GE")]
        [TestCase(Nationality.UK, "Flag_UK")]
        [TestCase(Nationality.FRA, "Flag_FR")]
        [TestCase(Nationality.BE, "Flag_BE")]
        [TestCase(Nationality.DE, "Flag_DE")]
        [TestCase(Nationality.NE, "Flag_NE")]
        [TestCase(Nationality.MJ, "Flag_MJ")]
        [TestCase(Nationality.IR, "Flag_Iran")]
        [TestCase(Nationality.IQ, "Flag_Iraq")]
        [TestCase(Nationality.SAUD, "Flag_Saudi")]
        [TestCase(Nationality.KW, "Flag_Kuwait")]
        [TestCase(Nationality.China, "Flag_China")]
        public void SelectedNationality_DisplaysItsAuthoredFlag(Nationality nationality, string expected)
        {
            Select(Unit(nationality: nationality));
            Assert.That(Flag.enabled, Is.True);
            Assert.That(SpriteName(Flag.sprite), Is.EqualTo(expected));
        }

        [Test]
        public void FriendlyCounters_RefreshIndependentlyWithoutDecimalOrMaximum()
        {
            var unit = Unit();
            unit.MoveActions.SetMax(3); unit.MoveActions.SetCurrent(3);
            unit.CombatActions.SetMax(4); unit.CombatActions.SetCurrent(4);
            unit.DeploymentActions.SetMax(2); unit.DeploymentActions.SetCurrent(2);
            unit.OpportunityActions.SetMax(1); unit.OpportunityActions.SetCurrent(1);
            Select(unit);
            Assert.That(Counts(), Is.EqualTo(new[] { "3", "4", "2", "0", "1" }));
            var flag = Flag.sprite;
            var nato = Nato.sprite;
            unit.MoveActions.SetCurrent(1); unit.CombatActions.SetCurrent(0);
            unit.DeploymentActions.SetCurrent(1); unit.OpportunityActions.SetCurrent(0);
            _panel.UpdateUnitPanel();
            Assert.That(Counts(), Is.EqualTo(new[] { "1", "0", "1", "0", "0" }));
            Assert.That(Flag.sprite, Is.SameAs(flag));
            Assert.That(Nato.sprite, Is.SameAs(nato));
            Assert.That(Field<TextMeshProUGUI>("_infoText").text, Does.Contain(unit.UnitName));
        }

        [Test]
        public void UnavailableMovementAndSpentHqSweep_DisplayZeroWithRawActionsRemaining()
        {
            var unit = Unit(classification: UnitClassification.HQ);
            Select(unit);
            Assert.That(Counts()[0], Is.EqualTo("0"));
            Assert.That(Counts()[3], Is.EqualTo("1"));
            Assert.That(unit.PerformIntelAction(), Is.True);
            _panel.UpdateUnitPanel();
            Assert.That(unit.IntelActions.Current, Is.GreaterThan(0));
            Assert.That(Counts()[3], Is.EqualTo("0"));

            unit = Unit();
            unit.SetDeploymentPosition(DeploymentPosition.Entrenched);
            Select(unit);
            Assert.That(unit.MoveActions.Current, Is.GreaterThan(0));
            Assert.That(Counts()[0], Is.EqualTo("0"));
        }

        [TestCase(SpottedLevel.Level1)]
        [TestCase(SpottedLevel.Level5)]
        public void VisibleEnemy_ShowsIdentityAndZeroActionPlaceholders(SpottedLevel level)
        {
            Select(Unit());
            var enemy = Unit(Side.AI, Nationality.MJ, UnitClassification.RECON);
            enemy.SetSpottedLevel(level);
            Select(enemy);
            Assert.That(SpriteName(Flag.sprite), Is.EqualTo("Flag_MJ"));
            Assert.That(SpriteName(Nato.sprite), Is.EqualTo("Icon_Recon"));
            Assert.That(Counts(), Is.All.EqualTo("0"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NoSelectionOrUnspottedEnemy_ClearsPreviousIdentityTextAndCounters(bool hiddenEnemy)
        {
            Select(Unit());
            var unit = hiddenEnemy ? Unit(Side.AI) : null;
            unit?.SetSpottedLevel(SpottedLevel.Level0);
            Select(unit);
            Assert.That(Field<TextMeshProUGUI>("_infoText").text, Is.Empty);
            Assert.That(Flag.sprite, Is.Null); Assert.That(Flag.enabled, Is.False);
            Assert.That(Nato.sprite, Is.Null); Assert.That(Nato.enabled, Is.False);
            Assert.That(Counts(), Is.All.EqualTo(string.Empty));
            Select(Unit());
            Assert.That(Flag.enabled && Nato.enabled, Is.True);
        }

        [Test]
        public void GenericNationalityAndUnknownClassification_DoNotRetainPreviousIdentity()
        {
            Select(Unit());
            Select(Unit(nationality: Nationality.GENERIC, classification: (UnitClassification)999));
            Assert.That(Flag.sprite, Is.Null); Assert.That(Flag.enabled, Is.False);
            Assert.That(SpriteName(Nato.sprite), Is.EqualTo(SpriteManager.UnknownDeploymentIcon));
        }

        [Test]
        public void Initialize_RequiresAllActionBarReferences()
        {
            Assert.That(_panel.Initialize(), Is.True);
            typeof(Prefab_UnitPanel).GetField("_flagImage", PrivateInstance).SetValue(_panel, null);
            var logger = Debug.unityLogger;
            var previousHandler = logger.logHandler;
            var capture = new ExpectedErrorCapture(previousHandler,
                "[Prefab_UnitPanel] Initialization errors found: Flag Image is not assigned. ");
            bool initialized;
            try
            {
                logger.logHandler = capture;
                initialized = _panel.Initialize();
            }
            finally { logger.logHandler = previousHandler; }
            Assert.That(initialized, Is.False);
            Assert.That(capture.Count, Is.EqualTo(1), "Missing references must still produce an actionable runtime error.");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Keep the intentional validation error out of the Console; forward all other logs.</summary>
        private sealed class ExpectedErrorCapture : ILogHandler
        {
            private readonly ILogHandler _previous;
            private readonly string _message;
            public int Count { get; private set; }

            public ExpectedErrorCapture(ILogHandler previous, string message)
            {
                _previous = previous;
                _message = message;
            }

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                if (logType == LogType.Error && string.Format(format, args) == _message)
                {
                    Count++;
                    return;
                }
                _previous.LogFormat(logType, context, format, args);
            }

            public void LogException(System.Exception exception, UnityEngine.Object context)
                => _previous.LogException(exception, context);
        }
    }
}
