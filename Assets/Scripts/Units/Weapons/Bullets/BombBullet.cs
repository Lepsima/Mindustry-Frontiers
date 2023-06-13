using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Teams;

public class BombBullet : Bullet {
    public new BombBulletType Type { get => (BombBulletType)base.Type; protected set => base.Type = value; }

    protected BombBullet(BombBulletType Type, Weapon weapon, Transform transform) : base(Type, weapon, transform) {

    }

    protected override IEnumerator BulletTracer() {
        TrailRenderer trail = transform.GetComponent<TrailRenderer>();
        Vector2 startPosition = transform.position;
        Vector2 hitPos = transform.up * Type.Range;

        float distance = Vector2.Distance(startPosition, hitPos);
        float startingDistance = distance;
        float destroyTime = Time.time + trail.time + 1f + (distance / Type.velocity);

        while (distance > 0) {
            transform.position = Vector2.Lerp(startPosition, hitPos, 1 - (distance / startingDistance));
            distance -= Time.deltaTime * Type.velocity;

            if (Damage(transform.position)) {
                destroyTime = Time.time + trail.time + 1f;
                break;
            }

            yield return null;
        }

        ParticleSystem hitEffect = transform.GetComponent<ParticleSystem>();
        hitEffect.Play();

        while (destroyTime > Time.time) yield return null;

        Type.pool.Return(transform.gameObject);
    }
}