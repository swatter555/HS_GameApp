#ifndef HAMMERANDSICKLE_HEXNOISE_INCLUDED
#define HAMMERANDSICKLE_HEXNOISE_INCLUDED

// -----------------------------------------------------------------------------
// HexNoise.hlsl
// World-space noise sample used to perturb hex-blend weights.
// Noise texture is expected to be a single-channel (R) tiling greyscale,
// imported as non-sRGB (Linear data). 256x256 recommended.
// -----------------------------------------------------------------------------

TEXTURE2D(_NoiseTex);
SAMPLER(sampler_NoiseTex);

// Returns a value in [0,1]. scaleWorldUnits controls the world-space tile size
// of one repeat of the noise texture.
float SampleHexNoise(float2 worldXY, float scaleWorldUnits)
{
    float2 uv = worldXY / max(scaleWorldUnits, 1e-3);
    return SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv).r;
}

#endif // HAMMERANDSICKLE_HEXNOISE_INCLUDED
