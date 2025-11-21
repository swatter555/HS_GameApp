#if UNITY_EDITOR
using HammerAndSickle.Services;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace HammerAndSickle.Core.Helpers
{
    /// <summary>
    /// Provides helper methods for creating sprite atlases from selected folders in the Unity Editor.
    /// </summary>
    public class AtlasHelper : MonoBehaviour
    {
        /// <summary>
        /// Creates sprite atlases from selected folders. This method is accessible from the Unity Editor menu.
        /// It processes each selected folder, creating a separate sprite atlas for the sprites within.
        /// </summary>
        [MenuItem("Assets/Create/Sprite Atlases From Selected Folders")]
        public static void AddSpritesToAtlas()
        {
            // Get all selected folders in the Project window
            var selectedFolders = Selection.GetFiltered<DefaultAsset>(SelectionMode.Assets);

            foreach (var folderAsset in selectedFolders)
            {
                string folderPath = AssetDatabase.GetAssetPath(folderAsset);

                // Skip processing if the selected item is not a valid folder
                if (!AssetDatabase.IsValidFolder(folderPath)) continue;

                // Create a new sprite atlas for the current folder
                SpriteAtlas atlas = new();

                // Configure the settings of the atlas
                ConfigureAtlasSettings(atlas);

                // Find and add all sprites in the current folder to the atlas
                string[] spriteGUIDs = AssetDatabase.FindAssets("", new[] { folderPath });
                foreach (string guid in spriteGUIDs)
                {
                    string spritePath = AssetDatabase.GUIDToAssetPath(guid);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    atlas.Add(new[] { sprite });
                }

                // Save the atlas asset with a name based on the folder name
                string atlasName = System.IO.Path.GetFileName(folderPath);
                AssetDatabase.CreateAsset(atlas, System.IO.Path.Combine(AppService.SpriteAtlasPath, $"{atlasName}.spriteatlas"));
            }

            // Save all changes made to assets
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Configures the settings of a sprite atlas for optimal texture packing and quality.
        /// </summary>
        /// <param name="atlas">The sprite atlas to configure.</param>
        private static void ConfigureAtlasSettings(SpriteAtlas atlas)
        {
            // Define and set packing settings for the atlas
            SpriteAtlasPackingSettings packingSettings = new()
            {
                enableRotation = false,  // Disables rotation for consistency
                enableTightPacking = false, // Ensures sprites are packed with specified padding
                padding = 2 // Adds padding around sprites to prevent texture bleeding
            };
            atlas.SetPackingSettings(packingSettings);

            // Define and set texture settings for the atlas
            SpriteAtlasTextureSettings textureSettings = new()
            {
                readable = true, // Allows texture data to be accessed by scripts
                generateMipMaps = false, // Disables MipMap generation for clearer textures
                filterMode = FilterMode.Bilinear, // Sets texture filtering mode
                sRGB = true // Ensures correct color space handling
            };
            atlas.SetTextureSettings(textureSettings);
        }
    }
}
#endif
