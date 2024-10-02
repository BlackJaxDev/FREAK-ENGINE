using ImageMagick;
using Silk.NET.Assimp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using XREngine.Components.Scene.Mesh;
using XREngine.Data;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Models;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;
using XREngine.Scene.Transforms;
using AScene = Silk.NET.Assimp.Scene;

namespace XREngine
{
    /// <summary>
    /// This class is used to import models from various formats using the Assimp library.
    /// Returns a SceneNode hierarchy populated with ModelComponents, and outputs generated materials and meshes.
    /// </summary>
    public unsafe class ModelImporter : IDisposable
    {
        protected ModelImporter(string path, bool async, Action? onCompleted)
        {
            _assimp = Assimp.GetApi();
            _path = path;
            _async = async;
            _onCompleted = onCompleted;
        }

        public string SourceFilePath => _path;

        private readonly Assimp _assimp;
        private readonly string _path;
        private readonly bool _async;
        private readonly Action? _onCompleted;

        private readonly ConcurrentDictionary<string, TextureInfo> _textureInfoCache = [];
        private readonly ConcurrentDictionary<string, MagickImage?> _textureCache = new();

        private readonly ConcurrentBag<XRMesh> _meshes = [];
        private readonly ConcurrentBag<XRMaterial> _materials = [];

        public static SceneNode? Import(
            string path,
            PostProcessSteps options,
            out IReadOnlyCollection<XRMaterial> materials,
            out IReadOnlyCollection<XRMesh> meshes,
            bool async,
            Action? onCompleted)
        {
            using var importer = new ModelImporter(path, async, onCompleted);
            var node = importer.Import(options);
            materials = importer._materials;
            meshes = importer._meshes;
            return node;
        }

        private readonly ConcurrentBag<Action> _meshProcessActions = [];

