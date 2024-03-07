using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public static class WeightedPosterExtensions
    {
        public static bool IsBlacklisted(this WeightedPosterObject poster)
        {
            if (poster is CustomWeightedPoster && ((CustomWeightedPoster)poster).userGenerated) return false;

            return CustomPostersPlugin.blacklistedPostersRaw.Contains(poster.selection.name)
                == CustomPostersPlugin.config_invertForeignPosterBlacklist.Value;
        }
    }

    [Serializable]
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
    }
}
