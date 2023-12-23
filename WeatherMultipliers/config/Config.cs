using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace WeatherMultipliers;

[Serializable]
public class Config : SyncedInstance<Config>
{
    public Dictionary<LevelWeatherType, ConfigEntry<float>> ValueMultipliers = new();

    private static readonly Dictionary<LevelWeatherType, float> defaultValueMultipliers = new() {
            {LevelWeatherType.Rainy, 1.1f},
            {LevelWeatherType.Stormy, 1.35f},
            {LevelWeatherType.Foggy, 1.25f},
            {LevelWeatherType.Flooded, 1.35f},
            {LevelWeatherType.Eclipsed, 1.70f},
        };

    public Config(ConfigFile cfg)
    {
        InitInstance(this);
        foreach (KeyValuePair<LevelWeatherType, float> entry in defaultValueMultipliers)
        {
            ValueMultipliers[entry.Key] = cfg.Bind(
                "Multipliers",
                entry.Key.ToString(),
                Mathf.Clamp(entry.Value, 1, 1000),
                $"Scrap value multiplier for {entry.Key} weather"
                );
        }
    }

    public static void RequestSync()
    {
        if (!IsClient) return;

        using FastBufferWriter stream = new(IntSize, Allocator.Temp);
        MessageManager.SendNamedMessage($"{PluginInfo.PLUGIN_GUID}_OnRequestConfigSync", 0uL, stream);
    }

    public static void OnRequestSync(ulong clientId, FastBufferReader _)
    {
        if (!IsHost) return;

        Plugin.Logger.LogInfo($"Config sync request received from client: {clientId}");

        byte[] array = SerializeToBytes(Instance);
        int value = array.Length;

        using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

        try
        {
            stream.WriteValueSafe(in value, default);
            stream.WriteBytesSafe(array);

            MessageManager.SendNamedMessage($"{PluginInfo.PLUGIN_GUID}_OnReceiveConfigSync", clientId, stream);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
        }
    }

    public static void OnReceiveSync(ulong _, FastBufferReader reader)
    {
        if (!reader.TryBeginRead(IntSize))
        {
            Plugin.Logger.LogError("Config sync error: Could not begin reading buffer.");
            return;
        }

        reader.ReadValueSafe(out int val, default);
        if (!reader.TryBeginRead(val))
        {
            Plugin.Logger.LogError("Config sync error: Host could not sync.");
            return;
        }

        byte[] data = new byte[val];
        reader.ReadBytesSafe(ref data, val);

        SyncInstance(data);

        Plugin.Logger.LogInfo("Successfully synced config with host.");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    public static void InitializeLocalPlayer()
    {
        if (IsHost)
        {
            MessageManager.RegisterNamedMessageHandler("ModName_OnRequestConfigSync", OnRequestSync);
            Synced = true;

            return;
        }

        Synced = false;
        MessageManager.RegisterNamedMessageHandler("ModName_OnReceiveConfigSync", OnReceiveSync);
        RequestSync();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
    public static void PlayerLeave()
    {
        RevertSync();
    }
}
