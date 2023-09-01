using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using Frontiers.Content;
using Frontiers.Content.VisualEffects;

public class ArmAnimator : MonoBehaviour {
    ArmData armData;

    Transform armBase;
    Transform armMiddle;
    Transform armEnd;

    ParticleSystem weldParticleSystem;

    ArmState state, previousState;
    float progress;
    bool stopOnEnd = false;

    public struct ArmState {
        public Vector2 position;
        public Vector2 endTarget;
        public float angle;
        public float length;

        public ArmState(Vector2 position, Vector2 endTarget, float angle, float length) {
            this.position = position;
            this.endTarget = endTarget;
            this.angle = angle;
            this.length = length;
        }

        public readonly (Vector2, Vector2, float) Lerp(ArmState other, float progress) {
            return (Vector2.Lerp(other.position, position, progress), Vector2.Lerp(other.endTarget, endTarget, progress), Mathf.Lerp(other.angle, angle, progress));
        }
    }

    public void Init(ArmData armData) {
        this.armData = armData;

        armBase = transform;
        armMiddle = armBase.GetChild(0);
        armEnd = armMiddle.GetChild(0);

        weldParticleSystem = armEnd.CreateEffect(armData.effect, Vector2.zero, Quaternion.identity, 1f);
    }

    public void StartAnimation() {
        state = IdleState();
        previousState = IdleState();
        progress = 0;

        enabled = true;
        weldParticleSystem.Play();
    }

    public void StopAnimation() {
        SetState(IdleState());
        weldParticleSystem.Stop();
        stopOnEnd = true;
    }

    private void Update() {
        progress = Mathf.Clamp01(progress + Time.deltaTime / state.length);

        (Vector2, Vector2, float) lerpState = state.Lerp(previousState, progress);

        // Set base position and rotation
        armBase.localPosition = lerpState.Item1;
        armBase.eulerAngles = new Vector3(0, 0, lerpState.Item3);

        // Set others rotations
        armMiddle.eulerAngles = new Vector3(0, 0, -lerpState.Item3);
        armEnd.transform.up = EndArmDirectionTo(lerpState.Item2);

        if (progress >= 1) {
            if (stopOnEnd) enabled = stopOnEnd = false;
            else SetState(GenerateState());
        }
    }

    public void SetState(ArmState newState) {
        previousState = state;
        state = newState;
        progress = 0f;
    }

    public Vector2 EndArmDirectionTo(Vector3 position) {
        return transform.parent.position + position - armEnd.transform.position;
    }

    public ArmState IdleState() {
        return new ArmState(armData.idlePosition, transform.parent.position, armData.idleAngle, 1f);
    }

    public ArmState GenerateState() {
        // The position the arm should move at
        Vector2 position = Vector2.Lerp(armData.minPosition, armData.maxPosition, Random.Range(0f, 1f));

        // The position the welder should look at
        Vector2 target = new(Random.Range(-armData.maxTargetOffset.x, armData.maxTargetOffset.x), Random.Range(-armData.maxTargetOffset.y, armData.maxTargetOffset.y));
        
        // Generate new state
        return new ArmState(position, target, Random.Range(armData.minBaseAngle, armData.maxBaseAngle), Random.Range(armData.minTime, armData.maxTime));
    }
}
