using static Veldrid.Vk.VulkanUtil;
using System.Diagnostics;
using System;

namespace Veldrid.Vk
{
    internal unsafe class VkTexture : Texture
    {
        private readonly VkGraphicsDevice _gd;
        private readonly Silk.NET.Vulkan.Vk _vk;
        private readonly Silk.NET.Vulkan.Image _optimalImage;
        private readonly VkMemoryBlock _memoryBlock;
        private readonly Silk.NET.Vulkan.Buffer _stagingBuffer;
        private PixelFormat _format; // Static for regular images -- may change for shared staging images
        private readonly uint _actualImageArrayLayers;
        private bool _destroyed;

        // Immutable except for shared staging Textures.
        private uint _width;
        private uint _height;
        private uint _depth;

        public override uint Width => _width;

        public override uint Height => _height;

        public override uint Depth => _depth;

        public override PixelFormat Format => _format;

        public override uint MipLevels { get; }

        public override uint ArrayLayers { get; }

        public override TextureUsage Usage { get; }

        public override TextureType Type { get; }

        public override TextureSampleCount SampleCount { get; }

        public override bool IsDisposed => _destroyed;

        public Silk.NET.Vulkan.Image OptimalDeviceImage => _optimalImage;
        public Silk.NET.Vulkan.Buffer StagingBuffer => _stagingBuffer;
        public VkMemoryBlock Memory => _memoryBlock;

        public Silk.NET.Vulkan.Format VkFormat { get; }
        public Silk.NET.Vulkan.SampleCountFlags VkSampleCount { get; }

        private Silk.NET.Vulkan.ImageLayout[] _imageLayouts;
        private bool _isSwapchainTexture;
        private string _name;

        public ResourceRefCount RefCount { get; }
        public bool IsSwapchainTexture => _isSwapchainTexture;

