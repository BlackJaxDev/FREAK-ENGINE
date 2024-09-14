#version 450
layout(location = 0) out float OutIntensity;
layout(location = 0) in vec3 FragPos;
uniform sampler2D Texture0;

void main()
{
    vec2 uv = FragPos.xy;
    if (uv.x > 1.0f || uv.y > 1.0f)
        discard;
    vec2 texelSize = 1.0f / vec2(textureSize(Texture0, 0));
    float result = 0.0f;
    for (int x = -2; x < 2; ++x) 
    {
        for (int y = -2; y < 2; ++y) 
        {
            vec2 offset = vec2(float(x), float(y)) * texelSize;
            result += texture(Texture0, uv + offset).r;
        }
    }
    OutIntensity = result / 16.0f;
}