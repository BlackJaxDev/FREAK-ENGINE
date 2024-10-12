#version 450

layout (location = 0) out vec3 OutColor;
layout (location = 0) in vec3 FragPos;

uniform sampler2D Texture0;

const float PI = 3.14159265359f;

void main()
{
    vec3 N = normalize(FragPos);

    vec3 irradiance = vec3(0.0f);

    // tangent space calculation from origin point
    vec3 up    = vec3(0.0f, 1.0f, 0.0f);
    vec3 right = cross(up, N);
    up         = cross(N, right);

    float sampleDelta = 0.025f;
    int numSamples = 0;
    for (float phi = 0.0f; phi < 2.0f * PI; phi += sampleDelta)
    {
        for (float theta = 0.0f; theta < 0.5f * PI; theta += sampleDelta)
        {
            // spherical to cartesian (in tangent space)
            float tanX = sin(theta) * cos(phi);
            float tanY = sin(theta) * sin(phi);
            float tanZ = cos(theta);

            // tangent space to world
            vec3 sampleVec = tanX * right + tanY * up + tanZ * N;
            
            vec3 dir = normalize(sampleVec);
            // Convert the direction vector to spherical coordinates
            float phi = atan(dir.z, dir.x); // Angle around the Y axis
            float theta2 = asin(dir.y); // Angle from the Y axis
            // Map spherical coordinates to [0, 1] range for texture sampling
            // phi ranges from -PI to PI, so we map it to [0, 1]
            // theta ranges from -PI/2 to PI/2, so we map it to [0, 1]
            vec2 uv = vec2((phi / (2.0f * PI)) + 0.5f, 1.0f - ((theta2 / PI) + 0.5f));

            irradiance += texture(Texture0, uv).rgb * cos(theta) * sin(theta);
            ++numSamples;
        }
    }

    OutColor = irradiance * vec3(PI / float(numSamples));
}
