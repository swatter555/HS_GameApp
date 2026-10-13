using System.Linq;
using System.Reflection;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.SceneManagement.Controllers;
using HammerAndSickle.SceneManagement;
using HammerAndSickle.Services;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HammerAndSickle.Tests
{
    /// <summary>Verifies scene bootstrap and real hex-selection updates using isolated saved-scene previews.</summary>
    public class BattleSelectionPanelTests
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly FieldInfo DataInstance = typeof(GameDataManager).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo SpriteInstance = typeof(SpriteManager).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo Handler = typeof(AppService).GetField("_testHandler", BindingFlags.NonPublic | BindingFlags.Static);
        private Scene _battle, _menu;
        private Scene1_Controller _controller;
        private DefaultDialog_Scene1 _updater;
        private Prefab_UnitPanel _unitPanel;
        private Prefab_TerrainPanel _terrainPanel;
        private HexDetectionService _hexSelection;
        private GameDataManager _data;
        private HexMap _map, _previousMap;
        private GameDataManager _previousData;
        private SpriteManager _previousSprites;
        private CombatUnit _previousUnit;
        private Leader _previousLeader;
        private Position2D _previousHex, _previousSize;
        private HexTile _previousHexData;
        private MapTheme _previousTheme;
        private TestHandler _handler, _previousHandler;

        private T Find<T>(Scene scene) where T : UnityEngine.Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
        private TextMeshProUGUI UnitText(string name) => (TextMeshProUGUI)typeof(Prefab_UnitPanel)
            .GetField(name, PrivateInstance).GetValue(_unitPanel);
        private string[] Counts() => new[] { "_moveActionsText", "_combatActionsText", "_deployActionsText", "_intelActionsText", "_oppActionsText" }
            .Select(field => UnitText(field).text).ToArray();

        [SetUp]
        public void SetUp()
        {
            _previousData = (GameDataManager)DataInstance.GetValue(null);
            _previousSprites = (SpriteManager)SpriteInstance.GetValue(null);
            _previousHandler = (TestHandler)Handler.GetValue(null);
            _previousUnit = GameDataManager.SelectedUnit;
            _previousLeader = GameDataManager.SelectedLeader;
            _previousHex = GameDataManager.SelectedHex;
            _previousHexData = GameDataManager.SelectedHexData;
            _previousMap = GameDataManager.CurrentHexMap;
            _previousSize = GameDataManager.CurrentMapSize;
            _previousTheme = GameDataManager.CurrentMapTheme;
            _handler = new TestHandler();
            AppService.SetTestHandler(_handler);
            try
            {
                _menu = EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainMenu.unity");
                SpriteInstance.SetValue(null, Find<SpriteManager>(_menu));
                _battle = EditorSceneManager.OpenPreviewScene("Assets/Scenes/BattleScene.unity");
                _controller = Find<Scene1_Controller>(_battle);
                _unitPanel = Find<Prefab_UnitPanel>(_battle);
                _terrainPanel = Find<Prefab_TerrainPanel>(_battle);
                _hexSelection = Find<HexDetectionService>(_battle);
                _data = _controller.gameObject.AddComponent<GameDataManager>();
                DataInstance.SetValue(null, _data);
                _map = MapFixtures.UniformMap();
                GameDataManager.CurrentHexMap = _map;
                GameDataManager.CurrentMapTheme = MapTheme.MiddleEast;
                Assert.That(_controller.InitializeSelectionPanels(), Is.True);
                _updater = Find<DefaultDialog_Scene1>(_battle);
            }
            catch { Cleanup(); throw; }
        }

        [TearDown]
        public void TearDown()
        {
            try { Assert.That(_handler.ExceptionCount, Is.Zero, "Selection refresh must not conceal an application exception."); }
            finally { Cleanup(); }
        }

        private void Cleanup()
        {
            GameDataManager.SelectedUnit = _previousUnit;
            GameDataManager.SelectedLeader = _previousLeader;
            GameDataManager.SelectedHex = _previousHex;
            GameDataManager.SelectedHexData = _previousHexData;
            GameDataManager.CurrentHexMap = _previousMap;
            GameDataManager.CurrentMapSize = _previousSize;
            GameDataManager.CurrentMapTheme = _previousTheme;
            DataInstance.SetValue(null, _previousData);
            SpriteInstance.SetValue(null, _previousSprites);
            _map?.Dispose();
            if (_battle.IsValid()) EditorSceneManager.ClosePreviewScene(_battle);
            if (_menu.IsValid()) EditorSceneManager.ClosePreviewScene(_menu);
            AppService.SetTestHandler(_previousHandler);
        }

        private CombatUnit Register(Side side = Side.Player)
        {
            var unit = new CombatUnit("Selected panel unit", UnitClassification.RECON, UnitRole.GroundCombat,
                side, side == Side.Player ? Nationality.USSR : Nationality.MJ);
            unit.SetPosition(new Position2D(2, 2));
            Assert.That(_data.RegisterCombatUnit(unit), Is.True);
            return unit;
        }

        private void Select(int x = 2, int y = 2)
        {
            _hexSelection.SelectHex(new Position2D(x, y));
            _updater.RefreshSelectionPanels();
        }

        [Test]
        public void Bootstrap_UsesTheExistingHudAndHidesBothPanels()
        {
            Assert.That(_updater, Is.Not.Null);
            Assert.That(_unitPanel.gameObject.activeSelf, Is.False);
            Assert.That(_terrainPanel.gameObject.activeSelf, Is.False);
            Assert.That(GameDataManager.SelectedHex, Is.EqualTo(GameDataManager.NoHexSelected));
            Assert.That(_controller.InitializeSelectionPanels(), Is.True);
            Assert.That(System.IO.File.ReadAllText("Assets/Scenes/BattleScene.unity"), Does.Not.Contain("07ac07b75122654458db9d74bc27200c"), "The saved battle scene must not reference the retired manager script.");
        }

        [Test]
        public void EmptyHexSelection_ShowsOnlyTerrainAndResolvesItsData()
        {
            Select();
            Assert.That(_terrainPanel.gameObject.activeSelf, Is.True);
            Assert.That(_unitPanel.gameObject.activeSelf, Is.False);
            Assert.That(GameDataManager.SelectedUnit, Is.Null);
            Assert.That(GameDataManager.SelectedHexData, Is.SameAs(_map.GetHexAt(new Position2D(2, 2))));
            var image = (UnityEngine.UI.Image)typeof(Prefab_TerrainPanel).GetField("_terrainImage", PrivateInstance).GetValue(_terrainPanel);
            Assert.That(image.enabled, Is.EqualTo(image.sprite != null));
            var portrait = image.sprite;
            _updater.RefreshSelectionPanels();
            Assert.That(image.sprite, Is.SameAs(portrait));
        }

        [Test]
        public void FriendlyHexSelection_ResolvesUnitAndUpdatesItsMonitorAndActions()
        {
            var unit = Register();
            unit.CombatActions.SetMax(3); unit.CombatActions.SetCurrent(3);
            Select();
            Assert.That(GameDataManager.SelectedUnit, Is.SameAs(unit));
            Assert.That(_unitPanel.gameObject.activeSelf && _terrainPanel.gameObject.activeSelf, Is.True);
            Assert.That(UnitText("_infoText").text, Does.Contain(unit.UnitName));
            Assert.That(UnitText("_combatActionsText").text, Is.EqualTo("3"));
        }

        [Test]
        public void ActionsSpentOnTheSameSelection_RefreshTheCounter()
        {
            var unit = Register();
            unit.CombatActions.SetMax(3); unit.CombatActions.SetCurrent(3);
            Select();
            unit.CombatActions.SetCurrent(0);
            _updater.RefreshSelectionPanels();
            Assert.That(UnitText("_combatActionsText").text, Is.EqualTo("0"));
        }

        [Test]
        public void SwitchingFromUnitToEmptyHex_HidesAndClearsOnlyTheUnitPanel()
        {
            Register(); Select(); Select(4, 4);
            Assert.That(_terrainPanel.gameObject.activeSelf, Is.True);
            Assert.That(_unitPanel.gameObject.activeSelf, Is.False);
            Assert.That(UnitText("_infoText").text, Is.Empty);
            Assert.That(Counts(), Is.All.EqualTo(string.Empty));
        }

        [Test]
        public void ClearingSelection_HidesBothAndClearsSelectionData()
        {
            Register(); Select();
            _hexSelection.ClearSelectionAndNotify();
            _updater.RefreshSelectionPanels();
            Assert.That(_terrainPanel.gameObject.activeSelf || _unitPanel.gameObject.activeSelf, Is.False);
            Assert.That(GameDataManager.SelectedUnit, Is.Null);
            Assert.That(GameDataManager.SelectedLeader, Is.Null);
            Assert.That(GameDataManager.SelectedHexData, Is.Null);
            Assert.That(UnitText("_infoText").text, Is.Empty);
        }

        [TestCase(SpottedLevel.Level0, false)]
        [TestCase(SpottedLevel.Level1, true)]
        [TestCase(SpottedLevel.Level5, true)]
        public void EnemySelection_ShowsOnlyVisibleContactsWithZeroActionCounts(SpottedLevel level, bool visible)
        {
            var enemy = Register(Side.AI);
            enemy.SetSpottedLevel(level);
            Select();
            Assert.That(_terrainPanel.gameObject.activeSelf, Is.True);
            Assert.That(_unitPanel.gameObject.activeSelf, Is.EqualTo(visible));
            Assert.That(GameDataManager.SelectedUnit, Is.SameAs(visible ? enemy : null));
            Assert.That(Counts(), Is.All.EqualTo(visible ? "0" : string.Empty));
        }

        [Test]
        public void HiddenAuthoredPanels_AreReusedAndReactivatedWithoutReplacementObjects()
        {
            Assert.That(_unitPanel.gameObject.activeSelf || _terrainPanel.gameObject.activeSelf, Is.False);
            Assert.That(_controller.InitializeSelectionPanels(), Is.True);
            Register(); Select();
            Assert.That(Find<Prefab_UnitPanel>(_battle), Is.SameAs(_unitPanel));
            Assert.That(Find<Prefab_TerrainPanel>(_battle), Is.SameAs(_terrainPanel));
            Assert.That(_unitPanel.gameObject.activeSelf && _terrainPanel.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void TerrainPortraitCatalog_KeysMatchTheImportedSpriteNames()
        {
            var names = typeof(SpriteManager).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string)
                    && (field.Name == "TP_Water" || field.Name.StartsWith("ME_TP_")
                        || field.Name.StartsWith("EU_TP_") || field.Name.StartsWith("CH_TP_")))
                .Select(field => (string)field.GetRawConstantValue()).ToArray();
            Assert.That(names.Length, Is.EqualTo(22));
            foreach (var name in names)
            {
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI Graphics/Elements/Terrain Portraits/" + name + ".png");
                Assert.That(sprite, Is.Not.Null, name);
                Assert.That(sprite.name.Replace("(Clone)", ""), Is.EqualTo(name));

            }
        }
    }
}
