using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.IO;

public class BulletGameObjectPool : MonoBehaviourPun {
    public readonly List<GameObject> pooledGameObjects = new List<GameObject>();

    public static BulletGameObjectPool defaultBulletGameObjectPool;

    private void Awake() {
        if (!defaultBulletGameObjectPool) defaultBulletGameObjectPool = this;
    }

    public void Instantiate() {
        int viewID = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "bPref"), new Vector3(1000f, 1000f, 1000f), Quaternion.identity).GetPhotonView().ViewID;
        photonView.RPC(nameof(RPC_Store), RpcTarget.All, viewID);
    }

    public static void CreateBullet(Vector2 position, float angle, short id, byte teamCode) {
        defaultBulletGameObjectPool.photonView.RPC(nameof(RPC_GetInstantiated), RpcTarget.All, position, angle, id, MapManager.GetCurrentTime(), teamCode);
    }

    [PunRPC]
    public void RPC_GetInstantiated(Vector2 position, float angle, short id, float timeCode, byte teamCode) {
        if (pooledGameObjects.Count != 0) {
            GameObject gameObject = pooledGameObjects[0];
            pooledGameObjects.Remove(gameObject);
            gameObject.GetComponent<Bullet>().Set(position, angle, id, timeCode, teamCode);

        } else if (photonView.Owner == PhotonNetwork.MasterClient) Instantiate();
    }

    public static void StoreBullet(int viewID) {
        defaultBulletGameObjectPool.photonView.RPC(nameof(RPC_Store), RpcTarget.All, viewID);
    }

    [PunRPC]
    public void RPC_Store(int viewID) {
        GameObject bulletGameObject = PhotonNetwork.GetPhotonView(viewID).gameObject;
        pooledGameObjects.Add(bulletGameObject);
        bulletGameObject.SetActive(false);
        bulletGameObject.GetComponent<Bullet>().Store();
    }

}
