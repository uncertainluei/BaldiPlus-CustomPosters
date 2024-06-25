using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Linq;
using System.Net.NetworkInformation;
using BepInEx;
using JetBrains.Annotations;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using Newtonsoft.Json.Converters;

namespace LuisRandomness.BBPCustomPosters
{
    public class WeightedCustomPoster : WeightedPosterObject
    {
        public WeightedCustomPoster(CustomPosterObject poster, int weight)
        {
            selection = poster;
            customPoster = poster;
            this.weight = weight;
        }

        public WeightedCustomPoster(CustomPosterObject poster) : this(poster,poster.Weight)
        {
        }

        public CustomPosterObject customPoster;

        public bool IncludeInLevel(string lvl, int id)
        {
            // Include IF either the whitelist is empty OR the level obeys the white/blacklist
            return customPoster.IncludeInLevel(lvl, id);
        }

    }

    public class CustomPosterObject : PosterObject
    {
        public static CustomPosterObject CreateInstance(string name, PosterPack pack, Texture2D texture, CustomPosterProperties properties)
        {
            int width = texture.width, height = texture.height, length = width / height;
            CustomPosterTextData[] customTextData = properties.textData.Build();

            if (width % height != 0)
                throw new InvalidDataException($"{pack.packName}: Poster \"{name}\" is in an invalid aspect ratio! Make sure it is in a X:1 ratio!");

            CustomPosterObject poster = CreateInstance<CustomPosterObject>();
            poster.name = name;
            poster.baseTexture = texture;
            poster.textData = customTextData.Where((CustomPosterTextData x) => x.segmentId == 0).ToArray();

            poster.pack = pack;

            poster.weight = properties.posterWeight;

            if (!properties.global)
                poster.spawnMode = PosterSpawnMode.Room;
            else if (!Enum.TryParse<PosterSpawnMode>(properties.spawnMode, true, out poster.spawnMode))
                poster.spawnMode = PosterSpawnMode.Global;

            poster.levelWhitelist = properties.levelWhitelist;
            poster.reverseWhitelist = properties.reverseWhitelist;

            if (poster.spawnMode == PosterSpawnMode.Global || properties.targetRooms.Length == 0)
            {
                poster.targetRooms = new RoomCategory[0];
            }
            else
            {
                List<RoomCategory> roomCats = new List<RoomCategory>();
                RoomCategory cat;
                foreach (string target in properties.targetRooms)
                {
                    try
                    {
                        cat = EnumExtensions.GetFromExtendedName<RoomCategory>(target);
                    }
                    catch
                    {
                        continue;
                    }

                    if (!roomCats.Contains(cat))
                        roomCats.Add(cat);
                }
                poster.targetRooms = roomCats.ToArray();
            }

            // Multi-poster conversion
            if (length > 1)
            {
                PosterObject[] posters = new PosterObject[length];
                posters[0] = poster; // Set the first object of the multi-poster array as the newly made poster

                string fixedName = texture.name;

                int i = 0;
                for (int x = 0; x < width; x += height) // Re-use height instead of a new local variable cus 1:1 aspect ratio
                {
                    Texture2D split = new Texture2D(height, height, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Point,
                        name = $"{fixedName}_{i}"
                    };

                    Color[] pixels = texture.GetPixels(x, 0, height, height);
                    split.SetPixels(pixels);
                    split.Apply();

                    if (i == 0)
                        poster.baseTexture = split;
                    else
                    {
                        posters[i] = ObjectCreators.CreatePosterObject(split, customTextData.Where((CustomPosterTextData y) => y.segmentId == i).ToArray());
                        posters[i].name = $"{fixedName}_{i}";
                    }
                    i++;

                    Destroy(texture); // Frees up the full texture as it is now unused
                }

                poster.multiPosterArray = posters;
            }

            return poster;
        }

        public bool IncludeInLevel(string lvl, int id)
        {
            if (!pack.Enabled) return false;
            if (levelWhitelist.Length == 0) return true;

            if (lvl == "INF" && levelWhitelist.Contains(lvl + id) != reverseWhitelist) return true; // Infinite Floors support
            return levelWhitelist.Contains(lvl) != reverseWhitelist;
        }

        public PosterPack pack;

        private string[] levelWhitelist;
        private bool reverseWhitelist;

        public RoomCategory[] targetRooms;
        public PosterSpawnMode spawnMode;

        private int weight;
        public int Weight => weight > 0 ? weight : pack.DefaultWeight;
    }

    public class CustomPosterTextData : PosterTextData
    {
        public int segmentId = 0;

        public CustomPosterTextData(PosterTextSettings builder)
        {
            textKey = builder.textKey;
            position = builder.position.ToIntVector2();
            size = builder.size.ToIntVector2();
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

    public class SerializableIntVector2
    {
        public IntVector2 ToIntVector2()
        {
            return new IntVector2(x, y);
        }

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

    public enum PosterSpawnMode
    {
        Global,
        Room,
        Chalkboard
    }

    public class CustomPosterProperties
    {
        [JsonIgnore] public static CustomPosterProperties defaultProperties = new CustomPosterProperties();

        /* How common/rare it will be in comparison to other posters.
         * If set to 0, this will be set to the default desired value.
        */
        [Range(0,float.MaxValue)]
        public int posterWeight = 0;

        [Obsolete("Poster length is automatically dictated by aspect ratio!")]
        public uint posterLength = 1;

        [Obsolete("Please use 'posterSpawnType' instead!")]
        public bool global = true;

        /* A whitelist indicating which levels will contain the poster.
         *  Levels are declared via string values.
         *  If empty, it will spawn in any level.
        */
        public string[] levelWhitelist = new string[0];

        // If true, the level whitelist will become a blacklist, and the
        // poster will be excluded from the floors declared in the list.
        public bool reverseWhitelist = false;

        // PosterTextData entries go here
        public PosterTextSettings[] textData = new PosterTextSettings[0];

        /* Target room categories that the poster will aim to spawn in.
         * This reflects the RoomCategory enum, and is compatible with extended categories
         * from other mods.
         *
         * Default entries (as of BB+ v0.5):
         * Null, Hall, Class, Office, Faculty, Test, FieldTrip, Buffer, Special, Mystery
        */
        public string[] targetRooms = new string[0];

        /* The type of poster pool the poster will be included in, reflects PosterSpawnMode enum.
         * Global - the poster can appear in any wall in the level
         * Room - the poster will only appear in listed room types
         * Chalkboard - appears as chalkboard, only filtered if target rooms are included
        */
        public string spawnMode = "Global";
        
    }
}
