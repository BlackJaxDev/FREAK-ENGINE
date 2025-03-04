﻿using Assimp;
using Assimp.Configs;
using Assimp.Unmanaged;
using Extensions;
using ImageMagick;
using System.Collections.Concurrent;
using System.Diagnostics;
using XREngine.Components.Scene.Mesh;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Models;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;
using XREngine.Scene.Transforms;
using AScene = Assimp.Scene;
using BlendMode = XREngine.Rendering.Models.Materials.BlendMode;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XREngine
{
    /// <summary>
    /// This class is used to import models from various formats using the Assimp library.
    /// Returns a SceneNode hierarchy populated with ModelComponents, and outputs generated materials and meshes.
    /// </summary>
    public class ModelImporter : IDisposable
    {
        protected ModelImporter(string path, bool async, Action? onCompleted, DelMaterialFactory? materialFactory)
        {
            _assimp = new AssimpContext();
            _path = path;
            _async = async;
            _onCompleted = onCompleted;
            _materialFactory = materialFactory ?? MaterialFactory;
        }

        private readonly ConcurrentDictionary<string, XRTexture2D> _texturePathCache = new();

        public XRMaterial MaterialFactory(string modelFilePath, string name, List<TextureSlot> textures, TextureFlags flags, ShadingMode mode, Dictionary<string, List<MaterialProperty>> properties)
        {
            //Random r = new();

            XRTexture[] textureList = new XRTexture[textures.Count];
            XRMaterial mat = new(textureList);
            Task.Run(() => Parallel.For(0, textures.Count, i => LoadTexture(modelFilePath, textures, textureList, i))).ContinueWith(x =>
            {
                for (int i = 0; i < textureList.Length; i++)
                {
                    XRTexture? tex = textureList[i];
                    if (tex is not null)
                        mat.Textures[i] = tex;
                }

                bool transp = textures.Any(x => (x.Flags & 0x2) != 0 || x.TextureType == TextureType.Opacity);
                bool normal = textures.Any(x => x.TextureType == TextureType.Normals);
                if (textureList.Length > 0)
                {
                    if (transp || textureList.Any(x => x is not null && x.HasAlphaChannel))
                    {
                        transp = true;
                        mat.Shaders.Add(ShaderHelper.UnlitTextureFragForward()!);
                    }
                    else
                    {
                        mat.Shaders.Add(ShaderHelper.TextureFragDeferred()!);
                        mat.Parameters =
                        [
                            new ShaderFloat(1.0f, "Opacity"),
                        new ShaderFloat(1.0f, "Specular"),
                        new ShaderFloat(0.9f, "Roughness"),
                        new ShaderFloat(0.0f, "Metallic"),
                        new ShaderFloat(1.0f, "IndexOfRefraction"),
                    ];
                    }
                }
                else
                {
                    //Show the material as magenta if no textures are present
                    mat.Shaders.Add(ShaderHelper.LitColorFragDeferred()!);
                    mat.Parameters =
                    [
                        new ShaderVector3(ColorF3.Magenta, "BaseColor"),
                    new ShaderFloat(1.0f, "Opacity"),
                    new ShaderFloat(1.0f, "Specular"),
                    new ShaderFloat(1.0f, "Roughness"),
                    new ShaderFloat(0.0f, "Metallic"),
                    new ShaderFloat(1.0f, "IndexOfRefraction"),
                ];
                }

                mat.RenderPass = transp ? (int)EDefaultRenderPass.TransparentForward : (int)EDefaultRenderPass.OpaqueDeferredLit;
                mat.Name = name;
                mat.RenderOptions = new RenderingParameters()
                {
                    CullMode = ECullMode.Back,
                    DepthTest = new DepthTest()
                    {
                        UpdateDepth = true,
                        Enabled = ERenderParamUsage.Enabled,
                        Function = EComparison.Less,
                    },
                    //LineWidth = 5.0f,
                    BlendModeAllDrawBuffers = transp ? BlendMode.EnabledTransparent() : BlendMode.Disabled(),
                };
            });

            return mat;
        }

        private void LoadTexture(string modelFilePath, List<TextureSlot> textures, XRTexture[] textureList, int i)
        {
            string path = textures[i].FilePath;
            if (string.IsNullOrWhiteSpace(path))
                return;

            path = path.Replace("/", "\\");
            bool rooted = Path.IsPathRooted(path);
            if (!rooted)
            {
                string? dir = Path.GetDirectoryName(modelFilePath);
                if (dir is not null)
                    path = Path.Combine(dir, path);
            }

            XRTexture2D TextureFactory(string x)
            {
                var tex = Engine.Assets.Load<XRTexture2D>(path);
                if (tex is null)
                {
                    //Debug.Out($"Failed to load texture: {path}");
                    tex = new XRTexture2D()
                    {
                        Name = Path.GetFileNameWithoutExtension(path),
                        MagFilter = ETexMagFilter.Linear,
                        MinFilter = ETexMinFilter.Linear,
                        UWrap = ETexWrapMode.Repeat,
                        VWrap = ETexWrapMode.Repeat,
                        AlphaAsTransparency = true,
                        AutoGenerateMipmaps = true,
                        Resizable = true,
                    };
                }
                else
                {
                    //Debug.Out($"Loaded texture: {path}");
                    tex.MagFilter = ETexMagFilter.Linear;
                    tex.MinFilter = ETexMinFilter.Linear;
                    tex.UWrap = ETexWrapMode.Repeat;
                    tex.VWrap = ETexWrapMode.Repeat;
                    tex.AlphaAsTransparency = true;
                    tex.AutoGenerateMipmaps = true;
                    tex.Resizable = false;
                    tex.SizedInternalFormat = ESizedInternalFormat.Rgba8;
                }
                return tex;
            }

            textureList[i] = _texturePathCache.GetOrAdd(path, TextureFactory);
        }

        public string SourceFilePath => _path;

        private readonly AssimpContext _assimp;
        private readonly string _path;
        private readonly bool _async;
        private readonly Action? _onCompleted;
        
        public delegate XRMaterial DelMaterialFactory(
            string modelFilePath,
            string name,
            List<TextureSlot> textures,
            TextureFlags flags,
            ShadingMode mode,
            Dictionary<string, List<MaterialProperty>> properties);

        private readonly DelMaterialFactory _materialFactory;

        private readonly ConcurrentDictionary<string, TextureSlot> _textureInfoCache = [];
        private readonly ConcurrentDictionary<string, MagickImage?> _textureCache = new();
        private readonly Dictionary<string, List<SceneNode>> _nodeCache = [];
        
        private readonly ConcurrentBag<XRMesh> _meshes = [];
        private readonly ConcurrentBag<XRMaterial> _materials = [];

        public static SceneNode? Import(
            string path,
            PostProcessSteps options,
            out IReadOnlyCollection<XRMaterial> materials,
            out IReadOnlyCollection<XRMesh> meshes,
            bool async,
            Action? onCompleted,
            DelMaterialFactory? materialFactory,
            SceneNode? parent,
            float scaleConversion = 1.0f,
            bool zUp = false)
        {
            using var importer = new ModelImporter(path, async, onCompleted, materialFactory);
            var node = importer.Import(options, true, false, scaleConversion, zUp, true);
            materials = importer._materials;
            meshes = importer._meshes;
            if (parent != null && node != null)
                parent.Transform.AddChild(node.Transform, false, true);
            return node;
        }
        public static async Task<(SceneNode? rootNode, IReadOnlyCollection<XRMaterial> materials, IReadOnlyCollection<XRMesh> meshes)> ImportAsync(
            string path,
            PostProcessSteps options,
            Action? onCompleted,
            DelMaterialFactory? materialFactory,
            SceneNode? parent,
            float scaleConversion = 1.0f,
            bool zUp = false)
            => await Task.Run(() =>
            {
                SceneNode? node = Import(path, options, out var materials, out var meshes, true, onCompleted, materialFactory, parent, scaleConversion, zUp);
                return (node, materials, meshes);
            });

        private readonly ConcurrentBag<Action> _meshProcessActions = [];

        private unsafe SceneNode? Import(
            PostProcessSteps options = PostProcessSteps.None,
            bool preservePivots = true,
            bool removeAssimpFBXNodes = true,
            float scaleConversion = 1.0f,
            bool zUp = false,
            bool multiThread = true)
        {
#if DEBUG
            Debug.Out($"Importing model: {SourceFilePath} with options: {options}");
            Stopwatch sw = new();
            sw.Start();
#endif
            float rotate = zUp ? -90.0f : 0.0f;
            _assimp.SetConfig(new BooleanPropertyConfig(AiConfigs.AI_CONFIG_IMPORT_FBX_PRESERVE_PIVOTS, preservePivots));
            //_assimp.SetConfig(new BooleanPropertyConfig(AiConfigs.AI_CONFIG_IMPORT_FBX_READ_ALL_MATERIALS, true));
            _assimp.SetConfig(new BooleanPropertyConfig(AiConfigs.AI_CONFIG_IMPORT_FBX_READ_MATERIALS, true));
            _assimp.SetConfig(new BooleanPropertyConfig(AiConfigs.AI_CONFIG_IMPORT_FBX_READ_TEXTURES, true));
            _assimp.SetConfig(new BooleanPropertyConfig(AiConfigs.AI_CONFIG_GLOB_MULTITHREADING, multiThread));

            _assimp.Scale = scaleConversion;
            _assimp.XAxisRotation = rotate;
            AScene scene = _assimp.ImportFile(SourceFilePath, options);

            if (scene is null || scene.SceneFlags == SceneFlags.Incomplete || scene.RootNode is null)
                return null;

            Debug.Out($"Loaded scene in {sw.ElapsedMilliseconds / 1000.0f} sec from {SourceFilePath} with options: {options}");
            SceneNode rootNode = new(Path.GetFileNameWithoutExtension(SourceFilePath));

            ProcessNode(true, scene.RootNode, scene, rootNode, Matrix4x4.Identity, null, null, removeAssimpFBXNodes);
            //Debug.Out(rootNode.PrintTree());

            //for (var i = 0; i < scene->MNumAnimations; i++)
            //{
            //    AAnimation* anim = scene->MAnimations[i];
            //}

#if DEBUG
            Debug.Out($"Model hierarchy processed in {sw.ElapsedMilliseconds / 1000.0f} sec.");
#endif

            if (_async)
            {
                void Complete(object o)
                {
#if DEBUG
                    sw.Stop();
                    Debug.Out($"Model imported asynchronously in {sw.ElapsedMilliseconds / 1000.0f} sec.");
#endif
                    _onCompleted?.Invoke();
                }
                Task.Run(ProcessMeshesParallel).ContinueWith(Complete);
            }
            else
            {
                ProcessMeshesParallel();
#if DEBUG
                sw.Stop();
                Debug.Out($"Model imported synchronously in {sw.ElapsedMilliseconds / 1000.0f} sec.");
#endif
                _onCompleted?.Invoke();
            }

            return rootNode;
        }

        //TODO: more extreme idea: allocate all initial meshes, and sequentially populate every mesh's buffers in parallel
        private ParallelLoopResult ProcessMeshesParallel()
            => Parallel.ForEach(_meshProcessActions, action => action());

        private void ProcessMeshesSequential()
        {
            foreach (var action in _meshProcessActions)
                action();
        }

        private void ProcessNode(
            bool rootNode,
            Node node,
            AScene scene,
            SceneNode parentSceneNode,
            Matrix4x4 invRootMatrix,
            TransformBase? rootTransform,
            Matrix4x4? fbxMatrixParent = null,
            bool removeAssimpFBXNodes = true)
        {
            SceneNode sceneNode;
            Matrix4x4? fbxMatrix = null;
            string name = node.Name;

            if (removeAssimpFBXNodes && !rootNode)
            {
                int assimpFBXMagic = name.IndexOf("_$AssimpFbx$");
                bool assimpFBXNode = assimpFBXMagic != -1;
                if (assimpFBXNode)
                {
                    Debug.Out($"Removing {name}");
                    name = name[..assimpFBXMagic];
                    bool affectsParent = parentSceneNode.Name?.StartsWith(name, StringComparison.InvariantCulture) ?? false;
                    if (affectsParent)
                    {
                        //Update parent transform
                        parentSceneNode.Transform.DeriveLocalMatrix(node.Transform.Transposed() * parentSceneNode.Transform.LocalMatrix);
                        parentSceneNode.Transform.RecalcLocal();
                        parentSceneNode.Transform.RecalcWorld(false);
                    }
                    else
                    {
                        fbxMatrix = node.Transform.Transposed();
                        if (fbxMatrixParent.HasValue)
                            fbxMatrix *= fbxMatrixParent.Value;
                    }
                    sceneNode = parentSceneNode;
                }
                else
                    sceneNode = CreateNode(node, parentSceneNode, fbxMatrixParent, true, name);
            }
            else
                sceneNode = CreateNode(node, parentSceneNode, fbxMatrixParent, false, name);

            if (rootNode)
                rootTransform = sceneNode.Transform;

            //if (node.MeshCount > 0)
            //    Debug.Out($"Node {name} has {node.MeshCount} meshes");

            EnqueueProcessMeshes(node, scene, sceneNode, invRootMatrix, rootTransform!);

            for (var i = 0; i < node.ChildCount; i++)
                ProcessNode(
                    false,
                    node.Children[i],
                    scene,
                    sceneNode,
                    invRootMatrix,
                    rootTransform,
                    fbxMatrix,
                    removeAssimpFBXNodes);
        }

        private SceneNode CreateNode(Node node, SceneNode parentSceneNode, Matrix4x4? fbxMatrixParent, bool removeAssimpFBXNodes, string name)
        {
            //Debug.Out($"Processing node: {name}");

            Matrix4x4 localTransform = node.Transform.Transposed();

            if (removeAssimpFBXNodes && fbxMatrixParent.HasValue)
                localTransform *= fbxMatrixParent.Value;

            SceneNode sceneNode = new(parentSceneNode, name);
            sceneNode.Transform.DeriveLocalMatrix(localTransform);
            sceneNode.Transform.RecalcLocal();
            sceneNode.Transform.RecalcWorld(false);

            if (_nodeCache.TryGetValue(name, out List<SceneNode>? nodes))
                nodes.Add(sceneNode);
            else
                _nodeCache.Add(name, [sceneNode]);

            return sceneNode;
        }

        private unsafe void EnqueueProcessMeshes(Node node, AScene scene, SceneNode sceneNode, Matrix4x4 invRootMatrix, TransformBase rootTransform)
        {
            int count = node.MeshCount;
            if (count == 0)
                return;

            _meshProcessActions.Add(() => ProcessMeshes(node, scene, sceneNode, invRootMatrix, rootTransform));
        }

        private unsafe void ProcessMeshes(Node node, AScene scene, SceneNode sceneNode, Matrix4x4 invRootMatrix, TransformBase rootTransform)
        {
            ModelComponent modelComponent = sceneNode.AddComponent<ModelComponent>()!;
            Model model = new();
            modelComponent.Name = node.Name;
            for (var i = 0; i < node.MeshCount; i++)
            {
                int meshIndex = node.MeshIndices[i];
                Mesh mesh = scene.Meshes[meshIndex];

                (XRMesh xrMesh, XRMaterial xrMaterial) = ProcessSubMesh(sceneNode.Transform, mesh, scene, invRootMatrix);

                _meshes.Add(xrMesh);
                _materials.Add(xrMaterial);

                model.Meshes.Add(new SubMesh(xrMesh, xrMaterial) { Name = mesh.Name, RootTransform = rootTransform });
            }

            modelComponent!.Model = model;
        }

        private unsafe (XRMesh mesh, XRMaterial material) ProcessSubMesh(
            TransformBase parentTransform,
            Mesh mesh,
            AScene scene,
            Matrix4x4 invRootMatrix)
        {
            //Debug.Out($"Processing mesh: {mesh->MName}");
            return (new(parentTransform, mesh, _assimp, _nodeCache, invRootMatrix), ProcessMaterial(mesh, scene));
        }

        private unsafe XRMaterial ProcessMaterial(Mesh mesh, AScene scene)
        {
            Material matInfo = scene.Materials[mesh.MaterialIndex];
            List<TextureSlot> textures = [];
            for (int i = 0; i < 22; ++i)
            {
                TextureType type = (TextureType)i;
                var maps = LoadMaterialTextures(matInfo, type);
                if (maps.Count > 0)
                    textures.AddRange(maps);
            }
            ReadProperties(matInfo, out string name, out TextureFlags flags, out ShadingMode mode, out var propDic);
            return _materialFactory(SourceFilePath, name, textures, flags, mode, propDic);
        }

        private static unsafe void ReadProperties(Material material, out string name, out TextureFlags flags, out ShadingMode shadingMode, out Dictionary<string, List<MaterialProperty>> properties)
        {
            var props = material.GetAllProperties();
            Dictionary<string, List<MaterialProperty>> dic = [];
            foreach (var prop in props)
            {
                if (!dic.TryGetValue(prop.Name, out List<MaterialProperty>? list))
                    dic.Add(prop.Name, list = []);
                list.Add(prop);
            }

            name = dic.TryGetValue(AI_MATKEY_NAME, out List<MaterialProperty>? nameList)
                ? nameList[0].GetStringValue() ?? AI_DEFAULT_MATERIAL_NAME
                : AI_DEFAULT_MATERIAL_NAME;

            flags = dic.TryGetValue(_AI_MATKEY_TEXFLAGS_BASE, out List<MaterialProperty>? flag) && flag[0].GetIntegerValue() is int f ? (TextureFlags)f : 0;
            shadingMode = dic.TryGetValue(AI_MATKEY_SHADING_MODEL, out List<MaterialProperty>? sm) && sm[0].GetIntegerValue() is int mode ? (ShadingMode)mode : ShadingMode.Flat;
            properties = dic;
        }

        const string AI_DEFAULT_MATERIAL_NAME = "DefaultMaterial";

        const string AI_MATKEY_BLEND_FUNC = "$mat.blend";
        const string AI_MATKEY_BUMPSCALING = "$mat.bumpscaling";
        const string AI_MATKEY_COLOR_AMBIENT = "$clr.ambient";
        const string AI_MATKEY_COLOR_DIFFUSE = "$clr.diffuse";
        const string AI_MATKEY_COLOR_EMISSIVE = "$clr.emissive";
        const string AI_MATKEY_COLOR_REFLECTIVE = "$clr.reflective";
        const string AI_MATKEY_COLOR_SPECULAR = "$clr.specular";
        const string AI_MATKEY_COLOR_TRANSPARENT = "$clr.transparent";
        const string AI_MATKEY_ENABLE_WIREFRAME = "$mat.wireframe";
        const string AI_MATKEY_GLOBAL_BACKGROUND_IMAGE = "?bg.global";
        const string AI_MATKEY_NAME = "?mat.name";
        const string AI_MATKEY_OPACITY = "$mat.opacity";
        const string AI_MATKEY_REFLECTIVITY = "$mat.reflectivity";
        const string AI_MATKEY_REFRACTI = "$mat.refracti";
        const string AI_MATKEY_SHADING_MODEL = "$mat.shadingm";
        const string AI_MATKEY_SHININESS = "$mat.shininess";
        const string AI_MATKEY_SHININESS_STRENGTH = "$mat.shinpercent";
        const string AI_MATKEY_TWOSIDED = "$mat.twosided";

        const string _AI_MATKEY_TEXTURE_BASE = "$tex.file";
        const string _AI_MATKEY_UVWSRC_BASE = "$tex.uvwsrc";
        const string _AI_MATKEY_TEXOP_BASE = "$tex.op";
        const string _AI_MATKEY_MAPPING_BASE = "$tex.mapping";
        const string _AI_MATKEY_TEXBLEND_BASE = "$tex.blend";
        const string _AI_MATKEY_MAPPINGMODE_U_BASE = "$tex.mapmodeu";
        const string _AI_MATKEY_MAPPINGMODE_V_BASE = "$tex.mapmodev";
        const string _AI_MATKEY_TEXMAP_AXIS_BASE = "$tex.mapaxis";
        const string _AI_MATKEY_UVTRANSFORM_BASE = "$tex.uvtrafo";
        const string _AI_MATKEY_TEXFLAGS_BASE = "$tex.flags";

        private unsafe List<TextureSlot> LoadMaterialTextures(Material mat, TextureType type)
        {
            List<TextureSlot> textures = [];
            var textureCount = mat.GetMaterialTextureCount(type);
            for (int i = 0; i < textureCount; i++)
            {
                if (!mat.GetMaterialTexture(type, i, out TextureSlot slot))
                    continue;

                string path = slot.FilePath;
                bool skip = false;
                foreach (var existingTexPath in _textureInfoCache.Keys)
                {
                    if (!string.Equals(existingTexPath, path, StringComparison.OrdinalIgnoreCase))
                        continue;

                    textures.Add(_textureInfoCache[existingTexPath]);
                    skip = true;
                    break;
                }
                if (!skip)
                {
                    textures.Add(slot);
                    _textureInfoCache.TryAdd(path, slot);
                }
            }
            return textures;
        }

        public void Dispose()
        {
            foreach (var tex in _textureCache.Values)
                tex?.Dispose();
            _textureCache.Clear();
            _textureInfoCache.Clear();
        }
    }
}