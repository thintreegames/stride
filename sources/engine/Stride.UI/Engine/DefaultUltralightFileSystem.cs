using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Stride.UI;

using System.Collections.Generic;
using Stride.Core.Serialization.Contents;
using Stride.Core.Assets;
using Stride.Engine;
using System.Text.Unicode;
using System.Text;
using UltralightNet;

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

        public long ReadFromFile(int handle, out byte[] data, long length)
        {
            if (handle == 0)
            {
                data = new byte[0];
                return 0;
            }

            if (openFiles.TryGetValue(handle, out var fileHeader))
            {
                var webContent = Content.Get<WebContent>(fileHeader.ContentPath);

                if (webContent == null)
                {
                    data = new byte[0];
                    return 0;
                }

                data = Encoding.UTF8.GetBytes(webContent.Content);
                return data.Length;
            }
            else
            {
                data = new byte[0];
                return 0;
            }
        }

        public int OpenFile(string path, bool open_for_writing)
        {
            if (!FileExists(path))
            {
                return 0;
            }

            Content.Load<WebContent>(path);

            int fileID = LastFileID++;
            openFiles.Add(fileID, new FileHeader
            {
                ContentPath = path
            });

            return fileID;
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
                    var webContent = Content.Get<WebContent>(fileHeader.ContentPath);

                    if (webContent == null)
                    {
                        result = 0;
                        return false;
                    }

                    result = Encoding.UTF8.GetByteCount(webContent.Content);
                    return true;
                }

                result = 0;
                return false;
            }
        }

        public void CloseFile(int handle)
        {
            if (handle == 0) return;

            if (openFiles.TryGetValue(handle, out var fileHeader))
            {
                Content.Unload(fileHeader.ContentPath);
            }

            openFiles.Remove(handle);
        }

        public bool GetFileMimeType(IntPtr ptrPath, IntPtr result)
        {
            var path = ULStringMarshaler.NativeToManaged(ptrPath);

            var webContent = Content.Get<WebContent>(path);

            if (webContent == null)
            {
                Methods.ulStringAssignCString(result, "");
                return false;
            }

            Methods.ulStringAssignCString(result, webContent.MimeType);
            return true;
        }

        public bool FileExists(string path) => Content.Exists(path);

    }
}