        private SceneNode? Import(PostProcessSteps options)
        {
#if DEBUG
            Debug.Out($"Importing model: {SourceFilePath} with options: {options}");
            Stopwatch sw = new();
            sw.Start();
#endif
            AScene* scene = _assimp.ImportFile(SourceFilePath, (uint)options);

            if (scene is null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode is null)
            {
                var error = _assimp.GetErrorStringS();
                Debug.Out($"Error loading model: {error}");
                return null;
            }

            SceneNode rootNode = new(Path.GetFileNameWithoutExtension(SourceFilePath));
            ProcessNode(scene->MRootNode, scene, Matrix4x4.Identity, rootNode);

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

        private unsafe void ProcessNode(Node* node, AScene* scene, Matrix4x4 parentWorldTransform, SceneNode parentSceneNode)
        {
            //Debug.Out($"Processing node: {node->MName}");

            Matrix4x4 localTransform = node->MTransformation;
            Matrix4x4 worldTransform = localTransform * parentWorldTransform;

            Transform tfm = [];
            tfm.DeriveWorldMatrix(localTransform);

            SceneNode sceneNode = new(parentSceneNode, node->MName, tfm);

            EnqueueProcessMeshes(node, scene, parentWorldTransform, sceneNode);

            for (var i = 0; i < node->MNumChildren; i++)
                ProcessNode(node->MChildren[i], scene, worldTransform, sceneNode);
        }

        private void EnqueueProcessMeshes(Node* node, AScene* scene, Matrix4x4 parentWorldTransform, SceneNode sceneNode)
        {
            uint count = node->MNumMeshes;
            if (count == 0)
                return;

            _meshProcessActions.Add(() => ProcessMeshes(node, scene, parentWorldTransform, sceneNode));
        }

        private void ProcessMeshes(Node* node, AScene* scene, Matrix4x4 parentWorldTransform, SceneNode sceneNode)
        {
            ModelComponent modelComponent = sceneNode.AddComponent<ModelComponent>()!;
            Model model = new();
            modelComponent.Name = node->MName;
            for (var i = 0; i < node->MNumMeshes; i++)
            {
                uint meshIndex = node->MMeshes[i];
                Mesh* mesh = scene->MMeshes[meshIndex];

                (XRMesh xrMesh, XRMaterial xrMaterial) = ProcessSubMesh(mesh, scene, parentWorldTransform);

                _meshes.Add(xrMesh);
                _materials.Add(xrMaterial);

                model.Meshes.Add(new SubMesh(xrMesh, xrMaterial) { Name = mesh->MName });
            }
            modelComponent!.Model = model;
        }

        private unsafe (XRMesh mesh, XRMaterial material) ProcessSubMesh(Mesh* mesh, AScene* scene, Matrix4x4 transform)
        {
            //Debug.Out($"Processing mesh: {mesh->MName}");

            Material* material = scene->MMaterials[mesh->MMaterialIndex];

            List<TextureInfo> textures = [];
            for (int i = 0; i < 22; ++i)
            {
                TextureType type = (TextureType)i;
                var maps = LoadMaterialTextures(material, type);
                if (maps.Count > 0)
                    textures.AddRange(maps);
            }

            XRMaterial mat =
                textures.Count > 0 ?
                new XRMaterial(new XRTexture[textures.Count], ShaderHelper.UnlitTextureFragForward()!) :
                XRMaterial.CreateUnlitColorMaterialForward(new ColorF4(1.0f, 1.0f, 0.0f, 1.0f));
            mat.RenderPass = (int)EDefaultRenderPass.OpaqueForward;

            //Loop properties
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

            mat.Name = dic.TryGetValue(AI_MATKEY_NAME, out List<object>? name)
                ? name[0]?.ToString() ?? AI_DEFAULT_MATERIAL_NAME
                : AI_DEFAULT_MATERIAL_NAME;

            TextureFlags texFlags = dic.TryGetValue(_AI_MATKEY_TEXFLAGS_BASE, out List<object>? flags) && flags[0] is int f ? (TextureFlags)f : 0;
            ShadingMode shadingModel = dic.TryGetValue(AI_MATKEY_SHADING_MODEL, out List<object>? sm) && sm[0] is int mode ? (ShadingMode)mode : ShadingMode.Flat;
            //TODO: switch default material based on shading model

            LoadTexturesAsynchronous(textures, mat);

            return (new(mesh, _assimp), mat);
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

        private void LoadTexturesSynchronous(List<TextureInfo> textures, XRMaterial mat)
        {
            for (int i = 0; i < textures.Count; i++)
            {
                TextureInfo info = textures[i];

                string path = info.path.Replace("/", "\\");
                bool rooted = Path.IsPathRooted(path);
                if (!rooted)
                {
                    string? dir = Path.GetDirectoryName(SourceFilePath);
                    if (dir is not null)
                        path = Path.Combine(dir, path);
                }

                var image = _textureCache.GetOrAdd(path, _ => System.IO.File.Exists(path) ? new(path) : null);
                XRTexture2D? texture = image is not null ? new(image) : new(1, 1);
                if (texture is null)
                    continue;

                texture.MagFilter = ETexMagFilter.Linear;
                texture.MinFilter = ETexMinFilter.Linear;
                texture.UWrap = ETexWrapMode.Repeat;
                texture.VWrap = ETexWrapMode.Repeat;
                texture.AlphaAsTransparency = true;
                texture.AutoGenerateMipmaps = true;
                texture.Signed = true;

                mat.Textures[i] = texture;
            }
        }
        private void LoadTexturesAsynchronous(List<TextureInfo> textures, XRMaterial mat)
        {
            Parallel.For(0, textures.Count, i =>
            {
                TextureInfo info = textures[i];

                string path = info.path.Replace("/", "\\");
                bool rooted = Path.IsPathRooted(path);
                if (!rooted)
                {
                    string? dir = Path.GetDirectoryName(SourceFilePath);
                    if (dir is not null)
                        path = Path.Combine(dir, path);
                }

                MagickImage? NewTexture(string _)
                    => System.IO.File.Exists(path) ? new(path) : null;

                var image = _textureCache.GetOrAdd(path, NewTexture);
                XRTexture2D texture = image is not null ? new(image) : new(1, 1);
                texture.Name = Path.GetFileNameWithoutExtension(path);
                texture.MagFilter = ETexMagFilter.Linear;
                texture.MinFilter = ETexMinFilter.Linear;
                texture.UWrap = ETexWrapMode.Repeat;
                texture.VWrap = ETexWrapMode.Repeat;
                texture.AlphaAsTransparency = true;
                texture.AutoGenerateMipmaps = false;
                texture.Signed = true;
                texture.Resizable = false;
                texture.SizedInternalFormat = ESizedInternalFormat.Rgba8;
                mat.Textures[i] = texture;

                //Engine.Assets.SaveTo(texture, Environment.SpecialFolder.DesktopDirectory, mat.Name ?? AI_DEFAULT_MATERIAL_NAME);
            });
        }

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

        private List<VertexPrimitive> CreatePrimitivesSequential(Vertex[] vertices, uint[][] indices)
        {
            List<Vertex> points = [];
            List<VertexLine> lines = [];
            List<VertexTriangle> triangles = [];
            for (uint i = 0; i < indices.Length; i++)
            {
                uint[] face = indices[i];
                switch (face.Length)
                {
                    case 0:
                        break;
                    case 1:
                        points.Add(vertices[face[0]]);
                        break;
                    case 2:
                        lines.Add(new VertexLine(vertices[face[0]], vertices[face[1]]));
                        break;
                    case 3:
                        triangles.Add(new VertexTriangle(vertices[face[0]], vertices[face[1]], vertices[face[2]]));
                        break;
                    case 4:
                        {
                            var quad = new VertexQuad(vertices[face[0]], vertices[face[1]], vertices[face[2]], vertices[face[3]]);
                            triangles.AddRange(quad.ToTriangles());
                            break;
                        }
                    default:
                        {
                            VertexPolygon polygon = new(face.Select(x => vertices[x]));
                            triangles.AddRange(polygon.ToTriangles());
                            break;
                        }
                }
            }
            return [.. points, .. lines, .. triangles];
        }
        private List<VertexPrimitive> CreatePrimitivesParallel(Vertex[] vertices, uint[][] indices)
        {
            ConcurrentBag<Vertex> points = [];
            ConcurrentBag<VertexLine> lines = [];
            ConcurrentBag<VertexTriangle> triangles = [];

            Parallel.For(0, indices.Length, i =>
            {
                uint[] face = indices[i];
                switch (face.Length)
                {
                    case 1:
                        points.Add(vertices[face[0]]);
                        break;
                    case 2:
                        lines.Add(new VertexLine(vertices[face[0]], vertices[face[1]]));
                        break;
                    case 3:
                        triangles.Add(new VertexTriangle(vertices[face[0]], vertices[face[1]], vertices[face[2]]));
                        break;
                    case 4:
                        {
                            VertexQuad quad = new(vertices[face[0]], vertices[face[1]], vertices[face[2]], vertices[face[3]]);
                            var tris = quad.ToTriangles();
                            foreach (var tri in tris)
                                triangles.Add(tri);
                        }
                        break;
                    default:
                        {
                            VertexPolygon polygon = new(face.Select(x => vertices[x]).ToList());
                            var tris = polygon.ToTriangles();
                            foreach (var tri in tris)
                                triangles.Add(tri);
                        }
                        break;
                }
            });

            return [.. points, .. lines, .. triangles];
        }

        /// <summary>
        /// Indices are stored in a jagged array, where each sub-array represents a face and contains the indices of the vertices that make up that face.
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        private unsafe uint[][] CollectIndices(Mesh* mesh)
        {
            uint[][] indices = new uint[mesh->MNumFaces][];
            for (uint i = 0; i < mesh->MNumFaces; i++)
            {
                Face face = mesh->MFaces[i];
                uint count = face.MNumIndices;
                indices[i] = new uint[count];
                for (uint j = 0; j < count; j++)
                    indices[i][j] = face.MIndices[j];
            }
            return indices;
        }

        private unsafe Vertex[] CollectVertices(Mesh* mesh)
        {
            Vertex[] vertices = new Vertex[mesh->MNumVertices];

            //Assimp can only handle up to 8 texture coordinates and colors per vertex
            bool[] hasTexCoords = new bool[8];
            bool[] hasColors = new bool[8];

            for (uint i = 0; i < mesh->MNumVertices; i++)
            {
                Vertex vertex = new(mesh->MVertices[i]);

                if (mesh->MNormals != null)
                    vertex.Normal = mesh->MNormals[i];

                if (mesh->MTangents != null)
                    vertex.Tangent = mesh->MTangents[i];

                //convert bitangent into normal or tangent depending on if we have only normals or tangents and bitangents, using cross
                if (mesh->MBitangents != null)
                {
                    if (mesh->MNormals != null && vertex.Tangent == null)
                        vertex.Tangent = Vector3.Cross(mesh->MNormals[i], mesh->MBitangents[i]);
                    else if (mesh->MTangents != null && vertex.Normal == null)
                        vertex.Normal = Vector3.Cross(mesh->MTangents[i], mesh->MBitangents[i]);
                }

                for (int x = 0; x < 8; ++x)
                {
                    var coord = mesh->MTextureCoords[x];
                    uint componentCount = mesh->MNumUVComponents[x]; //Usually just 2
                    if (coord != null)
                    {
                        Vector3 c = coord[i];
                        vertex.TextureCoordinateSets.Add(new Vector2(c.X, c.Y));
                        hasTexCoords[x] = true;
                    }
                }

                for (int x = 0; x < 8; ++x)
                {
                    var color = mesh->MColors[x];
                    if (color != null)
                    {
                        vertex.ColorSets.Add(color[i]);
                        hasColors[x] = true;
                    }
                }

                vertices[i] = vertex;
            }

            return vertices;
        }

        public void Dispose()
        {
            foreach (var tex in _textureCache.Values)
                tex?.Dispose();
            _textureCache.Clear();
            _textureInfoCache.Clear();
        }
    }

    internal record struct TextureInfo(string path, TextureMapping mapping, uint uvIndex, TextureOp op, TextureMapMode mapMode, uint flags)
    {
        public static implicit operator (string path, TextureMapping mapping, uint uvIndex, TextureOp op, TextureMapMode mapMode, uint flags)(TextureInfo value)
        {
            return (value.path, value.mapping, value.uvIndex, value.op, value.mapMode, value.flags);
        }

        public static implicit operator TextureInfo((string path, TextureMapping mapping, uint uvIndex, TextureOp op, TextureMapMode mapMode, uint flags) value)
        {
            return new TextureInfo(value.path, value.mapping, value.uvIndex, value.op, value.mapMode, value.flags);
        }
    }
}