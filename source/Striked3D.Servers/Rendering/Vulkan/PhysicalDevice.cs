using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Striked3D.Core;
using VKPhysicalDevice = Silk.NET.Vulkan.PhysicalDevice;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class PhysicalDevice
    {
        protected RenderingInstance _instance;
        private VKPhysicalDevice _physicalDevice;
        private string[] _deviceExtensions = { KhrSwapchain.ExtensionName };

        public VKPhysicalDevice NativeHandle
        {
            get { return _physicalDevice; } 
        }

        public string[] DeviceExtensions
        {
            get { return _deviceExtensions; }   
        }

        public PhysicalDevice(RenderingInstance _instance)
        {
            this._instance = _instance; 
        }

  
        public unsafe void Instanciate()
        {
            Logger.Debug(this, "Pick physical device");

            var devices = _instance.Api.GetPhysicalDevices(_instance.NativeHandle);

            if (!devices.Any())
            {
                throw new NotSupportedException("Failed to find GPUs with Vulkan support.");
            }

            _physicalDevice = devices.FirstOrDefault(device =>
            {
                var indices = FindQueueFamilies(device);
                var extensionsSupported = CheckDeviceExtensionSupport(device);
                var swapChainAdequate = false;
                if (extensionsSupported)
                {
                    var swapChainSupport = QuerySwapChainSupport(device);
                    swapChainAdequate = swapChainSupport.Formats.Length != 0 && swapChainSupport.PresentModes.Length != 0;
                }

                return indices.IsComplete() && extensionsSupported && swapChainAdequate;
            });

            if (_physicalDevice.Handle == 0)
                throw new Exception("No suitable device.");
        }

        public SwapChainSupportDetails GetQuerySwapChainSupport()
        {
            return QuerySwapChainSupport(_physicalDevice);
        }

        // Caching the returned values breaks the ability for resizing the window
        private unsafe SwapChainSupportDetails QuerySwapChainSupport(VKPhysicalDevice device)
        {
            var details = new SwapChainSupportDetails();
            this._instance.NativeHandleKhrSurface.GetPhysicalDeviceSurfaceCapabilities(
                device, 
                this._instance.NativeHandleSurface, out var surfaceCapabilities);

            details.Capabilities = surfaceCapabilities;

            var formatCount = 0u;
            this._instance.NativeHandleKhrSurface.GetPhysicalDeviceSurfaceFormats(device,
                this._instance.NativeHandleSurface, &formatCount, null);

            if (formatCount != 0)
            {
                details.Formats = new SurfaceFormatKHR[formatCount];

                using var mem = GlobalMemory.Allocate((int)formatCount * sizeof(SurfaceFormatKHR));
                var formats = (SurfaceFormatKHR*)Unsafe.AsPointer(ref mem.GetPinnableReference());

                this._instance.NativeHandleKhrSurface.GetPhysicalDeviceSurfaceFormats(
                    device, this._instance.NativeHandleSurface, &formatCount, formats);

                for (var i = 0; i < formatCount; i++)
                {
                    details.Formats[i] = formats[i];
                }
            }

            var presentModeCount = 0u;
            this._instance.NativeHandleKhrSurface.GetPhysicalDeviceSurfacePresentModes(
                device, this._instance.NativeHandleSurface, &presentModeCount, null);

            if (presentModeCount != 0)
            {
                details.PresentModes = new PresentModeKHR[presentModeCount];

                using var mem = GlobalMemory.Allocate((int)presentModeCount * sizeof(PresentModeKHR));
                var modes = (PresentModeKHR*)Unsafe.AsPointer(ref mem.GetPinnableReference());

                this._instance.NativeHandleKhrSurface.GetPhysicalDeviceSurfacePresentModes(
                    device, this._instance.NativeHandleSurface, &presentModeCount, modes);

                for (var i = 0; i < presentModeCount; i++)
                {
                    details.PresentModes[i] = modes[i];
                }
            }

            return details;
        }

        private unsafe bool CheckDeviceExtensionSupport(VKPhysicalDevice device)
        {
            return _deviceExtensions.All(ext => _instance.Api.IsDeviceExtensionPresent(device, ext));
        }

        public unsafe QueueFamilyIndices FindQueueFamilies()
        {
            return this.FindQueueFamilies(this.NativeHandle);
        }

        // Caching these values might have unintended side effects
        private unsafe QueueFamilyIndices FindQueueFamilies(VKPhysicalDevice device)
        {
            var indices = new QueueFamilyIndices();

            uint queryFamilyCount = 0;
            _instance.Api.GetPhysicalDeviceQueueFamilyProperties(device, &queryFamilyCount, null);

            using var mem = GlobalMemory.Allocate((int)queryFamilyCount * sizeof(QueueFamilyProperties));
            var queueFamilies = (QueueFamilyProperties*)Unsafe.AsPointer(ref mem.GetPinnableReference());

            _instance.Api.GetPhysicalDeviceQueueFamilyProperties(device, &queryFamilyCount, queueFamilies);
            for (var i = 0u; i < queryFamilyCount; i++)
            {
                var queueFamily = queueFamilies[i];
                // note: HasFlag is slow on .NET Core 2.1 and below.
                // if you're targeting these versions, use ((queueFamily.QueueFlags & QueueFlags.QueueGraphicsBit) != 0)
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit))
                {
                    indices.GraphicsFamily = i;
                }

                this._instance.NativeHandleKhrSurface.GetPhysicalDeviceSurfaceSupport(
                    device, i, this._instance.NativeHandleSurface, out var presentSupport);

                if (presentSupport == Vk.True)
                {
                    indices.PresentFamily = i;
                }

                if (indices.IsComplete())
                {
                    break;
                }
            }

            return indices;
        }
    }
}
