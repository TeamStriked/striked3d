using Silk.NET.Maths;
using Striked3D.Core.Graphics;
using Striked3D.Resources;
using Veldrid;

namespace Striked3D.Core.Graphics
{
    public class World2D : World
    {
        private DeviceBuffer CanvasBuffer { get; set; }

        private bool isInit = false;

        private void Create(IRenderer renderer)
        {
            //buffers
            CanvasBuffer = renderer.CreateBuffer(new BufferDescription
            (
                CanvasInfo.GetSizeInBytes(),
                BufferUsage.UniformBuffer
            ));

            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(renderer.Material2DLayout, CanvasBuffer);
            _resourceSet = renderer.CreateResourceSet(resourceSetDescription);

            this.isInit = true;
        }

        public override void Dispose()
        {
            CanvasBuffer.Dispose();
            _resourceSet.Dispose();
        }

        private Vector4D<float> prevSize = Vector4D<float>.Zero;

        public override void Update(IRenderer renderer, IViewport viewport)
        {
            if(!this.isInit)
            {
                this.Create(renderer);
            }

            var sizePos = new Vector4D<float>(viewport.Size.X, viewport.Size.Y, viewport.Position.X, viewport.Position.Y);

            if (prevSize != sizePos)
            {
                renderer.UpdateBuffer(this.CanvasBuffer, 0, sizePos);
                prevSize = sizePos;
            }
        }
    }
}
