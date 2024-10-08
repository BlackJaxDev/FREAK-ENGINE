#version 450

layout (location = 0) out vec4 OutColor;
layout (location = 0) in vec3 FragPos;

uniform samplerCube Texture0;
uniform vec3 SphereCenter;

void main()
{
    OutColor = texture(Texture0, normalize(FragPos - SphereCenter));
}
