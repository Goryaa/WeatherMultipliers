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
            harmony.PatchAll(typeof(Config));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
