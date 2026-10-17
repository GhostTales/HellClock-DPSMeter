using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DPSMeter
{
    internal static class BundledFont
    {
        const string ResourceName = "DPSMeter.Fonts.ibmplexfonts";
        const string RegularFontName = "IBMPlexSansCondensed-Regular";
        static AssetBundle _assetBundle;

        public static Font Load()
        {
            _assetBundle ??= LoadAssetBundle();

            var font = _assetBundle.LoadAllAssets<Font>()
                .FirstOrDefault(asset => asset.name.Equals(RegularFontName, StringComparison.Ordinal));
            if (font != null)
                return font;

            var assetNames = string.Join(", ", _assetBundle.GetAllAssetNames());
            throw new InvalidOperationException(
                $"The IBM Plex font bundle does not contain a Font asset named '{RegularFontName}'. Available assets: {assetNames}");
        }

        static AssetBundle LoadAssetBundle()
        {
            using var stream = typeof(BundledFont).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded font bundle resource '{ResourceName}' was not found.");
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);

            return AssetBundle.LoadFromMemory(bytes.ToArray())
                ?? throw new InvalidOperationException("Unity could not load the embedded IBM Plex font AssetBundle.");
        }
    }
}
