#version 460

layout (location = 0) out vec3 OutColor;
layout (location = 0) in vec3 FragPos;

uniform sampler2D Texture0;

void main()
{
    vec3 dir = normalize(FragPos);

    // Convert the direction vector to spherical coordinates
    float phi = atan(dir.z, dir.x); // Angle around the Y axis
    float theta = asin(dir.y);      // Angle from the Y axis
    // Map spherical coordinates to [0, 1] range for texture sampling
    // phi ranges from -PI to PI, so we map it to [0, 1]
    // theta ranges from -PI/2 to PI/2, so we map it to [0, 1]
    vec2 uv = vec2((phi / (2.0 * 3.14159265359)) + 0.5, (theta / 3.14159265359) + 0.5);

    OutColor = texture(Texture0, uv).rgb;
}