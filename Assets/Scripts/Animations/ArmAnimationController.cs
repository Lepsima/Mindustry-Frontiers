using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Frontiers.Content;

public class ArmAnimationController {
    public ArmAnimator[] armAnimators;

    public ArmAnimationController(ArmData[] arms) {
        foreach (ArmData arm in arms) {
            Transform baseArm = new GameObject("base-arm", typeof(SpriteRenderer), typeof(ArmAnimator)).transform;
            Transform middleArm = new GameObject("middle-arm", typeof(SpriteRenderer)).transform;
            Transform endArm = new GameObject("end-arm", typeof(SpriteRenderer)).transform;

            // Set middle arm transform
            middleArm.parent = baseArm;
            middleArm.SetLocalPositionAndRotation(arm.middleArmOffset, Quaternion.identity);

            // Set end arm transform
            endArm.parent = middleArm;
            endArm.SetLocalPositionAndRotation(arm.middleArmOffset, Quaternion.identity);

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