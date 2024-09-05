//using System;
//using System.Windows.Forms;
//using XREngine.Core.Maths.Transforms;

//namespace XREngine.Rendering.UI
//{
//    public interface ITMenuItem : IObjectSlim
//    {
//        event Action<TMenuItemComponent> Opening;
//        event Action<TMenuItemComponent> Closing;

//        void OnOpening();
//        void OnClosing();
//    }
//    public interface ITMenuDivider : ITMenuItem { }
//    public interface ITMenuOption : ITMenuItem, ITMenu
//    {
//        string Text { get; set; }
//        Keys HotKeys { get; set; }
//        void ExecuteAction();
//    }
//    public interface ITMenu : ITMenuItem
//    {
//    }
//    public class TMenuComponent : TMenuItemComponent, ITMenu
//    {
//        public TMenuItemComponent HoveredMenuItem { get; set; }

//        public void Show(UIComponent parent, Vector2 worldPosition, float z)
//        {

//        }

//        public void Show(UICanvasTransform canvas, Vector2 worldPosition, float z)
//            => Show(canvas, new Vector3(worldPosition, z));
        
//        public void Show(UICanvasTransform canvas, Vector3 worldPosition)
//        {

//        }
//    }
//    public class TMenuItemComponent : UIButtonComponent, ITMenuItem
//    {
//        public event Action<TMenuItemComponent> Opening;
//        public event Action<TMenuItemComponent> Closing;

//        public virtual void OnClosing() => Closing?.Invoke(this);
//        public virtual void OnOpening() => Opening?.Invoke(this);

//        //public event Action ChildrenCleared;
//        //public event Action<ITMenuItem> ChildAdded;

//        //public void Add(ITMenuItem item)
//        //{
//        //    _items.Add(item);
//        //    ChildAdded?.Invoke(item);
//        //}
//        //public void Clear()
//        //{
//        //    _items.Clear();
//        //    ChildrenCleared?.Invoke();
//        //}

//        //public override void OnOpening() => _children.ForEach(x => x.OnOpening());
//        //public override void OnClosing() => _children.ForEach(x => x.OnClosing());

//    }
//    public sealed class TMenuDivider : TMenuItemComponent, ITMenuDivider
//    {
//        public static TMenuDivider Instance { get; } = new TMenuDivider();
//        private TMenuDivider() : base() { }
//    }
//    public class TMenuOption : TMenuComponent, ITMenuOption
//    {
//        public TMenuOption(string text, Action action, Keys hotKeys)
//        {
//            Text = text;
//            HotKeys = hotKeys;
//            Action = action;
//        }

//        public string Text { get; set; }
//        public Keys HotKeys { get; set; }
//        public Action Action { get; set; }

//        public void ExecuteAction()
//        {
//            Action?.Invoke();
//        }
//    }
//}
