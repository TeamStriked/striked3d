using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class FrameBuffers
    {
        private RenderingInstance _instance;
        private RenderPass _renderPass;

        private Framebuffer[] _swapchainFramebuffers;

        public Framebuffer[] SwapchainFrameBuffers
        {
            get { return _swapchainFramebuffers; }  
        }

        public unsafe void Destroy()
        {
            foreach (var framebuffer in _swapchainFramebuffers)
            {
                this._instance.Api.DestroyFramebuffer(
                    this._renderPass.Swapchain.Device.NativeHandle,
                    framebuffer, null);
            }
        }

        public FrameBuffers(RenderingInstance _instance, RenderPass _renderPass)
        {
            this._instance = _instance;
            this._renderPass = _renderPass;

        }
        public unsafe void Instanciate()
        {
            _swapchainFramebuffers = new Framebuffer[this._renderPass.Swapchain.SwapchainImageViews.Length];

            for (var i = 0; i < this._renderPass.Swapchain.SwapchainImageViews.Length; i++)
            {
                var colorTexture = this._renderPass.Swapchain.ColorTexture;
                var depthTexture = this._renderPass.Swapchain.DepthTexture;
                var imageView = this._renderPass.Swapchain.SwapchainImageViews[i];

                Span<ImageView> views = stackalloc ImageView[3];
                views[0] = colorTexture.NativeViewHandle;
                views[1] = depthTexture.NativeViewHandle;
                views[2] = imageView;

                var framebufferInfo = new FramebufferCreateInfo
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = this._renderPass.NativeHandle,
                    AttachmentCount = (uint) views.Length,
                    PAttachments =  (ImageView*)Unsafe.AsPointer(ref views[0]),
                    Width = this._renderPass.Swapchain.SwapchainExtent.Width,
                    Height = this._renderPass.Swapchain.SwapchainExtent.Height,
                    Layers = 1
                };

                var framebuffer = new Framebuffer();
                if (this._instance.Api.CreateFramebuffer(this._renderPass.Swapchain.Device.NativeHandle, 
                    &framebufferInfo, null, &framebuffer) != Result.Success)
                {
                    throw new Exception("failed to create framebuffer!");
                }

                _swapchainFramebuffers[i] = framebuffer;
            }
        }


    }
}
