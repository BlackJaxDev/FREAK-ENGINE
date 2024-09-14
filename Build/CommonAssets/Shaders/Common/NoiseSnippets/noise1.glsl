float randNoise1(float n)
{
  return fract(sin(n) * 43758.5453123);
}
float noise1(float p)
{
	float fl = floor(p);
  float fc = fract(p);
	return mix(randNoise1(fl), randNoise1(fl + 1.0), fc);
}
float noise1(vec2 n)
{
	const vec2 d = vec2(0.0, 1.0);
  vec2 b = floor(n), f = smoothstep(vec2(0.0), vec2(1.0), fract(n));
	return mix(mix(randNoise1(b), randNoise1(b + d.yx), f.x), mix(randNoise1(b + d.xy), randNoise1(b + d.yy), f.x), f.y);
}
