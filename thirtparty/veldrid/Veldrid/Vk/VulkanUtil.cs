using System;
using System.Diagnostics;

namespace Veldrid.Vk
{
    internal unsafe static class VulkanUtil
    {
        public const uint QueueFamilyIgnored = ~0U;

        private static Lazy<bool> s_isVulkanLoaded = new Lazy<bool>(TryLoadVulkan);
        private static readonly Lazy<string[]> s_instanceExtensions = new Lazy<string[]>(EnumerateInstanceExtensions);

        [Conditional("DEBUG")]
        public static void CheckResult(Silk.NET.Vulkan.Result result)
        {
            if (result != Silk.NET.Vulkan.Result.Success)
            {
                throw new VeldridException("Unsuccessful VkResult: " + result);
            }
        }

        public static bool TryFindMemoryType(Silk.NET.Vulkan.PhysicalDeviceMemoryProperties memProperties, uint typeFilter, Silk.NET.Vulkan.MemoryPropertyFlags properties, out uint typeIndex)
        {
            typeIndex = 0;

            for (int i = 0; i < memProperties.MemoryTypeCount; i++)
            {
                if (((typeFilter & (1 << i)) != 0)
                    && (memProperties.GetMemoryType((uint)i).PropertyFlags & properties) == properties)
                {
                    typeIndex = (uint)i;
                    return true;
                }
            }

            return false;
        }

        public static string[] EnumerateInstanceLayers()
        {
            var _vk = Silk.NET.Vulkan.Vk.GetApi();

            uint propCount = 0;
            var result = _vk.EnumerateInstanceLayerProperties(ref propCount, null);
            CheckResult(result);
            if (propCount == 0)
            {
                return Array.Empty<string>();
            }

            Silk.NET.Vulkan.LayerProperties[] props = new Silk.NET.Vulkan.LayerProperties[propCount];
            _vk.EnumerateInstanceLayerProperties(ref propCount, ref props[0]);

            string[] ret = new string[propCount];
            for (int i = 0; i < propCount; i++)
            {
                fixed (byte* layerNamePtr = props[i].LayerName)
                {
                    ret[i] = Util.GetString(layerNamePtr);
                }
            }

            return ret;
        }

        public static string[] GetInstanceExtensions() => s_instanceExtensions.Value;

        private static string[] EnumerateInstanceExtensions()
        {
            if (!IsVulkanLoaded())
            {
                return Array.Empty<string>();
            }

            var _vk = Silk.NET.Vulkan.Vk.GetApi();

            uint propCount = 0;
            var result = _vk.EnumerateInstanceExtensionProperties((byte*)null, ref propCount, null);
            if (result != Silk.NET.Vulkan.Result.Success)
            {
                return Array.Empty<string>();
            }

            if (propCount == 0)
            {
                return Array.Empty<string>();
            }

            Silk.NET.Vulkan.ExtensionProperties[] props = new Silk.NET.Vulkan.ExtensionProperties[propCount];
            _vk.EnumerateInstanceExtensionProperties((byte*)null, ref propCount, ref props[0]);

            string[] ret = new string[propCount];
            for (int i = 0; i < propCount; i++)
            {
                fixed (byte* extensionNamePtr = props[i].ExtensionName)
                {
                    ret[i] = Util.GetString(extensionNamePtr);
                }
            }

            return ret;
        }

        public static bool IsVulkanLoaded() => s_isVulkanLoaded.Value;
        private static bool TryLoadVulkan()
        {
            try
            {
                uint propCount;
                Silk.NET.Vulkan.Vk.GetApi().EnumerateInstanceExtensionProperties((byte*)null, &propCount, null);
                return true;
            }
            catch { return false; }
        }

