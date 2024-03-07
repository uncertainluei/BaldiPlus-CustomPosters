using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace LuisRandomness.BBPCustomPosters
{
    [BepInPlugin("io.github.luisrandomness.bbp_custom_posters", "BB+ Custom Posters", ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    public class CustomPostersPlugin : BaseUnityPlugin
    {
        public const string ModVersion = "2024.2.0.0";

        internal static ManualLogSource Log;

        // POSTER VARIABLES
        private static List<string> additionalPosterPaths = new List<string>();
        private CustomPosterSettings defaultSettings;
        private PosterTextData[] defaultTextData = new PosterTextData[0];

        private static List<CustomWeightedPoster> posters = new List<CustomWeightedPoster>();
        private static Dictionary<string, int> posterDiffs = new Dictionary<string, int>();

        private static bool loaded = false;

        // CONFIGURATION
        internal static ConfigEntry<int> config_defaultWeight;
        internal static ConfigEntry<bool> config_adjustPosterChances;
        internal static ConfigEntry<float> config_posterChanceMultiplier;

        internal static ConfigEntry<string> config_foreignPosterBlacklist;

        internal static string[] blacklistedPostersRaw;

        internal static ConfigEntry<bool> config_invertForeignPosterBlacklist;

        internal static ConfigEntry<bool> config_logAllPosters;


        void Awake()
        {
            Log = Logger;
            InitConfigValues();

            // CustomPosterSettings grabs a config value, so that's built after the config is initialised
            defaultSettings = new CustomPosterSettings();

            LoadingEvents.RegisterOnAssetsLoaded(CreatePosters, false);

            GeneratorManagement.Register(this, GenerationModType.Addend, (string name, int floorid, LevelObject obj) =>
            {
                // If there aren't any posters in the map, don't add them
                if (obj.posters.Length > 0)
                {
                    posterDiffs.Add(name, obj.posters.Length);
                    List<WeightedPosterObject> currentPosters = new List<WeightedPosterObject>(obj.posters);
                    foreach (CustomWeightedPoster poster2 in posters)
                    {
                        if (poster2.IncludeInLevel(name))
                            currentPosters.Add(poster2);
                    }
                    obj.posters = currentPosters.ToArray();
                    obj.MarkAsNeverUnload();
                }
            });

            // Remove posters
            GeneratorManagement.Register(this, GenerationModType.Finalizer, (string name, int floorid, LevelObject obj) =>
            {
                if (config_adjustPosterChances.Value)
                {
                    List<WeightedPosterObject> currentPosters = new List<WeightedPosterObject>(obj.posters);
                    for (int i = 0; i < currentPosters.Count; i++)
                    {
                        if (currentPosters[i].IsBlacklisted())
                        {
                            currentPosters.RemoveAt(i);
                            i--;
                        }
                    }

                    obj.posters = currentPosters.ToArray();
                    float newPosterChance = obj.posterChance * currentPosters.Count / posterDiffs[name];
                    obj.posterChance = Mathf.Lerp(obj.posterChance, newPosterChance, config_posterChanceMultiplier.Value);
                }

                if (!config_logAllPosters.Value) return;

                Logger.LogInfo("[Level] \"" + name + "\", (Floor " + floorid + "):");
                foreach (WeightedPosterObject poster in obj.posters)
                {
                    Logger.LogInfo(" - \"" + poster.selection.name + "\" (Weight: " + poster.weight + ")");
                }
                Logger.LogInfo("");
            });
        }
        void InitConfigValues()
        {
            config_defaultWeight = Config.Bind(
                "General",
                "defaultPosterWeight",
                50,
                "Default poster weight if variable weight is not set.");
            config_adjustPosterChances = Config.Bind(
                "Poster Chances",
                "adjustPosterChances",
                true,
                "Adjusts the chance of posters generating based on the quantity difference.");
            config_posterChanceMultiplier = Config.Bind(
                "Poster Chances",
                "posterChanceMultiplier",
                0.5f,
                "Affects how much the poster chances are affected. Ideally tone it down if you have added 100+ posters!");

            config_foreignPosterBlacklist = Config.Bind("Foreign Posters",
                "blacklist",
                "",
                "(Names separated by commas) List of non-user-generated posters that should not be generated.");
            config_invertForeignPosterBlacklist = Config.Bind("Foreign Posters",
                "invertBlacklist",
                false,
                "If true, the blacklist above becomes a whitelist and only non-user-generated posters listed above can spawn.");

            blacklistedPostersRaw = config_foreignPosterBlacklist.Value.Split(new char[','], StringSplitOptions.RemoveEmptyEntries);

            // Ensure all split values are trimmed to remove leading spaces
            for (int i = 0; i < blacklistedPostersRaw.Length; i++)
                blacklistedPostersRaw[i] = blacklistedPostersRaw[i].Trim();

            config_logAllPosters = Config.Bind(
                "Debug",
                "logAllPosters",
                false,
                "Logs all available posters in every random floor setting.");
        }

        void CreatePosters()
        {
            // Make it where if any mod tries to add posters after post asset load
            loaded = true;

            string path = Path.Combine(AssetLoader.GetModPath(this), "Posters");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            GrabPostersFromDirectory(path, true);

            foreach (string path2 in additionalPosterPaths)
                GrabPostersFromDirectory(path2, false);

            additionalPosterPaths.Clear();
        }

        public static void AddPostersFromDirectory(BaseUnityPlugin plugin, params string[] paths)
        {
            if (!plugin)
                throw new MissingReferenceException("BepInEx Plugin not set!");

            int length = paths.Length + 1;
            string[] paths2 = new string[length];

            paths2[0] = AssetLoader.GetModPath(plugin);
            for (int i = 1; i < length; i++)
                paths2[i] = paths[i - 1];

            AddPostersFromDirectory(Path.Combine(paths2));
        }

        public static void AddPostersFromDirectory(string dir)
        {
            if (loaded)
            {
                Log.LogError("Could not add posters from \"" + dir + "\": Please call the method before any OnAllAssetsLoaded event, preferably in Awake().");
                return;
            }

            if (additionalPosterPaths.Contains(dir))
                return;

            additionalPosterPaths.Add(dir);
        }

        void GrabPostersFromDirectory(string dir, bool userGenerated)
        {
            if (!Directory.Exists(dir))
            {
                Logger.LogWarning("Folder \"" + dir + "\" does not exist. Make sure you spelled the path right!");
                return;
            }

            string[] files = Directory.GetFiles(dir);

            Texture2D tex;
            PosterObject newPoster;
            CustomPosterSettings settings;

            foreach (string file in files)
            {
                if (Path.GetExtension(file) != ".png") // Denies non-png files
                    continue;

                tex = AssetLoader.TextureFromFile(file);

                if (!File.Exists(file + ".json"))
                    settings = defaultSettings; // Load the default settings to reduce memory usage
                else
                {
                    settings = new CustomPosterSettings();
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(file + ".json"), settings);
                }

                if (tex.width % settings.posterLength > 0) // Image cannot be properly split into the length
                {
                    Logger.LogError("Poster \"" + tex.name + "\" could not be properly split into a multi-poster!");
                    DestroyImmediate(tex, true); // Frees up the texture as it is no longer used.
                    continue;
                }

                newPoster = ObjectCreators.CreatePosterObject(tex, defaultTextData);
                newPoster.name = tex.name;

                if (settings.posterLength > 1)
                {
                    int width = tex.width, height = tex.height;
                    int i = 0, splitWidth = width / (int)settings.posterLength;
                    newPoster.multiPosterArray = new PosterObject[settings.posterLength];

                    for (int x = 0; x < width; x += splitWidth)
                    {
                        Texture2D split = new Texture2D(splitWidth, height, TextureFormat.RGBA32, false)
                        {
                            filterMode = FilterMode.Point,
                            name = tex.name + "_" + i
                        };

                        Color[] pixels = tex.GetPixels(x, 0, splitWidth, height);
                        split.SetPixels(pixels);
                        split.Apply();
                        newPoster.multiPosterArray[i] = ObjectCreators.CreatePosterObject(split, defaultTextData);
                        i++;
                    }
                }

                posters.Add(new CustomWeightedPoster(newPoster, settings, userGenerated));
            }
        }
    }
}