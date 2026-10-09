using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Persistence;
using HammerAndSickle.SceneManagement;
using HammerAndSickle.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HammerAndSickle.Tests
{
    /// <summary>Checks the saved menu and actual thumbnail display without saving or replacing the user's scene.</summary>
    public class ScenarioThumbnailTests
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        private const string ThumbnailRoot = "Assets/Art/UI Graphics/Elements/Scenario Thumbs/";
        private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly FieldInfo HandlerField = typeof(AppService).GetField("_testHandler", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo LoadMethod = typeof(ScenarioDialog_Scene0).GetMethod("LoadThumbnail", PrivateInstance);
        private Scene _preview;
        private ScenarioDialog_Scene0 _dialog;
        private UnityEngine.UI.Image _image;
        private Sprite _fallback;
        private TestHandler _previousHandler;
        private TestHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _previousHandler = (TestHandler)HandlerField.GetValue(null);
            _handler = new TestHandler();
            AppService.SetTestHandler(_handler);
            try
            {
                _preview = EditorSceneManager.OpenPreviewScene(MainMenuPath);
                _dialog = _preview.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<ScenarioDialog_Scene0>(true)).Single();
                var serialized = new SerializedObject(_dialog);
                _image = (UnityEngine.UI.Image)serialized.FindProperty("_thumbnailImage").objectReferenceValue;
                _fallback = (Sprite)serialized.FindProperty("_placeholderThumbnail").objectReferenceValue;
                Assert.That(_image, Is.Not.Null);
                Assert.That(_fallback, Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(ThumbnailRoot + "ui-element-thumb-default.png")));
                Assert.That(_fallback, Is.Not.Null);
            }
            catch
            {
                Cleanup();
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            try { Assert.That(_handler.ExceptionCount, Is.Zero, "Thumbnail display must not hide an application exception."); }
            finally { Cleanup(); }
        }

        private void Cleanup()
        {
            try
            {
                if (_preview.IsValid()) EditorSceneManager.ClosePreviewScene(_preview);
            }
            finally { AppService.SetTestHandler(_previousHandler); }
        }

        private void Load(string key) => LoadMethod.Invoke(_dialog, new object[] { new ScenarioManifest { ThumbnailFilename = key } });

        [TestCase("scenario_1", "ui-element-thumb-scen1.png")]
        [TestCase("scenario_2", "ui-element-thumb_scen2.png")]
        [TestCase("scenario_3", "ui-element-thumb_scen3.png")]
        [TestCase("scenario_4", "ui-element-thumb_scen4.png")]
        [TestCase(" SCENARIO_1 ", "ui-element-thumb-scen1.png")]
        public void SavedMenu_KeyDisplaysTheAssignedSprite(string key, string filename)
        {
            var expected = AssetDatabase.LoadAssetAtPath<Sprite>(ThumbnailRoot + filename);
            Assert.That(expected, Is.Not.Null);
            Load(key);
            Assert.That(_image.sprite, Is.SameAs(expected));
            Assert.That(_handler.UiMessageCount, Is.Zero);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("scenario_99")]
        [TestCase("scenario_1.png")]
        public void MissingOrUnknownKey_ReplacesThePreviousImageWithDefault(string key)
        {
            Load("scenario_1");
            Assert.That(_image.sprite, Is.Not.SameAs(_fallback));
            Load(key);
            Assert.That(_image.sprite, Is.SameAs(_fallback));
            Assert.That(_handler.UiMessageCount, Is.EqualTo(string.IsNullOrWhiteSpace(key) ? 0 : 1));
        }

        [Test]
        public void MissingMappedSprite_ShowsDefaultAndReportsTheKey()
        {
            Load("scenario_1");
            var serialized = new SerializedObject(_dialog);
            serialized.FindProperty("_scenarioThumbnails").GetArrayElementAtIndex(0)
                .FindPropertyRelative("Sprite").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Load("scenario_1");
            Assert.That(_image.sprite, Is.SameAs(_fallback));
            Assert.That(_handler.LatestUiMessage, Does.Contain("scenario_1"));
        }

        [Test]
        public void MissingMappingList_ShowsDefault()
        {
            Load("scenario_1");
            typeof(ScenarioDialog_Scene0).GetField("_scenarioThumbnails", PrivateInstance).SetValue(_dialog, null);
            Load("scenario_1");
            Assert.That(_image.sprite, Is.SameAs(_fallback));
        }

        [Test]
        public void MissingManifest_ShowsDefault()
        {
            Load("scenario_1");
            LoadMethod.Invoke(_dialog, new object[] { null });
            Assert.That(_image.sprite, Is.SameAs(_fallback));
        }

        [Test]
        public void SavedMenu_BuildDependenciesIncludeEveryThumbnailAndDefault()
        {
            var paths = Directory.GetFiles(ThumbnailRoot, "*.png").Select(path => path.Replace('\\', '/')).ToArray();
            Assert.That(paths.Length, Is.EqualTo(5));
            var dependencies = AssetDatabase.GetDependencies(MainMenuPath, true);
            Assert.That(paths.Except(dependencies), Is.Empty, "Direct menu references must retain the Art assets in player builds.");
            var mappings = new SerializedObject(_dialog).FindProperty("_scenarioThumbnails");
            Assert.That(mappings.arraySize, Is.EqualTo(4));
        }

        [TestCase("Assets/StreamingAssets/Scenarios/khost/mission_khost.manifest")]
        [TestCase("Assets/StreamingAssets/Campaigns/grand_campaign/m01_khost/campaign_khost.manifest")]
        public void ShippedKhostManifest_DeserializesScenarioKeyAndDisplaysItsImage(string path)
        {
            var manifest = JsonSerializer.Deserialize<ScenarioManifest>(File.ReadAllText(path), JsonPolicy.Content);
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.IsValid(), Is.True);
            Assert.That(manifest.ThumbnailFilename, Is.EqualTo("scenario_1"));
            LoadMethod.Invoke(_dialog, new object[] { manifest });
            Assert.That(_image.sprite, Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(ThumbnailRoot + "ui-element-thumb-scen1.png")));
        }
    }
}
