using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace XREngine.Rendering.Graphics.Renderers.Vulkan
{

    public unsafe partial class VulkanRenderer
    {
        public class VkMaterial : GraphicMaterial<VulkanRenderer>
        {
            private PipelineLayout _pipelineLayout;
            private Pipeline _graphicsPipeline;

            public Pipeline GraphicsPipeline
            {
                get => _graphicsPipeline;
                set => _graphicsPipeline = value;
            }
            public PipelineLayout PipelineLayout
            {
                get => _pipelineLayout;
                set => _pipelineLayout = value;
            }

            public void CleanUp()
            {
                var API = Renderer.API;
                var device = Renderer.device;

                API!.DestroyPipeline(device, GraphicsPipeline, null);
                API!.DestroyPipelineLayout(device, PipelineLayout, null);
            }
            public void CreateGraphicsPipeline()
            {
                var API = Renderer.API;
                var device = Renderer.device;

                PipelineShaderStageCreateInfo[] shaderStages = GenShaderStages(
                    out ShaderModule vertShaderModule,
                    out ShaderModule fragShaderModule,
                    out PipelineShaderStageCreateInfo vertShaderStageInfo,
                    out PipelineShaderStageCreateInfo fragShaderStageInfo);

                var bindingDescription = Vertex.GetBindingDescription();
                var attributeDescriptions = Vertex.GetAttributeDescriptions();

                fixed (PipelineShaderStageCreateInfo* shaderStagesPtr = shaderStages)
                fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
                fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &Renderer.descriptorSetLayout)
                {
                    PipelineVertexInputStateCreateInfo vertexInputInfo = new()
                    {
                        SType = StructureType.PipelineVertexInputStateCreateInfo,
                        VertexBindingDescriptionCount = 1,
                        VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                        PVertexBindingDescriptions = &bindingDescription,
                        PVertexAttributeDescriptions = attributeDescriptionsPtr,
                    };

                    PipelineInputAssemblyStateCreateInfo inputAssembly = new()
                    {
                        SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                        Topology = PrimitiveTopology.TriangleList,
                        PrimitiveRestartEnable = false,
                    };

                    Viewport viewport = new()
                    {
                        X = 0,
                        Y = 0,
                        Width = Renderer.swapChainExtent.Width,
                        Height = Renderer.swapChainExtent.Height,
                        MinDepth = 0,
                        MaxDepth = 1,
                    };

                    Rect2D scissor = new()
                    {
                        Offset = { X = 0, Y = 0 },
                        Extent = Renderer.swapChainExtent,
                    };

                    PipelineViewportStateCreateInfo viewportState = new()
                    {
                        SType = StructureType.PipelineViewportStateCreateInfo,
                        ViewportCount = 1,
                        PViewports = &viewport,
                        ScissorCount = 1,
                        PScissors = &scissor,
                    };

                    PipelineRasterizationStateCreateInfo rasterizer = new()
                    {
                        SType = StructureType.PipelineRasterizationStateCreateInfo,
                        DepthClampEnable = false,
                        RasterizerDiscardEnable = false,
                        PolygonMode = PolygonMode.Fill,
                        LineWidth = 1,
                        CullMode = CullModeFlags.BackBit,
                        FrontFace = FrontFace.CounterClockwise,
                        DepthBiasEnable = false,
                    };

                    PipelineMultisampleStateCreateInfo multisampling = new()
                    {
                        SType = StructureType.PipelineMultisampleStateCreateInfo,
                        SampleShadingEnable = false,
                        RasterizationSamples = SampleCountFlags.Count1Bit,
                    };

                    PipelineDepthStencilStateCreateInfo depthStencil = new()
                    {
                        SType = StructureType.PipelineDepthStencilStateCreateInfo,
                        DepthTestEnable = true,
                        DepthWriteEnable = true,
                        DepthCompareOp = CompareOp.Less,
                        DepthBoundsTestEnable = false,
                        StencilTestEnable = false,
                    };

                    PipelineColorBlendAttachmentState colorBlendAttachment = new()
                    {
                        ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                        BlendEnable = false,
                    };

                    PipelineColorBlendStateCreateInfo colorBlending = new()
                    {
                        SType = StructureType.PipelineColorBlendStateCreateInfo,
                        LogicOpEnable = false,
                        LogicOp = LogicOp.Copy,
                        AttachmentCount = 1,
                        PAttachments = &colorBlendAttachment,
                    };

                    colorBlending.BlendConstants[0] = 0;
                    colorBlending.BlendConstants[1] = 0;
                    colorBlending.BlendConstants[2] = 0;
                    colorBlending.BlendConstants[3] = 0;

                    PipelineLayoutCreateInfo pipelineLayoutInfo = new()
                    {
                        SType = StructureType.PipelineLayoutCreateInfo,
                        PushConstantRangeCount = 0,
                        SetLayoutCount = 1,
                        PSetLayouts = descriptorSetLayoutPtr
                    };

                    if (Renderer.API!.CreatePipelineLayout(Renderer.device, pipelineLayoutInfo, null, out _pipelineLayout) != Result.Success)
                        throw new Exception("Failed to create pipeline layout.");

                    GraphicsPipelineCreateInfo pipelineInfo = new()
                    {
                        SType = StructureType.GraphicsPipelineCreateInfo,
                        StageCount = 2,
                        PStages = shaderStagesPtr,
                        PVertexInputState = &vertexInputInfo,
                        PInputAssemblyState = &inputAssembly,
                        PViewportState = &viewportState,
                        PRasterizationState = &rasterizer,
                        PMultisampleState = &multisampling,
                        PDepthStencilState = &depthStencil,
                        PColorBlendState = &colorBlending,
                        Layout = PipelineLayout,
                        RenderPass = Renderer.renderPass,
                        Subpass = 0,
                        BasePipelineHandle = default
                    };

                    if (Renderer.API!.CreateGraphicsPipelines(device, default, 1, pipelineInfo, null, out _graphicsPipeline) != Result.Success)
                        throw new Exception("Failed to create graphics pipeline.");
                }

                API!.DestroyShaderModule(device, fragShaderModule, null);
                API!.DestroyShaderModule(device, vertShaderModule, null);

                SilkMarshal.Free((nint)vertShaderStageInfo.PName);
                SilkMarshal.Free((nint)fragShaderStageInfo.PName);
            }

            private PipelineShaderStageCreateInfo[] GenShaderStages(
                out ShaderModule vertShaderModule,
                out ShaderModule fragShaderModule,
                out PipelineShaderStageCreateInfo vertShaderStageInfo,
                out PipelineShaderStageCreateInfo fragShaderStageInfo)
            {
                var vertShaderCode = File.ReadAllBytes("shaders/vert.spv");
                var fragShaderCode = File.ReadAllBytes("shaders/frag.spv");

                vertShaderModule = Renderer.CreateShaderModule(vertShaderCode);
                fragShaderModule = Renderer.CreateShaderModule(fragShaderCode);
                vertShaderStageInfo = new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.VertexBit,
                    Module = vertShaderModule,
                    PName = (byte*)SilkMarshal.StringToPtr("main")
                };
                fragShaderStageInfo = new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.FragmentBit,
                    Module = fragShaderModule,
                    PName = (byte*)SilkMarshal.StringToPtr("main")
                };
                return new[]
                {
                    vertShaderStageInfo,
                    fragShaderStageInfo
                };
            }
        }
    }
}
