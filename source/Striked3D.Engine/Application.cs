using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Striked3D.Engine
{
    public static class EngineInfo
    {
        public static string GetRunningDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
        public static string GetShaderDictonary()
        {
            return System.IO.Path.Combine(GetRunningDirectory(), "Shaders");
        }
        public static string GetAssetsDictonary()
        {
            return System.IO.Path.Combine(GetRunningDirectory(), "Assets");
        }

        public static string GetUserDir()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string specificFolder = Path.Combine(appDataFolder, "Striked3D");

            if(!Directory.Exists(specificFolder))
            {
                Directory.CreateDirectory(specificFolder);
            }

            return specificFolder;
        }
    }
}