        internal VkTexture(VkGraphicsDevice gd, ref TextureDescription description)
        {
            _gd = gd;
            _vk = gd.vk;
            _width = description.Width;
            _height = description.Height;
            _depth = description.Depth;
            MipLevels = description.MipLevels;
            ArrayLayers = description.ArrayLayers;
            bool isCubemap = ((description.Usage) & TextureUsage.Cubemap) == TextureUsage.Cubemap;
            _actualImageArrayLayers = isCubemap
                ? 6 * ArrayLayers
                : ArrayLayers;
            _format = description.Format;
            Usage = description.Usage;
            Type = description.Type;
            SampleCount = description.SampleCount;
            VkSampleCount = VkFormats.VdToVkSampleCount(SampleCount);
            VkFormat = VkFormats.VdToVkPixelFormat(Format, (description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil);

            bool isStaging = (Usage & TextureUsage.Staging) == TextureUsage.Staging;

            if (!isStaging)
            {
                Silk.NET.Vulkan.ImageCreateInfo imageCI = new Silk.NET.Vulkan.ImageCreateInfo();
                imageCI.SType = Silk.NET.Vulkan.StructureType.ImageCreateInfo;

                imageCI.MipLevels = MipLevels;
                imageCI.ArrayLayers = _actualImageArrayLayers;
                imageCI.ImageType = VkFormats.VdToVkTextureType(Type);
                imageCI.Extent = new Silk.NET.Vulkan.Extent3D();
                imageCI.Extent.Width = Width;
                imageCI.Extent.Height = Height;
                imageCI.Extent.Depth = Depth;
                imageCI.InitialLayout = Silk.NET.Vulkan.ImageLayout.Preinitialized;
                imageCI.Usage = VkFormats.VdToVkTextureUsage(Usage);
                imageCI.Tiling = isStaging ? Silk.NET.Vulkan.ImageTiling.Linear : Silk.NET.Vulkan.ImageTiling.Optimal;
                imageCI.Format = VkFormat;
                imageCI.Flags = Silk.NET.Vulkan.ImageCreateFlags.ImageCreateMutableFormatBit;

                imageCI.Samples = VkSampleCount;
                if (isCubemap)
                {
                    imageCI.Flags |= Silk.NET.Vulkan.ImageCreateFlags.ImageCreateCubeCompatibleBit;
                }

                uint subresourceCount = MipLevels * _actualImageArrayLayers * Depth;
                var result = _vk.CreateImage(gd.Device, &imageCI, null, out _optimalImage);
                CheckResult(result);

                Silk.NET.Vulkan.MemoryRequirements memoryRequirements = new Silk.NET.Vulkan.MemoryRequirements();
                
                bool prefersDedicatedAllocation;
                if (_gd.GetImageMemoryRequirements2 != null)
                {
                    Silk.NET.Vulkan.ImageMemoryRequirementsInfo2KHR memReqsInfo2 = new Silk.NET.Vulkan.ImageMemoryRequirementsInfo2KHR();
                    memReqsInfo2.Image = _optimalImage;
                    memReqsInfo2.SType = Silk.NET.Vulkan.StructureType.ImageMemoryRequirementsInfo2Khr;

                    Silk.NET.Vulkan.MemoryRequirements2KHR memReqs2 = new Silk.NET.Vulkan.MemoryRequirements2KHR();
                    memReqs2.SType = Silk.NET.Vulkan.StructureType.MemoryRequirements2Khr;

                    Silk.NET.Vulkan.MemoryDedicatedRequirementsKHR dedicatedReqs = new Silk.NET.Vulkan.MemoryDedicatedRequirementsKHR();
                    dedicatedReqs.SType = Silk.NET.Vulkan.StructureType.MemoryDedicatedRequirementsKhr;

                    memReqs2.PNext = &dedicatedReqs;
                    _gd.GetImageMemoryRequirements2(_gd.Device, &memReqsInfo2, &memReqs2);
                    memoryRequirements = memReqs2.MemoryRequirements;
                    prefersDedicatedAllocation = dedicatedReqs.PrefersDedicatedAllocation || dedicatedReqs.RequiresDedicatedAllocation;
                }
                else
                {
                    _vk.GetImageMemoryRequirements(gd.Device, _optimalImage, out memoryRequirements);
                    prefersDedicatedAllocation = false;
                }

                VkMemoryBlock memoryToken = gd.MemoryManager.Allocate(
                    gd.PhysicalDeviceMemProperties,
                    memoryRequirements.MemoryTypeBits,
                    Silk.NET.Vulkan.MemoryPropertyFlags.MemoryPropertyDeviceLocalBit,
                    false,
                    memoryRequirements.Size,
                    memoryRequirements.Alignment,
                    prefersDedicatedAllocation,
                    _optimalImage,
                    default);
                _memoryBlock = memoryToken;
                result = _vk.BindImageMemory(gd.Device, _optimalImage, _memoryBlock.DeviceMemory, _memoryBlock.Offset);
                CheckResult(result);

                _imageLayouts = new Silk.NET.Vulkan.ImageLayout[subresourceCount];
                for (int i = 0; i < _imageLayouts.Length; i++)
                {
                    _imageLayouts[i] = Silk.NET.Vulkan.ImageLayout.Preinitialized;
                }
            }
            else // isStaging
            {
                uint depthPitch = FormatHelpers.GetDepthPitch(
                    FormatHelpers.GetRowPitch(Width, Format),
                    Height,
                    Format);
                uint stagingSize = depthPitch * Depth;
                for (uint level = 1; level < MipLevels; level++)
                {
                    Util.GetMipDimensions(this, level, out uint mipWidth, out uint mipHeight, out uint mipDepth);

                    depthPitch = FormatHelpers.GetDepthPitch(
                        FormatHelpers.GetRowPitch(mipWidth, Format),
                        mipHeight,
                        Format);

                    stagingSize += depthPitch * mipDepth;
                }
                stagingSize *= ArrayLayers;

                Silk.NET.Vulkan.BufferCreateInfo bufferCI = new Silk.NET.Vulkan.BufferCreateInfo();
                bufferCI.
                    Usage = Silk.NET.Vulkan.BufferUsageFlags.BufferUsageTransferSrcBit | Silk.NET.Vulkan.BufferUsageFlags.BufferUsageTransferDstBit;
                bufferCI.Size = stagingSize;
                bufferCI.SType = Silk.NET.Vulkan.StructureType.BufferCreateInfo;

                var result = _vk.CreateBuffer(_gd.Device, &bufferCI, null, out _stagingBuffer);
                CheckResult(result);

                Silk.NET.Vulkan.MemoryRequirements bufferMemReqs;
                bool prefersDedicatedAllocation;
                if (_gd.GetBufferMemoryRequirements2 != null)
                {
                    Silk.NET.Vulkan.BufferMemoryRequirementsInfo2KHR memReqInfo2 = new Silk.NET.Vulkan.BufferMemoryRequirementsInfo2KHR();
                    memReqInfo2.SType = Silk.NET.Vulkan.StructureType.BufferMemoryRequirementsInfo2Khr;

                    memReqInfo2.Buffer = _stagingBuffer;
                    Silk.NET.Vulkan.MemoryRequirements2KHR memReqs2 = new Silk.NET.Vulkan.MemoryRequirements2KHR();
                    memReqs2.SType = Silk.NET.Vulkan.StructureType.MemoryRequirements2Khr;

                    Silk.NET.Vulkan.MemoryDedicatedRequirementsKHR dedicatedReqs = new Silk.NET.Vulkan.MemoryDedicatedRequirementsKHR();
                    dedicatedReqs.SType = Silk.NET.Vulkan.StructureType.MemoryDedicatedRequirementsKhr;
                    memReqs2.PNext = &dedicatedReqs;
                    _gd.GetBufferMemoryRequirements2(_gd.Device, &memReqInfo2, &memReqs2);
                    bufferMemReqs = memReqs2.MemoryRequirements;
                    prefersDedicatedAllocation = dedicatedReqs.PrefersDedicatedAllocation || dedicatedReqs.RequiresDedicatedAllocation;
                }
                else
                {
                    _vk.GetBufferMemoryRequirements(gd.Device, _stagingBuffer, out bufferMemReqs);
                    prefersDedicatedAllocation = false;
                }

                // Use "host cached" memory when available, for better performance of GPU -> CPU transfers
                var propertyFlags = Silk.NET.Vulkan.MemoryPropertyFlags.MemoryPropertyHostVisibleBit | Silk.NET.Vulkan.MemoryPropertyFlags.MemoryPropertyHostCoherentBit
                    | Silk.NET.Vulkan.MemoryPropertyFlags.MemoryPropertyHostCachedBit;
                if (!TryFindMemoryType(_gd.PhysicalDeviceMemProperties, bufferMemReqs.MemoryTypeBits, propertyFlags, out _))
                {
                    propertyFlags ^= Silk.NET.Vulkan.MemoryPropertyFlags.MemoryPropertyHostCachedBit;
                }
                _memoryBlock = _gd.MemoryManager.Allocate(
                    _gd.PhysicalDeviceMemProperties,
                    bufferMemReqs.MemoryTypeBits,
                    propertyFlags,
                    true,
                    bufferMemReqs.Size,
                    bufferMemReqs.Alignment,
                    prefersDedicatedAllocation,
                    default,
                    _stagingBuffer);

                result = _vk.BindBufferMemory(_gd.Device, _stagingBuffer, _memoryBlock.DeviceMemory, _memoryBlock.Offset);
                CheckResult(result);
            }

            ClearIfRenderTarget();
            TransitionIfSampled();
            RefCount = new ResourceRefCount(RefCountedDispose);
        }

        // Used to construct Swapchain textures.
        internal VkTexture(
            VkGraphicsDevice gd,
            uint width,
            uint height,
            uint mipLevels,
            uint arrayLayers,
            Silk.NET.Vulkan.Format vkFormat,
            TextureUsage usage,
            TextureSampleCount sampleCount,
            Silk.NET.Vulkan.Image existingImage)
        {
            Debug.Assert(width > 0 && height > 0);
            _gd = gd;
            _vk = gd.vk;
            MipLevels = mipLevels;
            _width = width;
            _height = height;
            _depth = 1;
            VkFormat = vkFormat;
            _format = VkFormats.VkToVdPixelFormat(VkFormat);
            ArrayLayers = arrayLayers;
            Usage = usage;
            Type = TextureType.Texture2D;
            SampleCount = sampleCount;
            VkSampleCount = VkFormats.VdToVkSampleCount(sampleCount);
            _optimalImage = existingImage;
            _imageLayouts = new[] { Silk.NET.Vulkan.ImageLayout.Undefined };
            _isSwapchainTexture = true;

            ClearIfRenderTarget();
            RefCount = new ResourceRefCount(DisposeCore);
        }

        private void ClearIfRenderTarget()
        {
            // If the image is going to be used as a render target, we need to clear the data before its first use.
            if ((Usage & TextureUsage.RenderTarget) != 0)
            {
                _gd.ClearColorTexture(this, new Silk.NET.Vulkan.ClearColorValue(0, 0, 0, 0));
            }
            else if ((Usage & TextureUsage.DepthStencil) != 0)
            {
                _gd.ClearDepthTexture(this, new Silk.NET.Vulkan.ClearDepthStencilValue(0, 0));
            }
        }

        private void TransitionIfSampled()
        {
            if ((Usage & TextureUsage.Sampled) != 0)
            {
                _gd.TransitionImageLayout(this, Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal);
            }
        }

        internal Silk.NET.Vulkan.SubresourceLayout GetSubresourceLayout(uint subresource)
        {
            bool staging = _stagingBuffer.Handle != 0;
            Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out uint arrayLayer);
            if (!staging)
            {
                Silk.NET.Vulkan.ImageAspectFlags aspect = (Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil
                  ? (Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit | Silk.NET.Vulkan.ImageAspectFlags.ImageAspectStencilBit)
                  : Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit;
                Silk.NET.Vulkan.ImageSubresource imageSubresource = new Silk.NET.Vulkan.ImageSubresource
                {
                    ArrayLayer = arrayLayer,
                    MipLevel = mipLevel,
                    AspectMask = aspect,
                };

                _vk.GetImageSubresourceLayout(_gd.Device, _optimalImage, &imageSubresource, out Silk.NET.Vulkan.SubresourceLayout layout);
                return layout;
            }
            else
            {
                uint blockSize = FormatHelpers.IsCompressedFormat(Format) ? 4u : 1u;
                Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);
                uint rowPitch = FormatHelpers.GetRowPitch(mipWidth, Format);
                uint depthPitch = FormatHelpers.GetDepthPitch(rowPitch, mipHeight, Format);

                Silk.NET.Vulkan.SubresourceLayout layout = new Silk.NET.Vulkan.SubresourceLayout()
                {
                    RowPitch = rowPitch,
                    DepthPitch = depthPitch,
                    ArrayPitch = depthPitch,
                    Size = depthPitch,
                };
                layout.Offset = Util.ComputeSubresourceOffset(this, mipLevel, arrayLayer);

                return layout;
            }
        }

