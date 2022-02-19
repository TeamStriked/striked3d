using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Resources;
using System;
using System.Threading.Tasks;
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
            get { return _Font; }
            set { SetProperty("Font", ref _Font, value); this.UpdateSizes(); }
        }
        public RgbaFloat Color
        {
            get { return _Color; }
            set
            {
                var orig = _Color;
                SetProperty("Color", ref _Color, value);
                if (!orig.Equals(value))
                {
                    this.UpdateCanvas();
                }
            }
        }

        public float FontSize
        {
            get {  return _FontSize; }
            set
            {
                var orig = _FontSize; 
                SetProperty("FontSize", ref _FontSize, value);
                if (!orig.Equals(value))
                {
                    this.UpdateSizes(); 
                }
            }
        }


        public Resources.FontAlign FontAlign
        {
            get { return _FontAlign; }
            set
            {
                var orig = _FontAlign;
                SetProperty("FontAlign", ref _FontAlign, value);
                if (!orig.Equals(value))
                {
                    this.UpdateSizes();
                }
            }
        }

        public string Content
        {
            get { return _Content; }
            set { 
                var orig = _Content;
                SetProperty("Content", ref _Content, value); 
                if(!orig.Equals(value))
                {
                    this.UpdateSizes();
                }
            }
        }

        public override void DrawCanvas()
        {
            var pos = ScreenPosition;
            pos.Y += this.Padding.Y;
            pos.X += this.Padding.X;

            this.DrawText(this.Font, Color, pos, FontSize, Content);

            this._screenSize.Y = FontSize + this.Padding.Y + this.Padding.Z;
            this._screenSize.X = Content.Length * FontSize;
        }

    }
}
