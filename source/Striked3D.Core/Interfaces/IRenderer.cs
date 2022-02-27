using Striked3D.Core;
using System;
using Veldrid;

namespace Striked3D.Graphics
{
    /// <summary>
    /// The interface which help to access rendering methods
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// The default 3d material layout
        /// </summary>
        public ResourceLayout Material3DLayout { get; }

        /// <summary>
        /// The default 2d material layout
        /// </summary>
        public ResourceLayout Material2DLayout { get; }

        /// <summary>
        /// The default layout for transforms
        /// </summary>
        public ResourceLayout TransformLayout { get; }

        /// <summary>
        /// The default layout for font atlases
        /// </summary>
        public ResourceLayout FontAtlasLayout { get; }

        /// <summary>
        /// The default layout for font atlases
        /// </summary>
        public ResourceLayout MaterialBitmapTexture { get; }

        /// <summary>
        /// A default index buffer (for rectangles, 6 indicies)
        /// </summary>
        public DeviceBuffer IndexDefaultBuffer { get; }

        /// <summary>
        /// A default texture set (with a single empty texture 4x4)
        /// </summary>
        public ResourceSet DefaultTextureSet { get; }

        /// <summary>
        /// The default system 2d material
        /// </summary>
        public IMaterial Default2DMaterial { get; }

        /// <summary>
        /// The default system 3d material
        /// </summary>
        public IMaterial Default3DMaterial { get; }

        /// <summary>
        /// A simple linear sampler for shaders
        /// </summary>
        public Sampler LinearSampler { get; }

        public void UpdateTexture(
           Texture texture,
           IntPtr source,
           uint sizeInBytes,
           uint x, uint y, uint z,
           uint width, uint height, uint depth,
           uint mipLevel, uint arrayLayer);

        public unsafe void UpdateBuffer<T>(
           DeviceBuffer buffer,
           uint bufferOffsetInBytes,
           T source) where T : unmanaged;

        public void UpdateBuffer<T>(
           DeviceBuffer buffer,
           uint bufferOffsetInBytes,
           T[] source) where T : unmanaged;

        public Shader[] CreateShader(string vertexCode, string fragmentCode);
        public Veldrid.Pipeline CreatePipeline(GraphicsPipelineDescription desc);

        public void SetViewport(IViewport viewport);
        public void SetMaterial(IMaterial mat);

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
