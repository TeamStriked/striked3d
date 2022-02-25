using Striked3D.Resources;
using Striked3D.Types;
using Veldrid;
using Striked3D.Graphics;
using Striked3D.Math;

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

            isInit = true;
        }

        public override void Dispose()
        {
            CanvasBuffer.Dispose();
            _resourceSet.Dispose();
        }

        private Vector4D<float> prevSize = Vector4D<float>.Zero;

        public override void Update(IRenderer renderer, IViewport viewport)
        {
            if (!isInit)
            {
                Create(renderer);
            }

            Vector4D<float> sizePos = new Vector4D<float>(viewport.Size.X, viewport.Size.Y, viewport.Position.X, viewport.Position.Y);

            if (prevSize != sizePos)
            {
                renderer.UpdateBuffer(CanvasBuffer, 0, sizePos);
                prevSize = sizePos;
            }
        }
    }
}
