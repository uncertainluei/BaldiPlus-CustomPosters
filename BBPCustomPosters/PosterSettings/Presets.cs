using System;
using System.Collections;
using System.Collections.Generic;

using System.Linq;
using System.Text;

using MTM101BaldAPI;
using MTM101BaldAPI.ObjectCreation;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.CustomPosters
{
    public static class PosterPresetStorage
    {
        private static readonly Dictionary<string, PosterPreset> presets = new Dictionary<string, PosterPreset>
        {
            {
                "",
                new PosterPreset()
                {
                    textData = new PosterTextData[0]
                }
            }
        };

        internal static Texture2D chalkOverlay;

        internal static IEnumerator AddPresets()
        {
            yield return 1;
            yield return "Adding poster presets";
            PosterObject[] posters = Resources.FindObjectsOfTypeAll<PosterObject>().Where(x => x.GetInstanceID() >= 0).ToArray();

            PosterObject posterToCopy = posters.First(x => x.name == "BLT_All");
            AddPosterPreset("bulletinboard", posterToCopy.textData, posterToCopy.baseTexture);

            posterToCopy = posters.First(x => x.name == "Chk_Apple");
            AddPosterPreset("chalkboard", posterToCopy.textData, posterToCopy.baseTexture, chalkOverlay);
            AddPosterPreset("chalk_apple", posterToCopy.textData, posterToCopy.baseTexture, chalkOverlay);

            AddPosterPreset("chalk_chalk", posters.First(x => x.name == "Chk_Chalk").textData, posterToCopy.baseTexture, chalkOverlay);
            AddPosterPreset("chalk_treehint", posters.First(x => x.name == "Chk_TreeHint").textData, posterToCopy.baseTexture, chalkOverlay);

            posterToCopy = posters.First(x => x.name == "Chk_Possible");
            AddPosterPreset("chalk_possible", posterToCopy.textData, posterToCopy.baseTexture, chalkOverlay);

            posterToCopy = posters.First(x => x.name == "Chk_BaldiSays");
            AddPosterPreset("chalk_baldisays", posterToCopy.textData, posterToCopy.baseTexture, chalkOverlay);

            posterToCopy = posters.First(x => x.name == "Chk_Mathh");
            AddPosterPreset("chalk_math", posterToCopy.textData, posterToCopy.baseTexture, chalkOverlay);

            posterToCopy = posters.First(x => x.name == "CLS_BaldiSays_1");
            AddPosterPreset("baldisays", posterToCopy.textData, posterToCopy.baseTexture);

            posterToCopy = posters.First(x => x.name == "HNT_Phone");
            AddPosterPreset("hint", posterToCopy.textData, null);

            posterToCopy = posters.First(x => x.name == "HNT_Rules");
            AddPosterPreset("rules", posterToCopy.textData, posterToCopy.baseTexture);

            posterToCopy = posters.First(x => x.name == "BaldiPoster");
            AddPosterPreset("character", posterToCopy.textData, null);

            yield break;
        }

        public static void AddPosterPreset(string key, PosterTextData[] txt, Texture2D tex = null, Texture2D olTex = null)
        {
            key = key.ToLower();
            if (presets.ContainsKey(key))
                throw new ArgumentException($"Poster preset \"{key}\" already exists in the dictionary!");

            if (txt == null)
                txt = new PosterTextData[0];

            presets.Add(key, new PosterPreset()
            {
                texture = tex,
                overlay = olTex,
                textData = txt
            });
        }

        public static PosterPreset GetPosterPreset(string key)
        {
            key = key.ToLower();
            if (presets.ContainsKey(key))
                return presets[key];

            return presets[""];
        }
    }

    public struct PosterPreset
    {
        public Texture2D texture;
        public Texture2D overlay;
        public PosterTextData[] textData;
    }
}
