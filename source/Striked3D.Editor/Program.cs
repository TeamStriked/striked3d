using Striked3D.Core;
using Striked3D.Nodes;
using Striked3D.Servers;
using System;
using System.Threading;

namespace Striked3D.Editor
{
    internal class Program
    {
        public static Window EditorWindow { get; set; }
        static void Main(string[] args)
        {
            var EditorWindow = WindowServer.createWindow("test", new Types.Vector2D<int>(800, 600));
            Logger.Debug( "Start engine..");
         
            EditorWindow.RegisterService<Servers.TextServer>();
            EditorWindow.RegisterService<Servers.RenderingServer>();
            EditorWindow.RegisterService<Servers.InputServer>();
            EditorWindow.RegisterService<Servers.CameraServer>();

            EditorWindow.OnLoad += () =>
            {
                EditorWindow.Tree.CreateNode<Camera>();
                EditorWindow.Tree.CreateNode<MeshInstance>();
                EditorWindow.Tree.CreateNode<Grid>();

            };

            Console.ReadLine();
            WindowServer.CloseAll();
        }
    }
}
