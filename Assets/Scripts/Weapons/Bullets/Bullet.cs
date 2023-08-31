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

    Vector2 startPosition, hitPosition;
    float distance, startingDistance;

    public bool active;

    public Bullet(Weapon weapon, Transform transform) {
        Type = weapon.Type.bulletType;

        this.weapon = weapon;
        this.transform = transform;

        mask = TeamUtilities.GetEnemyTeamMask(weapon.parentEntity.GetTeam());

        startPosition = transform.position;
        hitPosition = (Vector2)transform.up * Type.Range + startPosition;

        distance = Vector2.Distance(startPosition, hitPosition);
        startingDistance = distance;

        if (transform.TryGetComponent(out TrailRenderer trail)) trail.Clear();
        active = true;
    }

    public virtual void Update() {
        if (!active) Return();

        transform.position = Vector2.Lerp(startPosition, hitPosition, 1 - (distance / startingDistance));
        distance -= Time.deltaTime * Type.velocity;

        if (Physics2D.OverlapCircle(transform.position, Type.size, mask)) BulletCollision();
        else if (ShouldDespawn()) Despawn();
    }

    public void BulletCollision() {
        EffectPlayer.PlayEffect(Type.hitFX, transform.position, 1f);

        Collider2D collider = Physics2D.OverlapCircle(transform.position, Type.size, mask);
        if (collider.transform.TryGetComponent(out Entity entity)) Client.BulletHit(entity, Type);

        Return();
    }

    public virtual void Despawn() {
        EffectPlayer.PlayEffect(Type.despawnFX, transform.position, 1f);
        Return();
    }

    protected virtual bool ShouldDespawn() {
        return distance <= 0;
    }

    public void Return() {
        active = false;
        BulletManager.ReturnBullet(this);
    }

    public Vector2 GetPosition() {
        return transform.position;
    }

    public byte GetTeam() {
        return weapon.parentEntity.GetTeam();
    }
}

public class MissileBullet : Bullet {
    Transform target;
    readonly byte enemyTeam;
    readonly float lifeTime;

    public MissileBullet(Weapon weapon, Transform transform) : base(weapon, transform) {
        lifeTime = Time.time + Type.lifeTime;

        // The target the missile should follow
        enemyTeam = TeamUtilities.GetEnemyTeam(GetTeam());
        target = MapManager.Map.GetClosestEntity(transform.position, enemyTeam).transform;
    }

    public override void Update() {
        // Try to update target
        if (!target && (Type as MissileBulletType).canUpdateTarget) {
            Entity newTarget = MapManager.Map.GetClosestEntity(transform.position, enemyTeam);
            if (newTarget) target = newTarget.transform;
        }

        // Move forward
        transform.position += Time.deltaTime * Type.velocity * transform.up;

        // If has target rotate towards it
        if (target) {
            Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, target.position - transform.position);
            desiredRotation = Quaternion.Euler(0, 0, desiredRotation.eulerAngles.z);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, ((MissileBulletType)Type).homingStrength * Time.fixedDeltaTime);
        }

        // Check for collision
        if (Physics2D.OverlapCircle(transform.position, Type.size, mask)) BulletCollision();
        else if (ShouldDespawn()) {
            if ((Type as MissileBulletType).explodeOnDespawn) Client.Explosion(Type, GetPosition(), mask);
            Despawn();
        }
    }

    protected override bool ShouldDespawn() {
        return Time.time >= lifeTime;
    }
}

public class BombBullet : Bullet {
    float height;
    readonly float maxHeight;
    readonly Transform shadow;

    public BombBullet(Weapon weapon, Transform transform) : base(weapon, transform) {
        shadow = transform.GetChild(0);

        // Bomb bullets won't work on block weapons so no need to check for that
        height = ((Unit)weapon.parentEntity).GetHeight();
        maxHeight = ((Unit)weapon.parentEntity).Type.groundHeight;
    }

    public override void Update() {
        height -= ((BombBulletType)Type).fallVelocity * Time.deltaTime;
        transform.localScale = Vector3.one * Mathf.Lerp(((BombBulletType)Type).initialSize, ((BombBulletType)Type).finalSize, 1 - height / maxHeight);
        shadow.position = -Vector3.one * (height * 0.2f) + transform.position;

        if (ShouldDespawn()) {
            Client.Explosion(Type, GetPosition(), mask);
            Despawn();
        }
    }

    protected override bool ShouldDespawn() {
        return height <= 0f;
    }
}

public static class BulletManager {
    public static List<Bullet> allBullets = new();

    public static Bullet ShootBullet(this Weapon weapon, Vector2 position, float rotation) {
        Transform transform = weapon.Type.bulletType.pool.Take().transform;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, rotation));

        Bullet bullet = weapon.Type.bulletType.NewBullet(weapon, transform);
        allBullets.Add(bullet);
        return bullet;
    }

    public static void ReturnBullet(Bullet bullet) {
        bullet.Type.pool.Return(bullet.transform.gameObject);
        allBullets.Remove(bullet);
    }

    public static void UpdateBullets() {
        for (int i = allBullets.Count - 1; i >= 0; i--) allBullets[i].Update();
    }
}