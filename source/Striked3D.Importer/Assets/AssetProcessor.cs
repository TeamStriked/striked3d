using Striked3D.Core.AssetsPrimitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Striked3D.Core.Assets
{
    public class AssetProcessor
    {
        private static readonly Dictionary<string, BinaryAssetProcessor> s_assetProcessors = GetAssetProcessors();
        private static readonly Dictionary<Type, BinaryAssetSerializer> s_assetSerializers = DefaultSerializers.Get();
        private static Dictionary<string, BinaryAssetProcessor> GetAssetProcessors()
        {
            ImageSharpProcessor texProcessor = new ImageSharpProcessor();
            AssimpProcessor assimpProcessor = new AssimpProcessor();

            return new Dictionary<string, BinaryAssetProcessor>()
            {
                { ".png", texProcessor },
                { ".dae", assimpProcessor },
                { ".obj", assimpProcessor },
            };
        }

        public static T Load<T>(string path) where T: class
        {
            string extension = Path.GetExtension(path);

            if (!s_assetProcessors.TryGetValue(extension, out BinaryAssetProcessor processor))
            {
                return null;
            }

            object processedAsset;
            using (FileStream fs = File.OpenRead(path))
            {
                processedAsset = processor.Process(fs, extension);
            }

            return processedAsset as T;
        }
    }
}
