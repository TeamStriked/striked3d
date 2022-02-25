using Striked3D.Core.Reference;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Striked3D.Loader
{
    public abstract class LoaderProcessor
    {
        public abstract object Load(string filePath);
    }
    public abstract class LoaderProcessor<T> : LoaderProcessor
    {
        public override object Load(string filePath) => DoLoad(filePath);
        public abstract T DoLoad(string filePath);
    }
}
