using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LuisRandomness.BBPCustomPosters.Packs;
using System.Reflection;
using static Rewired.Controller;
using Newtonsoft.Json.Converters;

namespace LuisRandomness.BBPCustomPosters
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
                Debug.LogWarning($"Poster pack \"{Path.GetFileName(dir)}\" could not be loaded; either extension {ext} is not supported, or provided data is invalid.");
                Dispose();
                return;
            }

            AddPosters();
        }

        public void DisposeAllPosters()
        {
            globalPosters.Clear();
            roomPosters.Clear();
            chalkboardPosters.Clear();

            // Destroys all posters and their contents to free up memory
            foreach (CustomPosterObject poster in posters)
            {

                // Also destroys multi-posters
                foreach (PosterObject obj2 in poster.multiPosterArray)
                {
                    UnityEngine.Object.Destroy(obj2.baseTexture);
                    UnityEngine.Object.Destroy(obj2);
                }

                UnityEngine.Object.Destroy(poster.baseTexture);
                UnityEngine.Object.Destroy(poster);
            }
            posters.Clear();
        }

        public void Reload()
        {
            DisposeAllPosters();
            format.Reload();
        }

        private void AddPosters()
        {
            string name = "", ext;
            Texture2D texture;
            CustomPosterProperties properties;
            CustomPosterObject poster;
            PackFileEntry propertiesEntry;

            if (packType == PosterPackType.Pack)
            {
                propertiesEntry = format.Get("pack.json");
                if (propertiesEntry == null && metadata == null)
                {
                    return;
                }
                if (!TryUpdateMetadata(propertiesEntry.ReadAllText(), out Exception e))
                {
                    Debug.LogWarning($"{packName}: Pack metadata file (pack.json) does not seem to be valid! Exception trace: {e.ToString()}");
                    Dispose();
                    return;
                }
            }

            foreach (PackFileEntry entry in format.GetAllEntries())
            {
                if (entry.Name.IsNullOrWhiteSpace())
                    continue;

                ext = Path.GetExtension(entry.Name).Remove(0, 1).Trim();

                if (ext != "png" && ext != "jpg" && ext != "jpeg")
                    continue;

                name = Path.ChangeExtension(entry.FullName, null);

                if (!entry.ReadAllBytes().TryCreateTexture(name.Replace("/", "-"), out texture)) // Fix for texture packs mod crash
                {
                    Debug.LogError($"{packName}: Poster texture \"{name}\" could not load! This could be because of an unsupported file format.");
                    continue;
                }

                propertiesEntry = format.Get(entry.FullName + ".json");
                if (propertiesEntry != null)
                {
                    properties = new CustomPosterProperties();
                    JsonConvert.PopulateObject(propertiesEntry.ReadAllText(), properties);
                }
                else
                    properties = CustomPosterProperties.defaultProperties;

                try
                {
                    poster = CustomPosterObject.CreateInstance(name, this, texture, properties);
                }
                catch (Exception e)
                {
                    UnityEngine.Object.Destroy(texture);

                    Debug.LogError($"{packName}: Poster \"{name}\" could not load! See exception below:");
                    Debug.LogException(e);
                    continue;
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
        public Dictionary<RoomCategory,List<WeightedCustomPoster>> chalkboardPosters = new Dictionary<RoomCategory, List<WeightedCustomPoster>>();

        public int DefaultWeight => metadata.defaultWeight > 0 ? metadata.defaultWeight : CustomPostersPlugin.config_defaultWeight.Value; // TODO: Simplify

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
        public PosterPackBlueprint(PluginInfo pluginInfo, PosterPackType type, string name, string path, bool autoCreateDir = false, PosterPackMetadata meta = null)
        {
            this.pluginInfo = pluginInfo;
            this.type = type;
            this.name = name;
            this.path = path;
            this.autoCreateDir = autoCreateDir;
            this.meta = meta;
        }

        public PosterPackBlueprint(PluginInfo pluginInfo, string path, int defaultWeight)
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
        [JsonIgnore] public static readonly PosterPackMetadata personalMeta = new PosterPackMetadata()
        {
            credits = "Player",
            description = "Personal poster pack, ideal for quick prototyping",
            defaultWeight = 0
        };

        [JsonRequired] public byte packVersion = 0; // There will NEVER be more than 255 pack versions

        public string credits = "None";
        public string description = "No description set.";

        /* Default weight that is used if no weight is set a poster originating from such pack.
         * If set to zero, then the DefaultWeight config value is used instead.
         */
        public int defaultWeight = 0;
    }
}
