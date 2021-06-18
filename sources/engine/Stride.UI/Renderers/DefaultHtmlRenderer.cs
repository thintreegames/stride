using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Games;
using Stride.Graphics;
using Stride.UI.Controls;
using Stride.UI.Engine;
using TextCopy;
using UltralightNet;
using UltralightNet.AppCore;
using Color = Stride.Core.Mathematics.Color;

namespace Stride.UI.Renderers
{
    public class DefaultHtmlRenderer : ElementRenderer
    {
        private static bool init;

        public DefaultHtmlRenderer(IServiceRegistry services)
            : base(services)
        {
            if (!init)
            {
                UltralightThreaded.Load(services);
                init = true;
            }
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var htmlControl = (HtmlControl)element;
            if (htmlControl == null) return;

            if (htmlControl.SessionGuid == Guid.Empty)
            {
                htmlControl.SessionGuid = Guid.NewGuid();
                uint width = (uint)context.Resolution.X;
                uint height = (uint)context.Resolution.Y;

                UltralightThreaded.CreateSession(htmlControl.SessionGuid, width, height);
                UltralightThreaded.LoadUrl(htmlControl.SessionGuid, htmlControl.Url);
            }

            if (!UltralightThreaded.IsSessionReady(htmlControl.SessionGuid)) return;

            var view = UltralightThreaded.GetSessionView(htmlControl.SessionGuid);

            if (view == null) return;

            var cachedTexture = htmlControl.GetCacheTexture(view, GraphicsDevice, context.GraphicsContext);

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
