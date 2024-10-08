using Silk.NET.OpenAL.Extensions.Creative;
using XREngine.Core;
using XREngine.Data.Core;

namespace XREngine.Audio
{
    public sealed class EffectContext : XRBase
    {
        internal ListenerContext Listener { get; }
        internal EffectExtension EffectApi { get; }

        public EventDictionary<uint, AudioEffect> Effects { get; } = [];
        private ResourcePool<AudioEffect> EffectPool { get; }

        public EffectContext(ListenerContext listener, EffectExtension effects)
        {
            Listener = listener;
            EffectApi = effects;
            EffectPool = new ResourcePool<AudioEffect>(() => new AudioEffect(this));
        }

        public AudioEffect TakeEffect()
        {
            var effect = EffectPool.Take();
            Effects.Add(effect.Handle, effect);
            return effect;
        }

        public void ReleaseEffect(AudioEffect effect)
        {
            Effects.Remove(effect.Handle);
            EffectPool.Release(effect);
        }

        public void DestroyUnusedEffects(int count)
            => EffectPool.Destroy(count);

        public AudioEffect? GetEffectByHandle(uint handle)
            => Effects.TryGetValue(handle, out AudioEffect? effect) ? effect : null;
    }
}