using System.Collections.Generic;
using System.IO;
using MTM101BaldAPI.AssetTools;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using BepInEx.Configuration;

namespace LuisRandomness.BBPCustomPosters
{
    [BepInPlugin("io.github.luisrandomness.bbp_custom_posters", "BB+ Custom Posters", ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    public class CustomPostersPlugin : BaseUnityPlugin
    {
        public const string ModVersion = "2024.1.0.1";

        private static List<CustomWeightedPoster> posters;

        // CONFIGURATION
        internal static ConfigEntry<int> config_defaultWeight;
        internal static ConfigEntry<bool> config_adjustPosterChances;
        internal static ConfigEntry<float> config_posterChanceMultiplier;

        //internal static ConfigEntry<string[]> config_foreignPosterBlacklist;
        //internal static ConfigEntry<bool> config_invertForeignPosterBlacklist;

        internal static ConfigEntry<bool> config_logAllPosters;


        void Awake()
        {
            InitConfigValues();

            CustomPosterSettings defaultSettings = new CustomPosterSettings();
            PosterTextData[] defaultTextData = new PosterTextData[0];
            posters = new List<CustomWeightedPoster>();

            string path = Path.Combine(AssetLoader.GetModPath(this),"Posters");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string[] files = Directory.GetFiles(path);

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

                posters.Add(new CustomWeightedPoster(newPoster, settings));
            }

            GeneratorManagement.Register(this, GenerationModType.Addend, (string name, int floorid, LevelObject obj) =>
            {
                // If there aren't any posters in the map, don't add them
                if (obj.posters.Length > 0)
                {
                    int oldPosterCount = obj.posters.Length;
                    List<WeightedPosterObject> currentPosters = new List<WeightedPosterObject>(obj.posters);
                    foreach (WeightedPosterObject poster in currentPosters)
                    {
                        if (!(poster is CustomWeightedPoster) && poster.IsBlacklisted())
                            currentPosters.Remove(poster);
                    }
                    foreach (CustomWeightedPoster poster2 in posters)
                    {
                        if (poster2.IncludeInLevel(name))
                            currentPosters.Add(poster2);
                    }
                    obj.posters = currentPosters.ToArray();

                    if (config_adjustPosterChances.Value)
                    {
                        float newPosterChance = obj.posterChance * obj.posters.Length / oldPosterCount;
                        obj.posterChance = Mathf.Lerp(obj.posterChance, newPosterChance, config_posterChanceMultiplier.Value);
                    }   

                    obj.MarkAsNeverUnload();
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

            //config_foreignPosterBlacklist = Config.Bind("Foreign Posters", "blacklist", new string[0], "List of posters not added by the mod that should not be generated.\nThis works best for posters added by the base game, as other mods using generation management may add their own overrides first!");
            //config_invertForeignPosterBlacklist = Config.Bind("Foreign Posters", "invertBlacklist", false, "If true, only posters not added by the mod in the blacklist are kept.");

            config_logAllPosters = Config.Bind(
                "Debug",
                "logAllPosters",
                false,
                "Logs all available posters in every randomly generated floor.");
        }
    }
}