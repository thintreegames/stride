using ImpromptuNinjas.UltralightSharp;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Games;
using System;
using System.IO;
using System.Runtime.InteropServices;
using TextCopy;
using AppCore = ImpromptuNinjas.UltralightSharp.Safe.AppCore;
using Clipboard = ImpromptuNinjas.UltralightSharp.Clipboard;
using Config = ImpromptuNinjas.UltralightSharp.Safe.Config;
using String = ImpromptuNinjas.UltralightSharp.String;

namespace Stride.UI
{
    public static class UltralightDefaults
    {
        public static Config DefaultConfig { get; private set; }

        public static string AssetPathDB = "ultralight";

        public static Action HotReload;

        public static void Load(IServiceRegistry services)
        {
            AppCore.EnableDefaultLogger("UltralightLogs.txt");

            var ultralightFileSystem = services.GetService<IUltralightFileSystem>();

            if (ultralightFileSystem == null)
            {
                services.AddService(ultralightFileSystem = new DefaultUltralightFileSystem
                {
                    Content = services.GetSafeServiceAs<ContentManager>()
                });
            }

            DefaultConfig = new Config();

            DefaultConfig.SetEnableImages(true);
            DefaultConfig.SetUseGpuRenderer(false);
            DefaultConfig.SetEnableJavaScript(true);

            DefaultConfig.SetResourcePath("resources");
            DefaultConfig.SetCachePath("cache");
            AppCore.EnablePlatformFileSystem("/");

            AppCore.EnablePlatformFontLoader();


            unsafe
            {
                Ultralight.SetFileSystem(new FileSystem
                {
                    CloseFile = new FnPtr<FileSystemCloseFileCallback>(ultralightFileSystem.CloseFile),
                    FileExists = new FnPtr<FileSystemFileExistsCallback>(ultralightFileSystem.FileExists),
                    GetFileMimeType = new FnPtr<FileSystemGetFileMimeTypeCallback>(ultralightFileSystem.GetFileMimeType),
                    GetFileSize = new FnPtr<FileSystemGetFileSizeCallback>(ultralightFileSystem.GetFileSize),
                    OpenFile = new FnPtr<FileSystemOpenFileCallback>(ultralightFileSystem.OpenFile),
                    ReadFromFile = new FnPtr<FileSystemReadFromFileCallback>(ultralightFileSystem.ReadFromFile)
                });

                Ultralight.SetClipboard(new Clipboard
                {
                    Clear = new FnPtr<ClipboardClearCallback>(ClearClipboard),
                    ReadPlainText = new FnPtr<ClipboardReadPlainTextCallback>(ReadClipboard),
                    WritePlainText = new FnPtr<ClipboardWritePlainTextCallback>(WriteClipboard)
                });
            }
        }

        
        private static unsafe void WriteClipboard(String* text)
        {
            ClipboardService.SetText(text->Read());
        }

        private static void ClearClipboard()
        {
            ClipboardService.SetText("");
        }

        private static unsafe void ReadClipboard(String* result)
        {
            Ultralight.StringAssignString(result, String.Create(ClipboardService.GetText()));
        }
    }
}
