using System.Numerics;
using XREngine.Components;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;

namespace XREngine.Rendering
{
    /// <summary>
    /// This class is the base class for all render pipelines.
    /// A render pipeline is responsible for all rendering operations to render a scene to a viewport.
    /// </summary>
    public abstract class XRRenderPipeline
    {
        public const string SceneShaderPath = "Scene3D";

        public bool FBOsInitialized { get; set; } = false;
        public bool RegeneratingFBOs { get; protected set; } = false;

        //TODO: stereoscopic rendering output

        public XRFrameBuffer? DefaultRenderTarget { get; set; } = null;
        public XRQuadFrameBuffer? UserInterfaceFBO { get; protected set; }

        protected readonly RenderCommandCollection _renderCommands = new();

        public XRCamera? RenderingCamera => RenderingCameras.TryPeek(out XRCamera? camera) ? camera : null;
        public Stack<XRCamera> RenderingCameras { get; } = new Stack<XRCamera>();

        public XRViewport? Viewport { get; set; }

        public void PushRenderingCamera(XRCamera camera)
            => RenderingCameras.Push(camera);

        public void PopRenderingCamera()
            => RenderingCameras.Pop();

        public virtual void InitializeFBOs()
        {
            RegeneratingFBOs = true;
            DestroyFBOs();
        }
        public virtual void DestroyFBOs()
        {
            UserInterfaceFBO?.Destroy();
            UserInterfaceFBO = null;
        }

        //public abstract void RenderWithLayoutUpdate(XRCubeFrameBuffer renderFBO);
        public abstract void Render(VisualScene visualScene, XRCamera camera, XRViewport? vp, XRFrameBuffer? targetFrameBuffer);

        public void PreRenderSwap(CameraComponent camera)
        {
            camera.World?.VisualScene?.PreRenderSwap();
            camera.UserInterface?.Canvas?.PreRenderSwap();
            _renderCommands.SwapBuffers();
        }

        /// <summary>
        /// Gets visible items from the scene, runs pre-render methods for subscribers, and updates invalid HUD layouts.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="hud"></param>
        public void PreRenderUpdate(CameraComponent camera)
        {
            XRWorldInstance? world = camera.World;
            if (world is null)
                return;

            IVolume? volume = camera.CullWithFrustum ? camera.Camera.WorldFrustum() : null;
            world.VisualScene.PreRenderUpdate(_renderCommands, volume, camera.Camera);
            camera.UserInterface?.UpdateLayout();
        }

        public static XRTexture2D PrecomputeBRDF(uint width = 2048, uint height = 2048)
        {
            XRTexture2D brdf = XRTexture2D.CreateFrameBufferTexture(width, height, EPixelInternalFormat.Rg16f, EPixelFormat.Rg, EPixelType.HalfFloat);
            brdf.Resizable = true;
            brdf.UWrap = ETexWrapMode.ClampToEdge;
            brdf.VWrap = ETexWrapMode.ClampToEdge;
            brdf.MinFilter = ETexMinFilter.Linear;
            brdf.MagFilter = ETexMagFilter.Linear;
            brdf.SamplerName = "BRDF";
            XRTexture2D[] texRefs = [brdf];

            XRShader shader = XRShader.EngineShader(Path.Combine("Scene3D", "BRDF.fs"), EShaderType.Fragment);
            XRMaterial mat = new(texRefs, shader)
            {
                RenderOptions = new()
                {
                    DepthTest = new()
                    {
                        Enabled = ERenderParamUsage.Disabled,
                        Function = EComparison.Always,
                        UpdateDepth = false,
                    },
                }
            };

            //ndc space quad, so we don't have to load any camera matrices
            VertexTriangle[] tris = VertexQuad.Make(
                    new Vector3(-1.0f, -1.0f, -0.5f),
                    new Vector3(1.0f, -1.0f, -0.5f),
                    new Vector3(1.0f, 1.0f, -0.5f),
                    new Vector3(-1.0f, 1.0f, -0.5f),
                    false, false).ToTriangles();

            using XRMaterialFrameBuffer fbo = new(mat);
            fbo.SetRenderTargets((brdf, EFrameBufferAttachment.ColorAttachment0, 0, -1));

            using XRMesh data = XRMesh.Create(tris);
            using XRMeshRenderer quad = new(data, mat);
            BoundingRectangle region = new(IVector2.Zero, new IVector2((int)width, (int)height));

            //Now render the texture to the FBO using the quad
            fbo.BindForWriting();
            using (Engine.Rendering.State.PushRenderArea(region))
            {
                Engine.Rendering.State.Clear(EFrameBufferTextureType.Color);
                quad.Render(Matrix4x4.Identity);
            }
            fbo.UnbindFromWriting();
            return brdf;
        }

    }
}