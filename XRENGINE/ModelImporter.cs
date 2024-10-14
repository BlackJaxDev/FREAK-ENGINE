using Extensions;
using ImageMagick;
using Silk.NET.Assimp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using XREngine.Components.Scene.Mesh;
using XREngine.Data;
using XREngine.Data.Core;
using XREngine.Rendering;
using XREngine.Rendering.Models;
using XREngine.Scene;
using XREngine.Scene.Transforms;
using AScene = Silk.NET.Assimp.Scene;

namespace XREngine
{
    /// <summary>
    /// This class is used to import models from various formats using the Assimp library.
    /// Returns a SceneNode hierarchy populated with ModelComponents, and outputs generated materials and meshes.
    /// </summary>
    public class ModelImporter : IDisposable
    {
        protected ModelImporter(string path, bool async, Action? onCompleted, DelMaterialFactory materialFactory)
        {
            _assimp = Assimp.GetApi();
            _path = path;
            _async = async;
            _onCompleted = onCompleted;
            _materialFactory = materialFactory;
        }

        public string SourceFilePath => _path;

        private readonly Assimp _assimp;
        private readonly string _path;
        private readonly bool _async;
        private readonly Action? _onCompleted;
        
        public delegate XRMaterial DelMaterialFactory(string modelFilePath, string name, List<TextureInfo> textures, TextureFlags flags, ShadingMode mode, Dictionary<string, List<object>> properties);

        private readonly DelMaterialFactory _materialFactory;

        private readonly ConcurrentDictionary<string, TextureInfo> _textureInfoCache = [];
        private readonly ConcurrentDictionary<string, MagickImage?> _textureCache = new();
        private readonly Dictionary<string, List<SceneNode>> _nodeCache = [];
        
        private readonly ConcurrentBag<XRMesh> _meshes = [];
        private readonly ConcurrentBag<XRMaterial> _materials = [];
        public Matrix4x4 ScaleConversionMatrix { get; private set; } = Matrix4x4.Identity;
        public Matrix4x4 CoordinateConversionMatrix { get; private set; } = Matrix4x4.Identity;

        public static SceneNode? Import(
            string path,
            PostProcessSteps options,
            out IReadOnlyCollection<XRMaterial> materials,
            out IReadOnlyCollection<XRMesh> meshes,
            bool async,
            Action? onCompleted,
            DelMaterialFactory materialFactory,
            SceneNode? parent)
        {
            using var importer = new ModelImporter(path, async, onCompleted, materialFactory);
            var node = importer.Import(options);
            materials = importer._materials;
            meshes = importer._meshes;
            if (parent != null && node != null)
            {
                lock (parent.Transform.Children)
                {
                    parent.Transform.Children.Add(node.Transform);
                }
            }
            return node;
        }
        public static async Task<(SceneNode? rootNode, IReadOnlyCollection<XRMaterial> materials, IReadOnlyCollection<XRMesh> meshes)> ImportAsync(
            string path,
            PostProcessSteps options,
            Action? onCompleted,
            DelMaterialFactory materialFactory,
            SceneNode? parent)
        {
            var result = await Task.Run(() =>
            {
                SceneNode? node = Import(path, options, out var materials, out var meshes, true, onCompleted, materialFactory, parent);
                return (node, materials, meshes);
            });
            return result;
        }

        private readonly ConcurrentBag<Action> _meshProcessActions = [];

        private unsafe SceneNode? Import(PostProcessSteps options)
        {
#if DEBUG
            Debug.Out($"Importing model: {SourceFilePath} with options: {options}");
            Stopwatch sw = new();
            sw.Start();
#endif
            //float inchesToMeters = 0.0254f;
            //float rotate = -90.0f;
            float inchesToMeters = 1.0f;
            float rotate = 0.0f;

            ScaleConversionMatrix = Matrix4x4.CreateScale(inchesToMeters);
            CoordinateConversionMatrix = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, XRMath.DegToRad(rotate));
            AScene* scene = _assimp.ImportFile(SourceFilePath, (uint)options);

