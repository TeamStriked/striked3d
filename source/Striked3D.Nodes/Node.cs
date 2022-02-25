using Striked3D.Core;
using Striked3D.Core.Window;
using Striked3D.Services;

namespace Striked3D.Nodes
{
    public abstract class Node : Core.Object, INode
    {
        public IWindow Root { get; set; }

        private readonly string _Name = null;
        public string Name => (_Name == null) ? GetType().Name : _Name;

        public delegate void OnEnterTreeHandler();
        public event OnEnterTreeHandler OnNodeEnterTree;

        public delegate void OnLeaveTreeHandler();
        public event OnLeaveTreeHandler OnNodeLeaveTree;

        public delegate void OnIsReadyEventHandler();
        public event OnIsReadyEventHandler OnNodeIsReady;

        public bool IsEnterTree = false;

        private IViewport _viewPort;
        private INode _parent;
        public IViewport Viewport => _viewPort;

        public T GetParent<T>() where T : Node
        {
            if (_parent != null)
            {
                return _parent as T;
            }
            else
            {
                return null;
            }
        }

        public virtual T CreateChild<T>() where T : Node
        {
            T node = Root.Services.Get<ScreneTreeService>().CreateNode<T>(Id);
            node.OnEnterTree();
            node.OnEnterTreeReady();

            return node;
        }
        public virtual void AddChild(INode t)
        {
            Root.Services.Get<ScreneTreeService>().AddNode(t, Id);
            t.OnEnterTree();
            t.OnEnterTreeReady();
        }

        public virtual void RemoveChild(INode t)
        {
            Root.Services.Get<ScreneTreeService>().RemoveChild(t);
            t.OnLeaveTree();
        }

        public INode[] GetChilds()
        {
            return Root.Services.Get<ScreneTreeService>().GetChilds(Id);
        }

        public T[] GetChilds<T>() where T : Node
        {
            if (Root != null)
            {
                return Root.Services.Get<ScreneTreeService>().GetChilds<T>(Id);
            }
            else
            {
                return new T[0];
            }
        }

        public Node()
        {
        }

        public void ResetCache()
        {
            if (Root != null)
            {
                _viewPort = Root.Services.Get<ScreneTreeService>().GetViewport(Id);
                _parent = Root.Services.Get<ScreneTreeService>().GetParent<INode>(Id);
            }
        }

        public virtual void Update(double delta)
        {
        }

        public virtual void OnEnterTree()
        {

            if (Root != null)
            {
                _viewPort = Root.Services.Get<ScreneTreeService>().GetViewport(Id);
                _parent = Root.Services.Get<ScreneTreeService>().GetParent<INode>(Id);
            }

            IsEnterTree = true;
            OnNodeEnterTree?.Invoke();
        }
        public virtual void OnLeaveTree()
        {
            OnNodeLeaveTree?.Invoke();
        }

        public void OnEnterTreeReady()
        {
            OnNodeIsReady?.Invoke();
        }

        public virtual void FreeQueue()
        {
            Root.Services.Get<ScreneTreeService>().SetFreeQueue(Id);
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (INode child in GetChilds())
            {
                RemoveChild(child);
            }
        }
    }
}
