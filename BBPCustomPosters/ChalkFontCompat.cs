using BepInEx;
using BepInEx.Bootstrap;

using BetterChalkboardFont;
using BetterChalkboardFont.Plugins;

using HarmonyLib;

using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;

namespace UncertainLuei.BaldiPlus.CustomPosters.Compatibility
{
    static class ChalkFontCompat
    {
        internal const string ModGuid = "blayms.tbb.baldiplus.betterchkfont";

        internal static void Initialize()
        {
            PosterPresetStorage.chalkOverlay = BasePlugin.Chk_Overlay;
            LoadingEvents.RegisterOnAssetsLoaded(Chainloader.PluginInfos[ModGuid], LoadFonts(), false);
        }

        // Loads chalkboard fonts from the API on behalf of Blayms's Chalkboard Fonts mod so the fonts can be recognized by Custom Posters
        private static IEnumerator LoadFonts()
        {
            yield return 1;
            yield return "Loading Better Chalkboard fonts...";

            PosterObject[] chalkboards = Resources.FindObjectsOfTypeAll<PosterObject>().Where(x => x.GetInstanceID() >= 0 && x.name.ToLower().StartsWith("chk_")).ToArray();
            OnMainMenuLoad.onceBooted = true;
            
            foreach (PosterObject board in chalkboards)
            {
                foreach (PosterTextData data in board.textData)
                {
                    if (data.font.name == "COMIC_12_Smooth_Pro")
                    {
                        data.font = BasePlugin.BaldChalk_12_Smooth;
                        continue;
                    }
                    if (data.font.name == "COMIC_18_Smooth_Pro")
                    {
                        data.font = BasePlugin.BaldChalk_18_Smooth;
                        continue;
                    }
                    if (data.font.name == "COMIC_24_Smooth_Pro")
                    {
                        data.font = BasePlugin.BaldChalk_24_Smooth;
                        continue;
                    }
                    if (data.font.name == "COMIC_36_Smooth_Pro")
                    {
                        data.font = BasePlugin.BaldChalk_36_Smooth;
                        continue;
                    }
                }
            }
            yield break;
        }
    }
}

namespace UncertainLuei.BaldiPlus.CustomPosters.Compatibility.Patches
{
    [ConditionalPatchMod(ChalkFontCompat.ModGuid)]
    [HarmonyPatch(typeof(TextTextureGenerator_GenerateTextTexture), "Postfix")]
    public class ChalkOverlayPatch
    {
        private static bool Prefix(PosterObject poster)
        {
            // Skip for non-vanilla posters or those that do not use text data
            return poster.GetInstanceID() < 0 || poster.textData?.Length == 0;
        }
    }

    [ConditionalPatchMod(ChalkFontCompat.ModGuid)]
    [HarmonyPatch(typeof(TextTextureGenerator_LoadPosterData), "Postfix")]
    public class ChalkTextPatch
    {
        private static List<string> textToRestore = new List<string>(); 

        private static bool Prefix(object[] __args, PosterObject poster)
        {

            textToRestore.Clear();
            if (poster.GetInstanceID() >= 0 && poster.name.StartsWith("Chk") && poster.textData.Length > 0) return true;

            TextTextureGenerator instance = (TextTextureGenerator)__args[0];

            bool result = true;
            foreach (TMP_Text text in instance.textureTMPPre)
            {
                if (text.font.name.StartsWith("BaldChalkFont"))
                {
                    textToRestore.Add("");
                    continue;
                }
                result = false;
                textToRestore.Add(text.text);
            }
            return result;
        }

        private static void Postfix(object[] __args)
        {
            TextTextureGenerator instance = (TextTextureGenerator)__args[0];
            int i = 0, length = textToRestore.Count;
            for (; i < length; i++)
            {
                if (!textToRestore[i].IsNullOrWhiteSpace())
                    instance.textureTMPPre[i].text = textToRestore[i];
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool patchedIfCheck = false;

            CodeInstruction[] array = instructions.ToArray();
            int length = array.Length;

            for (int i = 0; i < length; i++)
            {
                //if (!poster.name.Contains("Chk_"))
                //    return;
                if (!patchedIfCheck &&
                    array[i].opcode == OpCodes.Ldarg_1 &&
                    array[i+1].opcode == OpCodes.Callvirt &&
                    array[i+2].opcode == OpCodes.Ldstr &&
                    array[i+3].opcode == OpCodes.Callvirt &&
                    array[i+4].opcode == OpCodes.Stloc_0 &&
                    array[i+5].opcode == OpCodes.Ldloc_0 &&
                    array[i+6].opcode == OpCodes.Brfalse)
                {
                    patchedIfCheck = true;
                    i += 6;
                    continue;
                }
                yield return array[i];
            }

            if (!patchedIfCheck)
                CustomPostersPlugin.Log.LogError("Transpiler \"CustomPosters.Compatibility.ChalkTextPatch.Transpiler\" did not go through!");

            yield break;
        }
    }
}