float randPerlin1(vec2 c)
{
	return fract(sin(dot(c.xy ,vec2(12.9898,78.233))) * 43758.5453);
}
float noisePerlin1(vec2 p, float freq)
{
	float unit = screenWidth / freq;
	vec2 ij = floor(p / unit);
	vec2 xy = mod(p, unit) / unit;
	//xy = 3.0f * xy * xy - 2.0f * xy * xy * xy;
	xy = 0.5f * (1.0f - cos(PI * xy));
	float a = randPerlin1((ij + vec2(0.0f, 0.0f)));
	float b = randPerlin1((ij + vec2(1.0f, 0.0f)));
	float c = randPerlin1((ij + vec2(0.0f, 1.0f)));
	float d = randPerlin1((ij + vec2(1.0f, 1.0f)));
	float x1 = mix(a, b, xy.x);
	float x2 = mix(c, d, xy.x);
	return mix(x1, x2, xy.y);
}
float perlin1(vec2 p, int res)
{
	float persistence = 0.5f;
	float n = 0.0f;
	float normK = 0.0f;
	float f = 4.0f;
	float amp = 1.0f;
	int iCount = 0;

	for (int i = 0; i < 50; i++)
  {
		n += amp * noisePerlin1(p, f);
		f *= 2.0f;
		normK += amp;
		amp *= persistence;
		if (iCount == res)
      break;
		iCount++;
	}

	float nf = n / normK;
	return nf * nf * nf * nf;
}
