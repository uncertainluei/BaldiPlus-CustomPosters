using Rewired;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LuisRandomness.BBPCustomPosters
{
    public static class ZipExtensions
    {
        public static bool TryCreateTexture(this byte[] bytes, string name, out Texture2D outputTexture)
        {
            outputTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                name = Path.GetFileNameWithoutExtension(name)
            };

            try
            {
                outputTexture.LoadImage(bytes);
            }
            catch
            {
                UnityEngine.Object.Destroy(outputTexture);
                return false;
            }
            return true;
        }

        public static byte[] ReadAllBytes(this ZipArchiveEntry entry)
        {
            using (Stream openedStream = entry.Open())
            using (MemoryStream ms = new MemoryStream())
            {
                openedStream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static string ReadAllText(this ZipArchiveEntry entry)
        {
            using (Stream openedStream = entry.Open())
            using (StreamReader sr = new StreamReader(openedStream))
                return sr.ReadToEnd();
        }
    }

    public static class PosterExtensions
    {
        public static bool IsBlacklisted(this WeightedPosterObject poster)
        {
            // Exclude non-default poster packs, including the personal posters folder
            if (poster is WeightedCustomPoster && ((WeightedCustomPoster)poster).customPoster.pack.packType != PosterPackType.Mod) return false;

            return CustomPostersPlugin.blacklistedPostersRaw.Contains(poster.selection.name.Trim())
                != CustomPostersPlugin.config_invertForeignPosterBlacklist.Value;
        }

        public static string GetSource(this WeightedPosterObject poster)
        {
            if (poster is WeightedCustomPoster)
                return ((WeightedCustomPoster)poster).customPoster.pack.packName;

            return "Vanilla/Unknown";
        }

        private static CustomPosterTextData[] blankData = new CustomPosterTextData[0];

        public static CustomPosterTextData[] Build(this PosterTextSettings[] customData)
        {
            int c = customData.Length;
            if (c == 0) return blankData;

            CustomPosterTextData[] newData = new CustomPosterTextData[c];
            for (int i = 0; i < c; i++)
                newData[i] = new CustomPosterTextData(customData[i]);

            return newData;
        }
    }
}
