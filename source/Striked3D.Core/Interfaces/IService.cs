namespace Striked3D.Core
{
    public interface IService
    {
        public void Update(double delta);
        public void Render(double delta);
        public void Register(Core.Window.IWindow window);
        public void Unregister();

    }
}
