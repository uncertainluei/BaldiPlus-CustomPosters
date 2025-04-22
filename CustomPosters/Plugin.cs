using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;

using HarmonyLib;

using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;

using Newtonsoft.Json;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TMPro;

using UncertainLuei.BaldiPlus.CustomPosters.Packs;
using UncertainLuei.BaldiPlus.CustomPosters.Compatibility;

using UnityEngine;


namespace UncertainLuei.BaldiPlus.CustomPosters
{
    [BepInPlugin(ModGuid, "Custom Posters", ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInDependency("blayms.tbb.baldiplus.betterchkfont", BepInDependency.DependencyFlags.SoftDependency)]
    public class CustomPostersPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "io.github.uncertainluei.baldiplus.customposters";
        public const string ModVersion = "2025.2";

        internal static ManualLogSource Log;

        // POSTER VARIABLES
        private static readonly List<PosterPackBlueprint> posterPackBlueprints = new List<PosterPackBlueprint>();

        internal static Dictionary<string, PosterPack> posterPacks = new Dictionary<string, PosterPack>();
        internal static List<PosterPack> activePosterPacks = new List<PosterPack>();

        private string currentFloorName;
        private int currentFloorId;
        private bool currentFloorFinalized;

        private static bool loaded = false;

        internal static Dictionary<string, TMP_FontAsset> fontAssets = new Dictionary<string, TMP_FontAsset>();

        void Awake()
        {
            Log = Logger;

            CustomPostersConfig.BindConfig(Config);
            PackFormatReader.InitReadChecks(Info);

            // Add personal pack
            posterPackBlueprints.Add(new PosterPackBlueprint(
                Info, PosterPackType.Personal, "Personal",
                Path.Combine(AssetLoader.GetModPath(this), "Posters"),
                true, new PosterPackMetadata()
                {
                    credits = "Player",
                    description = "Personal poster pack, ideal for quick prototyping",
                    defaultWeight = 0
                }));

            // Better Chalk Font compat
            bool chalkCompat = Chainloader.PluginInfos.ContainsKey(ChalkFontCompat.ModGuid);
            if (chalkCompat)
                ChalkFontCompat.Initialize();

            // Before generator management events
            LoadingEvents.RegisterOnAssetsLoaded(Info, GrabTmpFonts(chalkCompat), false);
            LoadingEvents.RegisterOnAssetsLoaded(Info, PosterPresetStorage.AddPresets(), false);
            LoadingEvents.RegisterOnAssetsLoaded(Info, CustomPostersEnumExts.RegisterEnumExts(), false);
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadPosterPacks(true), false);

            GeneratorManagement.Register(this, GenerationModType.Addend, OnGeneratorAddend);
            GeneratorManagement.Register(this, GenerationModType.Finalizer, OnGeneratorFinalizer);

            // This is loaded after the generator actions
            LoadingEvents.RegisterOnAssetsLoaded(Info, CustomPostersEnumExts.RegisterExtendedRooms(), true);

            new Harmony(ModGuid).PatchAllConditionals();
        }
        
        public void StartReloadPacks()
        {
            StartCoroutine(ReloadAllPacks());
        }

        public IEnumerator ReloadAllPacks()
        {
            IEnumerator posterLoad = LoadPosterPacks(false);
            while (posterLoad.MoveNext())
            {
                yield return null;
            }

            // Force-updates all SceneObjects
            foreach (SceneObject scene in Resources.FindObjectsOfTypeAll<SceneObject>().Where(x => x.levelObject != null))
            {
                OnGeneratorAddend(scene.levelTitle, scene.levelNo, scene);
                OnGeneratorFinalizer(scene.levelTitle, scene.levelNo, scene);
            }
            yield break;
        }

