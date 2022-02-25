using Striked3D.Core.Interfaces;

namespace Striked3D.Importer
{
    public abstract class ImportProcessor
    {
        public abstract string OutputExtension { get; }
        public abstract object Import(string filePath);
    }
    public abstract class ImportProcessor<T> : ImportProcessor where T : ISerializable
    {
        public override object Import(string filePath)
        {
            return DoImport(filePath);
        }

        public abstract T DoImport(string filePath);
    }
}
