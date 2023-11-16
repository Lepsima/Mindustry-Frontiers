using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Discord;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Frontiers.Teams;
using System;

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
        bool inEditor = Application.isEditor && !DiscordController.buildStatus;
        bool isPrivate = false;

        string largeText = Application.isEditor ? "Unity Editor" : Launcher.VERSION;

        string details = "";
        string state = "";
        string largeImage = "main-logo";

        if (!inEditor) {
            switch (gameState) {
                case State.MainMenu:
                    details = Launcher.VERSION; 
                    state = "In Multiplayer Menu";
                    break;

                case State.SearchingRoom:
                    details = Launcher.Instance.GetRoomCount() + " Listed rooms";
                    state = "Searching rooms";
                    break;

                case State.CreatingRoom:
                    state = "Creating room";
                    break;

                case State.InRoom:
                    details = "Sandbox";
                    state = isPrivate ? "-Private Room-" : PhotonNetwork.CurrentRoom == null ? "-Room Name Not Found-" : PhotonNetwork.CurrentRoom.Name;

                    break;

                case State.InGame:
                    details = "Sandbox";
                    state = isPrivate ? "-Private Room-" : TryGetMapName();
                    break;
            }

 

            Activity act = new() {
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

            ActivityParty party = DiscordController.GetParty(out bool exists);
            if (exists) {
                act.Party = party;
                act.Secrets = new ActivitySecrets() {
                    Join = party.Id + "-join",
                    Spectate = party.Id + "-spectate",
                    Match = party.Id + "-match",
                };
            }

            return act;

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