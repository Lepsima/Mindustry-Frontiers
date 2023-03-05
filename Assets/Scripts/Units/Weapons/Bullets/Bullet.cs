using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;

public class Bullet : LoadableContent, IView {
    BulletType Type { get; set; }
    public PhotonView PhotonView { get; set; }

    Rigidbody2D rigidBody2D;

    private void Awake() {
        rigidBody2D = GetComponent<Rigidbody2D>();
    }

    public void Set(Vector2 position, float angle, short id, float timeCode, byte teamCode) {
        base.Set(timeCode, teamCode, false);

        PhotonView = GetComponent<PhotonView>();

        Type = (BulletType)ContentLoader.GetContentById(id);
        transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, angle));

        enabled = true;

        GetComponent<Collider2D>().enabled = PhotonView.IsMine;
        GetComponent<TrailRenderer>().Clear();
        Invoke(nameof(StoreCall), Type.lifeTime);

        gameObject.SetActive(true);
        rigidBody2D.velocity = Type.speed * transform.up;
    }

    protected override int GetTeamLayer(bool ignore = false) => base.GetTeamLayer(true);

    protected override int GetTeamMask(bool ignore = false) => base.GetTeamMask(true);

    public void Store() {
        transform.position = new Vector3(1000f, 1000f, 1000f);
        enabled = false;
        Type = null;
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.TryGetComponent(out IDamageable damageable)) {
            MapManager.Instance.BulletHit(damageable, Type);
            StoreCall();
        }
    }

    private void StoreCall() {
        BulletGameObjectPool.StoreBullet(PhotonView.ViewID);
    }
}