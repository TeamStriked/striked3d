using Silk.NET.Input;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Core
{
    public abstract class InputEvent
    {
        
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
