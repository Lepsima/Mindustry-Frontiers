using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Content.SoundEffects;

public class CopterUnit : AircraftUnit {
    public new CopterUnitType Type { get => (CopterUnitType)base.Type; protected set => base.Type = value; }
    public Rotor[] rotors;
    public float maxRotorOutput;
    public float wreckSpinVelocity = 0f;

    protected override void CreateTransforms() {
        base.CreateTransforms();
        rotors = new Rotor[Type.rotors.Length];

        for (int i = 0; i < Type.rotors.Length; i++) {
            Rotator rotorData = Type.rotors[i];
            Rotor rotor = new(transform, rotorData, "Units");
            rotors[i] = rotor;
        }
    }

    protected override void Update() {
        base.Update();
        UpdateRotors(isLanded ? -1f : 1f);
    }

    protected override void WreckBehaviour() {
        wreckSpinVelocity = Mathf.Clamp(Type.wreckSpinAccel * Time.deltaTime + wreckSpinVelocity, 0, Type.wreckSpinMax);
        transform.eulerAngles += new Vector3(0, 0, wreckSpinVelocity * Time.deltaTime);
    }

    public void UpdateRotors(float power) {
        power = (power - 0.5f) * 2f; // Allow negative values
        float output = 0f;

        for (int i = 0; i < rotors.Length; i++) { 
            rotors[i].Update(power);
            output += rotors[i].Output();
        }

        output /= rotors.Length;
        maxRotorOutput = output;
        audioSource.pitch = maxRotorOutput;
    }

    public override float CalculateEnginePower() {
        return Mathf.Min(maxRotorOutput, base.CalculateEnginePower());
    }

    public override float CalculateLiftPower() {
        return Mathf.Min(maxRotorOutput, base.CalculateLiftPower());
    }

    public override void OnTakeOff() {
        base.OnTakeOff();

        AudioClip clip = Sounds.helicopterTakeoff.clip;
        audioSource.PlayOneShot(clip);

        Invoke(nameof(PlayLoop), takeoffTime);
    }

    private void PlayLoop() {
        audioSource.PlayOneShot(Sounds.helicopterTransition.clip);
        audioSource.loop = true;
        audioSource.clip = Sounds.helicopterLoop.clip;
        audioSource.Play();
    }
}