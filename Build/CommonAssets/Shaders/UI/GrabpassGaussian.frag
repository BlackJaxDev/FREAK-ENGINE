#version 450
layout (location = 0) out vec4 OutColor;

uniform sampler2D Texture0;
uniform float ScreenWidth;
uniform float ScreenHeight;
uniform int SampleCount;
uniform vec4 MatColor;
uniform float BlurStrength;

const float pi = 3.14159265359f;
float gaussian(float x, float sigma)
{
    return exp(-((x * x) / (2.0 * sigma * sigma))) / (sqrt(2.0 * pi) * sigma);
}

void main()
{
    float xOffset = 1.0f / ScreenWidth;
    float yOffset = 1.0f / ScreenHeight;
    vec2 vTexCoord = vec2(gl_FragCoord.x * xOffset, gl_FragCoord.y * yOffset);
    vec3 col = texture(Texture0, vTexCoord).rgb * gaussian(0.0, BlurStrength);

    for (int i = 1; i < SampleCount; i++)
    {
        float offset = float(i) * BlurStrength;
        float weight = gaussian(float(i), BlurStrength);
        col += texture(Texture0, vTexCoord + vec2(xOffset * offset, 0.0f)).rgb * weight;
        col += texture(Texture0, vTexCoord - vec2(xOffset * offset, 0.0f)).rgb * weight;
        col += texture(Texture0, vTexCoord + vec2(0.0f, yOffset * offset)).rgb * weight;
        col += texture(Texture0, vTexCoord - vec2(0.0f, yOffset * offset)).rgb * weight;
    }

    OutColor = vec4(col, 1.0f) * MatColor;
}