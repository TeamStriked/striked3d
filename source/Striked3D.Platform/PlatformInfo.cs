using System.Diagnostics;
using Veldrid;

namespace Striked3D.Platform
{
    public static class PlatformInfo
    {
        public const GraphicsBackend preferredBackend = GraphicsBackend.Vulkan;
        public const bool DebugRendering = true;

        public static string ApplicationDir => System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        public static string SystemAssetDir
        {
            get
            {
                string? dir = System.IO.Path.Combine(ApplicationDir, "Assets");
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                return dir;
            }
        }

    }
}
