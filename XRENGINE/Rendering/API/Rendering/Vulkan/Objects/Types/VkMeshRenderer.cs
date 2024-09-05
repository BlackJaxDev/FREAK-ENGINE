//using Silk.NET.Input;
//using Silk.NET.Vulkan;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using XREngine.Rendering.Models.Materials;
//using Buffer = Silk.NET.Vulkan.Buffer;

//namespace XREngine.Rendering.Vulkan;
//public unsafe partial class VulkanRenderer
//{
//    public class VkMeshRenderer(VulkanRenderer api, XRMeshRenderer data) : VkObject<XRMeshRenderer>(api, data)
//    {
//        private VkBuffer<VkVertex>? vertexBuffer;
//        private VkBuffer? indexBuffer;
//        private IndexType indexType = IndexType.Uint32;
//        private VkVertex[]? vertices;
//        private uint[]? indices;
//        private List<VkTexture>? _textures;

//        private PipelineLayout pipelineLayout;
//        private Pipeline pipeline;

//        public List<VkShader> Shaders { get; set; } = [];
//        public List<VkTexture>? Textures
//        {
//            get => _textures;
//            set => _textures = value;
//        }
//        public uint[]? Indices
//        {
//            get => indices;
//            set => indices = value;
//        }

//        public List<VertexInputAttributeDescription> VertexInputAttributes { get; } = VkVertex.GetAttributeDescriptions();
//        public List<VertexInputBindingDescription> VertexInputBindings { get; } = VkVertex.GetBindingDescriptions();

//        //public CullModeFlags CullMode { get; set; } = CullModeFlags.BackBit;
//        //public FrontFace FrontFace { get; set; } = FrontFace.Clockwise;
//        //public bool DiscardEnable { get; set; } = false;
//        //public PolygonMode PolyMode { get; set; } = PolygonMode.Fill;
//        //public bool EnableBlend { get; set; } = false;
//        //public PrimitiveTopology Topology { get; set; } = PrimitiveTopology.TriangleList;
//        //public bool PrimitiveRestart { get; set; } = false;
//        //public float LineWidth { get; set; } = 1.0f;
//        //public bool DepthBiasEnable { get; set; } = false;
//        //public bool DepthClampEnable { get; set; } = false;
//        //public bool LogicOpEnable { get; set; } = false;
//        //public LogicOp LogicOp { get; set; } = LogicOp.Copy;
//        //public BlendFactor SrcColorBlendFactor { get; set; } = BlendFactor.One;
//        //public BlendFactor DstColorBlendFactor { get; set; } = BlendFactor.Zero;
//        //public BlendOp ColorBlendOp { get; set; } = BlendOp.Add;
//        //public BlendFactor SrcAlphaBlendFactor { get; set; } = BlendFactor.One;
//        //public BlendFactor DstAlphaBlendFactor { get; set; } = BlendFactor.Zero;
//        //public BlendOp AlphaBlendOp { get; set; } = BlendOp.Add;
//        //public bool ColorWriteR { get; set; } = true;
//        //public bool ColorWriteG { get; set; } = true;
//        //public bool ColorWriteB { get; set; } = true;
//        //public bool ColorWriteA { get; set; } = true;
//        //public bool SampleShadingEnable { get; set; } = false;
//        //public float MinSampleShading { get; set; } = 1.0f;
//        //public bool AlphaToCoverageEnable { get; set; } = false;
//        //public bool AlphaToOneEnable { get; set; } = false;

//        public override void Destroy()
//        {
//            DestroyPipeline();
//            vertexBuffer?.Deallocate();
//            indexBuffer?.Deallocate();
//            vertexBuffer = null;
//            indexBuffer = null;
//        }

//        private void DestroyPipeline()
//        {
//            Api.DestroyPipeline(Renderer.device, pipeline, null);
//            Api.DestroyPipelineLayout(Renderer.device, pipelineLayout, null);
//        }

//        public override void Generate()
//        {
//            CreateVertexBuffer();
//            CreateIndexBuffer();
//            CreatePipeline();
//        }

//        private void CreateIndexBuffer()
//        {
//            ulong count = vertexBuffer!.Count;
//            if (count > uint.MaxValue)
//            {
//                throw new Exception("Vertex count cannot be larger than 4,294,967,295.");
//                //TODO: split mesh up into multiple buffers?
//                //indexType = IndexType.Uint64;
//                //CreateIndexBuffer<ulong>();
//            }
//            else if (count > ushort.MaxValue)
//            {
//                indexType = IndexType.Uint32;
//                CreateIndexBuffer<uint>();
//            }
//            else if (count > byte.MaxValue)
//            {
//                indexType = IndexType.Uint16;
//                CreateIndexBuffer<ushort>();
//            }
//            else
//            {
//                //TODO: is this an extension? Verify it works
//                indexType = IndexType.Uint8Ext;
//                CreateIndexBuffer<byte>();
//            }
//        }