        internal void TransitionImageLayout(
            Silk.NET.Vulkan.CommandBuffer cb,
            uint baseMipLevel,
            uint levelCount,
            uint baseArrayLayer,
            uint layerCount,
            Silk.NET.Vulkan.ImageLayout newLayout)
        {
            if (_stagingBuffer.Handle != default)
            {
                return;
            }

            Silk.NET.Vulkan.ImageLayout oldLayout = _imageLayouts[CalculateSubresource(baseMipLevel, baseArrayLayer)];
#if DEBUG
            for (uint level = 0; level < levelCount; level++)
            {
                for (uint layer = 0; layer < layerCount; layer++)
                {
                    if (_imageLayouts[CalculateSubresource(baseMipLevel + level, baseArrayLayer + layer)] != oldLayout)
                    {
                        throw new VeldridException("Unexpected image layout.");
                    }
                }
            }
#endif
            if (oldLayout != newLayout)
            {
                Silk.NET.Vulkan.ImageAspectFlags aspectMask;
                if ((Usage & TextureUsage.DepthStencil) != 0)
                {
                    aspectMask = FormatHelpers.IsStencilFormat(Format)
                        ? Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit | Silk.NET.Vulkan.ImageAspectFlags.ImageAspectStencilBit
                        : Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit;
                }
                else
                {
                    aspectMask = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit;
                }
                VulkanUtil.TransitionImageLayout(
                    _vk,
                    cb,
                    OptimalDeviceImage,
                    baseMipLevel,
                    levelCount,
                    baseArrayLayer,
                    layerCount,
                    aspectMask,
                    _imageLayouts[CalculateSubresource(baseMipLevel, baseArrayLayer)],
                    newLayout);

                for (uint level = 0; level < levelCount; level++)
                {
                    for (uint layer = 0; layer < layerCount; layer++)
                    {
                        _imageLayouts[CalculateSubresource(baseMipLevel + level, baseArrayLayer + layer)] = newLayout;
                    }
                }
            }
        }

