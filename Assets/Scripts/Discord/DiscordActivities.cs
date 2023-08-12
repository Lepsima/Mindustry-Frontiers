using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Discord;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Frontiers.Teams;

public class DiscordActivities {
    public enum State {
        MainMenu,
        SearchingRoom,
        CreatingRoom,
        InRoom,
        InGame
    }

    public static State gameState = State.MainMenu;

    public static void SetState(State gameState) {
        DiscordActivities.gameState = gameState;
    }

    public static Activity GetActivity() {
        bool inEditor = Application.isEditor;
        bool isPrivate = false;

        string largeText = "Mindustry Frontiers";

        string details = "";
        string state = "";
        string largeImage = "main-logo";

        if (!inEditor) {
            switch (gameState) {
                case State.MainMenu:
                    details = "In main menu";
                    break;

                case State.SearchingRoom:
                    details = "Searching rooms";
                    state = Launcher.Instance.GetRoomCount() + " Listed rooms";
                    break;

                case State.CreatingRoom:
                    details = "Creating room";
                    break;

                case State.InRoom:
                    details = "In room" + (isPrivate ? "" : ": " + PhotonNetwork.CurrentRoom.Name);
                    state = "Sandbox";

                    break;

                case State.InGame:
                    details = "In game" + (isPrivate ? "" : ": " + TryGetMapName());
                    state = "Sandbox";
                    break;
            }

            return new() {
                Details = details,
                State = state,

                Assets = {
                LargeImage = largeImage,
                LargeText = largeText,
            },

                Timestamps = {
                Start = DiscordController.startTime,
            }
            };

        } else {
            return new() {
                Details = "In Editor",

                Assets = {
                    LargeImage = largeImage,
                    LargeText = largeText,
                    SmallImage = "unity-logo",
                    SmallText = "In Editor"
                },
            };
        }
    }

    private static string TryGetMapName() {
        if (MapManager.Map != null) return MapManager.Map.name;
        return "Loading...";
    }
}