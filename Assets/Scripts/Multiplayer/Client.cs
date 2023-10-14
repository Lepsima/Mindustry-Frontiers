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

public class Client : MonoBehaviourPunCallbacks {
    public static Client local;
    public static Dictionary<short, SyncronizableObject> syncObjects = new();

    private void Awake() {
        local = this;
        MapLoader.OnMapLoaded += OnMapLoaded;
    }

    public static bool TypeEquals(Type target, Type reference) => target == reference || target.IsSubclassOf(reference);

    public static SyncronizableObject GetBySyncID(short syncID) {
        if (syncID == -1) return null;
        return syncObjects[syncID];
    }

    public static void SendSyncData(int[] data) {
        local.photonView.RPC(nameof(RPC_ReciveSyncData), RpcTarget.Others, (object)data);
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
        local.photonView.RPC(nameof(MasterRPC_CreateBlock), RpcTarget.MasterClient, position, orientation, type.id, teamCode);
    }

    [PunRPC]
    public void MasterRPC_CreateBlock(Vector2 position, int orientation, short contentID, byte teamCode) {
        // As the host, assign a Sync ID to the new block
        short syncID = HostSyncHandler.GetNewSyncID();
        local.photonView.RPC(nameof(RPC_CreateBlock), RpcTarget.All, position, orientation, contentID, syncID, teamCode);
    }

    [PunRPC]
    public void RPC_CreateBlock(Vector2 position, int orientation, short contentID, short syncID, byte teamCode) {
        // As all players, recive the new block
        if (isRecivingMap) return;
        MapManager.Instance.InstantiateBlock(position, orientation, contentID, syncID, teamCode);
    }



    public static void DestroyBlock(Block block, bool destroyed = false) {
        // As a player, send the remove command to all players
        local.photonView.RPC(nameof(RPC_DestroyBlock), RpcTarget.MasterClient, block.SyncID, destroyed);
    }

    [PunRPC]
    public void RPC_DestroyBlock(short syncID, bool destroyed) {
        // As all players, recive remove command
        if (isRecivingMap) return;
        ((Block)syncObjects[syncID]).Kill(destroyed);
    }



    public static void CreateUnit(Vector2 position, float rotation, Content type, byte teamCode) {
        // As a player, send unit creation command to the host
        local.photonView.RPC(nameof(MasterRPC_CreateUnit), RpcTarget.MasterClient, position, rotation, type.id, teamCode);
    }

    [PunRPC]
    public void MasterRPC_CreateUnit(Vector2 position, float rotation, short contentID, byte teamCode) {
        // As the host, assign a Sync ID to the unit
        short syncID = HostSyncHandler.GetNewSyncID();
        local.photonView.RPC(nameof(RPC_CreateUnit), RpcTarget.All, position, rotation, contentID, syncID, teamCode);
    }

    [PunRPC]
    public void RPC_CreateUnit(Vector2 position, float rotation, short contentID, short syncID, byte teamCode) {
        // As all players, recive the unit data with the Sync ID
        if (isRecivingMap) return;
        MapManager.Instance.InstantiateUnit(position, rotation, contentID, syncID, teamCode);
    }



    public static void DestroyUnit(Unit unit, bool destroyed = false) {
        // As a player, send a destroy command to all players
        local.photonView.RPC(nameof(RPC_DestroyUnit), RpcTarget.MasterClient, unit.SyncID, destroyed);
    }

    [PunRPC]
    public void RPC_DestroyUnit(short syncID, bool destroyed) {
        // As all players, destroy unit
        if (isRecivingMap) return;
        ((Unit)syncObjects[syncID]).Kill(destroyed);
    } 



    public static void UnitTakeOff(Unit unit) {
        if (!PhotonNetwork.IsMasterClient) return;
        local.photonView.RPC(nameof(RPC_UnitTakeoff), RpcTarget.All, unit.SyncID);
    }

    [PunRPC]
    public void RPC_UnitTakeoff(short syncID) {
        if (isRecivingMap) return;
        Unit unit = (Unit)syncObjects[syncID];
        unit.OnTakeOff();
    }

    public static void UnitChangePatrolPoint(Unit unit, Vector2 point) {
        if (!PhotonNetwork.IsMasterClient) return;
        local.photonView.RPC(nameof(RPC_UnitChangePatrolPoint), RpcTarget.All, unit.SyncID, point);
    }

