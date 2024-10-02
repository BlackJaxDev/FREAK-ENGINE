#version 450

layout (location = 0) out vec4 AlbedoOpacity;
layout (location = 1) out vec3 Normal;
layout (location = 2) out vec4 RMSI;

in vec3 FragNorm;
in vec3 FragBinorm;
in vec3 FragTan;
in vec2 FragUV0;

uniform sampler2D Texture0;
uniform sampler2D Texture1;
uniform float Opacity = 1.0f;
uniform float Specular = 1.0f;
uniform float Roughness = 0.0f;
uniform float Metallic = 0.0f;
uniform float IndexOfRefraction = 1.0f;

void main()
{
    Normal = normalize(FragNorm);
    AlbedoOpacity = vec4(texture(Texture0, FragUV0).rgb, Opacity);
    RMSI = vec4(Roughness, Metallic, Specular, IndexOfRefraction);
}
