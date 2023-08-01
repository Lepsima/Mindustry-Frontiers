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

    public class Rotor {
        public RotorBlade[] blades;

        public Transform transform;
        public UnitRotor Type;

        public float velocity;
        public float position;

        public Rotor(Transform parent, UnitRotor Type) {
            this.Type = Type;
            velocity = Type.velocity;

            // Create rotor top
            transform = new GameObject("Rotor-Top", typeof(SpriteRenderer)).transform;
            transform.parent = parent;
            transform.localPosition = Type.offset;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // Set the top sprite
            SpriteRenderer topSpriteRenderer = transform.GetComponent<SpriteRenderer>();
            topSpriteRenderer.sprite = Type.topSprite;
            topSpriteRenderer.sortingLayerName = "Units";
            topSpriteRenderer.sortingOrder = 12;

            // Create all the blades
            blades = new RotorBlade[Type.blades.Length];
            for (int i = 0; i < Type.blades.Length; i++) blades[i] = new(transform, Type, Type.blades[i]);
        }

        public void Update(float power, float deltaTime) {
            float deltaVel = Mathf.Sign(power) * Type.velocityIncrease * deltaTime;
            velocity = Mathf.Clamp(velocity + deltaVel, 0, Type.velocity);
            position += velocity * deltaTime;

            // Prevent large numbers
            if (position > 1f) position--;

            // Update each blade
            for (int i = 0; i < blades.Length; i++) blades[i].Update(position, velocity);
        }

        public float Output() {
            return velocity / Type.velocity;
        }

        public class RotorBlade {
            public SpriteRenderer spriteRenderer;
            public SpriteRenderer blurSpriteRenderer;

            public Transform transform;
            public UnitRotor Type;

            public float modifier;
            public float offset;

            public RotorBlade(Transform parent, UnitRotor Type, UnitRotorBlade rotorBladeType) {
                this.Type = Type;
                modifier = rotorBladeType.counterClockwise ? -360f : 360f;
                offset = rotorBladeType.offset;

                // Create the rotor transform
                transform = new GameObject("Rotor", typeof(SpriteRenderer)).transform;
                transform.parent = parent;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                // Set the rotor sprite
                spriteRenderer = transform.GetComponent<SpriteRenderer>();
                spriteRenderer.sprite = Type.sprite;
                spriteRenderer.sortingLayerName = "Units";
                spriteRenderer.sortingOrder = 10;

                // Create the rotor blur transform
                Transform blurTransform = new GameObject("Rotor-blur", typeof(SpriteRenderer)).transform;
                blurTransform.parent = transform;
                blurTransform.localPosition = Vector3.zero;
                blurTransform.localRotation = Quaternion.identity;
                blurTransform.localScale = Vector3.one;

                // Set the rotor blur transform
                blurSpriteRenderer = blurTransform.GetComponent<SpriteRenderer>();
                blurSpriteRenderer.sprite = Type.blurSprite;
                blurSpriteRenderer.sortingLayerName = "Units";
                blurSpriteRenderer.sortingOrder = 11;
            }

            public void Update(float position, float velocity) {
                // Update rotation
                transform.localEulerAngles = new Vector3(0f, 0f, position * modifier + offset);

                // Update sprite blur
                float blurValue = Type.BlurValue(velocity);
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f - blurValue);
                blurSpriteRenderer.color = new Color(1f, 1f, 1f, blurValue);
            }
        }
    }

    protected override void CreateTransforms() {
        base.CreateTransforms();
        rotors = new Rotor[Type.rotors.Length];

        for (int i = 0; i < Type.rotors.Length; i++) {
            UnitRotor rotorData = Type.rotors[i];
            Rotor rotor = new(transform, rotorData);
            rotors[i] = rotor;
        }
    }

    protected override void Update() {
        base.Update();

        float power = isLanded ? -1f : 1f;
        UpdateRotors(power, Time.deltaTime);
    }

    public void UpdateRotors(float power, float deltaTime) {
        power = (power - 0.5f) * 2f; // Allow negative values
        float output = 0f;

        for (int i = 0; i < rotors.Length; i++) { 
            rotors[i].Update(power, deltaTime);
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