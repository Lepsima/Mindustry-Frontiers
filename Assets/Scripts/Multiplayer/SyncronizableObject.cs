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

    public float syncTime = 2f, syncTimer = 0f;

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

    public bool CanSync() {
        return syncs && syncTimer <= Time.time;
    }

    public void Sync() {
        syncTimer = Time.time + syncTime;
        Client.SendSyncData(GetSyncData());
    }
}