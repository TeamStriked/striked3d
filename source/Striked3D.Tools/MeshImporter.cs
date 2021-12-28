using Silk.NET.Core.Native;
using Striked3D.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Tools
{
    public class MeshImporter
    {
        public static unsafe List<Hashtable> Import(string path)
        {
            var sysPath = System.IO.Path.Combine(Engine.EngineInfo.GetAssetsDictonary(), path);

            var meshes = new List<Hashtable>();

            var assimp = Silk.NET.Assimp.Assimp.GetApi();
            var scene = assimp.ImportFile((byte*)SilkMarshal.StringToPtr(sysPath), (uint)Silk.NET.Assimp.PostProcessSteps.Triangulate);
            var root = scene->MRootNode;

            processNode(root, scene, ref meshes);

            return meshes;
        }

        private static unsafe Hashtable processMesh(Silk.NET.Assimp.Mesh* mesh)
        {
            var hashtable = new Hashtable();

            var positions = new List<Vector3D<float>>();
            var normals = new List<Vector3D<float>>();
            var indicies = new List<ushort>();

            for (uint i = 0; i < mesh->MNumVertices; i++)
            {
                var vector = new Vector3D<float>();
                vector.X = mesh->MVertices[i].X;
                vector.Y = mesh->MVertices[i].Y;
                vector.Z = mesh->MVertices[i].Z;
                positions.Add(vector);

                var normalVector = new Vector3D<float>();
                normalVector.X = mesh->MNormals[i].X;
                normalVector.Y = mesh->MNormals[i].Y;
                normalVector.Z = mesh->MNormals[i].Z;
                normals.Add(normalVector);
            }

            //Iterate over the faces of the mesh
            for (int j = 0; j < mesh->MNumFaces; ++j)
            {
                //Get the face
                var face = mesh->MFaces[j];
                //Add the indices of the face to the vector
                for (int k = 0; k < face.MNumIndices; ++k)
                {
                    indicies.Add((ushort)face.MIndices[k]);
                }
            }

            hashtable.Add("positions", positions.ToArray());
            hashtable.Add("normals", normals.ToArray());
            hashtable.Add("indicies", indicies.ToArray());

            return hashtable;
        }

        private static unsafe void processNode(Silk.NET.Assimp.Node* node, Silk.NET.Assimp.Scene* scene, ref List<Hashtable> meshes)
        {
            for (int i = 0; i < node->MNumMeshes; i++)
            {
                //processes all the nodes meshes
                var nodeMesh = node->MMeshes[i];
                var mesh = scene->MMeshes[nodeMesh];
                meshes.Add(processMesh(mesh));
            }

            for (int i = 0; i < node->MNumChildren; i++)
            {
                processNode(node->MChildren[i], scene, ref meshes);
            }
        }
    }
}
