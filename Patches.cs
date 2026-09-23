using System.Collections.Generic;
using System.Linq;
using Arcade.Unlockables;
using Arcade.UI.YourProfile;
using BepInEx;
using HarmonyLib;
using Rhythm;
using UnityEngine;
using System.IO;
using System.Numerics;

namespace CustomCharacters
{
    [HarmonyPatch(typeof(CharacterIndex), "AddCharacters")]
    class CharacterIndexAddCharactersPatch
    {
        static bool Prefix(ref List<CharacterIndex.Character> newCharacters)
		{
            CharacterIndex.Character treble = (from c in newCharacters
                where c.name == "Treble"
                select c).FirstOrDefault();
            
            // if treble is null, something went wrong or the game is adding DLC characters
            if (treble == null) return true;

            List<CharacterIndex.Character> customCharactersToAdd = new List<CharacterIndex.Character>();
            foreach (var customChar in CustomCharacters.customCharactersInfo)
            {
                CharacterIndex.Character customCharacter = new CharacterIndex.Character
                {
                    name = customChar.name,
                    prefab = treble.prefab,
                    assistPrefab = treble.assistPrefab,
                    bgPrefab = null
                };
                customCharactersToAdd.Add(customCharacter);
            }
            newCharacters.AddRange(customCharactersToAdd);
			return true;
		}
    }

    // Oops, patching that wasn't needed and also broke intro animations for custom characters
    // [HarmonyPatch(typeof(RhythmPlayerAnimator), "Start")]
    // class RhythmPlayerAnimatorStartPatch
    // {
    //     static void Prefix(RhythmPlayerAnimator __instance)
    //     {
    //         if (CustomCharacters.usingCustomCharacter && __instance.Default.IntroDuration > 0f)
    //         {
    //             __instance.Intro(RhythmPlayer.ActionStates.DEFAULT);
    //             return;
    //         }
    //     }
    // }

    [HarmonyPatch(typeof(RhythmCharacterSelector), "InstantiatePlayer")]
    class RhythmCharacterSelectorInstantiatePlayerPatch
    {
        static void Postfix(RhythmCharacterSelector __instance, string character)
        {
            AssetBundle bundle = null;
            try
            {
                CustomCharacters.Logger.LogInfo($"InstantiatePlayer character='{character}'");
                if (!CustomCharacters.customCharacterNames.Contains(character))
                {
                    CustomCharacters.ResetPlayerOffsets();
                    CustomCharacters.Logger.LogInfo("[InstantiatePlayer Postfix] completed OK (not a custom character)");
                    CustomCharacters.usingCustomCharacter = false;
                    return;
                }

                CustomCharacters.usingCustomCharacter = true;

                CustomCharacterInfo customCharacter = CustomCharacters.customCharactersInfo
                    .FirstOrDefault(c => c.name == character);

                GameObject playerGO = __instance.player.currentAnimator.gameObject;
                Animator animator = playerGO.GetComponent<Animator>();
                RhythmPlayerAnimator rpa = playerGO.GetComponent<RhythmPlayerAnimator>();
                RuntimeAnimatorController controller = null;

                try
                {
                    bundle = AssetBundle.LoadFromFile(Path.Combine(Paths.PluginPath, "CustomCharacters", customCharacter.assetBundleName));
                    controller = bundle.LoadAsset<RuntimeAnimatorController>(customCharacter.animatorControllerName);
                    if (controller == null)
                    {
                        throw new System.ArgumentException($"Animator controller '{customCharacter.animatorControllerName}' not found in asset bundle '{customCharacter.assetBundleName}'.");
                    }
                }
                catch (System.Exception ex)
                {
                    CustomCharacters.Logger.LogError($"Error loading asset bundle or animator controller: {ex}");
                    throw;
                }

                animator.runtimeAnimatorController = controller;

                if (customCharacter.outlineWidth.HasValue)
                {
                    playerGO.GetComponent<Renderer>().material.SetFloat("_OutlineWidth", customCharacter.outlineWidth.Value);
                }

                UnityEngine.Vector4 outlineColor = playerGO.GetComponent<Renderer>().material.GetVector("_OutlineColor");

                if (customCharacter.outlineColorR.HasValue ||
                    customCharacter.outlineColorG.HasValue ||
                    customCharacter.outlineColorB.HasValue)
                {
                    outlineColor.x = customCharacter.outlineColorR ?? 0f;
                    outlineColor.y = customCharacter.outlineColorG ?? 0f;
                    outlineColor.z = customCharacter.outlineColorB ?? 0f;
                }

                outlineColor.w = customCharacter.outlineColorA ?? outlineColor.w;

                playerGO.GetComponent<Renderer>().material.SetVector("_OutlineColor", outlineColor);

                rpa.Default = customCharacter.defaultActionStates;
                rpa.Brawl = customCharacter.brawlActionStates;
                // These ones aren't used in Arcade as far as I know, but why not support them
                rpa.Running = customCharacter.runningActionStates;
                rpa.Falling = customCharacter.fallingActionStates;

                // Rebuild caches
                rpa.Awake();

                CustomCharacters.playerRestOffset = customCharacter.restOffset ?? 0f;
                CustomCharacters.playerAttackOffset = customCharacter.attackOffset ?? 0f;
                CustomCharacters.playerHoldOffset = customCharacter.holdOffset ?? 0f;

                CustomCharacters.Logger.LogInfo("[InstantiatePlayer Postfix] completed OK");
            }
            catch (System.Exception ex)
            {
                CustomCharacters.Logger.LogError($"Error in InstantiatePlayer patch: {ex}");
                CustomCharacters.Logger.LogError($"Falling back to spawning Beat as player character.");
                __instance.InstantiatePlayer("Beat");
            }
            finally
            {
                bundle?.Unload(false);
                bundle = null;
            }
        }
    }

