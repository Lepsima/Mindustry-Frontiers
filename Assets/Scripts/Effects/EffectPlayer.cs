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
        public static Effect build, bulletHit, casing, despawn, explosion, hitSmoke, muzzle, smallExplosion, takeoff, waterDeviation, rcs, weldSparks;

        public static void Load() {
            build = new("buildFX");
            bulletHit = new("bulletHitFX");
            casing = new("casingFX");
            despawn = new("despawnFX");
            explosion = new("explosionFX");
            hitSmoke = new("hitSmokeFX");
            muzzle = new("muzzleFX");
            smallExplosion = new("smallExplosionFX");
            takeoff = new("takeoffFX");
            waterDeviation = new("waterDeviationFX");
            rcs = new("rcsFX");
            weldSparks = new("weldSparkFX");
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