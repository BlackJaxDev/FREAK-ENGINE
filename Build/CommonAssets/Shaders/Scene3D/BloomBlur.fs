#version 450

layout(location = 0) out vec3 BloomColor;
layout(location = 0) in vec3 FragPos;

uniform sampler2D Texture0;

uniform float Ping;
uniform int LOD;

//We can use 5 texture lookups instead of 9 by using linear filtering and averaging the offsets and weights
uniform float Offset[3] = float[](0.0f, 1.3846153846f, 3.2307692308f);
uniform float Weight[3] = float[](0.2270270270f, 0.3162162162f, 0.0702702703f);
//uniform float Offset[5] = float[](0.0f, 1.0f, 2.0f, 3.0f, 4.0f);
//uniform float Weight[5] = float[] (0.2270270270, 0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162);

void main()
{
     vec2 uv = FragPos.xy;
     float weight;
     float offset;
     vec2 uvOffset;
     vec2 scale = vec2(Ping, 1.0f - Ping);
     vec2 texelSize = 1.0f / textureSize(Texture0, LOD) * scale;
     float lodf = float(LOD);
     vec3 result = textureLod(Texture0, uv, lodf).rgb * Weight[0];
     for (int i = 1; i <= 2; ++i)
     {
        weight = Weight[i];
        offset = Offset[i];
        uvOffset = texelSize * offset;

        result += textureLod(Texture0, uv + uvOffset, lodf).rgb * weight;
        result += textureLod(Texture0, uv - uvOffset, lodf).rgb * weight;
     }
     BloomColor = result;
}
