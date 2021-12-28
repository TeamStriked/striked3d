using Striked3D.Core;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Striked3D.Core
{
    public class NodeTree
    {
        public Dictionary<Guid, BaseNode> Nodes = new Dictionary<Guid, BaseNode>();

        public Window window;

        public NodeTree(Window _window) 
        {
            window = _window;
        }

        public T CreateNode<T>() where T: BaseNode
        {
            BaseNode instance = (BaseNode)Activator.CreateInstance<T>();
            instance.window = window;
            Nodes.Add(instance.Id, instance);

            instance.OnEnterTree();
            return instance as T;
        }

        public void ForwardUpdate(double delta)
        {
            foreach (var item in Nodes)
            {
                item.Value.Update(delta);
            }
        }
        public void ForwardRender(double delta)
        {
            foreach (var item in Nodes)
            {
                item.Value.Render(delta);
            }
        }

        public void ForwardInput(InputEvent e)
        {
            foreach(var item in Nodes)
            {
                item.Value.OnInput(e);
            }
        }
    }
}
