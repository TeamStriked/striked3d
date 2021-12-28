using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Striked3D.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class Swapchain
    {
        private KhrSwapchain _vkSwapchain;
        protected RenderingInstance _instance;
        protected LogicalDevice _device;

        public SwapchainKHR _swapchain;
        private Image[] _swapchainImages;
        private Format _swapchainImageFormat;
        private Format _depthFormat;
        
        private Extent2D _swapchainExtent;
        private ImageView[] _swapchainImageViews;

        private TextureBuffer _colorTexture;
        private TextureBuffer _depthTexture;
        public TextureBuffer DepthTexture
        {
            get { return _depthTexture; }
        }
        public TextureBuffer ColorTexture
        {
            get { return _colorTexture; }
        }
        public Image[] SwapchainImages
        {
            get { return _swapchainImages; }
        }

        public ImageView[] SwapchainImageViews
        {
            get { return _swapchainImageViews; }
            set {  _swapchainImageViews = value; }
        }

        public Format SwapchaiImageFormat
        {
            get
            {
                return _swapchainImageFormat;
            }
        }
        public Format DepthFormat
        {
            get
            {
                return _depthFormat;
            }
        }
        public Extent2D SwapchainExtent
        {
            get
            {
                return _swapchainExtent;
            }
        }


        public Swapchain(RenderingInstance _instance, LogicalDevice _device)
        {
            this._device = _device;
            this._instance = _instance;
        }

        public KhrSwapchain NativeKhrSwapChain
        {
            get { return _vkSwapchain; }
        }

        public SwapchainKHR NativeHandleSwapChain
        {
            get { return _swapchain; }
            set {  _swapchain = value; }
        }

        public LogicalDevice Device
        {
            get { return _device; }
        }

        public unsafe void Destroy()
        {
            foreach (var imageView in _swapchainImageViews)
            {
                _instance.Api.DestroyImageView(this._device.NativeHandle, imageView, null);
            }

            _vkSwapchain.DestroySwapchain(this._device.NativeHandle, _swapchain, null);
        }

    
        public void createColorTexture(CommandPool cpool)
        {
            this._colorTexture = new TextureBuffer(_instance, _device, cpool);
            this._colorTexture.Create(
                this.SwapchaiImageFormat,
                ImageTiling.Optimal,
                ImageUsageFlags.ImageUsageTransientAttachmentBit | ImageUsageFlags.ImageUsageColorAttachmentBit,
                 MemoryPropertyFlags.MemoryPropertyDeviceLocalBit
                );
            this._colorTexture.createImageView(this.SwapchaiImageFormat, ImageAspectFlags.ImageAspectColorBit);
            this._colorTexture.setNewLayout( ImageLayout.ColorAttachmentOptimal);
        }

   
        public void createDepthTexture(CommandPool cpool)
        {
            this._depthTexture = new TextureBuffer(_instance, _device, cpool);
            this._depthTexture.Create(DepthFormat, ImageTiling.Optimal, ImageUsageFlags.ImageUsageDepthStencilAttachmentBit, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);
            this._depthTexture.createImageView(DepthFormat, ImageAspectFlags.ImageAspectDepthBit);
            this._depthTexture.setNewLayout(ImageLayout.DepthStencilAttachmentOptimal);
        }

        public unsafe bool Instanciate()
        {
            if (!_instance.Api.TryGetDeviceExtension(_instance.NativeHandle, _device.NativeHandle, out _vkSwapchain))
            {
                throw new NotSupportedException("KHR_swapchain extension not found.");
            }

            Logger.Debug(this, "Create swapchain");

            var swapChainSupport = this._device.PhysicalRenderingDevice.GetQuerySwapChainSupport();

            var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
            var presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
            var extent = ChooseSwapExtent(swapChainSupport.Capabilities);

            // TODO: On SDL minimizing the window does not affect the frameBufferSize.
            // This check can be removed if it does
            if (extent.Width == 0 || extent.Height == 0)
                return false;

            var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 &&
                imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            var createInfo = new SwapchainCreateInfoKHR
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = this._instance.NativeHandleSurface,
                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = extent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ImageUsageColorAttachmentBit
            };

            var indices = _device.PhysicalRenderingDevice.FindQueueFamilies();
            uint[] queueFamilyIndices = { indices.GraphicsFamily.Value, indices.PresentFamily.Value };

            fixed (uint* qfiPtr = queueFamilyIndices)
            {
                if (indices.GraphicsFamily != indices.PresentFamily)
                {
                    createInfo.ImageSharingMode = SharingMode.Concurrent;
                    createInfo.QueueFamilyIndexCount = 2;
                    createInfo.PQueueFamilyIndices = qfiPtr;
                }
                else
                {
                    createInfo.ImageSharingMode = SharingMode.Exclusive;
                }

                createInfo.PreTransform = swapChainSupport.Capabilities.CurrentTransform;
                createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr;
                createInfo.PresentMode = presentMode;
                createInfo.Clipped = Vk.True;

                createInfo.OldSwapchain = default;

                if (!this._instance.Api.TryGetDeviceExtension(this._instance.NativeHandle, this._instance.Api.CurrentDevice.Value, out _vkSwapchain))
                {
                    throw new NotSupportedException("KHR_swapchain extension not found.");
                }

                fixed (SwapchainKHR* swapchain = &_swapchain)
                {
                    if (_vkSwapchain.CreateSwapchain(this._device.NativeHandle, &createInfo, null, swapchain) != Result.Success)
                    {
                        throw new Exception("failed to create swap chain!");
                    }
                }
            }

            _vkSwapchain.GetSwapchainImages(this._device.NativeHandle, _swapchain, &imageCount, null);
            _swapchainImages = new Image[imageCount];
            fixed (Image* swapchainImage = _swapchainImages)
            {
                _vkSwapchain.GetSwapchainImages(this._device.NativeHandle, _swapchain, &imageCount, swapchainImage);
            }

            _swapchainImageFormat = surfaceFormat.Format;
            _swapchainExtent = extent;


            var depthResult = this._device.getSupportedDepthFormat();
            if(depthResult == default)
            {
                throw new Exception("Cant find depth format");
            }

            _depthFormat = depthResult;
            return true;
        }

        private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return capabilities.CurrentExtent;
            }

            var actualExtent = new Extent2D
            { Height = (uint)_instance.WindowHandle.FramebufferSize.Y, Width = (uint)_instance.WindowHandle.FramebufferSize.X };
            actualExtent.Width = new[]
            {
                capabilities.MinImageExtent.Width,
                new[] {capabilities.MaxImageExtent.Width, actualExtent.Width}.Min()
            }.Max();
            actualExtent.Height = new[]
            {
                capabilities.MinImageExtent.Height,
                new[] {capabilities.MaxImageExtent.Height, actualExtent.Height}.Min()
            }.Max();

            return actualExtent;
        }

        private PresentModeKHR ChooseSwapPresentMode(PresentModeKHR[] presentModes)
        {
            foreach (var availablePresentMode in presentModes)
            {
                //PresentModeImmediateKhr
                if (availablePresentMode == PresentModeKHR.PresentModeFifoKhr)
                {
                    return availablePresentMode;
                }
            }

            return PresentModeKHR.PresentModeFifoKhr;
        }

        private SurfaceFormatKHR ChooseSwapSurfaceFormat(SurfaceFormatKHR[] formats)
        {
            foreach (var format in formats)
            {
                if (format.Format == Format.B8G8R8A8Unorm)
                {
                    return format;
                }
            }

            return formats[0];
        }

        public unsafe void CreateImageViews()
        {
            Logger.Debug(this, "Create image views");
            _swapchainImageViews = new ImageView[_swapchainImages.Length];
            for (var i = 0; i < _swapchainImages.Length; i++)
            {
                var createInfo = new ImageViewCreateInfo
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = _swapchainImages[i],
                    ViewType = ImageViewType.ImageViewType2D,
                    Format = _swapchainImageFormat,
                    Components =
                    {
                        R = ComponentSwizzle.Identity,
                        G = ComponentSwizzle.Identity,
                        B = ComponentSwizzle.Identity,
                        A = ComponentSwizzle.Identity
                    },
                    SubresourceRange =
                    {
                        AspectMask = ImageAspectFlags.ImageAspectColorBit,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1
                    }
                };

                ImageView imageView = default;
                if (this._instance.Api.CreateImageView(_device.NativeHandle, &createInfo, null, &imageView) != Result.Success)
                {
                    throw new Exception("failed to create image views!");
                }

                _swapchainImageViews[i] = imageView;
            }
        }


        public unsafe void BeginRenderPass()
        {

        }


    }
}
