﻿using static Veldrid.Vk.VulkanUtil;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Veldrid.Vk
{
    internal unsafe class VkSwapchainFramebuffer : VkFramebufferBase
    {
        private readonly VkGraphicsDevice _gd;
        private readonly Silk.NET.Vulkan.Vk _vk;
        private readonly VkSwapchain _swapchain;
        private readonly Silk.NET.Vulkan.SurfaceKHR _surface;
        private readonly PixelFormat? _depthFormat;
        private uint _currentImageIndex;

        private VkFramebuffer[] _scFramebuffers;
        private Silk.NET.Vulkan.Image[] _scImages;
        private Silk.NET.Vulkan.Format _scImageFormat;
        private Silk.NET.Vulkan.Extent2D _scExtent;
        private FramebufferAttachment[][] _scColorTextures;

        private FramebufferAttachment? _depthAttachment;
        private uint _desiredWidth;
        private uint _desiredHeight;
        private bool _destroyed;
        private string _name;
        private OutputDescription _outputDescription;

        public override Silk.NET.Vulkan.Framebuffer CurrentFramebuffer => _scFramebuffers[(int)_currentImageIndex].CurrentFramebuffer;

        public override Silk.NET.Vulkan.RenderPass RenderPassNoClear_Init => _scFramebuffers[0].RenderPassNoClear_Init;
        public override Silk.NET.Vulkan.RenderPass RenderPassNoClear_Load => _scFramebuffers[0].RenderPassNoClear_Load;
        public override Silk.NET.Vulkan.RenderPass RenderPassClear => _scFramebuffers[0].RenderPassClear; 

        public override IReadOnlyList<FramebufferAttachment> ColorTargets => _scColorTextures[(int)_currentImageIndex];

        public override FramebufferAttachment? DepthTarget => _depthAttachment;

        public override uint RenderableWidth => _scExtent.Width;
        public override uint RenderableHeight => _scExtent.Height;

        public override uint Width => _desiredWidth;
        public override uint Height => _desiredHeight;

        public uint ImageIndex => _currentImageIndex;

        public override OutputDescription OutputDescription => _outputDescription;

        public override uint AttachmentCount { get; }

        public VkSwapchain Swapchain => _swapchain;

        public override bool IsDisposed => _destroyed;

        public VkSwapchainFramebuffer(
            VkGraphicsDevice gd,
            VkSwapchain swapchain,
            Silk.NET.Vulkan.SurfaceKHR surface,
            uint width,
            uint height,
            PixelFormat? depthFormat)
            : base()
        {
            _gd = gd;
            _vk = gd.vk;
            _swapchain = swapchain;
            _surface = surface;
            _depthFormat = depthFormat;

            AttachmentCount = depthFormat.HasValue ? 2u : 1u; // 1 Color + 1 Depth
        }

        internal void SetImageIndex(uint index)
        {
            _currentImageIndex = index;
        }

        internal void SetNewSwapchain(
            Silk.NET.Vulkan.SwapchainKHR deviceSwapchain,
            uint width,
            uint height,
           Silk.NET.Vulkan.SurfaceFormatKHR surfaceFormat,
           Silk.NET.Vulkan.Extent2D swapchainExtent)
        {
            _desiredWidth = width;
            _desiredHeight = height;

            // Get the images
            uint scImageCount = 0;
            var result = _gd.vkSwapchain.GetSwapchainImages(_gd.Device, deviceSwapchain, ref scImageCount, null);
            CheckResult(result);
            if (_scImages == null)
            {
                _scImages = new Silk.NET.Vulkan.Image[(int)scImageCount];
            }
            result = _gd.vkSwapchain.GetSwapchainImages(_gd.Device, deviceSwapchain, ref scImageCount, out _scImages[0]);
            CheckResult(result);

            _scImageFormat = surfaceFormat.Format;
            _scExtent = swapchainExtent;

            CreateDepthTexture();
            CreateFramebuffers();

            _outputDescription = OutputDescription.CreateFromFramebuffer(this);
        }

        private void DestroySwapchainFramebuffers()
        {
            if (_scFramebuffers != null)
            {
                for (int i = 0; i < _scFramebuffers.Length; i++)
                {
                    _scFramebuffers[i]?.Dispose();
                    _scFramebuffers[i] = null;
                }
                Array.Clear(_scFramebuffers, 0, _scFramebuffers.Length);
            }
        }

        private void CreateDepthTexture()
        {
            if (_depthFormat.HasValue)
            {
                _depthAttachment?.Target.Dispose();
                VkTexture depthTexture = (VkTexture)_gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                    Math.Max(1, _scExtent.Width),
                    Math.Max(1, _scExtent.Height),
                    1,
                    1,
                    _depthFormat.Value,
                    TextureUsage.DepthStencil));
                _depthAttachment = new FramebufferAttachment(depthTexture, 0);
            }
        }

        private void CreateFramebuffers()
        {
            if (_scFramebuffers != null)
            {
                for (int i = 0; i < _scFramebuffers.Length; i++)
                {
                    _scFramebuffers[i]?.Dispose();
                    _scFramebuffers[i] = null;
                }
                Array.Clear(_scFramebuffers, 0, _scFramebuffers.Length);
            }

            Util.EnsureArrayMinimumSize(ref _scFramebuffers, (uint)_scImages.Length);
            Util.EnsureArrayMinimumSize(ref _scColorTextures, (uint)_scImages.Length);
            for (uint i = 0; i < _scImages.Length; i++)
            {
                VkTexture colorTex = new VkTexture(
                    _gd,
                    Math.Max(1, _scExtent.Width),
                    Math.Max(1, _scExtent.Height),
                    1,
                    1,
                    _scImageFormat,
                    TextureUsage.RenderTarget,
                    TextureSampleCount.Count1,
                    _scImages[i]);
                FramebufferDescription desc = new FramebufferDescription(_depthAttachment?.Target, colorTex);
                VkFramebuffer fb = new VkFramebuffer(_gd, ref desc, true);
                _scFramebuffers[i] = fb;
                _scColorTextures[i] = new FramebufferAttachment[] { new FramebufferAttachment(colorTex, 0) };
            }
        }

        public override void TransitionToIntermediateLayout(Silk.NET.Vulkan.CommandBuffer cb)
        {
            for (int i = 0; i < ColorTargets.Count; i++)
            {
                FramebufferAttachment ca = ColorTargets[i];
                VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
                vkTex.SetImageLayout(0, ca.ArrayLayer, Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal);
            }
        }

        public override void TransitionToFinalLayout(Silk.NET.Vulkan.CommandBuffer cb)
        {
            for (int i = 0; i < ColorTargets.Count; i++)
            {
                FramebufferAttachment ca = ColorTargets[i];
                VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
                vkTex.TransitionImageLayout(cb, 0, 1, ca.ArrayLayer, 1, Silk.NET.Vulkan.ImageLayout.PresentSrcKhr);
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

        protected override void DisposeCore()
        {
            if (!_destroyed)
            {
                _destroyed = true;
                _depthAttachment?.Target.Dispose();
                DestroySwapchainFramebuffers();
            }
        }
    }
}