        private IEnumerator GrabTmpFonts(bool chalkCompat)
        {
            TMP_FontAsset[] assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            yield return assets.Length;

            foreach (TMP_FontAsset font in assets)
            {
                yield return $"Grabbing font \"{font.name}\"";

                if (fontAssets.ContainsKey(font.name))
                {
                    Log.LogWarning($"Font with duplicate name {font.name} found! Skipping...");
                    continue;
                }
                fontAssets.Add(font.name, font);
            }

            if (!chalkCompat)
            {
                yield return "Fallbacking chalkboard fonts";
                fontAssets.Add("BaldChalkFont_12_Smooth", fontAssets["COMIC_12_Smooth_Pro"]);
                fontAssets.Add("BaldChalkFont_18_Smooth", fontAssets["COMIC_18_Smooth_Pro"]);
                fontAssets.Add("BaldChalkFont_24_Smooth", fontAssets["COMIC_24_Smooth_Pro"]);
                fontAssets.Add("BaldChalkFont_36_Smooth", fontAssets["COMIC_36_Smooth_Pro"]);
            }
            yield break;
        }

        private IEnumerator LoadPosterPacks(bool init = false)
        {
            // Indicate the beginning of the loading process
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
                GC.Collect();
            }

            foreach (PosterPackBlueprint temp in posterPackBlueprints)
            {
                yield return $"Processing pack blueprint \"{temp.name}\" ({idx}/{total})";

                if (!posterPacks.ContainsKey(temp.path))
                {
                    try
                    {
                        newPack = new PosterPack(temp);
                        if (!newPack.disposed)
                            posterPacks.Add(temp.path, newPack);
                    }
                    catch (Exception e)
                    {
                        MTM101BaldiDevAPI.CauseCrash(Info, new Exception($"Could not load \"{temp.name}\"! Stack trace: {e}"));
                    }
                }

                idx++;
            }

            foreach (string pack in packs)
            {
                yield return $"Processing potential pack \"{Path.GetFileNameWithoutExtension(pack)}\" ({idx}/{total})";

                if (!posterPacks.ContainsKey(pack))
                {
                    try
                    {
                        newPack = new PosterPack(pack);
                        if (!newPack.disposed)
                            posterPacks.Add(pack, newPack);
                    }
                    catch (Exception e)
                    {
                        MTM101BaldiDevAPI.CauseCrash(Info, e);
                    }
                }

                idx++;
            }

