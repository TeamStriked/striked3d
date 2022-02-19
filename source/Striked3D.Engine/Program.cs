﻿using Striked3D.Core.Input;
using Striked3D.Core.Window;
using Striked3D.Nodes;
using Striked3D.Services;
using System.Diagnostics;
using System.Threading;

namespace Striked3D.Editor
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityBoostEnabled = true;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            RootWindow win = new RootWindow();
            win.Services.Register<InputService>();
            win.Services.Register<GraphicService>();
            win.Services.Register<ScreneTreeService>();

            win.OnLoad += () =>
            {

                Nodes.Editor editor = new Nodes.Editor();
                win.RootViewport.AddChild(editor);

                EditorGrid grid = new EditorGrid();
                Camera3D camera = new Camera3D();
                editor.EditorViewport.AddChild(camera);
                camera.IsActive = true;


                /* var mesh = new MeshInstance3D();
              var mat = new Material3D(); 
              var surface = Core.Assets.AssetProcessor.Load<Mesh>("Assets\\Models\\monkey_smooth.obj");
              mesh.Surface = surface;
              mesh.Material = mat;
                                 editor.EditorViewport.AddChild(mesh);

                */
                editor.EditorViewport.AddChild(grid);


            };

            win.Run();

        }
    }
}