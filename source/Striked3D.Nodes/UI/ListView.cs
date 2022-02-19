using System.Collections.Generic;

namespace Striked3D.Nodes
{
    public class ListView : Control
    {
        private LayoutGrid panel;
        private readonly Dictionary<string, ListViewItem> items = new Dictionary<string, ListViewItem>();
        public ListViewItem activeItem;

        public delegate void OnSelectionChangeHandler(string key);
        public event OnSelectionChangeHandler OnSelectionChange;
        public override void DrawCanvas()
        {
        }
        public override void OnEnterTree()
        {
            base.OnEnterTree();

            panel = new LayoutGrid
            {
                Size = new Types.StringVector("100%", "100%"),
                Position = new Types.StringVector(0, 0),
                BackgroundColor = Veldrid.RgbaFloat.Clear
            };

            AddChild(panel);
        }

        public void AddElement(string key, string content)
        {
            lock (items)
            {
                if (!items.ContainsKey(key))
                {
                    ListViewItem viewItem = new ListViewItem
                    {
                        Content = content,
                        Position = new Types.StringVector(0, 0),
                        Color = new Veldrid.RgbaFloat(0, 1, 1, 1),
                        Background = Veldrid.RgbaFloat.Clear,
                        BackgroundHover = Veldrid.RgbaFloat.Black,
                        ColorHover = Veldrid.RgbaFloat.White,
                        Key = key
                    };
                    viewItem.OnClick += () =>
                    {
                        activeItem = viewItem;
                        OnSelectionChange?.Invoke(key);
                    };

                    panel.AddChild(viewItem);
                    items.Add(viewItem.Key, viewItem);
                }
            }
        }

        public void RemoveElement(string key)
        {
            lock (items)
            {
                if (items.ContainsKey(key))
                {
                    items[key].FreeQueue();
                    items.Remove(key);
                }
            }
        }

        public Dictionary<string, ListViewItem> GetItems()
        {
            return items; ;
        }
    }
}
