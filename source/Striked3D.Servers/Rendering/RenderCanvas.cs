using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Servers.Rendering
{
    public enum CanvasElementType
    {
        Rectable
    }
    public abstract class CanvasElement
    {
        public Vector4D<float> Color { get; set; }
        public Vector2D<float> Position { get; set; }
    }
    public  class CanvasRect : CanvasElement
    {
        public Vector2D<float> Size { get; set; }
    }
    public struct RenderCanvas
    {
        public Matrix4X4<float> transformMatrix { get; set; }
        public List<CanvasElement> elements {get;set;}
        public Guid viewport { get; set; }

        public RenderCanvas()
        {
            elements = new List<CanvasElement> ();
            transformMatrix = Matrix4X4<float>.Identity;
        }
    }
}
