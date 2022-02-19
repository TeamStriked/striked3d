
using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Core.Input;
using Striked3D.Helpers;
using Striked3D.Resources;
using System;
using System.Threading.Tasks;
using Veldrid;

namespace Striked3D.Nodes
{
    public class TextBox : Control
    {
        public const double simulateWaitKeyBackspaceTime = 0.1;

        private float _FontSize = 13;
        private float _borderThickness = 1;
        private string _Content = "";
        private string _Key = "";


        private double deltaBlinkingTime = 0;
        private double backspaceWaitTimeDelta = 0;

        private string newInput = null;

        private RgbaFloat _Color = new (1, 1, 1, 1);
        private RgbaFloat _Background = RGBHelper.FromHex("#292a2f");

        private RgbaFloat _BackgroundHover = RGBHelper.FromHex("#212226");
        private RgbaFloat _ColorHover = new (1, 1, 1, 1);

        private RgbaFloat _BorderColor = RGBHelper.FromHex("#303338");

        private Font _Font = Font.SystemFont;

        private bool recreateCursor = false;
        private InputService inputService;

        public delegate void OnInputChangeHandler(string text);
        public event OnInputChangeHandler OnChange;

        public Font Font
        {
            get { return _Font; }
            set { SetProperty("Font", ref _Font, value); this.UpdateSizes(); }
        }

        public RgbaFloat BorderColor
        {
            get { return _BorderColor; }
            set
            {
                var orig = _BorderColor;
                SetProperty("BorderColor", ref _BorderColor, value);
                if (orig != value)
                    this.UpdateCanvas();
            }
        }
        public float BorderThickness
        {
            get { return _borderThickness; }
            set
            {
                var orig = _borderThickness;
                SetProperty("BorderThickness", ref _borderThickness, value);
                if (orig != value)
                    this.UpdateCanvas();
            }
        }

        public RgbaFloat Color
        {
            get { return _Color; }
            set
            {
                var orig = _Color;
                SetProperty("Color", ref _Color, value);
                if (orig != value)
                    this.UpdateCanvas();
            }
        }

        public RgbaFloat Background
        {
            get { return _Background; }
            set
            {
                var orig = _Background;
                SetProperty("Background", ref _Background, value);
                if (orig != value)
                    this.UpdateCanvas();
            }
        }

        public RgbaFloat BackgroundHover
        {
            get { return _BackgroundHover; }
            set
            {
                var orig = _BackgroundHover;
                SetProperty("BackgroundHover", ref _BackgroundHover, value);
                if (orig != value)
                    this.UpdateCanvas();
            }
        }

        public RgbaFloat ColorHover
        {
            get { return _ColorHover; }
            set
            {
                var orig = _ColorHover;
                SetProperty("ColorHover", ref _ColorHover, value);
                if (orig != value)
                    this.UpdateCanvas();
            }
        }

        public float FontSize
        {
            get { return _FontSize; }
            set { SetProperty("FontSize", ref _FontSize, value); this.UpdateSizes(); }
        }

        public string Content
        {
            get { return _Content; }
            set { 
                var orig = _Content;
                SetProperty("Content", ref _Content, value);
                if (orig != value)
                    this.UpdateSizes(); 
            }
        }

        public string Key
        {
            get { return _Key; }
            set { SetProperty("Key", ref _Key, value); }
        }

        public string GetContentAsString()
        {
            return this.Content;
        }

        public override void OnEnterTree()
        {
            base.OnEnterTree();
            inputService = Root.Services.Get<InputService>();   
        }
        public TextBox() : base()
        {
            OnHover += () => this.DrawCanvas();
            OnHoverLeave += () => this.DrawCanvas();
            OnFocus += () => this.DrawCanvas();
            OnFocusLeave += () => this.DrawCanvas();
        }
        /*
        private void drawBlinker( RgbaFloat foregroundColor)
        {
            if (isFocused)
            {
                var start = ScreenPosition;
                start.X += fontMeasure.Size.Width + CursorSpace;

                var end = ScreenPosition;
                end.X += fontMeasure.Size.Width + CursorSpace;
                end.Y += ScreenSize.Y;

                this.DrawLine( foregroundColor, start, end, 1.0f);
            }
        }
        */
        bool isBlinking = true;

        public override void DrawCanvas()
        {
            var pos = ScreenPosition;

            pos.Y +=  this.Padding.Y;
            pos.X += this.Padding.X;

            var bgColor = isHover ? BackgroundHover : Background;
            if (bgColor.A != 0)
            {
                this.DrawRect(bgColor, ScreenPosition, ScreenSize);
               // this.DrawRectBorder( _BorderColor, ScreenPosition, ScreenPostionEnd, BorderThickness);
            }

            var foregroundColor = isHover ? ColorHover : Color;
            if (foregroundColor.A != 0)
            {
                this.DrawText(_Font, foregroundColor, pos, FontSize, Content); 

                if (isFocused)
                {
                    // drawBlinker(foregroundColor);
                }
            }

            this._screenSize.Y = FontSize + this.Padding.Y + this.Padding.Z;
        }

        public override void Update(double delta)
        {
            base.Update(delta);

            if (isHover)
            {
                inputService?.SetCursor(Silk.NET.Input.StandardCursor.IBeam);
                recreateCursor = true;
            }
            else if (recreateCursor)
            {
                inputService?.SetCursor(Silk.NET.Input.StandardCursor.Default);
                recreateCursor = false;
            }

            if (!isFocused)
            {
                if(this.newInput != null)
                {
                    OnChange?.Invoke(this.newInput);
                    this.newInput = null;
                }

                return;
            }

            if (backspaceWaitTimeDelta >= simulateWaitKeyBackspaceTime)
                backspaceWaitTimeDelta = 0;

            if (inputService.IsKeyPressed(Silk.NET.Input.Key.Backspace) && isFocused && backspaceWaitTimeDelta == 0)
            {
                string result = Content;
                if (result != null && result.Length > 0)
                {
                    result = result.Remove(result.Length - 1);
                    this.newInput = result;
                }

                Content = result;
            }

            backspaceWaitTimeDelta += delta;

            //blinker
            deltaBlinkingTime += delta;

            if (deltaBlinkingTime >= 0.5)
            {
                isBlinking = false;
            }
            if (deltaBlinkingTime >= 1.0)
            {
                deltaBlinkingTime = 0;
                isBlinking = true;
            }
        }

        public override void OnInput(InputEvent ev)
        {
            base.OnInput(ev);

            if (!isFocused)
                return;

            if (ev is KeyCharEvent )
            {
                var keyInput = (ev as KeyCharEvent);
                Content += keyInput.Char;
                this.newInput = Content;
            }

            if (ev is KeyInputEvent)
            {
                var keyInput = (ev as KeyInputEvent);

                if (keyInput.Key == Silk.NET.Input.Key.Backspace)
                {
                    backspaceWaitTimeDelta = 0;
                }
                if (keyInput.Key == Silk.NET.Input.Key.Enter && this.isFocused)
                {
                    this.SetFocus(false);
                }
            }
        }
    }
}
