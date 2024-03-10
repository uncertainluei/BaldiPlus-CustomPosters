using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace LuisRandomness.BBPCustomPosters
{
    public class CustomWeightedPoster : WeightedPosterObject
    {
        public CustomWeightedPoster(PosterObject poster, CustomPosterSettings settings, bool userGenerated = false)
        {
            this.userGenerated = userGenerated;

            selection = poster;
            weight = settings.posterWeight;

            levelWhitelist = settings.levelWhitelist;
            reverseWhitelist = settings.reverseWhitelist;
        }

        private readonly string[] levelWhitelist;
        private readonly bool reverseWhitelist;

        public readonly bool userGenerated;

        public bool IncludeInLevel(string levelName)
        {
            // Exclude IF weight = 0 as it won't appear anyway
            // Include IF either the whitelist is empty OR the level obeys the white/blacklist
            return weight > 0 && (levelWhitelist.Length == 0 || levelWhitelist.Contains(levelName) != reverseWhitelist);
        }
    }

    public class CustomPosterTextData : PosterTextData
    {
        public int segmentId = 0;

        public CustomPosterTextData(PosterTextSettings builder)
        {
            textKey = builder.textKey;
            position = PosterExtensions.Convert(builder.position);
            size = PosterExtensions.Convert(builder.size);
            fontSize = builder.fontSize;
            segmentId = builder.segmentId;

            if (!ColorUtility.TryParseHtmlString(builder.color, out color))
            {
                CustomPostersPlugin.Log.LogWarning("Text color \"" + builder.color + "\" could not be properly parsed! Using black instead...");
                color = Color.black;
            }

            if (!CustomPostersPlugin.fontAssets.TryGetValue(builder.font, out font))
            {
                CustomPostersPlugin.Log.LogWarning("Font \"" + font + "\" could not be found!");
                if (!CustomPostersPlugin.fontAssets.TryGetValue("COMIC_12_Pro", out font))
                {
                    CustomPostersPlugin.Log.LogWarning("Fallback font could not be found, using default TMP font...");
                    font = TMP_Settings.defaultFontAsset;
                }
                else
                    CustomPostersPlugin.Log.LogWarning("Using \"COMIC_12_Pro\" font...");
            }

            style = FontStyles.Normal;
            if (builder.bold)
                style |= FontStyles.Bold;
            if (builder.italic)
                style |= FontStyles.Italic;
            if (builder.underline)
                style |= FontStyles.Underline;

            Enum.TryParse(builder.alignment, false, out alignment);
        }
    }

    public static class PosterExtensions
    {
        public static bool IsBlacklisted(this WeightedPosterObject poster)
        {
            // Exclude user generated posters
            if (poster is CustomWeightedPoster && ((CustomWeightedPoster)poster).userGenerated) return false;

            return CustomPostersPlugin.blacklistedPostersRaw.Contains(poster.selection.name.Trim())
                != CustomPostersPlugin.config_invertForeignPosterBlacklist.Value;
        }

        public static CustomPosterTextData[] Build(this PosterTextSettings[] customData)
        {
            int c = customData.Length;
            CustomPosterTextData[] newData = new CustomPosterTextData[c];
            for (int i = 0; i < c; i++)
                newData[i] = new CustomPosterTextData(customData[i]);

            return newData;
        }

        public static PosterTextData[] GetTextsForSegment(this CustomPosterTextData[] customData, int segment = 0)
        {
            // If array is empty, just return itself
            if (customData.Length == 0) return customData;

            List<PosterTextData> results = new List<PosterTextData>();
            foreach (CustomPosterTextData customText in customData)
                if (customText.segmentId == segment)
                    results.Add(customText);

            return results.ToArray();
        }

        public static IntVector2 Convert(SerializableIntVector2 vec)
        {
            return new IntVector2(vec.x, vec.y);
        }
    }

    public class SerializableIntVector2
    {
        public int x, y;
    }

    public class PosterTextSettings
    {
        public string textKey = "pst_key";
        public SerializableIntVector2 position;
        public SerializableIntVector2 size;

        public string font = "COMIC_12_Pro";
        public int fontSize = 12;

        public bool bold = false;
        public bool italic = false;
        public bool underline = false;

        public string color = "black";
        public string alignment = "center";

        public int segmentId = 0;
    }

    public class CustomPosterSettings
    {
        // How common/rare it will be in comparison to other posters
        public int posterWeight = CustomPostersPlugin.config_defaultWeight.Value;

        // If higher than 1, then make a multi poster by splitting the image
        // into multiple textures.
        [Range(1, 32)]
        public uint posterLength = 1;

        // A whitelist indicating which levels will contain the poster.
        // Levels are declared via string values.
        // If empty, it will spawn in any level.
        public string[] levelWhitelist = new string[0];

        // If true, the level whitelist will become a blacklist, and the
        // poster will be excluded from the floors declared in the list.
        public bool reverseWhitelist = false;

        // PosterTextData entries go here
        public PosterTextSettings[] textData;
    }
}
