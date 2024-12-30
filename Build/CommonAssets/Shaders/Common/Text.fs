#version 460

layout(location = 4) in vec2 FragUV0;

out vec4 FragColor;

uniform sampler2D Texture0;

uniform vec4 TextColor;

void main()
{
    vec4 color = texture(Texture0, FragUV0);
    color *= TextColor;
    FragColor = color;
}