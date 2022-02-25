using System.Collections.Generic;
using static Veldrid.Vk.VulkanUtil;
using System;
using System.Diagnostics;

namespace Veldrid.Vk
{
    internal unsafe class VkFramebuffer : VkFramebufferBase
    {
        public const uint SubpassExternal = (~0U);

        private readonly VkGraphicsDevice _gd;
        private readonly Silk.NET.Vulkan.Vk _vk;
        private readonly Silk.NET.Vulkan.Framebuffer _deviceFramebuffer;
        private readonly Silk.NET.Vulkan.RenderPass _renderPassNoClearLoad;
        private readonly Silk.NET.Vulkan.RenderPass _renderPassNoClear;
        private readonly Silk.NET.Vulkan.RenderPass _renderPassClear;
        private readonly List<Silk.NET.Vulkan.ImageView> _attachmentViews = new List<Silk.NET.Vulkan.ImageView>();
        private bool _destroyed;
        private string _name;

        public override Silk.NET.Vulkan.Framebuffer CurrentFramebuffer => _deviceFramebuffer;
        public override Silk.NET.Vulkan.RenderPass RenderPassNoClear_Init => _renderPassNoClear;
        public override Silk.NET.Vulkan.RenderPass RenderPassNoClear_Load => _renderPassNoClearLoad;
        public override Silk.NET.Vulkan.RenderPass RenderPassClear => _renderPassClear;

        public override uint RenderableWidth => Width;
        public override uint RenderableHeight => Height;

        public override uint AttachmentCount { get; }

        public override bool IsDisposed => _destroyed;

