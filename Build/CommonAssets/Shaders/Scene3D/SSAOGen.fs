#version 450

const float PI = 3.14159265359f;
const float InvPI = 0.31831f;

layout(location = 0) out float OutIntensity;
layout(location = 0) in vec3 FragPos;

uniform sampler2D Texture0; //Normal
uniform sampler2D Texture1; //SSAO Noise
uniform sampler2D Texture2; //Depth

uniform vec3 Samples[64];
uniform float Radius = 0.75f;
uniform float Power = 4.0f;
uniform vec2 NoiseScale;

uniform mat4 WorldToCameraSpaceMatrix;
uniform mat4 CameraToWorldSpaceMatrix;
uniform mat4 ProjMatrix;
uniform mat4 InvProjMatrix;

vec3 ViewPosFromDepth(float depth, vec2 uv)
{
    vec4 clipSpacePosition = vec4(vec3(uv, depth) * 2.0f - 1.0f, 1.0f);
    vec4 viewSpacePosition = InvProjMatrix * clipSpacePosition;
    return viewSpacePosition.xyz / viewSpacePosition.w;
}

void main()
{
    vec2 uv = FragPos.xy;
    if (uv.x > 1.0f || uv.y > 1.0f)
        discard;

    vec3 Normal = texture(Texture0, uv).rgb;
    float Depth = texture(Texture2, uv).r;

    vec3 FragPosVS = ViewPosFromDepth(Depth, uv);
    //vec3 FragPosWS = (CameraToWorldSpaceMatrix * vec4(FragPosVS, 1.0f)).xyz;

    vec3 randomVec = vec3(texture(Texture1, uv * NoiseScale).rg * 2.0f - 1.0f, 0.0f);
    vec3 viewNormal = normalize(vec3(WorldToCameraSpaceMatrix * vec4(Normal, 0.0f)));
    vec3 viewTangent = normalize(randomVec - viewNormal * dot(randomVec, viewNormal));
    vec3 viewBitangent = cross(viewNormal, viewTangent);
    mat3 TBN = mat3(viewTangent, viewBitangent, viewNormal);

    int kernelSize = 64;
    float bias = 0.025f;

    vec3 noiseSample;
    vec4 offset;
    float sampleDepth;
    float occlusion = 0.0f;

    for (int i = 0; i < kernelSize; ++i)
    {
        noiseSample = TBN * Samples[i];
        noiseSample = FragPosVS + noiseSample * Radius;

        offset = ProjMatrix * vec4(noiseSample, 1.0f);
        offset.xyz /= offset.w;
        offset.xyz = offset.xyz * 0.5f + 0.5f;

        sampleDepth = ViewPosFromDepth(texture(Texture2, offset.xy).r, offset.xy).z;

        occlusion += (sampleDepth >= noiseSample.z + bias ? smoothstep(0.0f, 1.0f, Radius / abs(FragPosVS.z - sampleDepth)) : 0.0f);
    }

    OutIntensity = pow(1.0f - (occlusion / kernelSize), Power);
}
