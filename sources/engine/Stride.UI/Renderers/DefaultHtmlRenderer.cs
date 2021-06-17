using ImpromptuNinjas.UltralightSharp.Enums;
using ImpromptuNinjas.UltralightSharp.Safe;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Renderers;
using Color = Stride.Core.Mathematics.Color;
using String = ImpromptuNinjas.UltralightSharp.String;

using JavaScriptCore = ImpromptuNinjas.UltralightSharp.JavaScriptCore;
using JsValue = ImpromptuNinjas.UltralightSharp.JsValue;
using JsContext = ImpromptuNinjas.UltralightSharp.JsContext;
using Stride.Input;
using MouseButton = ImpromptuNinjas.UltralightSharp.Enums.MouseButton;
using KeyEvent = ImpromptuNinjas.UltralightSharp.Safe.KeyEvent;
using Stride.Games;
using System;

namespace Stride.UI.Renderers
{
    public class DefaultHtmlRenderer : ElementRenderer
    {
        //private volatile bool hotReload;

        private Renderer renderer;

        private bool init;

        public DefaultHtmlRenderer(IServiceRegistry services)
            : base(services)
        {
            if (!init)
            {
                UltralightDefaults.Load(services);

                renderer = new Renderer(UltralightDefaults.DefaultConfig);

                init = true;
            }

            //UltralightDefaults.HotReload += () => hotReload = true;
        }

        protected override void Destroy()
        {
            base.Destroy();

            renderer.Dispose();
        }    

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var htmlControl = (HtmlControl)element;
            if (htmlControl == null) return;

            if (htmlControl.ViewRender == null)
            {
                htmlControl.Init(renderer, 1280, 720);
                return;
            }

            if (htmlControl.ViewRender.IsLoading()) return;

            renderer.Update();
            renderer.Render();

            var cachedTexture = htmlControl.GetCacheTexture(GraphicsDevice, context.GraphicsContext);

            if (cachedTexture != null)
            {
                var sourceRect = new RectangleF(0, 0, cachedTexture.Width, cachedTexture.Height);

                var colorTint = Color.White;
                var worldMatrix = element.WorldMatrix;
                var renderSize = element.RenderSize;
                var borderSize = Vector4.Zero;

                Batch.DrawImage(cachedTexture, ref worldMatrix, ref sourceRect,
                    ref renderSize, ref borderSize,
                    ref colorTint, context.DepthBias, ImageOrientation.AsIs);
            }
        }
    }
}