        public VkFramebuffer(VkGraphicsDevice gd, ref FramebufferDescription description, bool isPresented)
            : base(description.DepthTarget, description.ColorTargets)
        {
            _gd = gd;
            _vk = gd.vk;

            Silk.NET.Vulkan.RenderPassCreateInfo renderPassCI = new Silk.NET.Vulkan.RenderPassCreateInfo();
            renderPassCI.SType = Silk.NET.Vulkan.StructureType.RenderPassCreateInfo;

            StackList<Silk.NET.Vulkan.AttachmentDescription> attachments = new StackList<Silk.NET.Vulkan.AttachmentDescription>();

            uint colorAttachmentCount = (uint)ColorTargets.Count;
            StackList<Silk.NET.Vulkan.AttachmentReference> colorAttachmentRefs = new StackList<Silk.NET.Vulkan.AttachmentReference>();
            for (int i = 0; i < colorAttachmentCount; i++)
            {
                VkTexture vkColorTex = Util.AssertSubtype<Texture, VkTexture>(ColorTargets[i].Target);
                Silk.NET.Vulkan.AttachmentDescription colorAttachmentDesc = new Silk.NET.Vulkan.AttachmentDescription();
                colorAttachmentDesc.Format = vkColorTex.VkFormat;
                colorAttachmentDesc.Samples = vkColorTex.VkSampleCount;
                colorAttachmentDesc.LoadOp = Silk.NET.Vulkan.AttachmentLoadOp.Load;
                colorAttachmentDesc.StoreOp = Silk.NET.Vulkan.AttachmentStoreOp.Store;
                colorAttachmentDesc.StencilLoadOp = Silk.NET.Vulkan.AttachmentLoadOp.DontCare;
                colorAttachmentDesc.StencilStoreOp = Silk.NET.Vulkan.AttachmentStoreOp.DontCare;
                colorAttachmentDesc.InitialLayout = isPresented
                    ? Silk.NET.Vulkan.ImageLayout.PresentSrcKhr
                    : ((vkColorTex.Usage & TextureUsage.Sampled) != 0)
                        ? Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal
                        : Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal;
                colorAttachmentDesc.FinalLayout = Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal;
                attachments.Add(colorAttachmentDesc);

                Silk.NET.Vulkan.AttachmentReference colorAttachmentRef = new Silk.NET.Vulkan.AttachmentReference();
                colorAttachmentRef.Attachment = (uint)i;
                colorAttachmentRef.Layout = Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal;
                colorAttachmentRefs.Add(colorAttachmentRef);
            }

            Silk.NET.Vulkan.AttachmentDescription depthAttachmentDesc = new Silk.NET.Vulkan.AttachmentDescription();
            Silk.NET.Vulkan.AttachmentReference depthAttachmentRef = new Silk.NET.Vulkan.AttachmentReference();
            if (DepthTarget != null)
            {
                VkTexture vkDepthTex = Util.AssertSubtype<Texture, VkTexture>(DepthTarget.Value.Target);
                bool hasStencil = FormatHelpers.IsStencilFormat(vkDepthTex.Format);
                depthAttachmentDesc.Format = vkDepthTex.VkFormat;
                depthAttachmentDesc.Samples = vkDepthTex.VkSampleCount;
                depthAttachmentDesc.LoadOp = Silk.NET.Vulkan.AttachmentLoadOp.Load;
                depthAttachmentDesc.StoreOp = Silk.NET.Vulkan.AttachmentStoreOp.Store;
                depthAttachmentDesc.StencilLoadOp = Silk.NET.Vulkan.AttachmentLoadOp.DontCare;
                depthAttachmentDesc.StencilStoreOp = hasStencil
                    ? Silk.NET.Vulkan.AttachmentStoreOp.Store
                    : Silk.NET.Vulkan.AttachmentStoreOp.DontCare;
                depthAttachmentDesc.InitialLayout = ((vkDepthTex.Usage & TextureUsage.Sampled) != 0)
                    ? Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal
                    : Silk.NET.Vulkan.ImageLayout.DepthStencilAttachmentOptimal;
                depthAttachmentDesc.FinalLayout = Silk.NET.Vulkan.ImageLayout.DepthStencilAttachmentOptimal;

                depthAttachmentRef.Attachment = (uint)description.ColorTargets.Length;
                depthAttachmentRef.Layout = Silk.NET.Vulkan.ImageLayout.DepthStencilAttachmentOptimal;
            }

            Silk.NET.Vulkan.SubpassDescription subpass = new Silk.NET.Vulkan.SubpassDescription();
            subpass.PipelineBindPoint = Silk.NET.Vulkan.PipelineBindPoint.Graphics;
            if (ColorTargets.Count > 0)
            {
                subpass.ColorAttachmentCount = colorAttachmentCount;
                subpass.PColorAttachments = (Silk.NET.Vulkan.AttachmentReference*)colorAttachmentRefs.Data;
            }

            if (DepthTarget != null)
            {
                subpass.PDepthStencilAttachment = &depthAttachmentRef;
                attachments.Add(depthAttachmentDesc);
            }

            Silk.NET.Vulkan.SubpassDependency subpassDependency = new Silk.NET.Vulkan.SubpassDependency();
            subpassDependency.SrcSubpass = SubpassExternal;
            subpassDependency.SrcStageMask = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
            subpassDependency.DstStageMask = Silk.NET.Vulkan.PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
            subpassDependency.DstAccessMask = Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentReadBit |  Silk.NET.Vulkan.AccessFlags.AccessColorAttachmentWriteBit;

            renderPassCI.AttachmentCount = attachments.Count;
            renderPassCI.PAttachments = (Silk.NET.Vulkan.AttachmentDescription*)attachments.Data;
            renderPassCI.SubpassCount = 1;
            renderPassCI.PSubpasses = &subpass;
            renderPassCI.DependencyCount = 1;
            renderPassCI.PDependencies = &subpassDependency;

            var creationResult = _vk.CreateRenderPass(_gd.Device, &renderPassCI, null, out _renderPassNoClear);
            CheckResult(creationResult);

            for (int i = 0; i < colorAttachmentCount; i++)
            {
                attachments[i].LoadOp = Silk.NET.Vulkan.AttachmentLoadOp.Load;
                attachments[i].InitialLayout = Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal;
            }
            if (DepthTarget != null)
            {
                attachments[attachments.Count - 1].LoadOp = Silk.NET.Vulkan.AttachmentLoadOp.Load;
                attachments[attachments.Count - 1].InitialLayout = Silk.NET.Vulkan.ImageLayout.DepthStencilAttachmentOptimal;
                bool hasStencil = FormatHelpers.IsStencilFormat(DepthTarget.Value.Target.Format);
                if (hasStencil)
                {
                    attachments[attachments.Count - 1].StencilLoadOp = Silk.NET.Vulkan.AttachmentLoadOp.Load;
                }

            }
            creationResult = _vk.CreateRenderPass(_gd.Device, &renderPassCI, null, out _renderPassNoClearLoad);
            CheckResult(creationResult);


            // Load version

            if (DepthTarget != null)
            {
                attachments[attachments.Count - 1].LoadOp = Silk.NET.Vulkan.AttachmentLoadOp.Clear;
                attachments[attachments.Count - 1].InitialLayout = Silk.NET.Vulkan.ImageLayout.Undefined;
                bool hasStencil = FormatHelpers.IsStencilFormat(DepthTarget.Value.Target.Format);
                if (hasStencil)
                {
                    attachments[attachments.Count - 1].StencilLoadOp = Silk.NET.Vulkan.AttachmentLoadOp.Clear;
                }
            }

            for (int i = 0; i < colorAttachmentCount; i++)
            {
                attachments[i].LoadOp = Silk.NET.Vulkan.AttachmentLoadOp.Clear;
                attachments[i].InitialLayout = Silk.NET.Vulkan.ImageLayout.Undefined;
            }

            creationResult = _vk.CreateRenderPass(_gd.Device, &renderPassCI, null, out _renderPassClear);
            CheckResult(creationResult);

            Silk.NET.Vulkan.FramebufferCreateInfo fbCI = new Silk.NET.Vulkan.FramebufferCreateInfo();
            uint fbAttachmentsCount = (uint)description.ColorTargets.Length;
            if (description.DepthTarget != null)
            {
                fbAttachmentsCount += 1;
            }

            Silk.NET.Vulkan.ImageView* fbAttachments = stackalloc Silk.NET.Vulkan.ImageView[(int)fbAttachmentsCount];
            for (int i = 0; i < colorAttachmentCount; i++)
            {
                VkTexture vkColorTarget = Util.AssertSubtype<Texture, VkTexture>(description.ColorTargets[i].Target);
                Silk.NET.Vulkan.ImageViewCreateInfo imageViewCI = new Silk.NET.Vulkan.ImageViewCreateInfo();
                imageViewCI.Image = vkColorTarget.OptimalDeviceImage;
                imageViewCI.Format = vkColorTarget.VkFormat;
                imageViewCI.ViewType = Silk.NET.Vulkan.ImageViewType.ImageViewType2D;
                imageViewCI.SubresourceRange = new Silk.NET.Vulkan.ImageSubresourceRange(
                    Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit,
                    description.ColorTargets[i].MipLevel,
                    1,
                    description.ColorTargets[i].ArrayLayer,
                    1);
                imageViewCI.SType = Silk.NET.Vulkan.StructureType.ImageViewCreateInfo;
                Silk.NET.Vulkan.ImageView* dest = (fbAttachments + i);
                Silk.NET.Vulkan.Result result = _vk.CreateImageView(_gd.Device, &imageViewCI, null, dest);
                CheckResult(result);
                _attachmentViews.Add(*dest);
            }

            // Depth
            if (description.DepthTarget != null)
            {
                VkTexture vkDepthTarget = Util.AssertSubtype<Texture, VkTexture>(description.DepthTarget.Value.Target);
                bool hasStencil = FormatHelpers.IsStencilFormat(vkDepthTarget.Format);
                Silk.NET.Vulkan.ImageViewCreateInfo depthViewCI = new Silk.NET.Vulkan.ImageViewCreateInfo();
                depthViewCI.Image = vkDepthTarget.OptimalDeviceImage;
                depthViewCI.Format = vkDepthTarget.VkFormat;
                depthViewCI.ViewType = description.DepthTarget.Value.Target.ArrayLayers == 1
                    ? Silk.NET.Vulkan.ImageViewType.ImageViewType2D
                    : Silk.NET.Vulkan.ImageViewType.ImageViewType2DArray;

                depthViewCI.SubresourceRange = new Silk.NET.Vulkan.ImageSubresourceRange(
                    hasStencil ? Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit | Silk.NET.Vulkan.ImageAspectFlags.ImageAspectStencilBit : Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit,
                    description.DepthTarget.Value.MipLevel,
                    1,
                    description.DepthTarget.Value.ArrayLayer,
                    1);

                depthViewCI.SType = Silk.NET.Vulkan.StructureType.ImageViewCreateInfo;

                Silk.NET.Vulkan.ImageView* dest = (fbAttachments + (fbAttachmentsCount - 1));
                Silk.NET.Vulkan.Result result = _vk.CreateImageView(_gd.Device, &depthViewCI, null, dest);
                CheckResult(result);
                _attachmentViews.Add(*dest);
            }

            Texture dimTex;
            uint mipLevel;
            if (ColorTargets.Count > 0)
            {
                dimTex = ColorTargets[0].Target;
                mipLevel = ColorTargets[0].MipLevel;
            }
            else
            {
                Debug.Assert(DepthTarget != null);
                dimTex = DepthTarget.Value.Target;
                mipLevel = DepthTarget.Value.MipLevel;
            }

            Util.GetMipDimensions(
                dimTex,
                mipLevel,
                out uint mipWidth,
                out uint mipHeight,
                out _);

            fbCI.Width = mipWidth;
            fbCI.Height = mipHeight;

            fbCI.AttachmentCount = fbAttachmentsCount;
            fbCI.PAttachments = fbAttachments;
            fbCI.Layers = 1;
            fbCI.RenderPass = _renderPassNoClear;
            fbCI.SType = Silk.NET.Vulkan.StructureType.FramebufferCreateInfo;

            creationResult = _vk.CreateFramebuffer(_gd.Device, &fbCI, null, out _deviceFramebuffer);
            CheckResult(creationResult);

            if (DepthTarget != null)
            {
                AttachmentCount += 1;
            }
            AttachmentCount += (uint)ColorTargets.Count;
        }

