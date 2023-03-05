using Frontiers.Content;
using Frontiers.Teams;
using Frontiers.Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastWeapon : Weapon {
    private GameObject trailPrefab;

    public override void Set(LoadableContent parentEntity, WeaponType weaponType, bool mirrored = false, float timeCode = 0, byte teamCode = 0) {
        base.Set(parentEntity, weaponType, mirrored, timeCode, teamCode);

        if (weaponType.bulletType.bulletClass != BulletClass.Instant) Debug.LogWarning("Only " + BulletClass.Instant + " is compatible with" + typeof(RaycastWeapon));
        trailPrefab = Assets.GetPrefab("tPref");
        shootParticleSystemEffect.transform.localPosition = Type.shootOffset;
    }

    public override void Shoot() {
        base.Shoot();
        Vector2 hitPoint = transform.position + (Vector3)Type.shootOffset + (transform.up * Type.Range);
        RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position + Type.shootOffset, transform.up, Type.Range, TeamUtilities.GetEnemyTeamMask(teamCode));

        if (hit) {
            if (hit.transform.TryGetComponent(out IDamageable damageable)) BulletHit(damageable);
            hitPoint = hit.point;

            //SpawnDecalFX hit.point hit.normal
        }

        HitscanFX(hitPoint);
    }

    public void HitscanFX(Vector2 hit) {
        TrailRenderer trail = Instantiate(trailPrefab, transform.position + (Vector3)Type.shootOffset, transform.rotation).GetComponent<TrailRenderer>();
        StartCoroutine(SpawnTrail(trail, hit, Type.bulletType.speed));
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector2 hitPos, float speed) {
        Vector2 startPosition = trail.transform.position;
        //trail.gameObject.layer = GetTeamLayer(true);

        float distance = Vector2.Distance(startPosition, hitPos);
        float startingDistance = distance;

        while (distance > 0) {
            trail.transform.position = Vector2.Lerp(startPosition, hitPos, 1 - (distance / startingDistance));
            distance -= Time.deltaTime * speed;

            yield return null;
        }

        trail.transform.position = hitPos;
        Destroy(trail.gameObject);
    }
}