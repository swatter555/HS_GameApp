using UnityEngine;

namespace HammerAndSickle.Renderers.Chunked
{
    /// <summary>
    /// Data container for one 16x16 hex chunk. Owns the GameObject, MeshFilter,
    /// MeshRenderer, and generated Mesh. Constructed and destroyed by HexChunkRenderer.
    /// </summary>
    public class HexChunk
    {
        public int ChunkX { get; }
        public int ChunkY { get; }
        public GameObject Go { get; }
        public MeshFilter Filter { get; }
        public MeshRenderer Renderer { get; }
        public Mesh Mesh { get; set; }

        public HexChunk(int cx, int cy, Transform parent, Material material)
        {
            ChunkX = cx;
            ChunkY = cy;

            Go = new GameObject($"HexChunk_{cx}_{cy}");
            Go.transform.SetParent(parent, worldPositionStays: false);

            Filter = Go.AddComponent<MeshFilter>();
            Renderer = Go.AddComponent<MeshRenderer>();
            Renderer.sharedMaterial = material;
            Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Renderer.receiveShadows = false;
        }

        /// <summary>
        /// Destroys the mesh and GameObject. Picks Destroy vs DestroyImmediate based
        /// on play mode — DestroyImmediate is unsafe at runtime (deletes mid-frame,
        /// breaks coroutines/listeners) but is required outside play mode.
        /// </summary>
        public void Destroy()
        {
            if (Application.isPlaying)
            {
                if (Mesh != null) Object.Destroy(Mesh);
                if (Go != null) Object.Destroy(Go);
            }
            else
            {
                if (Mesh != null) Object.DestroyImmediate(Mesh);
                if (Go != null) Object.DestroyImmediate(Go);
            }
        }
    }
}
