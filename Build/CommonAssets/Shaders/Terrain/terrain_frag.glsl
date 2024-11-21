
//Fragment shader:
//The fragment shader will handle the terrain's shading, including lighting and texturing.
//You can use a combination of diffuse, normal, and specular maps to create a realistic appearance.

#version 450
#extension GL_ARB_separate_shader_objects : enable

in Vector3 teWorldPos;
in Vector2 teTexCoord;

layout (location = 0) out Vector4 outColor;

layout (binding = 1) uniform sampler2D albedoTexture;
layout (binding = 2) uniform sampler2D normalTexture;
layout (binding = 3) uniform sampler2D specularTexture;

uniform Vector3 lightDirection;
uniform Vector3 cameraPosition;

Vector3 getNormal(Vector2 texCoord) {
    Vector3 normal = texture(normalTexture, texCoord).rgb * 2.0 - 1.0;
    return normalize(normal);
}

float getSpecular(Vector2 texCoord) {
    return texture(specularTexture, texCoord).r;
}

void main() {
    Vector3 normal = getNormal(teTexCoord);
    Vector3 viewDirection = normalize(cameraPosition - teWorldPos);

    Vector3 lightReflection = reflect(-lightDirection, normal);
    float specular = pow(max(dot(viewDirection, lightReflection), 0.0), getSpecular(teTexCoord));

    Vector3 albedo = texture(albedoTexture, teTexCoord).rgb;
    float diffuse = max(dot(normal, lightDirection), 0.0);

    Vector3 ambient = 0.1 * albedo;
    Vector3 color = ambient + diffuse * albedo + specular * Vector3(1.0);

    outColor = Vector4(color, 1.0);
}