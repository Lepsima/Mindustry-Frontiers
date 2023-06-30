using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Content.Maps;

public class MechUnit : Unit {
    public float randomNumberMultiplier = 0.5f;
    public float legSpeed;
    public new MechUnitType Type { get => (MechUnitType)base.Type; protected set => base.Type = value; }

    protected Transform baseTransform, leftLegTransform, rightLegTransform;
    protected SpriteRenderer leftLegSpriteRenderer, rightLegSpriteRenderer;
    protected float walkTime = 0f;
    protected Vector2 lastPosition;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        legSpeed = Type.velocityCap / Type.legStepDistance * 0.5f;
    }

    protected override void Update() {
        base.Update();

        // Get the amount of walked distance since the last frame, used for the leg visuals
        float walkedDistance = Vector2.Distance(lastPosition, transform.position);
        walkTime += walkedDistance / Type.velocityCap;
        lastPosition = transform.position;

        // Update the mech things
        HandleMech();
    }

    protected override void FixedUpdate() {
        base.FixedUpdate();
    }

    protected override void CreateTransforms() {
        // Instantiate the mech base
        baseTransform = new GameObject("Mech-base", typeof(SpriteRenderer)).transform;
        baseTransform.parent = transform;

        // Set up the mech's base
        baseTransform.localPosition = Vector3.zero;
        baseTransform.localRotation = Quaternion.identity;
        baseTransform.localScale = Vector3.one;

        // Instantiate the mech's left leg
        rightLegTransform = new GameObject("Mech-r-leg", typeof(SpriteRenderer)).transform;
        rightLegTransform.parent = baseTransform;

        // Set up the mech's left leg
        rightLegTransform.localPosition = Vector3.zero;
        rightLegTransform.localRotation = Quaternion.identity;
        rightLegTransform.localScale = Vector3.one;
        rightLegSpriteRenderer = rightLegTransform.GetComponent<SpriteRenderer>();

        // Instantiate the mech's right leg
        leftLegTransform = new GameObject("Mech-l-leg", typeof(SpriteRenderer)).transform;
        leftLegTransform.parent = baseTransform;

        // Set up the mech's right leg
        leftLegTransform.localPosition = Vector3.zero;
        leftLegTransform.localRotation = Quaternion.identity;
        leftLegTransform.localScale = Vector3.one;
        leftLegSpriteRenderer = leftLegTransform.GetComponent<SpriteRenderer>();
    }

    protected override void SetSprites() {
        base.SetSprites();

        // Set base/legs sprites
        SetOptionalSprite(baseTransform, Type.baseSprite);
        SetOptionalSprite(rightLegTransform, Type.legSprite);
        SpriteRenderer leftSpriteRenderer = SetOptionalSprite(leftLegTransform, Type.legSprite);

        // Flip the left leg sprite
        leftSpriteRenderer.flipX = true;
    }

    protected void HandleMech() {
        // Get the desired rotation of the base
        Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, ((Vector3)GetBehaviourPosition() - baseTransform.position).normalized);
        desiredRotation = Quaternion.Euler(0, 0, desiredRotation.eulerAngles.z);

        // Rotate the base transform
        float speed = Type.baseRotationSpeed * Time.fixedDeltaTime;
        baseTransform.rotation = Quaternion.RotateTowards(baseTransform.rotation, desiredRotation, speed);

        // Get the y position of each leg
        float legDistance = walkTime * legSpeed;

        float l = GetLegPosition(legDistance / Mathf.PI - 0.5f);
        float r = GetLegPosition(legDistance / Mathf.PI + 0.5f);

        /*
        float leftLegY = Mathf.Sin(legDistance) * Type.legStepDistance;
        float rightLegY = Mathf.Sin(legDistance + Mathf.PI) * Type.legStepDistance;
        */

        float leftLegY = l * Type.legStepDistance;
        float rightLegY = r * Type.legStepDistance;

        // Get the time in the walk cicle with half offset
        float leftLegT = Mathf.Sin(legDistance + Mathf.PI * 0.5f);
        float rightLegT = Mathf.Sin(legDistance + Mathf.PI * 1.5f);

        // Apply position and color to the left leg
        leftLegTransform.localPosition = new Vector3(0, leftLegY, 0);
        leftLegSpriteRenderer.color = Color.Lerp(Color.gray, Color.white, Mathf.Max(leftLegT, 0));

        // Apply position and color to the right leg
        rightLegTransform.localPosition = new Vector3(0, rightLegY, 0);
        rightLegSpriteRenderer.color = Color.Lerp(Color.gray, Color.white, Mathf.Max(rightLegT, 0));

        // Get the current sway of the unit
        float frontSway = Mathf.Sin(legDistance * 2f) * Type.frontSway;
        float sideSway = Mathf.Sin(legDistance) * Type.sideSway;

        // Apply sway to the unit body
        spriteHolder.transform.localPosition = new Vector3(sideSway, frontSway, 0);
    }

    private float GetLegPosition(float time) {
        float a = time % 1;
        float b = Mathf.Sign(time % 2 - 1);
        return b * (a - 0.5f);
    }

    public override TileType GetGroundTile() {
        // Since ground units are on the ground, find the under tile relative to the main unit body
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
