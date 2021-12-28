using System;
using System.Collections.Generic;
using System.Text;
using Striked3D.Core;

namespace Striked3D.Nodes
{
    public class MeshInstance : BaseNode
    {
        Guid meshId;
        Servers.RenderingServer renderServer = null;
        public override void OnEnterTree()
        { 
            base.OnEnterTree();

            renderServer =  window.GetService<Servers.RenderingServer>();
            renderServer.CreateMesh(SetMesh);
        }

        private void SetMesh(EngineCommandResult res)
        {
            var meshId = (Guid) res.result;

            Logger.Debug(this, "Return result: " + meshId.ToString());

            var model = Striked3D.Tools.MeshImporter.Import("Models\\monkey_smooth.obj");
            Logger.Debug(this, "Return result of imported meshes: " + model.Count);

            int surfaceId = 0;
            foreach(var surface in model)
            {
                renderServer.SetMeshData(meshId, surfaceId++, Servers.Rendering.RenderPrimitiveType.TRIANGLE_LIST, surface);
            }
        }
    }
}
