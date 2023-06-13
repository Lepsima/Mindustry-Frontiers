using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Assets;

public static class EffectManager {
    public static Dictionary<string, ParticleSystem> effectGameObjects = new();

    public static void PlayEffect(string name, Vector2 position, float size) {
        PlayEffect(name, position, Quaternion.identity, size);
    }

    public static void PlayEffect(string name, Vector2 position, Quaternion rotation, float size) {
        if (name == "") return;
        if (!effectGameObjects.ContainsKey(name)) InstantiateNew(name);

        ParticleSystem instance = effectGameObjects[name];
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = size * Vector2.one;

        instance.Play();
    }

    public static void InstantiateNew(string name) {
        GameObject prefab = AssetLoader.GetPrefab(name);
        GameObject instance = Object.Instantiate(prefab);
        effectGameObjects.Add(name, instance.GetComponent<ParticleSystem>());
    }
}