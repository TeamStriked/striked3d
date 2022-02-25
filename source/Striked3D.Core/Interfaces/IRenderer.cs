using System;
using Veldrid;

namespace Striked3D.Core
{
    public interface IRenderer
    {
        public ResourceLayout Material3DLayout { get; }
        public ResourceLayout Material2DLayout { get; }
        public ResourceLayout TransformLayout { get; }
        public ResourceLayout FontAtlasLayout { get; }
        public DeviceBuffer indexDefaultBuffer { get; }
        public ResourceSet DefaultTextureSet { get; }
        public IMaterial Default2DMaterial { get; }
        public IMaterial Default3DMaterial { get; }

        public Sampler LinearSampler { get; }

        public void UpdateTexture(
       Texture texture,
       IntPtr source,
       uint sizeInBytes,
       uint x, uint y, uint z,
       uint width, uint height, uint depth,
       uint mipLevel, uint arrayLayer);

        public Shader[] CreateShader(string vertexCode, string fragmentCode);
        public Veldrid.Pipeline CreatePipeline(GraphicsPipelineDescription desc);

        public void SetViewport(IViewport viewport);
        public void SetMaterial(IMaterial mat);

        public unsafe void UpdateBuffer<T>(
           DeviceBuffer buffer,
           uint bufferOffsetInBytes,
           T source) where T : unmanaged;

        public void UpdateBuffer<T>(
           DeviceBuffer buffer,
           uint bufferOffsetInBytes,
           T[] source) where T : unmanaged;

        public DeviceBuffer CreateBuffer(BufferDescription desc);

        public ResourceSet CreateResourceSet(ResourceSetDescription description);
        public TextureView CreateTextureView(Texture target);
        public Texture CreateTexture(TextureDescription desc);

        public void SetResourceSets(ResourceSet[] sets);
        public void BindBuffers(DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer = null);
        public void PushConstant<T>(
       T source) where T : unmanaged;
        public void DrawInstanced(int vertexAmount, int instances = 1);
        public void DrawIndexInstanced(int indiceLength, int instances = 1);
    }
}
