using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using TMPro;

using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using HarmonyLib;

// Curse UnityEngine.JsonUtility, all my homies use Json.Net
using Newtonsoft.Json;
using MTM101BaldAPI.Reflection;
using System.Linq;
using LuisRandomness.BBPCustomPosters.Packs;
using System.IO.Compression;
using Newtonsoft.Json.Converters;

namespace LuisRandomness.BBPCustomPosters
{
    [BepInPlugin(ModGuid, "BB+ Custom Posters", ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    public class CustomPostersPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "io.github.luisrandomness.bbp_custom_posters";
        public const string ModVersion = "2024.3.1.0";

        internal static ManualLogSource Log;

        // POSTER VARIABLES
        private static List<PosterPackBlueprint> posterPackBlueprints = new List<PosterPackBlueprint>();

        internal static Dictionary<string, PosterPack> posterPacks = new Dictionary<string, PosterPack>();
        internal static List<PosterPack> activePosterPacks = new List<PosterPack>();

        private static Dictionary<string, int> posterDiffs = new Dictionary<string, int>();

        private string _floorName;
        private int _floorNum;
        private bool _lastFloorFinalized;

        private List<CustomPosterObject> _posters;

        private static bool loaded = false;

        internal static Dictionary<string, TMP_FontAsset> fontAssets = new Dictionary<string, TMP_FontAsset>();

        // CONFIGURATION
        internal static ConfigEntry<int> config_defaultWeight;

        internal static ConfigEntry<string> config_foreignPosterBlacklist;
        internal static string[] blacklistedPostersRaw;
        internal static ConfigEntry<bool> config_invertForeignPosterBlacklist;

        internal static ConfigEntry<bool> config_globalPostersOnly;

        internal static ConfigEntry<bool> config_logAllPosters;

        void Awake()
        {
            Log = Logger;

            InitConfigValues();
            InitDefaultReadChecks();

            // Add personal pack
            posterPackBlueprints.Add(new PosterPackBlueprint(
                Info, PosterPackType.Personal, "Personal",
                Path.Combine(AssetLoader.GetModPath(this), "Posters"),
                true, PosterPackMetadata.personalMeta));

            // Before generator management events
            LoadingEvents.RegisterOnAssetsLoaded(Info, GrabTmpFonts(), false);
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadPosterPacks(true), false);

            GeneratorManagement.Register(this, GenerationModType.Addend, OnGeneratorAddend);
            GeneratorManagement.Register(this, GenerationModType.Finalizer, OnGeneratorFinalizer);

            new Harmony(ModGuid).PatchAllConditionals();
        }
        void InitConfigValues()
        {
            config_defaultWeight = Config.Bind(
                "General",
                "DefaultWeight",
                50,
                "Default poster weight if variable weight is not set.");

            config_globalPostersOnly = Config.Bind(
                "General",
                "GlobalPostersOnly",
                false,
                "Do not attempt to generate non-global/room-specific posters. This is in case such a failsave is necessary.");

            config_foreignPosterBlacklist = Config.Bind("Foreign Posters",
                "Blacklist",
                "",
                "(Names separated by commas) List of non-user-generated posters that should not be generated.");
            config_invertForeignPosterBlacklist = Config.Bind("Foreign Posters",
                "InvertBlacklist",
                false,
                "If true, the blacklist above becomes a whitelist and only non-user-generated posters listed above can spawn.");

            blacklistedPostersRaw = config_foreignPosterBlacklist.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Ensure all split values are trimmed to remove leading spaces
            for (int i = 0; i < blacklistedPostersRaw.Length; i++)
                blacklistedPostersRaw[i] = blacklistedPostersRaw[i].Trim();

            config_logAllPosters = Config.Bind(
                "Debug",
                "LogAllPosters",
                false,
                "Logs all available posters in every random floor setting.");
        }

        void InitDefaultReadChecks()
        {
            // Local file paths
            PackFormatReader.AddReadCheck(Info, (string path, string ext) =>
            {
                if (Directory.Exists(path))
                    PackFormatReader.Result = new LocalPackFormat(path);
            });

            // .ZIP archives
            PackFormatReader.AddReadCheck(Info, (string path, string ext) =>
            {
                if (ext != ".zip") return;

                ZipArchive archive;
                try
                {
                    archive = ZipFile.OpenRead(path);
                }
                catch
                {
                    return;
                }

                PackFormatReader.Result = new ZipPackFormat(path, archive);
            });
        }

        public IEnumerator ReloadAllPacks()
        {
            IEnumerator posterLoad = LoadPosterPacks(false);
            while (posterLoad.MoveNext())
            {
                yield return null;
            }

            CustomLevelObject lvl;

            // Force-updates all SceneObjects
            foreach (SceneObject scene in Resources.FindObjectsOfTypeAll<SceneObject>().Where(x => x.levelObject != null).ToArray())
            {
                if (!(scene.levelObject is CustomLevelObject))
                    continue;
                lvl = (CustomLevelObject)scene.levelObject;

                OnGeneratorAddend(scene.levelTitle, scene.levelNo, lvl);
                OnGeneratorFinalizer(scene.levelTitle, scene.levelNo, lvl);
            }
            yield break;
        }

        private IEnumerator GrabTmpFonts()
        {
            TMP_FontAsset[] assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();

            yield return assets.Length;

            foreach (TMP_FontAsset font in assets)
            {
                yield return $"Grabbing TMP font \"{font.name}\"";

                if (fontAssets.ContainsKey(font.name))
                {
                    Log.LogWarning($"Font with duplicate name {font.name} found! Skipping...");
                    continue;
                }
                fontAssets.Add(font.name, font);
            }

            yield break;
        }

        private IEnumerator LoadPosterPacks(bool init = false)
        {
            // Make it where if any mod tries to add posters after post asset load
            loaded = true;

            PosterPack newPack;

            string packsDir = Path.Combine(AssetLoader.GetModPath(this), "Packs");
            string[] packs = Directory.Exists(packsDir) ? Directory.GetFileSystemEntries(packsDir) : new string[0];

            int total = posterPackBlueprints.Count + packs.Length;
            int idx = 1;

            yield return total + (init ? 0 : 1);

            if (!init)
            {
                yield return "Updating existing packs";
                foreach (string pack in posterPacks.Keys)
                {
                    if (Directory.Exists(pack) || File.Exists(pack))
                    {
                        posterPacks[pack].Reload();
                    }
                    else
                    {
                        posterPacks[pack].Dispose();
                        posterPacks.Remove(pack);
                    }
                }
            }

            foreach (PosterPackBlueprint temp in posterPackBlueprints)
            {
                yield return $"Processing pack blueprint \"{temp.name}\" ({idx}/{total})";

                if (!posterPacks.ContainsKey(temp.path))
                {
                    newPack = new PosterPack(temp);
                    if (!newPack.disposed)
                        posterPacks.Add(temp.path, newPack);
                }

                idx++;
            }

            foreach (string pack in packs)
            {
                yield return $"Processing potential pack \"{Path.GetFileNameWithoutExtension(pack)}\" ({idx}/{total})";

                if (!posterPacks.ContainsKey(pack))
                {
                    newPack = new PosterPack(pack);
                    if (!newPack.disposed)
                        posterPacks.Add(pack, newPack);
                }

                idx++;
            }

            activePosterPacks.Clear();
            activePosterPacks.AddRange(posterPacks.Values);

            foreach (PosterPack pack in activePosterPacks)
            {
                Debug.Log(pack.globalPosters.Count);
            }
            yield break;
        }

        void OnGeneratorAddend(string name, int id, CustomLevelObject lvl)
        {
            if (name == _floorName && id == _floorNum) return;

            _floorName = name;
            _floorNum = id;
            _lastFloorFinalized = false;

            // Cache level title and num for compatibility with mods that invoke generator events on the generator itself
            RoomPlacementPatch.cached = true;
            RoomPlacementPatch.cachedLevelTitle = name;
            RoomPlacementPatch.cachedLevelNum = id;

            // If there aren't any posters in the map, don't add them
            if (lvl.posters.Length > 0)
            {
                List<WeightedPosterObject> currentPosters = new List<WeightedPosterObject>(lvl.posters);
                foreach (PosterPack pack in activePosterPacks)
                {
                    foreach (WeightedCustomPoster poster in pack.globalPosters)
                    {
                        if (poster.IncludeInLevel(name, id))
                        {
                            if (!currentPosters.Contains(poster))
                                currentPosters.Add(poster);
                        }
                        else
                            if (currentPosters.Contains(poster))
                            currentPosters.Remove(poster);
                    }
                }
                lvl.posters = currentPosters.ToArray();
            }
        }

        private void OnGeneratorFinalizer(string name, int id, CustomLevelObject obj)
        {
            if (_lastFloorFinalized) return;
            _lastFloorFinalized = true;

            string fixedName = (name == "INF") ? (name + id) : name; // Infinite Floors support

            // Remove blacklisted posters from the generator
            List<WeightedPosterObject> currentPosters = new List<WeightedPosterObject>(obj.posters);
            currentPosters.RemoveAll((WeightedPosterObject x) => x.IsBlacklisted());
            obj.posters = currentPosters.ToArray();

            //TODO: Rework logallposters

            if (config_logAllPosters.Value)
            {
                Logger.LogInfo($"Floor \"{name}\", ID {id}");
                Logger.LogInfo($"(Reference name \"{fixedName}\"):");
                foreach (WeightedPosterObject poster in obj.posters)
                    Logger.LogInfo($" - \"{poster.selection.name}\" ({poster.GetSource()}, Weight: {poster.weight})");

                Logger.LogInfo("");
            }
        }

        public static void AddOptionalPackFromMod(BaseUnityPlugin plugin, string name, params string[] args)
        {
            if (!plugin)
                throw new MissingReferenceException("BepInEx Plugin not set!");

            int length = args.Length + 1;
            string[] paths2 = new string[length];

            paths2[0] = AssetLoader.GetModPath(plugin);
            for (int i = 1; i < length; i++)
                paths2[i] = args[i - 1];

            AddOptionalPackFromDirectory(plugin, name, Path.Combine(paths2));
        }

        public static void AddOptionalPackFromDirectory(BaseUnityPlugin plugin, string name, string path)
        {
            if (!plugin)
                throw new MissingReferenceException("BepInEx Plugin not set!");
            if (loaded)
                throw new Exception($"Could not add posters from {plugin.Info.Metadata.Name}, path \"{path}\"! Please execute this before the \"Mod Asset Pre-Load\" loading event!");

            posterPackBlueprints.Add(new PosterPackBlueprint(plugin.Info, PosterPackType.Mod, $"{name} ({plugin.Info.Metadata.Name})", path));
        }

        public static void AddBuiltInPackFromMod(BaseUnityPlugin plugin, params string[] args)
        {
            if (!plugin)
                throw new MissingReferenceException("BepInEx Plugin not set!");

            int length = args.Length + 1;
            string[] paths2 = new string[length];

            paths2[0] = AssetLoader.GetModPath(plugin);
            for (int i = 1; i < length; i++)
                paths2[i] = args[i - 1];

            AddBuiltInPackFromDirectory(plugin, Path.Combine(paths2));
        }

        public static void AddBuiltInPackFromDirectory(BaseUnityPlugin plugin, string path, int defaultWeight = 0)
        {
            if (!plugin)
                throw new MissingReferenceException("BepInEx Plugin not set!");
            if (loaded)
                throw new Exception($"Could not add posters from {plugin.Info.Metadata.Name}, path \"{path}\"! Please execute this before the \"Mod Asset Pre-Load\" loading event!");

            posterPackBlueprints.Add(new PosterPackBlueprint(plugin.Info, path, defaultWeight));
        }
    }

