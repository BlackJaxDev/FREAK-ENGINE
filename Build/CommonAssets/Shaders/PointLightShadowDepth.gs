#version 450
layout (triangles) in;
layout (triangle_strip, max_vertices=18) out;

in gl_PerVertex
{
  vec4 gl_Position;
  float gl_PointSize;
  float gl_ClipDistance[];
} gl_in[];

out gl_PerVertex
{
  vec4 gl_Position;
  float gl_PointSize;
  float gl_ClipDistance[];
};

uniform mat4 InverseViewMatrices[6];
uniform mat4 ProjectionMatrices[6];

layout (location = 0) out vec3 FragPos;

void main()
{
	vec4 pos;
	vec3 fragPos;
	for (int face = 0; face < 6; ++face)
	{
		gl_Layer = face;
		for (int i = 0; i < 3; ++i)
		{
			pos = gl_in[i].gl_Position;
			FragPos = pos.xyz;
			gl_Position = (ProjectionMatrices[face] * inverse(InverseViewMatrices[face])) * pos;
			EmitVertex();
		}    
		EndPrimitive();
	}
} 