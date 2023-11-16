using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Frontiers.Content;
using Frontiers.Content.Maps;
using Frontiers.Teams;
using System.Linq;
using System.IO.Compression;
using Frontiers.Squadrons;

public class Client : MonoBehaviourPunCallbacks {
    public static Client Instance;
    public static Dictionary<short, SyncronizableObject> syncObjects = new();

    private void Awake() {
        Instance = this;
    }

    public static bool TypeEquals(Type target, Type reference) => target == reference || target.IsSubclassOf(reference);

    public static SyncronizableObject GetBySyncID(short syncID) {
        if (syncID == -1) return null;
        return syncObjects[syncID];
    }

    public static void SendSyncData(int[] data) {
        Instance.photonView.RPC(nameof(RPC_ReciveSyncData), RpcTarget.Others, (object)data);
    }

    [PunRPC]
    public void RPC_ReciveSyncData(int[] data) {
        if (isRecivingMap) return;

        int syncID = data[0];
        SyncronizableObject syncObject = syncObjects[(short)syncID];
        syncObject.ApplySyncData(data);
    }



    public static void CreateBlock(Vector2 position, int orientation, Content type, byte teamCode) {
        // As a player, send the block data to the host
        Instance.photonView.RPC(nameof(MasterRPC_CreateBlock), RpcTarget.MasterClient, position, orientation, type.id, teamCode);
    }

    [PunRPC]
    public void MasterRPC_CreateBlock(Vector2 position, int orientation, short contentID, byte teamCode) {
        // As the host, assign a Sync ID to the new block
        short syncID = HostSyncHandler.GetNewSyncID();
        Instance.photonView.RPC(nameof(RPC_CreateBlock), RpcTarget.All, position, orientation, contentID, syncID, teamCode);
    }

    [PunRPC]
    public void RPC_CreateBlock(Vector2 position, int orientation, short contentID, short syncID, byte teamCode) {
        // As all players, recive the new block
        if (isRecivingMap) return;
        MapManager.Instance.InstantiateBlock(position, orientation, contentID, syncID, teamCode);
    }



    public static void DestroyBlock(Block block, bool destroyed = false) {
        // As a player, send the remove command to all players
        Instance.photonView.RPC(nameof(RPC_DestroyBlock), RpcTarget.All, block.SyncID, destroyed);
    }

    [PunRPC]
    public void RPC_DestroyBlock(short syncID, bool destroyed) {
        // As all players, recive remove command
        if (isRecivingMap) return;
        ((Block)syncObjects[syncID]).Kill(destroyed);
    }



    public static void CreateUnit(Vector2 position, float rotation, Content type, byte teamCode) {
        // As a player, send unit creation command to the host
        Instance.photonView.RPC(nameof(MasterRPC_CreateUnit), RpcTarget.MasterClient, position, rotation, type.id, teamCode);
    }

    [PunRPC]
    public void MasterRPC_CreateUnit(Vector2 position, float rotation, short contentID, byte teamCode) {
        // As the host, assign a Sync ID to the unit
        short syncID = HostSyncHandler.GetNewSyncID();
        Instance.photonView.RPC(nameof(RPC_CreateUnit), RpcTarget.All, position, rotation, contentID, syncID, teamCode);
    }

    [PunRPC]
    public void RPC_CreateUnit(Vector2 position, float rotation, short contentID, short syncID, byte teamCode) {
        // As all players, recive the unit data with the Sync ID
        if (isRecivingMap) return;
        MapManager.Instance.InstantiateUnit(position, rotation, contentID, syncID, teamCode);
    }



    public static void DestroyUnit(Unit unit, bool destroyed = false) {
        // As a player, send a destroy command to all players
        Instance.photonView.RPC(nameof(RPC_DestroyUnit), RpcTarget.All, unit.SyncID, destroyed);
    }

    [PunRPC]
    public void RPC_DestroyUnit(short syncID, bool destroyed) {
        // As all players, destroy unit
        if (isRecivingMap) return;
        ((Unit)syncObjects[syncID]).Kill(destroyed);
    } 



    public static void UnitTakeOff(Unit unit) {
        if (!PhotonNetwork.IsMasterClient) return;
        Instance.photonView.RPC(nameof(RPC_UnitTakeoff), RpcTarget.All, unit.SyncID);
    }

    [PunRPC]
    public void RPC_UnitTakeoff(short syncID) {
        if (isRecivingMap) return;
        Unit unit = (Unit)syncObjects[syncID];
        unit.OnTakeOff();
    }

    public static void UnitChangePatrolPoint(Unit unit, Vector2 point) {
        if (!PhotonNetwork.IsMasterClient) return;
        Instance.photonView.RPC(nameof(RPC_UnitChangePatrolPoint), RpcTarget.All, unit.SyncID, point);
    }

    [PunRPC]
    public void RPC_UnitChangePatrolPoint(short syncID, Vector2 point) {
        if (isRecivingMap) return;
        Unit unit = (Unit)syncObjects[syncID];
        unit.patrolPosition = point;
    }

