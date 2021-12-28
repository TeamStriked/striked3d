using Silk.NET.Vulkan;
using Striked3D.Servers.Rendering.Vulkan;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Servers.Rendering
{
    public struct MaterialParameters
    {
        public CullModeFlags cullMode { get; set; }
        public FrontFace frontFace { get; set; }
        public LogicOp logic { get; set; }
        public PolygonMode polygonMode { get; set; }
        public bool blendEnabled { get; set; }
        public CompareOp depthCompareOp { get; set; }

        public Dictionary<ShaderStageFlags, string> shaders = new Dictionary<ShaderStageFlags, string>();
        public MaterialParameters()
        {
            polygonMode = PolygonMode.Fill;
            logic = LogicOp.NoOp;
            frontFace = FrontFace.Clockwise;
            cullMode = CullModeFlags.CullModeBackBit;
            blendEnabled = true;
            depthCompareOp = CompareOp.LessOrEqual;
        }
    }

    public struct Material
    {
        public bool isDirty = true;
        public MaterialParameters parameters { get; set; }
        public GraphicsPipeline pipeline;
    }
}
