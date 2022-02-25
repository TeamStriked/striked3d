using BinaryPack;
using Msdfgen;
using Msdfgen.IO;
using Striked3D.Types;
using Striked3D.Core;
using Striked3D.Core.Interfaces;
using Striked3D.Core.Reference;
using Striked3D.Core.Window;
using Striked3D.Resources;
using Striked3D.Types;
using Striked3D.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Striked3D.Importer
{
    public enum ImporterState
    {
        OK,
        FAILED
    }

    public  class Importer : IService
    {
        private static Queue<Task> taskQueue = new Queue<Task>();

        private Dictionary<string, ImportProcessor> extensions = new();

        public Importer()
        {
            extensions.Add(".ttf", new FontImporter());
        }

        public ImporterState ImportFile<T>(string inputPath, string outputPath, string fileName, bool reImport = false) where T : ISerializable
        {
            var extension = Path.GetExtension(inputPath);
            if (!extensions.ContainsKey(extension))
            {
                throw new Exception("Cant find extension " + extension + " in loader list");
            }

            var loader = extensions[extension];
            var writePath = System.IO.Path.Combine(outputPath, fileName + loader.OutputExtension);

            try
            {
                if (File.Exists(writePath))
                {
                    if(reImport)
                    {
                        System.IO.File.Delete(writePath);
                    }
                    else
                    {
                        return ImporterState.OK;
                    }
                }

                Logger.Debug(this, "Start parse " + inputPath + " to " + writePath);

                T result = (T) loader.Import(inputPath);
                var byteArray = result.Serialize();

                File.WriteAllBytes(writePath, byteArray);

                Logger.Debug(this, "Write to " + writePath);
                result = default;
                return ImporterState.OK;
            }
            catch (Exception ex)
            {
                Logger.Error(inputPath + " => " + ex.Message, ex.StackTrace);
            }

            return ImporterState.FAILED;
        }


        public void ImportFileAsync<T>(string inputPath, string outputPath, string fileName, bool reImport = false, Action<ImporterState> onFinish = null) where T : ISerializable
        {
            var task = new Task<ImporterState>(() => {
                var result =  this.ImportFile<T>(inputPath, outputPath, fileName, reImport);
                return result;
            });

            task.Start();
        }
        public void Update(double delta)
        {
        }

        public void Render(double delta)
        {
        }

        public void Register(IWindow window)
        {

           
        }

        public void Unregister()
        {
        }
    }
}
