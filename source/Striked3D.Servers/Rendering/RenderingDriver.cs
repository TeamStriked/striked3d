using Striked3D.Core;
using Striked3D.Servers.Rendering.Vulkan;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Servers.Rendering
{
    public abstract class RenderingDriver
    {
        public Window _window { get; set; }

        protected bool _framebufferResized = false;

        public LogicalDevice logicalDevice;
        public RenderingInstance instance;
        public PhysicalDevice physicalDevice;
        public Swapchain swapChain;
        public FrameBuffers frameBuffers;
        public RenderPass renderPass;
        public CommandBuffers commandBuffers;
        public Debugger debugger;
        public CommandPool commandPool;
        public RenderQueue queue;

        public abstract void Initialize(Window win);
        public abstract void Draw(double delta);
        public abstract void Destroy();

        protected bool EnableValidationLayers { get; set; } = true;
    }
}
