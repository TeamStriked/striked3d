using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Core.Reference;
using Striked3D.Graphics;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Striked3D.Resources
{
  
    internal struct DrawableMeshCache
    {
        public DeviceBuffer deviceBufferVertex { get; set; }
        public DeviceBuffer deviceBufferIndex { get; set; }

        public Dictionary<int, MeshSurfaceLodBlock> LodBlocks  { get; set; }

        public void Dispose()
        {
            LodBlocks = null;
            deviceBufferVertex?.Dispose();
            deviceBufferIndex?.Dispose();
        }
    }
    public class Mesh : Resource, IDrawable3D
    {
        private Dictionary<int, MeshSurface> _surfaces = new Dictionary<int, MeshSurface>();

        private List<DrawableMeshCache> drawableCache = new List<DrawableMeshCache>();

        protected bool _isDirty = true;
        public bool isDirty => _isDirty;

        private bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        public override void Dispose()
        {
        }

        public Dictionary<int, MeshSurface> Sufraces 
        { 
            get
            {
                return _surfaces; 
            }
        }

        IViewport IDrawable.Viewport => throw new NotImplementedException();

        public void AddSurface(int index, MeshSurface surfaceData)
        {
            lock(_surfaces)
            {
                this._surfaces.Add(index, surfaceData);
                this._isDirty = true;
            }
        }

        public void OnDraw3D(IRenderer renderer)
        {
            foreach (var surface in drawableCache)
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
                    foreach (var drawable in this.drawableCache)
                    {
                        drawable.Dispose();
                    }

                    this.drawableCache.Clear();

                    //create buffers
                    foreach (var surface in this._surfaces.Values)
                    {
                        //vertices
                        var cache = new DrawableMeshCache();

                        uint size = (uint)surface.Vertices.Length * Vertex.GetSizeInBytes();
                        var vbDescription = new BufferDescription
                        (
                            size,
                            BufferUsage.VertexBuffer
                        );

                        cache.deviceBufferVertex = renderer.CreateBuffer(vbDescription);
                        renderer.UpdateBuffer(cache.deviceBufferVertex, 0, surface.Vertices);

                        //indicies
                        var ibDescription = new BufferDescription
                        (
                            (uint)surface.Indicies.Length * sizeof(ushort),
                            BufferUsage.IndexBuffer
                        );
                        cache.deviceBufferIndex = renderer.CreateBuffer(ibDescription);
                        renderer.UpdateBuffer(cache.deviceBufferIndex, 0, surface.Indicies);

                        //copy lod blocks
                        cache.LodBlocks = surface.LodBlocks;
                        this.drawableCache.Add(cache);
                    }

                    this._isDirty = false;
              
            }

        }
    }
}