    [HarmonyPatch(typeof(RhythmCharacterSelector), "InstantiateAssist")]
    class RhythmCharacterSelectorInstantiateAssistPatch
    {
        static void Postfix(RhythmCharacterSelector __instance, string character)
        {
            AssetBundle bundle = null;
            try
            {
                CustomCharacters.Logger.LogInfo($"InstantiateAssist character='{character}'");
                if (!CustomCharacters.customCharacterNames.Contains(character))
                {
                    CustomCharacters.ResetAssistOffsets();
                    CustomCharacters.Logger.LogInfo("[InstantiateAssist Postfix] completed OK (not a custom character)");
                    return;
                }

                CustomCharacterInfo customCharacter = CustomCharacters.customCharactersInfo
                    .FirstOrDefault(c => c.name == character);

                GameObject assistGO = __instance.assist.currentAnimator.gameObject;
                Animator animator = assistGO.GetComponent<Animator>();
                RhythmAssistAnimator raa = assistGO.GetComponent<RhythmAssistAnimator>();
                RuntimeAnimatorController controller = null;

                try
                {
                    bundle = AssetBundle.LoadFromFile(Path.Combine(Paths.PluginPath, "CustomCharacters", customCharacter.assetBundleName));
                    controller = bundle.LoadAsset<RuntimeAnimatorController>(customCharacter.animatorControllerName);
                    if (controller == null)
                    {
                        throw new System.ArgumentException($"Animator controller '{customCharacter.animatorControllerName}' not found in asset bundle '{customCharacter.assetBundleName}'.");
                    }
                }
                catch (System.Exception ex)
                {
                    CustomCharacters.Logger.LogError($"Error loading asset bundle or animator controller: {ex}");
                    throw;
                }

                animator.runtimeAnimatorController = controller;

                if (customCharacter.outlineWidth.HasValue)
                {
                    assistGO.GetComponent<Renderer>().material.SetFloat("_OutlineWidth", customCharacter.outlineWidth.Value);
                }

                UnityEngine.Vector4 outlineColor = assistGO.GetComponent<Renderer>().material.GetVector("_OutlineColor");

                //DEBUG
                CustomCharacters.Logger.LogInfo($"Current outline color: {outlineColor}");

                if (customCharacter.outlineColorR.HasValue ||
                    customCharacter.outlineColorG.HasValue ||
                    customCharacter.outlineColorB.HasValue)
                {
                    outlineColor.x = customCharacter.outlineColorR ?? 0f;
                    outlineColor.y = customCharacter.outlineColorG ?? 0f;
                    outlineColor.z = customCharacter.outlineColorB ?? 0f;
                }

                outlineColor.w = customCharacter.outlineColorA ?? outlineColor.w;

                assistGO.GetComponent<Renderer>().material.SetVector("_OutlineColor", outlineColor);

                var defaultActionStates = raa.Default;
                defaultActionStates.AirBlock = customCharacter.defaultActionStates.AirBlock;
                defaultActionStates.GroundBlock = customCharacter.defaultActionStates.GroundBlock;
                defaultActionStates.HighAttacks = customCharacter.defaultActionStates.HighAttacks;
                defaultActionStates.LowAttacks = customCharacter.defaultActionStates.LowAttacks;
                raa.Default = defaultActionStates;

                // Rebuild caches
                raa.Awake();

                CustomCharacters.assistRestOffset = customCharacter.restOffset ?? 0f;
                CustomCharacters.assistAttackOffset = customCharacter.attackOffset ?? 0f;
                CustomCharacters.assistHoldOffset = customCharacter.holdOffset ?? 0f;

                CustomCharacters.Logger.LogInfo("[InstantiateAssist Postfix] completed OK");
            }
            catch (System.Exception ex)
            {
                CustomCharacters.Logger.LogError($"Error in InstantiateAssist patch: {ex}");
                CustomCharacters.Logger.LogError($"Falling back to spawning Quaver as assist character.");
                __instance.InstantiateAssist("Quaver");
            }
            finally
            {
                bundle?.Unload(false);
                bundle = null;
            }
        }
    }

