using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class PlayerListItem : MonoBehaviourPunCallbacks {

    [SerializeField] TMP_Text text;
    Player player;

    public void SetUp(Player player) {
        this.player = player;
        text.text = player.NickName;
    }

    public void SetColor(Color color) {
        text.color = color;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) {
        if (player == otherPlayer) Destroy(gameObject);
    }

    public override void OnLeftRoom() {
        Destroy(gameObject);
    }
}