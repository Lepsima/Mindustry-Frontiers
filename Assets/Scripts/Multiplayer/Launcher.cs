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
using Frontiers.Assets;
using Frontiers.Teams;

public class Launcher : MonoBehaviourPunCallbacks {
    const string VERSION = "v0.0.0.3";
    public static Launcher Instance;
    private static Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text roomNameText;

    [SerializeField] Color localPlayerColor;

    [SerializeField] Transform roomListContent;
    [SerializeField] Transform playerListContentTeam1;
    [SerializeField] Transform playerListContentTeam2;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] GameObject playerListItemPrefab;
    [SerializeField] GameObject startGameButton;

    private void Awake() {
        Instance = this;
        PhotonNetwork.GameVersion = VERSION;
    }

    private void Start() {
        AssetLoader.LoadAssets();
        ContentLoader.LoadContent();
        MapDisplayer.SetupAtlas();

        Debug.Log("Connecting to Master");
        PhotonNetwork.ConnectUsingSettings();
    }

    public void SetNickName(string name) {
        if (string.IsNullOrEmpty(name)) return;
        PhotonNetwork.NickName = name;
    }

    public override void OnConnectedToMaster() {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.NickName = "Player " + PhotonNetwork.CountOfPlayers.ToString("000");
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby() {
        Debug.Log("Joined Lobby");
        MenuManager.Instance.OpenMenu("TitleMenu");
    }

    public void CreateRoom() {
        if (string.IsNullOrEmpty(roomNameInputField.text)) return;
        PhotonNetwork.CreateRoom(roomNameInputField.text);
        MenuManager.Instance.OpenMenu("LoadingMenu");
    }

    public override void OnJoinedRoom() {
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
        errorText.text = "Room Creation Failed: " + message;
        MenuManager.Instance.OpenMenu("ErrorMenu");
    }

    public void StartGame() {
        PhotonNetwork.LoadLevel(1);
    }

    public void LeaveRoom() {
        PhotonNetwork.LeaveRoom();
        MenuManager.Instance.OpenMenu("LoadingMenu");
    }

    public void JoinRoom(RoomInfo info) {
        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.Instance.OpenMenu("LoadingMenu");
    }

    public override void OnLeftRoom() {
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