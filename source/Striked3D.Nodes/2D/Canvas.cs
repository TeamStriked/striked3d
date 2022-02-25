using Striked3D.Core;
using Striked3D.Engine.Resources;
using Striked3D.Graphics;
using Striked3D.Importer;
using Striked3D.Resources;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Veldrid;

namespace Striked3D.Nodes
{
    internal struct CanvasItem
    {
        public Material2DInfo info { get; set; }
        public Font font;
        public int atlasId = -1;
    }

    public abstract class Canvas : Node2D, IDrawable2D
    {
        private readonly List<CanvasItem> matInfoArray = new List<CanvasItem>();

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
            matInfoArray.Add(new CanvasItem { info = new Material2DInfo { IsFont = 0.0f, position = _position, size = _size, color = _color } });
        }
        public float GetTextWidth(Font font, float fontSize, string text)
        {
            if (font == null)
            {
                return 0;
            }

            float width = 0;
            float scale = (fontSize / FontImporter.renderSize);
            float size = FontImporter.renderSize * scale;

            foreach (char c in text)
            {
                FontAtlasGylph cacheChar = font.GetChar(c);

                width += (float)cacheChar.advance * scale;
            }

            return width;
        }
        public void DrawText(Font font, RgbaFloat _color, Vector2D<float> _position, float fontSize, string text)
        {
            if (font == null)
            {
                return;
            }

            float xPos = _position.X;
            float scale = (fontSize / FontImporter.renderSize);
            float size = FontImporter.renderSize * scale;

            foreach (char c in text)
            {
                FontAtlasGylph cacheChar = font.GetChar(c);

                float fromX = cacheChar.region.X;
                float fromY = cacheChar.region.Y;

                float toT = (cacheChar.region.X + FontImporter.renderSize);
                float toY = (cacheChar.region.Y + FontImporter.renderSize);

                matInfoArray.Add(new CanvasItem
                {
                    info = new Material2DInfo
                    {
                        IsFont = 1.0f,
                        position = new Vector2D<float>(xPos, _position.Y),
                        size = new Vector2D<float>(size, size),
                        color = _color,
                        FontRegion = new Vector4D<float>(fromX, fromY, toT, toY),
                        FontRange = MathF.Max(1.0f, (size / FontImporter.renderSize) * FontImporter.renderRange)
                    },
                    font = font,
                    atlasId = cacheChar.atlasId
                });

                xPos += (float)cacheChar.advance * scale;
            }
        }

        public void DrawRectBorder(RgbaFloat _color, Vector2D<float> _position, Vector2D<float> _endPosition, float thickness)
        {
            //top

            Vector2D<float> leftPos1 = _position;
            Vector2D<float> leftPos2 = _position;
            leftPos2.X = _endPosition.X;

            DrawLine(_color, leftPos1, leftPos2, 0, thickness);

            //bottom

            leftPos1 = _position;
            leftPos1.Y = _endPosition.Y - (thickness / 2);
            leftPos2 = _position;
            leftPos2.X = _endPosition.X;
            leftPos2.Y = _endPosition.Y - (thickness / 2);

            DrawLine(_color, leftPos1, leftPos2, 0, thickness);


            //left

            leftPos1 = _position;
            leftPos1.X += (thickness / 2);
            leftPos2 = _position;
            leftPos2.Y = _endPosition.Y;
            leftPos2.X += (thickness / 2);

            DrawLine(_color, leftPos1, leftPos2, thickness, 0);


            //right

            leftPos1 = _position;
            leftPos1.X = _endPosition.X;
            leftPos1.X -= (thickness / 2);
            leftPos2 = _position;
            leftPos2.X = _endPosition.X;
            leftPos2.Y = _endPosition.Y;
            leftPos2.X -= (thickness / 2);

            DrawLine(_color, leftPos1, leftPos2, thickness, 0);
        }

        public void DrawLine(RgbaFloat _color, Vector2D<float> _position, Vector2D<float> _endPosition, float thicknessX, float thicknessY)
        {
            Vector2D<float> size = _endPosition - _position;
            size.Y += thicknessY;
            size.X += thicknessX;

            matInfoArray.Add(new CanvasItem { info = new Material2DInfo { IsFont = 0.0f, position = _position, size = size, color = _color } });
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

                    foreach (CanvasItem item in matInfoArray)
                    {
                        if (item.info.IsFont > 0)
                        {
                            FontAtlas? atlas = item.font.GetAtlas(item.atlasId);

                            if (atlas != null)
                            {
                                renderer.SetResourceSets(new ResourceSet[] {
                                        Viewport.World2D.ResourceSet,
                                        atlas.fontAtlasSet
                                });
                            }

                        }
                        else
                        {
                            renderer.SetResourceSets(new ResourceSet[] {
                                    Viewport.World2D.ResourceSet,
                                    renderer.DefaultTextureSet
                            });
                        }

                        renderer.PushConstant(item.info);
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
            foreach (Font? fontInUse in matInfoArray.Where(df => df.font != null).Select(df => df.font).Distinct())
            {
                fontInUse.Bind(renderer);
            }

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
