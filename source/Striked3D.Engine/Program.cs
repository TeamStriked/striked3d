using System.Diagnostics;
using System.Threading;
using Striked3D.Core.Input;
using Striked3D.Core.Window;
using Striked3D.Importer;
using Striked3D.Nodes;
using Striked3D.Resources;
using Striked3D.Services;
using System;
using CommandLine;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Striked3D.Engine
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //set process to high priority
            Process.GetCurrentProcess().PriorityBoostEnabled = true;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            Parser.Default.ParseArguments<EditorArguments>(args)
            .WithParsed<EditorArguments>(o =>
            {
                if (String.IsNullOrEmpty(o.ProjectPath))
                {
                    throw new Exception("Please select a project path.");
                }

                Striked3D.Platform.ProjectManager.LoadProject(o.ProjectPath);

                RootWindow win = new RootWindow();
                win.IsDebug = o.Debug;
                if (win.IsDebug)
                {
                    Console.WriteLine("Start application in debug mode.");
                }

                win.Services.Register<Importer.Importer>();
                win.Services.Register<ResourceLoader>();
                win.Services.Register<InputService>();
                win.Services.Register<GraphicService>();
                win.Services.Register<ScreneTreeService>();

                var embbededFolderIcon = GetResourceStream("Striked3D.Engine.Resources.Images.folder.png");
                win.Services.Get<Importer.Importer>().ImportFile<BitmapTexture>(embbededFolderIcon, ".png", Platform.PlatformInfo.SystemAssetDir, "FolderIcon");

                var embeddedFont = GetResourceStream("Striked3D.Engine.Resources.Fonts.OpenSans-Regular.ttf");
                win.Services.Get<Importer.Importer>().ImportFile<Font>(embeddedFont, ".ttf", Platform.PlatformInfo.SystemAssetDir, "SystemFont");

                Font? font = win.Services.Get<ResourceLoader>().Load<Font>(System.IO.Path.Combine(Platform.PlatformInfo.SystemAssetDir, "SystemFont.stf"));
                BitmapTexture? folderTexture = win.Services.Get<ResourceLoader>().Load<BitmapTexture>(System.IO.Path.Combine(Platform.PlatformInfo.SystemAssetDir, "FolderIcon.stb"));

                Nodes.Editor.Theme.Font = font;
                Nodes.Editor.Theme.FolderIcon = folderTexture;
                
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

            });
        }
        public static byte[]  GetResourceStream(string filename)
        {
            var thisAssembly = Assembly.GetExecutingAssembly();
            using (var stream = thisAssembly.GetManifestResourceStream(filename))
            {
                if (stream == null)
                    return null;

                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
