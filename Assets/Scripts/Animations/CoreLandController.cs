using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[ExecuteInEditMode]
public class CoreLandController : MonoBehaviour {
    public GameObject parent;

    public ParticleSystem thruster1;
    public ParticleSystem thruster2;
    public ParticleSystem thruster3;
    public ParticleSystem thruster4;

    public ParticleSystem crashEffect;

    public EventHandler OnAnimationEnd;

    public float duration = 4.8f;

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
        //Destroy(parent, 5f);
    }

    public void OnDisable() {
        thruster1.Stop();
        thruster2.Stop();
        thruster3.Stop();
        thruster4.Stop();
        crashEffect.Stop();
    }
}