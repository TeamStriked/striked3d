using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Core.Input;
using Striked3D.Resources;
using System;
using System.Threading.Tasks;
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
            get { return _Font; }
            set { 
                SetProperty("Font", ref _Font, value); 
                this.UpdateSizes(); 
            }
        }

        public RgbaFloat Color
        {
            get { return _Color; }
            set
            {
                var origValue = _Color;
                SetProperty("Color", ref _Color, value);
                if(!origValue.Equals(value))
                {
                    this.UpdateCanvas();
                }
            }
        }

        public RgbaFloat Background
        {
            get { return _Background; }
            set
            {
                var origValue = _Background;
                SetProperty("Background", ref _Background, value);

                if (!origValue.Equals(value))
                {
                    this.UpdateCanvas();
                }
            }
        }

        public RgbaFloat BackgroundHover
        {
            get { return _BackgroundHover; }
            set
            {
                var origValue = _BackgroundHover;
                SetProperty("BackgroundHover", ref _BackgroundHover, value);
                if (!origValue.Equals(value))
                {
                    this.UpdateCanvas();
                }
            }
        }

        public RgbaFloat ColorHover
        {
            get { return _ColorHover; }
            set
            {
                var origValue = _ColorHover;
                SetProperty("ColorHover", ref _ColorHover, value);
                this.UpdateCanvas();
                if (!origValue.Equals(value))
                {
                    this.UpdateCanvas();
                }
            }
        }

        public float FontSize
        {
            get { return _FontSize; }
            set { 
                var origValue = _FontSize; 
                SetProperty("FontSize", ref _FontSize, value);

                if (!origValue.Equals(value))
                {
                    this.DrawCanvas();
                }
            }
        }

        public string Content
        {
            get { return _Content; }
            set
            {
                var origValue = _Content; 
                SetProperty("Content", ref _Content, value); 
                if(!origValue.Equals(value))
                {
                    this.UpdateSizes();
                }
            }
        }

        public string Key
        {
            get { return _Key; }
            set { SetProperty("Key", ref _Key, value); }
        }

        public  ListViewItem() : base(){
            this.OnHover += () => {  this.UpdateCanvas(); };
            this.OnHoverLeave += () => {  this.UpdateCanvas(); };
        }

        public override void DrawCanvas()
        {

            var bgColor = isHover ? BackgroundHover : Background;

            if (bgColor.A > 0)
            {
                this.DrawRect( bgColor, ScreenPosition, ScreenSize);
            }

            var foregroundColor = isHover ? ColorHover : Color;
            if (foregroundColor.A > 0)
            {
                 this.DrawText(this.Font, foregroundColor, ScreenPosition,  FontSize, Content);
            }
            else
            {
                return;
            }

            this._screenSize.Y = FontSize;
        }

    }
}
