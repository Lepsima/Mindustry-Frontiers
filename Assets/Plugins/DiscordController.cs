using UnityEngine;
using Discord;

public class DiscordController : MonoBehaviour {
    public static long startTime;
    public Discord.Discord discord;

    void Awake() {
        discord = new Discord.Discord(1139523769984110635, (ulong)Discord.CreateFlags.NoRequireDiscord);
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
            var activity = DiscordAcitvities.GetActivity();

            activityManager.UpdateActivity(activity, (res) => {
                if (res != Discord.Result.Ok) Debug.LogWarning("Failed connecting to Discord!");
            });


        } catch {
            Destroy(gameObject);
        }
    }
}