    public static void BulletHit(Entity entity, BulletType bulletType) {
        if (!PhotonNetwork.IsMasterClient) return;
        Instance.photonView.RPC(nameof(RPC_BulletHit), RpcTarget.All, entity.SyncID, bulletType.id);
    }

    [PunRPC]
    public void RPC_BulletHit(short syncID, short bulletID) {
        if (isRecivingMap || syncObjects[syncID] == null) return;

        Entity entity = (Entity)syncObjects[syncID];
        BulletType bulletType = BulletLoader.loadedBullets[bulletID];
        DamageHandler.BulletHit(bulletType, entity);     
    }



    public static void Damage(Entity entity, float damage) {
        if (!PhotonNetwork.IsMasterClient) return;
        Instance.photonView.RPC(nameof(RPC_Damage), RpcTarget.All, entity.SyncID, damage);
    }

    [PunRPC]
    public void RPC_Damage(short syncID, float damage) {
        if (isRecivingMap) return;
        Entity entity = (Entity)syncObjects[syncID];
        DamageHandler.Damage(new(damage), entity);
    }



    public static void Explosion(BulletType bulletType, Vector2 position, int mask) {
        if (!PhotonNetwork.IsMasterClient) return;
        Instance.photonView.RPC(nameof(RPC_Explosion), RpcTarget.All, bulletType.id, position, mask);
    }

    [PunRPC]
    public void RPC_Explosion(short bulletID, Vector2 position, int mask) {
        if (isRecivingMap) return;
        DamageHandler.BulletExplode(BulletLoader.loadedBullets[bulletID], position, mask);
    }



    public static void AddItem(Entity entity, Item item, int amount) {
        if (!PhotonNetwork.IsMasterClient) return;
        Instance.photonView.RPC(nameof(RPC_AddItem), RpcTarget.All, entity.SyncID, item.id, amount);
    }



    [PunRPC]
    public void RPC_AddItem(short syncID, short itemID, int amount) {
        if (isRecivingMap) return;
        ItemBlock block = (ItemBlock)syncObjects[syncID];
        Item item = (Item)ContentLoader.GetContentById(itemID);

        if (!block.CanReciveItem(item)) return;
        block.GetInventory().Add(item, amount);
    }

    public static void AddItems(Entity entity, ItemStack[] stacks) {
        if (!PhotonNetwork.IsMasterClient) return;
        int[] serializedStacks = ItemStack.Serialize(stacks);
        Instance.photonView.RPC(nameof(RPC_AddItems), RpcTarget.All, entity.SyncID, serializedStacks);
    }

    [PunRPC]
    public void RPC_AddItems(short syncID, int[] serializedStacks) {
        if (isRecivingMap) return;

        ItemStack[] stacks = ItemStack.DeSerialize(serializedStacks);
        ItemBlock block = (ItemBlock)syncObjects[syncID];

        if (!block.hasItemInventory) return;
        block.GetInventory().Add(stacks);
    }

    public static void CreateFire(Vector2 gridPosition) {
        if (!PhotonNetwork.IsMasterClient) return;
        Instance.photonView.RPC(nameof(RPC_CreateFire), RpcTarget.All, gridPosition);
    }

    [PunRPC]
    public void RPC_CreateFire(Vector2 gridPosition) {
        if (isRecivingMap) return;
        FireController.InstantiateFire(Vector2Int.CeilToInt(gridPosition));
    }



    public static void CreateSquadron(byte teamCode, byte id, string name) {
        Instance.photonView.RPC(nameof(RPC_CreateSquadron), RpcTarget.All, teamCode, id, name);
    }

    [PunRPC]
    public void RPC_CreateSquadron(byte teamCode, byte id, string name) {
        SquadronHandler.CreateSquadron(teamCode, name, id);
    }



