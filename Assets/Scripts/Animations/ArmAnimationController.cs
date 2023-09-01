using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ArmAnimationController : MonoBehaviour {
    public ArmAnimator[] armAnimators;
    public bool StartAnim = true;
    private bool prev = false;

    private void Update() {
        if (prev == StartAnim) return;

        if (StartAnim) StartArmAnimations();
        else StopArmAnimations();

        prev = StartAnim;
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