# How to make a character:

You will need to use **Unity 2022.3.41f1** to make an asset bundle, and then provide a JSON file as described below. The asset bundle should have your animation controller, animation clips, and sprites/textures needed.

It may be helpful to examine how existing characters do their animations. Providing them is outside the scope of this mod, but **AssetRipper** should be able to help you get the assets from the game for reference. Look for the animator controllers that end with **"Combat Controller"** or **"CombatController."** Exporting them as an asset package and then making a new Unity project that imports them might be easier to work with.

In the base game characters, the swap side and landing animations tend to have a transition into the idle state. Consider doing so.

The pivot point of sprites should be located at the character's feet. A pivot point higher up will cause the character to move into the ground; a lower pivot point will make the character appear to float. Horizontally, it makes sense to pick a point of your character's body you want to keep in one place (like the torso) and put the pivot point there, so the character doesn't move in a way that seems strange.

---

## JSON Structure Reference

Do not use **null** in any field! Use empty strings or empty arrays.

The JSON structure should be relatively self-commenting, but in case you want a reference, here it is.

The JSON file should be an array of objects with one object per character. Even if you only have one character, make it an array of one.

- **name**: String. This is what you want your character to be called. If you make a duplicate of another custom character, the mod will automatically add `"_dup#"` to one, so please have distinct names. Also don't use base game names, I didn't test what that does but I bet it's nothing good.
- **assetBundleName**: String. The filename of the asset bundle to use. Include the folder name in it, such as `"CrestFolder/crestAssetBundle"`.
- **animatorControllerName**: String. The name of the animator controller in the asset bundle to use. You can have multiple animator controllers in an asset bundle, so long as they're named differently.
- **restOffset**, **attackOffset**, **holdOffset**: Float. I added these in case you need to shift your character forward or backward a little for animations. Negative is moved back towards the center of the screen, positive is moved out towards the edge.

---

### Action States

Action States are a set of animations to use in certain situations:

- **defaultActionStates**: This action state is the default for regular note mode.
- **brawlActionStates**: This is used if you're in brawl mode. If an animation doesn't exist in this mode, the game will use the default action state instead for that animation.
- **runningActionStates** and **fallingActionStates**: These are used in story mode as far as I can tell. You shouldn't need any animations defined in them, but keep them in the JSON.

---

### Animation Names

Now on to the actual animation names:

- **Intro**: String. If you have an animation that plays when the character spawns in, put it here. If IntroDuration is 0, this won't play.
- **IntroDuration**: Float. Set this to how long after starting the intro animation the character should switch to the idle animation.
- **AirBlock**: String. What animation to use when blocking in the top lane. This animation should loop.
- **GroundBlock**: String. What animation to use when blocking in the bottom lane. This animation should loop.
- **Idle**: String. What animation to use when the character is at rest. This animation should loop.
- **SideSwitch**: String. The animation that plays when the character switches sides.
- **HighAttacks**: Array of strings. Attacks that the character will use in the top lane. The character will cycle through them for subsequent hits. If array is empty in defaultActionStates, will cause errors!
- **LowAttacks**: Array of strings. Attacks that the character will use in the bottom lane. The character will cycle through them for subsequent hits. If array is empty in defaultActionStates, will cause errors!
- **Jumps**: Array of strings. Jump animations. The character will cycle through them for subsequent jumps. If array is empty in defaultActionStates, will cause errors!
- **Lands**: Array of strings. Landing animations. The character will cycle through them for subsequent landings. If array is empty in defaultActionStates, will cause errors!
- **Hurts**: Array of strings. Damage animations that play when you miss a note. The character will cycle through them for subsequent missed notes. If array is empty in defaultActionStates, will cause errors!
- **HasSlams**: Boolean. Sets if the character has special slam animations (when the character is airborne and then attacks low). Must be false if SlamAttacks is an empty array!
- **IgnoreSlams**: Boolean. As far as I can tell, unused.
- **SlamAttacks**: Array of strings. Special slam attack animations.

---

## Ending notes

If you don't have an animation that's required, just put in your idle animation or another animation that you want to play.

Also, I'm bad at Unity, so don't ask me how to do things. Like I said earlier, reference existing characters or look up tutorials.
