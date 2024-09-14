#version 400

in vec4 position;
uniform sampler2D terrain;

void main(void)
{
    vec2 texcoord = position.xy;
    float height = texture(terrain, texcoord).a;
    vec4 displaced = vec4(position.x, position.y, height, 1.0);
    gl_Position = displaced;
}
