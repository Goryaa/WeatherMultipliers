using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace WeatherMultipliers
{
    [BepInPlugin("com.github", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Dictionary<LevelWeatherType, ConfigEntry<float>> ValueMultipliers = new();

        private readonly Harmony harmony = new("WeatherMultipliers");
        private static readonly Dictionary<LevelWeatherType, float> defaultValueMultipliers = new() {
            {LevelWeatherType.Rainy, 1.1f},
            {LevelWeatherType.Stormy, 1.35f},
            {LevelWeatherType.Foggy, 1.25f},
            {LevelWeatherType.Flooded, 1.35f},
            {LevelWeatherType.Eclipsed, 1.70f},
        };

        private void Awake()
        {
            foreach (KeyValuePair<LevelWeatherType, float> entry in defaultValueMultipliers)
            {
                ValueMultipliers[entry.Key] = Config.Bind(
                    "Multipliers",
                    entry.Key.ToString(),
                    Mathf.Clamp(entry.Value, 1, 1000),
                    $"Scrap value multiplier for {entry.Key} weather"
                    );
            }

            harmony.PatchAll();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}

namespace WeatherMultipliers.patches
{
    [HarmonyPatch(typeof(RoundManager), "SpawnScrapInLevel")]
    public class ApplyOnScrapGeneration
    {
        private static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("WeatherMultipliers.ApplyOnScrapGeneration");

        private static void Prefix(RoundManager __instance)
        {
            // Temporarily increase RoundManager scrapValueMultiplier based on rounds current weather
            LevelWeatherType weather = __instance.currentLevel.currentWeather;
            if (Plugin.ValueMultipliers.ContainsKey(weather))
            {
                float multiplier = Plugin.ValueMultipliers[__instance.currentLevel.currentWeather].Value;
                __instance.scrapValueMultiplier *= multiplier;
                logger.LogInfo($"Set scrap value multiplier ({multiplier}) for current weather \"{weather}\"");
            }
            else
            {
                logger.LogInfo($"No weather multiplier found for \"{weather}\"");
            }
        }

        private static void Postfix(RoundManager __instance)
        {
            // Reset RoundManager scrapValueMultiplier to original value
            LevelWeatherType weather = __instance.currentLevel.currentWeather;
            if (Plugin.ValueMultipliers.ContainsKey(weather))
            {
                float multiplier = Plugin.ValueMultipliers[__instance.currentLevel.currentWeather].Value;
                __instance.scrapValueMultiplier /= multiplier;
                logger.LogInfo($"Scrap generated, resetting scrap value multiplier to its original value of {__instance.scrapValueMultiplier}");
            }
        }
    }

    [HarmonyPatch(typeof(LungProp), "DisconnectFromMachinery")]
    public class ApplyLungPropMultiplier
    {
        private static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("WeatherMultipliers.ApplyLungPropMultiplier");

        private static void Prefix(LungProp __instance)
        {
            LevelWeatherType weather = __instance.roundManager.currentLevel.currentWeather;
            if (Plugin.ValueMultipliers.ContainsKey(weather))
            {
                float multiplier = Plugin.ValueMultipliers[weather].Value;
                __instance.scrapValue = (int)(multiplier * __instance.scrapValue);
                logger.LogInfo($"Adjusting LungProp (Apparatus) value for weather {weather}: {__instance.scrapValue}");
            }
        }
    }
}
