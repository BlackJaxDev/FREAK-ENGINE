using Silk.NET.Assimp;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace XREngine.Rendering.Graphics.Renderers.Vulkan
{
    public unsafe partial class VulkanRenderer
    {
        public class VkMesh : GraphicModel<VulkanRenderer>
        {
            private Vertex[] vertices;
            private uint[] indices;
            private Buffer[] uniformBuffers;
            private DeviceMemory[] uniformBuffersMemory;
            private Buffer indexBuffer;
            private DeviceMemory indexBufferMemory;
            private Buffer vertexBuffer;
            private DeviceMemory vertexBufferMemory;
            private ImageView textureImageView;
            private Sampler textureSampler;

            public VkMaterial? Material { get; private set; }

            public void Render(CommandBuffer cmdBuf, DescriptorSet descSet)
            {
                var API = Renderer.API;
                var device = Renderer.device;

                API!.CmdBindPipeline(cmdBuf, PipelineBindPoint.Graphics, Material!.GraphicsPipeline);
                var vertexBuffers = new Buffer[] { vertexBuffer };
                var offsets = new ulong[] { 0 };
                fixed (ulong* offsetsPtr = offsets)
                fixed (Buffer* vertexBuffersPtr = vertexBuffers)
                {
                    API!.CmdBindVertexBuffers(cmdBuf, 0, 1, vertexBuffersPtr, offsetsPtr);
                }
                API.CmdBindIndexBuffer(cmdBuf, indexBuffer, 0, IndexType.Uint32);
                API.CmdBindDescriptorSets(cmdBuf, PipelineBindPoint.Graphics, Material!.PipelineLayout, 0, 1, descSet, 0, null);
                API.CmdDrawIndexed(cmdBuf, (uint)indices!.Length, 1, 0, 0, 0);
            }
            public void CleanUp()
            {
                var API = Renderer.API;
                var device = Renderer.device;

                API!.DestroyBuffer(device, indexBuffer, null);
                API!.FreeMemory(device, indexBufferMemory, null);

                API!.DestroyBuffer(device, vertexBuffer, null);
                API!.FreeMemory(device, vertexBufferMemory, null);

            }
            private void UpdateUniformBuffer(uint currentImage)
            {
                //Silk Window has timing information so we are skipping the time code.
                var time = (float)Renderer.Window!.Time;

                UniformBufferObject ubo = new()
                {
                    model = Matrix4X4<float>.Identity * Matrix4X4.CreateFromAxisAngle(new Vector3D<float>(0, 0, 1), time * Scalar.DegreesToRadians(90.0f)),
                    view = Matrix4X4.CreateLookAt(new Vector3D<float>(2, 2, 2), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, 0, 1)),
                    proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), Renderer.swapChainExtent.Width / Renderer.swapChainExtent.Height, 0.1f, 10.0f),
                };
                ubo.proj.M22 *= -1;

                void* data;
                Renderer.API.MapMemory(Renderer.device, Renderer.uniformBuffersMemory![currentImage], 0, (ulong)Unsafe.SizeOf<UniformBufferObject>(), 0, &data);
                new Span<UniformBufferObject>(data, 1)[0] = ubo;
                Renderer.API.UnmapMemory(Renderer.device, Renderer.uniformBuffersMemory![currentImage]);
            }

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

                                Vertex vertex = new Vertex
                                {
                                    pos = new Vector3D<float>(position.X, position.Y, position.Z),
                                    color = new Vector3D<float>(1, 1, 1),
                                    //Flip Y for OBJ in Vulkan
                                    textCoord = new Vector2D<float>(texture.X, 1.0f - texture.Y)
                                };

                                if (vertexMap.TryGetValue(vertex, out var meshIndex))
                                    indices.Add(meshIndex);
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
                        VisitSceneNode(node->MChildren[c]);
                }
            }

            private void CreateVertexBuffer(Vertex[] vertices)
            {
                var API = Renderer.API;
                var device = Renderer.device;

                ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex>() * vertices!.Length);

                Buffer stagingBuffer = default;
                DeviceMemory stagingBufferMemory = default;
                Renderer.CreateBuffer(
                    bufferSize,
                    BufferUsageFlags.TransferSrcBit,
                    MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                    ref stagingBuffer,
                    ref stagingBufferMemory);

                void* data;
                API!.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
                vertices.AsSpan().CopyTo(new Span<Vertex>(data, vertices.Length));
                API!.UnmapMemory(device, stagingBufferMemory);

                Renderer.CreateBuffer(
                    bufferSize,
                    BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit,
                    MemoryPropertyFlags.DeviceLocalBit,
                    ref vertexBuffer,
                    ref vertexBufferMemory);

                Renderer.CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);

                API!.DestroyBuffer(device, stagingBuffer, null);
                API!.FreeMemory(device, stagingBufferMemory, null);
            }

            private void CreateIndexBuffer(uint[] indices)
            {
                var API = Renderer.API;
                var device = Renderer.device;

                ulong bufferSize = (ulong)(Unsafe.SizeOf<uint>() * indices!.Length);

                Buffer stagingBuffer = default;
                DeviceMemory stagingBufferMemory = default;
                Renderer.CreateBuffer(
                    bufferSize,
                    BufferUsageFlags.TransferSrcBit,
                    MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                    ref stagingBuffer,
                    ref stagingBufferMemory);

                void* data;
                API!.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
                indices.AsSpan().CopyTo(new Span<uint>(data, indices.Length));
                API!.UnmapMemory(device, stagingBufferMemory);

                Renderer.CreateBuffer(
                    bufferSize,
                    BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit,
                    MemoryPropertyFlags.DeviceLocalBit,
                    ref indexBuffer,
                    ref indexBufferMemory);

                Renderer.CopyBuffer(stagingBuffer, indexBuffer, bufferSize);

                API!.DestroyBuffer(device, stagingBuffer, null);
                API!.FreeMemory(device, stagingBufferMemory, null);
            }

            private void CreateUniformBuffers()
            {
                var swapChainImages = Renderer.swapChainImages;

                ulong bufferSize = (ulong)Unsafe.SizeOf<UniformBufferObject>();

                uniformBuffers = new Buffer[swapChainImages!.Length];
                uniformBuffersMemory = new DeviceMemory[swapChainImages!.Length];

                for (int i = 0; i < swapChainImages.Length; i++)
                    Renderer.CreateBuffer(
                        bufferSize,
                        BufferUsageFlags.UniformBufferBit,
                        MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                        ref uniformBuffers[i],
                        ref uniformBuffersMemory[i]);
            }

            private void CreateDescriptorPool()
            {
                var API = Renderer.API;
                var device = Renderer.device;
                var swapChainImages = Renderer.swapChainImages;

                var poolSizes = new DescriptorPoolSize[]
                {
                    new DescriptorPoolSize()
                    {
                        Type = DescriptorType.UniformBuffer,
                        DescriptorCount = (uint)swapChainImages!.Length,
                    },
                    new DescriptorPoolSize()
                    {
                        Type = DescriptorType.CombinedImageSampler,
                        DescriptorCount = (uint)swapChainImages!.Length,
                    }
                };

                fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)
                fixed (DescriptorPool* descriptorPoolPtr = &descriptorPool)
                {
                    DescriptorPoolCreateInfo poolInfo = new()
                    {
                        SType = StructureType.DescriptorPoolCreateInfo,
                        PoolSizeCount = (uint)poolSizes.Length,
                        PPoolSizes = poolSizesPtr,
                        MaxSets = (uint)swapChainImages!.Length,
                    };

                    if (API!.CreateDescriptorPool(device, poolInfo, null, descriptorPoolPtr) != Result.Success)
                        throw new Exception("Failed to create descriptor pool.");
                }
            }

            private void CreateDescriptorSets()
            {
                var API = Renderer.API;
                var device = Renderer.device;
                var swapChainImages = Renderer.swapChainImages;

                var layouts = new DescriptorSetLayout[swapChainImages!.Length];
                Array.Fill(layouts, descriptorSetLayout);

                fixed (DescriptorSetLayout* layoutsPtr = layouts)
                {
                    DescriptorSetAllocateInfo allocateInfo = new()
                    {
                        SType = StructureType.DescriptorSetAllocateInfo,
                        DescriptorPool = descriptorPool,
                        DescriptorSetCount = (uint)swapChainImages!.Length,
                        PSetLayouts = layoutsPtr,
                    };

                    descriptorSets = new DescriptorSet[swapChainImages.Length];
                    fixed (DescriptorSet* descriptorSetsPtr = descriptorSets)
                    {
                        if (API!.AllocateDescriptorSets(device, allocateInfo, descriptorSetsPtr) != Result.Success)
                            throw new Exception("Failed to allocate descriptor sets.");
                    }
                }

                for (int i = 0; i < swapChainImages.Length; i++)
                {
                    DescriptorBufferInfo bufferInfo = new()
                    {
                        Buffer = uniformBuffers![i],
                        Offset = 0,
                        Range = (ulong)Unsafe.SizeOf<UniformBufferObject>(),
                    };

                    DescriptorImageInfo imageInfo = new()
                    {
                        ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                        ImageView = textureImageView,
                        Sampler = textureSampler,
                    };

                    var descriptorWrites = new WriteDescriptorSet[]
                    {
                        new()
                        {
                            SType = StructureType.WriteDescriptorSet,
                            DstSet = descriptorSets[i],
                            DstBinding = 0,
                            DstArrayElement = 0,
                            DescriptorType = DescriptorType.UniformBuffer,
                            DescriptorCount = 1,
                            PBufferInfo = &bufferInfo,
                        },
                        new()
                        {
                            SType = StructureType.WriteDescriptorSet,
                            DstSet = descriptorSets[i],
                            DstBinding = 1,
                            DstArrayElement = 0,
                            DescriptorType = DescriptorType.CombinedImageSampler,
                            DescriptorCount = 1,
                            PImageInfo = &imageInfo,
                        }
                    };

                    fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites)
                    {
                        API!.UpdateDescriptorSets(device, (uint)descriptorWrites.Length, descriptorWritesPtr, 0, null);
                    }
                }
            }
        }
    }
}
