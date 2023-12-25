using BepInEx.Logging;
using HarmonyLib;

namespace WeatherMultipliers.patches;

[HarmonyPatch(typeof(LungProp), "DisconnectFromMachinery")]
public class ApparatusPatch
{
    private static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("WeatherMultipliers.ApparatusPatch");

    private static void Prefix(LungProp __instance)
    {
        LevelWeatherType weather = __instance.roundManager.currentLevel.currentWeather;
        if (Config.Instance.ValueMultipliers.ContainsKey(weather))
        {
            float multiplier = Config.Instance.ValueMultipliers[weather];
            __instance.scrapValue = (int)(multiplier * __instance.scrapValue);
            logger.LogInfo($"Adjusting LungProp (Apparatus) value for weather {weather}: {__instance.scrapValue}");
        }
    }
}
