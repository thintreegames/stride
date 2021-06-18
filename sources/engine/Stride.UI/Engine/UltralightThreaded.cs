using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Games;
using Stride.Graphics;
using TextCopy;
using UltralightNet;
using UltralightNet.AppCore;

namespace Stride.UI.Engine
{
    public static class UltralightThreaded
    {
        public interface IThreadedMessage { }

        public class ThreadedSessionCreation : IThreadedMessage
        {
            public Guid SessionGuid;
            public uint Width;
            public uint Height;
        }

        public class LoadSessionUrl : IThreadedMessage
        {
            public Guid SessionGuid;
            public string Url;
        }

        public class FireMouseInputEventMessage : IThreadedMessage
        {
            public Guid SessionGuid;
            public ULMouseEvent MouseEvent;
        }

        public class FireKeyInputEventMessage : IThreadedMessage
        {
            public Guid SessionGuid;
            public ULKeyEvent KeyEvent;
        }

        public class ThreadedSession
        {
            public Session ULSession { get; set; }
            public View ULView { get; set; }
        }

        public static IServiceRegistry Services;

        public static Thread Thread;

        private static BufferBlock<IThreadedMessage> bufferMessages;

        private static Dictionary<Guid, ThreadedSession> sessions;

        static UltralightThreaded()
        {
            bufferMessages = new BufferBlock<IThreadedMessage>();
            sessions = new Dictionary<Guid, ThreadedSession>();
        }

        public static void Load(IServiceRegistry services)
        {
            Services = services;

            Thread = new Thread(UltralightMain);
            Thread.Start();
        }

        public static void CreateSession(Guid sessionGuid, uint width, uint height)
        {
            bufferMessages.Post(new ThreadedSessionCreation() { SessionGuid = sessionGuid, Width = width, Height = height });
        }

        public static void LoadUrl(Guid sessionGuid, string url)
        {
            bufferMessages.Post(new LoadSessionUrl() { SessionGuid = sessionGuid, Url = url });
        }

        public static void FireMouseEvent(Guid sessionGuid, ULMouseEvent mouseEvent)
        {
            bufferMessages.Post(new FireMouseInputEventMessage() { SessionGuid = sessionGuid, MouseEvent = mouseEvent });
        }

        public static void FireKeyEvent(Guid sessionGuid, ULKeyEvent keyEvent)
        {
            bufferMessages.Post(new FireKeyInputEventMessage() { SessionGuid = sessionGuid, KeyEvent = keyEvent });
        }

        public static View GetSessionView(Guid sessionGuid)
        {
            if (sessions.TryGetValue(sessionGuid, out var threadedSession))
            {
                return threadedSession.ULView;
            }

            return default;
        }

        public static bool IsSessionReady(Guid sessionGuid)
        {
            if (sessions.TryGetValue(sessionGuid, out var threadedSession))
            {
                return !threadedSession.ULView.IsLoading;
            }

            return false;
        }

        private static void UltralightMain()
        {
            var ultralightFileSystem = Services.GetService<IUltralightFileSystem>();

            if (ultralightFileSystem == null)
            {
                Services.AddService(ultralightFileSystem = new DefaultUltralightFileSystem
                {
                    Content = Services.GetSafeServiceAs<ContentManager>()
                });
            }

            // Set Clipboard
            unsafe
            {
                ULPlatform.SetClipboard(new ULClipboard()
                {
                    WritePlainText = (text) => ClipboardService.SetText(text),

                    ReadPlainText = (IntPtr result) => Methods.ulStringAssignCString(result, ClipboardService.GetText()),

                    Clear = () => ClipboardService.SetText("")
                });
            }

            // Set file system to be handled with strides pipeline, (we use a service for this as editor and game use different sets of pipelines)
            ULPlatform.SetFileSystem(new ULFileSystem()
            {
                CloseFile = ultralightFileSystem.CloseFile,
                FileExists = ultralightFileSystem.FileExists,
                GetFileMimeType = ultralightFileSystem.GetFileMimeType,
                GetFileSize = ultralightFileSystem.GetFileSize,
                OpenFile = ultralightFileSystem.OpenFile,
                ReadFromFile = ultralightFileSystem.ReadFromFile
            });

            ULPlatform.SetLogger(new ULLogger()
            {
                LogMessage = (level, message) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[Ultralight] ({level}): {message}");
                }
            });

            // Enable file system
            //AppCoreMethods.ulEnablePlatformFileSystem("./");

            // Enable logger
            AppCoreMethods.ulEnableDefaultLogger("./UltralightLogs.txt");

            // Set Font Loader
            AppCoreMethods.ulEnablePlatformFontLoader();

            // Create config, used for specifying resources folder (used for URL loading)
            using var ulConfig = new ULConfig()
            {
                ResourcePath = "./resources" // Requires "UltralightNet.Resources"
            };

            // Set Renderer
            using var renderer = new Renderer(ulConfig, false);

            var game = Services.GetService<IGame>();

            while (game.IsRunning)
            {
                if (bufferMessages.TryReceive(out var threadedMessage))
                {
                    if (threadedMessage is ThreadedSessionCreation creation)
                    {
                        var session = new Session(renderer, true, creation.SessionGuid.ToString());

                        var view = new View(renderer, creation.Width, creation.Height, new ULViewConfig
                        {
                            EnableImages = true,
                            EnableJavaScript = true,
                            IsTransparent = true,
                            InitialFocus = true,
                        }, session);

                        sessions.Add(creation.SessionGuid, new ThreadedSession { ULSession = session, ULView = view });
                    }

                    if (threadedMessage is LoadSessionUrl sessionUrl)
                    {
                        if (sessions.TryGetValue(sessionUrl.SessionGuid, out var threadedSession))
                        {
                            threadedSession.ULView.URL = sessionUrl.Url;
                        }
                    }

                    if (threadedMessage is FireMouseInputEventMessage inputMouseEventMessage)
                    {
                        if (sessions.TryGetValue(inputMouseEventMessage.SessionGuid, out var threadedSession))
                        {
                            threadedSession.ULView.FireMouseEvent(inputMouseEventMessage.MouseEvent);
                        }
                    }

                    if (threadedMessage is FireKeyInputEventMessage inputKeyEventMessage)
                    {
                        if (sessions.TryGetValue(inputKeyEventMessage.SessionGuid, out var threadedSession))
                        {
                            threadedSession.ULView.FireKeyEvent(inputKeyEventMessage.KeyEvent);
                        }
                    }
                }

                renderer.Update();
                renderer.Render();

                Thread.Sleep(10);
            }

            sessions.Clear();
        }
    }
}
