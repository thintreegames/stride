// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Assets.SpriteFont;
using System.Threading.Tasks;
using Stride.Core.Serialization.Contents;
using System.IO;
using Stride.Engine;
using Stride.Core.IO;

namespace Stride.Assets.UI
{
    [AssetCompiler(typeof(UltralightContentAsset), typeof(AssetCompilationContext))]
    public sealed class UltralightContentAssetCompiler : AssetCompilerBase
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (UltralightContentAsset)assetItem.Asset;
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new UltralightContentCommand(targetUrlInStorage, asset, assetItem.Package));
        }

        private class UltralightContentCommand : AssetCommand<UltralightContentAsset>
        {
            public UltralightContentCommand(string url, UltralightContentAsset parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                if (!File.Exists(Parameters.Source.FullPath)) return Task.FromResult(ResultStatus.Failed);

                if (MicrothreadLocalDatabases.ProviderService == null) return Task.FromResult(ResultStatus.Failed);
                
                string data = File.ReadAllText(Parameters.Source.FullPath);

                string mimeType = ".html";
                switch (Path.GetExtension(Parameters.Source.FullPath).ToLower())
                {
                    case ".html":
                        mimeType = "text/html";
                        break;
                    case ".css":
                        mimeType = "text/css";
                        break;
                    case ".jpg":
                        mimeType = "text/jpeg";
                        break;
                    case ".jpeg":
                        mimeType = "text/jpeg";
                        break;
                    case ".png":
                        mimeType = "text/png";
                        break;
                    case ".js":
                        mimeType = "text/javascript";
                        break;
                }

                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                assetManager.Save(Url, new UltralightContent() { Content = data, MimeType = mimeType });

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
