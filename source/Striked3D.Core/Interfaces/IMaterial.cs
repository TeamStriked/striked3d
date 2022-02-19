using Striked3D.Core.Reference;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using PipelineVeldrid = Veldrid.Pipeline;

namespace Striked3D.Core
{
    public interface IMaterial : IResource
    {
        public abstract string VertexCode { get; }
        public abstract string FragmentCode { get; }

        public PipelineVeldrid Pipeline { get; }

        public FaceCullMode cullMode { get; set; }
        public uint constantSize { get; set; }
        public FrontFace frontFace { get; set; }
        public PrimitiveTopology mode { get; set; }
        public ComparisonKind depthStencilComparsion { get; set; }
        public BlendStateDescription blendState { get; set; }

        public bool scissorTestEnabled { get; set; }
        public bool depthClip { get; set; }
        public bool testEnabled { get; set; }
        public bool writeEnable { get; set; }

        public bool isDirty { get; }
    }
}
