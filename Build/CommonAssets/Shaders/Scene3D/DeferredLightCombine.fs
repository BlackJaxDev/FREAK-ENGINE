#version 450

const float PI = 3.14159265359f;
const float InvPI = 0.31831f;
const float MAX_REFLECTION_LOD = 4.0f;

layout(location = 0) out vec3 OutLo; //Diffuse Light Color, to start off the HDR Scene Texture
layout(location = 0) in vec3 FragPos;

layout(binding = 0) uniform sampler2D Texture0; //AlbedoOpacity
layout(binding = 1) uniform sampler2D Texture1; //Normal
layout(binding = 2) uniform sampler2D Texture2; //PBR: Roughness, Metallic, Specular, Index of refraction
layout(binding = 3) uniform sampler2D Texture3; //SSAO Intensity
layout(binding = 4) uniform sampler2D Texture4; //Depth
layout(binding = 5) uniform sampler2D Texture5; //Diffuse Light Color

layout(binding = 6) uniform sampler2D BRDF;

layout(binding = 7) uniform samplerCube Irradiance;
layout(binding = 8) uniform samplerCube Prefilter;

layout(binding = 9) uniform samplerCube Irradiance1;
layout(binding = 10) uniform samplerCube Prefilter1;

layout(binding = 11) uniform samplerCube Irradiance2;
layout(binding = 12) uniform samplerCube Prefilter2;

layout(binding = 13) uniform samplerCube Irradiance3;
layout(binding = 14) uniform samplerCube Prefilter3;

uniform vec3 CameraPosition;
uniform mat4 InverseViewMatrix;
uniform mat4 ProjMatrix;

vec3 SpecF_SchlickRoughness(in float VoH, in vec3 F0, in float roughness)
{
	float pow = pow(1.0f - VoH, 5.0f);
	return F0 + (max(vec3(1.0f - roughness), F0) - F0) * pow;
}
vec3 SpecF_SchlickRoughnessApprox(in float VoH, in vec3 F0, in float roughness)
{
	//Spherical Gaussian Approximation
	float pow = exp2((-5.55473f * VoH - 6.98316f) * VoH);
	return F0 + (max(vec3(1.0f - roughness), F0) - F0) * pow;
}
vec3 WorldPosFromDepth(in float depth, in vec2 uv)
{
	vec4 clipSpacePosition = vec4(vec3(uv, depth) * 2.0f - 1.0f, 1.0f);
	vec4 viewSpacePosition = inverse(ProjMatrix) * clipSpacePosition;
	viewSpacePosition /= viewSpacePosition.w;
	return (InverseViewMatrix * viewSpacePosition).xyz;
}
void main()
{
	vec2 uv = FragPos.xy;
	if (uv.x > 1.0f || uv.y > 1.0f)
		discard;
	//Normalize uv from [-1, 1] to [0, 1]
	uv = uv * 0.5f + 0.5f;

	vec3 albedoColor = texture(Texture0, uv).rgb;
	vec3 normal = texture(Texture1, uv).rgb;
	vec3 rms = texture(Texture2, uv).rgb;
	float ao = texture(Texture3, uv).r;
	float depth = texture(Texture4, uv).r;
	vec3 InLo = texture(Texture5, uv).rgb;
	vec3 irradianceColor = texture(Irradiance, normal).rgb;
	vec3 fragPosWS = WorldPosFromDepth(depth, uv);
	//float fogDensity = noise3(fragPosWS);

	float roughness = rms.x;
  	float metallic = rms.y;
	float specularIntensity = rms.z;

	vec3 V = normalize(CameraPosition - fragPosWS);
	float NoV = max(dot(normal, V), 0.0f);
	vec3 F0 = mix(vec3(0.04f), albedoColor, metallic);
	vec2 brdfValue = texture(BRDF, vec2(NoV, roughness)).rg;

	//Calculate specular and diffuse components
	//Preserve energy by making sure they add up to 1
	vec3 kS = SpecF_SchlickRoughnessApprox(NoV, F0, roughness) * specularIntensity;
	vec3 kD = (1.0f - kS) * (1.0f - metallic);
	vec3 R = reflect(-V, normal);

	//TODO: fix reflection vector, blend environment cubemaps via influence radius

	vec3 diffuse = irradianceColor * albedoColor;
	vec3 prefilteredColor = textureLod(Prefilter, R, roughness * MAX_REFLECTION_LOD).rgb;
	vec3 specular = prefilteredColor * (kS * brdfValue.x + brdfValue.y);

	OutLo = (kD * diffuse + specular) * ao + InLo * ao;
}
