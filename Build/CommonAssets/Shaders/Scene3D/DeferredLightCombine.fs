#version 450

const float PI = 3.14159265359f;
const float InvPI = 0.31831f;
const float MAX_REFLECTION_LOD = 4.0f;

layout(location = 0) out vec3 OutColor; //HDR Scene Color
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

uniform vec3 CameraPosition;
uniform mat4 WorldToCameraSpaceMatrix;
uniform mat4 CameraToWorldSpaceMatrix;
uniform mat4 ProjMatrix;
uniform mat4 InvProjMatrix;

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
	vec4 viewSpacePosition = InvProjMatrix * clipSpacePosition;
	viewSpacePosition /= viewSpacePosition.w;
	return (CameraToWorldSpaceMatrix * viewSpacePosition).xyz;
}
void main()
{
	vec2 uv = FragPos.xy;
	vec3 albedoColor = texture(Texture0, uv).rgb;
	vec3 normal = texture(Texture1, uv).rgb;
	vec3 rms = texture(Texture2, uv).rgb;
	float ao = texture(Texture3, uv).r;
	float depth = texture(Texture4, uv).r;
  vec3 Lo = texture(Texture5, uv).rgb;
  vec3 irradianceColor = texture(Irradiance, normal).rgb;
	vec3 fragPosWS = WorldPosFromDepth(depth, uv);
	//float fogDensity = noise3(fragPosWS);

	float roughness = rms.x;
  float metallic = rms.y;
	float specularIntensity = rms.z;

  vec3 V = normalize(CameraPosition - fragPosWS);
  float NoV = max(dot(normal, V), 0.0f);
  vec3 F0 = mix(vec3(0.04f), albedoColor, metallic);
  vec2 brdf = texture(BRDF, vec2(NoV, roughness)).rg;

  //Calculate specular and diffuse components
  //Preserve energy by making sure they add up to 1
  vec3 kS = SpecF_SchlickRoughnessApprox(NoV, F0, roughness) * specularIntensity;
  vec3 kD = (1.0f - kS) * (1.0f - metallic);
  vec3 R = reflect(-V, normal);

	//TODO: fix reflection vector, blend environment cubemaps via influence radius

  vec3 diffuse = irradianceColor * albedoColor;
  vec3 prefilteredColor = textureLod(Prefilter, R, roughness * MAX_REFLECTION_LOD).rgb;
  vec3 specular = prefilteredColor * (kS * brdf.x + brdf.y);

  OutColor = (kD * diffuse + specular) * ao +	Lo;
}
