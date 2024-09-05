using Silk.NET.Assimp;
using System.IO;
using System.Numerics;

namespace XREngine.Files
{
    public unsafe class ModelImporter : IDisposable
    {
        protected ModelImporter(string path)
        {
            _assimp = Assimp.GetApi();
            _path = path;
        }

        private Assimp _assimp;
        private string _path;

        private List<Texture> _texturesLoaded = new();
        public List<Mesh> Meshes { get; protected set; } = new List<Mesh>();
        public string Path => _path;

        public static ModelImporter Import(string path)
        {
            var importer = new ModelImporter(path);
            importer.Import();
            return importer;
        }

        private void Import()
        {
            var scene = _assimp.ImportFile(Path, (uint)PostProcessSteps.Triangulate);

            if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
            {
                var error = _assimp.GetErrorStringS();
                throw new Exception(error);
            }

            ProcessNode(scene->MRootNode, scene, Matrix4x4.Identity);
        }

        private unsafe void ProcessNode(Node* node, Scene* scene, Matrix4x4 transform)
        {
            transform = node->MTransformation * transform;

            for (var i = 0; i < node->MNumMeshes; i++)
                Meshes.Add(ProcessMesh(scene->MMeshes[node->MMeshes[i]], scene, transform));
            
            for (var i = 0; i < node->MNumChildren; i++)
                ProcessNode(node->MChildren[i], scene, transform);

            for (int i = 0; i < scene->MNumSkeletons; i++)
            {
                var skel = scene->MSkeletons[i];
                for (int j = 0; j < skel->MNumBones; j++)
                {
                    var bone = skel->MBones[j];
                    var index = 0;
                    for (int k = 0; k < skel->MNumBones; k++)
                    {
                        if (skel->MBones[k]->MName == name)
                        {
                            index = k;
                            break;
                        }
                    }

                    Meshes[0].AddBone(index, bone->MOffsetMatrix);
                }
            }
        }

        private unsafe Mesh ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Scene* scene, Matrix4x4 transform)
        {
            List<Vertex> vertices = new();
            FillVertices(mesh, vertices);

            List<uint> indices = new();
            FillIndices(mesh, indices);

            List<Texture> textures = new();
            // process materials
            Material* material = scene->MMaterials[mesh->MMaterialIndex];
            // we assume a convention for sampler names in the shaders. Each diffuse texture should be named
            // as 'texture_diffuseN' where N is a sequential number ranging from 1 to MAX_SAMPLER_NUMBER. 
            // Same applies to other texture as the following list summarizes:
            // diffuse: texture_diffuseN
            // specular: texture_specularN
            // normal: texture_normalN

            var diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse);
            if (diffuseMaps.Any())
                textures.AddRange(diffuseMaps);

            var specularMaps = LoadMaterialTextures(material, TextureType.Specular);
            if (specularMaps.Any())
                textures.AddRange(specularMaps);

            var normalMaps = LoadMaterialTextures(material, TextureType.Height);
            if (normalMaps.Any())
                textures.AddRange(normalMaps);

            var heightMaps = LoadMaterialTextures(material, TextureType.Ambient);
            if (heightMaps.Any())
                textures.AddRange(heightMaps);

            var result = new Silk.NET.Assimp.Mesh(BuildVertices(vertices), BuildIndices(indices));
            return result;
        }

        private static unsafe void FillIndices(Silk.NET.Assimp.Mesh* mesh, List<uint> indices)
        {
            for (uint i = 0; i < mesh->MNumFaces; i++)
            {
                Face face = mesh->MFaces[i];
                for (uint j = 0; j < face.MNumIndices; j++)
                    indices.Add(face.MIndices[j]);
            }
        }

        private static unsafe void FillVertices(Silk.NET.Assimp.Mesh* mesh, List<Vertex> vertices)
        {
            for (uint i = 0; i < mesh->MNumVertices; i++)
            {
                Vertex vertex = new(mesh->MVertices[i]);

                if (mesh->MNormals != null)
                    vertex.Normal = mesh->MNormals[i];
                if (mesh->MTangents != null)
                    vertex.Tangent = mesh->MTangents[i];
                if (mesh->MBitangents != null)
                    vertex.Bitangent = mesh->MBitangents[i];

                for (int x = 0; x < 8; ++i)
                {
                    var coord = mesh->MTextureCoords[x];
                    if (coord != null)
                    {
                        if (vertex.TexCoords == null)
                            vertex.TexCoords = new List<Vector3>();

                        vertex.TexCoords.Add(coord[i]);
                    }
                }

                for (int x = 0; x < 8; ++i)
                {
                    var color = mesh->MColors[x];
                    if (color != null)
                    {
                        if (vertex.Colors == null)
                            vertex.Colors = new List<Vector4>();

                        vertex.Colors.Add(color[i]);
                    }
                }

                vertices.Add(vertex);
            }
        }

        private unsafe List<Texture> LoadMaterialTextures(Material* mat, TextureType type)
        {
            var textureCount = _assimp.GetMaterialTextureCount(mat, type);
            List<Texture> textures = new();
            for (uint i = 0; i < textureCount; i++)
            {
                AssimpString path;
                _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
                bool skip = false;
                for (int j = 0; j < _texturesLoaded.Count; j++)
                {
                    if (_texturesLoaded[j].Path == path)
                    {
                        textures.Add(_texturesLoaded[j]);
                        skip = true;
                        break;
                    }
                }
                if (!skip)
                {
                    var texture = new Texture(Directory, type);
                    texture.Path = path;
                    textures.Add(texture);
                    _texturesLoaded.Add(texture);
                }
            }
            return textures;
        }

        private float[] BuildVertices(List<Vertex> vertexCollection)
        {
            var vertices = new List<float>();

            foreach (var vertex in vertexCollection)
            {
                vertices.Add(vertex.Position.X);
                vertices.Add(vertex.Position.Y);
                vertices.Add(vertex.Position.Z);
                vertices.Add(vertex.TexCoords.X);
                vertices.Add(vertex.TexCoords.Y);
            }

            return vertices.ToArray();
        }

        private uint[] BuildIndices(List<uint> indices)
        {
            return indices.ToArray();
        }

        public void Dispose()
        {
            foreach (var mesh in Meshes)
            {
                mesh.Dispose();
            }

            _texturesLoaded = null;
        }
    }
}