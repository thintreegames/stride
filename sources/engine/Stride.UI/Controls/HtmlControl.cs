using Stride.Core;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.UI.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UltralightNet;

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

        [DataMemberIgnore]
        public Guid SessionGuid;

        public HtmlControl()
        {
            CanBeHitByUser = true;
            jsFunctions = new List<(string, MethodInfo, HtmlViewModel)>();
        }

        public void CreateSession(uint width, uint height)
        {
            //View.SetAddConsoleMessageCallback(ConsoleMessageCallback);
            //View.SetDOMReadyCallback(DomReady);

            //View.SetFinishLoadingCallback((user_data, caller, frame_id, is_main_frame, url) =>
            //{
            //    loaded = true;
            //});

        }


        private void ConsoleMessageCallback(IntPtr user_data, View caller, ULMessageSource source, ULMessageLevel level,
            string message, uint lineNumber, uint columnNumber, string sourceId)
        {
            switch (level)
            {
                default:
                    System.Diagnostics.Debug.WriteLine($"[Ultralight Console] [{level}] {sourceId}:{lineNumber}:{columnNumber} {message}");
                    break;
                case ULMessageLevel.Error:
                    System.Diagnostics.Debug.WriteLine($"[Ultralight Console] {sourceId}:{lineNumber}:{columnNumber} {message}");
                    break;
                case ULMessageLevel.Warning:
                    System.Diagnostics.Debug.WriteLine($"[Ultralight Console] {sourceId}:{lineNumber}:{columnNumber} {message}");
                    break;
            }
        }

        private void DomReady(IntPtr user_data, View caller, ulong frame_id, bool is_main_frame, string url)
        {
            foreach (var jsFunction in jsFunctions)
            {
                //RegisterGlobalJsFunc(caller, jsFunction.Item1, jsFunction.Item2, jsFunction.Item3);
            }
        }

        /*
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
        }*/

        protected override void OnTouchDown(TouchEventArgs args)
        {
            base.OnTouchDown(args);

            if (SessionGuid == Guid.Empty) return;

            int screenX = (int)(args.ScreenPosition.X * args.Source.ActualWidth);
            int screenY = (int)(args.ScreenPosition.Y * args.Source.ActualHeight);

            UltralightThreaded.FireMouseEvent(SessionGuid, new ULMouseEvent(ULMouseEvent.ULMouseEventType.MouseDown, screenX, screenY, ULMouseEvent.Button.Left));
        }

        protected override void OnTouchUp(TouchEventArgs args)
        {
            base.OnTouchUp(args);

            if (SessionGuid == Guid.Empty) return;

            int screenX = (int)(args.ScreenPosition.X * args.Source.ActualWidth);
            int screenY = (int)(args.ScreenPosition.Y * args.Source.ActualHeight);

            UltralightThreaded.FireMouseEvent(SessionGuid, new ULMouseEvent(ULMouseEvent.ULMouseEventType.MouseUp, screenX, screenY, ULMouseEvent.Button.Left));
        }

        protected override void OnTouchMove(TouchEventArgs args)
        {
            base.OnTouchMove(args);

            if (SessionGuid == Guid.Empty) return;

            int screenX = (int)(args.ScreenPosition.X * args.Source.ActualWidth);
            int screenY = (int)(args.ScreenPosition.Y * args.Source.ActualHeight);

            UltralightThreaded.FireMouseEvent(SessionGuid, new ULMouseEvent(ULMouseEvent.ULMouseEventType.MouseMoved, screenX, screenY, ULMouseEvent.Button.None));
        }

        protected override void Update(GameTime time)
        {
            base.Update(time);

            if (input == null)
            {
                input = UIElementServices.Services?.GetService<InputManager>();
                return;
            }

            if (input != null)
            {
                ULKeyEventModifiers modifiers = 0;

                if (input.IsKeyDown(Keys.LeftAlt) ||
                    input.IsKeyDown(Keys.RightAlt))
                {
                    modifiers |= ULKeyEventModifiers.AltKey;
                }


                if (input.IsKeyDown(Keys.LeftCtrl) ||
                    input.IsKeyDown(Keys.RightCtrl))
                {
                    modifiers |= ULKeyEventModifiers.CtrlKey;
                }

                if (input.IsKeyDown(Keys.LeftWin) ||
                    input.IsKeyDown(Keys.RightWin))
                {
                    modifiers |= ULKeyEventModifiers.MetaKey;
                }

                if (input.IsKeyDown(Keys.LeftShift) ||
                    input.IsKeyDown(Keys.RightShift))
                {
                    modifiers |= ULKeyEventModifiers.ShiftKey;
                }

                foreach (var key in input.PressedKeys)
                {
                    var ultralightKey = new StrideKey2UtlraLight(key);
                    var ultraLightText = ultralightKey.GetText();

                    if (modifiers == 0 || modifiers.HasFlag(ULKeyEventModifiers.ShiftKey))
                    {
                        if (ultraLightText != null)
                        {
                            UltralightThreaded.FireKeyEvent(SessionGuid, new ULKeyEvent(ULKeyEventType.Char,
                                modifiers,
                                ultralightKey.GetVirtualKeyCode(),
                                0,
                                ultraLightText,
                                "",
                                false,
                                false,
                                ultralightKey.IsSystemKey()));
                        }
                    }

                    UltralightThreaded.FireKeyEvent(SessionGuid, new ULKeyEvent(ULKeyEventType.RawKeyDown,
                        modifiers,
                        ultralightKey.GetVirtualKeyCode(),
                        0,
                        "",
                        "",
                        ultralightKey.IsKeypad(),
                        false,
                        ultralightKey.IsSystemKey()));
                }

                foreach (var key in input.ReleasedKeys)
                {
                    var ultralightKey = new StrideKey2UtlraLight(key);

                    UltralightThreaded.FireKeyEvent(SessionGuid, new ULKeyEvent(ULKeyEventType.KeyUp,
                            modifiers,
                            ultralightKey.GetVirtualKeyCode(),
                            0,
                            "",
                            "",
                            ultralightKey.IsKeypad(),
                            false,
                            ultralightKey.IsSystemKey()));
                }
            }
        }

        /// <summary>
        /// Method triggered when the <see cref="Url"/> changes.
        /// Can be overridden in inherited class to changed the default behavior.
        /// </summary>
        protected virtual void OnUrlChanged()
        {
            if (string.IsNullOrEmpty(Url)) return;

            if (SessionGuid == Guid.Empty) return;

            UltralightThreaded.LoadUrl(SessionGuid, Url);

            InvalidateMeasure();
        }

        public void LoadHtml(string html)
        {

        }

        public void LoadUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            if (SessionGuid == Guid.Empty) return;

            UltralightThreaded.LoadUrl(SessionGuid, url);
        }

        public Texture GetCacheTexture(View view, GraphicsDevice device, GraphicsContext context)
        {
            var surface = view.Surface;

            if (cachedTexture == null || !surface.DirtyBounds.IsEmpty)
            {
                if (cachedTexture != null)
                {
                    cachedTexture.Dispose();
                    cachedTexture = null;
                }

                var bitmap = surface.Bitmap;
                var pixels = bitmap.LockPixels();

                cachedTexture = Texture.New2D(device, (int)view.Width, (int)view.Height, PixelFormat.B8G8R8A8_UNorm_SRgb);

                cachedTexture.SetData(context.CommandList, new DataPointer(pixels, (int)bitmap.Size));

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
