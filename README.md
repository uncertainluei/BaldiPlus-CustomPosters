# BB+ Custom Posters Mod
![Version](https://img.shields.io/badge/version-2024.1.0.0-purple) ![License](https://img.shields.io/badge/license-MIT-blue?link=https://github.com/LuisRandomness/BBP_CustomPostersMod/blob/main/LICENSE)
![BB+ version](https://img.shields.io/badge/bb+-0.4.1-69C12E?color=green) ![BepInEx version](https://img.shields.io/badge/bepinex-5.4.22-69C12E?color=yellow&link=https://github.com/BepInEx/BepInEx/releases/tag/v5.4.22) ![MTM101BMDE version](https://img.shields.io/badge/mtm101bmde-3.1.0.0-69C12E?color=red&link=https://gamebanana.com/mods/383711)
 
A mod for [*Baldi's Basics Plus v0.4.1*](https://store.steampowered.com/app/1275890/Baldis_Basics_Plus/), powered by [*BepInEx*](https://github.com/BepInEx/BepInEx) and the [*Baldi's Basics Plus Dev API*](https://gamebanana.com/mods/383711), that allows the creation of custom wall posters by using user-provided images.

Requires [*BepInEx v5.4.x*](https://github.com/BepInEx/BepInEx/tag/v5.4.22) and [*Baldi's Basics Plus Dev API v3.1.0.0*](https://gamebanana.com/mods/383711).

### Features provided by the mod
- Creation of single-wall posters by providing either **only** a **.PNG** image or also a **.JSON** definition file
	- **Custom posters should be added in `[BB+ INSTALL PATH]/BALDI_Data/StreamingAssets/Modded/io.github.luisrandomness.bbp_custom_posters/Posters`, and definition files must have the extension `.png.json`.**
- Setting custom properties to any created poster, such as:
	- The poster's *'weight'* probability in the generator
	- The width of the poster (allowing for *'multi-posters'*)
	- Floors the poster can/cannot appear on, **based on level name**
	- **Examples can be seen in the repository's `Examples` folder.**
- The following configuration options, **which can be accessed from `[BB+ INSTALL PATH]/BepInEx/config/io.github.luisrandomness.bbp_custom_posters.cfg`**:
	- Toggle to change poster probabilities according to the poster amount, and a percentage value option on how large the change is
	- Default weight of created posters if not specifically set
	- Debug option to log (**almost**) all posters in the floor to the console, useful for balancing created poster weights

**Planned features for future versions:**
- Better configuration system that accepts arrays
	- White/blacklist of posters added by either the base game or other mods
- Poster Text Data support (although quite slim)
- Ability to add custom chalkboards

### Screenshots
*All the posters (sans the one in the last screenshot) seen here are not included with the normal download.*
![Custom Multi-Poster in Classroom](https://i.imgur.com/gGWnWrJ.png)
![Custom Poster in Library](https://i.imgur.com/1mu1d35.png)
![Custom Poster in Clinic](https://i.imgur.com/261k0lO.png)
![Custom Poster in Classroom](https://i.imgur.com/M0u4FBS.png)
![Custom Multi-Poster in Hall](https://i.imgur.com/7CbzmRg.png)