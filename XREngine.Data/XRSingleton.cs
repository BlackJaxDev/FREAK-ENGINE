using XREngine.Data.Core;

namespace XREngine.Rendering
{
    public class XRSingleton<T> : XRBase where T : new()
    {
        static XRSingleton() => _instance = new Lazy<T>(() => new T(), true);

        private static readonly Lazy<T> _instance;

        public static T Instance => _instance.Value;
    }
}