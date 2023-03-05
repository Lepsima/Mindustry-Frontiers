using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public abstract class Weapon : LoadableContent {
    protected ParticleSystem shootParticleSystemEffect;

    public WeaponType Type { private set; get; }
    protected LoadableContent parentEntity;

    int currentRounds;
    float avilableShootTimer = 0f;
    bool isReloading = false;
    public bool isFullAuto = false;

    private void Update() {
        if (isFullAuto && IsAvilable()) {
            Shoot();
        }
    }

    public virtual void Set(LoadableContent parentEntity, WeaponType weaponType, bool mirrored = false, float timeCode = 0, byte teamCode = 0) {
        base.Set(timeCode, teamCode, false);

        this.parentEntity = parentEntity;
        Type = weaponType;

        shootParticleSystemEffect = transform.Find("ShootParticleSystem").GetComponent<ParticleSystem>();

        EndReload();
        SetSprites(mirrored);
    }

    private void SetSprites(bool mirrored) {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = Type.sprite;
        spriteRenderer.flipX = mirrored;
    }

    public bool IsAvilable() {
        if (isReloading || avilableShootTimer > Time.time) return false;

        if (currentRounds <= 0) {
            Reload();
            return false;
        }

        return true;
    }

    public virtual void Shoot() {
        currentRounds--;
        avilableShootTimer = Time.time + Type.shootTime;
        shootParticleSystemEffect.Play();
    }

    public void Reload() {
        isReloading = true;
        Invoke(nameof(EndReload), Type.reloadTime);
    }

    private void EndReload() {
        isReloading = false;
        currentRounds = Type.clipSize;
    }

    public void BulletHit(IDamageable damageable) {
        MapManager.Instance.BulletHit(damageable, Type.bulletType);
    }
}
