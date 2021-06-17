using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ImpromptuNinjas.UltralightSharp;
using String = ImpromptuNinjas.UltralightSharp.String;

namespace Stride.UI
{
    public unsafe interface IUltralightFileSystem
    {
        long ReadFromFile(UIntPtr handle, sbyte* data, long length);
        UIntPtr OpenFile(String* path, bool openForWriting);
        bool FileExists(String* path);
        void CloseFile(UIntPtr handle);
        bool GetFileMimeType(String* path, String* result);
        bool GetFileSize(UIntPtr handle, long* result);
    }
}
