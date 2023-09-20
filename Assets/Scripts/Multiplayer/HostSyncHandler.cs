using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public static class HostSyncHandler {
    public static List<SyncronizableObject> syncronizableObjects = new();
    public static float timePerUpdate;

    static int index;

    public static Client LocalClient => Client.local;

    public static int nextSyncID;

    public static void Set(int updatesPerSecond) {
        timePerUpdate = 1f / updatesPerSecond;
        nextSyncID = 0;
    }

    public static int GetNewSyncID() {
        if (!PhotonNetwork.IsMasterClient) return -1;

        nextSyncID++;
        return nextSyncID - 1;
    }

    public static void UpdateSyncObjects(float deltaTime) {
        if (!PhotonNetwork.IsMasterClient) return;

        int updates = Mathf.FloorToInt(deltaTime / timePerUpdate);

        for (int i = 0; i < updates; i++) {

            index++;
            if (index >= syncronizableObjects.Count) index = 0;

            SyncronizableObject syncObject = syncronizableObjects[index];
            if (syncObject.CanSync()) syncObject.Sync();
        }
    }
}