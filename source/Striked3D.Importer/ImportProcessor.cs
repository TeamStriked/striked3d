using Striked3D.Core;
using Striked3D.Core.Interfaces;
using Striked3D.Core.Reference;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Importer
{
    public abstract class ImportProcessor
    {
        public abstract string OutputExtension { get; }
        public abstract object Import(string filePath);
    }
    public abstract class ImportProcessor<T> : ImportProcessor where T : ISerializable
    {
        public override object Import(string filePath) => DoImport(filePath);
        public abstract T DoImport(string filePath);
    }
}
