using System.Numerics;
using XREngine.Components.Lights;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;

namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_LightCombinePass(ViewportRenderCommandContainer pipeline) : ViewportRenderCommand(pipeline)
    {
        public string AlbedoOpacityTexture { get; set; } = "AlbedoOpacityTexture";
        public string NormalTexture { get; set; } = "NormalTexture";
        public string RMSITexture { get; set; } = "RMSITexture";
        public string DepthViewTexture { get; set; } = "DepthViewTexture";

        public void SetOptions(string albedoOpacityTexture, string normalTexture, string rmsiTexture, string depthViewTexture)
        {
            AlbedoOpacityTexture = albedoOpacityTexture;
            NormalTexture = normalTexture;
            RMSITexture = rmsiTexture;
            DepthViewTexture = depthViewTexture;
        }

        private XRTexture2D? _albedoOpacityTextureCache = null;
        private XRTexture2D? _normalTextureCache = null;
        private XRTexture2D? _rmsiTextureCache = null;
        private XRTexture2DView? _depthViewTextureCache = null;

        public XRMeshRenderer? PointLightRenderer { get; private set; }
        public XRMeshRenderer? SpotLightRenderer { get; private set; }
        public XRMeshRenderer? DirectionalLightRenderer { get; private set; }

        protected override void Execute()
        {
            if (Pipeline.State.MainScene is not VisualScene3D scene)
                return;

            var albOpacTex = Pipeline.GetTexture<XRTexture2D>(AlbedoOpacityTexture);
            var normTex = Pipeline.GetTexture<XRTexture2D>(NormalTexture);
            var rmsiTex = Pipeline.GetTexture<XRTexture2D>(RMSITexture);
            var depthViewTex = Pipeline.GetTexture<XRTexture2DView>(DepthViewTexture);
            if (albOpacTex is null || normTex is null || rmsiTex is null || depthViewTex is null)
                throw new Exception("One or more required textures are missing.");

            if (_albedoOpacityTextureCache != albOpacTex ||
                _normalTextureCache != normTex ||
                _rmsiTextureCache != rmsiTex ||
                _depthViewTextureCache != depthViewTex)
            {
                _albedoOpacityTextureCache = albOpacTex;
                _normalTextureCache = normTex;
                _rmsiTextureCache = rmsiTex;
                _depthViewTextureCache = depthViewTex;
                CreateLightRenderers(albOpacTex, normTex, rmsiTex, depthViewTex);
            }

            var lights = scene.Lights;

            foreach (PointLightComponent c in lights.PointLights)
                RenderPointLight(c);

            foreach (SpotLightComponent c in lights.SpotLights)
                RenderSpotLight(c);

            foreach (DirectionalLightComponent c in lights.DirectionalLights)
                RenderDirLight(c);
        }

        private void RenderDirLight(DirectionalLightComponent c)
          => RenderLight(DirectionalLightRenderer!, c);
        private void RenderPointLight(PointLightComponent c)
            => RenderLight(PointLightRenderer!, c);
        private void RenderSpotLight(SpotLightComponent c)
            => RenderLight(SpotLightRenderer!, c);

        private LightComponent? _currentLightComponent;

        private void RenderLight(XRMeshRenderer renderer, LightComponent comp)
        {
            _currentLightComponent = comp;
            renderer.Render(comp.LightMatrix);
            _currentLightComponent = null;
        }
        private void LightManager_SettingUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
        {
            if (_currentLightComponent is null)
                return;

            Pipeline.State.SceneCamera?.PostProcessing?.Shadows.SetUniforms(materialProgram);
            _currentLightComponent.SetShadowUniforms(materialProgram);
            _currentLightComponent.SetUniforms(materialProgram);
        }

        private void CreateLightRenderers(
            XRTexture2D albOpacTex,
            XRTexture2D normTex,
            XRTexture2D rmsiTex,
            XRTexture2DView depthViewTex)
        {
            XRTexture[] lightRefs =
            [
                albOpacTex,
                normTex,
                rmsiTex,
                depthViewTex,
                //shadow map texture
            ];

            XRShader pointLightShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "DeferredLightingPoint.fs"), EShaderType.Fragment);
            XRShader spotLightShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "DeferredLightingSpot.fs"), EShaderType.Fragment);
            XRShader dirLightShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "DeferredLightingDir.fs"), EShaderType.Fragment);

            RenderingParameters additiveRenderParams = GetAdditiveParameters();
            XRMaterial pointLightMat = new(lightRefs, pointLightShader) { RenderOptions = additiveRenderParams };
            XRMaterial spotLightMat = new(lightRefs, spotLightShader) { RenderOptions = additiveRenderParams };
            XRMaterial dirLightMat = new(lightRefs, dirLightShader) { RenderOptions = additiveRenderParams };

            XRMesh pointLightMesh = XRMesh.Shapes.SolidSphere(Vector3.Zero, 1.0f, 20u);
            XRMesh spotLightMesh = XRMesh.Shapes.SolidCone(Vector3.Zero, Vector3.UnitZ, 1.0f, 1.0f, 32, true);
            XRMesh dirLightMesh = XRMesh.Shapes.SolidBox(new Vector3(-0.5f), new Vector3(0.5f));

            PointLightRenderer = new XRMeshRenderer(pointLightMesh, pointLightMat);
            PointLightRenderer.SettingUniforms += LightManager_SettingUniforms;

            SpotLightRenderer = new XRMeshRenderer(spotLightMesh, spotLightMat);
            SpotLightRenderer.SettingUniforms += LightManager_SettingUniforms;

            DirectionalLightRenderer = new XRMeshRenderer(dirLightMesh, dirLightMat);
            DirectionalLightRenderer.SettingUniforms += LightManager_SettingUniforms;
        }

        private static RenderingParameters GetAdditiveParameters()
        {
            RenderingParameters additiveRenderParams = new()
            {
                //Render only the backside so that the light still shows if the camera is inside of the volume
                //and the light does not add itself twice for the front and back faces.
                CullMode = ECulling.Front,
                RequiredEngineUniforms = EUniformRequirements.Camera,
                BlendMode = new()
                {
                    //Add the previous and current light colors together using FuncAdd with each mesh render
                    Enabled = ERenderParamUsage.Enabled,
                    RgbDstFactor = EBlendingFactor.One,
                    AlphaDstFactor = EBlendingFactor.One,
                    RgbSrcFactor = EBlendingFactor.One,
                    AlphaSrcFactor = EBlendingFactor.One,
                    RgbEquation = EBlendEquationMode.FuncAdd,
                    AlphaEquation = EBlendEquationMode.FuncAdd,
                },
                DepthTest = new()
                {
                    Enabled = ERenderParamUsage.Disabled,
                }
            };
            return additiveRenderParams;
        }
    }
}
