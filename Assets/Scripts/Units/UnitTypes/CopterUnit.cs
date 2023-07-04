using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;

public class CopterUnit : AircraftUnit {
    public new CopterUnitType Type { get => (CopterUnitType)base.Type; protected set => base.Type = value; }
    public Rotor[] rotors;
    public float maxRotorOutput;

    public class Rotor {
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer blurSpriteRenderer;

        public Transform transform;
        public UnitRotor Type;

        public float velocity;

        public Rotor(Transform parent, UnitRotor Type) {
            this.Type = Type;

            // Create rotor top transform
            Transform rotorTop = new GameObject("Rotor-Top", typeof(SpriteRenderer)).transform;
            rotorTop.parent = parent;
            rotorTop.localPosition = Type.offset;
            rotorTop.localRotation = Quaternion.identity;
            rotorTop.localScale = Vector3.one;

            // Set the top sprite
            SpriteRenderer topSpriteRenderer = rotorTop.GetComponent<SpriteRenderer>();
            topSpriteRenderer.sprite = Type.topSprite;
            topSpriteRenderer.sortingLayerName = "Units";
            topSpriteRenderer.sortingOrder = 12;

            // Create the rotor transform
            transform = new GameObject("Rotor", typeof(SpriteRenderer)).transform;
            transform.parent = rotorTop;
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

        public void Update(float power, float deltaTime) {
            // Change velocity
            float deltaVel = Mathf.Sign(power) * Type.velocityIncrease * deltaTime;
            velocity = Mathf.Clamp(velocity + deltaVel, 0, Type.velocity);

            // Rotate
            transform.localEulerAngles += new Vector3(0f, 0f, velocity * deltaTime * 360f);

            // Update sprite blur
            float blurValue = Type.BlurValue(velocity);
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f - blurValue);
            blurSpriteRenderer.color = new Color(1f, 1f, 1f, blurValue);
        }

        public float Output() {
            return velocity / Type.velocity;
        }
    }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
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
    }

    public override float CalculateEnginePower() {
        return Mathf.Min(maxRotorOutput, base.CalculateEnginePower());
    }
}