using System;
using static Veldrid.Vk.VulkanUtil;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace Veldrid.Vk
{
    internal unsafe class VkCommandList : CommandList
    {
        private readonly VkGraphicsDevice _gd;
        private Silk.NET.Vulkan.CommandPool _pool;
        private Silk.NET.Vulkan.CommandBuffer _cb;
        private bool _destroyed;

        private bool _commandBufferBegun;
        private bool _commandBufferEnded;
        private Silk.NET.Vulkan.Rect2D[] _scissorRects = Array.Empty<Silk.NET.Vulkan.Rect2D>();

        private Silk.NET.Vulkan.ClearValue[] _clearValues = Array.Empty<Silk.NET.Vulkan.ClearValue>();
        private bool[] _validColorClearValues = Array.Empty<bool>();
        private Silk.NET.Vulkan.ClearValue? _depthClearValue;
        private readonly List<VkTexture> _preDrawSampledImages = new List<VkTexture>();

        // Graphics State
        private VkFramebufferBase _currentFramebuffer;
        private bool _currentFramebufferEverActive;
        private Silk.NET.Vulkan.RenderPass _activeRenderPass;
        private VkPipeline _currentGraphicsPipeline;
        private BoundResourceSetInfo[] _currentGraphicsResourceSets = Array.Empty<BoundResourceSetInfo>();
        private bool[] _graphicsResourceSetsChanged;

        private bool _newFramebuffer; // Render pass cycle state

        // Compute State
        private VkPipeline _currentComputePipeline;
        private BoundResourceSetInfo[] _currentComputeResourceSets = Array.Empty<BoundResourceSetInfo>();
        private bool[] _computeResourceSetsChanged;
        private string _name;

        private readonly object _commandBufferListLock = new object();
        private readonly Queue<Silk.NET.Vulkan.CommandBuffer> _availableCommandBuffers = new Queue<Silk.NET.Vulkan.CommandBuffer>();
        private readonly List<Silk.NET.Vulkan.CommandBuffer> _submittedCommandBuffers = new List<Silk.NET.Vulkan.CommandBuffer>();

        private StagingResourceInfo _currentStagingInfo;
        private readonly object _stagingLock = new object();
        private readonly Dictionary<Silk.NET.Vulkan.CommandBuffer, StagingResourceInfo> _submittedStagingInfos = new Dictionary<Silk.NET.Vulkan.CommandBuffer, StagingResourceInfo>();
        private readonly List<StagingResourceInfo> _availableStagingInfos = new List<StagingResourceInfo>();
        private readonly List<VkBuffer> _availableStagingBuffers = new List<VkBuffer>();

        public Silk.NET.Vulkan.CommandPool CommandPool => _pool;
        public Silk.NET.Vulkan.CommandBuffer CommandBuffer => _cb;

        public ResourceRefCount RefCount { get; }

        public override bool IsDisposed => _destroyed;

        private readonly Silk.NET.Vulkan.Vk _vk;

        public VkCommandList( VkGraphicsDevice gd, ref CommandListDescription description)
            : base(ref description, gd.Features, gd.UniformBufferMinOffsetAlignment, gd.StructuredBufferMinOffsetAlignment)
        {
            _gd = gd;
            _vk = gd.vk;

            var poolCI = new Silk.NET.Vulkan.CommandPoolCreateInfo();
            poolCI.SType = Silk.NET.Vulkan.StructureType.CommandPoolCreateInfo;
            poolCI.Flags = Silk.NET.Vulkan.CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit;
            poolCI.QueueFamilyIndex = gd.GraphicsQueueIndex;
            var result = _vk.CreateCommandPool(_gd.Device, &poolCI, null, out _pool);
            CheckResult(result);

            isDeclaredAsSubpass = description.isSubpass;

            _cb = GetNextCommandBuffer();
            RefCount = new ResourceRefCount(DisposeCore);
        }

        private Silk.NET.Vulkan.CommandBuffer GetNextCommandBuffer()
        {
            lock (_commandBufferListLock)
            {
                if (_availableCommandBuffers.Count > 0)
                {
                    Silk.NET.Vulkan.CommandBuffer cachedCB = _availableCommandBuffers.Dequeue();
                    var resetResult = _vk.ResetCommandBuffer(cachedCB, 0);
                    CheckResult(resetResult);
                    return cachedCB;
                }
            }

            var cbAI = new Silk.NET.Vulkan.CommandBufferAllocateInfo();
            cbAI.SType = Silk.NET.Vulkan.StructureType.CommandBufferAllocateInfo;
            cbAI.CommandPool = _pool;
            cbAI.CommandBufferCount = 1;
            cbAI.Level = isDeclaredAsSubpass ? Silk.NET.Vulkan.CommandBufferLevel.Secondary : Silk.NET.Vulkan.CommandBufferLevel.Primary;
            var result = _vk.AllocateCommandBuffers(_gd.Device,&cbAI, out Silk.NET.Vulkan.CommandBuffer cb);
            CheckResult(result);
            return cb;
        }

        public void CommandBufferSubmitted(Silk.NET.Vulkan.CommandBuffer cb)
        {
            RefCount.Increment();
            foreach (ResourceRefCount rrc in _currentStagingInfo.Resources)
            {
                rrc.Increment();
            }

            _submittedStagingInfos.Add(cb, _currentStagingInfo);
            _currentStagingInfo = null;

            if (this.submitCommands != null)
            {
                foreach (var command in submitCommands)
                {
                    (command as VkCommandList).CommandBufferSubmitted((command as VkCommandList).CommandBuffer);
                }
            }
        }

        public void CommandBufferCompleted(Silk.NET.Vulkan.CommandBuffer completedCB)
        {
            lock (_commandBufferListLock)
            {
                for (int i = 0; i < _submittedCommandBuffers.Count; i++)
                {
                    Silk.NET.Vulkan.CommandBuffer submittedCB = _submittedCommandBuffers[i];
                    if (submittedCB.Handle == completedCB.Handle)
                    {
                        _availableCommandBuffers.Enqueue(completedCB);
                        _submittedCommandBuffers.RemoveAt(i);

                        if(submitCommands != null)
                        {
                            foreach(VkCommandList pass in submitCommands)
                            {
                                var getSubmittedBuffer = pass._submittedCommandBuffers[i];
                                pass.CommandBufferCompleted(getSubmittedBuffer);
                            }
                        }


                        i -= 1;
                    }
                }
            }

            lock (_stagingLock)
            {
                if (_submittedStagingInfos.TryGetValue(completedCB, out StagingResourceInfo info))
                {
                    RecycleStagingInfo(info);
                    _submittedStagingInfos.Remove(completedCB);
                }
            }

            RefCount.Decrement();
        }
   
        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            var clearValue = new Silk.NET.Vulkan.ClearValue
            {
                Color = new  Silk.NET.Vulkan.ClearColorValue(clearColor.R, clearColor.G, clearColor.B, clearColor.A)
            };

            if (_activeRenderPass.Handle != default)
            {
                var clearAttachment = new Silk.NET.Vulkan.ClearAttachment
                {
                    ColorAttachment = index,
                    AspectMask = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit,
                    ClearValue = clearValue
                };

                Texture colorTex = _currentFramebuffer.ColorTargets[(int)index].Target;
                var clearRect = new Silk.NET.Vulkan.ClearRect
                {
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                    Rect = new Silk.NET.Vulkan.Rect2D(new Silk.NET.Vulkan.Offset2D(0, 0),
                           new Silk.NET.Vulkan.Extent2D(colorTex.Width, colorTex.Height))
                };

                _vk.CmdClearAttachments(_cb, 1,  &clearAttachment, 1,  &clearRect);
            }
            else
            {
                // Queue up the clear value for the next RenderPass.
                _clearValues[index] = clearValue;
                _validColorClearValues[index] = true;
            }
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            var clearValue = new Silk.NET.Vulkan.ClearValue { DepthStencil = new Silk.NET.Vulkan.ClearDepthStencilValue(depth, stencil) };

            if (_activeRenderPass.Handle != default)
            {
                Silk.NET.Vulkan.ImageAspectFlags aspect = FormatHelpers.IsStencilFormat(_currentFramebuffer.DepthTarget.Value.Target.Format)
                    ? Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit | Silk.NET.Vulkan.ImageAspectFlags.ImageAspectStencilBit
                    : Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit;

                var clearAttachment = new Silk.NET.Vulkan.ClearAttachment
                {
                    AspectMask = aspect,
                    ClearValue = clearValue
                };

                uint renderableWidth = _currentFramebuffer.RenderableWidth;
                uint renderableHeight = _currentFramebuffer.RenderableHeight;
                if (renderableWidth > 0 && renderableHeight > 0)
                {
                    var clearRect = new Silk.NET.Vulkan.ClearRect
                    {
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                        Rect = new Silk.NET.Vulkan.Rect2D(new Silk.NET.Vulkan.Offset2D(0, 0),
                           new Silk.NET.Vulkan.Extent2D(renderableWidth, renderableHeight))
                    };

                    _vk.CmdClearAttachments(_cb, 1, &clearAttachment, 1, &clearRect);
                }
            }
            else
            {
                // Queue up the clear value for the next RenderPass.
                _depthClearValue = clearValue;
            }
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            PreDrawCommand();
            _vk.CmdDraw(_cb, vertexCount, instanceCount, vertexStart, instanceStart);
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            PreDrawCommand();
            _vk.CmdDrawIndexed(_cb, indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            PreDrawCommand();
            VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(indirectBuffer);
            _currentStagingInfo.Resources.Add(vkBuffer.RefCount);
            _vk.CmdDrawIndirect(_cb, vkBuffer.DeviceBuffer, offset, drawCount, stride);
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            PreDrawCommand();
            VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(indirectBuffer);
            _currentStagingInfo.Resources.Add(vkBuffer.RefCount);
            _vk.CmdDrawIndexedIndirect(_cb, vkBuffer.DeviceBuffer, offset, drawCount, stride);
        }

        private void PreDrawCommand()
        {
            TransitionImages(_preDrawSampledImages, Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal);
            _preDrawSampledImages.Clear();

            EnsureRenderPassActive();

            FlushNewResourceSets(
                _currentGraphicsResourceSets,
                _graphicsResourceSetsChanged,
                _currentGraphicsPipeline.ResourceSetCount,
                Silk.NET.Vulkan.PipelineBindPoint.Graphics,
                _currentGraphicsPipeline.PipelineLayout);
        }

        private void FlushNewResourceSets(
            BoundResourceSetInfo[] resourceSets,
            bool[] resourceSetsChanged,
            uint resourceSetCount,
            Silk.NET.Vulkan.PipelineBindPoint bindPoint,
            Silk.NET.Vulkan.PipelineLayout pipelineLayout)
        {
            VkPipeline pipeline = bindPoint == Silk.NET.Vulkan.PipelineBindPoint.Graphics ? _currentGraphicsPipeline : _currentComputePipeline;

            Silk.NET.Vulkan.DescriptorSet* descriptorSets = stackalloc Silk.NET.Vulkan.DescriptorSet[(int)resourceSetCount];
            uint* dynamicOffsets = stackalloc uint[pipeline.DynamicOffsetsCount];
            uint currentBatchCount = 0;
            uint currentBatchFirstSet = 0;
            uint currentBatchDynamicOffsetCount = 0;

            for (uint currentSlot = 0; currentSlot < resourceSetCount; currentSlot++)
            {
                bool batchEnded = !resourceSetsChanged[currentSlot] || currentSlot == resourceSetCount - 1;

                if (resourceSetsChanged[currentSlot])
                {
                    resourceSetsChanged[currentSlot] = false;
                    VkResourceSet vkSet = Util.AssertSubtype<ResourceSet, VkResourceSet>(resourceSets[currentSlot].Set);
                    descriptorSets[currentBatchCount] = vkSet.DescriptorSet;
                    currentBatchCount += 1;

                    ref SmallFixedOrDynamicArray curSetOffsets = ref resourceSets[currentSlot].Offsets;
                    for (uint i = 0; i < curSetOffsets.Count; i++)
                    {
                        dynamicOffsets[currentBatchDynamicOffsetCount] = curSetOffsets.Get(i);
                        currentBatchDynamicOffsetCount += 1;
                    }

                    // Increment ref count on first use of a set.
                    _currentStagingInfo.Resources.Add(vkSet.RefCount);
                    for (int i = 0; i < vkSet.RefCounts.Count; i++)
                    {
                        _currentStagingInfo.Resources.Add(vkSet.RefCounts[i]);
                    }
                }

                if (batchEnded)
                {
                    if (currentBatchCount != 0)
                    {
                        // Flush current batch.
                        _vk.CmdBindDescriptorSets(
                            _cb,
                            bindPoint,
                            pipelineLayout,
                            currentBatchFirstSet,
                            currentBatchCount,
                            descriptorSets,
                            currentBatchDynamicOffsetCount,
                            dynamicOffsets);
                    }

                    currentBatchCount = 0;
                    currentBatchFirstSet = currentSlot + 1;
                }
            }
        }

        private void TransitionImages(List<VkTexture> sampledTextures, Silk.NET.Vulkan.ImageLayout layout)
        {
            for (int i = 0; i < sampledTextures.Count; i++)
            {
                VkTexture tex = sampledTextures[i];
                tex.TransitionImageLayout(_cb, 0, tex.MipLevels, 0, tex.ArrayLayers, layout);
            }
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            PreDispatchCommand();

            _vk.CmdDispatch(_cb, groupCountX, groupCountY, groupCountZ);
        }

        private void PreDispatchCommand()
        {
            EnsureNoRenderPass();

            for (uint currentSlot = 0; currentSlot < _currentComputePipeline.ResourceSetCount; currentSlot++)
            {
                VkResourceSet vkSet = Util.AssertSubtype<ResourceSet, VkResourceSet>(
                    _currentComputeResourceSets[currentSlot].Set);

                TransitionImages(vkSet.SampledTextures, Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal);
                TransitionImages(vkSet.StorageTextures, Silk.NET.Vulkan.ImageLayout.General);
                for (int texIdx = 0; texIdx < vkSet.StorageTextures.Count; texIdx++)
                {
                    VkTexture storageTex = vkSet.StorageTextures[texIdx];
                    if ((storageTex.Usage & TextureUsage.Sampled) != 0)
                    {
                        _preDrawSampledImages.Add(storageTex);
                    }
                }
            }

            FlushNewResourceSets(
                _currentComputeResourceSets,
                _computeResourceSetsChanged,
                _currentComputePipeline.ResourceSetCount,
                Silk.NET.Vulkan.PipelineBindPoint.Compute,
                _currentComputePipeline.PipelineLayout);
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
            PreDispatchCommand();

            VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(indirectBuffer);
            _currentStagingInfo.Resources.Add(vkBuffer.RefCount);
            _vk.CmdDispatchIndirect(_cb, vkBuffer.DeviceBuffer, offset);
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            if (_activeRenderPass.Handle != default)
            {
                EndCurrentRenderPass();
            }

            VkTexture vkSource = Util.AssertSubtype<Texture, VkTexture>(source);
            _currentStagingInfo.Resources.Add(vkSource.RefCount);
            VkTexture vkDestination = Util.AssertSubtype<Texture, VkTexture>(destination);
            _currentStagingInfo.Resources.Add(vkDestination.RefCount);
            Silk.NET.Vulkan.ImageAspectFlags aspectFlags = ((source.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil)
                ? Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit | Silk.NET.Vulkan.ImageAspectFlags.ImageAspectStencilBit
                : Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit;
            var region = new Silk.NET.Vulkan.ImageResolve
            {
                Extent = new Silk.NET.Vulkan.Extent3D { Width = source.Width, Height = source.Height, Depth = source.Depth },
                SrcSubresource = new Silk.NET.Vulkan.ImageSubresourceLayers { LayerCount = 1, AspectMask = aspectFlags },
                DstSubresource = new Silk.NET.Vulkan.ImageSubresourceLayers { LayerCount = 1, AspectMask = aspectFlags }
            };

            vkSource.TransitionImageLayout(_cb, 0, 1, 0, 1, Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal);
            vkDestination.TransitionImageLayout(_cb, 0, 1, 0, 1, Silk.NET.Vulkan.ImageLayout.TransferDstOptimal);

            _vk.CmdResolveImage(
                _cb,
                vkSource.OptimalDeviceImage,
                 Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal,
                vkDestination.OptimalDeviceImage,
                Silk.NET.Vulkan.ImageLayout.TransferDstOptimal,
                1,
                &region);

            if ((vkDestination.Usage & TextureUsage.Sampled) != 0)
            {
                vkDestination.TransitionImageLayout(_cb, 0, 1, 0, 1, Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal);
            }
        }

        public override void Begin()
        {
            if (_commandBufferBegun)
            {
                throw new VeldridException(
                    "CommandList must be in its initial state, or End() must have been called, for Begin() to be valid to call.");
            }
            if (_commandBufferEnded)
            {
                _commandBufferEnded = false;
                _cb = GetNextCommandBuffer();
                if (_currentStagingInfo != null)
                {
                    RecycleStagingInfo(_currentStagingInfo);
                }
            }

            _currentStagingInfo = GetStagingResourceInfo();

            Silk.NET.Vulkan.CommandBufferBeginInfo beginInfo = new Silk.NET.Vulkan.CommandBufferBeginInfo();
            beginInfo.SType = Silk.NET.Vulkan.StructureType.CommandBufferBeginInfo;
            beginInfo.Flags = Silk.NET.Vulkan.CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit;
            _vk.BeginCommandBuffer(_cb, &beginInfo);
            _commandBufferBegun = true;

            ClearCachedState();
            _currentFramebuffer = null;
            _currentGraphicsPipeline = null;
            ClearSets(_currentGraphicsResourceSets);
            Util.ClearArray(_scissorRects);

            _currentComputePipeline = null;
            ClearSets(_currentComputeResourceSets);
        }

        public override void BeginWithSubpasses()
        {
            this._hasSubPasses = true;

            if (!_currentFramebufferEverActive && _currentFramebuffer != null)
            {
                BeginCurrentRenderPass(Silk.NET.Vulkan.SubpassContents.SecondaryCommandBuffers);
            }
        }

        public override void BeginAsSubpass(CommandList mainBuffer)
        {
            this.mainPass = mainBuffer;

            EnsureRenderPassActive();

            if (isDeclaredAsSubpass == false)
            {
                throw new VeldridException("CommandBuffer is not an sub pass.");
            }

            if (_commandBufferBegun)
            {
                throw new VeldridException(
                    "CommandList must be in its initial state, or End() must have been called, for Begin() to be valid to call.");
            }

            if (_commandBufferEnded)
            {
                _commandBufferEnded = false;
                _cb = GetNextCommandBuffer();
                if (_currentStagingInfo != null)
                {
                    RecycleStagingInfo(_currentStagingInfo);
                }
            }

            _currentStagingInfo = GetStagingResourceInfo();

            var mainPassBuffer = (VkCommandList)mainBuffer;

            Silk.NET.Vulkan.CommandBufferBeginInfo beginInfo = new Silk.NET.Vulkan.CommandBufferBeginInfo();
            beginInfo.SType = Silk.NET.Vulkan.StructureType.CommandBufferBeginInfo;
            var inheroInfo = new Silk.NET.Vulkan.CommandBufferInheritanceInfo();
            inheroInfo.SType = Silk.NET.Vulkan.StructureType.CommandBufferInheritanceInfo;
            inheroInfo.Framebuffer = mainPassBuffer._currentFramebuffer.CurrentFramebuffer;
            inheroInfo.RenderPass = mainPassBuffer._activeRenderPass;
            beginInfo.Flags = Silk.NET.Vulkan.CommandBufferUsageFlags.CommandBufferUsageRenderPassContinueBit;
            beginInfo.PInheritanceInfo = &inheroInfo;

            _vk.BeginCommandBuffer(_cb, &beginInfo);
            _commandBufferBegun = true;

            ClearCachedState();
            _currentFramebuffer = null;
            _currentGraphicsPipeline = null;
            ClearSets(_currentGraphicsResourceSets);
            Util.ClearArray(_scissorRects);

            _currentComputePipeline = null;
            ClearSets(_currentComputeResourceSets);
            SetFrameBufferFromMainPass(mainPassBuffer);
        }
        private CommandList[] submitCommands = null;
        public override void EndWithSubpasses(CommandList[] subCommands)
        {
            _commandBufferBegun = false;
            _commandBufferEnded = true;

            this.submitCommands = subCommands;

            var commandList =new  Silk.NET.Vulkan.CommandBuffer[subCommands.Length];
            int i = 0;
            foreach(var subCommand in subCommands)
            {
                commandList[i] = (subCommand as VkCommandList).CommandBuffer;
                i++;
            }

            fixed (Silk.NET.Vulkan.CommandBuffer* bufferPtr = &commandList[0])
            {
                _vk.CmdExecuteCommands(_cb, (uint)commandList.Length, bufferPtr);
            }

            if (_activeRenderPass.Handle != default)
            {
                EndCurrentRenderPass();
                _currentFramebuffer.TransitionToFinalLayout(_cb);
            }

            _vk.EndCommandBuffer(_cb);
            _submittedCommandBuffers.Add(_cb);
        }

        public override void EndAsSubpass()
        {
            if (mainPass == null || isDeclaredAsSubpass == false)
            {
                throw new VeldridException(
                    "CommandList ist not an subpass.");
            }

            _commandBufferBegun = false;
            _commandBufferEnded = true;

            _vk.EndCommandBuffer(_cb);
            _submittedCommandBuffers.Add(_cb);

            this.mainPass = null;
            _activeRenderPass.Handle = default;
            _framebuffer = null;
            _currentFramebufferEverActive = false;
            _currentFramebuffer = null;
        }

        public override void End()
        {
            if (!_commandBufferBegun)
            {
                throw new VeldridException("CommandBuffer must have been started before End() may be called.");
            }

            _commandBufferBegun = false;
            _commandBufferEnded = true;

            if (!_currentFramebufferEverActive && _currentFramebuffer != null)
            {
                BeginCurrentRenderPass();
            }

            if (_activeRenderPass.Handle != default)
            {
                EndCurrentRenderPass();
                _currentFramebuffer.TransitionToFinalLayout(_cb);
            }

            _vk.EndCommandBuffer(_cb);
            _submittedCommandBuffers.Add(_cb);
        }

        private void SetFrameBufferFromMainPass(VkCommandList mainBuffer)
        {
            _framebuffer = mainBuffer._framebuffer;

            _currentFramebuffer = mainBuffer._currentFramebuffer;
            _currentFramebufferEverActive = mainBuffer._currentFramebufferEverActive;
            _activeRenderPass = mainBuffer._activeRenderPass;
            _newFramebuffer = mainBuffer._newFramebuffer;

            Util.EnsureArrayMinimumSize(ref _scissorRects, Math.Max(1, (uint)_currentFramebuffer.ColorTargets.Count));
            uint clearValueCount = (uint)_currentFramebuffer.ColorTargets.Count;
            Util.EnsureArrayMinimumSize(ref _clearValues, clearValueCount + 1); // Leave an extra space for the depth value (tracked separately).
            Util.ClearArray(_validColorClearValues);
            Util.EnsureArrayMinimumSize(ref _validColorClearValues, clearValueCount);
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            if(this.isDeclaredAsSubpass)
            {
                throw new VeldridException("Cant set framebuffer of an sub pass.");
            }

            if (_activeRenderPass.Handle != default)
            {
                EndCurrentRenderPass();
            }
            else if (!_currentFramebufferEverActive && _currentFramebuffer != null)
            {
                // This forces any queued up texture clears to be emitted.
                BeginCurrentRenderPass();
                EndCurrentRenderPass();
            }

            if (_currentFramebuffer != null)
            {
                _currentFramebuffer.TransitionToFinalLayout(_cb);
            }

            VkFramebufferBase vkFB = Util.AssertSubtype<Framebuffer, VkFramebufferBase>(fb);
            _currentFramebuffer = vkFB;
            _currentFramebufferEverActive = false;
            _newFramebuffer = true;
            Util.EnsureArrayMinimumSize(ref _scissorRects, Math.Max(1, (uint)vkFB.ColorTargets.Count));
            uint clearValueCount = (uint)vkFB.ColorTargets.Count;
            Util.EnsureArrayMinimumSize(ref _clearValues, clearValueCount + 1); // Leave an extra space for the depth value (tracked separately).
            Util.ClearArray(_validColorClearValues);
            Util.EnsureArrayMinimumSize(ref _validColorClearValues, clearValueCount);
            _currentStagingInfo.Resources.Add(vkFB.RefCount);

            if (fb is VkSwapchainFramebuffer scFB)
            {
                _currentStagingInfo.Resources.Add(scFB.Swapchain.RefCount);
            }
        }

        private void EnsureRenderPassActive()
        {
            if(this.isDeclaredAsSubpass)
            {
                if(this.mainPass == null)
                {
                    throw new VeldridException("No main pass found.");
                }
                if ((this.mainPass as VkCommandList)._activeRenderPass.Handle == default)
                    throw new VeldridException("There is no renderpass active on main Pass");
            }
            else if (_activeRenderPass.Handle == default)
            {
                BeginCurrentRenderPass();
            }
        }

        private void EnsureNoRenderPass()
        {
            if (this.isDeclaredAsSubpass)
            {
                throw new VeldridException("There is active render pass of the main pass.");
            }
            else if (_activeRenderPass.Handle != default)
            {
                EndCurrentRenderPass();
            }
        }

        private void BeginCurrentRenderPass(Silk.NET.Vulkan.SubpassContents content = Silk.NET.Vulkan.SubpassContents.Inline)
        {
            Debug.Assert(_activeRenderPass.Handle == default);
            Debug.Assert(_currentFramebuffer != null);
            _currentFramebufferEverActive = true;

            uint attachmentCount = _currentFramebuffer.AttachmentCount;
            bool haveAnyAttachments = _currentFramebuffer.ColorTargets.Count > 0 || _currentFramebuffer.DepthTarget != null;
            bool haveAllClearValues = _depthClearValue.HasValue || _currentFramebuffer.DepthTarget == null;
            bool haveAnyClearValues = _depthClearValue.HasValue;
            for (int i = 0; i < _currentFramebuffer.ColorTargets.Count; i++)
            {
                if (!_validColorClearValues[i])
                {
                    haveAllClearValues = false;
                }
                else
                {
                    haveAnyClearValues = true;
                }
            }

            var renderPassBI = new Silk.NET.Vulkan.RenderPassBeginInfo();
            renderPassBI.SType = Silk.NET.Vulkan.StructureType.RenderPassBeginInfo;

            renderPassBI.RenderArea = new Silk.NET.Vulkan.Rect2D(new Silk.NET.Vulkan.Offset2D(0,0),
                new Silk.NET.Vulkan.Extent2D(_currentFramebuffer.RenderableWidth, _currentFramebuffer.RenderableHeight));

            renderPassBI.Framebuffer = _currentFramebuffer.CurrentFramebuffer;

            if (!haveAnyAttachments || !haveAllClearValues)
            {
                renderPassBI.RenderPass = _newFramebuffer
                    ? _currentFramebuffer.RenderPassNoClear_Init
                    : _currentFramebuffer.RenderPassNoClear_Load;
                _vk.CmdBeginRenderPass(_cb, &renderPassBI,  content);
                _activeRenderPass = renderPassBI.RenderPass;

                if (haveAnyClearValues)
                {
                    if (_depthClearValue.HasValue)
                    {
                        ClearDepthStencilCore(_depthClearValue.Value.DepthStencil.Depth, (byte)_depthClearValue.Value.DepthStencil.Stencil);
                        _depthClearValue = null;
                    }

                    for (uint i = 0; i < _currentFramebuffer.ColorTargets.Count; i++)
                    {
                        if (_validColorClearValues[i])
                        {
                            _validColorClearValues[i] = false;
                            var vkClearValue = _clearValues[i];
                            RgbaFloat clearColor = new RgbaFloat(
                                vkClearValue.Color.Float32_0,
                                vkClearValue.Color.Float32_1,
                                vkClearValue.Color.Float32_2,
                                vkClearValue.Color.Float32_3);
                            ClearColorTarget(i, clearColor);
                        }
                    }
                }
            }
            else
            {
                // We have clear values for every attachment.
                renderPassBI.RenderPass = _currentFramebuffer.RenderPassClear;
                fixed (Silk.NET.Vulkan.ClearValue* clearValuesPtr = &_clearValues[0])
                {
                    renderPassBI.ClearValueCount = attachmentCount;
                    renderPassBI.PClearValues = clearValuesPtr;
                    if (_depthClearValue.HasValue)
                    {
                        _clearValues[_currentFramebuffer.ColorTargets.Count] = _depthClearValue.Value;
                        _depthClearValue = null;
                    }
                    _vk.CmdBeginRenderPass(_cb, &renderPassBI, content);
                    _activeRenderPass = _currentFramebuffer.RenderPassClear;
                    Util.ClearArray(_validColorClearValues);
                }
            }

            _newFramebuffer = false;
        }

        private void EndCurrentRenderPass()
        {
            Debug.Assert(_activeRenderPass.Handle != default);
            _vk.CmdEndRenderPass(_cb);
            _currentFramebuffer.TransitionToIntermediateLayout(_cb);
            _activeRenderPass.Handle = default;

            // Place a barrier between RenderPasses, so that color / depth outputs
            // can be read in subsequent passes.
            _vk.CmdPipelineBarrier(
                _cb,
                Silk.NET.Vulkan.PipelineStageFlags.PipelineStageBottomOfPipeBit,
                Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTopOfPipeBit,
               0,
                0,
                null,
                0,
                null,
                0,
                null);
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(buffer);
            var deviceBuffer = vkBuffer.DeviceBuffer;
            ulong offset64 = offset;
            _vk.CmdBindVertexBuffers(_cb, index, 1, &deviceBuffer, &offset64);
            _currentStagingInfo.Resources.Add(vkBuffer.RefCount);
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(buffer);
            _vk.CmdBindIndexBuffer(_cb, vkBuffer.DeviceBuffer, offset, VkFormats.VdToVkIndexFormat(format));
            _currentStagingInfo.Resources.Add(vkBuffer.RefCount);
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            VkPipeline vkPipeline = Util.AssertSubtype<Pipeline, VkPipeline>(pipeline);
            if (!pipeline.IsComputePipeline && _currentGraphicsPipeline != pipeline)
            {
                Util.EnsureArrayMinimumSize(ref _currentGraphicsResourceSets, vkPipeline.ResourceSetCount);
                ClearSets(_currentGraphicsResourceSets);
                Util.EnsureArrayMinimumSize(ref _graphicsResourceSetsChanged, vkPipeline.ResourceSetCount);
                _vk.CmdBindPipeline(_cb, Silk.NET.Vulkan.PipelineBindPoint.Graphics, vkPipeline.DevicePipeline);
                _currentGraphicsPipeline = vkPipeline;
            }
            else if (pipeline.IsComputePipeline && _currentComputePipeline != pipeline)
            {
                Util.EnsureArrayMinimumSize(ref _currentComputeResourceSets, vkPipeline.ResourceSetCount);
                ClearSets(_currentComputeResourceSets);
                Util.EnsureArrayMinimumSize(ref _computeResourceSetsChanged, vkPipeline.ResourceSetCount);
                _vk.CmdBindPipeline(_cb, Silk.NET.Vulkan.PipelineBindPoint.Compute, vkPipeline.DevicePipeline);
                _currentComputePipeline = vkPipeline;
            }

            _currentStagingInfo.Resources.Add(vkPipeline.RefCount);
        }

        private void ClearSets(BoundResourceSetInfo[] boundSets)
        {
            foreach (BoundResourceSetInfo boundSetInfo in boundSets)
            {
                boundSetInfo.Offsets.Dispose();
            }
            Util.ClearArray(boundSets);
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (!_currentGraphicsResourceSets[slot].Equals(rs, dynamicOffsetsCount, ref dynamicOffsets))
            {
                _currentGraphicsResourceSets[slot].Offsets.Dispose();
                _currentGraphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
                _graphicsResourceSetsChanged[slot] = true;
                VkResourceSet vkRS = Util.AssertSubtype<ResourceSet, VkResourceSet>(rs);
            }
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (!_currentComputeResourceSets[slot].Equals(rs, dynamicOffsetsCount, ref dynamicOffsets))
            {
                _currentComputeResourceSets[slot].Offsets.Dispose();
                _currentComputeResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
                _computeResourceSetsChanged[slot] = true;
                VkResourceSet vkRS = Util.AssertSubtype<ResourceSet, VkResourceSet>(rs);
            }
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            if (index == 0 || _gd.Features.MultipleViewports)
            {
                var scissor = new Silk.NET.Vulkan.Rect2D(new Silk.NET.Vulkan.Offset2D((int)x, (int)y), new Silk.NET.Vulkan.Extent2D((uint)width, (uint)height));
                if (_scissorRects[index].Offset.X != scissor.Offset.X || _scissorRects[index].Offset.Y != scissor.Offset.Y ||
                    _scissorRects[index].Extent.Width != scissor.Extent.Width ||
                    _scissorRects[index].Extent.Height != scissor.Extent.Height)
                {
                    _scissorRects[index] = scissor;
                    _vk.CmdSetScissor(_cb, index, 1, &scissor);
                }
            }
        }

        public override void SetViewport(uint index, ref Viewport viewport)
        {
            if (index == 0 || _gd.Features.MultipleViewports)
            {
                float vpY = _gd.IsClipSpaceYInverted
                    ? viewport.Y
                    : viewport.Height + viewport.Y;
                float vpHeight = _gd.IsClipSpaceYInverted
                    ? viewport.Height
                    : -viewport.Height;

                var vkViewport = new Silk.NET.Vulkan.Viewport
                {
                    X = viewport.X,
                    Y = vpY,
                    Width = viewport.Width,
                    Height = vpHeight,
                    MinDepth = viewport.MinDepth,
                    MaxDepth = viewport.MaxDepth
                };

                _vk.CmdSetViewport(_cb, index, 1, &vkViewport);
            }
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            VkBuffer stagingBuffer = GetStagingBuffer(sizeInBytes);
            _gd.UpdateBuffer(stagingBuffer, 0, source, sizeInBytes);
            CopyBuffer(stagingBuffer, 0, buffer, bufferOffsetInBytes, sizeInBytes);
        }

        protected override void CopyBufferCore(
            DeviceBuffer source,
            uint sourceOffset,
            DeviceBuffer destination,
            uint destinationOffset,
            uint sizeInBytes)
        {
            EnsureNoRenderPass();

            VkBuffer srcVkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(source);
            _currentStagingInfo.Resources.Add(srcVkBuffer.RefCount);
            VkBuffer dstVkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(destination);
            _currentStagingInfo.Resources.Add(dstVkBuffer.RefCount);

            var region = new Silk.NET.Vulkan.BufferCopy
            {
                SrcOffset = sourceOffset,
                DstOffset = destinationOffset,
                Size = sizeInBytes
            };


            _vk.CmdCopyBuffer(_cb, srcVkBuffer.DeviceBuffer, dstVkBuffer.DeviceBuffer, 1, &region);

            Silk.NET.Vulkan.MemoryBarrier barrier = new Silk.NET.Vulkan.MemoryBarrier();
            barrier.SType = Silk.NET.Vulkan.StructureType.MemoryBarrier;
            barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit;
            barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessVertexAttributeReadBit;
            barrier.PNext = null;
            _vk.CmdPipelineBarrier(
                _cb,
                Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit, Silk.NET.Vulkan.PipelineStageFlags.PipelineStageVertexInputBit,
                0,
                1, &barrier,
                0, null,
                0, null);
        }

        protected override void CopyTextureCore(
            Texture source,
            uint srcX, uint srcY, uint srcZ,
            uint srcMipLevel,
            uint srcBaseArrayLayer,
            Texture destination,
            uint dstX, uint dstY, uint dstZ,
            uint dstMipLevel,
            uint dstBaseArrayLayer,
            uint width, uint height, uint depth,
            uint layerCount)
        {
            EnsureNoRenderPass();
            CopyTextureCore_VkCommandBuffer(
                _vk,
                _cb,
                source, srcX, srcY, srcZ, srcMipLevel, srcBaseArrayLayer,
                destination, dstX, dstY, dstZ, dstMipLevel, dstBaseArrayLayer,
                width, height, depth, layerCount);

            VkTexture srcVkTexture = Util.AssertSubtype<Texture, VkTexture>(source);
            _currentStagingInfo.Resources.Add(srcVkTexture.RefCount);
            VkTexture dstVkTexture = Util.AssertSubtype<Texture, VkTexture>(destination);
            _currentStagingInfo.Resources.Add(dstVkTexture.RefCount);
        }

        internal static void CopyTextureCore_VkCommandBuffer(

            Silk.NET.Vulkan.Vk _vk,
            Silk.NET.Vulkan.CommandBuffer cb,
            Texture source,
            uint srcX, uint srcY, uint srcZ,
            uint srcMipLevel,
            uint srcBaseArrayLayer,
            Texture destination,
            uint dstX, uint dstY, uint dstZ,
            uint dstMipLevel,
            uint dstBaseArrayLayer,
            uint width, uint height, uint depth,
            uint layerCount)
        {
            VkTexture srcVkTexture = Util.AssertSubtype<Texture, VkTexture>(source);
            VkTexture dstVkTexture = Util.AssertSubtype<Texture, VkTexture>(destination);

            bool sourceIsStaging = (source.Usage & TextureUsage.Staging) == TextureUsage.Staging;
            bool destIsStaging = (destination.Usage & TextureUsage.Staging) == TextureUsage.Staging;

            if (!sourceIsStaging && !destIsStaging)
            {
                var srcSubresource =  new Silk.NET.Vulkan.ImageSubresourceLayers
                {
                    AspectMask = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit,
                    LayerCount = layerCount,
                    MipLevel = srcMipLevel,
                    BaseArrayLayer = srcBaseArrayLayer
                };

                var dstSubresource = new Silk.NET.Vulkan.ImageSubresourceLayers
                {
                    AspectMask = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit,
                    LayerCount = layerCount,
                    MipLevel = dstMipLevel,
                    BaseArrayLayer = dstBaseArrayLayer
                };

                var region = new Silk.NET.Vulkan.ImageCopy
                {
                    SrcOffset = new Silk.NET.Vulkan.Offset3D { X = (int)srcX, Y = (int)srcY, Z = (int)srcZ },
                    DstOffset = new Silk.NET.Vulkan.Offset3D { X = (int)dstX, Y = (int)dstY, Z = (int)dstZ },
                    SrcSubresource = srcSubresource,
                    DstSubresource = dstSubresource,
                    Extent = new Silk.NET.Vulkan.Extent3D {Width = width, Height = height, Depth = depth }
                };

                srcVkTexture.TransitionImageLayout(
                    cb,
                    srcMipLevel,
                    1,
                    srcBaseArrayLayer,
                    layerCount,
                    Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal);

                dstVkTexture.TransitionImageLayout(
                    cb,
                    dstMipLevel,
                    1,
                    dstBaseArrayLayer,
                    layerCount,
                    Silk.NET.Vulkan.ImageLayout.TransferDstOptimal);

                _vk.CmdCopyImage(
                    cb,
                    srcVkTexture.OptimalDeviceImage,
                    Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal,
                    dstVkTexture.OptimalDeviceImage,
                    Silk.NET.Vulkan.ImageLayout.TransferDstOptimal,
                    1,
                    &region);

                if ((srcVkTexture.Usage & TextureUsage.Sampled) != 0)
                {
                    srcVkTexture.TransitionImageLayout(
                        cb,
                        srcMipLevel,
                        1,
                        srcBaseArrayLayer,
                        layerCount,
                        Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal);
                }

                if ((dstVkTexture.Usage & TextureUsage.Sampled) != 0)
                {
                    dstVkTexture.TransitionImageLayout(
                        cb,
                        dstMipLevel,
                        1,
                        dstBaseArrayLayer,
                        layerCount,
                        Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal);
                }
            }
            else if (sourceIsStaging && !destIsStaging)
            {
                var srcBuffer = srcVkTexture.StagingBuffer;
                Silk.NET.Vulkan.SubresourceLayout srcLayout = srcVkTexture.GetSubresourceLayout(
                    srcVkTexture.CalculateSubresource(srcMipLevel, srcBaseArrayLayer));
                Silk.NET.Vulkan.Image dstImage = dstVkTexture.OptimalDeviceImage;
                dstVkTexture.TransitionImageLayout(
                    cb,
                    dstMipLevel,
                    1,
                    dstBaseArrayLayer,
                    layerCount,
                    Silk.NET.Vulkan.ImageLayout.TransferDstOptimal);

                var dstSubresource = new Silk.NET.Vulkan.ImageSubresourceLayers
                {
                    AspectMask = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit,
                    LayerCount = layerCount,
                    MipLevel = dstMipLevel,
                    BaseArrayLayer = dstBaseArrayLayer
                };

                Util.GetMipDimensions(srcVkTexture, srcMipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);
                uint blockSize = FormatHelpers.IsCompressedFormat(srcVkTexture.Format) ? 4u : 1u;
                uint bufferRowLength = Math.Max(mipWidth, blockSize);
                uint bufferImageHeight = Math.Max(mipHeight, blockSize);
                uint compressedX = srcX / blockSize;
                uint compressedY = srcY / blockSize;
                uint blockSizeInBytes = blockSize == 1
                    ? FormatHelpers.GetSizeInBytes(srcVkTexture.Format)
                    : FormatHelpers.GetBlockSizeInBytes(srcVkTexture.Format);
                uint rowPitch = FormatHelpers.GetRowPitch(bufferRowLength, srcVkTexture.Format);
                uint depthPitch = FormatHelpers.GetDepthPitch(rowPitch, bufferImageHeight, srcVkTexture.Format);

                uint copyWidth = Math.Min(width, mipWidth);
                uint copyheight = Math.Min(height, mipHeight);

                var regions = new Silk.NET.Vulkan.BufferImageCopy
                {
                    BufferOffset = srcLayout.Offset
                        + (srcZ * depthPitch)
                        + (compressedY * rowPitch)
                        + (compressedX * blockSizeInBytes),
                    BufferRowLength = bufferRowLength,
                    BufferImageHeight = bufferImageHeight,
                    ImageExtent = new Silk.NET.Vulkan.Extent3D { Width = copyWidth, Height = copyheight, Depth = depth },
                    ImageOffset = new Silk.NET.Vulkan.Offset3D { X = (int)dstX, Y = (int)dstY, Z = (int)dstZ },
                    ImageSubresource = dstSubresource
                };

                _vk.CmdCopyBufferToImage(cb, srcBuffer, dstImage, Silk.NET.Vulkan.ImageLayout.TransferDstOptimal, 1, &regions);

                if ((dstVkTexture.Usage & TextureUsage.Sampled) != 0)
                {
                    dstVkTexture.TransitionImageLayout(
                        cb,
                        dstMipLevel,
                        1,
                        dstBaseArrayLayer,
                        layerCount,
                        Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal);
                }
            }
            else if (!sourceIsStaging && destIsStaging)
            {
                Silk.NET.Vulkan.Image srcImage = srcVkTexture.OptimalDeviceImage;
                srcVkTexture.TransitionImageLayout(
                    cb,
                    srcMipLevel,
                    1,
                    srcBaseArrayLayer,
                    layerCount,
                    Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal);

                var dstBuffer = dstVkTexture.StagingBuffer;
                Silk.NET.Vulkan.SubresourceLayout dstLayout = dstVkTexture.GetSubresourceLayout(
                    dstVkTexture.CalculateSubresource(dstMipLevel, dstBaseArrayLayer));

                Silk.NET.Vulkan.ImageAspectFlags aspect = (srcVkTexture.Usage & TextureUsage.DepthStencil) != 0
                    ? Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit
                    : Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit;

                var srcSubresource = new Silk.NET.Vulkan.ImageSubresourceLayers
                {
                    AspectMask = aspect,
                    LayerCount = layerCount,
                    MipLevel = srcMipLevel,
                    BaseArrayLayer = srcBaseArrayLayer
                };

                Util.GetMipDimensions(dstVkTexture, dstMipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);
                uint blockSize = FormatHelpers.IsCompressedFormat(srcVkTexture.Format) ? 4u : 1u;
                uint bufferRowLength = Math.Max(mipWidth, blockSize);
                uint bufferImageHeight = Math.Max(mipHeight, blockSize);
                uint compressedDstX = dstX / blockSize;
                uint compressedDstY = dstY / blockSize;
                uint blockSizeInBytes = blockSize == 1
                    ? FormatHelpers.GetSizeInBytes(dstVkTexture.Format)
                    : FormatHelpers.GetBlockSizeInBytes(dstVkTexture.Format);
                uint rowPitch = FormatHelpers.GetRowPitch(bufferRowLength, dstVkTexture.Format);
                uint depthPitch = FormatHelpers.GetDepthPitch(rowPitch, bufferImageHeight, dstVkTexture.Format);

                var region = new Silk.NET.Vulkan.BufferImageCopy
                {
                    BufferRowLength = mipWidth,
                    BufferImageHeight = mipHeight,
                    BufferOffset = dstLayout.Offset
                        + (dstZ * depthPitch)
                        + (compressedDstY * rowPitch)
                        + (compressedDstX * blockSizeInBytes),
                    ImageExtent = new Silk.NET.Vulkan.Extent3D { Width = width, Height = height, Depth = depth },
                    ImageOffset = new Silk.NET.Vulkan.Offset3D { X = (int)srcX, Y = (int)srcY, Z = (int)srcZ },
                    ImageSubresource = srcSubresource
                };

                _vk.CmdCopyImageToBuffer(cb, srcImage, Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal, dstBuffer, 1, &region);

                if ((srcVkTexture.Usage & TextureUsage.Sampled) != 0)
                {
                    srcVkTexture.TransitionImageLayout(
                        cb,
                        srcMipLevel,
                        1,
                        srcBaseArrayLayer,
                        layerCount,
                        Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal);
                }
            }
            else
            {
                Debug.Assert(sourceIsStaging && destIsStaging);
                var srcBuffer = srcVkTexture.StagingBuffer;
                Silk.NET.Vulkan.SubresourceLayout srcLayout = srcVkTexture.GetSubresourceLayout(
                    srcVkTexture.CalculateSubresource(srcMipLevel, srcBaseArrayLayer));
                var dstBuffer = dstVkTexture.StagingBuffer;
                Silk.NET.Vulkan.SubresourceLayout dstLayout = dstVkTexture.GetSubresourceLayout(
                    dstVkTexture.CalculateSubresource(dstMipLevel, dstBaseArrayLayer));

                uint zLimit = Math.Max(depth, layerCount);
                if (!FormatHelpers.IsCompressedFormat(source.Format))
                {
                    uint pixelSize = FormatHelpers.GetSizeInBytes(srcVkTexture.Format);
                    for (uint zz = 0; zz < zLimit; zz++)
                    {
                        for (uint yy = 0; yy < height; yy++)
                        {
                            var region = new Silk.NET.Vulkan.BufferCopy
                            {
                                SrcOffset = srcLayout.Offset
                                    + srcLayout.DepthPitch * (zz + srcZ)
                                    + srcLayout.RowPitch * (yy + srcY)
                                    + pixelSize * srcX,
                                DstOffset = dstLayout.Offset
                                    + dstLayout.DepthPitch * (zz + dstZ)
                                    + dstLayout.RowPitch * (yy + dstY)
                                    + pixelSize * dstX,
                                Size = width * pixelSize,
                            };

                            _vk.CmdCopyBuffer(cb, srcBuffer, dstBuffer, 1, &region);
                        }
                    }
                }
                else // IsCompressedFormat
                {
                    uint denseRowSize = FormatHelpers.GetRowPitch(width, source.Format);
                    uint numRows = FormatHelpers.GetNumRows(height, source.Format);
                    uint compressedSrcX = srcX / 4;
                    uint compressedSrcY = srcY / 4;
                    uint compressedDstX = dstX / 4;
                    uint compressedDstY = dstY / 4;
                    uint blockSizeInBytes = FormatHelpers.GetBlockSizeInBytes(source.Format);

                    for (uint zz = 0; zz < zLimit; zz++)
                    {
                        for (uint row = 0; row < numRows; row++)
                        {
                            var region = new Silk.NET.Vulkan.BufferCopy
                            {
                                SrcOffset = srcLayout.Offset
                                    + srcLayout.DepthPitch * (zz + srcZ)
                                    + srcLayout.RowPitch * (row + compressedSrcY)
                                    + blockSizeInBytes * compressedSrcX,
                                DstOffset = dstLayout.Offset
                                    + dstLayout.DepthPitch * (zz + dstZ)
                                    + dstLayout.RowPitch * (row + compressedDstY)
                                    + blockSizeInBytes * compressedDstX,
                                Size = denseRowSize,
                            };

                            _vk.CmdCopyBuffer(cb, srcBuffer, dstBuffer, 1, &region);
                        }
                    }

                }
            }
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
            EnsureNoRenderPass();
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(texture);
            _currentStagingInfo.Resources.Add(vkTex.RefCount);

            uint layerCount = vkTex.ArrayLayers;
            if ((vkTex.Usage & TextureUsage.Cubemap) != 0)
            {
                layerCount *= 6;
            }

            var region = new Silk.NET.Vulkan.ImageBlit();

            uint width = vkTex.Width;
            uint height = vkTex.Height;
            uint depth = vkTex.Depth;
            for (uint level = 1; level < vkTex.MipLevels; level++)
            {
                vkTex.TransitionImageLayoutNonmatching(_cb, level - 1, 1, 0, layerCount, Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal);
                vkTex.TransitionImageLayoutNonmatching(_cb, level, 1, 0, layerCount, Silk.NET.Vulkan.ImageLayout.TransferDstOptimal);

                Silk.NET.Vulkan.Image deviceImage = vkTex.OptimalDeviceImage;
                uint mipWidth = Math.Max(width >> 1, 1);
                uint mipHeight = Math.Max(height >> 1, 1);
                uint mipDepth = Math.Max(depth >> 1, 1);

                region.SrcSubresource = new Silk.NET.Vulkan.ImageSubresourceLayers
                {
                    AspectMask = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit,
                    BaseArrayLayer = 0,
                    LayerCount = layerCount,
                    MipLevel = level - 1
                };
              //  region.SrcOffsets = new Silk.NET.Vulkan.ImageBlit.SrcOffsetsBuffer();
              //  region.DstOffsets = new Silk.NET.Vulkan.ImageBlit.DstOffsetsBuffer();

                region.SrcOffsets.Element0 = new Silk.NET.Vulkan.Offset3D();
                region.SrcOffsets.Element1 = new Silk.NET.Vulkan.Offset3D { X = (int)width, Y = (int)height, Z = (int)depth };
                region.DstOffsets.Element0 = new Silk.NET.Vulkan.Offset3D();

                region.DstSubresource = new Silk.NET.Vulkan.ImageSubresourceLayers
                {
                    AspectMask = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit,
                    BaseArrayLayer = 0,
                    LayerCount = layerCount,
                    MipLevel = level
                };

                region.DstOffsets.Element1 = new Silk.NET.Vulkan.Offset3D { X= (int)mipWidth, Y = (int)mipHeight, Z = (int)mipDepth };
                _vk.CmdBlitImage(
                    _cb,
                    deviceImage, Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal,
                    deviceImage, Silk.NET.Vulkan.ImageLayout.TransferDstOptimal,
                    1, &region,
                    _gd.GetFormatFilter(vkTex.VkFormat));

                width = mipWidth;
                height = mipHeight;
                depth = mipDepth;
            }

            if ((vkTex.Usage & TextureUsage.Sampled) != 0)
            {
                vkTex.TransitionImageLayoutNonmatching(_cb, 0, vkTex.MipLevels, 0, layerCount, Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal);
            }
        }

        [Conditional("DEBUG")]
        private void DebugFullPipelineBarrier()
        {
            var memoryBarrier = new Silk.NET.Vulkan.MemoryBarrier();
            memoryBarrier.SType = Silk.NET.Vulkan.StructureType.MemoryBarrier;

            memoryBarrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessIndirectCommandReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessIndexReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessVertexAttributeReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessUniformReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessInputAttachmentReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessShaderWriteBit |
                   Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentWriteBit |
                   Silk.NET.Vulkan.AccessFlags.AccessDepthStencilAttachmentReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessDepthStencilAttachmentWriteBit |
                   Silk.NET.Vulkan.AccessFlags.AccessTransferReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit |
                   Silk.NET.Vulkan.AccessFlags.AccessHostReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessHostWriteBit;

            memoryBarrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessIndirectCommandReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessIndexReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessVertexAttributeReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessUniformReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessInputAttachmentReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessShaderWriteBit |
                   Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentWriteBit |
                   Silk.NET.Vulkan.AccessFlags.AccessDepthStencilAttachmentReadBit |
                  Silk.NET.Vulkan.AccessFlags.AccessDepthStencilAttachmentWriteBit |
                  Silk.NET.Vulkan.AccessFlags.AccessTransferReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit |
                   Silk.NET.Vulkan.AccessFlags.AccessHostReadBit |
                   Silk.NET.Vulkan.AccessFlags.AccessHostWriteBit;

            _vk.CmdPipelineBarrier(
                _cb,
                Silk.NET.Vulkan.PipelineStageFlags.PipelineStageAllCommandsBit, // srcStageMask
                 Silk.NET.Vulkan.PipelineStageFlags.PipelineStageAllCommandsBit, // dstStageMask
                0,
                1,                                  // memoryBarrierCount
                &memoryBarrier,                     // pMemoryBarriers
                0, null,
                0, null);
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

        private VkBuffer GetStagingBuffer(uint size)
        {
            lock (_stagingLock)
            {
                VkBuffer ret = null;
                foreach (VkBuffer buffer in _availableStagingBuffers)
                {
                    if (buffer.SizeInBytes >= size)
                    {
                        ret = buffer;
                        _availableStagingBuffers.Remove(buffer);
                        break;
                    }
                }
                if (ret == null)
                {
                    ret = (VkBuffer)_gd.ResourceFactory.CreateBuffer(new BufferDescription(size, BufferUsage.Staging));
                    ret.Name = $"Staging Buffer (CommandList {_name})";
                }

                _currentStagingInfo.BuffersUsed.Add(ret);
                return ret;
            }
        }

        private protected override void PushDebugGroupCore(string name)
        {
            vkCmdDebugMarkerBeginEXT_t  func = _gd.MarkerBegin;
            if (func == null) { return; }

           Silk.NET.Vulkan.DebugMarkerMarkerInfoEXT markerInfo = new Silk.NET.Vulkan.DebugMarkerMarkerInfoEXT();
            markerInfo.SType = Silk.NET.Vulkan.StructureType.DebugMarkerMarkerInfoExt;

            int byteCount = Encoding.UTF8.GetByteCount(name);
            byte* utf8Ptr = stackalloc byte[byteCount + 1];
            fixed (char* namePtr = name)
            {
                Encoding.UTF8.GetBytes(namePtr, name.Length, utf8Ptr, byteCount);
            }
            utf8Ptr[byteCount] = 0;

            markerInfo.PMarkerName = utf8Ptr;

            func(_cb, &markerInfo);
        }

        private protected override void PopDebugGroupCore()
        {
            vkCmdDebugMarkerEndEXT_t func = _gd.MarkerEnd;
            if (func == null) { return; }

            func(_cb);
        }

        private protected override void InsertDebugMarkerCore(string name)
        {
            vkCmdDebugMarkerInsertEXT_t func = _gd.MarkerInsert;
            if (func == null) { return; }

             Silk.NET.Vulkan.DebugMarkerMarkerInfoEXT markerInfo = new Silk.NET.Vulkan.DebugMarkerMarkerInfoEXT();
            markerInfo.SType = Silk.NET.Vulkan.StructureType.DebugMarkerMarkerInfoExt;

            int byteCount = Encoding.UTF8.GetByteCount(name);
            byte* utf8Ptr = stackalloc byte[byteCount + 1];
            fixed (char* namePtr = name)
            {
                Encoding.UTF8.GetBytes(namePtr, name.Length, utf8Ptr, byteCount);
            }
            utf8Ptr[byteCount] = 0;

            markerInfo.PMarkerName = utf8Ptr;

            func(_cb, &markerInfo);
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
                _vk.DestroyCommandPool(_gd.Device, _pool, null);

                Debug.Assert(_submittedStagingInfos.Count == 0);

                foreach (VkBuffer buffer in _availableStagingBuffers)
                {
                    buffer.Dispose();
                }
            }
        }

        private class StagingResourceInfo
        {
            public List<VkBuffer> BuffersUsed { get; } = new List<VkBuffer>();
            public HashSet<ResourceRefCount> Resources { get; } = new HashSet<ResourceRefCount>();
            public void Clear()
            {
                BuffersUsed.Clear();
                Resources.Clear();
            }
        }

        private StagingResourceInfo GetStagingResourceInfo()
        {
            lock (_stagingLock)
            {
                StagingResourceInfo ret;
                int availableCount = _availableStagingInfos.Count;
                if (availableCount > 0)
                {
                    ret = _availableStagingInfos[availableCount - 1];
                    _availableStagingInfos.RemoveAt(availableCount - 1);
                }
                else
                {
                    ret = new StagingResourceInfo();
                }

                return ret;
            }
        }

        private void RecycleStagingInfo(StagingResourceInfo info)
        {
            lock (_stagingLock)
            {
                foreach (VkBuffer buffer in info.BuffersUsed)
                {
                    _availableStagingBuffers.Add(buffer);
                }

                foreach (ResourceRefCount rrc in info.Resources)
                {
                    rrc.Decrement();
                }

                info.Clear();

                _availableStagingInfos.Add(info);
            }
        }

        private protected override void PushConstantCore(IntPtr source, uint sizeInBytes)
        {
            if(_currentGraphicsPipeline == null)
            {
                throw new Exception("There is no pipeline given.");
            }

            _vk.CmdPushConstants(_cb, _currentGraphicsPipeline.PipelineLayout, Silk.NET.Vulkan.ShaderStageFlags.ShaderStageVertexBit, 0, sizeInBytes, source.ToPointer());
        }

  
    }
}
