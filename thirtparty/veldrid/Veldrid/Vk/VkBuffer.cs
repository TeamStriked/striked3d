using System;
using static Veldrid.Vk.VulkanUtil;

namespace Veldrid.Vk
{
    internal unsafe class VkBuffer : DeviceBuffer
    {
        private readonly VkGraphicsDevice _gd;
        private readonly Silk.NET.Vulkan.Buffer _deviceBuffer;
        private readonly VkMemoryBlock _memory;
        private readonly Silk.NET.Vulkan.MemoryRequirements _bufferMemoryRequirements;
        public ResourceRefCount RefCount { get; }
        private bool _destroyed;
        private string _name;
        public override bool IsDisposed => _destroyed;

        public override uint SizeInBytes { get; }
        public override BufferUsage Usage { get; }

        public Silk.NET.Vulkan.Buffer DeviceBuffer => _deviceBuffer;
        public VkMemoryBlock Memory => _memory;

        public Silk.NET.Vulkan.MemoryRequirements BufferMemoryRequirements => _bufferMemoryRequirements;

        public VkBuffer(VkGraphicsDevice gd, uint sizeInBytes, BufferUsage usage, string callerMember = null)
        {
            _gd = gd;
            SizeInBytes = sizeInBytes;
            Usage = usage;

            Silk.NET.Vulkan.BufferUsageFlags vkUsage = Silk.NET.Vulkan.BufferUsageFlags.BufferUsageTransferSrcBit | Silk.NET.Vulkan.BufferUsageFlags.BufferUsageTransferDstBit;
            if ((usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer)
            {
                vkUsage |= Silk.NET.Vulkan.BufferUsageFlags.BufferUsageVertexBufferBit;
            }
            if ((usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer)
            {
                vkUsage |= Silk.NET.Vulkan.BufferUsageFlags.BufferUsageIndexBufferBit;
            }
            if ((usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer)
            {
                vkUsage |= Silk.NET.Vulkan.BufferUsageFlags.BufferUsageUniformBufferBit;
            }
            if ((usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite
                || (usage & BufferUsage.StructuredBufferReadOnly) == BufferUsage.StructuredBufferReadOnly)
            {
                vkUsage |= Silk.NET.Vulkan.BufferUsageFlags.BufferUsageStorageBufferBit;
            }
            if ((usage & BufferUsage.IndirectBuffer) == BufferUsage.IndirectBuffer)
            {
                vkUsage |= Silk.NET.Vulkan.BufferUsageFlags.BufferUsageIndirectBufferBit;
            }

            var bufferCI =  new Silk.NET.Vulkan.BufferCreateInfo();
            bufferCI.Size = sizeInBytes;
            bufferCI.Usage = vkUsage;
            bufferCI.SType = Silk.NET.Vulkan.StructureType.BufferCreateInfo;

            var result = gd.vk.CreateBuffer(gd.Device, &bufferCI, null, out _deviceBuffer);
            CheckResult(result);

            bool prefersDedicatedAllocation;
            if (_gd.GetBufferMemoryRequirements2 != null)
            {
                Silk.NET.Vulkan.BufferMemoryRequirementsInfo2KHR memReqInfo2 = new Silk.NET.Vulkan.BufferMemoryRequirementsInfo2KHR();
                memReqInfo2.SType = Silk.NET.Vulkan.StructureType.BufferMemoryRequirementsInfo2Khr;
                memReqInfo2.Buffer = _deviceBuffer;
                Silk.NET.Vulkan.MemoryRequirements2KHR memReqs2 = new Silk.NET.Vulkan.MemoryRequirements2KHR();
                memReqs2.SType = Silk.NET.Vulkan.StructureType.MemoryRequirements2Khr;

                Silk.NET.Vulkan.MemoryDedicatedRequirementsKHR dedicatedReqs = new Silk.NET.Vulkan.MemoryDedicatedRequirementsKHR();
                dedicatedReqs.SType = Silk.NET.Vulkan.StructureType.MemoryDedicatedRequirementsKhr;

                memReqs2.PNext = &dedicatedReqs;
                _gd.GetBufferMemoryRequirements2(_gd.Device, &memReqInfo2, &memReqs2);
                _bufferMemoryRequirements = memReqs2.MemoryRequirements;
                prefersDedicatedAllocation = dedicatedReqs.PrefersDedicatedAllocation || dedicatedReqs.RequiresDedicatedAllocation;
            }
            else
            {
                gd.vk.GetBufferMemoryRequirements(gd.Device, _deviceBuffer, out _bufferMemoryRequirements);
                prefersDedicatedAllocation = false;
            }

            var isStaging = (usage & BufferUsage.Staging) == BufferUsage.Staging;
            var hostVisible = isStaging || (usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;

            Silk.NET.Vulkan.MemoryPropertyFlags memoryPropertyFlags =
                hostVisible
                ? Silk.NET.Vulkan.MemoryPropertyFlags.MemoryPropertyHostVisibleBit | Silk.NET.Vulkan.MemoryPropertyFlags.MemoryPropertyHostCoherentBit
                : Silk.NET.Vulkan.MemoryPropertyFlags.MemoryPropertyDeviceLocalBit;
            if (isStaging)
            {
                // Use "host cached" memory for staging when available, for better performance of GPU -> CPU transfers
                var hostCachedAvailable = TryFindMemoryType(
                    gd.PhysicalDeviceMemProperties,
                    _bufferMemoryRequirements.MemoryTypeBits,
                    memoryPropertyFlags | Silk.NET.Vulkan.MemoryPropertyFlags.MemoryPropertyHostCachedBit,
                    out _);
                if (hostCachedAvailable)
                {
                    memoryPropertyFlags |= Silk.NET.Vulkan.MemoryPropertyFlags.MemoryPropertyHostCachedBit;
                }
            }

            VkMemoryBlock memoryToken = gd.MemoryManager.Allocate(
                gd.PhysicalDeviceMemProperties,
                _bufferMemoryRequirements.MemoryTypeBits,
                memoryPropertyFlags,
                hostVisible,
                _bufferMemoryRequirements.Size,
                _bufferMemoryRequirements.Alignment,
                prefersDedicatedAllocation,
                default,
                _deviceBuffer);
            _memory = memoryToken;
            result = gd.vk.BindBufferMemory(gd.Device, _deviceBuffer, _memory.DeviceMemory, _memory.Offset);
            CheckResult(result);

            RefCount = new ResourceRefCount(DisposeCore);
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
                _gd.vk.DestroyBuffer(_gd.Device, _deviceBuffer, null);
                _gd.MemoryManager.Free(Memory);
            }
        }
    }
}
