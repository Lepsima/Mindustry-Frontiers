using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Assets;

public static class AmbientSoundHandler{
    public static Dictionary<string, AmbientSoundInstance> ambientSoundInstances = new();
    public static AudioListener audioListener;

    public static AmbientSoundInstance Get(string name) {
        if (!ambientSoundInstances.ContainsKey(name)) ambientSoundInstances.Add(name, new(name));
        return ambientSoundInstances[name];
    }
}

public class AmbientSoundInstance {
    bool enabled;

    public readonly Transform transform;
    readonly AudioSource audioSource;
    readonly AudioClip audioClip;

    public List<AmbientSoundEmmiter> emmiters = new();

    public AmbientSoundInstance(string name) {
        transform = new GameObject("Ambient sound: " + name, typeof(AudioSource)).transform;
        audioSource = transform.GetComponent<AudioSource>();
        audioClip = AssetLoader.GetAsset<AudioClip>(name);

        audioSource.clip = audioClip;
        audioSource.loop = true;
        audioSource.Stop();
    }
    
    public void Enable(bool value) {
        if (value == enabled) return;
        if (value) audioSource.Play();
        else audioSource.Stop();
        enabled = value;
    }

    public void UpdateDistance() {
        Enable(emmiters.Count != 0);
        float closest = 9999f;

        for(int i = 0; i < emmiters.Count; i++) {
            float distance = emmiters[i].Distance();
            if (distance < closest) closest = distance;
        }

        SetDistance(closest);
    }

    public void SetDistance(float distance) {
        transform.position = new Vector3(0f, 0f, distance);
    }
}

public class AmbientSoundEmmiter {
    public readonly Transform transform;
    readonly AmbientSoundInstance ambientSoundInstance;

    public AmbientSoundEmmiter(Transform transform, string name, bool enabled = true) {
        this.transform = transform;
        ambientSoundInstance = AmbientSoundHandler.Get(name);
        if (enabled) Enable();
    }

    public void Enable() {
        ambientSoundInstance.emmiters.Add(this);
    }

    public void Disable() {
        ambientSoundInstance.emmiters.Remove(this);
    }

    public float Distance() {
        return Vector2.Distance(transform.position, ambientSoundInstance.transform.position);
    }
}