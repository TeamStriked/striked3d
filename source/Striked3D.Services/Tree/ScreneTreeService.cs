using Striked3D.Core;
using Striked3D.Core.Window;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Striked3D.Services
{
    public struct NodeTreeItem
    {
        public INode node { get; set; }
        public INode parent { get; set; }

        public List<INode> childs = new();
        public IViewport viewport { get; set; }
        public bool FreeQueue { get; set; }
        public string Order { get; set; }
    }

    public class ScreneTreeService : IService
    {
        private readonly Dictionary<Guid, NodeTreeItem> Nodes = new();

        private IWindow _window;

        public delegate void OnNewNodeHandler();
        public event OnNewNodeHandler NewNode = delegate { };

        public int TotalChilds()
        {
            return Nodes.Count;
        }

        public T GetNode<T>(Guid id) where T : class, INode
        {
            if (Nodes.ContainsKey(id))
            {
                if (Nodes[id].node is T)
                {
                    return Nodes[id].node as T;
                }
            }

            return null;
        }

        public void SetFreeQueue(Guid id)
        {
            lock (Nodes)
            {
                if (Nodes.ContainsKey(id))
                {
                    NodeTreeItem node = Nodes[id];
                    node.FreeQueue = true;

                    Nodes[id] = node;
                }
            }
        }

        public T GetParent<T>(Guid id) where T : class, INode
        {
            if (Nodes.ContainsKey(id) && Nodes[id].parent != null && Nodes[id].parent is T)
            {
                return Nodes[id].parent as T;
            }
            else
            {
                return null;
            }
        }

        public IViewport GetViewport(Guid id)
        {
            if (Nodes.ContainsKey(id))
            {
                return Nodes[id].viewport;
            }
            else
            {
                return null;
            }
        }

        public bool HasNode<T>(Guid id)
        {
            return Nodes.ContainsKey(id);
        }

        public INode[] GetChilds(Guid parentId)
        {
            if (parentId != Guid.Empty && Nodes.ContainsKey(parentId))
            {
                NodeTreeItem parent = Nodes[parentId];
                return parent.childs.ToArray();
            }
            else
            {
                return new INode[0];
            }
        }

        public T[] GetChilds<T>(Guid parentId) where T : class, INode
        {
            if (parentId != Guid.Empty && Nodes.ContainsKey(parentId))
            {
                NodeTreeItem parent = Nodes[parentId];
                return parent.childs.Where(df => df is T).Select(df => df as T).ToArray();
            }
            else
            {
                return new T[0];
            }
        }

        public INode[] GetRoots()
        {
            return Nodes.Values.Where(df => df.parent == null).Select(df => df.node).ToArray();
        }

        public unsafe List<T> GetAll<T>() where T : class
        {
            lock (Nodes)
            {
                return Nodes.Values.OrderBy(df => df.Order).Where(df => df.node is T).Select(df => df.node as T).ToList();
            }
        }
        public unsafe IEnumerable<T> GetAllEnumerator<T>() where T : class
        {
            return Nodes.Values.OrderBy(df => df.Order).Where(df => df.node is T).Select(df => df.node as T);
        }

        public NodeTreeItem[] GetAll()
        {
            lock (Nodes)
            {
                return Nodes.Values.OrderBy(df => df.Order).ToArray();
            }
        }

        public void AddNode(INode t)
        {
            AddNode(t, Guid.Empty);
        }

        public void AddNode(INode t, Guid parentId)
        {
            lock (Nodes)
            {
                t.Root = _window;

                NodeTreeItem item = new NodeTreeItem { node = t };
                Nodes.Add(t.Id, item);

                SetParent(t.Id, parentId);

                NewNode?.Invoke();
            }
        }

        public T CreateNode<T>() where T : class, INode
        {
            return CreateNode<T>(Guid.Empty);
        }

        public T CreateNode<T>(Guid parentId) where T : class, INode
        {
            lock (Nodes)
            {
                INode instance = Activator.CreateInstance<T>();
                instance.Root = _window;

                NodeTreeItem item = new NodeTreeItem { node = instance };
                Nodes.Add(instance.Id, item);

                SetParent(instance.Id, parentId);

                NewNode?.Invoke();
                return instance as T;
            }
        }

        public void SetParent(Guid id, Guid parentId)
        {
            lock (Nodes)
            {
                if (parentId != Guid.Empty && Nodes.ContainsKey(id) && Nodes.ContainsKey(parentId))
                {
                    NodeTreeItem value = Nodes[id];
                    NodeTreeItem parent = Nodes[parentId];

                    value.parent = parent.node;
                    value.viewport = (parent.node is IViewport) ? (parent.node as IViewport) : parent.viewport;

                    parent.childs.Add(value.node);

                    value.Order = parent.Order + "." + parent.childs.Count;

                    Nodes[id] = value;
                }
                else if (Nodes.ContainsKey(id))
                {
                    NodeTreeItem value = Nodes[id];
                    value.Order = GetRoots().Count().ToString();
                    Nodes[id] = value;
                }
            }
        }

        public void QueueFreeAll()
        {
            lock (Nodes)
            {
                foreach (KeyValuePair<Guid, NodeTreeItem> item in Nodes.Where(df => df.Value.FreeQueue).ToArray())
                {
                    RemoveChild(item.Value.node);
                }
            }
        }

        public void RemoveChild(INode n)
        {
            lock (Nodes)
            {
                if (Nodes.ContainsKey(n.Id))
                {
                    n.Dispose();

                    INode parentNode = Nodes[n.Id].parent;
                    if (Nodes.ContainsKey(parentNode.Id))
                    {
                        NodeTreeItem parent = Nodes[parentNode.Id];
                        parent.childs.Remove(n);
                    }

                    Nodes.Remove(n.Id);
                }
            }
        }

        public void Update(double delta)
        {
            lock (Nodes)
            {
                foreach (NodeTreeItem item in Nodes.Values.OrderBy(df => df.Order))
                {
                    item.node.Update(delta);
                }
            }
        }

        public void Render(double delta)
        {

        }

        public void Register(IWindow window)
        {
            _window = window;
        }

        public void Unregister()
        {
        }
    }
}
