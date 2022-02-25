using Striked3D.Core;
using Striked3D.Resources;
using Veldrid;
using Striked3D.Graphics;

namespace Striked3D.Nodes
{
    public class MeshInstance3D : VisualInstance3D
    {
        public Mesh Surface { get; set; }
        public Material3D Material { get; set; }

        public MeshInstance3D() : base()
        {
            Material = new Material3D();
        }

        public override void OnDraw3D(IRenderer renderer)
        {
            if (Material != null && !Material.isDirty && Transform != null)
            {
                Transform.OnDraw3D(renderer);

                renderer.SetViewport(Viewport);
                renderer.SetMaterial(Material);

                renderer.SetResourceSets(new ResourceSet[] { Viewport.World3D.ResourceSet, Transform.ModalSet });
                //set the pipeline

                //draw the surface
                Surface?.OnDraw3D(renderer);
            }
        }

        public override void BeforeDraw(IRenderer renderer)
        {
            if (Viewport != null && Viewport.World3D != null && Transform != null)
            {
                Transform.BeforeDraw(renderer);
                Material?.BeforeDraw(renderer);

                if (Material != null && !Material.isDirty)
                {
                    Surface?.BeforeDraw(renderer);
                }
            }
        }

        public override void Dispose()
        {
            Material?.Dispose();
            Transform?.Dispose();
        }
    }
}
