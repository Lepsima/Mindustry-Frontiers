using System;
using UnityEngine;

public class ThrusterAnimationTrigger : MonoBehaviour {
    public ParticleSystem thruster1;
    public ParticleSystem thruster2;
    public ParticleSystem thruster3;
    public ParticleSystem thruster4;

    public ParticleSystem crashEffect;

    public EventHandler OnAnimationEnd;

    public float duration = 4.8f;

    public void SetBlockSize(int size) {
        float halfSize = size * 0.5f;
        thruster1.transform.SetLocalPositionAndRotation(new Vector3(halfSize, 0), Quaternion.Euler(0, 0, -90));
        thruster2.transform.SetLocalPositionAndRotation(new Vector3(0, -halfSize), Quaternion.Euler(0, 0, 180));
        thruster3.transform.SetLocalPositionAndRotation(new Vector3(-halfSize, 0), Quaternion.Euler(0, 0, 90));
        thruster4.transform.SetLocalPositionAndRotation(new Vector3(0, halfSize), Quaternion.Euler(0, 0, 0));
    }

    public void OnEnable() {
        thruster1.Play();
        thruster2.Play();
        thruster3.Play();
        thruster4.Play();
        Invoke(nameof(PlayCrashEffect), duration);
    }

    public void PlayCrashEffect() {
        crashEffect.Play();
        OnAnimationEnd?.Invoke(this, EventArgs.Empty);
    }

    public void OnDisable() {
        thruster1.Stop();
        thruster2.Stop();
        thruster3.Stop();
        thruster4.Stop();
        crashEffect.Stop();
    }
}