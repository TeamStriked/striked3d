using System;
using System.Timers;

namespace Striked3D.Nodes
{
    public class EditorTree : Control
    {
        ListView view;

        public Editor editor;
        public EditorNodeView nodeView;

        public float RefreshInterval = 1000;
        System.Timers.Timer aTimer = new System.Timers.Timer();

        public override void OnEnterTree()
        {
            base.OnEnterTree();

            view = new ListView();
            view.Size = new Types.StringVector("100%", "100%");
            view.Position = new Types.StringVector(0, 0);

            view.OnSelectionChange += (string key) =>
            {
                this.nodeView.SetNode(Guid.Parse(key));
            };

            this.AddChild(view);

            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = RefreshInterval;
            aTimer.Enabled = true;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if(editor != null && editor.EditorViewport != null)
            {
                DrawElement(editor.EditorViewport);
            }
        }

        private void DrawElement(Node element)
        {
            //print element
            view.AddElement(element.Id.ToString(), element.Name);

            var childs = element.GetChilds();

            for(int i = 0; i < childs.Length; i++)
            {
                DrawElement(childs[i] as Node);
            }
        }

        public override void Dispose()
        {
            aTimer.Enabled = false;
            aTimer.Stop();
            aTimer.Dispose();

            base.Dispose();
        }

        public override void DrawCanvas()
        {
        }
    }
}
