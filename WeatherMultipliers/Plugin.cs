using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace WeatherMultipliers
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static new Config Config { get; internal set; }
        internal static new ManualLogSource Logger { get; private set; }
        private readonly Harmony harmony = new(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            Config = new Config(base.Config);
            Logger = base.Logger;

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

    [HarmonyPatch(typeof(LungProp), "DisconnectFromMachinery")]
    public class ApplyLungPropMultiplier
    {
        private static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("WeatherMultipliers.ApplyLungPropMultiplier");

        private static void Prefix(LungProp __instance)
        {
            LevelWeatherType weather = __instance.roundManager.currentLevel.currentWeather;
            if (Config.Instance.ValueMultipliers.ContainsKey(weather))
            {
                float multiplier = Config.Instance.ValueMultipliers[weather].Value;
                __instance.scrapValue = (int)(multiplier * __instance.scrapValue);
                logger.LogInfo($"Adjusting LungProp (Apparatus) value for weather {weather}: {__instance.scrapValue}");
            }
        }
    }
}
