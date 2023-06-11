using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthChangeEffect {
    public static Color damageColor, healColor, baseColor;

    float progress;

    static HealthChangeEffect() {
        damageColor = Color.red;
        healColor = Color.green;
        baseColor = Color.white;
    }

    public HealthChangeEffect(Sprite[] affectedSprites) {

    }

    public void Start(float amount) {
        
    }
}