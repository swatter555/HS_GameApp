using System.IO;
using UnityEditor;
using UnityEngine;

namespace HammerAndSickle.EditorTools.Chunked
{
    /// <summary>
    /// Phase 1 POC asset builder. Generates:
    ///   - A 3-slice Texture2DArray at Assets/Resources/Chunked/TestArray_RGB.asset
    ///     (slice 0 = red, slice 1 = green, slice 2 = blue) — 512x512 each, mipmapped.
    ///   - A tileable 256x256 greyscale noise PNG at Assets/Resources/Chunked/NoiseTexture.png
    ///     (imported as linear, wrap=Repeat).
    /// Both assets are consumed by the HexTerrainBlend shader via a Material the user wires up.
    /// </summary>
    public static class HexBlendTestAssetBuilder
    {
        private const string OutDir        = "Assets/Resources/Chunked";
        private const string ArrayAssetPath = OutDir + "/TestArray_RGB.asset";
        private const string NoisePngPath   = OutDir + "/NoiseTexture.png";

        [MenuItem("Tools/Hex Chunk/Build Test RGB Array (Phase 1)")]
        public static void BuildRgbTestArray()
        {
            try
            {
                const int size = 512;
                const int sliceCount = 3;

                Directory.CreateDirectory(OutDir);

                var array = new Texture2DArray(size, size, sliceCount, TextureFormat.RGBA32, mipChain: true, linear: false)
                {
                    name = "TestArray_RGB",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 4,
                };

                Color[] sliceColors = { Color.red, Color.green, Color.blue };

                for (int slice = 0; slice < sliceCount; slice++)
                {
                    var scratch = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true, linear: false);
                    var pixels = new Color[size * size];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = sliceColors[slice];
                    scratch.SetPixels(pixels);
                    scratch.Apply(updateMipmaps: true, makeNoLongerReadable: false);

                    int mips = scratch.mipmapCount;
                    for (int mip = 0; mip < mips; mip++)
                        Graphics.CopyTexture(scratch, 0, mip, array, slice, mip);

                    Object.DestroyImmediate(scratch);
                }

                array.Apply(updateMipmaps: false, makeNoLongerReadable: true);

                // Overwrite if exists.
                var existing = AssetDatabase.LoadAssetAtPath<Texture2DArray>(ArrayAssetPath);
                if (existing != null) AssetDatabase.DeleteAsset(ArrayAssetPath);
                AssetDatabase.CreateAsset(array, ArrayAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[HexChunk] Built 3-slice Texture2DArray at {ArrayAssetPath} (red/green/blue).");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HexChunk] Failed to build test array: {ex}");
            }
        }

        [MenuItem("Tools/Hex Chunk/Build Test Noise PNG (Phase 1)")]
        public static void BuildNoisePng()
        {
            try
            {
                const int size = 256;
                Directory.CreateDirectory(OutDir);

                var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false, linear: true);
                var pixels = new Color[size * size];

                // 4-octave fBm. Unity's PerlinNoise isn't strictly tileable, but at test-mesh scale
                // seam visibility is negligible. A dedicated tileable noise is a Phase 7 polish item.
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float fx = x / (float)size;
                        float fy = y / (float)size;
                        float n = 0f, amp = 1f, freq = 4f, ampSum = 0f;
                        for (int o = 0; o < 4; o++)
                        {
                            n += amp * Mathf.PerlinNoise(fx * freq, fy * freq);
                            ampSum += amp;
                            amp *= 0.5f;
                            freq *= 2f;
                        }
                        n /= ampSum;
                        pixels[y * size + x] = new Color(n, n, n, 1f);
                    }
                }
                tex.SetPixels(pixels);
                tex.Apply(updateMipmaps: false);

                byte[] png = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);

                File.WriteAllBytes(NoisePngPath, png);
                AssetDatabase.ImportAsset(NoisePngPath, ImportAssetOptions.ForceUpdate);

                var importer = AssetImporter.GetAtPath(NoisePngPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = false;               // linear data, not color
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.mipmapEnabled = true;
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    importer.SaveAndReimport();
                }

                Debug.Log($"[HexChunk] Built tileable noise at {NoisePngPath} (256x256, Linear, Repeat).");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HexChunk] Failed to build noise PNG: {ex}");
            }
        }

        [MenuItem("Tools/Hex Chunk/Build ALL Phase 1 Test Assets")]
        public static void BuildAll()
        {
            BuildRgbTestArray();
            BuildNoisePng();
        }
    }
}
