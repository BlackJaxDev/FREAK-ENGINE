namespace Extensions
{
    public static partial class Ext
    {
        //public static bool MoveUp(this TreeNode node)
        //{
        //    TreeNode parent = node.Parent;
        //    TreeView view = node.TreeView;
        //    if (parent != null)
        //    {
        //        int index = parent.Nodes.IndexOf(node);
        //        if (index > 0)
        //        {
        //            parent.Nodes.RemoveAt(index);
        //            parent.Nodes.Insert(index - 1, node);
        //            return true;
        //        }
        //    }
        //    else if (node.TreeView.Nodes.Contains(node))
        //    {
        //        int index = view.Nodes.IndexOf(node);
        //        if (index > 0)
        //        {
        //            view.Nodes.RemoveAt(index);
        //            view.Nodes.Insert(index - 1, node);
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        //public static bool MoveDown(this TreeNode node)
        //{
        //    TreeNode parent = node.Parent;
        //    TreeView view = node.TreeView;
        //    if (parent != null)
        //    {
        //        int index = parent.Nodes.IndexOf(node);
        //        if (index < parent.Nodes.Count - 1)
        //        {
        //            parent.Nodes.RemoveAt(index);
        //            parent.Nodes.Insert(index + 1, node);
        //            return true;
        //        }
        //    }
        //    else if (view != null && view.Nodes.Contains(node))
        //    {
        //        int index = view.Nodes.IndexOf(node);
        //        if (index < view.Nodes.Count - 1)
        //        {
        //            view.Nodes.RemoveAt(index);
        //            view.Nodes.Insert(index + 1, node);
        //            return true;
        //        }
        //    }
        //    return false;
        //}
    }
}
