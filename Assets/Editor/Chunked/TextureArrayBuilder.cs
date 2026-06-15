using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HammerAndSickle.EditorTools.Chunked
{
    /// <summary>
    /// Phase 3 — Builds a 108-slice Texture2DArray from terrain tile PNGs.
    /// Slot math: slot = (int)terrainType * 12 + variant.
    ///
    /// Terrain scope (decided 2026-04-17):
    ///   - Themed (per set):   Clear, Forest, Rough, Mountains — loaded from {Set}/
    ///   - Shared (all sets):  Water, Marsh                    — loaded from Any/
    ///   - Overlay-only:       MinorCity, MajorCity, Impassable — placeholder magenta, no art
    ///
    /// Naming convention: {prefix}_{TerrainName}_{variant}.png
    ///   - Themed prefixes: ME (MiddleEast), EU (Europe), CN (China)
    ///   - Shared prefix:   AN (Any)
    ///
    /// POC tolerance: missing files log warnings and fill with magenta, they do not hard-fail.
    /// Missing REQUIRED folders (Any/Water, Any/Marsh, or a themed required terrain) log errors.
    /// </summary>
    public static class TextureArrayBuilder
    {
        private const int TileSize = 512;
        private const int VariantsPerTerrain = 12;
        private const int TerrainTypeCount = 9;
        private const int TotalSlices = TerrainTypeCount * VariantsPerTerrain; // 108

        private const string ArtRoot = "Assets/Art/HexTiles";
        private const string OutDir = "Assets/Resources/Chunked";

        private const string SharedSetName = "Any";
        private const string SharedSetPrefix = "AN";

        private static readonly string[] TerrainTypeNames =
        {
            "Water", "Clear", "Forest", "Rough", "Marsh",
            "Mountains", "MinorCity", "MajorCity", "Impassable"
        };

        // Which terrains load from Any/ instead of the themed set folder.
        private static readonly HashSet<string> SharedTerrains = new()
        {
            "Water", "Marsh"
        };

        // Which terrains are overlay-only — no chunk texture, magenta placeholder is intentional.
        private static readonly HashSet<string> OverlayOnlyTerrains = new()
        {
            "MinorCity", "MajorCity", "Impassable"
        };

        private static readonly string[] TerrainSetNames = { "MiddleEast", "Europe", "China" };
        private static readonly string[] SetPrefixes     = { "ME",         "EU",     "CN"    };

        #region Menu Items

        [MenuItem("Tools/Hex Chunk/Rebuild Terrain Array (MiddleEast)")]
        public static void BuildMiddleEast() => BuildTerrainArray(0);

        [MenuItem("Tools/Hex Chunk/Rebuild Terrain Array (Europe)")]
        public static void BuildEurope() => BuildTerrainArray(1);

        [MenuItem("Tools/Hex Chunk/Rebuild Terrain Array (China)")]
        public static void BuildChina() => BuildTerrainArray(2);

        [MenuItem("Tools/Hex Chunk/Rebuild All Terrain Arrays")]
        public static void BuildAll()
        {
            for (int i = 0; i < TerrainSetNames.Length; i++)
                BuildTerrainArray(i);
        }

        #endregion // Menu Items

        #region Core Builder

        private static void BuildTerrainArray(int setIndex)
        {
            string setName = TerrainSetNames[setIndex];
            string setPrefix = SetPrefixes[setIndex];
            string setDir = $"{ArtRoot}/{setName}";
            string sharedDir = $"{ArtRoot}/{SharedSetName}";
            string assetPath = $"{OutDir}/TerrainArray_{setName}.asset";

            // POC tolerance: skip missing themed sets with a warning
            if (!AssetDatabase.IsValidFolder(setDir))
            {
                Debug.LogWarning($"[HexChunk] Themed set folder missing, skipping: {setDir}");
                return;
            }

            // Shared folder is required for Water + Marsh
            if (!AssetDatabase.IsValidFolder(sharedDir))
            {
                Debug.LogError($"[HexChunk] Shared folder '{SharedSetName}' is missing: {sharedDir}. " +
                               $"Water and Marsh art must live there.");
                return;
            }

            // Warn if themed folder has orphan Water/Marsh subfolders (should be in Any/)
            foreach (var sharedTerrain in SharedTerrains)
            {
                string orphan = $"{setDir}/{sharedTerrain}";
                if (AssetDatabase.IsValidFolder(orphan))
                {
                    Debug.LogWarning($"[HexChunk] Orphan folder: {orphan} is a shared terrain and " +
                                     $"should be moved to {sharedDir}/{sharedTerrain}.");
                }
            }

            try
            {
                Directory.CreateDirectory(OutDir);

                // Build the magenta placeholder texture once
                var magenta = CreateSolidTexture(TileSize, Color.magenta);

                var array = new Texture2DArray(TileSize, TileSize, TotalSlices,
                    TextureFormat.RGBA32, mipChain: true, linear: false)
                {
                    name = $"TerrainArray_{setName}",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 4,
                };

                int populated = 0;
                int placeholders = 0;
                int overlayOnly = 0;

                for (int terrainIndex = 0; terrainIndex < TerrainTypeCount; terrainIndex++)
                {
                    string terrainName = TerrainTypeNames[terrainIndex];

                    // Overlay-only terrains (cities, impassable) — fill with magenta, no art expected.
                    if (OverlayOnlyTerrains.Contains(terrainName))
                    {
                        for (int variant = 0; variant < VariantsPerTerrain; variant++)
                        {
                            int slot = terrainIndex * VariantsPerTerrain + variant;
                            CopySliceWithMips(magenta, array, slot);
                            overlayOnly++;
                        }
                        continue;
                    }

                    // Resolve source folder + prefix: shared terrains come from Any/
                    bool isShared = SharedTerrains.Contains(terrainName);
                    string terrainDir = isShared
                        ? $"{sharedDir}/{terrainName}"
                        : $"{setDir}/{terrainName}";
                    string prefix = isShared ? SharedSetPrefix : setPrefix;

                    if (!AssetDatabase.IsValidFolder(terrainDir))
                    {
                        Debug.LogError($"[HexChunk] Required terrain folder missing: {terrainDir}. " +
                                       $"All 12 variants of '{terrainName}' will be magenta placeholders.");
                    }

                    bool terrainFolderExists = AssetDatabase.IsValidFolder(terrainDir);

                    for (int variant = 0; variant < VariantsPerTerrain; variant++)
                    {
                        int slot = terrainIndex * VariantsPerTerrain + variant;

                        Texture2D source = null;
                        if (terrainFolderExists)
                            source = LoadTileTexture(terrainDir, prefix, terrainName, variant);

                        Texture2D tile = source != null ? source : magenta;
                        CopySliceWithMips(tile, array, slot);

                        if (source != null)
                            populated++;
                        else
                            placeholders++;
                    }
                }

                Object.DestroyImmediate(magenta);

                array.Apply(updateMipmaps: false, makeNoLongerReadable: true);

                // Overwrite existing asset if present
                var existing = AssetDatabase.LoadAssetAtPath<Texture2DArray>(assetPath);
                if (existing != null) AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.CreateAsset(array, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[HexChunk] Built TerrainArray_{setName}: " +
                          $"{populated} tiles populated, {placeholders} missing-tile placeholders, " +
                          $"{overlayOnly} overlay-only slots (intentional placeholder). " +
                          $"Asset: {assetPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HexChunk] Failed to build TerrainArray_{setName}: {ex}");
            }
        }

        #endregion // Core Builder

        #region Tile Loading

        /// <summary>
        /// Loads a tile PNG by convention: {prefix}_{TerrainName}_{variant}.png
        /// e.g. ME_Clear_0.png, ME_Forest_11.png, AN_Water_3.png, AN_Marsh_7.png
        /// Ensures Read/Write is enabled on the importer so GetPixels succeeds.
        /// Returns null if file not found (warning, not error).
        /// </summary>
        private static Texture2D LoadTileTexture(string terrainDir, string prefix,
            string terrainName, int variant)
        {
            string fileName = $"{prefix}_{terrainName}_{variant}";
            string assetPath = $"{terrainDir}/{fileName}.png";

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[HexChunk] Tile missing (placeholder used): {assetPath}");
                return null;
            }

            // Ensure readable for GetPixels; reimport only if needed
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                Debug.LogWarning($"[HexChunk] Tile failed to load after reimport: {assetPath}");
                return null;
            }

            if (tex.width != TileSize || tex.height != TileSize)
            {
                Debug.LogError($"[HexChunk] Tile wrong size ({tex.width}x{tex.height}), " +
                               $"expected {TileSize}x{TileSize}: {assetPath}");
                return null;
            }

            return tex;
        }

        #endregion // Tile Loading

        #region Helpers

        /// <summary>
        /// Copies all mip levels from a source Texture2D into one slice of the array.
        /// Uses GetPixels/SetPixels to handle format conversion (e.g. RGB24 source → RGBA32 array).
        /// </summary>
        private static void CopySliceWithMips(Texture2D source, Texture2DArray array, int slice)
        {
            var scratch = new Texture2D(source.width, source.height, TextureFormat.RGBA32,
                mipChain: true, linear: false);
            scratch.SetPixels(source.GetPixels());
            scratch.Apply(updateMipmaps: true, makeNoLongerReadable: false);

            int mips = scratch.mipmapCount;
            for (int mip = 0; mip < mips; mip++)
                Graphics.CopyTexture(scratch, 0, mip, array, slice, mip);

            Object.DestroyImmediate(scratch);
        }

        private static Texture2D CreateSolidTexture(int size, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true, linear: false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            return tex;
        }

        #endregion // Helpers
    }
}
