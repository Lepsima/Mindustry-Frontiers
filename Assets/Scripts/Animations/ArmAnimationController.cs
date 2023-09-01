using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Frontiers.Content;

public class ArmAnimationController {
    public ArmAnimator[] armAnimators;

    public ArmAnimationController(Transform parent, ArmData[] arms) {
        foreach (ArmData arm in arms) {
            Transform baseArm = new GameObject("base-arm", typeof(SpriteRenderer), typeof(ArmAnimator)).transform;
            Transform middleArm = new GameObject("middle-arm", typeof(SpriteRenderer)).transform;
            Transform endArm = new GameObject("end-arm", typeof(SpriteRenderer)).transform;

            // Set base arm transform
            baseArm.parent = parent;
            baseArm.SetLocalPositionAndRotation(arm.idlePosition, Quaternion.Euler(0, 0, arm.idleAngle));
            baseArm.GetComponent<SpriteRenderer>().sprite = arm.baseSprite;

            // Set middle arm transform
            middleArm.parent = baseArm;
            middleArm.SetLocalPositionAndRotation(arm.middleArmOffset, Quaternion.identity);
            middleArm.GetComponent<SpriteRenderer>().sprite = arm.middleSprite;

            // Set end arm transform
            endArm.parent = middleArm;
            endArm.SetLocalPositionAndRotation(-arm.middleArmOffset, Quaternion.identity);
            endArm.GetComponent<SpriteRenderer>().sprite = arm.endSprite;

            // Instantiate animator component
            ArmAnimator armAnimator = baseArm.GetComponent<ArmAnimator>();
            armAnimator.Init(arm);
        }
    }

    public void StartArmAnimations() {
        foreach(ArmAnimator arm in armAnimators) {
            arm.StartAnimation();
        }
    }

    public void StopArmAnimations() {
        foreach (ArmAnimator arm in armAnimators) {
            arm.StopAnimation();
        }
    }
}