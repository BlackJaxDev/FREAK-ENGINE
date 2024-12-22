using System.ComponentModel;
using XREngine.Core.Files;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.Models.Materials
{
    /// <summary>
    /// Contains parameters for rendering an object, such as blending and depth testing.
    /// </summary>
    public class RenderingParameters : XRAsset
    {
        private DepthTest _depthTest = new();
        private StencilTest _stencilTest = new();
        private Dictionary<uint, BlendMode>? _blendModesPerDrawBuffer;
        private float _lineWidth = AbstractRenderer.DefaultLineSize;
        private float _pointSize = AbstractRenderer.DefaultPointSize;
        private ECullMode _cullMode = ECullMode.Back;
        private EWinding _winding = EWinding.Clockwise;
        private bool _writeAlpha = true;
        private bool _writeBlue = true;
        private bool _writeGreen = true;
        private bool _writeRed = true;
        private EUniformRequirements _requiredEngineUniforms = EUniformRequirements.None;
        private BlendMode? _blendModeAllDrawBuffers;

        [Browsable(false)]
        public bool HasBlending => (BlendModesPerDrawBuffer?.Values.Any(x => x.Enabled == ERenderParamUsage.Enabled) ?? false) || BlendModeAllDrawBuffers?.Enabled == ERenderParamUsage.Enabled;

        public RenderingParameters() { }
        public RenderingParameters(bool defaultBlendEnabled)
        {
            if (!defaultBlendEnabled)
                return;

            BlendModeAllDrawBuffers = BlendMode.EnabledTransparent();
        }

        public EUniformRequirements RequiredEngineUniforms
        {
            get => _requiredEngineUniforms;
            set => SetField(ref _requiredEngineUniforms, value);
        }
        public bool WriteRed
        {
            get => _writeRed;
            set => SetField(ref _writeRed, value);
        }
        public bool WriteGreen
        {
            get => _writeGreen;
            set => SetField(ref _writeGreen, value);
        }
        public bool WriteBlue
        {
            get => _writeBlue;
            set => SetField(ref _writeBlue, value);
        }
        public bool WriteAlpha
        {
            get => _writeAlpha;
            set => SetField(ref _writeAlpha, value);
        }
        public EWinding Winding
        {
            get => _winding;
            set => SetField(ref _winding, value);
        }
        public ECullMode CullMode
        {
            get => _cullMode;
            set => SetField(ref _cullMode, value);
        }
        public float PointSize
        {
            get => _pointSize;
            set => SetField(ref _pointSize, value);
        }
        public float LineWidth
        {
            get => _lineWidth;
            set => SetField(ref _lineWidth, value);
        }
        public DepthTest DepthTest
        {
            get => _depthTest;
            set => SetField(ref _depthTest, value);
        }
        public StencilTest StencilTest
        {
            get => _stencilTest;
            set => SetField(ref _stencilTest, value);
        }
        public Dictionary<uint, BlendMode>? BlendModesPerDrawBuffer
        {
            get => _blendModesPerDrawBuffer;
            set => SetField(ref _blendModesPerDrawBuffer, value);
        }
        public BlendMode? BlendModeAllDrawBuffers
        {
            get => _blendModeAllDrawBuffers;
            set => SetField(ref _blendModeAllDrawBuffers, value);
        }
    }
}
