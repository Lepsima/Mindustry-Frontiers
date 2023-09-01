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
            Transform baseArm;
            Transform middleArm;
            Transform endArm;
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