        internal void TransitionImageLayoutNonmatching(
            Silk.NET.Vulkan.CommandBuffer cb,
            uint baseMipLevel,
            uint levelCount,
            uint baseArrayLayer,
            uint layerCount,
            Silk.NET.Vulkan.ImageLayout newLayout)
        {
            if (_stagingBuffer.Handle != default)
            {
                return;
            }

            for (uint level = baseMipLevel; level < baseMipLevel + levelCount; level++)
            {
                for (uint layer = baseArrayLayer; layer < baseArrayLayer + layerCount; layer++)
                {
                    uint subresource = CalculateSubresource(level, layer);
                    Silk.NET.Vulkan.ImageLayout oldLayout = _imageLayouts[subresource];

                    if (oldLayout != newLayout)
                    {
                        Silk.NET.Vulkan.ImageAspectFlags aspectMask;
                        if ((Usage & TextureUsage.DepthStencil) != 0)
                        {
                            aspectMask = FormatHelpers.IsStencilFormat(Format)
                                ? Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit | Silk.NET.Vulkan.ImageAspectFlags.ImageAspectStencilBit
                                : Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit;
                        }
                        else
                        {
                            aspectMask = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit;
                        }
                        VulkanUtil.TransitionImageLayout(
                            _vk,
                            cb,
                            OptimalDeviceImage,
                            level,
                            1,
                            layer,
                            1,
                            aspectMask,
                            oldLayout,
                            newLayout);

                        _imageLayouts[subresource] = newLayout;
                    }
                }
            }
        }

