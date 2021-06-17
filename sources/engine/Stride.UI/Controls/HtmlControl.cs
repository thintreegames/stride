using ImpromptuNinjas.UltralightSharp.Enums;
using ImpromptuNinjas.UltralightSharp.Safe;
using Stride.Core;
using Stride.Games;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using String = ImpromptuNinjas.UltralightSharp.String;

using JavaScriptCore = ImpromptuNinjas.UltralightSharp.JavaScriptCore;
using JsValue = ImpromptuNinjas.UltralightSharp.JsValue;
using JsContext = ImpromptuNinjas.UltralightSharp.JsContext;
using Stride.Input;
using MouseButton = ImpromptuNinjas.UltralightSharp.Enums.MouseButton;
using KeyEvent = ImpromptuNinjas.UltralightSharp.Safe.KeyEvent;
using System.ComponentModel;
using Stride.Core.Mathematics;
using Stride.Core.IO;
using Stride.Core.Assets;

namespace Stride.UI.Controls
{
    public interface HtmlViewModel
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class JSFunctionAttribute : Attribute
    {
        public string JSName { get; set; }

        public JSFunctionAttribute(string jsName = null)
        {
            JSName = jsName;
        }
    }

    [DataContract(nameof(HtmlControl))]
    [DataContractMetadataType(typeof(HTMLMetadata))]
    [DebuggerDisplay("Html - Name={Name}")]
    [Display(category: InputCategory)]
    public class HtmlControl : ContentControl
    {
        private Texture cachedTexture;
        private InputManager input;

        private volatile bool hotReload;

        [DataMemberIgnore]
        public Session Session;

        [DataMemberIgnore]
        public View ViewRender;

        private List<(string, MethodInfo, HtmlViewModel)> jsFunctions;

        private string url;
        /// <summary>
        /// Gets or sets the url of the html control.
        /// </summary>
        /// <userdoc>The text of the html control.</userdoc>
        [DataMember]
        [DefaultValue(null)]
        public string Url
        {
            get { return url; }
            set
            {
                if (url == value) return;
                url = value;
                OnUrlChanged();
            }
        }


        public HtmlControl()
        {
            CanBeHitByUser = true;
            jsFunctions = new List<(string, MethodInfo, HtmlViewModel)>();
        }

        internal void Init(Renderer renderer, uint resWidth, uint resHeight)
        {
            Session = new Session(renderer, true, string.IsNullOrEmpty(Name) ? "htmlControl" : Name);

            ViewRender = new View(renderer, resWidth, resHeight, true, Session);

            var gch = GCHandle.Alloc(this);
            var gchPtr = GCHandle.ToIntPtr(gch);

            ViewRender.SetAddConsoleMessageCallback(ConsoleMessageCallback, gchPtr);
            ViewRender.SetDomReadyCallback(DomReady, gchPtr);

            ViewRender.Focus();

            ViewRender.LoadUrl(Url);
        }

        private void ConsoleMessageCallback(IntPtr userData, View caller, MessageSource source,
            MessageLevel level, string message, uint lineNumber, uint columnNumber, string sourceId)
        {
            switch (level)
            {
                default:
                    System.Diagnostics.Debug.WriteLine($"[Ultralight Console] [{level}] {sourceId}:{lineNumber}:{columnNumber} {message}");
                    break;
                case MessageLevel.Error:
                    System.Diagnostics.Debug.WriteLine($"[Ultralight Console] {sourceId}:{lineNumber}:{columnNumber} {message}");
                    break;
                case MessageLevel.Warning:
                    System.Diagnostics.Debug.WriteLine($"[Ultralight Console] {sourceId}:{lineNumber}:{columnNumber} {message}");
                    break;
            }
        }

        private void DomReady(IntPtr userData, View caller, ulong frameId, bool isMainFrame, string url)
        {
            foreach (var jsFunction in jsFunctions)
            {
                RegisterGlobalJsFunc(caller, jsFunction.Item1, jsFunction.Item2, jsFunction.Item3);
            }
        }

        public void RegisterViewModel(HtmlViewModel viewModel)
        {
            var methods = viewModel.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(JSFunctionAttribute), false);

