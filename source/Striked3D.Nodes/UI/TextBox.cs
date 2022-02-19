using Striked3D.Core.Input;
using Striked3D.Helpers;
using Striked3D.Resources;
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

        private RgbaFloat _Color = new(1, 1, 1, 1);
        private RgbaFloat _Background = RGBHelper.FromHex("#292a2f");

        private RgbaFloat _BackgroundHover = RGBHelper.FromHex("#212226");
        private RgbaFloat _ColorHover = new(1, 1, 1, 1);

        private RgbaFloat _BorderColor = RGBHelper.FromHex("#303338");

        private Font _Font = Font.SystemFont;

        private bool recreateCursor = false;
        private InputService inputService;

        public delegate void OnInputChangeHandler(string text);
        public event OnInputChangeHandler OnChange;

        public Font Font
        {
            get => _Font;
            set { SetProperty("Font", ref _Font, value); UpdateSizes(); }
        }

        public RgbaFloat BorderColor
        {
            get => _BorderColor;
            set
            {
                RgbaFloat orig = _BorderColor;
                SetProperty("BorderColor", ref _BorderColor, value);
                if (orig != value)
                {
                    UpdateCanvas();
                }
            }
        }
        public float BorderThickness
        {
            get => _borderThickness;
            set
            {
                float orig = _borderThickness;
                SetProperty("BorderThickness", ref _borderThickness, value);
                if (orig != value)
                {
                    UpdateCanvas();
                }
            }
        }

        public RgbaFloat Color
        {
            get => _Color;
            set
            {
                RgbaFloat orig = _Color;
                SetProperty("Color", ref _Color, value);
                if (orig != value)
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
                RgbaFloat orig = _Background;
                SetProperty("Background", ref _Background, value);
                if (orig != value)
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
                RgbaFloat orig = _BackgroundHover;
                SetProperty("BackgroundHover", ref _BackgroundHover, value);
                if (orig != value)
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
                RgbaFloat orig = _ColorHover;
                SetProperty("ColorHover", ref _ColorHover, value);
                if (orig != value)
                {
                    UpdateCanvas();
                }
            }
        }

        public float FontSize
        {
            get => _FontSize;
            set { SetProperty("FontSize", ref _FontSize, value); UpdateSizes(); }
        }

        public string Content
        {
            get => _Content;
            set
            {
                string orig = _Content;
                SetProperty("Content", ref _Content, value);
                if (orig != value)
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

        public string GetContentAsString()
        {
            return Content;
        }

        public override void OnEnterTree()
        {
            base.OnEnterTree();
            inputService = Root.Services.Get<InputService>();
        }
        public TextBox() : base()
        {
            OnHover += () => DrawCanvas();
            OnHoverLeave += () => DrawCanvas();
            OnFocus += () => DrawCanvas();
            OnFocusLeave += () => DrawCanvas();
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
        private bool isBlinking = true;

        public override void DrawCanvas()
        {
            Silk.NET.Maths.Vector2D<float> pos = ScreenPosition;

            pos.Y += Padding.Y;
            pos.X += Padding.X;

            RgbaFloat bgColor = isHover ? BackgroundHover : Background;
            if (bgColor.A != 0)
            {
                DrawRect(bgColor, ScreenPosition, ScreenSize);
                // this.DrawRectBorder( _BorderColor, ScreenPosition, ScreenPostionEnd, BorderThickness);
            }

            RgbaFloat foregroundColor = isHover ? ColorHover : Color;
            if (foregroundColor.A != 0)
            {
                DrawText(_Font, foregroundColor, pos, FontSize, Content);

                if (isFocused)
                {
                    // drawBlinker(foregroundColor);
                }
            }

            _screenSize.Y = FontSize + Padding.Y + Padding.Z;
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
                if (newInput != null)
                {
                    OnChange?.Invoke(newInput);
                    newInput = null;
                }

                return;
            }

            if (backspaceWaitTimeDelta >= simulateWaitKeyBackspaceTime)
            {
                backspaceWaitTimeDelta = 0;
            }

            if (inputService.IsKeyPressed(Silk.NET.Input.Key.Backspace) && isFocused && backspaceWaitTimeDelta == 0)
            {
                string result = Content;
                if (result != null && result.Length > 0)
                {
                    result = result.Remove(result.Length - 1);
                    newInput = result;
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
            {
                return;
            }

            if (ev is KeyCharEvent)
            {
                KeyCharEvent keyInput = (ev as KeyCharEvent);
                Content += keyInput.Char;
                newInput = Content;
            }

            if (ev is KeyInputEvent)
            {
                KeyInputEvent keyInput = (ev as KeyInputEvent);

                if (keyInput.Key == Silk.NET.Input.Key.Backspace)
                {
                    backspaceWaitTimeDelta = 0;
                }
                if (keyInput.Key == Silk.NET.Input.Key.Enter && isFocused)
                {
                    SetFocus(false);
                }
            }
        }
    }
}
