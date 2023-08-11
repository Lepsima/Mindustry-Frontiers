using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Discord;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Frontiers.Teams;

public class DiscordAcitvities {
    public enum State {
        MainMenu,
        SearchingRoom,
        CreatingRoom,
        InRoom,
        InGame
    }

    public static State gameState = State.MainMenu;

    public static void SetState(State gameState) {
        DiscordAcitvities.gameState = gameState;
    }

    public static Activity GetActivity() {
        bool inEditor = Application.isEditor;
        bool isPrivate = false;

        string largeText = "Mindustry Frontiers";
        largeText += inEditor ? " | Editor" : "";

        string details = null;
        string state = null;
        string largeImage = "main-logo";

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
                state = GetPlayerCountVsString();

                break;

            case State.InGame:
                details = "In game" + (isPrivate ? "" : ": " + TryGetMapName());
                state = GetPlayerCountVsString();
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
    }

    private static string GetPlayerCountVsString() {
        return RoomManager.Instance.shardedTeamPlayers + " vs " + RoomManager.Instance.cruxTeamPlayers;
    }

    private static string TryGetMapName() {
        if (MapManager.Map != null) return MapManager.Map.name;
        return "Loading...";
    }
}