using XREngine.Rendering;
using XREngine.Scene;

namespace XREngine
{
    public static partial class Engine
    {
        public static partial class Rendering
        {
            public static List<AbstractRenderAPIObject?> CreateObjectsForAllWindows(GenericRenderObject obj)
                => Windows.Select(window => window.Renderer.GetOrCreateAPIRenderObject(obj)).ToList();

            public static Dictionary<GenericRenderObject, AbstractRenderAPIObject> CreateObjectsForWindow(XRWindow window)
            {
                Dictionary<GenericRenderObject, AbstractRenderAPIObject> roDic = [];
                foreach (GenericRenderObject obj in RenderObjects)
                {
                    AbstractRenderAPIObject? apiRO = window.Renderer.GetOrCreateAPIRenderObject(obj);
                    if (apiRO is null)
                        continue;
                    
                    roDic.Add(obj, apiRO);
                    obj.AddWrapper(apiRO);
                }
                return roDic;
            }

            public static void DestroyObjectsForWindow(XRWindow window)
            {
                foreach (GenericRenderObject obj in RenderObjects)
                    if (window.Renderer.TryGetAPIRenderObject(obj, out var apiRO) && apiRO is not null)
                        obj.RemoveWrapper(apiRO);
            }

            public static PhysicsScene NewPhysicsScene()
                => new DefaultPhysxScene();

            public static VisualScene NewVisualScene()
                => new VisualScene3D();

            public static XRRenderPipeline NewRenderPipeline()
                => new DefaultRenderPipeline();
        }
    }
}
