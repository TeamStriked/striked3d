using Striked3D.Resources;
using Veldrid;

namespace Striked3D.Nodes
{
    public class Label : Control
    {
        private float _FontSize = 13;
        private Resources.FontAlign _FontAlign = Resources.FontAlign.Left;
        private string _Content = "";

        private RgbaFloat _Color = new RgbaFloat(1, 1, 1, 1);
        private Font _Font = Font.SystemFont;

        public Font Font
        {
            get => _Font;
            set { SetProperty("Font", ref _Font, value); UpdateSizes(); }
        }
        public RgbaFloat Color
        {
            get => _Color;
            set
            {
                RgbaFloat orig = _Color;
                SetProperty("Color", ref _Color, value);
                if (!orig.Equals(value))
                {
                    UpdateCanvas();
                }
            }
        }

        public float FontSize
        {
            get => _FontSize;
            set
            {
                float orig = _FontSize;
                SetProperty("FontSize", ref _FontSize, value);
                if (!orig.Equals(value))
                {
                    UpdateSizes();
                }
            }
        }


        public Resources.FontAlign FontAlign
        {
            get => _FontAlign;
            set
            {
                FontAlign orig = _FontAlign;
                SetProperty("FontAlign", ref _FontAlign, value);
                if (!orig.Equals(value))
                {
                    UpdateSizes();
                }
            }
        }

        public string Content
        {
            get => _Content;
            set
            {
                string orig = _Content;
                SetProperty("Content", ref _Content, value);
                if (!orig.Equals(value))
                {
                    UpdateSizes();
                }
            }
        }

        public override void DrawCanvas()
        {
            Silk.NET.Maths.Vector2D<float> pos = ScreenPosition;
            pos.Y += Padding.Y;
            pos.X += Padding.X;

            DrawText(Font, Color, pos, FontSize, Content);

            _screenSize.Y = FontSize + Padding.Y + Padding.Z;
            _screenSize.X = Content.Length * FontSize;
        }

    }
}
