// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.UI;

namespace Stride.Engine
{
    /// <summary>
    /// Ultralight containing html data.
    /// </summary>
    [DataContract("UltralightContent")]
    [ContentSerializer(typeof(DataContentSerializerWithReuse<UltralightContent>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<UltralightContent>), Profile = "Content")]
    public sealed class UltralightContent : ComponentBase
    {
        /// <summary>
        /// Text based ultralight content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Mime type of content
        /// </summary>
        public string MimeType { get; set; }
    }
}
