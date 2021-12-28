using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities { get; set; }
        public SurfaceFormatKHR[] Formats { get; set; }
        public PresentModeKHR[] PresentModes { get; set; }
    }

}
