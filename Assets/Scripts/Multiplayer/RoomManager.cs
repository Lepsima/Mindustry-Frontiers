using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using Photon.Pun.UtilityScripts;
using System;
using ExitGames.Client.Photon;
using Frontiers.Teams;
using Frontiers.Content.Maps;

public class RoomManager : MonoBehaviourPunCallbacks {
    public static RoomManager Instance;
    public PhotonTeamsManager photonTeamsManager;

    private float switchButtonCooldown;
    public bool updateManagers = false;

    public int shardedTeamPlayers;
    public int cruxTeamPlayers;

    #region - Unity callbacks -

    private void Awake() {
        if (Instance) {
            Destroy(gameObject);
            return;
        }

        if (!photonTeamsManager) photonTeamsManager = GetComponent<PhotonTeamsManager>();
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        Instance = this;
    }

    private void Update() {
        if (!updateManagers) return;
        MapManager.Instance.UpdateMapManager();
    }

    private void FixedUpdate() {
        if (!updateManagers) return;
        HostSyncHandler.UpdateSyncObjects(Time.fixedDeltaTime);
    }

    #endregion

    #region - Room -

    public override void OnJoinedRoom() {
        if (PhotonNetwork.LocalPlayer.GetPhotonTeam() == null) PhotonNetwork.LocalPlayer.JoinTeam(TeamUtilities.GetDefaultTeam());
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) {
        if (!changedProps.ContainsKey("_pt")) return;

        shardedTeamPlayers = TeamUtilities.TryGetTeamMembers(1).Length;
        cruxTeamPlayers = TeamUtilities.TryGetTeamMembers(2).Length;
    }

    public void SwitchTeam() {
        if (switchButtonCooldown >= Time.time) return;
        switchButtonCooldown = Time.time + 1f;
        PhotonNetwork.LocalPlayer.SwitchTeam(TeamUtilities.GetEnemyTeam(TeamUtilities.GetLocalTeam()));
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) {
        if (scene.buildIndex == 1) {
            DiscordActivities.SetState(DiscordActivities.State.InGame);

            // Initialize managers
            MapManager.InitializeMapManager();

            // If this client is the master, spawn cores
            if (TeamUtilities.IsMaster()) {
                MapLoader.LoadMap(Launcher.Instance.selectedMap);
            } else {
                Client.RequestMap();
            }
        }
    }

    #endregion
}