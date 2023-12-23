using BepInEx.Logging;
using HarmonyLib;

namespace WeatherMultipliers.patches;

[HarmonyPatch(typeof(RoundManager), "SpawnScrapInLevel")]
public class ScrapGeneration
{
    private static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("WeatherMultipliers.ScrapGeneration");

    private static void Prefix(RoundManager __instance)
    {
        // Temporarily increase RoundManager scrapValueMultiplier based on rounds current weather
        LevelWeatherType weather = __instance.currentLevel.currentWeather;
        if (Config.Instance.ValueMultipliers.ContainsKey(weather))
        {
            float multiplier = Config.Instance.ValueMultipliers[__instance.currentLevel.currentWeather].Value;
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
        if (Config.Instance.ValueMultipliers.ContainsKey(weather))
        {
            float multiplier = Config.Instance.ValueMultipliers[__instance.currentLevel.currentWeather].Value;
            __instance.scrapValueMultiplier /= multiplier;
            logger.LogInfo($"Scrap generated, resetting scrap value multiplier to its original value of {__instance.scrapValueMultiplier}");
        }
    }
}