            if (scene is null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode is null)
            {
                var error = _assimp.GetErrorStringS();
                Debug.Out($"Error loading model: {error}");
                return null;
            }

            SceneNode rootNode = new(Path.GetFileNameWithoutExtension(SourceFilePath));
            ProcessNode(scene->MRootNode, scene, rootNode, true);

            //Debug.Out(rootNode.PrintTree());

            //for (var i = 0; i < scene->MNumAnimations; i++)
            //{
            //    AAnimation* anim = scene->MAnimations[i];
            //}

            for (var i = 0; i < scene->MNumSkeletons; i++)
            {
                Skeleton* skeleton = scene->MSkeletons[i];
                string name = skeleton->MName.ToString();
                Debug.Out($"Reading skeleton {name}");
                for (var j = 0; j < skeleton->MNumBones; j++)
                {
                    SkeletonBone* bone = skeleton->MBones[j];
                    int parentIndex = bone->MParent;
                    SkeletonBone* parent = parentIndex >= 0 && parentIndex < skeleton->MNumBones ? skeleton->MBones[parentIndex] : null;

                }
            }

#if DEBUG
            Debug.Out($"Model hierarchy processed in {sw.ElapsedMilliseconds / 1000.0f} sec.");
#endif

            if (_async)
            {
                void Complete(Task<ParallelLoopResult> x)
                {
#if DEBUG
                    sw.Stop();
                    Debug.Out($"Model imported asynchronously in {sw.ElapsedMilliseconds / 1000.0f} sec.");
#endif
                    _assimp.FreeScene(scene);
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
                _assimp.FreeScene(scene);
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

        private unsafe void ProcessNode(Node* node, AScene* scene, SceneNode parentSceneNode, bool root = false)
        {
            string name = node->MName;
            //Debug.Out($"Processing node: {name}");
            Matrix4x4 localTransform = node->MTransformation.Transposed();
            if (root)
                localTransform *= ScaleConversionMatrix * CoordinateConversionMatrix;
            Transform tfm = [];
            tfm.DeriveLocalMatrix(localTransform);
            SceneNode sceneNode = new(parentSceneNode, name, tfm);
            if (!_nodeCache.TryGetValue(name, out List<SceneNode>? nodes))
                _nodeCache.Add(name, nodes = []);
            nodes.Add(sceneNode);
            EnqueueProcessMeshes(node, scene, sceneNode);
            for (var i = 0; i < node->MNumChildren; i++)
                ProcessNode(node->MChildren[i], scene, sceneNode);
        }

        private unsafe void EnqueueProcessMeshes(Node* node, AScene* scene, SceneNode sceneNode)
        {
            uint count = node->MNumMeshes;
            if (count == 0)
                return;

            _meshProcessActions.Add(() => ProcessMeshes(node, scene, sceneNode));
        }

        private unsafe void ProcessMeshes(Node* node, AScene* scene, SceneNode sceneNode)
        {
            ModelComponent modelComponent = sceneNode.AddComponent<ModelComponent>()!;
            Model model = new();
            modelComponent.Name = node->MName;
            for (var i = 0; i < node->MNumMeshes; i++)
            {
                uint meshIndex = node->MMeshes[i];
                Mesh* mesh = scene->MMeshes[meshIndex];

                (XRMesh xrMesh, XRMaterial xrMaterial) = ProcessSubMesh(sceneNode.Transform, mesh, scene);

                _meshes.Add(xrMesh);
                _materials.Add(xrMaterial);

                model.Meshes.Add(new SubMesh(xrMesh, xrMaterial) { Name = mesh->MName });
            }
            modelComponent!.Model = model;
        }

        private unsafe (XRMesh mesh, XRMaterial material) ProcessSubMesh(
            TransformBase transform,
            Mesh* mesh,
            AScene* scene)
        {
            //Debug.Out($"Processing mesh: {mesh->MName}");
            return (new(transform, mesh, _assimp, _nodeCache, ScaleConversionMatrix, CoordinateConversionMatrix), ProcessMaterial(mesh, scene));
        }

        private unsafe XRMaterial ProcessMaterial(Mesh* mesh, AScene* scene)
        {
            Material* matInfo = scene->MMaterials[mesh->MMaterialIndex];
            List<TextureInfo> textures = [];
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

        private static unsafe void ReadProperties(Material* material, out string name, out TextureFlags flags, out ShadingMode shadingMode, out Dictionary<string, List<object>> properties)
        {
            Dictionary<string, List<object>> dic = [];
            void GetOrAdd(string key, object value)
            {
                if (!dic.TryGetValue(key, out List<object>? v))
                    dic.Add(key, v = []);
                v.Add(value);
            }
            for (uint i = 0; i < material->MNumProperties; ++i)
            {
                var prop = material->MProperties[i];
                string key = prop->MKey.AsString;
                uint index = prop->MIndex;
                uint dataLength = prop->MDataLength;
                uint semantic = prop->MSemantic;
                byte* data = prop->MData;
                switch (prop->MType)
                {
                    case PropertyTypeInfo.Float:
                        GetOrAdd(key, *(float*)data);
                        break;
                    case PropertyTypeInfo.Double:
                        GetOrAdd(key, *(double*)data);
                        break;
                    case PropertyTypeInfo.String:
                        int length = *(int*)data;
                        string str = Encoding.UTF8.GetString(data + 4, length);
                        GetOrAdd(key, str);
                        break;
                    case PropertyTypeInfo.Integer:
                        GetOrAdd(key, *(int*)data);
                        break;
                    case PropertyTypeInfo.Buffer:
                        GetOrAdd(key, new DataSource(data, dataLength, true));
                        break;
                }
            }

            name = dic.TryGetValue(AI_MATKEY_NAME, out List<object>? nameList)
                ? nameList[0]?.ToString() ?? AI_DEFAULT_MATERIAL_NAME
                : AI_DEFAULT_MATERIAL_NAME;

            flags = dic.TryGetValue(_AI_MATKEY_TEXFLAGS_BASE, out List<object>? flag) && flag[0] is int f ? (TextureFlags)f : 0;
            shadingMode = dic.TryGetValue(AI_MATKEY_SHADING_MODEL, out List<object>? sm) && sm[0] is int mode ? (ShadingMode)mode : ShadingMode.Flat;
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

        private unsafe List<TextureInfo> LoadMaterialTextures(Material* mat, TextureType type)
        {
            List<TextureInfo> textures = [];
            var textureCount = _assimp.GetMaterialTextureCount(mat, type);
            for (uint i = 0; i < textureCount; i++)
            {
                AssimpString pathPtr;
                TextureMapping mapping;
                uint uvIndex;
                float blend;
                TextureOp operation;
                TextureMapMode mapMode;
                uint flags;

                var result = _assimp.GetMaterialTexture(mat, type, i, &pathPtr, &mapping, &uvIndex, &blend, &operation, &mapMode, &flags);
                if (result != Return.Success)
                    continue;

                string path = pathPtr.AsString;
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
                    var info = (path, mapping, uvIndex, operation, mapMode, flags);
                    textures.Add(info);
                    _textureInfoCache.TryAdd(path, info);
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

    public record struct TextureInfo(string path, TextureMapping mapping, uint uvIndex, TextureOp op, TextureMapMode mapMode, uint flags)
    {
        public static implicit operator (string path, TextureMapping mapping, uint uvIndex, TextureOp op, TextureMapMode mapMode, uint flags)(TextureInfo value)
            => (value.path, value.mapping, value.uvIndex, value.op, value.mapMode, value.flags);

        public static implicit operator TextureInfo((string path, TextureMapping mapping, uint uvIndex, TextureOp op, TextureMapMode mapMode, uint flags) value)
            => new(value.path, value.mapping, value.uvIndex, value.op, value.mapMode, value.flags);
    }
}