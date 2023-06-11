using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public static class Server {
    public static Queue<SyncronizableObject> syncQueue = new();
    public static float timePerUpdate;

    public static Client LocalClient => Client.local;

    public static int nextSyncID;

    public static void Set(int updatesPerSecond) {
        timePerUpdate = 1f / updatesPerSecond;
        syncQueue = new();
        nextSyncID = 0;
    }

    public static int GetNewSyncID() {
        if (!PhotonNetwork.IsMasterClient) return -1;

        int syncID = nextSyncID;
        nextSyncID++;
        return syncID;
    }

    public static void UpdateSyncObjects(float deltaTime) {
        if (!PhotonNetwork.IsMasterClient) return;

        int updates = Mathf.FloorToInt(deltaTime / timePerUpdate);

        for (int i = 0; i < updates; i++) {
            SyncronizableObject syncObject = syncQueue.Dequeue();

            if (!syncObject || !syncObject.syncs) {
                syncObject.RetrySync();
                continue; 
            }

            Client.SendSyncData(syncObject.GetSyncValues());
        }
    }
}