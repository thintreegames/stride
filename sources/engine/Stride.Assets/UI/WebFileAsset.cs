// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core;
using Stride.Engine;

namespace Stride.Assets.UI
{
    [DataContract("WebFileAsset")]
    public abstract class WebFileAsset : SourceCodeAsset
    {
        public abstract string MimeType { get; }
    }


    [DataContract("HtmlFileAsset")]
    [AssetDescription(Extension, AlwaysMarkAsRoot = true, AllowArchetype = false, Referenceable = false)]
    [AssetContentType(typeof(HtmlContent))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.1.0.1")]
    public sealed partial class HtmlFileAsset : WebFileAsset
    {
        private const string CurrentVersion = "2.1.0.1";

        public const string Extension = ".html";

        public override string MimeType => "text/html";
    }

    [DataContract("CSSFileAsset")]
    [AssetDescription(Extension, AlwaysMarkAsRoot = true, AllowArchetype = false, Referenceable = false)]
    [AssetContentType(typeof(CSSContent))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.1.0.1")]
    public sealed partial class CSSFileAsset : WebFileAsset
    {
        private const string CurrentVersion = "2.1.0.1";

        public const string Extension = ".css";

        public override string MimeType => "text/css";
    }

    [DataContract("JavaScriptFileAsset")]
    [AssetDescription(Extension, AlwaysMarkAsRoot = true, AllowArchetype = false, Referenceable = false)]
    [AssetContentType(typeof(JavaScriptContent))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.1.0.1")]
    public sealed partial class JavaScriptFileAsset : WebFileAsset
    {
        private const string CurrentVersion = "2.1.0.1";

        public const string Extension = ".js";

        public override string MimeType => "text/javascript";
    }
}
