#version 450

layout(location = 0) out vec3 BloomColor;
layout(location = 0) in vec3 FragPos;

uniform sampler2D Texture0;

uniform float Ping;
uniform int LOD;
uniform float Radius = 1.0f;

//We can use 5 texture lookups instead of 9 by using linear filtering and averaging the offsets and weights
uniform float Offset[3] = float[](0.0f, 1.3846153846f, 3.2307692308f);
uniform float Weight[3] = float[](0.2270270270f, 0.3162162162f, 0.0702702703f);

void main()
{
      vec2 uv = FragPos.xy;
      if (uv.x > 1.0f || uv.y > 1.0f)
         discard;
      //Normalize uv from [-1, 1] to [0, 1]
      uv = uv * 0.5f + 0.5f;

      vec2 scale = vec2(Ping, 1.0f - Ping);
      vec2 texelSize = 1.0f / textureSize(Texture0, LOD) * scale;
      float lodf = float(LOD);
      vec3 result = textureLod(Texture0, uv, lodf).rgb * Weight[0];

      for (int i = 1; i <= 2; ++i)
      {
         float weight = Weight[i];
         float offset = Offset[i] * Radius;
         vec2 uvOffset = texelSize * offset;

         result += textureLod(Texture0, uv + uvOffset, lodf).rgb * weight;
         result += textureLod(Texture0, uv - uvOffset, lodf).rgb * weight;
      }

      BloomColor = result;
}
