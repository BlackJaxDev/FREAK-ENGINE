using Silk.NET.OpenAL.Extensions.Creative;
using XREngine.Core;
using XREngine.Data.Core;

namespace XREngine.Audio
{
    public sealed class AudioEffect : XRBase, IDisposable, IPoolable
    {
        public EffectContext ParentContext { get; }
        internal uint Handle { get; set; }
        private EffectExtension Api => ParentContext.EffectApi;

        public AudioEffect(EffectContext parentContext)
        {
            ParentContext = parentContext;
            Handle = ParentContext.EffectApi.GenEffect();
        }

        //TODO

        public void Dispose()
        {
            ParentContext.EffectApi.DeleteEffect(Handle);
            GC.SuppressFinalize(this);
        }

        void IPoolable.OnPoolableReset()
        {
            Handle = ParentContext.EffectApi.GenEffect();
        }

        void IPoolable.OnPoolableReleased()
        {
            ParentContext.EffectApi.DeleteEffect(Handle);
        }

        void IPoolable.OnPoolableDestroyed()
        {
            Dispose();
        }
    }
}