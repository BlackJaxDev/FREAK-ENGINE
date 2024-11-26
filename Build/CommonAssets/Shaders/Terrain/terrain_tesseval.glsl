﻿
//Tessellation evaluation shader:
//This shader will interpolate the position, normal, and texture coordinates of the vertices generated by the tessellation control shader, and generate new vertices for the tessellated mesh.

#version 450
#extension GL_ARB_separate_shader_objects : enable

layout (triangles, equal_spacing, cw) in;

in Vector3 tcWorldPos[];
in Vector2 tcTexCoord[];

out Vector3 teWorldPos;
out Vector2 teTexCoord;

void main()
{
    Vector3 position = gl_TessCoord.x * tcWorldPos[0] + gl_TessCoord.y * tcWorldPos[1] + gl_TessCoord.z * tcWorldPos[2];
    Vector2 texCoord = gl_TessCoord.x * tcTexCoord[0] + gl_TessCoord.y * tcTexCoord[1] + gl_TessCoord.z * tcTexCoord[2];

    teWorldPos = position;
    teTexCoord = texCoord;

    gl_Position =
    gl_in[0].gl_Position * gl_TessCoord.x +
    gl_in[1].gl_Position * gl_TessCoord.y +
    gl_in[2].gl_Position * gl_TessCoord.z;
}