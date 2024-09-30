using ImageMagick;
using Silk.NET.Assimp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using XREngine.Components.Scene.Mesh;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Models;
using XREngine.Scene;
using XREngine.Scene.Transforms;
using AScene = Silk.NET.Assimp.Scene;

namespace XREngine
{
    public unsafe class ModelImporter : IDisposable
    {
        protected ModelImporter(string path)
        {
            _assimp = Assimp.GetApi();
            _path = path;
        }

        private readonly Assimp _assimp;
        private readonly string _path;
        private readonly ConcurrentBag<XRMesh> _meshes = [];
        private readonly ConcurrentBag<XRMaterial> _materials = [];
        private readonly ConcurrentDictionary<string, TextureInfo> _textureInfoCache = [];
        private readonly ConcurrentDictionary<string, MagickImage?> _textureCache = new();

        public string SourceFilePath => _path;

        public static SceneNode? Import(string path, PostProcessSteps options)
        {
            using var importer = new ModelImporter(path);
            return importer.Import(options);
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

            Task.Run(() => Parallel.ForEach(_meshProcessActions, action => action()));

            for (int i = 0; i < scene->MNumSkeletons; i++)
            {
                var skel = scene->MSkeletons[i];
                for (int j = 0; j < skel->MNumBones; j++)
                {
                    var bone = skel->MBones[j];
                    var index = 0;
                    for (int k = 0; k < skel->MNumBones; k++)
                    {
                        //if (skel->MBones[k]->MName == name)
                        //{
                        //    index = k;
                        //    break;
                        //}
                    }

                    //Meshes[0].AddBone(index, bone->MOffsetMatrix);
                }
            }
#if DEBUG
            sw.Stop();
            Debug.Out($"Model imported in {sw.ElapsedMilliseconds / 1000.0f} sec.");
#endif
            return rootNode;
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


            XRTexture[] xrTextures = new XRTexture[textures.Count];
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

                xrTextures[i] = texture;
            }

            XRMaterial xrMaterial =
                xrTextures.Length > 0 && xrTextures[0] is XRTexture2D tex ?
                XRMaterial.CreateUnlitTextureMaterialForward(tex) :
                XRMaterial.CreateUnlitColorMaterialForward(new ColorF4(1.0f, 1.0f, 0.0f, 1.0f));

            xrMaterial.RenderPass = (int)EDefaultRenderPass.OpaqueForward;
            xrMaterial.Textures.AddRange(xrTextures);

            return (new(mesh, _assimp), xrMaterial);
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