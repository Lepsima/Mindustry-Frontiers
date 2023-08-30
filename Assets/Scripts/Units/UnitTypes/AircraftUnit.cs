using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Content.Upgrades;
using Frontiers.Content.VisualEffects;

public class AircraftUnit : Unit {
    public new AircraftUnitType Type { get => (AircraftUnitType)base.Type; protected set => base.Type = value; }

    TrailRenderer rTrailRenderer;
    TrailRenderer lTrailRenderer;
    [SerializeField] ParticleSystem waterDeviationEffect;

    protected float targetHeight, targetVelocity, liftVelocity, takeoffAccel, lowestEnginePower;
    protected bool isFleeing, isWreck;

    #region - Upgradable Stats -

    protected float 
        drag, force, takeoffTime, 
        takeoffHeight, maxLiftVelocity;

    #endregion

    protected override void ApplyUpgrageMultiplier(UpgradeType upgrade) {
        base.ApplyUpgrageMultiplier(upgrade);

        UnitUpgradeMultipliers mult = upgrade.properties as UnitUpgradeMultipliers;

        drag += drag * mult.flying_drag;
        force += force * mult.flying_force;
        takeoffTime += takeoffTime * mult.flying_takeoffTime;
        takeoffHeight += takeoffHeight * mult.flying_takeoffHeight;
        maxLiftVelocity += maxLiftVelocity * mult.flying_maxLiftVelocity;

        takeoffAccel = takeoffHeight / takeoffTime;
    }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);

        drag = Type.drag;
        force = Type.force;
        takeoffTime = Type.takeoffTime;
        takeoffHeight = Type.takeoffHeight;
        maxLiftVelocity = Type.maxLiftVelocity;

        takeoffAccel = takeoffHeight / takeoffTime;
    }

    protected override void Update() {
        base.Update();
        SetDragTrailLenght(gForce * 0.3f);
    }

    public override void HandlePhysics() {
        Vector2 dragDir = velocity - (Vector2)Vector3.Project(velocity, transform.up);
        acceleration -= (1 - drag * Time.fixedDeltaTime) * dragDir;

        // Drag force inversely proportional to velocity
        acceleration -= (1 - drag * Time.fixedDeltaTime) * velocity;
        base.HandlePhysics();
    }

    public override void HandleHeight() {
        // If is landed, the unit shouldn't move
        if (isLanded) {
            height = 0f;
            liftVelocity = 0f;
            return;
        }

        if (isTakingOff) {
            // Apply a custom force to simulate takeoff
            liftVelocity += takeoffAccel * Time.fixedDeltaTime;

        } else {
            // Calculate lift force
            float engineAccel = force * CalculateLiftPower() / currentMass;
            float liftAccel = 0f;

            if (!isWreck) {
                // This gets the force the unit should make to not go full on kamikaze to the ground
                liftAccel = targetHeight - height < 0f ? 9.81f - engineAccel : engineAccel;
            }

            // Apply the calculated forces
            liftVelocity = Mathf.Clamp((liftAccel - 9.81f) * Time.fixedDeltaTime + liftVelocity, -maxLiftVelocity, maxLiftVelocity);
        }

        // Update the current height and size
        height = Mathf.Clamp(height + liftVelocity * Time.fixedDeltaTime, 0, Type.groundHeight);
        transform.localScale = Mathf.Lerp(0.75f, 1f, height / Type.groundHeight) * Type.size * Vector3.one;

        // If is touching and moving towards the ground, crash
        if (height == 0f && liftVelocity < 0f) {
            Crash();
            liftVelocity = 0f;
        }
    }

    public override void UpdateCurrentMass() {
        base.UpdateCurrentMass();

        // Get the minimum engine power to elevate with this mass
        lowestEnginePower = 9.81f * currentMass / force;
    }

    protected void SetDragTrailLenght(float time) {
        if (!Type.hasDragTrails) return;

        // Set drag trail time
        time = Mathf.Abs(time);
        rTrailRenderer.time = time;
        lTrailRenderer.time = time;
    }

    protected override void CreateTransforms() {
        base.CreateTransforms();

        if (Type.useAerodynamics) {
            waterDeviationEffect = shadow.transform.CreateEffect(Effects.waterDeviation, Vector2.zero, Quaternion.identity);
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
        } else {
            base.Kill(destroyed);           
        }
    }


    #region - Math & Getters -

    public override Vector2 GetDirection(Vector2 target) => Type.useAerodynamics ? transform.up : base.GetDirection(target);

    public override float GetTargetVelocity() => targetVelocity;

    public override void Tilt(float targetAngle) {
        // Lerps the roll rotation of the unit transform towards the target angle
        float lerpVal = Mathf.LerpAngle(spriteHolder.localEulerAngles.y, targetAngle, Type.bankSpeed * Time.fixedDeltaTime);
        spriteHolder.localEulerAngles = new Vector3(0, lerpVal, 0);
    }

    protected override bool StopsToShoot() {
        return !Type.useAerodynamics;
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

    public override bool IsWreck() {
        return isWreck;
    }

    public override bool CanMove() {
        return base.CanMove() && !isTakingOff;
    }

    public override bool CanRotate() {
        return base.CanRotate() && !isTakingOff;
    }

    public override float CalculateEnginePower() {
        return Mathf.Min(base.CalculateEnginePower(), Mathf.Clamp01(targetVelocity - velocity.magnitude));
    }

    public virtual float CalculateLiftPower() {
        return base.CalculateEnginePower();
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
        Invoke(nameof(EndTakeOff), takeoffTime);

        //Play particle system
        EffectPlayer.PlayEffect(Effects.takeoff, transform.position, size);
    }

    protected override void EndTakeOff() {
        base.EndTakeOff();

        // Apply takeoff boost
        // TODO
        velocity = force / 3 * transform.up;
    }

    // Dock into a landing pad
    public override void Dock(LandPadBlock landpad) {
        base.Dock(landpad);

        height = 0f;
        liftVelocity = 0f;
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

    public void Flee(bool value) {
        isFleeing = value;
        canLoseTarget = !value;

        if (value) {
            patrolPosition = GetPosition() + (Vector2)transform.up * 25f;
            MaintainWeaponsActive(0.5f);
        }
    }

    protected override void AttackBehaviour() {
        targetHeight = Type.groundHeight;
        targetVelocity = maxVelocity;
        targetPower = 1f;

        //If is landed, takeoff, else do normal behaviour
        if (isLanded) TakeOff();
        else if (!isTakingOff) base.AttackBehaviour();

        if (IsFleeing()) {
            // If close enough to flee position, stop fleeing, else continue fleeing
            if (Vector2.Distance(patrolPosition, GetPosition()) < 5f) Flee(false);
            else SetBehaviourPosition(patrolPosition);
        } else if (Target && Type.useAerodynamics && Vector2.Distance(Target.GetPosition(), transform.position) < 2f) { 
            Flee(true);
        }
    }

    protected override void PatrolBehaviour() {
        base.PatrolBehaviour();

        targetHeight = Type.groundHeight;
        targetVelocity = maxVelocity * 0.75f;
        targetPower = 0.9f;

        //If is landed, takeoff, else do normal behaviour
        if (isLanded) TakeOff();
        else if (!isTakingOff) base.PatrolBehaviour();
    }

    protected override void ReturnBehaviour() {
        if (isLanded) return;

        targetHeight = Type.groundHeight * 0.5f;
        targetVelocity = maxVelocity * 0.5f;
        targetPower = Mathf.Max(lowestEnginePower, 0.5f);

        if (Target) {
            float distance = Vector2.Distance(Target.GetPosition(), GetPosition());
            bool isCloseToLandpad = distance < Target.size / 2 + 7.5f;

            // Lower even more the velocity if is near the landing pad
            if (isCloseToLandpad) {
                targetHeight = 2.5f;
                targetVelocity = 3f; 
                targetPower = Mathf.Clamp01(lowestEnginePower + 0.1f);
            }
        }

        base.ReturnBehaviour();
    }

    protected override void AssistBehaviour() {
        targetHeight = Type.groundHeight;
        targetVelocity = maxVelocity;
        targetPower = 1f;

        base.AssistBehaviour();
    }

    protected override void IdlingBehaviour() {
        targetHeight = Type.groundHeight * 0.5f;
        targetVelocity = maxVelocity * 0.5f;
        targetPower = Mathf.Clamp01(lowestEnginePower + 0.1f);

        base.IdlingBehaviour();
    }

    #endregion
}