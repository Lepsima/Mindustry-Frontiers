using UnityEngine;
using Discord;
using System;
using System.Diagnostics;
using Photon.Pun;
using Photon.Realtime;

public class DiscordController : MonoBehaviour {
    public static long startTime;
    private static string partyID;
    public static bool buildStatus = true;
    public Discord.Discord discord;

    ActivityManager activityManager;

    void Awake() {
        if (Process.GetProcessesByName("Discord").Length <= 0) {
            Destroy(gameObject);
            return;
        } 

        discord = new Discord.Discord(1139523769984110635L, (ulong)CreateFlags.NoRequireDiscord);
        startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        activityManager = discord.GetActivityManager();
    }

    void Update() {
        try {
            discord.RunCallbacks();
        } catch {
            Clear();
            Destroy(gameObject);
        }
    }

    private void LateUpdate() {
        try {
            Activity activity = DiscordActivities.GetActivity();

            activityManager.UpdateActivity(activity, (res) => {
                if (res != Result.Ok) UnityEngine.Debug.LogWarning("Failed connecting to Discord!");
            });

        } catch {
            Clear();
            Destroy(gameObject);
        }
    }

    private void OnDestroy() {
        Clear();
    }

    private void OnApplicationQuit() {
        if ((!Application.isEditor || buildStatus)) Clear();
    }

    private void Clear() {
        if (discord == null) return;
        discord.Dispose();
        discord = null;
    }

    public static void SetParty(string id) {
        partyID = id;
    }

    public static ActivityParty GetParty(out bool exists) {
        Room room = PhotonNetwork.CurrentRoom;

        exists = partyID != null && room != null;
        if (!exists) return new();

        return new ActivityParty() {
            Size = new PartySize() {
                CurrentSize = room.Players.Count,
                MaxSize = room.MaxPlayers,
            },

            Id = partyID,
            Privacy = room.IsVisible ? ActivityPartyPrivacy.Public : ActivityPartyPrivacy.Private,
        };
    }

}