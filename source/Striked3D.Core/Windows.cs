using Striked3D.Core;
using Striked3D.Types;
using System;
using System.Collections.Generic;

namespace Striked3D.Core
{
    public static class Windows
    {
        public static Dictionary<string, Window> All = new Dictionary<string, Window>();
        public static Window createWindow(string name, string title, Vector2D<int> size)
        {
            var window = new Window(title, size);
            All.Add(name, window); 

            return window;
        }
    }
}
