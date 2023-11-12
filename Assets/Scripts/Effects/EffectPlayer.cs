using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Assets;
using Frontiers.Content.VisualEffects;
using Frontiers.Pooling;
using Newtonsoft.Json;

namespace Frontiers.Content.VisualEffects {
    
    public class Effect {
        public GameObject prefab;
        public string name;
        public short id;

        public Effect(string name) {
            this.name = name;
            prefab = AssetLoader.GetPrefab(name);
            EffectHandler.Handle(this);
        }
    }

    public static class EffectHandler {
        public static Dictionary<string, Effect> loadedEffects = new();

        public static void Handle(Effect effect) {
            effect.id = (short)loadedEffects.Count;
            loadedEffects.Add(effect.name, effect);
        }
    }

    public static class Effects {
        public static Effect 
            build, bulletHit, casing, despawn, smallExplosion, explosion, 
            bigExplosion, hitSmoke, muzzle, takeoff, waterDeviation, rcs, 
            weldSparks, craftSmoke, smeltSmoke, rockSparks, combustion, 
            factorySmoke, smeltTop, smokeMuzzle;

        public static void Load() {
            build = new("buildFX"); // Block placed, glow particles
            bulletHit = new("bulletHitFX"); // Bullet hitting entity, smoke and sparks
            casing = new("casingFX"); // Casing ejection, single particle
            despawn = new("despawnFX"); // Small shockwave
            smallExplosion = new("smallExplosionFX"); // Small explosion, smoke, shockwave and sparks
            explosion = new("explosionFX"); // Explosion, smoke, shockwave and sparks
            bigExplosion = new("bigExplosionFX"); // Big explosion, reverse shockwave, smoke, shockwave and sparks
            hitSmoke = new("hitSmokeFX"); // Looping smoke with light
            muzzle = new("muzzleFX"); // Muzze shoot with flash, smoke and sparks
            takeoff = new("takeoffFX"); // Smoke particles
            waterDeviation = new("waterDeviationFX"); // Water colored particles ejected from 2 sides
            rcs = new("rcsFX"); // Small jet pulse of white smoke particles
            weldSparks = new("weldSparkFX"); // Looping sparks
            craftSmoke = new("craftSmokeFX"); // Small amount of white smoke particles
            smeltSmoke = new("smeltSmokeFX"); // Small amount of long lasting white smoke particles
            rockSparks = new("rockSparksFX"); // Sparks and brown colored smoke
            combustion = new("combustionFX"); // Small smoke explosion
            factorySmoke = new("factorySmokeFX"); // "Isometric" looping smoke
            smeltTop = new("smeltTopFX"); // Custom effect for smelters
            smokeMuzzle = new("smokeMuzzleFX"); // Muzze effect without flash
        }
    }
}

public static class EffectPlayer {
    public static void PlayEffect(Effect effect, Vector2 position, float size) {
        PlayEffect(effect, position, Quaternion.identity, size);
    }

    public static void PlayEffect(Effect effect, Vector2 position, Quaternion rotation, float size) {
        if (effect == null) return;
        Transform transfrom = Object.Instantiate(effect.prefab, position, rotation, null).transform;

        transfrom.SetPositionAndRotation(position, rotation);
        transfrom.localScale = size * Vector3.one;

        ParticleSystem particleSystem = transfrom.GetComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particleSystem.main;
        main.stopAction = ParticleSystemStopAction.Destroy;

        particleSystem.Play();
    }

    public static ParticleSystem CreateEffect(this Transform parent, Effect effect, Vector2 position, Quaternion rotation, float localSize = 1f) {
        if (effect == null) return null;
        GameObject instance = Object.Instantiate(effect.prefab, Vector3.zero, Quaternion.identity, parent);

        instance.transform.SetLocalPositionAndRotation(position, rotation);
        instance.transform.localScale = localSize * Vector3.one;

        return instance.GetComponent<ParticleSystem>();
    }
}