    public static void RequestSquadrons() {
        Instance.photonView.RPC(nameof(RPC_RequestSquadrons), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    public void RPC_RequestSquadrons(int actorNumber) {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);

        for (int i = 0; i < SquadronHandler.team1Squadrons.Length; i++) {
            Squadron squadron = SquadronHandler.team1Squadrons[i];
            if (squadron == null) continue;

            byte id = squadron.squadronID;
            string name = squadron.name;
            short[] members = squadron.GetMembersSyncIDs();

            Instance.photonView.RPC(nameof(RPC_ReciveSquadron), player, squadron.teamCode, id, name, members);
        }

        for (int i = 0; i < SquadronHandler.team2Squadrons.Length; i++) {
            Squadron squadron = SquadronHandler.team2Squadrons[i];
            if (squadron == null) continue;

            byte id = squadron.squadronID;
            string name = squadron.name;
            short[] members = squadron.GetMembersSyncIDs();

            Instance.photonView.RPC(nameof(RPC_ReciveSquadron), player, squadron.teamCode, id, name, members);
        }
    }

    [PunRPC]
    public void RPC_ReciveSquadron(byte teamCode, byte id, string name, short[] members) {
        SquadronHandler.CreateSquadron(teamCode, name, id);
        Squadron squadron = SquadronHandler.GetSquadronByID(teamCode == 1, id);

        for (int i = 0; i < members.Length; i++) {
            Unit unit = (Unit)syncObjects[members[i]];
            squadron.Add(unit);
        }
    }



    public static void AddMemeberToSquadron(Squadron squadron, Unit unit) {
        if (squadron.teamCode != unit.GetTeam()) return;
        Instance.photonView.RPC(nameof(RPC_AddMemberToSquadron), RpcTarget.All, squadron.teamCode, squadron.squadronID, unit.SyncID);
    }

    [PunRPC]
    public void RPC_AddMemberToSquadron(byte teamCode, byte id, short unitID) {
        Squadron squadron = SquadronHandler.GetSquadronByID(teamCode == 1, id);
        Unit unit = (Unit)syncObjects[unitID];
        squadron.Add(unit);
    }



    public static void RemoveMemeberFromSquadron(Squadron squadron, Unit unit) {
        if (squadron.teamCode != unit.GetTeam()) return;
        Instance.photonView.RPC(nameof(RPC_RemoveMemberFromSquadron), RpcTarget.All, squadron.teamCode, squadron.squadronID, unit.SyncID);
    }

    [PunRPC]
    public void RPC_RemoveMemberFromSquadron(byte teamCode, byte id, short unitID) {
        Squadron squadron = SquadronHandler.GetSquadronByID(teamCode == 1, id);
        Unit unit = (Unit)syncObjects[unitID];
        squadron.Remove(unit);
    }



    public static void ChangeSquadronAction(Squadron squadron, Frontiers.Squadrons.Action action) {
        Instance.photonView.RPC(nameof(RPC_ChangeSquadronAction), RpcTarget.All, squadron.teamCode, squadron.squadronID, action.action, action.radius, action.position);
    }

    [PunRPC]
    public void RPC_ChangeSquadronAction(byte teamCode, byte id, int actionID, float radius, Vector2 position) {
        Frontiers.Squadrons.Action action = new(actionID, radius, position);
        Squadron squadron = SquadronHandler.GetSquadronByID(teamCode == 1, id);
        squadron.SetAction(action);
    }

    #region - Map sync -

    public bool isRecivingMap = false;
    public List<int> mapRequestActorNumbers = new();
    public static byte[] blockData = null;
    public static byte[] unitData = null;

    public static void RequestMap() {
        Instance.photonView.RPC(nameof(RPC_RequestMap), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        Instance.isRecivingMap = true;
    }

    [PunRPC]
    public void RPC_RequestMap(int actorNumber) {
        if (MapManager.IsLoaded()) {
            // Send map directly
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            Map map = MapManager.Map;
            SendMap(player, map);
        } else {
            // Add player to request list, the map will be sent once is loaded
            if (!mapRequestActorNumbers.Contains(actorNumber)) mapRequestActorNumbers.Add(actorNumber);
        }
    }

    public static void OnMapLoaded(Map map) {
        if (blockData != null) map.BlocksFromBytes(blockData);
        if (unitData != null) map.UnitsFromBytes(unitData);

        if (!PhotonNetwork.IsMasterClient || Instance.mapRequestActorNumbers.Count == 0) return;

        // Send the map to the players in the request list
        foreach(int actorNumber in Instance.mapRequestActorNumbers) {
            Instance.SendMap(PhotonNetwork.CurrentRoom.GetPlayer(actorNumber), map);
        }
    }

    /// <summary>
    /// Sends the data of a map to a player
    /// </summary>
    /// <param name="player">The player that will recive the map data</param>
    /// <param name="map">The map that wants to be sent</param>
    public void SendMap(Player player, Map map) {
        // This here to prevent dumb things
        if (!PhotonNetwork.IsMasterClient) return;

        Instance.photonView.RPC(nameof(RPC_ReciveMapData), player, map.name, (Vector2)map.size, map.TilemapToBytes());
        Instance.photonView.RPC(nameof(RPC_ReciveBlockData), player, map.BlocksToBytes(true));
        Instance.photonView.RPC(nameof(RPC_ReciveUnitData), player, map.UnitsToBytes(true));
    }

    [PunRPC]
    public void RPC_ReciveMapData(string name, Vector2 size, byte[] tileMapData) {
        isRecivingMap = false;
        MapLoader.LoadMap(name, Vector2Int.CeilToInt(size), tileMapData);
    }

    [PunRPC]
    public void RPC_ReciveBlockData(byte[] blockData) {
        if (isRecivingMap) Client.blockData.AddRange(blockData);
        else MapManager.Map.BlocksFromBytes(blockData);
    }

    [PunRPC] 
    public void RPC_ReciveUnitData(byte[] unitData) {
        if (isRecivingMap) Client.unitData.AddRange(unitData);
        else MapManager.Map.UnitsFromBytes(unitData);
    }

    #endregion
}