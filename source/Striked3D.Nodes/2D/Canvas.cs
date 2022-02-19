using Silk.NET.Maths;
using Striked3D.Core;
using Striked3D.Engine.Resources;
using Striked3D.Graphics;
using Striked3D.Resources;
using System;
using System.Collections.Generic;
using Veldrid;

namespace Striked3D.Nodes
{

    public abstract class Canvas : Node2D, IDrawable2D
    {
        private readonly List<Material2DInfo> matInfoArray = new List<Material2DInfo>();

        public Material2D Material { get; set; }

        private bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        private bool needsToBeRecreate = false;

        public abstract void DrawCanvas();

        public override void OnEnterTree()
        {
            base.OnEnterTree();
            UpdateCanvas();
        }

        public void UpdateCanvas()
        {
            needsToBeRecreate = true;
        }
        public void DrawRect(RgbaFloat _color, Vector2D<float> _position, Vector2D<float> _size)
        {
            matInfoArray.Add(new Material2DInfo { IsFont = 0.0f, position = _position, size = _size, color = _color });
        }

        public void DrawText(Font font, RgbaFloat _color, Vector2D<float> _position, float fontSize, string text)
        {

            /*
             *   var fontService = this.Root.Services.Get<FontService>();
            if(fontService != null)
            {
               this.textBlocks.Add(fontService.RegisterText(font, text, fontSize, _position, _color));
            }
            */

            font.AddChars(text);

            FontAtlas atlas = font.Atlas;

            if (atlas.bitmap == null)
            {
                return;
            }

            float xPos = _position.X;
            float scale = (fontSize / Font.renderSize);
            float size = Font.renderSize * scale;

            foreach (char c in text)
            {
                if (!atlas.chars.ContainsKey(c))
                {
                    continue;
                }

                FontAtlasGylph cacheChar = atlas.chars[c];

                float fromX = cacheChar.region.X;
                float fromY = cacheChar.region.Y;

                float toT = (cacheChar.region.X + Font.renderSize);
                float toY = (cacheChar.region.Y + Font.renderSize);

                matInfoArray.Add(new Material2DInfo
                {
                    IsFont = 1.0f,
                    position = new Vector2D<float>(xPos, _position.Y),
                    size = new Vector2D<float>(size, size),
                    color = _color,
                    FontRegion = new Vector4D<float>(fromX, fromY, toT, toY),
                    FontRange = MathF.Max(1.0f, (size / Font.renderSize) * Font.renderRange)
                });

                xPos += (float)cacheChar.advance * scale;
            }
        }

        public void DrawRectBorder(RgbaFloat _color, Vector2D<float> _position, Vector2D<float> _endPosition, float thickness)
        {
            Vector2D<float> leftPos1 = _position;
            Vector2D<float> leftPos2 = _position;
            leftPos2.X = _endPosition.X;

            DrawLine(_color, leftPos1, leftPos2, thickness);

            leftPos1 = _position;
            leftPos1.Y = _endPosition.Y - (thickness / 2);
            leftPos2 = _position;
            leftPos2.X = _endPosition.X;
            leftPos2.Y = _endPosition.Y - (thickness / 2);

            DrawLine(_color, leftPos1, leftPos2, thickness);

            leftPos1 = _position;
            leftPos1.X += (thickness / 2);
            leftPos2 = _position;
            leftPos2.Y = _endPosition.Y;
            leftPos2.X += (thickness / 2);

            DrawLine(_color, leftPos1, leftPos2, thickness);

            leftPos1 = _position;
            leftPos1.X = _endPosition.X;
            leftPos1.X -= (thickness / 2);
            leftPos2 = _position;
            leftPos2.X = _endPosition.X;
            leftPos2.Y = _endPosition.Y;
            leftPos2.X -= (thickness / 2);

            DrawLine(_color, leftPos1, leftPos2, thickness);
        }

        public void DrawLine(RgbaFloat _color, Vector2D<float> _position, Vector2D<float> _endPosition, float thickness)
        {

        }

        public float GetTextWidth(Font font, RgbaFloat _color, Vector2D<float> _position, float fontSize, string text)
        {
            return text.Length * fontSize;
        }

        public virtual void OnDraw2D(IRenderer renderer)
        {
            IMaterial mat = (Material == null) ? renderer.Default2DMaterial : Material;
            if (mat != null && !mat.isDirty)
            {
                if (matInfoArray.Count > 0)
                {
                    renderer.SetMaterial(mat);
                    renderer.SetViewport(Viewport);
                    renderer.SetResourceSets(new ResourceSet[] {
                            Viewport.World2D.ResourceSet,
                            renderer.DefaultTextureSet
                    });

                    renderer.BindBuffers(null, renderer.indexDefaultBuffer);

                    foreach (Material2DInfo info in matInfoArray)
                    {
                        renderer.PushConstant(info);
                        renderer.DrawIndexInstanced(6);
                    }
                }
            }
        }

        public void ClearCanvas()
        {
            matInfoArray.Clear();
        }

        private void CreateBuffers(IRenderer renderer)
        {
            ClearCanvas();
            DrawCanvas();
        }

        public virtual void BeforeDraw(IRenderer renderer)
        {
            if (needsToBeRecreate)
            {
                Material?.BeforeDraw(renderer);
                CreateBuffers(renderer);

                needsToBeRecreate = false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            ClearCanvas();
        }
    }
}
