
//Vertex shader:
//The vertex shader will process the terrain's heightmap and calculate the position, normal, and texture coordinates for each vertex.

#version 450
#extension GL_ARB_separate_shader_objects : enable

layout (location = 0) in vec2 inPosition;
layout (location = 1) in vec2 inTexCoord;

layout (binding = 0) uniform sampler2D heightmap;

out gl_PerVertex
{
    vec4 gl_Position;
};

out vec3 worldPos;
out vec2 texCoord;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

float getHeight(vec2 uv)
{
    return texture(heightmap, uv).r;
}

void main()
{
    vec3 position = vec3(inPosition, getHeight(inTexCoord));
    worldPos = vec3(model * vec4(position, 1.0));
    gl_Position = projection * view * vec4(worldPos, 1.0);
    texCoord = inTexCoord;
}