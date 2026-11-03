using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectBootstrap
{
    // The app icon, from the PNGs in Assets/_Project/AppIcon: AppIcon-<size>.png, one per pixel size
    // (the source drawings are in docs/icon-propositions). Each icon slot of every platform gets the
    // file of its own size, or the next larger one, which Unity scales down. Sizes of 87 px and less
    // are a simpler drawing, without the stars, so the viewer still reads on a Settings row.
    public static class AppIcons
    {
        const string Folder = "Assets/_Project/AppIcon";

        public static void Ensure()
        {
            var textures = new System.Collections.Generic.SortedDictionary<int, Texture2D>();
            foreach (var path in Directory.GetFiles(Folder, "AppIcon-*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (!int.TryParse(name.Substring("AppIcon-".Length), out var size))
                    continue;
                ConfigureImporter(path);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                    textures[size] = texture;
            }
            if (textures.Count == 0)
            {
                Debug.LogWarning($"[AppIcons] No AppIcon-<size>.png in {Folder}; keeping Unity's icon");
                return;
            }

            Texture2D largest = null;
            foreach (var texture in textures.Values)
                largest = texture;
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { largest }, IconKind.Any);

            var filled = 0;
            foreach (var target in new[] { NamedBuildTarget.iOS, NamedBuildTarget.Android })
            {
                foreach (var kind in PlayerSettings.GetSupportedIconKinds(target))
                {
                    var icons = PlayerSettings.GetPlatformIcons(target, kind);
                    foreach (var icon in icons)
                    {
                        // Adaptive Android icons take a background and a foreground layer: leave
                        // them empty so Android falls back to the legacy and round icons.
                        if (icon.maxLayerCount > 1)
                            continue;
                        icon.SetTexture(Pick(textures, icon.width));
                        filled++;
                    }
                    PlayerSettings.SetPlatformIcons(target, kind, icons);
                }
            }
            Debug.Log($"[AppIcons] {filled} icon slots from {textures.Count} sizes");
        }

        // The texture of exactly that size, or the smallest larger one, or the largest of all.
        static Texture2D Pick(System.Collections.Generic.SortedDictionary<int, Texture2D> textures, int width)
        {
            Texture2D largest = null;
            foreach (var pair in textures)
            {
                if (pair.Key >= width)
                    return pair.Value;
                largest = pair.Value;
            }
            return largest;
        }

        // Icons go into the app as drawn: no compression, no power-of-two rescaling, no mipmaps.
        static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;
            if (importer.textureCompression == TextureImporterCompression.Uncompressed &&
                importer.npotScale == TextureImporterNPOTScale.None && !importer.mipmapEnabled)
                return;
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }
}
