using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using aiMatrix4x4 = Assimp.Matrix4x4;
using Striked3D.Core.AssetsPrimitives;
using Assimp;
using Silk.NET.Maths;
using Striked3D.Types;

namespace Striked3D.Core.Assets
{
    public class AssimpProcessor : BinaryAssetProcessor<Striked3D.Resources.Mesh>
    {
        public unsafe override Striked3D.Resources.Mesh ProcessT(Stream stream, string extension)
        {
            var generatedMesh = new Striked3D.Resources.Mesh();

            AssimpContext ac = new AssimpContext();
            Scene scene = ac.ImportFileFromStream(
                stream,
                PostProcessSteps.FlipWindingOrder | PostProcessSteps.GenerateNormals | PostProcessSteps.CalculateTangentSpace | PostProcessSteps.FlipUVs,
                extension);
            aiMatrix4x4 rootNodeInverseTransform = scene.RootNode.Transform;
            rootNodeInverseTransform.Inverse();

            List<MeshSurface> parts = new ();
            List<ProcessedAnimation> animations = new ();

            HashSet<string> encounteredNames = new ();
            for (int meshIndex = 0; meshIndex < scene.MeshCount; meshIndex++)
            {
                Mesh mesh = scene.Meshes[meshIndex];
                string meshName = mesh.Name;
                if (string.IsNullOrEmpty(meshName))
                {
                    meshName = $"mesh_{meshIndex}";
                }
                int counter = 1;
                while (!encounteredNames.Add(meshName))
                {
                    meshName = mesh.Name + "_" + counter.ToString();
                    counter += 1;
                }
                int vertexCount = mesh.VertexCount;

                Vertex[] vertices = new Vertex[vertexCount];
   

                uint[] boneIndicies = new uint[vertexCount];
                float[] boneWeights = new float[vertexCount];

                Vector3 min = vertexCount > 0 ? mesh.Vertices[0].ToSystemVector3() : Vector3.Zero;
                Vector3 max = vertexCount > 0 ? mesh.Vertices[0].ToSystemVector3() : Vector3.Zero;

                for (int i = 0; i < vertexCount; i++)
                {
                    Vector3 position = mesh.Vertices[i].ToSystemVector3();
                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position);

                    vertices[i] = new Vertex();

                    vertices[i].Position = new Vector3D<float>(position.X, position.Y, position.Z);

                    var normal = mesh.Normals[i].ToSystemVector3();
                    var tangent = mesh.Tangents[i].ToSystemVector3();

                    vertices[i].Normal = new Vector3D<float>(normal.X, normal.Y, normal.Z);
                    vertices[i].Tangent = new Vector3D<float>(tangent.X, tangent.Y, tangent.Z);

                    if (mesh.HasVertexColors(0))
                    {
                        vertices[i].Color = new RgbaFloat(mesh.VertexColorChannels[0][i].R,
                            mesh.VertexColorChannels[0][i].G,
                            mesh.VertexColorChannels[0][i].B,
                            mesh.VertexColorChannels[0][i].A);
                    }
                    else
                    {
                        vertices[i].Color = new RgbaFloat();
                    }

                    if (mesh.HasTextureCoords(0))
                    {
                        vertices[i].Uv1 = new Vector2D<float>(mesh.TextureCoordinateChannels[0][i].X, mesh.TextureCoordinateChannels[0][i].Y);
                    }
                    else
                    {
                        vertices[i].Uv1 = Vector2D<float>.Zero;
                    }

                    if (mesh.HasTextureCoords(1))
                    {
                        vertices[i].Uv2 = new Vector2D<float>(mesh.TextureCoordinateChannels[1][i].X, mesh.TextureCoordinateChannels[1][i].Y);
                    }
                    else
                    {
                        vertices[i].Uv2 =  Vector2D<float>.Zero;
                    }
                }

            /*
                Dictionary<string, uint> boneIDsByName = new Dictionary<string, uint>();

                if (mesh.HasBones)
                {
                    Dictionary<int, int> assignedBoneWeights = new Dictionary<int, int>();
                    for (uint boneID = 0; boneID < mesh.BoneCount; boneID++)
                    {
                        Bone bone = mesh.Bones[(int)boneID];
                        string boneName = bone.Name;
                        int suffix = 1;
                        while (boneIDsByName.ContainsKey(boneName))
                        {
                            boneName = bone.Name + "_" + suffix.ToString();
                            suffix += 1;
                        }

                        boneIDsByName.Add(boneName, boneID);
                        foreach (VertexWeight weight in bone.VertexWeights)
                        {
                            int relativeBoneIndex = GetAndIncrementRelativeBoneIndex(assignedBoneWeights, weight.VertexID);

                            boneIndicies[relativeBoneIndex] = boneID;
                            boneWeights[relativeBoneIndex] = weight.Weight;
                        }

                        System.Numerics.Matrix4x4 offsetMat = bone.OffsetMatrix.ToSystemMatrixTransposed();
                        System.Numerics.Matrix4x4.Decompose(offsetMat, out var scale, out var rot, out var trans);
                        offsetMat = System.Numerics.Matrix4x4.CreateScale(scale)
                            * System.Numerics.Matrix4x4.CreateFromQuaternion(rot)
                            * System.Numerics.Matrix4x4.CreateTranslation(trans);
                    }
                }
            */
                List<ushort> indices = new List<ushort>();
                foreach (Face face in mesh.Faces)
                {
                    if (face.IndexCount == 3)
                    {
                        indices.Add((ushort) face.Indices[0]);
                        indices.Add((ushort)face.Indices[1]);
                        indices.Add((ushort)face.Indices[2]);
                    }
                }

                MeshSurface part = new MeshSurface
                {
                    Vertices = vertices,
                    Indicies = indices.ToArray(),
                };

                part.LodBlocks.Add(0, new MeshSurfaceLodBlock { Indicies = indices.Count, Vertices = vertices.Length });
                generatedMesh.AddSurface(meshIndex, part);
            }

