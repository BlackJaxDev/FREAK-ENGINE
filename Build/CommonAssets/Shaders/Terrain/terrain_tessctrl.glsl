
//Tessellation control shader:
//This shader will determine the level of tessellation for each patch based on the distance from the camera.
//Closer patches will have higher tessellation levels, while those further away will have lower levels.
//This ensures that the terrain mesh has more detail where it is most visible.

#version 450
#extension GL_ARB_separate_shader_objects : enable

layout (vertices = 3) out;

in Vector3 worldPos[];
in Vector2 texCoord[];

out Vector3 tcWorldPos[];
out Vector2 tcTexCoord[];

uniform Vector3 cameraPosition;
uniform float maxTessellationLevel;
uniform float minTessellationLevel;
uniform float tessellationRange;
uniform float lodDistance; // Add a uniform to control LOD distance

float getTessellationLevel(float distance) {
    float t = clamp((distance - lodDistance) / tessellationRange, 0.0, 1.0);
    return mix(maxTessellationLevel, minTessellationLevel, t);
}

void main() {
    tcWorldPos[gl_InvocationID] = worldPos[gl_InvocationID];
    tcTexCoord[gl_InvocationID] = texCoord[gl_InvocationID];

    if (gl_InvocationID == 0) {
        float edgeDistance0 = distance(cameraPosition, (worldPos[1] + worldPos[2]) * 0.5);
        float edgeDistance1 = distance(cameraPosition, (worldPos[0] + worldPos[2]) * 0.5);
        float edgeDistance2 = distance(cameraPosition, (worldPos[0] + worldPos[1]) * 0.5);

        gl_TessLevelOuter[0] = getTessellationLevel(edgeDistance0);
        gl_TessLevelOuter[1] = getTessellationLevel(edgeDistance1);
        gl_TessLevelOuter[2] = getTessellationLevel(edgeDistance2);
        gl_TessLevelInner[0] = min(min(gl_TessLevelOuter[0], gl_TessLevelOuter[1]), gl_TessLevelOuter[2]);
    }
}