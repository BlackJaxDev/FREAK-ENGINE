using Silk.NET.Assimp;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

public unsafe partial class VulkanAPI : BaseAPI
{
    internal Vk? vk;

    const int MAX_FRAMES_IN_FLIGHT = 2;
    private int currentFrame = 0;

    public override void UpdateWindowOptions(WindowOptions options)
    {

    }

    public override void SetWindow(IWindow window)
    {
        base.SetWindow(window);
        if (window.VkSurface is null)
            throw new Exception("Windowing platform doesn't support Vulkan.");
    }

    public override void Init()
    {
        vk = Vk.GetApi();
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateCommandPool();

        CreateDescriptorSetLayout();
        CreateAllSwapChainObjects();

        CreateModelBuffers();
        CreateUniformBuffers();

        CreateSyncObjects();
    }

    public override void CleanUp()
    {
        DestroyAllSwapChainObjects();

        _modelTexture?.Destroy();

        DestroyDescriptorSetLayout();
        DestroyModelBuffers();

        DestroySyncObjects();
        DestroyCommandPool();

        DestroyLogicalDevice();
        DestroyValidationLayers();
        DestroySurface();
        DestroyInstance();
        vk!.Dispose();
    }

    public void DeviceWaitIdle()
        => vk!.DeviceWaitIdle(device);

    const string MODEL_PATH = @"Assets\test.obj";
    const string TEXTURE_PATH = @"Assets\test.png";

    private void LoadModel()
    {
        using var assimp = Assimp.GetApi();
        var scene = assimp.ImportFile(MODEL_PATH, (uint)PostProcessPreset.TargetRealTimeMaximumQuality);

        var vertexMap = new Dictionary<Vertex, uint>();
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        VisitSceneNode(scene->MRootNode);

        assimp.ReleaseImport(scene);

        this.vertices = vertices.ToArray();
        this.indices = indices.ToArray();

        void VisitSceneNode(Node* node)
        {
            for (int m = 0; m < node->MNumMeshes; m++)
            {
                var mesh = scene->MMeshes[node->MMeshes[m]];

                for (int f = 0; f < mesh->MNumFaces; f++)
                {
                    var face = mesh->MFaces[f];

                    for (int i = 0; i < face.MNumIndices; i++)
                    {
                        uint index = face.MIndices[i];

                        var position = mesh->MVertices[index];
                        var texture = mesh->MTextureCoords[0][(int)index];

                        Vertex vertex = new()
                        {
                            pos = new Vector3D<float>(position.X, position.Y, position.Z),
                            color = new Vector3D<float>(1, 1, 1),
                            //Flip Y for OBJ in Vulkan
                            textCoord = new Vector2D<float>(texture.X, 1.0f - texture.Y)
                        };

                        if (vertexMap.TryGetValue(vertex, out var meshIndex))
                        {
                            indices.Add(meshIndex);
                        }
                        else
                        {
                            indices.Add((uint)vertices.Count);
                            vertexMap[vertex] = (uint)vertices.Count;
                            vertices.Add(vertex);
                        }
                    }
                }
            }

            for (int c = 0; c < node->MNumChildren; c++)
            {
                VisitSceneNode(node->MChildren[c]);
            }
        }
    }
}