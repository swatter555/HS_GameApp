// -----------------------------------------------------------------------------
// HexTerrainBlend.shader
// Phase 1 POC shader for the chunk-based hex terrain renderer.
// - URP unlit, handwritten HLSL (no Shader Graph).
// - Accepts per-vertex (terrainIndices float4, weights float4) attributes.
// - Samples a Texture2DArray up to 4 times per fragment, blended by weights.
// - Optional world-space noise perturbation of weights, disabled by shader keyword.
// -----------------------------------------------------------------------------
Shader "HammerAndSickle/HexTerrainBlend"
{
    Properties
    {
        [NoScaleOffset] _TerrainArray      ("Terrain Array (2DArray)", 2DArray) = "white" {}
        [NoScaleOffset] _NoiseTex          ("Noise (R)",                2D)      = "gray"  {}
        _BlendNoiseStrength                ("Blend Noise Strength",     Range(0,1)) = 0.2
        _NoiseScale                        ("Noise Scale (world units)", Float)   = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"       = "Opaque"
            "RenderPipeline"   = "UniversalPipeline"
            "Queue"            = "Geometry"
            "IgnoreProjector"  = "True"
        }
        LOD 100

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off        // test meshes may be authored with either winding
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.5

            // Toggle noise perturbation off for extreme zoom-out (set globally from C#).
            #pragma shader_feature_local _HEXBLEND_NOISE_OFF

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Includes/HexNoise.hlsl"

            TEXTURE2D_ARRAY(_TerrainArray);
            SAMPLER(sampler_TerrainArray);

            CBUFFER_START(UnityPerMaterial)
                float _BlendNoiseStrength;
                float _NoiseScale;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS  : POSITION;
                float2 uv          : TEXCOORD0;
                float4 terrainIdx  : TEXCOORD1;  // up to 4 Texture2DArray slice indices
                float4 weights     : TEXCOORD2;  // matching blend weights (sum ~= 1)
            };

            struct Varyings
            {
                float4 positionHCS                 : SV_POSITION;
                float3 positionWS                  : TEXCOORD0;
                float2 uv                          : TEXCOORD1;
                // Indices are flat across each triangle — all 3 verts MUST use the same
                // index set, otherwise the provoking vertex's indices silently win.
                nointerpolation float4 terrainIdx  : TEXCOORD2;
                float4 weights                     : TEXCOORD3;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS);
                OUT.positionHCS = pos.positionCS;
                OUT.positionWS  = pos.positionWS;
                OUT.uv          = IN.uv;
                OUT.terrainIdx  = IN.terrainIdx;
                OUT.weights     = IN.weights;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float4 w = IN.weights;

                #ifndef _HEXBLEND_NOISE_OFF
                    // Two decorrelated noise samples perturb the blend boundary.
                    // Multiplicative: channels at zero stay zero, so noise never
                    // invents weight on inactive terrain slots.
                    float n1 = SampleHexNoise(IN.positionWS.xy,                          _NoiseScale);
                    float n2 = SampleHexNoise(IN.positionWS.xy + float2(17.3, 29.1),     _NoiseScale);
                    float p1 = (n1 * 2.0 - 1.0) * _BlendNoiseStrength;
                    float p2 = (n2 * 2.0 - 1.0) * _BlendNoiseStrength;
                    w.x *= (1.0 + p1);
                    w.y *= (1.0 - p1);
                    w.z *= (1.0 + p2);
                    w.w *= (1.0 - p2);
                    w = max(w, 0.0);
                #endif

                // Renormalize so channels sum to 1. Guard against a fully-zero input.
                float wsum = w.x + w.y + w.z + w.w;
                w /= max(wsum, 1e-5);

                // Compute texture-array mip gradients from world position, not UV.
                // UV is discontinuous across hex boundaries (A's triangle has uv~1,
                // B's triangle has uv~0 at the same world point) so screen-space
                // ddx/ddy of UV explodes in pixel quads that straddle a hex edge,
                // forcing the GPU to pick the coarsest mip and producing a blurry
                // line along hex boundaries. World position is continuous, so its
                // derivatives are well-behaved everywhere.
                //
                // Hex bounding box in world units (matches HexChunkMeshBuilder):
                //   width  = 2 * HALF_HEX_WIDTH  = 2.56
                //   height = 2 * dy_far          ≈ 2.77334 (with HW=1.28, VS=1.92)
                const float2 invHexBBox = float2(1.0 / 2.56, 1.0 / 2.77333593);
                float2 uvDdx = ddx(IN.positionWS.xy) * invHexBBox;
                float2 uvDdy = ddy(IN.positionWS.xy) * invHexBBox;

                half4 c0 = SAMPLE_TEXTURE2D_ARRAY_GRAD(_TerrainArray, sampler_TerrainArray, IN.uv, IN.terrainIdx.x, uvDdx, uvDdy);
                half4 c1 = SAMPLE_TEXTURE2D_ARRAY_GRAD(_TerrainArray, sampler_TerrainArray, IN.uv, IN.terrainIdx.y, uvDdx, uvDdy);
                half4 c2 = SAMPLE_TEXTURE2D_ARRAY_GRAD(_TerrainArray, sampler_TerrainArray, IN.uv, IN.terrainIdx.z, uvDdx, uvDdy);
                half4 c3 = SAMPLE_TEXTURE2D_ARRAY_GRAD(_TerrainArray, sampler_TerrainArray, IN.uv, IN.terrainIdx.w, uvDdx, uvDdy);

                half4 final = c0 * w.x + c1 * w.y + c2 * w.z + c3 * w.w;
                final.a = 1.0;
                return final;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
