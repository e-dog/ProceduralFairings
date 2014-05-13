--- Forum thread ---
http://forum.kerbalspaceprogram.com/showthread.php/39512-0-21-Procedural-Fairings-2-1-base-rings-new-models-and-more

--- Download ---
https://github.com/e-dog/ProceduralFairings/releases

--- License ---
http://creativecommons.org/licenses/by/3.0/


--- Installation ---
Remove old version of the mod.
Copy ProceduralFairings into "Gamedata" in your KSP folder.

-- Installation Notes --

-If you downloaded KSP from Squad's website, then the KSP folder is where you unzipped it when you first downloaded the game
-If you downloaded KSP from Steam, then right-clicking KSP in your Steam library, select "properties," switching to the "local files" tab, and pressing "browse local files" opens the game folder.
-The "Source" folder is unnecessary unless you want to modify and compile plug-in code.
-In the KSP main folder a "GameData" folder contains all add-ons; without any add-ons, it contains only the "Squad" sub-folder - the stock "add-on" from the developers of the game. Unzip the Keramzit folder into your Gamedata folder.

--- Use ---

-- Tutorial: --
http://imgur.com/a/xCF0q

-- Steps --
1 Put a fairing base under your payload (all Procedural Fairings parts are in the Aerodynamics tab) and a decoupler if necessary.
2 Attached fairings automatically reshape for your payload.
3 Enabling symmetry on fairings will encapsulate your payload
4 Rearrange stages to jettison fairings at the proper stage.

-- Inline Fairings --
-Flipping another fairing base over and adding it above the payload will cause side fairings to stick to it instead of creating a nose cone, thereby creating inline fairings between two bases.
-Procedural Fairings includes low-profile base rings intended for inline fairings.

-- Controls --
Right-click parts and use tweakables.

-- Career mode --
Maximum (and minimum) part size is limited by tech. See GameData/ProceduralFairings/common.cfg for details.

--- Version history ---
3.00
-Moved files up to GameData folder (no Keramzit folder anymore). Make sure to delete old mod before installing (which is a good practice anyway).
-Added new resizable fairing bases with configurable number of side nodes.
-Old parts (bases and adapter) are deprecated. Launched vessels should be fine, but you might have trouble loading old designs in VAB/SPH in career mode.
-Added new part: Thrust Plate Multi-Adapter.
-Using KSPAPIExtensions by Swamp-Ig for better tweakables.
-Removed old keyboard-based tweaks - use new tweakables.
-Tweaking outer diameter (with fairings), instead of inner radius.
-Added fairing decoupler torque tweakable.
-Side nodes (for attaching fairings) get larger with the base size to make them more sturdy in KSP 0.23.5+
-Tech limits are not checked in sandbox mode anymore.
-Extra payload radius is now zero by default.
-Fixed interstage adapter decoupling with fuselage fairings.

2.4.4
-Added tweakables.
-Rearranged tech tree, added 3.75m and 5m parts.
-Interstage adapter is available earlier now, but its radius is limited by aerodynamics tech.
-Launch clamps are ignored in payload scanning now.
-Payload scanning doesn't follow surface attachment to the parent part anymore.
-Improved interstage fairing shape when its top is inside payload.
-Added base cone angle limit to make fairings look better.
-Part descriptions and readme text copy edited by Duxwing.

2.4.3
-Improved payload scanning for interstage adapter.
-Recompiled for KSP 0.23.

2.4.2
-Zero-radius payload is now used when no payload attached; fairings therefore will always reshape.
-Added parts to the tech tree.
-Moved fuselage shrouds to Structural tab.
-Changing adapter attachment node size with radius.

2.4.1
-Disabled fuel crossfeed on the interstage adapter because enabling it confuses Engineer Redux.
-So added stock decoupler module to the interstage adapter's topmost node as to aid delta-v calculations.
-Improved fairing shape for interstage adapter when fairing top is inside payload.

2.4
-Added procedural interstage fairing adapter with adjustable radii and height which decouples from the top part when fairings are ejected.
-Added conic fuselage.
-Fixed another inline fairing shape bug.

2.3
-Changed fuselage texture to distinguish it from fairings.
-Fairing shape can be locked: mouse over the side fairing/fuselage and press L.
-Reduced side nodes size for smaller base rings and 0.625m fairing base (for easier placement).
-Fixed inline fairings making a top cone when there should be just a cylinder.

2.2
-Added experimental egg-shaped fuselage (a side fairing without a decoupler).
-Moved fairing decoupler code to separate PartModule.
-Auto-struts are now also created between the top inline base and side fairings. If your payload is wobbly, then the sides might wobble.
-Fixed bug with misplaced fairings on new ring bases.

2.1
-Added low-profile fairing bases (base rings), intended for inline fairings. All of them have 4 side fairing attachment points.
-Replaced base model with one that looks more lightweight. It has the same size etc., so it won't break your existing ships.
-Fuel crossfeed for a fairing base can be toggled by mousing over it and pressing G in the editor or using the right-click menu in flight.
-Auto-struts between side fairings can now be disabled by mousing over the base and pressing T.
-Fixed inline fairings' not connecting with the top base sometimes.
-Fixed nested inline fairings' not connecting to the proper base.
-Fairing outline (blue lines) is not displayed now for inline fairings if sides are attached to any two bases.

2.0
-Inline truncated fairings are now created between two bases (one must be flipped). It won't work properly for off-center bases. If you want it off-center, tell me what for and how it should look.
-You can now change ejection force by pressing F when mouse is over the side fairing.
-Fixed rapid unplanned disassembly of side fairings when going out of time warp sometimes.

1.3
-Fixed ejection direction bug - it shouldn't matter how you place fairings now.

1.2
-Added invisible automatically placed struts between side fairings to mostly eliminate wobble.
-Replaced ejectionNoseDv with ejectionTorque so that all ejected fairings have the same motion, regardless of shape.
-Improved payload scanning for better fitting of mesh and box colliders.
-You can now adjust radius by moving the mouse over the base part while holding R (the default key, can be changed in part .cfg).
-Fixed "recursion" bug which caused misplaced fairings to grow out of control. (It's also a foundation for future inline fairings).
-Using a (hopefully) better method to offset side fairing center of mass.
-Using proportionally smaller part of texture for 1/3 (and smaller) side fairings to reduce texture stretching.
-Renamed "capsule-shaped" fairings to "egg-shaped" to be more Kerbal.

1.1
-Fix for future FAR compatibility (requires fixed FAR version)
-So lessened rotation on eject as to reduce collisions with payload and lower stages.
-Conic side fairings added. Original ones are made more capsule-shaped.

1.0
-Initial release.
