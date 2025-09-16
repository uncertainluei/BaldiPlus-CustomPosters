using System;
using Newtonsoft.Json;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.CustomPosters
{
    public enum PosterSpawnMode
    {
        Global,
        Room,
        Chalkboard
    }

    public class SerializableIntVector2
    {
        public int x, y;
    }

    public class PosterTextSettings
    {

        [JsonIgnore] public static readonly PosterTextSettings defaultSettings = new PosterTextSettings();

        public string textKey = "pst_key";
        public SerializableIntVector2 position;
        public SerializableIntVector2 size;

        public string font = "";
        public int fontSize = -1;

        public bool bold = false;
        public bool italic = false;
        public bool underline = false;

        public string color = "";
        public string alignment = "";

        public int segmentId = 0;
    }

    public class CustomPosterProperties
    {
        [JsonIgnore] public static CustomPosterProperties defaultProperties = new CustomPosterProperties();

        /* How common/rare it will be in comparison to other posters.
         * If set to 0, this will be set to the default desired value.
        */
        [Range(0, int.MaxValue)]
        public int posterWeight = 0;

        /* A whitelist indicating which scene will contain the poster.
         * SceneObject titles are declared via string values.
         * If empty, it will spawn in any level.
        */
        public string[] lvlTitleWhitelist = new string[0];

        /* If true, the scene whitelist will become a blacklist, and the
         * poster will be excluded from the floors declared in the list.
        */
        public bool reverseTitleWhitelist = false;

        /* Ditto, but for the LevelObject's type.
         * Reflects the LevelType enum, 
        */
        public string[] lvlTypeWhitelist = new string[0];
        public bool reverseTypeWhitelist = false;

        // PosterTextData entries go here
        public PosterTextSettings[] textData = new PosterTextSettings[0];

        /* Target room categories that the poster will aim to spawn in.
         * This reflects the RoomCategory enum, and is also compatible with extended categories
         * from other mods.
         *
         * Default entries (as of BB+ v0.9 Pre-release 1):
         * Null, Hall, Class, Office, Faculty, Test, FieldTrip, Buffer, Special, Mystery, Store
         * 
         * Additional entries (added by Custom Posters):
         * Closet, Clinic, Cafeteria, Library, LightbulbTesting, Wormhole, BeltRoom, Laboratory
        */
        public string[] targetRooms = new string[0];

        /* The type of poster pool the poster will be included in, reflects PosterSpawnMode enum.
         * Global - the poster can appear in any wall in the level
         * Room - the poster will only appear in listed room types
         * Chalkboard - appears as chalkboard, only filtered if target rooms are included
        */
        public string spawnMode = "Global";

        /* The poster's assigned preset, which may affect the poster's texture (if none is applied)
         * and/or text data, aiming to reduce duplicate textures and writing PosterTextData easier.
         * Leaving this field blank will resort to default settings.
         * 
         * Current presets (case-insensitive):
         * BulletinBoard - Bulletin boards found in faculty rooms
         * 
         * Chalkboard / Chalk_Apple - Chalkboard template ("An apple a day saves lives")
         * Chalk_Chalk - Chalkboard template ("Chalk Board!")
         * Chalk_Treehint - Chalkboard template (vertical "COOL")
         * Chalk_Possible - Chalkboard template ("Do it! You are POSSIBLE!")
         * Chalk_Baldisays - Chalkboard template ("Baldi says: You can't hide from me! Haha!")
         * Chalk_Math - Chalkboard template ("Math = More Math = Math math math math")
         * 
         * BaldiSays - Self-explanatory.
         * Hint - Informatory posters for mechanics i.e. the PayPhone
         * Rules - Slightly wider informatory poster
         * Character - Character poster found in the Principal's office
        */
        public string preset = "";

        // LEGACY COMPAT, WILL BE REMOVED NEXT UPDATE!
        [Obsolete("Please use lvlTitleWhitelist!")] public string[] levelWhitelist = new string[0];
        [Obsolete("Please use reverseTitleWhitelist!")] public bool reverseWhitelist = false;

    }
}
