using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Teams;

public class Bullet {
    public BulletType Type;
    public Weapon weapon;

    public Transform transform;
    public int mask;

    public Bullet(Weapon weapon, Transform transform) {
        Type = weapon.Type.bulletType;
        this.weapon = weapon;
        this.transform = transform;

        mask = TeamUtilities.GetEnemyTeamMask(weapon.parentEntity.GetTeam());
        weapon.StartCoroutine(Type.BulletBehaviour(this));
    }

    public void OnBulletCollision() {
        EffectManager.PlayEffect(Type.hitFX, transform.position, 1f);

        Collider2D collider = Physics2D.OverlapCircle(transform.position, Type.size, mask);
        if (collider.transform.TryGetComponent(out Entity entity)) Client.BulletHit(entity, Type);
    }

    public void Return() {
        Type.pool.Return(transform.gameObject);
    }
}

public static class BulletManager {
    public static Bullet ShootBullet(this Weapon weapon, Vector2 position, float rotation) {
        Transform transform = weapon.Type.bulletType.pool.Take().transform;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, rotation));

        return new(weapon, transform.transform);
    }
}