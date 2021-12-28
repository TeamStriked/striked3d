using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using VKRenderPass = Silk.NET.Vulkan.RenderPass;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public class RenderPass
    {
        private VKRenderPass _renderPass;

        private RenderingInstance _instance;
        private Swapchain _swapchain;
        public RenderPass(RenderingInstance instance, Swapchain swapchain)
        {
            this._instance = instance;
            this._swapchain = swapchain;
        }

        public Swapchain Swapchain { get { return _swapchain; } }
        public VKRenderPass NativeHandle { get { return _renderPass; } }

        public unsafe void Destroy()
        {
            this._instance.Api.DestroyRenderPass(_swapchain.Device.NativeHandle, _renderPass, null);
        }

        public unsafe void Instanciate()
        {
           

            var colorAttachment = new AttachmentDescription
            {
                Format = this._swapchain.SwapchaiImageFormat,
                Samples = this._swapchain.Device.MsaaLevel,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.ColorAttachmentOptimal
            };

            var depthAttachment = new AttachmentDescription
            {
                Format = this._swapchain.DepthFormat,
                Samples = this._swapchain.Device.MsaaLevel,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
            };

            var colorAttachmentResolve = new AttachmentDescription
            {
                Format = this._swapchain.SwapchaiImageFormat,
                Samples = SampleCountFlags.SampleCount1Bit,
                LoadOp = AttachmentLoadOp.DontCare,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr
            };


            Span<AttachmentDescription> attachments = stackalloc AttachmentDescription[3];
            attachments[0] = colorAttachment;
            attachments[1] = depthAttachment;
            attachments[2] = colorAttachmentResolve;

            var colorAttachmentRef = new AttachmentReference
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal
            };

            var depthAttachmentRef = new AttachmentReference
            {
                Attachment = 1,
                Layout = ImageLayout.DepthStencilAttachmentOptimal
            };

            var colorAttachmentResolveRef = new AttachmentReference
            {
                Attachment = 2,
                Layout = ImageLayout.ColorAttachmentOptimal
            };

            var subpass = new SubpassDescription
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef,
                PResolveAttachments = &colorAttachmentResolveRef,
                PDepthStencilAttachment = &depthAttachmentRef,
            };

            var dependency = new SubpassDependency
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
                DstAccessMask = AccessFlags.AccessColorAttachmentReadBit | AccessFlags.AccessColorAttachmentWriteBit
            };

            var renderPassInfo = new RenderPassCreateInfo
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachments.Length,
                PAttachments = (AttachmentDescription*)Unsafe.AsPointer(ref attachments[0]),
                SubpassCount = 1,
                PSubpasses = &subpass,
                DependencyCount = 1,
                PDependencies = &dependency
            };

            fixed (VKRenderPass* renderPass = &_renderPass)
            {
                if (this._instance.Api.CreateRenderPass(this._swapchain.Device.NativeHandle, &renderPassInfo, null, renderPass) != Result.Success)
                {
                    throw new Exception("failed to create render pass!");
                }
            }
        }


    }
}
