using Silk.NET.Input;
using Striked3D.Core.Window;
using Striked3D.Math;
using Striked3D.Services;
using Striked3D.Types;
using System.Linq;
using System.Numerics;

namespace Striked3D.Core.Input
{
    public class InputService : IService
    {
        private ServiceRegistry _registry;

        public IInputContext input { get; set; }
        public IKeyboard PrimaryKeyboard { get; set; }
        public IMouse PrimaryMouse { get; set; }

        public delegate void InputEventHandler(InputEvent e);
        public event InputEventHandler OnInput = delegate { };

        public void Register(IWindow window)
        {
            _registry = window.Services;

            input = window.CreateInput();

            PrimaryMouse = input.Mice.FirstOrDefault();
            PrimaryKeyboard = input.Keyboards.FirstOrDefault();

            PrimaryKeyboard.KeyChar += (s, c) =>
            {
                KeyCharEvent res = new KeyCharEvent { Char = c };
                ForwardInput(res);
            };

            PrimaryKeyboard.KeyUp += (IKeyboard keyboard, Key key, int some) =>
            {
                KeyInputEvent res = new KeyInputEvent { Key = key, KeyIndex = some, IsUp = true };
                ForwardInput(res);
            };

            PrimaryKeyboard.KeyDown += (IKeyboard keyboard, Key key, int some) =>
            {
                KeyInputEvent res = new KeyInputEvent { Key = key, KeyIndex = some, IsUp = false };
                ForwardInput(res);
            };

            PrimaryMouse.MouseMove += (IMouse mouse, Vector2 pos) =>
            {
                MouseInputEvent res = new MouseInputEvent { Position = new Vector2D<float>(pos.X, pos.Y) };
                ForwardInput(res);
            };

            PrimaryMouse.Scroll += (IMouse mouse, ScrollWheel wheel) =>
            {
                MouseInputWheelEvent res = new MouseInputWheelEvent { Position = new Vector2D<float>(wheel.X, wheel.Y) };
                ForwardInput(res);
            };

            PrimaryMouse.MouseUp += (IMouse mouse, MouseButton button) =>
            {
                MouseButtonEvent res = new MouseButtonEvent { Button = button, IsUp = true };
                ForwardInput(res);
            };

            PrimaryMouse.MouseDown += (IMouse mouse, MouseButton button) =>
            {

                MouseButtonEvent res = new MouseButtonEvent { Button = button, IsUp = false };
                ForwardInput(res);
            };
        }
        public void SetCursor(StandardCursor cursor)
        {
            PrimaryMouse.Cursor.StandardCursor = cursor;
        }

        public void SetCursorMode(CursorMode mode)
        {
            PrimaryMouse.Cursor.CursorMode = mode;
        }

        public CursorMode GetCursorMode()
        {
            return PrimaryMouse.Cursor.CursorMode;
        }

        public bool IsKeyPressed(Key key)
        {
            return PrimaryKeyboard.IsKeyPressed(key);
        }

        public bool IseMouseButtonPressed(MouseButton button)
        {
            return PrimaryMouse.IsButtonPressed(button);
        }

        public Vector2D<float> GetMousePosition()
        {
            return new Vector2D<float>(PrimaryMouse.Position.X, PrimaryMouse.Position.Y);
        }

        public void SetMousePosition(Vector2D<float> Position)
        {
            PrimaryMouse.Position = new Vector2(Position.X, Position.Y);
        }

        private void ForwardInput(InputEvent ev)
        {
            ScreneTreeService treeService = _registry.Get<ScreneTreeService>();

            foreach (IInputable child in treeService.GetAll<IInputable>())
            {
                child.OnInput(ev);
            }
        }

        public void Render(double delta)
        {

        }

        public void Unregister()
        {
        }

        public void Update(double delta)
        {
        }
    }
}
