using Striked3D.Resources;
using Veldrid;

namespace Striked3D.Nodes
{
    public class ListViewItem : Control
    {

        private float _FontSize = 13;
        private string _Content = "";
        private string _Key = "";

        private RgbaFloat _Color = new RgbaFloat(1, 0, 0, 1);
        private RgbaFloat _Background = new RgbaFloat(1, 0, 0, 1);
        private RgbaFloat _BackgroundHover = new RgbaFloat(0, 1, 0, 1);
        private RgbaFloat _ColorHover = new RgbaFloat(0, 1, 0, 1);

        private Font _Font = Font.SystemFont;
        public Font Font
        {
            get => _Font;
            set
            {
                SetProperty("Font", ref _Font, value);
                UpdateSizes();
            }
        }

        public RgbaFloat Color
        {
            get => _Color;
            set
            {
                RgbaFloat origValue = _Color;
                SetProperty("Color", ref _Color, value);
                if (!origValue.Equals(value))
                {
                    UpdateCanvas();
                }
            }
        }

        public RgbaFloat Background
        {
            get => _Background;
            set
            {
                RgbaFloat origValue = _Background;
                SetProperty("Background", ref _Background, value);

                if (!origValue.Equals(value))
                {
                    UpdateCanvas();
                }
            }
        }

        public RgbaFloat BackgroundHover
        {
            get => _BackgroundHover;
            set
            {
                RgbaFloat origValue = _BackgroundHover;
                SetProperty("BackgroundHover", ref _BackgroundHover, value);
                if (!origValue.Equals(value))
                {
                    UpdateCanvas();
                }
            }
        }

        public RgbaFloat ColorHover
        {
            get => _ColorHover;
            set
            {
                RgbaFloat origValue = _ColorHover;
                SetProperty("ColorHover", ref _ColorHover, value);
                UpdateCanvas();
                if (!origValue.Equals(value))
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
                float origValue = _FontSize;
                SetProperty("FontSize", ref _FontSize, value);

                if (!origValue.Equals(value))
                {
                    UpdateCanvas();
                }
            }
        }

        public string Content
        {
            get => _Content;
            set
            {
                string origValue = _Content;
                SetProperty("Content", ref _Content, value);
                if (!origValue.Equals(value))
                {
                    UpdateSizes();
                }
            }
        }

        public string Key
        {
            get => _Key;
            set => SetProperty("Key", ref _Key, value);
        }

        public ListViewItem() : base()
        {
            OnHover += () => { UpdateCanvas(); };
            OnHoverLeave += () => { UpdateCanvas(); };
        }

        public override void DrawCanvas()
        {
            RgbaFloat bgColor = isHover ? BackgroundHover : Background;

            if (bgColor.A > 0)
            {
                DrawRect(bgColor, ScreenPosition, ScreenSize);
            }

            RgbaFloat foregroundColor = isHover ? ColorHover : Color;
            if (foregroundColor.A > 0)
            {
                DrawText(Font, foregroundColor, ScreenPosition, FontSize, Content);
            }
            else
            {
                return;
            }

            _screenSize.Y = FontSize;
        }

    }
}