            // Nodes
            Assimp.Node rootNode = scene.RootNode;
            List<ProcessedNode> processedNodes = new List<ProcessedNode>();
            ConvertNode(rootNode, -1, processedNodes);

            ProcessedNodeSet nodes = new ProcessedNodeSet(processedNodes.ToArray(), 0, rootNodeInverseTransform.ToSystemMatrixTransposed());

            for (int animIndex = 0; animIndex < scene.AnimationCount; animIndex++)
            {
                Animation animation = scene.Animations[animIndex];
                Dictionary<string, ProcessedAnimationChannel> channels = new Dictionary<string, ProcessedAnimationChannel>();
                for (int channelIndex = 0; channelIndex < animation.NodeAnimationChannelCount; channelIndex++)
                {
                    NodeAnimationChannel nac = animation.NodeAnimationChannels[channelIndex];
                    channels[nac.NodeName] = ConvertChannel(nac);
                }

                string baseAnimName = animation.Name;
                if (string.IsNullOrEmpty(baseAnimName))
                {
                    baseAnimName = "anim_" + animIndex;
                }

                string animationName = baseAnimName;


                int counter = 1;
                while (!encounteredNames.Add(animationName))
                {
                    animationName = baseAnimName + "_" + counter.ToString();
                    counter += 1;
                }
            }

            

            return generatedMesh;

         /*   return new ProcessedModel()
            {
                Surfaces = parts.ToArray(),
                Animations = animations.ToArray(),
                Nodes = nodes
            };
         */
        }

        private int GetAndIncrementRelativeBoneIndex(Dictionary<int, int> assignedBoneWeights, int vertexID)
        {
            int currentCount = 0;
            assignedBoneWeights.TryGetValue(vertexID, out currentCount);
            assignedBoneWeights[vertexID] = currentCount + 1;
            return currentCount;
        }

