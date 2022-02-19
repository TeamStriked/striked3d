using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Core.Pipeline;
using Striked3D.Core.Reference;
using Striked3D.Resources;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Striked3D.Nodes
{
    public class MeshInstance3D : VisualInstance3D
    {
        public Mesh Surface { get; set; }
        public Material3D Material { get; set; }

        public MeshInstance3D() : base()
        {
            this.Material = new Material3D();
        }

        public override void OnDraw3D(IRenderer renderer)
        {
            if (Material != null && !Material.isDirty && this.Transform != null)
            {
                this.Transform.OnDraw3D(renderer);

                renderer.SetViewport(Viewport);
                renderer.SetMaterial(Material);

                renderer.SetResourceSets(new ResourceSet[] { Viewport.World3D.ResourceSet , this.Transform.ModalSet });
                //set the pipeline

                //draw the surface
                this.Surface?.OnDraw3D(renderer);
            }
        }

        public override void BeforeDraw(IRenderer renderer)
        {
            if (Viewport != null && Viewport.World3D != null && this.Transform != null)
            {
                this.Transform.BeforeDraw(renderer);
                this.Material?.BeforeDraw(renderer);

                if (this.Material != null && !this.Material.isDirty)
                {
                    this.Surface?.BeforeDraw(renderer);
                }
            }
        }

        public override void Dispose()
        {
            this.Material?.Dispose();
            this.Transform?.Dispose();
        }
    }
}
