using BepInEx;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UncertainLuei.BaldiPlus.CustomPosters.Packs;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.CustomPosters
{
    public class PosterPack
    {
        internal PosterPack(PosterPackBlueprint template) : this(template.name, template.type, template.path, template.autoCreateDir, template.meta)
        {
            mod = template.pluginInfo;
        }

        internal PosterPack(string dir) : this(Path.GetFileNameWithoutExtension(dir), PosterPackType.Pack, dir)
        {
        }

        private PackFormat format;

        internal PosterPack(string name, PosterPackType type, string dir, bool autoCreateDir = false, PosterPackMetadata metadata = null)
        {
            packName = name;
            packType = type;

            string ext = Path.GetExtension(dir);

            if (autoCreateDir)
                Directory.CreateDirectory(dir);

            this.metadata = metadata;

            if (!PackFormatReader.TryGrabFormat(dir, ext, out format))
            {
                CustomPostersPlugin.Log.LogWarning($"Poster pack \"{Path.GetFileName(dir)}\" could not be loaded; either extension {ext} is not supported, or provided data is invalid.");
                Dispose();
                return;
            }

            DeserializePack();
        }

        public void DisposeAllPosters()
        {
            globalPosters.Clear();
            roomPosters.Clear();
            chalkboardPosters.Clear();

            // Destroys all posters and their contents to free up memory
            foreach (CustomPosterObject poster in posters)
                GameObject.Destroy(poster);
            posters.Clear();
        }

        public void Reload()
        {
            DisposeAllPosters();
            format.Reload();
            DeserializePack();
        }

        private void DeserializePack()
        {
            string name = "", ext, last = "", toOverlay = "";
            CustomPosterProperties properties;
            PackFileEntry fileEntry;
            Texture2D tex, overlay = null;

            if (packType == PosterPackType.Pack)
            {
                fileEntry = format.Get("pack.json");
                if (fileEntry == null && metadata == null)
                {
                    CustomPostersPlugin.Log.LogWarning($"{packName}: Pack metadata file (pack.json) could not be found!");
                    Dispose();
                    return;
                }
                if (!TryUpdateMetadata(fileEntry.ReadAllText(), out Exception e))
                {
                    CustomPostersPlugin.Log.LogWarning($"{packName}: Pack metadata file (pack.json) does not seem to be valid! Exception trace: {e}");
                    Dispose();
                    return;
                }
                if (metadata.packVersion > PosterPackMetadata.currentPackVersion)
                {
                    CustomPostersPlugin.Log.LogWarning($"{packName}: Pack (version {metadata.packVersion}) is incompatible with this version of the mod (pack version {PosterPackMetadata.currentPackVersion})!");
                    Dispose();
                    return;
                }
            }

            List<PackFileEntry> entries = format.GetAllEntries().Where(x => !x.Name.IsNullOrWhiteSpace() && (packType != PosterPackType.Pack || x.FullName != "pack.json"))
                .OrderBy(x => x.FullName) // Present as a failsave
                .ToList();

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].FullName == last)
                    continue;

                ext = Path.GetExtension(entries[i].Name).Remove(0, 1).Trim();

                name = Path.ChangeExtension(entries[i].FullName, null);
                last = name;
                if (name != toOverlay && overlay)
                {
                    GameObject.Destroy(overlay);
                    overlay = null;
                }

                if (ext == "json")
                {
                    properties = new CustomPosterProperties();
                    try
                    {
                        JsonConvert.PopulateObject(entries[i].ReadAllText(), properties);
                    }
                    catch (Exception e)
                    {
                        CustomPostersPlugin.Log.LogError($"{packName}: .json properties \"{name}\" could not be read!\nStack trace: {e}");
                        continue;
                    }

                    ext = Path.GetExtension(name);
                    if (ext.IsNullOrWhiteSpace())
                    {
                        DeserializePoster(name, properties, null);
                        continue;
                    }

                    ext = ext.Remove(0, 1).Trim();
                    if (ext != "png" && ext != "jpg" && ext != "jpeg")
                        continue;

                    fileEntry = format.Get(name);
                    if (fileEntry == null)
                    {
                        CustomPostersPlugin.Log.LogError($"{packName}: Poster texture \"{name}\" could not be found!");
                        continue;
                    }
                    if (!fileEntry.ReadAllBytes().TryCreateTexture(Path.ChangeExtension(name, null), out tex)) // Fix for texture packs mod crash
                    {
                        CustomPostersPlugin.Log.LogError($"{packName}: Poster texture \"{name}\" could not load! This could be because of an unsupported file format.");
                        continue;
                    }

                    DeserializePoster(Path.ChangeExtension(name, null), properties, tex, overlay);
                    overlay = null;
                    continue;
                }

                if (ext != "png" && ext != "jpg" && ext != "jpeg")
                    continue;

                if (!entries[i].ReadAllBytes().TryCreateTexture(Path.ChangeExtension(name, null), out tex)) // Fix for texture packs mod crash
                {
                    CustomPostersPlugin.Log.LogError($"{packName}: Poster texture \"{name}\" could not load! This could be because of an unsupported file format.");
                    continue;
                }
                if (name.ToLower().EndsWith("_overlay") && format.Get(toOverlay = $"{name.Remove(name.Length - 8)}.{ext}") != null)
                {
                    overlay = tex;
                    continue;
                }

                DeserializePoster(Path.ChangeExtension(name, null), CustomPosterProperties.defaultProperties, tex, null);
                overlay = null;
            }

            if (overlay)
                GameObject.Destroy(overlay);
        }

        private void DeserializePoster(string name, CustomPosterProperties properties, Texture2D tex = null, Texture2D overlay = null)
        {
            CustomPosterObject poster;
            
            try
            {
                poster = CustomPosterObject.CreateInstance(name, this, tex, overlay, properties);
            }
            catch (Exception e)
            {
                if (tex)
                    UnityEngine.Object.Destroy(tex);

                CustomPostersPlugin.Log.LogError($"{packName}: Poster \"{name}\" could not load! See exception below:");
                CustomPostersPlugin.Log.LogError(e);
                return;
            }

            posters.Add(poster);

            WeightedCustomPoster weighted = new WeightedCustomPoster(poster);

            switch (poster.spawnMode)
            {
                case PosterSpawnMode.Global:
                    globalPosters.Add(weighted);
                    break;
                case PosterSpawnMode.Room:
                    AddPosterIntoMode(poster, weighted, roomPosters, false);
                    break;
                case PosterSpawnMode.Chalkboard:
                    AddPosterIntoMode(poster, weighted, chalkboardPosters, true);
                    break;
            }
        }

        private void AddPosterIntoMode(CustomPosterObject poster, WeightedCustomPoster weighted, Dictionary<RoomCategory, List<WeightedCustomPoster>> dictionary, bool includeNull)
        {
            List<WeightedCustomPoster> _posters;

            // TODO:: Improve this to not include duplicates
            if (includeNull && poster?.targetRooms.Length == 0)
            {
                if (!dictionary.TryGetValue(RoomCategory.Null, out _posters))
                {
                    _posters = new List<WeightedCustomPoster>();
                    dictionary[RoomCategory.Null] = _posters;
                }
                _posters.Add(weighted);
            }

            foreach (RoomCategory cat in poster.targetRooms)
            {
                if (!dictionary.TryGetValue(cat, out _posters))
                {
                    _posters = new List<WeightedCustomPoster>();
                    dictionary[cat] = _posters;
                }
                _posters.Add(weighted);
            }
        }

        private bool TryUpdateMetadata(string json, out Exception exception)
        {
            PosterPackMetadata newMeta = new PosterPackMetadata();

            try
            {
                JsonConvert.PopulateObject(json, newMeta);
            }
            catch (Exception e)
            {
                exception = e;
                return false;
            }

            metadata = newMeta;
            exception = null;
            return true;
        }

        public bool disposed { get; private set; } = false;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                DisposeAllPosters();
            }
        }

        private readonly PluginInfo mod;

        public readonly string packName;
        public readonly PosterPackType packType;
        public PosterPackMetadata metadata;

        private List<CustomPosterObject> posters = new List<CustomPosterObject>();

        public List<WeightedCustomPoster> globalPosters = new List<WeightedCustomPoster>();
        public Dictionary<RoomCategory, List<WeightedCustomPoster>> roomPosters = new Dictionary<RoomCategory, List<WeightedCustomPoster>>();
        public Dictionary<RoomCategory, List<WeightedCustomPoster>> chalkboardPosters = new Dictionary<RoomCategory, List<WeightedCustomPoster>>();

        public int DefaultWeight => metadata.defaultWeight > 0 ? metadata.defaultWeight : CustomPostersConfig.defaultWeight.Value; // TODO: Simplify

        public bool Enabled => true;
    }

    public enum PosterPackType : byte
    {
        Personal, // Personal posters folder
        Pack, // Optional/downloadable poster packs
        Mod // Default mod posters
    }

    public struct PosterPackBlueprint
    {
        internal PosterPackBlueprint(PluginInfo pluginInfo, PosterPackType type, string name, string path, bool autoCreateDir = false, PosterPackMetadata meta = null)
        {
            this.pluginInfo = pluginInfo;
            this.type = type;
            this.name = name;
            this.path = path;
            this.autoCreateDir = autoCreateDir;
            this.meta = meta;
        }

        internal PosterPackBlueprint(PluginInfo pluginInfo, string path, int defaultWeight)
        {
            this.pluginInfo = pluginInfo;
            this.type = PosterPackType.Mod;
            this.name = $"{pluginInfo.Metadata.Name} (Built-in)";
            this.path = path;
            this.autoCreateDir = false;
            this.meta = new PosterPackMetadata()
            {
                description = "Built-in mod poster pack",
                defaultWeight = defaultWeight
            };
        }

        public PosterPackType type;
        public PluginInfo pluginInfo;

        public string name;
        public string path;

        public bool autoCreateDir;

        public PosterPackMetadata meta;
    }

    public class PosterPackMetadata
    {
        [JsonIgnore] public const byte currentPackVersion = 2;
        [JsonRequired] public byte packVersion = currentPackVersion; // There will NEVER be more than 255 pack versions

        public string credits = "None";
        public string description = "No description set.";

        /* Default weight that is used if no weight is set a poster originating from such pack.
         * If set to zero, then the DefaultWeight config value is used instead.
         */
        public int defaultWeight = 0;
    }
}
