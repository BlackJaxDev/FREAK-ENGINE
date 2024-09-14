#version 450

layout(location = 0) out vec4 OutColor;
layout(location = 0) in vec3 FragPos;

uniform sampler2D HDRSceneTex; //HDR scene color
uniform sampler2D Texture1; //Bloom
uniform sampler2D Texture2; //Depth
uniform usampler2D Texture3; //Stencil
uniform sampler2D HUDTex; //HUD

uniform vec3 HighlightColor = vec3(0.92f, 1.0f, 0.086f);

struct VignetteStruct
{
    vec3 Color;
    float Intensity;
    float Power;
};
uniform VignetteStruct Vignette;

struct ColorGradeStruct
{
    vec3 Tint;

    float Exposure;
    float Contrast;
    float Gamma;

    float Hue;
    float Saturation;
    float Brightness;
};
uniform ColorGradeStruct ColorGrade;

vec3 RGBtoHSV(vec3 c)
{
    vec4 K = vec4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10f;
    return vec3(abs(q.z + (q.w - q.y) / (6.0f * d + e)), d / (q.x + e), q.x);
}
vec3 HSVtoRGB(vec3 c)
{
    vec4 K = vec4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0f - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0f, 1.0f), c.y);
}
float rand(vec2 coord)
{
    return fract(sin(dot(coord, vec2(12.9898f, 78.233f))) * 43758.5453f);
}
float GetStencilHighlightIntensity(vec2 uv)
{
    int outlineSize = 1;
    ivec2 texSize = textureSize(HDRSceneTex, 0);
    vec2 texelSize = 1.0f / texSize;
    vec2 texelX = vec2(texelSize.x, 0.0f);
    vec2 texelY = vec2(0.0f, texelSize.y);
    uint stencilCurrent = texture(Texture3, uv).r;
    uint selectionBits = stencilCurrent & 1;
    uint diff = 0;
    vec2 zero = vec2(0.0f);

    //Check neighboring stencil texels that indicate highlighted/selected
    for (int i = 1; i <= outlineSize; ++i)
    {
          vec2 yPos = clamp(uv + texelY * i, zero, uv);
          vec2 yNeg = clamp(uv - texelY * i, zero, uv);
          vec2 xPos = clamp(uv + texelX * i, zero, uv);
          vec2 xNeg = clamp(uv - texelX * i, zero, uv);
          diff += (texture(Texture3, yPos).r & 1) - selectionBits;
          diff += (texture(Texture3, yNeg).r & 1) - selectionBits;
          diff += (texture(Texture3, xPos).r & 1) - selectionBits;
          diff += (texture(Texture3, xNeg).r & 1) - selectionBits;
    }
    return clamp(float(diff), 0.0f, 1.0f);
}
void main()
{
	vec2 uv = FragPos.xy;
	if (uv.x > 1.0f || uv.y > 1.0f)
		discard;

	vec3 hdrSceneColor = texture(HDRSceneTex, uv).rgb;

  //Add each blurred bloom mipmap
  //Starts at 1/2 size lod because original image is not blurred (and doesn't need to be)
  for (float lod = 1.0f; lod < 5.0f; lod += 1.0f)
    hdrSceneColor += textureLod(Texture1, uv, lod).rgb;

  //Tone mapping
	vec3 ldrSceneColor = vec3(1.0f) - exp(-hdrSceneColor * ColorGrade.Exposure);

	//Color grading
	ldrSceneColor *= ColorGrade.Tint;
	vec3 hsv = RGBtoHSV(ldrSceneColor);
	hsv.x *= ColorGrade.Hue;
	hsv.y *= ColorGrade.Saturation;
	hsv.z *= ColorGrade.Brightness;
	ldrSceneColor = HSVtoRGB(hsv);
	ldrSceneColor = (ldrSceneColor - 0.5f) * ColorGrade.Contrast + 0.5f;

  float highlight = GetStencilHighlightIntensity(uv);
	ldrSceneColor = mix(ldrSceneColor, HighlightColor, highlight);

	//Vignette
	vec2 vigUV = uv * (1.0f - uv.yx);
 	float vig = clamp(pow(vigUV.x * vigUV.y * Vignette.Intensity, Vignette.Power), 0.0f, 1.0f);
	ldrSceneColor = mix(Vignette.Color, ldrSceneColor, vig);

  //Add HUD on top of scene
  vec4 hudColor = texture(HUDTex, uv);
  ldrSceneColor = mix(ldrSceneColor, hudColor.rgb, hudColor.a);

	//Gamma-correct
	ldrSceneColor = pow(ldrSceneColor, vec3(1.0f / ColorGrade.Gamma));
  //Fix subtle banding by applying fine noise
  ldrSceneColor += mix(-0.5f / 255.0f, 0.5f / 255.0f, rand(uv));

	OutColor = vec4(ldrSceneColor, 1.0f);

  //float depth = GetDistanceFromDepth(texture(Texture2, uv).r);
  //uint stencil = texture(Texture3, uv).r;
  //OutColor = vec4(vec3(float(stencil) / 255.0f), 1.0f);
}
