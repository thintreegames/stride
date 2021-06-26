// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.UI;

namespace Stride.Engine
{
    [DataContract("WebContent")]
    public abstract class WebContent : ComponentBase
    {
        /// <summary>
        /// Text based ultralight content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Mime type of content
        /// </summary>
        public abstract string MimeType { get; }
    }

    [DataContract("HtmlContent")]
    [ContentSerializer(typeof(DataContentSerializerWithReuse<HtmlContent>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<HtmlContent>), Profile = "Content")]
    public class HtmlContent : WebContent
    {
        public override string MimeType => "text/html";
    }

    [DataContract("CSSContent")]
    [ContentSerializer(typeof(DataContentSerializerWithReuse<CSSContent>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<CSSContent>), Profile = "Content")]
    public class CSSContent : WebContent
    {
        public override string MimeType => "text/css";
    }

    [DataContract("JavaScriptContent")]
    [ContentSerializer(typeof(DataContentSerializerWithReuse<JavaScriptContent>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<JavaScriptContent>), Profile = "Content")]
    public class JavaScriptContent : WebContent
    {
        public override string MimeType => "text/javascript";
    }
}
