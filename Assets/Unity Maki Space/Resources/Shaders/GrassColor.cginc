#ifndef GRASS_COLOR_INCLUDE
#define GRASS_COLOR_INCLUDE

#include "Snoise.cginc"

#define GRASS_PLAINS_COORDS_START float2(41, 86) / 256
#define GRASS_PLAINS_COORDS_END float2(139, 47) / 256

float3 GrassColor(float3 worldPos, sampler2D grassColorMap)
{
    const float n = snoise(worldPos * 0.02) * 0.5 + 0.5;
    // * 0.5 because it turns really blue in the resource pack that we're using
    const float2 grassMapUv = lerp(GRASS_PLAINS_COORDS_START, GRASS_PLAINS_COORDS_END, n * 0.5);
    const float3 grassColor = tex2D(grassColorMap, grassMapUv);
    return grassColor;
}

#endif