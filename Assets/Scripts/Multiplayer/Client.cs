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
    public static Dictionary<int, SyncronizableObject> syncObjects = new();

    private void Awake() {
        local = this;
        MapLoader.OnMapLoaded += OnMapLoaded;
    }

    public static bool TypeEquals(Type target, Type reference) => target == reference || target.IsSubclassOf(reference);

    public static SyncronizableObject GetBySyncID(int syncID) {
        return syncObjects[syncID];
    }

    public static void SendSyncData(float[] values) {
        local.photonView.RPC(nameof(RPC_ReciveSyncValues), RpcTarget.Others, (object)values);
    }

    [PunRPC]
    public void RPC_ReciveSyncValues(float[] values) {
        if (isRecivingMap) return;
        int syncID = (int)values[0];
        SyncronizableObject syncObject = syncObjects[syncID];
        syncObject.ApplySyncValues(values);
    }


    public static void BuildBlock(ConstructionBlock block) {
        local.photonView.RPC(nameof(MasterRPC_BuildBlock), RpcTarget.MasterClient, (Vector2)block.GetGridPosition(), block.GetOrientation(), block.Type.id, block.GetTeam(), block.SyncID);
    }

    [PunRPC]
    public void MasterRPC_BuildBlock(Vector2 position, int orientation, short contentID, byte teamCode, int syncID) {
        local.photonView.RPC(nameof(RPC_CreateBlock), RpcTarget.All, position, orientation, false, contentID, syncID, teamCode);
    }

    public static void CreateBlock(Vector2 position, int orientation, bool isPlan, Content type, byte teamCode) {
        local.photonView.RPC(nameof(MasterRPC_CreateBlock), RpcTarget.MasterClient, position, orientation, isPlan, type.id, teamCode);
    }

    [PunRPC]
    public void MasterRPC_CreateBlock(Vector2 position, int orientation, bool isPlan, short contentID, byte teamCode) {
        int syncID = Server.GetNewSyncID();
        local.photonView.RPC(nameof(RPC_CreateBlock), RpcTarget.All, position, orientation, isPlan, contentID, syncID, teamCode);
    }

    [PunRPC]
    public void RPC_CreateBlock(Vector2 position, int orientation, bool isPlan, short contentID, int syncID, byte teamCode) {
        if (isRecivingMap) return;

        if (isPlan) { 
            MapManager.Instance.InstantiateConstructionBlock(position, orientation, contentID, syncID, teamCode); 
        } else { 

            if (syncObjects.ContainsKey(syncID)) {
                ConstructionBlock constructionBlock = syncObjects[syncID] as ConstructionBlock;
                MapManager.Instance.DeleteBlock(constructionBlock, false);
            }

            MapManager.Instance.InstantiateBlock(position, orientation, contentID, syncID, teamCode); 
        }
    }



    public static void DestroyBlock(Block block, bool destroyed = false) {
        local.photonView.RPC(nameof(MasterRPC_DestroyBlock), RpcTarget.MasterClient, block.SyncID, destroyed);
    }

    [PunRPC]
    public void MasterRPC_DestroyBlock(int syncID, bool destroyed) {
        local.photonView.RPC(nameof(RPC_DestroyBlock), RpcTarget.All, syncID, destroyed);
    }

    [PunRPC]
    public void RPC_DestroyBlock(int syncID, bool destroyed) {
        if (isRecivingMap) return;
        MapManager.Instance.DeleteBlock((Block)syncObjects[syncID], destroyed);
    }



    public static void CreateUnit(Vector2 position, float rotation, Content type, byte teamCode) {
        local.photonView.RPC(nameof(MasterRPC_CreateUnit), RpcTarget.MasterClient, position, rotation, type.id, teamCode);
    }

    [PunRPC]
    public void MasterRPC_CreateUnit(Vector2 position, float rotation, short contentID, byte teamCode) {
        int syncID = Server.GetNewSyncID();
        local.photonView.RPC(nameof(RPC_CreateUnit), RpcTarget.All, position, rotation, contentID, syncID, teamCode);
    }

    [PunRPC]
    public void RPC_CreateUnit(Vector2 position, float rotation, short contentID, int syncID, byte teamCode) {
        if (isRecivingMap) return;
        MapManager.Instance.InstantiateUnit(position, rotation, contentID, syncID, teamCode);
    }



    public static void DestroyUnit(Unit unit, bool destroyed = false) {
        local.photonView.RPC(nameof(MasterRPC_DestroyUnit), RpcTarget.MasterClient, unit.SyncID, destroyed);
    }

    [PunRPC]
    public void MasterRPC_DestroyUnit(int syncID, bool destroyed) {
        local.photonView.RPC(nameof(RPC_DestroyUnit), RpcTarget.All, syncID, destroyed);
    }

    [PunRPC]
    public void RPC_DestroyUnit(int syncID, bool destroyed) {
        if (isRecivingMap) return;
        MapManager.Instance.DeleteUnit((Unit)syncObjects[syncID], destroyed);
    }



    public static void WeaponShoot(Weapon weapon) {
        if (!PhotonNetwork.IsMasterClient) return;
        local.photonView.RPC(nameof(RPC_WeaponShoot), RpcTarget.All, weapon.parentEntity.SyncID, weapon.weaponID);
    }

    [PunRPC]
    public void RPC_WeaponShoot(int syncID, int weaponID) {
        if (isRecivingMap) return;
        Weapon weapon = ((IArmed)syncObjects[syncID]).GetWeaponByID(weaponID);
        weapon.Shoot();
    }

    public static void UnitTakeOff(Unit unit) {
        if (!PhotonNetwork.IsMasterClient) return;
        local.photonView.RPC(nameof(RPC_UnitTakeoff), RpcTarget.All, unit.SyncID);
    }

    [PunRPC]
    public void RPC_UnitTakeoff(int syncID) {
        if (isRecivingMap) return;
        Unit unit = (Unit)syncObjects[syncID];
        unit.TakeOff();
    }

    public static void UnitChangeMode(Unit unit, int mode, bool registerPrev = false) {
        if (!PhotonNetwork.IsMasterClient || (int)unit.Mode == mode || !unit.CanRequest()) return;
        unit.AddToRequestTimer();
        local.photonView.RPC(nameof(RPC_UnitChangeMode), RpcTarget.All, unit.SyncID, mode, registerPrev);
    }

    [PunRPC]
    public void RPC_UnitChangeMode(int syncID, int mode, bool registerPrev) {
        if (isRecivingMap) return;
        Unit unit = (Unit)syncObjects[syncID];
        unit.ChangeMode(mode, registerPrev);
    }

    public static void UnitChangePatrolPoint(Unit unit, Vector2 point) {
        if (!PhotonNetwork.IsMasterClient) return;
        local.photonView.RPC(nameof(RPC_UnitChangePatrolPoint), RpcTarget.All, unit.SyncID, point);
    }

    [PunRPC]
    public void RPC_UnitChangePatrolPoint(int syncID, Vector2 point) {
        if (isRecivingMap) return;
        Unit unit = (Unit)syncObjects[syncID];
        unit.patrolPosition = point;
    }

    public static void BulletHit(Entity entity, BulletType bulletType) {
        if (!PhotonNetwork.IsMasterClient) return;
        local.photonView.RPC(nameof(RPC_BulletHit), RpcTarget.All, entity.SyncID, bulletType.id);
    }

    [PunRPC]
    public void RPC_BulletHit(int syncID, short bulletID) {
        if (isRecivingMap) return;
        Entity entity = (Entity)syncObjects[syncID];
        BulletType bulletType = (BulletType)ContentLoader.GetContentById(bulletID);

        if (entity.TryGetComponent(out IDamageable damageable)) damageable.Damage(bulletType.damage);

        if (bulletType.HasBlastDamage()) {
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(entity.GetPosition(), bulletType.blastRadius, TeamUtilities.GetTeamMask(entity.GetTeam()))) {
                if (collider.transform.TryGetComponent(out IDamageable areaDamageable)) {

                    float distance = Vector2.Distance(entity.transform.position, collider.transform.position);
                    areaDamageable.Damage(bulletType.Damage(areaDamageable, distance));
                }
            }

        } else {

        }
    }



    public static void Damage(Entity entity, float damage) {
        if (!PhotonNetwork.IsMasterClient) return;
        local.photonView.RPC(nameof(RPC_Damage), RpcTarget.All, entity.SyncID, damage);
    }

    [PunRPC]
    public void RPC_Damage(int syncID, float damage) {
        if (isRecivingMap) return;
        Entity entity = (Entity)syncObjects[syncID];
        entity.Damage(damage);
    }

    public static void AddItem(Entity entity, Item item, int amount) {
        if (!PhotonNetwork.IsMasterClient) return;
        local.photonView.RPC(nameof(RPC_AddItem), RpcTarget.All, entity.SyncID, item.id, amount);
    }

    [PunRPC]
    public void RPC_AddItem(int syncID, short itemID, int amount) {
        if (isRecivingMap) return;
        Entity entity = (Entity)syncObjects[syncID];
        Item item = (Item)ContentLoader.GetContentById(itemID);

        if (!entity.CanReciveItem(item)) return;
        entity.GetInventory().Add(item, amount);
    }

    public static void AddItems(Entity entity, ItemStack[] stacks) {
        if (!PhotonNetwork.IsMasterClient) return;
        int[] serializedStacks = ItemStack.Serialize(stacks);
        local.photonView.RPC(nameof(RPC_AddItems), RpcTarget.All, entity.SyncID, serializedStacks);
    }

    [PunRPC]
    public void RPC_AddItems(int syncID, int[] serializedStacks) {
        if (isRecivingMap) return;

        ItemStack[] stacks = ItemStack.DeSerialize(serializedStacks);
        Entity entity = (Entity)syncObjects[syncID];

        if (!entity.hasInventory) return;
        entity.GetInventory().Add(stacks);
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

    public static void RequestMap() {
        local.photonView.RPC(nameof(RPC_RequestMap), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        local.isRecivingMap = true;
    }

    [PunRPC]
    public void RPC_RequestMap(int actorNumber) {
        if (MapManager.IsLoaded()) {
            Map map = MapManager.Map;

            string name = map.name;
            string[] tileMapData = map.TilemapsToStringArray();

            local.photonView.RPC(nameof(RPC_ReciveMapData), PhotonNetwork.CurrentRoom.GetPlayer(actorNumber), name, (Vector2)map.size, tileMapData);
        } else {
            if (!mapRequestActorNumbers.Contains(actorNumber)) mapRequestActorNumbers.Add(actorNumber);
        }
    }

    public void OnMapLoaded(object sender, MapLoader.MapLoadedEventArgs e) {
        if (!PhotonNetwork.IsMasterClient) {
            photonView.RPC(nameof(RPC_RequestEntityData), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            return;
        }

        if (mapRequestActorNumbers.Count == 0) return;

        Map map = e.loadedMap;

        string name = map.name;
        string[] tileMapData = map.TilemapsToStringArray();


        foreach(int actorNumber in mapRequestActorNumbers) {
            local.photonView.RPC(nameof(RPC_ReciveMapData), PhotonNetwork.CurrentRoom.GetPlayer(actorNumber), name, (Vector2)map.size, tileMapData);
        }
    }

    [PunRPC]
    public void RPC_ReciveMapData(string name, Vector2 size, string[] tileMapData)  {
        MapLoader.ReciveMap(name, size, tileMapData);
        isRecivingMap = false;
    }

    [PunRPC]
    public void RPC_RequestEntityData(int actorNumber) {
        string[] blockData = MapManager.Map.BlocksToStringArray();
        local.photonView.RPC(nameof(RPC_ReciveBlockData), PhotonNetwork.CurrentRoom.GetPlayer(actorNumber), blockData);
    }

    [PunRPC]
    public void RPC_ReciveBlockData(string[] blockData) {
        MapManager.Map.SetBlocksFromStringArray(blockData);
    }

    #endregion
}