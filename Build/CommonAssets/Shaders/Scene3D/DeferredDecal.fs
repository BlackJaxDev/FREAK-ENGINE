#version 450

layout (location = 0) out vec4 AlbedoOpacity;
layout (location = 1) out vec3 Normal;
layout (location = 2) out vec4 RMSI;

uniform sampler2D Texture0; //Screen AlbedoOpacity
uniform sampler2D Texture1; //Screen Normal
uniform sampler2D Texture2; //Screen RMSI
uniform sampler2D Texture3; //Screen Depth
uniform sampler2D Texture4; //Decal AlbedoOpacity
//uniform sampler2D Texture5; //Decal Normal
//uniform sampler2D Texture6; //Decal Roughness / Metallic

uniform float ScreenWidth;
uniform float ScreenHeight;
uniform mat4 InvProjMatrix;
uniform mat4 CameraToWorldSpaceMatrix;
uniform mat4 InvBoxWorldMatrix;
uniform mat4 BoxWorldMatrix;
uniform vec3 BoxHalfScale;

vec3 WorldPosFromDepth(in float depth, in vec2 uv)
{
	vec4 clipSpacePosition = vec4(vec3(uv, depth) * 2.0f - 1.0f, 1.0f);
	vec4 viewSpacePosition = InvProjMatrix * clipSpacePosition;
	viewSpacePosition /= viewSpacePosition.w;
	return (CameraToWorldSpaceMatrix * viewSpacePosition).xyz;
}
void main()
{
  vec2 uv = gl_FragCoord.xy / vec2(ScreenWidth, ScreenHeight);

	//Retrieve shading information from GBuffer textures
	vec4 albedo = texture(Texture0, uv);
	vec3 normal = texture(Texture1, uv).rgb;
	vec4 rmsi = texture(Texture2, uv);
	float depth = texture(Texture3, uv).r;

	//Resolve world fragment position using depth and screen UV
	vec3 fragPosWS = WorldPosFromDepth(depth, uv);
  vec4 fragPosOS = (InvBoxWorldMatrix * vec4(fragPosWS, 1.0f));
	fragPosOS.xyz /= BoxHalfScale;

  if (abs(fragPosOS.x) > 1.0f ||
			abs(fragPosOS.y) > 1.0f ||
			abs(fragPosOS.z) > 1.0f)
    discard;

  vec2 decalUV = fragPosOS.xz * vec2(0.5f) + vec2(0.5f);
	float intensity = smoothstep(0.0f, 1.0f, 1.0f - abs(fragPosOS.y));

	vec3 viewDDYBinormal = normalize(dFdy(fragPosWS));
	vec3 viewDDXTangent = normalize(dFdx(fragPosWS));

	//orthonormalize
	//tangent = normalize(tangent - normal * dot(normal, tangent));

	vec3 normal2 = normalize(cross(viewDDXTangent, viewDDYBinormal));
	mat3 tbnToWorld = mat3(CameraToWorldSpaceMatrix) * mat3(viewDDXTangent, viewDDYBinormal, normal2);

	vec4 decalAlbedoOpacity = texture(Texture4, decalUV);
	//vec3 decalNormal = normal;//texture(Texture5, decalUV).rgb;
	//decalNormal = normalize(normal + mat3(BoxWorldMatrix) * (tbnToWorld * decalNormal));
	//vec4 decalRMSI = texture(Texture6, decalUV);

	decalAlbedoOpacity.rgb = mix(albedo.rgb, decalAlbedoOpacity.rgb, decalAlbedoOpacity.a);
	//decalNormal = mix(normal, decalNormal, decalAlbedo.a);
	//decalRMSI = mix(rmsi, decalRMSI, decalAlbedo.a);

	AlbedoOpacity = vec4(decalAlbedoOpacity.rgb, albedo.a);
  Normal = normal;
	RMSI = rmsi;
}
