using System.Collections;
using XREngine.Data.Core;
using XREngine.Rendering.Pipelines.Commands;

namespace XREngine.Rendering
{
    public class ViewportRenderCommandContainer : XRBase, IReadOnlyList<ViewportRenderCommand>
    {
        private readonly List<ViewportRenderCommand> _commands = [];
        public IReadOnlyList<ViewportRenderCommand> Commands => _commands;

        //public bool FBOsInitialized { get; private set; } = false;
        //public bool ModifyingFBOs { get; protected set; } = false;

        public int Count => Commands.Count;
        public ViewportRenderCommand this[int index] => Commands[index];

        public StateObject AddUsing<T>(Action<T>? setOptionsFunc = null) where T : ViewportStateRenderCommandBase
        {
            T cmd = Add<T>();
            setOptionsFunc?.Invoke(cmd);
            return cmd.GetUsingState();
        }
        public StateObject AddUsing(Type t, Action<ViewportStateRenderCommandBase>? setOptionsFunc = null)
        {
            if (!typeof(ViewportStateRenderCommandBase).IsAssignableFrom(t))
                throw new ArgumentException("Type must be a subclass of ViewportStateRenderCommand.", nameof(t));

            var cmd = (ViewportStateRenderCommandBase)Add(t);
            setOptionsFunc?.Invoke(cmd);
            return cmd.GetUsingState();
        }
        public StateObject AddUsing(ViewportStateRenderCommandBase cmd)
        {
            _commands.Add(cmd);
            return cmd.GetUsingState();
        }

        public T Add<T>() where T : ViewportRenderCommand
        {
            //Create instance with this as the only parameter
            T cmd = (T)Activator.CreateInstance(typeof(T), this);
            _commands.Add(cmd);
            return cmd;
        }
        public ViewportRenderCommand Add(Type t)
        {
            if (!typeof(ViewportRenderCommand).IsAssignableFrom(t))
                throw new ArgumentException("Type must be a subclass of ViewportRenderCommand.", nameof(t));

            ViewportRenderCommand cmd = (ViewportRenderCommand)Activator.CreateInstance(t, this);
            _commands.Add(cmd);
            return cmd;
        }
        public void Add(ViewportRenderCommand cmd)
            => _commands.Add(cmd);

        public IEnumerator<ViewportRenderCommand> GetEnumerator()
            => Commands.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)Commands).GetEnumerator();

        /// <summary>
        /// Executes all commands in the container.
        /// </summary>
        public void Execute()
        {
            foreach (ViewportRenderCommand command in _commands)
                command.ExecuteIfShould();
        }
        public void DestroyFBOs()
        {
            foreach (var rc in _commands)
                rc?.DestroyFBOs();
            //FBOsInitialized = false;
        }
        //public void GenerateFBOs()
        //{
        //    foreach (var rc in _commands)
        //        rc?.GenerateFBOs();
        //    FBOsInitialized = true;
        //}
        //public void RegenerateFBOs()
        //{
        //    foreach (var rc in _commands)
        //    {
        //        rc?.DestroyFBOs();
        //        rc?.GenerateFBOs();
        //    }
        //    FBOsInitialized = true;
        //}
    }
}
