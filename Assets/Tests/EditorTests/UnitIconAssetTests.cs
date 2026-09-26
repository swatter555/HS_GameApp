using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Map;
using HammerAndSickle.Models;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// RC-0: validate imported art against live profiles, formation bays and the saved bootstrap scene.
    /// This fixture observes assets; it does not repack atlases, change importers or save scenes.
    /// </summary>
    public class UnitIconAssetTests : BaseTestFixture
    {
        private const string IconRoot = "Assets/Art/Sprites/Unit Icons";
        private const string AtlasRoot = "Assets/Art/Sprite Atlases";
        private static readonly string[] Groups = { "Soviet", "NATO", "Generic", "Regional", "Chinese" };
        private static readonly string[] Fields =
            { "_sovietIconAtlas", "_natoIconAtlas", "_genericIconAtlas", "_regionalIconAtlas", "_chineseIconAtlas" };

        // These persisted profiles await the national support split. Do not substitute another
        // country's picture. Remove each exception when the profile is resolved or safely retired.
        private static readonly Dictionary<WeaponType, string> Pending = new()
        {
            { WeaponType.ART_LIGHT_ARAB, "GEN_LightArt" },
            { WeaponType.ART_HEAVY_ARAB, "GEN_HeavyArt" },
            { WeaponType.TRK_GEN_ARAB, "AR_Truck_W" }
        };

        private Dictionary<string, string> _assets;
        private Dictionary<string, SpriteAtlas> _atlases;
        private SpriteAtlas[] _sceneAtlases;

        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized) CombatUnitDB.Initialize();

            var paths = Directory.GetFiles(IconRoot, "*.png", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/')).ToArray();
            Assert.That(paths, Is.Not.Empty);
            Assert.That(paths.GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1).Select(g => g.Key), Is.Empty, "Ambiguous unit filenames.");
            _assets = paths.ToDictionary(Path.GetFileNameWithoutExtension, StringComparer.Ordinal);
            _atlases = Groups.ToDictionary(g => g,
                g => AssetDatabase.LoadAssetAtPath<SpriteAtlas>($"{AtlasRoot}/{g}.spriteatlas"));
            foreach (var pair in _atlases) Assert.That(pair.Value, Is.Not.Null, pair.Key);

            // A preview scene checks saved references without replacing or saving the user's scene.
            var preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainMenu.unity");
            try
            {
                var managers = preview.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<SpriteManager>(true)).ToArray();
                Assert.That(managers.Length, Is.EqualTo(1));
                var manager = new SerializedObject(managers[0]);
                for (int i = 0; i < Groups.Length; i++)
                    Assert.That(manager.FindProperty(Fields[i])?.objectReferenceValue,
                        Is.EqualTo(_atlases[Groups[i]]), $"MainMenu {Fields[i]} must reference {Groups[i]}.");
                _sceneAtlases = typeof(SpriteManager).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.FieldType == typeof(SpriteAtlas))
                    .Select(f => (SpriteAtlas)f.GetValue(managers[0])).Where(a => a != null).Distinct().ToArray();
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        [Test]
        public void EveryDeclaredUnitSprite_HasAnExactImportedAsset_AndEveryAssetHasADeclaration()
        {
            var names = typeof(SpriteManager).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string) && IsUnitConstant(f.Name))
                .Select(f => (string)f.GetRawConstantValue()).ToArray();
            Assert.That(names.Distinct().Count(), Is.EqualTo(names.Length), "Duplicate unit declarations.");
            var required = names.Except(Pending.Values).ToArray();
            Assert.That(_assets.Keys, Is.EquivalentTo(required),
                "Declared unit art and imported PNGs must match, including art for upcoming profiles.");
        }

        private static bool IsUnitConstant(string name)
        {
            // CH is also used by the existing Chinese map theme and terrain portraits.
            if (name.StartsWith("CH_TP_", StringComparison.Ordinal)) return false;
            if (new[] { "CH_Nameplate", "CH_MajorCity", "CH_MinorCity", "CH_Sprawl", "CH_Fort", "CH_Airbase" }
                .Contains(name)) return false;
            return Regex.IsMatch(name, @"^(SV|US|FR|GE|UK|NATO|IQ|IR|MJ|SA|CH|GEN|AR)_");
        }

        [Test]
        public void UnitIconPrefab_DefaultSpriteStillResolvesAfterReplacementImport()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/Units/Prefab_UnitIcons.prefab");
            Assert.That(prefab, Is.Not.Null);
            var icon = prefab.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(r => r.name == "SpriteRenderer_Icon");
            Assert.That(icon.sprite, Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>(
                $"{IconRoot}/Soviet/SV_T72A.png")));
            Assert.That(icon.sprite, Is.Not.Null);
        }

        [Test]
        public void ImportedIcons_PreserveTheFullCanvasAndImportContract()
        {
            foreach (var pair in _assets)
            {
                var importer = AssetImporter.GetAtPath(pair.Value) as TextureImporter;
                Assert.That(importer, Is.Not.Null, pair.Value);
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                Assert.That((width, height), Is.EqualTo((512, 512)), pair.Value);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), pair.Value);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), pair.Value);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100), pair.Value);
                Assert.That(importer.alphaIsTransparency, Is.True, pair.Value);
                Assert.That(importer.mipmapEnabled, Is.False, pair.Value);
                Assert.That(importer.isReadable, Is.False, pair.Value);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), pair.Value);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pair.Value);
                Assert.That(sprite, Is.Not.Null, pair.Value);
                Assert.That(sprite.name, Is.EqualTo(pair.Key), pair.Value);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(512, 512)), pair.Value);
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(256, 256)), pair.Value);
            }
        }

        [Test]
        public void EveryImportedIcon_IsPackedOnce_InItsAssignedAtlas_WithoutSearchCollisions()
        {
            var packables = _atlases.ToDictionary(p => p.Key, p => p.Value.GetPackables());
            foreach (var pair in packables)
            {
                Assert.That(pair.Value.Any(p => p == null), Is.False, $"{pair.Key}: missing packable.");
                Assert.That(SpriteAtlasExtensions.IsIncludeInBuild(_atlases[pair.Key]), Is.True, pair.Key);
            }
            foreach (var pair in _assets)
            {
                string group = Path.GetFileName(Path.GetDirectoryName(pair.Value));
                Assert.That(_atlases.ContainsKey(group), Is.True, pair.Value);
                Assert.That(packables.SelectMany(p => p.Value)
                    .Count(p => AssetDatabase.GetAssetPath(p) == pair.Value), Is.EqualTo(1), pair.Value);
                Assert.That(packables[group].Count(p => AssetDatabase.GetAssetPath(p) == pair.Value),
                    Is.EqualTo(1), $"Wrong atlas: {pair.Value}");
                Assert.That(ResolvingAtlases(pair.Key), Is.EquivalentTo(new[] { _atlases[group] }),
                    $"{pair.Key}: missing packed sprite or ambiguous runtime atlas lookup.");
            }
        }

        [Test]
        public void EveryImportedHelicopterSet_HasSixFramesInTheSameAtlas()
        {
            foreach (var group in _assets.Keys.Where(n => Regex.IsMatch(n, @"_Frame\d+$"))
                .GroupBy(n => Regex.Replace(n, @"_Frame\d+$", "")))
            {
                Assert.That(group, Is.EquivalentTo(Enumerable.Range(0, 6).Select(i => $"{group.Key}_Frame{i}")), group.Key);
                Assert.That(group.Select(n => Path.GetDirectoryName(_assets[n])).Distinct().Count(),
                    Is.EqualTo(1), group.Key);
            }
        }

        [Test]
        public void ProfilesAndFormationBays_ResolveThroughTheSavedAtlases_AndWriteInventory()
        {
            var failures = new List<string>();
            var rows = new StringBuilder("Profile\tDisplay name\tSprite\tAsset\tGUID\tAtlas\tFormation bays\tState\n");
            var usages = new Dictionary<WeaponType, List<string>>();
            foreach (string id in CombatUnitDB.GetAllTemplateIds().OrderBy(id => id))
            {
                var bays = CombatUnitDB.GetUnitTemplate(id).EquipmentBays;
                foreach (var slot in new[] { (bays.Deployed, DeploymentPosition.Deployed),
                    (bays.Mobile, DeploymentPosition.Mobile), (bays.Embarked, DeploymentPosition.Embarked) })
                {
                    if (slot.Item1 == WeaponType.NONE) continue;
                    if (!WeaponProfileDB.HasWeaponProfile(slot.Item1))
                    { failures.Add($"{id}/{slot.Item2}: unregistered {slot.Item1}"); continue; }
                    if (!usages.ContainsKey(slot.Item1)) usages[slot.Item1] = new List<string>();
                    usages[slot.Item1].Add($"{id}/{slot.Item2}");
                    string expected = WeaponProfileDB.GetWeaponProfile(slot.Item1).IconProfile.Icon;
                    foreach (HexDirection facing in Enum.GetValues(typeof(HexDirection)))
                        if (bays.GetIcon(slot.Item2, facing) != expected)
                            failures.Add($"{id}/{slot.Item2}/{facing}: wrong icon");
                }
            }
            var usedSprites = new HashSet<string>();
            var seenPending = new HashSet<WeaponType>();
            foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
            {
                if (!WeaponProfileDB.HasWeaponProfile(type)) continue;
                var profile = WeaponProfileDB.GetWeaponProfile(type);
                string icon = profile.IconProfile.Icon;
                string[] frames = profile.IconProfile.IconType == RegimentIconType.Helo_Animation
                    ? Enumerable.Range(0, 6).Select(i => Regex.Replace(icon, @"_Frame0$", $"_Frame{i}")).ToArray()
                    : new[] { icon };
                foreach (string frame in frames)
                {
                    usedSprites.Add(frame);
                    var resolving = ResolvingAtlases(frame);
                    string state = "Connected; visual acceptance pending";
                    if (Pending.TryGetValue(type, out string pendingIcon))
                    {
                        seenPending.Add(type);
                        if (frame != pendingIcon || resolving.Length != 0 || _assets.ContainsKey(frame))
                            failures.Add($"{type}: stale pending exception; remove or reconcile it.");
                        state = "PENDING: national support profile/art split";
                    }
                    else
                    {
                        if (resolving.Length != 1) failures.Add($"{type}/{frame}: resolves in {resolving.Length} atlases");
                        if (type != WeaponType.BASE_AIRBASE && !_assets.ContainsKey(frame))
                            failures.Add($"{type}/{frame}: missing imported unit PNG");
                    }
                    string asset = _assets.TryGetValue(frame, out string imported) ? imported :
                        (type == WeaponType.BASE_AIRBASE ? FindSpritePath(frame) : "");
                    rows.AppendLine(string.Join("\t", type, profile.LongName, frame, asset,
                        AssetDatabase.AssetPathToGUID(asset), string.Join(";", resolving.Select(AssetDatabase.GetAssetPath)),
                        usages.TryGetValue(type, out var uses) ? string.Join(";", uses) : "No current template", state));
                }
            }
            foreach (var pair in _assets.Where(p => !usedSprites.Contains(p.Key)).OrderBy(p => p.Key))
                rows.AppendLine(string.Join("\t", "", "", pair.Key, pair.Value, AssetDatabase.AssetPathToGUID(pair.Value),
                    string.Join(";", ResolvingAtlases(pair.Key).Select(AssetDatabase.GetAssetPath)), "",
                    pair.Key.StartsWith("FR_Puma") || pair.Key.StartsWith("FR_Gazelle")
                        ? "Roster pending; approved provisional French frames" : "No registered profile reference"));
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/unit-icon-reachability.tsv", rows.ToString());
            TestContext.Progress.WriteLine($"{WeaponProfileDB.ProfileCount} profiles; {CombatUnitDB.TemplateCount} templates; " +
                $"{_assets.Count} PNGs. Inventory: Temp/unit-icon-reachability.tsv. Pending: {string.Join(", ", seenPending)}");
            Assert.That(seenPending, Is.EquivalentTo(Pending.Keys), "Remove unused pending exceptions.");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private SpriteAtlas[] ResolvingAtlases(string name)
        {
            var matches = new List<SpriteAtlas>();
            foreach (var atlas in _sceneAtlases)
            {
                var sprite = atlas.GetSprite(name);
                if (sprite == null) continue;
                matches.Add(atlas);
                UnityEngine.Object.DestroyImmediate(sprite); // GetSprite returns a temporary clone.
            }
            return matches.ToArray();
        }

        private static string FindSpritePath(string name) => AssetDatabase.FindAssets($"{name} t:Sprite", new[] { "Assets/Art" })
            .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == name) ?? "";
    }
}
