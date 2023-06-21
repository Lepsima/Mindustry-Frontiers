using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Content.Maps;

public class MechUnit : Unit {
    public new MechUnitType Type { get => (MechUnitType)base.Type; protected set => base.Type = value; }

    protected Transform baseTransform, leftLegTransform, rightLegTransform;
    protected SpriteRenderer leftLegSpriteRenderer, rightLegSpriteRenderer;
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
        baseTransform.parent = transform;

        baseTransform.localPosition = Vector3.zero;
        baseTransform.localRotation = Quaternion.identity;
        baseTransform.localScale = Vector3.one;

        rightLegTransform = new GameObject("Mech-r-leg", typeof(SpriteRenderer)).transform;
        rightLegTransform.parent = baseTransform;

        rightLegTransform.localPosition = Vector3.zero;
        rightLegTransform.localRotation = Quaternion.identity;
        rightLegTransform.localScale = Vector3.one;

        rightLegSpriteRenderer = rightLegTransform.GetComponent<SpriteRenderer>();

        leftLegTransform = new GameObject("Mech-l-leg", typeof(SpriteRenderer)).transform;
        leftLegTransform.parent = baseTransform;

        leftLegTransform.localPosition = Vector3.zero;
        leftLegTransform.localRotation = Quaternion.identity;
        leftLegTransform.localScale = Vector3.one;

        leftLegSpriteRenderer = leftLegTransform.GetComponent<SpriteRenderer>();
    }

    protected override void SetSprites() {
        base.SetSprites();
        SetOptionalSprite(baseTransform, Type.baseSprite);
        SetOptionalSprite(rightLegTransform, Type.legSprite);
        SpriteRenderer leftSpriteRenderer = SetOptionalSprite(leftLegTransform, Type.legSprite);

        leftSpriteRenderer.flipX = true;
    }

    protected void HandleLegs() {
        Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, ((Vector3)GetBehaviourPosition() - baseTransform.position).normalized);
        desiredRotation = Quaternion.Euler(0, 0, desiredRotation.eulerAngles.z);

        float speed = Type.baseRotationSpeed * Time.fixedDeltaTime;
        baseTransform.rotation = Quaternion.RotateTowards(baseTransform.rotation, desiredRotation, speed);

        float leftLegY = Mathf.Sin(walkTime * legSpeed) * Type.legStepDistance;
        float rightLegY = Mathf.Sin(walkTime * legSpeed + Mathf.PI) * Type.legStepDistance;

        float leftLegT = Mathf.Sin(walkTime * legSpeed + Mathf.PI * 0.5f);
        float rightLegT = Mathf.Sin(walkTime * legSpeed + Mathf.PI * 1.5f);

        leftLegTransform.localPosition = new Vector3(0, leftLegY, 0);
        leftLegSpriteRenderer.color = Color.Lerp(Color.gray, Color.white, Mathf.Max(leftLegT, 0));

        rightLegTransform.localPosition = new Vector3(0, rightLegY, 0);
        rightLegSpriteRenderer.color = Color.Lerp(Color.gray, Color.white, Mathf.Max(rightLegT, 0));

        float frontSway = Mathf.Sin(walkTime * legSpeed * 2f) * Type.frontSway;
        float sideSway = Mathf.Sin(walkTime * legSpeed) * Type.sideSway;
        spriteHolder.transform.localPosition = new Vector3(sideSway, frontSway, 0);
    }

    public override TileType GetGroundTile() {
        Vector2 position = transform.position;
        if (!MapManager.Map.IsInBounds(position)) return null;
        return MapManager.Map.GetMapTileTypeAt(Map.MapLayer.Ground, position);
    }

    protected override void SetEffects() {
        
    }

    public override void HandleHeight() {

    }

    public override void Tilt(float value) {

    }

    public override bool IsFleeing() {
        return false;
    }
}
