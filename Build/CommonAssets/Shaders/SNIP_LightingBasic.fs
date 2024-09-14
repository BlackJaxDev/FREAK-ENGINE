struct BaseLight
{
    vec3 Color;
    float DiffuseIntensity;
    float AmbientIntensity;
    mat4 WorldToLightSpaceProjMatrix;
    sampler2D ShadowMap;
};
struct DirLight
{
    BaseLight Base;
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
    PointLight Base;
    vec3 Direction;
    float InnerCutoff;
    float OuterCutoff;
    float Exponent;
};

uniform vec3 CameraPosition;
uniform vec3 GlobalAmbient;

uniform int DirLightCount; 
uniform DirLight DirectionalLights[2];

uniform int SpotLightCount;
uniform SpotLight SpotLights[16];

uniform int PointLightCount;
uniform PointLight PointLights[16];

//0 is fully in shadow, 1 is fully lit
float ReadShadowMap(in vec3 fragPos, in vec3 normal, in float diffuseFactor, in BaseLight light)
{
    float maxBias = 0.04;
    float minBias = 0.001;

    vec4 fragPosLightSpace = light.WorldToLightSpaceProjMatrix * vec4(fragPos, 1.0);
    vec3 fragCoord = fragPosLightSpace.xyz / fragPosLightSpace.w;
    fragCoord = fragCoord * vec3(0.5) + vec3(0.5);
    float bias = max(maxBias * -diffuseFactor, minBias);

    float depth = texture(light.ShadowMap, fragCoord.xy).r;
    float shadow = (fragCoord.z - bias) > depth ? 0.0 : 1.0;        

    //float shadow = 0.0;
    //vec2 texelSize = 1.0 / textureSize(light.ShadowMap, 0);
    //for (int x = -1; x <= 1; ++x)
    //{
    //    for (int y = -1; y <= 1; ++y)
    //    {
    //        float pcfDepth = texture(light.ShadowMap, fragCoord.xy + vec2(x, y) * texelSize).r;
    //        shadow += fragCoord.z - bias > pcfDepth ? 0.0 : 1.0;        
    //    }    
    //}
    //shadow *= 0.111111111; //divided by 9

    return shadow;
}

float Attenuate(in float dist, in float radius)
{
    return pow(clamp(1.0 - pow(dist / radius, 4), 0.0, 1.0), 2.0) / (dist * dist + 1.0);
}

vec3 CalcColor(BaseLight light, vec3 lightDirection, vec3 normal, vec3 fragPos, vec3 albedo, float spec, float ambientOcclusion)
{
    vec3 AmbientColor = vec3(light.Color * light.AmbientIntensity);
    vec3 DiffuseColor = vec3(0.0);
    vec3 SpecularColor = vec3(0.0);

    float DiffuseFactor = dot(normal, -lightDirection);
    if (DiffuseFactor > 0.0)
    {
        DiffuseColor = light.Color * light.DiffuseIntensity * albedo * DiffuseFactor;

        vec3 posToEye = normalize(CameraPosition - fragPos);
        vec3 reflectDir = reflect(lightDirection, normal);
        float SpecularFactor = dot(posToEye, reflectDir);
        if (SpecularFactor > 0.0)
        {
            SpecularColor = light.Color * spec * pow(SpecularFactor, 64.0);
        }
    }

    float shadow = ReadShadowMap(fragPos, normal, DiffuseFactor, light);
    return (AmbientColor + (DiffuseColor + SpecularColor) * shadow) * ambientOcclusion;
}

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 fragPos, vec3 albedo, float spec, float ambientOcclusion)
{
    return CalcColor(light.Base, light.Direction, normal, fragPos, albedo, spec, ambientOcclusion);
}

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 albedo, float spec, float ambientOcclusion)
{
    vec3 lightToPos = fragPos - light.Position;
    return Attenuate(length(lightToPos), light.Radius) * CalcColor(light.Base, normalize(lightToPos), normal, fragPos, albedo, spec, ambientOcclusion);
} 

vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 albedo, float spec, float ambientOcclusion)
{
    //if (light.OuterCutoff <= 1.5707) //~90 degrees in radians
    {
        vec3 lightToPos = normalize(fragPos - light.Base.Position);
        float clampedCosine = max(0.0, dot(lightToPos, normalize(light.Direction)));
        float spotEffect = smoothstep(light.OuterCutoff, light.InnerCutoff, clampedCosine);
	    //if (clampedCosine >= light.OuterCutoff)
        {
            vec3 lightToPos = fragPos - light.Base.Position;
            float spotAttn = pow(clampedCosine, light.Exponent);
            float distAttn = Attenuate(length(lightToPos) / light.Base.Brightness, light.Base.Radius);
            vec3 color = CalcColor(light.Base.Base, normalize(lightToPos), normal, fragPos, albedo, spec, ambientOcclusion);
            return spotEffect * spotAttn * distAttn * color;
        }
    }
    return vec3(0.0);
}

vec3 CalcTotalLight(vec3 normal, vec3 fragPosWS, vec3 albedoColor, float specularIntensity, float ambientOcclusion)
{
    vec3 totalLight = GlobalAmbient;

    for (int i = 0; i < DirLightCount; ++i)
        totalLight += CalcDirLight(DirectionalLights[i], normal, fragPosWS, albedoColor, specularIntensity, ambientOcclusion);

    for (int i = 0; i < PointLightCount; ++i)
        totalLight += CalcPointLight(PointLights[i], normal, fragPosWS, albedoColor, specularIntensity, ambientOcclusion);

    for (int i = 0; i < SpotLightCount; ++i)
        totalLight += CalcSpotLight(SpotLights[i], normal, fragPosWS, albedoColor, specularIntensity, ambientOcclusion);

    return totalLight;
}