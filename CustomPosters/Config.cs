using BepInEx.Configuration;

using System;

namespace UncertainLuei.BaldiPlus.CustomPosters
{
    static class CustomPostersConfig
    {
        internal static ConfigEntry<int> defaultWeight;
        internal static ConfigEntry<bool> extendRoomEnums;

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
            extendRoomEnums = config.Bind(
                "General",
                "ExtendRoomEnums",
                true,
                "Adds additional room category enum values for special rooms that may lack one. Turn off if you are having problems with special rooms alongside other mods!");

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

            // In the event I get off my lazy arse and make a mods manager mod...
            ReloadPosterBlacklist();

            logGeneratorPosters = config.Bind(
                "Debug",
                "LogAllPosters",
                false,
                "Logs all available posters in every random floor setting.");
        }

        internal static void ReloadPosterBlacklist()
        {
            blacklistedPosters = blacklist.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Ensure all split values are trimmed to remove leading spaces
            for (int i = 0; i < blacklistedPosters.Length; i++)
                blacklistedPosters[i] = blacklistedPosters[i].Trim();
        }
    }
}
