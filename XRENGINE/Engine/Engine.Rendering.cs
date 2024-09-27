using XREngine.Rendering;
using XREngine.Scene;

namespace XREngine
{
    public static partial class Engine
    {
        public static partial class Rendering
        {
            //TODO: create objects for only relevant windows that house the viewports that this object is visible in
            public static List<AbstractRenderAPIObject?> CreateObjectsForAllWindows(GenericRenderObject obj)
                => Windows.Select(window => window.Renderer.GetOrCreateAPIRenderObject(obj)).ToList();

            public static Dictionary<GenericRenderObject, AbstractRenderAPIObject> CreateObjectsForNewRenderer(AbstractRenderer renderer)
            {
                Dictionary<GenericRenderObject, AbstractRenderAPIObject> roDic = [];
                foreach (var pair in GenericRenderObject.RenderObjectCache)
                    foreach (var obj in pair.Value)
                    {
                        AbstractRenderAPIObject? apiRO = renderer.GetOrCreateAPIRenderObject(obj);
                        if (apiRO is null)
                            continue;

                        roDic.Add(obj, apiRO);
                        obj.AddWrapper(apiRO);
                    }
                
                return roDic;
            }

            public static void DestroyObjectsForRenderer(AbstractRenderer renderer)
            {
                foreach (var pair in GenericRenderObject.RenderObjectCache)
                    foreach (var obj in pair.Value)
                        if (renderer.TryGetAPIRenderObject(obj, out var apiRO) && apiRO is not null)
                            obj.RemoveWrapper(apiRO);
            }

            public static PhysicsScene NewPhysicsScene()
                => new DefaultPhysxScene();

            public static VisualScene NewVisualScene()
                => new VisualScene3D();

            public static RenderPipeline NewRenderPipeline()
                => new TestRenderPipeline();
        }
    }
}
