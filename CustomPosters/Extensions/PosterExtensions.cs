using MTM101BaldAPI;
using MTM101BaldAPI.PlusExtensions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TMPro;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.CustomPosters
{
    public class WeightedCustomPoster : WeightedPosterObject
    {
        public WeightedCustomPoster(CustomPosterObject poster, int weight)
        {
            selection = poster;
            customPoster = poster;
            this.weight = weight;
        }

        public WeightedCustomPoster(CustomPosterObject poster) : this(poster, poster.Weight)
        {
        }
        
        public CustomPosterObject customPoster;

        public bool IncludeInLevel(string lvl, LevelType type)
        {
            return customPoster.IncludeInLevel(lvl, type);
        }
    }

    public class CustomPosterObject : ExtendedPosterObject
    {
        public static CustomPosterObject CreateInstance(string name, PosterPack pack, Texture2D texture, Texture2D overlay, CustomPosterProperties properties)
        {
            PosterPreset preset = PosterPresetStorage.GetPosterPreset(properties.preset);
            bool hasTexture = true, hasOverlay = true;
            if (texture == null)
            {
                hasTexture = false;
                texture = preset.texture;
                if (texture == null)
                    throw new ArgumentNullException("No texture was found for this poster!");
            }

            if (overlay == null)
            {
                hasOverlay = false;
                overlay = preset.overlay;
            }

            int width = texture.width, height = texture.height, length = width / height;
            SegmentedPosterTextData[] customTextData = properties.textData.Build(preset);

            if (width % height != 0)
                throw new InvalidDataException($"{pack.packName}: Poster \"{name}\" is in an invalid aspect ratio! Make sure it is in a X:1 ratio!");

            if (overlay && (overlay.width != width || overlay.height != height))
            {
                overlay = null;
                CustomPostersPlugin.Log.LogWarning($"{pack.packName}: Overlay texture for \"{name}\" does not have the same size as the source texture, and thus will not be displayed!");
            }

            CustomPosterObject poster = CreateInstance<CustomPosterObject>();
            poster.name = name;
            poster.baseTexture = texture;
            poster.textData = customTextData.Where((x) => x.segmentId == 0).ToArray();
            poster.pack = pack;

            poster.providesTexture = hasTexture;
            poster.providesOverlay = hasOverlay;

            poster.overlayData = new PosterImageData[0];
            if (overlay)
                poster.overlayData = new PosterImageData[] { new PosterImageData(overlay, new IntVector2(0, 0))};

            poster.weight = properties.posterWeight;

            poster.spawnMode = PosterSpawnMode.Global;
            if (Enum.TryParse(properties.spawnMode, true, out PosterSpawnMode newMode))
                poster.spawnMode = newMode;

            poster.lvlTitleWhitelist = properties.lvlTitleWhitelist;
            poster.reverseTitleWhitelist = properties.reverseTitleWhitelist;

            poster.lvlTypeWhitelist = properties.lvlTypeWhitelist.ToEnumArray<LevelType>();
            poster.reverseTypeWhitelist = properties.reverseTypeWhitelist;

            if (properties.levelWhitelist?.Length > 0)
            {
                CustomPostersPlugin.Log.LogWarning($"{pack.packName}: Poster \"{name}\" is using legacy property 'levelWhitelist', which will be removed next update! Please use 'lvlTitleWhitelist' and 'reverseTitleWhitelist' instead!");
                poster.lvlTitleWhitelist = properties.levelWhitelist;
                poster.reverseTitleWhitelist = properties.reverseWhitelist;
            }

            if (poster.spawnMode == PosterSpawnMode.Global || properties.targetRooms.Length == 0)
                poster.targetRooms = new RoomCategory[0];
            else
                poster.targetRooms = properties.targetRooms.ToEnumArray<RoomCategory>();

            // Multi-poster conversion
            if (length > 1)
            {
                ExtendedPosterObject[] posters = new ExtendedPosterObject[length];
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
                        posters[i] = CreateInstance<ExtendedPosterObject>();
                        posters[i].overlayData = new PosterImageData[0];
                        posters[i].baseTexture = split;
                        posters[i].textData = customTextData.Where((y) => y.segmentId == i).ToArray();
                        posters[i].name = $"{name}_{i}";
                    }

                    if (overlay)
                    {
                        split = new Texture2D(height, height, TextureFormat.RGBA32, false)
                        {
                            filterMode = FilterMode.Point,
                            name = $"{fixedName}_{i}"
                        };

                        pixels = texture.GetPixels(x, 0, height, height);
                        split.SetPixels(pixels);
                        split.Apply();

                        if (i == 0)
                        {
                            poster.overlayData[0].texture = split;
                            poster.overlayData[0].size.x = height;
                            poster.overlayData[0].size.z = height;
                        }
                        else
                            posters[i].overlayData = new PosterImageData[] { new PosterImageData(split, new IntVector2(0, 0)) };
                    }
                    i++;

                    Destroy(texture); // Frees up the full posters and overlay textures as they're now unused
                    if (overlay)
                        Destroy(overlay);
                }

                poster.multiPosterArray = posters;
            }

            return poster;
        }

        private void OnDestroy()
        {
            if (multiPosterArray?.Length > 1)
            {
                foreach (PosterObject poster in multiPosterArray)
                {
                    if (providesTexture && poster.baseTexture)
                        Destroy(poster.baseTexture);
                    if (providesOverlay && overlayData?.Length > 0)
                        Destroy(((ExtendedPosterObject)poster).overlayData[0].texture);
                }
                return;
            }

            if (providesTexture && baseTexture)
                Destroy(baseTexture);
            if (providesOverlay && overlayData?.Length > 0)
                Destroy(overlayData[0].texture);
        }

        public bool IncludeInLevel(string name, LevelType type)
        {
            if (!pack.Enabled) return false;

            // Include IF either the whitelist is empty OR the level obeys the white/blacklists
            return (lvlTitleWhitelist.Length == 0 || lvlTitleWhitelist.Contains(name) != reverseTitleWhitelist) &&
                (lvlTypeWhitelist.Length == 0 || lvlTypeWhitelist.Contains(type));
        }

        public PosterPack pack;

        private bool providesTexture;
        private bool providesOverlay;

        private string[] lvlTitleWhitelist;
        private bool reverseTitleWhitelist;

        private LevelType[] lvlTypeWhitelist;
        private bool reverseTypeWhitelist;

        public RoomCategory[] targetRooms;
        public PosterSpawnMode spawnMode;

        private int weight;
        public int Weight => weight > 0 ? weight : pack.DefaultWeight;
    }

    public class SegmentedPosterTextData : PosterTextData
    {
        public int segmentId = 0;
        
        public SegmentedPosterTextData(PosterTextSettings builder, PosterTextData reference = null) : base() // The PosterTextData() constructor is run first
        {
            textKey = builder.textKey;
            segmentId = builder.segmentId;
            fontSize = 12;
            color = Color.black;

            // If it uses the placeholder value (aka it wasn't overwritten)
            if (reference != null)
            {
                position.x = reference.position.x;
                position.z = reference.position.z;

                size.x = reference.size.x;
                size.z = reference.size.z;

                fontSize = reference.fontSize;
                font = reference.font;

                color = reference.color;
                alignment = reference.alignment;
            }
            // Set position if no placeholder value is found
            if (builder.position != null)
            {
                position.x = builder.position.x;
                position.z = builder.position.y;
            }

            // Set position if no placeholder value is found
            if (builder.size != null)
            {
                size.x = builder.size.x;
                size.z = builder.size.y;
            }

            if (builder.fontSize >= 0)
                fontSize = builder.fontSize;

            if (ColorUtility.TryParseHtmlString(builder.color, out Color newColor))
                color = newColor;
            else if (builder.color != "")
                CustomPostersPlugin.Log.LogWarning("Text color \"" + builder.color + "\" could not be properly parsed! Using default color instead...");

            if (CustomPostersPlugin.fontAssets.TryGetValue(builder.font, out TMP_FontAsset newFont))
                font = newFont;
            else
            {
                if (builder.font != "")
                    CustomPostersPlugin.Log.LogWarning("Font \"" + font + "\" could not be found!");

                if (font != null)
                {
                }
                else if (!CustomPostersPlugin.fontAssets.TryGetValue("COMIC_12_Pro", out font))
                {
                    CustomPostersPlugin.Log.LogWarning("Fallback font could not be found, using default font asset...");
                    font = TMP_Settings.defaultFontAsset;
                }
                else if (builder.font != "")
                    CustomPostersPlugin.Log.LogWarning("Using \"COMIC_12_Pro\" font...");
            }

            style = FontStyles.Normal;
            if (builder.bold)
                style |= FontStyles.Bold;
            if (builder.italic)
                style |= FontStyles.Italic;
            if (builder.underline)
                style |= FontStyles.Underline;
            
            if (builder.alignment != "" && Enum.TryParse(builder.alignment, true, out TextAlignmentOptions newAlignment))
                alignment = newAlignment;
        }
    }

    public static class PosterExtensions
    {
        public static bool IsBlacklisted(this WeightedPosterObject poster)
        {
            // Exclude non-default poster packs, including the personal posters folder
            if (poster is WeightedCustomPoster && ((WeightedCustomPoster)poster).customPoster.pack.packType != PosterPackType.Mod) return false;

            return CustomPostersConfig.blacklistedPosters.Contains(poster.selection.name.Trim())
                != CustomPostersConfig.blacklistInvert.Value;
        }

        public static string GetSource(this WeightedPosterObject poster)
        {
            if (poster is WeightedCustomPoster weightedCustom)
                return weightedCustom.customPoster.pack.packName;

            if (poster.selection.GetInstanceID() >= 0)
                return "Vanilla";

            return "Unknown";
        }

        private static readonly SegmentedPosterTextData[] blankData = new SegmentedPosterTextData[0];

        public static SegmentedPosterTextData[] Build(this PosterTextSettings[] customData, PosterPreset preset)
        {
            int customCount = customData.Length, presetCount = preset.textData.Length;

            if (customCount == 0 && presetCount == 0)
                return blankData;

            int count = Math.Max(customCount, presetCount), i = 0;
            PosterTextData currentData;
            SegmentedPosterTextData[] newData = new SegmentedPosterTextData[count];

            for (; i < customCount; i++)
            {
                currentData = null;
                if (i < presetCount)
                    currentData = preset.textData[i];

                newData[i] = new SegmentedPosterTextData(customData[i], currentData);
            }
            for (; i < presetCount; i++)
            {
                newData[i] = new SegmentedPosterTextData(PosterTextSettings.defaultSettings, preset.textData[i]);
            }

            return newData;
        }

        public static SerializableIntVector2 ToSerializable(this IntVector2 vector)
        {
            return new SerializableIntVector2()
            {
                x = vector.x,
                y = vector.z
            };
        }
    }
}
