#version 450

layout (location = 0) in vec3 FragPos;
layout(location = 0) out float Depth;

uniform vec3 LightPos;
uniform float FarPlaneDist;

void main()
{
	// write modified depth
	// map to [0;1] range by dividing by far_plane
	float d = length(FragPos - LightPos) / FarPlaneDist;
	gl_FragDepth = d;
	Depth = d;
}