//        private void CreateIndexBuffer<T>() where T : struct
//            => indexBuffer = StageAndCopy(indices!.Cast<T>(), (ulong)indices!.Length, BufferUsageFlags.IndexBufferBit);

//        private void CreateVertexBuffer()
//            => vertexBuffer = StageAndCopy(vertices!, (ulong)vertices!.Length, BufferUsageFlags.VertexBufferBit);

//        private VkBuffer StageAndCopy<T>(IEnumerable<T> items, ulong count, BufferUsageFlags usage) where T : struct
//        {
//            //https://vulkan-tutorial.com/Vertex_buffers/Staging_buffer
//            using VkBuffer staging = new(Renderer, count)
//            {
//                Usage = BufferUsageFlags.TransferSrcBit,
//                Properties = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
//            };
//            staging.Generate();
//            staging.Set(0, items);

//            var buf = new VkBuffer(Renderer, new XRDataBuffer())
//            {
//                Usage = BufferUsageFlags.TransferDstBit | usage,
//                Properties = MemoryPropertyFlags.DeviceLocalBit
//            };
//            buf.Generate();

//            staging.CopyTo(buf);
//            return buf;
//        }

//        private void CreatePipeline()
//        {
//            fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &Renderer.descriptorSetLayout)
//            {
//                PipelineLayoutCreateInfo pipelineLayoutInfo = new()
//                {
//                    SType = StructureType.PipelineLayoutCreateInfo,
//                    PushConstantRangeCount = 0,
//                    SetLayoutCount = 1,
//                    PSetLayouts = descriptorSetLayoutPtr
//                };

//                if (Api.CreatePipelineLayout(Renderer.device, ref pipelineLayoutInfo, null, out pipelineLayout) != Result.Success)
//                    throw new Exception("Failed to create pipeline layout.");

//                var inputBindingsPtr = stackalloc VertexInputBindingDescription[VertexInputBindings.Count];
//                for (int i = 0; i < VertexInputBindings.Count; i++)
//                    inputBindingsPtr[i] = VertexInputBindings[i];

//                var attributeDescriptionsPtr = stackalloc VertexInputAttributeDescription[VertexInputAttributes.Count];
//                for (int i = 0; i < VertexInputAttributes.Count; i++)
//                    attributeDescriptionsPtr[i] = VertexInputAttributes[i];

//                PipelineVertexInputStateCreateInfo vertexInputInfo = new()
//                {
//                    SType = StructureType.PipelineVertexInputStateCreateInfo,
//                    VertexBindingDescriptionCount = 1,
//                    VertexAttributeDescriptionCount = (uint)VertexInputAttributes.Count,
//                    PVertexBindingDescriptions = inputBindingsPtr,
//                    PVertexAttributeDescriptions = attributeDescriptionsPtr,
//                };

//                PipelineInputAssemblyStateCreateInfo inputAssembly = new()
//                {
//                    SType = StructureType.PipelineInputAssemblyStateCreateInfo,
//                    Topology = Topology,
//                    PrimitiveRestartEnable = PrimitiveRestart,
//                };

//                Viewport viewport = new()
//                {
//                    X = 0,
//                    Y = 0,
//                    Width = Renderer.swapChainExtent.Width,
//                    Height = Renderer.swapChainExtent.Height,
//                    MinDepth = 0,
//                    MaxDepth = 1,
//                };

//                Rect2D scissor = new()
//                {
//                    Offset = { X = 0, Y = 0 },
//                    Extent = Renderer.swapChainExtent,
//                };

//                PipelineViewportStateCreateInfo viewportState = new()
//                {
//                    SType = StructureType.PipelineViewportStateCreateInfo,
//                    ViewportCount = 1,
//                    PViewports = &viewport,
//                    ScissorCount = 1,
//                    PScissors = &scissor,
//                };

//                var opts = Data?.Material?.RenderOptions ?? new RenderingParameters();

//                PipelineRasterizationStateCreateInfo rasterizer = new()
//                {
//                    SType = StructureType.PipelineRasterizationStateCreateInfo,
//                    RasterizerDiscardEnable = opts.DiscardEnable.ToVulkan(),
//                    PolygonMode = opts.PolygonMode,
//                    LineWidth = opts.LineWidth,
//                    CullMode = ToVulkan(opts.CullMode),
//                    FrontFace = ToVulkan(opts.Winding),
//                    DepthBiasEnable = DepthBiasEnable,
//                    DepthClampEnable = DepthClampEnable,
//                };

