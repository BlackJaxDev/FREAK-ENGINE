using Silk.NET.Assimp;
using System.Collections.Concurrent;
using System.Numerics;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Scene;
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

        public List<XRMesh> Meshes { get; protected set; } = [];
        public string Path => _path;

        public static ModelImporter Import(string path)
        {
            var importer = new ModelImporter(path);
            //importer.Import();
            return importer;
        }

        private SceneNode Import(XRWorldInstance world, PostProcessSteps postProcess)
        {
            var scene = _assimp.ImportFile(Path, (uint)postProcess);

            if (scene is null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode is null)
            {
                var error = _assimp.GetErrorStringS();
                throw new Exception(error);
            }

            //SceneNode rootNode = new(world, System.IO.Path.GetFileNameWithoutExtension(Path));
            //return ProcessNode(scene->MRootNode, scene, Matrix4x4.Identity, rootNode);
            return null;
        }

        private unsafe SceneNode ProcessNode(Node* node, AScene* scene, Matrix4x4 transform, SceneNode parent)
        {
            SceneNode sceneNode = new(parent);

            transform = node->MTransformation * transform;

            for (var i = 0; i < node->MNumMeshes; i++)
                Meshes.Add(ProcessMesh(scene->MMeshes[node->MMeshes[i]], scene, transform));

            for (var i = 0; i < node->MNumChildren; i++)
                ProcessNode(node->MChildren[i], scene, transform, sceneNode);

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

            return sceneNode;
        }

        private unsafe XRMesh ProcessMesh(Mesh* mesh, AScene* scene, Matrix4x4 transform)
        {
            List<Texture> textures = [];
            // process materials
            Material* material = scene->MMaterials[mesh->MMaterialIndex];
            // we assume a convention for sampler names in the shaders. Each diffuse texture should be named
            // as 'texture_diffuseN' where N is a sequential number ranging from 1 to MAX_SAMPLER_NUMBER. 
            // Same applies to other texture as the following list summarizes:
            // diffuse: texture_diffuseN
            // specular: texture_specularN
            // normal: texture_normalN

            var diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse);
            if (diffuseMaps.Count != 0)
                textures.AddRange(diffuseMaps);

            var specularMaps = LoadMaterialTextures(material, TextureType.Specular);
            if (specularMaps.Count != 0)
                textures.AddRange(specularMaps);

            var normalMaps = LoadMaterialTextures(material, TextureType.Height);
            if (normalMaps.Count != 0)
                textures.AddRange(normalMaps);

            var heightMaps = LoadMaterialTextures(material, TextureType.Ambient);
            if (heightMaps.Count != 0)
                textures.AddRange(heightMaps);

            var vertices = CollectVertices(mesh);
            var indices = CollectIndices(mesh);
            return XRMesh.Create(CreatePrimitivesParallel(vertices, CollectIndices(mesh)));
        }

        private static List<VertexPrimitive> CreatePrimitives(Vertex[] vertices, uint[][] indices)
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
        private static List<VertexPrimitive> CreatePrimitivesParallel(Vertex[] vertices, uint[][] indices)
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

        private static unsafe uint[][] CollectIndices(Mesh* mesh)
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

        private static unsafe Vertex[] CollectVertices(Mesh* mesh)
        {
            Vertex[] vertices = new Vertex[mesh->MNumVertices];
            bool[] hasTexCoords = new bool[8];
            bool[] hasColors = new bool[8];

            for (uint i = 0; i < mesh->MNumVertices; i++)
            {
                Vertex vertex = new(mesh->MVertices[i]);

                if (mesh->MNormals != null)
                    vertex.Normal = mesh->MNormals[i];

                if (mesh->MTangents != null)
                    vertex.Tangent = mesh->MTangents[i];

                //TODO: convert bitangent into normal or tangent depending on if we have only normals or tangents and bitangents
                //if (mesh->MBitangents != null)
                //    vertex.Bitangent = mesh->MBitangents[i];

                for (int x = 0; x < 8; ++i)
                {
                    var coord = mesh->MTextureCoords[x];
                    if (coord != null)
                    {
                        Vector3 c = coord[i];
                        vertex.TextureCoordinateSets.Add(new Vector2(c.X, c.Y));
                        hasTexCoords[x] = true;
                    }
                }

                for (int x = 0; x < 8; ++i)
                {
                    var color = mesh->MColors[x];
                    if (color != null)
                    {
                        vertex.ColorSets.Add(color[i]);
                        hasColors[x] = true;
                    }
                }
            }

            return vertices;
        }

        private unsafe List<Texture> LoadMaterialTextures(Material* mat, TextureType type)
        {
            List<Texture> textures = [];
            //var textureCount = _assimp.GeXRMaterialTextureCount(mat, type);
            //for (uint i = 0; i < textureCount; i++)
            //{
            //    AssimpString path;
            //    _assimp.GeXRMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
            //    bool skip = false;
            //    for (int j = 0; j < _texturesLoaded.Count; j++)
            //    {
            //        if (_texturesLoaded[j].MFilename == path)
            //        {
            //            textures.Add(_texturesLoaded[j]);
            //            skip = true;
            //            break;
            //        }
            //    }
            //    if (!skip)
            //    {
            //        var texture = new Texture(Path, type);
            //        texture.Path = path;
            //        textures.Add(texture);
            //        _texturesLoaded.Add(texture);
            //    }
            //}
            return textures;
        }

        public void Dispose()
        {

        }
    }
}