// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Compiler;
using Stride.Assets.Presentation.Resources.Thumbnails;
using Stride.Assets.Scripts;
using Stride.Editor.Thumbnails;
using Stride.Assets.UI;

namespace Stride.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(HtmlFileAsset), typeof(ThumbnailCompilationContext))]
    public class HtmlFileThumbnailCompiler : StaticThumbnailCompiler<HtmlFileAsset>
    {
        public HtmlFileThumbnailCompiler()
            : base(StaticThumbnails.HtmlThumbnail) { }
    }

    [AssetCompiler(typeof(CSSFileAsset), typeof(ThumbnailCompilationContext))]
    public class CSSFileThumbnailCompiler : StaticThumbnailCompiler<CSSFileAsset>
    {
        public CSSFileThumbnailCompiler()
            : base(StaticThumbnails.CSSThumbnail) { }
    }


    [AssetCompiler(typeof(JavaScriptFileAsset), typeof(ThumbnailCompilationContext))]
    public class JavaScriptFileThumbnailCompiler : StaticThumbnailCompiler<JavaScriptFileAsset>
    {
        public JavaScriptFileThumbnailCompiler()
            : base(StaticThumbnails.JavaScriptThumbnail) { }
    }
}
