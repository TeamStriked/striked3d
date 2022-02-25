using Silk.NET.Input;
using Striked3D.Types;

namespace Striked3D.Core.Input
{
    public abstract class InputEvent
    {

    }


    public class KeyInputEvent : InputEvent
    {
        public bool IsUp { get; set; }

        public Key Key { get; set; }
        public int KeyIndex { get; set; }
    }
    public class KeyCharEvent : InputEvent
    {
        public char Char { get; set; }
    }

    public class MouseInputEvent : InputEvent
    {
        public Vector2D<float> Position { get; set; }
    }
    public class MouseInputWheelEvent : InputEvent
    {
        public Vector2D<float> Position { get; set; }
    }
    public class MouseButtonEvent : InputEvent
    {
        public MouseButton Button { get; set; }
        public bool IsUp { get; set; }
    }
}