        internal Silk.NET.Vulkan.ImageLayout GetImageLayout(uint mipLevel, uint arrayLayer)
        {
            return _imageLayouts[CalculateSubresource(mipLevel, arrayLayer)];
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

        internal void SetStagingDimensions(uint width, uint height, uint depth, PixelFormat format)
        {
            Debug.Assert(_stagingBuffer.Handle != default);
            Debug.Assert(Usage == TextureUsage.Staging);
            _width = width;
            _height = height;
            _depth = depth;
            _format = format;
        }

        private protected override void DisposeCore()
        {
            RefCount.Decrement();
        }

        private void RefCountedDispose()
        {
            if (!_destroyed)
            {
                base.Dispose();

                _destroyed = true;

                bool isStaging = (Usage & TextureUsage.Staging) == TextureUsage.Staging;
                if (isStaging)
                {
                   _vk.DestroyBuffer(_gd.Device, _stagingBuffer, null);
                }
                else
                {
                    _vk.DestroyImage(_gd.Device, _optimalImage, null);
                }

                if (_memoryBlock.DeviceMemory.Handle != 0)
                {
                    _gd.MemoryManager.Free(_memoryBlock);
                }
            }
        }

        internal void SetImageLayout(uint mipLevel, uint arrayLayer, Silk.NET.Vulkan.ImageLayout layout)
        {
            _imageLayouts[CalculateSubresource(mipLevel, arrayLayer)] = layout;
        }
    }
}
