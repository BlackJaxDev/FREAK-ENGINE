#version 450

layout (location = 0) in vec4 Position0; //xyz vertex coords, w scale
layout (location = 1) in vec4 Other0; //instance positions
layout (location = 2) in vec4 Other1; //instance colors

uniform mat4 ModelMatrix;
uniform mat3 NormalMatrix;
uniform mat4 WorldToCameraSpaceMatrix;
uniform mat4 ProjMatrix;

layout (location = 0) out vec3 FragPos;
layout (location = 4) out vec4 FragColor0;

out gl_PerVertex
{
	vec4 gl_Position;
	float gl_PointSize;
	float gl_ClipDistance[];
};

void main()
{
	mat4 ViewMatrix = WorldToCameraSpaceMatrix;
  //ViewMatrix[0][0] = 1.0f;
  //ViewMatrix[0][1] = 0.0f;
  //ViewMatrix[0][2] = 0.0f;
  //ViewMatrix[1][0] = 0.0f;
  //ViewMatrix[1][1] = 1.0f;
  //ViewMatrix[1][2] = 0.0f;

	vec4 position = ModelMatrix * vec4(Other0.xyz + (Position0.xyz * Other0.w), 1.0f);

	FragPos = position.xyz;
  FragColor0 = Other1;

	gl_Position = ProjMatrix * ViewMatrix * position;
}
