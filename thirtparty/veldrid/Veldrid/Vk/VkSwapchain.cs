using System.Linq;
using static Veldrid.Vk.VulkanUtil;
using System;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Veldrid.Vk
{
    internal unsafe class VkSwapchain : Swapchain
    {
        private readonly VkGraphicsDevice _gd;
        private readonly Silk.NET.Vulkan.SurfaceKHR _surface;
        private Silk.NET.Vulkan.SwapchainKHR _deviceSwapchain;
        private readonly VkSwapchainFramebuffer _framebuffer;
        private Silk.NET.Vulkan.Fence _imageAvailableFence;
        private readonly uint _presentQueueIndex;
        private readonly Silk.NET.Vulkan.Queue _presentQueue;
        private bool _syncToVBlank;
        private readonly SwapchainSource _swapchainSource;
        private readonly bool _colorSrgb;
        private bool? _newSyncToVBlank;
        private uint _currentImageIndex;
        private string _name;
        private bool _disposed;

        public override string Name { get => _name; set { _name = value; _gd.SetResourceName(this, value); } }
        public override Framebuffer Framebuffer => _framebuffer;
        public override bool SyncToVerticalBlank
        {
            get => _newSyncToVBlank ?? _syncToVBlank;
            set
            {
                if (_syncToVBlank != value)
                {
                    _newSyncToVBlank = value;
                }
            }
        }

        public override bool IsDisposed => _disposed;

        public Silk.NET.Vulkan.SwapchainKHR DeviceSwapchain => _deviceSwapchain;
        public uint ImageIndex => _currentImageIndex;
        public Silk.NET.Vulkan.Fence ImageAvailableFence => _imageAvailableFence;
        public Silk.NET.Vulkan.SurfaceKHR Surface => _surface;
        public Silk.NET.Vulkan.Queue PresentQueue => _presentQueue;
        public uint PresentQueueIndex => _presentQueueIndex;
        public ResourceRefCount RefCount { get; }

        private Silk.NET.Vulkan.Vk _vk;

        public VkSwapchain(VkGraphicsDevice gd, ref SwapchainDescription description) : this(gd, ref description, default) { }

        public VkSwapchain(VkGraphicsDevice gd, ref SwapchainDescription description, Silk.NET.Vulkan.SurfaceKHR existingSurface)
        {
            _gd = gd;
            _vk = gd.vk;

            _syncToVBlank = description.SyncToVerticalBlank;
            _swapchainSource = description.Source;
            _colorSrgb = description.ColorSrgb;

            if (existingSurface.Handle == default)
            {
                _surface = VkSurfaceUtil.CreateSurface(gd, gd.Instance, _swapchainSource);
            }
            else
            {
                _surface = existingSurface;
            }

            if (!GetPresentQueueIndex(out _presentQueueIndex))
            {
                throw new VeldridException($"The system does not support presenting the given Vulkan surface.");
            }
            _vk.GetDeviceQueue(_gd.Device, _presentQueueIndex, 0, out _presentQueue);

            _framebuffer = new VkSwapchainFramebuffer(gd, this, _surface, description.Width, description.Height, description.DepthFormat);

            CreateSwapchain(description.Width, description.Height);

            Silk.NET.Vulkan.FenceCreateInfo fenceCI = new Silk.NET.Vulkan.FenceCreateInfo();
            fenceCI.SType = Silk.NET.Vulkan.StructureType.FenceCreateInfo;

            fenceCI.Flags = 0;
            _vk.CreateFence(_gd.Device, &fenceCI, null, out _imageAvailableFence);

            AcquireNextImage(_gd.Device, default, _imageAvailableFence);
            fixed(Silk.NET.Vulkan.Fence* ptr = &_imageAvailableFence)
            {
                _vk.WaitForFences(_gd.Device, 1, ptr, true, ulong.MaxValue);
                _vk.ResetFences(_gd.Device, 1, ptr);
            }

            RefCount = new ResourceRefCount(DisposeCore);
        }

        public override void Resize(uint width, uint height)
        {
            RecreateAndReacquire(width, height);
        }

        public bool AcquireNextImage(Silk.NET.Vulkan.Device device, Silk.NET.Vulkan.Semaphore semaphore, Silk.NET.Vulkan.Fence fence)
        {
            if (_newSyncToVBlank != null)
            {
                _syncToVBlank = _newSyncToVBlank.Value;
                _newSyncToVBlank = null;
                RecreateAndReacquire(_framebuffer.Width, _framebuffer.Height);
                return false;
            }

            var result = _gd.vkSwapchain.AcquireNextImage(
                device,
                _deviceSwapchain,
                ulong.MaxValue,
                semaphore,
                fence,
                ref _currentImageIndex);
            _framebuffer.SetImageIndex(_currentImageIndex);

            if (result == Silk.NET.Vulkan.Result.ErrorOutOfDateKhr || result == Silk.NET.Vulkan.Result.SuboptimalKhr)
            {
                CreateSwapchain(_framebuffer.Width, _framebuffer.Height);
                return false;
            }
            else if (result != Silk.NET.Vulkan.Result.Success)
            {
                throw new VeldridException("Could not acquire next image from the Vulkan swapchain.");
            }

            return true;
        }

        private void RecreateAndReacquire(uint width, uint height)
        {
            if (CreateSwapchain(width, height))
            {
                if (AcquireNextImage(_gd.Device, default, _imageAvailableFence))
                {
                    fixed (Silk.NET.Vulkan.Fence* ptr = &_imageAvailableFence)
                    {
                        _vk.WaitForFences(_gd.Device, 1, ptr, true, ulong.MaxValue);
                        _vk.ResetFences(_gd.Device, 1, ptr);
                    }
                }
            }
        }

        private bool CreateSwapchain(uint width, uint height)
        {

            
            // Obtain the surface capabilities first -- this will indicate whether the surface has been lost.
            var result = _gd.vkSurface.GetPhysicalDeviceSurfaceCapabilities(_gd.PhysicalDevice, _surface, out Silk.NET.Vulkan.SurfaceCapabilitiesKHR surfaceCapabilities);
            if (result == Silk.NET.Vulkan.Result.ErrorSurfaceLostKhr)
            {
                throw new VeldridException($"The Swapchain's underlying surface has been lost.");
            }

            if (surfaceCapabilities.MinImageExtent.Width == 0 && surfaceCapabilities.MinImageExtent.Height == 0
                && surfaceCapabilities.MaxImageExtent.Width == 0 && surfaceCapabilities.MaxImageExtent.Height == 0)
            {
                return false;
            }

            if (_deviceSwapchain.Handle != default)
            {
                _gd.WaitForIdle();
            }

            _currentImageIndex = 0;
            uint surfaceFormatCount = 0;
            result = _gd.vkSurface.GetPhysicalDeviceSurfaceFormats(_gd.PhysicalDevice, _surface, &surfaceFormatCount, null);
            CheckResult(result);
            Silk.NET.Vulkan.SurfaceFormatKHR[] formats = new Silk.NET.Vulkan.SurfaceFormatKHR[surfaceFormatCount];
            result = _gd.vkSurface.GetPhysicalDeviceSurfaceFormats(_gd.PhysicalDevice, _surface, ref surfaceFormatCount, out formats[0]);
            CheckResult(result);

            Silk.NET.Vulkan.Format desiredFormat = _colorSrgb
                ? Silk.NET.Vulkan.Format.B8G8R8A8Srgb
                : Silk.NET.Vulkan.Format.B8G8R8A8Unorm;

            Silk.NET.Vulkan.SurfaceFormatKHR surfaceFormat = new Silk.NET.Vulkan.SurfaceFormatKHR();
            if (formats.Length == 1 && formats[0].Format == Silk.NET.Vulkan.Format.Undefined)
            {
                surfaceFormat = new Silk.NET.Vulkan.SurfaceFormatKHR { ColorSpace = Silk.NET.Vulkan.ColorSpaceKHR.ColorspaceSrgbNonlinearKhr, Format = desiredFormat };
            }
            else
            {
                foreach (Silk.NET.Vulkan.SurfaceFormatKHR format in formats)
                {
                    if (format.ColorSpace == Silk.NET.Vulkan.ColorSpaceKHR.ColorspaceSrgbNonlinearKhr && format.Format == desiredFormat)
                    {
                        surfaceFormat = format;
                        break;
                    }
                }
                if (surfaceFormat.Format == Silk.NET.Vulkan.Format.Undefined)
                {
                    if (_colorSrgb && surfaceFormat.Format != Silk.NET.Vulkan.Format.R8G8B8A8Srgb)
                    {
                        throw new VeldridException($"Unable to create an sRGB Swapchain for this surface.");
                    }

                    surfaceFormat = formats[0];
                }
            }

            uint presentModeCount = 0;
           
            result = _gd.vkSurface.GetPhysicalDeviceSurfacePresentModes(_gd.PhysicalDevice, _surface, ref presentModeCount, null);
            CheckResult(result);
            Silk.NET.Vulkan.PresentModeKHR[] presentModes = new Silk.NET.Vulkan.PresentModeKHR[presentModeCount];
            result = _gd.vkSurface.GetPhysicalDeviceSurfacePresentModes(_gd.PhysicalDevice, _surface, ref presentModeCount, out presentModes[0]);
            CheckResult(result);

            Silk.NET.Vulkan.PresentModeKHR presentMode = Silk.NET.Vulkan.PresentModeKHR.PresentModeFifoKhr;

            if (_syncToVBlank)
            {
                if (presentModes.Contains(Silk.NET.Vulkan.PresentModeKHR.PresentModeFifoRelaxedKhr))
                {
                    presentMode = Silk.NET.Vulkan.PresentModeKHR.PresentModeFifoRelaxedKhr;
                }
            }
            else
            {
                if (presentModes.Contains(Silk.NET.Vulkan.PresentModeKHR.PresentModeMailboxKhr))
                {
                    presentMode = Silk.NET.Vulkan.PresentModeKHR.PresentModeMailboxKhr;
                }
                else if (presentModes.Contains(Silk.NET.Vulkan.PresentModeKHR.PresentModeImmediateKhr))
                {
                    presentMode = Silk.NET.Vulkan.PresentModeKHR.PresentModeImmediateKhr;
                }
            }

            uint maxImageCount = surfaceCapabilities.MaxImageCount == 0 ? uint.MaxValue : surfaceCapabilities.MaxImageCount;
            uint imageCount = Math.Min(maxImageCount, surfaceCapabilities.MinImageCount + 1);

            Silk.NET.Vulkan.SwapchainCreateInfoKHR swapchainCI = new Silk.NET.Vulkan.SwapchainCreateInfoKHR();
            swapchainCI.SType = Silk.NET.Vulkan.StructureType.SwapchainCreateInfoKhr;

            swapchainCI.Surface = _surface;
            swapchainCI.PresentMode = presentMode;
            swapchainCI.ImageFormat = surfaceFormat.Format;
            swapchainCI.ImageColorSpace = surfaceFormat.ColorSpace;
            uint clampedWidth = Util.Clamp(width, surfaceCapabilities.MinImageExtent.Width, surfaceCapabilities.MaxImageExtent.Width);
            uint clampedHeight = Util.Clamp(height, surfaceCapabilities.MinImageExtent.Height, surfaceCapabilities.MaxImageExtent.Height);
            swapchainCI.ImageExtent = new Silk.NET.Vulkan.Extent2D { Width = clampedWidth, Height = clampedHeight };
            swapchainCI.MinImageCount = imageCount;
            swapchainCI.ImageArrayLayers = 1;
            swapchainCI.ImageUsage = Silk.NET.Vulkan.ImageUsageFlags.ImageUsageColorAttachmentBit | Silk.NET.Vulkan.ImageUsageFlags.ImageUsageTransferDstBit;

            FixedArray2<uint> queueFamilyIndices = new FixedArray2<uint>(_gd.GraphicsQueueIndex, _gd.PresentQueueIndex);

            if (_gd.GraphicsQueueIndex != _gd.PresentQueueIndex)
            {
                swapchainCI.ImageSharingMode = Silk.NET.Vulkan.SharingMode.Concurrent;
                swapchainCI.QueueFamilyIndexCount = 2;
                swapchainCI.PQueueFamilyIndices = &queueFamilyIndices.First;
            }
            else
            {
                swapchainCI.ImageSharingMode = Silk.NET.Vulkan.SharingMode.Exclusive;
                swapchainCI.QueueFamilyIndexCount = 0;
            }

            swapchainCI.PreTransform = Silk.NET.Vulkan.SurfaceTransformFlagsKHR.SurfaceTransformIdentityBitKhr;
            swapchainCI.CompositeAlpha = Silk.NET.Vulkan.CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr;
            swapchainCI.Clipped = true;

            Silk.NET.Vulkan.SwapchainKHR oldSwapchain = _deviceSwapchain;
            swapchainCI.OldSwapchain = oldSwapchain;

            result = _gd.vkSwapchain.CreateSwapchain(_gd.Device, &swapchainCI, null, out _deviceSwapchain);
            CheckResult(result);
            if (oldSwapchain.Handle != default)
            {
                _gd.vkSwapchain.DestroySwapchain(_gd.Device, oldSwapchain, null);
            }

            _framebuffer.SetNewSwapchain(_deviceSwapchain, width, height, surfaceFormat, swapchainCI.ImageExtent);
            return true;
        }

        private bool GetPresentQueueIndex(out uint queueFamilyIndex)
        {
            uint graphicsQueueIndex = _gd.GraphicsQueueIndex;
            uint presentQueueIndex = _gd.PresentQueueIndex;

            if (QueueSupportsPresent(graphicsQueueIndex, _surface))
            {
                queueFamilyIndex = graphicsQueueIndex;
                return true;
            }
            else if (graphicsQueueIndex != presentQueueIndex && QueueSupportsPresent(presentQueueIndex, _surface))
            {
                queueFamilyIndex = presentQueueIndex;
                return true;
            }

            queueFamilyIndex = 0;
            return false;
        }

        private bool QueueSupportsPresent(uint queueFamilyIndex, Silk.NET.Vulkan.SurfaceKHR surface)
        {
            var result = _gd.vkSurface.GetPhysicalDeviceSurfaceSupport(
                _gd.PhysicalDevice,
                queueFamilyIndex,
                surface,
                out Silk.NET.Core.Bool32 supported);
            CheckResult(result);
            return supported;
        }

        public override void Dispose()
        {
            RefCount.Decrement();
        }

        private void DisposeCore()
        {
            _vk.DestroyFence(_gd.Device, _imageAvailableFence, null);
            _framebuffer.Dispose();

            _gd.vkSwapchain.DestroySwapchain(_gd.Device, _deviceSwapchain, null);
            _gd.vkSurface.DestroySurface(_gd.Instance, _surface, null);

            _disposed = true;
        }
    }
}
