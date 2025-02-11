using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using Newtonsoft.Json;

namespace UncertainLuei.BaldiPlus.CustomPosters
{
    static class CustomPostersConfig
    {
        internal static ConfigEntry<int> defaultWeight;

        private static ConfigEntry<string> blacklist;
        internal static string[] blacklistedPosters;
        internal static ConfigEntry<bool> blacklistInvert;

        internal static ConfigEntry<bool> logGeneratorPosters;

        internal static void BindConfig(ConfigFile config)
        {
            defaultWeight = config.Bind(
                "General",
                "DefaultWeight",
                50,
                "Default poster weight if variable weight is not set.");

            blacklist = config.Bind(
                "Foreign Posters",
                "Blacklist",
                "",
                "(Names separated by commas) List of non-user-generated posters that should not be generated.");
            blacklistInvert = config.Bind(
                "Foreign Posters",
                "InvertBlacklist",
                false,
                "If true, the blacklist above becomes a whitelist and only non-user-generated posters listed above can spawn.");

            blacklistedPosters = blacklist.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Ensure all split values are trimmed to remove leading spaces
            for (int i = 0; i < blacklistedPosters.Length; i++)
                blacklistedPosters[i] = blacklistedPosters[i].Trim();

            logGeneratorPosters = config.Bind(
                "Debug",
                "LogAllPosters",
                false,
                "Logs all available posters in every random floor setting.");
        }
    }
}
