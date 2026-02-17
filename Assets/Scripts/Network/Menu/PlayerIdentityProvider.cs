using System;
using UnityEngine;

#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

public static class PlayerIdentityProvider
{
    private const string PersistentPlayerIdKey = "PersistentPlayerId";

    // Prefer Steam identity; otherwise return a device-local persistent id.
    public static string GetLocalPlayerId()
    {
        string steamId = TryGetSteamId();
        if (!string.IsNullOrWhiteSpace(steamId))
        {
            return $"steam:{steamId}";
        }

        if (PlayerPrefs.HasKey(PersistentPlayerIdKey))
        {
            return PlayerPrefs.GetString(PersistentPlayerIdKey);
        }

        string generatedId = $"local:{Guid.NewGuid():N}";
        PlayerPrefs.SetString(PersistentPlayerIdKey, generatedId);
        PlayerPrefs.Save();
        return generatedId;
    }

    // Steam lookup, returns empty when Steam is unavailable.
    private static string TryGetSteamId()
    {
#if !DISABLESTEAMWORKS
        try
        {
            if (!SteamManager.Initialized)
            {
                return string.Empty;
            }

            CSteamID steamId = SteamUser.GetSteamID();
            return steamId.m_SteamID.ToString();
        }
        catch (Exception)
        {
            return string.Empty;
        }
#else
        return string.Empty;
#endif
    }
}
