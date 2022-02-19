using Striked3D.Core;
using Striked3D.Core.Window;
using Striked3D.Nodes;
using Striked3D.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Striked3D.Nodes
{
    public abstract class Node : Core.Object, INode
    {
        public IWindow Root { get; set; }

        private string _Name = null;
        public string Name { get { return (_Name == null) ? this.GetType().Name : _Name; } }

        public delegate void OnEnterTreeHandler();
        public event OnEnterTreeHandler OnNodeEnterTree;

        public delegate void OnLeaveTreeHandler();
        public event OnLeaveTreeHandler OnNodeLeaveTree;

        public delegate void OnIsReadyEventHandler();
        public event OnIsReadyEventHandler OnNodeIsReady;

        public bool resetCache = true;
        public bool IsEnterTree = false;

        private IViewport _viewPort;
        public IViewport Viewport
        {
            get
            {
                if (resetCache || _viewPort == null)
                {
                    _viewPort = Root.Services.Get<ScreneTreeService>().GetViewport(this.Id);
                    resetCache = false;
                }

                return _viewPort;
            }
        }

        public T GetParent<T>() where T : Node
        {
            var service =  Root.Services.Get<ScreneTreeService>();
            return service.GetParent<T>(this.Id);
        }

        public virtual T CreateChild<T>() where T : Node
        {
            var node = Root.Services.Get<ScreneTreeService>().CreateNode<T>(this.Id);
            node.OnEnterTree();
            node.OnEnterTreeReady();

            return node;
        }
        public virtual void AddChild(INode t)
        {
            Root.Services.Get<ScreneTreeService>().AddNode(t, this.Id);
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
            return Root.Services.Get<ScreneTreeService>().GetChilds(this.Id);
        }

        public T[] GetChilds<T>() where T :Node
        {
            return Root.Services.Get<ScreneTreeService>().GetChilds<T>(this.Id);
        }

        public Node()
        {
        }

        public virtual void Update(double delta)
        {
        }

        public virtual void OnEnterTree()
        {
            this.IsEnterTree = true;
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
             Root.Services.Get<ScreneTreeService>().SetFreeQueue(this.Id);
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (var child in this.GetChilds())
            {
                this.RemoveChild(child);
            }
        }
    }
}
