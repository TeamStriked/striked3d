using Striked3D.Core;
using Striked3D.Core.Interfaces;
using Striked3D.Core.Reference;
using Striked3D.Core.Window;
using Striked3D.Loader;
using Striked3D.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Striked3D
{
    public enum ResourceLoaderState
    {
        OK,
        FAILED
    }

    public class ResourceLoader : IService
    {
        private Dictionary<string, object> cache = new Dictionary<string, object>();

        public ResourceLoader()
        {

        }

        public T Load<T>(string filepath) where T: ISerializable
        {
            if (cache.ContainsKey(filepath))
            {
                return (T) cache[filepath];
            }
            try
            {
                if (!File.Exists(filepath))
                {
                   return default(T);
                }

                var bytes = File.ReadAllBytes(filepath);

                T newNode = Activator.CreateInstance<T>();
                newNode.Deserialize(bytes);

                this.cache.Add(filepath, newNode);
                return newNode;
            }
            catch (Exception ex)
            {
                Logger.Error(filepath + " => " + ex.Message, ex.StackTrace);
            }

            return default(T);
        }

        public void Register(IWindow window)
        {
        }

        public void Render(double delta)
        {
        }

        public void Unregister()
        {
        }

        public void Update(double delta)
        {
        }
    }

}
