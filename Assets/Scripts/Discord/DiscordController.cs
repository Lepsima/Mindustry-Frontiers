using UnityEngine;
using Discord;
using System.Diagnostics;

public class DiscordController : MonoBehaviour {
    public static long startTime;
    public Discord.Discord discord;

    void Awake() {
        if (Process.GetProcessesByName("Discord").Length <= 0) {
            Destroy(gameObject);
            return;
        } 

        discord = new Discord.Discord(1139523769984110635L, (ulong)CreateFlags.NoRequireDiscord);
        startTime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    void Update() {
        try {
            discord.RunCallbacks();
        } catch {   
            Destroy(gameObject);
        }
    }

    private void LateUpdate() {
        try {
            var activityManager = discord.GetActivityManager();
            var activity = DiscordActivities.GetActivity();

            activityManager.UpdateActivity(activity, (res) => {
                if (res != Result.Ok) UnityEngine.Debug.LogWarning("Failed connecting to Discord!");
            });

        } catch {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit() {
        if (!Application.isEditor) discord.Dispose();
    }
}