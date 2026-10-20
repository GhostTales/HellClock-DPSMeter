using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace DPSMeter
{
    internal static class BundledFont
    {
        const string ResourceName = "DPSMeter.Fonts.ibmplexfonts";
        const string FontPrefix = "IBMPlexSansCondensed-";
        static AssetBundle _assetBundle;
        static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

        public static Font Regular => Get("Regular");
        public static Font Medium => Get("Medium");
        public static Font Light => Get("Light");
        public static Font Bold => Get("Bold");

        /// <summary>Returns the bundled font face (e.g. "Medium"), or null if it is unavailable.</summary>
        public static Font Get(string style) => Fonts.TryGetValue(FontPrefix + style, out var font) ? font : null;

        // The game's stripped UnityEngine.AssetBundleModule only keeps the async API, so the
        // blocking getters on the async requests are used to load synchronously.
        public static void Load()
        {
            _assetBundle ??= AssetBundle.LoadFromFileAsync(ExtractBundle()).assetBundle
                ?? throw new InvalidOperationException("Unity could not load the IBM Plex font AssetBundle.");

            foreach (var asset in _assetBundle.LoadAllAssetsAsync(typeof(Font)).allAssets)
            {
                if (asset is Font font) Fonts[font.name] = font;
            }

            if (Regular == null)
                throw new InvalidOperationException($"The font bundle has no '{FontPrefix}Regular' font. Fonts found: {string.Join(", ", Fonts.Keys)}");
        }

        static string ExtractBundle()
        {
            using var source = typeof(BundledFont).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");

            var directory = Path.Combine(Paths.CachePath, "DPSMeter");
            var path = Path.Combine(directory, "ibmplexfonts");
            if (File.Exists(path) && new FileInfo(path).Length == source.Length)
                return path;

            Directory.CreateDirectory(directory);
            using var destination = File.Create(path);
            source.CopyTo(destination);
            return path;
        }
    }
}