        public static void TransitionImageLayout(
            Silk.NET.Vulkan.Vk _vk,
            Silk.NET.Vulkan.CommandBuffer cb,
            Silk.NET.Vulkan.Image  image,
            uint baseMipLevel,
            uint levelCount,
            uint baseArrayLayer,
            uint layerCount,
            Silk.NET.Vulkan.ImageAspectFlags aspectMask,
            Silk.NET.Vulkan.ImageLayout oldLayout,
            Silk.NET.Vulkan.ImageLayout newLayout)
        {
            Debug.Assert(oldLayout != newLayout);

            var barrier = new Silk.NET.Vulkan.ImageMemoryBarrier();
            barrier.SType = Silk.NET.Vulkan.StructureType.ImageMemoryBarrier;

            barrier.OldLayout = oldLayout;
            barrier.NewLayout = newLayout;
            barrier.SrcQueueFamilyIndex = QueueFamilyIgnored;
            barrier.DstQueueFamilyIndex = QueueFamilyIgnored;
            barrier.Image = image;
            barrier.SubresourceRange = new Silk.NET.Vulkan.ImageSubresourceRange();
            barrier.SubresourceRange.AspectMask = aspectMask;
            barrier.SubresourceRange.BaseMipLevel = baseMipLevel;
            barrier.SubresourceRange.LevelCount = levelCount;
            barrier.SubresourceRange.BaseArrayLayer = baseArrayLayer;
            barrier.SubresourceRange.LayerCount = layerCount;

            Silk.NET.Vulkan.PipelineStageFlags srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageNone;
            Silk.NET.Vulkan.PipelineStageFlags dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageNone;

            if ((oldLayout == Silk.NET.Vulkan.ImageLayout.Undefined || oldLayout == Silk.NET.Vulkan.ImageLayout.Preinitialized) && newLayout == Silk.NET.Vulkan.ImageLayout.TransferDstOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessNone;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTopOfPipeBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageFragmentShaderBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.TransferDstOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageFragmentShaderBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.Preinitialized && newLayout == Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessNone;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTopOfPipeBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.Preinitialized && newLayout == Silk.NET.Vulkan.ImageLayout.General)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessNone;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTopOfPipeBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageComputeShaderBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.Preinitialized && newLayout == Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessNone;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTopOfPipeBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageFragmentShaderBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.General && newLayout == Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferReadBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageFragmentShaderBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.General)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageFragmentShaderBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageComputeShaderBit;
            }

            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferReadBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageFragmentShaderBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.TransferDstOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageFragmentShaderBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.TransferDstOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferReadBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.TransferDstOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal)
            {
                barrier.SrcAccessMask =  Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentWriteBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.TransferDstOptimal)
            {
                barrier.SrcAccessMask =  Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentWriteBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.SrcAccessMask =  Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentWriteBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageFragmentShaderBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.DepthStencilAttachmentOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessDepthStencilAttachmentWriteBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageLateFragmentTestsBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageFragmentShaderBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.PresentSrcKhr)
            {
                barrier.SrcAccessMask =  Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentWriteBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessMemoryReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageBottomOfPipeBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.TransferDstOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.PresentSrcKhr)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessMemoryReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageBottomOfPipeBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.TransferDstOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit;
                barrier.DstAccessMask =  Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentWriteBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.TransferDstOptimal && newLayout == Silk.NET.Vulkan.ImageLayout.DepthStencilAttachmentOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferWriteBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessDepthStencilAttachmentWriteBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageLateFragmentTestsBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.General && newLayout == Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessShaderWriteBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageComputeShaderBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
            }
            else if (oldLayout == Silk.NET.Vulkan.ImageLayout.PresentSrcKhr && newLayout == Silk.NET.Vulkan.ImageLayout.TransferSrcOptimal)
            {
                barrier.SrcAccessMask = Silk.NET.Vulkan.AccessFlags.AccessMemoryReadBit;
                barrier.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessTransferReadBit;
                srcStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageBottomOfPipeBit;
                dstStageFlags = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageTransferBit;
            }
            else
            {
                Debug.Fail("Invalid image layout transition.");
            }

            _vk.CmdPipelineBarrier(
                cb,
                srcStageFlags,
                dstStageFlags,
                0,
                0, null,
                0, null,
                1, &barrier);
        }
    }

    internal unsafe static class VkPhysicalDeviceMemoryPropertiesEx
    {
        public static Silk.NET.Vulkan.MemoryType GetMemoryType(this Silk.NET.Vulkan.PhysicalDeviceMemoryProperties memoryProperties, uint index)
        {
            return (&memoryProperties.MemoryTypes.Element0)[index];
        }
    }
}
