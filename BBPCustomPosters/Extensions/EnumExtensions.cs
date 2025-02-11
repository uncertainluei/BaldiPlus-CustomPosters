using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UncertainLuei.BaldiPlus.CustomPosters
{
    public static class CustomPostersEnumExts
    {
        public static RoomCategory Closet { get; private set; }
        public static RoomCategory Clinic { get; private set; }

        public static RoomCategory Cafeteria { get; private set; }
        public static RoomCategory Library { get; private set; }

        internal static IEnumerator RegisterEnumExts()
        {
            yield return 1;
            yield return "Registering room enum extensions";

            Closet = EnumExtensions.ExtendEnum<RoomCategory>("Closet");
            Clinic = EnumExtensions.ExtendEnum<RoomCategory>("Clinic");

            Cafeteria = EnumExtensions.ExtendEnum<RoomCategory>("Cafeteria");
            Library = EnumExtensions.ExtendEnum<RoomCategory>("Library");
            yield break;
        }

        internal static IEnumerator RegisterExtendedRooms()
        {
            yield return 1;
            yield return "Assigning extended room enums";

            NPC gottaSweep = NPCMetaStorage.Instance.Get(Character.Sweep).value;
            gottaSweep.potentialRoomAssets.Do(x => x.selection.category = Closet);

            NPC drReflex = NPCMetaStorage.Instance.Get(Character.DrReflex).value;
            drReflex.potentialRoomAssets.Do(x => x.selection.category = Clinic);

            RoomAsset[] rooms = Resources.FindObjectsOfTypeAll<RoomAsset>().Where(x => x.category == RoomCategory.Special).ToArray();
            rooms.Where(x => x.name.StartsWith("Cafeteria")).Do(x => x.category = Cafeteria);
            rooms.Where(x => x.name.StartsWith("Library")).Do(x => x.category = Library);
            yield break;
        }
    }
}
