using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Frontiers.Content;
using Frontiers.Teams;
using Photon.Pun;

public abstract class SyncronizableObject : MonoBehaviour {
    public short SyncID { set; get; }
    public int syncValues = 0;

    public float syncTime = 2f, syncTimer = 0f;

    public static bool IsMaster => PhotonNetwork.IsMasterClient;
    public bool syncs = true;

    public void Set(short SyncID) {
        this.SyncID = SyncID;
    }

    public virtual int[] GetSyncData() {
        return new int[syncValues];
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