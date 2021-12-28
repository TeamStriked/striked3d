using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Silk.NET.Vulkan;
using Striked3D.Core;
using Striked3D.Servers.Rendering;
using Striked3D.Types;

namespace Striked3D.Nodes
{
    public class Grid : BaseNode
    {
        Guid meshId;
        Guid materialId;
        Servers.RenderingServer renderServer = null;
        public override void OnEnterTree()
        {
            base.OnEnterTree();

            renderServer = window.GetService<Servers.RenderingServer>();
            renderServer.CreateMaterial(SetMaterial);
        }

        private void SetMaterial(EngineCommandResult res)
        {
            Logger.Debug(this, "New material create");
            materialId = (Guid)res.result;
          
            renderServer.SetMaterialParameter(materialId, "cullMode", CullModeFlags.CullModeBackBit);
            renderServer.SetMaterialParameter(materialId, "logic", LogicOp.NoOp);
            renderServer.SetMaterialParameter(materialId, "frontFace", FrontFace.CounterClockwise);
            renderServer.SetMaterialParameter(materialId, "depthCompareOp", CompareOp.GreaterOrEqual);
            renderServer.SetMaterialParameter(materialId, "blendEnabled", true);

            renderServer.SetMaterialShader(materialId, ShaderStageFlags.ShaderStageFragmentBit, "grid.frag");
            renderServer.SetMaterialShader(materialId, ShaderStageFlags.ShaderStageVertexBit, "grid.vert");

            renderServer.CreateMesh(SetMesh);
        }

        private void SetMesh(EngineCommandResult res)
        {
            meshId = (Guid)res.result;

            var hash = new Hashtable();

            var positions = new List<Vector3D<float>>();
            positions.Add(new Vector3D<float>(1, 1, 0));
            positions.Add(new Vector3D<float>(-1, -1, 0));
            positions.Add(new Vector3D<float>(-1, 1, 0));
            positions.Add(new Vector3D<float>(-1, -1, 0));
            positions.Add(new Vector3D<float>(1, 1, 0));
            positions.Add(new Vector3D<float>(1, -1, 0));

            hash.Add("positions", positions.ToArray());

            renderServer.SetMeshData(meshId, 0, RenderPrimitiveType.TRIANGLE_LIST, hash);
            renderServer.SetMaterial(meshId, 0, materialId);
            renderServer.SetPriority(meshId, -1);
        }
    }
}
