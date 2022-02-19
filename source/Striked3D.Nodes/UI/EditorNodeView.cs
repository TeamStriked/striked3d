using Silk.NET.Maths;
using Striked3D.Core;
using Striked3D.Resources;
using Striked3D.Services;
using Striked3D.Utils;
using System;
using System.Collections.Generic;
using Veldrid;

namespace Striked3D.Nodes
{
    internal struct EditorNodeViewItem
    {
        public TextBox TextBox { get; set; }
        public Striked3D.Core.Object obj { get; set; }
        public string Key { get; set; }
        public string Row { get; set; }
    }
    public class EditorNodeView : Control
    {
        private LayoutGrid view;

        public Editor editor;
        private Node selectedNode;
        private Guid selectedNodeId;
        private readonly List<EditorNodeViewItem> itemList = new List<EditorNodeViewItem>();

        public void SetNode(Guid nodeid)
        {
            foreach (INode child in view.GetChilds())
            {
                view.RemoveChild(child);
            }

            ScreneTreeService treeService = Root.Services.Get<ScreneTreeService>();
            Node newNode = treeService.GetNode<Node>(nodeid);

            if (newNode != null && selectedNodeId != nodeid)
            {
                selectedNode = newNode;
                selectedNodeId = nodeid;

                generateProperties(selectedNode);
            }

            if (newNode == null)
            {
                selectedNodeId = Guid.Empty;
                selectedNode = null;
                itemList.Clear();
            }
        }
        public override void DrawCanvas()
        {
        }
        private void generateProperties(Striked3D.Core.Object node)
        {

            Dictionary<string, Type> properties = node.GetExportProperties();

            foreach (KeyValuePair<string, Type> prop in properties)
            {

                LayoutGrid labelLayout = new LayoutGrid
                {
                    Direction = UIPanelDirection.VERTICAL,
                    Size = new Types.StringVector("100%", "200px"),
                    Position = new Types.StringVector(0, 0),
                    AutoHeight = true
                };

                view.AddChild(labelLayout);


                Label label = new Label
                {
                    Content = prop.Key,
                    Padding = new Vector4D<int>(5, 5, 5, 5)
                };
                // label.Font = new Resources.Font("Arial", SkiaSharp.SKFontStyleWeight.Bold);

                labelLayout.AddChild(label);

                LayoutGrid layout = new LayoutGrid
                {
                    Direction = UIPanelDirection.HORIZONTAL,
                    Size = new Types.StringVector("100%", "50px"),
                    Position = new Types.StringVector(0, 0),
                    AutoHeight = true,
                    AutoGrid = true
                };

                labelLayout.AddChild(layout);

                if (prop.Value == typeof(Vector3D<float>))
                {
                    createVectorField(layout, "X", node, prop.Key, "X");
                    createVectorField(layout, "Y", node, prop.Key, "Y");
                    createVectorField(layout, "Z", node, prop.Key, "Z");
                }

                if (prop.Value == typeof(Quaternion<float>))
                {
                    createVectorField(layout, "X", node, prop.Key, "X");
                    createVectorField(layout, "Y", node, prop.Key, "Y");
                    createVectorField(layout, "Z", node, prop.Key, "Z");
                    createVectorField(layout, "W", node, prop.Key, "W");
                }

                if (prop.Value == typeof(Transform3D))
                {
                    Transform3D contentValue = node.GetValue<Transform3D>(prop.Key);
                    if (contentValue != null)
                    {
                        generateProperties(contentValue);
                    }
                }

            }
        }

        private TextBox createVectorField(LayoutGrid grid, string label, Striked3D.Core.Object obj, string key, string row)
        {
            object contentValue = obj.GetValue(key);
            Type contentValueType = obj.GetValueType(key);

            if (contentValue == null)
            {
                return null;
            }

            object rowValue = contentValueType.GetField(row).GetValue(contentValue);

            if (rowValue == null)
            {
                return null;
            }

            TextBox textBox = new TextBox
            {
                Padding = new Vector4D<int>(5, 5, 5, 5),
                Content = rowValue.ToString(),
                Background = new RgbaFloat(1, 0, 0, 1)
            };
            textBox.OnChange += (string value) =>
            {
                object previousValue = obj.GetValue(key);

                if (rowValue is float)
                {
                    contentValueType.GetField(row).SetValue(previousValue, value.ToFloat());
                }
                else if (rowValue is int)
                {
                    contentValueType.GetField(row).SetValue(previousValue, value.ToInteger());
                }

                obj.SetValue(key, previousValue);
            };

            itemList.Add(new EditorNodeViewItem
            {
                Key = key,
                Row = row,
                obj = obj,
                TextBox = textBox,
            });

            grid.AddChild(textBox);
            return textBox;
        }

        public override void Update(double delta)
        {
            base.Update(delta);

            foreach (EditorNodeViewItem item in itemList)
            {
                if (!item.TextBox.isFocused)
                {
                    object contentValue = item.obj.GetValue(item.Key);
                    Type contentValueType = item.obj.GetValueType(item.Key);

                    if (contentValue == null)
                    {
                        continue;
                    }

                    object rowValue = contentValueType.GetField(item.Row).GetValue(contentValue);
                    item.TextBox.Content = rowValue.ToString();
                }
            }
        }

        public override void OnEnterTree()
        {
            base.OnEnterTree();

            view = new LayoutGrid
            {
                Direction = UIPanelDirection.VERTICAL,
                Size = new Types.StringVector("100%", "100%"),
                Position = new Types.StringVector(0, 0)
            };

            AddChild(view);
        }
    }
}
