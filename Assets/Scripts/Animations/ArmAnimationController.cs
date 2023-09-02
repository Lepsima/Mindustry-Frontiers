using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Frontiers.Content;

public class ArmAnimationController {
    public ArmAnimator[] armAnimators;

    public ArmAnimationController(Transform parent, ArmData[] arms) {
        armAnimators = new ArmAnimator[arms.Length];

        for (int i = 0; i < arms.Length; i++) {
            ArmData arm = arms[i];

            Transform baseArm = new GameObject("base-arm", typeof(SpriteRenderer), typeof(ArmAnimator)).transform;
            Transform middleArm = new GameObject("middle-arm", typeof(SpriteRenderer)).transform;
            Transform endArm = new GameObject("end-arm", typeof(SpriteRenderer)).transform;

            // Set base arm transform
            baseArm.parent = parent;
            baseArm.SetLocalPositionAndRotation(arm.idlePosition, Quaternion.Euler(0, 0, arm.idleAngle));

            SpriteRenderer spriteRenderer = baseArm.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = arm.baseSprite;
            spriteRenderer.sortingLayerName = "Blocks";
            spriteRenderer.sortingOrder = 4;

            // Set middle arm transform
            middleArm.parent = baseArm;
            middleArm.SetLocalPositionAndRotation(arm.middleArmOffset, Quaternion.identity);

            spriteRenderer = middleArm.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = arm.middleSprite;
            spriteRenderer.sortingLayerName = "Blocks";
            spriteRenderer.sortingOrder = 5;

            // Set end arm transform
            endArm.parent = middleArm;
            endArm.SetLocalPositionAndRotation(-arm.middleArmOffset, Quaternion.identity);

            spriteRenderer = endArm.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = arm.endSprite;
            spriteRenderer.sortingLayerName = "Blocks";
            spriteRenderer.sortingOrder = 6;

            // Instantiate animator component
            ArmAnimator armAnimator = baseArm.GetComponent<ArmAnimator>();
            armAnimator.Init(arm);
            armAnimators[i] = armAnimator;
        }
    }

    public void StartArmAnimations() {
        if (armAnimators == null) return;

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