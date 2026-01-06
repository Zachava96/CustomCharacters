using System.Collections.Generic;
using System.Linq;
using Arcade.Unlockables;
using BepInEx;
using HarmonyLib;
using Rhythm;
using UnityEngine;
using System.IO;

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

    [HarmonyPatch(typeof(RhythmPlayerAnimator), "Start")]
    class RhythmPlayerAnimatorStartPatch
    {
        static void Prefix(RhythmPlayerAnimator __instance)
        {
            if (CustomCharacters.usingCustomCharacter && __instance.Default.IntroDuration > 0f)
            {
                __instance.Intro(RhythmPlayer.ActionStates.DEFAULT);
                return;
            }
        }
    }

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

                rpa.Default = customCharacter.defaultActionStates;
                rpa.Brawl = customCharacter.brawlActionStates;
                // These ones aren't used in Arcade as far as I know, but why not support them
                rpa.Running = customCharacter.runningActionStates;
                rpa.Falling = customCharacter.fallingActionStates;

                // Rebuild caches
                rpa.Awake();

                CustomCharacters.playerRestOffset = customCharacter.restOffset;
                CustomCharacters.playerAttackOffset = customCharacter.attackOffset;
                CustomCharacters.playerHoldOffset = customCharacter.holdOffset;

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

                var defaultActionStates = raa.Default;
                defaultActionStates.AirBlock = customCharacter.defaultActionStates.AirBlock;
                defaultActionStates.GroundBlock = customCharacter.defaultActionStates.GroundBlock;
                defaultActionStates.HighAttacks = customCharacter.defaultActionStates.HighAttacks;
                defaultActionStates.LowAttacks = customCharacter.defaultActionStates.LowAttacks;
                raa.Default = defaultActionStates;

                // Rebuild caches
                raa.Awake();

                CustomCharacters.assistRestOffset = customCharacter.restOffset;
                CustomCharacters.assistAttackOffset = customCharacter.attackOffset;
                CustomCharacters.assistHoldOffset = customCharacter.holdOffset;

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

    [HarmonyPatch(typeof(RhythmBaseCharacter), "rhythmRestPoint", MethodType.Getter)]
    class RhythmBaseCharacterGetRhythmRestPointPatch
    {
        static void Postfix(RhythmBaseCharacter __instance, ref Vector3 __result)
        {
            if (__instance is RhythmPlayer)
            {
                __result += __instance.side.Pick(Vector3.left, Vector3.right) * CustomCharacters.playerRestOffset;
            }
            else if (__instance is RhythmAssist)
            {
                __result += __instance.side.Pick(Vector3.left, Vector3.right) * CustomCharacters.assistRestOffset;
            }
        }
    }

    [HarmonyPatch(typeof(RhythmBaseCharacter), "attackPoint", MethodType.Getter)]
    class RhythmBaseCharacterGetAttackPointPatch
    {
        static void Postfix(RhythmBaseCharacter __instance, ref Vector3 __result)
        {
            if (__instance is RhythmPlayer)
            {
                __result += __instance.side.Pick(Vector3.left, Vector3.right) * CustomCharacters.playerAttackOffset;
            }
            else if (__instance is RhythmAssist)
            {
                __result += __instance.side.Pick(Vector3.left, Vector3.right) * CustomCharacters.assistAttackOffset;
            }
        }
    }
        
    [HarmonyPatch(typeof(RhythmBaseCharacter), "holdPoint", MethodType.Getter)]
    class RhythmBaseCharacterGetHoldPointPatch
    {
        static void Postfix(RhythmBaseCharacter __instance, ref Vector3 __result)
        {
            if (__instance is RhythmPlayer)
            {
                __result += __instance.side.Pick(Vector3.left, Vector3.right) * CustomCharacters.playerHoldOffset;
            }
            else if (__instance is RhythmAssist)
            {
                __result += __instance.side.Pick(Vector3.left, Vector3.right) * CustomCharacters.assistHoldOffset;
            }
        }
    }
}