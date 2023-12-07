using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationInfo : MonoBehaviour {
    public float length;

    public EventHandler OnAnimationEnd;

    private void OnEnable() {
        OnAnimationEnd?.Invoke(this, EventArgs.Empty);
    }
}