using Silk.NET.Vulkan;
using Striked3D.Servers.Rendering.Vulkan;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Striked3D.Servers.Rendering
{
    public enum RenderPrimitiveType
    {
		TRIANGLE_LIST
	}
	public struct RenderVertex
    {

    }

    public struct RenderObjectSurface
    {
        public Guid material;
		[MarshalAs(UnmanagedType.LPArray)]
		public Vertex[] vertices;
		[MarshalAs(UnmanagedType.LPArray)]
		public ushort[] indicies;

		public RenderPrimitiveType type;

        public bool isDirty = false;
        public RenderBuffer renderBuffer;

        private unsafe void CreateOrResizeBuffer(RenderingInstance instance, LogicalDevice device, ref Buffer buffer, ref DeviceMemory buffer_memory, ref ulong bufferSize, ulong newSize, BufferUsageFlags usage)
		{
			if (buffer.Handle != default)
			{
				instance.Api.DestroyBuffer(device.NativeHandle, buffer, default);
			}

			if (buffer_memory.Handle != default)
			{
				instance.Api.FreeMemory(device.NativeHandle, buffer_memory, default);
			}

			var bufferInfo = new BufferCreateInfo();
			bufferInfo.SType = StructureType.BufferCreateInfo;
			bufferInfo.Size = newSize;
			bufferInfo.Usage = usage;
			bufferInfo.SharingMode = SharingMode.Exclusive;

			if (instance.Api.CreateBuffer(device.NativeHandle, bufferInfo, default, out buffer) != Result.Success)
			{
				throw new Exception($"Unable to create a device buffer");
			}

			instance.Api.GetBufferMemoryRequirements(device.NativeHandle, buffer, out var req);
			MemoryAllocateInfo allocInfo = new MemoryAllocateInfo();
			allocInfo.SType = StructureType.MemoryAllocateInfo;
			allocInfo.AllocationSize = req.Size;
            allocInfo.MemoryTypeIndex = device.GetMemoryTypeIndex( MemoryPropertyFlags.MemoryPropertyHostVisibleBit, req.MemoryTypeBits);

            if (instance.Api.AllocateMemory(device.NativeHandle, &allocInfo, default, out buffer_memory) != Result.Success)
			{
				throw new Exception($"Unable to allocate device memory");
            }

            if (instance.Api.BindBufferMemory(device.NativeHandle, buffer, buffer_memory, 0) != Result.Success)
            {
                throw new Exception($"Unable to bind device memory");
            }

            bufferSize = req.Size;
		}

		public unsafe void UploadMesh(RenderingInstance renderingInstance, LogicalDevice device)
        {
            if (isDirty == false)
            {
                return;
            }

			if(vertices != null && vertices.Length > 0)
            {
				ulong vertex_size = (ulong)vertices.Length * (ulong)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex));
				if (renderBuffer.VertexBuffer.Handle == default || renderBuffer.VertexBufferSize < vertex_size)
				{
					CreateOrResizeBuffer(renderingInstance, device, ref renderBuffer.VertexBuffer,
						ref renderBuffer.VertexBufferMemory, ref renderBuffer.VertexBufferSize, vertex_size,
						BufferUsageFlags.BufferUsageVertexBufferBit);
				}

				void* vtx_dst = null;
				if (renderingInstance.Api.MapMemory(device.NativeHandle, renderBuffer.VertexBufferMemory, 0, renderBuffer.VertexBufferSize, 0, (void**)(&vtx_dst)) != Result.Success)
				{
					throw new Exception($"Unable to map device memory");
				}

				fixed (Vertex* FirstResult = &vertices[0])
				{
					Unsafe.CopyBlock(vtx_dst, FirstResult, (uint)vertex_size);
				}

				renderingInstance.Api.UnmapMemory(device.NativeHandle, renderBuffer.VertexBufferMemory);
			}

			if (indicies != null && indicies.Length > 0)
			{
				ulong index_size = (ulong)indicies.Length * (ulong)sizeof(ushort);
				if (renderBuffer.IndexBuffer.Handle == default || renderBuffer.IndexBufferSize < index_size)
				{
					CreateOrResizeBuffer(renderingInstance, device, ref renderBuffer.IndexBuffer,
						ref renderBuffer.IndexBufferMemory, ref renderBuffer.IndexBufferSize, index_size,
						BufferUsageFlags.BufferUsageIndexBufferBit);
				}

				void* idx_dst = null;

				if (renderingInstance.Api.MapMemory(device.NativeHandle, renderBuffer.IndexBufferMemory, 0, renderBuffer.IndexBufferSize, 0, (void**)(&idx_dst)) != Result.Success)
				{
					throw new Exception($"Unable to map device memory");
				}

				fixed (ushort* FirstResult = &indicies[0])
				{
					Unsafe.CopyBlock(idx_dst, FirstResult, (uint)index_size);
				}

				renderingInstance.Api.UnmapMemory(device.NativeHandle, renderBuffer.IndexBufferMemory);
			}
		}
	}

	public struct RenderBuffer
	{
		public DeviceMemory VertexBufferMemory;
		public DeviceMemory IndexBufferMemory;
		public ulong VertexBufferSize;
		public ulong IndexBufferSize;
		public Silk.NET.Vulkan.Buffer VertexBuffer;
		public Silk.NET.Vulkan.Buffer IndexBuffer;

		public bool isDirty = false;
	};

	public struct RenderObject
	{
		public Matrix4X4<float> transformMatrix;
		public Guid viewport;
		public Dictionary<int, RenderObjectSurface> surfaces;

		public int Priority;

		public RenderObject()
        {
			Priority = 0;
			transformMatrix = Matrix4X4<float>.Identity;
			surfaces = new Dictionary<int, RenderObjectSurface>();
		}
	};
}
