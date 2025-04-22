using HarmonyLib;

using MTM101BaldAPI.Patches;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.CustomPosters.Patches
{
    [HarmonyPatch(typeof(ExtendedPosterEvenWithoutTextPatch))]
    [HarmonyPatch("Prefix")]
    public class PlainExtendedPosterPatch
    {
        /* Go ahead, call me lazy for not making posters only use
         * ExtendedPosterObject if they have an overlay on, I don't care
        */
        static bool Prefix(PosterObject poster)
        {
            if (!(poster is CustomPosterObject ext)) return true;
            if (ext.overlayData?.Length > 0) return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(TextTextureGenerator))]
    [HarmonyPatch("GenerateTextTexture")]
    public class TextTexturePatch
    {
        static void Postfix(Texture2D __result, PosterObject poster)
        {
            __result.name = poster.name + "_Localized";
        }
    }

    [HarmonyPatch(typeof(LevelBuilder))]
    [HarmonyPatch("LoadRoom", typeof(RoomAsset), typeof(IntVector2), typeof(IntVector2), typeof(Direction), typeof(bool), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D))]
    public class RoomPlacementPatch
    {
        private static string _name;
        private static LevelType _type;

        private static void Postfix(RoomController __result, LevelObject ___ld)
        {
            _name = CoreGameManager.Instance.sceneObject.levelTitle;

            _type = LevelType.Schoolhouse;
            if (___ld)
                _type = ___ld.type;

            foreach (PosterPack pack in CustomPostersPlugin.activePosterPacks)
                if (pack.roomPosters.TryGetValue(__result.category, out List<WeightedCustomPoster> _posters))
                    __result.potentialPosters.AddRange(_posters.Where((WeightedCustomPoster x) => x.IncludeInLevel(_name, _type)));

            __result.potentialPosters.RemoveAll((WeightedPosterObject x) => x.IsBlacklisted() || x.selection == null);
        }
    }

    [HarmonyPatch(typeof(ChalkboardBuilderFunction))]
    [HarmonyPatch("Build")]
    public class ChalkboardBuilderPatch
    {
        private static string _name;
        private static LevelType _type;

        private static bool Prefix(ChalkboardBuilderFunction __instance, ref WeightedPosterObject[] ___chalkBoards, LevelBuilder builder)
        {
            _name = CoreGameManager.Instance.sceneObject.levelTitle;

            _type = LevelType.Schoolhouse;
            if (builder.ld)
                _type = builder.ld.type;

            List<WeightedPosterObject> weightedPosters = new List<WeightedPosterObject>(___chalkBoards);

            foreach (PosterPack pack in CustomPostersPlugin.activePosterPacks)
            {
                if (pack.chalkboardPosters.TryGetValue(RoomCategory.Null, out List<WeightedCustomPoster> _posters))
                    weightedPosters.AddRange(_posters.Where((WeightedCustomPoster x) => x.IncludeInLevel(_name, _type)));
                if (pack.chalkboardPosters.TryGetValue(__instance.room.category, out _posters))
                    weightedPosters.AddRange(_posters.Where((WeightedCustomPoster x) => x.IncludeInLevel(_name, _type)));
            }

            weightedPosters.RemoveAll((WeightedPosterObject x) => x.IsBlacklisted() || x.selection == null);
            ___chalkBoards = weightedPosters.ToArray();

            return ___chalkBoards.Length > 0;
        }
    }
}
