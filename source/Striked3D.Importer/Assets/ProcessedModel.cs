using Striked3D.Resources;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Veldrid;

namespace Striked3D.Core.AssetsPrimitives
{
    public class ProcessedModel
    {
        public Mesh Mesh { get; set; }
        public ProcessedNodeSet Nodes { get; set; }
        public ProcessedAnimation[] Animations { get; set; }
    }

    public class ProcessedMeshPart
    {
        public byte[] VertexData { get; set; }
        public VertexElementDescription[] VertexElements { get; set; }
        public byte[] IndexData { get; set; }
        public IndexFormat IndexFormat { get; set; }
        public uint IndexCount { get; set; }
        public Dictionary<string, uint> BoneIDsByName { get; set; }
        public Silk.NET.Maths.Matrix4X4<float>[] BoneOffsets { get; set; }

        public ProcessedMeshPart(
            byte[] vertexData,
            VertexElementDescription[] vertexElements,
            byte[] indexData,
            IndexFormat indexFormat,
            uint indexCount,
            Dictionary<string, uint> boneIDsByName,
            Silk.NET.Maths.Matrix4X4<float>[] boneOffsets)
        {
            VertexData = vertexData;
            VertexElements = vertexElements;
            IndexData = indexData;
            IndexFormat = indexFormat;
            IndexCount = indexCount;
            BoneIDsByName = boneIDsByName;
            BoneOffsets = boneOffsets;
        }

        public ModelResources CreateDeviceResources(
            GraphicsDevice gd,
            ResourceFactory factory)
        {
            DeviceBuffer vertexBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)VertexData.Length, BufferUsage.VertexBuffer));
            gd.UpdateBuffer(vertexBuffer, 0, VertexData);

            DeviceBuffer indexBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)IndexData.Length, BufferUsage.IndexBuffer));
            gd.UpdateBuffer(indexBuffer, 0, IndexData);

            return new ModelResources(vertexBuffer, indexBuffer, IndexFormat, IndexCount);
        }
    }

    public class ProcessedAnimation
    {
        public ProcessedAnimation(
            string name,
            double durationInTicks,
            double ticksPerSecond,
            Dictionary<string, ProcessedAnimationChannel> animationChannels)
        {
            Name = name;
            DurationInTicks = durationInTicks;
            TicksPerSecond = ticksPerSecond;
            AnimationChannels = animationChannels;
        }

        public string Name { get; set; }
        public double DurationInTicks { get; set; }
        public double TicksPerSecond { get; set; }
        public Dictionary<string, ProcessedAnimationChannel> AnimationChannels { get; set; }

        public double DurationInSeconds => DurationInTicks * TicksPerSecond;
    }

    public class ProcessedAnimationChannel
    {
        public ProcessedAnimationChannel(string nodeName, VectorKey[] positions, VectorKey[] scales, QuaternionKey[] rotations)
        {
            NodeName = nodeName;
            Positions = positions;
            Scales = scales;
            Rotations = rotations;
        }

        public string NodeName { get; set; }
        public VectorKey[] Positions { get; set; }
        public VectorKey[] Scales { get; set; }
        public QuaternionKey[] Rotations { get; set; }
    }

    public struct VectorKey
    {
        public readonly double Time;
        public readonly Vector3 Value;

        public VectorKey(double time, Vector3 value)
        {
            Time = time;
            Value = value;
        }
    }

    public struct QuaternionKey
    {
        public readonly double Time;
        public readonly Quaternion Value;

        public QuaternionKey(double time, Quaternion value)
        {
            Time = time;
            Value = value;
        }
    }

    public class ProcessedNodeSet
    {
        public ProcessedNodeSet(ProcessedNode[] nodes, int rootNodeIndex, Matrix4x4 rootNodeInverseTransform)
        {
            Nodes = nodes;
            RootNodeIndex = rootNodeIndex;
            RootNodeInverseTransform = rootNodeInverseTransform;
        }

        public ProcessedNode[] Nodes { get; set; }
        public int RootNodeIndex { get; set; }
        public Matrix4x4 RootNodeInverseTransform { get; set; }
    }

    public class ProcessedNode
    {
        public ProcessedNode(string name, Matrix4x4 transform, int parentIndex, int[] childIndices)
        {
            Name = name;
            Transform = transform;
            ParentIndex = parentIndex;
            ChildIndices = childIndices;
        }

        public string Name { get; set; }
        public Matrix4x4 Transform { get; set; }
        public int ParentIndex { get; set; }
        public int[] ChildIndices { get; set; }
    }

    public struct ModelResources
    {
        public readonly DeviceBuffer VertexBuffer;
        public readonly DeviceBuffer IndexBuffer;
        public readonly IndexFormat IndexFormat;
        public readonly uint IndexCount;

        public ModelResources(DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer, IndexFormat indexFormat, uint indexCount)
        {
            VertexBuffer = vertexBuffer;
            IndexBuffer = indexBuffer;
            IndexFormat = indexFormat;
            IndexCount = indexCount;
        }
    }

    public class ProcessedModelSerializer : BinaryAssetSerializer<ProcessedModel>
    {
        public override ProcessedModel ReadT(BinaryReader reader)
        {
           // MeshSurface[] parts = reader.ReadObjectArray(ReadMeshPart);

            return new ProcessedModel()
            {
              //  Surfaces = parts
            };
        }

        public override void WriteT(BinaryWriter writer, ProcessedModel value)
        {
         //   writer.WriteObjectArray(value.Surfaces, WriteMeshPart);
        }

        private void WriteMeshPart(BinaryWriter writer, MeshSurface part)
        {
        
        }

        private MeshSurface ReadMeshPart(BinaryReader reader)
        {
            return new MeshSurface();
        }


        private void WriteVertexElementDesc(BinaryWriter writer, VertexElementDescription desc)
        {
            writer.Write(desc.Name);
            writer.WriteEnum(desc.Semantic);
            writer.WriteEnum(desc.Format);
        }

        public VertexElementDescription ReadVertexElementDesc(BinaryReader reader)
        {
            string name = reader.ReadString();
            VertexElementSemantic semantic = reader.ReadEnum<VertexElementSemantic>();
            VertexElementFormat format = reader.ReadEnum<VertexElementFormat>();
            return new VertexElementDescription(name, format, semantic);
        }
    }
}
