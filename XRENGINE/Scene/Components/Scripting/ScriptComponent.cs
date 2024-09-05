using System.ComponentModel;
using XREngine.Data.Core;

namespace XREngine.Components.Logic.Scripting
{
    public abstract class ScriptComponent : XRComponent
    {
        private TextFile? _script;
        [Browsable(false)]
        public TextFile? Script
        {
            get => _script;
            set => _script = value;
        }
        
        public List<TickingScriptExecInfo> TickingMethods { get; set; } = [];

        /// <summary>
        /// Script is called with this info when the owning actor is spawned in the world.
        /// </summary>
        /// 
        public ScriptExecInfo SpawnedMethod { get; set; } = null;

        /// <summary>
        /// Script is called with this info when the owning actor is despawned from the world.
        /// </summary>
        public ScriptExecInfo DespawnedMethod { get; set; } = null;
        
        public abstract bool Execute(string methodName, params object[] args);
        public bool Execute(ScriptExecInfo info)
        {
            if (info != null)
                return Execute(info.MethodName, info.Arguments);
            return false;
        }

        protected override void Constructing()
        {
            if (TickingMethods != null)
            {
                foreach (var info in TickingMethods)
                {
                    info.TickMethod = TickMethod;
                    RegisterTick(info.TickGroup, info.TickOrder, info.Method);
                }
            }
            Execute(SpawnedMethod);
        }

        protected override void OnDestroying()
        {
            if (TickingMethods != null)
            {
                foreach (var info in TickingMethods)
                {
                    info.TickMethod = null;
                    UnregisterTick(info.TickGroup, info.TickOrder, info.Method);
                }
            }
            Execute(DespawnedMethod);
        }

        private void TickMethod(string methodName)
            => Execute(methodName);

        protected virtual void OnLoaded(TextFile script) { }
        protected virtual void OnUnloaded(TextFile script) { }
    }
    public class TickingScriptExecInfo : XRBase
    {
        public string MethodName { get; set; } = string.Empty;
        public int TickOrder { get; set; }
        public ETickGroup TickGroup { get; set; }

        internal Action<string>? TickMethod { get; set; }
        internal void Method()
            => TickMethod?.Invoke(MethodName);
    }
    public class ScriptExecInfo : XRBase
    {
        public string MethodName { get; set; } = string.Empty;
        public object[] Arguments { get; set; } = [];
    }
}
