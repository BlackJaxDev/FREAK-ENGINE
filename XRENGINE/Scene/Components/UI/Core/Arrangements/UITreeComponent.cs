namespace XREngine.Rendering.UI
{
    public class UITreeComponent : UIGridTransform
    {
        public UITreeComponent()
        {
            
        }
    }
    public class UITreeItemComponent : UIBoundableTransform
    {
        public UITreeComponent OwningTreeComponent { get; set; }
        public int TreeDepth { get; set; }

        public UITreeItemComponent()
        {

        }
    }
}