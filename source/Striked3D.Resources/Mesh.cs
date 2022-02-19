using Striked3D.Core;
using Striked3D.Graphics;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using Veldrid;

namespace Striked3D.Resources
{

    internal struct DrawableMeshCache
    {
        public DeviceBuffer deviceBufferVertex { get; set; }
        public DeviceBuffer deviceBufferIndex { get; set; }

        public Dictionary<int, MeshSurfaceLodBlock> LodBlocks { get; set; }

        public void Dispose()
        {
            LodBlocks = null;
            deviceBufferVertex?.Dispose();
            deviceBufferIndex?.Dispose();
        }
    }
    public class Mesh : Resource, IDrawable3D
    {
        private readonly Dictionary<int, MeshSurface> _surfaces = new Dictionary<int, MeshSurface>();

        private readonly List<DrawableMeshCache> drawableCache = new List<DrawableMeshCache>();

        protected bool _isDirty = true;
        public bool isDirty => _isDirty;

        private bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        public override void Dispose()
        {
        }

        public Dictionary<int, MeshSurface> Sufraces => _surfaces;

        IViewport IDrawable.Viewport => throw new NotImplementedException();

        public void AddSurface(int index, MeshSurface surfaceData)
        {
            lock (_surfaces)
            {
                _surfaces.Add(index, surfaceData);
                _isDirty = true;
            }
        }

        public void OnDraw3D(IRenderer renderer)
        {
            foreach (DrawableMeshCache surface in drawableCache)
            {
                if (surface.LodBlocks != null && surface.LodBlocks.ContainsKey(0))
                {
                    renderer.BindBuffers(surface.deviceBufferVertex, surface.deviceBufferIndex);
                    renderer.DrawIndexInstanced(surface.LodBlocks[0].Indicies);
                }
            }
        }

        public void BeforeDraw(IRenderer renderer)
        {
            //todo: do async -> generate in thread then bind it
            //todo: global material
            //todo: lods 

            if (_isDirty)
            {

                //clear cache
                foreach (DrawableMeshCache drawable in drawableCache)
                {
                    drawable.Dispose();
                }

                drawableCache.Clear();

                //create buffers
                foreach (MeshSurface surface in _surfaces.Values)
                {
                    //vertices
                    DrawableMeshCache cache = new DrawableMeshCache();

                    uint size = (uint)surface.Vertices.Length * Vertex.GetSizeInBytes();
                    BufferDescription vbDescription = new BufferDescription
                        (
                            size,
                            BufferUsage.VertexBuffer
                        );

                    cache.deviceBufferVertex = renderer.CreateBuffer(vbDescription);
                    renderer.UpdateBuffer(cache.deviceBufferVertex, 0, surface.Vertices);

                    //indicies
                    BufferDescription ibDescription = new BufferDescription
                        (
                            (uint)surface.Indicies.Length * sizeof(ushort),
                            BufferUsage.IndexBuffer
                        );
                    cache.deviceBufferIndex = renderer.CreateBuffer(ibDescription);
                    renderer.UpdateBuffer(cache.deviceBufferIndex, 0, surface.Indicies);

                    //copy lod blocks
                    cache.LodBlocks = surface.LodBlocks;
                    drawableCache.Add(cache);
                }

                _isDirty = false;

            }

        }
    }
}