        private ProcessedAnimationChannel ConvertChannel(NodeAnimationChannel nac)
        {
            string nodeName = nac.NodeName;
            Striked3D.Core.AssetsPrimitives.VectorKey[] positions = new Striked3D.Core.AssetsPrimitives.VectorKey[nac.PositionKeyCount];
            for (int i = 0; i < nac.PositionKeyCount; i++)
            {
                Assimp.VectorKey assimpKey = nac.PositionKeys[i];
                positions[i] = new Striked3D.Core.AssetsPrimitives.VectorKey(assimpKey.Time, assimpKey.Value.ToSystemVector3());
            }

            Striked3D.Core.AssetsPrimitives.VectorKey[] scales = new Striked3D.Core.AssetsPrimitives.VectorKey[nac.ScalingKeyCount];
            for (int i = 0; i < nac.ScalingKeyCount; i++)
            {
                Assimp.VectorKey assimpKey = nac.ScalingKeys[i];
                scales[i] = new Striked3D.Core.AssetsPrimitives.VectorKey(assimpKey.Time, assimpKey.Value.ToSystemVector3());
            }

            Striked3D.Core.AssetsPrimitives.QuaternionKey[] rotations = new Striked3D.Core.AssetsPrimitives.QuaternionKey[nac.RotationKeyCount];
            for (int i = 0; i < nac.RotationKeyCount; i++)
            {
                Assimp.QuaternionKey assimpKey = nac.RotationKeys[i];
                rotations[i] = new Striked3D.Core.AssetsPrimitives.QuaternionKey(assimpKey.Time, assimpKey.Value.ToSystemQuaternion());
            }

            return new ProcessedAnimationChannel(nodeName, positions, scales, rotations);
        }

        private int ConvertNode(Assimp.Node node, int parentIndex, List<ProcessedNode> processedNodes)
        {
            int currentIndex = processedNodes.Count;
            int[] childIndices = new int[node.ChildCount];
            var nodeTransform = node.Transform.ToSystemMatrixTransposed();
            ProcessedNode pn = new ProcessedNode(node.Name, nodeTransform, parentIndex, childIndices);
            processedNodes.Add(pn);

            for (int i = 0; i < childIndices.Length; i++)
            {
                int childIndex = ConvertNode(node.Children[i], currentIndex, processedNodes);
                childIndices[i] = childIndex;
            }

            return currentIndex;
        }

        private unsafe struct VertexDataBuilder
        {
            private readonly GCHandle _gch;
            private readonly unsafe byte* _dataPtr;
            private readonly int _vertexSize;

            public VertexDataBuilder(byte[] data, int vertexSize)
            {
                _gch = GCHandle.Alloc(data, GCHandleType.Pinned);
                _dataPtr = (byte*)_gch.AddrOfPinnedObject();
                _vertexSize = vertexSize;
            }

            public void WriteVertexElement<T>(int vertex, int elementOffset, ref T data)
            {
                byte* dst = _dataPtr + (_vertexSize * vertex) + elementOffset;
                Unsafe.Copy(dst, ref data);
            }

            public void WriteVertexElement<T>(int vertex, int elementOffset, T data)
            {
                byte* dst = _dataPtr + (_vertexSize * vertex) + elementOffset;
                Unsafe.Copy(dst, ref data);
            }

            public void FreeGCHandle()
            {
                _gch.Free();
            }
        }
    }

    public static class AssimpExtensions
    {
        public static unsafe System.Numerics.Matrix4x4 ToSystemMatrixTransposed(this aiMatrix4x4 mat)
        {
            return System.Numerics.Matrix4x4.Transpose(Unsafe.Read<System.Numerics.Matrix4x4>(&mat));
        }

        public static System.Numerics.Quaternion ToSystemQuaternion(this Assimp.Quaternion quat)
        {
            return new System.Numerics.Quaternion(quat.X, quat.Y, quat.Z, quat.W);
        }

        public static Vector3 ToSystemVector3(this Assimp.Vector3D v3)
        {
            return new Vector3(v3.X, v3.Y, v3.Z);
        }
    }
}
