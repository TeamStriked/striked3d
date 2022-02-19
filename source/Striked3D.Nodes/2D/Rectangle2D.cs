using Veldrid;

namespace Striked3D.Nodes
{
    public class Rectangle2D : Control
    {
        private RgbaFloat _Color = new RgbaFloat(1, 0, 0, 1);

        public RgbaFloat Color
        {
            get => _Color;
            set
            {
                SetProperty("Color", ref _Color, value);
                UpdateCanvas();
            }
        }

        public override void DrawCanvas()
        {
            DrawRect(Color, ScreenPosition, ScreenSize);
        }
    }
}
