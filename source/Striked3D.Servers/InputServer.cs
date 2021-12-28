using Silk.NET.Input;
using Striked3D.Core;
using System;
using System.Numerics;
using System.Linq;

namespace Striked3D.Servers
{
    public partial class InputServer : Server
    {
        public override int Priority
        {
            get
            {
                return 0;
            }
        }

        public override ServerType RunType
        {
            get
            {
                return ServerType.None;
            }
        }

        public InputServer() : base(new InputServerThread())
        {

        }
        protected override void Deregister()
        {
            Logger.Debug(this, "Deregister camera server");
        }

        public Vector2 workSize = Vector2.Zero;
        public Vector2 workPositon = Vector2.Zero;
        public IInputContext input { get; set; }
        public IKeyboard PrimaryKeyboard { get; set; }
        public IMouse PrimaryMouse { get; set; }

        public delegate void InputEventHandler(InputEvent e);
        public event InputEventHandler OnInput = delegate { };

        protected override void Loop(double delta)
        {
        }

        protected override void Register()
        {
            input = this.serverWindow.NativeWindow.CreateInput();

            PrimaryMouse = input.Mice.FirstOrDefault();
            PrimaryKeyboard = input.Keyboards.FirstOrDefault();

            PrimaryKeyboard.KeyUp += (IKeyboard keyboard, Key key, int some) => {
            };

            PrimaryMouse.MouseMove += (IMouse mouse, Vector2 pos) => {
                var res = new MouseInputEvent { Position = new Types.Vector2D<float>(pos.X, pos.Y) };
                serverWindow.Tree.ForwardInput(res);
            };

            PrimaryMouse.Scroll += (IMouse mouse, ScrollWheel wheel) => {
                var res = new MouseInputWheelEvent { Position = new Types.Vector2D<float>(wheel.X, wheel.Y) };
                serverWindow.Tree.ForwardInput(res);
            };

            PrimaryMouse.MouseUp += (IMouse mouse, MouseButton button) => {

                var res = new MouseButtonEvent { Button = button, IsUp = true };
                serverWindow.Tree.ForwardInput(res);
            };

            PrimaryMouse.MouseDown += (IMouse mouse, MouseButton button) => {

                var res = new MouseButtonEvent { Button = button, IsUp = false };
                serverWindow.Tree.ForwardInput(res);
            };
        }
    }
}
