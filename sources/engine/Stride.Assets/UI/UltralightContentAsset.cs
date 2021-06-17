// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core;
using Stride.Engine;

namespace Stride.Assets.UI
{
    /// <summary>
    /// This assets represents a tree of UI elements. 
    /// </summary>
    [DataContract("UltralightContentAsset")]
    [AssetDescription(FileExtension, AllowArchetype = false, AlwaysMarkAsRoot = true)]
    [AssetContentType(typeof(UltralightContent))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.1.0.1")]
    public sealed partial class UltralightContentAsset : AssetWithSource
    {
        private const string CurrentVersion = "2.1.0.1";

        /// <summary>
        /// The default file extension used by the <see cref="UltralightContentAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdulc";
    }
}
