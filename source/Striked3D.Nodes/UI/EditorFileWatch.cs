using Striked3D.Core;
using Striked3D.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

namespace Striked3D.Nodes.UI
{
    public class EditorFileWatch : Control
    {
        private LayoutGrid panel;
        private readonly Dictionary<string, EditorFileWatchItem> items = new Dictionary<string, EditorFileWatchItem>();
        public EditorFileWatchItem activeItem;
        public List<string> openFolders = new List<string>();

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

            this.SubscribeFolder(ProjectManager.CurrentProject.SystemPath);
        }

        public void AddElement(string key, string content)
        {
            lock (items)
            {
                if (!items.ContainsKey(key))
                {
                    EditorFileWatchItem viewItem = new EditorFileWatchItem
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
                      //  OnSelectionChange?.Invoke(key);
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

        public Dictionary<string, EditorFileWatchItem> GetItems()
        {
            return items;
        }

   
        private void SubscribeFolder(string path)
        {
            string[] dictArray = Directory.GetDirectories(path);
            foreach (var dict in dictArray)
            {
                var folderPath = new DirectoryInfo(dict).Name;
                this.AddElement(dict, folderPath);
            }

            string[] fileArray = Directory.GetFiles(path);
            foreach (var file in fileArray)
            {
                var fileName = System.IO.Path.GetFileName(file);
                this.AddElement(file, fileName);
            }

            var watcher = new FileSystemWatcher(path);

            watcher.NotifyFilter = NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite;
            watcher.Filter = "*.*";

            watcher.Changed += (object sender, FileSystemEventArgs e) => OnChanged(sender,e);
            watcher.Created += (object sender, FileSystemEventArgs e) => OnCreated(sender, e);
            watcher.Deleted += (object sender, FileSystemEventArgs e) => OnDeleted(sender, e);
            watcher.Renamed += (object sender, RenamedEventArgs e) => OnRenamed(sender, e);
            watcher.Error += (object sender, ErrorEventArgs e) => OnError(sender, e);

            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Changed: {e.FullPath}");

            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            var fileName = System.IO.Path.GetFileName(e.FullPath);
            this.AddElement(e.FullPath, fileName);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            this.RemoveElement(e.FullPath);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            this.RemoveElement(e.OldFullPath);

            var fileName = System.IO.Path.GetFileName(e.FullPath);
            this.AddElement(e.FullPath, fileName);
        }

        private void OnError(object sender, ErrorEventArgs e) => Logger.Error(e.GetException().Message, e.GetException().StackTrace);

    }
}
