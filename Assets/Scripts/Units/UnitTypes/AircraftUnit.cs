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

    protected float targetHeight, liftVelocity = 0f;
    protected bool isFleeing, isWreck;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
    }

    protected override void Update() {
        base.Update();
        SetDragTrailLenght(gForce * 0.3f);
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
        if (isTakingOff) {
            // Apply a custom force to simulate takeoff
            float takeoffAccel = (2f * Type.groundHeight * Type.takeoffHeight) / (Type.takeoffTime * Type.takeoffTime);
            liftVelocity += takeoffAccel * Time.fixedDeltaTime;

        } else {
            // Calculate lift force
            float liftAccel = isWreck ? 0 : Type.force * GetEnginePower() / currentMass;
            liftVelocity = Mathf.Clamp((liftAccel - 9.81f) * Time.fixedDeltaTime + liftVelocity, -5f, Type.maxLiftVelocity);
        }

        // Update the current height
        height = Mathf.Clamp(height + liftVelocity * Time.fixedDeltaTime, 0, Type.groundHeight);

        // If is touching and moving towards the ground, crash
        if (height == 0f && liftVelocity < 0f) {
            Crash();
            liftVelocity = 0f;
        }
    }

    protected void SetDragTrailLenght(float time) {
        if (!Type.hasDragTrails) return;
        time = Mathf.Abs(time);
        rTrailRenderer.time = time;
        lTrailRenderer.time = time;
    }

    protected override void CreateTransforms() {
        if (Type.useAerodynamics) {
            waterDeviationEffect = transform.CreateEffect("WaterDeviationFX", Vector2.zero, Quaternion.identity, 0f);
        }

        if (Type.hasDragTrails) {
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
    }

    public override void Kill(bool destroyed) {
        if (!isWreck && Type.hasWreck && destroyed) {
            health = Type.wreckHealth;
            isWreck = true;
        }
        else {
            base.Kill(destroyed);           
        }
    }


    #region - Math & Getters -

    public override Vector2 GetDirection(Vector2 target) => Type.useAerodynamics ? transform.up : (target - GetPosition()).normalized;

    public override void Tilt(float targetAngle) {
        // Lerps the roll rotation of the unit transform towards the target angle
        float lerpVal = Mathf.LerpAngle(spriteHolder.localEulerAngles.y, targetAngle, Type.bankSpeed * Time.fixedDeltaTime);
        spriteHolder.localEulerAngles = new Vector3(0, lerpVal, 0);
    }

    protected override bool StopsToShoot() {
        return !Type.useAerodynamics;
    }

    public override float CalculateEnginePower() {
        // Get the percent of power the engine should produce
        float enginePower = base.CalculateEnginePower();
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
        return base.CanMove() && !isTakingOff;
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

        //Allow free movement
        Invoke(nameof(EndTakeOff), Type.takeoffTime);

        //Play particle system
        Effect.PlayEffect("TakeoffFX", transform.position, size);
    }

    protected override void EndTakeOff() {
        base.EndTakeOff();

        // Apply takeoff boost
        // TODO
        velocity = Type.force / 3 * transform.up;
    }

    public override void Dock(LandPadBlock landpad) {
        base.Dock(landpad);

        height = 0f;
        SetDragTrailLenght(0);
    }

    #endregion

    #region - Events - 

    protected override void OnFloorTileChange() {
        base.OnFloorTileChange();

        if (Type.useAerodynamics) {
            // Change the water deviation emmision property depending on the tile below the shadow
            bool isWater = FloorTile != null && FloorTile.isWater;
            ParticleSystem.EmissionModule emissionModule = waterDeviationEffect.emission;
            emissionModule.rateOverDistanceMultiplier = isWater ? 5f : 0f;
        }
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

        if (Target && Type.useAerodynamics && Vector2.Distance(Target.GetPosition(), transform.position) < 2f) {
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
        targetSpeed = 0.55f;

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