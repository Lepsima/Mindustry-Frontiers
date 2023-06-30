using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;

public class AircraftUnit : Unit {
    public new AircraftUnitType Type { get => (AircraftUnitType)base.Type; protected set => base.Type = value; }

    TrailRenderer rTrailRenderer;
    TrailRenderer lTrailRenderer;
    [SerializeField] ParticleSystem waterDeviationEffect;

    protected float targetHeight;
    protected bool isFleeing;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
    }

    protected override void Update() {
        base.Update();
        SetDragTrailLenght(gForce * 0.3f);
    }

    protected override void FixedUpdate() {
        base.FixedUpdate();
    }

    public override void ChangeMode(int mode, bool registerPrev) {
        base.ChangeMode(mode, registerPrev);
        isFleeing = false;
    }

    public override void HandlePhysics() {
        // Drag force inversely proportional to velocity
        acceleration -= (1 - Type.drag * 0.33f * Time.fixedDeltaTime) * (velocity * transform.up);

        // Drag force inversely proportional to direction
        acceleration -= (1 - Type.drag * 0.67f * Time.fixedDeltaTime) * velocity;

        base.HandlePhysics();
    }

    public override void HandleHeight() {
        // If is taking off climb until half fly height
        if (isTakingOff) {
            ChangeHeight(false);
        } else {
            // Change height increase or decrease based on velocity
            bool isFalling = velocity.sqrMagnitude <= 0.05f || IsFalling();
            ChangeHeight(isFalling);
        }
    }

    protected void SetDragTrailLenght(float time) {
        time = Mathf.Abs(time);
        rTrailRenderer.time = time;
        lTrailRenderer.time = time;
    }

    protected override void CreateTransforms() {
        waterDeviationEffect = transform.CreateEffect("WaterDeviationFX", Vector2.zero, Quaternion.identity, 0f);

        // Get the prefab for the trail
        GameObject prefab = AssetLoader.GetPrefab("UnitTrail");

        // Get the opposite trail offset
        Vector2 leftOffset = Type.trailOffset;
        leftOffset.x *= -1f;

        // Instantiate the right sided trail
        rTrailRenderer = Instantiate(prefab, transform).GetComponent<TrailRenderer>();
        rTrailRenderer.transform.localPosition = Type.trailOffset;

        // Instantiate the left sided trail
        lTrailRenderer = Instantiate(prefab, transform).GetComponent<TrailRenderer>();
        lTrailRenderer.transform.localPosition = leftOffset;
    }


    #region - Math & Getters -

    public override Vector2 GetDirection(Vector2 target) => Type.useAerodynamics ? transform.up : (target - GetPosition()).normalized;

    public override void Tilt(float targetAngle) {
        // Lerps the roll rotation of the unit transform towards the target angle
        float lerpVal = Mathf.LerpAngle(spriteHolder.localEulerAngles.y, targetAngle, Type.bankSpeed * Time.fixedDeltaTime);
        spriteHolder.localEulerAngles = new Vector3(0, lerpVal, 0);
    }

    public void ChangeHeight(bool isFalling) {
        // Default values
        float liftForce = 3;
        float fallForce = -3;

        // Update the current height
        height = Mathf.Clamp((isFalling ? fallForce : liftForce) * Time.fixedDeltaTime + height, 0, Type.groundHeight);

        // If is touching ground, crash
        if (height < 0.05f) Land();
    }

    protected override bool DoesStopToShoot() {
        return !Type.useAerodynamics;
    }

    public override float GetEnginePower() {
        // Get the percent of power the engine should produce
        float enginePower = base.GetEnginePower();
        if (height > targetHeight) enginePower *= 0.75f;
        return enginePower * (height / Type.groundHeight);
    }

    public override float GetRotationPower() {
        // Get the power at wich the unit should rotate
        float power = Mathf.Clamp01(2 / gForce);
        float distance = Vector2.Distance(GetBehaviourPosition(), transform.position);
        if (distance < 5f || isFleeing) power *= Mathf.Clamp01(distance / 10);
        return power;
    }

    public bool IsFalling() {
        return targetHeight < height;
    }

    public override bool IsFleeing() {
        return isFleeing;
    }

    public override bool CanMove() {
        return base.CanRotate() && !isTakingOff;
    }

    public override bool CanRotate() {
        return base.CanRotate() && !isTakingOff;
    }
    #endregion

    #region - Landing / Takeoff - 
    public override void OnTakeOff() {
        if (isTakingOff || !isLanded) return;

        //Start takeOff
        isTakingOff = true;
        isLanded = false;
        velocity = Vector2.zero;

        //Allow free movement in ±3s
        Invoke(nameof(EndTakeOff), 3f);

        //Play particle system
        Effect.PlayEffect("TakeoffFX", transform.position, size);
    }

    protected override void EndTakeOff() {
        base.EndTakeOff();

        // Apply takeoff boost
        velocity = Type.force / 3 * transform.up;
    }

    public override void Land() {
        // If the current target is a landing pad
        if (Target is LandPadBlock landPad) {

            // Check if it's close enough to land
            float distance = Vector2.Distance(landPad.GetPosition(), transform.position);
            if (distance > landPad.size * 0.75f) return;

            //Land on landpad
            if (!landPad.Land(this)) {
                Client.DestroyUnit(this, true);
                return;
            }

            // When landed, set the current landing pad to the target one
            currentLandPadBlock = landPad;

            //Set landed true and stop completely the unit
            isLanded = true;
            velocity = Vector2.zero;
            height = 0f;
            SetDragTrailLenght(0);
        }
    }

    #endregion

    #region - Events - 

    protected override void OnFloorTileChange() {
        base.OnFloorTileChange();

        // Change the water deviation emmision property depending on the tile below the shadow
        bool isWater = FloorTile != null && FloorTile.isWater;
        ParticleSystem.EmissionModule emissionModule = waterDeviationEffect.emission;
        emissionModule.rateOverDistanceMultiplier = isWater ? 5f : 0f;
    }

    #endregion

    #region - Behaviour -

    protected override void AttackBehaviour() {
        targetHeight = Type.groundHeight;
        targetSpeed = 1f;

        //If is landed, takeoff, else do normal behaviour
        if (isLanded) TakeOff();
        else if (!isTakingOff) base.AttackBehaviour();

        if (IsFleeing()) {
            // If close enough to flee position, stop fleeing, else continue fleeing
            if (Vector2.Distance(patrolPosition, GetPosition()) < 5f) isFleeing = false;
            else SetBehaviourPosition(patrolPosition);
        }

        if (Type.useAerodynamics && Vector2.Distance(Target.GetPosition(), transform.position) < 2f) {
            isFleeing = true;
            patrolPosition = GetPosition() + (Vector2)transform.up * 25f;

            // Continue shooting for x seconds
            MaintainWeaponsActive(0.5f);
        }
    }

    protected override void PatrolBehaviour() {
        base.PatrolBehaviour();

        targetHeight = Type.groundHeight;
        targetSpeed = 0.9f;

        //If is landed, takeoff, else do normal behaviour
        if (isLanded) TakeOff();
        else if (!isTakingOff) base.PatrolBehaviour();
    }

    protected override void ReturnBehaviour() {
        if (isLanded) return;

        targetHeight = Type.groundHeight * 0.5f;
        targetSpeed = 0.5f;

        base.ReturnBehaviour();
    }

    protected override void AssistBehaviour() {
        targetHeight = Type.groundHeight;
        targetSpeed = 1f;

        base.AssistBehaviour();
    }

    protected override void IdlingBehaviour() {
        targetHeight = Type.groundHeight * 0.5f;
        targetSpeed = 0.5f;

        base.IdlingBehaviour();
    }

    #endregion
}