using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using Newtonsoft.Json;

namespace CustomCharacters
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("UNBEATABLE.exe")]
    public class CustomCharacters : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "net.zachava.customcharacters";
        public const string PLUGIN_NAME = "Custom Characters";
        public const string PLUGIN_VERSION = "1.0.0";
        internal static new ManualLogSource Logger;
        public static float playerRestOffset = 0f;
        public static float playerAttackOffset = 0f;
        public static float playerHoldOffset = 0f;
        public static float assistRestOffset = 0f;
        public static float assistAttackOffset = 0f;
        public static float assistHoldOffset = 0f;
        public static List<CustomCharacterInfo> customCharactersInfo = new List<CustomCharacterInfo>();
        public static List<string> customCharacterNames = new List<string>();
        public static bool usingCustomCharacter = false;
        public static void ResetPlayerOffsets()
        {
            playerRestOffset = 0f;
            playerAttackOffset = 0f;
            playerHoldOffset = 0f;
        }
        public static void ResetAssistOffsets()
        {
            assistRestOffset = 0f;
            assistAttackOffset = 0f;
            assistHoldOffset = 0f;
        }

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");

            string customCharactersFolderPath = Path.Combine(Paths.PluginPath, "CustomCharacters");
            string[] jsonFiles = Directory.GetFiles(customCharactersFolderPath, "*.json", SearchOption.AllDirectories);
            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFile);
                    List<CustomCharacterInfo> characterInfo = JsonConvert.DeserializeObject<List<CustomCharacterInfo>>(jsonContent);
                    if (characterInfo != null)
                    {
                        foreach (var info in characterInfo)
                        {
                            if (customCharacterNames.Contains(info.name))
                            {
                                string appendString = $"_dup{customCharacterNames.Count(n => n.StartsWith(info.name)) + 1}";
                                Logger.LogWarning($"Duplicate custom character name '{info.name}' found in {jsonFile}. Renaming this entry to {info.name + appendString}.");
                                info.name += appendString;
                            }
                            customCharactersInfo.Add(info);
                            customCharacterNames.Add(info.name);
                        }
                        Logger.LogInfo($"Loaded custom character info from {jsonFile}");
                    }
                    else
                    {
                        Logger.LogWarning($"Failed to deserialize custom character info from {jsonFile}");
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"Error reading custom character file {jsonFile}: {ex}");
                }
            }
            
            var harmony = new Harmony(PLUGIN_GUID);
			harmony.PatchAll();
        }
    }
}