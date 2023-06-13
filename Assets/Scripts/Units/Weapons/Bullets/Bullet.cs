using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Teams;

public class Bullet {
    protected BulletType Type;
    protected Weapon weapon;

    protected Transform transform;
    protected int mask;

    public Bullet(BulletType Type, Weapon weapon, Transform transform) {
        this.Type = Type;
        this.transform = transform;

        mask = TeamUtilities.GetEnemyTeamMask(weapon.parentEntity.GetTeam());
        weapon.StartCoroutine(Type.BulletBehaviour(this, transform, mask));
    }

    public void OnBulletCollision() {
        Collider2D collider = Physics2D.OverlapCircle(transform.position, Type.size, mask);
        if (collider.transform.TryGetComponent(out Entity entity)) Client.BulletHit(entity, Type);
    }

    public void Return() {
        Type.pool.Return(transform.gameObject);
    }
}

public static class BulletManager {
    public static Bullet ShootBullet(this Weapon weapon, BulletType Type, Vector2 position, float rotation) {
        Transform transform = Type.pool.Take().transform;
        Bullet bullet = new(Type, weapon, transform.transform);

        transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, rotation));
        return bullet;
    }
}