using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Assets;
using Frontiers.Pooling;

public static class Effect {
    public static Dictionary<string, GameObject> prefabDictionary = new();

    public static void PlayEffect(string name, Vector2 position, float size) {
        PlayEffect(name, position, Quaternion.identity, size);
    }

    public static void PlayEffect(string name, Vector2 position, Quaternion rotation, float size) {
        if (name == "") return;
        if (!prefabDictionary.ContainsKey(name)) prefabDictionary.Add(name, AssetLoader.GetPrefab(name));

        GameObject prefab = prefabDictionary[name];
        Transform transfrom = Object.Instantiate(prefab, position, rotation, null).transform;

        transfrom.SetPositionAndRotation(position, rotation);
        transfrom.localScale = size * Vector3.one;

        ParticleSystem particleSystem = transfrom.GetComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particleSystem.main;
        main.stopAction = ParticleSystemStopAction.Destroy;

        particleSystem.Play();
    }

    public static ParticleSystem CreateEffect(this Transform parent, string name, Vector2 position, Quaternion rotation, float localSize = 1f) {
        if (name == "") return null;
        if (!prefabDictionary.ContainsKey(name)) prefabDictionary.Add(name, AssetLoader.GetPrefab(name));

        GameObject prefab = prefabDictionary[name];
        GameObject instance = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);

        instance.transform.SetLocalPositionAndRotation(position, rotation);
        instance.transform.localScale = localSize * Vector3.one;

        return instance.GetComponent<ParticleSystem>();
    }
}