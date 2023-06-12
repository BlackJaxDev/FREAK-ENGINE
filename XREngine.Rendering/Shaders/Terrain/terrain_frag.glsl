
//Fragment shader:
//The fragment shader will handle the terrain's shading, including lighting and texturing.
//You can use a combination of diffuse, normal, and specular maps to create a realistic appearance.

#version 450
#extension GL_ARB_separate_shader_objects : enable

in vec3 teWorldPos;
in vec2 teTexCoord;

layout (location = 0) out vec4 outColor;

layout (binding = 1) uniform sampler2D albedoTexture;
layout (binding = 2) uniform sampler2D normalTexture;
layout (binding = 3) uniform sampler2D specularTexture;

uniform vec3 lightDirection;
uniform vec3 cameraPosition;

vec3 getNormal(vec2 texCoord) {
    vec3 normal = texture(normalTexture, texCoord).rgb * 2.0 - 1.0;
    return normalize(normal);
}

float getSpecular(vec2 texCoord) {
    return texture(specularTexture, texCoord).r;
}

void main() {
    vec3 normal = getNormal(teTexCoord);
    vec3 viewDirection = normalize(cameraPosition - teWorldPos);

    vec3 lightReflection = reflect(-lightDirection, normal);
    float specular = pow(max(dot(viewDirection, lightReflection), 0.0), getSpecular(teTexCoord));

    vec3 albedo = texture(albedoTexture, teTexCoord).rgb;
    float diffuse = max(dot(normal, lightDirection), 0.0);

    vec3 ambient = 0.1 * albedo;
    vec3 color = ambient + diffuse * albedo + specular * vec3(1.0);

    outColor = vec4(color, 1.0);
}