//	<https://www.shadertoy.com/view/4dS3Wd>
//	By Morgan McGuire @morgan3d, http://graphicscodex.com

float hash(float n)
{
  return fract(sin(n) * 1e4);
}
float hash(vec2 p)
{
  return fract(1e4 * sin(17.0f * p.x + p.y * 0.1f) * (0.1f + abs(sin(p.y * 13.0f + p.x))));
}

float noise4(float x)
{
	float i = floor(x);
	float f = fract(x);
	float u = f * f * (3.0f - 2.0f * f);
	return mix(hash(i), hash(i + 1.0f), u);
}
float noise4(vec2 x)
{
	vec2 i = floor(x);
	vec2 f = fract(x);

	// Four corners in 2D of a tile
	float a = hash(i);
	float b = hash(i + vec2(1.0f, 0.0f));
	float c = hash(i + vec2(0.0f, 1.0f));
	float d = hash(i + vec2(1.0f, 1.0f));

	// Simple 2D lerp using smoothstep envelope between the values.
	// return vec3(mix(mix(a, b, smoothstep(0.0f, 1.0f, f.x)),
	//			mix(c, d, smoothstep(0.0f, 1.0f, f.x)),
	//			smoothstep(0.0f, 1.0f, f.y)));

	// Same code, with the clamps in smoothstep and common subexpressions
	// optimized away.
	vec2 u = f * f * (3.0f - 2.0f * f);
	return mix(a, b, u.x) + (c - a) * u.y * (1.0f - u.x) + (d - b) * u.x * u.y;
}
// This one has non-ideal tiling properties that I'm still tuning
float noise4(vec3 x)
{
	const vec3 step = vec3(110, 241, 171);

	vec3 i = floor(x);
	vec3 f = fract(x);

	// For performance, compute the base input to a 1D hash from the integer part of the argument and the
	// incremental change to the 1D based on the 3D -> 1D wrapping
  float n = dot(i, step);

	vec3 u = f * f * (3.0f - 2.0f * f);
	return mix(mix(mix( hash(n + dot(step, vec3(0, 0, 0))), hash(n + dot(step, vec3(1, 0, 0))), u.x),
                 mix( hash(n + dot(step, vec3(0, 1, 0))), hash(n + dot(step, vec3(1, 1, 0))), u.x), u.y),
             mix(mix( hash(n + dot(step, vec3(0, 0, 1))), hash(n + dot(step, vec3(1, 0, 1))), u.x),
                 mix( hash(n + dot(step, vec3(0, 1, 1))), hash(n + dot(step, vec3(1, 1, 1))), u.x), u.y), u.z);
}
