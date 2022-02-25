using System.Collections.Generic;

namespace Veldrid.Vk
{
    internal abstract class VkFramebufferBase : Framebuffer
    {
        public VkFramebufferBase(
            FramebufferAttachmentDescription? depthTexture,
            IReadOnlyList<FramebufferAttachmentDescription> colorTextures)
            : base(depthTexture, colorTextures)
        {
            RefCount = new ResourceRefCount(DisposeCore);
        }

        public VkFramebufferBase()
        {
            RefCount = new ResourceRefCount(DisposeCore);
        }

        public ResourceRefCount RefCount { get; }

        public abstract uint RenderableWidth { get; }
        public abstract uint RenderableHeight { get; }

        public override void Dispose()
        {
            RefCount.Decrement();
        }

        protected abstract void DisposeCore();

        public abstract Silk.NET.Vulkan.Framebuffer CurrentFramebuffer { get; }
        public abstract Silk.NET.Vulkan.RenderPass RenderPassNoClear_Init { get; }
        public abstract Silk.NET.Vulkan.RenderPass RenderPassNoClear_Load { get; }
        public abstract Silk.NET.Vulkan.RenderPass RenderPassClear { get; }
        public abstract uint AttachmentCount { get; }
        public abstract void TransitionToIntermediateLayout(Silk.NET.Vulkan.CommandBuffer cb);
        public abstract void TransitionToFinalLayout(Silk.NET.Vulkan.CommandBuffer cb);
    }
}
