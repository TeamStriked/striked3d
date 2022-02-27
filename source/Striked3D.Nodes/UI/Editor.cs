using Striked3D.Helpers;
using Striked3D.Math;
using Striked3D.Nodes.UI;
using Striked3D.Resources;

namespace Striked3D.Nodes
{
    public struct EditorTheme
    {
        public Font Font { get; set; }
        public BitmapTexture FolderIcon { get; set; }
        public int IconSize = 16;
        public Vector4D<float> IconModulator = new Vector4D<float>(0,1,0,1);
    }

    public class Editor : Node
    {
        public static EditorTheme Theme = new EditorTheme();

        private LayoutGrid verticalGrid;
        private LayoutGrid headerGrid;
        private LayoutGrid footerGrid;
        private LayoutGrid contentGrid;
        private LayoutGrid contentLeftGrid;
        private LayoutGrid contentLeftGrid2;
        private LayoutGrid contentCenterGrid;
        private LayoutGrid contentRightGrid;

        private Viewport _editorViewport;
        private ViewportContainer _editorViewportContainer;
        public Viewport EditorViewport => _editorViewport;
        public ViewportContainer EditorViewportContainer => _editorViewportContainer;

        public override void OnEnterTree()
        {
            base.OnEnterTree();

            verticalGrid = new LayoutGrid
            {
                Direction = UIPanelDirection.VERTICAL,
                Size = new Types.StringVector("100%", "100%"),
                Position = new Types.StringVector("0px", "0px")
            };

            AddChild(verticalGrid);

            CreateHeader();
            CreateContent();
            CreateFooter();

            EditorNodeView nodeView = new EditorNodeView
            {
                Size = new Types.StringVector("100%", "100%"),
                Position = new Types.StringVector("0px", "0px"),
                editor = this
            };

            contentRightGrid.AddChild(nodeView);

            EditorTree tree = new EditorTree
            {
                Size = new Types.StringVector("100%", "100%"),
                Position = new Types.StringVector("0px", "0px"),
                editor = this,
                nodeView = nodeView
            };
       
            EditorFileWatch fileWatch = new EditorFileWatch
            {
                Size = new Types.StringVector("100%", "100%"),
                Position = new Types.StringVector("0px", "0px"),
            };
   

           contentLeftGrid2.AddChild(tree);
           contentLeftGrid.AddChild(fileWatch);
        }

        private void CreateHeader()
        {
            headerGrid = new LayoutGrid
            {
                Direction = UIPanelDirection.HORIZONTAL,
                Size = new Types.StringVector("100%", "50px"),
                Position = new Types.StringVector("0px", "0px"),
                BackgroundColor = RGBHelper.FromHex("#3a3f43")
            };

            verticalGrid.AddChild(headerGrid);
        }
        private void CreateFooter()
        {
            footerGrid = new LayoutGrid
            {
                Direction = UIPanelDirection.HORIZONTAL,
                Size = new Types.StringVector("100%", "50px"),
                Position = new Types.StringVector("0px", "0px"),
                BackgroundColor = RGBHelper.FromHex("#3a3f43")
            };

            verticalGrid.AddChild(footerGrid);
        }
        private void CreateContent()
        {
            contentGrid = new LayoutGrid
            {
                Direction = UIPanelDirection.HORIZONTAL,
                Size = new Types.StringVector("100%", "100%;-100px"),
                Position = new Types.StringVector("0px", "0px")
            };

            verticalGrid.AddChild(contentGrid);

            contentLeftGrid = new LayoutGrid
            {
                Direction = UIPanelDirection.VERTICAL,
                Size = new Types.StringVector("15%", "100%"),
                Position = new Types.StringVector("0px", "0px"),
                BackgroundColor = RGBHelper.FromHex("#212428")
            };

            contentGrid.AddChild(contentLeftGrid);


            contentLeftGrid2 = new LayoutGrid
            {
                Direction = UIPanelDirection.VERTICAL,
                Size = new Types.StringVector("15%", "100%"),
                Position = new Types.StringVector("0px", "0px"),
                BackgroundColor = RGBHelper.FromHex("#2a2e33")
            };

            contentGrid.AddChild(contentLeftGrid2);

            contentCenterGrid = new LayoutGrid
            {
                Direction = UIPanelDirection.VERTICAL,
                Size = new Types.StringVector("50%", "100%"),
                Position = new Types.StringVector("0px", "0px")
            };

            contentGrid.AddChild(contentCenterGrid);


            LayoutGrid editorCenterMenuTop = new LayoutGrid
            {
                Direction = UIPanelDirection.HORIZONTAL,
                Size = new Types.StringVector("100%", "50px"),
                Position = new Types.StringVector("0px", "0px"),
                BackgroundColor = RGBHelper.FromHex("#212428")
            };

            LayoutGrid contentViewContainer = new LayoutGrid
            {
                Direction = UIPanelDirection.HORIZONTAL,
                Size = new Types.StringVector("100%", "100%;-100px"),
                Position = new Types.StringVector("0px", "0px"),
                BackgroundColor = RGBHelper.FromHex("#00ff00")
            };


            LayoutGrid editorCenterMenuBottom = new LayoutGrid
            {
                Direction = UIPanelDirection.HORIZONTAL,
                Size = new Types.StringVector("100%", "50px"),
                Position = new Types.StringVector("0px", "0px"),
                BackgroundColor = RGBHelper.FromHex("#212428")
            };

            contentCenterGrid.AddChild(editorCenterMenuTop);
            //       contentCenterGrid.AddChild(contentViewContainer);
            createViewport();
            contentCenterGrid.AddChild(editorCenterMenuBottom);


            contentRightGrid = new LayoutGrid
            {
                Direction = UIPanelDirection.VERTICAL,
                Size = new Types.StringVector("20%", "100%"),
                Position = new Types.StringVector("0px", "0px"),
                BackgroundColor = RGBHelper.FromHex("#2b2e33")
            };

            contentGrid.AddChild(contentRightGrid);
        }

        private void createViewport()
        {
            _editorViewportContainer = new ViewportContainer
            {
                Size = new Types.StringVector("100%", "100%;-100px"),
                Position = new Types.StringVector("0px", "0px")
            };

            contentCenterGrid.AddChild(_editorViewportContainer);

            _editorViewport = new Viewport();
            _editorViewportContainer.AddChild(_editorViewport);
        }
    }
}
