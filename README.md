# BB+ Custom Posters Mod
![Version](https://img.shields.io/badge/version-2025.3-purple) ![GitHub License](https://img.shields.io/github/license/uncertainluei/BaldiPlus-CustomPosters)
![BB+ version](https://img.shields.io/badge/bb+-0.11-69C12E?color=green) ![BepInEx version](https://img.shields.io/badge/bepinex-5.4.23-69C12E?color=yellow&link=https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23) ![Json.NET version](https://img.shields.io/badge/json.net-13.0.3-69C12E?color=orange) ![MTM101BMDE version](https://img.shields.io/badge/mtm101bmde-7.0.1.1-69C12E?color=red&link=https://gamebanana.com/mods/383711)
 
A mod for [*Baldi's Basics Plus v0.11*](https://store.steampowered.com/app/1275890/Baldis_Basics_Plus/), powered by [*BepInEx*](https://github.com/BepInEx/BepInEx), [*Json.NET*](https://github.com/JamesNK/Newtonsoft.Json) and the [*Baldi's Basics Plus Dev API*](https://gamebanana.com/mods/383711), that allows the creation of user-provided wall posters.

Requires [*BepInEx v5.4.23 and above*](https://github.com/BepInEx/BepInEx/tag/v5.4.23) and [*Baldi's Basics Plus Dev API v8.0.0.0 and above*](https://gamebanana.com/mods/383711).

Json.NET is bunded with the download, and is [licensed under MIT](https://github.com/JamesNK/Newtonsoft.Json?tab=MIT-1-ov-file#MIT-1-ov-file).

## Mod Features
- Creation of single-wall posters by providing at least either a **.PNG**/**.JP(E)G** image or a **.JSON** definition file
	- **Custom posters should be added in `[BB+ INSTALL PATH]/BALDI_Data/StreamingAssets/Modded/io.github.uncertainluei.baldiplus.customposters/Posters`.**
	- **A definition file is attached to the poster's texture image if it's named after the latter's file name (incl. extension) i.e. `MyPoster.png.json`.**
	- **An overlay (which will appear over its baked text) can be provided with an image file named after the base texture, and the `_Overlay` suffix.**
	- **Other mods can depend on this and have their own poster paths read using the `CustomPostersPlugin.AddBuiltInPackFromMod` and/or '`CustomPostersPlugin.AddBuiltInPackFromDirectory` helper methods.**
- Setting custom properties to any created poster, such as:
	- The poster's *'weight'* probability in the generator
	- Floors the poster can/cannot appear on, **based on the scene/level title and level object type**
	- Poster Text Data (for both single and *'multi'* posters)
	- The poster's spawning mode, whether it can spawn anywhere, what rooms it can spawn in, or if it spawns as a chalkboard
	- The preset of the poster, which affects the fallback texture and text data that it'll use
	- **Examples can be seen in the repository's `Examples` folder.**
- The following configuration options, **which can be accessed from `[BB+ INSTALL PATH]/BepInEx/config/io.github.uncertainluei.baldiplus.customposters.cfg`**:
	- Default weight of created posters if not specifically set
	- White/blacklist of posters added by either the base game or other mods
	- Debug option to log (**almost**) all posters in the floor to the console, useful for balancing created poster weights
- Runtime-created text poster textures are now named for easier debugging using tools such as UnityExplorer
- Support for Blayms's [**Better Chalkboard Font** mod](https://gamebanana.com/mods/525402).

**Planned features for future versions:**
- Documentation on how to use the mod **(IMPORTANT!!)**
- Better configuration system that accepts arrays (maybe)
- A menu to toggle and reload poster packs (reloading functionality is already in place)

## Build Instructions
This is for building the mod's .DLL and .PDB file, which should be found at the `CustomPosters/bin/Debug*/netstandard2.0/` directory.

\*`Release` if built with the *Release* configuration

### Visual Studio 2022 (.NET)
Run `CustomPosters.sln` in Visual Studio as a project. Building should then be as simple as going to **Build -> Build Solution** in the menu bar (or pressing Ctrl+Shift+B).

### Terminal
Make sure you have the [.NET SDK](https://dotnet.microsoft.com/en-us/download) installed. Open your terminal on the cloned/downloaded repository's directory, and execute:

`dotnet build .\CustomPosters.sln`

This will build to the *Debug* configuration by default, append `-c Release` if you want to built it with the *Release* configuration.