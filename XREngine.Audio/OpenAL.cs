using Silk.NET.OpenAL;
using System.Numerics;

namespace XREngine.Audio
{
    public class OpenAL
    {
        public void Initialize()
        {
            AL al = AL.GetApi();

            uint buffer = al.GenBuffer();
            uint source = al.GenSource();

            al.SetListenerProperty(ListenerVector3.Position, Vector3.Zero);
        }
    }
}