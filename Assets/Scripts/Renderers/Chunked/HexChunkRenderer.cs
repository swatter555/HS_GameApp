using System.Collections.Generic;
using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Patterns;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using UnityEngine;

namespace HammerAndSickle.Renderers.Chunked
{
    /// <summary>
    /// Singleton that owns the chunk grid and terrain material. Builds chunks on
    /// scenario load via BuildAllChunks; supports partial rebuilds via RebuildChunk
    /// and full rebuilds via Rebuild. Inspector knobs cover both shader-side tuning
    /// (live, no rebuild) and builder-side tuning (rebuild required).
    /// </summary>
    public class HexChunkRenderer : Singleton<HexChunkRenderer>
    {
        private const string CLASS_NAME = nameof(HexChunkRenderer);

        [SerializeField] private Material terrainMaterial;

        [Header("Terrain Set")]
        [Tooltip("Which baked Texture2DArray to bind. Loaded from Resources/Chunked/TerrainArray_<MapTheme>.asset at BuildAllChunks time.")]
        [SerializeField] private MapTheme terrainSet = MapTheme.MiddleEast;

        [Header("Shader Tuning (applied live)")]
        [Tooltip("Noise strength for blend-boundary perturbation. 0 = crisp hex shapes, 1 = maximum wobble.")]
        [Range(0f, 1f)]
        [SerializeField] private float blendNoiseStrength = 0.2f;

        [Tooltip("World-space noise period. Smaller = tighter ripples, larger = broad soft wobble.")]
        [Range(0.1f, 20f)]
        [SerializeField] private float noiseScale = 2.0f;

        [Tooltip("Disable noise entirely (sets the _HEXBLEND_NOISE_OFF shader keyword).")]
        [SerializeField] private bool disableNoise = false;

        [Header("Builder Tuning (rebuild required)")]
        [Tooltip(">1 peaks the dominant slot at each corner (harder edges, hex interior cleaner). <1 flattens (broader blend). 1 = neutral.")]
        [Range(0.25f, 4f)]
        [SerializeField] private float cornerWeightPower = 1f;

