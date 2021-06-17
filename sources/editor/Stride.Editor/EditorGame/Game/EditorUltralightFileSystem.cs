using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.UI;

using ImpromptuNinjas.UltralightSharp;
using String = ImpromptuNinjas.UltralightSharp.String;
using System.Collections.Generic;
using Stride.Assets.UI;
using System.Text;

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

        public long ReadFromFile(UIntPtr handle, sbyte* data, long length)
        {
            byte* handlePtr = (byte*)handle.ToPointer();
            if (openFiles.TryGetValue(*(int*)handle.ToPointer(), out var fileHeader))
            {
                var html = File.ReadAllText(fileHeader.FilePath);

                var htmlData = Encoding.UTF8.GetBytes(html);

                UnmanagedMemoryStream writeStream = new UnmanagedMemoryStream((byte*)data, htmlData.Length, htmlData.Length, FileAccess.Write);
                writeStream.Write(htmlData, 0, htmlData.Length);
                writeStream.Close();

                return html.Length;
            }
            else
            {
                return 0;
            }
        }

        public UIntPtr OpenFile(String* path, bool openForWriting)
        {
            var assetLoc = path->Read();
            var assetViewModel = SessionView.AllAssets.FirstOrDefault(asset => asset.AssetItem.Location == assetLoc);

            if (assetViewModel == null || assetViewModel.Asset is not UltralightContentAsset ultralightContentAsset)
            {
                return UIntPtr.Zero;
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

            IntPtr memIntPtr = Marshal.AllocHGlobal(sizeof(int));
            byte* memBytePtr = (byte*)memIntPtr.ToPointer();

            Span<int> span = new Span<int>(memBytePtr, 1);
            span.Fill(fileID);

            return (UIntPtr)memBytePtr;
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            UltralightDefaults.HotReload?.Invoke();
        }

        public bool GetFileSize(UIntPtr handle, long* result)
        {
            if (handle == UIntPtr.Zero)
            {
                *result = 0;
                return false;
            }
            else
            {
                byte* handlePtr = (byte*)handle.ToPointer();
                if (openFiles.TryGetValue(*(int*)handle.ToPointer(), out var fileHeader))
                {
                    *result = fileHeader.FileSize;
                    return true;
                }

                *result = 0;
                return false;
            }
        }

        public void CloseFile(UIntPtr handle)
        {
            if (handle == UIntPtr.Zero)
            {
                return;
            }

            byte* handlePtr = (byte*)handle.ToPointer();
            int openFileId = *(int*)handlePtr;
            openFiles.Remove(openFileId);
            Marshal.FreeHGlobal((IntPtr)handlePtr);
        }

        public bool GetFileMimeType(String* path, String* result)
        {
            var assetViewModel = SessionView.AllAssets.FirstOrDefault(asset => asset.AssetItem.Location == path->Read());

            if (assetViewModel == null || assetViewModel.Asset is not UltralightContentAsset ultralightContentAsset)
            {
                return false;
            }

            switch (Path.GetExtension(ultralightContentAsset.Source.FullPath).ToLower())
            {
                case ".html":
                    {
                        Ultralight.StringAssignString(result, String.Create("text/html"));
                        return true;
                    }
                case ".css":
                    {
                        Ultralight.StringAssignString(result, String.Create("text/css"));
                        return true;
                    }
                case ".jpg":
                    {
                        Ultralight.StringAssignString(result, String.Create("image/jpeg"));
                        return true;
                    }
                case ".jpeg":
                    {
                        Ultralight.StringAssignString(result, String.Create("image/jpeg"));
                        return true;
                    }
                case ".png":
                    {
                        Ultralight.StringAssignString(result, String.Create("image/png"));
                        return true;
                    }
                case ".js":
                    {
                        Ultralight.StringAssignString(result, String.Create("text/javascript"));
                        return true;
                    }
                default:
                    return false;
            }
        }

        public bool FileExists(String* path)
        {
            var assetViewModel = SessionView.AllAssets.FirstOrDefault(asset => asset.AssetItem.Location == path->Read());

            if (assetViewModel == null || assetViewModel.Asset is not UltralightContentAsset ultralightContentAsset)
            {
                return false;
            }

            return true;
        }

    }
}
