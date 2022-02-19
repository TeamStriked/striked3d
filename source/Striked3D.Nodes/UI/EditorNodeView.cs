using Silk.NET.Maths;
using Striked3D.Core;
using Striked3D.Resources;
using Striked3D.Services;
using Striked3D.Utils;
using System;
using System.Collections.Generic;
using System.Timers;
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
        LayoutGrid view;

        public Editor editor;
        private Node selectedNode;
        private Guid selectedNodeId;
        private List<EditorNodeViewItem> itemList = new List<EditorNodeViewItem>();

        public void SetNode(Guid nodeid)
        {
            foreach (var child in this.view.GetChilds())
            {
                this.view.RemoveChild(child);
            }

            var treeService = this.Root.Services.Get<ScreneTreeService>();
            var newNode = treeService.GetNode<Node>(nodeid);

            if (newNode != null && selectedNodeId != nodeid)
            {
                selectedNode = newNode;
                selectedNodeId = nodeid;

                this.generateProperties(selectedNode);
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
       
            var properties = node.GetExportProperties();

            foreach (var prop in properties)
            {

                var labelLayout = new LayoutGrid();
                labelLayout.Direction = UIPanelDirection.VERTICAL;
                labelLayout.Size = new Types.StringVector("100%", "200px");
                labelLayout.Position = new Types.StringVector(0, 0);
                labelLayout.AutoHeight = true;

                view.AddChild(labelLayout);
               

                var label = new Label();

                label.Content = prop.Key;
                label.Padding = new Vector4D<int>(5, 5, 5, 5);
                // label.Font = new Resources.Font("Arial", SkiaSharp.SKFontStyleWeight.Bold);

                labelLayout.AddChild(label);
           
               var layout = new LayoutGrid();
               layout.Direction = UIPanelDirection.HORIZONTAL;
               layout.Size = new Types.StringVector("100%", "50px");
               layout.Position = new Types.StringVector(0, 0);
               layout.AutoHeight = true;
               layout.AutoGrid = true;

               labelLayout.AddChild(layout);

               if (prop.Value == typeof(Vector3D<System.Single>))
               {
                   this.createVectorField(layout, "X", node, prop.Key, "X");
                   this.createVectorField(layout, "Y", node, prop.Key, "Y");
                   this.createVectorField(layout, "Z", node, prop.Key, "Z");
               }

               if (prop.Value == typeof(Quaternion<System.Single>))
               {
                   this.createVectorField(layout, "X", node, prop.Key, "X");
                   this.createVectorField(layout, "Y", node, prop.Key, "Y");
                   this.createVectorField(layout, "Z", node, prop.Key, "Z");
                   this.createVectorField(layout, "W", node, prop.Key, "W");
               }

               if (prop.Value == typeof(Transform3D))
               {
                   var contentValue = node.GetValue<Transform3D>(prop.Key);
                   if(contentValue != null)
                   {
                       this.generateProperties(contentValue);
                   }
               }
         
            }
        }

        private TextBox createVectorField(LayoutGrid grid, string label, Striked3D.Core.Object obj, string key, string row)
        {
            var contentValue = obj.GetValue(key);
            var contentValueType = obj.GetValueType(key);

            if (contentValue == null)
                return null;

            var rowValue = contentValueType.GetField(row).GetValue(contentValue);

            if(rowValue == null)
                return null;

            var textBox = new TextBox();
            textBox.Padding = new Vector4D<int>(5, 5, 5, 5);
            textBox.Content = rowValue.ToString();
            textBox.Background = new RgbaFloat(1, 0, 0, 1);
            textBox.OnChange += (string value) =>
            {
                var previousValue = obj.GetValue(key);

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

            this.itemList.Add(new EditorNodeViewItem {
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

            foreach(var item in this.itemList)
            {
                if(!item.TextBox.isFocused )
                {
                    var contentValue = item.obj.GetValue(item.Key);
                    var contentValueType = item.obj.GetValueType(item.Key);

                    if (contentValue == null)
                        continue;

                    var rowValue = contentValueType.GetField(item.Row).GetValue(contentValue);
                    item.TextBox.Content = rowValue.ToString();
                }
            }
        }

        public override void OnEnterTree()
        {
            base.OnEnterTree();

            view = new LayoutGrid();
            view.Direction = UIPanelDirection.VERTICAL;
            view.Size = new Types.StringVector("100%", "100%");
            view.Position = new Types.StringVector(0, 0);

            this.AddChild(view);
        }
    }
}
