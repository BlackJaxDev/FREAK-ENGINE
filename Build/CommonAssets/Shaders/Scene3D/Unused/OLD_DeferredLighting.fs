#version 450

#define MAX_DIR_LIGHTS 1
#define MAX_SPOT_LIGHTS 3
#define MAX_POINT_LIGHTS 1

const float PI = 3.14159265359f;
const float InvPI = 0.31831f;

layout(location = 0) out vec3 OutColor; //Final Deferred Pass Color. Used later by the Post Process fragment shader.
layout(location = 0) in vec3 FragPos;

layout(binding = 0) uniform sampler2D Texture0; //AlbedoOpacity
layout(binding = 1) uniform sampler2D Texture1; //Normal
layout(binding = 2) uniform sampler2D Texture2; //PBR: Roughness, Metallic, Specular, Index of refraction
layout(binding = 3) uniform sampler2D Texture3; //SSAO Intensity
layout(binding = 4) uniform sampler2D Texture4; //Depth
layout(binding = 5) uniform sampler2D Texture5; //BRDF LUT
layout(binding = 6) uniform samplerCube Texture6; //Irradiance Map
layout(binding = 7) uniform samplerCube Texture7; //Prefilter Map
layout(binding = 8)
uniform sampler2D DirShadowMaps[MAX_DIR_LIGHTS];
layout(binding = 8 + MAX_DIR_LIGHTS)
uniform sampler2D SpotShadowMaps[MAX_SPOT_LIGHTS];
layout(binding = 8 + MAX_DIR_LIGHTS + MAX_SPOT_LIGHTS)
uniform samplerCube PointShadowMaps[MAX_POINT_LIGHTS];

uniform vec3 CameraPosition;
uniform vec3 CameraForward;
uniform float CameraNearZ;
uniform float CameraFarZ;
uniform float ScreenWidth;
uniform float ScreenHeight;
uniform float ScreenOrigin;
uniform float ProjOrigin;
uniform float ProjRange;
uniform mat4 WorldToCameraSpaceMatrix;
uniform mat4 CameraToWorldSpaceMatrix;
uniform mat4 ProjMatrix;
uniform mat4 InvProjMatrix;

uniform vec3 GlobalAmbient;
uniform int DirLightCount;
uniform int SpotLightCount;
uniform int PointLightCount;

struct BaseLight
{
    vec3 Color;
    float DiffuseIntensity;
};
struct DirLight
{
    BaseLight Base;
    mat4 WorldToLightSpaceProjMatrix;

    vec3 Direction;
};
struct PointLight
{
    BaseLight Base;

    vec3 Position;
    float Radius;
    float Brightness;
};
struct SpotLight
{
    BaseLight Base;
    mat4 WorldToLightSpaceProjMatrix;

    vec3 Position;
    vec3 Direction;
    float Radius;
    float Brightness;
    float Exponent;

    float InnerCutoff;
    float OuterCutoff;
};

uniform float ShadowBase = 3.0f;
uniform float ShadowMult = 6.0f;
uniform float ShadowBiasMin = 0.00001f;
uniform float ShadowBiasMax = 0.004f;

uniform DirLight DirLights[MAX_DIR_LIGHTS];
uniform SpotLight SpotLights[MAX_SPOT_LIGHTS];
uniform PointLight PointLights[MAX_POINT_LIGHTS];

float GetShadowBias(in float NoL, in float base, in float power, in float minBias, in float maxBias)
{
    float mapped = pow(base, -NoL * power);
    return mix(minBias, maxBias, mapped);
}
//0 is fully in shadow, 1 is fully lit
float ReadShadowMap2D(in vec3 fragPosWS, in vec3 N, in float NoL, in sampler2D shadowMap, in mat4 lightMatrix)
{
	//Move the fragment position into light space
	vec4 fragPosLightSpace = lightMatrix * vec4(fragPosWS, 1.0f);
	vec3 fragCoord = fragPosLightSpace.xyz / fragPosLightSpace.w;
	fragCoord = fragCoord * 0.5f + 0.5f;

	//Create bias depending on angle of normal to the light
	float bias = GetShadowBias(NoL, ShadowBase, ShadowMult, ShadowBiasMin, ShadowBiasMax);

	//Hard shadow
	float depth = texture(shadowMap, fragCoord.xy).r;
	float shadow = (fragCoord.z - bias) > depth ? 0.0f : 1.0f;

	//PCF shadow
	//float shadow = 0.0;
	//vec2 texelSize = 1.0f / textureSize(shadowMap, 0);
	//for (int x = -1; x <= 1; ++x)
	//{
	//    for (int y = -1; y <= 1; ++y)
	//    {
	//        float pcfDepth = texture(shadowMap, fragCoord.xy + vec2(x, y) * texelSize).r;
	//        shadow += fragCoord.z - bias > pcfDepth ? 0.0f : 1.0f;
	//    }
	//}
	//shadow *= 0.111111111f; //divided by 9

	return shadow;
}
//0 is fully in shadow, 1 is fully lit
float ReadPointShadowMap(in int lightIndex, in float farPlaneDist, in vec3 fragToLightWS, in float lightDist, in float NoL)
{
    float bias = GetShadowBias(NoL, 10.0f, 2.5f, 0.15f, 5.0f);

    //Hard shadow
    float closestDepth = texture(PointShadowMaps[lightIndex], fragToLightWS).r * farPlaneDist;
    float shadow = lightDist - bias > closestDepth ? 0.0f : 1.0f;

    //PCF shadow
    //float shadow = 0.0;
    //vec2 texelSize = 1.0f / textureSize(map, 0);
    //for (int x = -1; x <= 1; ++x)
    //{
    //    for (int y = -1; y <= 1; ++y)
    //    {
    //           for (int z = -1; z <= 1; ++z)
    //        {
    //            float pcfDepth = texture(map, fragToLightWS + vec3(x, y, z) * texelSize).r * farPlaneDist;
    //            shadow += fragCoord.z - bias > pcfDepth ? 0.0f : 1.0f;
    //        }
    //    }
    //}
    //shadow *= 0.111111111f; //divided by 9

    return shadow;
}

