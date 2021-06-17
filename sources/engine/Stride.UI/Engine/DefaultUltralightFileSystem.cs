using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Stride.UI;

using ImpromptuNinjas.UltralightSharp;
using String = ImpromptuNinjas.UltralightSharp.String;
using System.Collections.Generic;
using Stride.Core.Serialization.Contents;
using Stride.Core.Assets;
using Stride.Engine;
using System.Text.Unicode;
using System.Text;

namespace Stride.UI
{
    public unsafe class DefaultUltralightFileSystem : IUltralightFileSystem
    {
        public struct FileHeader
        {
            public string ContentPath;
        }

        public ContentManager Content { get; set; }

        private static int LastFileID = 1;

        private Dictionary<int, FileHeader> openFiles;

        public DefaultUltralightFileSystem()
        {
            openFiles = new Dictionary<int, FileHeader>();
        }

        public long ReadFromFile(UIntPtr handle, sbyte* data, long length)
        {
            byte* handlePtr = (byte*)handle.ToPointer();
            if (openFiles.TryGetValue(*(int*)handle.ToPointer(), out var fileHeader))
            {
                var rawAsset = Content.Get<UltralightContent>(fileHeader.ContentPath);

                if (rawAsset == null)
                {
                    return 0;
                }

                var htmlData = Encoding.UTF8.GetBytes(rawAsset.Content);

                UnmanagedMemoryStream writeStream = new UnmanagedMemoryStream((byte*)data, htmlData.Length, htmlData.Length, FileAccess.Write);
                writeStream.Write(htmlData, 0, htmlData.Length);
                writeStream.Close();

                return htmlData.Length;
            }
            else
            {
                return 0;
            }
        }

        public UIntPtr OpenFile(String* path, bool openForWriting)
        {
            if (!FileExists(path))
            {
                return UIntPtr.Zero;
            }

            Content.Load<UltralightContent>(path->Read());

            int fileID = LastFileID++;
            openFiles.Add(fileID, new FileHeader
            {
                ContentPath = path->Read()
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
                    var rawAsset = Content.Get<UltralightContent>(fileHeader.ContentPath);

                    if (rawAsset == null)
                    {
                        *result = 0;
                        return false;
                    }

                    *result = Encoding.UTF8.GetByteCount(rawAsset.Content);
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

            if (openFiles.TryGetValue(openFileId, out var fileHeader))
            {
                Content.Unload(fileHeader.ContentPath);
            }

            openFiles.Remove(openFileId);

            Marshal.FreeHGlobal((IntPtr)handlePtr);
        }

        public bool GetFileMimeType(String* path, String* result)
        {
            var rawAsset = Content.Get<UltralightContent>(path->Read());

            if (rawAsset == null)
            {
                return false;
            }

            Ultralight.StringAssignString(result, String.Create(rawAsset.MimeType));
            return true;
        }

        public bool FileExists(String* path) => Content.Exists(path->Read());

    }
}
