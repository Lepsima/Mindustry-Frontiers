using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Teams;

public class DamageHandler : MonoBehaviour {

    public struct Hit {
        public float damage, buildingMultiplier;

        public Hit(float damage, float buildingMultiplier = 1f) {
            this.damage = damage;
            this.buildingMultiplier = buildingMultiplier;
        }

        public float Value(IDamageable damageable) {
            return damageable.IsBuilding() ? damage * buildingMultiplier : damage;
        }

        public bool IsValid() {
            return damage > 0f;
        }
    }

    public struct Area {
        public float innerDamage, outerDamage, maxRange, falloffRange, buildingMultiplier;

        public Area(float innerDamage, float outerDamage, float maxRange, float buildingMultiplier = 1f, float falloffRange = 0f) {
            this.innerDamage = innerDamage;
            this.outerDamage = outerDamage;
            this.maxRange = maxRange;
            this.falloffRange = falloffRange;
            this.buildingMultiplier = buildingMultiplier;
        }

        public float Value(IDamageable damageable, float distance) {
            float percent = (distance - falloffRange) / (maxRange - falloffRange);
            float raw = distance > falloffRange ? Mathf.Lerp(innerDamage, outerDamage, percent) : innerDamage;

            float mult = damageable.IsBuilding() ? buildingMultiplier : 1f;
            return raw * mult;
        }

        public bool IsValid() {
            return maxRange > 0f;
        }
    }

    public static void BulletHit(BulletType bulletType, Entity entity) {
        if (bulletType.Explodes()) {
            BulletExplode(bulletType, entity.GetPosition(), entity.GetTeam());

        } else if (entity.TryGetComponent(out IDamageable damageable)) {
            damageable.Damage(bulletType.damage);
        }
    }

    public static void BulletExplode(BulletType bulletType, Vector2 position, int mask) {
        AreaDamage(bulletType, position, mask);
    }

    public static void Damage(Hit hit, IDamageable damageable) {
        if (!hit.IsValid()) return;
        damageable.Damage(hit.Value(damageable));
    }

    public static void AreaDamage(BulletType bulletType, Vector2 position, int mask) {
        if (!bulletType.IsValid()) return;

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(position, bulletType.blastRadius, mask)) {

            if (collider.transform.TryGetComponent(out IDamageable damageable)) {

                float distance = Vector2.Distance(position, collider.transform.position);
                damageable.Damage(bulletType.Value(damageable, distance));
            }
        }
    }

    public static void AreaDamage(Area area, Vector2 position, int mask) {
        if (!area.IsValid()) return;

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(position, area.maxRange, mask)) {

            if (collider.transform.TryGetComponent(out IDamageable damageable)) {

                float distance = Vector2.Distance(position, collider.transform.position);
                damageable.Damage(area.Value(damageable, distance));
            }
        }
    }
}