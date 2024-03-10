using HarmonyLib;
using UnityEngine;

namespace LuisRandomness.BBPCustomPosters.Patches
{
    [HarmonyPatch(typeof(TextTextureGenerator))]
    [HarmonyPatch("GenerateTextTexture")]
    internal class TextTexturePatch
    {
        static void Postfix(Texture2D __result, PosterObject poster)
        {
            __result.name = poster.name + "_WithText";
        }
    }
}
