float randNoise2(vec2 n)
{
	return fract(sin(dot(n, vec2(12.9898f, 4.1414f))) * 43758.5453f);
}
float noise(vec2 p)
{
	vec2 ip = floor(p);
	vec2 u = fract(p);
  
	u = u * u * (3.0f - 2.0f * u);

	float res = mix(
		mix(randNoise2(ip), randNoise2(ip + vec2(1.0f, 0.0f)), u.x),
		mix(randNoise2(ip + vec2(0.0f, 1.0f)), randNoise2(ip + vec2(1.0f, 1.0f)), u.x), u.y);

	return res * res;
}
