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
using Stride.Core.Assets.TextAccessors;
using System.Threading;
using Stride.UI.Engine;

namespace Stride.Editor.EditorGame.Game
{
    public unsafe class EditorUltralightFileSystem : IUltralightFileSystem
    {
        public SessionViewModel SessionView { get; set; }

        private static int LastFileID = 1;

        private Dictionary<int, FileStream> openFiles;

        private Dictionary<string, FileSystemWatcher> pathFileSystemWatchers;

        public EditorUltralightFileSystem()
        {
            openFiles = new Dictionary<int, FileStream>();
            pathFileSystemWatchers = new Dictionary<string, FileSystemWatcher>();
        }

        public long ReadFromFile(int handle, out byte[] data, long length)
        {
            if (handle == 0)
            {
                data = new byte[0];
                return 0;
            }

            if (openFiles.TryGetValue(handle, out var fileStream))
            {
                data = new byte[length];
                return fileStream.Read(data, 0, (int)length);
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

            if (assetViewModel == null) return 0;

            if (assetViewModel.Asset is not WebFileAsset webFileAsset) return 0;

            int fileID = LastFileID++;

            if (webFileAsset.TextAccessor is not DefaultTextAccessor defaultTextAccessor) return 0;

            if (string.IsNullOrEmpty(defaultTextAccessor.FilePath)) return 0;

            FileStream fileStream = null;
            int timeoutLimit = 5;
            for (int i = 0; i < timeoutLimit; i++)
            {
                if (AttemptFileOpen(new FileInfo(defaultTextAccessor.FilePath), out fileStream))
                {
                    break;
                }

                Thread.Sleep(100);
            }

            if (fileStream == null) return 0;

            var directory = Path.GetDirectoryName(defaultTextAccessor.FilePath);
            if (!pathFileSystemWatchers.ContainsKey(directory))
            {
                FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(directory);
                fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;

                fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                fileSystemWatcher.EnableRaisingEvents = true;

                pathFileSystemWatchers.Add(directory, fileSystemWatcher);
            }

            openFiles.Add(fileID, fileStream);

            return fileID;
        }

        protected bool AttemptFileOpen(FileInfo file, out FileStream stream)
        {
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                stream = null;
                return false;
            }
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            UltralightThreaded.HotReload();
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
                if (openFiles.TryGetValue(fileHandle, out var fileStream))
                {
                    result = fileStream.Length;
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

            if (openFiles.TryGetValue(handle, out var fileStream))
            {
                fileStream.Close();
            }

            openFiles.Remove(handle);
        }

        public bool GetFileMimeType(IntPtr ptrPath, IntPtr result)
        {
            var path = ULStringMarshaler.NativeToManaged(ptrPath);

            var assetViewModel = SessionView.AllAssets.FirstOrDefault(asset => asset.AssetItem.Location == path);

            if (assetViewModel == null)
            {
                Methods.ulStringAssignCString(result, string.Empty);
                return false;
            }

            if (assetViewModel.Asset is WebFileAsset webFileAsset)
            {
                Methods.ulStringAssignCString(result, webFileAsset.MimeType);
                return true;
            }

            Methods.ulStringAssignCString(result, string.Empty);
            return false;
        }

        public bool FileExists(string path)
        {
            var assetViewModel = SessionView.AllAssets.FirstOrDefault(asset => asset.AssetItem.Location == path);
            return assetViewModel != null && assetViewModel.Asset is WebFileAsset webFileAsset;
        }

    }
}
