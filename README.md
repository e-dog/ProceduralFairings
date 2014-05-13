# Procedural Fairings

Procedural Fairings mod for Kerbal Space Program.

[Forum thread](http://forum.kerbalspaceprogram.com/showthread.php/39512-0-21-Procedural-Fairings-2-1-base-rings-new-models-and-more)

[Download](https://github.com/e-dog/ProceduralFairings/releases)

[License](http://creativecommons.org/licenses/by/3.0/)


## Installation
Remove old version of the mod.

Copy ProceduralFairings into "Gamedata" in your KSP folder.

## Installation Notes

If you downloaded KSP from Squad's website, then the KSP folder is where you unzipped it when you first downloaded the game

If you downloaded KSP from Steam, then right-clicking KSP in your Steam library, select "properties," switching to the "local files" tab, and pressing "browse local files" opens the game folder.

In the KSP main folder a "GameData" folder contains all add-ons; without any add-ons, it contains only the "Squad" and "NASAMission" sub-folders - the stock "add-ons" from the developers of the game. Unzip the ProceduralFairings folder into your Gamedata folder.

## Tutorial
[Pictures](http://imgur.com/a/xCF0q)

### Steps
1. Put a fairing base under your payload (all Procedural Fairings parts are in the Aerodynamics tab) and a decoupler if necessary.
2. Attached fairings automatically reshape for your payload.
3. Enabling symmetry on fairings will encapsulate your payload
4. Rearrange stages to jettison fairings at the proper stage.

### Inline Fairings
- Flipping another fairing base over and adding it above the payload will cause side fairings to stick to it instead of creating a nose cone, thereby creating inline fairings between two bases.
- Procedural Fairings includes low-profile base rings intended for inline fairings.

### Controls
Right-click parts and use tweakables.

## Career mode
Maximum (and minimum) part size is limited by tech. See GameData/ProceduralFairings/common.cfg for details.

## Version history
**3.00**
- First release on GitHub.
- Moved files up to GameData folder (no Keramzit folder anymore). Make sure to delete old mod before installing (which is a good practice anyway).
- Added new resizable fairing bases with configurable number of side nodes.
- Old parts (bases and adapter) are deprecated. Launched vessels should be fine, but you might have trouble loading old designs in VAB/SPH in career mode.
- Added new part: Thrust Plate Multi-Adapter.
- Using KSPAPIExtensions by Swamp-Ig for better tweakables.
- Removed old keyboard-based tweaks - use new tweakables.
- Tweaking outer diameter (with fairings), instead of inner radius.
- Added fairing decoupler torque tweakable.
- Side nodes (for attaching fairings) get larger with the base size to make them more sturdy in KSP 0.23.5+
- Tech limits are not checked in sandbox mode anymore.
- Extra payload radius is now zero by default.
- Fixed interstage adapter decoupling with fuselage fairings.