    [HarmonyPatch(typeof(TextTextureGenerator))]
    [HarmonyPatch("GenerateTextTexture")]
    internal class TextTexturePatch
    {
        static void Postfix(Texture2D __result, PosterObject poster)
        {
            __result.name = poster.name + "_WithText";
        }
    }

    [HarmonyPatch(typeof(LevelBuilder))]
    [HarmonyPatch("StartGenerate")]
    [HarmonyPriority(600)]
    internal class HandleLevelCacheOnGenerate
    {
        private static void Postfix(LevelBuilder __instance)
        {
            RoomPlacementPatch.cached = false;
            __instance.StartCoroutine(DecacheWhenDone(__instance));
        }

        private static IEnumerator DecacheWhenDone(LevelBuilder lb)
        {
            yield return new WaitWhile(() => lb.levelInProgress);
            RoomPlacementPatch.cached = false;
            yield break;
        }
    }

    [HarmonyPatch(typeof(LevelBuilder))]
    [HarmonyPatch("LoadRoom")]
    internal class RoomPlacementPatch
    {
        public static bool cached;

        public static string cachedLevelTitle;
        public static int cachedLevelNum;

        private static void Postfix(RoomController __result)
        {
            if (CustomPostersPlugin.config_globalPostersOnly.Value)
                return;

            string lvl = cached ? cachedLevelTitle : CoreGameManager.Instance.sceneObject.levelTitle;
            int num = cached ? cachedLevelNum : CoreGameManager.Instance.sceneObject.levelNo;

            foreach (PosterPack pack in CustomPostersPlugin.activePosterPacks)
                if (pack.roomPosters.TryGetValue(__result.category, out List<WeightedCustomPoster> _posters))
                    __result.potentialPosters.AddRange(_posters.Where((WeightedCustomPoster x) => x.IncludeInLevel(lvl, num)));

            __result.potentialPosters.RemoveAll((WeightedPosterObject x) => x.IsBlacklisted() || x.selection == null);
        }
    }
    
