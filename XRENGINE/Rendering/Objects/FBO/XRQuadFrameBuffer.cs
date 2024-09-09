using System.Numerics;
using XREngine.Data.Rendering;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering
{
    /// <summary>
    /// Represents a framebuffer, material, quad (actually a giant triangle), and camera to render with.
    /// </summary>
    public class XRQuadFrameBuffer : XRMaterialFrameBuffer
    {
        /// <summary>
        /// Use to set uniforms to the program containing the fragment shader.
        /// </summary>
        public event DelSetUniforms? SettingUniforms;

        /// <summary>
        /// 2D camera for capturing the screen rendered to the framebuffer.
        /// </summary>
        private readonly XRCamera _quadCamera;

        public XRMeshRenderer FullScreenMesh { get; }

        private static XRMesh Mesh(bool useTriangle)
        {
            if (useTriangle)
            {
                VertexTriangle triangle = new(
                    new Vector3(0.0f, 0.0f, 0.0f),
                    new Vector3(2.0f, 0.0f, 0.0f),
                    new Vector3(0.0f, 2.0f, 0.0f));

                return XRMesh.Create(triangle);
            }
            else
            {
                VertexTriangle triangle1 = new(
                    new Vector3(0.0f, 0.0f, 0.0f),
                    new Vector3(1.0f, 0.0f, 0.0f),
                    new Vector3(0.0f, 1.0f, 0.0f));

                VertexTriangle triangle2 = new(
                    new Vector3(0.0f, 1.0f, 0.0f),
                    new Vector3(1.0f, 0.0f, 0.0f),
                    new Vector3(1.0f, 1.0f, 0.0f));

                return XRMesh.Create(triangle1, triangle2);
            }
        }

        /// <summary>
        /// Renders a material to the screen using a fullscreen orthographic quad.
        /// </summary>
        /// <param name="mat">The material containing textures to render to this fullscreen quad.</param>
        public XRQuadFrameBuffer(XRMaterial mat, bool useTriangle = false) : base(mat)
        {
            FullScreenMesh = new XRMeshRenderer(Mesh(useTriangle), Material);
            FullScreenMesh.SettingUniforms += SetUniforms;

            _quadCamera = new XRCamera(new Transform(), new XROrthographicCameraParameters(1.0f, 1.0f, -0.5f, 0.5f));
        }

        public XRQuadFrameBuffer(
            XRMaterial material,
            bool useTriangle,
            params (IFrameBufferAttachement Target, EFrameBufferAttachment Attachment, int MipLevel, int LayerIndex)[]? targets)
            : this(material, useTriangle) => SetRenderTargets(targets);

        private void SetUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
            => SettingUniforms?.Invoke(materialProgram);

        /// <summary>
        /// Renders the FBO to the entire region set by Engine.Rendering.State.PushRenderArea().
        /// </summary>
        public void Render(XRFrameBuffer? target = null)
        {
            target?.BindForWriting();
            using (Engine.Rendering.State.PushRenderingCamera(_quadCamera))
                FullScreenMesh.Render();
            target?.UnbindFromWriting();
        }
    }
}
