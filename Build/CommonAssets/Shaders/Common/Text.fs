#version 430 core

layout(location = 4) in vec2 FragUV0;

out vec4 FragColor;

uniform sampler2D Texture0;

void main()
{
    FragColor = vec4(1.0f, 0.0f, 0.0f, 1.0f);//texture(Texture0, FragUV0);
}