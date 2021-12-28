using Silk.NET.Vulkan;
using Striked3D.Servers.Rendering.Vulkan;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Striked3D.Servers.Rendering
{
    public struct VertexInputDescription
    {
        public List<VertexInputBindingDescription> bindings;
		public List<VertexInputAttributeDescription> attributes;
    };

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Vertex
    {
        public Vector3D<float> position;
		public Vector3D<float> normal;
		public Vector3D<float> color;

		public static VertexInputDescription GetVertexDescription()
		{
			VertexInputDescription description = new VertexInputDescription();
			description.bindings = new List<VertexInputBindingDescription>();
			description.attributes = new List<VertexInputAttributeDescription>();

			//we will have just 1 vertex buffer binding, with a per-vertex rate
			VertexInputBindingDescription mainBinding = new VertexInputBindingDescription();
			mainBinding.Binding = 0;
			mainBinding.Stride = (uint) System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex));
			mainBinding.InputRate = VertexInputRate.Vertex;

			description.bindings.Add(mainBinding);

			//Position will be stored at Location 0
			VertexInputAttributeDescription positionAttribute = new VertexInputAttributeDescription();
			positionAttribute.Binding = 0;
			positionAttribute.Location = 0;
			positionAttribute.Format = Format.R32G32B32Sfloat;
			positionAttribute.Offset = (uint) Marshal.OffsetOf<Vertex>("position");

			//Normal will be stored at Location 1
			VertexInputAttributeDescription normalAttribute = new VertexInputAttributeDescription();
			normalAttribute.Binding = 0;
			normalAttribute.Location = 1;
			normalAttribute.Format = Format.R32G32B32Sfloat;
			normalAttribute.Offset = (uint)Marshal.OffsetOf<Vertex>("normal");

			//Position will be stored at Location 2
			VertexInputAttributeDescription colorAttribute = new VertexInputAttributeDescription();
			colorAttribute.Binding = 0;
			colorAttribute.Location = 2;
			colorAttribute.Format = Format.R32G32B32Sfloat;
			colorAttribute.Offset = (uint)Marshal.OffsetOf<Vertex>("color");

			description.attributes.Add(positionAttribute);
			description.attributes.Add(normalAttribute);
			description.attributes.Add(colorAttribute);

			return description;
		}

	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MeshPushConstants
    {
        public Matrix4X4<float> model;
    };

}
