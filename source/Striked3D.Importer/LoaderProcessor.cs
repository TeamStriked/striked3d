namespace Striked3D.Loader
{
    public abstract class LoaderProcessor
    {
        public abstract object Load(string filePath);
    }
    public abstract class LoaderProcessor<T> : LoaderProcessor
    {
        public override object Load(string filePath)
        {
            return DoLoad(filePath);
        }

        public abstract T DoLoad(string filePath);
    }
}
