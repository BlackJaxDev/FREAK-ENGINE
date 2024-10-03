using System.Collections;
using System.Numerics;
using XREngine.Data.Rendering;
using XREngine.Data.Transforms.Rotations;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering
{
    public class XRCubeFrameBuffer : XRMaterialFrameBuffer, IEnumerable<XRCamera>
    {
        public event DelSetUniforms? SettingUniforms;

        private readonly XRMeshRenderer _cube;
        public TransformBase Transform
        {
            get => _transform;
            set => SetField(ref _transform, value);
        }

        public IReadOnlyList<XRCamera> Cameras => _cameras;

        private readonly XRCamera[] _cameras;
        private readonly Transform[] _cameraTransforms;
        private TransformBase _transform;

        public XRCubeFrameBuffer(XRMaterial? mat, TransformBase? transform = null, float nearZ = 1.0f, float farZ = 1000.0f, bool perspectiveCameras = true) : base(mat)
        {
            _transform = transform ?? new Transform();
            float middle = (nearZ + farZ) * 0.5f;

            _cube = new XRMeshRenderer(XRMesh.Shapes.SolidBox(new Vector3(-middle), new Vector3(middle), true), Material);
            _cube.SettingUniforms += SetUniforms;

            _cameraTransforms = new Transform[6];
            _cameras = new XRCamera[6];

            Rotator[] rotations =
            [
                new(  0.0f,  90.0f,   0.0f), //+X
                new(  0.0f, -90.0f,   0.0f), //-X
                new(-90.0f,   0.0f, 180.0f), //+Y
                new( 90.0f,   0.0f, 180.0f), //-Y
                new(  0.0f, 180.0f,   0.0f), //+Z
                new(  0.0f,   0.0f,   0.0f), //-Z
            ];

            XRCameraParameters p = perspectiveCameras
                ? new XRPerspectiveCameraParameters(nearZ, farZ, 90.0f, 1.0f)
                : new XROrthographicCameraParameters(2.0f, 2.0f, nearZ, farZ);

            for (int i = 0; i < 6; ++i)
                _cameras[i] = new(_cameraTransforms[i] = new Transform()
                {
                    Rotation = rotations[i].ToQuaternion(),
                    Parent = Transform
                }, p);
        }

        private void SetUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
            => SettingUniforms?.Invoke(materialProgram);

        /// <summary>
        /// Renders the one side of the FBO to the entire region set by Engine.Rendering.State.PushRenderArea.
        /// </summary>
        public void RenderFullscreen(ECubemapFace face)
        {
            var state = Engine.Rendering.State.PipelineState;
            if (state != null)
            {
                using (state.PushRenderingCamera(_cameras[(int)face]))
                {
                    _cube.Render(Matrix4x4.Identity);
                }
            }
            else
            {
                _cube.Render(Matrix4x4.Identity);
            }
        }

        public IEnumerator<XRCamera> GetEnumerator()
            => ((IEnumerable<XRCamera>)_cameras).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => _cameras.GetEnumerator();
    }
}
