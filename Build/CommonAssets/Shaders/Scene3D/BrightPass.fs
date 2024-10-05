#version 450

layout(location = 0) out vec3 BloomColor;
layout(location = 0) in vec3 FragPos;

uniform sampler2D HDRSceneTex; //HDR color from Deferred & Forward passes
uniform float BloomIntensity = 1.0f;
uniform float BloomThreshold = 1.0f;

void main()
{
    vec2 uv = FragPos.xy;
    if (uv.x > 1.0f || uv.y > 1.0f)
        discard;
    //Normalize uv from [-1, 1] to [0, 1]
    uv = uv * 0.5f + 0.5f;

    vec3 hdr = texture(HDRSceneTex, uv).rgb;

    vec3 luminance = vec3(0.299f, 0.587f, 0.114f); //vec3(0.2126f, 0.7152f, 0.0722f)
    float brightness = dot(hdr * BloomThreshold, luminance);
    float multiplier = clamp(floor(brightness), 0.0f, 1.0f);
    
    BloomColor = hdr * multiplier * BloomIntensity;
}
