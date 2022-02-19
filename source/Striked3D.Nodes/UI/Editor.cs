using Striked3D.Helpers;
using Striked3D.Nodes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Nodes
{
    public class Editor : Node
    {
        LayoutGrid verticalGrid;
        LayoutGrid headerGrid;
        LayoutGrid footerGrid;
        LayoutGrid contentGrid;

        LayoutGrid contentLeftGrid;
        LayoutGrid contentCenterGrid;
        LayoutGrid contentRightGrid;

        private Viewport _editorViewport;
        private ViewportContainer _editorViewportContainer;
        public Viewport EditorViewport => _editorViewport;
        public ViewportContainer EditorViewportContainer => _editorViewportContainer;

        public override void OnEnterTree()
        {
            base.OnEnterTree();

            verticalGrid = new LayoutGrid();
            verticalGrid.Direction = UIPanelDirection.VERTICAL;
            verticalGrid.Size = new Types.StringVector("100%", "100%");
            verticalGrid.Position = new Types.StringVector("0px", "0px");

            this.AddChild(verticalGrid);

            this.CreateHeader();
            this.CreateContent();
            this.CreateFooter();

            var nodeView = new EditorNodeView();
            nodeView.Size = new Types.StringVector("100%", "100%");
            nodeView.Position = new Types.StringVector("0px", "0px");
            nodeView.editor = this;

            contentRightGrid.AddChild(nodeView);

            var tree = new EditorTree();
            tree.Size = new Types.StringVector("100%", "100%");
            tree.Position = new Types.StringVector("0px", "0px");
            tree.editor = this;
            tree.nodeView = nodeView;

            contentLeftGrid.AddChild(tree);
        }

        private void CreateHeader()
        {
            headerGrid = new LayoutGrid();
            headerGrid.Direction = UIPanelDirection.HORIZONTAL;
            headerGrid.Size = new Types.StringVector("100%", "50px");
            headerGrid.Position = new Types.StringVector("0px", "0px");
            headerGrid.BackgroundColor = RGBHelper.FromHex("#3a3f43");

            verticalGrid.AddChild(headerGrid);
        }
        private void CreateFooter()
        {
            footerGrid = new LayoutGrid();
            footerGrid.Direction = UIPanelDirection.HORIZONTAL;
            footerGrid.Size = new Types.StringVector("100%", "50px");
            footerGrid.Position = new Types.StringVector("0px", "0px");
            footerGrid.BackgroundColor = RGBHelper.FromHex("#3a3f43");

            verticalGrid.AddChild(footerGrid);
        }
        private void CreateContent()
        {
            contentGrid = new LayoutGrid();
            contentGrid.Direction = UIPanelDirection.HORIZONTAL;
            contentGrid.Size = new Types.StringVector("100%", "100%;-100px");
            contentGrid.Position = new Types.StringVector("0px", "0px");

            verticalGrid.AddChild(contentGrid);

            contentLeftGrid = new LayoutGrid();
            contentLeftGrid.Direction = UIPanelDirection.VERTICAL;
            contentLeftGrid.Size = new Types.StringVector("25%", "100%");
            contentLeftGrid.Position = new Types.StringVector("0px", "0px");
            contentLeftGrid.BackgroundColor = RGBHelper.FromHex("#2b2e33");

            contentGrid.AddChild(contentLeftGrid);

            contentCenterGrid = new LayoutGrid();
            contentCenterGrid.Direction = UIPanelDirection.VERTICAL;
            contentCenterGrid.Size = new Types.StringVector("50%", "100%");
            contentCenterGrid.Position = new Types.StringVector("0px", "0px");

            contentGrid.AddChild(contentCenterGrid);


            var editorCenterMenuTop = new LayoutGrid();
            editorCenterMenuTop.Direction = UIPanelDirection.HORIZONTAL;
            editorCenterMenuTop.Size = new Types.StringVector("100%", "50px");
            editorCenterMenuTop.Position = new Types.StringVector("0px", "0px");
            editorCenterMenuTop.BackgroundColor = RGBHelper.FromHex("#212428");

            var contentViewContainer = new LayoutGrid();
            contentViewContainer.Direction = UIPanelDirection.HORIZONTAL;
            contentViewContainer.Size = new Types.StringVector("100%", "100%;-100px");
            contentViewContainer.Position = new Types.StringVector("0px", "0px");
            contentViewContainer.BackgroundColor = RGBHelper.FromHex("#00ff00");


            var editorCenterMenuBottom = new LayoutGrid();
            editorCenterMenuBottom.Direction = UIPanelDirection.HORIZONTAL;
            editorCenterMenuBottom.Size = new Types.StringVector("100%", "50px");
            editorCenterMenuBottom.Position = new Types.StringVector("0px", "0px");
            editorCenterMenuBottom.BackgroundColor = RGBHelper.FromHex("#212428");

            contentCenterGrid.AddChild(editorCenterMenuTop);
     //       contentCenterGrid.AddChild(contentViewContainer);
            this.createViewport();
            contentCenterGrid.AddChild(editorCenterMenuBottom);


            contentRightGrid = new LayoutGrid();
            contentRightGrid.Direction = UIPanelDirection.VERTICAL;
            contentRightGrid.Size = new Types.StringVector("25%", "100%");
            contentRightGrid.Position = new Types.StringVector("0px", "0px");
            contentRightGrid.BackgroundColor = RGBHelper.FromHex("#2b2e33");

            contentGrid.AddChild(contentRightGrid);
        }

        private void createViewport()
        {
            _editorViewportContainer = new ViewportContainer();
            _editorViewportContainer.Size = new Types.StringVector("100%", "100%;-100px");
            _editorViewportContainer.Position = new Types.StringVector("0px", "0px");

            contentCenterGrid.AddChild(_editorViewportContainer);

            _editorViewport = new Viewport();
            _editorViewportContainer.AddChild(_editorViewport);
        }
    }
}
