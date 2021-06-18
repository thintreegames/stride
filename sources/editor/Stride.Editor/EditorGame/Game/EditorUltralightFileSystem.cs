using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.UI;

using System.Collections.Generic;
using Stride.Assets.UI;
using System.Text;
using UltralightNet;

namespace Stride.Editor.EditorGame.Game
{
    public unsafe class EditorUltralightFileSystem : IUltralightFileSystem
    {
        public struct FileHeader
        {
            public long FileSize;
            public string FilePath;
        }

        public SessionViewModel SessionView { get; set; }

        private static int LastFileID = 1;

        private Dictionary<int, FileHeader> openFiles;

        private Dictionary<string, FileSystemWatcher> pathFileSystemWatchers;

        public EditorUltralightFileSystem()
        {
            openFiles = new Dictionary<int, FileHeader>();
            pathFileSystemWatchers = new Dictionary<string, FileSystemWatcher>();
        }

        public long ReadFromFile(int handle, out byte[] data, long length)
        {
            if (handle == 0)
            {
                data = new byte[0];
                return 0;
            }

            if (openFiles.TryGetValue(handle, out var fileHeader))
            {
                data = File.ReadAllBytes(fileHeader.FilePath);
                return fileHeader.FileSize;
            }
            else
            {
                data = new byte[0];
                return 0;
            }
        }

        public int OpenFile(string path, bool open_for_writing)
        {
            var assetViewModel = SessionView.AllAssets.FirstOrDefault(asset => asset.AssetItem.Location == path);

            if (assetViewModel == null || assetViewModel.Asset is not UltralightContentAsset ultralightContentAsset)
            {
                return 0;
            }

            FileInfo file = new FileInfo(ultralightContentAsset.Source.FullPath);

            int fileID = LastFileID++;

            var directory = Path.GetDirectoryName(ultralightContentAsset.Source.FullPath);
            if (!pathFileSystemWatchers.ContainsKey(directory))
            {
                FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(directory);
                fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;

                fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                fileSystemWatcher.EnableRaisingEvents = true;

                pathFileSystemWatchers.Add(directory, fileSystemWatcher);
            }

            openFiles.Add(fileID, new FileHeader
            {
                FilePath = ultralightContentAsset.Source.FullPath,
                FileSize = file.Length
            });

            return fileID;
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            //UltralightDefaults.HotReload?.Invoke();
        }

        public bool GetFileSize(int fileHandle, out long result)
        {
            if (fileHandle == 0)
            {
                result = 0;
                return false;
            }
            else
            {
                if (openFiles.TryGetValue(fileHandle, out var fileHeader))
                {
                    result = fileHeader.FileSize;
                    return true;
                }

                result = 0;
                return false;
            }
        }

        public void CloseFile(int handle)
        {
            if (handle == 0)
            {
                return;
            }

            openFiles.Remove(handle);
        }

        public bool GetFileMimeType(IntPtr ptrPath, IntPtr result)
        {
            var path = ULStringMarshaler.NativeToManaged(ptrPath);

            var assetViewModel = SessionView.AllAssets.FirstOrDefault(asset => asset.AssetItem.Location == path);

            if (assetViewModel == null || assetViewModel.Asset is not UltralightContentAsset ultralightContentAsset)
            {
                Methods.ulStringAssignCString(result, "");
                return false;
            }

            string mimeType = string.Empty;
            switch (Path.GetExtension(ultralightContentAsset.Source.FullPath).ToLower())
            {
                case ".html":
                    {
                        Methods.ulStringAssignCString(result, "text/html");
                        return true;
                    }
                case ".css":
                    {
                        Methods.ulStringAssignCString(result, "text/css");
                        return true;
                    }
                case ".js":
                    {
                        Methods.ulStringAssignCString(result, "text/javascript");
                        return true;
                    }
                default:
                    {
                        Methods.ulStringAssignCString(result, string.Empty);
                        return false;
                    }
            }
        }

        public bool FileExists(string path)
        {
            var assetViewModel = SessionView.AllAssets.FirstOrDefault(asset => asset.AssetItem.Location == path);
            return assetViewModel != null && assetViewModel.Asset is UltralightContentAsset ultralightContentAsset;
        }

    }
}
