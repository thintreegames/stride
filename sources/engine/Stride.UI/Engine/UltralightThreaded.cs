using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Games;
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

        public class ReloadPageEventMessage : IThreadedMessage
        {
            public Guid SessionGuid;
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

        public class EvalEventMessage : IThreadedMessage
        {
            public Guid SessionGuid;
            public string Javascript;
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

        private static Dictionary<Guid, Dictionary<string, (object, MethodInfo)>> consoleCallbacks;

        static UltralightThreaded()
        {
            bufferMessages = new BufferBlock<IThreadedMessage>();
            sessions = new Dictionary<Guid, ThreadedSession>();
            consoleCallbacks = new Dictionary<Guid, Dictionary<string, (object, MethodInfo)>>();
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

        public static void HotReload()
        {
            foreach(var sessionKeyValue in sessions)
            {
                bufferMessages.Post(new ReloadPageEventMessage() { SessionGuid = sessionKeyValue.Key });
            }
        }

        public static void Eval(Guid sessionGuid, string javascript)
        {
            bufferMessages.Post(new EvalEventMessage() { SessionGuid = sessionGuid, Javascript = javascript });        
        }

        public static void RegisterCallback(Guid sessionGuid, string callBack, object viewModel, MethodInfo method)
        {
            if (consoleCallbacks.TryGetValue(sessionGuid, out var lookupCallBacks))
            {
                if (lookupCallBacks.TryGetValue(callBack, out var callbacks))
                {
                    lookupCallBacks[callBack] = (viewModel, method);
                }
                else
                {
                    lookupCallBacks.Add(callBack, (viewModel, method));
                }
            }
            else
            {
                var lookupCallbacks = new Dictionary<string, (object, MethodInfo)>();
                lookupCallbacks.Add(callBack, (viewModel, method));

                consoleCallbacks.Add(sessionGuid, lookupCallbacks);
            }
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
                if (bufferMessages.TryReceiveAll(out var threadedMessages))
                {
                    foreach (var threadedMessage in threadedMessages)
                    {
                        switch (threadedMessage)
                        {
                            case ThreadedSessionCreation creation:
                                {
                                    var session = new Session(renderer, true, creation.SessionGuid.ToString());

                                    var view = new View(renderer, creation.Width, creation.Height, new ULViewConfig
                                    {
                                        EnableImages = true,
                                        EnableJavaScript = true,
                                        IsTransparent = true,
                                        InitialFocus = true,
                                    }, session);

                                    var sessionGuidData = creation.SessionGuid.ToByteArray();

                                    var ptr = Marshal.AllocHGlobal(sessionGuidData.Length);

                                    unsafe
                                    {
                                        var span = new Span<byte>(ptr.ToPointer(), sessionGuidData.Length);
                                        sessionGuidData.CopyTo(span);
                                    }

                                    view.SetAddConsoleMessageCallback(ConsoleMessageCallback, ptr);

                                    sessions.Add(creation.SessionGuid, new ThreadedSession { ULSession = session, ULView = view });
                                }
                                break;

                            case LoadSessionUrl sessionUrl:
                                {
                                    if (sessions.TryGetValue(sessionUrl.SessionGuid, out var threadedSession))
                                    {
                                        threadedSession.ULView.URL = sessionUrl.Url;
                                    }
                                }
                                break;

                            case ReloadPageEventMessage reloadPage:
                                {
                                    if (sessions.TryGetValue(reloadPage.SessionGuid, out var threadedSession))
                                    {
                                        threadedSession.ULView.Reload();
                                    }
                                }
                                break;
                            case FireMouseInputEventMessage inputMouseEventMessage:
                                {
                                    if (sessions.TryGetValue(inputMouseEventMessage.SessionGuid, out var threadedSession))
                                    {
                                        threadedSession.ULView.FireMouseEvent(inputMouseEventMessage.MouseEvent);
                                    }
                                }
                                break;
                            case FireKeyInputEventMessage inputKeyEventMessage:
                                {
                                    if (sessions.TryGetValue(inputKeyEventMessage.SessionGuid, out var threadedSession))
                                    {
                                        threadedSession.ULView.FireKeyEvent(inputKeyEventMessage.KeyEvent);
                                    }
                                }
                                break;
                            case EvalEventMessage evalEventMessage:
                                {
                                    if (sessions.TryGetValue(evalEventMessage.SessionGuid, out var threadedSession))
                                    {
                                        threadedSession.ULView.EvaluateScript(evalEventMessage.Javascript, out string exception);
                                    }
                                }
                                break;
                        }
                    }
                }

                renderer.Update();
                renderer.Render();

                Thread.Sleep(10);
            }

            sessions.Clear();
        }

        private static void ConsoleMessageCallback(IntPtr user_data, View caller,
            ULMessageSource source, ULMessageLevel level, 
            string message, uint line_number, uint column_number, string source_id)
        {
            System.Diagnostics.Debug.WriteLine(message);

            if (level == ULMessageLevel.Error) return;

            unsafe
            {
                var span = new ReadOnlySpan<byte>(user_data.ToPointer(), 16);

                var sessionId = new Guid(span);

                if (consoleCallbacks.TryGetValue(sessionId, out var callbacks))
                {
                    var msgs = message.Split(' ');

                    if (msgs.Length < 1) return;

                    var callbackName = msgs[0];

                    if (callbacks.TryGetValue(callbackName, out var method))
                    {
                        var parameters = method.Item2.GetParameters();

                        var args = new object[msgs.Length - 1];

                        if (parameters.Length != args.Length) throw new ArgumentException("Parameters of method delegate is not the same as the message sent");

                        for (int i = 0; i < args.Length; i++)
                        {
                            var argMsg = msgs[i + 1];
                            var parameter = parameters[i];

                            switch(parameter.ParameterType)
                            {
                                case Type t when t == typeof(string):
                                    args[i] = argMsg;
                                    break;

                                case Type t when t == typeof(bool):
                                    args[i] = bool.Parse(argMsg);
                                    break;

                                case Type t when t == typeof(sbyte):
                                    args[i] = sbyte.Parse(argMsg);
                                    break;
                                case Type t when t == typeof(byte):
                                    args[i] = byte.Parse(argMsg);
                                    break;
                                case Type t when t == typeof(short):
                                    args[i] = short.Parse(argMsg);
                                    break;
                                case Type t when t == typeof(ushort):
                                    args[i] = ushort.Parse(argMsg);
                                    break;
                                case Type t when t == typeof(int):
                                    args[i] = int.Parse(argMsg);
                                    break;
                                case Type t when t == typeof(uint):
                                    args[i] = uint.Parse(argMsg);
                                    break;
                                case Type t when t == typeof(long):
                                    args[i] = int.Parse(argMsg);
                                    break;
                                case Type t when t == typeof(ulong):
                                    args[i] = uint.Parse(argMsg);
                                    break;

                                case Type t when t == typeof(float):
                                    args[i] = float.Parse(argMsg);
                                    break;
                                case Type t when t == typeof(double):
                                    args[i] = double.Parse(argMsg);
                                    break;

                                default:
                                    throw new NotSupportedException($"Type {parameter.ParameterType} is not a supported javascript eval type");
                            }
                        }

                        method.Item2.Invoke(method.Item1, args);
                    }

                }
            }
        }
    }
}