        public override void TransitionToIntermediateLayout(Silk.NET.Vulkan.CommandBuffer cb)
        {
            for (int i = 0; i < ColorTargets.Count; i++)
            {
                FramebufferAttachment ca = ColorTargets[i];
                VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
                vkTex.SetImageLayout(ca.MipLevel, ca.ArrayLayer, Silk.NET.Vulkan.ImageLayout.ColorAttachmentOptimal);
            }
            if (DepthTarget != null)
            {
                VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(DepthTarget.Value.Target);
                vkTex.SetImageLayout(
                    DepthTarget.Value.MipLevel,
                    DepthTarget.Value.ArrayLayer,
                    Silk.NET.Vulkan.ImageLayout.DepthStencilAttachmentOptimal);
            }
        }

        public override void TransitionToFinalLayout(Silk.NET.Vulkan.CommandBuffer cb)
        {
            for (int i = 0; i < ColorTargets.Count; i++)
            {
                FramebufferAttachment ca = ColorTargets[i];
                VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
                if ((vkTex.Usage & TextureUsage.Sampled) != 0)
                {
                    vkTex.TransitionImageLayout(
                        cb,
                        ca.MipLevel, 1,
                        ca.ArrayLayer, 1,
                        Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal);
                }
            }
            if (DepthTarget != null)
            {
                VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(DepthTarget.Value.Target);
                if ((vkTex.Usage & TextureUsage.Sampled) != 0)
                {
                    vkTex.TransitionImageLayout(
                        cb,
                        DepthTarget.Value.MipLevel, 1,
                        DepthTarget.Value.ArrayLayer, 1,
                        Silk.NET.Vulkan.ImageLayout.ShaderReadOnlyOptimal);
                }
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
                _vk.DestroyFramebuffer(_gd.Device, _deviceFramebuffer, null);
                _vk.DestroyRenderPass(_gd.Device, _renderPassNoClear, null);
                _vk.DestroyRenderPass(_gd.Device, _renderPassNoClearLoad, null);
                _vk.DestroyRenderPass(_gd.Device, _renderPassClear, null);

                foreach (var view in _attachmentViews)
                {
                    _vk.DestroyImageView(_gd.Device, view, null);
                }

                _destroyed = true;
            }
        }
    }
}