//                PipelineMultisampleStateCreateInfo multisampling = new()
//                {
//                    SType = StructureType.PipelineMultisampleStateCreateInfo,
//                    SampleShadingEnable = SampleShadingEnable,
//                    AlphaToCoverageEnable = AlphaToCoverageEnable,
//                    AlphaToOneEnable = AlphaToOneEnable,
//                    MinSampleShading = MinSampleShading,
//                    RasterizationSamples = SampleCountFlags.Count1Bit,
//                };

//                PipelineColorBlendAttachmentState colorBlendAttachment = new()
//                {
//                    BlendEnable = EnableBlend,
//                    SrcColorBlendFactor = SrcColorBlendFactor,
//                    DstColorBlendFactor = DstColorBlendFactor,
//                    ColorBlendOp = ColorBlendOp,
//                    SrcAlphaBlendFactor = SrcAlphaBlendFactor,
//                    DstAlphaBlendFactor = DstAlphaBlendFactor,
//                    AlphaBlendOp = AlphaBlendOp,
//                    ColorWriteMask =
//                        (ColorWriteR ? ColorComponentFlags.RBit : 0) |
//                        (ColorWriteG ? ColorComponentFlags.GBit : 0) |
//                        (ColorWriteB ? ColorComponentFlags.BBit : 0) |
//                        (ColorWriteA ? ColorComponentFlags.ABit : 0),
//                };

//                PipelineColorBlendStateCreateInfo colorBlending = new()
//                {
//                    SType = StructureType.PipelineColorBlendStateCreateInfo,
//                    LogicOpEnable = LogicOpEnable,
//                    LogicOp = LogicOp,
//                    AttachmentCount = 1,
//                    PAttachments = &colorBlendAttachment,
//                };

//                colorBlending.BlendConstants[0] = 0;
//                colorBlending.BlendConstants[1] = 0;
//                colorBlending.BlendConstants[2] = 0;
//                colorBlending.BlendConstants[3] = 0;

//                var shaderStages = stackalloc PipelineShaderStageCreateInfo[Shaders.Count];
//                for (int i = 0; i < Shaders.Count; i++)
//                {
//                    var s = Shaders[i];
//                    s.Generate();
//                    shaderStages[i] = s.ShaderStageCreateInfo;
//                }

//                GraphicsPipelineCreateInfo pipelineInfo = new()
//                {
//                    SType = StructureType.GraphicsPipelineCreateInfo,
//                    StageCount = 2,
//                    PStages = shaderStages,
//                    PVertexInputState = &vertexInputInfo,
//                    PInputAssemblyState = &inputAssembly,
//                    PViewportState = &viewportState,
//                    PRasterizationState = &rasterizer,
//                    PMultisampleState = &multisampling,
//                    PColorBlendState = &colorBlending,
//                    Layout = pipelineLayout,
//                    RenderPass = Renderer.renderPass,
//                    Subpass = 0,
//                    BasePipelineHandle = default
//                };

//                if (Api.CreateGraphicsPipelines(Renderer.device, default, 1, ref pipelineInfo, null, out pipeline) != Result.Success)
//                    throw new Exception("Failed to create graphics pipeline.");
//            }

//            foreach (var shader in Shaders)
//                shader?.Destroy();
//        }

//        public void Draw(CommandBuffer cmd, DescriptorSet set)
//        {
//            if (vertexBuffer is null || indexBuffer is null)
//                throw new Exception("Vertex buffer or index buffer is null.");

//            Api.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, pipeline);

//            var vertexBuffers = new Buffer[] { vertexBuffer.Buffer };
//            var offsets = new ulong[] { 0 };

//            fixed (ulong* offsetsPtr = offsets)
//            fixed (Buffer* vertexBuffersPtr = vertexBuffers)
//            {
//                Api.CmdBindVertexBuffers(cmd, 0, 1, vertexBuffersPtr, offsetsPtr);
//            }

//            Api.CmdBindIndexBuffer(cmd, indexBuffer.Buffer, 0, indexType);
//            Api.CmdBindDescriptorSets(cmd, PipelineBindPoint.Graphics, pipelineLayout, 0, 1, ref set, 0, null);
//            Api.CmdDrawIndexed(cmd, (uint)indexBuffer.Count, 1, 0, 0, 0);
//        }

//        public static VertexInputBindingDescription GetBindingDescriptionForBuffer(uint bufferIndex, uint stride, bool perInstance)
//            => new()
//            {
//                Binding = bufferIndex,
//                Stride = stride,
//                InputRate = perInstance ? VertexInputRate.Instance : VertexInputRate.Vertex,
//            };

//        public static VertexInputAttributeDescription GetAttributeDescriptionForBufferItem(uint bufferIndex, uint attributeIndex, Format attributeFormat, uint offset)
//            => new()
//            {
//                Binding = bufferIndex,
//                Location = attributeIndex,
//                Format = attributeFormat,
//                Offset = offset,
//            };
//    }
//}