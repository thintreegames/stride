using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.UI
{
    public interface IUltralightFileSystem
    {
        long ReadFromFile(int handle, out byte[] data, long length);
        int OpenFile(string path, bool open_for_writing);
        bool FileExists(string path);
        void CloseFile(int handle);
        bool GetFileMimeType(IntPtr path, IntPtr result);
        bool GetFileSize(int fileHandle, out long result);
    }
}
