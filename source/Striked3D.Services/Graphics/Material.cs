using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Core.Pipeline;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.SPIRV;
using PipelineVeldrid = Veldrid.Pipeline;

namespace Striked3D.Resources
{
    public abstract class Material : Resource, IMaterial
    {
        public abstract string VertexCode { get; }
        public abstract void BeforeDraw(IRenderer renderer);
        public abstract string FragmentCode { get;  }

        protected PipelineBuilder builder = new ();
        protected bool _isDirty = true;

        public bool isDirty => _isDirty;

        public Pipeline Pipeline
        {
            get
            {
                return this.builder?.Pipeline;
            }
        }

        public Material()
        {
            this.cullMode = FaceCullMode.Back;
            this.constantSize = 0;
            this.frontFace = FrontFace.Clockwise;
            this.mode = PrimitiveTopology.TriangleStrip;
            this.depthStencilComparsion = ComparisonKind.LessEqual;
            this.blendState = BlendStateDescription.SingleOverrideBlend;

            this.scissorTestEnabled = false;
            this.depthClip = true;
            this. testEnabled = true;
            this.writeEnable = true;
        }

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
    }
}