        [Tooltip("Multiplier on the owning hex's contribution at each corner. >1 makes each hex interior look more like its own texture; neighbors only show near the edge.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float centerHexBias = 1f;

        [Tooltip("Pulls corner UVs inward from the [0,1] tile edges, avoiding tile-edge pixel artifacts that can manifest as seams.")]
        [Range(0f, 0.15f)]
        [SerializeField] private float uvInset = 0f;

        [Tooltip("Seed offset for the variant hash. Change to reshuffle which variant lands on which hex.")]
        [SerializeField] private int variantSeedOffset = 0;

        [Header("Debug")]
        [Tooltip("When true, builder changes automatically trigger a rebuild via the cached map.")]
        [SerializeField] private bool autoRebuildOnValidate = false;

        [Tooltip("Log chunk-build timing and terrain-array binding to the console. Warnings/errors about broken setup always log.")]
        [SerializeField] private bool debugLog = false;

        private readonly Dictionary<(int cx, int cy), HexChunk> _chunks = new();
        private int _chunksX;
        private int _chunksY;

        // Cached so Rebuild() can re-issue the build without the driver re-invoking it.
        private HexMap _lastMap;
        private HexGridSystem _lastGrid;

        #region Public API

        /// <summary>Current terrain material (for external assignment of Texture2DArray).</summary>
        public Material TerrainMaterial => terrainMaterial;

        /// <summary>
        /// Builds all chunks for the given HexMap. Clears any existing chunks first
        /// and caches map+grid for subsequent Rebuild() calls.
        /// </summary>
        public void BuildAllChunks(HexMap map, HexGridSystem grid)
        {
            try
            {
                Clear();

                if (map == null || !map.IsInitialized)
                {
                    Debug.LogWarning($"[{CLASS_NAME}] Cannot build chunks: map is null or not initialized.");
                    return;
                }

                if (terrainMaterial == null)
                {
                    Debug.LogError($"[{CLASS_NAME}] TerrainMaterial not assigned.");
                    return;
                }

                _lastMap = map;
                _lastGrid = grid;

                BindTerrainArray();
                MirrorSeedToSelector();
                ApplyShaderTuning();

                int mapWidth = map.MapSize.x;
                int mapHeight = map.MapSize.y;

                _chunksX = Mathf.CeilToInt(mapWidth / (float)HexChunkMeshBuilder.ChunkSize);
                _chunksY = Mathf.CeilToInt(mapHeight / (float)HexChunkMeshBuilder.ChunkSize);

                var settings = CurrentBuildSettings();
                var sw = System.Diagnostics.Stopwatch.StartNew();

                for (int cy = 0; cy < _chunksY; cy++)
                {
                    for (int cx = 0; cx < _chunksX; cx++)
                    {
                        var mesh = HexChunkMeshBuilder.Build(cx, cy, map, grid, settings);
                        if (mesh == null) continue; // empty region (off-map padding) — skip the GameObject too

                        var chunk = new HexChunk(cx, cy, transform, terrainMaterial)
                        {
                            Mesh = mesh,
                        };
                        chunk.Filter.sharedMesh = mesh;
                        _chunks[(cx, cy)] = chunk;
                    }
                }

                sw.Stop();
                if (debugLog)
                    Debug.Log($"[{CLASS_NAME}] Built {_chunksX}x{_chunksY} chunk grid " +
                              $"({_chunks.Count} chunks) in {sw.ElapsedMilliseconds}ms.");
            }
            catch (System.Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(BuildAllChunks), ex);
            }
        }

        /// <summary>Rebuilds a single chunk by grid coordinates. No-op if the chunk doesn't exist.</summary>
        public void RebuildChunk(int cx, int cy, HexMap map, HexGridSystem grid)
        {
            if (!_chunks.TryGetValue((cx, cy), out var chunk)) return;

            MirrorSeedToSelector();
            ApplyShaderTuning();
            if (chunk.Mesh != null)
            {
                if (Application.isPlaying) Object.Destroy(chunk.Mesh);
                else Object.DestroyImmediate(chunk.Mesh);
            }

            var mesh = HexChunkMeshBuilder.Build(cx, cy, map, grid, CurrentBuildSettings());
            chunk.Mesh = mesh;
            chunk.Filter.sharedMesh = mesh;
        }

        /// <summary>Rebuilds all chunks using the last map+grid passed to BuildAllChunks.</summary>
        [ContextMenu("Rebuild All Chunks")]
        public void Rebuild()
        {
            if (_lastMap == null || _lastGrid == null)
            {
                Debug.LogWarning($"[{CLASS_NAME}] Rebuild requested but no cached map/grid. Call BuildAllChunks first.");
                return;
            }
            BuildAllChunks(_lastMap, _lastGrid);
        }

        /// <summary>
        /// Sets the active terrain set from the scenario's MapTheme enum and rebinds
        /// the array. Called by BattleManager during scenario load.
        /// </summary>
        public void SetActiveTerrainSet(MapTheme theme)
        {
            terrainSet = theme;
            BindTerrainArray();
        }

        /// <summary>
        /// Loads TerrainArray_&lt;terrainSet&gt;.asset from Resources/Chunked and binds it
        /// to the terrain material's _TerrainArray slot. Call after rebaking the array —
        /// the bake deletes and recreates the asset, so any pre-authored material reference
        /// goes stale.
        /// </summary>
        [ContextMenu("Rebind Terrain Array")]
        public void BindTerrainArray()
        {
            if (terrainMaterial == null)
            {
                Debug.LogWarning($"[{CLASS_NAME}] Cannot bind array: terrainMaterial is null.");
                return;
            }

            string path = $"Chunked/TerrainArray_{terrainSet}";
            var array = Resources.Load<Texture2DArray>(path);
            if (array == null)
            {
                Debug.LogError($"[{CLASS_NAME}] Resources.Load failed: {path}. " +
                               $"Did TextureArrayBuilder run? Expected at Assets/Resources/{path}.asset");
                return;
            }

            // Slice-count sanity check. A mismatch usually means TextureArrayBuilder ran
            // against a stale TerrainType enum or a VariantsPerTerrain change wasn't propagated.
            int terrainCount = System.Enum.GetValues(typeof(TerrainType)).Length;
            int expectedDepth = terrainCount * HexChunkVariantSelector.VariantsPerTerrain;
            if (array.depth != expectedDepth)
            {
                Debug.LogWarning(
                    $"[{CLASS_NAME}] TerrainArray_{terrainSet} has {array.depth} slices; " +
                    $"expected {expectedDepth} ({terrainCount} terrains x {HexChunkVariantSelector.VariantsPerTerrain} variants). " +
                    $"Rebake the array if TerrainType or VariantsPerTerrain changed.");
            }

            terrainMaterial.SetTexture("_TerrainArray", array);
            if (debugLog)
                Debug.Log($"[{CLASS_NAME}] Bound TerrainArray_{terrainSet} ({array.depth} slices, {array.width}x{array.height}).");
        }

        /// <summary>Pushes shader-side tuning knobs to the material. No rebuild required.</summary>
        [ContextMenu("Apply Shader Tuning")]
        public void ApplyShaderTuning()
        {
            if (terrainMaterial == null) return;
            terrainMaterial.SetFloat("_BlendNoiseStrength", blendNoiseStrength);
            terrainMaterial.SetFloat("_NoiseScale", noiseScale);
            if (disableNoise) terrainMaterial.EnableKeyword("_HEXBLEND_NOISE_OFF");
            else terrainMaterial.DisableKeyword("_HEXBLEND_NOISE_OFF");
        }

        /// <summary>Captures inspector tuning values into a HexChunkBuildSettings struct for one build pass.</summary>
        public HexChunkBuildSettings CurrentBuildSettings() =>
            new(cornerWeightPower, centerHexBias, uvInset, variantSeedOffset);

        /// <summary>
        /// Mirrors variantSeedOffset to HexChunkVariantSelector.SeedOffset so editor
        /// tools and gizmos that call the parameter-less GetVariant/GetSlot see the
        /// same shuffle as the build pipeline.
        /// </summary>
        private void MirrorSeedToSelector() => HexChunkVariantSelector.SeedOffset = variantSeedOffset;

        /// <summary>Resets all tuning knobs to their defaults and re-applies live state.</summary>
        [ContextMenu("Reset Tuning to Defaults")]
        public void ResetTuningToDefaults()
        {
            blendNoiseStrength = 0.2f;
            noiseScale = 2.0f;
            disableNoise = false;
            cornerWeightPower = 1f;
            centerHexBias = 1f;
            uvInset = 0f;
            variantSeedOffset = 0;
            ApplyShaderTuning();
            MirrorSeedToSelector();
        }

        /// <summary>Destroys all chunk GameObjects and meshes.</summary>
        public void Clear()
        {
            foreach (var chunk in _chunks.Values)
                chunk.Destroy();
            _chunks.Clear();
            _chunksX = 0;
            _chunksY = 0;
        }

        #endregion // Public API

        #region Unity Callbacks

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Live-push shader params so scrubbing inspector sliders updates the view immediately.
            if (terrainMaterial != null)
                ApplyShaderTuning();

            if (autoRebuildOnValidate && Application.isPlaying && _lastMap != null)
            {
                MirrorSeedToSelector();
                Rebuild();
            }
        }
#endif

        protected override void OnDestroy()
        {
            Clear();
            base.OnDestroy();
        }

        #endregion // Unity Callbacks
    }
}
