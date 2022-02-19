
using System;
using System.Collections.Generic;

namespace Striked3D.Nodes
{
    public class ListView : Control
    {
        LayoutGrid panel;
        Dictionary<string, ListViewItem> items = new Dictionary<string, ListViewItem>();
        public ListViewItem activeItem;

        public delegate void OnSelectionChangeHandler(string key);
        public event OnSelectionChangeHandler OnSelectionChange;
        public override void DrawCanvas()
        {
        }
        public override void OnEnterTree()
        {
            base.OnEnterTree();

            panel = new LayoutGrid();
            panel.Size = new Types.StringVector("100%", "100%");
            panel.Position = new Types.StringVector(0, 0);
            panel.BackgroundColor = Veldrid.RgbaFloat.Clear;

            this.AddChild(panel);
        }

        public void AddElement(string key, string content)
        {
            lock(this.items)
            {
                if (!this.items.ContainsKey(key))
                {
                    var viewItem = new ListViewItem();

                    viewItem.Content = content;
                    viewItem.Position = new Types.StringVector(0, 0);
                    viewItem.Color = new Veldrid.RgbaFloat(0, 1, 1, 1);
                    viewItem.Background = Veldrid.RgbaFloat.Clear;
                    viewItem.BackgroundHover = Veldrid.RgbaFloat.Black;
                    viewItem.ColorHover = Veldrid.RgbaFloat.White;
                    viewItem.Key = key;
                    viewItem.OnClick += () =>
                    {
                        activeItem = viewItem;
                        OnSelectionChange?.Invoke(key);
                    };

                    this.panel.AddChild(viewItem);
                    this.items.Add(viewItem.Key, viewItem);
                }
            }
        }

        public void RemoveElement(string key)
        {
            lock (this.items)
            {
                if (this.items.ContainsKey(key))
                {
                    this.items[key].FreeQueue();
                    this.items.Remove(key);
                }
            }
        }

        public Dictionary<string, ListViewItem> GetItems()
        {
            return this.items; ;
        }
    }
}
