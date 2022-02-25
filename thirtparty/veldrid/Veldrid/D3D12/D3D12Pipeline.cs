using Vortice.Direct3D12;
using System.Diagnostics;
using System;
using Vortice.Mathematics;

namespace Veldrid.D3D12
{
    internal class D3D12Pipeline : Pipeline
    {
        private string _name;
        private bool _disposed;

        public ID3D12BlendState BlendState { get; }
        public Color4 BlendFactor { get; }
        public ID3D12DepthStencilState DepthStencilState { get; }
        public uint StencilReference { get; }
        public ID3D12RasterizerState RasterizerState { get; }
        public Vortice.Direct3D.PrimitiveTopology PrimitiveTopology { get; }
        public ID3D12InputLayout InputLayout { get; }
        public ID3D12VertexShader VertexShader { get; }
        public ID3D12GeometryShader GeometryShader { get; } // May be null.
        public ID3D12HullShader HullShader { get; } // May be null.
        public ID3D12DomainShader DomainShader { get; } // May be null.
        public ID3D12PixelShader PixelShader { get; }
        public ID3D12ComputeShader ComputeShader { get; }
        public new D3D12ResourceLayout[] ResourceLayouts { get; }
        public int[] VertexStrides { get; }

        public override bool IsComputePipeline { get; }

        public D3D12Pipeline(D3D11ResourceCache cache, ref GraphicsPipelineDescription description)
            : base(ref description)
        {
            byte[] vsBytecode = null;
            Shader[] stages = description.ShaderSet.Shaders;
            for (int i = 0; i < description.ShaderSet.Shaders.Length; i++)
            {
                if (stages[i].Stage == ShaderStages.Vertex)
                {
                    D3D12Shader D3D12VertexShader = ((D3D12Shader)stages[i]);
                    VertexShader = (ID3D12VertexShader)d3d11VertexShader.DeviceShader;
                    vsBytecode = D3D12VertexShader.Bytecode;
                }
                if (stages[i].Stage == ShaderStages.Geometry)
                {
                    GeometryShader = (ID3D12GeometryShader)((D3D12Shader)stages[i]).DeviceShader;
                }
                if (stages[i].Stage == ShaderStages.TessellationControl)
                {
                    HullShader = (ID3D12HullShader)((D3D12Shader)stages[i]).DeviceShader;
                }
                if (stages[i].Stage == ShaderStages.TessellationEvaluation)
                {
                    DomainShader = (ID3D12DomainShader)((D3D12Shader)stages[i]).DeviceShader;
                }
                if (stages[i].Stage == ShaderStages.Fragment)
                {
                    PixelShader = (ID3D12PixelShader)((D3D12Shader)stages[i]).DeviceShader;
                }
                if (stages[i].Stage == ShaderStages.Compute)
                {
                    ComputeShader = (ID3D12ComputeShader)((D3D12Shader)stages[i]).DeviceShader;
                }
            }

            cache.GetPipelineResources(
                ref description.BlendState,
                ref description.DepthStencilState,
                ref description.RasterizerState,
                description.Outputs.SampleCount != TextureSampleCount.Count1,
                description.ShaderSet.VertexLayouts,
                vsBytecode,
                out ID3D12BlendState blendState,
                out ID3D12DepthStencilState depthStencilState,
                out ID3D12RasterizerState rasterizerState,
                out ID3D12InputLayout inputLayout);

            BlendState = blendState;
            BlendFactor = new Color4(description.BlendState.BlendFactor.ToVector4());
            DepthStencilState = depthStencilState;
            StencilReference = description.DepthStencilState.StencilReference;
            RasterizerState = rasterizerState;
            PrimitiveTopology = D3D12Formats.VdToD3D11PrimitiveTopology(description.PrimitiveTopology);

            ResourceLayout[] genericLayouts = description.ResourceLayouts;
            ResourceLayouts = new D3D12ResourceLayout[genericLayouts.Length];
            for (int i = 0; i < ResourceLayouts.Length; i++)
            {
                ResourceLayouts[i] = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(genericLayouts[i]);
            }

            Debug.Assert(vsBytecode != null || ComputeShader != null);
            if (vsBytecode != null && description.ShaderSet.VertexLayouts.Length > 0)
            {
                InputLayout = inputLayout;
                int numVertexBuffers = description.ShaderSet.VertexLayouts.Length;
                VertexStrides = new int[numVertexBuffers];
                for (int i = 0; i < numVertexBuffers; i++)
                {
                    VertexStrides[i] = (int)description.ShaderSet.VertexLayouts[i].Stride;
                }
            }
            else
            {
                VertexStrides = Array.Empty<int>();
            }
        }

        public D3D12Pipeline(D3D11ResourceCache cache, ref ComputePipelineDescription description)
            : base(ref description)
        {
            IsComputePipeline = true;
            ComputeShader = (ID3D12ComputeShader)((D3D12Shader)description.ComputeShader).DeviceShader;
            ResourceLayout[] genericLayouts = description.ResourceLayouts;
            ResourceLayouts = new D3D12ResourceLayout[genericLayouts.Length];
            for (int i = 0; i < ResourceLayouts.Length; i++)
            {
                ResourceLayouts[i] = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(genericLayouts[i]);
            }
        }

        public override string Name
        {
            get => _name;
            set => _name = value;
        }

        public override bool IsDisposed => _disposed;

        public override void Dispose()
        {
            _disposed = true;
        }
    }
}
