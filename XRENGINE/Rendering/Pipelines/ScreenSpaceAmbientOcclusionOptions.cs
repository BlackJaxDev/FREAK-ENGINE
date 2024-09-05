using System.Numerics;
using XREngine.Data;
using XREngine.Data.Core;

namespace XREngine.Rendering
{
    public class ScreenSpaceAmbientOcclusionOptions : XRBase
    {
        public const int DefaultSamples = 64;
        const uint DefaultNoiseWidth = 4u, DefaultNoiseHeight = 4u;
        const float DefaultMinSampleDist = 0.1f, DefaultMaxSampleDist = 1.0f;

        public Vector2[] Noise { get; private set; }
        public Vector3[] Kernel { get; private set; }
        public int Samples { get; private set; }
        public uint NoiseWidth { get; private set; }
        public uint NoiseHeight { get; private set; }
        public float MinSampleDist { get; private set; }
        public float MaxSampleDist { get; private set; }

        public ScreenSpaceAmbientOcclusionOptions(
            int samples = DefaultSamples,
            uint noiseWidth = DefaultNoiseWidth,
            uint noiseHeight = DefaultNoiseHeight,
            float minSampleDist = DefaultMinSampleDist,
            float maxSampleDist = DefaultMaxSampleDist)
        {
            Samples = samples;
            NoiseWidth = noiseWidth;
            NoiseHeight = noiseHeight;
            MinSampleDist = minSampleDist;
            MaxSampleDist = maxSampleDist;

            Random r = new();

            Kernel = new Vector3[samples];
            Noise = new Vector2[noiseWidth * noiseHeight];

            float scale;
            Vector3 sample;

            for (int i = 0; i < samples; ++i)
            {
                sample = Vector3.Normalize(new Vector3(
                    (float)r.NextDouble() * 2.0f - 1.0f,
                    (float)r.NextDouble() * 2.0f - 1.0f,
                    (float)r.NextDouble() + 0.1f));
                scale = i / (float)samples;
                sample *= Interp.Lerp(minSampleDist, maxSampleDist, scale * scale);
                Kernel[i] = sample;
            }

            for (int i = 0; i < Noise.Length; ++i)
                Noise[i] = Vector2.Normalize(new Vector2((float)r.NextDouble(), (float)r.NextDouble()));
        }
    }
}