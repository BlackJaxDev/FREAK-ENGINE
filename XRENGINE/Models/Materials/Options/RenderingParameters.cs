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
        private AlphaTest _alphaTest = new();
        private DepthTest _depthTest = new();
        private StencilTest _stencilTest = new();
        private BlendMode _blendMode = new();
        private EUniformRequirements _uniformRequirements;
        private float _lineWidth = AbstractRenderer.DefaultLineSize;
        private float _pointSize = AbstractRenderer.DefaultPointSize;
        private ECulling _cullMode = ECulling.Back;
        private EWinding _winding = EWinding.Clockwise;
        private bool _writeAlpha = true;
        private bool _writeBlue = true;
        private bool _writeGreen = true;
        private bool _writeRed = true;
        private EUniformRequirements _requiredEngineUniforms = EUniformRequirements.None;

        [Browsable(false)]
        public bool HasTransparency => BlendMode.Enabled == ERenderParamUsage.Enabled;

        public RenderingParameters() { }
        public RenderingParameters(bool defaultBlendEnabled, float? defaultAlphaTestDiscardMax)
        {
            if (defaultBlendEnabled)
            {
                BlendMode.Enabled = ERenderParamUsage.Enabled;
                BlendMode.RgbSrcFactor = EBlendingFactor.SrcAlpha;
                BlendMode.AlphaSrcFactor = EBlendingFactor.SrcAlpha;
                BlendMode.RgbDstFactor = EBlendingFactor.OneMinusSrcAlpha;
                BlendMode.AlphaDstFactor = EBlendingFactor.OneMinusSrcAlpha;
                BlendMode.RgbEquation = EBlendEquationMode.FuncAdd;
                BlendMode.AlphaEquation = EBlendEquationMode.FuncAdd;
            }
            if (defaultAlphaTestDiscardMax != null)
            {
                AlphaTest.Enabled = ERenderParamUsage.Enabled;
                AlphaTest.Ref = defaultAlphaTestDiscardMax.Value;
                AlphaTest.Comp = EComparison.Lequal;
            }
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
        public ECulling CullMode
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
        public AlphaTest AlphaTest
        {
            get => _alphaTest;
            set => SetField(ref _alphaTest, value);
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
        public BlendMode BlendMode
        {
            get => _blendMode;
            set => SetField(ref _blendMode, value);
        }
        public EUniformRequirements UniformRequirements
        {
            get => _uniformRequirements;
            set => SetField(ref _uniformRequirements, value);
        }
    }
}
