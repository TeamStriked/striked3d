using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Striked3D.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class LogicalDevice
    {
        protected PhysicalDevice _physialDevice;
        protected RenderingInstance _instance;
        private Device _device;
        private Queue _graphicsQueue;
        private Queue _presentQueue;
        private SampleCountFlags _mssaLevel;

        public SampleCountFlags MsaaLevel
        {
            get { return _mssaLevel; }
        }

        public Device NativeHandle
        {
            get { return _device; }
        }

        public Queue NativeGraphicsQueue
        {
            get { return _graphicsQueue; }
        }

        public Queue NativePresentQueue
        {
            get { return _presentQueue; }
        }


        public PhysicalDevice PhysicalRenderingDevice
        {
            get { return _physialDevice; }
        }

        public LogicalDevice(RenderingInstance _instance, PhysicalDevice _physialDevice)
        {        
            this._physialDevice = _physialDevice;
            this._instance = _instance;

        }
        private uint _graphicsFamilyIndex;

        public uint GraphicsFamilyIndex
        {
            get { return _graphicsFamilyIndex; }    
        }

        public unsafe void Instanciate()
        {
            Logger.Debug(this, "Create logical device");

            var indices = this._physialDevice.FindQueueFamilies();
            var uniqueQueueFamilies = indices.GraphicsFamily.Value == indices.PresentFamily.Value
                ? new[] { indices.GraphicsFamily.Value }
                : new[] { indices.GraphicsFamily.Value, indices.PresentFamily.Value };
            _graphicsFamilyIndex = indices.GraphicsFamily.Value;

            using var mem = GlobalMemory.Allocate((int)uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
            var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

            var queuePriority = 1f;
            for (var i = 0; i < uniqueQueueFamilies.Length; i++)
            {
                var queueCreateInfo = new DeviceQueueCreateInfo
                {
                    SType = StructureType.DeviceQueueCreateInfo,
                    QueueFamilyIndex = uniqueQueueFamilies[i],
                    QueueCount = 1,
                    PQueuePriorities = &queuePriority
                };
                queueCreateInfos[i] = queueCreateInfo;
            }

            var deviceFeatures = new PhysicalDeviceFeatures();

            var createInfo = new DeviceCreateInfo();
            createInfo.SType = StructureType.DeviceCreateInfo;
            createInfo.QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length;
            createInfo.PQueueCreateInfos = queueCreateInfos;
            createInfo.PEnabledFeatures = &deviceFeatures;
            createInfo.EnabledExtensionCount = (uint)_physialDevice.DeviceExtensions.Length;

            var enabledExtensionNames = SilkMarshal.StringArrayToPtr(_physialDevice.DeviceExtensions);
            createInfo.PpEnabledExtensionNames = (byte**)enabledExtensionNames;

            if (this._instance.ValidationEnabled)
            {
                createInfo.EnabledLayerCount = (uint)_instance.Validationlayer.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(_instance.Validationlayer);
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
            }

            fixed (Device* device = &_device)
            {
                if (_instance.Api.CreateDevice(_physialDevice.NativeHandle, &createInfo, null, device) != Result.Success)
                {
                    throw new Exception("Failed to create logical device.");
                }
            }

            fixed (Queue* graphicsQueue = &_graphicsQueue)
            {
                _instance.Api.GetDeviceQueue(_device, indices.GraphicsFamily.Value, 0, graphicsQueue);
            }

            fixed (Queue* presentQueue = &_presentQueue)
            {
                _instance.Api.GetDeviceQueue(_device, indices.PresentFamily.Value, 0, presentQueue);
            }

            this._mssaLevel = this.getMaxMsaaLevel();
            _instance.Api.CurrentDevice = _device;
            Console.WriteLine($"{_instance.Api.CurrentInstance?.Handle} {_instance.Api.CurrentDevice?.Handle}");

        }

        public SampleCountFlags getMaxMsaaLevel() 
        {
            _instance.Api.GetPhysicalDeviceProperties(this._physialDevice.NativeHandle, out var prop);
            var counts = prop.Limits.FramebufferColorSampleCounts & prop.Limits.FramebufferDepthSampleCounts;

            if ((counts & SampleCountFlags.SampleCount64Bit) == SampleCountFlags.SampleCount64Bit) { return SampleCountFlags.SampleCount64Bit; }
            if ((counts & SampleCountFlags.SampleCount32Bit) == SampleCountFlags.SampleCount32Bit) { return SampleCountFlags.SampleCount32Bit; }
            if ((counts & SampleCountFlags.SampleCount16Bit) == SampleCountFlags.SampleCount16Bit) { return SampleCountFlags.SampleCount16Bit; }
            if ((counts & SampleCountFlags.SampleCount8Bit) == SampleCountFlags.SampleCount8Bit) { return SampleCountFlags.SampleCount8Bit; }
            if ((counts & SampleCountFlags.SampleCount4Bit) == SampleCountFlags.SampleCount4Bit) { return SampleCountFlags.SampleCount4Bit; }
            if ((counts & SampleCountFlags.SampleCount2Bit) == SampleCountFlags.SampleCount2Bit) { return SampleCountFlags.SampleCount2Bit; }

            return SampleCountFlags.SampleCount1Bit;
        }

        public void WaitFor()
        {
            _ = _instance.Api.DeviceWaitIdle(_device);
        }

    
        public unsafe void Destroy()
        {
            this._instance.Api.DestroyDevice(_device, null);
        }

        public uint GetMemoryTypeIndex( MemoryPropertyFlags properties, uint type_bits)
        {
            _instance.Api.GetPhysicalDeviceMemoryProperties(this._physialDevice.NativeHandle, out var prop);
            for (int i = 0; i < prop.MemoryTypeCount; i++)
            {
                if ((prop.MemoryTypes[i].PropertyFlags & properties) == properties && (type_bits & (1u << i)) != 0)
                {
                    return (uint)i;
                }
            }
            return 0xFFFFFFFF; // Unable to find memoryType
        }

        public Format getSupportedDepthFormat()
        {
            // Since all depth formats may be optional, we need to find a suitable depth format to use
            // Start with the highest precision packed format
            var depthFormats = new Format[5] { Format.D32SfloatS8Uint, Format.D32Sfloat, Format.D24UnormS8Uint, Format.D16UnormS8Uint, Format.D16Unorm };

            foreach (var format in depthFormats)
            {
                var formatProps = new FormatProperties();
                _instance.Api.GetPhysicalDeviceFormatProperties(this._physialDevice.NativeHandle, format, out formatProps);
                // Format must support depth stencil attachment for optimal tiling

                FormatFeatureFlags depthBit = FormatFeatureFlags.FormatFeatureDepthStencilAttachmentBit;
                if ((formatProps.OptimalTilingFeatures & depthBit) == depthBit)
                {
                    return format;
                }
            }

            return default;
        }
    }
}