    [HarmonyPatch(typeof(ChalkboardBuilderFunction))]
    [HarmonyPatch("Build")]
    internal class ChalkboardBuilderPatch
    {
        private static bool Prefix(ChalkboardBuilderFunction __instance, ref WeightedPosterObject[] ___chalkBoards)
        {
            if (CustomPostersPlugin.config_globalPostersOnly.Value)
                return true;

            string lvl = RoomPlacementPatch.cached ? RoomPlacementPatch.cachedLevelTitle : CoreGameManager.Instance.sceneObject.levelTitle;
            int num = RoomPlacementPatch.cached ? RoomPlacementPatch.cachedLevelNum : CoreGameManager.Instance.sceneObject.levelNo;

            List<WeightedPosterObject> weightedPosters = new List<WeightedPosterObject>(___chalkBoards);

            foreach (PosterPack pack in CustomPostersPlugin.activePosterPacks)
            {
                if (pack.chalkboardPosters.TryGetValue(RoomCategory.Null, out List<WeightedCustomPoster> _posters))
                    weightedPosters.AddRange(_posters.Where((WeightedCustomPoster x) => x.IncludeInLevel(lvl, num)));
                if (pack.chalkboardPosters.TryGetValue(__instance.room.category, out _posters))
                    weightedPosters.AddRange(_posters.Where((WeightedCustomPoster x) => x.IncludeInLevel(lvl, num)));
            }

            weightedPosters.RemoveAll((WeightedPosterObject x) => x.IsBlacklisted() || x.selection == null);
            ___chalkBoards = weightedPosters.ToArray();

            return ___chalkBoards.Length > 0;
        }
    }
}