    [PunRPC]
    public void RPC_UnitChangePatrolPoint(short syncID, Vector2 point) {
        if (isRecivingMap) return;
        Unit unit = (Unit)syncObjects[syncID];
        unit.patrolPosition = point;
    }

    public static void BulletHit(Entity entity, BulletType bulletType) {
        if (!PhotonNetwork.IsMasterClient) return;
        local.photonView.RPC(nameof(RPC_BulletHit), RpcTarget.All, entity.SyncID, bulletType.id);
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
        local.photonView.RPC(nameof(RPC_Damage), RpcTarget.All, entity.SyncID, damage);
    }

    [PunRPC]
    public void RPC_Damage(short syncID, float damage) {
        if (isRecivingMap) return;
        Entity entity = (Entity)syncObjects[syncID];
        DamageHandler.Damage(new(damage), entity);
    }



    public static void Explosion(BulletType bulletType, Vector2 position, int mask) {
        if (!PhotonNetwork.IsMasterClient) return;
        local.photonView.RPC(nameof(RPC_Explosion), RpcTarget.All, bulletType.id, position, mask);
    }

    [PunRPC]
    public void RPC_Explosion(short bulletID, Vector2 position, int mask) {
        if (isRecivingMap) return;
        DamageHandler.BulletExplode(BulletLoader.loadedBullets[bulletID], position, mask);
    }



    public static void AddItem(Entity entity, Item item, int amount) {
        if (!PhotonNetwork.IsMasterClient) return;
        local.photonView.RPC(nameof(RPC_AddItem), RpcTarget.All, entity.SyncID, item.id, amount);
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
        local.photonView.RPC(nameof(RPC_AddItems), RpcTarget.All, entity.SyncID, serializedStacks);
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
        local.photonView.RPC(nameof(RPC_CreateFire), RpcTarget.All, gridPosition);
    }

    [PunRPC]
    public void RPC_CreateFire(Vector2 gridPosition) {
        if (isRecivingMap) return;
        FireController.InstantiateFire(Vector2Int.CeilToInt(gridPosition));
    }

    #region - Map sync -

    public bool isRecivingMap = false;
    public List<int> mapRequestActorNumbers = new();
    public static MapAssembler mapAssembler;

    public static void RequestMap() {
        local.photonView.RPC(nameof(RPC_RequestMap), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        local.isRecivingMap = true;
        mapAssembler = new();
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

    public void OnMapLoaded(object sender, MapLoader.MapLoadedEventArgs e) {
        if (!PhotonNetwork.IsMasterClient || mapRequestActorNumbers.Count == 0) return;

        // Send the map to the players in the request list
        foreach(int actorNumber in mapRequestActorNumbers) {
            SendMap(PhotonNetwork.CurrentRoom.GetPlayer(actorNumber), e.loadedMap);
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

        string name = map.name;

        byte[] tileMapData = map.TilemapToBytes();
        byte[] blockData = MapManager.Map.BlocksToBytes(true);
        byte[] unitData = MapManager.Map.UnitsToBytes(true);

        local.photonView.RPC(nameof(RPC_ReciveMapData), player, name, (Vector2)map.size, tileMapData);
        local.photonView.RPC(nameof(RPC_ReciveBlockData), player, blockData);
        local.photonView.RPC(nameof(RPC_ReciveUnitData), player, blockData);
    }

    [PunRPC]
    public void RPC_ReciveMapData(string name, Vector2 size, byte[] tileMapData) {
        if (mapAssembler == null) Debug.LogWarning("The fuck did you do this time?");
        mapAssembler.ReciveTilemap(name, Vector2Int.CeilToInt(size), tileMapData);
    }

    [PunRPC]
    public void RPC_ReciveBlockData(byte[] blockData) {
        if (mapAssembler == null) Debug.LogWarning("The fuck did you do this time?");
        mapAssembler.ReciveBlocks(blockData);
    }

    [PunRPC] 
    public void RPC_ReciveUnitData(byte[] unitData) {
        if (mapAssembler == null) Debug.LogWarning("The fuck did you do this time?");
        mapAssembler.ReciveUnits(unitData);
    }

    #endregion
}