                foreach (var attribute in attributes)
                {
                    if (attribute is JSFunctionAttribute jsFunctionAttribute)
                    {
                        var name = jsFunctionAttribute.JSName != null ? jsFunctionAttribute.JSName : method.Name;
                        jsFunctions.Add((name, method, viewModel));
                    }
                }
            }
        }

        private void RegisterGlobalJsFunc(View caller, string jsFuncName, MethodInfo safeJsFunc, HtmlViewModel viewModel)
        {
            unsafe
            {
                Internal_RegisterGlobalJsFunc(caller, jsFuncName, (JsContext* ctx, JsValue* function, JsValue* thisObject,
                    UIntPtr argumentCount, JsValue** arguments, JsValue** exception) =>
                {
                    var argCount = argumentCount.ToUInt32();

                    object[] parameters = new object[argCount];

                    for (int i = 0; i < argCount; i++)
                    {
                        JsValue* arg = arguments[i];

                        if (JavaScriptCore.JsValueIsString(ctx, arg))
                        {
                            var jsString = JavaScriptCore.JsValueToStringCopy(ctx, arg, exception);
                            var len = JavaScriptCore.StringGetLength(jsString).ToUInt32();
                            string data = Marshal.PtrToStringUni((IntPtr)JavaScriptCore.StringGetCharactersPtr(jsString), (int)len);
                            parameters[i] = data;
                        }
                        else if (JavaScriptCore.JsValueIsNumber(ctx, arg))
                        {
                            var data = JavaScriptCore.JsValueToNumber(ctx, arg, exception);
                            parameters[i] = data;
                        }
                        else if (JavaScriptCore.JsValueIsBoolean(ctx, arg))
                        {
                            var data = (bool)JavaScriptCore.JsValueToBoolean(ctx, arg);
                            parameters[i] = data;
                        }
                    }

                    var returnObj = safeJsFunc.Invoke(viewModel, parameters);

                    return null;
                });
            }
        }

        private void Internal_RegisterGlobalJsFunc(View caller, string jsFuncName,
           ImpromptuNinjas.UltralightSharp.ObjectCallAsFunctionCallback funcCallBack)
        {
            var globalContext = caller.LockJsContext();

            unsafe
            {
                var globaObject = globalContext.GetGlobalObject();
                var ctx = globalContext.Unsafe;

                var name = new JsString(jsFuncName).Unsafe;
                var func = JavaScriptCore.JsObjectMakeFunctionWithCallback(ctx, name, funcCallBack);

                JavaScriptCore.JsObjectSetProperty(ctx, globaObject.Unsafe, name, func, JsPropertyAttribute.None, null);
            }

            caller.UnlockJsContext();
        }

        protected override void OnTouchDown(TouchEventArgs args)
        {
            base.OnTouchDown(args);

            int screenX = (int)(args.ScreenPosition.X * ViewRender.GetWidth());
            int screenY = (int)(args.ScreenPosition.Y * ViewRender.GetHeight());

            ViewRender.FireMouseEvent(new MouseEvent(MouseEventType.MouseDown, screenX, screenY, MouseButton.Left));
        }

        protected override void OnTouchUp(TouchEventArgs args)
        {
            base.OnTouchUp(args);

            int screenX = (int)(args.ScreenPosition.X * ViewRender.GetWidth());
            int screenY = (int)(args.ScreenPosition.Y * ViewRender.GetHeight());

            ViewRender.FireMouseEvent(new MouseEvent(MouseEventType.MouseUp, screenX, screenY, MouseButton.Left));
        }


        protected override void OnTouchMove(TouchEventArgs args)
        {
            base.OnTouchMove(args);

            int screenX = (int)(args.ScreenPosition.X * ViewRender.GetWidth());
            int screenY = (int)(args.ScreenPosition.Y * ViewRender.GetHeight());

            ViewRender.FireMouseEvent(new MouseEvent(MouseEventType.MouseMoved, screenX, screenY, MouseButton.None));
        }

        protected override void Update(GameTime time)
        {
            base.Update(time);

            if (input == null)
            {
                input = UIElementServices.Services?.GetService<InputManager>();
                return;
            }
            else if (ViewRender != null)
            {
                if (hotReload && !ViewRender.IsLoading())
                {
                    if (!string.IsNullOrEmpty(Url))
                    {
                        ViewRender.LoadUrl(Url);
                        ViewRender.Reload();
                        cachedTexture = null;
                    }
                    hotReload = false;
                }
            }

            if (input != null)
            {
                uint modifiers = 0;

                if (input.IsKeyDown(Keys.LeftAlt) ||
                    input.IsKeyDown(Keys.RightAlt))
                {
                    modifiers = 1 << 0;
                }


                if (input.IsKeyDown(Keys.LeftCtrl) ||
                    input.IsKeyDown(Keys.RightCtrl))
                {
                    modifiers = 1 << 1;
                }

                if (input.IsKeyDown(Keys.LeftWin) ||
                    input.IsKeyDown(Keys.RightWin))
                {
                    modifiers = 1 << 2;
                }

                if (input.IsKeyDown(Keys.LeftShift) ||
                    input.IsKeyDown(Keys.RightShift))
                {
                    modifiers = 1 << 3;
                }

                foreach (var key in input.PressedKeys)
                {
                    var ultralightKey = new StrideKey2UtlraLight(key);

                    unsafe
                    {
                        var ultraLightText = ultralightKey.GetText();

                        var text = String.Create(ultraLightText);
                        var modifiedText = String.Create("");

                        if (modifiers == 0 || modifiers == 1 << 3)
                        {
                            if (ultraLightText != null)
                            {
                                ViewRender.FireKeyEvent(new KeyEvent(KeyEventType.Char,
                                    modifiers,
                                    ultralightKey.GetVirtualKeyCode(),
                                    0,
                                    text,
                                    text,
                                    false,
                                    false,
                                    ultralightKey.IsSystemKey()));
                            }
                        }

                        ViewRender.FireKeyEvent(new KeyEvent(KeyEventType.RawKeyDown,
                            modifiers,
                            ultralightKey.GetVirtualKeyCode(),
                            0,
                            text,
                            text,
                            ultralightKey.IsKeypad(),
                            false,
                            ultralightKey.IsSystemKey()));
                    }
                }

                foreach (var key in input.ReleasedKeys)
                {
                    var ultralightKey = new StrideKey2UtlraLight(key);

                    unsafe
                    {
                        var text = String.Create(ultralightKey.GetText());
                        var modifiedText = String.Create("");

                        ViewRender.FireKeyEvent(new KeyEvent(KeyEventType.KeyUp,
                            modifiers,
                            ultralightKey.GetVirtualKeyCode(),
                            0,
                            text,
                            modifiedText,
                            ultralightKey.IsKeypad(),
                            false,
                            ultralightKey.IsSystemKey()));
                    }
                }
            }


        }

        /// <summary>
        /// Method triggered when the <see cref="Url"/> changes.
        /// Can be overridden in inherited class to changed the default behavior.
        /// </summary>
        protected virtual void OnUrlChanged()
        {
            if (ViewRender != null)
            {
                LoadUrl(Url);
            }

            InvalidateMeasure();
        }

        public void LoadHtml(string html)
        {
            ViewRender.LoadHtml(html);
        }

        public void LoadUrl(string url)
        {
            if (!ViewRender.IsLoading())
            {
                ViewRender.LoadUrl(url);
            }
        }

        public Texture GetCacheTexture(GraphicsDevice device, GraphicsContext context)
        {
            var surface = ViewRender.GetSurface();

            if (cachedTexture == null || !surface.GetDirtyBounds().IsEmpty())
            {
                if (cachedTexture != null)
                {
                    cachedTexture.Dispose();
                    cachedTexture = null;
                }

                var bitmap = surface.GetBitmap();
                var pixels = bitmap.LockPixels();

                cachedTexture = Texture.New2D(device, (int)ViewRender.GetWidth(), (int)ViewRender.GetHeight(), PixelFormat.B8G8R8A8_UNorm);

                cachedTexture.SetData(context.CommandList, new DataPointer(pixels, (int)bitmap.GetSize().ToUInt64()));

                bitmap.UnlockPixels();

                surface.ClearDirtyBounds();
            }

            return cachedTexture;
        }

        private class HTMLMetadata
        {
        }
    }

}
