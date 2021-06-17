using System;
using Stride.Core.Assets;

namespace Stride.Assets.UI
{
    public class UltralightContentImporter : RawAssetImporterBase<UltralightContentAsset>
    {
        // Supported file extensions for this importer
        public const string FileExtensions = ".html,.css,.js";
        private static readonly Guid Uid = new Guid("DCA0341A-6384-4D34-97EA-6558FEE8FA5D");

        public override Guid Id => Uid;

        public override string Description => "Ultralight content importer for importing web based assets";

        public override string SupportedFileExtensions => FileExtensions;
    }
}
