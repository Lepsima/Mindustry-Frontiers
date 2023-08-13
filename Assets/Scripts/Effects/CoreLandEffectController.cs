using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CoreLandEffectController : MonoBehaviour {
    public ParticleSystem thruster1;
    public ParticleSystem thruster2;
    public ParticleSystem thruster3;
    public ParticleSystem thruster4;
    public ParticleSystem crashEffect;

    public void OnEnable() {
        thruster1.Play();
        thruster2.Play();
        thruster3.Play();
        thruster4.Play();
        Invoke(nameof(PlayCrashEffect), 4.8f);
    }

    public void PlayCrashEffect() {
        crashEffect.Play();
    }

    public void OnDisable() {
        thruster1.Stop();
        thruster2.Stop();
        thruster3.Stop();
        thruster4.Stop();
    }
}