using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Servers.Rendering.Vulkan
{
    public struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public bool IsComplete()
        {
            return GraphicsFamily.HasValue && PresentFamily.HasValue;
        }
    }
}
