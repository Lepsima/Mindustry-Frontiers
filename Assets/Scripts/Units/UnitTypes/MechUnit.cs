using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public class MechUnit : Unit {
    public new MechUnitType Type { get => (MechUnitType)base.Type; protected set => base.Type = value; }

    protected Transform baseTransform, leftLegTransform, rightLegTransform;
    protected float walkTime = 0f, legSpeed;
    protected Vector2 lastPosition;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        legSpeed = Type.velocityCap / Type.legStepDistance * 0.5f;
    }

    protected override void Update() {
        base.Update();

        float walkedDistance = Vector2.Distance(lastPosition, transform.position);
        walkTime += walkedDistance / Type.velocityCap;
        lastPosition = transform.position;
        HandleLegs();
    }

    protected override void FixedUpdate() {
        base.FixedUpdate();
    }

    protected override void CreateTransforms() {
        baseTransform = new GameObject("Mech-base", typeof(SpriteRenderer)).transform;
        baseTransform.parent = spriteHolder;
        baseTransform.localPosition = Vector3.zero;
        baseTransform.localRotation = Quaternion.identity;
        baseTransform.localScale = Vector3.one;

        rightLegTransform = new GameObject("Mech-r-leg", typeof(SpriteRenderer)).transform;
        rightLegTransform.parent = baseTransform;
        rightLegTransform.localPosition = Vector3.zero;
        rightLegTransform.localRotation = Quaternion.identity;
        rightLegTransform.localScale = Vector3.one;

        leftLegTransform = new GameObject("Mech-l-leg", typeof(SpriteRenderer)).transform;
        leftLegTransform.parent = baseTransform;
        leftLegTransform.localPosition = Vector3.zero;
        leftLegTransform.localRotation = Quaternion.identity;
        leftLegTransform.localScale = Vector3.one;
    }

    protected override void SetSprites() {
        base.SetSprites();

        SetOptionalSprite(baseTransform, Type.baseSprite);
        SetOptionalSprite(rightLegTransform, Type.legSprite);
        SpriteRenderer leftSpriteRenderer = SetOptionalSprite(leftLegTransform, Type.legSprite);

        leftSpriteRenderer.flipX = true;
    }

    protected void HandleLegs() {
        baseTransform.up = (Vector3)GetBehaviourPosition() - baseTransform.position;

        float leftLegY = Mathf.Sin(walkTime * legSpeed) * Type.legStepDistance;
        leftLegTransform.localPosition = new Vector3(0, leftLegY, 0);

        float rightLegY = Mathf.Sin(walkTime * legSpeed + Mathf.PI) * Type.legStepDistance;
        rightLegTransform.localPosition = new Vector3(0, rightLegY, 0);
    }

    public override void HandleHeight() {

    }

    public override void Tilt(float value) {

    }

    public override bool IsFleeing() {
        return false;
    }
}