float Attenuate(in float dist, in float radius)
{
    return pow(clamp(1.0f - pow(dist / radius, 4.0f), 0.0f, 1.0f), 2.0f) / (dist * dist + 1.0f);
}

//Trowbridge-Reitz GGX
float SpecD_TRGGX(in float NoH2, in float a2)
{
	float num    = a2;
	float denom  = (NoH2 * (a2 - 1.0f) + 1.0f);
	denom        = PI * denom * denom;

	return num / denom;
}
float SpecG_SchlickGGX(in float NoV, in float k)
{
	float num   = NoV;
	float denom = NoV * (1.0f - k) + k;

	return num / denom;
}
float SpecG_Smith(in float NoV, in float NoL, in float k)
{
	float ggx1 = SpecG_SchlickGGX(NoV, k);
	float ggx2 = SpecG_SchlickGGX(NoL, k);
	return ggx1 * ggx2;
}
vec3 SpecF_Schlick(in float VoH, in vec3 F0)
{
	float pow = pow(1.0f - VoH, 5.0f);
	return F0 + (1.0f - F0) * pow;
}
vec3 SpecF_SchlickApprox(in float VoH, in vec3 F0)
{
	//Spherical Gaussian Approximation
	float pow = exp2((-5.55473f * VoH - 6.98316f) * VoH);
	return F0 + (1.0f - F0) * pow;
}
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
vec3 CalcColor(
in BaseLight light,
in float NoL,
in float NoH,
in float NoV,
in float HoV,
in float lightAttenuation,
in vec3 albedo,
in vec3 rms,
in vec3 F0)
{
	float roughness = rms.x;
	float metallic = rms.y;
	float specular = rms.z;

	float a = roughness * roughness;
	float k = roughness + 1.0f;
	k = k * k * 0.125f; //divide by 8

	float D = SpecD_TRGGX(NoH * NoH, a * a);
	float G = SpecG_Smith(NoV, NoL, k);
	vec3  F = SpecF_Schlick(HoV, F0);

	//Cook-Torrance Specular
	float denom = 4.0f * NoV * NoL + 0.0001f;
	vec3 spec =  specular * D * G * F / denom;

	vec3 kS = F;
	vec3 kD = 1.0f - kS;
	kD *= 1.0f - metallic;

	vec3 radiance = lightAttenuation * light.Color * light.DiffuseIntensity;
	return (kD * albedo / PI + spec) * radiance * NoL;
}
vec3 CalcDirLight(
in int lightIndex,
in vec3 N,
in vec3 V,
in vec3 fragPosWS,
in vec3 albedo,
in vec3 rms,
in vec3 F0)
{
	DirLight light = DirLights[lightIndex];

	vec3 L = -light.Direction;
	vec3 H = normalize(V + L);
	float NoL = max(dot(N, L), 0.0f);
	float NoH = max(dot(N, H), 0.0f);
	float NoV = max(dot(N, V), 0.0f);
	float HoV = max(dot(H, V), 0.0f);

  vec3 color = CalcColor(
		light.Base,
		NoL, NoH, NoV, HoV,
		1.0f, albedo, rms, F0);

	float shadow = ReadShadowMap2D(
		fragPosWS, N, NoL,
		DirShadowMaps[lightIndex],
		light.WorldToLightSpaceProjMatrix);

	return color * shadow;
}
vec3 CalcPointLight(
in int lightIndex,
in vec3 N,
in vec3 V,
in vec3 fragPosWS,
in vec3 albedo,
in vec3 rms,
in vec3 F0)
{
	PointLight light = PointLights[lightIndex];

	vec3 L = light.Position - fragPosWS;
  float realLightDist = length(L);
	float lightDist = realLightDist / light.Brightness;
	float radius = light.Radius / light.Brightness;
	float attn = Attenuate(lightDist, radius);
	L = normalize(L);
	vec3 H = normalize(V + L);

	float NoL = max(dot(N, L), 0.0f);
	float NoH = max(dot(N, H), 0.0f);
	float NoV = max(dot(N, V), 0.0f);
	float HoV = max(dot(H, V), 0.0f);

	vec3 color = CalcColor(
		light.Base,
		NoL, NoH, NoV, HoV,
		attn, albedo, rms, F0);

	float shadow = ReadPointShadowMap(
		lightIndex, radius, -L, lightDist, NoL);

	return color * shadow;
}
vec3 CalcSpotLight(
in int lightIndex,
in vec3 N,
in vec3 V,
in vec3 fragPosWS,
in vec3 albedo,
in vec3 rms,
in vec3 F0)
{
	SpotLight light = SpotLights[lightIndex];

	vec3 L = light.Position - fragPosWS;
	float lightDist = length(L);
	L = normalize(L);

	//OuterCutoff is the smaller value, despite being a larger angle
	//cos(90) == 0
	//cos(0) == 1

	float cosine = dot(L, -normalize(light.Direction));

	if (cosine <= light.OuterCutoff)
		return vec3(0.0f);

	float clampedCosine = clamp(cosine, light.OuterCutoff, light.InnerCutoff);

	//Subtract smaller value and divide by range to normalize value
	float time = (clampedCosine - light.OuterCutoff) / (light.InnerCutoff - light.OuterCutoff);

	//Make transition smooth rather than linear
	float spotAmt = smoothstep(0.0f, 1.0f, time);
	float distAttn = Attenuate(lightDist / light.Brightness, light.Radius / light.Brightness);
	float attn = spotAmt * distAttn * pow(clampedCosine, light.Exponent);

	vec3 H = normalize(V + L);
	float NoL = max(dot(N, L), 0.0f);
	float NoH = max(dot(N, H), 0.0f);
	float NoV = max(dot(N, V), 0.0f);
	float HoV = max(dot(H, V), 0.0f);

	vec3 color = CalcColor(
		light.Base,
		NoL, NoH, NoV, HoV,
		attn, albedo, rms, F0);

	float shadow = ReadShadowMap2D(
		fragPosWS, N, NoL,
		SpotShadowMaps[lightIndex],
		light.WorldToLightSpaceProjMatrix);

	return color * shadow;
}
vec3 CalcTotalLight(
in vec3 fragPosWS,
in vec3 normal,
in vec3 albedo,
in vec3 rms,
in float ao)
{
	const float MAX_REFLECTION_LOD = 4.0f;

	float roughness = rms.x;
	float metallic = rms.y;
	vec3 V = normalize(CameraPosition - fragPosWS);
	float NoV = max(dot(normal, V), 0.0f);
	vec3 F0 = mix(vec3(0.04f), albedo, metallic);
	vec3 Lo = vec3(0.0f);

	int i = 0;
	while (i < DirLightCount) Lo += CalcDirLight(
		i++, normal, V, fragPosWS, albedo, rms, F0);

	i = 0;
	while (i < PointLightCount) Lo += CalcPointLight(
		i++, normal, V, fragPosWS, albedo, rms, F0);

	i = 0;
	while (i < SpotLightCount) Lo += CalcSpotLight(
		i++, normal, V, fragPosWS, albedo, rms, F0);

	//Calculate specular and diffuse components
	//Preserve energy by making sure they add up to 1
	vec3 kS = SpecF_SchlickRoughnessApprox(NoV, F0, roughness);
	vec3 kD = (1.0f - kS) * (1.0f - metallic);
	vec3 R = reflect(-V, normal);

	vec3 irradiance = texture(Texture6, normal).rgb;
	vec3 diffuse = irradiance * albedo;
	vec3 prefilteredColor = textureLod(Texture7, R, roughness * MAX_REFLECTION_LOD).rgb;
	vec2 brdf = texture(Texture5, vec2(NoV, roughness)).rg;
	vec3 specular = prefilteredColor * (kS * brdf.x + brdf.y);

	return (kD * diffuse + specular) * ao + Lo;
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
	if (uv.x > 1.0f || uv.y > 1.0f)
		discard;

	//Retrieve shading information from GBuffer textures
	vec3 albedo = texture(Texture0, uv).rgb;
	vec3 normal = texture(Texture1, uv).rgb;
	vec3 rms = texture(Texture2, uv).rgb;
	float ao = texture(Texture3, uv).r;
	float depth = texture(Texture4, uv).r;

	//Resolve world fragment position using depth and screen UV
	vec3 fragPosWS = WorldPosFromDepth(depth, uv);

	OutColor = CalcTotalLight(fragPosWS, normal, albedo, rms, ao) * ao;
}
