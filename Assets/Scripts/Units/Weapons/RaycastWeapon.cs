using Frontiers.Content;
using Frontiers.Teams;
using Frontiers.Assets;
using Frontiers.Pooling;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastWeapon : Weapon {
    public static GameObjectPool tracerGameObjectPool;
    private List<GameObject> activeBullets = new List<GameObject>();

    public override void Set(Entity parentEntity, int weaponID, WeaponType weaponType, bool mirrored = false, bool onTop = false) {
        base.Set(parentEntity, weaponID, weaponType, mirrored, onTop);

        if (weaponType.bulletType.bulletClass != BulletClass.instant) Debug.LogWarning("Only " + BulletClass.instant + " is compatible with" + typeof(RaycastWeapon));
        shootParticleSystemEffect.transform.localPosition = Type.shootOffset;
    }

    public override void Shoot() {
        base.Shoot();
        Vector2 direction = transform.up + (Vector3)GetSpread();
        Vector2 originPoint = transform.position + GetLocalOffset(GetOffset());
        shootParticleSystemEffect.transform.position = originPoint;

        Vector2 hitPoint = direction * Type.Range + originPoint;
        //RaycastHit2D hit = Physics2D.Raycast(originPoint, transform.up, Type.Range, TeamUtilities.GetEnemyTeamMask(teamCode));

        HitscanFX(hitPoint);
    }

    public void HitscanFX(Vector2 hit) {
        StartCoroutine(SpawnTracer(hit, Type.bulletType.velocity));
    }

    private Vector3 GetLocalOffset(Vector2 offset) {
        return (offset.x * transform.right) + (offset.y * transform.up);
    }

    private IEnumerator SpawnTracer(Vector2 hitPos, float speed) {
        GameObject tracerGameObject = tracerGameObjectPool.Take();
        activeBullets.Add(tracerGameObject);

        tracerGameObject.transform.SetPositionAndRotation(transform.position + GetLocalOffset(GetOffset()), transform.rotation);

        TrailRenderer trail = tracerGameObject.GetComponent<TrailRenderer>();
        Vector2 startPosition = trail.transform.position;

        float distance = Vector2.Distance(startPosition, hitPos);
        float startingDistance = distance;

        trail.transform.up = -((Vector3)hitPos - trail.transform.position);
        float destroyTime = Time.time + trail.time + 1f + (distance / speed);

        while (distance > 0) {
            trail.transform.position = Vector2.Lerp(startPosition, hitPos, 1 - (distance / startingDistance));
            distance -= Time.deltaTime * speed;

            if (Damage(trail.transform.position)) {
                destroyTime = Time.time + trail.time + 1f;
                break;
            }

            yield return null;
        }

        ParticleSystem hitEffect = trail.GetComponent<ParticleSystem>();
        hitEffect.Play();

        while (destroyTime > Time.time) yield return null;

        activeBullets.Remove(tracerGameObject);
        tracerGameObjectPool.Return(tracerGameObject);
    }

    public bool Damage(Vector2 point) {
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(point, 0.05f, TeamUtilities.GetEnemyTeamMask(parentEntity.GetTeam()))) {
            if (collider.transform.TryGetComponent(out Entity entity)) {
                BulletHit(entity);
                return true;
            }
        }

        return false;
    }

    public void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;

        StopAllCoroutines();

        int amount = activeBullets.Count;
        for (int i = amount - 1; i >= 0; i--) tracerGameObjectPool.Return(activeBullets[i]);
    }
}