            activePosterPacks.Clear();
            activePosterPacks.AddRange(posterPacks.Values);
            yield break;
        }

        private void OnGeneratorAddend(string name, int id, SceneObject scene)
        {
            if (name == currentFloorName && id == currentFloorId) return;

            currentFloorName = name;
            currentFloorId = id;
            currentFloorFinalized = false;

            foreach (LevelObject lvl in scene.GetCustomLevelObjects())
                OnGeneratorAddendLvl(name, lvl);
        }

        private void OnGeneratorAddendLvl(string name, LevelObject lvl)
        {
            // If there aren't any posters in the level, don't add them
            if (lvl.posters.Length > 0)
            {
                List<WeightedPosterObject> currentPosters = new List<WeightedPosterObject>(lvl.posters);
                foreach (PosterPack pack in activePosterPacks)
                {
                    foreach (WeightedCustomPoster poster in pack.globalPosters)
                    {
                        if (poster.IncludeInLevel(name, lvl.type))
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

        private void OnGeneratorFinalizer(string name, int id, SceneObject scene)
        {
            if (currentFloorFinalized) return;
            currentFloorFinalized = true;

            if (CustomPostersConfig.logGeneratorPosters.Value)
                Logger.LogInfo($"SceneObject \"{name}\", ID {id}");

            foreach (LevelObject lvl in scene.GetCustomLevelObjects())
                OnGeneratorFinalizerLvl(lvl);

            if (CustomPostersConfig.logGeneratorPosters.Value)
                Logger.LogInfo("");
        }

        private void OnGeneratorFinalizerLvl(LevelObject lvl)
        {
            // Remove blacklisted posters from the generator
            List<WeightedPosterObject> currentPosters = new List<WeightedPosterObject>(lvl.posters);
            currentPosters.RemoveAll(x => x.selection == null || x.IsBlacklisted());
            lvl.posters = currentPosters.ToArray();

            if (CustomPostersConfig.logGeneratorPosters.Value)
            {
                Logger.LogInfo($" LevelObject \"{lvl.name}\", type {lvl.type.ToStringExtended()}");
                foreach (WeightedPosterObject poster in lvl.posters)
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

        // For debugging purposes
        public static void DeserializeVanillaPosters(string outputDir)
        {
            if (!Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                }
                catch (Exception e)
                {
                    throw new Exception($"Couldn't create directory! See below:\n{e}");
                }
            }

            int idx, arrayLength;
            string posterPath;
            Texture2D tex;
            List<PosterTextSettings> customTextData = new List<PosterTextSettings>();

            List<PosterObject> posters = Resources.FindObjectsOfTypeAll<PosterObject>().Where(x => x.GetInstanceID() > 0).ToList();
            List<PosterObject> postersToIgnore = new List<PosterObject>();
            posters.Do(x =>
            {
                if (x.multiPosterArray != null && x.multiPosterArray.Length > 1)
                {
                    arrayLength = x.multiPosterArray.Length;
                    for (idx = 1; idx < arrayLength; idx++)
                        postersToIgnore.Add(x.multiPosterArray[idx]);
                }
            });
            posters.RemoveAll(x => postersToIgnore.Contains(x));

            posters.Do(x =>
            {
                customTextData.Clear();
                posterPath = Path.Combine(outputDir, x.name);
                if (x.multiPosterArray != null && x.multiPosterArray.Length > 1)
                {
                    idx = 0;
                    arrayLength = x.baseTexture.width;

                    tex = new Texture2D(arrayLength * x.multiPosterArray.Length, x.baseTexture.height);
                    foreach (PosterObject poster in x.multiPosterArray)
                    {
                        tex.SetPixels(idx*arrayLength, 0, poster.baseTexture.width, poster.baseTexture.height, poster.baseTexture.isReadable ? poster.baseTexture.GetPixels() : poster.baseTexture.MakeReadableCopy(false).GetPixels());
                        if (poster.textData != null && poster.textData.Length > 0)
                        {
                            poster.textData.Do(y => customTextData.Add(new PosterTextSettings()
                            {
                                segmentId = idx,

                                alignment = y.alignment.ToString(),

                                bold = y.style.HasFlag(FontStyles.Bold),
                                italic = y.style.HasFlag(FontStyles.Italic),
                                underline = y.style.HasFlag(FontStyles.Underline),

                                color = ColorUtility.ToHtmlStringRGB(y.color),

                                font = y.font.name,
                                fontSize = y.fontSize,

                                position = y.position.ToSerializable(),
                                size = y.size.ToSerializable(),

                                textKey = y.textKey
                            }));
                        }
                        idx++;
                        tex.Apply();
                        File.WriteAllBytes(posterPath+".png", tex.EncodeToPNG());
                    }
                }
                else
                {
                    if (x.baseTexture == null)
                        x.baseTexture = (Texture2D)x.material[0].mainTexture;

                    File.WriteAllBytes(posterPath + ".png", x.baseTexture.isReadable ? x.baseTexture.EncodeToPNG() : x.baseTexture.MakeReadableCopy(false).EncodeToPNG());
                    
                    if (x.textData != null && x.textData.Length > 0)
                    {
                        x.textData.Do(y => customTextData.Add(new PosterTextSettings()
                        {
                            alignment = y.alignment.ToString(),

                            bold = y.style == FontStyles.Bold,
                            italic = y.style == FontStyles.Italic,
                            underline = y.style == FontStyles.Underline,

                            color = ColorUtility.ToHtmlStringRGB(y.color),

                            font = y.font.name,
                            fontSize = y.fontSize,

                            position = y.position.ToSerializable(),
                            size = y.size.ToSerializable(),
                        
                            textKey = y.textKey
                        }));
                    }
                }
                File.WriteAllText(posterPath + ".json", JsonConvert.SerializeObject(new CustomPosterProperties()
                {
                    textData = customTextData.ToArray()
                }, Formatting.Indented));
            });
        }
    }
}
