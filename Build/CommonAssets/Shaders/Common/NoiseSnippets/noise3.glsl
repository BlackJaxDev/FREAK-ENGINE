float mod289(float x)
{
  return x - floor(x * (1.0f / 289.0f)) * 289.0f;
}
vec4 mod289(vec4 x)
{
  return x - floor(x * (1.0f / 289.0f)) * 289.0f;
}
vec4 perm(vec4 x)
{
  return mod289(((x * 34.0f) + 1.0f) * x);
}
float noise3(vec3 p)
{
  vec3 a = floor(p);
  vec3 d = p - a;
  d = d * d * (3.0f - 2.0f * d);

  vec4 b = a.xxyy + vec4(0.0f, 1.0f, 0.0f, 1.0f);
  vec4 k1 = perm(b.xyxy);
  vec4 k2 = perm(k1.xyxy + b.zzww);

  vec4 c = k2 + a.zzzz;
  vec4 k3 = perm(c);
  vec4 k4 = perm(c + 1.0f);

  vec4 o1 = fract(k3 * (1.0f / 41.0f));
  vec4 o2 = fract(k4 * (1.0f / 41.0f));

  vec4 o3 = o2 * d.z + o1 * (1.0f - d.z);
  vec2 o4 = o3.yw * d.x + o3.xz * (1.0f - d.x);

  return o4.y * d.y + o4.x * (1.0f - d.y);
}
