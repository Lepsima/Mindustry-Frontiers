using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Frontiers.Content;
using Frontiers.Teams;
using Photon.Pun;

public abstract class SyncronizableObject : MonoBehaviour {
    public int SyncID { set; get; }
    public int syncValues = 1;

    public float defaultSyncTime = 2f;
    public float syncTime = 2f;

    public static bool IsMaster => PhotonNetwork.IsMasterClient;
    public bool syncs = true;

    public void Set(int SyncID) {
        this.SyncID = SyncID;
    }

    public virtual int[] GetSyncData() {
        int[] data = new int[syncValues];
        data[0] = SyncID;
        return data;
    }

    public virtual void ApplySyncData(int[] values) {
        // Do nothing here
    }

    public void RetrySync() {
        Invoke(nameof(AddToSyncQueue), syncTime);
    }

    public void AddToSyncQueue() {
        HostSyncHandler.syncQueue.Enqueue(this);
    }
}