    [HarmonyPatch(typeof(CharacterDropdown), "UpdatePreview")]
    class CharacterDropdownUpdatePreviewPatch
    {
        static bool Prefix(CharacterDropdown __instance, int index)
        {
            string character = __instance.dropdown.options[index].text;
            if (!CustomCharacters.customCharacterNames.Contains(character))
            {
                //CustomCharacters.Logger.LogInfo($"UpdatePreview character='{character}' (not a custom character)");
                return true;
            }

            AssetBundle bundle = null;
            try
            {                
                CustomCharacterInfo customCharacter = CustomCharacters.customCharactersInfo
                        .FirstOrDefault(c => c.name == character);

                RuntimeAnimatorController controller = null;
                try
                {
                    bundle = AssetBundle.LoadFromFile(Path.Combine(Paths.PluginPath, "CustomCharacters", customCharacter.assetBundleName));
                    controller = bundle.LoadAsset<RuntimeAnimatorController>(customCharacter.animatorControllerName);
                    if (controller == null)
                    {
                        throw new System.ArgumentException($"Animator controller '{customCharacter.animatorControllerName}' not found in asset bundle '{customCharacter.assetBundleName}'.");
                    }
                }
                catch (System.Exception ex)
                {
                    CustomCharacters.Logger.LogError($"Error loading asset bundle or animator controller: {ex}");
                    throw;
                }

                foreach (Animator animator in __instance.previewAnimators)
				{
					animator.runtimeAnimatorController = controller;
                    //CustomCharacters.Logger.LogInfo($"UpdatePreview idle animation='{customCharacter.defaultActionStates.Idle}'");
					__instance.StartCoroutine(__instance.SetAnimatorState(animator, customCharacter.defaultActionStates.Idle));
				}
            }
            catch (System.Exception ex)
            {
                CustomCharacters.Logger.LogError($"Error in UpdatePreview patch: {ex}");
            }
            finally
            {
                bundle?.Unload(false);
                bundle = null;
            }
            return false;
        }
    }


    [HarmonyPatch(typeof(RhythmBaseCharacter), "rhythmRestPoint", MethodType.Getter)]
    class RhythmBaseCharacterGetRhythmRestPointPatch
    {
        static void Postfix(RhythmBaseCharacter __instance, ref UnityEngine.Vector3 __result)
        {
            if (__instance is RhythmPlayer)
            {
                __result += __instance.side.Pick(UnityEngine.Vector3.left, UnityEngine.Vector3.right) * CustomCharacters.playerRestOffset;
            }
            else if (__instance is RhythmAssist)
            {
                __result += __instance.side.Pick(UnityEngine.Vector3.left, UnityEngine.Vector3.right) * CustomCharacters.assistRestOffset;
            }
        }
    }

    [HarmonyPatch(typeof(RhythmBaseCharacter), "attackPoint", MethodType.Getter)]
    class RhythmBaseCharacterGetAttackPointPatch
    {
        static void Postfix(RhythmBaseCharacter __instance, ref UnityEngine.Vector3 __result)
        {
            if (__instance is RhythmPlayer)
            {
                __result += __instance.side.Pick(UnityEngine.Vector3.left, UnityEngine.Vector3.right) * CustomCharacters.playerAttackOffset;
            }
            else if (__instance is RhythmAssist)
            {
                __result += __instance.side.Pick(UnityEngine.Vector3.left, UnityEngine.Vector3.right) * CustomCharacters.assistAttackOffset;
            }
        }
    }
        
    [HarmonyPatch(typeof(RhythmBaseCharacter), "holdPoint", MethodType.Getter)]
    class RhythmBaseCharacterGetHoldPointPatch
    {
        static void Postfix(RhythmBaseCharacter __instance, ref UnityEngine.Vector3 __result)
        {
            if (__instance is RhythmPlayer)
            {
                __result += __instance.side.Pick(UnityEngine.Vector3.left, UnityEngine.Vector3.right) * CustomCharacters.playerHoldOffset;
            }
            else if (__instance is RhythmAssist)
            {
                __result += __instance.side.Pick(UnityEngine.Vector3.left, UnityEngine.Vector3.right) * CustomCharacters.assistHoldOffset;
            }
        }
    }
}