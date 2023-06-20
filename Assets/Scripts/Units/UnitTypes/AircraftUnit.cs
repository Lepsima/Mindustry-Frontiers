using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public class AircraftUnit : Unit {
    public new AircraftUnitType Type { get => (AircraftUnitType)base.Type; protected set => base.Type = value; }
    private LandPadBlock currentLandPadBlock;

    protected float targetHeight;
    protected bool isTakingOff, isFleeing;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
    }

    protected override void Update() {
        base.Update();
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

    public override Vector2 GetDirection(Vector2 target) => Type.useAerodynamics ? (Vector2)transform.up : (target - GetPosition()).normalized;

    public void Tilt(float targetAngle) {
        float lerpVal = Mathf.LerpAngle(spriteHolder.localEulerAngles.y, targetAngle, Type.bankSpeed * Time.fixedDeltaTime);
        spriteHolder.localEulerAngles = new Vector3(0, lerpVal, 0);
    }

    public void ChangeHeight(bool isFalling) {
        float liftForce = 3;
        float fallForce = -3;
        height = Mathf.Clamp((isFalling ? fallForce : liftForce) * Time.fixedDeltaTime + height, 0, Type.groundHeight);

        // If is touching ground, crash
        if (height < 0.05f) Land(null);
    }

    protected override bool StopToShoot() {
        return !Type.useAerodynamics;
    }

    public override float GetEnginePower() {
        // Get the percent of power the engine should produce
        float enginePower = Mathf.Clamp01(fuel > 0f ? 1f : 0f) * targetSpeed;
        if (height > targetHeight) enginePower *= 0.75f;
        return enginePower * (height / Type.groundHeight);
    }

    public override float GetRotationPower() {
        float power = Mathf.Clamp01(2 / gForce);
        float distance = Vector2.Distance(GetBehaviourPosition(), transform.position);
        if (distance < 5f || isFleeing) power *= Mathf.Clamp01(distance / 10);
        return power;
    }

    public bool IsFalling() {
        return targetHeight < height;
    }
    public bool IsFleeing() {
        return isFleeing;
    }

    public override bool CanMove() {
        return base.CanRotate() && !isTakingOff;
    }

    public override bool CanRotate() {
        return base.CanRotate() && !isTakingOff;
    }

    #region - Landing / Takeoff - 

    //Land unit on the ground, if obstructed: crash, THIS CURRENTLY CRASHES(into the map) THE UNIT ALWAYS
    public void Land() {
        MapCrash();
    }

    public void Land(LandPadBlock landPad) {
        //Land on landpad
        if (!landPad.Land(this)) return;
        currentLandPadBlock = landPad;

        //Set landed true and stop completely the unit
        isLanded = true;
        velocity = Vector2.zero;
        //spriteHolder.localScale = Vector3.one * 0.7f;

        height = 0f;
        SetDragTrailLenght(0);
    } //Land unit on a near landpad


    //When crash into the map
    public void MapCrash() {
        //Crash effects: TODO
        Client.DestroyUnit(this, true);
    }


    //Take off from land
    public void TakeOff() {
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


    //Enable physics movement
    private void EndTakeOff() {
        //If is landed on a landpad, takeoff from it
        if (currentLandPadBlock) currentLandPadBlock.TakeOff(this);
        currentLandPadBlock = null;

        //Takeoff ended, allowing free movement
        isTakingOff = false;
        //spriteHolder.localScale = Vector3.one;
        velocity = Type.force / 3 * transform.up;
    }
    #endregion

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
}