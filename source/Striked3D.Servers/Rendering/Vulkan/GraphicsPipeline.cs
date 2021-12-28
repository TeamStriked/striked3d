using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Striked3D.Assets;
using Striked3D.Core;
using Striked3D.Engine;
using Striked3D.Servers.Camera;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class GraphicsPipeline
    {
        private RenderingInstance _instance;
        private RenderPass _renderPass;

        private PipelineLayout _pipelineLayout;
        private Pipeline _graphicsPipeline;
        private List<ShaderModule> shaderModules = new List<ShaderModule>();

        private bool isCreated = false;

        public bool IsCreated { get { return isCreated; } }

        public PipelineLayout NativeLayoutHandle
        {
            get { return _pipelineLayout; }
        }

        public RenderPass RenderPass
        {
            get { return _renderPass; }
        }
        public Pipeline NativeHandle
        {
            get { return _graphicsPipeline; }
        }

        public unsafe void Destroy()
        {
            foreach (var mod in shaderModules)
            {
                this._instance.Api.DestroyShaderModule(this._renderPass.Swapchain.Device.NativeHandle,
                    mod, null);
            }

            shaderModules.Clear();

            if(_graphicsPipeline.Handle != default)
            {
                this._instance.Api.DestroyPipeline(this._renderPass.Swapchain.Device.NativeHandle,
                _graphicsPipeline, null);
            }

            if (_pipelineLayout.Handle != default)
            {
                this._instance.Api.DestroyPipelineLayout(this._renderPass.Swapchain.Device.NativeHandle,
                _pipelineLayout, null);
            }
        }

        public GraphicsPipeline()
        {
            shaderModules = new List<ShaderModule>();
        }

        private unsafe PipelineShaderStageCreateInfo createShader(string name, ShaderStageFlags type)
        {
            var read = ResourceReader.GetEmbeddedResourceFile (name);
            var newPath = System.IO.Path.Combine(EngineInfo.GetUserDir(), name);
            shaderc.Compiler comp = new shaderc.Compiler();
            shaderc.Result res = null;

            if (type == ShaderStageFlags.ShaderStageVertexBit)
            {
                 res = comp.Compile(read, newPath, shaderc.ShaderKind.VertexShader);
            }
            else if (type == ShaderStageFlags.ShaderStageFragmentBit)
            {
                res = comp.Compile(read, newPath, shaderc.ShaderKind.FragmentShader);
            }

            if (res == null || res.Status != shaderc.Status.Success)
            {
                throw new Exception("Cant parse shader");
            }
       
            var vertShaderModule = CreateShaderModule(res);
            var vertShaderStageInfo = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = type,
                Module = vertShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

            shaderModules.Add(vertShaderModule);
            return vertShaderStageInfo;
        }

        public VertexInputDescription info { get; set; }
        public MaterialParameters parameters { get; set; }
        
        public unsafe void Instanciate(MaterialParameters _parameters, RenderingInstance _instance, RenderPass _renderPass)
        {
            parameters = _parameters;

            if (parameters.shaders == null || parameters.shaders.Count <= 0)
            {
                return;
            }

            this._instance = _instance;
            this._renderPass = _renderPass;

            List<PipelineShaderStageCreateInfo> shaders = new List<PipelineShaderStageCreateInfo>();

            foreach(var shaderParam in parameters.shaders)
            {
                shaders.Add(createShader(shaderParam.Value, shaderParam.Key));
            }

            var shaderStages = stackalloc PipelineShaderStageCreateInfo[shaders.Count];
            int shaderId = 0;
            foreach(var shader in shaders)
            {
                shaderStages[shaderId++] = shader;
            }

            Span<VertexInputAttributeDescription> attribute_desc = stackalloc VertexInputAttributeDescription[info.attributes.Count];
            var attributeId = 0;
            foreach (var attribute in info.attributes)
            {
                attribute_desc[attributeId++] = attribute;
            }

            Span<VertexInputBindingDescription> binding_desc = stackalloc VertexInputBindingDescription[info.bindings.Count];
            var bindingId = 0;
            foreach (var binding in info.bindings)
            {
                binding_desc[bindingId++] = binding;
            }

            var vertexInputInfo = new PipelineVertexInputStateCreateInfo();
            vertexInputInfo.SType = StructureType.PipelineVertexInputStateCreateInfo;
            vertexInputInfo.VertexBindingDescriptionCount = (uint)binding_desc.Length;
            vertexInputInfo.PVertexBindingDescriptions = (VertexInputBindingDescription*)Unsafe.AsPointer(ref binding_desc[0]);
            vertexInputInfo.VertexAttributeDescriptionCount = (uint)attribute_desc.Length;
            vertexInputInfo.PVertexAttributeDescriptions = (VertexInputAttributeDescription*)Unsafe.AsPointer(ref attribute_desc[0]);

            var inputAssembly = new PipelineInputAssemblyStateCreateInfo
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = Vk.False
            };

            var viewport = new Viewport
            {
                X = 0.0f,
                Y = 0.0f,
                Width = this._renderPass.Swapchain.SwapchainExtent.Width,
                Height = this._renderPass.Swapchain.SwapchainExtent.Height,
                MinDepth = 0.0f,
                MaxDepth = 1.0f
            };

            var scissor = new Rect2D { Offset = default, Extent = this._renderPass.Swapchain.SwapchainExtent };
            var viewportState = new PipelineViewportStateCreateInfo
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                PViewports = &viewport,
                ScissorCount = 1,
                PScissors = &scissor
            };

            var rasterizer = new PipelineRasterizationStateCreateInfo
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = Vk.False,
                RasterizerDiscardEnable = Vk.False,
                PolygonMode = parameters.polygonMode,
                LineWidth = 1.0f,
                CullMode = parameters.cullMode,
                FrontFace = parameters.frontFace,
                DepthBiasEnable = Vk.False
            };

            var multisampling = new PipelineMultisampleStateCreateInfo
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = Vk.False,
                RasterizationSamples = this._renderPass.Swapchain.Device.MsaaLevel
            };

            var colorBlendAttachment = new PipelineColorBlendAttachmentState
            {
                ColorWriteMask = ColorComponentFlags.ColorComponentRBit |
                                 ColorComponentFlags.ColorComponentGBit |
                                 ColorComponentFlags.ColorComponentBBit |
                                 ColorComponentFlags.ColorComponentABit,
                 SrcColorBlendFactor = BlendFactor.SrcAlpha,
                 DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                 ColorBlendOp = BlendOp.Add,
                 SrcAlphaBlendFactor = BlendFactor.SrcAlpha,
                 DstAlphaBlendFactor = BlendFactor.DstAlpha,
                 AlphaBlendOp = BlendOp.Max,
                 BlendEnable = parameters.blendEnabled ? Vk.True : Vk.False
            };

            var colorBlending = new PipelineColorBlendStateCreateInfo
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = Vk.False,
                LogicOp = parameters.logic,
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachment
            };

            colorBlending.BlendConstants[0] = 0.0f;
            colorBlending.BlendConstants[1] = 0.0f;
            colorBlending.BlendConstants[2] = 0.0f;
            colorBlending.BlendConstants[3] = 0.0f;

            var vertPushConst = new PushConstantRange();
            vertPushConst.StageFlags = ShaderStageFlags.ShaderStageVertexBit;
            vertPushConst.Offset = 0;
            vertPushConst.Size =  (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(CameraData)) + (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshPushConstants));
            var pipelineLayoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 0,
                PushConstantRangeCount = 1,
                PPushConstantRanges = (PushConstantRange*)Unsafe.AsPointer(ref vertPushConst)
            };

            fixed (PipelineLayout* pipelineLayout = &_pipelineLayout)
            {
                if (this._instance.Api.CreatePipelineLayout(
                    this._renderPass.Swapchain.Device.NativeHandle, 
                    &pipelineLayoutInfo, null, pipelineLayout) != Result.Success)
                {
                    throw new Exception("failed to create pipeline layout!");
                }
            }

            Span<DynamicState> dynamic_states = stackalloc DynamicState[] { DynamicState.Viewport, DynamicState.Scissor };
            var dynamic_state = new PipelineDynamicStateCreateInfo();
            dynamic_state.SType = StructureType.PipelineDynamicStateCreateInfo;
            dynamic_state.DynamicStateCount = (uint)dynamic_states.Length;
            dynamic_state.PDynamicStates = (DynamicState*)Unsafe.AsPointer(ref dynamic_states[0]);


            var stencilState = new StencilOpState();
            stencilState.CompareOp = CompareOp.Always;

            var depthBlending = new PipelineDepthStencilStateCreateInfo
            {
                SType = StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = Vk.False,
                DepthWriteEnable = Vk.False,
                DepthCompareOp = parameters.depthCompareOp,
                DepthBoundsTestEnable = Vk.False,
                StencilTestEnable = Vk.False,
                MinDepthBounds = 0f,
                MaxDepthBounds = 0f,
                Back = stencilState,
            };

            colorBlending.BlendConstants[0] = 0.0f;
            colorBlending.BlendConstants[1] = 0.0f;
            colorBlending.BlendConstants[2] = 0.0f;
            colorBlending.BlendConstants[3] = 0.0f;

            var pipelineInfo = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = shaderStages,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PDynamicState = &dynamic_state,
                PColorBlendState = &colorBlending,
                PDepthStencilState = &depthBlending,
                Layout = _pipelineLayout,
                RenderPass = this._renderPass.NativeHandle,
                Subpass = 0,
                BasePipelineHandle = default
            };

            fixed (Pipeline* graphicsPipeline = &_graphicsPipeline)
            {
                if (this._instance.Api.CreateGraphicsPipelines
                        (this._renderPass.Swapchain.Device.NativeHandle, 
                        default, 1, &pipelineInfo, null, graphicsPipeline) != Result.Success)
                {
                    throw new Exception("failed to create graphics pipeline!");
                }
            }

            isCreated = true;
        }

        private unsafe ShaderModule CreateShaderModule(shaderc.Result res)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)res.CodeLength,
            };

            var destination = (byte*) res.CodePointer.ToPointer();
            createInfo.PCode = (uint*)destination;
         

            var shaderModule = new ShaderModule();
            if (this._instance.Api.CreateShaderModule(this._renderPass.Swapchain.Device.NativeHandle, &createInfo, null, &shaderModule) != Result.Success)
            {
                throw new Exception("failed to create shader module!");
            }

            return shaderModule;
        }

        byte[] LoadEmbeddedResourceBytes(string path)
        {
            var shaderPath = System.IO.Path.Combine(Engine.EngineInfo.GetShaderDictonary(), path);
            Logger.Debug(this, "Load shader from " + shaderPath);
            using (var ms = new MemoryStream())
            {
                using (FileStream file = new FileStream(shaderPath, FileMode.Open, FileAccess.Read))
                {
                    file.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

    }
}
