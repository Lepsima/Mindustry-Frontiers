using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class PVComponentDisabler : MonoBehaviour {
    public bool disableIfMine;
    public Behaviour[] behavioursToDisable;

    private void Awake() {
        if (GetComponent<PhotonView>().IsMine == disableIfMine) foreach(Behaviour behaviour in behavioursToDisable) behaviour.enabled = false;
    }
}
