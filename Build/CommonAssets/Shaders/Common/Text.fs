#version 460

layout(location = 4) in vec2 FragUV0;

out vec4 FragColor;

uniform sampler2D Texture0;

void main()
{
    vec4 color = texture(Texture0, FragUV0);
    if (color.a < 0.1f)
        discard;
    color.rgb = vec3(1.0f, 1.0f, 1.0f) - color.rgb;
    FragColor = color;
}