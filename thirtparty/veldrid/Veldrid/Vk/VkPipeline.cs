using static Veldrid.Vk.VulkanUtil;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Veldrid.Vk
{
    internal unsafe class VkPipeline : Pipeline
    {
        public const uint SubpassExternal = uint.MaxValue;
        private readonly VkGraphicsDevice _gd;
        private readonly Silk.NET.Vulkan.Pipeline _devicePipeline;
        private readonly Silk.NET.Vulkan.PipelineLayout _pipelineLayout;
        private readonly Silk.NET.Vulkan.RenderPass _renderPass;
        private bool _destroyed;
        private string _name;

        public Silk.NET.Vulkan.Pipeline DevicePipeline => _devicePipeline;

        public Silk.NET.Vulkan.PipelineLayout PipelineLayout => _pipelineLayout;

        public uint ResourceSetCount { get; }
        public int DynamicOffsetsCount { get; }
        public bool ScissorTestEnabled { get; }

        public override bool IsComputePipeline { get; }

        public ResourceRefCount RefCount { get; }

        public override bool IsDisposed => _destroyed;

        public VkPipeline(VkGraphicsDevice gd, ref GraphicsPipelineDescription description)
            : base(ref description)
        {
            _gd = gd;
            IsComputePipeline = false;
            RefCount = new ResourceRefCount(DisposeCore);

            Silk.NET.Vulkan.GraphicsPipelineCreateInfo pipelineCI = new Silk.NET.Vulkan.GraphicsPipelineCreateInfo();
            pipelineCI.SType = Silk.NET.Vulkan.StructureType.GraphicsPipelineCreateInfo;

            // Blend State
            Silk.NET.Vulkan.PipelineColorBlendStateCreateInfo blendStateCI = new Silk.NET.Vulkan.PipelineColorBlendStateCreateInfo();
            blendStateCI.SType = Silk.NET.Vulkan.StructureType.PipelineColorBlendStateCreateInfo;


            int attachmentsCount = description.BlendState.AttachmentStates.Length;
            Silk.NET.Vulkan.PipelineColorBlendAttachmentState* attachmentsPtr
                = stackalloc Silk.NET.Vulkan.PipelineColorBlendAttachmentState[attachmentsCount];
            for (int i = 0; i < attachmentsCount; i++)
            {
                BlendAttachmentDescription vdDesc = description.BlendState.AttachmentStates[i];
                Silk.NET.Vulkan.PipelineColorBlendAttachmentState attachmentState = new Silk.NET.Vulkan.PipelineColorBlendAttachmentState();


                attachmentState.SrcColorBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.SourceColorFactor);
                attachmentState.DstColorBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.DestinationColorFactor);
                attachmentState.ColorBlendOp = VkFormats.VdToVkBlendOp(vdDesc.ColorFunction);
                attachmentState.SrcAlphaBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.SourceAlphaFactor);
                attachmentState.DstAlphaBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.DestinationAlphaFactor);
                attachmentState.AlphaBlendOp = VkFormats.VdToVkBlendOp(vdDesc.AlphaFunction);
                attachmentState.BlendEnable = vdDesc.BlendEnabled;
                attachmentState.ColorWriteMask = Silk.NET.Vulkan.ColorComponentFlags.ColorComponentRBit
                    | Silk.NET.Vulkan.ColorComponentFlags.ColorComponentGBit | Silk.NET.Vulkan.ColorComponentFlags.ColorComponentBBit | Silk.NET.Vulkan.ColorComponentFlags.ColorComponentABit;
                attachmentsPtr[i] = attachmentState;
            }

            blendStateCI.AttachmentCount = (uint)attachmentsCount;
            blendStateCI.PAttachments = attachmentsPtr;
            RgbaFloat blendFactor = description.BlendState.BlendFactor;

            blendStateCI.BlendConstants[0] = blendFactor.R;
            blendStateCI.BlendConstants[1] = blendFactor.G;
            blendStateCI.BlendConstants[2] = blendFactor.B;
            blendStateCI.BlendConstants[3] = blendFactor.A;

            pipelineCI.PColorBlendState = &blendStateCI;

            // Rasterizer State
            RasterizerStateDescription rsDesc = description.RasterizerState;

            Silk.NET.Vulkan.PipelineRasterizationStateCreateInfo rsCI = new Silk.NET.Vulkan.PipelineRasterizationStateCreateInfo();
            rsCI.CullMode = VkFormats.VdToVkCullMode(rsDesc.CullMode);
            rsCI.PolygonMode = VkFormats.VdToVkPolygonMode(rsDesc.FillMode);
            rsCI.DepthClampEnable = !rsDesc.DepthClipEnabled;
            rsCI.FrontFace = rsDesc.FrontFace == FrontFace.Clockwise ? Silk.NET.Vulkan.FrontFace.Clockwise : Silk.NET.Vulkan.FrontFace.CounterClockwise;
            rsCI.LineWidth = 1f;
            rsCI.SType = Silk.NET.Vulkan.StructureType.PipelineRasterizationStateCreateInfo;
            pipelineCI.PRasterizationState = &rsCI;

            ScissorTestEnabled = rsDesc.ScissorTestEnabled;

            // Dynamic State
            Silk.NET.Vulkan.PipelineDynamicStateCreateInfo dynamicStateCI = new Silk.NET.Vulkan.PipelineDynamicStateCreateInfo();
            dynamicStateCI.SType = Silk.NET.Vulkan.StructureType.PipelineDynamicStateCreateInfo;

            Silk.NET.Vulkan.DynamicState* dynamicStates = stackalloc Silk.NET.Vulkan.DynamicState[2];
            dynamicStates[0] = Silk.NET.Vulkan.DynamicState.Viewport;
            dynamicStates[1] = Silk.NET.Vulkan.DynamicState.Scissor;
            dynamicStateCI.DynamicStateCount = 2;
            dynamicStateCI.PDynamicStates = dynamicStates;

            pipelineCI.PDynamicState = &dynamicStateCI;

            // Depth Stencil State
            DepthStencilStateDescription vdDssDesc = description.DepthStencilState;
            Silk.NET.Vulkan.PipelineDepthStencilStateCreateInfo dssCI = new Silk.NET.Vulkan.PipelineDepthStencilStateCreateInfo();
            dssCI.DepthWriteEnable = vdDssDesc.DepthWriteEnabled;
            dssCI.DepthTestEnable = vdDssDesc.DepthTestEnabled;
            dssCI.DepthCompareOp = VkFormats.VdToVkCompareOp(vdDssDesc.DepthComparison);
            dssCI.StencilTestEnable = vdDssDesc.StencilTestEnabled;
            dssCI.SType = Silk.NET.Vulkan.StructureType.PipelineDepthStencilStateCreateInfo;

            dssCI.Front = new Silk.NET.Vulkan.StencilOpState();
            dssCI.Front.FailOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilFront.Fail);
            dssCI.Front.PassOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilFront.Pass);
            dssCI.Front.DepthFailOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilFront.DepthFail);
            dssCI.Front.CompareOp = VkFormats.VdToVkCompareOp(vdDssDesc.StencilFront.Comparison);
            dssCI.Front.CompareMask = vdDssDesc.StencilReadMask;
            dssCI.Front.WriteMask = vdDssDesc.StencilWriteMask;
            dssCI.Front.Reference = vdDssDesc.StencilReference;


            dssCI.Back = new Silk.NET.Vulkan.StencilOpState();
            dssCI.Back.FailOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilBack.Fail);
            dssCI.Back.PassOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilBack.Pass);
            dssCI.Back.DepthFailOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilBack.DepthFail);
            dssCI.Back.CompareOp = VkFormats.VdToVkCompareOp(vdDssDesc.StencilBack.Comparison);
            dssCI.Back.CompareMask = vdDssDesc.StencilReadMask;
            dssCI.Back.WriteMask = vdDssDesc.StencilWriteMask;
            dssCI.Back.Reference = vdDssDesc.StencilReference;

            pipelineCI.PDepthStencilState = &dssCI;

            // Multisample
            Silk.NET.Vulkan.PipelineMultisampleStateCreateInfo multisampleCI = new Silk.NET.Vulkan.PipelineMultisampleStateCreateInfo();
            multisampleCI.SType = Silk.NET.Vulkan.StructureType.PipelineMultisampleStateCreateInfo;

            Silk.NET.Vulkan.SampleCountFlags vkSampleCount = VkFormats.VdToVkSampleCount(description.Outputs.SampleCount);
            multisampleCI.RasterizationSamples = vkSampleCount;
            multisampleCI.AlphaToCoverageEnable = description.BlendState.AlphaToCoverageEnabled;

            pipelineCI.PMultisampleState = &multisampleCI;

            // Input Assembly
            Silk.NET.Vulkan.PipelineInputAssemblyStateCreateInfo inputAssemblyCI = new Silk.NET.Vulkan.PipelineInputAssemblyStateCreateInfo();
            inputAssemblyCI.SType = Silk.NET.Vulkan.StructureType.PipelineInputAssemblyStateCreateInfo;

            inputAssemblyCI.Topology = VkFormats.VdToVkPrimitiveTopology(description.PrimitiveTopology);

            pipelineCI.PInputAssemblyState = &inputAssemblyCI;

            // Vertex Input State
            Silk.NET.Vulkan.PipelineVertexInputStateCreateInfo vertexInputCI = new Silk.NET.Vulkan.PipelineVertexInputStateCreateInfo();
            vertexInputCI.SType = Silk.NET.Vulkan.StructureType.PipelineVertexInputStateCreateInfo;

            VertexLayoutDescription[] inputDescriptions = description.ShaderSet.VertexLayouts;
            uint bindingCount = (uint)inputDescriptions.Length;
            uint attributeCount = 0;
            for (int i = 0; i < inputDescriptions.Length; i++)
            {
                attributeCount += (uint)inputDescriptions[i].Elements.Length;
            }
            Silk.NET.Vulkan.VertexInputBindingDescription* bindingDescs = stackalloc Silk.NET.Vulkan.VertexInputBindingDescription[(int)bindingCount];
            Silk.NET.Vulkan.VertexInputAttributeDescription* attributeDescs = stackalloc Silk.NET.Vulkan.VertexInputAttributeDescription[(int)attributeCount];

            int targetIndex = 0;
            int targetLocation = 0;
            for (int binding = 0; binding < inputDescriptions.Length; binding++)
            {
                VertexLayoutDescription inputDesc = inputDescriptions[binding];
                bindingDescs[binding] = new Silk.NET.Vulkan.VertexInputBindingDescription()
                {
                    Binding = (uint)binding,
                    InputRate = (inputDesc.InstanceStepRate != 0) ? Silk.NET.Vulkan.VertexInputRate.Instance : Silk.NET.Vulkan.VertexInputRate.Vertex,
                    Stride = inputDesc.Stride
                };

                uint currentOffset = 0;
                for (int location = 0; location < inputDesc.Elements.Length; location++)
                {
                    VertexElementDescription inputElement = inputDesc.Elements[location];

                    attributeDescs[targetIndex] = new Silk.NET.Vulkan.VertexInputAttributeDescription()
                    {
                        Format = VkFormats.VdToVkVertexElementFormat(inputElement.Format),
                        Binding = (uint)binding,
                        Location = (uint)(targetLocation + location),
                        Offset = inputElement.Offset != 0 ? inputElement.Offset : currentOffset
                    };

                    targetIndex += 1;
                    currentOffset += FormatHelpers.GetSizeInBytes(inputElement.Format);
                }

                targetLocation += inputDesc.Elements.Length;
            }

            vertexInputCI.VertexBindingDescriptionCount = bindingCount;
            vertexInputCI.PVertexBindingDescriptions = bindingDescs;
            vertexInputCI.VertexAttributeDescriptionCount = attributeCount;
            vertexInputCI.PVertexAttributeDescriptions = attributeDescs;

            pipelineCI.PVertexInputState = &vertexInputCI;

            // Shader Stage

            Silk.NET.Vulkan.SpecializationInfo specializationInfo = new Silk.NET.Vulkan.SpecializationInfo();
            SpecializationConstant[] specDescs = description.ShaderSet.Specializations;
            if (specDescs != null)
            {
                uint specDataSize = 0;
                foreach (SpecializationConstant spec in specDescs)
                {
                    specDataSize += VkFormats.GetSpecializationConstantSize(spec.Type);
                }
                byte* fullSpecData = stackalloc byte[(int)specDataSize];
                int specializationCount = specDescs.Length;
                Silk.NET.Vulkan.SpecializationMapEntry* mapEntries = stackalloc Silk.NET.Vulkan.SpecializationMapEntry[specializationCount];
                uint specOffset = 0;
                for (int i = 0; i < specializationCount; i++)
                {
                    ulong data = specDescs[i].Data;
                    byte* srcData = (byte*)&data;
                    uint dataSize = VkFormats.GetSpecializationConstantSize(specDescs[i].Type);
                    Unsafe.CopyBlock(fullSpecData + specOffset, srcData, dataSize);
                    mapEntries[i].ConstantID = specDescs[i].ID;
                    mapEntries[i].Offset = specOffset;
                    mapEntries[i].Size = (UIntPtr)dataSize;
                    specOffset += dataSize;
                }
                specializationInfo.DataSize = (UIntPtr)specDataSize;
                specializationInfo.PData = fullSpecData;
                specializationInfo.MapEntryCount = (uint)specializationCount;
                specializationInfo.PMapEntries = mapEntries;
            }

            Shader[] shaders = description.ShaderSet.Shaders;
            StackList<Silk.NET.Vulkan.PipelineShaderStageCreateInfo> stages = new StackList<Silk.NET.Vulkan.PipelineShaderStageCreateInfo>();
            foreach (Shader shader in shaders)
            {
                VkShader vkShader = Util.AssertSubtype<Shader, VkShader>(shader);
                Silk.NET.Vulkan.PipelineShaderStageCreateInfo stageCI = new Silk.NET.Vulkan.PipelineShaderStageCreateInfo();
                stageCI.SType = Silk.NET.Vulkan.StructureType.PipelineShaderStageCreateInfo;

                stageCI.Module = vkShader.ShaderModule;
                stageCI.Stage = VkFormats.VdToVkShaderStages(shader.Stage);
                // stageCI.pName = CommonStrings.main; // Meh
                stageCI.PName = new FixedUtf8String(shader.EntryPoint); // TODO: DONT ALLOCATE HERE
                stageCI.PSpecializationInfo = &specializationInfo;
                stages.Add(stageCI);
            }

            pipelineCI.StageCount = stages.Count;
            pipelineCI.PStages = (Silk.NET.Vulkan.PipelineShaderStageCreateInfo*)stages.Data;

            // ViewportState
            Silk.NET.Vulkan.PipelineViewportStateCreateInfo viewportStateCI = new Silk.NET.Vulkan.PipelineViewportStateCreateInfo();
            viewportStateCI.SType = Silk.NET.Vulkan.StructureType.PipelineViewportStateCreateInfo;

            viewportStateCI.ViewportCount = 1;
            viewportStateCI.ScissorCount = 1;

            pipelineCI.PViewportState = &viewportStateCI;

            // Pipeline Layout
            ResourceLayout[] resourceLayouts = description.ResourceLayouts;
            Silk.NET.Vulkan.PipelineLayoutCreateInfo pipelineLayoutCI = new Silk.NET.Vulkan.PipelineLayoutCreateInfo();
            pipelineLayoutCI.SType = Silk.NET.Vulkan.StructureType.PipelineLayoutCreateInfo;

            pipelineLayoutCI.SetLayoutCount = (uint)resourceLayouts.Length;

            Silk.NET.Vulkan.DescriptorSetLayout* dsls = stackalloc Silk.NET.Vulkan.DescriptorSetLayout[resourceLayouts.Length];
            for (int i = 0; i < resourceLayouts.Length; i++)
            {
                dsls[i] = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(resourceLayouts[i]).DescriptorSetLayout;
            }
            pipelineLayoutCI.PSetLayouts = dsls;

       
            //push constants
            if (description.PushConstantDescription.SizeInBytes > 0)
            {
                Silk.NET.Vulkan.PushConstantRange push_constant = new Silk.NET.Vulkan.PushConstantRange();
                push_constant.Offset = 0;
                push_constant.Size = description.PushConstantDescription.SizeInBytes;
                push_constant.StageFlags = Silk.NET.Vulkan.ShaderStageFlags.ShaderStageVertexBit;

                pipelineLayoutCI.PushConstantRangeCount = 1;
                pipelineLayoutCI.PPushConstantRanges = &push_constant;
            }
     

            _gd.vk.CreatePipelineLayout(_gd.Device, &pipelineLayoutCI, null, out _pipelineLayout);
            pipelineCI.Layout = _pipelineLayout;

            // Create fake RenderPass for compatibility.

            Silk.NET.Vulkan.RenderPassCreateInfo renderPassCI = new Silk.NET.Vulkan.RenderPassCreateInfo();
            renderPassCI.SType = Silk.NET.Vulkan.StructureType.RenderPassCreateInfo;

            OutputDescription outputDesc = description.Outputs;
            StackList<Silk.NET.Vulkan.AttachmentDescription, Size512Bytes> attachments = new StackList<Silk.NET.Vulkan.AttachmentDescription, Size512Bytes>();

            // TODO: A huge portion of this next part is duplicated in VkFramebuffer.cs.

            StackList<Silk.NET.Vulkan.AttachmentDescription> colorAttachmentDescs = new StackList<Silk.NET.Vulkan.AttachmentDescription>();
            StackList<Silk.NET.Vulkan.AttachmentReference> colorAttachmentRefs = new StackList<Silk.NET.Vulkan.AttachmentReference>();
            for (uint i = 0; i < outputDesc.ColorAttachments.Length; i++)
            {
                colorAttachmentDescs[i].Format = VkFormats.VdToVkPixelFormat(outputDesc.ColorAttachments[i].Format);
                colorAttachmentDescs[i].Samples = vkSampleCount;
                colorAttachmentDescs[i].LoadOp = Silk.NET.Vulkan.AttachmentLoadOp.DontCare;
                colorAttachmentDescs[i].StoreOp = Silk.NET.Vulkan.AttachmentStoreOp.Store;
                colorAttachmentDescs[i].StencilLoadOp = Silk.NET.Vulkan.AttachmentLoadOp.DontCare;
                colorAttachmentDescs[i].StencilStoreOp = Silk.NET.Vulkan.AttachmentStoreOp.DontCare;
                colorAttachmentDescs[i].InitialLayout = Silk.NET.Vulkan.ImageLayout.Undefined;
                colorAttachmentDescs[i].FinalLayout = Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal;
                attachments.Add(colorAttachmentDescs[i]);

                colorAttachmentRefs[i].Attachment = i;
                colorAttachmentRefs[i].Layout = Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal;
            }

            Silk.NET.Vulkan.AttachmentDescription depthAttachmentDesc = new Silk.NET.Vulkan.AttachmentDescription();
            Silk.NET.Vulkan.AttachmentReference depthAttachmentRef = new Silk.NET.Vulkan.AttachmentReference();
            if (outputDesc.DepthAttachment != null)
            {
                PixelFormat depthFormat = outputDesc.DepthAttachment.Value.Format;
                bool hasStencil = FormatHelpers.IsStencilFormat(depthFormat);
                depthAttachmentDesc.Format = VkFormats.VdToVkPixelFormat(outputDesc.DepthAttachment.Value.Format, toDepthFormat: true);
                depthAttachmentDesc.Samples = vkSampleCount;
                depthAttachmentDesc.LoadOp = Silk.NET.Vulkan.AttachmentLoadOp.DontCare;
                depthAttachmentDesc.StoreOp = Silk.NET.Vulkan.AttachmentStoreOp.Store;
                depthAttachmentDesc.StencilLoadOp = Silk.NET.Vulkan.AttachmentLoadOp.DontCare;
                depthAttachmentDesc.StencilStoreOp = hasStencil ? Silk.NET.Vulkan.AttachmentStoreOp.Store : Silk.NET.Vulkan.AttachmentStoreOp.DontCare;
                depthAttachmentDesc.InitialLayout = Silk.NET.Vulkan.ImageLayout.Undefined;
                depthAttachmentDesc.FinalLayout = Silk.NET.Vulkan.ImageLayout.DepthStencilAttachmentOptimal;

                depthAttachmentRef.Attachment = (uint)outputDesc.ColorAttachments.Length;
                depthAttachmentRef.Layout = Silk.NET.Vulkan.ImageLayout.DepthStencilAttachmentOptimal;
            }

            Silk.NET.Vulkan.SubpassDescription subpass = new Silk.NET.Vulkan.SubpassDescription();
            subpass.PipelineBindPoint = Silk.NET.Vulkan.PipelineBindPoint.Graphics;
            subpass.ColorAttachmentCount = (uint)outputDesc.ColorAttachments.Length;
            subpass.PColorAttachments = (Silk.NET.Vulkan.AttachmentReference*)colorAttachmentRefs.Data;
            for (int i = 0; i < colorAttachmentDescs.Count; i++)
            {
                attachments.Add(colorAttachmentDescs[i]);
            }

            if (outputDesc.DepthAttachment != null)
            {
                subpass.PDepthStencilAttachment = &depthAttachmentRef;
                attachments.Add(depthAttachmentDesc);
            }

            Silk.NET.Vulkan.SubpassDependency subpassDependency = new Silk.NET.Vulkan.SubpassDependency();
            subpassDependency.SrcSubpass = SubpassExternal;
            subpassDependency.SrcStageMask = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
            subpassDependency.DstStageMask = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
            subpassDependency.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentReadBit |  Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentWriteBit;

            renderPassCI.AttachmentCount = attachments.Count;
            renderPassCI.PAttachments = (Silk.NET.Vulkan.AttachmentDescription*)attachments.Data;
            renderPassCI.SubpassCount = 1;
            renderPassCI.PSubpasses = &subpass;
            renderPassCI.DependencyCount = 1;
            renderPassCI.PDependencies = &subpassDependency;
            renderPassCI.SType = Silk.NET.Vulkan.StructureType.RenderPassCreateInfo;

            var creationResult = _gd.vk.CreateRenderPass(_gd.Device, &renderPassCI, null, out _renderPass);
            CheckResult(creationResult);

            pipelineCI.RenderPass = _renderPass;
            pipelineCI.SType = Silk.NET.Vulkan.StructureType.GraphicsPipelineCreateInfo;

            var result = _gd.vk.CreateGraphicsPipelines(_gd.Device, default, 1, &pipelineCI, null, out _devicePipeline);
            CheckResult(result);

            ResourceSetCount = (uint)description.ResourceLayouts.Length;
            DynamicOffsetsCount = 0;
            foreach (VkResourceLayout layout in description.ResourceLayouts)
            {
                DynamicOffsetsCount += layout.DynamicBufferCount;
            }
        }

        public VkPipeline(VkGraphicsDevice gd, ref ComputePipelineDescription description)
            : base(ref description)
        {
            _gd = gd;
            IsComputePipeline = true;
            RefCount = new ResourceRefCount(DisposeCore);

            Silk.NET.Vulkan.ComputePipelineCreateInfo pipelineCI = new Silk.NET.Vulkan.ComputePipelineCreateInfo();

            // Pipeline Layout
            ResourceLayout[] resourceLayouts = description.ResourceLayouts;
            Silk.NET.Vulkan.PipelineLayoutCreateInfo pipelineLayoutCI = new Silk.NET.Vulkan.PipelineLayoutCreateInfo();
            pipelineLayoutCI.SetLayoutCount = (uint)resourceLayouts.Length;
            pipelineLayoutCI.SType = Silk.NET.Vulkan.StructureType.PipelineLayoutCreateInfo;

            Silk.NET.Vulkan.DescriptorSetLayout* dsls = stackalloc Silk.NET.Vulkan.DescriptorSetLayout[resourceLayouts.Length];
            for (int i = 0; i < resourceLayouts.Length; i++)
            {
                dsls[i] = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(resourceLayouts[i]).DescriptorSetLayout;
            }
            pipelineLayoutCI.PSetLayouts = dsls;

            _gd.vk.CreatePipelineLayout(_gd.Device, &pipelineLayoutCI, null, out _pipelineLayout);
            pipelineCI.Layout = _pipelineLayout;

            // Shader Stage

            Silk.NET.Vulkan.SpecializationInfo specializationInfo;
            SpecializationConstant[] specDescs = description.Specializations;
            if (specDescs != null)
            {
                uint specDataSize = 0;
                foreach (SpecializationConstant spec in specDescs)
                {
                    specDataSize += VkFormats.GetSpecializationConstantSize(spec.Type);
                }
                byte* fullSpecData = stackalloc byte[(int)specDataSize];
                int specializationCount = specDescs.Length;
                Silk.NET.Vulkan.SpecializationMapEntry* mapEntries = stackalloc Silk.NET.Vulkan.SpecializationMapEntry[specializationCount];
                uint specOffset = 0;
                for (int i = 0; i < specializationCount; i++)
                {
                    ulong data = specDescs[i].Data;
                    byte* srcData = (byte*)&data;
                    uint dataSize = VkFormats.GetSpecializationConstantSize(specDescs[i].Type);
                    Unsafe.CopyBlock(fullSpecData + specOffset, srcData, dataSize);
                    mapEntries[i].ConstantID = specDescs[i].ID;
                    mapEntries[i].Offset = specOffset;
                    mapEntries[i].Size = (UIntPtr)dataSize;
                    specOffset += dataSize;
                }
                specializationInfo.DataSize = (UIntPtr)specDataSize;
                specializationInfo.PData = fullSpecData;
                specializationInfo.MapEntryCount = (uint)specializationCount;
                specializationInfo.PMapEntries = mapEntries;
            }

            Shader shader = description.ComputeShader;
            VkShader vkShader = Util.AssertSubtype<Shader, VkShader>(shader);
            Silk.NET.Vulkan.PipelineShaderStageCreateInfo stageCI = new Silk.NET.Vulkan.PipelineShaderStageCreateInfo();
            stageCI.SType = Silk.NET.Vulkan.StructureType.PipelineShaderStageCreateInfo;

            stageCI.Module = vkShader.ShaderModule;
            stageCI.Stage = VkFormats.VdToVkShaderStages(shader.Stage);
            stageCI.PName = CommonStrings.main; // Meh
            stageCI.PSpecializationInfo = &specializationInfo;
            pipelineCI.Stage = stageCI;

            var result = _gd.vk.CreateComputePipelines(
                _gd.Device,
                default,
                1,
                &pipelineCI,
                null,
                out _devicePipeline);
            CheckResult(result);

            ResourceSetCount = (uint)description.ResourceLayouts.Length;
            DynamicOffsetsCount = 0;
            foreach (VkResourceLayout layout in description.ResourceLayouts)
            {
                DynamicOffsetsCount += layout.DynamicBufferCount;
            }
        }

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                _gd.SetResourceName(this, value);
            }
        }

        public override void Dispose()
        {
            RefCount.Decrement();
        }

        private void DisposeCore()
        {
            if (!_destroyed)
            {
                _destroyed = true;
                _gd.vk.DestroyPipelineLayout(_gd.Device, _pipelineLayout, null);
                _gd.vk.DestroyPipeline(_gd.Device, _devicePipeline, null);
                if (!IsComputePipeline)
                {
                    _gd.vk.DestroyRenderPass(_gd.Device, _renderPass, null);
                }
            }
        }
    }
}
