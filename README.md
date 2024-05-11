# BB+ Custom Posters Mod
![Version](https://img.shields.io/badge/version-2024.3.0.0-purple) ![License](https://img.shields.io/badge/license-MIT-blue?link=https://github.com/LuisRandomness/BaldiPlus-CustomPosters/blob/main/LICENSE)
![BB+ version](https://img.shields.io/badge/bb+-0.5-69C12E?color=green) ![BepInEx version](https://img.shields.io/badge/bepinex-5.4.23-69C12E?color=yellow&link=https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23) ![Json.NET version](https://img.shields.io/badge/json.net-13.0.3-69C12E?color=orange) ![MTM101BMDE version](https://img.shields.io/badge/mtm101bmde-4.1.1.1-69C12E?color=red&link=https://gamebanana.com/mods/383711)
 
A mod for [*Baldi's Basics Plus v0.5*](https://store.steampowered.com/app/1275890/Baldis_Basics_Plus/), powered by [*BepInEx*](https://github.com/BepInEx/BepInEx), [*Json.NET*](https://github.com/JamesNK/Newtonsoft.Json) and the [*Baldi's Basics Plus Dev API*](https://gamebanana.com/mods/383711), that allows the creation of custom wall posters by using user-provided images.

Requires [*BepInEx v5.4.23 and above*](https://github.com/BepInEx/BepInEx/tag/v5.4.22) and [*Baldi's Basics Plus Dev API v4.1.1.1 and above*](https://gamebanana.com/mods/383711).

Json.NET is bunded with the download, and is [licensed under MIT](https://github.com/JamesNK/Newtonsoft.Json?tab=MIT-1-ov-file#MIT-1-ov-file).

### Features provided by the mod
- Creation of single-wall posters by providing either **only** a **.PNG** image or also a **.JSON** definition file
	- **Custom posters should be added in `[BB+ INSTALL PATH]/BALDI_Data/StreamingAssets/Modded/io.github.luisrandomness.bbp_custom_posters/Posters`, and definition files must have the extension `.png.json`.**
	- **Other mods can depend on this and have their own poster paths read using the `CustomPostersPlugin.AddBuiltInPackFromMod` and/or '`CustomPostersPlugin.AddBuiltInPackFromDirectory` helper methods.**
- Setting custom properties to any created poster, such as:
	- The poster's *'weight'* probability in the generator
	- ~~The width of the poster (allowing for *'multi-posters'*)~~ (This is now automatically set.)
	- Floors the poster can/cannot appear on, **based on level name**
	- Poster Text Data support (for both single and *'multi'* posters)
	- What room categories can it specifically spawn in, and whether it can spawn as part of the level's global poster pool or not
	- **Examples can be seen in the repository's `Examples` folder.**
- The following configuration options, **which can be accessed from `[BB+ INSTALL PATH]/BepInEx/config/io.github.luisrandomness.bbp_custom_posters.cfg`**:
	- Default weight of created posters if not specifically set
	- White/blacklist of posters added by either the base game or other mods
	- Debug option to log (**almost**) all posters in the floor to the console, useful for balancing created poster weights
	- Debug option to create a dummy example file containing all customizable settings available
- Runtime-created text poster textures are now named for easier debugging using tools such as UnityExplorer 

**Planned features for future versions:**
- Better configuration system that accepts arrays (maybe)
- Ability to add custom chalkboards
- Documentation on how to use the mod
- ~~Support for possible "poster packs" (.zip archives)~~
  - A menu to toggle poster packs

### Screenshots
*Some posters (excluding the ones in the first and last screenshots) seen here are not included with the normal download.*
![Custom Multi-Poster with Text Data](https://i.imgur.com/pOoEoPV.png)
![Custom Multi-Poster in Classroom](https://i.imgur.com/gGWnWrJ.png)
![Custom Poster in Library](https://i.imgur.com/1mu1d35.png)
![Custom Poster in Clinic](https://i.imgur.com/261k0lO.png)
![Custom Poster in Classroom](https://i.imgur.com/M0u4FBS.png)
![Custom Multi-Poster in Hall](https://i.imgur.com/7CbzmRg.png)