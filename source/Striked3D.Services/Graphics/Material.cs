using Striked3D.Core;
using Striked3D.Core.Pipeline;
using Striked3D.Graphics;
using Veldrid;

namespace Striked3D.Resources
{
    public abstract class Material : Resource, IMaterial
    {
        public abstract string VertexCode { get; }
        public abstract void BeforeDraw(IRenderer renderer);
        public abstract string FragmentCode { get; }

        protected PipelineBuilder builder = new();
        protected bool _isDirty = true;

        public bool isDirty => _isDirty;

        public Pipeline Pipeline => builder?.Pipeline;

        public Material()
        {
            cullMode = FaceCullMode.Back;
            constantSize = 0;
            frontFace = FrontFace.Clockwise;
            mode = PrimitiveTopology.TriangleStrip;
            depthStencilComparsion = ComparisonKind.LessEqual;
            blendState = BlendStateDescription.SingleOverrideBlend;

            scissorTestEnabled = false;
            depthClip = true;
            testEnabled = true;
            writeEnable = true;
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
