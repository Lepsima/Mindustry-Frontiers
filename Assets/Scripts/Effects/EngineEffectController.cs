using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineEffectController : MonoBehaviour {
    public int engineSize;
    public float targetLength;

    float realLength = 0;
    float oscillationDiff = 0;

    public void UpdateEngineEffect() {
        // Update length
        float diff = targetLength - realLength;
        float change = Time.deltaTime * Mathf.Clamp(diff, -7.5f, 7.5f);
        realLength += change;

        // Get oscillation
        oscillationDiff = Mathf.Sin(Time.time * 15f) * 0.1f * realLength;

        // Set scale
        transform.localScale = new Vector2(engineSize, engineSize * (realLength + oscillationDiff));
    }
}
