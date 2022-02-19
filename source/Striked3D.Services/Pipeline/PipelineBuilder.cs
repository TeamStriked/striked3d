using PipelineVeldrid = Veldrid.Pipeline;
using Veldrid;

namespace Striked3D.Core.Pipeline
{
    public class PipelineBuilder
    {
        private PipelineVeldrid _pipeline;
        private Shader[] _shaders;

        private bool isDirty = true;

        public ResourceLayout[] layouts;
        public VertexLayoutDescription[] descriptions;

        public void SetResourceLayouts(ResourceLayout[] layouts)
        {
            this.layouts = layouts;
        }

        public PipelineVeldrid Pipeline
        {
            get { return _pipeline; }   
        }

        public void SetVertexLayout(VertexLayoutDescription[] layout)
        {
            this.descriptions = layout;
        }

        public void Generate(IRenderer renderer, IMaterial material)
        {
            if (!isDirty )
            {
                return;
            }

            this.Dispose();

            foreach(var layout in layouts)
            {
                if(layout == null)
                {
                    return;
                }
            }

            _shaders = renderer.CreateShader(material.VertexCode, material.FragmentCode);

            // Create pipeline
            var pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = material.blendState,
                DepthStencilState = new
                (
                    material.testEnabled,
                    material.writeEnable,
                    material.depthStencilComparsion
                ),
                RasterizerState = new
                (
                    material.cullMode,
                    PolygonFillMode.Solid,
                    material.frontFace,
                    material.depthClip,
                    material.scissorTestEnabled
                ),
                PrimitiveTopology = material.mode,
                ResourceLayouts = (layouts == null || layouts.Length <= 0) ? System.Array.Empty<ResourceLayout>() : layouts,
                ShaderSet = new
                (
                    (descriptions == null || descriptions.Length <= 0) ? System.Array.Empty<VertexLayoutDescription>() : descriptions,
                    _shaders
                ),
            };

             if (material.constantSize > 0)
               pipelineDescription.PushConstantDescription = new PushConstantDescription(material.constantSize);

              _pipeline = renderer.CreatePipeline(pipelineDescription);

             this.isDirty = false;
        }

        public void Dispose()
        {
            this._pipeline?.Dispose();
            if(_shaders != null)
            {
                foreach (var child in _shaders)
                {
                    child?.Dispose();
                }
            }
            this._pipeline = null;
        }
    }
}
