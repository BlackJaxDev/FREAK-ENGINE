using Silk.NET.GLFW;
using System.Numerics;

namespace XREngine.Rendering.UI
{
    public interface IMenuItem
    {
        event Action<MenuItemComponent> Opening;
        event Action<MenuItemComponent> Closing;

        void OnOpening();
        void OnClosing();
    }
    public interface IMenuDivider : IMenuItem { }
    public interface IMenuOption : IMenuItem, IMenu
    {
        string Text { get; set; }
        Keys[] HotKeys { get; set; }
        void ExecuteAction();
    }
    public interface IMenu : IMenuItem
    {
    }
    public class MenuComponent : MenuItemComponent, IMenu
    {
        public MenuItemComponent? HoveredMenuItem { get; set; }

        public void Show(UIComponent parent, Vector2 worldPosition, float z)
        {

        }

        public void Show(UICanvasTransform canvas, Vector2 worldPosition, float z)
            => Show(canvas, new Vector3(worldPosition, z));

        public void Show(UICanvasTransform canvas, Vector3 worldPosition)
        {

        }
    }
    public class MenuItemComponent : UIButtonComponent, IMenuItem
    {
        public event Action<MenuItemComponent>? Opening;
        public event Action<MenuItemComponent>? Closing;

        public virtual void OnClosing() => Closing?.Invoke(this);
        public virtual void OnOpening() => Opening?.Invoke(this);

        //public event Action ChildrenCleared;
        //public event Action<ITMenuItem> ChildAdded;

        //public void Add(ITMenuItem item)
        //{
        //    _items.Add(item);
        //    ChildAdded?.Invoke(item);
        //}
        //public void Clear()
        //{
        //    _items.Clear();
        //    ChildrenCleared?.Invoke();
        //}

        //public override void OnOpening() => _children.ForEach(x => x.OnOpening());
        //public override void OnClosing() => _children.ForEach(x => x.OnClosing());

    }
    public sealed class MenuDivider : MenuItemComponent, IMenuDivider
    {
        public static MenuDivider Instance { get; } = new MenuDivider();
        private MenuDivider() : base() { }
    }
    public class MenuOption(string text, Action action, Keys[] hotKeys) : MenuComponent, IMenuOption
    {
        public string Text { get; set; } = text;
        public Keys[] HotKeys { get; set; } = hotKeys;
        public Action Action { get; set; } = action;

        public void ExecuteAction()
        {
            Action?.Invoke();
        }
    }
}
