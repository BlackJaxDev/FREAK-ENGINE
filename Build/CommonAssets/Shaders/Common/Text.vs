#version 460

layout (location = 0) in vec3 Position;
layout (location = 1) in vec3 Normal;
layout (location = 2) in vec2 TexCoord0;

layout(std430, binding = 0) buffer GlyphTransformsBuffer
{
    mat4 GlyphTransforms[];
};

layout(std430, binding = 1) buffer GlyphTexCoordsBuffer
{
    vec4 GlyphTexCoords[];
};

uniform mat4 ModelMatrix;
uniform mat4 InverseViewMatrix;
uniform mat4 ProjMatrix;

layout (location = 0) out vec3 FragPos;
layout (location = 1) out vec3 FragNorm;
layout (location = 4) out vec2 FragUV0;
layout (location = 20) out vec3 FragPosLocal;

out gl_PerVertex
{
	vec4 gl_Position;
	float gl_PointSize;
	float gl_ClipDistance[];
};

void main()
{
	mat4 mvMatrix = inverse(InverseViewMatrix) * ModelMatrix;
    mat4 mvpMatrix = ProjMatrix * mvMatrix;
    mat3 normalMatrix = transpose(inverse(mat3(mvMatrix)));

    mat4 transform = GlyphTransforms[gl_InstanceID];
    vec4 uvRect = GlyphTexCoords[gl_InstanceID];

    vec4 position = transform * vec4(Position, 1.0f);
    vec3 normal = vec3(0.0f, 0.0f, 1.0f);

	FragPosLocal = position.xyz;
	FragPos = (mvpMatrix * position).xyz;
    gl_Position = mvpMatrix * position;
	FragNorm = normalize(normalMatrix * normal);
    FragUV0 = mix(uvRect.xy, uvRect.zw, Position.xy);
}