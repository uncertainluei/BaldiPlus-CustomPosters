using HarmonyLib;

using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
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

        public static RoomCategory LightbulbTesting { get; private set; }
        public static RoomCategory Wormhole { get; private set; }
        public static RoomCategory BeltRoom { get; private set; }
        public static RoomCategory Laboratory { get; private set; }


        internal static IEnumerator RegisterEnumExts()
        {
            yield return 1;
            yield return "Registering room enum extensions";

            Closet = EnumExtensions.ExtendEnum<RoomCategory>("Closet");
            Clinic = EnumExtensions.ExtendEnum<RoomCategory>("Clinic");

            Cafeteria = EnumExtensions.ExtendEnum<RoomCategory>("Cafeteria");
            Library = EnumExtensions.ExtendEnum<RoomCategory>("Library");

            // 0.10 room types
            LightbulbTesting = EnumExtensions.ExtendEnum<RoomCategory>("LightbulbTesting");
            Wormhole = EnumExtensions.ExtendEnum<RoomCategory>("Wormhole");
            BeltRoom = EnumExtensions.ExtendEnum<RoomCategory>("BeltRoom");
            Laboratory = EnumExtensions.ExtendEnum<RoomCategory>("Laboratory"); // Baldi's Lab/Teleporter room

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

            RoomAsset[] rooms = Resources.FindObjectsOfTypeAll<RoomAsset>().Where(x => (x.category == RoomCategory.Special || x.category == RoomCategory.Null) && x.roomFunctionContainer != null).ToArray();
            rooms.Where(x => x.roomFunctionContainer.name.StartsWith("Cafeteria")).Do(x => x.category = Cafeteria);
            rooms.Where(x => x.roomFunctionContainer.name.StartsWith("Library")).Do(x => x.category = Library);
            rooms.Where(x => x.roomFunctionContainer.name.StartsWith("LightbulbTesting")).Do(x => x.category = LightbulbTesting);
            rooms.Where(x => x.roomFunctionContainer.name.StartsWith("Wormhole")).Do(x => x.category = Wormhole);
            rooms.Where(x => x.roomFunctionContainer.name.StartsWith("BeltRoom")).Do(x => x.category = BeltRoom);
            rooms.Where(x => x.roomFunctionContainer.name.StartsWith("Teleporter")).Do(x => x.category = Laboratory);
            yield break;
        }

        internal static T[] ToEnumArray<T>(this string[] stringArray) where T : Enum
        {
            List<T> enums = new List<T>();
            T element;
            foreach (string target in stringArray)
            {
                try
                {
                    element = EnumExtensions.GetFromExtendedName<T>(target);
                }
                catch
                {
                    continue;
                }

                if (!enums.Contains(element))
                    enums.Add(element);
            }
            return enums.ToArray();
        }
    }
}
