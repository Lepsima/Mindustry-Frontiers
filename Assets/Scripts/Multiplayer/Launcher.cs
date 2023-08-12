using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Frontiers.Content;
using Frontiers.Content.Maps;
using Frontiers.Content.Upgrades;
using Frontiers.Assets;
using Frontiers.Teams;

public class Launcher : MonoBehaviourPunCallbacks {
    const string VERSION = "v0.2d";
    public static Launcher Instance;
    private static Dictionary<string, RoomInfo> cachedRoomList = new();

    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] TMP_Text stateText;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text roomNameText;

    [SerializeField] Color localPlayerColor;

    [SerializeField] Transform roomListContent;
    [SerializeField] Transform playerListContentTeam1;
    [SerializeField] Transform playerListContentTeam2;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] GameObject playerListItemPrefab;
    [SerializeField] GameObject startGameButton;

    private static string state = "";

    private void Awake() {
        Instance = this;
        PhotonNetwork.GameVersion = VERSION;
    }

    private void Start() {
        Directories.RegenerateFolders();
        AssetLoader.LoadAssets();
        ContentLoader.LoadContents();
        MapDisplayer.SetupAtlas();

        SetState("Connecting To Master...");
        PhotonNetwork.ConnectUsingSettings();

        GameObject discordGameObject = new("Discord rich presence", typeof(DiscordController));
        DontDestroyOnLoad(discordGameObject);

        DiscordActivities.SetState(DiscordActivities.State.MainMenu);
    }

    public static void SetState(string value) {
        state = value;
        if (Instance) Instance.SetStateText(value);
    }

    public string GetState() {
        return state;
    }

    public int GetRoomCount() {
        return cachedRoomList.Count;
    }

    private void SetStateText(string value) {
        stateText.text = value;
    }

    public void SetNickName(string name) {
        if (string.IsNullOrEmpty(name)) return;
        PhotonNetwork.NickName = name;
    }

    public override void OnConnectedToMaster() {
        SetState("Connected To Master And Joining Lobby...");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.NickName = "Player " + PhotonNetwork.CountOfPlayers.ToString("000");
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby() {
        SetState("Joined Lobby");
        MenuManager.Instance.OpenMenu("TitleMenu");
    }

    public void CreateRoom() {
        if (string.IsNullOrEmpty(roomNameInputField.text)) return;

        SetState("Creating Room...");
        PhotonNetwork.CreateRoom(roomNameInputField.text);
        MenuManager.Instance.OpenMenu("LoadingMenu");
    }

    public void OnEnterRoomCreationMenu() {
        SetState("Room Creation Menu");
        DiscordActivities.SetState(DiscordActivities.State.CreatingRoom);
    }

    public void OnEnterRoomSearchingMenu() {
        SetState("Room Search Menu");
        DiscordActivities.SetState(DiscordActivities.State.SearchingRoom);
    }

    public void OnEnterMainMenu() {
        SetState("Joined Lobby");
        DiscordActivities.SetState(DiscordActivities.State.MainMenu);
    }

    public override void OnJoinedRoom() {
        SetState("Joined Room");
        DiscordActivities.SetState(DiscordActivities.State.InRoom);

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        MenuManager.Instance.OpenMenu("RoomMenu");

        SetPlayerListContent(playerListContentTeam1, TeamUtilities.TryGetTeamMembers(1));
        SetPlayerListContent(playerListContentTeam2, TeamUtilities.TryGetTeamMembers(2));

        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    void SetPlayerListContent(Transform contentList, Player[] players) {
        foreach (Transform transform in contentList) Destroy(transform.gameObject);

        for (int i = 0; i < players.Length; i++) {
            Player player = players[i];

            PlayerListItem item = Instantiate(playerListItemPrefab, contentList).GetComponent<PlayerListItem>();
            item.SetUp(player);

            if (player.NickName == PhotonNetwork.NickName) item.SetColor(localPlayerColor);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) {
        if (!changedProps.ContainsKey("_pt")) return;

        SetPlayerListContent(playerListContentTeam1, TeamUtilities.TryGetTeamMembers(1));
        SetPlayerListContent(playerListContentTeam2, TeamUtilities.TryGetTeamMembers(2));
    }

    public override void OnMasterClientSwitched(Player newMasterClient) {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnCreateRoomFailed(short returnCode, string message) {
        SetState("Room Creation Has Failed");
        errorText.text = "Room Creation Failed: " + message;
        MenuManager.Instance.OpenMenu("ErrorMenu");
    }

    public void StartGame() {
        PhotonNetwork.LoadLevel(1);
    }

    public void LeaveRoom() {
        SetState("Leaving room...");
        PhotonNetwork.LeaveRoom();
        MenuManager.Instance.OpenMenu("LoadingMenu");
    }

    public void JoinRoom(RoomInfo info) {
        SetState("Joining Room...");
        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.Instance.OpenMenu("LoadingMenu");
    }

    public override void OnLeftRoom() {
        SetState("Left Room");
        MenuManager.Instance.OpenMenu("TitleMenu");
        cachedRoomList.Clear();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList) {
        foreach (Transform transform in roomListContent) Destroy(transform.gameObject);

        for (int i = 0; i < roomList.Count; i++) {
            RoomInfo info = roomList[i];

            if (info.RemovedFromList) cachedRoomList.Remove(info.Name);
            else cachedRoomList[info.Name] = info;

            foreach (KeyValuePair<string, RoomInfo> entry in cachedRoomList) {
                Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(cachedRoomList[entry.Key]);
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        PlayerListItem item = Instantiate(playerListItemPrefab, playerListContentTeam1).GetComponent<PlayerListItem>();
        item.SetUp(newPlayer);

        if (newPlayer.NickName == PhotonNetwork.NickName) item.SetColor(localPlayerColor);
    }
}