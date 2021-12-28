using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;

namespace Striked3D.Assets
{
    public static class ResourceReader
    {
        public static string GetEmbeddedResourceFile(string filename)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string ns = typeof(ResourceReader).Namespace;
            string name = String.Format("{0}.{1}", ns, filename);
            using (var s = assembly.GetManifestResourceStream(name))
            using (var r = new System.IO.StreamReader(s))
            {
                string result = r.ReadToEnd();
                return result;
            }
        }
    }
}
