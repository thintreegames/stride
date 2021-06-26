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
using Stride.Core.Assets.TextAccessors;
using Stride.Core.Serialization;

namespace Stride.Assets.UI
{
    [AssetCompiler(typeof(HtmlFileAsset), typeof(AssetCompilationContext))]
    public class HtmlFileAssetCompiler : WebFileAssetCompiler<HtmlFileAsset, HtmlContent> { }

    [AssetCompiler(typeof(CSSFileAsset), typeof(AssetCompilationContext))]
    public class CSSFileAssetCompiler : WebFileAssetCompiler<CSSFileAsset, CSSContent> { }

    [AssetCompiler(typeof(JavaScriptFileAsset), typeof(AssetCompilationContext))]
    public class JavaScriptFileAssetCompiler : WebFileAssetCompiler<JavaScriptFileAsset, JavaScriptContent> { }

    public abstract class WebFileAssetCompiler<T, O> : AssetCompilerBase where T : WebFileAsset where O : WebContent, new()
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (T)assetItem.Asset;
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new WebFileAssetCommand(targetUrlInStorage, asset, assetItem.Package));
        }

        private class WebFileAssetCommand : AssetCommand<T>
        {
            public WebFileAssetCommand(string url, T parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
            }

            public override IEnumerable<ObjectUrl> GetInputFiles()
            {
                if (Parameters.TextAccessor is DefaultTextAccessor defaultTextAccessor)
                {
                    yield return new ObjectUrl(UrlType.File, defaultTextAccessor.FilePath);
                }
            }

            protected override void ComputeAssemblyHash(BinarySerializationWriter writer)
            {
                base.ComputeAssemblyHash(writer);
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                if (Parameters.TextAccessor is DefaultTextAccessor defaultTextAccessor)
                {
                    var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                    assetManager.Save(Url, new O() { Content = File.ReadAllText(defaultTextAccessor.FilePath) });
                }
                else
                {
                    var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                    assetManager.Save(Url, new O() { Content = Parameters.TextAccessor.Get() });
                }

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
